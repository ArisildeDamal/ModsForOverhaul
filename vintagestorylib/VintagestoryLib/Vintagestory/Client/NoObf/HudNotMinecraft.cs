using System;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.Client.NoObf
{
	public class HudNotMinecraft : HudElement
	{
		public HudNotMinecraft(ICoreClientAPI capi)
			: base(capi)
		{
			capi.ChatCommands.Create("notminecraft").WithDescription("No, this is not Minecraft").HandleWith(new OnCommandDelegate(this.OnNotMinecraft));
		}

		private TextCommandResult OnNotMinecraft(TextCommandCallingArgs textCommandCallingArgs)
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

		public override void OnGuiOpened()
		{
			this.grassBlock = this.capi.World.GetBlock(new AssetLocation("soil-medium-normal"));
			this.dummySlot = new DummySlot(new ItemStack(this.grassBlock, 1));
			string text = "No, this is not Minecraft";
			double textWidth = CairoFont.WhiteSmallishText().GetTextExtents(text).Width / (double)RuntimeEnv.GUIScale;
			ElementBounds.Fixed(EnumDialogArea.RightBottom, 0.0, 0.0, 45.0, 20.0);
			ElementBounds textBounds = ElementBounds.Fixed(47.0, 13.0, 330.0, 40.0);
			ElementBounds hudbounds = ElementBounds.Fixed(EnumDialogArea.LeftTop, 0.0, 0.0, textWidth + 60.0, 50.0).WithFixedAlignmentOffset(10.0, 10.0);
			this.crossBounds = ElementBounds.Fixed(EnumDialogArea.LeftTop, 0.0, 0.0, 35.0, 35.0).WithFixedPadding(17.0, 17.0);
			this.crossBounds.WithParent(hudbounds);
			this.crossBounds.CalcWorldBounds();
			base.SingleComposer = this.capi.Gui.CreateCompo("notminecraftdialog", hudbounds).AddShadedDialogBG(ElementBounds.Fill, false, 5.0, 0.75f).BeginChildElements()
				.AddStaticText(text, CairoFont.WhiteSmallishText(), textBounds, null)
				.EndChildElements()
				.Compose(true);
			this.TryOpen();
			ClientMain clientMain = this.capi.World as ClientMain;
			clientMain.LastReceivedMilliseconds = clientMain.ElapsedMilliseconds;
			this.crossTexture = this.capi.Gui.Icons.GenTexture((int)this.crossBounds.InnerWidth, (int)this.crossBounds.InnerHeight, delegate(Context ctx, ImageSurface surface)
			{
				ctx.SetSourceRGBA(0.8, 0.0, 0.0, 1.0);
				this.capi.Gui.Icons.DrawCross(ctx, 0.0, 0.0, 4.0, this.crossBounds.InnerWidth - GuiElement.scaled(5.0), false);
			});
		}

		public override bool Focusable
		{
			get
			{
				return false;
			}
		}

		public override void OnRenderGUI(float deltaTime)
		{
			base.OnRenderGUI(deltaTime);
			if (this.IsOpened())
			{
				int size = (int)GuiElement.scaled(22.0);
				this.capi.Render.RenderItemstackToGui(this.dummySlot, this.crossBounds.drawX + this.crossBounds.InnerWidth / 2.0, this.crossBounds.drawY + this.crossBounds.InnerHeight / 2.0, 50.0, (float)size, -1, true, false, true);
				this.capi.Render.Render2DLoadedTexture(this.crossTexture, (float)((int)this.crossBounds.drawX), (float)((int)this.crossBounds.drawY), 200f);
			}
		}

		public override void Dispose()
		{
			base.Dispose();
		}

		private LoadedTexture crossTexture;

		private Block grassBlock;

		private ItemSlot dummySlot;

		private ElementBounds crossBounds;
	}
}
