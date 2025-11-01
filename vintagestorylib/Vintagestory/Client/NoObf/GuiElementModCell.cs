using System;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf
{
	public class GuiElementModCell : GuiElementTextBase, IGuiElementCell, IDisposable
	{
		ElementBounds IGuiElementCell.Bounds
		{
			get
			{
				return this.Bounds;
			}
		}

		public GuiElementModCell(ICoreClientAPI capi, ModCellEntry cell, ElementBounds bounds, IAsset warningIcon)
			: base(capi, "", null, bounds)
		{
			this.cell = cell;
			if (cell.TitleFont == null)
			{
				cell.TitleFont = CairoFont.WhiteSmallishText();
			}
			if (cell.DetailTextFont == null)
			{
				cell.DetailTextFont = CairoFont.WhiteSmallText();
				cell.DetailTextFont.Color[3] *= 0.6;
			}
			this.modcellTexture = new LoadedTexture(capi);
			ModInfo info = cell.Mod.Info;
			if (((info != null) ? info.Dependencies : null) != null)
			{
				foreach (ModDependency dep in cell.Mod.Info.Dependencies)
				{
					if (dep.Version.Length != 0 && !(dep.Version == "*") && cell.Mod.Enabled && (dep.ModID == "game" || dep.ModID == "creative" || dep.ModID == "survival") && !GameVersion.IsCompatibleApiVersion(dep.Version))
					{
						this.warningIcon = warningIcon;
						this.warningTextTexture = capi.Gui.TextTexture.GenTextTexture(Lang.Get("mod-versionmismatch", new object[] { dep.Version, "1.21.5" }), CairoFont.WhiteDetailText(), new TextBackground
						{
							FillColor = GuiStyle.DialogLightBgColor,
							Padding = 3,
							Radius = GuiStyle.ElementBGRadius
						});
					}
				}
			}
			this.capi = capi;
		}

		private void Compose()
		{
			this.ComposeHover(true, ref this.leftHighlightTextureId);
			this.ComposeHover(false, ref this.rightHighlightTextureId);
			this.genOnTexture();
			ImageSurface surface = new ImageSurface(Format.Argb32, this.Bounds.OuterWidthInt, this.Bounds.OuterHeightInt);
			Context ctx = new Context(surface);
			double rightBoxWidth = GuiElement.scaled(GuiElementModCell.unscaledRightBoxWidth);
			this.Bounds.CalcWorldBounds();
			ModContainer mod = this.cell.Mod;
			bool flag = ((mod != null) ? mod.Info : null) != null && (mod == null || mod.Error == null);
			if (this.cell.DrawAsButton)
			{
				GuiElement.RoundRectangle(ctx, 0.0, 0.0, this.Bounds.OuterWidth, this.Bounds.OuterHeight, 0.0);
				ctx.SetSourceRGB(GuiStyle.DialogDefaultBgColor[0], GuiStyle.DialogDefaultBgColor[1], GuiStyle.DialogDefaultBgColor[2]);
				ctx.Fill();
			}
			double textOffset = 0.0;
			if (mod.Icon != null)
			{
				int imageSize = (int)(this.Bounds.InnerHeight - this.Bounds.absPaddingY * 2.0 - 10.0);
				textOffset = (double)(imageSize + 15);
				surface.Image(mod.Icon, (int)this.Bounds.absPaddingX + 5, (int)this.Bounds.absPaddingY + 5, imageSize, imageSize);
			}
			this.Font = this.cell.TitleFont;
			this.titleTextheight = this.textUtil.AutobreakAndDrawMultilineTextAt(ctx, this.Font, this.cell.Title, this.Bounds.absPaddingX + textOffset, this.Bounds.absPaddingY, this.Bounds.InnerWidth - textOffset, EnumTextOrientation.Left);
			this.Font = this.cell.DetailTextFont;
			this.textUtil.AutobreakAndDrawMultilineTextAt(ctx, this.Font, this.cell.DetailText, this.Bounds.absPaddingX + textOffset, this.Bounds.absPaddingY + this.titleTextheight + this.Bounds.absPaddingY, this.Bounds.InnerWidth - textOffset, EnumTextOrientation.Left);
			if (this.cell.RightTopText != null)
			{
				TextExtents extents = this.Font.GetTextExtents(this.cell.RightTopText);
				this.textUtil.AutobreakAndDrawMultilineTextAt(ctx, this.Font, this.cell.RightTopText, this.Bounds.absPaddingX + this.Bounds.InnerWidth - extents.Width - rightBoxWidth - GuiElement.scaled(10.0), this.Bounds.absPaddingY + GuiElement.scaled((double)this.cell.RightTopOffY), extents.Width + 1.0, EnumTextOrientation.Right);
			}
			if (this.cell.DrawAsButton)
			{
				base.EmbossRoundRectangleElement(ctx, 0.0, 0.0, this.Bounds.OuterWidth, this.Bounds.OuterHeight, false, (int)GuiElement.scaled(4.0), 0);
			}
			if (!flag)
			{
				ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.5);
				GuiElement.RoundRectangle(ctx, 0.0, 0.0, this.Bounds.OuterWidth, this.Bounds.OuterHeight, 1.0);
				ctx.Fill();
			}
			double checkboxsize = GuiElement.scaled(this.unscaledSwitchSize);
			double padd = GuiElement.scaled(this.unscaledSwitchPadding);
			double x = this.Bounds.absPaddingX + this.Bounds.InnerWidth - GuiElement.scaled(0.0) - checkboxsize - padd;
			double y = this.Bounds.absPaddingY + this.Bounds.absPaddingY;
			if (this.showModifyIcons)
			{
				ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.2);
				GuiElement.RoundRectangle(ctx, x, y, checkboxsize, checkboxsize, 3.0);
				ctx.Fill();
				base.EmbossRoundRectangleElement(ctx, x, y, checkboxsize, checkboxsize, true, (int)GuiElement.scaled(2.0), 2);
			}
			if (this.warningIcon != null)
			{
				this.capi.Gui.DrawSvg(this.warningIcon, surface, (int)(x - GuiElement.scaled(3.0)), (int)(y + GuiElement.scaled(35.0)), (int)GuiElement.scaled(30.0), (int)GuiElement.scaled(30.0), new int?(ColorUtil.ColorFromRgba(255, 209, 74, 255)));
				this.capi.Gui.DrawSvg(this.capi.Assets.Get("textures/icons/excla.svg"), surface, (int)(x - GuiElement.scaled(3.0)), (int)(y + GuiElement.scaled(35.0)), (int)GuiElement.scaled(30.0), (int)GuiElement.scaled(30.0), new int?(-16777216));
			}
			base.generateTexture(surface, ref this.modcellTexture, true);
			ctx.Dispose();
			surface.Dispose();
		}

		private void genOnTexture()
		{
			double size = GuiElement.scaled(this.unscaledSwitchSize - 2.0 * this.unscaledSwitchPadding);
			ImageSurface surface = new ImageSurface(Format.Argb32, (int)size, (int)size);
			Context ctx = base.genContext(surface);
			GuiElement.RoundRectangle(ctx, 0.0, 0.0, size, size, 2.0);
			GuiElement.fillWithPattern(this.api, ctx, GuiElement.waterTextureName, false, false, 255, 1f);
			base.generateTexture(surface, ref this.switchOnTextureId, true);
			ctx.Dispose();
			surface.Dispose();
		}

		private void ComposeHover(bool left, ref int textureId)
		{
			ImageSurface surface = new ImageSurface(Format.Argb32, (int)this.Bounds.OuterWidth, (int)this.Bounds.OuterHeight);
			Context ctx = base.genContext(surface);
			double boxWidth = GuiElement.scaled(GuiElementModCell.unscaledRightBoxWidth);
			if (left)
			{
				ctx.NewPath();
				ctx.LineTo(0.0, 0.0);
				ctx.LineTo(this.Bounds.InnerWidth - boxWidth, 0.0);
				ctx.LineTo(this.Bounds.InnerWidth - boxWidth, this.Bounds.OuterHeight);
				ctx.LineTo(0.0, this.Bounds.OuterHeight);
				ctx.ClosePath();
			}
			else
			{
				ctx.NewPath();
				ctx.LineTo(this.Bounds.InnerWidth - boxWidth, 0.0);
				ctx.LineTo(this.Bounds.OuterWidth, 0.0);
				ctx.LineTo(this.Bounds.OuterWidth, this.Bounds.OuterHeight);
				ctx.LineTo(this.Bounds.InnerWidth - boxWidth, this.Bounds.OuterHeight);
				ctx.ClosePath();
			}
			ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.15);
			ctx.Fill();
			base.generateTexture(surface, ref textureId, true);
			ctx.Dispose();
			surface.Dispose();
		}

		public void UpdateCellHeight()
		{
			this.Bounds.CalcWorldBounds();
			double unscaledPadding = this.Bounds.absPaddingY / (double)RuntimeEnv.GUIScale;
			double boxwidth = this.Bounds.InnerWidth;
			ModContainer mod = this.cell.Mod;
			if (((mod != null) ? mod.Info : null) != null && mod.Icon != null)
			{
				int imageSize = (int)(this.Bounds.InnerHeight - this.Bounds.absPaddingY * 2.0 - 10.0);
				boxwidth -= (double)(imageSize + 10);
			}
			this.Font = this.cell.TitleFont;
			base.Text = this.cell.Title;
			this.titleTextheight = this.textUtil.GetMultilineTextHeight(this.Font, this.cell.Title, boxwidth, EnumLinebreakBehavior.Default) / (double)RuntimeEnv.GUIScale;
			this.Font = this.cell.DetailTextFont;
			base.Text = this.cell.DetailText;
			double detailTextHeight = this.textUtil.GetMultilineTextHeight(this.Font, this.cell.DetailText, boxwidth, EnumLinebreakBehavior.Default) / (double)RuntimeEnv.GUIScale;
			this.Bounds.fixedHeight = unscaledPadding + this.titleTextheight + unscaledPadding + detailTextHeight + unscaledPadding;
			if (this.showModifyIcons && this.Bounds.fixedHeight < 73.0)
			{
				this.Bounds.fixedHeight = 73.0;
			}
		}

		public void OnRenderInteractiveElements(ICoreClientAPI api, float deltaTime)
		{
			if (this.modcellTexture.TextureId == 0)
			{
				this.Compose();
			}
			api.Render.Render2DTexturePremultipliedAlpha(this.modcellTexture.TextureId, (float)((int)this.Bounds.absX), (float)((int)this.Bounds.absY), (float)this.Bounds.OuterWidthInt, (float)this.Bounds.OuterHeightInt, 50f, null);
			int mx = api.Input.MouseX;
			int my = api.Input.MouseY;
			Vec2d pos = this.Bounds.PositionInside(mx, my);
			ModContainer mod = this.cell.Mod;
			if (((mod != null) ? mod.Info : null) != null && pos != null)
			{
				if (pos.X > this.Bounds.InnerWidth - GuiElement.scaled(GuiElementMainMenuCell.unscaledRightBoxWidth))
				{
					api.Render.Render2DTexturePremultipliedAlpha(this.rightHighlightTextureId, (double)((int)this.Bounds.absX), (double)((int)this.Bounds.absY), this.Bounds.OuterWidth, this.Bounds.OuterHeight, 50f, null);
				}
				else
				{
					api.Render.Render2DTexturePremultipliedAlpha(this.leftHighlightTextureId, (double)((int)this.Bounds.absX), (double)((int)this.Bounds.absY), this.Bounds.OuterWidth, this.Bounds.OuterHeight, 50f, null);
				}
			}
			if (this.On)
			{
				double size = GuiElement.scaled(this.unscaledSwitchSize - 2.0 * this.unscaledSwitchPadding);
				double padding = GuiElement.scaled(this.unscaledSwitchPadding);
				double x = this.Bounds.renderX + this.Bounds.InnerWidth - size + padding - GuiElement.scaled(5.0);
				double y = this.Bounds.renderY + GuiElement.scaled(8.0) + padding;
				api.Render.Render2DTexturePremultipliedAlpha(this.switchOnTextureId, x, y, (double)((int)size), (double)((int)size), 50f, null);
			}
			else
			{
				api.Render.Render2DTexturePremultipliedAlpha(this.rightHighlightTextureId, (double)((int)this.Bounds.renderX), (double)((int)this.Bounds.renderY), this.Bounds.OuterWidth, this.Bounds.OuterHeight, 50f, null);
				api.Render.Render2DTexturePremultipliedAlpha(this.leftHighlightTextureId, (double)((int)this.Bounds.renderX), (double)((int)this.Bounds.renderY), this.Bounds.OuterWidth, this.Bounds.OuterHeight, 50f, null);
			}
			if (this.warningTextTexture != null && this.IsPositionInside(api.Input.MouseX, api.Input.MouseY))
			{
				api.Render.GlScissorFlag(false);
				api.Render.Render2DTexturePremultipliedAlpha(this.warningTextTexture.TextureId, (float)(mx + 25), (float)(my + 10), (float)this.warningTextTexture.Width, (float)this.warningTextTexture.Height, 500f, null);
				api.Render.GlScissorFlag(true);
			}
		}

		public override void Dispose()
		{
			base.Dispose();
			LoadedTexture loadedTexture = this.modcellTexture;
			if (loadedTexture != null)
			{
				loadedTexture.Dispose();
			}
			LoadedTexture loadedTexture2 = this.warningTextTexture;
			if (loadedTexture2 != null)
			{
				loadedTexture2.Dispose();
			}
			this.api.Render.GLDeleteTexture(this.leftHighlightTextureId);
			this.api.Render.GLDeleteTexture(this.rightHighlightTextureId);
			this.api.Render.GLDeleteTexture(this.switchOnTextureId);
		}

		public void OnMouseUpOnElement(MouseEvent args, int elementIndex)
		{
			int mousex = this.api.Input.MouseX;
			int mousey = this.api.Input.MouseY;
			Vec2d vec2d = this.Bounds.PositionInside(mousex, mousey);
			this.api.Gui.PlaySound("menubutton_press", false, 1f);
			if (vec2d.X > this.Bounds.InnerWidth - GuiElement.scaled(GuiElementMainMenuCell.unscaledRightBoxWidth))
			{
				Action<int> onMouseDownOnCellRight = this.OnMouseDownOnCellRight;
				if (onMouseDownOnCellRight != null)
				{
					onMouseDownOnCellRight(elementIndex);
				}
				args.Handled = true;
				return;
			}
			Action<int> onMouseDownOnCellLeft = this.OnMouseDownOnCellLeft;
			if (onMouseDownOnCellLeft != null)
			{
				onMouseDownOnCellLeft(elementIndex);
			}
			args.Handled = true;
		}

		public void OnMouseMoveOnElement(MouseEvent args, int elementIndex)
		{
		}

		public void OnMouseDownOnElement(MouseEvent args, int elementIndex)
		{
		}

		public static double unscaledRightBoxWidth = 40.0;

		public ModCellEntry cell;

		private double titleTextheight;

		private bool showModifyIcons = true;

		public bool On;

		internal int leftHighlightTextureId;

		internal int rightHighlightTextureId;

		internal int switchOnTextureId;

		internal double unscaledSwitchPadding = 4.0;

		internal double unscaledSwitchSize = 25.0;

		private LoadedTexture modcellTexture;

		private LoadedTexture warningTextTexture;

		private IAsset warningIcon;

		private ICoreClientAPI capi;

		public Action<int> OnMouseDownOnCellLeft;

		public Action<int> OnMouseDownOnCellRight;
	}
}
