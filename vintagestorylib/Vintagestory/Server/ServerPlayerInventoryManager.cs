using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace Vintagestory.Server
{
	public class ServerPlayerInventoryManager : PlayerInventoryManager
	{
		public ServerPlayerInventoryManager(OrderedDictionary<string, InventoryBase> AllInventories, IPlayer player, ServerMain server)
			: base(AllInventories, player)
		{
			this.server = server;
		}

		public override ItemSlot CurrentHoveredSlot
		{
			get
			{
				throw new NotImplementedException("This information is not available on the server");
			}
			set
			{
				throw new NotImplementedException("This information is not available on the server");
			}
		}

		public override void BroadcastHotbarSlot()
		{
			this.server.BroadcastHotbarSlot(this.player as ServerPlayer, true);
		}

		public override bool DropItem(ItemSlot slot, bool fullStack = false)
		{
			if (((slot != null) ? slot.Itemstack : null) == null)
			{
				return false;
			}
			int quantity = (fullStack ? slot.Itemstack.StackSize : 1);
			EnumHandling handling = EnumHandling.PassThrough;
			slot.Itemstack.Collectible.OnHeldDropped(this.server, this.player, slot, quantity, ref handling);
			if (handling != EnumHandling.PassThrough)
			{
				return false;
			}
			if (quantity >= slot.Itemstack.StackSize && slot == base.ActiveHotbarSlot && this.player.Entity.Controls.HandUse != EnumHandInteract.None)
			{
				if (!this.player.Entity.TryStopHandAction(true, EnumItemUseCancelReason.Dropped))
				{
					return false;
				}
				if (slot.StackSize <= 0)
				{
					slot.Itemstack = null;
					slot.MarkDirty();
				}
			}
			IInventory targetInv = base.GetOwnInventory("ground");
			ItemStackMoveOperation op = new ItemStackMoveOperation(this.server, EnumMouseButton.Left, EnumModifierKey.SHIFT, EnumMergePriority.AutoMerge, quantity);
			op.ActingPlayer = this.player;
			slot.TryPutInto(targetInv[0], ref op);
			slot.MarkDirty();
			return true;
		}

		public override void NotifySlot(IPlayer toPlayer, ItemSlot slot)
		{
			if (slot.Inventory == null)
			{
				return;
			}
			this.server.SendPacket(toPlayer as IServerPlayer, new Packet_Server
			{
				Id = 66,
				NotifySlot = new Packet_NotifySlot
				{
					InventoryId = slot.Inventory.InventoryID,
					SlotId = slot.Inventory.GetSlotId(slot)
				}
			});
		}

		internal void OnPlayerDisconnect()
		{
			List<KeyValuePair<string, InventoryBase>> toRemove = new List<KeyValuePair<string, InventoryBase>>();
			foreach (KeyValuePair<string, InventoryBase> entry in this.Inventories)
			{
				if (!(entry.Value is InventoryBasePlayer))
				{
					toRemove.Add(entry);
				}
			}
			foreach (KeyValuePair<string, InventoryBase> inv in toRemove)
			{
				base.CloseInventory(inv.Value);
				this.Inventories.Remove(inv.Key);
			}
		}

		private ServerMain server;
	}
}
