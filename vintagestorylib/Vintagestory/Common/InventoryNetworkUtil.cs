using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.Common
{
	public class InventoryNetworkUtil : IInventoryNetworkUtil
	{
		public ICoreAPI Api { get; set; }

		public bool PauseInventoryUpdates
		{
			get
			{
				return this.pauseInvUpdates;
			}
			set
			{
				bool flag = !value && this.pauseInvUpdates;
				this.pauseInvUpdates = value;
				if (flag)
				{
					while (this.pkts.Count > 0)
					{
						Packet_InventoryUpdate pkt = this.pkts.Dequeue();
						this.UpdateFromPacket(this.Api.World, pkt);
					}
				}
			}
		}

		public InventoryNetworkUtil(InventoryBase inv, ICoreAPI api)
		{
			this.inv = inv;
			this.Api = api;
		}

		public virtual void HandleClientPacket(IPlayer byPlayer, int packetId, byte[] data)
		{
			Packet_Client packet = new Packet_Client();
			Packet_ClientSerializer.DeserializeBuffer(data, data.Length, packet);
			this.HandleClientPacket(byPlayer, packetId, packet);
		}

		public virtual void HandleClientPacket(IPlayer byPlayer, int packetId, Packet_Client packet)
		{
			IWorldPlayerData plrData = byPlayer.WorldData;
			switch (packetId)
			{
			case 7:
			{
				Packet_ActivateInventorySlot p = packet.ActivateInventorySlot;
				EnumMouseButton button = (EnumMouseButton)p.MouseButton;
				long lastChanged = p.TargetLastChanged;
				if (this.inv.lastChangedSinceServerStart < lastChanged)
				{
					this.SendInventoryContents(byPlayer, this.inv.InventoryID);
					return;
				}
				int targetSlotId = p.TargetSlot;
				IInventory targetInv = this.inv;
				if (this.inv is ITabbedInventory)
				{
					((ITabbedInventory)this.inv).SetTab(packet.ActivateInventorySlot.TabIndex);
				}
				ItemSlot targetSlot = targetInv[targetSlotId];
				if (targetSlot == null)
				{
					this.Api.World.Logger.Warning("{0} left-clicked slot {1} in {2}, but slot did not exist!", new object[]
					{
						(byPlayer != null) ? byPlayer.PlayerName : null,
						targetSlotId,
						targetInv.InventoryID
					});
					return;
				}
				string sourceInvId = "mouse-" + plrData.PlayerUID;
				ItemSlot sourceSlot = byPlayer.InventoryManager.GetInventory(sourceInvId)[0];
				ItemStackMoveOperation op = new ItemStackMoveOperation(this.Api.World, button, (EnumModifierKey)p.Modifiers, (EnumMergePriority)p.Priority, 0);
				op.WheelDir = p.Dir;
				op.ActingPlayer = byPlayer;
				if (button == EnumMouseButton.Wheel)
				{
					op.RequestedQuantity = 1;
				}
				string mouseSlotContents = (sourceSlot.Empty ? "empty" : string.Format("{0}x{1}", sourceSlot.StackSize, sourceSlot.GetStackName()));
				string targetSlotContents = (targetSlot.Empty ? "empty" : string.Format("{0}x{1}", targetSlot.StackSize, targetSlot.GetStackName()));
				targetInv.ActivateSlot(targetSlotId, sourceSlot, ref op);
				string mouseSlotContentsAfter = (sourceSlot.Empty ? "empty" : string.Format("{0}x{1}", sourceSlot.StackSize, sourceSlot.GetStackName()));
				if (mouseSlotContents != mouseSlotContentsAfter)
				{
					string targetSlotContentsAfter = (targetSlot.Empty ? "empty" : string.Format("{0}x{1}", targetSlot.StackSize, targetSlot.GetStackName()));
					ILogger logger = this.Api.World.Logger;
					string text = "{0} left clicked slot {1} in {2}. Before: (mouse: {3}, inv: {4}), after: (mouse: {5}, inv: {6})";
					object[] array = new object[7];
					int num = 0;
					IPlayer actingPlayer = op.ActingPlayer;
					array[num] = ((actingPlayer != null) ? actingPlayer.PlayerName : null);
					array[1] = targetSlotId;
					array[2] = targetInv.InventoryID;
					array[3] = mouseSlotContents;
					array[4] = targetSlotContents;
					array[5] = mouseSlotContentsAfter;
					array[6] = targetSlotContentsAfter;
					logger.Audit(text, array);
					return;
				}
				break;
			}
			case 8:
			{
				string[] invIds = new string[]
				{
					packet.MoveItemstack.SourceInventoryId,
					packet.MoveItemstack.TargetInventoryId
				};
				int[] slotIds = new int[]
				{
					packet.MoveItemstack.SourceSlot,
					packet.MoveItemstack.TargetSlot
				};
				if (this.SendDirtyInventoryContents(byPlayer, invIds[0], packet.MoveItemstack.SourceLastChanged) || this.SendDirtyInventoryContents(byPlayer, invIds[1], packet.MoveItemstack.TargetLastChanged))
				{
					InventoryBase targetInv2 = (InventoryBase)byPlayer.InventoryManager.GetInventory(invIds[1]);
					this.Api.World.Logger.Audit("Revert itemstack move command by {0} to move {1}x{4} from {2} to {3}", new object[]
					{
						byPlayer.PlayerName,
						packet.MoveItemstack.Quantity,
						invIds[0],
						invIds[1],
						targetInv2[slotIds[1]].GetStackName()
					});
					return;
				}
				if (this.inv is ITabbedInventory)
				{
					((ITabbedInventory)this.inv).SetTab(packet.MoveItemstack.TabIndex);
				}
				ItemStackMoveOperation op2 = new ItemStackMoveOperation(this.Api.World, (EnumMouseButton)packet.MoveItemstack.MouseButton, (EnumModifierKey)packet.MoveItemstack.Modifiers, (EnumMergePriority)packet.MoveItemstack.Priority, packet.MoveItemstack.Quantity);
				op2.ActingPlayer = byPlayer;
				ItemSlot itemSlot = this.inv.GetSlotsIfExists(byPlayer, invIds, slotIds)[0];
				AssetLocation assetLocation;
				if (itemSlot == null)
				{
					assetLocation = null;
				}
				else
				{
					ItemStack itemstack = itemSlot.Itemstack;
					assetLocation = ((itemstack != null) ? itemstack.Collectible.Code : null);
				}
				AssetLocation collectibleCode = assetLocation;
				if (this.inv.TryMoveItemStack(byPlayer, invIds, slotIds, ref op2))
				{
					this.Api.World.Logger.Audit("{0} moved {1}x{4} from {2} to {3}", new object[]
					{
						byPlayer.PlayerName,
						packet.MoveItemstack.Quantity,
						invIds[0],
						invIds[1],
						collectibleCode
					});
					return;
				}
				this.SendInventoryContents(byPlayer, invIds[0]);
				this.SendInventoryContents(byPlayer, invIds[1]);
				return;
			}
			case 9:
			{
				Packet_FlipItemstacks p2 = packet.Flipitemstacks;
				string[] invIds2 = new string[] { p2.SourceInventoryId, p2.TargetInventoryId };
				int[] slotIds2 = new int[] { p2.SourceSlot, p2.TargetSlot };
				long[] lastChanged2 = new long[] { p2.SourceLastChanged, p2.TargetLastChanged };
				if (this.SendDirtyInventoryContents(byPlayer, invIds2[0], lastChanged2[0]) || this.SendDirtyInventoryContents(byPlayer, invIds2[1], lastChanged2[1]))
				{
					return;
				}
				InventoryBase sourceInv = (InventoryBase)byPlayer.InventoryManager.GetInventory(invIds2[0]);
				InventoryBase targetInv3 = (InventoryBase)byPlayer.InventoryManager.GetInventory(invIds2[1]);
				if (sourceInv is ITabbedInventory)
				{
					((ITabbedInventory)sourceInv).SetTab(packet.Flipitemstacks.SourceTabIndex);
				}
				if (targetInv3 is ITabbedInventory)
				{
					((ITabbedInventory)targetInv3).SetTab(packet.Flipitemstacks.TargetTabIndex);
				}
				if (this.inv.TryFlipItemStack(byPlayer, invIds2, slotIds2, lastChanged2))
				{
					this.NotifyPlayersItemstackMoved(byPlayer, invIds2, slotIds2);
					return;
				}
				this.RevertPlayerItemstackMove(byPlayer, invIds2, slotIds2);
				break;
			}
			default:
				return;
			}
		}

		protected virtual bool SendDirtyInventoryContents(IPlayer owningPlayer, string inventoryId, long lastChangedClient)
		{
			InventoryBase targetInv = (InventoryBase)owningPlayer.InventoryManager.GetInventory(inventoryId);
			if (targetInv == null)
			{
				return false;
			}
			if (targetInv.lastChangedSinceServerStart > lastChangedClient)
			{
				this.SendInventoryContents(owningPlayer, inventoryId);
				return true;
			}
			return false;
		}

		protected virtual void RevertPlayerItemstackMove(IPlayer owningPlayer, string[] invIds, int[] slotIds)
		{
			ItemSlot[] slots = this.inv.GetSlotsIfExists(owningPlayer, invIds, slotIds);
			if (slots[0] != null && slots[1] != null)
			{
				Packet_Server serverPacket = InventoryNetworkUtil.getDoubleUpdatePacket(owningPlayer, invIds, slotIds);
				((ICoreServerAPI)this.Api).Network.SendArbitraryPacket(serverPacket, new IServerPlayer[] { (IServerPlayer)owningPlayer });
			}
		}

		protected virtual void SendInventoryContents(IPlayer owningPlayer, string inventoryId)
		{
			InventoryBase targetInv = (InventoryBase)owningPlayer.InventoryManager.GetInventory(inventoryId);
			if (targetInv == null)
			{
				return;
			}
			Packet_InventoryContents packet = (targetInv.InvNetworkUtil as InventoryNetworkUtil).ToPacket(owningPlayer);
			Packet_Server serverPacket = new Packet_Server
			{
				Id = 30,
				InventoryContents = packet
			};
			((ICoreServerAPI)this.Api).Network.SendArbitraryPacket(serverPacket, new IServerPlayer[] { (IServerPlayer)owningPlayer });
		}

		protected virtual void NotifyPlayersItemstackMoved(IPlayer player, string[] invIds, int[] slotIds)
		{
			Packet_Server serverPacket = InventoryNetworkUtil.getDoubleUpdatePacket(player, invIds, slotIds);
			((ICoreServerAPI)this.Api).Network.BroadcastArbitraryPacket(serverPacket, new IServerPlayer[] { (IServerPlayer)player });
		}

		public static Packet_Server getDoubleUpdatePacket(IPlayer player, string[] invIds, int[] slotIds)
		{
			IInventory inventory = player.InventoryManager.GetInventory(invIds[0]);
			IInventory inv2 = player.InventoryManager.GetInventory(invIds[1]);
			ItemStack itemstack = inventory[slotIds[0]].Itemstack;
			ItemStack itemstack2 = inv2[slotIds[1]].Itemstack;
			Packet_InventoryDoubleUpdate packet = new Packet_InventoryDoubleUpdate
			{
				ClientId = player.ClientId,
				InventoryId1 = invIds[0],
				InventoryId2 = invIds[1],
				SlotId1 = slotIds[0],
				SlotId2 = slotIds[1],
				ItemStack1 = ((itemstack != null) ? StackConverter.ToPacket(itemstack) : null),
				ItemStack2 = ((itemstack2 != null) ? StackConverter.ToPacket(itemstack2) : null)
			};
			return new Packet_Server
			{
				Id = 32,
				InventoryDoubleUpdate = packet
			};
		}

		internal virtual Packet_ItemStack[] CreatePacketItemStacks()
		{
			Packet_ItemStack[] itemstacks = new Packet_ItemStack[this.inv.CountForNetworkPacket];
			for (int i = 0; i < this.inv.CountForNetworkPacket; i++)
			{
				IItemStack stack = this.inv[i].Itemstack;
				if (stack != null)
				{
					MemoryStream ms = new MemoryStream();
					BinaryWriter writer = new BinaryWriter(ms);
					stack.Attributes.ToBytes(writer);
					itemstacks[i] = new Packet_ItemStack
					{
						ItemClass = (int)stack.Class,
						ItemId = stack.Id,
						StackSize = stack.StackSize,
						Attributes = ms.ToArray()
					};
				}
				else
				{
					itemstacks[i] = new Packet_ItemStack
					{
						ItemClass = -1,
						ItemId = 0,
						StackSize = 0
					};
				}
			}
			return itemstacks;
		}

		public virtual Packet_InventoryContents ToPacket(IPlayer player)
		{
			Packet_InventoryContents packet_InventoryContents = new Packet_InventoryContents();
			packet_InventoryContents.ClientId = player.ClientId;
			packet_InventoryContents.InventoryId = this.inv.InventoryID;
			packet_InventoryContents.InventoryClass = this.inv.ClassName;
			Packet_ItemStack[] Itemstacks = this.CreatePacketItemStacks();
			packet_InventoryContents.SetItemstacks(Itemstacks, Itemstacks.Length, Itemstacks.Length);
			return packet_InventoryContents;
		}

		public virtual Packet_Server getSlotUpdatePacket(IPlayer player, int slotId)
		{
			ItemSlot slot = this.inv[slotId];
			if (slot == null)
			{
				return null;
			}
			ItemStack itemstack = slot.Itemstack;
			Packet_ItemStack pstack = null;
			if (itemstack != null)
			{
				pstack = StackConverter.ToPacket(itemstack);
			}
			Packet_InventoryUpdate packet = new Packet_InventoryUpdate
			{
				ClientId = player.ClientId,
				InventoryId = this.inv.InventoryID,
				ItemStack = pstack,
				SlotId = slotId
			};
			return new Packet_Server
			{
				Id = 31,
				InventoryUpdate = packet
			};
		}

		public virtual object DidOpen(IPlayer player)
		{
			if (this.inv.Api.Side != EnumAppSide.Client)
			{
				return null;
			}
			Packet_InvOpenClose packet = new Packet_InvOpenClose
			{
				InventoryId = this.inv.InventoryID,
				Opened = 1
			};
			return new Packet_Client
			{
				Id = 30,
				InvOpenedClosed = packet
			};
		}

		public virtual object DidClose(IPlayer player)
		{
			if (this.inv.Api.Side != EnumAppSide.Client)
			{
				return null;
			}
			Packet_InvOpenClose packet = new Packet_InvOpenClose
			{
				InventoryId = this.inv.InventoryID,
				Opened = 0
			};
			return new Packet_Client
			{
				Id = 30,
				InvOpenedClosed = packet
			};
		}

		public virtual void UpdateFromPacket(IWorldAccessor resolver, Packet_InventoryContents packet)
		{
			for (int i = 0; i < packet.ItemstacksCount; i++)
			{
				ItemSlot slot = this.inv[i];
				if (this.UpdateSlotStack(slot, this.ItemStackFromPacket(resolver, packet.Itemstacks[i])))
				{
					this.inv.DidModifyItemSlot(slot, null);
				}
			}
		}

		public virtual void UpdateFromPacket(IWorldAccessor resolver, Packet_InventoryUpdate packet)
		{
			if (this.PauseInventoryUpdates)
			{
				this.pkts.Enqueue(packet);
				return;
			}
			if (packet.SlotId >= this.inv.Count)
			{
				string[] array = new string[10];
				array[0] = "Client received server InventoryUpdate for ";
				array[1] = this.inv.InventoryID;
				array[2] = ", slot ";
				array[3] = packet.SlotId.ToString();
				array[4] = " but max is ";
				array[5] = (this.inv.Count - 1).ToString();
				array[6] = ". For ";
				array[7] = this.inv.ClassName;
				array[8] = " at ";
				int num = 9;
				BlockPos pos = this.inv.Pos;
				array[num] = ((pos != null) ? pos.ToString() : null);
				throw new Exception(string.Concat(array));
			}
			ItemSlot slot = this.inv[packet.SlotId];
			if (slot == null)
			{
				return;
			}
			this.UpdateSlotStack(slot, this.ItemStackFromPacket(resolver, packet.ItemStack));
			this.inv.DidModifyItemSlot(slot, null);
		}

		public virtual void UpdateFromPacket(IWorldAccessor resolver, Packet_InventoryDoubleUpdate packet)
		{
			if (packet.InventoryId1 == this.inv.InventoryID)
			{
				ItemSlot slot = this.inv[packet.SlotId1];
				this.UpdateSlotStack(slot, this.ItemStackFromPacket(resolver, packet.ItemStack1));
				this.inv.DidModifyItemSlot(slot, null);
			}
			if (packet.InventoryId2 == this.inv.InventoryID)
			{
				ItemSlot slot2 = this.inv[packet.SlotId2];
				this.UpdateSlotStack(slot2, this.ItemStackFromPacket(resolver, packet.ItemStack2));
				this.inv.DidModifyItemSlot(slot2, null);
			}
		}

		protected ItemStack ItemStackFromPacket(IWorldAccessor resolver, Packet_ItemStack pItemStack)
		{
			if (pItemStack == null || ((pItemStack.ItemClass == -1) | (pItemStack.ItemId == 0)))
			{
				return null;
			}
			return StackConverter.FromPacket(pItemStack, resolver);
		}

		private bool UpdateSlotStack(ItemSlot slot, ItemStack newStack)
		{
			if (slot.Itemstack != null && newStack != null && slot.Itemstack.Collectible == newStack.Collectible)
			{
				ItemStack itemstack = slot.Itemstack;
				newStack.TempAttributes = ((itemstack != null) ? itemstack.TempAttributes : null);
			}
			bool flag = newStack == null != (slot.Itemstack == null) || (newStack != null && !newStack.Equals(this.Api.World, slot.Itemstack, GlobalConstants.IgnoredStackAttributes));
			slot.Itemstack = newStack;
			return flag;
		}

		public object GetActivateSlotPacket(int slotId, ItemStackMoveOperation op)
		{
			Packet_ActivateInventorySlot activateSlotPacket = new Packet_ActivateInventorySlot
			{
				MouseButton = (int)op.MouseButton,
				TargetInventoryId = this.inv.InventoryID,
				TargetSlot = slotId,
				TargetLastChanged = this.inv.lastChangedSinceServerStart,
				Modifiers = (int)op.Modifiers,
				Priority = (int)op.CurrentPriority,
				Dir = op.WheelDir
			};
			if (this.inv is ITabbedInventory)
			{
				activateSlotPacket.TabIndex = ((ITabbedInventory)this.inv).CurrentTab.Index;
			}
			return new Packet_Client
			{
				Id = 7,
				ActivateInventorySlot = activateSlotPacket
			};
		}

		public object GetFlipSlotsPacket(IInventory sourceInv, int sourceSlotId, int targetSlotId)
		{
			Packet_Client p = new Packet_Client
			{
				Id = 9,
				Flipitemstacks = new Packet_FlipItemstacks
				{
					SourceInventoryId = sourceInv.InventoryID,
					SourceLastChanged = ((InventoryBase)sourceInv).lastChangedSinceServerStart,
					SourceSlot = sourceSlotId,
					TargetInventoryId = this.inv.InventoryID,
					TargetLastChanged = this.inv.lastChangedSinceServerStart,
					TargetSlot = targetSlotId
				}
			};
			if (sourceInv is ITabbedInventory)
			{
				p.Flipitemstacks.SourceTabIndex = (sourceInv as ITabbedInventory).CurrentTab.Index;
			}
			if (sourceInv is CreativeInventoryTab)
			{
				p.Flipitemstacks.SourceTabIndex = (sourceInv as CreativeInventoryTab).TabIndex;
			}
			if (this.inv is ITabbedInventory)
			{
				p.Flipitemstacks.TargetTabIndex = (this.inv as ITabbedInventory).CurrentTab.Index;
			}
			if (this.inv is CreativeInventoryTab)
			{
				p.Flipitemstacks.TargetTabIndex = (this.inv as CreativeInventoryTab).TabIndex;
			}
			return p;
		}

		protected InventoryBase inv;

		private bool pauseInvUpdates;

		private Queue<Packet_InventoryUpdate> pkts = new Queue<Packet_InventoryUpdate>();
	}
}
