using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Vintagestory.Common
{
	public class PlayerInventoryNetworkUtil : InventoryNetworkUtil
	{
		public PlayerInventoryNetworkUtil(InventoryBase inv, ICoreAPI api)
			: base(inv, api)
		{
		}

		public override void UpdateFromPacket(IWorldAccessor world, Packet_InventoryUpdate packet)
		{
			ItemSlot slot = this.inv[packet.SlotId];
			if (this.IsOwnHotbarSlotClient(slot))
			{
				ItemStack prevStack = slot.Itemstack;
				if (prevStack != null)
				{
					ItemStack newStackPreview = base.ItemStackFromPacket(world, packet.ItemStack);
					if (newStackPreview == null || prevStack.Collectible != newStackPreview.Collectible)
					{
						IClientPlayer plr = (world as IClientWorldAccessor).Player;
						prevStack.Collectible.OnHeldInteractCancel(0f, slot, plr.Entity, plr.CurrentBlockSelection, plr.CurrentEntitySelection, EnumItemUseCancelReason.Destroyed);
					}
				}
			}
			base.UpdateFromPacket(world, packet);
		}

		private bool IsOwnHotbarSlotClient(ItemSlot slot)
		{
			ICoreClientAPI coreClientAPI = base.Api as ICoreClientAPI;
			return ((coreClientAPI != null) ? coreClientAPI.World.Player.InventoryManager.ActiveHotbarSlot : null) == slot;
		}
	}
}
