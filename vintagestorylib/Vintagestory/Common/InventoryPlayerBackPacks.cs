using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.Common
{
	public class InventoryPlayerBackPacks : InventoryBasePlayer
	{
		public override int CountForNetworkPacket
		{
			get
			{
				return 4;
			}
		}

		public InventoryPlayerBackPacks(string className, string playerUID, ICoreAPI api)
			: base(className, playerUID, api)
		{
			this.bagSlots = base.GenEmptySlots(4);
			this.baseWeight = 1f;
			this.bagInv = new BagInventory(api, this.bagSlots);
		}

		public InventoryPlayerBackPacks(string inventoryId, ICoreAPI api)
			: base(inventoryId, api)
		{
			this.bagSlots = base.GenEmptySlots(4);
			this.baseWeight = 1f;
			this.bagInv = new BagInventory(api, this.bagSlots);
		}

		public override bool CanPlayerAccess(IPlayer player, EntityPos position)
		{
			return base.CanPlayerAccess(player, position);
		}

		public override void AfterBlocksLoaded(IWorldAccessor world)
		{
			base.AfterBlocksLoaded(world);
			this.bagInv.ReloadBagInventory(this, this.bagSlots);
		}

		public override int Count
		{
			get
			{
				return this.bagSlots.Length + this.bagInv.Count;
			}
		}

		public override ItemSlot this[int slotId]
		{
			get
			{
				if (slotId < 0 || slotId >= this.Count)
				{
					return null;
				}
				if (slotId < this.bagSlots.Length)
				{
					return this.bagSlots[slotId];
				}
				return this.bagInv[slotId - this.bagSlots.Length];
			}
			set
			{
				if (slotId < 0 || slotId >= this.Count)
				{
					throw new ArgumentOutOfRangeException("slotId");
				}
				if (value == null)
				{
					throw new ArgumentNullException("value");
				}
				if (slotId < this.bagSlots.Length)
				{
					this.bagSlots[slotId] = value;
				}
				this.bagInv[slotId - this.bagSlots.Length] = value;
			}
		}

		public override void FromTreeAttributes(ITreeAttribute tree)
		{
			this.bagSlots = this.SlotsFromTreeAttributes(tree, null, null);
		}

		public override void ToTreeAttributes(ITreeAttribute tree)
		{
			base.SlotsToTreeAttributes(this.bagSlots, tree);
		}

		protected override ItemSlot NewSlot(int slotId)
		{
			return new ItemSlotBackpack(this);
		}

		public override float GetSuitability(ItemSlot sourceSlot, ItemSlot targetSlot, bool isMerge)
		{
			float multiplier = (float)(((sourceSlot.Itemstack.Collectible.GetStorageFlags(sourceSlot.Itemstack) & EnumItemStorageFlags.Backpack) > (EnumItemStorageFlags)0) ? 2 : 1);
			if (targetSlot is ItemSlotBagContent && !this.openedByPlayerGUIds.Contains(this.playerUID) && !(sourceSlot is DummySlot))
			{
				multiplier *= 0.35f;
			}
			if (targetSlot is ItemSlotBagContent && (targetSlot.StorageType & (targetSlot.StorageType - 1)) == (EnumItemStorageFlags)0 && (targetSlot.StorageType & sourceSlot.Itemstack.Collectible.GetStorageFlags(sourceSlot.Itemstack)) > (EnumItemStorageFlags)0)
			{
				multiplier *= 1.2f;
			}
			float suitability = base.GetSuitability(sourceSlot, targetSlot, isMerge);
			float num;
			if (sourceSlot.Inventory is InventoryGeneric)
			{
				ItemStack itemstack = sourceSlot.Itemstack;
				if (itemstack == null || itemstack.Collectible.Tool == null)
				{
					num = (float)1;
					goto IL_00CB;
				}
			}
			num = (float)0;
			IL_00CB:
			return (suitability + num) * multiplier + (float)((sourceSlot is ItemSlotOutput || sourceSlot is ItemSlotCraftingOutput) ? 1 : 0);
		}

		public override void OnItemSlotModified(ItemSlot slot)
		{
			if (slot is ItemSlotBagContent)
			{
				this.bagInv.SaveSlotIntoBag((ItemSlotBagContent)slot);
				return;
			}
			this.bagInv.ReloadBagInventory(this, this.bagSlots);
			if (this.Api.Side == EnumAppSide.Server)
			{
				IServerPlayer serverPlayer = this.Api.World.PlayerByUid(this.playerUID) as IServerPlayer;
				if (serverPlayer == null)
				{
					return;
				}
				serverPlayer.BroadcastPlayerData(false);
			}
		}

		public override void PerformNotifySlot(int slotId)
		{
			ItemSlotBagContent backpackContent = this[slotId] as ItemSlotBagContent;
			if (backpackContent != null)
			{
				base.PerformNotifySlot(backpackContent.BagIndex);
			}
			base.PerformNotifySlot(slotId);
		}

		public override object ActivateSlot(int slotId, ItemSlot sourceSlot, ref ItemStackMoveOperation op)
		{
			bool flag = slotId < this.bagSlots.Length && this.bagSlots[slotId].Itemstack == null;
			object packet = base.ActivateSlot(slotId, sourceSlot, ref op);
			if (flag)
			{
				this.bagInv.ReloadBagInventory(this, this.bagSlots);
			}
			return packet;
		}

		public override void DiscardAll()
		{
			for (int i = 0; i < this.bagSlots.Length; i++)
			{
				if (this.bagSlots[i].Itemstack != null)
				{
					this.dirtySlots.Add(i);
				}
				this.bagSlots[i].Itemstack = null;
			}
			this.bagInv.ReloadBagInventory(this, this.bagSlots);
		}

		public override void DropAll(Vec3d pos, int maxStackSize = 0)
		{
			IPlayer player = base.Player;
			JsonObject jsonObject;
			if (player == null)
			{
				jsonObject = null;
			}
			else
			{
				EntityPlayer entity = player.Entity;
				jsonObject = ((entity != null) ? entity.Properties.Attributes : null);
			}
			JsonObject attr = jsonObject;
			int timer = ((attr == null) ? GlobalConstants.TimeToDespawnPlayerInventoryDrops : attr["droppedItemsOnDeathTimer"].AsInt(GlobalConstants.TimeToDespawnPlayerInventoryDrops));
			for (int i = 0; i < this.bagSlots.Length; i++)
			{
				ItemSlot slot = this.bagSlots[i];
				if (slot.Itemstack != null)
				{
					EnumHandling handling = EnumHandling.PassThrough;
					slot.Itemstack.Collectible.OnHeldDropped(this.Api.World, base.Player, slot, slot.StackSize, ref handling);
					if (handling == EnumHandling.PassThrough)
					{
						this.dirtySlots.Add(i);
						base.spawnItemEntity(slot.Itemstack, pos, timer);
						slot.Itemstack = null;
					}
				}
			}
			this.bagInv.ReloadBagInventory(this, this.bagSlots);
		}

		protected ItemSlot[] bagSlots;

		protected BagInventory bagInv;
	}
}
