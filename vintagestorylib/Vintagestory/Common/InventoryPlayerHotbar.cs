using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.Common
{
	internal class InventoryPlayerHotbar : InventoryBasePlayer
	{
		public InventoryPlayerHotbar(string className, string playerUID, ICoreAPI api)
			: base(className, playerUID, api)
		{
			this.slots = base.GenEmptySlots(12);
			this.baseWeight = 1.1f;
			this.InvNetworkUtil = new PlayerInventoryNetworkUtil(this, api);
		}

		public InventoryPlayerHotbar(string inventoryId, ICoreAPI api)
			: base(inventoryId, api)
		{
			this.slots = base.GenEmptySlots(12);
			this.baseWeight = 1.1f;
			this.InvNetworkUtil = new PlayerInventoryNetworkUtil(this, api);
		}

		public override void OnItemSlotModified(ItemSlot slot)
		{
			base.OnItemSlotModified(slot);
			this.updateSlotStatMods(slot);
		}

		public void updateSlotStatMods(ItemSlot slot)
		{
			if (slot is ItemSlotOffhand)
			{
				this.updateSlotStatMods(this.offHandStatMod, slot, "offhanditem");
			}
			if (slot == base.Player.InventoryManager.ActiveHotbarSlot)
			{
				this.DropSlotIfHot(slot, base.Player);
				this.updateSlotStatMods(this.mainHandStatMod, slot, "mainhanditem");
			}
		}

		public void updateSlotStatMods(List<string> list, ItemSlot slot, string handcategory)
		{
			IPlayer player = this.Api.World.PlayerByUid(this.playerUID);
			if (this.Api.Side == EnumAppSide.Client)
			{
				player.InventoryManager.BroadcastHotbarSlot();
			}
			EntityPlayer entity = player.Entity;
			if (((entity != null) ? entity.Stats : null) == null)
			{
				return;
			}
			foreach (string key in list)
			{
				player.Entity.Stats.Remove(key, handcategory);
			}
			list.Clear();
			if (slot.Empty)
			{
				return;
			}
			JsonObject itemAttributes = slot.Itemstack.ItemAttributes;
			if (itemAttributes == null || !itemAttributes["statModifier"].Exists)
			{
				if (handcategory == "offhanditem")
				{
					player.Entity.Stats.Set("hungerrate", "offhanditem", 0.2f, true);
					list.Add("hungerrate");
				}
				return;
			}
			JsonObject itemAttributes2 = slot.Itemstack.ItemAttributes;
			JsonObject statmods = ((itemAttributes2 != null) ? itemAttributes2["statModifier"] : null);
			foreach (JsonObject jsonObject in statmods)
			{
				string key2 = jsonObject.AsString(null);
				player.Entity.Stats.Set(key2, handcategory, statmods[key2].AsFloat(0f), true);
				list.Add(key2);
			}
		}

		public override int Count
		{
			get
			{
				return this.slots.Length;
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
				return this.slots[slotId];
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
				this.slots[slotId] = value;
			}
		}

		public override void FromTreeAttributes(ITreeAttribute tree)
		{
			this.slots = this.SlotsFromTreeAttributes(tree, null, null);
			if (this.slots.Length < 11)
			{
				this.slots = this.slots.Append(new ItemSlotSkill(this));
				this.slots = this.slots.Append(new ItemSlotOffhand(this));
				return;
			}
			if (this.slots.Length < 12)
			{
				this.slots = this.slots.Append(new ItemSlotOffhand(this));
				if (this.slots.Length == 12)
				{
					this.slots[11].Itemstack = this.slots[10].Itemstack;
					this.slots[10].Itemstack = null;
				}
			}
		}

		public override float GetSuitability(ItemSlot sourceSlot, ItemSlot targetSlot, bool isMerge)
		{
			if (!isMerge && targetSlot == this.slots[11])
			{
				return 0.4f;
			}
			return base.GetSuitability(sourceSlot, targetSlot, isMerge) + ((sourceSlot is ItemSlotGround || sourceSlot is DummySlot) ? 0.5f : 0f);
		}

		public override void ToTreeAttributes(ITreeAttribute tree)
		{
			base.SlotsToTreeAttributes(this.slots, tree);
			this.ResolveBlocksOrItems();
		}

		protected override ItemSlot NewSlot(int slotId)
		{
			if (slotId == 10)
			{
				return new ItemSlotSkill(this);
			}
			if (slotId == 11)
			{
				return new ItemSlotOffhand(this);
			}
			return new ItemSlotSurvival(this)
			{
				BackgroundIcon = ((1 + slotId).ToString() ?? "")
			};
		}

		public override void DropAll(Vec3d pos, int maxStackSize = 0)
		{
			this.slots[10].Itemstack = null;
			base.DropAll(pos, maxStackSize);
		}

		private ItemSlot[] slots;

		private List<string> mainHandStatMod = new List<string>();

		private List<string> offHandStatMod = new List<string>();
	}
}
