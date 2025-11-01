using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.Common
{
	public class InventoryPlayerMouseCursor : InventoryBasePlayer
	{
		public InventoryPlayerMouseCursor(string className, string playerUID, ICoreAPI api)
			: base(className, playerUID, api)
		{
			this.slot = new ItemSlotUniversal(this);
		}

		public InventoryPlayerMouseCursor(string inventoryId, ICoreAPI api)
			: base(inventoryId, api)
		{
			this.slot = new ItemSlotUniversal(this);
		}

		public override int Count
		{
			get
			{
				return 1;
			}
		}

		public override ItemSlot this[int slotId]
		{
			get
			{
				if (slotId != 0)
				{
					return null;
				}
				return this.slot;
			}
			set
			{
				if (slotId != 0)
				{
					throw new ArgumentOutOfRangeException("slotId");
				}
				if (value == null)
				{
					throw new ArgumentNullException("value");
				}
				this.slot = value;
			}
		}

		public override WeightedSlot GetBestSuitedSlot(ItemSlot sourceSlot, ItemStackMoveOperation op, List<ItemSlot> skipSlots = null)
		{
			return new WeightedSlot();
		}

		public override void OnItemSlotModified(ItemSlot slot)
		{
			base.OnItemSlotModified(slot);
			if (this.wasEmpty && !slot.Empty)
			{
				TreeAttribute tree = new TreeAttribute();
				tree["itemstack"] = new ItemstackAttribute(slot.Itemstack.Clone());
				TreeAttribute treeAttribute = tree;
				string text = "byentityid";
				IPlayer player = base.Player;
				long? num;
				if (player == null)
				{
					num = null;
				}
				else
				{
					EntityPlayer entity = player.Entity;
					num = ((entity != null) ? new long?(entity.EntityId) : null);
				}
				long? num2 = num;
				treeAttribute[text] = new LongAttribute(num2.GetValueOrDefault());
				this.Api.Event.PushEvent("onitemgrabbed", tree);
			}
			this.wasEmpty = slot.Empty;
		}

		public override void ToTreeAttributes(ITreeAttribute tree)
		{
		}

		public override void FromTreeAttributes(ITreeAttribute tree)
		{
		}

		private ItemSlot slot;

		private bool wasEmpty = true;
	}
}
