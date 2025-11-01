using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.Client.NoObf
{
	public class GuiDialogFirstlaunchInfo : GuiDialog
	{
		public override string ToggleKeyCombinationCode
		{
			get
			{
				return "firstlaunchinfo";
			}
		}

		public GuiDialogFirstlaunchInfo(ICoreClientAPI capi)
			: base(capi)
		{
			this.Compose();
			capi.ChatCommands.Create("firstlaunchinfo").WithDescription("Show the first launch info dialog").HandleWith(new OnCommandDelegate(this.OnCmd));
		}

		private TextCommandResult OnCmd(TextCommandCallingArgs textCommandCallingArgs)
		{
			if (this.IsOpened())
			{
				this.TryClose();
			}
			else
			{
				this.TryOpen();
			}
			return TextCommandResult.Success("", null);
		}

		private void Compose()
		{
			string code = ((this.playstyle == "creativebuilding") ? Lang.Get("start-creativeintro", Array.Empty<object>()) : Lang.Get("start-survivalintro", Array.Empty<object>()));
			CairoFont font = CairoFont.WhiteSmallText().WithLineHeightMultiplier(1.149999976158142);
			RichTextComponentBase[] comps = VtmlUtil.Richtextify(this.capi, code, font, new Action<LinkTextComponent>(this.didClickLink));
			ElementBounds bounds = ElementBounds.Fixed(0.0, 0.0, 400.0, 300.0);
			bounds.ParentBounds = ElementBounds.Empty;
			GuiElementRichtext elem = new GuiElementRichtext(this.capi, comps, bounds);
			elem.BeforeCalcBounds();
			bounds.ParentBounds = null;
			float y = (float)(elem.Bounds.fixedY + elem.Bounds.fixedHeight);
			base.ClearComposers();
			base.SingleComposer = this.capi.Gui.CreateCompo("helpdialog", ElementStdBounds.AutosizedMainDialog).AddShadedDialogBG(ElementStdBounds.DialogBackground().WithFixedPadding(GuiStyle.ElementToDialogPadding, GuiStyle.ElementToDialogPadding), false, 5.0, 0.75f).BeginChildElements()
				.AddInteractiveElement(elem, null)
				.AddSmallButton(Lang.Get("button-close", Array.Empty<object>()), new ActionConsumable(this.OnClose), ElementStdBounds.MenuButton((y + 50f) / 80f, EnumDialogArea.CenterFixed).WithAlignment(EnumDialogArea.RightFixed).WithFixedPadding(6.0), EnumButtonStyle.Normal, null)
				.AddSmallButton(Lang.Get("button-close-noshow", Array.Empty<object>()), new ActionConsumable(this.OnCloseAndDontShow), ElementStdBounds.MenuButton((y + 50f) / 80f, EnumDialogArea.CenterFixed).WithAlignment(EnumDialogArea.LeftFixed).WithFixedPadding(6.0), EnumButtonStyle.Normal, null)
				.EndChildElements()
				.Compose(true);
		}

		private void didClickLink(LinkTextComponent component)
		{
			this.TryClose();
			component.HandleLink();
		}

		public override void OnGuiOpened()
		{
			this.Compose();
			base.OnGuiOpened();
		}

		private bool OnCloseAndDontShow()
		{
			this.TryClose();
			if (this.playstyle == "creativebuilding")
			{
				ClientSettings.ShowCreativeHelpDialog = false;
			}
			else
			{
				ClientSettings.ShowSurvivalHelpDialog = false;
			}
			return true;
		}

		private bool OnClose()
		{
			this.TryClose();
			return true;
		}

		public override void OnLevelFinalize()
		{
			this.playstyle = (this.capi.World as ClientMain).ServerInfo.Playstyle;
		}

		private string playstyle;
	}
}
