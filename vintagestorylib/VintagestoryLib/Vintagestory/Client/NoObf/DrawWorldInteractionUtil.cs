using System;
using System.Collections.Generic;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class DrawWorldInteractionUtil
	{
		public GuiComposer Composer
		{
			get
			{
				return this.Composers[this.composerKeyCode];
			}
		}

		public DrawWorldInteractionUtil(ICoreClientAPI capi, GuiDialog.DlgComposers composers, string composerSuffixCode)
		{
			this.capi = capi;
			this.Composers = composers;
			this.composerKeyCode = "worldInteractionHelp" + composerSuffixCode;
		}

		public void ComposeBlockWorldInteractionHelp(WorldInteraction[] wis)
		{
			if (wis == null || wis.Length == 0)
			{
				this.Composers.Remove(this.composerKeyCode);
				return;
			}
			this.capi.World.FrameProfiler.Mark("blockinteractionhelp-recomp-1");
			ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
			if (this.composer == null)
			{
				this.composer = this.capi.Gui.CreateCompo(this.composerKeyCode, dialogBounds);
			}
			else
			{
				this.composer.Clear(dialogBounds);
			}
			this.Composers[this.composerKeyCode] = this.composer;
			double lineHeight = GuiElement.scaled(this.UnscaledLineHeight);
			int i = 0;
			int j = 0;
			while (j < wis.Length)
			{
				WorldInteraction wi = wis[j];
				ItemStack[] stacks = wi.Itemstacks;
				if (stacks == null || wi.GetMatchingStacks == null)
				{
					goto IL_0159;
				}
				stacks = wi.GetMatchingStacks(wi, this.capi.World.Player.CurrentBlockSelection, this.capi.World.Player.CurrentEntitySelection);
				if (stacks != null && stacks.Length != 0)
				{
					goto IL_0159;
				}
				IL_0249:
				j++;
				continue;
				IL_0159:
				if (stacks != null || wi.ShouldApply == null || wi.ShouldApply(wi, this.capi.World.Player.CurrentBlockSelection, this.capi.World.Player.CurrentEntitySelection))
				{
					double yOffset = (double)i * (this.UnscaledLineHeight + 8.0);
					ElementBounds textBounds = ElementBounds.Fixed(0.0, yOffset, 600.0, 80.0);
					this.composer.AddIf(stacks != null && stacks.Length != 0).AddCustomRender(textBounds.FlatCopy(), delegate(float dt, ElementBounds bounds)
					{
						long index = this.capi.World.ElapsedMilliseconds / 1000L % (long)stacks.Length;
						float size = (float)lineHeight * 0.8f;
						this.capi.Render.RenderItemstackToGui(new DummySlot(stacks[(int)(checked((IntPtr)index))]), bounds.renderX + lineHeight / 2.0 + 1.0, bounds.renderY + lineHeight / 2.0, 100.0, size, ColorUtil.ColorFromRgba(this.Color), true, false, true);
					}).EndIf()
						.AddStaticCustomDraw(textBounds, delegate(Context ctx, ImageSurface surface, ElementBounds bounds)
						{
							this.drawHelp(ctx, surface, bounds, stacks, lineHeight, wi);
						});
					i++;
					goto IL_0249;
				}
				goto IL_0249;
			}
			this.capi.World.FrameProfiler.Mark("blockinteractionhelp-recomp-2");
			if (i == 0)
			{
				this.Composers.Remove(this.composerKeyCode);
				return;
			}
			this.composer.Compose(true);
			this.capi.World.FrameProfiler.Mark("blockinteractionhelp-recomp-3");
		}

		public void drawHelp(Context ctx, ImageSurface surface, ElementBounds currentBounds, ItemStack[] stacks, double lineheight, WorldInteraction wi)
		{
			this.capi.World.FrameProfiler.Mark("blockinteractionhelp-recomp-2.1");
			double x = 0.0;
			double y = currentBounds.drawY;
			double[] color = (double[])GuiStyle.DialogDefaultTextColor.Clone();
			color[0] = (color[0] + 1.0) / 2.0;
			color[1] = (color[1] + 1.0) / 2.0;
			color[2] = (color[2] + 1.0) / 2.0;
			CairoFont font = CairoFont.WhiteMediumText().WithColor(color).WithFontSize(this.FontSize)
				.WithStroke(GuiStyle.DarkBrownColor, 2.0);
			font.SetupContext(ctx);
			double textHeight = font.GetFontExtents().Height;
			double symbolspacing = 5.0;
			double pluswdith = font.GetTextExtents("+").Width;
			if ((stacks != null && stacks.Length != 0) || wi.RequireFreeHand)
			{
				GuiElement.RoundRectangle(ctx, x, y + 1.0, lineheight, lineheight, 3.5);
				ctx.SetSourceRGBA(color);
				ctx.LineWidth = 1.5;
				ctx.StrokePreserve();
				ctx.SetSourceRGBA(new double[] { 1.0, 1.0, 1.0, 0.5 });
				ctx.Fill();
				ctx.SetSourceRGBA(new double[] { 1.0, 1.0, 1.0, 1.0 });
				x += lineheight + symbolspacing + 1.0;
			}
			List<HotKey> hotkeys = new List<HotKey>();
			if (wi.HotKeyCodes != null)
			{
				foreach (string keycode in wi.HotKeyCodes)
				{
					HotKey hk = this.capi.Input.GetHotKeyByCode(keycode);
					if (hk != null)
					{
						hotkeys.Add(hk);
					}
				}
			}
			else
			{
				HotKey hk2 = this.capi.Input.GetHotKeyByCode(wi.HotKeyCode);
				if (hk2 != null)
				{
					hotkeys.Add(hk2);
				}
			}
			foreach (HotKey hk3 in hotkeys)
			{
				if (!(hk3.Code != "ctrl") || hk3.CurrentMapping.Ctrl)
				{
					x = this.DrawHotkey(hk3, x, y, ctx, font, lineheight, textHeight, pluswdith, symbolspacing, color);
				}
			}
			foreach (HotKey hk4 in hotkeys)
			{
				if (!(hk4.Code != "shift") || hk4.CurrentMapping.Shift)
				{
					x = this.DrawHotkey(hk4, x, y, ctx, font, lineheight, textHeight, pluswdith, symbolspacing, color);
				}
			}
			foreach (HotKey hk5 in hotkeys)
			{
				if (!(hk5.Code == "shift") && !(hk5.Code == "ctrl") && !hk5.CurrentMapping.Shift && !hk5.CurrentMapping.Ctrl)
				{
					x = this.DrawHotkey(hk5, x, y, ctx, font, lineheight, textHeight, pluswdith, symbolspacing, color);
				}
			}
			if (wi.MouseButton == EnumMouseButton.Left)
			{
				HotKey hk6 = this.capi.Input.GetHotKeyByCode("primarymouse");
				x = this.DrawHotkey(hk6, x, y, ctx, font, lineheight, textHeight, pluswdith, symbolspacing, color);
			}
			if (wi.MouseButton == EnumMouseButton.Right)
			{
				HotKey hk7 = this.capi.Input.GetHotKeyByCode("secondarymouse");
				x = this.DrawHotkey(hk7, x, y, ctx, font, lineheight, textHeight, pluswdith, symbolspacing, color);
			}
			this.capi.World.FrameProfiler.Mark("blockinteractionhelp-recomp-2.2");
			string text = ": " + Lang.Get(wi.ActionLangCode, Array.Empty<object>());
			this.capi.Gui.Text.DrawTextLine(ctx, font, text, x - 4.0, y + (lineheight - textHeight) / 2.0 + 2.0, false);
			this.ActualWidth = x + font.GetTextExtents(text).Width;
			this.capi.World.FrameProfiler.Mark("blockinteractionhelp-recomp-2.3");
		}

		private double DrawHotkey(HotKey hk, double x, double y, Context ctx, CairoFont font, double lineheight, double textHeight, double pluswdith, double symbolspacing, double[] color)
		{
			KeyCombination map = hk.CurrentMapping;
			if (map.IsMouseButton(map.KeyCode))
			{
				return this.DrawMouseButton(map.KeyCode - 240, x, y, ctx, font, lineheight, textHeight, pluswdith, symbolspacing, color);
			}
			if (map.Ctrl)
			{
				x = HotkeyComponent.DrawHotkey(this.capi, GlKeyNames.ToString(GlKeys.LControl), x, y, ctx, font, lineheight, textHeight, pluswdith, symbolspacing, 10.0, color);
			}
			if (map.Shift)
			{
				x = HotkeyComponent.DrawHotkey(this.capi, GlKeyNames.ToString(GlKeys.LShift), x, y, ctx, font, lineheight, textHeight, pluswdith, symbolspacing, 10.0, color);
			}
			x = HotkeyComponent.DrawHotkey(this.capi, map.PrimaryAsString(), x, y, ctx, font, lineheight, textHeight, pluswdith, symbolspacing, 10.0, color);
			return x;
		}

		private double DrawMouseButton(int button, double x, double y, Context ctx, CairoFont font, double lineheight, double textHeight, double pluswdith, double symbolspacing, double[] color)
		{
			if (button == 0 || button == 2)
			{
				if (x > 0.0)
				{
					this.capi.Gui.Text.DrawTextLine(ctx, font, "+", (double)((int)x) + symbolspacing, y + (double)((int)((lineheight - textHeight) / 2.0)) + 2.0, false);
					x += pluswdith + 2.0 * symbolspacing;
				}
				this.capi.Gui.Icons.DrawIcon(ctx, (button == 0) ? "leftmousebutton" : "rightmousebutton", x, y + 1.0, lineheight, lineheight, color);
				return x + lineheight + symbolspacing + 1.0;
			}
			string text = ((button == 1) ? "mb" : ("b" + button.ToString()));
			return HotkeyComponent.DrawHotkey(this.capi, text, x, y, ctx, font, lineheight, textHeight, pluswdith, symbolspacing, 8.0, color);
		}

		public void Dispose()
		{
			GuiComposer guiComposer = this.composer;
			if (guiComposer == null)
			{
				return;
			}
			guiComposer.Dispose();
		}

		private ICoreClientAPI capi;

		private GuiDialog.DlgComposers Composers;

		public double ActualWidth;

		private string composerKeyCode;

		public double UnscaledLineHeight = 30.0;

		public float FontSize = 20f;

		private GuiComposer composer;

		public Vec4f Color = ColorUtil.WhiteArgbVec;
	}
}
