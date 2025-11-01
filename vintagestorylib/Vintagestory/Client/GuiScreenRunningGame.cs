using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.Client.Network;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;
using Vintagestory.Server;

namespace Vintagestory.Client
{
	public class GuiScreenRunningGame : GuiScreen
	{
		public GuiScreenRunningGame(ScreenManager screenManager, GuiScreen parent)
			: base(screenManager, parent)
		{
			this.platform = ScreenManager.Platform;
			this.runningGame = new ClientMain(this, ScreenManager.Platform);
			base.RenderBg = false;
		}

		private static void Logger_EntryAddedClient(EnumLogType logType, string message, object[] args)
		{
			GuiScreenRunningGame.Logger_EntryAdded(EnumAppSide.Client, logType, message, args);
		}

		private static void Logger_EntryAddedServer(EnumLogType logType, string message, object[] args)
		{
			GuiScreenRunningGame.Logger_EntryAdded(EnumAppSide.Server, logType, message, args);
		}

		private static void Logger_EntryAdded(EnumAppSide side, EnumLogType logType, string message, params object[] args)
		{
			object obj = GuiScreenRunningGame.warningEntriesLock;
			lock (obj)
			{
				if (GuiScreenRunningGame.captureWarnings && (logType == EnumLogType.Error || logType == EnumLogType.Warning || logType == EnumLogType.Fatal))
				{
					GuiScreenRunningGame.warningEntries.Add(new LogEntry
					{
						Logtype = logType,
						Message = message,
						args = args,
						Side = side
					});
				}
			}
		}

		private void handOverRenderingToRunningGame()
		{
			this.runningGame.MouseGrabbed = this.platform.IsFocused;
			this.ScreenManager.LoadScreen(this);
			ScreenManager.introMusicShouldStop = true;
			if (ScreenManager.IntroMusic != null && !ScreenManager.IntroMusic.HasStopped)
			{
				float volume = ScreenManager.IntroMusic.Params.Volume;
				TyronThreadPool.QueueLongDurationTask(delegate
				{
					while ((double)ScreenManager.IntroMusic.Params.Volume > 0.01)
					{
						Thread.Sleep(40);
						ScreenManager.IntroMusic.SetVolume(volume *= 0.98f);
					}
					ScreenManager.IntroMusic.Stop();
				});
			}
			ScreenManager.EnqueueMainThreadTask(new Action(this.printWarningsAndEndCapture));
		}

		public override bool OnEvent(string eventCode, object arg)
		{
			if (eventCode == "maploaded")
			{
				this.handOverRenderingToRunningGame();
				return true;
			}
			return false;
		}

		public void Start(bool singleplayer, StartServerArgs serverargs, ServerConnectData connectData)
		{
			this.singleplayer = singleplayer;
			this.serverargs = serverargs;
			this.connectData = connectData;
			this.runningGame.IsSingleplayer = singleplayer;
			this.runningGame.Start();
			ServerConnectData serverConnectData = this.connectData;
			if (((serverConnectData != null) ? serverConnectData.ErrorMessage : null) == null)
			{
				this.Connect();
			}
		}

		private void Connect()
		{
			GuiScreenRunningGame.warningEntries.Clear();
			GuiScreenRunningGame.captureWarnings = true;
			ILogger logger = ScreenManager.Platform.Logger;
			LogEntryDelegate logEntryDelegate;
			if ((logEntryDelegate = GuiScreenRunningGame.<>O.<0>__Logger_EntryAddedClient) == null)
			{
				logEntryDelegate = (GuiScreenRunningGame.<>O.<0>__Logger_EntryAddedClient = new LogEntryDelegate(GuiScreenRunningGame.Logger_EntryAddedClient));
			}
			logger.EntryAdded -= logEntryDelegate;
			ILogger logger2 = ScreenManager.Platform.Logger;
			LogEntryDelegate logEntryDelegate2;
			if ((logEntryDelegate2 = GuiScreenRunningGame.<>O.<0>__Logger_EntryAddedClient) == null)
			{
				logEntryDelegate2 = (GuiScreenRunningGame.<>O.<0>__Logger_EntryAddedClient = new LogEntryDelegate(GuiScreenRunningGame.Logger_EntryAddedClient));
			}
			logger2.EntryAdded += logEntryDelegate2;
			if (this.singleplayer)
			{
				this.platform.StartSinglePlayerServer(this.serverargs);
				TyronThreadPool.QueueTask(delegate
				{
					while (ServerMain.Logger == null)
					{
						Thread.Sleep(1);
					}
					LoggerBase logger3 = ServerMain.Logger;
					LogEntryDelegate logEntryDelegate3;
					if ((logEntryDelegate3 = GuiScreenRunningGame.<>O.<1>__Logger_EntryAddedServer) == null)
					{
						logEntryDelegate3 = (GuiScreenRunningGame.<>O.<1>__Logger_EntryAddedServer = new LogEntryDelegate(GuiScreenRunningGame.Logger_EntryAddedServer));
					}
					logger3.EntryAdded -= logEntryDelegate3;
					LoggerBase logger4 = ServerMain.Logger;
					LogEntryDelegate logEntryDelegate4;
					if ((logEntryDelegate4 = GuiScreenRunningGame.<>O.<1>__Logger_EntryAddedServer) == null)
					{
						logEntryDelegate4 = (GuiScreenRunningGame.<>O.<1>__Logger_EntryAddedServer = new LogEntryDelegate(GuiScreenRunningGame.Logger_EntryAddedServer));
					}
					logger4.EntryAdded += logEntryDelegate4;
				});
				this.connectData = new ServerConnectData();
				this.runningGame.Connectdata = this.connectData;
				DummyTcpNetClient netClient = new DummyTcpNetClient();
				DummyNetwork[] dummyNetworks = this.platform.GetSinglePlayerServerNetwork();
				netClient.SetNetwork(dummyNetworks[0]);
				this.runningGame.MainNetClient = netClient;
				DummyUdpNetClient udpNetClient = new DummyUdpNetClient();
				this.runningGame.UdpNetClient = udpNetClient;
				udpNetClient.SetNetwork(dummyNetworks[1]);
			}
			else
			{
				this.runningGame.Connectdata = this.connectData;
				TcpNetClient client = new TcpNetClient();
				UdpNetClient udpclient = new UdpNetClient();
				this.runningGame.MainNetClient = client;
				this.runningGame.UdpNetClient = udpclient;
			}
			ScreenManager.Platform.Logger.Notification("Initialized Server Connection");
		}

		public override void RenderToPrimary(float dt)
		{
			this.platform.DoPostProcessingEffects = true;
			float ssaaLevel = ClientSettings.SSAA;
			float width = (float)this.platform.WindowSize.Width;
			int height = this.platform.WindowSize.Height;
			int fullWidth = (int)(width * ssaaLevel);
			int fullHeight = (int)((float)height * ssaaLevel);
			this.platform.GlViewport(0, 0, fullWidth, fullHeight);
			this.runningGame.MainGameLoop(dt);
			if (this.runningGame.doReconnect)
			{
				this.Reconnect();
				return;
			}
			if (this.runningGame.exitToDisconnectScreen)
			{
				this.ExitOrRedirect(true, null);
				return;
			}
			if (this.runningGame.exitToMainMenu)
			{
				bool deleteWorld = this.runningGame.deleteWorld;
				this.ExitOrRedirect(false, null);
				if (deleteWorld)
				{
					TyronThreadPool.QueueTask(delegate
					{
						Thread.Sleep(150);
						try
						{
							this.ScreenManager.GamePlatform.XPlatInterface.MoveFileToRecyclebin(this.serverargs.SaveFileLocation);
						}
						catch
						{
						}
					});
				}
				return;
			}
		}

		public override void RenderAfterPostProcessing(float dt)
		{
			if (this.runningGame.doReconnect || this.runningGame.exitToMainMenu)
			{
				return;
			}
			this.runningGame.RenderAfterPostProcessing(dt);
		}

		public override void RenderAfterFinalComposition(float dt)
		{
			if (this.runningGame.doReconnect || this.runningGame.exitToMainMenu)
			{
				return;
			}
			this.runningGame.RenderAfterFinalComposition(dt);
		}

		public override void RenderAfterBlit(float dt)
		{
			if (this.runningGame.doReconnect || this.runningGame.exitToMainMenu)
			{
				return;
			}
			this.runningGame.RenderAfterBlit(dt);
		}

		public override void RenderToDefaultFramebuffer(float dt)
		{
			int width = this.platform.WindowSize.Width;
			int height = this.platform.WindowSize.Height;
			this.platform.GlViewport(0, 0, width, height);
			if (!this.runningGame.exitToMainMenu)
			{
				this.runningGame.RenderToDefaultFramebuffer(dt);
			}
		}

		public override void OnWindowClosed()
		{
			GuiScreenRunningGame.captureWarnings = false;
			this.ExitOrRedirect(false, "window close event");
		}

		private void Reconnect()
		{
			GuiScreenRunningGame.captureWarnings = true;
			this.ExitOrRedirect(false, null);
			this.ScreenManager.StartGame(this.singleplayer, this.serverargs, this.connectData);
		}

		public override void ReloadWorld(string reason)
		{
			GuiScreenRunningGame.captureWarnings = false;
			this.ExitOrRedirect(false, reason);
			while (ScreenManager.Platform.IsServerRunning)
			{
				Thread.Sleep(5);
			}
			this.ScreenManager.StartGame(this.singleplayer, this.serverargs, this.connectData);
		}

		public void ExitOrRedirect(bool isDisconnect = false, string reason = null)
		{
			GuiScreenRunningGame.captureWarnings = false;
			this.runningGame.MouseGrabbed = false;
			this.runningGame.DestroyGameSession(isDisconnect);
			if (reason == null)
			{
				reason = this.runningGame.exitReason;
			}
			if (reason == null)
			{
				reason = "unknown";
			}
			if (isDisconnect)
			{
				string disconnectReason = this.runningGame.disconnectReason ?? "unknown";
				ScreenManager.Platform.Logger.Notification("Exiting current game to disconnected screen, reason: {0}", new object[] { disconnectReason });
				this.ScreenManager.LoadScreen(new GuiScreenDisconnected(disconnectReason, this.ScreenManager, this.ScreenManager.mainScreen, "server-disconnected"));
			}
			else
			{
				ScreenManager.Platform.Logger.Notification("Exiting current game to main menu, reason: {0}", new object[] { reason });
				if (this.runningGame.IsSingleplayer && ScreenManager.Platform.IsServerRunning)
				{
					this.ScreenManager.LoadAndCacheScreen(typeof(GuiScreenExitingServer));
				}
				else
				{
					this.ScreenManager.StartMainMenu();
				}
			}
			if (this.runningGame.GetRedirect() != null)
			{
				this.ScreenManager.TryRedirect(this.runningGame.GetRedirect());
			}
			this.runningGame = null;
			this.ScreenManager.GamePlatform.ResetGamePauseAndUptimeState();
		}

		public override void OnKeyDown(KeyEvent args)
		{
			this.runningGame.OnKeyDown(args);
		}

		public override void OnKeyUp(KeyEvent args)
		{
			this.runningGame.OnKeyUp(args);
		}

		public override void OnKeyPress(KeyEvent args)
		{
			this.runningGame.OnKeyPress(args);
		}

		public override void OnMouseDown(MouseEvent args)
		{
			if (!this.runningGame.Platform.IsFocused)
			{
				return;
			}
			this.runningGame.OnMouseDownRaw(args);
		}

		public override void OnMouseMove(MouseEvent args)
		{
			if (!this.runningGame.Platform.IsFocused || this.runningGame.disposed)
			{
				return;
			}
			this.runningGame.OnMouseMove(args);
		}

		public override void OnMouseUp(MouseEvent args)
		{
			if (!this.runningGame.Platform.IsFocused)
			{
				return;
			}
			this.runningGame.OnMouseUpRaw(args);
		}

		public override void OnMouseWheel(MouseWheelEventArgs args)
		{
			this.runningGame.OnMouseWheel(args);
		}

		public override bool OnFileDrop(string filename)
		{
			return this.runningGame.OnFileDrop(filename);
		}

		public override void OnFocusChanged(bool focus)
		{
			this.runningGame.OnFocusChanged(focus);
		}

		private void printWarningsAndEndCapture()
		{
			object obj = GuiScreenRunningGame.warningEntriesLock;
			lock (obj)
			{
				GuiScreenRunningGame.captureWarnings = false;
				if (GuiScreenRunningGame.warningEntries.Count > 0)
				{
					ScreenManager.Platform.Logger.Warning("===============================================================");
					ScreenManager.Platform.Logger.Warning("(x_x) Captured {0} issues during startup:", new object[] { GuiScreenRunningGame.warningEntries.Count });
					foreach (LogEntry line in GuiScreenRunningGame.warningEntries)
					{
						object obj2;
						if (line.Side != EnumAppSide.Server)
						{
							obj2 = ScreenManager.Platform.Logger;
						}
						else
						{
							ILogger logger = ServerMain.Logger;
							obj2 = logger;
						}
						object obj3 = obj2;
						if (obj3 != null)
						{
							((ILogger)obj3).Log(line.Logtype, line.Message, line.args);
						}
					}
					ScreenManager.Platform.Logger.Warning("===============================================================");
				}
				else
				{
					ScreenManager.Platform.Logger.Notification("===============================================================");
					ScreenManager.Platform.Logger.Notification("(^_^) No issues captured during startup");
					ScreenManager.Platform.Logger.Notification("===============================================================");
				}
			}
		}

		internal ClientMain runningGame;

		private ServerConnectData connectData;

		private bool singleplayer;

		public StartServerArgs serverargs;

		private ClientPlatformAbstract platform;

		private static object warningEntriesLock = new object();

		private static List<LogEntry> warningEntries = new List<LogEntry>();

		private static bool captureWarnings = true;

		[CompilerGenerated]
		private static class <>O
		{
			public static LogEntryDelegate <0>__Logger_EntryAddedClient;

			public static LogEntryDelegate <1>__Logger_EntryAddedServer;
		}
	}
}
