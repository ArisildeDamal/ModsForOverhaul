using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf
{
	public class GuiDialogToolMode : GuiDialog
	{
		public override string ToggleKeyCombinationCode
		{
			get
			{
				return "toolmodeselect";
			}
		}

		public GuiDialogToolMode(ICoreClientAPI capi)
			: base(capi)
		{
			capi.Event.RegisterEventBusListener(new EventBusListenerDelegate(this.OnEventBusEvent), 0.5, "keepopentoolmodedlg");
		}

		private void OnEventBusEvent(string eventName, ref EnumHandling handling, IAttribute data)
		{
			this.keepOpen = true;
		}

		internal override bool OnKeyCombinationToggle(KeyCombination viaKeyComb)
		{
			IClientPlayer player = this.capi.World.Player;
			ItemSlot itemSlot;
			if (player == null)
			{
				itemSlot = null;
			}
			else
			{
				IPlayerInventoryManager inventoryManager = player.InventoryManager;
				itemSlot = ((inventoryManager != null) ? inventoryManager.ActiveHotbarSlot : null);
			}
			ItemSlot slot = itemSlot;
			bool flag;
			if (slot == null)
			{
				flag = null != null;
			}
			else
			{
				ItemStack itemstack = slot.Itemstack;
				flag = ((itemstack != null) ? itemstack.Collectible.GetToolModes(slot, this.capi.World.Player, this.capi.World.Player.CurrentBlockSelection) : null) != null;
			}
			if (!flag)
			{
				return false;
			}
			BlockSelection currentBlockSelection = this.capi.World.Player.CurrentBlockSelection;
			this.blockSele = ((currentBlockSelection != null) ? currentBlockSelection.Clone() : null);
			return base.OnKeyCombinationToggle(viaKeyComb);
		}

		public override void OnGuiOpened()
		{
			this.ComposeDialog();
		}

		private void ComposeDialog()
		{
			this.prevSlotOver = -1;
			base.ClearComposers();
			ItemSlot slot = this.capi.World.Player.InventoryManager.ActiveHotbarSlot;
			SkillItem[] items = slot.Itemstack.Collectible.GetToolModes(slot, this.capi.World.Player, this.blockSele);
			if (items == null)
			{
				return;
			}
			this.multilineItems = new List<List<SkillItem>>();
			this.multilineItems.Add(new List<SkillItem>());
			int cols = 1;
			for (int i = 0; i < items.Length; i++)
			{
				List<SkillItem> lineitems = this.multilineItems[this.multilineItems.Count - 1];
				if (items[i].Linebreak)
				{
					this.multilineItems.Add(lineitems = new List<SkillItem>());
				}
				lineitems.Add(items[i]);
			}
			foreach (List<SkillItem> val in this.multilineItems)
			{
				cols = Math.Max(cols, val.Count);
			}
			double size = GuiElementPassiveItemSlot.unscaledSlotSize + GuiElementItemSlotGridBase.unscaledSlotPadding;
			double innerWidth = (double)cols * size;
			int rows = this.multilineItems.Count;
			foreach (SkillItem val2 in items)
			{
				innerWidth = Math.Max(innerWidth, CairoFont.WhiteSmallishText().GetTextExtents(val2.Name).Width / (double)RuntimeEnv.GUIScale + 1.0);
			}
			ElementBounds skillGridBounds = ElementBounds.Fixed(0.0, 0.0, innerWidth, (double)rows * size);
			ElementBounds textBounds = ElementBounds.Fixed(0.0, (double)rows * (size + 2.0) + 5.0, innerWidth, 25.0);
			base.SingleComposer = this.capi.Gui.CreateCompo("toolmodeselect", ElementStdBounds.AutosizedMainDialog).AddShadedDialogBG(ElementStdBounds.DialogBackground().WithFixedPadding(GuiStyle.ElementToDialogPadding / 2.0), false, 5.0, 0.75f).BeginChildElements();
			int idx = 0;
			for (int j = 0; j < this.multilineItems.Count; j++)
			{
				int line = j;
				int baseIndex = idx;
				List<SkillItem> lineitems2 = this.multilineItems[line];
				base.SingleComposer.AddSkillItemGrid(lineitems2, lineitems2.Count, 1, delegate(int num)
				{
					this.OnSlotClick(baseIndex + num);
				}, skillGridBounds, "skillitemgrid-" + line.ToString());
				base.SingleComposer.GetSkillItemGrid("skillitemgrid-" + line.ToString()).OnSlotOver = delegate(int num)
				{
					this.OnSlotOver(line, num);
				};
				skillGridBounds = skillGridBounds.BelowCopy(0.0, 5.0, 0.0, 0.0);
				idx += lineitems2.Count;
			}
			base.SingleComposer.AddDynamicText("", CairoFont.WhiteSmallishText(), textBounds, "name").EndChildElements().Compose(true);
		}

		private void OnSlotOver(int line, int num)
		{
			List<SkillItem> skillItems = this.multilineItems[line];
			if (num >= skillItems.Count)
			{
				return;
			}
			this.prevSlotOver = num;
			base.SingleComposer.GetDynamicText("name").SetNewText(skillItems[num].Name, false, false, false);
		}

		private void OnSlotClick(int num)
		{
			ItemSlot slot = this.capi.World.Player.InventoryManager.ActiveHotbarSlot;
			CollectibleObject collectibleObject;
			if (slot == null)
			{
				collectibleObject = null;
			}
			else
			{
				ItemStack itemstack = slot.Itemstack;
				collectibleObject = ((itemstack != null) ? itemstack.Collectible : null);
			}
			CollectibleObject obj = collectibleObject;
			if (obj != null)
			{
				obj.SetToolMode(slot, this.capi.World.Player, this.blockSele, num);
				Packet_ToolMode pt = new Packet_ToolMode
				{
					Mode = num
				};
				if (this.blockSele != null)
				{
					pt.X = this.blockSele.Position.X;
					pt.Y = this.blockSele.Position.InternalY;
					pt.Z = this.blockSele.Position.Z;
					pt.SelectionBoxIndex = this.blockSele.SelectionBoxIndex;
					pt.Face = this.blockSele.Face.Index;
					pt.HitX = CollectibleNet.SerializeDouble(this.blockSele.HitPosition.X);
					pt.HitY = CollectibleNet.SerializeDouble(this.blockSele.HitPosition.Y);
					pt.HitZ = CollectibleNet.SerializeDouble(this.blockSele.HitPosition.Z);
				}
				this.capi.Network.SendPacketClient(new Packet_Client
				{
					Id = 27,
					ToolMode = pt
				});
				slot.MarkDirty();
			}
			if (this.keepOpen)
			{
				this.keepOpen = false;
				this.ComposeDialog();
				return;
			}
			this.TryClose();
		}

		public override bool PrefersUngrabbedMouse
		{
			get
			{
				return false;
			}
		}

		public override void OnRenderGUI(float deltaTime)
		{
			if (this.capi.Settings.Bool["immersiveMouseMode"])
			{
				BlockSelection blockSelection = this.blockSele;
				if (((blockSelection != null) ? blockSelection.Position : null) != null)
				{
					Vec3d pos = MatrixToolsd.Project(new Vec3d((double)this.blockSele.Position.X + 0.5, (double)this.blockSele.Position.Y + this.floatyDialogPosition, (double)this.blockSele.Position.Z + 0.5), this.capi.Render.PerspectiveProjectionMat, this.capi.Render.PerspectiveViewMat, this.capi.Render.FrameWidth, this.capi.Render.FrameHeight);
					if (pos.Z < 0.0)
					{
						return;
					}
					base.SingleComposer.Bounds.Alignment = EnumDialogArea.None;
					base.SingleComposer.Bounds.fixedOffsetX = 0.0;
					base.SingleComposer.Bounds.fixedOffsetY = 0.0;
					base.SingleComposer.Bounds.absFixedX = pos.X - base.SingleComposer.Bounds.OuterWidth / 2.0;
					base.SingleComposer.Bounds.absFixedY = (double)this.capi.Render.FrameHeight - pos.Y - base.SingleComposer.Bounds.OuterHeight * this.floatyDialogAlign;
					base.SingleComposer.Bounds.absMarginX = 0.0;
					base.SingleComposer.Bounds.absMarginY = 0.0;
				}
			}
			base.OnRenderGUI(deltaTime);
		}

		private List<List<SkillItem>> multilineItems;

		private BlockSelection blockSele;

		private bool keepOpen;

		private int prevSlotOver = -1;

		private readonly double floatyDialogPosition = 0.5;

		private readonly double floatyDialogAlign = 0.75;
	}
}
