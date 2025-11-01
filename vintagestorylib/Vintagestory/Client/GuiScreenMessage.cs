using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.Client
{
	public class GuiScreenMessage : GuiScreen
	{
		public GuiScreenMessage(string title, string text, Action OnPressBack, ScreenManager screenManager, GuiScreen parentScreen)
			: base(screenManager, parentScreen)
		{
			this.DidPressOnBack = OnPressBack;
			this.ShowMainMenu = true;
			this.ElementComposer = base.dialogBase("mainmenu-message", -1.0, -1.0).AddStaticText(title, CairoFont.WhiteSmallishText(), ElementStdBounds.Rowed(0f, 0.0, EnumDialogArea.LeftFixed).WithFixedWidth(300.0), null).AddStaticText(text, CairoFont.WhiteSmallishText(), ElementStdBounds.Rowed(1f, 0.0, EnumDialogArea.LeftFixed).WithFixedWidth(500.0), null)
				.AddButton(Lang.Get("Back", Array.Empty<object>()), new ActionConsumable(this.OnBack), ElementStdBounds.Rowed(5.2f, 0.0, EnumDialogArea.RightFixed).WithFixedPadding(10.0, 2.0), EnumButtonStyle.Normal, null)
				.EndChildElements()
				.Compose(true);
		}

		private bool OnBack()
		{
			this.DidPressOnBack();
			return true;
		}

		private Action DidPressOnBack;
	}
}
