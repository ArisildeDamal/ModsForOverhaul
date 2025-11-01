using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.Util;

namespace Vintagestory.Client
{
	public class GuiScreenServerResetWorld : GuiScreen
	{
		public GuiScreenServerResetWorld(ScreenManager screenManager, GuiScreen parentScreen)
			: base(screenManager, parentScreen)
		{
			this.backend = new ServerCtrlBackendInterface();
			this.ShowMainMenu = true;
			this.InitGui();
		}

		private void InitGui()
		{
			double width = CairoFont.ButtonText().GetTextExtents(Lang.Get("general-save", Array.Empty<object>())).Width;
			ElementBounds rowLeft = ElementBounds.Fixed(0.0, 0.0, 300.0, 35.0);
			ElementBounds rowRight = ElementBounds.Fixed(330.0, 0.0, 300.0, 25.0);
			string[] playstyleValues = new string[] { "test" };
			string[] playstyleNames = new string[] { "test" };
			this.ElementComposer = ScreenManager.GuiComposers.Create("mainmenu-servercontrol-dashboard", ElementStdBounds.MainScreenRightPart()).AddImageBG(ElementBounds.Fill, GuiElement.dirtTextureName, 1f, 1f, 0.125f).BeginChildElements(ElementBounds.Fixed(EnumDialogArea.CenterFixed, 0.0, 110.0, 550.0, 600.0))
				.AddStaticText(Lang.Get("serverctrl-dashboard", Array.Empty<object>()), CairoFont.WhiteSmallText(), rowLeft, null)
				.AddStaticText(Lang.Get("serverctrl-serverstatus", Array.Empty<object>()), CairoFont.WhiteSmallText(), rowLeft = rowLeft.BelowCopy(0.0, 30.0, 0.0, 0.0), null)
				.AddRichtext(Lang.Get("Loading...", Array.Empty<object>()), CairoFont.WhiteSmallText(), rowLeft = rowLeft.BelowCopy(0.0, 30.0, 0.0, 0.0), null)
				.AddStaticText(Lang.Get("serverctrl-servername", Array.Empty<object>()), CairoFont.WhiteSmallText(), rowLeft = rowLeft.BelowCopy(0.0, 30.0, 0.0, 0.0), null)
				.AddStaticText(Lang.Get("serverctrl-serverdescription", Array.Empty<object>()), CairoFont.WhiteSmallText(), rowLeft = rowLeft.BelowCopy(0.0, 0.0, 0.0, 0.0), null)
				.AddStaticText(Lang.Get("serverctrl-whitelisted", Array.Empty<object>()), CairoFont.WhiteSmallText(), rowLeft = rowLeft.BelowCopy(0.0, 0.0, 0.0, 0.0), null)
				.AddStaticText(Lang.Get("serverctrl-serverpassword", Array.Empty<object>()), CairoFont.WhiteSmallText(), rowLeft = rowLeft.BelowCopy(0.0, 0.0, 0.0, 0.0), null)
				.AddStaticText(Lang.Get("serverctrl-motd", Array.Empty<object>()), CairoFont.WhiteSmallText(), rowLeft = rowLeft.BelowCopy(0.0, 0.0, 0.0, 0.0), null)
				.AddStaticText(Lang.Get("serverctrl-advertise", Array.Empty<object>()), CairoFont.WhiteSmallText(), rowLeft = rowLeft.BelowCopy(0.0, 0.0, 0.0, 0.0), null)
				.AddStaticText(Lang.Get("serverctrl-seed", Array.Empty<object>()), CairoFont.WhiteSmallText(), rowLeft = rowLeft.BelowCopy(0.0, 0.0, 0.0, 0.0), null)
				.AddStaticText(Lang.Get("serverctrl-playstyle", Array.Empty<object>()), CairoFont.WhiteSmallText(), rowLeft.BelowCopy(0.0, 0.0, 0.0, 0.0), null)
				.AddTextInput(rowRight = rowRight.BelowCopy(0.0, 40.0, 0.0, 0.0), null, null, "servername")
				.AddTextInput(rowRight = rowRight.BelowCopy(0.0, 10.0, 0.0, 0.0), null, null, "serverdescription")
				.AddSwitch(new Action<bool>(this.onToggleWhiteListed), rowRight = rowRight.BelowCopy(0.0, 10.0, 0.0, 0.0), "whiteListedSwitch", 25.0, 4.0)
				.AddTextInput(rowRight = rowRight.BelowCopy(0.0, 10.0, 0.0, 0.0).WithFixedWidth(300.0), null, null, "serverpassword")
				.AddTextInput(rowRight = rowRight.BelowCopy(0.0, 10.0, 0.0, 0.0), null, null, "motd")
				.AddSwitch(new Action<bool>(this.onToggleAdvertise), rowRight = rowRight.BelowCopy(0.0, 10.0, 0.0, 0.0), "advertiseSwith", 25.0, 4.0)
				.AddTextInput(rowRight = rowRight.BelowCopy(0.0, 10.0, 0.0, 0.0).WithFixedWidth(300.0), null, null, "seed")
				.AddDropDown(playstyleValues, playstyleNames, 0, new SelectionChangedDelegate(this.onPlayStyleChanged), rowRight.BelowCopy(0.0, 10.0, 0.0, 0.0), null)
				.AddButton(Lang.Get("general-cancel", Array.Empty<object>()), new ActionConsumable(this.OnCancel), ElementStdBounds.Rowed(5.2f, 0.0, EnumDialogArea.LeftFixed).WithFixedPadding(10.0, 2.0), EnumButtonStyle.Normal, null)
				.AddButton(Lang.Get("general-save", Array.Empty<object>()), new ActionConsumable(this.OnSave), ElementStdBounds.Rowed(5.2f, 0.0, EnumDialogArea.RightFixed).WithFixedPadding(10.0, 2.0), EnumButtonStyle.Normal, null)
				.EndChildElements()
				.Compose(true);
		}

		private void onPlayStyleChanged(string code, bool selected)
		{
			throw new NotImplementedException();
		}

		private void onToggleAdvertise(bool t1)
		{
			throw new NotImplementedException();
		}

		private void onToggleWhiteListed(bool t1)
		{
			throw new NotImplementedException();
		}

		private bool OnSave()
		{
			return true;
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

		private ServerCtrlBackendInterface backend;
	}
}
