using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Vintagestory.Client.NoObf
{
	internal class GuiDialogEscapeMenu : GuiDialog, IGameSettingsHandler, IGuiCompositeHandler
	{
		public bool IsIngame
		{
			get
			{
				return true;
			}
		}

		public override double InputOrder
		{
			get
			{
				return 0.0;
			}
		}

		public override double DrawOrder
		{
			get
			{
				return 0.89;
			}
		}

		public GuiComposerManager GuiComposers
		{
			get
			{
				return (this.capi.World as ClientMain).GuiComposers;
			}
		}

		public GuiComposer GuiComposerForRender
		{
			get
			{
				return base.SingleComposer;
			}
			set
			{
				base.SingleComposer = value;
			}
		}

		public int? MaxViewDistanceAlarmValue
		{
			get
			{
				if (this.game.IsSingleplayer)
				{
					return null;
				}
				return new int?((this.capi.World as ClientMain).WorldMap.MaxViewDistance);
			}
		}

		public override string ToggleKeyCombinationCode
		{
			get
			{
				return "escapemenudialog";
			}
		}

		public override bool DisableMouseGrab
		{
			get
			{
				return true;
			}
		}

		public ICoreClientAPI Api
		{
			get
			{
				return this.capi;
			}
		}

		public void LoadComposer(GuiComposer composer)
		{
			base.SingleComposer = composer;
		}

		public GuiDialogEscapeMenu(ICoreClientAPI capi)
			: base(capi)
		{
			this.gameSettingsMenu = new GuiCompositeSettings(this, false);
			this.game = capi.World as ClientMain;
			this.game.eventManager.OnGameWindowFocus.Add(new Action<bool>(this.OnWindowFocusChanged));
			this.game.eventManager.OnNewServerToClientChatLine.Add(new ChatLineDelegate(this.OnServerChatLine));
			this.EscapeMenuHome();
		}

		private void OnServerChatLine(int groupId, string message, EnumChatType chattype, string data)
		{
			if (groupId != GlobalConstants.ServerInfoChatGroup)
			{
				return;
			}
			if (!this.game.IsSingleplayer)
			{
				return;
			}
			bool flag = data != null && data.StartsWithOrdinal("foundnatdevice:");
			bool foundNatPrivate = data != null && data.StartsWithOrdinal("foundnatdeviceprivip:");
			bool notFoundNat = data != null && data.StartsWithOrdinal("nonatdevice");
			if (flag || foundNatPrivate)
			{
				string ip = data.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries)[1];
				this.internethosting = Lang.Get("singleplayer-hosting-inet", new object[]
				{
					ip,
					foundNatPrivate ? "...?" : ""
				});
				this.internethostingrevealed = Lang.Get("singleplayer-hosting-inet-iprevealed", new object[]
				{
					ip,
					foundNatPrivate ? "...?" : ""
				});
				if (foundNatPrivate)
				{
					this.internethostingWarn = true;
					this.internethostingtooltip = Lang.Get("opentolan-foundnatprivate", Array.Empty<object>());
				}
				this.EscapeMenuHome();
			}
			if (notFoundNat)
			{
				this.internethostingWarn = true;
				this.internethosting = Lang.Get("Internet hosting failed", Array.Empty<object>());
				this.internethostingtooltip = Lang.Get("No UPnP or NAT-PMP device found. Please make sure your router has UPnP enabled.", Array.Empty<object>());
				this.EscapeMenuHome();
			}
			if (data == "masterserverstatus:ok")
			{
				this.advertiseStatus = "ok";
				this.EscapeMenuHome();
			}
			if (data == "masterserverstatus:fail")
			{
				this.advertiseStatus = "fail";
				this.EscapeMenuHome();
			}
		}

		private void OnWindowFocusChanged(bool focus)
		{
			if (ClientSettings.PauseGameOnLostFocus && !focus)
			{
				this.TryOpen();
			}
		}

		public override void OnGuiOpened()
		{
			this.gameSettingsMenu.IsInCreativeMode = this.game.player.worlddata.CurrentGameMode == EnumGameMode.Creative;
			this.EscapeMenuHome();
			this.game.ShouldRender2DOverlays = true;
			if (!this.game.OpenedToLan)
			{
				this.game.PauseGame(true);
			}
		}

		internal void EscapeMenuHome()
		{
			if (this.internethosting == null || this.internethosting == "")
			{
				this.internethosting = Lang.Get("Searching for UPnP devices...", Array.Empty<object>());
			}
			this.inetHostingFont = CairoFont.WhiteSmallText();
			if (this.internethostingWarn || this.advertiseStatus == "fail")
			{
				this.inetHostingFont = this.inetHostingFont.WithColor(new double[] { 0.9450980392156862, 0.7490196078431373, 0.4666666666666667, 1.0 });
			}
			if (this.advertiseStatus == "ok")
			{
				this.inetHostingFont = this.inetHostingFont.WithColor(new double[] { 0.5254901960784314, 0.8352941176470589, 0.5490196078431373, 1.0 });
				this.internethostingtooltip = this.internethostingtooltip + ((this.internethostingtooltip.Length > 0) ? "\n\n" : "") + Lang.Get("Registration at the master server successfull and external connections are working!", Array.Empty<object>());
			}
			if (this.advertiseStatus == "fail")
			{
				this.internethostingtooltip = this.internethostingtooltip + ((this.internethostingtooltip.Length > 0) ? "\n\n" : "") + Lang.Get("Registration at the master server was not successfull, external connections probably blocked by firewall", Array.Empty<object>());
			}
			double buttonWidth = 330.0;
			float bposy = 1.5f;
			base.ClearComposers();
			GuiComposer guiComposer = this.game.GuiComposers.Create("escapemenu", ElementStdBounds.AutosizedMainDialog).AddShadedDialogBG(ElementStdBounds.DialogBackground().WithFixedPadding(GuiStyle.ElementToDialogPadding, GuiStyle.ElementToDialogPadding), false, 5.0, 0.75f).BeginChildElements()
				.AddStaticText((this.game.IsSingleplayer && !this.game.OpenedToLan) ? Lang.Get("game-ispaused", Array.Empty<object>()) : Lang.Get("game-isrunning", Array.Empty<object>()), CairoFont.WhiteSmallishText().WithFontSize(25f), EnumTextOrientation.Center, ElementStdBounds.MenuButton(0f, EnumDialogArea.CenterFixed).WithFixedWidth(buttonWidth), null)
				.AddIf(this.game.OpenedToLan)
				.AddRichtext(Lang.Get("singleplayer-hosting-local", Array.Empty<object>()), CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center), ElementStdBounds.MenuButton(0.37f, EnumDialogArea.CenterFixed).WithFixedWidth(buttonWidth), new Action<LinkTextComponent>(this.revealLocalIp), "hosttext")
				.AddHoverText(Lang.Get("game-spidergame", Array.Empty<object>()), CairoFont.WhiteDetailText(), 350, ElementStdBounds.MenuButton(0.37f, EnumDialogArea.CenterFixed).WithFixedSize(buttonWidth, 14.0), null)
				.AddIf(this.game.OpenedToInternet)
				.AddRichtext(this.internethosting, this.inetHostingFont.WithOrientation(EnumTextOrientation.Center), ElementStdBounds.MenuButton(0.61f, EnumDialogArea.CenterFixed).WithFixedWidth(buttonWidth), new Action<LinkTextComponent>(this.revealInetIp), "internethosttext")
				.AddIf(this.internethostingtooltip != null && this.internethostingtooltip.Length > 0)
				.AddHoverText(this.internethostingtooltip, CairoFont.WhiteDetailText(), 350, ElementStdBounds.MenuButton(0.61f, EnumDialogArea.CenterFixed).WithFixedSize(buttonWidth, 14.0), null)
				.EndIf()
				.EndIf()
				.EndIf()
				.AddButton(Lang.Get("pause-back2game", Array.Empty<object>()), new ActionConsumable(this.OnBackToGame), ElementStdBounds.MenuButton(1f, EnumDialogArea.CenterFixed).WithFixedWidth(buttonWidth), EnumButtonStyle.Normal, null)
				.AddButton(Lang.Get("mainmenu-settings", Array.Empty<object>()), new ActionConsumable(this.gameSettingsMenu.OpenSettingsMenu), ElementStdBounds.MenuButton(bposy, EnumDialogArea.CenterFixed).WithFixedWidth(buttonWidth), EnumButtonStyle.Normal, null)
				.Execute(delegate
				{
					bposy += 0.55f;
				})
				.AddIf(this.game.IsSingleplayer && !this.game.OpenedToLan)
				.Execute(delegate
				{
					bposy += 0.3f;
				})
				.AddButton(Lang.Get("pause-open2lan", Array.Empty<object>()), new ActionConsumable(this.onOpenToLan), ElementStdBounds.MenuButton(bposy, EnumDialogArea.CenterFixed).WithFixedWidth(buttonWidth), CairoFont.ButtonText().WithFontSize(18f), EnumButtonStyle.Normal, null)
				.EndIf()
				.AddIf(this.game.IsSingleplayer && this.game.OpenedToLan && !this.game.OpenedToInternet)
				.Execute(delegate
				{
					bposy += 0.35f;
				})
				.AddButton(Lang.Get("pause-open2internet", Array.Empty<object>()), new ActionConsumable(this.onOpenToInternet), ElementStdBounds.MenuButton(bposy, EnumDialogArea.CenterFixed).WithFixedWidth(buttonWidth), CairoFont.ButtonText().WithFontSize(18f), EnumButtonStyle.Normal, null)
				.EndIf();
			ClientPlayer player = this.game.player;
			bool flag;
			if (player == null)
			{
				flag = false;
			}
			else
			{
				IWorldPlayerData worldData = player.WorldData;
				flag = ((worldData != null) ? new EnumGameMode?(worldData.CurrentGameMode) : null).GetValueOrDefault() == EnumGameMode.Survival;
			}
			GuiComposer guiComposer2 = guiComposer.AddIf(flag).Execute(delegate
			{
				bposy += 0.45f;
			}).AddButton(Lang.Get("pause-survivalguide", Array.Empty<object>()), new ActionConsumable(this.openSurvivalGuide), ElementStdBounds.MenuButton(bposy, EnumDialogArea.CenterFixed).WithFixedWidth(buttonWidth), CairoFont.ButtonText().WithFontSize(18f), EnumButtonStyle.Normal, null)
				.EndIf();
			ClientPlayer player2 = this.game.player;
			bool flag2;
			if (player2 == null)
			{
				flag2 = false;
			}
			else
			{
				IWorldPlayerData worldData2 = player2.WorldData;
				flag2 = ((worldData2 != null) ? new EnumGameMode?(worldData2.CurrentGameMode) : null).GetValueOrDefault() == EnumGameMode.Creative;
			}
			base.SingleComposer = guiComposer2.AddIf(flag2).Execute(delegate
			{
				bposy += 0.45f;
			}).AddButton(Lang.Get("pause-commandhandbook", Array.Empty<object>()), new ActionConsumable(this.openCommandHandbook), ElementStdBounds.MenuButton(bposy, EnumDialogArea.CenterFixed).WithFixedWidth(buttonWidth), CairoFont.ButtonText().WithFontSize(18f), EnumButtonStyle.Normal, null)
				.EndIf()
				.AddButton(this.game.IsSingleplayer ? Lang.Get("pause-savequit", Array.Empty<object>()) : Lang.Get("pause-disconnect", Array.Empty<object>()), new ActionConsumable(this.OnLeaveWorld), ElementStdBounds.MenuButton(bposy + 1f, EnumDialogArea.CenterFixed).WithFixedWidth(buttonWidth), EnumButtonStyle.Normal, null)
				.EndChildElements()
				.Compose(false);
		}

		private void revealLocalIp(LinkTextComponent component)
		{
			GuiElementRichtext richtext = base.SingleComposer.GetRichtext("hosttext");
			if (richtext == null)
			{
				return;
			}
			richtext.SetNewText(Lang.Get("Hosting local game at {0}", new object[] { this.localip }), CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center), null);
		}

		private void revealInetIp(LinkTextComponent component)
		{
			GuiElementRichtext richtext = base.SingleComposer.GetRichtext("internethosttext");
			if (richtext == null)
			{
				return;
			}
			richtext.SetNewText(this.internethostingrevealed, this.inetHostingFont, null);
		}

		private bool openCommandHandbook()
		{
			new LinkTextComponent(this.Api, "none", CairoFont.SmallTextInput(), null).SetHref("commandhandbook://").Trigger();
			this.TryClose();
			if (this.capi.IsSinglePlayer && !this.capi.OpenedToLan && !this.capi.Settings.Bool["noHandbookPause"])
			{
				this.capi.PauseGame(true);
			}
			return true;
		}

		private bool openSurvivalGuide()
		{
			new LinkTextComponent(this.Api, "none", CairoFont.SmallTextInput(), null).SetHref("handbook://craftinginfo-starterguide").Trigger();
			this.TryClose();
			if (this.capi.IsSinglePlayer && !this.capi.OpenedToLan && !this.capi.Settings.Bool["noHandbookPause"])
			{
				this.capi.PauseGame(true);
			}
			return true;
		}

		private bool onOpenToLan()
		{
			this.RequireConfirm(0, Lang.Get("confirm-opentolan", Array.Empty<object>()), false);
			return true;
		}

		private bool onOpenToInternet()
		{
			this.RequireConfirm(1, Lang.Get("confirm-opentointernet", Array.Empty<object>()), true);
			return true;
		}

		private bool RequireConfirm(int type, string text, bool checkbox = false)
		{
			this.confirmType = type;
			float offY = checkbox > false;
			base.ClearComposers();
			base.SingleComposer = this.game.GuiComposers.Create("escapemenu-confirm", ElementStdBounds.AutosizedMainDialog).AddShadedDialogBG(ElementStdBounds.DialogBackground().WithFixedPadding(GuiStyle.ElementToDialogPadding, GuiStyle.ElementToDialogPadding), false, 5.0, 0.75f).BeginChildElements()
				.AddStaticText(Lang.Get("Please Confirm", Array.Empty<object>()), CairoFont.WhiteSmallishText(), ElementStdBounds.Rowed(0.1f, 0.0, EnumDialogArea.LeftFixed).WithFixedWidth(370.0), null)
				.AddStaticText(text, CairoFont.WhiteSmallishText(), ElementStdBounds.Rowed(1f, 0.0, EnumDialogArea.LeftFixed).WithFixedSize(500.0, 200.0), null)
				.AddIf(checkbox)
				.AddSwitch(delegate(bool bla)
				{
				}, ElementStdBounds.Rowed(4f, 0.0, EnumDialogArea.None), "switch", 30.0, 4.0)
				.AddStaticText(Lang.Get("Publicly advertise the server for everyone to join", Array.Empty<object>()), CairoFont.WhiteSmallishText(), ElementStdBounds.Rowed(4f, 4.0, EnumDialogArea.None).WithFixedOffset(40.0, 0.0).WithFixedWidth(450.0), null)
				.EndIf()
				.AddButton(Lang.Get("Cancel", Array.Empty<object>()), new ActionConsumable(this.OnCancel), ElementStdBounds.Rowed(4.3f + offY, 0.0, EnumDialogArea.LeftFixed).WithFixedPadding(10.0, 2.0).WithFixedAlignmentOffset(-10.0, 0.0), EnumButtonStyle.Normal, null)
				.AddButton(Lang.Get("Confirm", Array.Empty<object>()), new ActionConsumable(this.OnConfirm), ElementStdBounds.Rowed(4.3f + offY, 0.0, EnumDialogArea.RightFixed).WithFixedPadding(10.0, 2.0), EnumButtonStyle.Normal, null)
				.EndChildElements()
				.Compose(false);
			if (checkbox)
			{
				base.SingleComposer.GetSwitch("switch").On = true;
			}
			return true;
		}

		private bool OnConfirm()
		{
			if (this.confirmType == 0)
			{
				this.game.OpenedToLan = true;
				this.game.PauseGame(false);
				this.EscapeMenuHome();
				this.game.SendPacketClient(ClientPackets.Chat(GlobalConstants.GeneralChatGroup, "/allowlan 1", null));
			}
			if (this.confirmType == 1)
			{
				bool advertise = base.SingleComposer.GetSwitch("switch").On;
				this.game.OpenedToInternet = true;
				this.EscapeMenuHome();
				this.game.SendPacketClient(ClientPackets.Chat(GlobalConstants.GeneralChatGroup, "/upnp 1", null));
				this.game.SendPacketClient(ClientPackets.Chat(GlobalConstants.GeneralChatGroup, "/serverconfig advertise " + (advertise ? "1" : "0"), null));
			}
			return true;
		}

		private bool OnCancel()
		{
			this.EscapeMenuHome();
			return true;
		}

		public bool OnBackPressed()
		{
			this.EscapeMenuHome();
			return true;
		}

		internal bool OnBackToGame()
		{
			this.game.Logger.VerboseDebug("Back to game clicked");
			this.TryClose();
			this.game.Logger.VerboseDebug("Escape menu closed");
			return true;
		}

		public override void OnGuiClosed()
		{
			this.game.api.eventapi.PushEvent("leftGraphicsDlg", null);
			base.OnGuiClosed();
			this.game.PauseGame(false);
		}

		internal bool OnLeaveWorld()
		{
			this.game.SendLeave(0);
			this.game.exitReason = "leave world button pressed";
			this.game.DestroyGameSession(false);
			return true;
		}

		public override bool OnEscapePressed()
		{
			return !this.gameSettingsMenu.IsCapturingHotKey && base.OnEscapePressed();
		}

		internal override bool OnKeyCombinationToggle(KeyCombination viaKeyComb)
		{
			return (this.IsOpened() || this.game.DialogsOpened == 0) && base.OnKeyCombinationToggle(viaKeyComb);
		}

		public override bool CaptureAllInputs()
		{
			return this.IsOpened();
		}

		public override bool CaptureRawMouse()
		{
			return this.IsOpened();
		}

		bool IGameSettingsHandler.LeaveSettingsMenu()
		{
			this.EscapeMenuHome();
			return true;
		}

		public override void OnKeyDown(KeyEvent args)
		{
			this.gameSettingsMenu.OnKeyDown(args);
			base.OnKeyDown(args);
			args.Handled = true;
		}

		public override void OnKeyUp(KeyEvent args)
		{
			this.gameSettingsMenu.OnKeyUp(args);
			base.OnKeyUp(args);
			args.Handled = true;
		}

		public override void OnMouseDown(MouseEvent args)
		{
			this.gameSettingsMenu.OnMouseDown(args);
			base.OnMouseDown(args);
			args.Handled = true;
		}

		public override void OnMouseUp(MouseEvent args)
		{
			this.gameSettingsMenu.OnMouseUp(args);
			base.OnMouseUp(args);
			args.Handled = true;
		}

		public void ReloadShaders()
		{
			ShaderRegistry.ReloadShaders();
			ClientEventManager eventManager = this.game.eventManager;
			if (eventManager == null)
			{
				return;
			}
			eventManager.TriggerReloadShaders();
		}

		public override void OnRenderGUI(float deltaTime)
		{
			GuiComposer singleComposer = base.SingleComposer;
			bool nowInGraphics = singleComposer != null && singleComposer.DialogName.StartsWithOrdinal("gamesettings-graphics");
			if (this.wasInGraphics != nowInGraphics)
			{
				if (!nowInGraphics)
				{
					this.game.api.eventapi.PushEvent("leftGraphicsDlg", null);
				}
				else
				{
					this.game.api.eventapi.PushEvent("enteredGraphicsDlg", null);
				}
				this.wasInGraphics = nowInGraphics;
			}
			base.OnRenderGUI(deltaTime);
		}

		public override void OnFinalizeFrame(float dt)
		{
			GuiCompositeSettings guiCompositeSettings = this.gameSettingsMenu;
			if (guiCompositeSettings != null)
			{
				guiCompositeSettings.Refresh();
			}
			base.OnFinalizeFrame(dt);
		}

		public override void Dispose()
		{
			GuiComposer cp;
			if ((this.capi.World as ClientMain).GuiComposers.Composers.TryGetValue("gamesettings-graphicsingame", out cp))
			{
				cp.Dispose();
			}
			if ((this.capi.World as ClientMain).GuiComposers.Composers.TryGetValue("gamesettings-controlsingame", out cp))
			{
				cp.Dispose();
			}
			if ((this.capi.World as ClientMain).GuiComposers.Composers.TryGetValue("gamesettings-interfaceoptionsingame", out cp))
			{
				cp.Dispose();
			}
			if ((this.capi.World as ClientMain).GuiComposers.Composers.TryGetValue("gamesettings-soundoptionsingame", out cp))
			{
				cp.Dispose();
			}
			if ((this.capi.World as ClientMain).GuiComposers.Composers.TryGetValue("gamesettings-developeroptionsingame", out cp))
			{
				cp.Dispose();
			}
			base.Dispose();
		}

		public GuiComposer dialogBase(string name, double width = -1.0, double height = -1.0)
		{
			throw new NotImplementedException();
		}

		public void OnMacroEditor()
		{
			this.TryClose();
			GuiDialog guiDialog = this.game.LoadedGuis.FirstOrDefault((GuiDialog dlg) => dlg is GuiDialogMacroEditor);
			if (guiDialog == null)
			{
				return;
			}
			guiDialog.TryOpen();
		}

		private GuiCompositeSettings gameSettingsMenu;

		private string internethosting = "";

		private string internethostingrevealed;

		private string internethostingtooltip = "";

		private bool internethostingWarn;

		private string advertiseStatus;

		private ClientMain game;

		private string localip = RuntimeEnv.GetLocalIpAddress();

		private CairoFont inetHostingFont;

		private int confirmType;

		private bool wasInGraphics;
	}
}
