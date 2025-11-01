using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.Client.Util;
using Vintagestory.Common;

namespace Vintagestory.Client
{
	public class GuiScreenServerDashboard : GuiScreen
	{
		private bool onDownloadWorldNow()
		{
			if (this.gameServerStatus.DownloadState == EnumDownloadSavesStatus.Ready)
			{
				this.ScreenManager.api.Gui.OpenLink(this.gameServerStatus.Downloadsavefilename);
			}
			return true;
		}

		private bool onRequestDownload()
		{
			this.currentScreen = "confirmdownload";
			this.ScreenManager.LoadScreen(new GuiScreenConfirmAction(Lang.Get("This action will create a downloadable copy of your savegame and player data. It can be requested once every 24 hours and the downloadable copy will stay online for 24 hours as well. Preparing the copy takes a few minutes, check server dashboard see the current copy status.", Array.Empty<object>()), new Action<bool>(this.OnDidConfirmDownlod), this.ScreenManager, this, false));
			return true;
		}

		private void OnDidConfirmDownlod(bool confirm)
		{
			if (!confirm)
			{
				this.ScreenManager.LoadScreenNoLoadCall(this);
				return;
			}
			this.backend.RequestDownload(delegate(EnumAuthServerResponse status, GameServerStatus response)
			{
				this.gameServerStatus = response;
				this.ScreenManager.LoadScreenNoLoadCall(this);
				if (this.gameServerStatus.ActiveserverDays <= 0f)
				{
					this.screenServerExpired(this.gameServerStatus.ActiveserverDays);
				}
				else
				{
					this.screenServerStatus(response);
				}
				this.dlStatusProbingTries = 12;
				this.runDownloadStatusProber();
			});
		}

		private void runDownloadStatusProber()
		{
			if (this.gameServerStatus.DownloadState == EnumDownloadSavesStatus.Ready && this.ElementComposer.GetRichtext("dlStatusText") != null)
			{
				this.ElementComposer.GetRichtext("dlStatusText").SetNewText(Lang.Get("Your world download is ready, please download it within 24 hours.", Array.Empty<object>()), CairoFont.WhiteDetailText(), null);
			}
			if (this.gameServerStatus.DownloadState == EnumDownloadSavesStatus.Copying)
			{
				if (this.ElementComposer.GetRichtext("dlStatusText") != null)
				{
					string[] downloadStateStrs = new string[]
					{
						Lang.Get("World download requested, copying in progress.", Array.Empty<object>()),
						Lang.Get("World download requested, copying in progress..", Array.Empty<object>()),
						Lang.Get("World download requested, copying in progress...", Array.Empty<object>())
					};
					this.ElementComposer.GetRichtext("dlStatusText").SetNewText(downloadStateStrs[GameMath.Mod(this.dlStatusProbingTries, 3)], CairoFont.WhiteDetailText(), null);
				}
				if (!this.CallbackEnqueued)
				{
					this.CallbackEnqueued = true;
					ScreenManager.EnqueueCallBack(delegate
					{
						this.backend.GetStatus(delegate(EnumAuthServerResponse rs, GameServerStatus gs)
						{
							if (rs == EnumAuthServerResponse.Good)
							{
								if (this.currentScreen != "dashboard")
								{
									this.gameServerStatus = gs;
								}
								else
								{
									this.onStatusReady(rs, gs);
								}
							}
							this.CallbackEnqueued = false;
							this.runDownloadStatusProber();
						});
					}, 5000);
				}
				this.dlStatusProbingTries--;
			}
			if (this.ElementComposer.GetButton("worldDownloadButton") != null)
			{
				this.ElementComposer.GetButton("worldDownloadButton").Enabled = this.gameServerStatus.DownloadState == EnumDownloadSavesStatus.Ready;
			}
		}

		private void screenSelectRegion(GameServerStatus status)
		{
			this.currentScreen = "selectregion";
			ElementBounds rowLeft = ElementBounds.Fixed(0.0, 50.0, 300.0, 35.0);
			this.ElementComposer = this.screenBase(true).AddStaticText(Lang.Get("Please select the server region. This may take up to 40 seconds after confirming.", Array.Empty<object>()), CairoFont.WhiteSmallText(), rowLeft = rowLeft.BelowCopy(0.0, 30.0, 0.0, 0.0), null).AddDropDown(status.Regions, status.Regions, 0, null, rowLeft = rowLeft.BelowCopy(0.0, 30.0, 0.0, 0.0), "regionSelect")
				.AddButton(Lang.Get("Confirm", Array.Empty<object>()), new ActionConsumable(this.onConfirmRegion), rowLeft.BelowCopy(0.0, 30.0, 0.0, 0.0), EnumButtonStyle.Normal, "saveButton")
				.EndChildElements()
				.Compose(true);
		}

		private bool onConfirmRegion()
		{
			this.ElementComposer.GetButton("saveButton").Enabled = false;
			this.backend.SelectRegion(this.ElementComposer.GetDropDown("regionSelect").SelectedValue, new OnSrvActionComplete<ServerCtrlResponse>(this.onRegionSelected));
			return true;
		}

		private void onRegionSelected(EnumAuthServerResponse reqStatus, ServerCtrlResponse response)
		{
			this.setResponseNotifier(reqStatus, (response != null) ? response.Reason : null);
			if (reqStatus != EnumAuthServerResponse.Good)
			{
				this.ElementComposer.GetButton("saveButton").Enabled = true;
				return;
			}
			if (response.StatusCode == "ok" || response.StatusCode == "success")
			{
				this.backend.GetStatus(new OnSrvActionComplete<GameServerStatus>(this.onStatusReady));
				this.screenLoading();
				return;
			}
			this.setResponseNotifier(EnumAuthServerResponse.Bad, response.Code);
		}

		private void screenSelectVersion(string[] versions)
		{
			this.currentScreen = "selectversion";
			ElementBounds rowLeft = ElementBounds.Fixed(0.0, 50.0, 300.0, 35.0);
			string[] versionnames = versions;
			int index = versions.IndexOf("1.21.5");
			if (index >= 0)
			{
				versionnames = (string[])versions.Clone();
				string[] array = versionnames;
				int num = index;
				array[num] = array[num] + " " + Lang.Get("(recommended)", Array.Empty<object>());
			}
			this.ElementComposer = this.screenBase(true).AddStaticText(Lang.Get("Version Selector", Array.Empty<object>()), CairoFont.WhiteSmallishText(), rowLeft = rowLeft.BelowCopy(0.0, -10.0, 0.0, 0.0), null).AddStaticText(Lang.Get("Please select the server version you wish to change to.", Array.Empty<object>()), CairoFont.WhiteSmallText(), rowLeft = rowLeft.BelowCopy(0.0, 10.0, 0.0, 0.0).WithFixedWidth(500.0), null)
				.AddDropDown(versions, versionnames, index, null, rowLeft = rowLeft.BelowCopy(0.0, 10.0, 0.0, 0.0).WithFixedWidth(300.0), "versionSelect")
				.AddIf(this.showCancelOnSelectVersion)
				.AddButton(Lang.Get("Cancel", Array.Empty<object>()), new ActionConsumable(this.onCancel), rowLeft = rowLeft.BelowCopy(0.0, 100.0, 0.0, 0.0).WithFixedWidth(300.0), EnumButtonStyle.Normal, "cancelButton")
				.AddButton(Lang.Get("Confirm", Array.Empty<object>()), new ActionConsumable(this.onConfirmVersion), rowLeft = rowLeft.RightCopy(10.0, 0.0, 0.0, 0.0), EnumButtonStyle.Normal, "saveButton")
				.EndIf()
				.AddIf(!this.showCancelOnSelectVersion)
				.AddButton(Lang.Get("Confirm", Array.Empty<object>()), new ActionConsumable(this.onConfirmVersion), rowLeft.BelowCopy(0.0, 100.0, 0.0, 0.0), EnumButtonStyle.Normal, "saveButton")
				.EndIf()
				.EndChildElements()
				.Compose(true);
		}

		private bool onConfirmVersion()
		{
			this.ElementComposer.GetButton("saveButton").Enabled = false;
			this.backend.SelectVersion(this.ElementComposer.GetDropDown("versionSelect").SelectedValue, new OnSrvActionComplete<ServerCtrlResponse>(this.onRegionSelected));
			return true;
		}

		private bool onConfigureServer()
		{
			this.dashboardLoading();
			this.backend.GetConfig(new OnSrvActionComplete<GameServerConfigResponse>(this.onServerConfigReceived));
			return true;
		}

		private void onServerConfigReceived(EnumAuthServerResponse reqStatus, GameServerConfigResponse response)
		{
			this.currentScreen = "settings";
			if (reqStatus == EnumAuthServerResponse.Bad)
			{
				this.setResponseNotifier(reqStatus, null);
				return;
			}
			this.dashboardReady();
			ElementBounds rowLeft = ElementBounds.Fixed(0.0, 50.0, 200.0, 25.0);
			ElementBounds rowRight = ElementBounds.Fixed(0.0, 50.0, 400.0, 25.0).FixedRightOf(rowLeft, 0.0);
			ServerConfigPart scfg = response.ServerConfig;
			string[] langVals;
			string[] langNames;
			GuiCompositeSettings.getLanguages(out langVals, out langNames);
			int langIndex = langVals.IndexOf(scfg.ServerLanguage);
			this.ElementComposer = this.screenBase(false).AddStaticText(Lang.Get("Server configuration", Array.Empty<object>()), CairoFont.WhiteSmallishText(), rowLeft = rowLeft.BelowCopy(0.0, -25.0, 0.0, 0.0), null).AddStaticText(Lang.Get("Server Name", Array.Empty<object>()), CairoFont.WhiteSmallText(), rowLeft = rowLeft.BelowCopy(0.0, 10.0, 0.0, 0.0), null)
				.AddTextInput(rowRight = rowRight.BelowCopy(0.0, 5.0, 0.0, 0.0), null, CairoFont.WhiteSmallText(), "serverName")
				.AddStaticText(Lang.Get("Server description", Array.Empty<object>()), CairoFont.WhiteSmallText(), rowLeft = rowLeft.BelowCopy(0.0, 10.0, 0.0, 0.0), null)
				.AddTextArea(rowRight = rowRight.BelowCopy(0.0, 15.0, 0.0, 0.0).WithFixedHeight(100.0), null, CairoFont.WhiteSmallText(), "serverDescription")
				.AddStaticText(Lang.Get("Welcome message", Array.Empty<object>()), CairoFont.WhiteSmallText(), rowLeft = rowLeft.BelowCopy(0.0, 92.0, 0.0, 0.0), null)
				.AddTextInput(rowRight = rowRight.BelowCopy(0.0, 15.0, 0.0, 0.0).WithFixedHeight(25.0), null, CairoFont.WhiteSmallText(), "serverMotd")
				.AddStaticText(Lang.Get("Login password", Array.Empty<object>()), CairoFont.WhiteSmallText(), rowLeft = rowLeft.BelowCopy(0.0, 15.0, 0.0, 0.0), null)
				.AddTextInput(rowRight = rowRight.BelowCopy(0.0, 15.0, 0.0, 0.0).WithFixedHeight(25.0), null, CairoFont.WhiteSmallText(), "serverPassword")
				.AddStaticText(Lang.Get("Language", Array.Empty<object>()), CairoFont.WhiteSmallText(), rowLeft = rowLeft.BelowCopy(0.0, 15.0, 0.0, 0.0), null)
				.AddDropDown(langVals, langNames, langIndex, null, rowRight = rowRight.BelowCopy(0.0, 15.0, 0.0, 0.0), CairoFont.WhiteSmallText(), "language")
				.AddStaticText(Lang.Get("Allow PvP", Array.Empty<object>()), CairoFont.WhiteSmallText(), rowLeft = rowLeft.BelowCopy(0.0, 20.0, 0.0, 0.0), null)
				.AddSwitch(null, rowRight = rowRight.BelowCopy(0.0, 15.0, 0.0, 0.0), "serverPvp", 30.0, 4.0)
				.AddStaticText(Lang.Get("Allow Fire Spread", Array.Empty<object>()), CairoFont.WhiteSmallText(), rowLeft = rowLeft.BelowCopy(0.0, 20.0, 0.0, 0.0), null)
				.AddSwitch(null, rowRight = rowRight.BelowCopy(0.0, 15.0, 0.0, 0.0), "serverFireSpread", 30.0, 4.0)
				.AddStaticText(Lang.Get("On the public server list", Array.Empty<object>()), CairoFont.WhiteSmallText(), rowLeft = rowLeft.BelowCopy(0.0, 20.0, 0.0, 0.0), null)
				.AddSwitch(null, rowRight.BelowCopy(0.0, 15.0, 0.0, 0.0), "advertise", 30.0, 4.0)
				.AddSmallButton(Lang.Get("Cancel", Array.Empty<object>()), new ActionConsumable(this.onCancel), rowLeft = rowLeft.BelowCopy(0.0, 40.0, 0.0, 0.0).WithFixedHeight(35.0), EnumButtonStyle.Normal, "saveButton")
				.AddSmallButton(Lang.Get("Save", Array.Empty<object>()), new ActionConsumable(this.onSaveServerConfig), rowLeft.RightCopy(10.0, 0.0, 0.0, 0.0).WithFixedHeight(35.0), EnumButtonStyle.Normal, "cancelButton")
				.AddSmallButton(Lang.Get("Request World Download", Array.Empty<object>()), new ActionConsumable(this.onRequestDownload), rowLeft = rowLeft.BelowCopy(0.0, 40.0, 0.0, 0.0), EnumButtonStyle.Small, "serverDownloadWorld")
				.AddSmallButton(Lang.Get("Change Server Version", Array.Empty<object>()), new ActionConsumable(this.onChangeVersion), rowLeft.RightCopy(10.0, 0.0, 0.0, 0.0), EnumButtonStyle.Small, "serverChangeVersion")
				.AddSmallButton(Lang.Get("Delete Savegame", Array.Empty<object>()), new ActionConsumable(this.onDeleteSaves), rowLeft = rowLeft.BelowCopy(0.0, 10.0, 0.0, 0.0).WithFixedHeight(25.0), EnumButtonStyle.Small, "deleteButton")
				.AddSmallButton(Lang.Get("Delete everything", Array.Empty<object>()), new ActionConsumable(this.onDeleteEverything), rowLeft.RightCopy(10.0, 0.0, 0.0, 0.0), EnumButtonStyle.Small, "deleteallButton")
				.EndChildElements();
			this.ElementComposer.Compose(true);
			this.ElementComposer.GetTextInput("serverName").SetValue(scfg.ServerName, true);
			this.ElementComposer.GetTextArea("serverDescription").SetMaxLines(5);
			this.ElementComposer.GetTextArea("serverDescription").SetValue(scfg.ServerDescription, true);
			this.ElementComposer.GetTextInput("serverPassword").SetValue(scfg.Password, true);
			this.ElementComposer.GetTextInput("serverMotd").SetValue(scfg.WelcomeMessage, true);
			this.ElementComposer.GetSwitch("serverPvp").SetValue(scfg.AllowPvP);
			this.ElementComposer.GetSwitch("serverFireSpread").SetValue(scfg.AllowFireSpread);
			this.ElementComposer.GetSwitch("advertise").SetValue(scfg.AdvertiseServer);
			this.ElementComposer.GetButton("serverDownloadWorld").Enabled = this.gameServerStatus.DownloadState == EnumDownloadSavesStatus.Idle;
			if (this.gameServerStatus.QuantitySavegames == 0)
			{
				this.ElementComposer.GetButton("deleteButton").Enabled = false;
			}
		}

		private void OnRemoveAllMods(bool b)
		{
			this.dashboardLoading();
			this.backend.DeleteAllMods(delegate(EnumAuthServerResponse _, ServerCtrlResponse _)
			{
				this.backend.GetConfig(new OnSrvActionComplete<GameServerConfigResponse>(this.onServerConfigReceived));
			});
		}

		private void OnRemoveSelectedMod(bool b)
		{
			GuiElementDropDown mods = this.ElementComposer.GetDropDown("mods");
			this.dashboardLoading();
			this.backend.DeleteMod(mods.SelectedValue, delegate(EnumAuthServerResponse _, ServerCtrlResponse _)
			{
				this.backend.GetConfig(new OnSrvActionComplete<GameServerConfigResponse>(this.onServerConfigReceived));
			});
		}

		private bool onDeleteEverything()
		{
			this.currentScreen = "confirmdeleteall";
			this.ScreenManager.LoadScreen(new GuiScreenConfirmAction(Lang.Get("Really delete your Vintagehosting server and worlds? This action cannot be undone! This action can take up to 30 seconds.", Array.Empty<object>()), new Action<bool>(this.OnDidConfirmDeleteAll), this.ScreenManager, this, false));
			return true;
		}

		private bool onDeleteSaves()
		{
			this.currentScreen = "confirmdelete";
			this.ScreenManager.LoadScreen(new GuiScreenConfirmAction(Lang.Get("Really delete the world? This action cannot be undone!", Array.Empty<object>()), new Action<bool>(this.OnDidConfirmDelete), this.ScreenManager, this, false));
			return true;
		}

		private void OnDidConfirmDeleteAll(bool ok)
		{
			if (ok)
			{
				this.dashboardLoading();
				this.backend.DeleteAll(delegate(EnumAuthServerResponse resp, GameServerStatus req)
				{
					this.onStatusReady(resp, req);
					this.ScreenManager.LoadScreenNoLoadCall(this);
				});
				return;
			}
			this.ScreenManager.LoadScreenNoLoadCall(this);
		}

		private void OnDidConfirmDelete(bool ok)
		{
			if (ok)
			{
				this.dashboardLoading();
				this.backend.DeleteSaves(delegate(EnumAuthServerResponse resp, GameServerStatus req)
				{
					this.onStatusReady(resp, req);
					this.ScreenManager.LoadScreenNoLoadCall(this);
				});
				return;
			}
			this.ScreenManager.LoadScreenNoLoadCall(this);
		}

		private bool onSaveServerConfig()
		{
			this.dashboardLoading();
			string scfg = JsonUtil.ToString<ServerConfigPart>(new ServerConfigPart
			{
				ServerName = this.ElementComposer.GetTextInput("serverName").GetText(),
				ServerDescription = this.ElementComposer.GetTextArea("serverDescription").GetText(),
				WelcomeMessage = this.ElementComposer.GetTextInput("serverMotd").GetText(),
				ServerLanguage = this.ElementComposer.GetDropDown("language").SelectedValue,
				AllowPvP = this.ElementComposer.GetSwitch("serverPvp").On,
				AllowFireSpread = this.ElementComposer.GetSwitch("serverFireSpread").On,
				AdvertiseServer = this.ElementComposer.GetSwitch("advertise").On,
				Password = this.ElementComposer.GetTextInput("serverPassword").GetText()
			});
			this.backend.SetConfig(new OnSrvActionComplete<GameServerStatus>(this.onStatusReady), scfg, null);
			return true;
		}

		private bool onChangeVersion()
		{
			this.showCancelOnSelectVersion = true;
			this.dashboardLoading();
			this.backend.GetGameVersions(delegate(EnumAuthServerResponse status, string[] versions)
			{
				this.screenSelectVersion(versions);
			});
			return true;
		}

		public GuiScreenServerDashboard(ScreenManager screenManager, GuiScreen parentScreen)
			: base(screenManager, parentScreen)
		{
			this.backend = new ServerCtrlBackendInterface();
			this.ShowMainMenu = true;
			this.wcu = new WorldConfig(screenManager.verifiedMods);
		}

		private void onStatusReady(EnumAuthServerResponse reqStatus, GameServerStatus gameServerStatus)
		{
			this.gameServerStatus = gameServerStatus;
			this.dashboardReady();
			this.setResponseNotifier(reqStatus, (gameServerStatus != null) ? gameServerStatus.Reason : null);
			this.proberActive = false;
			if (reqStatus != EnumAuthServerResponse.Good)
			{
				return;
			}
			if (gameServerStatus.ActiveserverDays <= 0f)
			{
				this.screenServerExpired(gameServerStatus.ActiveserverDays);
				return;
			}
			if (gameServerStatus.StatusCode == "selectregion")
			{
				this.screenSelectRegion(gameServerStatus);
			}
			if (gameServerStatus.StatusCode == "userdoesnotexist")
			{
				this.showCancelOnSelectVersion = false;
				this.backend.GetGameVersions(delegate(EnumAuthServerResponse status, string[] versions)
				{
					this.screenSelectVersion(versions);
				});
			}
			if (gameServerStatus.StatusCode == "ok" || gameServerStatus.StatusCode == "stopped" || gameServerStatus.StatusCode == "running")
			{
				this.screenServerStatus(gameServerStatus);
			}
		}

		private void screenServerStatus(GameServerStatus gameServerStatus)
		{
			this.currentScreen = "dashboard";
			int width = 600;
			ElementBounds rowBounds = ElementBounds.Fixed(0.0, 0.0, (double)width, 35.0);
			string statusText = Lang.Get("<font opacity=\"0.6\">Status: </font> {0}", new object[] { Lang.Get("serverstatus-" + gameServerStatus.StatusCode, Array.Empty<object>()) });
			string downloadStateStr = "";
			if (gameServerStatus.DownloadState == EnumDownloadSavesStatus.Ready)
			{
				downloadStateStr = Lang.Get("Your world download is ready, please download it within 24 hours.", Array.Empty<object>());
			}
			if (gameServerStatus.DownloadState == EnumDownloadSavesStatus.Copying)
			{
				downloadStateStr = Lang.Get("World download requested, copying in progress...", Array.Empty<object>());
			}
			string descText;
			if (gameServerStatus.ActiveserverDays >= 2f)
			{
				descText = Lang.Get("Your game server (version {0}) is installed and ready to use. You have {1} days of server time left.", new object[]
				{
					gameServerStatus.Version,
					(int)gameServerStatus.ActiveserverDays
				});
			}
			else
			{
				descText = Lang.Get("Your game server (version {0}) is installed and ready to use. You have {1} hours of server time left.", new object[]
				{
					gameServerStatus.Version,
					(int)(gameServerStatus.ActiveserverDays * 24f)
				});
			}
			GuiElementRichtext notiElem = new GuiElementRichtext(this.ElementComposer.Api, VtmlUtil.Richtextify(this.ElementComposer.Api, gameServerStatus.Dashboardnotification, CairoFont.WhiteSmallText(), null), rowBounds.FlatCopy());
			notiElem.CalcHeightAndPositions();
			this.ElementComposer = this.screenBase(true).BeginChildElements(rowBounds.FlatCopy()).AddStaticText(descText, CairoFont.WhiteSmallText(), rowBounds = rowBounds.BelowCopy(0.0, 80.0, 0.0, 0.0), null);
			if (!string.IsNullOrEmpty(gameServerStatus.Dashboardnotification))
			{
				this.ElementComposer.AddGameOverlay(rowBounds = rowBounds.BelowCopy(0.0, 30.0, 0.0, 0.0).WithFixedHeight(notiElem.TotalHeight / (double)RuntimeEnv.GUIScale), new double[] { 1.0, 0.9294117647058824, 0.4117647058823529, 1.0 }).AddRichtext(gameServerStatus.Dashboardnotification, CairoFont.WhiteSmallText().WithColor(new double[] { 0.2, 0.2, 0.2, 1.0 }), rowBounds.FlatCopy().WithFixedHeight(35.0).WithFixedOffset(5.0, 0.0)
					.WithFixedWidth(590.0), "dashboardnotification");
			}
			ElementBounds copyBounds;
			ElementBounds stopServerBounds;
			this.ElementComposer.AddRichtext(statusText, CairoFont.WhiteSmallishText(), rowBounds = rowBounds.BelowCopy(0.0, 30.0, 0.0, 0.0).WithFixedHeight(35.0).WithFixedPadding(0.0, 0.0), "serverStatus").AddRichtext(Lang.Get("<font opacity=\"0.6\">Host:</font> {0}", new object[] { gameServerStatus.Identifier }), CairoFont.WhiteSmallishText(), rowBounds = rowBounds.BelowCopy(0.0, 5.0, 0.0, 0.0).WithFixedHeight(35.0), null).AddAutoSizeHoverText(Lang.Get("With this information other players can connect to your server, be sure to whitelist them however.", Array.Empty<object>()), CairoFont.WhiteDetailText(), 300, rowBounds.FlatCopy().WithFixedWidth((double)(width - 50)), null)
				.AddIconButton("copy", new Action<bool>(this.OnCopyConnectionString), copyBounds = rowBounds.RightCopy(0.0, 0.0, 0.0, 0.0).WithFixedAlignmentOffset(-30.0, 0.0).WithFixedSize(30.0, 30.0), null)
				.AddAutoSizeHoverText(Lang.Get("Copy host to clipboard", Array.Empty<object>()), CairoFont.WhiteSmallText(), 150, copyBounds.FlatCopy(), null)
				.AddRichtext(Lang.Get("<font opacity=\"0.6\">To give players the ability to join your world, join the server and type<br>/player <i>playername</i> whitelist on</font>", new object[] { gameServerStatus.ConnectionString }), CairoFont.WhiteDetailText(), rowBounds = rowBounds.BelowCopy(0.0, 5.0, 0.0, 0.0).WithFixedWidth((double)width).WithAlignment(EnumDialogArea.None), null)
				.AddSmallButton(Lang.Get("Start Server", Array.Empty<object>()), new ActionConsumable(this.onStartServer), rowBounds = rowBounds.BelowCopy(0.0, 30.0, 0.0, 0.0).WithFixedWidth(200.0), EnumButtonStyle.Normal, "startButton")
				.AddSmallButton(Lang.Get("Stop Server", Array.Empty<object>()), new ActionConsumable(this.onStopServer), stopServerBounds = rowBounds.FlatCopy().WithFixedWidth(200.0).RightOf(rowBounds, 10.0), EnumButtonStyle.Normal, "stopButton")
				.AddSmallButton(Lang.Get("Force Stop Server", Array.Empty<object>()), new ActionConsumable(this.onKillServer), rowBounds.FlatCopy().WithFixedWidth(150.0).RightOf(stopServerBounds, 10.0), EnumButtonStyle.Small, "killButton")
				.AddSmallButton(Lang.Get("Server Settings", Array.Empty<object>()), new ActionConsumable(this.onConfigureServer), rowBounds = rowBounds.BelowCopy(0.0, 35.0, 0.0, 0.0).WithAlignment(EnumDialogArea.None), EnumButtonStyle.Small, "serverConfigButton")
				.AddIf(gameServerStatus.DownloadState != EnumDownloadSavesStatus.Ready)
				.AddHoverText(Lang.Get("worldsetting-onlywhenstopped", Array.Empty<object>()), CairoFont.WhiteDetailText(), 300, rowBounds.FlatCopy(), null)
				.EndIf()
				.AddSmallButton(Lang.Get("World Settings", Array.Empty<object>()), new ActionConsumable(this.onConfigureWorld), rowBounds.FlatCopy().RightOf(rowBounds, 10.0), EnumButtonStyle.Small, "worldConfigButton")
				.AddIf(gameServerStatus.QuantitySavegames > 0)
				.AddHoverText(Lang.Get("worldsetting-onlyonnewworlds", Array.Empty<object>()), CairoFont.WhiteDetailText(), 300, rowBounds.FlatCopy().RightOf(rowBounds, 10.0), null)
				.EndIf()
				.AddIf(gameServerStatus.DownloadState > EnumDownloadSavesStatus.Idle)
				.AddRichtext(downloadStateStr, CairoFont.WhiteDetailText(), rowBounds = rowBounds.BelowCopy(0.0, 20.0, 0.0, 0.0).WithFixedWidth((double)width).WithAlignment(EnumDialogArea.None), "dlStatusText")
				.AddSmallButton(Lang.Get("Download World", Array.Empty<object>()), new ActionConsumable(this.onDownloadWorldNow), rowBounds.BelowCopy(0.0, -15.0, 0.0, 0.0).WithFixedSize(1.0, 1.0).WithAlignment(EnumDialogArea.None)
					.WithFixedPadding(2.0, 2.0), EnumButtonStyle.Small, "worldDownloadButton")
				.EndIf()
				.AddButton(Lang.Get("Join Server", Array.Empty<object>()), new ActionConsumable(this.onJoinServer), rowBounds.BelowCopy(0.0, 50.0, 0.0, 0.0).WithFixedWidth(200.0).WithFixedPadding(20.0, 0.0)
					.WithAlignment(EnumDialogArea.CenterFixed), EnumButtonStyle.Normal, "joinButton")
				.AddIf(this.ScreenManager.ClientIsOffline)
				.AddRichtext(Lang.Get("offlinemultiplayerwarning", Array.Empty<object>()), CairoFont.WhiteSmallishText(), ElementBounds.Fixed(0.0, 40.0, 690.0, 30.0), null)
				.EndIf()
				.EndChildElements()
				.EndChildElements()
				.Compose(true);
			if (this.ElementComposer.GetButton("worldDownloadButton") != null)
			{
				this.ElementComposer.GetButton("worldDownloadButton").Enabled = gameServerStatus.DownloadState == EnumDownloadSavesStatus.Ready;
			}
			this.dashboardReady();
			this.connectionString = gameServerStatus.ConnectionString;
			this.identifier = gameServerStatus.Identifier;
			this._password = gameServerStatus.Password;
			if (gameServerStatus.DownloadState == EnumDownloadSavesStatus.Copying)
			{
				this.runDownloadStatusProber();
			}
		}

		private bool onShowLog()
		{
			this.dashboardLoading();
			this.backend.GetLog(new OnSrvActionComplete<GameServerLogResponse>(this.screenLog));
			return true;
		}

		private void screenLog(EnumAuthServerResponse reqStatus, GameServerLogResponse response)
		{
			this.currentScreen = "logscreen";
			this.dashboardReady();
			this.logText = string.Join("\n", response.Log);
			ElementBounds rowLeft = ElementBounds.Fixed(-25.0, 45.0, 200.0, 25.0);
			ElementBounds logtextBounds = ElementBounds.Fixed(-30.0, 80.0, 610.0, 700.0);
			ElementBounds clippingBounds = logtextBounds.ForkBoundingParent(0.0, 0.0, 0.0, 0.0);
			ElementBounds insetBounds = logtextBounds.FlatCopy().FixedGrow(6.0).WithFixedOffset(-3.0, -3.0);
			ElementBounds scrollbarBounds = insetBounds.CopyOffsetedSibling(logtextBounds.fixedWidth + 7.0, 0.0, 0.0, 0.0).WithFixedWidth(20.0);
			ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(10.0);
			bgBounds.BothSizing = ElementSizing.FitToChildren;
			bgBounds.WithChildren(new ElementBounds[] { insetBounds, clippingBounds, scrollbarBounds });
			ElementBounds copyBounds;
			this.ElementComposer = this.screenBase(true).AddStaticText("Server Logs:", CairoFont.WhiteSmallishText(), rowLeft, null).AddSmallButton(Lang.Get("Back", Array.Empty<object>()), new ActionConsumable(this.onCancel), rowLeft = rowLeft.BelowCopy(0.0, logtextBounds.fixedHeight + 40.0, 0.0, 0.0).WithFixedHeight(35.0), EnumButtonStyle.Normal, "backButton")
				.AddIconButton("copy", new Action<bool>(this.OnCopyLog), copyBounds = rowLeft.RightCopy(10.0, 2.0, 0.0, 0.0).WithFixedSize(30.0, 30.0), null)
				.AddAutoSizeHoverText(Lang.Get("Copy Log to clipboard", Array.Empty<object>()), CairoFont.WhiteSmallText(), 250, copyBounds.FlatCopy(), null)
				.BeginChildElements(bgBounds)
				.BeginClip(clippingBounds)
				.AddInset(insetBounds, 3, 0.85f)
				.AddDynamicText("", CairoFont.WhiteDetailText(), logtextBounds, "logtext")
				.EndClip()
				.AddVerticalScrollbar(new Action<float>(this.OnNewScrollbarvalue), scrollbarBounds, "scrollbar")
				.EndChildElements()
				.Compose(true);
			GuiElementDynamicText dynamicText = this.ElementComposer.GetDynamicText("logtext");
			dynamicText.AutoHeight();
			dynamicText.SetNewText(this.logText, false, false, false);
			dynamicText.Enabled = false;
			GuiElementScrollbar scrollbar = this.ElementComposer.GetScrollbar("scrollbar");
			scrollbar.SetHeights(600f, (float)logtextBounds.fixedHeight);
			scrollbar.CurrentYPosition = (float)logtextBounds.fixedHeight - 680f;
			dynamicText.Bounds.fixedY = -logtextBounds.fixedHeight + 680.0;
			dynamicText.Bounds.CalcWorldBounds();
		}

		private void OnCopyLog(bool ok)
		{
			if (this.logText == null)
			{
				this.logText = string.Empty;
			}
			ScreenManager.Platform.XPlatInterface.SetClipboardText(this.logText);
		}

		private void OnNewScrollbarvalue(float value)
		{
			GuiElementDynamicText dynamicText = this.ElementComposer.GetDynamicText("logtext");
			dynamicText.Bounds.fixedY = (double)(3f - value);
			dynamicText.Bounds.CalcWorldBounds();
		}

		private void probeStatus()
		{
			this.backend.GetStatus(delegate(EnumAuthServerResponse st1, GameServerStatus gameserverStatus)
			{
				if (this.ScreenManager.CurrentScreen != this)
				{
					return;
				}
				this.onStatusReady(st1, gameserverStatus);
				this.runServerStatusProber(gameserverStatus.StatusCode);
			});
		}

		private bool onConfigureWorld()
		{
			this.dashboardLoading();
			this.backend.GetConfig(new OnSrvActionComplete<GameServerConfigResponse>(this.onWorldConfigReceived));
			return true;
		}

		private bool onCancel()
		{
			this.backend.GetStatus(new OnSrvActionComplete<GameServerStatus>(this.onStatusReady));
			this.screenLoading();
			return true;
		}

		private void onWorldConfigReceived(EnumAuthServerResponse reqStatus, GameServerConfigResponse response)
		{
			this.wcu.IsNewWorld = this.gameServerStatus.QuantitySavegames == 0;
			(this.wcu.Jworldconfig.Token as JObject).Merge(response.WorldConfig.Token, new JsonMergeSettings
			{
				MergeArrayHandling = MergeArrayHandling.Replace
			});
			this.wcu.loadWorldConfigValues(this.wcu.Jworldconfig, this.wcu.WorldConfigsCustom);
			this.currentScreen = "worldconfig";
			this.customizeScreen = new GuiScreenWorldCustomize(new Action<bool>(this.OnReturnFromCustomizer), this.ScreenManager, this, this.wcu.Clone(), null);
			this.ScreenManager.LoadScreen(this.customizeScreen);
		}

		private void OnReturnFromCustomizer(bool didApply)
		{
			if (didApply)
			{
				this.wcu = this.customizeScreen.wcu;
				this.wcu.Jworldconfig.Token["Seed"] = this.wcu.Seed;
				this.wcu.Jworldconfig.Token["MapSizeY"] = this.wcu.MapsizeY;
				string worldconfig = this.wcu.Jworldconfig.ToString();
				this.backend.SetConfig(new OnSrvActionComplete<GameServerStatus>(this.onStatusReady), null, worldconfig);
			}
			this.ScreenManager.LoadScreen(this);
		}

		private void OnCopyConnectionString(bool ok)
		{
			ScreenManager.Platform.XPlatInterface.SetClipboardText(this.identifier);
		}

		private bool onJoinServer()
		{
			this.ScreenManager.ConnectToMultiplayer(this.connectionString, this._password);
			return true;
		}

		private void dashboardLoading()
		{
			foreach (KeyValuePair<string, GuiElement> val in this.ElementComposer.interactiveElements)
			{
				GuiElementTextButton btn = val.Value as GuiElementTextButton;
				if (btn != null)
				{
					btn.Enabled = false;
				}
			}
		}

		private void dashboardReady()
		{
			if (this.ScreenManager.CurrentScreen != this)
			{
				return;
			}
			foreach (KeyValuePair<string, GuiElement> val in this.ElementComposer.interactiveElements)
			{
				GuiElementTextButton btn = val.Value as GuiElementTextButton;
				if (btn != null)
				{
					btn.Enabled = true;
				}
			}
			if (this.ElementComposer.GetButton("joinButton") != null)
			{
				this.ElementComposer.GetButton("joinButton").Enabled = this.gameServerStatus.StatusCode == "running";
			}
			if (this.ElementComposer.GetButton("serverConfigButton") != null)
			{
				this.ElementComposer.GetButton("serverConfigButton").Enabled = this.gameServerStatus.StatusCode == "stopped";
				this.ElementComposer.GetButton("worldConfigButton").Enabled = this.gameServerStatus.StatusCode == "stopped";
			}
			if (this.ElementComposer.GetButton("worldConfigButton") != null && this.gameServerStatus.QuantitySavegames > 0)
			{
				this.ElementComposer.GetButton("worldConfigButton").Enabled = false;
			}
			if (this.ElementComposer.GetButton("worldDownloadButton") != null)
			{
				this.ElementComposer.GetButton("worldDownloadButton").Enabled = this.gameServerStatus.DownloadState == EnumDownloadSavesStatus.Ready;
			}
		}

		private bool onKillServer()
		{
			this.ScreenManager.LoadScreen(new GuiScreenConfirmAction(Lang.Get("vintagehostingserver-confirmkill", Array.Empty<object>()), new Action<bool>(this.OnDidConfirmKillServer), this.ScreenManager, this, false));
			return true;
		}

		private void OnDidConfirmKillServer(bool ok)
		{
			if (ok)
			{
				this.dashboardLoading();
				this.backend.ForceStop(new OnSrvActionComplete<ServerCtrlResponse>(this.onStopped));
			}
			this.ScreenManager.LoadScreen(this);
		}

		private bool onStopServer()
		{
			this.dashboardLoading();
			this.backend.Stop(new OnSrvActionComplete<ServerCtrlResponse>(this.onStopped));
			return true;
		}

		private void onStopped(EnumAuthServerResponse reqStatus, ServerCtrlResponse response)
		{
			if (this.ElementComposer.GetRichtext("serverStatus") != null)
			{
				if (reqStatus == EnumAuthServerResponse.Offline)
				{
					this.setResponseNotifier(reqStatus, null);
				}
				else
				{
					this.serverStatusProbingTries = 7;
					this.desireState = "stopped";
					this.runServerStatusProber(response.StatusCode);
				}
			}
			this.dashboardReady();
		}

		private bool onStartServer()
		{
			this.dashboardLoading();
			this.backend.Start(new OnSrvActionComplete<ServerCtrlResponse>(this.onStarted));
			return true;
		}

		private void onStarted(EnumAuthServerResponse reqStatus, ServerCtrlResponse response)
		{
			if (this.ElementComposer.GetRichtext("serverStatus") != null)
			{
				if (reqStatus == EnumAuthServerResponse.Offline)
				{
					this.setResponseNotifier(reqStatus, null);
				}
				else
				{
					this.desireState = "running";
					this.serverStatusProbingTries = 7;
					this.runServerStatusProber(response.StatusCode);
				}
			}
			this.dashboardReady();
		}

		private void runServerStatusProber(string statuscode)
		{
			string statusText = Lang.Get("<font opacity=\"0.6\">Status: </font> {0}", new object[] { Lang.Get("serverstatus-" + statuscode, Array.Empty<object>()) });
			if (statuscode != this.desireState && !this.proberActive)
			{
				statusText = Lang.Get("<font opacity=\"0.6\">Status: </font> Loading...", Array.Empty<object>());
				this.serverStatusProbingTries--;
				if (this.serverStatusProbingTries > 0)
				{
					this.proberActive = true;
					ScreenManager.EnqueueCallBack(new Action(this.probeStatus), 3000);
				}
				else
				{
					statusText = Lang.Get("<font opacity=\"0.6\">Status: </font> Timeout", Array.Empty<object>());
				}
			}
			GuiElementRichtext richtext = this.ElementComposer.GetRichtext("serverStatus");
			if (richtext == null)
			{
				return;
			}
			richtext.SetNewText(statusText, CairoFont.WhiteSmallishText(), null);
		}

		private void setResponseNotifier(EnumAuthServerResponse reqStatus, string invalidReason)
		{
			GuiElementRichtext elem = this.ElementComposer.GetRichtext("notificationtext");
			switch (reqStatus)
			{
			case EnumAuthServerResponse.Good:
			{
				CairoFont font = CairoFont.WhiteDetailText().WithColor(GuiStyle.SuccessTextColor);
				elem.SetNewText("Request succesfull", font, null);
				return;
			}
			case EnumAuthServerResponse.Bad:
			{
				CairoFont font2 = CairoFont.WhiteDetailText().WithColor(GuiStyle.ErrorTextColor);
				if (invalidReason == null)
				{
					elem.SetNewText("Bad response from server. Programming error. Please send us a support ticket with your client-main.log log file attached (its in %appdata%/VintageStoryData/Logs).", font2, null);
					return;
				}
				string text;
				if (Lang.HasTranslation("vintagehosting-response-" + invalidReason, true, true))
				{
					text = Lang.Get("vintagehosting-response-" + invalidReason, Array.Empty<object>());
				}
				else
				{
					text = Lang.Get("vintagehosting-response-badrequest", new object[] { invalidReason });
				}
				elem.SetNewText(text, font2, null);
				return;
			}
			case EnumAuthServerResponse.Offline:
			{
				CairoFont font3 = CairoFont.WhiteDetailText().WithColor(GuiStyle.ErrorTextColor);
				elem.SetNewText("Unable to connect to auth server, server either offline or no internet connection.", font3, null);
				return;
			}
			default:
				return;
			}
		}

		private void screenLoading()
		{
			this.currentScreen = "loading";
			ElementBounds rowLeft = ElementBounds.Fixed(0.0, 50.0, 300.0, 35.0).WithAlignment(EnumDialogArea.CenterFixed);
			this.ElementComposer = this.screenBase(true).AddRichtext(Lang.Get("Loading...", Array.Empty<object>()), CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center), rowLeft.BelowCopy(0.0, 30.0, 0.0, 0.0), "loadingText").EndChildElements()
				.Compose(true);
		}

		private GuiComposer screenBase(bool showSupportText)
		{
			ElementBounds rowLeft = ElementBounds.Fixed(0.0, 0.0, 400.0, 35.0).WithAlignment(EnumDialogArea.CenterFixed);
			ElementBounds supportBounds = ElementBounds.Fixed(0.0, 0.0, 595.0, 35.0).WithAlignment(EnumDialogArea.CenterBottom);
			CairoFont font = CairoFont.WhiteDetailText().WithColor(GuiStyle.ErrorTextColor).WithOrientation(EnumTextOrientation.Center);
			return base.dialogBase("mainmenu-servercontrol-dashboard", 650.0, -1.0).AddRichtext("", font, ElementBounds.Fixed(EnumDialogArea.CenterFixed, 0.0, 0.0, 550.0, 60.0), "notificationtext").AddIf(showSupportText)
				.AddRichtext(Lang.Get("serverctrl-getsupport", Array.Empty<object>()), CairoFont.WhiteSmallText(), supportBounds, null)
				.EndIf()
				.BeginChildElements(ElementBounds.Fixed(EnumDialogArea.CenterFixed, 0.0, 30.0, 600.0, 700.0))
				.AddStaticText(Lang.Get("serverctrl-dashboard", Array.Empty<object>()), CairoFont.WhiteMediumText().WithOrientation(EnumTextOrientation.Center), rowLeft, null);
		}

		public override void OnScreenLoaded()
		{
			this.backend.GetStatus(new OnSrvActionComplete<GameServerStatus>(this.onStatusReady));
			this.screenLoading();
			this.wcu.LoadPlayStyles();
			if (this.wcu.PlayStyles.Count > 0)
			{
				this.wcu.selectPlayStyle(0);
			}
		}

		public override void RenderAfterFinalComposition(float dt)
		{
			base.RenderAfterFinalComposition(dt);
			this.accum += dt;
			if (this.backend.IsLoading && (double)this.accum > 0.25)
			{
				this.accum = 0f;
				int num = (int)(this.ScreenManager.GamePlatform.EllapsedMs / 500L % 3L);
				GuiElementRichtext richtext = this.ElementComposer.GetRichtext("loadingText");
				if (richtext == null)
				{
					return;
				}
				richtext.SetNewText(this.loadtexts[num], CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center), null);
			}
		}

		private string prettyServerTimeLeft(float days)
		{
			if (days < 0f)
			{
				if (days > -1f)
				{
					return Lang.Get("Your active server time expired {0:0.#} hours ago.", new object[] { -days * 24f });
				}
				int idays = (int)(-(int)days);
				float hours = (-days - (float)idays) * 24f;
				if (idays == 1)
				{
					return Lang.Get("Your active server time expired 1 day and {0:0.#} hours ago.", new object[] { hours });
				}
				return Lang.Get("Your active server time expired {0:0.#} days and {1:0.#} hours ago.", new object[] { idays, hours });
			}
			else
			{
				if (days < 1f)
				{
					return Lang.Get("Your active server time will expire in {0:0.#} hours.", new object[] { days * 24f });
				}
				int idays2 = (int)days;
				float hours2 = (days - (float)idays2) * 24f;
				if (idays2 == 1)
				{
					return Lang.Get("Your active server time will expire in 1 day and {0:0.#} hours.", new object[] { hours2 });
				}
				return Lang.Get("Your active server time will expire in {0:0.#} days and {1:0.#} hours", new object[] { idays2, hours2 });
			}
		}

		private void screenServerExpired(float daysActive)
		{
			this.currentScreen = "expired";
			ElementBounds rowLeft = ElementBounds.Fixed(0.0, 50.0, 500.0, 35.0).WithAlignment(EnumDialogArea.CenterFixed);
			this.ElementComposer = this.screenBase(true).AddStaticText(this.prettyServerTimeLeft(daysActive), CairoFont.WhiteSmallText(), rowLeft = rowLeft.BelowCopy(0.0, 30.0, 0.0, 0.0), null).AddRichtext(Lang.Get("serverctrl-expireddesc", Array.Empty<object>()), CairoFont.WhiteSmallText(), rowLeft = rowLeft.BelowCopy(0.0, 5.0, 0.0, 0.0), "richtext");
			this.ElementComposer.GetRichtext("richtext").BeforeCalcBounds();
			string downloadStateStr = "";
			if (this.gameServerStatus.DownloadState == EnumDownloadSavesStatus.Ready)
			{
				downloadStateStr = Lang.Get("Your world download is ready, please download it within 24 hours.", Array.Empty<object>());
			}
			if (this.gameServerStatus.DownloadState == EnumDownloadSavesStatus.Copying)
			{
				downloadStateStr = Lang.Get("World download requested, copying in progress...", Array.Empty<object>());
			}
			if (this.gameServerStatus.DownloadState != EnumDownloadSavesStatus.Idle)
			{
				this.ElementComposer.AddRichtext(downloadStateStr, CairoFont.WhiteDetailText(), rowLeft = rowLeft.BelowCopy(0.0, 30.0, 0.0, 0.0), "dlStatusText").AddSmallButton(Lang.Get("Download World", Array.Empty<object>()), new ActionConsumable(this.onDownloadWorldNow), rowLeft = rowLeft.BelowCopy(0.0, -55.0, 0.0, 0.0).WithFixedSize(1.0, 1.0).WithFixedPadding(5.0, 3.0), EnumButtonStyle.Small, "worldDownloadButton");
			}
			else
			{
				this.ElementComposer.AddSmallButton(Lang.Get("Request world download", Array.Empty<object>()), new ActionConsumable(this.onRequestDownload), rowLeft = rowLeft.BelowCopy(0.0, 30.0, 0.0, 0.0).WithFixedHeight(40.0), EnumButtonStyle.Normal, "requestDlExpired");
			}
			this.ElementComposer.AddButton(Lang.Get("Back to main menu", Array.Empty<object>()), new ActionConsumable(this.onMainMenu), rowLeft.BelowCopy(0.0, 50.0, 0.0, 0.0).WithFixedHeight(40.0), EnumButtonStyle.Normal, "menuButton").EndChildElements().Compose(true);
			if (this.gameServerStatus.DownloadState == EnumDownloadSavesStatus.Copying)
			{
				this.ElementComposer.GetButton("worldDownloadButton").Enabled = false;
			}
		}

		private bool onMainMenu()
		{
			this.ScreenManager.StartMainMenu();
			return true;
		}

		private bool CallbackEnqueued;

		private ServerCtrlBackendInterface backend;

		private string connectionString;

		private string identifier;

		private string _password;

		private GameServerStatus gameServerStatus;

		private bool showCancelOnSelectVersion;

		private WorldConfig wcu;

		private string currentScreen;

		private string logText;

		private int serverStatusProbingTries;

		private int dlStatusProbingTries;

		private string desireState;

		private bool proberActive;

		private GuiScreenWorldCustomize customizeScreen;

		private string[] loadtexts = new string[]
		{
			Lang.Get("Loading.", Array.Empty<object>()),
			Lang.Get("Loading..", Array.Empty<object>()),
			Lang.Get("Loading...", Array.Empty<object>())
		};

		private float accum;
	}
}
