using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.Client
{
	public class GuiScreenDisconnected : GuiScreen
	{
		public GuiScreenDisconnected(string reason, ScreenManager screenManager, GuiScreen parentScreen, string title = "server-disconnected")
			: base(screenManager, parentScreen)
		{
			ScreenManager.GuiComposers.ClearCache();
			this.ElementComposer = ScreenManager.GuiComposers.Create("mainmenu-disconnected", ElementBounds.Fixed(EnumDialogArea.CenterTop, 0.0, 20.0, 710.0, 330.0).WithAlignment(EnumDialogArea.CenterMiddle).WithFixedMargin(5.0)).AddShadedDialogBG(ElementBounds.Fill, false, 5.0, 1f).AddRichtext(Lang.Get(title, Array.Empty<object>()), CairoFont.WhiteSmallishText().WithOrientation(EnumTextOrientation.Center), ElementBounds.Fixed(EnumDialogArea.CenterTop, 0.0, 20.0, 690.0, 60.0), new Action<LinkTextComponent>(this.didClickLink), "centertext")
				.AddRichtext(reason, CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center), ElementBounds.Fixed(EnumDialogArea.CenterTop, 0.0, 65.0, 690.0, 450.0), new Action<LinkTextComponent>(this.didClickLink), "centertextreason")
				.AddButton(Lang.Get("Back to main menu", Array.Empty<object>()), new ActionConsumable(this.OnBack), ElementStdBounds.MenuButton(4.5f, EnumDialogArea.CenterFixed).WithFixedPadding(5.0, 3.0).WithFixedMargin(5.0), EnumButtonStyle.Normal, null)
				.EndChildElements();
			this.ElementComposer.GetRichtext("centertextreason").MaxHeight = 450;
			this.ElementComposer.Compose(true);
		}

		private void didClickLink(LinkTextComponent link)
		{
			this.ScreenManager.StartMainMenu();
			ScreenManager.EnqueueMainThreadTask(delegate
			{
				this.ScreenManager.api.Gui.OpenLink(link.Href);
			});
		}

		private bool OnBack()
		{
			this.ScreenManager.StartMainMenu();
			return true;
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
	}
}
