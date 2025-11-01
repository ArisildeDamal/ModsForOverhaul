using System;
using System.Collections.Generic;
using System.IO;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.Server;

namespace Vintagestory.Client
{
	internal class GuiScreenConnectingToServer : GuiScreen
	{
		private static ILogger Logger
		{
			get
			{
				return ScreenManager.Platform.Logger;
			}
		}

		public GuiScreenConnectingToServer(bool singleplayer, ScreenManager ScreenManager, GuiScreen parent)
			: base(ScreenManager, parent)
		{
			this.singleplayer = singleplayer;
			this._lines = new List<string>();
			if (parent != null)
			{
				this.runningGame = ((GuiScreenRunningGame)parent).runningGame;
				if (singleplayer)
				{
					if (ClientSettings.DeveloperMode)
					{
						this.ComposeDeveloperLogDialog("startingspserver", Lang.Get("Launching singleplayer server...", Array.Empty<object>()), Lang.Get("Starting server...", Array.Empty<object>()));
					}
					else
					{
						this._logToWatch = EnumLogType.StoryEvent;
						this.ComposePlayerLogDialog("startingspserver", Lang.Get("It begins...", Array.Empty<object>()));
					}
				}
				else
				{
					ElementBounds textBounds = ElementBounds.Fixed(0.0, 0.0, 900.0, 200.0);
					ElementBounds insetBounds = textBounds.ForkBoundingParent(10.0, 10.0, 10.0, 10.0);
					ElementBounds dialogBounds = insetBounds.ForkBoundingParent(0.0, 50.0, 0.0, 100.0).WithAlignment(EnumDialogArea.CenterFixed).WithFixedPosition(0.0, 280.0);
					GuiComposer elementComposer = this.ElementComposer;
					if (elementComposer != null)
					{
						elementComposer.Dispose();
					}
					this.ElementComposer = ScreenManager.GuiComposers.Create("connectingtoserver", dialogBounds).BeginChildElements(insetBounds).AddStaticCustomDraw(ElementBounds.Fill, delegate(Context ctx, ImageSurface surface, ElementBounds bounds)
					{
						GuiElement.RoundRectangle(ctx, bounds.bgDrawX, bounds.bgDrawY, bounds.OuterWidth, bounds.OuterHeight, 1.0);
						ctx.SetSourceRGBA(GuiStyle.DialogLightBgColor);
						ctx.Fill();
					})
						.AddDynamicText(Lang.Get("Connecting to multiplayer server...", Array.Empty<object>()), CairoFont.WhiteSmallishText().WithOrientation(EnumTextOrientation.Center), textBounds, "centertext")
						.EndChildElements()
						.AddButton(Lang.Get("Cancel", Array.Empty<object>()), new ActionConsumable(this.onCancel), ElementStdBounds.MenuButton(4f, EnumDialogArea.CenterFixed).WithFixedPadding(10.0, 4.0), EnumButtonStyle.Normal, "cancelButton")
						.Compose(true);
					this.ElementComposer.GetButton("cancelButton").Enabled = true;
				}
			}
			GuiScreenConnectingToServer.Logger.Debug("GuiScreenConnectingToServer constructed");
		}

		protected void LogAdded(EnumLogType type, string message, object[] args)
		{
			if (type == this._logToWatch || type == EnumLogType.Error || type == EnumLogType.Fatal)
			{
				try
				{
					string msg = string.Format(message, args);
					string line = string.Format("{0:d.M.yyyy HH:mm:ss} [{1}] {2}", DateTime.Now, type, msg);
					this._lines.Add(line);
				}
				catch (FormatException)
				{
					this._lines.Add("Couldn't write to log file, failed formatting " + message + " (FormatException)");
				}
			}
		}

		protected void ComposePlayerLogDialog(string dialogCode, string firstLine)
		{
			ElementBounds textBounds = ElementBounds.Fixed(0.0, 0.0, 900.0, 300.0);
			ElementBounds insetBounds = textBounds.ForkBoundingParent(10.0, 7.0, 10.0, 10.0);
			ElementBounds clipBounds = textBounds.FlatCopy().WithParent(insetBounds);
			clipBounds.fixedHeight -= 3.0;
			ElementBounds dialogBounds = insetBounds.ForkBoundingParent(0.0, 50.0, 26.0, 80.0).WithAlignment(EnumDialogArea.CenterFixed).WithFixedPosition(0.0, 280.0);
			ElementBounds titleBounds = ElementBounds.Fixed(0.0, -30.0, dialogBounds.fixedWidth, 28.0);
			ElementBounds buttonBounds = ElementBounds.FixedPos(EnumDialogArea.CenterBottom, 0.0, 0.0).WithFixedPadding(10.0, 2.0);
			ElementBounds scrollbarBounds = ElementBounds.Fixed(EnumDialogArea.RightFixed, 0.0, 1.0, 10.0, insetBounds.fixedHeight - 2.0);
			GuiComposer elementComposer = this.ElementComposer;
			if (elementComposer != null)
			{
				elementComposer.Dispose();
			}
			CairoFont loadingFont = CairoFont.WhiteSmallishText().WithOrientation(EnumTextOrientation.Center);
			loadingFont.Color[3] = 0.65;
			this.ElementComposer = ScreenManager.GuiComposers.Create(dialogCode, dialogBounds).BeginChildElements(insetBounds).AddStaticCustomDraw(ElementBounds.Fill, delegate(Context ctx, ImageSurface surface, ElementBounds bounds)
			{
				GuiElement.RoundRectangle(ctx, bounds.bgDrawX, bounds.bgDrawY, bounds.OuterWidth, bounds.OuterHeight, 1.0);
				ctx.SetSourceRGBA(GuiStyle.DialogLightBgColor);
				ctx.Fill();
			})
				.BeginClip(clipBounds)
				.AddDynamicText(firstLine, CairoFont.WhiteSmallishText(), textBounds, "centertext")
				.EndClip()
				.AddCompactVerticalScrollbar(new Action<float>(this.OnNewScrollbarBalue), scrollbarBounds, "scrollbar")
				.AddDynamicText(Lang.Get("Loading...", Array.Empty<object>()), loadingFont, titleBounds, null)
				.EndChildElements()
				.AddSmallButton(Lang.Get("Open Logs folder", Array.Empty<object>()), new ActionConsumable(this.onOpenLogs), buttonBounds.BelowCopy(0.0, 50.0, 0.0, 0.0), EnumButtonStyle.Normal, "logsButton")
				.AddButton((dialogCode == "startingspserver") ? Lang.Get("Cancel", Array.Empty<object>()) : Lang.Get("Force quit", Array.Empty<object>()), new ActionConsumable(this.onCancel), buttonBounds, EnumButtonStyle.Normal, "cancelButton")
				.Compose(true);
			this.ElementComposer.GetButton("cancelButton").Enabled = true;
			this.ElementComposer.GetScrollbar("scrollbar").SetHeights((float)clipBounds.fixedHeight, (float)textBounds.fixedHeight);
		}

		internal void ComposeDeveloperLogDialog(string dialogCode, string titleText, string firstLine)
		{
			ElementBounds textBounds = ElementBounds.Fixed(0.0, 0.0, 900.0, 300.0);
			ElementBounds insetBounds = textBounds.ForkBoundingParent(5.0, 5.0, 5.0, 5.0);
			ElementBounds clipBounds = textBounds.FlatCopy().WithParent(insetBounds);
			clipBounds.fixedHeight -= 3.0;
			ElementBounds dialogBounds = insetBounds.ForkBoundingParent(0.0, 50.0, 26.0, 80.0).WithAlignment(EnumDialogArea.CenterFixed).WithFixedPosition(0.0, 280.0);
			ElementBounds titleBounds = ElementBounds.Fixed(0.0, 0.0, dialogBounds.fixedWidth, 20.0);
			ElementBounds buttonBounds = ElementBounds.FixedPos(EnumDialogArea.CenterBottom, 0.0, 0.0).WithFixedPadding(10.0, 2.0);
			ElementBounds scrollbarBounds = ElementBounds.Fixed(EnumDialogArea.RightFixed, 0.0, 50.0, 20.0, insetBounds.fixedHeight);
			GuiComposer elementComposer = this.ElementComposer;
			if (elementComposer != null)
			{
				elementComposer.Dispose();
			}
			this.ElementComposer = ScreenManager.GuiComposers.Create(dialogCode, ElementBounds.Fill).AddShadedDialogBG(ElementBounds.Fill, false, 5.0, 0.75f).BeginChildElements(dialogBounds)
				.AddStaticText(titleText, CairoFont.WhiteSmallishText(), titleBounds, null)
				.AddInset(insetBounds, 3, 0.8f)
				.BeginClip(clipBounds)
				.AddDynamicText(firstLine, CairoFont.WhiteSmallishText(), textBounds, "centertext")
				.EndClip()
				.AddVerticalScrollbar(new Action<float>(this.OnNewScrollbarBalue), scrollbarBounds, "scrollbar")
				.AddSmallButton(Lang.Get("Open Logs folder", Array.Empty<object>()), new ActionConsumable(this.onOpenLogs), buttonBounds.BelowCopy(0.0, 50.0, 0.0, 0.0), EnumButtonStyle.Normal, "logsButton")
				.AddButton((dialogCode == "startingspserver") ? Lang.Get("Cancel", Array.Empty<object>()) : Lang.Get("Force quit", Array.Empty<object>()), new ActionConsumable(this.onCancel), buttonBounds, EnumButtonStyle.Normal, "cancelButton")
				.EndChildElements()
				.Compose(true);
			this.ElementComposer.GetButton("cancelButton").Enabled = true;
			this.ElementComposer.GetScrollbar("scrollbar").SetHeights((float)clipBounds.fixedHeight, (float)textBounds.fixedHeight);
		}

		private void OnNewScrollbarBalue(float value)
		{
			ElementBounds bounds = this.ElementComposer.GetDynamicText("centertext").Bounds;
			bounds.fixedY = (double)(5f - value);
			bounds.CalcWorldBounds();
		}

		private bool onOpenLogs()
		{
			NetUtil.OpenUrlInBrowser(GamePaths.Logs);
			return true;
		}

		private bool onCancel()
		{
			if (this.runningGame != null)
			{
				this.runningGame.DestroyGameSession(false);
				this.runningGame = null;
				this.ScreenManager.GamePlatform.ExitSinglePlayerServer();
			}
			if (this.singleplayer)
			{
				this.ScreenManager.LoadAndCacheScreen(typeof(GuiScreenExitingServer));
			}
			else
			{
				this.ScreenManager.LoadAndCacheScreen(typeof(GuiScreenMultiplayer));
			}
			this.ElementComposer.GetButton("cancelButton").Enabled = false;
			return true;
		}

		protected void updateLogText()
		{
			string log = string.Join("\n", this._lines);
			GuiElementDynamicText textElem = this.ElementComposer.GetDynamicText("centertext");
			GuiElementScrollbar scrollElem = this.ElementComposer.GetScrollbar("scrollbar");
			if (textElem == null)
			{
				return;
			}
			if (log.Length > 10000)
			{
				return;
			}
			textElem.SetNewText(log, true, false, false);
			if (scrollElem != null)
			{
				scrollElem.SetNewTotalHeight((float)(textElem.Bounds.OuterHeight / (double)ClientSettings.GUIScale));
				if (!scrollElem.mouseDownOnScrollbarHandle && this.prevText != log)
				{
					scrollElem.ScrollToBottom();
				}
			}
			this.prevText = log;
		}

		public override void RenderToDefaultFramebuffer(float dt)
		{
			if (!this.singleplayer || !ClientSettings.DeveloperMode)
			{
				if (!this.runningGame.BlocksReceivedAndLoaded)
				{
					this.ellapseMs = this.ScreenManager.GamePlatform.EllapsedMs;
				}
				this.ScreenManager.mainScreen.Render(dt, this.ellapseMs, true);
			}
			if (ServerMain.Logger != null && !this.loggerAdded)
			{
				this.loggerAdded = true;
				ServerMain.Logger.EntryAdded += this.LogAdded;
			}
			if (this.singleplayer && ScreenManager.Platform.EllapsedMs - this.lastLogfileCheck > 400L)
			{
				this.updateLogText();
				this.lastLogfileCheck = ScreenManager.Platform.EllapsedMs;
			}
			if (this.runningGame == null)
			{
				this.ScreenManager.StartMainMenu();
				return;
			}
			this.updateScreenUI();
			this.ElementComposer.Render(dt);
			this.ElementComposer.PostRender(dt);
			LoadedTexture versionNumberTexture = this.ScreenManager.versionNumberTexture;
			float windowSizeX = (float)this.ScreenManager.GamePlatform.WindowSize.Width;
			float windowSizeY = (float)this.ScreenManager.GamePlatform.WindowSize.Height;
			this.ScreenManager.api.renderapi.Render2DTexturePremultipliedAlpha(versionNumberTexture.TextureId, windowSizeX - (float)versionNumberTexture.Width - 10f, windowSizeY - (float)versionNumberTexture.Height - 10f, (float)versionNumberTexture.Width, (float)versionNumberTexture.Height, 50f, null);
			this.runningGame.ExecuteMainThreadTasks(dt);
			if ((!this.runningGame.IsSingleplayer || (this.runningGame.IsSingleplayer && this.runningGame.AssetsReceived && !this.runningGame.AssetLoadingOffThread && ScreenManager.Platform.IsLoadedSinglePlayerServer())) && !this.runningGame.StartedConnecting)
			{
				this.connectToGameServer();
			}
			if (this.runningGame.exitToDisconnectScreen)
			{
				this.exitToDisconnectScreen();
			}
			if (this.runningGame.exitToMainMenu)
			{
				this.exitToMainMenu();
			}
		}

		private void updateScreenUI()
		{
			long ellapsedMS = ScreenManager.Platform.EllapsedMs;
			if (this.runningGame.AssetsReceived && this.runningGame.ServerReady)
			{
				if (!this.singleplayer)
				{
					GuiElementDynamicText dynamicText = this.ElementComposer.GetDynamicText("centertext");
					if (dynamicText != null)
					{
						dynamicText.SetNewText(Lang.Get("Data received, launching client instance...", Array.Empty<object>()), false, false, false);
					}
				}
				else
				{
					if (ellapsedMS - this.lastDotsUpdate > 500L)
					{
						this.lastDotsUpdate = ellapsedMS;
						this.dotsCount = this.dotsCount % 3 + 1;
					}
					GuiElementDynamicText center = this.ElementComposer.GetDynamicText("centertext");
					if (center != null)
					{
						string msg;
						if (ClientSettings.DeveloperMode)
						{
							msg = "\n" + Lang.Get("Data received, launching single player instance...", Array.Empty<object>());
						}
						else
						{
							msg = "\n" + Lang.Get("...", Array.Empty<object>());
						}
						int dotsCountLocal = this.dotsCount;
						while (--dotsCountLocal > 0)
						{
							msg = msg + " " + Lang.Get("...", Array.Empty<object>());
						}
						center.SetNewText(this.prevText + msg, false, false, false);
					}
				}
				GuiElementDynamicText textElem = this.ElementComposer.GetDynamicText("centertext");
				GuiElementScrollbar scrollElem = this.ElementComposer.GetScrollbar("scrollbar");
				if (textElem != null && scrollElem != null)
				{
					scrollElem.SetNewTotalHeight((float)(textElem.Bounds.OuterHeight / (double)ClientSettings.GUIScale));
					if (!scrollElem.mouseDownOnScrollbarHandle)
					{
						scrollElem.ScrollToBottom();
						return;
					}
				}
			}
			else if (this.runningGame.Connectdata.ErrorMessage == null)
			{
				if (ellapsedMS - this.lastTextUpdate > 150L)
				{
					this.lastTextUpdate = ellapsedMS;
					if (this.runningGame.Connectdata.Connected)
					{
						int kbytes = this.runningGame.networkProc.TotalBytesReceivedAndReceiving / 1024;
						string text;
						if (this.runningGame.Connectdata.PositionInQueue > 0)
						{
							text = Lang.Get("connect-inqueue", new object[] { this.runningGame.Connectdata.PositionInQueue });
						}
						else
						{
							text = Lang.Get("Connected to server, downloading data...", Array.Empty<object>());
							text = text + "\n" + Lang.Get("{0} kilobyte received", new object[] { kbytes });
						}
						if (text != this.ElementComposer.GetDynamicText("centertext").GetText())
						{
							GuiScreenConnectingToServer.Logger.Notification(text);
						}
						this.ElementComposer.GetDynamicText("centertext").SetNewText(text, false, false, false);
						return;
					}
				}
			}
			else
			{
				string text2 = Lang.Get("error-connecting", new object[] { this.runningGame.Connectdata.ErrorMessage });
				if (text2 != this.ElementComposer.GetDynamicText("centertext").GetText())
				{
					GuiScreenConnectingToServer.Logger.Notification(Lang.Get("error-connecting-host", new object[]
					{
						this.runningGame.Connectdata.Host,
						this.runningGame.Connectdata.ErrorMessage
					}));
				}
				this.ElementComposer.GetDynamicText("centertext").SetNewText(text2, false, false, false);
			}
		}

		public void exitToMainMenu()
		{
			this.runningGame.Dispose();
			if (this.runningGame.IsSingleplayer && ScreenManager.Platform.IsServerRunning)
			{
				this.ScreenManager.LoadAndCacheScreen(typeof(GuiScreenExitingServer));
				return;
			}
			this.ScreenManager.StartMainMenu();
		}

		private void exitToDisconnectScreen()
		{
			ClientMain clientMain = this.runningGame;
			if (clientMain != null)
			{
				clientMain.Dispose();
			}
			GuiScreenConnectingToServer.Logger.Notification("Exiting current game");
			ClientMain clientMain2 = this.runningGame;
			if (((clientMain2 != null) ? clientMain2.disconnectAction : null) == "trydownloadmods")
			{
				ServerConnectData cdata = this.runningGame.Connectdata;
				string installPath = ((cdata.Host == null) ? GamePaths.DataPathMods : Path.Combine(GamePaths.DataPathServerMods, GamePaths.ReplaceInvalidChars(cdata.Host + "-" + cdata.Port.ToString())));
				GuiScreenDownloadMods modScreen = new GuiScreenDownloadMods(cdata, installPath, this.runningGame.disconnectMissingMods, this.ScreenManager, this.ScreenManager.mainScreen);
				modScreen.serverargs = (this.ParentScreen as GuiScreenRunningGame).serverargs;
				this.ScreenManager.LoadScreen(modScreen);
				return;
			}
			string disconnectReason = this.runningGame.disconnectReason ?? "unknown";
			ClientMain clientMain3 = this.runningGame;
			GuiScreenDisconnected disconnectScreen;
			if (((clientMain3 != null) ? clientMain3.disconnectAction : null) == "disconnectSP")
			{
				disconnectScreen = new GuiScreenDisconnected(disconnectReason, this.ScreenManager, this.ScreenManager.mainScreen, "singleplayer-disconnected");
			}
			else
			{
				disconnectScreen = new GuiScreenDisconnected(disconnectReason, this.ScreenManager, this.ScreenManager.mainScreen, "server-disconnected");
			}
			this.ScreenManager.LoadScreen(disconnectScreen);
		}

		private void connectToGameServer()
		{
			GuiScreenConnectingToServer.Logger.Debug("Opening socket to server...");
			this.runningGame.StartedConnecting = true;
			try
			{
				this.runningGame.Connect();
			}
			catch (Exception e)
			{
				GuiScreenConnectingToServer.Logger.Notification("Exiting current game");
				string msg = Lang.Get("Could not initiate connection: {0}\n\n<font color=\"#bbb\">Full Trace:\n{1}</font>", new object[]
				{
					e.Message,
					LoggerBase.CleanStackTrace(e.ToString())
				});
				GuiScreenConnectingToServer.Logger.Warning(msg.Replace("\n\n", "\n"));
				this.runningGame.Dispose();
				this.ScreenManager.LoadScreen(new GuiScreenDisconnected(msg, this.ScreenManager, this.ScreenManager.mainScreen, "server-unableconnect"));
			}
		}

		public override void Dispose()
		{
			base.Dispose();
			if (ServerMain.Logger != null)
			{
				ServerMain.Logger.EntryAdded -= this.LogAdded;
			}
			this._lines = null;
		}

		protected ClientMain runningGame;

		protected long lastLogfileCheck;

		protected long lastTextUpdate;

		private long lastDotsUpdate;

		private int dotsCount;

		private bool singleplayer;

		private string prevText;

		private long ellapseMs;

		private List<string> _lines;

		protected bool loggerAdded;

		private readonly EnumLogType _logToWatch = EnumLogType.Event;
	}
}
