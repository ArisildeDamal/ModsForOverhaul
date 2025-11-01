using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.Client.NoObf
{
	internal class GuiDialogMacroEditor : GuiDialog
	{
		internal IMacroBase SelectedMacro
		{
			get
			{
				IMacroBase macro;
				(this.capi.World as ClientMain).macroManager.MacrosByIndex.TryGetValue(this.selectedIndex, out macro);
				return macro;
			}
		}

		public override string ToggleKeyCombinationCode
		{
			get
			{
				return "macroeditor";
			}
		}

		public GuiDialogMacroEditor(ICoreClientAPI capi)
			: base(capi)
		{
			this.skillItems = new List<SkillItem>();
			this.ComposeDialog();
		}

		private void LoadSkillList()
		{
			this.skillItems.Clear();
			for (int i = 0; i < this.cols * this.rows; i++)
			{
				IMacroBase j;
				(this.capi.World as ClientMain).macroManager.MacrosByIndex.TryGetValue(i, out j);
				SkillItem sk;
				if (j == null)
				{
					sk = new SkillItem();
				}
				else
				{
					if (j.iconTexture == null)
					{
						(j as Macro).GenTexture(this.capi, (int)GuiElementPassiveItemSlot.unscaledSlotSize);
					}
					sk = new SkillItem
					{
						Code = new AssetLocation(j.Code),
						Name = j.Name,
						Hotkey = j.KeyCombination,
						Texture = j.iconTexture
					};
				}
				this.skillItems.Add(sk);
			}
		}

		private void ComposeDialog()
		{
			this.LoadSkillList();
			this.selectedIndex = 0;
			this.currentSkillitem = this.skillItems[0];
			int spacing = 5;
			double size = GuiElementPassiveItemSlot.unscaledSlotSize + GuiElementItemSlotGridBase.unscaledSlotPadding;
			double innerWidth = (double)this.cols * size;
			ElementBounds macroBounds = ElementBounds.Fixed(0.0, 30.0, innerWidth, (double)this.rows * size);
			ElementBounds macroInsetBounds = macroBounds.ForkBoundingParent(3.0, 6.0, 3.0, 3.0);
			double halfWidth = innerWidth / 2.0 - 5.0;
			ElementBounds nameBounds = ElementBounds.FixedSize(halfWidth, 30.0).FixedUnder(macroInsetBounds, (double)(spacing + 10));
			ElementBounds hotkeyBounds = ElementBounds.Fixed(innerWidth / 2.0 + 8.0, 0.0, halfWidth, 30.0).FixedUnder(macroInsetBounds, (double)(spacing + 10));
			ElementBounds nameInputBounds = ElementBounds.FixedSize(halfWidth, 30.0).FixedUnder(nameBounds, (double)(spacing - 10));
			ElementBounds hotkeyInputBounds = ElementBounds.Fixed(innerWidth / 2.0 + 8.0, 0.0, halfWidth, 30.0).FixedUnder(hotkeyBounds, (double)(spacing - 10));
			ElementBounds commmandsBounds = ElementBounds.FixedSize(300.0, 30.0).FixedUnder(nameInputBounds, (double)(spacing + 10));
			ElementBounds textAreaBounds = ElementBounds.Fixed(0.0, 0.0, innerWidth - 20.0, 100.0);
			ElementBounds clippingBounds = ElementBounds.Fixed(0.0, 0.0, innerWidth - 20.0 - 1.0, 99.0).FixedUnder(commmandsBounds, (double)(spacing - 10));
			ElementBounds scrollbarBounds = clippingBounds.CopyOffsetedSibling(clippingBounds.fixedWidth + 6.0, -1.0, 0.0, 0.0).WithFixedWidth(20.0).FixedGrow(0.0, 2.0);
			ElementBounds clearMacroBounds = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(clippingBounds, (double)(6 + 2 * spacing)).WithAlignment(EnumDialogArea.LeftFixed)
				.WithFixedPadding(10.0, 2.0);
			ElementBounds saveMacroBounds = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(clippingBounds, (double)(6 + 2 * spacing)).WithAlignment(EnumDialogArea.RightFixed)
				.WithFixedPadding(10.0, 2.0);
			ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
			bgBounds.BothSizing = ElementSizing.FitToChildren;
			ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightMiddle).WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0.0);
			if (base.SingleComposer != null)
			{
				base.SingleComposer.Dispose();
			}
			base.SingleComposer = this.capi.Gui.CreateCompo("texteditordialog", dialogBounds).AddShadedDialogBG(bgBounds, true, 5.0, 0.75f).AddDialogTitleBar(Lang.Get("Macro Editor", Array.Empty<object>()), new Action(this.OnTitleBarClose), null, null, null)
				.BeginChildElements(bgBounds)
				.AddInset(macroInsetBounds, 3, 0.85f)
				.BeginChildElements()
				.AddSkillItemGrid(this.skillItems, this.cols, this.rows, new Action<int>(this.OnSlotClick), macroBounds, "skillitemgrid")
				.EndChildElements()
				.AddStaticText(Lang.Get("macroname", Array.Empty<object>()), CairoFont.WhiteSmallText(), nameBounds, null)
				.AddStaticText(Lang.Get("macrohotkey", Array.Empty<object>()), CairoFont.WhiteSmallText(), hotkeyBounds, null)
				.AddTextInput(nameInputBounds, new Action<string>(this.OnMacroNameChanged), CairoFont.TextInput(), "macroname")
				.AddInset(hotkeyInputBounds, 2, 0.7f)
				.AddDynamicText("", CairoFont.TextInput(), hotkeyInputBounds.FlatCopy().WithFixedPadding(3.0, 3.0).WithFixedOffset(3.0, 3.0), "hotkey")
				.AddStaticText(Lang.Get("macrocommands", Array.Empty<object>()), CairoFont.WhiteSmallText(), commmandsBounds, null)
				.BeginClip(clippingBounds)
				.AddTextArea(textAreaBounds, new Action<string>(this.OnCommandCodeChanged), CairoFont.TextInput().WithFontSize(16f), "commands")
				.EndClip()
				.AddVerticalScrollbar(new Action<float>(this.OnNewScrollbarvalue), scrollbarBounds, "scrollbar")
				.AddSmallButton(Lang.Get("Delete", Array.Empty<object>()), new ActionConsumable(this.OnClearMacro), clearMacroBounds, EnumButtonStyle.Normal, null)
				.AddSmallButton(Lang.Get("Save", Array.Empty<object>()), new ActionConsumable(this.OnSaveMacro), saveMacroBounds, EnumButtonStyle.Normal, null)
				.EndChildElements()
				.Compose(true);
			base.SingleComposer.GetTextArea("commands").OnCursorMoved = new Action<double, double>(this.OnTextAreaCursorMoved);
			base.SingleComposer.GetScrollbar("scrollbar").SetHeights((float)textAreaBounds.fixedHeight - 1f, (float)textAreaBounds.fixedHeight);
			base.SingleComposer.GetSkillItemGrid("skillitemgrid").selectedIndex = 0;
			this.OnSlotClick(0);
			base.SingleComposer.UnfocusOwnElements();
		}

		private void OnTextAreaCursorMoved(double posX, double posY)
		{
			double lineHeight = base.SingleComposer.GetTextArea("commands").Font.GetFontExtents().Height;
			base.SingleComposer.GetScrollbar("scrollbar").EnsureVisible(posX, posY);
			base.SingleComposer.GetScrollbar("scrollbar").EnsureVisible(posX, posY + lineHeight + 5.0);
		}

		private void OnCommandCodeChanged(string newCode)
		{
			GuiElementTextArea textArea = base.SingleComposer.GetTextArea("commands");
			base.SingleComposer.GetScrollbar("scrollbar").SetNewTotalHeight((float)textArea.Bounds.OuterHeight);
		}

		private void OnMacroNameChanged(string newname)
		{
		}

		private void OnSlotClick(int index)
		{
			GuiElementEditableTextBase textInput = base.SingleComposer.GetTextInput("macroname");
			GuiElementTextArea textArea = base.SingleComposer.GetTextArea("commands");
			GuiElementDynamicText hotkeyText = base.SingleComposer.GetDynamicText("hotkey");
			base.SingleComposer.GetSkillItemGrid("skillitemgrid").selectedIndex = index;
			this.selectedIndex = index;
			this.currentSkillitem = this.skillItems[index];
			if ((this.capi.World as ClientMain).macroManager.MacrosByIndex.ContainsKey(index))
			{
				this.currentMacro = this.SelectedMacro;
			}
			else
			{
				this.currentMacro = new Macro();
				this.currentSkillitem = new SkillItem();
			}
			textInput.SetValue(this.currentSkillitem.Name, true);
			textArea.LoadValue(textArea.Lineize(string.Join("\r\n", this.currentMacro.Commands)));
			if (this.currentSkillitem.Hotkey != null)
			{
				GuiElementDynamicText guiElementDynamicText = hotkeyText;
				KeyCombination hotkey = this.currentSkillitem.Hotkey;
				guiElementDynamicText.SetNewText(((hotkey != null) ? hotkey.ToString() : null) ?? "", false, false, false);
			}
			else
			{
				hotkeyText.SetNewText("", false, false, false);
			}
			base.SingleComposer.GetScrollbar("scrollbar").SetNewTotalHeight((float)textArea.Bounds.OuterHeight);
		}

		public override void OnGuiOpened()
		{
			base.OnGuiOpened();
			this.ComposeDialog();
		}

		private bool OnClearMacro()
		{
			if (this.selectedIndex < 0)
			{
				return true;
			}
			(this.capi.World as ClientMain).macroManager.DeleteMacro(this.selectedIndex);
			GuiElementTextInput textInput = base.SingleComposer.GetTextInput("macroname");
			GuiElementTextArea textArea = base.SingleComposer.GetTextArea("commands");
			GuiElementDynamicText dynamicText = base.SingleComposer.GetDynamicText("hotkey");
			textInput.SetValue("", true);
			textArea.SetValue("", true);
			dynamicText.SetNewText("", false, false, false);
			this.currentMacro = new Macro();
			this.currentSkillitem = new SkillItem();
			this.LoadSkillList();
			return true;
		}

		private bool OnSaveMacro()
		{
			if (this.selectedIndex < 0 || this.currentMacro == null)
			{
				return true;
			}
			GuiElementTextInput textInput = base.SingleComposer.GetTextInput("macroname");
			GuiElementTextArea textArea = base.SingleComposer.GetTextArea("commands");
			this.currentMacro.Name = textInput.GetText();
			if (this.currentMacro.Name.Length == 0)
			{
				this.currentMacro.Name = "Macro " + (this.selectedIndex + 1).ToString();
				textInput.SetValue(this.currentMacro.Name, true);
			}
			this.currentMacro.Commands = textArea.GetLines().ToArray();
			for (int i = 0; i < this.currentMacro.Commands.Length; i++)
			{
				this.currentMacro.Commands[i] = this.currentMacro.Commands[i].TrimEnd(new char[] { '\n', '\r' });
			}
			this.currentMacro.Index = this.selectedIndex;
			this.currentMacro.Code = Regex.Replace(this.currentMacro.Name.Replace(" ", "_"), "[^a-z0-9_-]+", "", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
			this.currentMacro.GenTexture(this.capi, (int)GuiElementPassiveItemSlot.unscaledSlotSize);
			MacroManager mm = (this.capi.World as ClientMain).macroManager;
			base.SingleComposer.GetTextInput("macroname").Font.WithColor(GuiStyle.DialogDefaultTextColor);
			if (mm.MacrosByIndex.Values.FirstOrDefault((IMacroBase m) => m.Code == this.currentMacro.Code && m.Index != this.selectedIndex) != null)
			{
				this.capi.TriggerIngameError(this, "duplicatemacro", Lang.Get("A macro of this name exists already, please choose another name", Array.Empty<object>()));
				base.SingleComposer.GetTextInput("macroname").Font.WithColor(GuiStyle.ErrorTextColor);
				return false;
			}
			mm.SetMacro(this.selectedIndex, this.currentMacro);
			this.LoadSkillList();
			return true;
		}

		private void OnTitleBarClose()
		{
			this.TryClose();
		}

		private void OnNewScrollbarvalue(float value)
		{
			GuiElementTextArea textArea = base.SingleComposer.GetTextArea("commands");
			textArea.Bounds.fixedY = (double)(1f - value);
			textArea.Bounds.CalcWorldBounds();
		}

		public override void OnMouseDown(MouseEvent args)
		{
			base.OnMouseDown(args);
			if (this.selectedIndex < 0)
			{
				return;
			}
			GuiElementDynamicText hotkeyText = base.SingleComposer.GetDynamicText("hotkey");
			hotkeyText.Font.Color = new double[] { 1.0, 1.0, 1.0, 0.9 };
			hotkeyText.RecomposeText(false);
			if (hotkeyText.Bounds.PointInside(args.X, args.Y))
			{
				hotkeyText.SetNewText("?", false, false, false);
				this.hotkeyCapturer.BeginCapture();
				return;
			}
			this.CancelCapture();
		}

		public override void OnKeyUp(KeyEvent args)
		{
			if (this.hotkeyCapturer.OnKeyUp(args, delegate
			{
				if (this.currentMacro != null)
				{
					this.currentMacro.KeyCombination = this.hotkeyCapturer.CapturedKeyComb;
					GuiElementDynamicText hotkeyText = base.SingleComposer.GetDynamicText("hotkey");
					if (ScreenManager.hotkeyManager.IsHotKeyRegistered(this.currentMacro.KeyCombination))
					{
						hotkeyText.Font.Color = GuiStyle.ErrorTextColor;
					}
					else
					{
						hotkeyText.Font.Color = new double[] { 1.0, 1.0, 1.0, 0.9 };
					}
					hotkeyText.SetNewText(this.hotkeyCapturer.CapturedKeyComb.ToString(), false, true, false);
				}
			}))
			{
				return;
			}
			base.OnKeyUp(args);
		}

		public override void OnKeyDown(KeyEvent args)
		{
			if (!this.hotkeyCapturer.OnKeyDown(args))
			{
				base.OnKeyDown(args);
				return;
			}
			if (this.hotkeyCapturer.IsCapturing())
			{
				base.SingleComposer.GetDynamicText("hotkey").SetNewText(this.hotkeyCapturer.CapturingKeyComb.ToString(), false, false, false);
				return;
			}
			this.CancelCapture();
		}

		private void CancelCapture()
		{
			GuiElementDynamicText hotkeyText = base.SingleComposer.GetDynamicText("hotkey");
			IMacroBase selectedMacro = this.SelectedMacro;
			if (((selectedMacro != null) ? selectedMacro.KeyCombination : null) != null)
			{
				hotkeyText.SetNewText(this.SelectedMacro.KeyCombination.ToString(), false, false, false);
			}
			this.hotkeyCapturer.EndCapture(false);
		}

		public override bool CaptureAllInputs()
		{
			return this.hotkeyCapturer.IsCapturing();
		}

		public override bool PrefersUngrabbedMouse
		{
			get
			{
				return true;
			}
		}

		public override void Dispose()
		{
			base.Dispose();
			if (this.skillItems == null)
			{
				return;
			}
			foreach (SkillItem skillItem in this.skillItems)
			{
				if (skillItem != null)
				{
					skillItem.Dispose();
				}
			}
		}

		private List<SkillItem> skillItems;

		private int rows = 2;

		private int cols = 8;

		private int selectedIndex = -1;

		private IMacroBase currentMacro;

		private SkillItem currentSkillitem;

		private HotkeyCapturer hotkeyCapturer = new HotkeyCapturer();
	}
}
