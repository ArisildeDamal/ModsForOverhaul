using System;
using System.Collections.Generic;
using System.Text;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;
using Vintagestory.ModDb;

namespace Vintagestory.Client
{
	public class GuiScreenDownloadMods : GuiScreen
	{
		public GuiScreenDownloadMods(ServerConnectData connectdata, string installPath, List<string> modsToDownload, ScreenManager screenManager, GuiScreen parentScreen)
			: base(screenManager, parentScreen)
		{
			this.connectdata = connectdata;
			this.modsToDownload = modsToDownload;
			this.installPath = installPath;
			this.dlType = EnumDownloadModType.ServerRequiredMods;
			if (connectdata == null)
			{
				this.dlType = EnumDownloadModType.SelectiveInstall;
			}
			else if (connectdata.Host == null)
			{
				this.dlType = EnumDownloadModType.ResolveDependencies;
			}
			ScreenManager.GuiComposers.ClearCache();
			ElementBounds titleBounds = ElementBounds.Fixed(EnumDialogArea.LeftFixed, 0.0, 0.0, 650.0, 30.0);
			ElementBounds textBounds = ElementBounds.Fixed(EnumDialogArea.LeftFixed, 0.0, 0.0, 650.0, 360.0).FixedUnder(titleBounds, 0.0);
			ElementBounds dlgBounds = textBounds.ForkBoundingParent(15.0, 15.0, 15.0, 15.0).WithAlignment(EnumDialogArea.CenterMiddle);
			string text = "";
			string title = "";
			switch (this.dlType)
			{
			case EnumDownloadModType.ServerRequiredMods:
				title = Lang.Get("downloadmods-title-serverinstall", Array.Empty<object>());
				text = Lang.Get("downloadmods-serverinstall", new object[] { modsToDownload.Count });
				break;
			case EnumDownloadModType.SelectiveInstall:
				title = Lang.Get("downloadmods-title-selectinstall", Array.Empty<object>());
				text = Lang.Get("downloadmods-selectinstall", new object[] { modsToDownload[0] });
				break;
			case EnumDownloadModType.ResolveDependencies:
				title = Lang.Get("downloadmods-title-dependencyinstall", Array.Empty<object>());
				text = Lang.Get("downloadmods-dependencyinstall", new object[] { string.Join(", ", new string[] { modsToDownload[0] }) });
				break;
			}
			this.ElementComposer = ScreenManager.GuiComposers.Create("mainmenu-downloadmods", dlgBounds).AddShadedDialogBG(ElementBounds.Fill, false, 5.0, 1f).BeginChildElements(textBounds)
				.AddRichtext(title, CairoFont.WhiteSmallishText().WithWeight(FontWeight.Bold), titleBounds, new Action<LinkTextComponent>(this.didClickLink), "titleText")
				.AddRichtext(text + "\r\n\r\n" + Lang.Get("downloadmods-disclaimer", Array.Empty<object>()), CairoFont.WhiteSmallishText(), textBounds.ForkChild().WithFixedPosition(0.0, 25.0), new Action<LinkTextComponent>(this.didClickLink), "centertext")
				.AddButton(Lang.Get("Cancel", Array.Empty<object>()), new ActionConsumable(this.OnCancel), ElementStdBounds.Rowed(4.5f, 0.0, EnumDialogArea.LeftFixed).WithFixedPadding(10.0, 2.0), EnumButtonStyle.Normal, null)
				.AddButton(Lang.Get("Download mods", Array.Empty<object>()), new ActionConsumable(this.OnConfirm), ElementStdBounds.Rowed(4.5f, 0.0, EnumDialogArea.RightFixed).WithFixedPadding(10.0, 2.0), EnumButtonStyle.Normal, "confirmButton")
				.EndChildElements()
				.Compose(true);
		}

		private bool OnCancel()
		{
			this.ScreenManager.StartMainMenu();
			this.ScreenManager.guiMainmenuLeft.OnMultiplayer();
			return true;
		}

		private bool OnConfirm()
		{
			ElementBounds dialogBounds = ElementBounds.Fixed(0.0, 50.0, 800.0, 390.0).WithAlignment(EnumDialogArea.CenterFixed).WithFixedPosition(0.0, 150.0);
			ElementBounds textBounds = ElementBounds.Fixed(0.0, 0.0, 790.0, 300.0);
			ElementBounds insetBounds = textBounds.ForkBoundingParent(5.0, 5.0, 5.0, 5.0);
			ElementBounds clipBounds = textBounds.FlatCopy().WithParent(insetBounds);
			clipBounds.fixedHeight -= 3.0;
			ElementBounds scrollbarBounds = ElementBounds.Fixed(EnumDialogArea.RightFixed, 0.0, 1.0, 10.0, insetBounds.fixedHeight - 2.0);
			ElementBounds titleBounds = ElementBounds.Fixed(0.0, -30.0, dialogBounds.fixedWidth, 28.0);
			CairoFont loadingFont = CairoFont.WhiteSmallishText().WithOrientation(EnumTextOrientation.Center);
			loadingFont.Color[3] = 0.65;
			this.modsLeftToDownload = this.modsToDownload.Count;
			this.modsToDownloadTotal = this.modsToDownload.Count;
			ElementBounds cancelBounds = ElementBounds.Fixed(0, 30).FixedUnder(insetBounds, 0.0).WithFixedPadding(10.0, 2.0)
				.WithAlignment(EnumDialogArea.LeftFixed);
			ElementBounds joinBounds = ElementBounds.Fixed(0, 30).FixedUnder(insetBounds, 0.0).WithAlignment(EnumDialogArea.RightFixed)
				.WithFixedAlignmentOffset(-10.0, 0.0)
				.WithFixedPadding(10.0, 2.0);
			ServerConnectData serverConnectData = this.connectdata;
			string btnText = Lang.Get((((serverConnectData != null) ? serverConnectData.Host : null) == null) ? "Continue" : "Join Server", Array.Empty<object>());
			this.ElementComposer = ScreenManager.GuiComposers.Create("downloadmods", ElementBounds.Fill).AddShadedDialogBG(ElementBounds.Fill, false, 5.0, 1f).BeginChildElements(dialogBounds)
				.AddDynamicText(Lang.Get("Attempting to download {0} mods...", new object[] { this.modsLeftToDownload }), loadingFont, titleBounds, "titleText")
				.AddInset(insetBounds, 3, 0.8f)
				.BeginClip(clipBounds)
				.AddRichtext("", CairoFont.WhiteSmallText(), textBounds, "logText")
				.EndClip()
				.AddVerticalScrollbar(new Action<float>(this.OnNewScrollbarBalue), scrollbarBounds, "scrollbar")
				.AddButton(Lang.Get("Cancel", Array.Empty<object>()), new ActionConsumable(this.OnCancel), cancelBounds, EnumButtonStyle.Normal, null)
				.AddButton(btnText, new ActionConsumable(this.OnJoin), joinBounds, EnumButtonStyle.Normal, "joinBtn")
				.EndChildElements()
				.Compose(true);
			this.modUtil = new ModDbUtil(this.ScreenManager.api, ClientSettings.ModDbUrl, this.installPath);
			this.ElementComposer.GetButton("joinBtn").Enabled = false;
			textBounds.CalcWorldBounds();
			clipBounds.CalcWorldBounds();
			this.ElementComposer.GetScrollbar("scrollbar").SetHeights((float)clipBounds.fixedHeight, (float)textBounds.fixedHeight);
			return true;
		}

		public override void RenderToPrimary(float dt)
		{
			base.RenderToPrimary(dt);
			if (this.modUtil != null && this.modsToDownload.Count > 0 && !this.modUtil.IsLoading)
			{
				this.waitcounter++;
				if (this.waitcounter > 2)
				{
					string mod = this.modsToDownload[0];
					this.modsToDownload.RemoveAt(0);
					this.modUtil.SearchAndInstall(mod, "1.21.5", new ModInstallProgressUpdate(this.onProgressUpdate), false);
					this.waitcounter = 0;
				}
			}
		}

		private bool OnJoin()
		{
			this.ScreenManager.loadMods();
			if (this.connectdata == null)
			{
				this.ScreenManager.StartMainMenu();
			}
			else if (this.connectdata.Host == null)
			{
				this.ScreenManager.ConnectToSingleplayer(this.serverargs);
			}
			else
			{
				this.ScreenManager.ConnectToMultiplayer(this.connectdata.HostRaw, this.connectdata.ServerPassword);
			}
			return true;
		}

		private void OnNewScrollbarBalue(float value)
		{
			ElementBounds bounds = this.ElementComposer.GetRichtext("logText").Bounds;
			bounds.fixedY = (double)(5f - value);
			bounds.CalcWorldBounds();
		}

		private void onProgressUpdate(string message, EnumModInstallState state)
		{
			if (state == EnumModInstallState.InProgress || state == EnumModInstallState.Offline)
			{
				this.logText.Append(message);
			}
			else
			{
				this.logText.AppendLine(message);
			}
			if (state != EnumModInstallState.InProgress)
			{
				this.modsLeftToDownload--;
				this.ElementComposer.GetDynamicText("titleText").SetNewText(Lang.Get("Attempting to download {0}/{1} mods...", new object[]
				{
					this.modsToDownloadTotal - this.modsLeftToDownload,
					this.modsToDownloadTotal
				}), false, false, false);
				if (state != EnumModInstallState.InstalledOrReady)
				{
					this.errorCount++;
				}
			}
			if (this.modsLeftToDownload == 0 && this.errorCount > 0)
			{
				this.logText.AppendLine("\r\n" + Lang.Get("Unable to download some mods from the mod database. You'll have to manually install {0} mods. Sorry!", new object[] { this.errorCount }));
			}
			if (this.modsLeftToDownload == 0 && this.errorCount == 0)
			{
				this.ElementComposer.GetButton("joinBtn").Enabled = true;
				StringBuilder stringBuilder = this.logText;
				string text = "\r\n";
				ServerConnectData serverConnectData = this.connectdata;
				stringBuilder.AppendLine(text + ((((serverConnectData != null) ? serverConnectData.Host : null) == null) ? Lang.Get("All mods downloaded, ready to continue!", Array.Empty<object>()) : Lang.Get("All mods downloaded, ready to join this server!", Array.Empty<object>())));
			}
			this.ElementComposer.GetRichtext("logText").SetNewText(this.logText.ToString(), CairoFont.WhiteSmallText(), null);
			this.ScreenManager.GamePlatform.Logger.Notification(this.logText.ToString());
			GuiElementScrollbar scrollElem = this.ElementComposer.GetScrollbar("scrollbar");
			GuiElementRichtext textElem = this.ElementComposer.GetRichtext("logText");
			scrollElem.SetNewTotalHeight((float)(textElem.Bounds.OuterHeight / (double)ClientSettings.GUIScale));
			if (!scrollElem.mouseDownOnScrollbarHandle)
			{
				scrollElem.ScrollToBottom();
			}
		}

		private void didClickLink(LinkTextComponent link)
		{
			this.ScreenManager.StartMainMenu();
			ScreenManager.EnqueueMainThreadTask(delegate
			{
				this.ScreenManager.api.Gui.OpenLink(link.Href);
			});
		}

		public override void RenderToDefaultFramebuffer(float dt)
		{
			if (ScreenManager.KeyboardKeyState[50])
			{
				this.ScreenManager.StartMainMenu();
				return;
			}
			this.ElementComposer.Render(dt);
			this.ScreenManager.RenderMainMenuParts(dt, this.ElementComposer.Bounds, false, true);
			if (this.ScreenManager.mainMenuComposer.MouseOverCursor != null)
			{
				this.FocusedMouseCursor = this.ScreenManager.mainMenuComposer.MouseOverCursor;
			}
			this.ElementComposer.PostRender(dt);
		}

		private ServerConnectData connectdata;

		private List<string> modsToDownload;

		private string installPath;

		private StringBuilder logText = new StringBuilder();

		internal StartServerArgs serverargs;

		private EnumDownloadModType dlType;

		private int modsToDownloadTotal;

		private int modsLeftToDownload;

		private int errorCount;

		private ModDbUtil modUtil;

		private int waitcounter;
	}
}
