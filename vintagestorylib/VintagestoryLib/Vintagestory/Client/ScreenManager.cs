using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cairo;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Client.Gui;
using Vintagestory.Client.MaxObf;
using Vintagestory.Client.NoObf;
using Vintagestory.ClientNative;
using Vintagestory.Common;
using Vintagestory.ModDb;

namespace Vintagestory.Client
{
	public class ScreenManager : KeyEventHandler, MouseEventHandler, NewFrameHandler
	{
		public ClientPlatformAbstract GamePlatform
		{
			get
			{
				return ScreenManager.Platform;
			}
		}

		public List<IInventory> OpenedInventories
		{
			get
			{
				return null;
			}
		}

		public IWorldAccessor World
		{
			get
			{
				return null;
			}
		}

		public ScreenManager(ClientPlatformAbstract platform)
		{
			ScreenManager.Platform = platform;
			this.textures = new Dictionary<string, int>();
			this.sessionManager = new SessionManager();
			ScreenManager.hotkeyManager = new HotkeyManager();
			ScreenManager.FrameProfiler = new FrameProfilerUtil(delegate(string text)
			{
				platform.Logger.Notification(text);
			});
		}

		public void Start(ClientProgramArgs args, string[] rawArgs)
		{
			this.api = new MainMenuAPI(this);
			ScreenManager.GuiComposers = new GuiComposerManager(this.api);
			ScreenManager.ParsedArgs = args;
			ScreenManager.RawCmdLineArgs = rawArgs;
			ScreenManager.MainThreadId = Environment.CurrentManagedThreadId;
			RuntimeEnv.MainThreadId = ScreenManager.MainThreadId;
			ScreenManager.Platform.SetTitle("Vintage Story");
			this.mainScreen = new GuiScreenMainRight(this, null);
			ScreenManager.Platform.SetWindowClosedHandler(new Action(this.onWindowClosed));
			ScreenManager.Platform.SetFrameHandler(this);
			ScreenManager.Platform.RegisterKeyboardEvent(this);
			ScreenManager.Platform.RegisterMouseEvent(this);
			ScreenManager.Platform.RegisterOnFocusChange(new ClientPlatformAbstract.OnFocusChanged(this.onFocusChanged));
			ScreenManager.Platform.SetFileDropHandler(new Action<string>(this.onFileDrop));
			this.LoadAndCacheScreen(typeof(GuiScreenLoadingGame));
			this.versionNumberTexture = this.api.Gui.TextTexture.GenUnscaledTextTexture(GameVersion.LongGameVersion, CairoFont.WhiteDetailText(), null);
			Thread thread = new Thread(new ThreadStart(this.DoGameInitStage1));
			thread.Start();
			thread.IsBackground = true;
			this.registerSettingsWatchers();
			ScreenManager.Platform.GlDebugMode = ClientSettings.GlDebugMode;
			ScreenManager.Platform.GlErrorChecking = ClientSettings.GlErrorChecking;
			ScreenManager.Platform.GlToggleBlend(true, EnumBlendMode.Standard);
			TyronThreadPool.QueueLongDurationTask(delegate
			{
				for (;;)
				{
					string texDebugDispose = Environment.GetEnvironmentVariable("TEXTURE_DEBUG_DISPOSE");
					string cairoDebugDispose = Environment.GetEnvironmentVariable("CAIRO_DEBUG_DISPOSE");
					string vaoDebugDispose = Environment.GetEnvironmentVariable("VAO_DEBUG_DISPOSE");
					if (!string.IsNullOrEmpty(texDebugDispose))
					{
						RuntimeEnv.DebugTextureDispose = texDebugDispose == "1";
					}
					if (!string.IsNullOrEmpty(cairoDebugDispose))
					{
						CairoDebug.Enabled = cairoDebugDispose == "1";
					}
					if (!string.IsNullOrEmpty(vaoDebugDispose))
					{
						RuntimeEnv.DebugVAODispose = vaoDebugDispose == "1";
					}
					Thread.Sleep(1000);
				}
			});
		}

		public void registerSettingsWatchers()
		{
			ClientSettings.Inst.AddWatcher<int>("masterSoundLevel", new OnSettingsChanged<int>(this.OnMasterSoundLevelChanged));
			ClientSettings.Inst.AddWatcher<int>("musicLevel", new OnSettingsChanged<int>(this.OnMusicLevelChanged));
			ClientSettings.Inst.AddWatcher<int>("windowBorder", new OnSettingsChanged<int>(this.OnWindowBorderChanged));
			ClientSettings.Inst.AddWatcher<int>("gameWindowMode", new OnSettingsChanged<int>(this.OnWindowModeChanged));
			ClientSettings.Inst.AddWatcher<bool>("glDebugMode", delegate(bool val)
			{
				ScreenManager.Platform.GlDebugMode = val;
			});
			ClientSettings.Inst.AddWatcher<bool>("glErrorChecking", delegate(bool val)
			{
				ScreenManager.Platform.GlErrorChecking = val;
			});
			ClientSettings.Inst.AddWatcher<float>("guiScale", delegate(float val)
			{
				RuntimeEnv.GUIScale = val;
				ScreenManager.GuiComposers.MarkAllDialogsForRecompose();
				this.loadCursors();
			});
			ScreenManager.Platform.AddAudioSettingsWatchers();
			ScreenManager.Platform.MasterSoundLevel = (float)ClientSettings.MasterSoundLevel / 100f;
		}

		private void OnWindowModeChanged(int newvalue)
		{
			GuiComposer elementComposer = this.CurrentScreen.ElementComposer;
			if (elementComposer == null)
			{
				return;
			}
			GuiElementDropDown dropDown = elementComposer.GetDropDown("windowModeSwitch");
			if (dropDown == null)
			{
				return;
			}
			dropDown.SetSelectedIndex(GuiCompositeSettings.GetWindowModeIndex());
		}

		private void OnWindowBorderChanged(int newValue)
		{
			ScreenManager.Platform.WindowBorder = (EnumWindowBorder)newValue;
			GuiComposer elementComposer = this.CurrentScreen.ElementComposer;
			if (elementComposer == null)
			{
				return;
			}
			GuiElementDropDown dropDown = elementComposer.GetDropDown("windowBorder");
			if (dropDown == null)
			{
				return;
			}
			dropDown.SetSelectedIndex(ClientSettings.WindowBorder);
		}

		public void DoGameInitStage1()
		{
			this.sessionManager.GetNewestVersion(new Action<string>(this.OnReceivedNewestVersion));
			this.loadingText = Lang.Get("Loading assets", Array.Empty<object>());
			ScreenManager.Platform.LoadAssets();
			this.loadingText = Lang.Get("Loading sounds", Array.Empty<object>());
			ScreenManager.Platform.Logger.Notification("Loading sounds");
			ScreenManager.LoadSoundsInitial();
			ScreenManager.Platform.Logger.Notification("Sounds loaded");
			this.loadingText = null;
		}

		public void DoGameInitStage2()
		{
			ShaderRegistry.Load();
			if (!this.cursorsLoaded)
			{
				this.loadCursors();
				this.cursorsLoaded = true;
			}
			TyronThreadPool.QueueTask(delegate
			{
				AssetLocation trackLocation = new AssetLocation("music/roots.ogg");
				AudioData audiodata = ScreenManager.LoadMusicTrack(ScreenManager.Platform.AssetManager.TryGet_BaseAssets(trackLocation, true));
				if (audiodata != null)
				{
					Random rand = new Random();
					ScreenManager.IntroMusic = ScreenManager.Platform.CreateAudio(new SoundParams
					{
						SoundType = EnumSoundType.Music,
						Location = trackLocation
					}, audiodata);
					while (!ScreenManager.Platform.IsShuttingDown && !ScreenManager.introMusicShouldStop)
					{
						ScreenManager.IntroMusic.Start();
						while (!ScreenManager.IntroMusic.HasStopped)
						{
							Thread.Sleep(100);
							if (ScreenManager.Platform.IsShuttingDown)
							{
								break;
							}
						}
						if (!ScreenManager.Platform.IsShuttingDown)
						{
							Thread.Sleep((300 + rand.Next(600)) * 1000);
						}
					}
				}
			});
			ScreenManager.hotkeyManager.RegisterDefaultHotKeys();
			this.setupHotkeyHandlers();
			if (!ClientSettings.DisableModSafetyCheck)
			{
				ModDbUtil.GetBlockedModsAsync(ScreenManager.Platform.Logger);
			}
			if (!this.sessionManager.IsCachedSessionKeyValid())
			{
				ScreenManager.Platform.Logger.Notification("Cached session key is invalid, require login");
				ScreenManager.Platform.ToggleOffscreenBuffer(true);
				this.LoadAndCacheScreen(typeof(GuiScreenLogin));
				return;
			}
			ScreenManager.Platform.Logger.Notification("Cached session key is valid, validating with server");
			this.loadingText = "Validating session with server";
			this.sessionManager.ValidateSessionKeyWithServer(new Action<EnumAuthServerResponse>(this.OnValidationDone));
			TyronThreadPool.QueueTask(delegate
			{
				int i = 1;
				Thread.Sleep(1000);
				while (this.awaitValidation)
				{
					this.loadingText = "Validating session with server\n" + i.ToString();
					i++;
					Thread.Sleep(1000);
				}
			});
		}

		private void setupHotkeyHandlers()
		{
			ScreenManager.hotkeyManager.SetHotKeyHandler("recomposeallguis", delegate(KeyCombination viaKeyComb)
			{
				ScreenManager.GuiComposers.RecomposeAllDialogs();
				return true;
			}, false);
			ScreenManager.hotkeyManager.SetHotKeyHandler("reloadworld", delegate(KeyCombination viaKeyComb)
			{
				this.CurrentScreen.ReloadWorld("Reload world hotkey triggered");
				return true;
			}, false);
			ScreenManager.hotkeyManager.SetHotKeyHandler("cycledialogoutlines", delegate(KeyCombination viaKeyComb)
			{
				GuiComposer.Outlines = (GuiComposer.Outlines + 1) % 3;
				return true;
			}, false);
			ScreenManager.hotkeyManager.SetHotKeyHandler("togglefullscreen", delegate(KeyCombination viaKeyComb)
			{
				GuiCompositeSettings.SetWindowMode((ScreenManager.Platform.GetWindowState() != WindowState.Fullscreen) ? 1 : 0);
				return true;
			}, false);
		}

		private void loadCursors()
		{
			this.LoadCursor("textselect");
			this.LoadCursor("linkselect");
			this.LoadCursor("move");
			this.LoadCursor("busy");
			this.LoadCursor("normal");
			ScreenManager.Platform.UseMouseCursor("normal", true);
		}

		private void LoadCursor(string code)
		{
			if (RuntimeEnv.OS == OS.Mac)
			{
				return;
			}
			Dictionary<string, Vec2i> coords = ScreenManager.Platform.AssetManager.Get("textures/gui/cursors/coords.json").ToObject<Dictionary<string, Vec2i>>(null);
			IAsset asset = ScreenManager.Platform.AssetManager.TryGet_BaseAssets("textures/gui/cursors/" + code + ".png", true);
			BitmapRef bmp = ((asset != null) ? asset.ToBitmap(this.api) : null);
			if (bmp != null)
			{
				ScreenManager.Platform.LoadMouseCursor(code, coords[code].X, coords[code].Y, bmp);
			}
		}

		private void OnReceivedNewestVersion(string newestversion)
		{
			this.newestVersion = newestversion;
		}

		private void OnValidationDone(EnumAuthServerResponse response)
		{
			this.validationResponse = new EnumAuthServerResponse?(response);
			this.awaitValidation = false;
		}

		private void DoGameInitStage3()
		{
			ScreenManager.Platform.ToggleOffscreenBuffer(true);
			ILogger logger = ScreenManager.Platform.Logger;
			string text = "Server validation response: ";
			EnumAuthServerResponse? enumAuthServerResponse = this.validationResponse;
			logger.Notification(text + enumAuthServerResponse.ToString());
			this.ClientIsOffline = false;
			enumAuthServerResponse = this.validationResponse;
			EnumAuthServerResponse enumAuthServerResponse2 = EnumAuthServerResponse.Good;
			if ((enumAuthServerResponse.GetValueOrDefault() == enumAuthServerResponse2) & (enumAuthServerResponse != null))
			{
				this.DoGameInitStage4();
				return;
			}
			if (this.validationResponse.GetValueOrDefault() == EnumAuthServerResponse.Bad)
			{
				this.LoadAndCacheScreen(typeof(GuiScreenLogin));
				this.validationResponse = null;
				return;
			}
			this.ClientIsOffline = true;
			this.DoGameInitStage4();
		}

		internal void DoGameInitStage4()
		{
			this.validationResponse = null;
			this.loadingText = "Loading mod meta infos";
			TyronThreadPool.QueueTask(delegate
			{
				this.loadMods();
				ScreenManager.EnqueueMainThreadTask(new Action(this.DoGameInitStage5));
			});
		}

		private void DoGameInitStage5()
		{
			this.StartMainMenu();
			this.HandleArgs();
		}

		public static bool AsyncSoundLoadComplete { get; set; }

		public static void CatalogSounds(Action onCompleted)
		{
			ScreenManager.AsyncSoundLoadComplete = false;
			ScreenManager.soundAudioData.Clear();
			new LoadSoundsThread(ScreenManager.Platform.Logger, null, onCompleted).Process();
		}

		public static void LoadSoundsSlow_Async(ClientMain game)
		{
			new Thread(new ThreadStart(new LoadSoundsThread(ScreenManager.Platform.Logger, game, null).ProcessSlow))
			{
				IsBackground = true,
				Priority = ThreadPriority.BelowNormal,
				Name = "LoadSounds async"
			}.Start();
		}

		public static void LoadSoundsInitial()
		{
			List<IAsset> soundAssets = ScreenManager.Platform.AssetManager.GetMany(AssetCategory.sounds, false);
			ScreenManager.Platform.Logger.VerboseDebug("Loadsounds, found " + soundAssets.Count.ToString() + " sounds");
			List<AudioData> menuSounds = new List<AudioData>();
			foreach (IAsset soundAsset in soundAssets)
			{
				ScreenManager.LoadSound(soundAsset);
				if (soundAsset.Location.PathStartsWith("sounds/menubutton"))
				{
					menuSounds.Add(new AudioMetaData(soundAsset)
					{
						Loaded = 0
					});
				}
			}
			foreach (AudioData audioData in menuSounds)
			{
				audioData.Load();
				Thread.Sleep(1);
			}
		}

		public static void LoadSoundsSlow(ClientMain game)
		{
			bool debug = ClientSettings.ExtendedDebugInfo;
			foreach (KeyValuePair<AssetLocation, AudioData> soundEntry in ScreenManager.soundAudioDataAsyncLoadTemp)
			{
				if (game.disposed)
				{
					return;
				}
				if (soundEntry.Key.BeginsWith("game", "sounds/weather/") || soundEntry.Key.BeginsWith("game", "sounds/environment") || soundEntry.Key.BeginsWith("game", "sounds/effect/tempstab") || soundEntry.Key.BeginsWith("game", "sounds/effect/rift"))
				{
					if (debug)
					{
						ScreenManager.Platform.Logger.VerboseDebug("Load sound asset " + soundEntry.Key.ToShortString());
					}
					soundEntry.Value.Load();
					Thread.Sleep(15);
					if (game.disposed)
					{
						return;
					}
				}
			}
			ScreenManager.AsyncSoundLoadComplete = true;
			ScreenManager.Platform.Logger.VerboseDebug("Loaded highest priority sound assets");
			if (ClientSettings.OptimizeRamMode < 2)
			{
				if (game.disposed)
				{
					return;
				}
				foreach (KeyValuePair<AssetLocation, AudioData> soundEntry2 in ScreenManager.soundAudioDataAsyncLoadTemp)
				{
					if (debug && soundEntry2.Value.Loaded == 0)
					{
						ScreenManager.Platform.Logger.VerboseDebug("Load sound asset " + soundEntry2.Key);
					}
					soundEntry2.Value.Load();
					Thread.Sleep(20);
					if (game.disposed)
					{
						return;
					}
				}
			}
			ScreenManager.soundAudioDataAsyncLoadTemp.Clear();
		}

		public static AudioData LoadMusicTrack(IAsset asset)
		{
			if (asset != null && !asset.IsLoaded())
			{
				asset.Origin.LoadAsset(asset);
			}
			if (asset == null || asset.Data == null)
			{
				return null;
			}
			AudioData track;
			if (!ScreenManager.soundAudioData.TryGetValue(asset.Location, out track))
			{
				ScreenManager.soundAudioData[asset.Location] = ScreenManager.Platform.CreateAudioData(asset);
				asset.Data = null;
			}
			else
			{
				track.Load();
			}
			return ScreenManager.soundAudioData[asset.Location];
		}

		public static AudioData LoadSound(IAsset asset)
		{
			if (asset == null)
			{
				return null;
			}
			Dictionary<AssetLocation, AudioData> dictionary = ScreenManager.soundAudioData;
			AssetLocation location = asset.Location;
			AudioMetaData audioMetaData = new AudioMetaData(asset);
			audioMetaData.Loaded = 0;
			AudioData audioData = audioMetaData;
			dictionary[location] = audioMetaData;
			return audioData;
		}

		public void loadMods()
		{
			List<string> modSearchPaths = new List<string>(ClientSettings.ModPaths);
			if (ScreenManager.ParsedArgs.AddModPath != null)
			{
				modSearchPaths.AddRange(ScreenManager.ParsedArgs.AddModPath);
			}
			this.modloader = new ModLoader(this.GamePlatform.Logger, EnumAppSide.Client, modSearchPaths, ScreenManager.ParsedArgs.TraceLog);
			this.allMods.Clear();
			this.allMods.AddRange(this.modloader.LoadModInfos());
			this.verifiedMods = this.modloader.DisableAndVerify(new List<ModContainer>(this.allMods), ClientSettings.DisabledMods);
			CrashReporter.LoadedMods = this.verifiedMods.Where((ModContainer mod) => mod.Enabled).ToList<ModContainer>();
		}

		private void HandleArgs()
		{
			if (ScreenManager.ParsedArgs.AddOrigin != null)
			{
				foreach (string text in ScreenManager.ParsedArgs.AddOrigin)
				{
					string[] domainPaths = Directory.GetDirectories(text);
					for (int i = 0; i < domainPaths.Length; i++)
					{
						string domain = new DirectoryInfo(domainPaths[i]).Name;
						ScreenManager.Platform.AssetManager.CustomAppOrigins.Add(new PathOrigin(domain, domainPaths[i], "textures"));
					}
				}
			}
			if (ScreenManager.ParsedArgs.ConnectServerAddress != null)
			{
				this.ConnectToMultiplayer(ScreenManager.ParsedArgs.ConnectServerAddress, ScreenManager.ParsedArgs.Password);
				return;
			}
			if (ScreenManager.ParsedArgs.InstallModId != null)
			{
				ScreenManager.EnqueueMainThreadTask(delegate
				{
					this.GamePlatform.XPlatInterface.FocusWindow();
					this.InstallMod(ScreenManager.ParsedArgs.InstallModId);
				});
				return;
			}
			if (ScreenManager.ParsedArgs.OpenWorldName != null || ScreenManager.ParsedArgs.CreateRndWorld)
			{
				ScreenManager.EnqueueMainThreadTask(new Action(this.openWorldFromArgs));
				return;
			}
		}

		private void openWorldFromArgs()
		{
			string playstyle = ScreenManager.ParsedArgs.PlayStyle;
			string worldname = ScreenManager.ParsedArgs.OpenWorldName;
			if (worldname == null)
			{
				int i = 0;
				while (File.Exists(string.Concat(new string[]
				{
					GamePaths.Saves,
					Path.DirectorySeparatorChar.ToString(),
					"world",
					i.ToString(),
					".vcdbs"
				})))
				{
					i++;
				}
				worldname = "world" + i.ToString() + ".vcdbs";
			}
			foreach (ModContainer mod in this.verifiedMods)
			{
				ModWorldConfiguration worldConfig = mod.WorldConfig;
				if (((worldConfig != null) ? worldConfig.PlayStyles : null) != null)
				{
					foreach (PlayStyle modplaystyle in mod.WorldConfig.PlayStyles)
					{
						if (modplaystyle.LangCode == playstyle)
						{
							StartServerArgs startServerArgs = new StartServerArgs();
							ReadOnlySpan<char> readOnlySpan = GamePaths.Saves;
							char directorySeparatorChar = Path.DirectorySeparatorChar;
							startServerArgs.SaveFileLocation = readOnlySpan + new ReadOnlySpan<char>(ref directorySeparatorChar) + worldname + ".vcdbs";
							startServerArgs.WorldName = worldname;
							startServerArgs.PlayStyle = modplaystyle.Code;
							startServerArgs.PlayStyleLangCode = modplaystyle.LangCode;
							startServerArgs.WorldType = modplaystyle.WorldType;
							startServerArgs.WorldConfiguration = modplaystyle.WorldConfig;
							startServerArgs.AllowCreativeMode = true;
							startServerArgs.DisabledMods = ClientSettings.DisabledMods;
							startServerArgs.Language = ClientSettings.Language;
							this.ConnectToSingleplayer(startServerArgs);
							break;
						}
					}
				}
			}
		}

		public void OnNewFrame(float dt)
		{
			if (ScreenManager.debugDrawCallNextFrame)
			{
				ScreenManager.Platform.DebugDrawCalls = true;
			}
			if (this.validationResponse != null)
			{
				this.DoGameInitStage3();
			}
			if (this.CurrentScreen is GuiScreenRunningGame)
			{
				ScreenManager.Platform.MaxFps = (float)ClientSettings.MaxFPS;
			}
			else
			{
				ScreenManager.Platform.MaxFps = 60f;
			}
			if ((long)Environment.TickCount - this.lastSaveCheck > 2000L)
			{
				ClientSettings.Inst.Save(false);
				this.lastSaveCheck = (long)Environment.TickCount;
				if (this.guiMainmenuLeft != null && this.newestVersion != null)
				{
					if (GameVersion.IsNewerVersionThan(this.newestVersion, "1.21.5"))
					{
						this.guiMainmenuLeft.SetHasNewVersion(this.newestVersion);
					}
					this.newestVersion = null;
				}
			}
			int quantity = ScreenManager.MainThreadTasks.Count;
			while (quantity-- > 0)
			{
				Queue<Action> mainThreadTasks = ScreenManager.MainThreadTasks;
				lock (mainThreadTasks)
				{
					ScreenManager.MainThreadTasks.Dequeue()();
				}
			}
			this.Render(dt);
			ScreenManager.FrameProfiler.Mark("rendered");
			if (ScreenManager.debugDrawCallNextFrame)
			{
				ScreenManager.Platform.DebugDrawCalls = false;
				ScreenManager.debugDrawCallNextFrame = false;
			}
		}

		public static void EnqueueMainThreadTask(Action a)
		{
			Queue<Action> mainThreadTasks = ScreenManager.MainThreadTasks;
			lock (mainThreadTasks)
			{
				ScreenManager.MainThreadTasks.Enqueue(a);
			}
		}

		public static void EnqueueCallBack(Action a, int msdelay)
		{
			TyronThreadPool.QueueTask(delegate
			{
				Thread.Sleep(msdelay);
				ScreenManager.EnqueueMainThreadTask(a);
			});
		}

		internal void Render(float dt)
		{
			int width = ScreenManager.Platform.WindowSize.Width;
			int height = ScreenManager.Platform.WindowSize.Height;
			ScreenManager.Platform.GlViewport(0, 0, width, height);
			ScreenManager.Platform.ClearFrameBuffer(EnumFrameBuffer.Default);
			ScreenManager.Platform.ClearFrameBuffer(EnumFrameBuffer.Primary);
			ScreenManager.Platform.GlDisableDepthTest();
			ScreenManager.Platform.GlDisableCullFace();
			ScreenManager.Platform.CheckGlError(null);
			ScreenManager.Platform.DoPostProcessingEffects = false;
			ScreenManager.Platform.LoadFrameBuffer(EnumFrameBuffer.Primary);
			this.CurrentScreen.RenderToPrimary(dt);
			ScreenManager.FrameProfiler.Mark("doneRend");
			float[] projMat = null;
			GuiScreenRunningGame cs = this.CurrentScreen as GuiScreenRunningGame;
			if (cs != null)
			{
				projMat = cs.runningGame.CurrentProjectionMatrix;
			}
			ScreenManager.Platform.RenderPostprocessingEffects(projMat);
			this.CurrentScreen.RenderAfterPostProcessing(dt);
			ScreenManager.Platform.RenderFinalComposition();
			this.CurrentScreen.RenderAfterFinalComposition(dt);
			ScreenManager.Platform.BlitPrimaryToDefault();
			ScreenManager.Platform.CheckGlError(null);
			ScreenManager.FrameProfiler.Mark("doneRender2Default");
			Mat4f.Identity(this.api.renderapi.pMatrix);
			Mat4f.Ortho(this.api.renderapi.pMatrix, 0f, (float)ScreenManager.Platform.WindowSize.Width, (float)ScreenManager.Platform.WindowSize.Height, 0f, 0f, 20001f);
			ScreenManager.Platform.GlDepthFunc(EnumDepthFunction.Lequal);
			float clearval = 20000f;
			GL.ClearBuffer(ClearBuffer.Depth, 0, ref clearval);
			GL.DepthRange(0f, 20000f);
			ScreenManager.Platform.GlEnableDepthTest();
			ScreenManager.Platform.GlDisableCullFace();
			ScreenManager.Platform.GlToggleBlend(true, EnumBlendMode.Standard);
			this.CurrentScreen.RenderAfterBlit(dt);
			if (this.CurrentScreen.RenderBg && !ScreenManager.Platform.IsShuttingDown)
			{
				GuiCompositeMainMenuLeft guiCompositeMainMenuLeft = this.guiMainmenuLeft;
				if (guiCompositeMainMenuLeft != null)
				{
					guiCompositeMainMenuLeft.RenderBg(dt, this.withMainMenu);
				}
				this.withMainMenu = false;
			}
			this.CurrentScreen.RenderToDefaultFramebuffer(dt);
			ScreenManager.Platform.GlDepthFunc(EnumDepthFunction.Less);
			ScreenManager.FrameProfiler.Mark("doneAfterRender");
			ScreenManager.Platform.CheckGlError(null);
		}

		internal void RenderMainMenuParts(float dt, ElementBounds bounds, bool withMainMenu, bool darkenEdges = true)
		{
			this.withMainMenu = withMainMenu;
			if (withMainMenu)
			{
				ScreenManager.Platform.GlToggleBlend(true, EnumBlendMode.Standard);
				this.mainMenuComposer.Render(dt);
				this.mainMenuComposer.PostRender(dt);
			}
			float windowSizeX = (float)ScreenManager.Platform.WindowSize.Width;
			float windowSizeY = (float)ScreenManager.Platform.WindowSize.Height;
			this.api.Render.Render2DTexturePremultipliedAlpha(this.versionNumberTexture.TextureId, (double)(windowSizeX - (float)this.versionNumberTexture.Width - 10f), (double)(windowSizeY - (float)this.versionNumberTexture.Height) - GuiElement.scaled(5.0), (double)this.versionNumberTexture.Width, (double)this.versionNumberTexture.Height, 50f, null);
		}

		public IInventory GetOwnInventory(string className)
		{
			throw new NotImplementedException();
		}

		public void SendPacketClient(Packet_Client packetClient)
		{
			throw new NotImplementedException();
		}

		public void TriggerOnMouseEnterSlot(ItemSlot slot)
		{
			throw new NotImplementedException();
		}

		public void TriggerOnMouseLeaveSlot(ItemSlot itemSlot)
		{
			throw new NotImplementedException();
		}

		public void TriggerOnMouseClickSlot(ItemSlot itemSlot)
		{
			throw new NotImplementedException();
		}

		public IPlayerInventoryManager PlayerInventoryManager
		{
			get
			{
				return null;
			}
		}

		public IPlayer Player
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public void RenderItemstack(ItemStack itemstack, double posX, double posY, double posZ, float size, int color, bool rotate = false, bool showStackSize = true)
		{
			throw new NotImplementedException();
		}

		public void BeginClipArea(ElementBounds bounds)
		{
			ScreenManager.Platform.GlScissor((int)bounds.renderX, (int)((double)ScreenManager.Platform.WindowSize.Height - bounds.renderY - bounds.InnerHeight), (int)bounds.InnerWidth, (int)bounds.InnerHeight);
			ScreenManager.Platform.GlScissorFlag(true);
		}

		public void EndClipArea()
		{
			ScreenManager.Platform.GlScissorFlag(false);
		}

		private void OnMasterSoundLevelChanged(int newValue)
		{
			ScreenManager.Platform.MasterSoundLevel = (float)newValue / 100f;
		}

		private void OnMusicLevelChanged(int newValue)
		{
			if (ScreenManager.IntroMusic != null && !ScreenManager.IntroMusic.HasStopped)
			{
				ScreenManager.IntroMusic.SetVolume();
			}
		}

		private void onWindowClosed()
		{
			this.CurrentScreen.OnWindowClosed();
		}

		public void OnKeyDown(KeyEvent e)
		{
			ScreenManager.KeyboardKeyState[e.KeyCode] = true;
			ScreenManager.KeyboardModifiers.AltPressed = e.AltPressed;
			ScreenManager.KeyboardModifiers.CtrlPressed = e.CtrlPressed;
			ScreenManager.KeyboardModifiers.ShiftPressed = e.ShiftPressed;
			if (this.CurrentScreen.GetType() != typeof(GuiScreenRunningGame))
			{
				bool handled = ScreenManager.hotkeyManager.TriggerGlobalHotKey(e, null, null, false);
				e.Handled = handled;
			}
			this.CurrentScreen.OnKeyDown(e);
		}

		public void OnKeyUp(KeyEvent e)
		{
			ScreenManager.KeyboardKeyState[e.KeyCode] = false;
			ScreenManager.KeyboardModifiers.AltPressed = e.AltPressed;
			ScreenManager.KeyboardModifiers.CtrlPressed = e.CtrlPressed;
			ScreenManager.KeyboardModifiers.ShiftPressed = e.ShiftPressed;
			this.CurrentScreen.OnKeyUp(e);
		}

		public void OnKeyPress(KeyEvent e)
		{
			if (e.KeyCode == 50)
			{
				this.CurrentScreen.OnBackPressed();
			}
			this.CurrentScreen.OnKeyPress(e);
		}

		public void OnMouseDown(MouseEvent e)
		{
			ScreenManager.MouseButtonState[(int)e.Button] = true;
			this.mouseX = e.X;
			this.mouseY = e.Y;
			this.CurrentScreen.OnMouseDown(e);
		}

		public void OnMouseUp(MouseEvent e)
		{
			ScreenManager.MouseButtonState[(int)e.Button] = false;
			this.CurrentScreen.OnMouseUp(e);
		}

		public void OnMouseMove(MouseEvent e)
		{
			this.mouseX = e.X;
			this.mouseY = e.Y;
			this.CurrentScreen.OnMouseMove(e);
		}

		public void OnMouseWheel(Vintagestory.API.Client.MouseWheelEventArgs e)
		{
			this.CurrentScreen.OnMouseWheel(e);
		}

		public void LoadAndCacheScreen(Type screenType)
		{
			if (this.CachedScreens.ContainsValue(screenType))
			{
				if (this.CurrentScreen != null && !this.CachedScreens.ContainsKey(this.CurrentScreen) && this.CurrentScreen != this.mainScreen)
				{
					this.CurrentScreen.Dispose();
				}
				this.CurrentScreen = this.CachedScreens.FirstOrDefault((KeyValuePair<GuiScreen, Type> x) => x.Value == screenType).Key;
				if (this.CurrentScreen != null)
				{
					this.CurrentScreen.OnScreenLoaded();
					return;
				}
			}
			else
			{
				this.CurrentScreen = (GuiScreen)Activator.CreateInstance(screenType, new object[] { this, this.mainScreen });
				this.CachedScreens[this.CurrentScreen] = screenType;
				if (this.CurrentScreen != null)
				{
					this.CurrentScreen.OnScreenLoaded();
				}
			}
		}

		public void LoadScreen(GuiScreen screen)
		{
			if (this.CurrentScreen != null && !this.CachedScreens.ContainsKey(this.CurrentScreen) && screen != this.CurrentScreen && screen != null && screen.ShouldDisposePreviousScreen)
			{
				this.CurrentScreen.Dispose();
			}
			this.CurrentScreen = screen;
			if (this.CurrentScreen != null)
			{
				this.CurrentScreen.OnScreenLoaded();
			}
		}

		public void LoadScreenNoLoadCall(GuiScreen screen)
		{
			if (this.CurrentScreen != null && !this.CachedScreens.ContainsKey(this.CurrentScreen) && screen != this.CurrentScreen && screen != null && screen.ShouldDisposePreviousScreen)
			{
				this.CurrentScreen.Dispose();
			}
			this.CurrentScreen = screen;
		}

		internal void StartMainMenu()
		{
			this.initMainMenu();
			this.CurrentScreen = this.mainScreen;
			this.CurrentScreen.OnScreenLoaded();
			ScreenManager.Platform.MouseGrabbed = false;
		}

		private void initMainMenu()
		{
			ScreenManager.GuiComposers.ClearCache();
			foreach (KeyValuePair<GuiScreen, Type> val in this.CachedScreens)
			{
				val.Key.Dispose();
			}
			this.CachedScreens.Clear();
			GuiScreen currentScreen = this.CurrentScreen;
			if (currentScreen != null)
			{
				currentScreen.Dispose();
			}
			GuiScreenMainRight guiScreenMainRight = this.mainScreen;
			if (guiScreenMainRight != null)
			{
				guiScreenMainRight.Dispose();
			}
			GuiCompositeMainMenuLeft guiCompositeMainMenuLeft = this.guiMainmenuLeft;
			if (guiCompositeMainMenuLeft != null)
			{
				guiCompositeMainMenuLeft.Dispose();
			}
			this.guiMainmenuLeft = new GuiCompositeMainMenuLeft(this);
			this.mainScreen.Compose();
			this.mainScreen.Refresh();
		}

		public void StartGame(bool singleplayer, StartServerArgs serverargs, ServerConnectData connectData)
		{
			GuiScreenRunningGame screenGame = new GuiScreenRunningGame(this, this.mainScreen);
			screenGame.Start(singleplayer, serverargs, connectData);
			this.CurrentScreen = new GuiScreenConnectingToServer(singleplayer, this, screenGame);
		}

		public void InstallMod(string modid)
		{
			GuiScreenRunningGame screenGame = this.CurrentScreen as GuiScreenRunningGame;
			if (screenGame != null)
			{
				screenGame.ExitOrRedirect(false, "modinstall request");
				Action <>9__1;
				TyronThreadPool.QueueTask(delegate
				{
					int i = 0;
					while (i++ < 1000 && !(this.CurrentScreen is GuiScreenMainRight))
					{
						Thread.Sleep(100);
					}
					Action action;
					if ((action = <>9__1) == null)
					{
						action = (<>9__1 = delegate
						{
							this.InstallMod(modid);
						});
					}
					ScreenManager.EnqueueMainThreadTask(action);
				}, "mod install");
				return;
			}
			modid = modid.Replace("vintagestorymodinstall://", "").TrimEnd('/');
			this.LoadScreen(new GuiScreenDownloadMods(null, GamePaths.DataPathMods, new List<string> { modid }, this, this.mainScreen));
		}

		public void ConnectToMultiplayer(string host, string password)
		{
			if (host.Contains("vintagestoryjoin://"))
			{
				GuiScreen curScreen = this.CurrentScreen;
				this.LoadScreen(new GuiScreenConfirmAction(Lang.Get("confirm-joinserver", new object[] { host.Replace("vintagestoryjoin://", "").TrimEnd('/') }), delegate(bool ok)
				{
					if (ok)
					{
						this.ConnectToMultiplayer(host.Replace("vintagestoryjoin://", ""), null);
						return;
					}
					this.LoadScreen(curScreen);
				}, this, this.CurrentScreen, false));
				return;
			}
			try
			{
				ServerConnectData connectData = ServerConnectData.FromHost(host);
				connectData.ServerPassword = password;
				this.StartGame(false, null, connectData);
			}
			catch (Exception e)
			{
				this.LoadScreen(new GuiScreenDisconnected(Lang.Get("multiplayer-disconnected", new object[] { e.Message }), this, this.mainScreen, "server-disconnected"));
				ScreenManager.Platform.Logger.Warning("Could not initiate connection:");
				ScreenManager.Platform.Logger.Warning(e);
			}
		}

		public void ConnectToSingleplayer(StartServerArgs serverargs)
		{
			this.StartGame(true, serverargs, null);
		}

		internal int GetGuiTexture(string name)
		{
			if (!this.textures.ContainsKey(name))
			{
				BitmapRef bmp = ScreenManager.Platform.AssetManager.Get("textures/gui/" + name).ToBitmap(this.api);
				int textureid = ScreenManager.Platform.LoadTexture(bmp, false, 0, false);
				this.textures[name] = textureid;
				bmp.Dispose();
			}
			return this.textures[name];
		}

		public int GetMouseCurrentX()
		{
			return this.mouseX;
		}

		public int GetMouseCurrentY()
		{
			return this.mouseY;
		}

		public static void PlaySound(string name)
		{
			ScreenManager.PlaySound(new AssetLocation("sounds/" + name).WithPathAppendixOnce(".ogg"));
		}

		public static void PlaySound(AssetLocation location)
		{
			location = location.Clone().WithPathAppendixOnce(".ogg");
			AudioData data;
			ScreenManager.soundAudioData.TryGetValue(location, out data);
			if (data != null)
			{
				if (data.Load())
				{
					ILoadedSound sound = ScreenManager.Platform.CreateAudio(new SoundParams(location), data);
					sound.Start();
					TyronThreadPool.QueueLongDurationTask(delegate
					{
						while (!sound.HasStopped)
						{
							Thread.Sleep(100);
						}
						sound.Dispose();
					});
					return;
				}
			}
			else
			{
				ScreenManager.Platform.Logger.Error("Could not play {0}, sound file not found", new object[] { location });
			}
		}

		public ClientPlatformAbstract getGamePlatform()
		{
			return ScreenManager.Platform;
		}

		private void onFocusChanged(bool focus)
		{
			this.CurrentScreen.OnFocusChanged(focus);
		}

		private void onFileDrop(string filename)
		{
			this.CurrentScreen.OnFileDrop(filename);
		}

		internal void TryRedirect(MultiplayerServerEntry entry)
		{
			ScreenManager.Platform.Logger.Notification("Redirecting to new server");
			this.ConnectToMultiplayer(entry.host, entry.password);
		}

		internal void OfferRestart(string message)
		{
			GuiScreen currentScreen = this.CurrentScreen;
			Action<bool> <>9__1;
			ScreenManager.EnqueueMainThreadTask(delegate
			{
				ScreenManager <>4__this = this;
				string text = "restart-title";
				string message2 = message;
				string text2 = "general-cancel";
				string text3 = "game-exit";
				Action<bool> action;
				if ((action = <>9__1) == null)
				{
					action = (<>9__1 = delegate(bool val)
					{
						if (val)
						{
							ScreenManager.Platform.WindowExit("requires restart");
							return;
						}
						GuiScreenConnectingToServer csc = currentScreen as GuiScreenConnectingToServer;
						if (csc != null)
						{
							csc.exitToMainMenu();
							return;
						}
						this.StartMainMenu();
					});
				}
				<>4__this.LoadScreen(new GuiScreenConfirmAction(text, message2, text2, text3, action, this, currentScreen, "restart-confirmation", false));
			});
		}

		public static int MainThreadId;

		public static GuiComposerManager GuiComposers;

		public static ClientPlatformAbstract Platform;

		public static FrameProfilerUtil FrameProfiler;

		public static KeyModifiers KeyboardModifiers = new KeyModifiers();

		private static int keysMax = 512;

		public static bool[] KeyboardKeyState = new bool[ScreenManager.keysMax];

		public static bool[] MouseButtonState = new bool[(int)Enum.GetValues(typeof(EnumMouseButton)).Cast<EnumMouseButton>().Max<EnumMouseButton>()];

		public static Dictionary<AssetLocation, AudioData> soundAudioData = new Dictionary<AssetLocation, AudioData>();

		public static Dictionary<AssetLocation, AudioData> soundAudioDataAsyncLoadTemp = new Dictionary<AssetLocation, AudioData>();

		public static Queue<Action> MainThreadTasks = new Queue<Action>();

		public static bool debugDrawCallNextFrame = false;

		public SessionManager sessionManager;

		public static HotkeyManager hotkeyManager;

		private Dictionary<GuiScreen, Type> CachedScreens = new Dictionary<GuiScreen, Type>();

		internal GuiScreen CurrentScreen;

		internal GuiScreenMainRight mainScreen;

		private long lastSaveCheck;

		public string loadingText;

		public bool ClientIsOffline = true;

		private bool cursorsLoaded;

		protected EnumAuthServerResponse? validationResponse;

		private bool awaitValidation = true;

		private int mouseX;

		private int mouseY;

		internal static ILoadedSound IntroMusic;

		internal static bool introMusicShouldStop = false;

		internal GuiComposer mainMenuComposer;

		internal GuiCompositeMainMenuLeft guiMainmenuLeft;

		public MainMenuAPI api;

		internal ModLoader modloader;

		internal List<ModContainer> allMods = new List<ModContainer>();

		internal List<ModContainer> verifiedMods = new List<ModContainer>();

		public static ClientProgramArgs ParsedArgs;

		public static string[] RawCmdLineArgs;

		internal LoadedTexture versionNumberTexture;

		internal string newestVersion;

		private bool withMainMenu = true;

		internal Dictionary<string, int> textures;
	}
}
