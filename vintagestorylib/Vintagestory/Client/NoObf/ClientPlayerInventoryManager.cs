using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf
{
	public class ClientPlayerInventoryManager : PlayerInventoryManager
	{
		public override ItemSlot CurrentHoveredSlot
		{
			get
			{
				return this.currentHoveredSlot;
			}
			set
			{
				this.currentHoveredSlot = value;
				this.game.api.Input.TriggerOnMouseEnterSlot(value);
			}
		}

		public ClientPlayerInventoryManager(OrderedDictionary<string, InventoryBase> AllInventories, IPlayer player, ClientMain game)
			: base(AllInventories, player)
		{
			this.game = game;
		}

		public override int ActiveHotbarSlotNumber
		{
			get
			{
				return base.ActiveHotbarSlotNumber;
			}
			set
			{
				int beforeSlot = base.ActiveHotbarSlotNumber;
				if (value == beforeSlot)
				{
					return;
				}
				if (this.player == this.game.player && this.game.eventManager != null)
				{
					if (!this.game.eventManager.TriggerBeforeActiveSlotChanged(this.game, beforeSlot, value))
					{
						return;
					}
					this.game.SendPacketClient(ClientPackets.SelectedHotbarSlot(value));
				}
				base.ActiveHotbarSlotNumber = value;
				if (this.player == this.game.player)
				{
					ClientEventManager eventManager = this.game.eventManager;
					if (eventManager == null)
					{
						return;
					}
					eventManager.TriggerAfterActiveSlotChanged(this.game, beforeSlot, value);
				}
			}
		}

		public void SetActiveHotbarSlotNumberFromServer(int slotid)
		{
			int beforeSlot = base.ActiveHotbarSlotNumber;
			base.ActiveHotbarSlotNumber = slotid;
			if (this.player == this.game.player)
			{
				ClientEventManager eventManager = this.game.eventManager;
				if (eventManager == null)
				{
					return;
				}
				eventManager.TriggerAfterActiveSlotChanged(this.game, beforeSlot, slotid);
			}
		}

		public override void NotifySlot(IPlayer player, ItemSlot slot)
		{
		}

		public override bool DropItem(ItemSlot slot, bool fullStack = false)
		{
			if (((slot != null) ? slot.Itemstack : null) == null)
			{
				return false;
			}
			int quantity = (fullStack ? slot.Itemstack.StackSize : 1);
			EnumHandling handling = EnumHandling.PassThrough;
			slot.Itemstack.Collectible.OnHeldDropped(this.game, this.game.player, slot, quantity, ref handling);
			if (handling != EnumHandling.PassThrough)
			{
				return false;
			}
			if (quantity >= slot.Itemstack.StackSize && slot == this.game.player.inventoryMgr.ActiveHotbarSlot && this.game.EntityPlayer.Controls.HandUse != EnumHandInteract.None)
			{
				EnumHandInteract beforeUseType = this.game.EntityPlayer.Controls.HandUse;
				if (!this.game.EntityPlayer.TryStopHandAction(true, EnumItemUseCancelReason.Dropped))
				{
					return false;
				}
				if (slot.StackSize <= 0)
				{
					slot.Itemstack = null;
					slot.MarkDirty();
				}
				this.game.SendHandInteraction(2, this.game.BlockSelection, this.game.EntitySelection, beforeUseType, EnumHandInteractNw.CancelHeldItemUse, false, EnumItemUseCancelReason.Dropped);
			}
			IInventory targetInv = base.GetOwnInventory("ground");
			ItemStackMoveOperation op = new ItemStackMoveOperation(this.game, EnumMouseButton.Left, (EnumModifierKey)0, EnumMergePriority.AutoMerge, quantity);
			op.ActingPlayer = this.game.player;
			slot.TryPutInto(targetInv[0], ref op);
			int tabIndex = 0;
			CreativeInventoryTab iti = slot.Inventory as CreativeInventoryTab;
			if (iti != null)
			{
				tabIndex = iti.TabIndex;
			}
			Packet_Client packet = new Packet_Client
			{
				Id = 8,
				MoveItemstack = new Packet_MoveItemstack
				{
					Quantity = quantity,
					SourceInventoryId = slot.Inventory.InventoryID,
					SourceSlot = slot.Inventory.GetSlotId(slot),
					SourceLastChanged = slot.Inventory.LastChanged,
					TargetInventoryId = targetInv.InventoryID,
					TargetSlot = 0,
					TargetLastChanged = targetInv.LastChanged,
					TabIndex = tabIndex
				}
			};
			this.game.SendPacketClient(packet);
			return true;
		}

		public override void BroadcastHotbarSlot()
		{
		}

		public ItemSlot currentHoveredSlot;

		private ClientMain game;
	}
}
