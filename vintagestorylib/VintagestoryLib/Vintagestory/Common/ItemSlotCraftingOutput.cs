using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.Common
{
	public class ItemSlotCraftingOutput : ItemSlotOutput
	{
		private InventoryCraftingGrid inv
		{
			get
			{
				return (InventoryCraftingGrid)this.inventory;
			}
		}

		public ItemSlotCraftingOutput(InventoryBase inventory)
			: base(inventory)
		{
		}

		protected override void FlipWith(ItemSlot withSlot)
		{
			ItemStackMoveOperation op = new ItemStackMoveOperation(this.inv.Api.World, EnumMouseButton.Left, (EnumModifierKey)0, EnumMergePriority.AutoMerge, base.StackSize);
			this.CraftSingle(withSlot, ref op);
		}

		public override int TryPutInto(ItemSlot sinkSlot, ref ItemStackMoveOperation op)
		{
			if (this.Empty)
			{
				return 0;
			}
			op.RequestedQuantity = base.StackSize;
			ItemStack craftedStack = this.itemstack.Clone();
			if (this.hasLeftOvers)
			{
				int moved = base.TryPutInto(sinkSlot, ref op);
				if (!this.Empty)
				{
					this.triggerEvent(craftedStack, moved, op.ActingPlayer);
					return moved;
				}
				this.hasLeftOvers = false;
				this.inv.ConsumeIngredients(sinkSlot);
				if (this.inv.CanStillCraftCurrent())
				{
					this.itemstack = this.prevStack.Clone();
				}
			}
			if (op.ShiftDown)
			{
				this.CraftMany(sinkSlot, ref op);
			}
			else
			{
				this.CraftSingle(sinkSlot, ref op);
			}
			if (op.ActingPlayer != null)
			{
				this.triggerEvent(craftedStack, op.MovedQuantity, op.ActingPlayer);
			}
			else
			{
				InventoryBasePlayer playerInventory = base.Inventory as InventoryBasePlayer;
				if (playerInventory != null)
				{
					this.triggerEvent(craftedStack, op.MovedQuantity, playerInventory.Player);
				}
			}
			return op.MovedQuantity;
		}

		private void triggerEvent(ItemStack craftedStack, int moved, IPlayer actingPlayer)
		{
			TreeAttribute tree = new TreeAttribute();
			craftedStack.StackSize = moved;
			tree["itemstack"] = new ItemstackAttribute(craftedStack);
			tree["byentityid"] = new LongAttribute(actingPlayer.Entity.EntityId);
			actingPlayer.Entity.World.Api.Event.PushEvent("onitemcrafted", tree);
		}

		private void CraftMany(ItemSlot sinkSlot, ref ItemStackMoveOperation op)
		{
			if (this.itemstack == null)
			{
				return;
			}
			int movedtotal = 0;
			int mv;
			for (;;)
			{
				this.prevStack = this.itemstack.Clone();
				int stackSize = base.StackSize;
				op.RequestedQuantity = base.StackSize;
				op.MovedQuantity = 0;
				mv = this.TryPutIntoNoEvent(sinkSlot, ref op);
				movedtotal += mv;
				if (stackSize > mv)
				{
					break;
				}
				this.inv.ConsumeIngredients(sinkSlot);
				if (!this.inv.CanStillCraftCurrent())
				{
					goto IL_007A;
				}
				this.itemstack = this.prevStack;
			}
			this.hasLeftOvers = mv > 0;
			IL_007A:
			if (movedtotal > 0)
			{
				sinkSlot.OnItemSlotModified(sinkSlot.Itemstack);
				this.OnItemSlotModified(sinkSlot.Itemstack);
			}
		}

		public virtual int TryPutIntoNoEvent(ItemSlot sinkSlot, ref ItemStackMoveOperation op)
		{
			if (!sinkSlot.CanTakeFrom(this, EnumMergePriority.AutoMerge) || !this.CanTake() || this.itemstack == null)
			{
				return 0;
			}
			if (sinkSlot.Itemstack == null)
			{
				int q = Math.Min(sinkSlot.GetRemainingSlotSpace(base.Itemstack), op.RequestedQuantity);
				if (q > 0)
				{
					sinkSlot.Itemstack = this.TakeOut(q);
					op.MovedQuantity = (op.MovableQuantity = Math.Min(sinkSlot.StackSize, q));
				}
				return op.MovedQuantity;
			}
			ItemStackMergeOperation mergeop = op.ToMergeOperation(sinkSlot, this);
			op = mergeop;
			int origRequestedQuantity = op.RequestedQuantity;
			op.RequestedQuantity = Math.Min(sinkSlot.GetRemainingSlotSpace(this.itemstack), op.RequestedQuantity);
			sinkSlot.Itemstack.Collectible.TryMergeStacks(mergeop);
			op.RequestedQuantity = origRequestedQuantity;
			return mergeop.MovedQuantity;
		}

		private void CraftSingle(ItemSlot sinkSlot, ref ItemStackMoveOperation op)
		{
			int prevQuantity = base.StackSize;
			int num = this.TryPutIntoNoEvent(sinkSlot, ref op);
			if (num == prevQuantity)
			{
				this.inv.ConsumeIngredients(sinkSlot);
			}
			if (num > 0)
			{
				sinkSlot.OnItemSlotModified(sinkSlot.Itemstack);
				this.OnItemSlotModified(sinkSlot.Itemstack);
			}
		}

		public bool hasLeftOvers;

		private ItemStack prevStack;
	}
}
