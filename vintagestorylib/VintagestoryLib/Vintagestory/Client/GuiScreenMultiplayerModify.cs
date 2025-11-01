using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;

namespace Vintagestory.Client
{
	public class GuiScreenMultiplayerModify : GuiScreen
	{
		public GuiScreenMultiplayerModify(MultiplayerServerEntry serverentry, ScreenManager screenManager, GuiScreen parentScreen)
			: base(screenManager, parentScreen)
		{
			this.ShowMainMenu = true;
			this.serverentry = serverentry;
			this.InitGui();
		}

		private void InitGui()
		{
			double saveWidth = CairoFont.ButtonText().GetTextExtents(Lang.Get("general-save", Array.Empty<object>())).Width + 20.0;
			this.ElementComposer = base.dialogBase("mainmenu-multiplayernewserver", -1.0, 330.0).AddStaticText(Lang.Get("multiplayer-modifyserver", Array.Empty<object>()), CairoFont.WhiteSmallishText(), ElementStdBounds.Rowed(0f, 0.0, EnumDialogArea.LeftFixed).WithFixedWidth(300.0), null).AddStaticText(Lang.Get("multiplayer-servername", Array.Empty<object>()), CairoFont.WhiteSmallText(), ElementStdBounds.Rowed(1.05f, 0.0, EnumDialogArea.LeftFixed).WithFixedWidth(300.0), null)
				.AddTextInput(ElementStdBounds.Rowed(1f, 0.0, EnumDialogArea.RightFixed).WithFixedSize(300.0, 30.0).WithFixedAlignmentOffset(-35.0, 0.0), null, null, "servername")
				.AddStaticText(Lang.Get("multiplayer-address", Array.Empty<object>()), CairoFont.WhiteSmallText(), ElementStdBounds.Rowed(1.74f, 0.0, EnumDialogArea.LeftFixed).WithFixedWidth(300.0), null)
				.AddTextInput(ElementStdBounds.Rowed(1.7f, 0.0, EnumDialogArea.RightFixed).WithFixedSize(300.0, 30.0).WithFixedAlignmentOffset(-35.0, 0.0), null, null, "serverhost")
				.AddIconButton("copy", new Action<bool>(this.OnCopyServer), ElementStdBounds.Rowed(1.7f, 0.0, EnumDialogArea.RightFixed).WithFixedSize(30.0, 30.0), null)
				.AddHoverText(Lang.Get("Copies the server address to your clipboard", Array.Empty<object>()), CairoFont.WhiteDetailText(), 200, ElementStdBounds.Rowed(1.7f, 0.0, EnumDialogArea.RightFixed).WithFixedSize(30.0, 30.0), null)
				.AddStaticText(Lang.Get("multiplayer-serverpassword", Array.Empty<object>()), CairoFont.WhiteSmallText(), ElementStdBounds.Rowed(2.47f, 0.0, EnumDialogArea.LeftFixed).WithFixedWidth(300.0), null)
				.AddTextInput(ElementStdBounds.Rowed(2.4f, 0.0, EnumDialogArea.RightFixed).WithFixedSize(300.0, 30.0).WithFixedAlignmentOffset(-35.0, 0.0), null, null, "serverpassword")
				.AddDynamicText("", CairoFont.WhiteSmallText().WithColor(GuiStyle.ErrorTextColor), ElementStdBounds.Rowed(3.5f, 0.0, EnumDialogArea.None).WithFixedSize(550.0, 30.0), "errorLine")
				.AddButton(Lang.Get("general-cancel", Array.Empty<object>()), new ActionConsumable(this.OnCancel), ElementStdBounds.Rowed(4.2f, 0.0, EnumDialogArea.LeftFixed).WithFixedPadding(10.0, 2.0), EnumButtonStyle.Normal, null)
				.AddButton(Lang.Get("general-delete", Array.Empty<object>()), new ActionConsumable(this.OnDelete), ElementStdBounds.Rowed(4.2f, 0.0, EnumDialogArea.RightFixed).WithFixedAlignmentOffset(-saveWidth - 20.0, 0.0).WithFixedPadding(10.0, 2.0), EnumButtonStyle.Normal, null)
				.AddButton(Lang.Get("general-save", Array.Empty<object>()), new ActionConsumable(this.OnSave), ElementStdBounds.Rowed(4.2f, 0.0, EnumDialogArea.RightFixed).WithFixedPadding(10.0, 2.0), EnumButtonStyle.Normal, null)
				.EndChildElements()
				.Compose(true);
			this.ElementComposer.GetTextInput("servername").SetValue(this.serverentry.name, true);
			this.ElementComposer.GetTextInput("serverhost").SetValue(this.serverentry.host, true);
			this.ElementComposer.GetTextInput("serverpassword").HideCharacters();
			this.ElementComposer.GetTextInput("serverpassword").SetValue(this.serverentry.password, true);
		}

		private void OnCopyServer(bool ok)
		{
			ScreenManager.Platform.XPlatInterface.SetClipboardText(this.serverentry.host);
		}

		private bool OnSave()
		{
			this.serverentry.name = this.ElementComposer.GetTextInput("servername").GetText().Replace(",", " ");
			this.serverentry.host = this.ElementComposer.GetTextInput("serverhost").GetText().Replace(",", " ");
			string password = this.ElementComposer.GetTextInput("serverpassword").GetText().Replace(",", "&comma;");
			string error;
			NetUtil.getUriInfo(this.serverentry.host, out error);
			if (error != null)
			{
				this.ElementComposer.GetDynamicText("errorLine").SetNewText(error, true, false, false);
				return true;
			}
			if (this.serverentry.host.Length == 0)
			{
				this.ElementComposer.GetDynamicText("errorLine").SetNewText(Lang.Get("No host / ip address supplied", Array.Empty<object>()), true, false, false);
				return true;
			}
			ClientSettings.Inst.GetStringListSetting("multiplayerservers", new List<string>())[this.serverentry.index] = string.Concat(new string[]
			{
				this.serverentry.name,
				",",
				this.serverentry.host,
				",",
				password
			});
			ClientSettings.Inst.Save(true);
			this.ScreenManager.LoadAndCacheScreen(typeof(GuiScreenMultiplayer));
			return true;
		}

		private bool OnDelete()
		{
			this.ScreenManager.LoadScreen(new GuiScreenConfirmAction(Lang.Get("multiplayer-deleteserver-confirmation", new object[] { this.serverentry.name }), new Action<bool>(this.OnDidConfirmDelete), this.ScreenManager, this, false));
			return true;
		}

		private void OnDidConfirmDelete(bool confirm)
		{
			if (confirm)
			{
				List<string> entries = ClientSettings.Inst.GetStringListSetting("multiplayerservers", new List<string>());
				for (int i = 0; i < entries.Count; i++)
				{
					if (entries[i] == string.Concat(new string[]
					{
						this.serverentry.name,
						",",
						this.serverentry.host,
						",",
						this.serverentry.password
					}) || entries[i] == this.serverentry.name + "," + this.serverentry.host)
					{
						entries.RemoveAt(i);
						break;
					}
				}
				ClientSettings.Inst.Strings["multiplayerservers"] = entries;
				ClientSettings.Inst.Save(true);
				this.ScreenManager.LoadAndCacheScreen(typeof(GuiScreenMultiplayer));
				return;
			}
			this.ScreenManager.LoadScreen(this);
		}

		private bool OnCancel()
		{
			this.ScreenManager.LoadAndCacheScreen(typeof(GuiScreenMultiplayer));
			return true;
		}

		public override void OnScreenLoaded()
		{
			this.InitGui();
		}

		private MultiplayerServerEntry serverentry;
	}
}
