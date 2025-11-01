using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common
{
	public class InventoryCraftingGrid : InventoryBasePlayer
	{
		public InventoryCraftingGrid(string inventoryID, ICoreAPI api)
			: base(inventoryID, api)
		{
			this.slots = base.GenEmptySlots(this.GridSizeSq);
			this.outputSlot = new ItemSlotCraftingOutput(this);
			this.InvNetworkUtil = new CraftingInventoryNetworkUtil(this, api);
		}

		public InventoryCraftingGrid(string className, string instanceID, ICoreAPI api)
			: base(className, instanceID, api)
		{
			this.slots = base.GenEmptySlots(this.GridSizeSq);
			this.outputSlot = new ItemSlotCraftingOutput(this);
			this.InvNetworkUtil = new CraftingInventoryNetworkUtil(this, api);
		}

		public override void LateInitialize(string inventoryID, ICoreAPI api)
		{
			base.LateInitialize(inventoryID, api);
			(this.InvNetworkUtil as CraftingInventoryNetworkUtil).Api = api;
		}

		internal void BeginCraft()
		{
			this.isCrafting = true;
		}

		internal void EndCraft()
		{
			this.isCrafting = false;
			this.FindMatchingRecipe();
		}

		public bool CanStillCraftCurrent()
		{
			return this.MatchingRecipe != null && this.MatchingRecipe.Matches(this.Api.World.PlayerByUid(this.playerUID), this.slots, this.GridSize);
		}

		public override float GetTransitionSpeedMul(EnumTransitionType transType, ItemStack stack)
		{
			if (stack == this.outputSlot.Itemstack)
			{
				return 0f;
			}
			return base.GetTransitionSpeedMul(transType, stack);
		}

		public override int Count
		{
			get
			{
				return this.GridSizeSq + 1;
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
				if (slotId == this.GridSizeSq)
				{
					return this.outputSlot;
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
				if (slotId == this.GridSizeSq)
				{
					this.outputSlot = (ItemSlotCraftingOutput)value;
					return;
				}
				this.slots[slotId] = value;
			}
		}

		public override object ActivateSlot(int slotId, ItemSlot sourceSlot, ref ItemStackMoveOperation op)
		{
			object packet;
			if (slotId == this.GridSizeSq)
			{
				this.BeginCraft();
				packet = base.ActivateSlot(slotId, sourceSlot, ref op);
				if (!this.outputSlot.Empty && op.ShiftDown)
				{
					if (this.Api.Side == EnumAppSide.Client)
					{
						this.outputSlot.Itemstack = null;
					}
					else
					{
						base.Player.InventoryManager.DropItem(this.outputSlot, true);
					}
				}
				this.EndCraft();
			}
			else
			{
				packet = base.ActivateSlot(slotId, sourceSlot, ref op);
			}
			return packet;
		}

		public override void OnItemSlotModified(ItemSlot slot)
		{
			if (this.isCrafting)
			{
				return;
			}
			if (slot is ItemSlotCraftingOutput)
			{
				return;
			}
			this.DropSlotIfHot(slot, base.Player);
			this.FindMatchingRecipe();
		}

		public override void DidModifyItemSlot(ItemSlot slot, ItemStack extractedStack = null)
		{
			base.DidModifyItemSlot(slot, extractedStack);
		}

		public override bool TryMoveItemStack(IPlayer player, string[] invIds, int[] slotIds, ref ItemStackMoveOperation op)
		{
			bool flag = base.TryMoveItemStack(player, invIds, slotIds, ref op);
			if (flag)
			{
				this.FindMatchingRecipe();
			}
			return flag;
		}

		internal void FindMatchingRecipe()
		{
			this.MatchingRecipe = null;
			this.outputSlot.Itemstack = null;
			List<GridRecipe> recipes = this.Api.World.GridRecipes;
			IPlayer player = this.Api.World.PlayerByUid(this.playerUID);
			foreach (GridRecipe recipe in recipes)
			{
				if (!recipe.Shapeless && recipe.Enabled && recipe.Matches(player, this.slots, this.GridSize))
				{
					this.FoundMatch(recipe);
					return;
				}
			}
			foreach (GridRecipe recipe2 in recipes)
			{
				if (recipe2.Shapeless && recipe2.Enabled && recipe2.Matches(player, this.slots, this.GridSize))
				{
					this.FoundMatch(recipe2);
					return;
				}
			}
			this.dirtySlots.Add(this.GridSizeSq);
		}

		private void FoundMatch(GridRecipe recipe)
		{
			this.MatchingRecipe = recipe;
			this.MatchingRecipe.GenerateOutputStack(this.slots, this.outputSlot);
			this.dirtySlots.Add(this.GridSizeSq);
		}

		internal void ConsumeIngredients(ItemSlot outputSlot)
		{
			if (this.MatchingRecipe == null || outputSlot.Itemstack == null)
			{
				return;
			}
			if (!outputSlot.Itemstack.Collectible.ConsumeCraftingIngredients(this.slots, outputSlot, this.MatchingRecipe))
			{
				this.MatchingRecipe.ConsumeInput(this.Api.World.PlayerByUid(this.playerUID), this.slots, this.GridSize);
			}
			for (int i = 0; i < this.GridSizeSq + 1; i++)
			{
				this.dirtySlots.Add(i);
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
			ItemSlot[] attrSlots = this.SlotsFromTreeAttributes(tree, null, null);
			int? num = ((attrSlots != null) ? new int?(attrSlots.Length) : null);
			int num2 = this.slots.Length;
			if ((num.GetValueOrDefault() == num2) & (num != null))
			{
				this.slots = attrSlots;
			}
		}

		public override void ToTreeAttributes(ITreeAttribute tree)
		{
			base.SlotsToTreeAttributes(this.slots, tree);
			this.ResolveBlocksOrItems();
		}

		public override void OnOwningEntityDeath(Vec3d pos)
		{
			foreach (ItemSlot slot in this)
			{
				if (!(slot is ItemSlotCraftingOutput) && !slot.Empty)
				{
					this.Api.World.SpawnItemEntity(slot.Itemstack, pos, null);
					slot.Itemstack = null;
					slot.MarkDirty();
				}
			}
		}

		private int GridSize = 3;

		private int GridSizeSq = 9;

		private ItemSlot[] slots;

		private ItemSlotCraftingOutput outputSlot;

		public GridRecipe MatchingRecipe;

		private bool isCrafting;
	}
}
