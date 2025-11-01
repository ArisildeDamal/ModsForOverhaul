using System;
using Vintagestory.API.Common;

namespace Vintagestory.Common
{
	public class CraftingInventoryNetworkUtil : InventoryNetworkUtil
	{
		public CraftingInventoryNetworkUtil(InventoryBase inv, ICoreAPI api)
			: base(inv, api)
		{
		}

		public override void UpdateFromPacket(IWorldAccessor resolver, Packet_InventoryContents packet)
		{
			for (int i = 0; i < packet.ItemstacksCount; i++)
			{
				this.inv[i].Itemstack = base.ItemStackFromPacket(resolver, packet.Itemstacks[i]);
			}
			(this.inv as InventoryCraftingGrid).FindMatchingRecipe();
		}
	}
}
