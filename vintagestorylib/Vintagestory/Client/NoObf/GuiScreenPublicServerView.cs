using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.Client.NoObf
{
	public class GuiScreenPublicServerView : GuiScreen
	{
		public GuiScreenPublicServerView(ServerListEntry entry, ScreenManager screenManager, GuiScreen parentScreen)
			: base(screenManager, parentScreen)
		{
			this.entry = entry;
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
				return;
			}
			ScreenManager.GuiComposers.Dispose("mainmenu-browserpublicserverview");
		}

		private void InitGui()
		{
			ElementBounds titleElement = ElementBounds.Fixed(0.0, 0.0, 700.0, 30.0);
			ElementBounds leftElement = titleElement.BelowCopy(0.0, 0.0, 0.0, 0.0).WithFixedSize(170.0, 35.0);
			List<string> configsList = new List<string>();
			if (this.entry.hasPassword)
			{
				configsList.Add(Lang.Get("Password protected", Array.Empty<object>()));
			}
			if (this.entry.whitelisted)
			{
				configsList.Add(Lang.Get("Whitelisted players only", Array.Empty<object>()));
			}
			string configs = string.Join(", ", configsList);
			List<string> modList = new List<string>();
			int i = 0;
			foreach (ModPacket val in this.entry.mods)
			{
				if (i++ > 20 && this.entry.mods.Length > 25)
				{
					break;
				}
				modList.Add(val.id);
			}
			string mods = string.Join(", ", modList);
			if (modList.Count < this.entry.mods.Length)
			{
				mods += Lang.Get(" and {0} more", new object[] { this.entry.mods.Length - modList.Count });
			}
			if (mods.Length == 0)
			{
				mods = Lang.Get("server-nomods", Array.Empty<object>());
			}
			CairoFont font = CairoFont.WhiteSmallText();
			this.ElementComposer = base.dialogBase("mainmenu-browserpublicserverview", -1.0, -1.0).AddStaticText(this.entry.serverName, CairoFont.WhiteSmallishText(), titleElement.FlatCopy(), null).AddStaticText(Lang.Get("Description", Array.Empty<object>()), font, leftElement = leftElement.BelowCopy(0.0, 0.0, 0.0, 0.0), null)
				.AddRichtext((this.entry.gameDescription.Length == 0) ? "<i>No description</i>" : this.entry.gameDescription, font, leftElement.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedSize(540.0, 35.0), "desc");
			ElementBounds rtBounds = this.ElementComposer.GetRichtext("desc").Bounds;
			this.ElementComposer.GetRichtext("desc").BeforeCalcBounds();
			this.ElementComposer.AddStaticText(Lang.Get("Playstyle", Array.Empty<object>()), font, leftElement = leftElement.BelowCopy(0.0, 0.0, 0.0, 0.0).WithFixedOffset(0.0, Math.Max(0.0, rtBounds.fixedHeight - 30.0)), null).AddStaticText((this.entry.playstyle.langCode == null) ? Lang.Get("playstyle-" + this.entry.playstyle.id, Array.Empty<object>()) : Lang.Get("playstyle-" + this.entry.playstyle.langCode, Array.Empty<object>()), font, EnumTextOrientation.Left, leftElement.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedSize(540.0, 70.0), null).AddStaticText(Lang.Get("Currently online", Array.Empty<object>()), font, leftElement = leftElement.BelowCopy(0.0, 0.0, 0.0, 0.0), null)
				.AddStaticText(this.entry.players.ToString() + " / " + this.entry.maxPlayers.ToString(), font, EnumTextOrientation.Left, leftElement.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedSize(540.0, 30.0), null);
			if (configs.Length > 0)
			{
				this.ElementComposer.AddStaticText(Lang.Get("Configuration", Array.Empty<object>()), font, leftElement = leftElement.BelowCopy(0.0, 0.0, 0.0, 0.0), null).AddStaticText(configs, font, EnumTextOrientation.Left, leftElement.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedSize(540.0, 30.0), null);
			}
			this.ElementComposer.AddStaticText(Lang.Get("Game version", Array.Empty<object>()), font, leftElement = leftElement.BelowCopy(0.0, 0.0, 0.0, 0.0), null).AddStaticText(this.entry.gameVersion, font, EnumTextOrientation.Left, leftElement.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedSize(540.0, 30.0), null).AddStaticText(Lang.Get("Mods", new object[] { this.entry.mods.Length }), font, leftElement = leftElement.BelowCopy(0.0, 0.0, 0.0, 0.0), null)
				.AddStaticText(mods, font, EnumTextOrientation.Left, leftElement.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedSize(540.0, 30.0), "mods");
			this.ElementComposer.GetStaticText("mods").Bounds.CalcWorldBounds();
			double height = this.ElementComposer.GetStaticText("mods").GetTextHeight() / (double)RuntimeEnv.GUIScale;
			if (this.entry.hasPassword)
			{
				this.ElementComposer.AddIf(this.entry.hasPassword).AddStaticText(Lang.Get("Password", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftElement = leftElement.BelowCopy(0.0, height - 20.0, 0.0, 0.0), null).AddTextInput(leftElement.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedOffset(0.0, -3.0)
					.WithFixedSize(540.0, 30.0), null, null, "password")
					.EndIf();
			}
			else
			{
				leftElement = leftElement.FlatCopy();
				leftElement.fixedY += height;
			}
			double joinlen = CairoFont.ButtonText().GetTextExtents(Lang.Get("Join Server", Array.Empty<object>())).Width / (double)RuntimeEnv.GUIScale;
			this.ElementComposer.AddButton(Lang.Get("Back", Array.Empty<object>()), new ActionConsumable(this.OnBack), ElementBounds.Fixed(0, 0).FixedUnder(leftElement, 20.0).WithAlignment(EnumDialogArea.LeftFixed)
				.WithFixedAlignmentOffset(0.0, 0.0)
				.WithFixedPadding(10.0, 2.0), EnumButtonStyle.Normal, null).AddButton(Lang.Get("Add to Favorites", Array.Empty<object>()), new ActionConsumable(this.OnAddToFavorites), ElementBounds.Fixed(0, 0).FixedUnder(leftElement, 20.0).WithAlignment(EnumDialogArea.RightFixed)
				.WithFixedAlignmentOffset(-30.0 - joinlen, 0.0)
				.WithFixedPadding(10.0, 2.0), EnumButtonStyle.Normal, null).AddButton(Lang.Get("Join Server", Array.Empty<object>()), new ActionConsumable(this.OnJoin), ElementBounds.Fixed(0, 0).FixedUnder(leftElement, 20.0).WithAlignment(EnumDialogArea.RightFixed)
				.WithFixedPadding(10.0, 2.0), EnumButtonStyle.Normal, null)
				.EndChildElements()
				.Compose(true);
		}

		private bool OnAddToFavorites()
		{
			List<string> entries = ClientSettings.Inst.GetStringListSetting("multiplayerservers", new List<string>());
			string uri = this.entry.serverIp;
			string name = this.entry.serverName.Replace(",", "");
			GuiElementTextInput textInput = this.ElementComposer.GetTextInput("password");
			string password = ((textInput != null) ? textInput.GetText().Replace(",", "&comma;") : null);
			entries.Add(string.Concat(new string[] { name, ",", uri, ",", password }));
			ClientSettings.Inst.Strings["multiplayerservers"] = entries;
			this.ScreenManager.LoadAndCacheScreen(typeof(GuiScreenMultiplayer));
			return true;
		}

		private bool OnBack()
		{
			this.ScreenManager.LoadAndCacheScreen(typeof(GuiScreenPublicServers));
			return true;
		}

		private bool OnJoin()
		{
			if (!this.entry.hasPassword)
			{
				this.ScreenManager.ConnectToMultiplayer(this.entry.serverIp, null);
			}
			else
			{
				this.ScreenManager.ConnectToMultiplayer(this.entry.serverIp, this.ElementComposer.GetTextInput("password").GetText());
			}
			return true;
		}

		private ServerListEntry entry;
	}
}
