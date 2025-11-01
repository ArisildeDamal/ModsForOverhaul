using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;

namespace Vintagestory.Client
{
	public class GuiScreenMultiplayerNewServer : GuiScreen
	{
		public GuiScreenMultiplayerNewServer(ScreenManager screenManager, GuiScreen parentScreen)
			: base(screenManager, parentScreen)
		{
			this.ShowMainMenu = true;
			this.InitGui();
		}

		private void InitGui()
		{
			this.ElementComposer = base.dialogBase("mainmenu-multiplayernewserver", -1.0, 330.0).AddStaticText(Lang.Get("multiplayer-addserver", Array.Empty<object>()), CairoFont.WhiteSmallishText(), ElementStdBounds.Rowed(0f, 0.0, EnumDialogArea.LeftFixed).WithFixedWidth(300.0), null).AddStaticText(Lang.Get("multiplayer-servername", Array.Empty<object>()), CairoFont.WhiteSmallText(), ElementStdBounds.Rowed(1.07f, 0.0, EnumDialogArea.LeftFixed).WithFixedWidth(300.0), null)
				.AddTextInput(ElementStdBounds.Rowed(1f, 0.0, EnumDialogArea.RightFixed).WithFixedSize(300.0, 30.0), null, null, "servername")
				.AddStaticText(Lang.Get("multiplayer-address", Array.Empty<object>()), CairoFont.WhiteSmallText(), ElementStdBounds.Rowed(1.77f, 0.0, EnumDialogArea.LeftFixed).WithFixedWidth(300.0), null)
				.AddTextInput(ElementStdBounds.Rowed(1.7f, 0.0, EnumDialogArea.RightFixed).WithFixedSize(300.0, 30.0), null, null, "serverhost")
				.AddStaticText(Lang.Get("multiplayer-serverpassword", Array.Empty<object>()), CairoFont.WhiteSmallText(), ElementStdBounds.Rowed(2.47f, 0.0, EnumDialogArea.LeftFixed).WithFixedWidth(300.0), null)
				.AddTextInput(ElementStdBounds.Rowed(2.4f, 0.0, EnumDialogArea.RightFixed).WithFixedSize(300.0, 30.0), null, null, "serverpassword")
				.AddDynamicText("", CairoFont.WhiteSmallText().WithColor(GuiStyle.ErrorTextColor), ElementStdBounds.Rowed(3.5f, 0.0, EnumDialogArea.None).WithFixedSize(550.0, 30.0), "errorLine")
				.AddButton(Lang.Get("general-back", Array.Empty<object>()), new ActionConsumable(this.OnBack), ElementStdBounds.Rowed(4.2f, 0.0, EnumDialogArea.LeftFixed).WithFixedPadding(10.0, 2.0), EnumButtonStyle.Normal, null)
				.AddButton(Lang.Get("general-create", Array.Empty<object>()), new ActionConsumable(this.OnCreate), ElementStdBounds.Rowed(4.2f, 0.0, EnumDialogArea.RightFixed).WithFixedPadding(10.0, 2.0), EnumButtonStyle.Normal, null)
				.EndChildElements()
				.Compose(true);
			this.ElementComposer.GetTextInput("serverpassword").HideCharacters();
		}

		private bool OnBack()
		{
			this.ScreenManager.LoadAndCacheScreen(typeof(GuiScreenMultiplayer));
			return true;
		}

		private bool OnCreate()
		{
			List<string> entries = ClientSettings.Inst.GetStringListSetting("multiplayerservers", new List<string>());
			string uri = this.ElementComposer.GetTextInput("serverhost").GetText().Replace(",", "");
			string name = this.ElementComposer.GetTextInput("servername").GetText().Replace(",", "");
			string password = this.ElementComposer.GetTextInput("serverpassword").GetText().Replace(",", "&comma;");
			string error;
			NetUtil.getUriInfo(uri, out error);
			if (error != null)
			{
				this.ElementComposer.GetDynamicText("errorLine").SetNewText(error, true, false, false);
				return true;
			}
			if (!this.LooksValidURI(uri))
			{
				this.ElementComposer.GetDynamicText("errorLine").SetNewText(Lang.Get("No host / ip address supplied", Array.Empty<object>()), true, false, false);
				return true;
			}
			if (name.Length == 0)
			{
				name = uri;
			}
			entries.Add(string.Concat(new string[] { name, ",", uri, ",", password }));
			ClientSettings.Inst.Strings["multiplayerservers"] = entries;
			this.ScreenManager.LoadAndCacheScreen(typeof(GuiScreenMultiplayer));
			return true;
		}

		private bool LooksValidURI(string uri)
		{
			if (uri.Length == 0)
			{
				return false;
			}
			if (uri == "[::1]")
			{
				return true;
			}
			try
			{
				new Uri(uri);
				return true;
			}
			catch (Exception)
			{
				string port = "";
				int colon = uri.LastIndexOf(':');
				if (colon >= 0 && colon < uri.Length - 1 && uri[colon + 1] >= '0' && uri[colon + 1] <= '9')
				{
					port = uri.Substring(colon);
					uri = uri.Substring(0, colon);
					if (uri.Length == 0)
					{
						return false;
					}
				}
				try
				{
					new Uri("https://" + uri + "/" + port);
				}
				catch (Exception)
				{
					return false;
				}
			}
			return true;
		}

		public override void OnScreenLoaded()
		{
			this.InitGui();
		}
	}
}
