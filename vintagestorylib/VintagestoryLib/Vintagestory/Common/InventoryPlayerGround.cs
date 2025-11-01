using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common
{
	internal class InventoryPlayerGround : InventoryBasePlayer
	{
		public InventoryPlayerGround(string className, string playerUID, ICoreAPI api)
			: base(className, playerUID, api)
		{
			this.slot = new ItemSlotGround(this);
		}

		public InventoryPlayerGround(string inventoryID, ICoreAPI api)
			: base(inventoryID, api)
		{
			this.slot = new ItemSlotGround(this);
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
			return new WeightedSlot
			{
				slot = null,
				weight = 0f
			};
		}

		public override void FromTreeAttributes(ITreeAttribute tree)
		{
		}

		public override void ToTreeAttributes(ITreeAttribute tree)
		{
		}

		public override void OnItemSlotModified(ItemSlot slot)
		{
			IPlayer player = this.Api.World.PlayerByUid(this.playerUID);
			Entity entityplayer = ((player != null) ? player.Entity : null);
			if (slot.Itemstack != null && entityplayer != null)
			{
				Vec3d spawnpos = entityplayer.SidedPos.XYZ.Add(0.0, (double)(entityplayer.CollisionBox.Y1 + entityplayer.CollisionBox.Y2 * 0.75f), 0.0);
				Vec3d velocity = (entityplayer.SidedPos.AheadCopy(1.0).XYZ.Add(entityplayer.LocalEyePos) - spawnpos) * 0.1 + entityplayer.SidedPos.Motion * 1.5;
				ItemStack stack = slot.Itemstack;
				slot.Itemstack = null;
				while (stack.StackSize > 0)
				{
					Vec3d velo = velocity.Clone().Add((double)((float)(this.Api.World.Rand.NextDouble() - 0.5) / 60f), (double)((float)(this.Api.World.Rand.NextDouble() - 0.5) / 60f), (double)((float)(this.Api.World.Rand.NextDouble() - 0.5) / 60f));
					ItemStack dropStack = stack.Clone();
					dropStack.StackSize = Math.Min(4, stack.StackSize);
					stack.StackSize -= dropStack.StackSize;
					this.Api.World.SpawnItemEntity(dropStack, spawnpos, velo);
				}
			}
		}

		public override void MarkSlotDirty(int slotId)
		{
		}

		private ItemSlot slot;
	}
}
