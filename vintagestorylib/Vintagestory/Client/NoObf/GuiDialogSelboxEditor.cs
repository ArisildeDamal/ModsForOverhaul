using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.Client.NoObf
{
	internal class GuiDialogSelboxEditor : GuiDialog
	{
		public GuiDialogSelboxEditor(ICoreClientAPI capi)
			: base(capi)
		{
			capi.ChatCommands.GetOrCreate("dev").BeginSubCommand("bsedit").WithRootAlias("bsedit")
				.WithDescription("Opens the block selection editor")
				.HandleWith(new OnCommandDelegate(this.CmdSelectionBoxEditor))
				.EndSubCommand();
		}

		private TextCommandResult CmdSelectionBoxEditor(TextCommandCallingArgs textCommandCallingArgs)
		{
			this.TryOpen();
			return TextCommandResult.Success("", null);
		}

		public override void OnGuiOpened()
		{
			BlockSelection blockSel = this.capi.World.Player.CurrentBlockSelection;
			this.boxIndex = 0;
			if (blockSel == null)
			{
				this.capi.World.Player.ShowChatNotification("Look at a block first");
				this.capi.Event.EnqueueMainThreadTask(delegate
				{
					this.TryClose();
				}, "closegui");
				return;
			}
			this.nowPos = blockSel.Position.Copy();
			this.nowBlock = this.capi.World.BlockAccessor.GetBlock(blockSel.Position);
			TreeAttribute tree = new TreeAttribute();
			tree.SetInt("nowblockid", this.nowBlock.Id);
			tree.SetBlockPos("pos", blockSel.Position);
			this.capi.Event.PushEvent("oneditselboxes", tree);
			if (this.nowBlock.SelectionBoxes != null)
			{
				this.originalSelBoxes = new Cuboidf[this.nowBlock.SelectionBoxes.Length];
				for (int i = 0; i < this.originalSelBoxes.Length; i++)
				{
					this.originalSelBoxes[i] = this.nowBlock.SelectionBoxes[i].Clone();
				}
			}
			this.currentSelBoxes = this.nowBlock.SelectionBoxes;
			this.ComposeDialog();
		}

		public override string ToggleKeyCombinationCode
		{
			get
			{
				return null;
			}
		}

		private void ComposeDialog()
		{
			base.ClearComposers();
			ElementBounds line = ElementBounds.Fixed(0.0, 21.0, 500.0, 20.0);
			ElementBounds input = ElementBounds.Fixed(0.0, 11.0, 500.0, 30.0);
			ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
			bgBounds.BothSizing = ElementSizing.FitToChildren;
			ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.LeftTop).WithFixedAlignmentOffset(60.0 + GuiStyle.DialogToScreenPadding, GuiStyle.DialogToScreenPadding);
			ElementBounds tabBounds = ElementBounds.Fixed(-320.0, 35.0, 300.0, 300.0);
			GuiTab[] tabs = new GuiTab[]
			{
				new GuiTab
				{
					DataInt = 0,
					Name = "Hitbox 1"
				},
				new GuiTab
				{
					DataInt = 1,
					Name = "Hitbox 2"
				},
				new GuiTab
				{
					DataInt = 2,
					Name = "Hitbox 3"
				},
				new GuiTab
				{
					DataInt = 3,
					Name = "Hitbox 4"
				},
				new GuiTab
				{
					DataInt = 4,
					Name = "Hitbox 5"
				},
				new GuiTab
				{
					DataInt = 5,
					Name = "Hitbox 6"
				}
			};
			this.isChanging = true;
			base.SingleComposer = this.capi.Gui.CreateCompo("transformeditor", dialogBounds).AddShadedDialogBG(bgBounds, true, 5.0, 0.75f).AddDialogTitleBar("Block Hitbox Editor (" + this.nowBlock.GetHeldItemName(new ItemStack(this.nowBlock, 1)) + ")", new Action(this.OnTitleBarClose), null, null, null)
				.BeginChildElements(bgBounds)
				.AddVerticalTabs(tabs, tabBounds, new Action<int, GuiTab>(this.OnTabClicked), "verticalTabs")
				.AddStaticText("X1", CairoFont.WhiteDetailText(), line = line.FlatCopy().WithFixedWidth(230.0), null)
				.AddNumberInput(input = input.BelowCopy(0.0, 0.0, 0.0, 0.0).WithFixedWidth(230.0), delegate(string val)
				{
					this.onCoordVal(val, 0);
				}, CairoFont.WhiteDetailText(), "x1")
				.AddStaticText("X2", CairoFont.WhiteDetailText(), line.RightCopy(40.0, 0.0, 0.0, 0.0), null)
				.AddNumberInput(input.RightCopy(40.0, 0.0, 0.0, 0.0), delegate(string val)
				{
					this.onCoordVal(val, 3);
				}, CairoFont.WhiteDetailText(), "x2")
				.AddStaticText("Y1", CairoFont.WhiteDetailText(), line = line.BelowCopy(0.0, 33.0, 0.0, 0.0), null)
				.AddNumberInput(input = input.BelowCopy(0.0, 22.0, 0.0, 0.0), delegate(string val)
				{
					this.onCoordVal(val, 1);
				}, CairoFont.WhiteDetailText(), "y1")
				.AddStaticText("Y2", CairoFont.WhiteDetailText(), line.RightCopy(40.0, 0.0, 0.0, 0.0), null)
				.AddNumberInput(input.RightCopy(40.0, 0.0, 0.0, 0.0), delegate(string val)
				{
					this.onCoordVal(val, 4);
				}, CairoFont.WhiteDetailText(), "y2")
				.AddStaticText("Z1", CairoFont.WhiteDetailText(), line = line.BelowCopy(0.0, 32.0, 0.0, 0.0), null)
				.AddNumberInput(input = input.BelowCopy(0.0, 22.0, 0.0, 0.0), delegate(string val)
				{
					this.onCoordVal(val, 2);
				}, CairoFont.WhiteDetailText(), "z1")
				.AddStaticText("Z2", CairoFont.WhiteDetailText(), line.RightCopy(40.0, 0.0, 0.0, 0.0), null)
				.AddNumberInput(input.RightCopy(40.0, 0.0, 0.0, 0.0), delegate(string val)
				{
					this.onCoordVal(val, 5);
				}, CairoFont.WhiteDetailText(), "z2")
				.AddStaticText("ΔX", CairoFont.WhiteDetailText(), line = line.BelowCopy(0.0, 38.0, 0.0, 0.0).WithFixedWidth(50.0), null)
				.AddNumberInput(input = input.BelowCopy(0.0, 28.0, 0.0, 0.0).WithFixedWidth(50.0), delegate(string val)
				{
					this.onDeltaVal(val, 0);
				}, CairoFont.WhiteDetailText(), "dx")
				.AddStaticText("ΔY", CairoFont.WhiteDetailText(), line = line.RightCopy(5.0, 0.0, 0.0, 0.0), null)
				.AddNumberInput(input = input.RightCopy(5.0, 0.0, 0.0, 0.0), delegate(string val)
				{
					this.onDeltaVal(val, 1);
				}, CairoFont.WhiteDetailText(), "dy")
				.AddStaticText("ΔZ", CairoFont.WhiteDetailText(), line = line.RightCopy(5.0, 0.0, 0.0, 0.0), null)
				.AddNumberInput(input = input.RightCopy(5.0, 0.0, 0.0, 0.0), delegate(string val)
				{
					this.onDeltaVal(val, 2);
				}, CairoFont.WhiteDetailText(), "dz")
				.AddStaticText("Json Code", CairoFont.WhiteDetailText(), line.BelowCopy(-110.0, 36.0, 0.0, 0.0).WithFixedWidth(500.0), null)
				.BeginClip(input.BelowCopy(-110.0, 26.0, 0.0, 0.0).WithFixedHeight(200.0).WithFixedWidth(500.0))
				.AddTextArea(input = input.BelowCopy(-110.0, 26.0, 0.0, 0.0).WithFixedHeight(200.0).WithFixedWidth(500.0), null, CairoFont.WhiteSmallText(), "textarea")
				.EndClip()
				.AddSmallButton("Close & Apply", new ActionConsumable(this.OnApplyJson), input = input.BelowCopy(0.0, 20.0, 0.0, 0.0).WithFixedSize(200.0, 20.0).WithAlignment(EnumDialogArea.LeftFixed)
					.WithFixedPadding(10.0, 2.0), EnumButtonStyle.Normal, null)
				.AddSmallButton("Copy JSON", new ActionConsumable(this.OnCopyJson), input.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedPadding(10.0, 2.0), EnumButtonStyle.Normal, null)
				.EndChildElements()
				.Compose(true);
			Cuboidf hitbox = new Cuboidf();
			if (this.boxIndex < this.currentSelBoxes.Length)
			{
				hitbox = this.currentSelBoxes[this.boxIndex];
			}
			else
			{
				while (this.boxIndex >= this.currentSelBoxes.Length)
				{
					this.currentSelBoxes = this.currentSelBoxes.Append(new Cuboidf());
				}
				this.nowBlock.SelectionBoxes = this.currentSelBoxes;
			}
			for (int i = 0; i < this.coordnames.Length; i++)
			{
				base.SingleComposer.GetNumberInput(this.coordnames[i]).SetValue(hitbox[i]);
				base.SingleComposer.GetNumberInput(this.coordnames[i]).Interval = 0.0625f;
			}
			base.SingleComposer.GetNumberInput("dx").Interval = 0.0625f;
			base.SingleComposer.GetNumberInput("dy").Interval = 0.0625f;
			base.SingleComposer.GetNumberInput("dz").Interval = 0.0625f;
			base.SingleComposer.GetVerticalTab("verticalTabs").SetValue(this.boxIndex, false);
			this.isChanging = false;
		}

		private void OnTabClicked(int index, GuiTab tab)
		{
			this.boxIndex = index;
			this.ComposeDialog();
		}

		private bool OnApplyJson()
		{
			this.originalSelBoxes = this.currentSelBoxes;
			TreeAttribute tree = new TreeAttribute();
			tree.SetInt("nowblockid", this.nowBlock.Id);
			tree.SetBlockPos("pos", this.nowPos);
			this.capi.Event.PushEvent("onapplyselboxes", tree);
			this.TryClose();
			return true;
		}

		private bool OnCopyJson()
		{
			ScreenManager.Platform.XPlatInterface.SetClipboardText(this.getJson());
			return true;
		}

		private void updateJson()
		{
			base.SingleComposer.GetTextArea("textarea").SetValue(this.getJson(), true);
		}

		private string getJson()
		{
			List<Cuboidf> nonEmptyBoxes = new List<Cuboidf>();
			for (int i = 0; i < this.currentSelBoxes.Length; i++)
			{
				if (!this.currentSelBoxes[i].Empty)
				{
					nonEmptyBoxes.Add(this.currentSelBoxes[i]);
				}
			}
			if (nonEmptyBoxes.Count == 0)
			{
				return "";
			}
			if (nonEmptyBoxes.Count == 1)
			{
				Cuboidf box = this.currentSelBoxes[0];
				return string.Format(GlobalConstants.DefaultCultureInfo, "\tselectionBox: {{ x1: {0}, y1: {1}, z1: {2}, x2: {3}, y2: {4}, z2: {5} }}\n", new object[] { box.X1, box.Y1, box.Z1, box.X2, box.Y2, box.Z2 });
			}
			StringBuilder json = new StringBuilder();
			json.Append("\tselectionBoxes: [\n");
			foreach (Cuboidf box2 in nonEmptyBoxes)
			{
				json.Append(string.Format(GlobalConstants.DefaultCultureInfo, "\t\t{{ x1: {0}, y1: {1}, z1: {2}, x2: {3}, y2: {4}, z2: {5} }},\n", new object[] { box2.X1, box2.Y1, box2.Z1, box2.X2, box2.Y2, box2.Z2 }));
			}
			json.Append("\t]");
			return json.ToString();
		}

		private void onCoordVal(string val, int index)
		{
			if (this.isChanging)
			{
				return;
			}
			this.isChanging = true;
			float value;
			float.TryParse(val, NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out value);
			this.currentSelBoxes[this.boxIndex][index] = value;
			this.updateJson();
			this.isChanging = false;
		}

		private void onDeltaVal(string val, int index)
		{
			if (this.isChanging)
			{
				return;
			}
			this.isChanging = true;
			float value;
			float.TryParse(val, NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out value);
			Cuboidf hitbox = this.currentSelBoxes[this.boxIndex];
			switch (index)
			{
			case 0:
				hitbox.X1 += value;
				hitbox.X2 += value;
				base.SingleComposer.GetNumberInput("dx").SetValue("", true);
				break;
			case 1:
				hitbox.Y1 += value;
				hitbox.Y2 += value;
				base.SingleComposer.GetNumberInput("dy").SetValue("", true);
				break;
			case 2:
				hitbox.Z1 += value;
				hitbox.Z2 += value;
				base.SingleComposer.GetNumberInput("dz").SetValue("", true);
				break;
			}
			for (int i = 0; i < this.coordnames.Length; i++)
			{
				base.SingleComposer.GetNumberInput(this.coordnames[i]).SetValue(hitbox[i]);
				base.SingleComposer.GetNumberInput(this.coordnames[i]).Interval = 0.0625f;
			}
			this.updateJson();
			this.isChanging = false;
		}

		private void OnTitleBarClose()
		{
			this.TryClose();
		}

		public override void OnGuiClosed()
		{
			base.OnGuiClosed();
			this.currentSelBoxes = this.originalSelBoxes;
			TreeAttribute tree = new TreeAttribute();
			tree.SetInt("nowblockid", this.nowBlock.Id);
			tree.SetBlockPos("pos", this.nowPos);
			this.capi.Event.PushEvent("oncloseeditselboxes", tree);
		}

		public override void OnMouseWheel(MouseWheelEventArgs args)
		{
			base.OnMouseWheel(args);
			args.SetHandled(true);
		}

		public override bool PrefersUngrabbedMouse
		{
			get
			{
				return true;
			}
		}

		private Cuboidf[] originalSelBoxes;

		private Block nowBlock;

		private BlockPos nowPos;

		private Cuboidf[] currentSelBoxes;

		private int boxIndex;

		private string[] coordnames = new string[] { "x1", "y1", "z1", "x2", "y2", "z2" };

		private bool isChanging;
	}
}
