using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using CommandLine;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.ClientNative;
using Vintagestory.Common;
using Vintagestory.Server;
using Vintagestory.Server.Network;

namespace Vintagestory.Client
{
	public class ClientProgram
	{
		public static void Main(string[] rawArgs)
		{
			AppDomain currentDomain = AppDomain.CurrentDomain;
			ResolveEventHandler resolveEventHandler;
			if ((resolveEventHandler = ClientProgram.<>O.<0>__AssemblyResolve) == null)
			{
				resolveEventHandler = (ClientProgram.<>O.<0>__AssemblyResolve = new ResolveEventHandler(AssemblyResolver.AssemblyResolve));
			}
			currentDomain.AssemblyResolve += resolveEventHandler;
			ClientProgram.rawArgs = rawArgs;
			new ClientProgram(rawArgs);
		}

		public ClientProgram(string[] rawArgs)
		{
			ClientProgram <>4__this = this;
			AppDomain.CurrentDomain.UnhandledException += this.HandleUnhandledException;
			ClientProgram.progArgs = new ClientProgramArgs();
			ParserResult<ClientProgramArgs> progArgsRaw = new Parser(delegate(ParserSettings config)
			{
				config.HelpWriter = null;
				config.IgnoreUnknownArguments = true;
				config.AutoHelp = false;
				config.AutoVersion = false;
			}).ParseArguments<ClientProgramArgs>(rawArgs);
			ClientProgram.progArgs = progArgsRaw.Value;
			if (ClientProgram.progArgs.DataPath != null && ClientProgram.progArgs.DataPath.Length > 0)
			{
				GamePaths.DataPath = ClientProgram.progArgs.DataPath;
			}
			if (ClientProgram.progArgs.LogPath != null && ClientProgram.progArgs.LogPath.Length > 0)
			{
				GamePaths.CustomLogPath = ClientProgram.progArgs.LogPath;
			}
			GamePaths.EnsurePathsExist();
			if (RuntimeEnv.OS == OS.Windows && (ClientProgram.progArgs.PrintVersion || ClientProgram.progArgs.PrintHelp))
			{
				WindowsConsole.Attach();
			}
			if (ClientProgram.progArgs.PrintVersion)
			{
				Console.WriteLine("1.21.5");
				return;
			}
			if (ClientProgram.progArgs.PrintHelp)
			{
				Console.WriteLine(ClientProgram.progArgs.GetUsage(progArgsRaw));
				return;
			}
			if (ClientProgram.progArgs.InstallModId != null)
			{
				ClientProgram.progArgs.InstallModId = ClientProgram.progArgs.InstallModId.Replace("vintagestorymodinstall://", "");
			}
			UriHandler handler = UriHandler.Instance;
			if (handler.TryConnectClientPipe())
			{
				if (ClientProgram.progArgs.ConnectServerAddress != null)
				{
					handler.SendConnect(ClientProgram.progArgs.ConnectServerAddress);
					handler.Dispose();
					return;
				}
				if (ClientProgram.progArgs.InstallModId != null)
				{
					handler.SendModInstall(ClientProgram.progArgs.InstallModId);
					handler.Dispose();
					return;
				}
			}
			else
			{
				handler.StartPipeServer();
			}
			this.dummyNetwork = new DummyNetwork();
			this.dummyNetworkUdp = new DummyNetwork();
			this.dummyNetwork.Start();
			this.dummyNetworkUdp.Start();
			this.crashreporter = new CrashReporter(EnumAppSide.Client);
			try
			{
				this.crashreporter.Start(delegate
				{
					<>4__this.Start(ClientProgram.progArgs, rawArgs);
				});
			}
			finally
			{
				handler.Dispose();
			}
		}

		private void HandleUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			Exception exceptionObject = (Exception)e.ExceptionObject;
			if (this.crashreporter == null)
			{
				this.platform.XPlatInterface.ShowMessageBox("Fatal Error", exceptionObject.Message);
				return;
			}
			if (!this.crashreporter.isCrashing)
			{
				this.crashreporter.Crash(exceptionObject);
			}
		}

		private void Start(ClientProgramArgs args, string[] rawArgs)
		{
			string appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			if (!Debugger.IsAttached)
			{
				Environment.CurrentDirectory = appPath;
			}
			ClientProgram.logger = new ClientLogger();
			ClientProgram.logger.TraceLog = args.TraceLog;
			ClientPlatformWindows platform = new ClientPlatformWindows(ClientProgram.logger);
			platform.ShaderUniforms.SepiaLevel = ClientSettings.SepiaLevel;
			platform.ShaderUniforms.ExtraContrastLevel = ClientSettings.ExtraContrastLevel;
			CrashReporter.SetLogger((Logger)platform.Logger);
			platform.LogAndTestHardwareInfosStage1();
			ClientProgram.screenManager = new ScreenManager(platform);
			GuiStyle.DecorativeFontName = ClientSettings.DecorativeFontName;
			GuiStyle.StandardFontName = ClientSettings.DefaultFontName;
			Lang.PreLoad(ScreenManager.Platform.Logger, GamePaths.AssetsPath, ClientSettings.Language);
			if (RuntimeEnv.OS == OS.Windows && !ClientSettings.SkipNvidiaProfileCheck && NvidiaGPUFix64.SOP_SetProfile("Vintagestory", ClientProgram.GetExecutableName()) == 1)
			{
				platform.XPlatInterface.ShowMessageBox("Vintagestory Nvidia Profile", Lang.Get("Your game is now configured to use your dedicated NVIDIA Graphics card. This requires a restart so please start the game again.", Array.Empty<object>()));
				return;
			}
			if (!CleanInstallCheck.IsCleanInstall())
			{
				platform.XPlatInterface.ShowMessageBox("Vintagestory Warning", Lang.Get("launchfailure-notcleaninstall", Array.Empty<object>()));
				return;
			}
			if (RuntimeEnv.OS == OS.Windows && !ClientSettings.MultipleInstances)
			{
				bool createdNew;
				new Mutex(true, "Vintagestory", out createdNew);
				if (!createdNew)
				{
					platform.XPlatInterface.ShowMessageBox(Lang.Get("Multiple Instances", Array.Empty<object>()), Lang.Get("game-alreadyrunning", Array.Empty<object>()));
					return;
				}
			}
			ClientProgram.Signals[0] = PosixSignalRegistration.Create(PosixSignal.SIGTERM, new Action<PosixSignalContext>(this.OnExit));
			ClientProgram.Signals[1] = PosixSignalRegistration.Create(PosixSignal.SIGINT, new Action<PosixSignalContext>(this.OnExit));
			platform.SetServerExitInterface(platform.ServerExit);
			platform.crashreporter = this.crashreporter;
			platform.singlePlayerServerDummyNetwork = new DummyNetwork[2];
			platform.singlePlayerServerDummyNetwork[0] = this.dummyNetwork;
			platform.singlePlayerServerDummyNetwork[1] = this.dummyNetworkUdp;
			this.platform = platform;
			platform.OnStartSinglePlayerServer = delegate(StartServerArgs serverargs)
			{
				this.startServerargs = serverargs;
				new Thread(new ThreadStart(this.ServerThreadStart))
				{
					Name = "SingleplayerServer",
					Priority = ThreadPriority.BelowNormal,
					IsBackground = true
				}.Start();
			};
			WindowState windowState2;
			switch (ClientSettings.GameWindowMode)
			{
			case 1:
				windowState2 = WindowState.Fullscreen;
				break;
			case 2:
				windowState2 = WindowState.Maximized;
				break;
			case 3:
				windowState2 = WindowState.Fullscreen;
				break;
			default:
				windowState2 = WindowState.Normal;
				break;
			}
			WindowState windowState = windowState2;
			ScreenManager.Platform.Logger.Debug("Creating game window with window mode " + windowState.ToString());
			Size2i screenSize = ScreenManager.Platform.ScreenSize;
			if (ClientSettings.IsNewSettingsFile)
			{
				int width = 1280;
				int height = 850;
				float guiscale = 1f;
				if (screenSize.Width - 20 < width || screenSize.Height - 20 < height)
				{
					guiscale = 0.875f;
					width = Math.Min(screenSize.Width - 20, width);
					height = Math.Min(screenSize.Height - 20, height);
				}
				if (height < 680)
				{
					guiscale = 0.75f;
				}
				if (screenSize.Width > 2500)
				{
					guiscale = 1.25f;
				}
				if (screenSize.Width > 3000)
				{
					guiscale = 1.5f;
					width = 2000;
				}
				if (screenSize.Width > 5000)
				{
					guiscale = 2f;
				}
				if (screenSize.Height > 1300)
				{
					screenSize.Height = 1200;
				}
				ClientSettings.ScreenWidth = width;
				ClientSettings.ScreenHeight = height;
				ClientSettings.GUIScale = guiscale;
			}
			if (ClientSettings.ScreenWidth < 10)
			{
				ClientSettings.ScreenWidth = 10;
			}
			if (ClientSettings.ScreenHeight < 10)
			{
				ClientSettings.ScreenHeight = 10;
			}
			string[] array = ClientSettings.GlContextVersion.Split('.', StringSplitOptions.None);
			int openGlMajor = array[0].ToInt(4);
			int openGlMinor = array[1].ToInt(3);
			GameWindowSettings gameWindowSettings = GameWindowSettings.Default;
			NativeWindowSettings nativeWindowSettings = new NativeWindowSettings
			{
				Title = "Vintage Story",
				APIVersion = new Version(openGlMajor, openGlMinor),
				ClientSize = new Vector2i(ClientSettings.ScreenWidth, ClientSettings.ScreenHeight),
				Flags = ContextFlags.Default,
				Vsync = ((ClientSettings.VsyncMode != 0) ? VSyncMode.On : VSyncMode.Off),
				WindowState = windowState,
				WindowBorder = (WindowBorder)ClientSettings.WindowBorder
			};
			if (RuntimeEnv.OS == OS.Mac)
			{
				nativeWindowSettings.Flags = ContextFlags.ForwardCompatible;
			}
			GLFW.SetErrorCallback(new GLFWCallbacks.ErrorCallback(this.GlfwErrorCallback));
			GameWindowNative gamewindow = this.AttemptToOpenWindow(gameWindowSettings, nativeWindowSettings, openGlMajor, openGlMinor, 3);
			if (windowState == WindowState.Normal && !RuntimeEnv.IsWaylandSession)
			{
				gamewindow.CenterWindow();
			}
			platform.StartAudio();
			platform.LogAndTestHardwareInfosStage2();
			platform.window = gamewindow;
			platform.XPlatInterface.Window = gamewindow;
			platform.SetDirectMouseMode(ClientSettings.DirectMouseMode);
			platform.WindowSize.Width = gamewindow.ClientSize.X;
			platform.WindowSize.Height = gamewindow.ClientSize.Y;
			if (ClientSettings.GameWindowMode == 3)
			{
				platform.SetWindowAttribute(WindowAttribute.AutoIconify, false);
			}
			ClientProgram.screenManager.Start(args, rawArgs);
			platform.Start();
			Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
			try
			{
				gamewindow.Run();
			}
			finally
			{
				if (RuntimeEnv.OS == OS.Windows)
				{
					GLFW.IconifyWindow(gamewindow.WindowPtr);
				}
				Thread.CurrentThread.Priority = ThreadPriority.Normal;
				ScreenManager.Platform.Logger.Debug("After gamewindow.Run()");
				platform.DisposeFrameBuffers(platform.FrameBuffers);
				platform.StopAudio();
				gamewindow.Dispose();
			}
		}

		private GameWindowNative AttemptToOpenWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings, int openGlMajor, int openGlMinor, int tries)
		{
			nativeWindowSettings.APIVersion = new Version(openGlMajor, openGlMinor);
			GameWindowNative gamewindow;
			try
			{
				gamewindow = new GameWindowNative(gameWindowSettings, nativeWindowSettings);
			}
			catch (Exception e)
			{
				bool throwException = false;
				if (tries <= 1)
				{
					throwException = true;
				}
				else
				{
					GLFWException ge = e as GLFWException;
					if (ge != null)
					{
						int i = ge.Message.IndexOf(openGlMinor.ToString() + ", got version ");
						if (i > 0)
						{
							openGlMajor = (int)(e.Message[i + 15] - '0');
							openGlMinor = (int)(e.Message[i + 17] - '0');
							Thread.Sleep(100);
							return this.AttemptToOpenWindow(gameWindowSettings, nativeWindowSettings, openGlMajor, openGlMinor, tries - 1);
						}
						i = ge.Message.IndexOf("OpenGL version " + openGlMajor.ToString() + "." + openGlMinor.ToString());
						if (i < 0)
						{
							throwException = true;
						}
					}
				}
				if (openGlMajor < 3 || (openGlMajor == 3 && openGlMinor <= 3))
				{
					throwException = true;
				}
				if (throwException)
				{
					throw new Exception("** Unable to start OpenGL graphics. Try: (1) restart the computer (2) update the graphics driver (3) more advice at wiki.vintagestory.at/Troubleshooting_Guide#OpenGL_crash **", e);
				}
				if (openGlMajor > 4)
				{
					openGlMajor = 4;
					openGlMinor = 3;
				}
				else if (openGlMajor == 4 && openGlMinor > 3)
				{
					openGlMinor = 3;
				}
				else
				{
					openGlMajor = 3;
					openGlMinor = 3;
				}
				Thread.Sleep(100);
				return this.AttemptToOpenWindow(gameWindowSettings, nativeWindowSettings, openGlMajor, openGlMinor, tries - 1);
			}
			if (openGlMajor < 4 || (openGlMajor == 4 && openGlMinor < 3))
			{
				ClientSettings.AllowSSBOs = false;
			}
			return gamewindow;
		}

		private void GlfwErrorCallback(ErrorCode error, string description)
		{
			if (error == ErrorCode.FormatUnavailable)
			{
				this.platform.Logger.Debug("GLFW FormatUnavailable: " + description);
				return;
			}
			ILogger logger = this.platform.Logger;
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(27, 2);
			defaultInterpolatedStringHandler.AppendLiteral("GLFW Exception: ErrorCode:");
			defaultInterpolatedStringHandler.AppendFormatted<ErrorCode>(error);
			defaultInterpolatedStringHandler.AppendLiteral(" ");
			defaultInterpolatedStringHandler.AppendFormatted(description);
			logger.Error(defaultInterpolatedStringHandler.ToStringAndClear());
		}

		private void OnExit(PosixSignalContext ctx)
		{
			ctx.Cancel = true;
			UriHandler.Instance.Dispose();
			ClientProgram.screenManager.GamePlatform.WindowExit("SIGTERM or SIGINT received");
		}

		public void ServerThreadStart()
		{
			ServerMain server = null;
			ServerProgramArgs serverArgs = new Parser(delegate(ParserSettings config)
			{
				config.IgnoreUnknownArguments = true;
				config.AutoHelp = false;
				config.AutoVersion = false;
			}).ParseArguments<ServerProgramArgs>(ClientProgram.rawArgs).Value;
			this.dummyNetwork.Clear();
			this.platform.Logger.Notification("Server args parsed");
			try
			{
				server = new ServerMain(this.startServerargs, ClientProgram.rawArgs, serverArgs, false);
				this.platform.Logger.Notification("Server main instantiated");
				server.exit = this.platform.ServerExit;
				DummyTcpNetServer netServer = new DummyTcpNetServer();
				netServer.SetNetwork(this.dummyNetwork);
				server.MainSockets[0] = netServer;
				DummyUdpNetServer udpNetServer = new DummyUdpNetServer();
				udpNetServer.SetNetwork(this.dummyNetworkUdp);
				server.UdpSockets[0] = udpNetServer;
				this.platform.IsServerRunning = true;
				this.platform.SetGamePausedState(false);
				server.PreLaunch();
				server.Launch();
				this.platform.Logger.Notification("Server launched");
				bool wasPaused = false;
				do
				{
					if (!wasPaused && this.platform.IsGamePaused)
					{
						server.Suspend(true, 60000);
						wasPaused = true;
					}
					if (wasPaused && !this.platform.IsGamePaused)
					{
						server.Suspend(false, 60000);
						wasPaused = false;
					}
					server.Process();
					if (!this.platform.singlePlayerServerLoaded)
					{
						this.platform.Logger.VerboseDebug("--- Server started ---");
					}
					this.platform.singlePlayerServerLoaded = true;
				}
				while (this.platform.ServerExit == null || !this.platform.ServerExit.GetExit());
				server.Stop("Exit request by client", null, EnumLogType.Notification);
				this.platform.IsServerRunning = false;
				this.platform.singlePlayerServerLoaded = false;
				server.Dispose();
			}
			catch (Exception e)
			{
				this.platform.Logger.Fatal(e);
				if (server != null)
				{
					server.Stop("Exception thrown by server during startup or process", null, EnumLogType.Notification);
					this.platform.IsServerRunning = false;
					this.platform.singlePlayerServerLoaded = false;
					try
					{
						server.Dispose();
					}
					catch (Exception)
					{
					}
				}
				if (e is RestartGameException)
				{
					ClientProgram.screenManager.OfferRestart(e.Message);
				}
			}
			this.dummyNetwork.Clear();
		}

		private static string GetExecutableName()
		{
			string fileName = Process.GetCurrentProcess().MainModule.FileName;
			int num = fileName.LastIndexOf('\\');
			return fileName.Substring(num + 1, fileName.Length - num - 1);
		}

		private CrashReporter crashreporter;

		private DummyNetwork dummyNetwork;

		private DummyNetwork dummyNetworkUdp;

		private StartServerArgs startServerargs;

		private static Logger logger;

		public ClientPlatformWindows platform;

		private static string[] rawArgs;

		private static ClientProgramArgs progArgs;

		private static readonly PosixSignalRegistration[] Signals = new PosixSignalRegistration[2];

		public static ScreenManager screenManager;

		[CompilerGenerated]
		private static class <>O
		{
			public static ResolveEventHandler <0>__AssemblyResolve;
		}
	}
}
