using System;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.Client
{
	internal class GuiScreenConfirmAction : GuiScreen
	{
		public override bool ShouldDisposePreviousScreen
		{
			get
			{
				return false;
			}
		}

		public GuiScreenConfirmAction(string text, Action<bool> DidPressButton, ScreenManager screenManager, GuiScreen parentScreen, bool onlyCancel = false)
			: this("Please Confirm", text, "Cancel", "Confirm", DidPressButton, screenManager, parentScreen, null, onlyCancel)
		{
		}

		public GuiScreenConfirmAction(string title, string text, string cancelText, string confirmText, Action<bool> DidPressButton, ScreenManager screenManager, GuiScreen parentScreen, string composersubcode, bool onlyCancel = false)
			: base(screenManager, parentScreen)
		{
			this.DidPressButton = DidPressButton;
			this.ShowMainMenu = true;
			CairoFont font = CairoFont.WhiteSmallText().WithFontSize(17f).WithLineHeightMultiplier(1.25);
			double unscheight = screenManager.api.Gui.Text.GetMultilineTextHeight(font, text, GuiElement.scaled(650.0), EnumLinebreakBehavior.Default) / (double)RuntimeEnv.GUIScale;
			ElementBounds titleBounds = ElementStdBounds.Rowed(0f, 0.0, EnumDialogArea.LeftFixed).WithFixedWidth(400.0);
			ElementBounds btnBounds = ElementBounds.Fixed(0.0, unscheight + 90.0, 0.0, 0.0).WithFixedPadding(10.0, 2.0);
			this.ElementComposer = base.dialogBase("mainmenu-confirmaction" + composersubcode, -1.0, unscheight + 130.0).AddStaticText(Lang.Get(title, Array.Empty<object>()), CairoFont.WhiteSmallishText().WithWeight(FontWeight.Bold), titleBounds, null).AddRichtext(text, font, ElementBounds.FixedSize(650.0, 650.0).FixedUnder(titleBounds, 30.0), null)
				.AddButton(Lang.Get(cancelText, Array.Empty<object>()), new ActionConsumable(this.OnCancel), btnBounds, EnumButtonStyle.Normal, null)
				.AddIf(!onlyCancel)
				.AddButton(Lang.Get(confirmText, Array.Empty<object>()), new ActionConsumable(this.OnConfirm), btnBounds.FlatCopy().WithAlignment(EnumDialogArea.RightFixed), EnumButtonStyle.Normal, "confirmButton")
				.EndIf()
				.EndChildElements()
				.Compose(true);
		}

		private bool OnConfirm()
		{
			this.ElementComposer.GetButton("confirmButton").Enabled = false;
			this.DidPressButton(true);
			return true;
		}

		private bool OnCancel()
		{
			this.DidPressButton(false);
			return true;
		}

		private Action<bool> DidPressButton;
	}
}
