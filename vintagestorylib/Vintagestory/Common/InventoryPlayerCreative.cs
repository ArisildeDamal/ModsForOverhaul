using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common
{
	public class InventoryPlayerCreative : InventoryBasePlayer, ITabbedInventory, IInventory, IReadOnlyCollection<ItemSlot>, IEnumerable<ItemSlot>, IEnumerable
	{
		public bool Accessible
		{
			get
			{
				IPlayer player = this.Api.World.PlayerByUid(this.playerUID);
				EnumGameMode? mode = ((player != null) ? new EnumGameMode?(player.WorldData.CurrentGameMode) : null);
				return this.playerUID != null && mode.GetValueOrDefault() == EnumGameMode.Creative;
			}
		}

		public InventoryPlayerCreative(string className, string playerUID, ICoreAPI api)
			: base(className, playerUID, api)
		{
			this.blackholeSlot = new ItemSlotBlackHole(this);
			this.InvNetworkUtil = new CreativeNetworkUtil(this, api);
		}

		public InventoryPlayerCreative(string inventoryId, ICoreAPI api)
			: base(inventoryId, api)
		{
			this.blackholeSlot = new ItemSlotBlackHole(this);
			this.InvNetworkUtil = new CreativeNetworkUtil(this, api);
		}

		public override void LateInitialize(string inventoryID, ICoreAPI api)
		{
			base.LateInitialize(inventoryID, api);
			(this.InvNetworkUtil as CreativeNetworkUtil).Api = api;
		}

		public override float GetTransitionSpeedMul(EnumTransitionType transType, ItemStack stack)
		{
			return 0f;
		}

		public override bool CanPlayerAccess(IPlayer player, EntityPos position)
		{
			return base.CanPlayerAccess(player, position) && player.WorldData.CurrentGameMode == EnumGameMode.Creative;
		}

		public override object ActivateSlot(int slotId, ItemSlot sourceSlot, ref ItemStackMoveOperation op)
		{
			if (!this.Accessible)
			{
				return null;
			}
			if (op.ShiftDown)
			{
				return base.ActivateSlot(slotId, sourceSlot, ref op);
			}
			Packet_Client packet_Client = (Packet_Client)base.ActivateSlot(slotId, sourceSlot, ref op);
			packet_Client.ActivateInventorySlot.TabIndex = this.currentTab.Index;
			return packet_Client;
		}

		internal void UpdateFromWorld(IWorldAccessor world)
		{
			if (this.tabs.TabsByCode.Count == 0)
			{
				IList<Block> blocks = world.Blocks;
				IList<Item> items = world.Items;
				blocks = blocks.OrderBy(delegate(Block elem)
				{
					if (elem == null)
					{
						return null;
					}
					return new EnumBlockMaterial?(elem.BlockMaterial);
				}).ToList<Block>();
				items = items.OrderBy(delegate(Item elem)
				{
					if (elem == null)
					{
						return null;
					}
					return elem.Tool;
				}).ToList<Item>();
				CollectibleObject[] collectibles = new CollectibleObject[blocks.Count + items.Count];
				Array.Copy(blocks.ToArray<Block>(), collectibles, blocks.Count);
				Array.Copy(items.ToArray<Item>(), 0, collectibles, blocks.Count, items.Count);
				Dictionary<string, List<ItemStack>> dictionary = this.GatherTabStacks(collectibles);
				this.tabs = new CreativeTabs();
				foreach (KeyValuePair<string, List<ItemStack>> val in dictionary)
				{
					this.tabs.Add(this.CreateTab(val.Key, val.Value));
				}
				this.SetTab(0);
			}
		}

		private CreativeTab CreateTab(string tabCode, List<ItemStack> tabStacks)
		{
			CreativeTab tab = new CreativeTab(tabCode, new CreativeInventoryTab(tabStacks.Count, base.InventoryID, this.Api));
			int i = 0;
			foreach (ItemStack stack in tabStacks)
			{
				tab.Inventory[i++].Itemstack = stack;
			}
			return tab;
		}

		private Dictionary<string, List<ItemStack>> GatherTabStacks(CollectibleObject[] collectibles)
		{
			Dictionary<string, List<ItemStack>> itemstacksByTab = new Dictionary<string, List<ItemStack>>();
			foreach (CollectibleObject collectible in collectibles)
			{
				if (((collectible != null) ? collectible.CreativeInventoryTabs : null) != null)
				{
					foreach (string tab in collectible.CreativeInventoryTabs)
					{
						List<ItemStack> stackList;
						if (!itemstacksByTab.TryGetValue(tab, out stackList))
						{
							stackList = new List<ItemStack>();
							itemstacksByTab[tab] = stackList;
						}
						stackList.Add(new ItemStack(collectible, 1));
					}
				}
				if (((collectible != null) ? collectible.CreativeInventoryStacks : null) != null)
				{
					for (int j = 0; j < collectible.CreativeInventoryStacks.Length; j++)
					{
						CreativeTabAndStackList ctasl = collectible.CreativeInventoryStacks[j];
						for (int k = 0; k < ctasl.Tabs.Length; k++)
						{
							List<ItemStack> stackList2;
							if (!itemstacksByTab.TryGetValue(ctasl.Tabs[k], out stackList2))
							{
								stackList2 = new List<ItemStack>();
								itemstacksByTab[ctasl.Tabs[k]] = stackList2;
							}
							for (int l = 0; l < ctasl.Stacks.Length; l++)
							{
								ItemStack stack = ctasl.Stacks[l].ResolvedItemstack.Clone();
								stack.ResolveBlockOrItem(this.Api.World);
								stackList2.Add(stack);
							}
						}
					}
				}
			}
			return itemstacksByTab;
		}

		public CreativeTab CurrentTab
		{
			get
			{
				return this.currentTab;
			}
		}

		public int CurrentTabIndex
		{
			get
			{
				return (this.currentTab.Inventory as CreativeInventoryTab).TabIndex;
			}
		}

		public void SetTab(int tabIndex)
		{
			this.currentTab = this.tabs.Tabs.FirstOrDefault((CreativeTab tab) => tab.Index == tabIndex);
			(this.currentTab.Inventory as CreativeInventoryTab).TabIndex = tabIndex;
		}

		public override void AfterBlocksLoaded(IWorldAccessor world)
		{
		}

		public override object Open(IPlayer player)
		{
			this.UpdateFromWorld(player.Entity.World);
			CreativeTab creativeTab = this.currentTab;
			if (creativeTab == null)
			{
				return null;
			}
			return creativeTab.Inventory.Open(player);
		}

		public override object Close(IPlayer player)
		{
			CreativeTab creativeTab = this.currentTab;
			if (creativeTab == null)
			{
				return null;
			}
			return creativeTab.Inventory.Close(player);
		}

		public override int Count
		{
			get
			{
				return this.currentTab.Inventory.Count;
			}
		}

		public CreativeTabs CreativeTabs
		{
			get
			{
				return this.tabs;
			}
		}

		public override ItemSlot this[int slotId]
		{
			get
			{
				if (slotId == 99999)
				{
					return this.blackholeSlot;
				}
				if (slotId < 0 || slotId >= this.Count)
				{
					return new ItemSlotCreative(this);
				}
				return this.currentTab.Inventory[slotId];
			}
			set
			{
				throw new NotSupportedException("InventoryPlayerCreative doesn't support replacing slots");
			}
		}

		public override void ResolveBlocksOrItems()
		{
		}

		public override int GetSlotId(ItemSlot slot)
		{
			if (slot is ItemSlotBlackHole)
			{
				return 99999;
			}
			if (slot.Itemstack == null)
			{
				return -1;
			}
			for (int i = 0; i < this.Count; i++)
			{
				if (slot.Itemstack.Equals(this[i].Itemstack))
				{
					return i;
				}
			}
			return -1;
		}

		public override WeightedSlot GetBestSuitedSlot(ItemSlot sourceSlot, ItemStackMoveOperation op, List<ItemSlot> skipSlots = null)
		{
			if (skipSlots.Contains(this.blackholeSlot))
			{
				return new WeightedSlot
				{
					weight = -1f
				};
			}
			if (!this.blackholeSlot.CanTakeFrom(sourceSlot, EnumMergePriority.AutoMerge))
			{
				return new WeightedSlot
				{
					slot = null,
					weight = 0f
				};
			}
			return new WeightedSlot
			{
				slot = this.blackholeSlot,
				weight = 0.01f
			};
		}

		public override void FromTreeAttributes(ITreeAttribute tree)
		{
		}

		public override void ToTreeAttributes(ITreeAttribute tree)
		{
		}

		public override void MarkSlotDirty(int slotId)
		{
		}

		public override void DiscardAll()
		{
		}

		public override void DropAll(Vec3d pos, int maxStackSize = 0)
		{
		}

		public override bool HasOpened(IPlayer player)
		{
			return player.WorldData.CurrentGameMode == EnumGameMode.Creative;
		}

		public CreativeTab GetSelectedTab()
		{
			return this.currentTab;
		}

		public CreativeTabs tabs = new CreativeTabs();

		private CreativeTab currentTab;

		private ItemSlotBlackHole blackholeSlot;
	}
}
