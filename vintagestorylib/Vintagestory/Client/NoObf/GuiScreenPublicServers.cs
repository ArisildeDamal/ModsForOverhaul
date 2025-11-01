using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf
{
	public class GuiScreenPublicServers : GuiScreen
	{
		public GuiScreenPublicServers(ScreenManager screenManager, GuiScreen parent)
			: base(screenManager, parent)
		{
			this.ShowMainMenu = true;
			this.InitGui();
			screenManager.GamePlatform.WindowResized += delegate(int w, int h)
			{
				this.invalidate();
			};
			ClientSettings.Inst.AddWatcher<float>("guiScale", delegate(float s)
			{
				this.invalidate();
			});
		}

		private void invalidate()
		{
			if (base.IsOpened)
			{
				this.InitGui();
				if (this.packet != null)
				{
					this.popCells();
					return;
				}
			}
			else
			{
				ScreenManager.GuiComposers.Dispose("mainmenu-browserpublicservers");
			}
		}

		private void InitGui()
		{
			int windowHeight = this.ScreenManager.GamePlatform.WindowSize.Height;
			int windowWidth = this.ScreenManager.GamePlatform.WindowSize.Width;
			ElementBounds buttonBounds = ElementBounds.FixedSize(60.0, 30.0).WithFixedPadding(10.0, 2.0);
			ElementBounds titleBounds = ElementBounds.Fixed(EnumDialogArea.LeftTop, 0.0, 0.0, 690.0, 35.0);
			double num = Math.Max(400.0, (double)windowWidth * 0.5) / (double)ClientSettings.GUIScale;
			string pwdprotect = Lang.Get("Password protected", Array.Empty<object>());
			double pwdprotectlen = CairoFont.WhiteSmallText().GetTextExtents(pwdprotect).Width / (double)RuntimeEnv.GUIScale;
			string openforall = Lang.Get("Open for all", Array.Empty<object>());
			double openforalllen = CairoFont.WhiteSmallText().GetTextExtents(openforall).Width / (double)RuntimeEnv.GUIScale;
			string modded = Lang.Get("Modded", Array.Empty<object>());
			double moddedlen = CairoFont.WhiteSmallText().GetTextExtents(modded).Width / (double)RuntimeEnv.GUIScale;
			ElementBounds insetBounds;
			this.ElementComposer = base.dialogBase("mainmenu-browserpublicservers", -1.0, -1.0).AddDynamicText(Lang.Get("multiplayer-loadingpublicservers", Array.Empty<object>()), CairoFont.WhiteSmallishText(), titleBounds, "titleText").AddSwitch(new Action<bool>(this.onToggleOpen4All), ElementBounds.Fixed(0, 45), "4allSwitch", 20.0, 3.0)
				.AddStaticTextAutoBoxSize(openforall, CairoFont.WhiteSmallText(), EnumTextOrientation.Left, ElementBounds.Fixed(25, 45), null)
				.AddSwitch(new Action<bool>(this.onTogglePwdProtected), ElementBounds.Fixed((int)(50.0 + openforalllen), 45), "pwdSwitch", 20.0, 3.0)
				.AddStaticTextAutoBoxSize(pwdprotect, CairoFont.WhiteSmallText(), EnumTextOrientation.Left, ElementBounds.Fixed((int)(75.0 + openforalllen), 45), null)
				.AddSwitch(new Action<bool>(this.onToggleWhitelisted), ElementBounds.Fixed((int)(100.0 + pwdprotectlen + openforalllen), 45), "whitelistSwitch", 20.0, 3.0)
				.AddStaticTextAutoBoxSize(Lang.Get("Whitelisted", Array.Empty<object>()), CairoFont.WhiteSmallText(), EnumTextOrientation.Left, ElementBounds.Fixed((int)(125.0 + pwdprotectlen + openforalllen), 45), null)
				.AddSwitch(new Action<bool>(this.onToggleModded), ElementBounds.Fixed((int)(170.0 + pwdprotectlen + openforalllen + moddedlen), 45), "moddedSwitch", 20.0, 3.0)
				.AddStaticTextAutoBoxSize(Lang.Get("Modded", Array.Empty<object>()), CairoFont.WhiteSmallText(), EnumTextOrientation.Left, ElementBounds.Fixed((int)(195.0 + pwdprotectlen + openforalllen + moddedlen), 45), null)
				.AddTextInput(ElementStdBounds.Rowed(1f, 0.0, EnumDialogArea.None).WithFixedSize(300.0, 30.0), new Action<string>(this.OnSearch), null, "search")
				.AddInset(insetBounds = titleBounds.BelowCopy(0.0, 70.0, 0.0, 0.0).WithFixedSize(Math.Max(400.0, (double)windowWidth * 0.5) / (double)ClientSettings.GUIScale, (double)((float)Math.Max(300, windowHeight) / ClientSettings.GUIScale - 270f)), 4, 0.85f)
				.AddVerticalScrollbar(new Action<float>(this.OnNewScrollbarvalue), ElementStdBounds.VerticalScrollbar(insetBounds), "scrollbar")
				.BeginClip(this.clippingBounds = insetBounds.ForkContainingChild(3.0, 3.0, 3.0, 3.0))
				.AddCellList(this.tableBounds = this.clippingBounds.ForkContainingChild(0.0, 0.0, 0.0, -3.0).WithFixedPadding(5.0), new OnRequireCell<ServerListEntry>(this.createCell), new List<ServerListEntry>(), "serverstable")
				.EndClip()
				.AddButton(Lang.Get("general-back", Array.Empty<object>()), new ActionConsumable(this.OnBack), buttonBounds.FixedUnder(insetBounds, 10.0), EnumButtonStyle.Normal, null)
				.AddRichtext("", CairoFont.WhiteSmallText(), ElementBounds.Fixed(0.0, 0.0, 500.0, 30.0).FixedUnder(insetBounds, 20.0).WithAlignment(EnumDialogArea.RightFixed), "summaryText")
				.EndChildElements()
				.Compose(true);
			this.tableBounds.CalcWorldBounds();
			this.clippingBounds.CalcWorldBounds();
			this.ElementComposer.GetScrollbar("scrollbar").SetHeights((float)this.clippingBounds.fixedHeight, (float)this.tableBounds.fixedHeight);
			this.ElementComposer.GetTextInput("search").SetPlaceHolderText(Lang.Get("Search...", Array.Empty<object>()));
			this.ElementComposer.GetTextInput("search").SetValue(this.searchText, true);
			this.ElementComposer.GetSwitch("4allSwitch").SetValue(ClientSettings.ShowOpenForAllServers);
			this.ElementComposer.GetSwitch("pwdSwitch").SetValue(ClientSettings.ShowPasswordProtectedServers);
			this.ElementComposer.GetSwitch("whitelistSwitch").SetValue(ClientSettings.ShowWhitelistedServers);
			this.ElementComposer.GetSwitch("moddedSwitch").SetValue(ClientSettings.ShowModdedServers);
		}

		private IGuiElementCell createCell(ServerListEntry cell, ElementBounds bounds)
		{
			GuiElementMainMenuCell guiElementMainMenuCell = new GuiElementMainMenuCell(this.ScreenManager.api, cell, bounds);
			guiElementMainMenuCell.MainTextWidthSub = GuiElement.scaled(40.0);
			guiElementMainMenuCell.ShowModifyIcons = false;
			guiElementMainMenuCell.OnMouseDownOnCellLeft = new Action<int>(this.OnClickCellLeft);
			guiElementMainMenuCell.FixedHeight = new double?((double)50);
			cell.LeftOffY = -2f;
			return guiElementMainMenuCell;
		}

		private void OnSearch(string text)
		{
			if (this.searchText != text)
			{
				this.searchText = text;
				this.updateFilter();
			}
		}

		private void updateFilter()
		{
			this.ElementComposer.GetCellList("serverstable").FilterCells(delegate(IGuiElementCell c)
			{
				ServerListEntry entry = (c as GuiElementMainMenuCell).cellEntry as ServerListEntry;
				if (!ClientSettings.ShowPasswordProtectedServers && entry.hasPassword)
				{
					return false;
				}
				if (!ClientSettings.ShowWhitelistedServers && entry.whitelisted)
				{
					return false;
				}
				if (!ClientSettings.ShowModdedServers)
				{
					ModPacket[] mods = entry.mods;
					if (mods != null && mods.Length != 0)
					{
						return false;
					}
				}
				if (!ClientSettings.ShowOpenForAllServers && !entry.hasPassword && !entry.whitelisted)
				{
					return false;
				}
				if (this.searchText != null && this.searchText.Length != 0)
				{
					string serverName = entry.serverName;
					return serverName != null && serverName.CaseInsensitiveContains(this.searchText, StringComparison.CurrentCultureIgnoreCase);
				}
				return true;
			});
			this.updateStatsAndBounds();
			ElementBounds bounds = this.ElementComposer.GetCellList("serverstable").Bounds;
			bounds.fixedY = 0.0;
			bounds.CalcWorldBounds();
		}

		private void onToggleOpen4All(bool on)
		{
			ClientSettings.ShowOpenForAllServers = on;
			this.updateFilter();
		}

		private void onToggleWhitelisted(bool on)
		{
			ClientSettings.ShowWhitelistedServers = on;
			this.updateFilter();
		}

		private void onToggleModded(bool on)
		{
			ClientSettings.ShowModdedServers = on;
			this.updateFilter();
		}

		private void onTogglePwdProtected(bool on)
		{
			ClientSettings.ShowPasswordProtectedServers = on;
			this.updateFilter();
		}

		private bool OnBack()
		{
			this.ScreenManager.LoadAndCacheScreen(typeof(GuiScreenMultiplayer));
			return true;
		}

		public override void OnScreenLoaded()
		{
			this.LoadServerEntries();
			this.InitGui();
		}

		private void LoadServerEntries()
		{
			this.isLoading = true;
			this.getServersAsync(ClientSettings.MasterserverUrl + "list", delegate(ResponsePacket packet)
			{
				this.isLoading = false;
				this.packet = packet;
				ScreenManager.EnqueueMainThreadTask(delegate
				{
					this.popCells();
				});
			});
		}

		private void popCells()
		{
			this.cells.Clear();
			ResponsePacket responsePacket = this.packet;
			if (((responsePacket != null) ? responsePacket.status : null) != "ok")
			{
				ServerListEntry cell = new ServerListEntry
				{
					Title = "Could not connect to master server",
					TitleFont = CairoFont.WhiteSmallishText().WithColor(GuiStyle.ErrorTextColor)
				};
				this.cells.Add(cell);
			}
			else
			{
				IOrderedEnumerable<ServerListEntry> entries = this.packet.data.OrderByDescending((ServerListEntry elem) => elem.players + ((elem.players == 0 && elem.serverName == "Vintage Story Server") ? (-1) : 0));
				this.serversTotal = 0;
				this.playersTotal = 0;
				foreach (ServerListEntry entry in entries)
				{
					this.serversTotal++;
					this.playersTotal += entry.players;
					List<string> properties = new List<string>();
					properties.Add(Lang.Get("{0}/{1} players online", new object[] { entry.players, entry.maxPlayers }));
					if (entry.hasPassword)
					{
						properties.Add(Lang.Get("password protected", Array.Empty<object>()));
					}
					if (entry.mods.Length != 0)
					{
						properties.Add(Lang.Get("modded", Array.Empty<object>()));
					}
					if (entry.whitelisted)
					{
						properties.Add(Lang.Get("whitelisted", Array.Empty<object>()));
					}
					entry.Title = entry.serverName;
					entry.RightTopText = Lang.Get("v{0}", new object[] { entry.gameVersion });
					entry.DetailText = string.Join(", ", properties);
				}
				this.cells = entries.ToList<ServerListEntry>();
				GuiElementRichtext rtelem = this.ElementComposer.GetRichtext("summaryText");
				rtelem.Bounds.fixedWidth = 500.0;
				rtelem.SetNewTextWithoutRecompose(Lang.Get("multiplayer-publicservers-stats", new object[] { this.playersTotal, this.serversTotal }), CairoFont.WhiteSmallText(), null, false);
				rtelem.BeforeCalcBounds();
				rtelem.Bounds.fixedWidth = rtelem.MaxLineWidth / (double)RuntimeEnv.GUIScale + 10.0;
				rtelem.RecomposeText();
			}
			this.ElementComposer.GetCellList("serverstable").ReloadCells(this.cells);
			this.updateFilter();
			this.ElementComposer.GetDynamicText("titleText").SetNewText(Lang.Get("multiplayer-browsepublicservers", Array.Empty<object>()), false, false, false);
		}

		private void updateStatsAndBounds()
		{
			this.tableBounds.CalcWorldBounds();
			this.clippingBounds.CalcWorldBounds();
			this.ElementComposer.GetScrollbar("scrollbar").SetHeights((float)this.clippingBounds.fixedHeight, (float)this.tableBounds.fixedHeight);
		}

		private void OnNewScrollbarvalue(float value)
		{
			ElementBounds bounds = this.ElementComposer.GetCellList("serverstable").Bounds;
			bounds.fixedY = (double)(0f - value);
			bounds.CalcWorldBounds();
		}

		private void OnClickCellLeft(int index)
		{
			if (this.cells[index].serverIp == null)
			{
				return;
			}
			this.ScreenManager.LoadScreen(new GuiScreenPublicServerView(this.cells[index], this.ScreenManager, this));
		}

		public override void RenderToDefaultFramebuffer(float dt)
		{
			base.RenderToDefaultFramebuffer(dt);
			if (this.isLoading && this.ScreenManager.GamePlatform.EllapsedMs - this.ellapsedMs > 1000L)
			{
				int index = (int)(this.ScreenManager.GamePlatform.EllapsedMs / 1000L % 2L);
				string[] texts = new string[]
				{
					Lang.Get("multiplayer-loadingpublicservers", Array.Empty<object>()),
					Lang.Get("multiplayer-loadingpublicservers2", Array.Empty<object>())
				};
				this.ElementComposer.GetDynamicText("titleText").SetNewText(texts[index], false, false, false);
				this.ellapsedMs = this.ScreenManager.GamePlatform.EllapsedMs;
			}
		}

		private async void getServersAsync(string url, Action<ResponsePacket> onComplete)
		{
			ResponsePacket packet = null;
			try
			{
				HttpResponseMessage httpResponseMessage = await VSWebClient.Inst.GetAsync(url);
				httpResponseMessage.EnsureSuccessStatusCode();
				string responseBody = await httpResponseMessage.Content.ReadAsStringAsync();
				packet = JsonConvert.DeserializeObject<ResponsePacket>(responseBody);
				this.ScreenManager.GamePlatform.Logger.Notification("Master server list retrieved. Status {0}. Response length: {1}", new object[]
				{
					packet.status,
					(responseBody == null) ? (-1) : responseBody.Length
				});
			}
			catch (Exception e)
			{
				this.ScreenManager.GamePlatform.Logger.Error("Failed retrieving master server list at url {0}.", new object[] { url });
				this.ScreenManager.GamePlatform.Logger.Error(e);
			}
			onComplete(packet);
		}

		private ElementBounds tableBounds;

		private ElementBounds clippingBounds;

		private bool isLoading;

		private long ellapsedMs;

		private string searchText;

		private List<ServerListEntry> cells = new List<ServerListEntry>();

		private ResponsePacket packet;

		private int serversTotal;

		private int playersTotal;
	}
}
