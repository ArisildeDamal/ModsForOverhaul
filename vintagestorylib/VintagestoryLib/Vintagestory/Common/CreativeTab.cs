using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;

namespace Vintagestory.Common
{
	public class CreativeTab
	{
		public IInventory Inventory { get; set; }

		public string Code { get; set; }

		public Dictionary<int, string> SearchCache { get; set; }

		public Dictionary<int, string> SearchCacheNames { get; set; }

		public int Index { get; set; }

		public CreativeTab(string code, IInventory inventory)
		{
			this.Code = code;
			this.Inventory = inventory;
		}

		public Dictionary<int, string> CreateSearchCache(IWorldAccessor world)
		{
			Dictionary<int, string> searchCache = new Dictionary<int, string>();
			Dictionary<int, string> searchCacheNames = new Dictionary<int, string>();
			int slotID = 0;
			while (slotID < this.Inventory.Count && !((ClientCoreAPI)world.Api).disposed)
			{
				ItemSlot slot = this.Inventory[slotID];
				ItemStack stack = slot.Itemstack;
				if (stack != null)
				{
					string stackName = stack.GetName();
					searchCacheNames[slotID] = stackName.ToSearchFriendly().ToLowerInvariant();
					ISearchTextProvider istp = stack.Collectible as ISearchTextProvider;
					searchCache[slotID] = stackName + " " + (((istp != null) ? istp.GetSearchText(world, slot) : null) ?? stack.GetDescription(world, slot, false).ToSearchFriendly().ToLowerInvariant());
				}
				slotID++;
			}
			this.SearchCacheNames = searchCacheNames;
			this.SearchCache = searchCache;
			return this.SearchCache;
		}
	}
}
