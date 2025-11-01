using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Vintagestory.Client.NoObf
{
	internal class HudMouseTools : GuiDialog
	{
		private int currentFrontElemIndex
		{
			get
			{
				return 1 - this.currentBackElemIndex;
			}
		}

		public override double DrawOrder
		{
			get
			{
				return 0.9;
			}
		}

		public override string ToggleKeyCombinationCode
		{
			get
			{
				return null;
			}
		}

		public HudMouseTools(ICoreClientAPI capi)
			: base(capi)
		{
			capi.Event.RegisterGameTickListener(new Action<float>(this.RecheckItemInfo), 500, 0);
		}

		public override bool TryClose()
		{
			return false;
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
			if (this.dirty && !this.itemstackinfoElements[this.currentBackElemIndex].Dirty)
			{
				this.dirty = false;
				this.currentBackElemIndex = 1 - this.currentBackElemIndex;
				this.itemstackinfoElements[this.currentFrontElemIndex].Render = true;
				this.itemstackinfoElements[this.currentBackElemIndex].Render = false;
				this.recalcAlignmentOffset = true;
			}
			if (this.capi.Input.InWorldMouseButton.Right)
			{
				this.dirty = true;
				this.itemstackinfoElements[this.currentBackElemIndex].SetSourceSlot(null, false);
				this.currentSlot = null;
			}
			this.slotBounds.fixedX = (double)(5f + (float)this.capi.Input.MouseX / ClientSettings.GUIScale);
			this.slotBounds.fixedY = (double)(5f + (float)this.capi.Input.MouseY / ClientSettings.GUIScale);
			this.slotBounds.CalcWorldBounds();
			ElementBounds curStackInfoBounds = this.itemstackinfoElements[this.currentFrontElemIndex].Bounds;
			double num = (double)this.capi.Input.MouseX + curStackInfoBounds.OuterWidth + (double)HudMouseTools.tooltipOffsetX - (double)(this.capi.Render.FrameWidth - 5);
			double bottomOverlapPixels = (double)this.capi.Input.MouseY + curStackInfoBounds.OuterHeight + (double)HudMouseTools.tooltipOffsetY - (double)(this.capi.Render.FrameHeight - 5);
			bool newRightOverlap = num > 0.0;
			bool newBottomOverlap = bottomOverlapPixels > 0.0;
			if (this.currentSlot != null && (this.recalcAlignmentOffset || newBottomOverlap || this.rightOverlap))
			{
				curStackInfoBounds.WithFixedAlignmentOffset(newRightOverlap ? (-(curStackInfoBounds.OuterWidth + (double)(3 * HudMouseTools.tooltipOffsetX)) / (double)ClientSettings.GUIScale) : 0.0, newBottomOverlap ? (-bottomOverlapPixels / (double)ClientSettings.GUIScale - 10.0) : 0.0);
				curStackInfoBounds.CalcWorldBounds();
				this.bottomOverlap = newBottomOverlap;
				this.rightOverlap = newRightOverlap;
			}
			this.recalcAlignmentOffset = false;
			this.capi.Render.GlPushMatrix();
			this.capi.Render.GlTranslate(0f, 0f, 160f);
			base.OnRenderGUI(deltaTime);
			this.capi.Render.GlPopMatrix();
			bool nowlshiftdown = this.capi.Input.KeyboardKeyStateRaw[1];
			if (nowlshiftdown != this.lshiftdown && ClientSettings.ExtendedDebugInfo)
			{
				this.itemstackinfoElements[this.currentBackElemIndex].SetSourceSlot(null, false);
				if (this.itemstackinfoElements[this.currentBackElemIndex].SetSourceSlot(this.currentSlot, false))
				{
					this.dirty = true;
				}
				this.lshiftdown = nowlshiftdown;
			}
		}

		public override void OnOwnPlayerDataReceived()
		{
			this.TryOpen();
			this.mouseCursorInv = this.capi.World.Player.InventoryManager.GetOwnInventory("mouse");
			double off = -GuiElementPassiveItemSlot.unscaledSlotSize * 0.25;
			this.slotBounds = ElementStdBounds.Slot(0.0, 0.0).WithFixedAlignmentOffset(off, off);
			this.Composers["mouseSlot"] = this.capi.Gui.CreateCompo("mouseSlot", ElementBounds.Fill).AddPassiveItemSlot(this.slotBounds, this.mouseCursorInv, this.mouseCursorInv[0], false).Compose(true);
			ElementBounds stackInfoBounds = ElementBounds.FixedSize(EnumDialogArea.None, (double)GuiElementItemstackInfo.BoxWidth, 0.0).WithFixedPadding(10.0).WithFixedPosition(25.0, 35.0);
			stackInfoBounds.WithParent(this.slotBounds);
			this.Composers["itemstackinfo"] = this.capi.Gui.CreateCompo("itemstackinfo", ElementBounds.Fill).AddInteractiveElement(new GuiElementItemstackInfo(this.capi, stackInfoBounds, new InfoTextDelegate(this.OnRequireInfoText)), "itemstackinfo1").AddInteractiveElement(new GuiElementItemstackInfo(this.capi, stackInfoBounds.FlatCopy(), new InfoTextDelegate(this.OnRequireInfoText)), "itemstackinfo2")
				.Compose(true);
			this.itemstackinfoElements = new GuiElementItemstackInfo[]
			{
				(GuiElementItemstackInfo)this.Composers["itemstackinfo"].GetElement("itemstackinfo1"),
				(GuiElementItemstackInfo)this.Composers["itemstackinfo"].GetElement("itemstackinfo2")
			};
			this.itemstackinfoElements[0].Render = false;
			this.itemstackinfoElements[1].Render = false;
		}

		private string OnRequireInfoText(ItemSlot slot)
		{
			return slot.GetStackDescription(this.capi.World, ClientSettings.ExtendedDebugInfo);
		}

		public override bool IsOpened()
		{
			return !this.capi.Input.MouseGrabbed && this.capi.World.Player.InventoryManager.OpenedInventories.Count > 0;
		}

		public override bool IsOpened(string dialogComposerName)
		{
			if (dialogComposerName == "itemstackinfo")
			{
				ItemSlot slot = this.itemstackinfoElements[this.currentFrontElemIndex].GetSlot();
				return ((slot != null) ? slot.Itemstack : null) != null;
			}
			return base.IsOpened(dialogComposerName);
		}

		public override bool ShouldReceiveRenderEvents()
		{
			return true;
		}

		public override EnumDialogType DialogType
		{
			get
			{
				return EnumDialogType.HUD;
			}
		}

		public override void OnMouseDown(MouseEvent args)
		{
		}

		public override void OnMouseUp(MouseEvent args)
		{
		}

		public override void OnMouseMove(MouseEvent args)
		{
		}

		private void RecheckItemInfo(float dt)
		{
			if (this.itemstackinfoElements[this.currentBackElemIndex].SetSourceSlot(this.currentSlot, false))
			{
				this.dirty = true;
			}
		}

		public override bool OnMouseEnterSlot(ItemSlot slot)
		{
			if (this.capi.Input.InWorldMouseButton.Right)
			{
				return false;
			}
			this.dirty = true;
			this.itemstackinfoElements[this.currentBackElemIndex].SetSourceSlot(slot, false);
			this.currentSlot = slot;
			this.recalcAlignmentOffset = true;
			return false;
		}

		public override bool OnMouseLeaveSlot(ItemSlot slot)
		{
			this.itemstackinfoElements[this.currentBackElemIndex].SetSourceSlot(null, false);
			this.itemstackinfoElements[this.currentFrontElemIndex].SetSourceSlot(null, false);
			this.currentSlot = null;
			return false;
		}

		public override bool OnMouseClickSlot(ItemSlot itemSlot)
		{
			this.dirty = true;
			this.itemstackinfoElements[this.currentBackElemIndex].SetSourceSlot(itemSlot, false);
			return false;
		}

		private static int tooltipOffsetX = 10;

		private static int tooltipOffsetY = 30;

		private ElementBounds slotBounds = ElementBounds.Empty;

		private IInventory mouseCursorInv;

		private GuiElementItemstackInfo[] itemstackinfoElements;

		private int currentBackElemIndex;

		private bool bottomOverlap;

		private bool rightOverlap;

		private bool recalcAlignmentOffset;

		private bool dirty;

		private bool lshiftdown;

		private ItemSlot currentSlot;
	}
}
