using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using Cairo;
using OpenTK.Audio.OpenAL;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SkiaSharp;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.ClientNative;
using Vintagestory.Common;
using Vintagestory.Common.Convert;
using VSPlatform;

namespace Vintagestory.Client.NoObf
{
	public sealed class ClientPlatformWindows : ClientPlatformAbstract
	{
		public void StartAudio()
		{
			if (this.audio == null)
			{
				this.audio = new AudioOpenAl(this.logger);
			}
		}

		public override void AddAudioSettingsWatchers()
		{
			ClientSettings.Inst.AddWatcher<string>("audioDevice", delegate(string newDevice)
			{
				this.CurrentAudioDevice = newDevice;
			});
			ClientSettings.Inst.AddWatcher<bool>("useHRTFaudio", delegate(bool _)
			{
				LoadedSoundNative.ChangeOutputDevice(delegate
				{
					this.audio.RecreateContext(this.logger);
				});
			});
		}

		public void StopAudio()
		{
			ILoadedSound introMusic = ScreenManager.IntroMusic;
			if (introMusic != null)
			{
				introMusic.Dispose();
			}
			if (this.audio != null)
			{
				this.audio.Dispose();
				this.audio = null;
			}
		}

		public override IList<string> AvailableAudioDevices
		{
			get
			{
				return this.audio.Devices;
			}
		}

		public override string CurrentAudioDevice
		{
			get
			{
				return this.audio.CurrentDevice;
			}
			set
			{
				LoadedSoundNative.ChangeOutputDevice(delegate
				{
					this.audio.RecreateContext(this.logger);
				});
			}
		}

		public override AudioData CreateAudioData(IAsset asset)
		{
			this.StartAudio();
			AudioMetaData sampleFromArray = this.audio.GetSampleFromArray(asset);
			sampleFromArray.Loaded = 2;
			return sampleFromArray;
		}

		public override float MasterSoundLevel
		{
			get
			{
				return this.audio.MasterSoundLevel;
			}
			set
			{
				this.audio.MasterSoundLevel = value;
			}
		}

		public override ILoadedSound CreateAudio(SoundParams sound, AudioData data)
		{
			if ((data as AudioMetaData).Asset == null)
			{
				return null;
			}
			if (data.Loaded < 2)
			{
				if (data.Loaded == 0)
				{
					LoggerBase loggerBase = this.logger;
					string text = "Loading sound file, game may stutter ";
					AudioMetaData audioMetaData = data as AudioMetaData;
					loggerBase.VerboseDebug(text + ((audioMetaData != null) ? audioMetaData.Asset.Location : null));
					data.Load();
				}
				else
				{
					LoggerBase loggerBase2 = this.logger;
					string text2 = "Attempt to use still-loading sound file, sound may error or not play ";
					AudioMetaData audioMetaData2 = data as AudioMetaData;
					loggerBase2.VerboseDebug(text2 + ((audioMetaData2 != null) ? audioMetaData2.Asset.Location : null));
				}
			}
			return new LoadedSoundNative(sound, (AudioMetaData)data);
		}

		public override ILoadedSound CreateAudio(SoundParams sound, AudioData data, ClientMain game)
		{
			AudioMetaData audioMetaData = data as AudioMetaData;
			if (((audioMetaData != null) ? audioMetaData.Asset : null) == null)
			{
				return null;
			}
			if (data.Loaded == 0)
			{
				LoggerBase loggerBase = this.logger;
				string text = "Loading sound file, game may stutter ";
				AudioMetaData audioMetaData2 = data as AudioMetaData;
				loggerBase.VerboseDebug(text + ((audioMetaData2 != null) ? audioMetaData2.Asset.Location : null));
				data.Load();
			}
			return new LoadedSoundNative(sound, (AudioMetaData)data, game);
		}

		public override void UpdateAudioListener(float posX, float posY, float posZ, float orientX, float orientY, float orientZ)
		{
			this.StartAudio();
			this.audio.UpdateListener(new Vector3(posX, posY, posZ), new Vector3(orientX, orientY, orientZ));
		}

		public ClientPlatformWindows(Logger logger)
		{
			if (logger == null)
			{
				this.logger = new NullLogger();
			}
			else
			{
				base.XPlatInterface = XPlatformInterfaces.GetInterface();
				this.screensize = base.XPlatInterface.GetScreenSize();
				this.logger = logger;
			}
			TyronThreadPool.Inst.Logger = this.logger;
			this.uptimeStopWatch.Start();
			this.frameStopWatch = new Stopwatch();
			this.frameStopWatch.Start();
		}

		private void window_RenderFrame(FrameEventArgs e)
		{
			if (this.doResize != 0 && Environment.TickCount >= this.doResize)
			{
				this.Window_Resize();
			}
			ScreenManager.FrameProfiler.Begin(null, Array.Empty<object>());
			if (ClientSettings.VsyncMode != 1 && base.MaxFps > 10f && base.MaxFps < 241f)
			{
				int freeTime = (int)(1000f / base.MaxFps - 1000f * (float)this.frameStopWatch.ElapsedTicks / (float)Stopwatch.Frequency);
				if (freeTime > 0)
				{
					Thread.Sleep(freeTime);
				}
			}
			float dt = (float)this.frameStopWatch.ElapsedTicks / (float)Stopwatch.Frequency;
			this.frameStopWatch.Restart();
			ScreenManager.FrameProfiler.Mark("sleep");
			this.UpdateMousePosition();
			this.RenderBloom = ClientSettings.Bloom && base.DoPostProcessingEffects;
			this.RenderGodRays = ClientSettings.GodRayQuality > 0 && base.DoPostProcessingEffects;
			this.RenderFXAA = ClientSettings.FXAA && base.DoPostProcessingEffects;
			this.RenderSSAO = ClientSettings.SSAOQuality > 0 && base.DoPostProcessingEffects;
			this.SetupSSAO = ClientSettings.SSAOQuality > 0;
			this.ShadowMapQuality = ClientSettings.ShadowMapQuality;
			ShaderProgramBase.shadowmapQuality = this.ShadowMapQuality;
			this.frameHandler.OnNewFrame(dt);
			this.window.SwapBuffers();
			ScreenManager.FrameProfiler.End();
		}

		public override AssetManager AssetManager
		{
			get
			{
				return this.assetManager;
			}
		}

		public override ILogger Logger
		{
			get
			{
				return this.logger;
			}
		}

		public string GetGraphicsCardRenderer()
		{
			return GL.GetString(StringName.Renderer);
		}

		public void LogAndTestHardwareInfosStage1()
		{
			this.logger.Notification("Process path: {0}", new object[] { StringUtil.SanitizePath(Environment.ProcessPath) });
			this.logger.Notification("Operating System: " + RuntimeEnv.GetOsString());
			this.logger.Notification("CPU Cores: {0}", new object[] { Environment.ProcessorCount });
			this.logger.Notification("CPU: {0}", new object[] { base.XPlatInterface.GetCpuInfo() });
			this.logger.Notification("Available RAM: {0} MB", new object[] { base.XPlatInterface.GetRamCapacity() / 1024L });
			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				this.LogFrameworkVersions();
			}
		}

		private void LogFrameworkVersions()
		{
			this.logger.Notification("C# Framework: " + this.GetFrameworkInfos());
			this.logger.Notification("Cairo Graphics Version: " + CairoAPI.VersionString);
		}

		public void LogAndTestHardwareInfosStage2()
		{
			this.logger.Notification("Graphics Card Vendor: " + GL.GetString(StringName.Vendor));
			this.logger.Notification("Graphics Card Version: " + GL.GetString(StringName.Version));
			this.logger.Notification("Graphics Card Renderer: " + GL.GetString(StringName.Renderer));
			this.logger.Notification("Graphics Card ShadingLanguageVersion: " + GL.GetString(StringName.ShadingLanguageVersion));
			this.logger.Notification("GL.MaxVertexUniformComponents: " + GL.GetInteger(GetPName.MaxVertexUniformComponents).ToString());
			this.logger.Notification("GL.MaxUniformBlockSize: " + GL.GetInteger(GetPName.MaxUniformBlockSize).ToString());
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				this.LogFrameworkVersions();
			}
			this.logger.Notification("OpenAL Version: " + AL.Get(ALGetString.Version));
			string path = Path.Combine(GamePaths.Binaries, "Lib/OpenTK.dll");
			if (File.Exists(path))
			{
				FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(path);
				this.logger.Notification(string.Concat(new string[] { "OpenTK Version: ", fvi.FileVersion, " (", fvi.Comments, ")" }));
			}
			LoggerBase loggerBase = this.logger;
			string text = "Zstd Version: ";
			Version version = ZstdNative.Version;
			loggerBase.Notification(text + ((version != null) ? version.ToString() : null));
			this.CheckGlError("loghwinfo");
			if (RuntimeEnv.OS != OS.Mac && ClientSettings.TestGlExtensions)
			{
				HashSet<string> required = new HashSet<string>(new string[] { "GL_ARB_framebuffer_object", "GL_ARB_vertex_array_object", "GL_ARB_draw_instanced", "GL_ARB_explicit_attrib_location" });
				int count = GL.GetInteger(GetPName.NumExtensions);
				for (int i = 0; i < count; i++)
				{
					string extension = GL.GetString(StringNameIndexed.Extensions, i);
					this.supportsGlDebugMode |= extension == "GL_ARB_debug_output" || extension == "GL_KHR_debug";
					this.supportsPersistentMapping |= extension == "GL_ARB_buffer_storage";
					if (required.Contains(extension))
					{
						required.Remove(extension);
					}
				}
				if (required.Count > 0)
				{
					throw new NotSupportedException("Your graphics card does not support the extensions " + string.Join(", ", required) + " which is required to start the game");
				}
			}
			this.CheckGlError("testhwinfo");
		}

		public override string GetGraphicCardInfos()
		{
			return string.Concat(new string[]
			{
				"GC Vendor: ",
				GL.GetString(StringName.Vendor),
				"\nGC Version: ",
				GL.GetString(StringName.Version),
				"\nGC Renderer: ",
				GL.GetString(StringName.Renderer),
				"\nGC ShaderVersion: ",
				GL.GetString(StringName.ShadingLanguageVersion)
			});
		}

		public override string GetFrameworkInfos()
		{
			string text = ".net ";
			Version version = Environment.Version;
			return text + ((version != null) ? version.ToString() : null);
		}

		public override bool IsExitAvailable()
		{
			return true;
		}

		public override void SetWindowSize(int width, int height)
		{
			this.window.ClientSize = new Vector2i(width, height);
			this.Window_Resize();
		}

		public override long EllapsedMs
		{
			get
			{
				return this.uptimeStopWatch.ElapsedMilliseconds;
			}
		}

		public override BitmapRef CreateBitmap(int width, int height)
		{
			return new BitmapExternal(width, height);
		}

		public override void SetBitmapPixelsArgb(BitmapRef bmp, int[] pixels)
		{
			BitmapExternal bitmapExternal = (BitmapExternal)bmp;
			int width = bitmapExternal.bmp.Width;
			int height = bitmapExternal.bmp.Height;
			FastBitmap fastBitmap = new FastBitmap();
			fastBitmap.bmp = bitmapExternal.bmp;
			for (int i = 0; i < width; i++)
			{
				for (int j = 0; j < height; j++)
				{
					fastBitmap.SetPixel(i, j, pixels[i + j * width]);
				}
			}
		}

		public override BitmapRef CreateBitmapFromPng(IAsset asset)
		{
			BitmapRef bitmapRef;
			using (MemoryStream stream = new MemoryStream(asset.Data))
			{
				bitmapRef = new BitmapExternal(stream, this.Logger, asset.Location);
			}
			return bitmapRef;
		}

		public override BitmapRef CreateBitmapFromPng(byte[] data)
		{
			return this.CreateBitmapFromPng(data, data.Length);
		}

		public override BitmapRef CreateBitmapFromPng(byte[] data, int dataLength)
		{
			return new BitmapExternal(data, dataLength, this.Logger);
		}

		public override BitmapRef CreateBitmapFromPixels(int[] pixels, int width, int height)
		{
			IntPtr scan = GCHandle.Alloc(pixels, GCHandleType.Pinned).AddrOfPinnedObject();
			return new BitmapExternal(SKBitmap.FromImage(SKImage.FromPixels(new SKImageInfo(width, height), scan)));
		}

		public override IAviWriter CreateAviWriter(float framerate, string codec)
		{
			return base.XPlatInterface.GetAviWriter(ClientSettings.RecordingBufferSize, (double)framerate, codec);
		}

		public override AvailableCodec[] GetAvailableCodecs()
		{
			return base.XPlatInterface.AvailableCodecs();
		}

		public override string GetGameVersion()
		{
			return "1.21.5";
		}

		public void SetServerExitInterface(GameExit exit)
		{
			this.gameexit = exit;
		}

		public override void ThreadSpinWait(int iterations)
		{
			Thread.SpinWait(iterations);
		}

		public override void LoadAssets()
		{
			if (this.assetManager == null)
			{
				this.assetManager = new AssetManager(GamePaths.AssetsPath, EnumAppSide.Client);
			}
			this.logger.Notification("Start discovering assets");
			int count = this.assetManager.InitAndLoadBaseAssets(this.logger, "textures");
			this.logger.Notification("Found {0} base assets in total", new object[] { count });
		}

		public override Size2i WindowSize
		{
			get
			{
				return this.windowsize;
			}
		}

		public override Size2i ScreenSize
		{
			get
			{
				return this.screensize;
			}
		}

		public override EnumWindowBorder WindowBorder
		{
			get
			{
				return (EnumWindowBorder)this.window.WindowBorder;
			}
			set
			{
				this.window.WindowBorder = (WindowBorder)value;
			}
		}

		public void Start()
		{
			this.window.FocusedChanged += this.window_FocusChanged;
			this.window.KeyDown += this.game_KeyDown;
			this.window.KeyUp += this.game_KeyUp;
			this.window.TextInput += this.game_KeyPress;
			this.window.MouseDown += this.Mouse_ButtonDown;
			this.window.MouseUp += this.Mouse_ButtonUp;
			this.window.MouseMove += this.Mouse_Move;
			this.window.MouseWheel += this.Mouse_WheelChanged;
			this.window.RenderFrame += this.window_RenderFrame;
			this.window.Closing += this.window_Closing;
			this.window.Resize += this.OnWindowResize;
			this.window.Title = "Vintage Story";
			this.window.FileDrop += this.Window_FileDrop;
			this.frameBuffers = this.SetupDefaultFrameBuffers();
			this.minimalGuiShaderProgram = new ShaderProgramMinimalGui();
			this.minimalGuiShaderProgram.Compile();
			this.windowsize.Width = this.window.ClientSize.X;
			this.windowsize.Height = this.window.ClientSize.Y;
			GL.LineWidth(1.5f);
			OpenTK.Graphics.OpenGL.ErrorCode code = GL.GetError();
			this.SupportsThickLines = code != OpenTK.Graphics.OpenGL.ErrorCode.InvalidValue;
			this.cpuCoreCount = Environment.ProcessorCount;
		}

		public override int CpuCoreCount
		{
			get
			{
				return this.cpuCoreCount;
			}
		}

		public override void RebuildFrameBuffers()
		{
			List<FrameBufferRef> oldFrameBuffers = this.frameBuffers;
			List<FrameBufferRef> newFrameBuffers = this.SetupDefaultFrameBuffers();
			this.frameBuffers = newFrameBuffers;
			this.DisposeFrameBuffers(oldFrameBuffers);
		}

		private void Window_FileDrop(FileDropEventArgs e)
		{
			Action<string> action = this.fileDropEventHandler;
			if (action == null)
			{
				return;
			}
			action(e.FileNames[0]);
		}

		private void OnWindowResize(ResizeEventArgs e)
		{
			this.doResize = Environment.TickCount + 40;
		}

		private void Window_Resize()
		{
			this.doResize = 0;
			if (this.window.WindowState != WindowState.Minimized)
			{
				Vector2i windowSize = this.window.ClientSize;
				if (this.window.ClientSize.X < 600)
				{
					windowSize.X = 600;
				}
				if (this.window.ClientSize.Y < 400)
				{
					windowSize.Y = 400;
				}
				if (this.window.ClientSize.Y < 400 || this.window.ClientSize.X < 600)
				{
					this.window.ClientSize = windowSize;
				}
			}
			if (this.window.ClientSize.X == 0 || this.window.ClientSize.Y == 0)
			{
				this.logger.Notification("Window was resized to {0} {1}? Window probably got minimized. Will not rebuild frame buffers", new object[]
				{
					this.window.ClientSize.X,
					this.window.ClientSize.Y
				});
				return;
			}
			if (this.window.ClientSize.X == this.windowsize.Width && this.window.ClientSize.Y == this.windowsize.Height)
			{
				return;
			}
			this.logger.Notification("Window was resized to {0} {1}, rebuilding framebuffers...", new object[]
			{
				this.window.ClientSize.X,
				this.window.ClientSize.Y
			});
			this.RebuildFrameBuffers();
			this.windowsize.Width = this.window.ClientSize.X;
			this.windowsize.Height = this.window.ClientSize.Y;
			if (this.window.WindowState == WindowState.Normal)
			{
				ClientSettings.ScreenWidth = this.window.Size.X;
				ClientSettings.ScreenHeight = this.window.Size.Y;
			}
			WindowState windowState2 = this.window.WindowState;
			int num;
			if (windowState2 != WindowState.Maximized)
			{
				if (windowState2 != WindowState.Fullscreen)
				{
					num = 0;
				}
				else
				{
					num = ((ClientSettings.GameWindowMode == 3) ? 3 : 1);
				}
			}
			else
			{
				num = 2;
			}
			int windowState = num;
			if (ClientSettings.GameWindowMode != windowState)
			{
				ClientSettings.GameWindowMode = windowState;
			}
			base.TriggerWindowResized(this.window.ClientSize.X, this.window.ClientSize.Y);
		}

		private void window_Closing(CancelEventArgs e)
		{
			this.gameexit.exit = true;
			try
			{
				this.windowClosedHandler();
			}
			catch (Exception)
			{
			}
		}

		public override void SetVSync(bool enabled)
		{
			this.window.VSync = (enabled ? VSyncMode.On : VSyncMode.Off);
		}

		public override void SetDirectMouseMode(bool enabled)
		{
			GLFW.SetInputMode(this.window.WindowPtr, RawMouseMotionAttribute.RawMouseMotion, enabled);
		}

		public override string SaveScreenshot(string path = null, string filename = null, bool withAlpha = false, bool flip = false, string metaDataStr = null)
		{
			this.screenshot.d_GameWindow = this.window;
			FrameBufferRef currentFrameBuffer = this.CurrentFrameBuffer;
			Size2i size = ((currentFrameBuffer == null) ? new Size2i(this.window.ClientSize.X, this.window.ClientSize.Y) : new Size2i(currentFrameBuffer.Width, currentFrameBuffer.Height));
			return this.screenshot.SaveScreenshot(this, size, path, filename, withAlpha, flip, metaDataStr);
		}

		public override BitmapRef GrabScreenshot(bool withAlpha = false, bool scale = false)
		{
			this.screenshot.d_GameWindow = this.window;
			FrameBufferRef currentFrameBuffer = this.CurrentFrameBuffer;
			Size2i size = ((currentFrameBuffer == null) ? new Size2i(this.window.ClientSize.X, this.window.ClientSize.Y) : new Size2i(currentFrameBuffer.Width, currentFrameBuffer.Height));
			return new BitmapExternal(this.screenshot.GrabScreenshot(size, scale, false, withAlpha));
		}

		public override BitmapRef GrabScreenshot(int width, int height, bool scaleScreenshot, bool flip, bool withAlpha = false)
		{
			this.screenshot.d_GameWindow = this.window;
			return new BitmapExternal(this.screenshot.GrabScreenshot(new Size2i(width, height), scaleScreenshot, flip, withAlpha));
		}

		public override void WindowExit(string reason)
		{
			this.logger.Notification("Exiting game now. Server running=" + this.serverRunning.ToString() + ". Exit reason: {0}", new object[] { reason });
			base.IsShuttingDown = true;
			if (this.gameexit != null)
			{
				this.gameexit.exit = true;
			}
			try
			{
				UriHandler.Instance.Dispose();
				GameWindowNative gameWindowNative = this.window;
				if (gameWindowNative != null)
				{
					gameWindowNative.Close();
				}
			}
			catch (Exception)
			{
				Environment.Exit(0);
			}
		}

		public override void SetTitle(string applicationname)
		{
			this.window.Title = applicationname;
		}

		public override WindowState GetWindowState()
		{
			return this.window.WindowState;
		}

		public override void SetWindowState(WindowState value)
		{
			this.window.WindowState = value;
			if (this.window.Location.Y < 0)
			{
				this.window.Location = new Vector2i(this.window.Location.X, 0);
			}
		}

		public override void SetWindowAttribute(WindowAttribute attribute, bool value)
		{
			GLFW.SetWindowAttrib(this.window.WindowPtr, attribute, value);
		}

		public override void StartSinglePlayerServer(StartServerArgs serverargs)
		{
			this.ServerExit = new GameExit();
			this.OnStartSinglePlayerServer(serverargs);
		}

		public override void ExitSinglePlayerServer()
		{
			this.ServerExit.SetExit(true);
		}

		public override bool IsLoadedSinglePlayerServer()
		{
			return this.singlePlayerServerLoaded;
		}

		public override DummyNetwork[] GetSinglePlayerServerNetwork()
		{
			return this.singlePlayerServerDummyNetwork;
		}

		public override bool IsFocused
		{
			get
			{
				return this.window.IsFocused;
			}
		}

		public override void SetFileDropHandler(Action<string> handler)
		{
			this.fileDropEventHandler = handler;
		}

		public override void RegisterOnFocusChange(ClientPlatformAbstract.OnFocusChanged handler)
		{
			this.focusChangedDelegates.Add(handler);
		}

		private void window_FocusChanged(FocusedChangedEventArgs e)
		{
			foreach (ClientPlatformAbstract.OnFocusChanged onFocusChanged in this.focusChangedDelegates)
			{
				onFocusChanged(this.window.IsFocused);
			}
		}

		public override void SetWindowClosedHandler(Action handler)
		{
			this.windowClosedHandler = handler;
		}

		public override void SetFrameHandler(NewFrameHandler handler)
		{
			this.frameHandler = handler;
		}

		public override void RegisterKeyboardEvent(KeyEventHandler handler)
		{
			this.keyEventHandlers.Add(handler);
		}

		public override void RegisterMouseEvent(MouseEventHandler handler)
		{
			this.mouseEventHandlers.Add(handler);
		}

		public override void AddOnCrash(OnCrashHandler handler)
		{
			this.crashreporter.OnCrash = new Action(this.OnCrash);
			this.onCrashHandler = handler;
		}

		public override void ClearOnCrash()
		{
			this.onCrashHandler = null;
			this.crashreporter.OnCrash = null;
		}

		private void OnCrash()
		{
			if (this.onCrashHandler != null)
			{
				this.onCrashHandler.OnCrash();
			}
		}

		public override bool DebugDrawCalls
		{
			get
			{
				return this.debugDrawCalls;
			}
			set
			{
				this.debugDrawCalls = value;
				if (!value)
				{
					this.logger.Notification("Call stacks:");
					int i = 0;
					foreach (string val in this.drawCallStacks)
					{
						this.logger.Notification("{0}: {1}", new object[]
						{
							i++,
							val.Substring(0, 600)
						});
					}
				}
				this.drawCallStacks.Clear();
			}
		}

		public override void RenderMesh(MeshRef modelRef)
		{
			RuntimeStats.drawCallsCount++;
			if (this.debugDrawCalls)
			{
				this.drawCallStacks.Add(Environment.StackTrace);
			}
			VAO vao = (VAO)modelRef;
			if (vao.VaoId != 0 && !vao.Disposed)
			{
				GL.BindVertexArray(vao.VaoId);
				for (int i = 0; i < vao.vaoSlotNumber; i++)
				{
					GL.EnableVertexAttribArray(i);
				}
				GL.BindBuffer(BufferTarget.ElementArrayBuffer, vao.vboIdIndex);
				GL.DrawElements(vao.drawMode, vao.IndicesCount, DrawElementsType.UnsignedInt, 0);
				GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
				for (int j = 0; j < vao.vaoSlotNumber; j++)
				{
					GL.DisableVertexAttribArray(j);
				}
				GL.BindVertexArray(0);
				return;
			}
			if (vao.VaoId == 0)
			{
				throw new ArgumentException("Fatal: Trying to render an uninitialized mesh");
			}
			throw new ArgumentException("Fatal: Trying to render a disposed mesh");
		}

		public void RenderFullscreenTriangle(MeshRef modelRef)
		{
			RuntimeStats.drawCallsCount++;
			GL.BindVertexArray(((VAO)modelRef).VaoId);
			GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
			GL.BindVertexArray(0);
		}

		public override void RenderMesh(MeshRef modelRef, int[] indices, int[] indicesSizes, int groupCount)
		{
			this.RenderMesh(modelRef, indices, indicesSizes, groupCount, false);
		}

		public override void RenderMesh(MeshRef modelRef, int[] indices, int[] indicesSizes, int groupCount, bool useSSBOs)
		{
			RuntimeStats.drawCallsCount++;
			VAO vao = (VAO)modelRef;
			GL.BindVertexArray(vao.VaoId);
			for (int i = 0; i < vao.vaoSlotNumber; i++)
			{
				GL.EnableVertexAttribArray(i);
			}
			if (useSSBOs)
			{
				GL.BindBuffer(BufferTarget.ElementArrayBuffer, ClientPlatformAbstract.singleIndexBufferId);
				GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 3, vao.xyzVboId);
				GL.MultiDrawElements<int>(vao.drawMode, indicesSizes, DrawElementsType.UnsignedInt, indices, groupCount);
				GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 3, 0);
			}
			else
			{
				GL.BindBuffer(BufferTarget.ElementArrayBuffer, vao.vboIdIndex);
				GL.MultiDrawElements<int>(vao.drawMode, indicesSizes, DrawElementsType.UnsignedInt, indices, groupCount);
			}
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
			for (int j = 0; j < vao.vaoSlotNumber; j++)
			{
				GL.DisableVertexAttribArray(j);
			}
			GL.BindVertexArray(0);
		}

		public override void RenderMeshInstanced(MeshRef modelRef, int quantity = 1)
		{
			RuntimeStats.drawCallsCount++;
			VAO vao = (VAO)modelRef;
			GL.BindVertexArray(vao.VaoId);
			for (int i = 0; i < vao.vaoSlotNumber; i++)
			{
				GL.EnableVertexAttribArray(i);
			}
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, vao.vboIdIndex);
			GL.DrawElementsInstanced(vao.drawMode, vao.IndicesCount, DrawElementsType.UnsignedInt, IntPtr.Zero, quantity);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
			for (int j = 0; j < vao.vaoSlotNumber; j++)
			{
				GL.DisableVertexAttribArray(j);
			}
			GL.BindVertexArray(0);
		}

		public override List<FrameBufferRef> FrameBuffers
		{
			get
			{
				return this.frameBuffers;
			}
		}

		public override bool IsServerRunning
		{
			get
			{
				return this.serverRunning;
			}
			set
			{
				this.serverRunning = value;
			}
		}

		public override void SetGamePausedState(bool paused)
		{
			this.gamepause = paused;
		}

		public override void ResetGamePauseAndUptimeState()
		{
			this.uptimeStopWatch.Start();
		}

		public override bool IsGamePaused
		{
			get
			{
				return this.gamepause;
			}
		}

		public override void ToggleOffscreenBuffer(bool enable)
		{
			this.OffscreenBuffer = enable;
		}

		public override void DisposeFrameBuffer(FrameBufferRef frameBuffer, bool disposeTextures = true)
		{
			if (frameBuffer == null)
			{
				return;
			}
			if (disposeTextures)
			{
				for (int i = 0; i < frameBuffer.ColorTextureIds.Length; i++)
				{
					this.GLDeleteTexture(frameBuffer.ColorTextureIds[i]);
				}
				if (frameBuffer.DepthTextureId > 0)
				{
					this.GLDeleteTexture(frameBuffer.DepthTextureId);
				}
			}
			GL.DeleteFramebuffer(frameBuffer.FboId);
		}

		public override FrameBufferRef CreateFramebuffer(FramebufferAttrs fbAttrs)
		{
			FrameBufferRef framebuffer = new FrameBufferRef
			{
				FboId = GL.GenFramebuffer(),
				Width = fbAttrs.Width,
				Height = fbAttrs.Height
			};
			this.CurrentFrameBuffer = framebuffer;
			List<DrawBuffersEnum> drawBuffers = new List<DrawBuffersEnum>();
			List<int> colorTextureIds = new List<int>();
			foreach (FramebufferAttrsAttachment fbAtt in fbAttrs.Attachments)
			{
				RawTexture tex = fbAtt.Texture;
				int textureId = ((tex.TextureId == 0) ? GL.GenTexture() : tex.TextureId);
				if (tex.TextureId == 0)
				{
					GL.BindTexture(TextureTarget.Texture2D, textureId);
					GL.TexImage2D(TextureTarget.Texture2D, 0, (PixelInternalFormat)tex.PixelInternalFormat, tex.Width, tex.Height, 0, (PixelFormat)tex.PixelFormat, PixelType.Float, IntPtr.Zero);
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)tex.MinFilter);
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)tex.MagFilter);
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)tex.WrapS);
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)tex.WrapT);
				}
				GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, (FramebufferAttachment)fbAtt.AttachmentType, TextureTarget.Texture2D, textureId, 0);
				if (fbAtt.AttachmentType == EnumFramebufferAttachment.DepthAttachment)
				{
					GL.DepthFunc(DepthFunction.Less);
					framebuffer.DepthTextureId = textureId;
				}
				else
				{
					drawBuffers.Add((DrawBuffersEnum)fbAtt.AttachmentType);
					colorTextureIds.Add(tex.TextureId = textureId);
				}
			}
			framebuffer.ColorTextureIds = colorTextureIds.ToArray();
			GL.DrawBuffers(drawBuffers.Count, drawBuffers.ToArray());
			this.CheckFboStatus(FramebufferTarget.Framebuffer, fbAttrs.Name);
			this.CurrentFrameBuffer = null;
			GL.BindTexture(TextureTarget.Texture2D, 0);
			return framebuffer;
		}

		public FrameBufferRef CurrentFrameBuffer
		{
			get
			{
				return this.curFb;
			}
			set
			{
				this.curFb = value;
				if (value == null)
				{
					GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
					return;
				}
				GL.BindFramebuffer(FramebufferTarget.Framebuffer, value.FboId);
			}
		}

		public List<FrameBufferRef> SetupDefaultFrameBuffers()
		{
			this.SetupSSAO = ClientSettings.SSAOQuality > 0;
			if (ClientSettings.IsNewSettingsFile && this.window.ClientSize.X > 1920)
			{
				ClientSettings.SSAA = 0.5f;
			}
			List<FrameBufferRef> framebuffers = new List<FrameBufferRef>(31);
			for (int i = 0; i <= 24; i++)
			{
				framebuffers.Add(null);
			}
			this.ShadowMapQuality = ClientSettings.ShadowMapQuality;
			this.ssaaLevel = ClientSettings.SSAA;
			int fullWidth = (int)((float)this.window.ClientSize.X * this.ssaaLevel);
			int fullHeight = (int)((float)this.window.ClientSize.Y * this.ssaaLevel);
			if (fullWidth == 0 || fullHeight == 0)
			{
				return framebuffers;
			}
			PixelFormat rgbaFormat = PixelFormat.Rgba;
			this.CheckGlError("sdfb-begin");
			List<FrameBufferRef> list = framebuffers;
			int num = 0;
			FrameBufferRef frameBufferRef = new FrameBufferRef();
			frameBufferRef.FboId = GL.GenFramebuffer();
			frameBufferRef.Width = fullWidth;
			frameBufferRef.Height = fullHeight;
			FrameBufferRef frameBufferRef2 = frameBufferRef;
			list[num] = frameBufferRef;
			FrameBufferRef frameBuffer = frameBufferRef2;
			frameBuffer.DepthTextureId = GL.GenTexture();
			if (frameBuffer.FboId == 0)
			{
				base.XPlatInterface.ShowMessageBox("Fatal error", "Unable to generate a new framebuffer. This shouldn't happen, ever. Maybe a restart resolves the problem?");
			}
			this.CurrentFrameBuffer = frameBuffer;
			GL.BindTexture(TextureTarget.Texture2D, frameBuffer.DepthTextureId);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent32, fullWidth, fullHeight, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9728);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, 9728);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, 33071);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, 33071);
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, frameBuffer.DepthTextureId, 0);
			GL.DepthFunc(DepthFunction.Less);
			frameBuffer.ColorTextureIds = ArrayUtil.CreateFilled<int>(this.SetupSSAO ? 4 : 2, (int n) => GL.GenTexture());
			GL.BindTexture(TextureTarget.Texture2D, frameBuffer.ColorTextureIds[0]);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, fullWidth, fullHeight, 0, rgbaFormat, PixelType.UnsignedShort, IntPtr.Zero);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (this.ssaaLevel <= 1f) ? 9728 : 9729);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (this.ssaaLevel <= 1f) ? 9728 : 9729);
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, frameBuffer.ColorTextureIds[0], 0);
			GL.BindTexture(TextureTarget.Texture2D, frameBuffer.ColorTextureIds[1]);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, fullWidth, fullHeight, 0, rgbaFormat, PixelType.UnsignedByte, IntPtr.Zero);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (this.ssaaLevel <= 1f) ? 9728 : 9729);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (this.ssaaLevel <= 1f) ? 9728 : 9729);
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, TextureTarget.Texture2D, frameBuffer.ColorTextureIds[1], 0);
			if (this.SetupSSAO)
			{
				GL.BindTexture(TextureTarget.Texture2D, frameBuffer.ColorTextureIds[2]);
				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, fullWidth, fullHeight, 0, rgbaFormat, PixelType.Float, IntPtr.Zero);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9729);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, 9729);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, new float[] { 1f, 1f, 1f, 1f });
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, 33069);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, 33069);
				GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment2, TextureTarget.Texture2D, frameBuffer.ColorTextureIds[2], 0);
				GL.BindTexture(TextureTarget.Texture2D, frameBuffer.ColorTextureIds[3]);
				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, fullWidth, fullHeight, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9729);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, 9729);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, new float[] { 1f, 1f, 1f, 1f });
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, 33069);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, 33069);
				GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment3, TextureTarget.Texture2D, frameBuffer.ColorTextureIds[3], 0);
				DrawBuffersEnum[] bufs = new DrawBuffersEnum[]
				{
					DrawBuffersEnum.ColorAttachment0,
					DrawBuffersEnum.ColorAttachment1,
					DrawBuffersEnum.ColorAttachment2,
					DrawBuffersEnum.ColorAttachment3
				};
				GL.DrawBuffers(4, bufs);
			}
			else
			{
				DrawBuffersEnum[] bufs2 = new DrawBuffersEnum[]
				{
					DrawBuffersEnum.ColorAttachment0,
					DrawBuffersEnum.ColorAttachment1
				};
				GL.DrawBuffers(2, bufs2);
			}
			this.CheckFboStatus(FramebufferTarget.Framebuffer, EnumFrameBuffer.Primary);
			List<FrameBufferRef> list2 = framebuffers;
			int num2 = 1;
			FrameBufferRef frameBufferRef3 = new FrameBufferRef();
			frameBufferRef3.FboId = GL.GenFramebuffer();
			frameBufferRef3.Width = fullWidth;
			frameBufferRef3.Height = fullHeight;
			frameBufferRef2 = frameBufferRef3;
			list2[num2] = frameBufferRef3;
			frameBuffer = frameBufferRef2;
			frameBuffer.ColorTextureIds = new int[]
			{
				GL.GenTexture(),
				GL.GenTexture(),
				GL.GenTexture()
			};
			this.CurrentFrameBuffer = frameBuffer;
			GL.BindTexture(TextureTarget.Texture2D, frameBuffer.ColorTextureIds[0]);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, fullWidth, fullHeight, 0, rgbaFormat, PixelType.UnsignedShort, IntPtr.Zero);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9729);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, 9729);
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, frameBuffer.ColorTextureIds[0], 0);
			GL.BindTexture(TextureTarget.Texture2D, frameBuffer.ColorTextureIds[1]);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R16f, fullWidth, fullHeight, 0, PixelFormat.Red, PixelType.UnsignedShort, IntPtr.Zero);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9729);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, 9729);
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, TextureTarget.Texture2D, frameBuffer.ColorTextureIds[1], 0);
			GL.BindTexture(TextureTarget.Texture2D, frameBuffer.ColorTextureIds[2]);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, fullWidth, fullHeight, 0, rgbaFormat, PixelType.UnsignedByte, IntPtr.Zero);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9729);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, 9729);
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment2, TextureTarget.Texture2D, frameBuffer.ColorTextureIds[2], 0);
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, framebuffers[0].DepthTextureId, 0);
			DrawBuffersEnum[] bufs3 = new DrawBuffersEnum[]
			{
				DrawBuffersEnum.ColorAttachment0,
				DrawBuffersEnum.ColorAttachment1,
				DrawBuffersEnum.ColorAttachment2
			};
			GL.DrawBuffers(3, bufs3);
			this.ClearFrameBuffer(EnumFrameBuffer.Transparent);
			this.CheckFboStatus(FramebufferTarget.Framebuffer, EnumFrameBuffer.Transparent);
			if (this.SetupSSAO)
			{
				int ssaoquality = ClientSettings.SSAOQuality;
				float ssaoSizeFq = 0.5f;
				List<FrameBufferRef> list3 = framebuffers;
				int num3 = 13;
				FrameBufferRef frameBufferRef4 = new FrameBufferRef();
				frameBufferRef4.FboId = GL.GenFramebuffer();
				frameBufferRef4.Width = (int)((float)fullWidth * ssaoSizeFq);
				frameBufferRef4.Height = (int)((float)fullHeight * ssaoSizeFq);
				frameBufferRef2 = frameBufferRef4;
				list3[num3] = frameBufferRef4;
				frameBuffer = frameBufferRef2;
				this.CurrentFrameBuffer = frameBuffer;
				frameBuffer.ColorTextureIds = new int[]
				{
					GL.GenTexture(),
					GL.GenTexture()
				};
				GL.BindTexture(TextureTarget.Texture2D, frameBuffer.ColorTextureIds[0]);
				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, frameBuffer.Width, frameBuffer.Height, 0, PixelFormat.Rgb, PixelType.Float, IntPtr.Zero);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9728);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, 9728);
				GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, frameBuffer.ColorTextureIds[0], 0);
				Random rand = new Random(5);
				int num4 = frameBuffer.ColorTextureIds[1];
				int size = 16;
				float[] vecs = new float[size * size * 3];
				Vec3f tmpvec = new Vec3f();
				for (int j = 0; j < size * size; j++)
				{
					tmpvec.Set((float)rand.NextDouble() * 2f - 1f, (float)rand.NextDouble() * 2f - 1f, 0f).Normalize();
					vecs[j * 3] = tmpvec.X;
					vecs[j * 3 + 1] = tmpvec.Y;
					vecs[j * 3 + 2] = tmpvec.Z;
				}
				GL.BindTexture(TextureTarget.Texture2D, frameBuffer.ColorTextureIds[1]);
				GL.TexImage2D<float>(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32f, size, size, 0, PixelFormat.Rgb, PixelType.Float, vecs);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9728);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, 9728);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, 10497);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, 10497);
				for (int k = 0; k < 64; k++)
				{
					Vec3f vec = new Vec3f((float)rand.NextDouble() * 2f - 1f, (float)rand.NextDouble() * 2f - 1f, (float)rand.NextDouble());
					vec.Normalize();
					vec *= (float)rand.NextDouble();
					float scale = (float)k / 64f;
					scale = GameMath.Lerp(0.1f, 1f, scale * scale);
					vec *= scale;
					this.ssaoKernel[k * 3] = vec.X;
					this.ssaoKernel[k * 3 + 1] = vec.Y;
					this.ssaoKernel[k * 3 + 2] = vec.Z;
				}
				GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
				this.CheckFboStatus(FramebufferTarget.Framebuffer, EnumFrameBuffer.SSAO);
				foreach (EnumFrameBuffer val in new EnumFrameBuffer[]
				{
					EnumFrameBuffer.SSAOBlurVertical,
					EnumFrameBuffer.SSAOBlurHorizontal
				})
				{
					List<FrameBufferRef> list4 = framebuffers;
					int num5 = (int)val;
					FrameBufferRef frameBufferRef5 = new FrameBufferRef();
					frameBufferRef5.FboId = GL.GenFramebuffer();
					frameBufferRef5.Width = (int)((float)fullWidth * ssaoSizeFq);
					frameBufferRef5.Height = (int)((float)fullHeight * ssaoSizeFq);
					frameBufferRef2 = frameBufferRef5;
					list4[num5] = frameBufferRef5;
					frameBuffer = frameBufferRef2;
					this.CurrentFrameBuffer = frameBuffer;
					frameBuffer.ColorTextureIds = new int[] { GL.GenTexture() };
					this.setupAttachment(frameBuffer, frameBuffer.Width, frameBuffer.Height, 0, rgbaFormat, PixelInternalFormat.Rgba8);
					GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
					this.CheckFboStatus(FramebufferTarget.Framebuffer, val);
				}
			}
			List<FrameBufferRef> list5 = framebuffers;
			int num6 = 2;
			FrameBufferRef frameBufferRef6 = new FrameBufferRef();
			frameBufferRef6.FboId = GL.GenFramebuffer();
			frameBufferRef6.Width = fullWidth / 2;
			frameBufferRef6.Height = fullHeight / 2;
			frameBufferRef2 = frameBufferRef6;
			list5[num6] = frameBufferRef6;
			frameBuffer = frameBufferRef2;
			this.CurrentFrameBuffer = frameBuffer;
			frameBuffer.ColorTextureIds = new int[] { GL.GenTexture() };
			this.setupAttachment(frameBuffer, fullWidth / 2, fullHeight / 2, 0, rgbaFormat, PixelInternalFormat.Rgba8);
			GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
			this.CheckFboStatus(FramebufferTarget.Framebuffer, EnumFrameBuffer.BlurHorizontalMedRes);
			List<FrameBufferRef> list6 = framebuffers;
			int num7 = 3;
			FrameBufferRef frameBufferRef7 = new FrameBufferRef();
			frameBufferRef7.FboId = GL.GenFramebuffer();
			frameBufferRef7.Width = fullWidth / 2;
			frameBufferRef7.Height = fullHeight / 2;
			frameBufferRef2 = frameBufferRef7;
			list6[num7] = frameBufferRef7;
			frameBuffer = frameBufferRef2;
			this.CurrentFrameBuffer = frameBuffer;
			frameBuffer.ColorTextureIds = new int[] { GL.GenTexture() };
			this.setupAttachment(frameBuffer, fullWidth / 2, fullHeight / 2, 0, rgbaFormat, PixelInternalFormat.Rgba8);
			GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
			this.CheckFboStatus(FramebufferTarget.Framebuffer, EnumFrameBuffer.BlurVerticalMedRes);
			List<FrameBufferRef> list7 = framebuffers;
			int num8 = 9;
			FrameBufferRef frameBufferRef8 = new FrameBufferRef();
			frameBufferRef8.FboId = GL.GenFramebuffer();
			frameBufferRef8.Width = fullWidth / 4;
			frameBufferRef8.Height = fullHeight / 4;
			frameBufferRef2 = frameBufferRef8;
			list7[num8] = frameBufferRef8;
			frameBuffer = frameBufferRef2;
			this.CurrentFrameBuffer = frameBuffer;
			frameBuffer.ColorTextureIds = new int[] { GL.GenTexture() };
			this.setupAttachment(frameBuffer, fullWidth / 4, fullHeight / 4, 0, rgbaFormat, PixelInternalFormat.Rgba8);
			GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
			this.CheckFboStatus(FramebufferTarget.Framebuffer, EnumFrameBuffer.BlurHorizontalLowRes);
			List<FrameBufferRef> list8 = framebuffers;
			int num9 = 8;
			FrameBufferRef frameBufferRef9 = new FrameBufferRef();
			frameBufferRef9.FboId = GL.GenFramebuffer();
			frameBufferRef2 = frameBufferRef9;
			list8[num9] = frameBufferRef9;
			frameBuffer = frameBufferRef2;
			this.CurrentFrameBuffer = frameBuffer;
			frameBuffer.ColorTextureIds = new int[] { GL.GenTexture() };
			this.setupAttachment(frameBuffer, fullWidth / 4, fullHeight / 4, 0, rgbaFormat, PixelInternalFormat.Rgba8);
			GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
			this.CheckFboStatus(FramebufferTarget.Framebuffer, EnumFrameBuffer.BlurVerticalLowRes);
			List<FrameBufferRef> list9 = framebuffers;
			int num10 = 4;
			FrameBufferRef frameBufferRef10 = new FrameBufferRef();
			frameBufferRef10.FboId = GL.GenFramebuffer();
			frameBufferRef10.Width = fullWidth;
			frameBufferRef10.Height = fullHeight;
			frameBufferRef2 = frameBufferRef10;
			list9[num10] = frameBufferRef10;
			frameBuffer = frameBufferRef2;
			this.CurrentFrameBuffer = frameBuffer;
			frameBuffer.ColorTextureIds = new int[] { GL.GenTexture() };
			this.setupAttachment(frameBuffer, fullWidth, fullHeight, 0, rgbaFormat, PixelInternalFormat.Rgba16f);
			GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
			this.CheckFboStatus(FramebufferTarget.Framebuffer, EnumFrameBuffer.FindBright);
			List<FrameBufferRef> list10 = framebuffers;
			int num11 = 7;
			FrameBufferRef frameBufferRef11 = new FrameBufferRef();
			frameBufferRef11.FboId = GL.GenFramebuffer();
			frameBufferRef11.Width = fullWidth / 2;
			frameBufferRef11.Height = fullHeight / 2;
			frameBufferRef2 = frameBufferRef11;
			list10[num11] = frameBufferRef11;
			frameBuffer = frameBufferRef2;
			this.CurrentFrameBuffer = frameBuffer;
			frameBuffer.ColorTextureIds = new int[] { GL.GenTexture() };
			this.setupAttachment(frameBuffer, fullWidth / 2, fullHeight / 2, 0, rgbaFormat, PixelInternalFormat.Rgba16f);
			GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
			this.CheckFboStatus(FramebufferTarget.Framebuffer, EnumFrameBuffer.GodRays);
			List<FrameBufferRef> list11 = framebuffers;
			int num12 = 10;
			FrameBufferRef frameBufferRef12 = new FrameBufferRef();
			frameBufferRef12.FboId = GL.GenFramebuffer();
			frameBufferRef12.Width = fullWidth;
			frameBufferRef12.Height = fullHeight;
			frameBufferRef2 = frameBufferRef12;
			list11[num12] = frameBufferRef12;
			frameBuffer = frameBufferRef2;
			this.CurrentFrameBuffer = frameBuffer;
			frameBuffer.ColorTextureIds = new int[] { GL.GenTexture() };
			this.setupAttachment(frameBuffer, fullWidth, fullHeight, 0, rgbaFormat, PixelInternalFormat.Rgba16f);
			GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
			this.CheckFboStatus(FramebufferTarget.Framebuffer, EnumFrameBuffer.Luma);
			List<FrameBufferRef> list12 = framebuffers;
			int num13 = 5;
			FrameBufferRef frameBufferRef13 = new FrameBufferRef();
			frameBufferRef13.FboId = GL.GenFramebuffer();
			frameBufferRef13.Width = fullWidth / 4;
			frameBufferRef13.Height = fullHeight / 4;
			frameBufferRef2 = frameBufferRef13;
			list12[num13] = frameBufferRef13;
			frameBuffer = frameBufferRef2;
			frameBuffer.ColorTextureIds = Array.Empty<int>();
			this.CheckGlError("sdfb-lide");
			this.CurrentFrameBuffer = frameBuffer;
			frameBuffer.DepthTextureId = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, frameBuffer.DepthTextureId);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent32, frameBuffer.Width, frameBuffer.Height, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9729);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, 9729);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareMode, 0);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, 33071);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, 33071);
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, frameBuffer.DepthTextureId, 0);
			GL.DepthFunc(DepthFunction.Less);
			GL.DrawBuffer(DrawBufferMode.None);
			GL.ReadBuffer(ReadBufferMode.None);
			this.CheckFboStatus(FramebufferTarget.Framebuffer, EnumFrameBuffer.LiquidDepth);
			int shadowMapWidth = Math.Max(4, this.ShadowMapQuality + 2) * 1024;
			int shadowMapHeight = Math.Max(4, this.ShadowMapQuality + 2) * 1024;
			List<FrameBufferRef> list13 = framebuffers;
			int num14 = 11;
			FrameBufferRef frameBufferRef14 = new FrameBufferRef();
			frameBufferRef14.FboId = GL.GenFramebuffer();
			frameBufferRef14.Width = shadowMapWidth;
			frameBufferRef14.Height = shadowMapHeight;
			frameBufferRef2 = frameBufferRef14;
			list13[num14] = frameBufferRef14;
			frameBuffer = frameBufferRef2;
			frameBuffer.ColorTextureIds = Array.Empty<int>();
			this.CheckGlError("sdfb-fsm");
			if (this.ShadowMapQuality > 0)
			{
				this.CurrentFrameBuffer = frameBuffer;
				frameBuffer.DepthTextureId = GL.GenTexture();
				GL.BindTexture(TextureTarget.Texture2D, frameBuffer.DepthTextureId);
				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent32, shadowMapWidth, shadowMapHeight, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9729);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, 9729);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareMode, 34894);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareFunc, 515);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, new float[] { 1f, 1f, 1f, 1f });
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, 33069);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, 33069);
				GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, frameBuffer.DepthTextureId, 0);
				GL.DepthFunc(DepthFunction.Less);
				GL.DrawBuffer(DrawBufferMode.None);
				GL.ReadBuffer(ReadBufferMode.None);
				this.CheckFboStatus(FramebufferTarget.Framebuffer, EnumFrameBuffer.ShadowmapFar);
			}
			List<FrameBufferRef> list14 = framebuffers;
			int num15 = 12;
			FrameBufferRef frameBufferRef15 = new FrameBufferRef();
			frameBufferRef15.FboId = GL.GenFramebuffer();
			frameBufferRef15.Width = shadowMapWidth;
			frameBufferRef15.Height = shadowMapHeight;
			frameBufferRef2 = frameBufferRef15;
			list14[num15] = frameBufferRef15;
			frameBuffer = frameBufferRef2;
			frameBuffer.ColorTextureIds = Array.Empty<int>();
			this.CheckGlError("sdfb-nsm-before");
			if (this.ShadowMapQuality > 1)
			{
				this.CurrentFrameBuffer = frameBuffer;
				frameBuffer.DepthTextureId = GL.GenTexture();
				GL.BindTexture(TextureTarget.Texture2D, frameBuffer.DepthTextureId);
				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent32, shadowMapWidth, shadowMapHeight, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9729);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, 9729);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareMode, 34894);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareFunc, 515);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, new float[] { 1f, 1f, 1f, 1f });
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, 33069);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, 33069);
				GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, frameBuffer.DepthTextureId, 0);
				GL.DepthFunc(DepthFunction.Less);
				GL.DrawBuffer(DrawBufferMode.None);
				GL.ReadBuffer(ReadBufferMode.None);
				this.CheckFboStatus(FramebufferTarget.Framebuffer, EnumFrameBuffer.ShadowmapNear);
			}
			this.CheckGlError("sdfb-nsm-after");
			this.PixelPackBuffer = new ClientPlatformWindows.GLBuffer[3];
			for (int l = 0; l < this.PixelPackBuffer.Length; l++)
			{
				this.PixelPackBuffer[l] = new ClientPlatformWindows.GLBuffer
				{
					BufferId = GL.GenBuffer()
				};
				GL.BindBuffer(BufferTarget.PixelPackBuffer, this.PixelPackBuffer[l].BufferId);
				int size2 = 4 * this.sampleCount;
				GL.BufferData(BufferTarget.PixelPackBuffer, size2, IntPtr.Zero, BufferUsageHint.StreamRead);
			}
			GL.BindBuffer(BufferTarget.PixelPackBuffer, 0);
			MeshData quad = QuadMeshUtil.GetCustomQuadModelData(-1f, -1f, 0f, 2f, 2f);
			quad.Normals = null;
			quad.Rgba = null;
			quad.Uv = null;
			if (this.screenQuad != null)
			{
				this.screenQuad.Dispose();
			}
			this.screenQuad = this.UploadMesh(quad);
			if (this.OffscreenBuffer)
			{
				this.CurrentFrameBuffer = framebuffers[0];
			}
			else
			{
				this.CurrentFrameBuffer = null;
				GL.DrawBuffer(DrawBufferMode.Back);
			}
			this.logger.Notification("(Re-)loaded frame buffers");
			return framebuffers;
		}

		private void setupAttachment(FrameBufferRef frameBuffer, int width, int height, int index, PixelFormat rgbaFormat, PixelInternalFormat dataFormat)
		{
			GL.BindTexture(TextureTarget.Texture2D, frameBuffer.ColorTextureIds[index]);
			GL.TexImage2D(TextureTarget.Texture2D, 0, dataFormat, width, height, 0, rgbaFormat, (dataFormat == PixelInternalFormat.Rgba16f) ? PixelType.UnsignedShort : PixelType.UnsignedByte, IntPtr.Zero);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9729);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, 9729);
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, frameBuffer.ColorTextureIds[index], 0);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, Convert.ToInt32(TextureWrapMode.ClampToEdge));
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, Convert.ToInt32(TextureWrapMode.ClampToEdge));
		}

		public void DisposeFrameBuffers(List<FrameBufferRef> buffers)
		{
			for (int i = 0; i < buffers.Count; i++)
			{
				if (buffers[i] != null)
				{
					GL.DeleteFramebuffer(buffers[i].FboId);
					GL.DeleteTexture(buffers[i].DepthTextureId);
					for (int j = 0; j < buffers[i].ColorTextureIds.Length; j++)
					{
						GL.DeleteTexture(buffers[i].ColorTextureIds[j]);
					}
				}
			}
		}

		public override void ClearFrameBuffer(FrameBufferRef framebuffer, bool clearDepth = true)
		{
			this.ClearFrameBuffer(framebuffer, this.clearColor, clearDepth, true);
		}

		public override void ClearFrameBuffer(FrameBufferRef framebuffer, float[] clearColor, bool clearDepthBuffer = true, bool clearColorBuffers = true)
		{
			this.CurrentFrameBuffer = framebuffer;
			if (clearColorBuffers)
			{
				for (int i = 0; i < framebuffer.ColorTextureIds.Length; i++)
				{
					GL.ClearBuffer(ClearBuffer.Color, i, clearColor);
				}
			}
			if (clearDepthBuffer)
			{
				float clearval = 1f;
				GL.ClearBuffer(ClearBuffer.Depth, 0, ref clearval);
			}
		}

		public override void LoadFrameBuffer(FrameBufferRef frameBuffer, int textureId)
		{
			this.CurrentFrameBuffer = frameBuffer;
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, textureId, 0);
			GL.Viewport(0, 0, frameBuffer.Width, frameBuffer.Height);
		}

		public override void LoadFrameBuffer(FrameBufferRef frameBuffer)
		{
			this.CurrentFrameBuffer = frameBuffer;
			GL.Viewport(0, 0, frameBuffer.Width, frameBuffer.Height);
		}

		public override void UnloadFrameBuffer(FrameBufferRef frameBuffer)
		{
			this.LoadFrameBuffer(EnumFrameBuffer.Primary);
		}

		public override void ClearFrameBuffer(EnumFrameBuffer framebuffer)
		{
			switch (framebuffer)
			{
			case EnumFrameBuffer.Default:
				this.CurrentFrameBuffer = null;
				GL.DrawBuffer(DrawBufferMode.Back);
				GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);
				this.CurrentFrameBuffer = this.frameBuffers[0];
				return;
			case EnumFrameBuffer.Primary:
			{
				GL.ClearBuffer(ClearBuffer.Color, 0, new float[] { 0f, 0f, 0f, 1f });
				GL.ClearBuffer(ClearBuffer.Color, 1, new float[] { 0f, 0f, 0f, 1f });
				if (this.RenderSSAO)
				{
					GL.ClearBuffer(ClearBuffer.Color, 2, new float[] { 0f, 0f, 0f, 1f });
					GL.ClearBuffer(ClearBuffer.Color, 3, new float[] { 0f, 0f, 0f, 1f });
				}
				float clearval = 1f;
				GL.ClearBuffer(ClearBuffer.Depth, 0, ref clearval);
				return;
			}
			case EnumFrameBuffer.Transparent:
			{
				GL.ClearBuffer(ClearBuffer.Color, 0, new float[4]);
				ClearBuffer clearBuffer = ClearBuffer.Color;
				int num = 1;
				float[] array = new float[4];
				array[0] = 1f;
				GL.ClearBuffer(clearBuffer, num, array);
				GL.ClearBuffer(ClearBuffer.Color, 2, new float[4]);
				return;
			}
			case EnumFrameBuffer.BlurHorizontalMedRes:
			case EnumFrameBuffer.BlurVerticalMedRes:
			case EnumFrameBuffer.FindBright:
				return;
			case EnumFrameBuffer.LiquidDepth:
				break;
			default:
				if (framebuffer - EnumFrameBuffer.ShadowmapFar > 1)
				{
					return;
				}
				break;
			}
			FrameBufferRef fb = this.FrameBuffers[(int)framebuffer];
			float clearval2 = 1f;
			GL.Viewport(0, 0, fb.Width, fb.Height);
			GL.ClearBuffer(ClearBuffer.Depth, 0, ref clearval2);
		}

		public override void LoadFrameBuffer(EnumFrameBuffer framebuffer)
		{
			switch (framebuffer)
			{
			case EnumFrameBuffer.Default:
				this.CurrentFrameBuffer = null;
				GL.Viewport(0, 0, this.window.ClientSize.X, this.window.ClientSize.Y);
				GL.DrawBuffer(DrawBufferMode.Back);
				return;
			case EnumFrameBuffer.Primary:
				if (this.OffscreenBuffer)
				{
					this.CurrentFrameBuffer = this.frameBuffers[0];
					GL.Viewport(0, 0, (int)(this.ssaaLevel * (float)this.window.ClientSize.X), (int)(this.ssaaLevel * (float)this.window.ClientSize.Y));
					return;
				}
				this.CurrentFrameBuffer = null;
				GL.DrawBuffer(DrawBufferMode.Back);
				break;
			case EnumFrameBuffer.Transparent:
			{
				this.CurrentFrameBuffer = this.frameBuffers[1];
				ScreenManager.FrameProfiler.Mark("rendTransp-fbbound");
				this.GlDisableCullFace();
				this.GlDepthMask(false);
				this.GlEnableDepthTest();
				ScreenManager.FrameProfiler.Mark("rendTransp-dbset");
				DrawBuffersEnum[] bufs = new DrawBuffersEnum[]
				{
					DrawBuffersEnum.ColorAttachment0,
					DrawBuffersEnum.ColorAttachment1,
					DrawBuffersEnum.ColorAttachment2
				};
				GL.DrawBuffers(3, bufs);
				GL.Enable(EnableCap.Blend);
				GL.BlendEquation(0, BlendEquationMode.FuncAdd);
				GL.BlendFunc(0, BlendingFactorSrc.One, BlendingFactorDest.One);
				GL.BlendEquation(1, BlendEquationMode.FuncAdd);
				GL.BlendFunc(1, BlendingFactorSrc.Zero, BlendingFactorDest.OneMinusSrcColor);
				GL.BlendEquation(2, BlendEquationMode.FuncAdd);
				GL.BlendFunc(2, BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
				return;
			}
			case EnumFrameBuffer.BlurHorizontalMedRes:
			case EnumFrameBuffer.BlurVerticalMedRes:
				GL.Viewport(0, 0, (int)(this.ssaaLevel * (float)this.window.ClientSize.X / 2f), (int)(this.ssaaLevel * (float)this.window.ClientSize.Y / 2f));
				this.CurrentFrameBuffer = this.frameBuffers[(int)framebuffer];
				return;
			case EnumFrameBuffer.FindBright:
				this.CurrentFrameBuffer = this.frameBuffers[(int)framebuffer];
				return;
			case EnumFrameBuffer.LiquidDepth:
				this.GlDepthMask(true);
				this.GlEnableDepthTest();
				this.GlToggleBlend(true, EnumBlendMode.Standard);
				this.GlEnableCullFace();
				this.CurrentFrameBuffer = this.frameBuffers[(int)framebuffer];
				return;
			case (EnumFrameBuffer)6:
				break;
			case EnumFrameBuffer.GodRays:
				GL.Viewport(0, 0, (int)(this.ssaaLevel * (float)this.window.ClientSize.X / 2f), (int)(this.ssaaLevel * (float)this.window.ClientSize.Y / 2f));
				this.CurrentFrameBuffer = this.frameBuffers[(int)framebuffer];
				return;
			case EnumFrameBuffer.BlurVerticalLowRes:
			case EnumFrameBuffer.BlurHorizontalLowRes:
				GL.Viewport(0, 0, (int)(this.ssaaLevel * (float)this.window.ClientSize.X / 4f), (int)(this.ssaaLevel * (float)this.window.ClientSize.Y / 4f));
				this.CurrentFrameBuffer = this.frameBuffers[(int)framebuffer];
				return;
			case EnumFrameBuffer.Luma:
				GL.Disable(EnableCap.Blend);
				this.CurrentFrameBuffer = this.frameBuffers[(int)framebuffer];
				return;
			case EnumFrameBuffer.ShadowmapFar:
			case EnumFrameBuffer.ShadowmapNear:
				this.GlDepthMask(true);
				this.GlEnableDepthTest();
				this.GlToggleBlend(true, EnumBlendMode.Standard);
				this.GlEnableCullFace();
				this.CurrentFrameBuffer = this.frameBuffers[(int)framebuffer];
				return;
			case EnumFrameBuffer.SSAO:
			{
				FrameBufferRef fb = this.frameBuffers[(int)framebuffer];
				GL.Viewport(0, 0, fb.Width, fb.Height);
				this.CurrentFrameBuffer = this.frameBuffers[(int)framebuffer];
				return;
			}
			case EnumFrameBuffer.SSAOBlurVertical:
			case EnumFrameBuffer.SSAOBlurHorizontal:
			case EnumFrameBuffer.SSAOBlurVerticalHalfRes:
			case EnumFrameBuffer.SSAOBlurHorizontalHalfRes:
			{
				FrameBufferRef fb2 = this.frameBuffers[(int)framebuffer];
				GL.Viewport(0, 0, fb2.Width, fb2.Height);
				this.CurrentFrameBuffer = fb2;
				return;
			}
			default:
				return;
			}
		}

		public override void UnloadFrameBuffer(EnumFrameBuffer framebuffer)
		{
			if (framebuffer == EnumFrameBuffer.Transparent)
			{
				this.GlDepthMask(true);
			}
			GL.Viewport(0, 0, (int)((float)this.window.ClientSize.X * this.ssaaLevel), (int)((float)this.window.ClientSize.Y * this.ssaaLevel));
			if (this.OffscreenBuffer)
			{
				this.CurrentFrameBuffer = this.frameBuffers[0];
				return;
			}
			this.CurrentFrameBuffer = null;
			GL.DrawBuffer(DrawBufferMode.Back);
		}

		public override void MergeTransparentRenderPass()
		{
			if (this.OffscreenBuffer)
			{
				this.CurrentFrameBuffer = this.frameBuffers[0];
			}
			else
			{
				this.CurrentFrameBuffer = null;
				GL.DrawBuffer(DrawBufferMode.Back);
			}
			GL.Disable(EnableCap.DepthTest);
			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
			ShaderProgramTransparentcompose transparentcompose = ShaderPrograms.Transparentcompose;
			transparentcompose.Use();
			transparentcompose.Revealage2D = this.frameBuffers[1].ColorTextureIds[1];
			transparentcompose.Accumulation2D = this.frameBuffers[1].ColorTextureIds[0];
			transparentcompose.InGlow2D = this.frameBuffers[1].ColorTextureIds[2];
			this.RenderFullscreenTriangle(this.screenQuad);
			transparentcompose.Stop();
		}

		public override void RenderPostprocessingEffects(float[] projectMatrix)
		{
			if (!this.OffscreenBuffer)
			{
				return;
			}
			int width = this.window.ClientSize.X;
			int height = this.window.ClientSize.Y;
			if (this.RenderBloom)
			{
				this.GlToggleBlend(false, EnumBlendMode.Standard);
				this.LoadFrameBuffer(EnumFrameBuffer.FindBright);
				ShaderProgramFindbright findbright = ShaderPrograms.Findbright;
				findbright.Use();
				findbright.ColorTex2D = this.frameBuffers[0].ColorTextureIds[0];
				findbright.GlowTex2D = this.frameBuffers[0].ColorTextureIds[1];
				findbright.AmbientBloomLevel = ClientSettings.AmbientBloomLevel / 100f + this.ShaderUniforms.AmbientBloomLevelAdd[0] + this.ShaderUniforms.AmbientBloomLevelAdd[1] + this.ShaderUniforms.AmbientBloomLevelAdd[2] + this.ShaderUniforms.AmbientBloomLevelAdd[3];
				findbright.ExtraBloom = this.ShaderUniforms.ExtraBloom;
				this.RenderFullscreenTriangle(this.screenQuad);
				findbright.Stop();
				ShaderProgramBlur blur = ShaderPrograms.Blur;
				blur.Use();
				blur.FrameSize = new Vec2f((float)width * this.ssaaLevel, (float)height * this.ssaaLevel);
				this.LoadFrameBuffer(EnumFrameBuffer.BlurHorizontalMedRes);
				blur.IsVertical = 0;
				blur.InputTexture2D = this.frameBuffers[4].ColorTextureIds[0];
				this.RenderFullscreenTriangle(this.screenQuad);
				this.LoadFrameBuffer(EnumFrameBuffer.BlurVerticalMedRes);
				blur.IsVertical = 1;
				blur.InputTexture2D = this.frameBuffers[2].ColorTextureIds[0];
				this.RenderFullscreenTriangle(this.screenQuad);
				GL.Viewport(0, 0, (int)(this.ssaaLevel * (float)this.window.ClientSize.X / 4f), (int)(this.ssaaLevel * (float)this.window.ClientSize.Y / 4f));
				this.LoadFrameBuffer(EnumFrameBuffer.BlurHorizontalLowRes);
				blur.IsVertical = 0;
				blur.InputTexture2D = this.frameBuffers[3].ColorTextureIds[0];
				this.RenderFullscreenTriangle(this.screenQuad);
				this.LoadFrameBuffer(EnumFrameBuffer.BlurVerticalLowRes);
				blur.IsVertical = 1;
				blur.InputTexture2D = this.frameBuffers[9].ColorTextureIds[0];
				this.RenderFullscreenTriangle(this.screenQuad);
				blur.Stop();
				GL.Viewport(0, 0, (int)(this.ssaaLevel * (float)this.window.ClientSize.X), (int)(this.ssaaLevel * (float)this.window.ClientSize.Y));
				this.GlToggleBlend(true, EnumBlendMode.Standard);
			}
			if (this.RenderGodRays)
			{
				this.LoadFrameBuffer(EnumFrameBuffer.GodRays);
				ShaderProgramGodrays godrays = ShaderPrograms.Godrays;
				godrays.Use();
				godrays.InvFrameSizeIn = new Vec2f(1f / ((float)width * this.ssaaLevel), 1f / ((float)height * this.ssaaLevel));
				godrays.SunPosScreenIn = this.ShaderUniforms.SunPositionScreen;
				godrays.SunPos3dIn = this.ShaderUniforms.LightPosition3D;
				godrays.PlayerViewVector = this.ShaderUniforms.PlayerViewVector;
				godrays.Dusk = this.ShaderUniforms.Dusk;
				godrays.IGlobalTimeIn = (float)this.EllapsedMs / 1000f;
				godrays.InputTexture2D = this.frameBuffers[0].ColorTextureIds[0];
				godrays.GlowParts2D = this.frameBuffers[0].ColorTextureIds[1];
				this.RenderFullscreenTriangle(this.screenQuad);
				godrays.Stop();
				GL.Viewport(0, 0, (int)(this.ssaaLevel * (float)this.window.ClientSize.X), (int)(this.ssaaLevel * (float)this.window.ClientSize.Y));
			}
			if (this.RenderSSAO && projectMatrix != null)
			{
				this.GlToggleBlend(false, EnumBlendMode.Standard);
				this.LoadFrameBuffer(EnumFrameBuffer.SSAO);
				GL.ClearBuffer(ClearBuffer.Color, 0, new float[] { 1f, 1f, 1f, 1f });
				ShaderProgramSsao ssao = ShaderPrograms.Ssao;
				ssao.Use();
				ssao.GNormal2D = this.frameBuffers[0].ColorTextureIds[2];
				ssao.GPosition2D = this.frameBuffers[0].ColorTextureIds[3];
				ssao.TexNoise2D = this.frameBuffers[13].ColorTextureIds[1];
				float ssaoSizeFq = ((this.ssaaLevel == 1f) ? 0.5f : 1f);
				ssao.ScreenSize = new Vec2f(this.ssaaLevel * (float)width * ssaoSizeFq, this.ssaaLevel * (float)height * ssaoSizeFq);
				ssao.Revealage2D = this.frameBuffers[1].ColorTextureIds[1];
				ssao.Projection = projectMatrix;
				ssao.SamplesArray(64, this.ssaoKernel);
				this.RenderFullscreenTriangle(this.screenQuad);
				ssao.Stop();
				ShaderProgramBilateralblur progblur = ShaderPrograms.Bilateralblur;
				progblur.Use();
				int q = ((ClientSettings.SSAOQuality == 1) ? 1 : 3);
				for (int i = 0; i < q; i++)
				{
					FrameBufferRef fb = this.frameBuffers[15];
					this.LoadFrameBuffer(EnumFrameBuffer.SSAOBlurHorizontal);
					progblur.FrameSize = new Vec2f((float)fb.Width, (float)fb.Height);
					progblur.IsVertical = 0;
					progblur.InputTexture2D = this.frameBuffers[(i == 0) ? 13 : 14].ColorTextureIds[0];
					progblur.DepthTexture2D = this.frameBuffers[0].DepthTextureId;
					this.RenderFullscreenTriangle(this.screenQuad);
					this.LoadFrameBuffer(EnumFrameBuffer.SSAOBlurVertical);
					progblur.IsVertical = 1;
					progblur.FrameSize = new Vec2f((float)fb.Width, (float)fb.Height);
					progblur.InputTexture2D = this.frameBuffers[15].ColorTextureIds[0];
					this.RenderFullscreenTriangle(this.screenQuad);
				}
				progblur.Stop();
				this.GlToggleBlend(true, EnumBlendMode.Standard);
				GL.Viewport(0, 0, (int)(this.ssaaLevel * (float)this.window.ClientSize.X), (int)(this.ssaaLevel * (float)this.window.ClientSize.Y));
			}
			if (this.RenderFXAA)
			{
				this.LoadFrameBuffer(EnumFrameBuffer.Luma);
				ShaderProgramLuma luma = ShaderPrograms.Luma;
				luma.Use();
				luma.Scene2D = this.frameBuffers[0].ColorTextureIds[0];
				this.RenderFullscreenTriangle(this.screenQuad);
				luma.Stop();
			}
			else
			{
				this.LoadFrameBuffer(EnumFrameBuffer.Luma);
				ShaderProgramBlit blit = ShaderPrograms.Blit;
				blit.Use();
				blit.Scene2D = this.frameBuffers[0].ColorTextureIds[0];
				this.RenderFullscreenTriangle(this.screenQuad);
				blit.Stop();
			}
			GL.Enable(EnableCap.Blend);
			this.LoadFrameBuffer(EnumFrameBuffer.Primary);
			ScreenManager.Platform.CheckGlError(null);
		}

		public override void RenderFinalComposition()
		{
			if (!this.OffscreenBuffer)
			{
				return;
			}
			int bloomPartsTexId = this.frameBuffers[8].ColorTextureIds[0];
			int godrayPartsTexId = this.frameBuffers[7].ColorTextureIds[0];
			int primarySceneTexId = this.frameBuffers[10].ColorTextureIds[0];
			if (this.RenderBloom)
			{
				int num = this.frameBuffers[8].ColorTextureIds[0];
			}
			DrawBuffersEnum[] bufs = new DrawBuffersEnum[] { DrawBuffersEnum.ColorAttachment0 };
			GL.DrawBuffers(1, bufs);
			GL.Disable(EnableCap.DepthTest);
			this.GlToggleBlend(true, EnumBlendMode.Standard);
			ShaderProgramFinal progf = ShaderPrograms.Final;
			progf.Use();
			progf.PrimaryScene2D = primarySceneTexId;
			progf.BloomParts2D = bloomPartsTexId;
			progf.GlowParts2D = this.frameBuffers[0].ColorTextureIds[1];
			progf.GodrayParts2D = godrayPartsTexId;
			progf.AmbientBloomLevel = ClientSettings.AmbientBloomLevel / 100f + this.ShaderUniforms.AmbientBloomLevelAdd[0] + this.ShaderUniforms.AmbientBloomLevelAdd[1] + this.ShaderUniforms.AmbientBloomLevelAdd[2] + this.ShaderUniforms.AmbientBloomLevelAdd[3];
			if (this.RenderSSAO)
			{
				progf.SsaoScene2D = this.frameBuffers[14].ColorTextureIds[0];
			}
			progf.InvFrameSizeIn = new Vec2f(1f / ((float)this.window.ClientSize.X * this.ssaaLevel), 1f / ((float)this.window.ClientSize.Y * this.ssaaLevel));
			progf.GammaLevel = ClientSettings.GammaLevel;
			progf.ExtraGamma = ClientSettings.ExtraGammaLevel;
			progf.ContrastLevel = this.ShaderUniforms.ExtraContrastLevel;
			progf.BrightnessLevel = ClientSettings.BrightnessLevel + Math.Max(0f, this.ShaderUniforms.DropShadowIntensity * 2f - 1.66f) / 3f;
			progf.SepiaLevel = this.ShaderUniforms.SepiaLevel + this.ShaderUniforms.ExtraSepia;
			progf.WindWaveCounter = this.ShaderUniforms.WindWaveCounter;
			progf.GlitchEffectStrength = this.ShaderUniforms.GlitchStrength;
			if (this.RenderGodRays)
			{
				progf.SunPosScreenIn = this.ShaderUniforms.SunPositionScreen;
				progf.SunPos3dIn = this.ShaderUniforms.SunPosition3D;
				progf.PlayerViewVector = this.ShaderUniforms.PlayerViewVector;
			}
			progf.DamageVignetting = this.ShaderUniforms.DamageVignetting;
			progf.DamageVignettingSide = this.ShaderUniforms.DamageVignettingSide;
			progf.FrostVignetting = this.ShaderUniforms.FrostVignetting;
			this.RenderFullscreenTriangle(this.screenQuad);
			progf.Stop();
			if (this.RenderSSAO)
			{
				bufs = new DrawBuffersEnum[]
				{
					DrawBuffersEnum.ColorAttachment0,
					DrawBuffersEnum.ColorAttachment1,
					DrawBuffersEnum.ColorAttachment2,
					DrawBuffersEnum.ColorAttachment3
				};
				GL.DrawBuffers(4, bufs);
				return;
			}
			bufs = new DrawBuffersEnum[]
			{
				DrawBuffersEnum.ColorAttachment0,
				DrawBuffersEnum.ColorAttachment1
			};
			GL.DrawBuffers(2, bufs);
		}

		private void DebugCallback(DebugSource source, DebugType type, int id, DebugSeverity severity, int length, IntPtr message, IntPtr userParam)
		{
			if (type == DebugType.DebugTypeOther)
			{
				return;
			}
			string messageString = Marshal.PtrToStringAnsi(message, length);
			this.Logger.Notification("{0} {1} | {2}", new object[] { severity, type, messageString });
			if (type == DebugType.DebugTypeError)
			{
				throw new Exception(messageString);
			}
		}

		public override void BlitPrimaryToDefault()
		{
			if (!this.OffscreenBuffer)
			{
				return;
			}
			int textureId = this.frameBuffers[0].ColorTextureIds[0];
			this.LoadFrameBuffer(EnumFrameBuffer.Default);
			GL.Viewport(0, 0, this.window.ClientSize.X, this.window.ClientSize.Y);
			ShaderProgramBlit blit = ShaderPrograms.Blit;
			blit.Use();
			blit.Scene2D = textureId;
			this.RenderFullscreenTriangle(this.screenQuad);
			blit.Stop();
		}

		private void CheckFboStatus(FramebufferTarget target, EnumFrameBuffer fbtype)
		{
			this.CheckFboStatus(target, fbtype.ToString() ?? "");
		}

		private void CheckFboStatus(FramebufferTarget target, string fbtype)
		{
			FramebufferErrorCode err = GL.Ext.CheckFramebufferStatus(target);
			switch (err)
			{
			case FramebufferErrorCode.FramebufferComplete:
				return;
			case FramebufferErrorCode.FramebufferIncompleteAttachment:
				throw new Exception("FBO " + fbtype + ": One or more attachment points are not framebuffer attachment complete. This could mean there’s no texture attached or the format isn’t renderable. For color textures this means the base format must be RGB or RGBA and for depth textures it must be a DEPTH_COMPONENT format. Other causes of this error are that the width or height is zero or the z-offset is out of range in case of render to volume.");
			case FramebufferErrorCode.FramebufferIncompleteMissingAttachment:
				throw new Exception("FBO " + fbtype + ": There are no attachments.");
			case FramebufferErrorCode.FramebufferIncompleteDimensionsExt:
				throw new Exception("FBO " + fbtype + ": Attachments are of different size. All attachments must have the same width and height.");
			case FramebufferErrorCode.FramebufferIncompleteFormatsExt:
				throw new Exception("FBO " + fbtype + ": The color attachments have different format. All color attachments must have the same format.");
			case FramebufferErrorCode.FramebufferIncompleteDrawBuffer:
				throw new Exception("FBO " + fbtype + ": An attachment point referenced by GL.DrawBuffers() doesn’t have an attachment.");
			case FramebufferErrorCode.FramebufferIncompleteReadBuffer:
				throw new Exception("FBO " + fbtype + ": The attachment point referenced by GL.ReadBuffers() doesn’t have an attachment.");
			case FramebufferErrorCode.FramebufferUnsupported:
				throw new Exception("FBO " + fbtype + ": This particular FBO configuration is not supported by the implementation.");
			}
			throw new Exception(string.Concat(new string[]
			{
				"FBO ",
				fbtype,
				": Framebuffer unknown error (",
				err.ToString(),
				")"
			}));
		}

		public override bool GlErrorChecking { get; set; }

		public override bool GlDebugMode
		{
			get
			{
				return this.glDebugMode;
			}
			set
			{
				if (value)
				{
					if (!this.supportsGlDebugMode)
					{
						throw new NotSupportedException("Your graphics card does not seem to support gl debug mode (neither GL_ARB_debug_output nor GL_KHR_debug was found)");
					}
					ClientPlatformWindows._debugProcCallback = new DebugProc(this.DebugCallback);
					ClientPlatformWindows._debugProcCallbackHandle = GCHandle.Alloc(ClientPlatformWindows._debugProcCallback);
					GL.DebugMessageCallback(ClientPlatformWindows._debugProcCallback, IntPtr.Zero);
					GL.Enable(EnableCap.DebugOutput);
					GL.Enable(EnableCap.DebugOutputSynchronous);
				}
				else
				{
					GL.Disable(EnableCap.DebugOutput);
					GL.Disable(EnableCap.DebugOutputSynchronous);
				}
				this.glDebugMode = value;
			}
		}

		public override void CheckGlError(string errmsg = null)
		{
			if (!this.GlErrorChecking)
			{
				return;
			}
			OpenTK.Graphics.OpenGL.ErrorCode err = GL.GetError();
			if (err != OpenTK.Graphics.OpenGL.ErrorCode.NoError)
			{
				throw new Exception(string.Format("{0} - OpenGL threw an error: {1}", (errmsg == null) ? "" : (errmsg + " "), err));
			}
		}

		public override void CheckGlErrorAlways(string errmsg = null)
		{
			OpenTK.Graphics.OpenGL.ErrorCode err = GL.GetError();
			if (err != OpenTK.Graphics.OpenGL.ErrorCode.NoError)
			{
				string note = (ClientSettings.GlDebugMode ? "" : ". Enable Gl Debug Mode in the settings or clientsettings.json to track this error");
				string logmsg = string.Format("{0} - OpenGL threw an error: {1}{2}", (errmsg == null) ? "" : (errmsg + " "), err, note);
				this.Logger.Error(logmsg);
			}
			if (err == OpenTK.Graphics.OpenGL.ErrorCode.OutOfMemory)
			{
				throw new OutOfMemoryException("Either the graphics card or the OS ran out of memory! Please close other programs and reduce your view distance to prevent the game from crashing.");
			}
		}

		public override string GlGetError()
		{
			OpenTK.Graphics.OpenGL.ErrorCode err = GL.GetError();
			if (err != OpenTK.Graphics.OpenGL.ErrorCode.NoError)
			{
				return err.ToString();
			}
			return null;
		}

		public override string GetGLShaderVersionString()
		{
			return GL.GetString(StringName.ShadingLanguageVersion);
		}

		public override int GenSampler(bool linear)
		{
			int num = GL.GenSampler();
			GL.SamplerParameter(num, SamplerParameterName.TextureMagFilter, linear ? 9729 : 9728);
			GL.SamplerParameter(num, SamplerParameterName.TextureMinFilter, 9986);
			return num;
		}

		public override void GLWireframes(bool toggle)
		{
			GL.PolygonMode(TriangleFace.FrontAndBack, toggle ? PolygonMode.Line : PolygonMode.Fill);
		}

		public override void GlViewport(int x, int y, int width, int height)
		{
			GL.Viewport(x, y, width, height);
		}

		public override void GlScissor(int x, int y, int width, int height)
		{
			GL.Scissor(x, y, width, height);
		}

		public override bool GlScissorFlagEnabled
		{
			get
			{
				return GL.IsEnabled(EnableCap.ScissorTest);
			}
		}

		public override void GlScissorFlag(bool enable)
		{
			if (enable)
			{
				GL.Enable(EnableCap.ScissorTest);
				return;
			}
			GL.Disable(EnableCap.ScissorTest);
		}

		public override void GlEnableDepthTest()
		{
			GL.Enable(EnableCap.DepthTest);
		}

		public override void GlDisableDepthTest()
		{
			GL.Disable(EnableCap.DepthTest);
		}

		public override void BindTexture2d(int texture)
		{
			GL.ActiveTexture(TextureUnit.Texture0);
			GL.BindTexture(TextureTarget.Texture2D, texture);
		}

		public override void BindTextureCubeMap(int texture)
		{
			GL.BindTexture(TextureTarget.TextureCubeMap, texture);
		}

		public override void UnBindTextureCubeMap()
		{
			GL.BindTexture(TextureTarget.TextureCubeMap, 0);
		}

		public override void GlToggleBlend(bool on, EnumBlendMode blendMode = EnumBlendMode.Standard)
		{
			if (on)
			{
				GL.Enable(EnableCap.Blend);
				switch (blendMode)
				{
				case EnumBlendMode.Multiply:
					GL.BlendFuncSeparate(BlendingFactorSrc.Zero, BlendingFactorDest.OneMinusSrcAlpha, BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha);
					return;
				case EnumBlendMode.Brighten:
					GL.BlendFunc(BlendingFactor.DstColor, BlendingFactor.One);
					return;
				case EnumBlendMode.PremultipliedAlpha:
					GL.BlendFunc(BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);
					return;
				case EnumBlendMode.Glow:
					GL.BlendFuncSeparate(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.One, BlendingFactorSrc.One, BlendingFactorDest.Zero);
					return;
				case EnumBlendMode.Overlay:
					GL.BlendFuncSeparate(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha, BlendingFactorSrc.One, BlendingFactorDest.One);
					return;
				default:
					GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
					if (this.RenderSSAO)
					{
						GL.BlendEquation(2, BlendEquationMode.FuncAdd);
						GL.BlendFunc(2, BlendingFactorSrc.One, BlendingFactorDest.Zero);
						GL.BlendEquation(3, BlendEquationMode.FuncAdd);
						GL.BlendFunc(3, BlendingFactorSrc.One, BlendingFactorDest.Zero);
						return;
					}
					break;
				}
			}
			else
			{
				GL.Disable(EnableCap.Blend);
			}
		}

		public override void GlDisableCullFace()
		{
			GL.Disable(EnableCap.CullFace);
		}

		public override void GlEnableCullFace()
		{
			GL.Enable(EnableCap.CullFace);
		}

		public override void GlClearColorRgbaf(float r, float g, float b, float a)
		{
			GL.ClearColor(r, g, b, a);
		}

		public override void GLLineWidth(float width)
		{
			if (RuntimeEnv.OS == OS.Mac)
			{
				return;
			}
			GL.LineWidth(width);
		}

		public override void SmoothLines(bool on)
		{
			if (RuntimeEnv.OS == OS.Mac)
			{
				return;
			}
			if (on)
			{
				GL.Enable(EnableCap.LineSmooth);
				GL.Hint(HintTarget.LineSmoothHint, HintMode.Nicest);
				return;
			}
			GL.Disable(EnableCap.LineSmooth);
			GL.Hint(HintTarget.LineSmoothHint, HintMode.DontCare);
		}

		public override void GlDepthMask(bool flag)
		{
			GL.DepthMask(flag);
		}

		public override void GlDepthFunc(EnumDepthFunction depthFunc)
		{
			GL.DepthFunc((DepthFunction)depthFunc);
		}

		public override void GlCullFaceBack()
		{
			GL.CullFace(TriangleFace.Back);
		}

		public override void GlGenerateTex2DMipmaps()
		{
			GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
		}

		public override int LoadCairoTexture(ImageSurface surface, bool linearMag)
		{
			if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
			{
				throw new InvalidOperationException("Texture uploads must happen in the main thread. We only have one OpenGL context.");
			}
			int id = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, id);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9729);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, linearMag ? 9729 : 9728);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, surface.Width, surface.Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, surface.DataPtr);
			return id;
		}

		public override void LoadOrUpdateCairoTexture(ImageSurface surface, bool linearMag, ref LoadedTexture intoTexture)
		{
			if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
			{
				throw new InvalidOperationException("Texture uploads must happen in the main thread. We only have one OpenGL context.");
			}
			if (intoTexture.TextureId == 0 || intoTexture.Width != surface.Width || intoTexture.Height != surface.Height)
			{
				if (intoTexture.TextureId != 0)
				{
					GL.DeleteTexture(intoTexture.TextureId);
				}
				intoTexture.TextureId = GL.GenTexture();
				intoTexture.Width = surface.Width;
				intoTexture.Height = surface.Height;
				GL.BindTexture(TextureTarget.Texture2D, intoTexture.TextureId);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9729);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, linearMag ? 9729 : 9728);
				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, surface.Width, surface.Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, surface.DataPtr);
			}
			else
			{
				GL.BindTexture(TextureTarget.Texture2D, intoTexture.TextureId);
				GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, surface.Width, surface.Height, PixelFormat.Bgra, PixelType.UnsignedByte, surface.DataPtr);
			}
			this.CheckGlError("LoadOrUpdateCairoTexture");
		}

		public override int LoadTexture(SKBitmap bmp, bool linearMag = false, int clampMode = 0, bool generateMipmaps = false)
		{
			return this.LoadTexture(new BitmapExternal(bmp), linearMag, clampMode, generateMipmaps);
		}

		public override void LoadIntoTexture(IBitmap srcBmp, int targetTextureId, int destX, int destY, bool generateMipmaps = false)
		{
			GL.BindTexture(TextureTarget.Texture2D, targetTextureId);
			BitmapExternal bmpExt = srcBmp as BitmapExternal;
			if (bmpExt != null)
			{
				GL.TexSubImage2D(TextureTarget.Texture2D, 0, destX, destY, srcBmp.Width, srcBmp.Height, PixelFormat.Bgra, PixelType.UnsignedByte, bmpExt.PixelsPtrAndLock);
			}
			else
			{
				GL.TexSubImage2D<int>(TextureTarget.Texture2D, 0, destX, destY, srcBmp.Width, srcBmp.Height, PixelFormat.Bgra, PixelType.UnsignedByte, srcBmp.Pixels);
			}
			if (this.ENABLE_MIPMAPS && generateMipmaps)
			{
				this.BuildMipMaps(targetTextureId);
			}
		}

		public override int LoadTexture(IBitmap bmp, bool linearMag = false, int clampMode = 0, bool generateMipmaps = false)
		{
			if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
			{
				throw new InvalidOperationException("Texture uploads must happen in the main thread. We only have one OpenGL context.");
			}
			int id = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, id);
			if (this.ENABLE_ANISOTROPICFILTERING)
			{
				float maxAniso = GL.GetFloat(GetPName.MaxTextureMaxAnisotropy);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxAnisotropy, maxAniso);
			}
			if (clampMode == 1)
			{
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, 33071);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, 33071);
			}
			else if (clampMode == 2)
			{
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, 10497);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, 10497);
			}
			BitmapExternal bitmapExternal = bmp as BitmapExternal;
			if (bitmapExternal != null)
			{
				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bmp.Width, bmp.Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, bitmapExternal.PixelsPtrAndLock);
			}
			else
			{
				GL.TexImage2D<int>(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bmp.Width, bmp.Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, bmp.Pixels);
			}
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9729);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, linearMag ? 9729 : 9728);
			if (this.ENABLE_MIPMAPS && generateMipmaps)
			{
				this.BuildMipMaps(id);
			}
			return id;
		}

		public override void LoadOrUpdateTextureFromBgra_DeferMipMap(int[] rgbaPixels, bool linearMag, int clampMode, ref LoadedTexture intoTexture)
		{
			PixelFormat format = PixelFormat.Bgra;
			this.LoadOrUpdateTextureFromPixels(rgbaPixels, linearMag, clampMode, ref intoTexture, format, false);
		}

		public override void LoadOrUpdateTextureFromBgra(int[] rgbaPixels, bool linearMag, int clampMode, ref LoadedTexture intoTexture)
		{
			PixelFormat format = PixelFormat.Bgra;
			this.LoadOrUpdateTextureFromPixels(rgbaPixels, linearMag, clampMode, ref intoTexture, format, true);
		}

		public override void LoadOrUpdateTextureFromRgba(int[] rgbaPixels, bool linearMag, int clampMode, ref LoadedTexture intoTexture)
		{
			PixelFormat format = PixelFormat.Rgba;
			this.LoadOrUpdateTextureFromPixels(rgbaPixels, linearMag, clampMode, ref intoTexture, format, true);
		}

		private void LoadOrUpdateTextureFromPixels(int[] rgbaPixels, bool linearMag, int clampMode, ref LoadedTexture intoTexture, PixelFormat format, bool makeMipMap)
		{
			if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
			{
				throw new InvalidOperationException("Texture uploads must happen in the main thread. We only have one OpenGL context.");
			}
			if (intoTexture.TextureId == 0 || intoTexture.Width * intoTexture.Height != rgbaPixels.Length)
			{
				if (intoTexture.TextureId != 0)
				{
					GL.DeleteTexture(intoTexture.TextureId);
				}
				intoTexture.TextureId = GL.GenTexture();
				GL.BindTexture(TextureTarget.Texture2D, intoTexture.TextureId);
				if (this.ENABLE_ANISOTROPICFILTERING)
				{
					float maxAniso = GL.GetFloat(GetPName.MaxTextureMaxAnisotropy);
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxAnisotropy, maxAniso);
				}
				if (clampMode == 1)
				{
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, 33071);
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, 33071);
				}
				GL.TexImage2D<int>(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, intoTexture.Width, intoTexture.Height, 0, format, PixelType.UnsignedByte, rgbaPixels);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9729);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, linearMag ? 9729 : 9728);
				if (makeMipMap)
				{
					this.BuildMipMaps(intoTexture.TextureId);
					return;
				}
			}
			else
			{
				GL.BindTexture(TextureTarget.Texture2D, intoTexture.TextureId);
				GL.TexSubImage2D<int>(TextureTarget.Texture2D, 0, 0, 0, intoTexture.Width, intoTexture.Height, format, PixelType.UnsignedByte, rgbaPixels);
			}
		}

		public override void BuildMipMaps(int textureId)
		{
			if (!this.ENABLE_MIPMAPS)
			{
				return;
			}
			GL.BindTexture(TextureTarget.Texture2D, textureId);
			int MipMapCount;
			GL.GetTexParameter(TextureTarget.Texture2D, GetTextureParameter.TextureMaxLevel, out MipMapCount);
			if (MipMapCount > 0)
			{
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9986);
				GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureLodBias, 0f);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, ClientSettings.MipMapLevel);
			}
		}

		public override int Load3DTextureCube(BitmapRef[] bmps)
		{
			GL.ActiveTexture(TextureUnit.Texture0);
			int textureId = GL.GenTexture();
			GL.BindTexture(TextureTarget.TextureCubeMap, textureId);
			for (int i = 0; i < 6; i++)
			{
				this.Load3DTextureSide((BitmapExternal)bmps[i], TextureTarget.TextureCubeMapPositiveX + i);
			}
			GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, 9729);
			GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, 9729);
			GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, Convert.ToInt32(TextureWrapMode.ClampToEdge));
			GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, Convert.ToInt32(TextureWrapMode.ClampToEdge));
			GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, Convert.ToInt32(TextureWrapMode.ClampToEdge));
			return textureId;
		}

		private void Load3DTextureSide(BitmapExternal bmp, TextureTarget target)
		{
			GL.TexImage2D(target, 0, PixelInternalFormat.Rgba, bmp.Width, bmp.Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, bmp.PixelsPtrAndLock);
		}

		public override void GLDeleteTexture(int id)
		{
			GL.DeleteTexture(id);
		}

		public override int GlGetMaxTextureSize()
		{
			int size = 1024;
			try
			{
				GL.GetInteger(GetPName.MaxTextureSize, out size);
			}
			catch
			{
			}
			return size;
		}

		public override UBORef CreateUBO(int shaderProgramId, int bindingPoint, string blockName, int size)
		{
			int handle = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.UniformBuffer, handle);
			GL.BufferData(BufferTarget.UniformBuffer, size, IntPtr.Zero, BufferUsageHint.DynamicDraw);
			ScreenManager.Platform.CheckGlError(null);
			int uboIndex = GL.GetUniformBlockIndex(shaderProgramId, blockName);
			ScreenManager.Platform.CheckGlError(null);
			GL.UniformBlockBinding(shaderProgramId, uboIndex, bindingPoint);
			GL.BindBufferBase(BufferRangeTarget.UniformBuffer, bindingPoint, handle);
			ScreenManager.Platform.CheckGlError(null);
			UBO ubo = new UBO();
			ubo.Handle = handle;
			ubo.Size = size;
			ubo.Unbind();
			ScreenManager.Platform.CheckGlError(null);
			return ubo;
		}

		public override void UpdateMesh(MeshRef modelRef, MeshData data)
		{
			VAO vao = (VAO)modelRef;
			BufferAccessMask flags = BufferAccessMask.MapWriteBit | BufferAccessMask.MapUnsynchronizedBit;
			if (vao.Persistent)
			{
				flags |= BufferAccessMask.MapFlushExplicitBit | BufferAccessMask.MapPersistentBit;
			}
			bool pers = vao.Persistent;
			if (data.xyz != null)
			{
				this.updateVAO(data.xyz, data.XyzOffset, data.XyzCount, vao.xyzVboId, vao.xyzPtr, pers);
			}
			if (data.Normals != null && data.VerticesCount > 0)
			{
				this.updateVAO(data.Normals, data.NormalsOffset, data.VerticesCount, vao.normalsVboId, vao.normalsPtr, pers);
			}
			if (data.Uv != null && data.UvCount > 0)
			{
				this.updateVAO(data.Uv, data.UvOffset, data.UvCount, vao.uvVboId, vao.uvPtr, pers);
			}
			if (data.Rgba != null && data.RgbaCount > 0)
			{
				this.updateVAO(data.Rgba, data.RgbaOffset, data.RgbaCount, vao.rgbaVboId, vao.rgbaPtr, pers);
			}
			if (data.Flags != null && data.FlagsCount > 0)
			{
				this.updateVAO(data.Flags, data.FlagsOffset, data.FlagsCount, vao.flagsVboId, vao.flagsPtr, pers);
			}
			if (data.CustomFloats != null && data.CustomFloats.Count > 0)
			{
				this.updateVAO(data.CustomFloats.Values, data.CustomFloats.BaseOffset, data.CustomFloats.Count, vao.customDataFloatVboId, vao.customDataFloatPtr, pers);
			}
			if (data.CustomShorts != null && data.CustomShorts.Count > 0)
			{
				this.updateVAO(data.CustomShorts.Values, data.CustomShorts.BaseOffset, data.CustomShorts.Count, vao.customDataShortVboId, vao.customDataShortPtr, pers);
			}
			if (data.CustomInts != null && data.CustomInts.Count > 0)
			{
				this.updateVAO(data.CustomInts.Values, data.CustomInts.BaseOffset, data.CustomInts.Count, vao.customDataIntVboId, vao.customDataIntPtr, pers);
			}
			if (data.CustomBytes != null && data.CustomBytes.Count > 0)
			{
				this.updateVAO(data.CustomBytes.Values, data.CustomBytes.BaseOffset, data.CustomBytes.Count, vao.customDataByteVboId, vao.customDataBytePtr, pers);
			}
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			this.updateIndices(data.Indices, data.IndicesOffset, data.IndicesCount, vao, pers);
			if (this.GlErrorChecking && this.GlDebugMode)
			{
				this.CheckGlError(string.Format("Error when trying to update vao indices, modeldata xyz/rgba/uv/indices sizes: {0}/{1}/{2}/{3}", new object[] { data.XyzCount, data.RgbaCount, data.UvCount, data.IndicesCount }));
			}
		}

		private unsafe void updateVAO(float[] data, int offset, int count, int vboId, IntPtr vboPtr, bool pers)
		{
			GL.BindBuffer(BufferTarget.ArrayBuffer, vboId);
			if (pers)
			{
				float* ptr = vboPtr / 4 + offset / 4 * 4;
				for (int i = 0; i < count; i++)
				{
					*(ptr++) = data[i];
				}
				return;
			}
			GL.BufferSubData<float>(BufferTarget.ArrayBuffer, (IntPtr)offset, 4 * count, data);
		}

		private unsafe void updateVAO(int[] data, int offset, int count, int vboId, IntPtr vboPtr, bool pers)
		{
			GL.BindBuffer(BufferTarget.ArrayBuffer, vboId);
			if (pers)
			{
				int* ptr = vboPtr / 4 + offset / 4 * 4;
				for (int i = 0; i < count; i++)
				{
					*(ptr++) = data[i];
				}
				return;
			}
			GL.BufferSubData<int>(BufferTarget.ArrayBuffer, (IntPtr)offset, 4 * count, data);
		}

		private unsafe void updateVAO(short[] data, int offset, int count, int vboId, IntPtr vboPtr, bool pers)
		{
			GL.BindBuffer(BufferTarget.ArrayBuffer, vboId);
			if (pers)
			{
				short* ptr = vboPtr / 2 + offset / 2 * 2;
				for (int i = 0; i < count; i++)
				{
					*(ptr++) = data[i];
				}
				return;
			}
			GL.BufferSubData<short>(BufferTarget.ArrayBuffer, (IntPtr)offset, 2 * count, data);
		}

		private unsafe void updateVAO(ushort[] data, int offset, int count, int vboId, IntPtr vboPtr, bool pers)
		{
			GL.BindBuffer(BufferTarget.ArrayBuffer, vboId);
			if (pers)
			{
				ushort* ptr = vboPtr / 2 + offset / 2 * 2;
				for (int i = 0; i < count; i++)
				{
					*(ptr++) = data[i];
				}
				return;
			}
			GL.BufferSubData<ushort>(BufferTarget.ArrayBuffer, (IntPtr)offset, 2 * count, data);
		}

		private unsafe void updateVAO(byte[] data, int offset, int count, int vboId, IntPtr vboPtr, bool pers)
		{
			GL.BindBuffer(BufferTarget.ArrayBuffer, vboId);
			if (pers)
			{
				byte* ptr = vboPtr + (IntPtr)(offset / 1);
				for (int i = 0; i < count; i++)
				{
					*(ptr++) = data[i];
				}
				return;
			}
			GL.BufferSubData<byte>(BufferTarget.ArrayBuffer, (IntPtr)offset, count, data);
		}

		private unsafe void updateIndices(int[] Indices, int IndicesOffset, int IndicesCount, VAO vao, bool pers)
		{
			if (Indices != null)
			{
				GL.BindBuffer(BufferTarget.ElementArrayBuffer, vao.vboIdIndex);
				if (pers)
				{
					int* ptr = vao.indicesPtr;
					ptr += IndicesOffset / 4;
					for (int i = 0; i < IndicesCount; i++)
					{
						*(ptr++) = Indices[i];
					}
				}
				else
				{
					GL.BufferSubData<int>(BufferTarget.ElementArrayBuffer, (IntPtr)IndicesOffset, 4 * IndicesCount, Indices);
				}
				GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
				vao.IndicesCount = IndicesCount;
			}
		}

		public override MeshRef AllocateEmptyMesh(int xyzSize, int normalsSize, int uvSize, int rgbaSize, int flagsSize, int indicesSize, CustomMeshDataPartFloat customFloats, CustomMeshDataPartShort customShorts, CustomMeshDataPartByte customBytes, CustomMeshDataPartInt customInts, EnumDrawMode drawMode = EnumDrawMode.Triangles, bool staticDraw = true)
		{
			VAO vao = new VAO();
			int vaoId = GL.GenVertexArray();
			int vaoSlotNumber = 0;
			GL.BindVertexArray(vaoId);
			int xyzVboId = 0;
			int normalsVboId = 0;
			int uvVboId = 0;
			int rgbaVboId = 0;
			int flagsVboId = 0;
			BufferUsageHint usageHint = (staticDraw ? BufferUsageHint.StaticDraw : BufferUsageHint.DynamicDraw);
			bool flag = this.supportsPersistentMapping && !staticDraw;
			bool doPStorage = false;
			BufferStorageFlags flags = (BufferStorageFlags)450;
			MapBufferAccessMask mapflags = MapBufferAccessMask.MapWriteBit | MapBufferAccessMask.MapPersistentBit | MapBufferAccessMask.MapCoherentBit;
			if (xyzSize > 0)
			{
				xyzVboId = this.GenArrayBuffer(xyzSize, ref vao.xyzPtr, doPStorage, flags, mapflags, usageHint);
				GL.VertexAttribPointer(vaoSlotNumber++, 3, VertexAttribPointerType.Float, false, 0, 0);
			}
			this.CheckGlError("Failed loading model");
			if (normalsSize > 0)
			{
				normalsVboId = this.GenArrayBuffer(normalsSize, ref vao.normalsPtr, doPStorage, flags, mapflags, usageHint);
				GL.VertexAttribPointer(vaoSlotNumber++, 4, VertexAttribPointerType.Int2101010Rev, true, 0, 0);
			}
			if (uvSize > 0)
			{
				uvVboId = this.GenArrayBuffer(uvSize, ref vao.uvPtr, doPStorage, flags, mapflags, usageHint);
				GL.VertexAttribPointer(vaoSlotNumber++, 2, VertexAttribPointerType.Float, false, 0, 0);
			}
			if (rgbaSize > 0)
			{
				rgbaVboId = this.GenArrayBuffer(rgbaSize, ref vao.rgbaPtr, doPStorage, flags, mapflags, usageHint);
				GL.VertexAttribPointer(vaoSlotNumber++, 4, VertexAttribPointerType.UnsignedByte, true, 0, 0);
			}
			if (flagsSize > 0)
			{
				flagsVboId = this.GenArrayBuffer(flagsSize, ref vao.flagsPtr, doPStorage, flags, mapflags, usageHint);
				GL.VertexAttribIPointer(vaoSlotNumber++, 1, VertexAttribIntegerType.UnsignedInt, 0, IntPtr.Zero);
			}
			vaoSlotNumber = this.AddCustoms(vao, vaoSlotNumber, customFloats, customShorts, customInts, customBytes, doPStorage, flags, mapflags, 0);
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			int vboIdIndex = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, vboIdIndex);
			if (doPStorage)
			{
				GL.BufferStorage(BufferTarget.ElementArrayBuffer, indicesSize, IntPtr.Zero, flags);
				vao.indicesPtr = GL.MapBufferRange(BufferTarget.ElementArrayBuffer, IntPtr.Zero, indicesSize, mapflags);
			}
			else
			{
				GL.BufferData(BufferTarget.ElementArrayBuffer, indicesSize, IntPtr.Zero, BufferUsageHint.StaticDraw);
			}
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
			GL.BindVertexArray(0);
			this.CheckGlError("Failed loading model");
			vao.Persistent = doPStorage;
			vao.VaoId = vaoId;
			vao.IndicesCount = indicesSize;
			vao.vaoSlotNumber = vaoSlotNumber;
			vao.vboIdIndex = vboIdIndex;
			vao.normalsVboId = normalsVboId;
			vao.xyzVboId = xyzVboId;
			vao.uvVboId = uvVboId;
			vao.rgbaVboId = rgbaVboId;
			vao.flagsVboId = flagsVboId;
			vao.drawMode = this.DrawModeToPrimiteType(drawMode);
			return vao;
		}

		private int AddCustoms(VAO vao, int vaoSlotNumber, CustomMeshDataPartFloat customFloats, CustomMeshDataPartShort customShorts, CustomMeshDataPartInt customInts, CustomMeshDataPartByte customBytes, bool doPStorage, BufferStorageFlags flags, MapBufferAccessMask mapflags, int pruneCustomInts)
		{
			int customDataFloatsVboId = 0;
			int customDataBytesVboId = 0;
			int customDataIntsVboId = 0;
			int customDataShortsVboId = 0;
			if (customFloats != null)
			{
				BufferUsageHint hint = (customFloats.StaticDraw ? BufferUsageHint.StaticDraw : BufferUsageHint.DynamicDraw);
				customDataFloatsVboId = this.GenArrayBuffer(4 * customFloats.AllocationSize, ref vao.customDataFloatPtr, doPStorage, flags, mapflags, hint);
				int[] interleaveSizes = customFloats.InterleaveSizes;
				for (int i = 0; i < interleaveSizes.Length; i++)
				{
					GL.VertexAttribPointer(vaoSlotNumber, interleaveSizes[i], VertexAttribPointerType.Float, false, customFloats.InterleaveStride, customFloats.InterleaveOffsets[i]);
					if (customFloats.Instanced)
					{
						GL.VertexAttribDivisor(vaoSlotNumber, 1);
					}
					vaoSlotNumber++;
				}
			}
			if (customShorts != null)
			{
				BufferUsageHint hint2 = (customShorts.StaticDraw ? BufferUsageHint.StaticDraw : BufferUsageHint.DynamicDraw);
				customDataShortsVboId = this.GenArrayBuffer(2 * customShorts.AllocationSize, ref vao.customDataShortPtr, doPStorage, flags, mapflags, hint2);
				int[] interleaveSizes2 = customShorts.InterleaveSizes;
				for (int j = 0; j < interleaveSizes2.Length; j++)
				{
					if (customShorts.Conversion == DataConversion.Integer)
					{
						GL.VertexAttribIPointer(vaoSlotNumber, interleaveSizes2[j], VertexAttribIntegerType.Short, customShorts.InterleaveStride, (IntPtr)customShorts.InterleaveOffsets[j]);
					}
					else
					{
						GL.VertexAttribPointer(vaoSlotNumber, interleaveSizes2[j], VertexAttribPointerType.UnsignedShort, customShorts.Conversion == DataConversion.NormalizedFloat, customShorts.InterleaveStride, customShorts.InterleaveOffsets[j]);
					}
					if (customShorts.Instanced)
					{
						GL.VertexAttribDivisor(vaoSlotNumber, 1);
					}
					vaoSlotNumber++;
				}
			}
			if (customInts != null && (pruneCustomInts == 0 || customInts.InterleaveStride > 4))
			{
				int prunedDivisor = 1 + pruneCustomInts;
				BufferUsageHint hint3 = (customInts.StaticDraw ? BufferUsageHint.StaticDraw : BufferUsageHint.DynamicDraw);
				customDataIntsVboId = this.GenArrayBuffer(4 * customInts.AllocationSize / prunedDivisor, ref vao.customDataIntPtr, doPStorage, flags, mapflags, hint3);
				int[] interleaveSizes3 = customInts.InterleaveSizes;
				for (int k = 0; k < interleaveSizes3.Length; k++)
				{
					if (k - pruneCustomInts != -1)
					{
						if (customInts.Conversion == DataConversion.Integer)
						{
							GL.VertexAttribIPointer(vaoSlotNumber, interleaveSizes3[k], VertexAttribIntegerType.UnsignedInt, customInts.InterleaveStride / prunedDivisor, (IntPtr)customInts.InterleaveOffsets[k - pruneCustomInts]);
						}
						else
						{
							GL.VertexAttribPointer(vaoSlotNumber, interleaveSizes3[k], VertexAttribPointerType.UnsignedInt, customInts.Conversion == DataConversion.NormalizedFloat, customInts.InterleaveStride / prunedDivisor, customInts.InterleaveOffsets[k - pruneCustomInts]);
						}
						if (customInts.Instanced)
						{
							GL.VertexAttribDivisor(vaoSlotNumber, 1);
						}
						vaoSlotNumber++;
					}
				}
			}
			if (customBytes != null)
			{
				BufferUsageHint hint4 = (customBytes.StaticDraw ? BufferUsageHint.StaticDraw : BufferUsageHint.DynamicDraw);
				customDataBytesVboId = this.GenArrayBuffer(customBytes.AllocationSize, ref vao.customDataBytePtr, doPStorage, flags, mapflags, hint4);
				int[] interleaveSizes4 = customBytes.InterleaveSizes;
				for (int l = 0; l < interleaveSizes4.Length; l++)
				{
					if (customBytes.Conversion == DataConversion.Integer)
					{
						GL.VertexAttribIPointer(vaoSlotNumber, interleaveSizes4[l], VertexAttribIntegerType.UnsignedByte, customBytes.InterleaveStride, (IntPtr)customBytes.InterleaveOffsets[l]);
					}
					else
					{
						GL.VertexAttribPointer(vaoSlotNumber, interleaveSizes4[l], VertexAttribPointerType.UnsignedByte, customBytes.Conversion == DataConversion.NormalizedFloat, customBytes.InterleaveStride, customBytes.InterleaveOffsets[l]);
					}
					if (customBytes.Instanced)
					{
						GL.VertexAttribDivisor(vaoSlotNumber, 1);
					}
					vaoSlotNumber++;
				}
			}
			vao.customDataFloatVboId = customDataFloatsVboId;
			vao.customDataByteVboId = customDataBytesVboId;
			vao.customDataIntVboId = customDataIntsVboId;
			vao.customDataShortVboId = customDataShortsVboId;
			return vaoSlotNumber;
		}

		private int GenArrayBuffer(int dataSize, ref IntPtr ptr, bool doPStorage, BufferStorageFlags flags, MapBufferAccessMask mapflags, BufferUsageHint usageHint)
		{
			int id = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, id);
			if (doPStorage)
			{
				GL.BufferStorage(BufferTarget.ArrayBuffer, dataSize, IntPtr.Zero, flags);
				ptr = GL.MapBufferRange(BufferTarget.ArrayBuffer, IntPtr.Zero, dataSize, mapflags);
			}
			else
			{
				GL.BufferData(BufferTarget.ArrayBuffer, dataSize, IntPtr.Zero, usageHint);
			}
			return id;
		}

		public override MeshRef UploadMesh(MeshData data)
		{
			int vaoId = GL.GenVertexArray();
			int vaoSlotNumber = 0;
			GL.BindVertexArray(vaoId);
			int xyzVboId = 0;
			int normalsVboId = 0;
			int uvVboId = 0;
			int rgbaVboId = 0;
			int customDataFloatVboId = 0;
			int customDataShortVboId = 0;
			int customDataIntVboId = 0;
			int customDataByteVboId = 0;
			int flagsVboId = 0;
			if (data.xyz != null)
			{
				BufferUsageHint hint = (data.XyzStatic ? BufferUsageHint.StaticDraw : BufferUsageHint.DynamicDraw);
				xyzVboId = GL.GenBuffer();
				GL.BindBuffer(BufferTarget.ArrayBuffer, xyzVboId);
				GL.BufferData<float>(BufferTarget.ArrayBuffer, 4 * data.XyzCount, data.xyz, hint);
				GL.VertexAttribPointer(vaoSlotNumber, 3, VertexAttribPointerType.Float, false, 0, 0);
				if (data.XyzInstanced)
				{
					GL.VertexAttribDivisor(vaoSlotNumber, 1);
				}
				vaoSlotNumber++;
			}
			if (data.Normals != null)
			{
				BufferUsageHint hint = (data.XyzStatic ? BufferUsageHint.StaticDraw : BufferUsageHint.DynamicDraw);
				normalsVboId = GL.GenBuffer();
				GL.BindBuffer(BufferTarget.ArrayBuffer, normalsVboId);
				GL.BufferData<int>(BufferTarget.ArrayBuffer, 4 * data.VerticesCount, data.Normals, hint);
				GL.VertexAttribPointer(vaoSlotNumber, 4, VertexAttribPointerType.Int2101010Rev, true, 0, 0);
				if (data.XyzInstanced)
				{
					GL.VertexAttribDivisor(vaoSlotNumber, 1);
				}
				vaoSlotNumber++;
			}
			if (data.Uv != null)
			{
				BufferUsageHint hint = (data.UvStatic ? BufferUsageHint.StaticDraw : BufferUsageHint.DynamicDraw);
				uvVboId = GL.GenBuffer();
				GL.BindBuffer(BufferTarget.ArrayBuffer, uvVboId);
				GL.BufferData<float>(BufferTarget.ArrayBuffer, 4 * data.UvCount, data.Uv, hint);
				GL.VertexAttribPointer(vaoSlotNumber, 2, VertexAttribPointerType.Float, false, 0, 0);
				if (data.UvInstanced)
				{
					GL.VertexAttribDivisor(vaoSlotNumber, 1);
				}
				vaoSlotNumber++;
			}
			if (data.Rgba != null)
			{
				BufferUsageHint hint = (data.RgbaStatic ? BufferUsageHint.StaticDraw : BufferUsageHint.DynamicDraw);
				rgbaVboId = GL.GenBuffer();
				GL.BindBuffer(BufferTarget.ArrayBuffer, rgbaVboId);
				GL.BufferData<byte>(BufferTarget.ArrayBuffer, data.RgbaCount, data.Rgba, hint);
				GL.VertexAttribPointer(vaoSlotNumber, 4, VertexAttribPointerType.UnsignedByte, true, 0, 0);
				if (data.RgbaInstanced)
				{
					GL.VertexAttribDivisor(vaoSlotNumber, 1);
				}
				vaoSlotNumber++;
			}
			if (data.Flags != null)
			{
				BufferUsageHint hint = (data.FlagsStatic ? BufferUsageHint.StaticDraw : BufferUsageHint.DynamicDraw);
				flagsVboId = GL.GenBuffer();
				GL.BindBuffer(BufferTarget.ArrayBuffer, flagsVboId);
				GL.BufferData<int>(BufferTarget.ArrayBuffer, 4 * data.Flags.Length, data.Flags, hint);
				GL.VertexAttribIPointer(vaoSlotNumber, 1, VertexAttribIntegerType.UnsignedInt, 0, IntPtr.Zero);
				if (data.FlagsInstanced)
				{
					GL.VertexAttribDivisor(vaoSlotNumber, 1);
				}
				vaoSlotNumber++;
			}
			if (data.CustomFloats != null)
			{
				BufferUsageHint hint = (data.CustomFloats.StaticDraw ? BufferUsageHint.StaticDraw : BufferUsageHint.DynamicDraw);
				customDataFloatVboId = GL.GenBuffer();
				GL.BindBuffer(BufferTarget.ArrayBuffer, customDataFloatVboId);
				GL.BufferData<float>(BufferTarget.ArrayBuffer, 4 * data.CustomFloats.AllocationSize, data.CustomFloats.Values, hint);
				for (int i = 0; i < data.CustomFloats.InterleaveSizes.Length; i++)
				{
					GL.VertexAttribPointer(vaoSlotNumber, data.CustomFloats.InterleaveSizes[i], VertexAttribPointerType.Float, false, data.CustomFloats.InterleaveStride, data.CustomFloats.InterleaveOffsets[i]);
					if (data.CustomFloats.Instanced)
					{
						GL.VertexAttribDivisor(vaoSlotNumber, 1);
					}
					vaoSlotNumber++;
				}
			}
			if (data.CustomShorts != null)
			{
				BufferUsageHint hint = (data.CustomShorts.StaticDraw ? BufferUsageHint.StaticDraw : BufferUsageHint.DynamicDraw);
				customDataShortVboId = GL.GenBuffer();
				GL.BindBuffer(BufferTarget.ArrayBuffer, customDataShortVboId);
				GL.BufferData<short>(BufferTarget.ArrayBuffer, 2 * data.CustomShorts.AllocationSize, data.CustomShorts.Values, hint);
				for (int j = 0; j < data.CustomShorts.InterleaveSizes.Length; j++)
				{
					if (data.CustomShorts.Conversion == DataConversion.Integer)
					{
						GL.VertexAttribIPointer(vaoSlotNumber, data.CustomShorts.InterleaveSizes[j], VertexAttribIntegerType.Short, data.CustomShorts.InterleaveStride, IntPtr.Zero);
					}
					else
					{
						GL.VertexAttribPointer(vaoSlotNumber, data.CustomShorts.InterleaveSizes[j], VertexAttribPointerType.Short, data.CustomShorts.Conversion == DataConversion.NormalizedFloat, data.CustomShorts.InterleaveStride, data.CustomShorts.InterleaveOffsets[j]);
					}
					if (data.CustomShorts.Instanced)
					{
						GL.VertexAttribDivisor(vaoSlotNumber, 1);
					}
					vaoSlotNumber++;
				}
			}
			if (data.CustomInts != null)
			{
				BufferUsageHint hint = (data.CustomInts.StaticDraw ? BufferUsageHint.StaticDraw : BufferUsageHint.DynamicDraw);
				customDataIntVboId = GL.GenBuffer();
				GL.BindBuffer(BufferTarget.ArrayBuffer, customDataIntVboId);
				GL.BufferData<int>(BufferTarget.ArrayBuffer, 4 * data.CustomInts.AllocationSize, data.CustomInts.Values, hint);
				for (int k = 0; k < data.CustomInts.InterleaveSizes.Length; k++)
				{
					GL.VertexAttribIPointer(vaoSlotNumber, data.CustomInts.InterleaveSizes[k], VertexAttribIntegerType.Int, data.CustomInts.InterleaveStride, IntPtr.Zero);
					if (data.CustomInts.Instanced)
					{
						GL.VertexAttribDivisor(vaoSlotNumber, 1);
					}
					vaoSlotNumber++;
				}
			}
			if (data.CustomBytes != null)
			{
				BufferUsageHint hint = (data.CustomBytes.StaticDraw ? BufferUsageHint.StaticDraw : BufferUsageHint.DynamicDraw);
				customDataByteVboId = GL.GenBuffer();
				GL.BindBuffer(BufferTarget.ArrayBuffer, customDataByteVboId);
				GL.BufferData<byte>(BufferTarget.ArrayBuffer, data.CustomBytes.AllocationSize, data.CustomBytes.Values, hint);
				for (int l = 0; l < data.CustomBytes.InterleaveSizes.Length; l++)
				{
					if (data.CustomBytes.Conversion == DataConversion.Integer)
					{
						GL.VertexAttribIPointer(vaoSlotNumber, data.CustomBytes.InterleaveSizes[l], VertexAttribIntegerType.UnsignedByte, data.CustomBytes.InterleaveStride, IntPtr.Zero);
					}
					else
					{
						GL.VertexAttribPointer(vaoSlotNumber, data.CustomBytes.InterleaveSizes[l], VertexAttribPointerType.UnsignedByte, data.CustomBytes.Conversion == DataConversion.NormalizedFloat, data.CustomBytes.InterleaveStride, data.CustomBytes.InterleaveOffsets[l]);
					}
					if (data.CustomBytes.Instanced)
					{
						GL.VertexAttribDivisor(vaoSlotNumber, 1);
					}
					vaoSlotNumber++;
				}
			}
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			int vboIdIndex = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, vboIdIndex);
			GL.BufferData<int>(BufferTarget.ElementArrayBuffer, 4 * data.IndicesCount, data.Indices, data.IndicesStatic ? BufferUsageHint.StaticDraw : BufferUsageHint.DynamicDraw);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
			GL.BindVertexArray(0);
			this.CheckGlError("Something failed during mesh upload");
			return new VAO
			{
				VaoId = vaoId,
				IndicesCount = data.IndicesCount,
				vaoSlotNumber = vaoSlotNumber,
				vboIdIndex = vboIdIndex,
				normalsVboId = normalsVboId,
				xyzVboId = xyzVboId,
				uvVboId = uvVboId,
				rgbaVboId = rgbaVboId,
				customDataFloatVboId = customDataFloatVboId,
				customDataIntVboId = customDataIntVboId,
				customDataByteVboId = customDataByteVboId,
				customDataShortVboId = customDataShortVboId,
				flagsVboId = flagsVboId,
				drawMode = this.DrawModeToPrimiteType(data.mode)
			};
		}

		public override void DeleteMesh(MeshRef modelref)
		{
			if (modelref == null)
			{
				return;
			}
			((VAO)modelref).Dispose();
		}

		public override void UpdateSSBOMesh(MeshRef modelRef, MeshData data)
		{
			if (data.xyz == null)
			{
				return;
			}
			VAO vao = (VAO)modelRef;
			BufferAccessMask flags = BufferAccessMask.MapWriteBit | BufferAccessMask.MapUnsynchronizedBit;
			if (vao.Persistent)
			{
				flags |= BufferAccessMask.MapFlushExplicitBit | BufferAccessMask.MapPersistentBit;
			}
			bool pers = vao.Persistent;
			int count = data.VerticesCount;
			if (ClientPlatformWindows.facedataBuffer == null || ClientPlatformWindows.facedataBuffer.Length < count / 4)
			{
				ClientPlatformWindows.facedataBuffer = new FaceData[count / 4];
			}
			float[] src = data.xyz;
			float[] uv = data.Uv;
			int[] rflags = data.Flags;
			int[] colordataInts = ((data.CustomInts != null && data.CustomInts.Count > 0) ? data.CustomInts.Values : null);
			int colordataStride = ((data.CustomInts != null && data.CustomInts.Count > 0) ? (data.CustomInts.InterleaveStride / 4) : 1);
			FaceData[] facesBuffer = ClientPlatformWindows.facedataBuffer;
			for (int i = 0; i < count; i += 4)
			{
				float u0 = uv[i * 2];
				float v0 = uv[i * 2 + 1];
				float v = uv[i * 2 + 3];
				float u = uv[i * 2 + 4];
				float v2 = uv[i * 2 + 5];
				if (u0 < -1.5E-05f || u0 > 1.000015f || v0 < -1.5E-05f || v0 > 1.000015f)
				{
					u0 = 0f;
					v0 = 0f;
				}
				if (u < -1.5E-05f || u > 1.000015f || v2 < -1.5E-05f || v2 > 1.000015f)
				{
					u = 0f;
					v2 = 0f;
				}
				bool rotatedUv;
				if (rotatedUv = v0 == v)
				{
					float u2 = uv[i * 2 + 2];
					if (u != u2)
					{
						rotatedUv = false;
					}
				}
				facesBuffer[i / 4] = new FaceData(src, i * 3, u0, v0, u - u0, v2 - v0, rflags, i, (colordataInts != null) ? colordataInts[i * colordataStride] : 0, rotatedUv);
			}
			int offset = data.XyzOffset / 12 * 16;
			GL.BindBuffer(BufferTarget.ShaderStorageBuffer, vao.xyzVboId);
			GL.BufferSubData<FaceData>(BufferTarget.ShaderStorageBuffer, (IntPtr)offset, 16 * count, ClientPlatformWindows.facedataBuffer);
			GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
			if (data.Rgba != null && data.RgbaCount > 0)
			{
				this.updateVAO(data.Rgba, data.RgbaOffset, data.RgbaCount, vao.rgbaVboId, vao.rgbaPtr, pers);
			}
			if (data.CustomFloats != null && data.CustomFloats.Count > 0)
			{
				this.updateVAO(data.CustomFloats.Values, data.CustomFloats.BaseOffset, data.CustomFloats.Count, vao.customDataFloatVboId, vao.customDataFloatPtr, pers);
			}
			if (data.CustomShorts != null && data.CustomShorts.Count > 0)
			{
				this.updateVAO(data.CustomShorts.Values, data.CustomShorts.BaseOffset, data.CustomShorts.Count, vao.customDataShortVboId, vao.customDataShortPtr, pers);
			}
			if (data.CustomInts != null && data.CustomInts.Count > 0 && data.CustomInts.InterleaveStride > 4)
			{
				int[] customInts = data.CustomInts.Values;
				if (data.CustomInts.InterleaveStride / 4 != 2)
				{
					throw new Exception("We are assuming 2 customInts per vertex if it is not 1");
				}
				int ciCount = data.CustomInts.Count / 2;
				if (ClientPlatformWindows.customIntsPruned == null || ClientPlatformWindows.customIntsPruned.Length < ciCount)
				{
					ClientPlatformWindows.customIntsPruned = new int[ciCount];
				}
				int[] customIntsCopy = ClientPlatformWindows.customIntsPruned;
				for (int j = 0; j < ciCount; j++)
				{
					customIntsCopy[j] = customInts[j * 2 + 1];
				}
				this.updateVAO(ClientPlatformWindows.customIntsPruned, data.CustomInts.BaseOffset / 2, ciCount, vao.customDataIntVboId, vao.customDataIntPtr, pers);
			}
			if (data.CustomBytes != null && data.CustomBytes.Count > 0)
			{
				this.updateVAO(data.CustomBytes.Values, data.CustomBytes.BaseOffset, data.CustomBytes.Count, vao.customDataByteVboId, vao.customDataBytePtr, pers);
			}
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			vao.IndicesCount = data.IndicesCount;
			if (this.GlErrorChecking && this.GlDebugMode)
			{
				this.CheckGlError(string.Format("Error when trying to update vao indices, modeldata xyz/rgba/uv/indices sizes: {0}/{1}/{2}/{3}", new object[] { data.XyzCount, data.RgbaCount, data.UvCount, data.IndicesCount }));
			}
		}

		public override MeshRef AllocateEmptySSBOMesh(int xyzSize, int normalsSize, int uvSize, int rgbaSize, int flagsSize, int indicesSize, CustomMeshDataPartFloat customFloats, CustomMeshDataPartShort customShorts, CustomMeshDataPartByte customBytes, CustomMeshDataPartInt customInts, EnumDrawMode drawMode = EnumDrawMode.Triangles, bool staticDraw = true)
		{
			VAO vao = new VAO();
			int vaoId = GL.GenVertexArray();
			int vaoSlotNumber = 0;
			GL.BindVertexArray(vaoId);
			int xyzVboId = 0;
			int rgbaVboId = 0;
			BufferUsageHint usageHint = (staticDraw ? BufferUsageHint.StaticDraw : BufferUsageHint.DynamicDraw);
			BufferStorageFlags flags = (BufferStorageFlags)450;
			MapBufferAccessMask mapflags = MapBufferAccessMask.MapWriteBit | MapBufferAccessMask.MapPersistentBit | MapBufferAccessMask.MapCoherentBit;
			if (xyzSize > 0)
			{
				xyzVboId = GL.GenBuffer();
				GL.BindBuffer(BufferTarget.ShaderStorageBuffer, xyzVboId);
				GL.BufferStorage(BufferTarget.ShaderStorageBuffer, xyzSize / 12 * 16, IntPtr.Zero, BufferStorageFlags.DynamicStorageBit);
			}
			this.CheckGlError("Failed loading model");
			if (rgbaSize > 0)
			{
				rgbaVboId = this.GenArrayBuffer(rgbaSize, ref vao.rgbaPtr, false, flags, mapflags, usageHint);
				GL.VertexAttribPointer(vaoSlotNumber++, 4, VertexAttribPointerType.UnsignedByte, true, 0, 0);
			}
			vaoSlotNumber = this.AddCustoms(vao, vaoSlotNumber, customFloats, customShorts, customInts, customBytes, false, flags, mapflags, 1);
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			if (ClientPlatformAbstract.singleIndexBufferSize < indicesSize)
			{
				ClientPlatformAbstract.singleIndexBufferSize = indicesSize;
				if (ClientPlatformAbstract.singleIndexBufferSize != 0)
				{
					ClientPlatformAbstract.DisposeIndexBuffer();
				}
				ClientPlatformAbstract.singleIndexBufferId = GL.GenBuffer();
				GL.BindBuffer(BufferTarget.ElementArrayBuffer, ClientPlatformAbstract.singleIndexBufferId);
				int[] indicesStd = new int[(indicesSize + 23) / 24 * 6];
				for (int i = 0; i < indicesStd.Length; i += 6)
				{
					int vtx = i / 6 * 4;
					indicesStd[i] = vtx;
					indicesStd[i + 1] = vtx + 1;
					indicesStd[i + 2] = vtx + 2;
					indicesStd[i + 3] = vtx;
					indicesStd[i + 4] = vtx + 2;
					indicesStd[i + 5] = vtx + 3;
				}
				GL.BufferData<int>(BufferTarget.ElementArrayBuffer, indicesSize, indicesStd, BufferUsageHint.StaticDraw);
				GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
			}
			GL.BindVertexArray(0);
			this.CheckGlError("Failed loading model");
			vao.Persistent = false;
			vao.VaoId = vaoId;
			vao.IndicesCount = indicesSize;
			vao.vaoSlotNumber = vaoSlotNumber;
			vao.vboIdIndex = ClientPlatformAbstract.singleIndexBufferId;
			vao.normalsVboId = 0;
			vao.xyzVboId = xyzVboId;
			vao.uvVboId = 0;
			vao.rgbaVboId = rgbaVboId;
			vao.flagsVboId = 0;
			vao.drawMode = this.DrawModeToPrimiteType(drawMode);
			return vao;
		}

		private PrimitiveType DrawModeToPrimiteType(EnumDrawMode drawmode)
		{
			if (drawmode == EnumDrawMode.Lines)
			{
				return PrimitiveType.Lines;
			}
			if (drawmode == EnumDrawMode.LineStrip)
			{
				return PrimitiveType.LineStrip;
			}
			return PrimitiveType.Triangles;
		}

		public override string CurrentMouseCursor { get; protected set; }

		public override bool LoadMouseCursor(string cursorCoode, int hotx, int hoty, BitmapRef bmpRef)
		{
			try
			{
				SKBitmap bmp = ((BitmapExternal)bmpRef).bmp;
				if (bmp.Width > 32 || bmp.Height > 32)
				{
					return false;
				}
				float gUIScale = ClientSettings.GUIScale;
				if (gUIScale != 1f)
				{
					bmp = bmp.Resize(new SKImageInfo((int)((float)bmp.Width * gUIScale), (int)((float)bmp.Height * gUIScale)), new SKSamplingOptions(SKCubicResampler.Mitchell));
				}
				int i = 0;
				byte[] data = new byte[bmp.BytesPerPixel * bmp.Width * bmp.Height];
				for (int y = 0; y < bmp.Height; y++)
				{
					for (int x = 0; x < bmp.Width; x++)
					{
						SKColor color = bmp.GetPixel(x, y);
						data[i] = color.Red;
						data[i + 1] = color.Green;
						data[i + 2] = color.Blue;
						data[i + 3] = color.Alpha;
						i += 4;
					}
				}
				this.preLoadedCursors[cursorCoode] = new MouseCursor(hotx, hoty, bmp.Width, bmp.Height, data);
				bmp.Dispose();
			}
			catch (Exception ex)
			{
				this.Logger.Error("Failed loading mouse cursor {0}:", new object[] { cursorCoode });
				this.Logger.Error(ex);
				this.RestoreWindowCursor();
				return false;
			}
			return true;
		}

		public override void UseMouseCursor(string cursorCode, bool forceUpdate = false)
		{
			if ((cursorCode == null || cursorCode == this.CurrentMouseCursor) && !forceUpdate)
			{
				return;
			}
			try
			{
				this.window.Cursor = this.preLoadedCursors[cursorCode];
				this.CurrentMouseCursor = cursorCode;
			}
			catch
			{
				this.RestoreWindowCursor();
			}
		}

		public override void RestoreWindowCursor()
		{
			this.window.Cursor = MouseCursor.Default;
		}

		public override bool MouseGrabbed
		{
			get
			{
				return this.window.CursorState == CursorState.Grabbed;
			}
			set
			{
				CursorState newState = (value ? CursorState.Grabbed : CursorState.Normal);
				if (newState != this.window.CursorState && !RuntimeEnv.IsWaylandSession)
				{
					Vector2 newPos = new Vector2((float)this.window.ClientSize.X / 2f, (float)this.window.ClientSize.Y / 2f);
					this.SetMousePosition(newPos.X, newPos.Y);
					this.window.MousePosition = newPos;
				}
				this.window.CursorState = newState;
			}
		}

		private void UpdateMousePosition()
		{
			if (!this.window.IsFocused || this.window.MouseState.Position == this.previousMousePosition)
			{
				return;
			}
			float xdelta;
			float ydelta;
			if (this.previousCursorState != this.window.CursorState)
			{
				ydelta = (xdelta = 0f);
			}
			else
			{
				xdelta = this.window.MouseState.Position.X - this.previousMousePosition.X;
				ydelta = this.window.MouseState.Position.Y - this.previousMousePosition.Y;
			}
			foreach (MouseEventHandler mouseEventHandler in this.mouseEventHandlers)
			{
				MouseEvent args = new MouseEvent((int)this.mouseX, (int)this.mouseY, (int)xdelta, (int)ydelta);
				mouseEventHandler.OnMouseMove(args);
			}
			if (this.window.CursorState == CursorState.Grabbed)
			{
				this.ignoreMouseMoveEvent = true;
				this.SetMousePosition((float)this.window.ClientSize.X / 2f, (float)this.window.ClientSize.Y / 2f);
			}
			else if (this.ignoreMouseMoveEvent)
			{
				this.ignoreMouseMoveEvent = false;
			}
			this.previousMousePosition = this.window.MouseState.Position;
			this.previousCursorState = this.window.CursorState;
		}

		private void Mouse_Move(MouseMoveEventArgs e)
		{
			if (this.ignoreMouseMoveEvent)
			{
				return;
			}
			this.SetMousePosition(e.X, e.Y);
		}

		private void SetMousePosition(float x, float y)
		{
			this.mouseX = x;
			this.mouseY = y;
			if (RuntimeEnv.OS == OS.Mac)
			{
				this.mouseY += (float)ClientSettings.WeirdMacOSMouseYOffset;
			}
		}

		private void Mouse_WheelChanged(OpenTK.Windowing.Common.MouseWheelEventArgs e)
		{
			foreach (MouseEventHandler mouseEventHandler in this.mouseEventHandlers)
			{
				float delta = e.OffsetY * ClientSettings.MouseWheelSensivity;
				if (RuntimeEnv.OS == OS.Mac)
				{
					delta = GameMath.Clamp(delta, -1f, 1f);
				}
				this.prevWheelValue += delta;
				Vintagestory.API.Client.MouseWheelEventArgs e2 = new Vintagestory.API.Client.MouseWheelEventArgs
				{
					delta = (int)delta,
					deltaPrecise = (float)((int)delta),
					value = (int)this.prevWheelValue,
					valuePrecise = this.prevWheelValue
				};
				mouseEventHandler.OnMouseWheel(e2);
			}
		}

		private void Mouse_ButtonDown(MouseButtonEventArgs e)
		{
			EnumMouseButton enumMouseButton = MouseButtonConverter.ToEnumMouseButton(e.Button);
			foreach (MouseEventHandler mouseEventHandler in this.mouseEventHandlers)
			{
				MouseEvent args = new MouseEvent((int)this.mouseX, (int)this.mouseY, enumMouseButton, (int)e.Modifiers);
				mouseEventHandler.OnMouseDown(args);
			}
		}

		private void Mouse_ButtonUp(MouseButtonEventArgs e)
		{
			EnumMouseButton enumMouseButton = MouseButtonConverter.ToEnumMouseButton(e.Button);
			foreach (MouseEventHandler mouseEventHandler in this.mouseEventHandlers)
			{
				MouseEvent e2 = new MouseEvent((int)this.mouseX, (int)this.mouseY, enumMouseButton, (int)e.Modifiers);
				mouseEventHandler.OnMouseUp(e2);
			}
		}

		private void game_KeyPress(TextInputEventArgs e)
		{
			foreach (KeyEventHandler keyEventHandler in this.keyEventHandlers)
			{
				keyEventHandler.OnKeyPress(new KeyEvent
				{
					KeyCode = e.Unicode,
					KeyChar = (char)e.Unicode
				});
			}
		}

		private void game_KeyDown(KeyboardKeyEventArgs e)
		{
			if (e.Key == Keys.Unknown)
			{
				return;
			}
			int key = KeyConverter.NewKeysToGlKeys[(int)e.Key];
			foreach (KeyEventHandler keyEventHandler in this.keyEventHandlers)
			{
				KeyEvent args = new KeyEvent
				{
					KeyCode = key
				};
				if (this.EllapsedMs - this.lastKeyUpMs <= 200L)
				{
					args.KeyCode2 = new int?(this.lastKeyUpKey);
				}
				args.CommandPressed = e.Command;
				args.CtrlPressed = e.Control;
				args.ShiftPressed = e.Shift;
				args.AltPressed = e.Alt;
				keyEventHandler.OnKeyDown(args);
			}
		}

		private void game_KeyUp(KeyboardKeyEventArgs e)
		{
			if (e.Key == Keys.Unknown)
			{
				return;
			}
			int key = KeyConverter.NewKeysToGlKeys[(int)e.Key];
			this.lastKeyUpMs = this.EllapsedMs;
			this.lastKeyUpKey = key;
			foreach (KeyEventHandler keyEventHandler in this.keyEventHandlers)
			{
				KeyEvent args = new KeyEvent
				{
					KeyCode = key
				};
				args.CommandPressed = e.Command;
				args.CtrlPressed = e.Control;
				args.ShiftPressed = e.Shift;
				args.AltPressed = e.Alt;
				keyEventHandler.OnKeyUp(args);
			}
		}

		public override MouseEvent CreateMouseEvent(EnumMouseButton button)
		{
			return new MouseEvent((int)this.mouseX, (int)this.mouseY, button, 0);
		}

		public override DefaultShaderUniforms ShaderUniforms { get; set; } = new DefaultShaderUniforms();

		public override ShaderProgramMinimalGui MinimalGuiShader
		{
			get
			{
				return this.minimalGuiShaderProgram;
			}
		}

		public override int GetUniformLocation(ShaderProgram program, string name)
		{
			return GL.GetUniformLocation(program.ProgramId, name);
		}

		public override bool CompileShader(Shader shader)
		{
			int shaderId = GL.CreateShader((ShaderType)shader.shaderType);
			shader.ShaderId = shaderId;
			string shaderCode = shader.Code;
			if (shaderCode != null)
			{
				if (shaderCode.IndexOfOrdinal("#version") == -1)
				{
					this.logger.Warning("Shader {0}: Is not defining a shader version via #version", new object[] { shader.Filename });
				}
				if (RuntimeEnv.OS == OS.Mac)
				{
					shaderCode = Regex.Replace(shaderCode, "#version \\d+", "#version 330");
				}
				else if (ScreenManager.Platform.UseSSBOs && shader.UsesSSBOs())
				{
					shaderCode = Regex.Replace(shaderCode, "#version \\d+", "#version 430");
				}
				int startIndex = shaderCode.IndexOf('\n', Math.Max(0, shaderCode.IndexOfOrdinal("#version"))) + 1;
				shaderCode = shaderCode.Insert(startIndex, shader.PrefixCode);
			}
			GL.ShaderSource(shaderId, shaderCode);
			GL.CompileShader(shaderId);
			int outval;
			GL.GetShader(shaderId, ShaderParameter.CompileStatus, out outval);
			if (outval != 1)
			{
				string logText = GL.GetShaderInfoLog(shaderId);
				this.logger.Error("Shader compile error in {0} {1}", new object[]
				{
					shader.Filename,
					logText.TrimEnd()
				});
				this.logger.VerboseDebug("{0}", new object[] { shaderCode });
				return false;
			}
			return true;
		}

		public override bool CreateShaderProgram(ShaderProgram program)
		{
			bool ok = true;
			int programId = GL.CreateProgram();
			program.ProgramId = programId;
			GL.AttachShader(programId, program.VertexShader.ShaderId);
			GL.AttachShader(programId, program.FragmentShader.ShaderId);
			if (program.GeometryShader != null)
			{
				GL.AttachShader(programId, program.GeometryShader.ShaderId);
			}
			foreach (KeyValuePair<int, string> val in program.attributes)
			{
				GL.BindAttribLocation(program.ProgramId, val.Key, val.Value);
			}
			GL.LinkProgram(programId);
			int outval;
			GL.GetProgram(programId, GetProgramParameterName.LinkStatus, out outval);
			string logText = GL.GetProgramInfoLog(programId);
			if (outval != 1)
			{
				this.logger.Error("Link error in shader program for pass {0}: {1}", new object[]
				{
					program.PassName,
					logText.TrimEnd()
				});
				ok = false;
			}
			else
			{
				this.logger.Notification("Loaded Shaderprogramm for render pass {0}.", new object[] { program.PassName });
			}
			return ok;
		}

		private AudioOpenAl audio;

		public GameExit gameexit;

		public bool SupportsThickLines;

		private int cpuCoreCount = 2;

		private AssetManager assetManager;

		private Stopwatch frameStopWatch;

		internal Stopwatch uptimeStopWatch = new Stopwatch();

		private Logger logger;

		private int doResize;

		private static DebugProc _debugProcCallback;

		private static GCHandle _debugProcCallbackHandle;

		public GameWindowNative window;

		private Screenshot screenshot = new Screenshot();

		public Action<StartServerArgs> OnStartSinglePlayerServer;

		public GameExit ServerExit = new GameExit();

		public bool singlePlayerServerLoaded;

		public DummyNetwork[] singlePlayerServerDummyNetwork;

		private Size2i windowsize = new Size2i();

		private Size2i screensize = new Size2i();

		private List<ClientPlatformAbstract.OnFocusChanged> focusChangedDelegates = new List<ClientPlatformAbstract.OnFocusChanged>();

		private Action windowClosedHandler;

		private NewFrameHandler frameHandler;

		public CrashReporter crashreporter;

		private OnCrashHandler onCrashHandler;

		public List<KeyEventHandler> keyEventHandlers = new List<KeyEventHandler>();

		public List<MouseEventHandler> mouseEventHandlers = new List<MouseEventHandler>();

		public Action<string> fileDropEventHandler;

		private bool debugDrawCalls;

		private List<string> drawCallStacks = new List<string>();

		private List<FrameBufferRef> frameBuffers;

		private MeshRef screenQuad;

		private bool serverRunning;

		private bool gamepause;

		private bool OffscreenBuffer = true;

		private bool RenderBloom;

		private bool RenderGodRays;

		private bool RenderFXAA;

		private bool RenderSSAO;

		private bool SetupSSAO;

		private int ShadowMapQuality;

		private float ssaaLevel;

		public ClientPlatformWindows.GLBuffer[] PixelPackBuffer;

		public int sampleCount = 32;

		public int CurrentPixelPackBufferNum;

		private Random rand = new Random();

		private float[] ssaoKernel = new float[192];

		private FrameBufferRef curFb;

		private float[] clearColor = new float[] { 0f, 0f, 0f, 1f };

		private bool glDebugMode;

		private bool supportsGlDebugMode;

		private bool supportsPersistentMapping;

		public bool ENABLE_MIPMAPS = true;

		public bool ENABLE_ANISOTROPICFILTERING;

		public bool ENABLE_TRANSPARENCY = true;

		[ThreadStatic]
		private static FaceData[] facedataBuffer;

		[ThreadStatic]
		private static int[] customIntsPruned;

		private const float minUV = -1.5E-05f;

		private const float maxUV = 1.000015f;

		private Vector2 previousMousePosition;

		private CursorState previousCursorState;

		private float mouseX;

		private float mouseY;

		private Dictionary<string, MouseCursor> preLoadedCursors = new Dictionary<string, MouseCursor>();

		private bool ignoreMouseMoveEvent;

		private float prevWheelValue;

		private long lastKeyUpMs;

		private int lastKeyUpKey;

		private ShaderProgramMinimalGui minimalGuiShaderProgram;

		public class GLBuffer
		{
			public int BufferId;
		}
	}
}
