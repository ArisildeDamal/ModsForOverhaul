using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace Vintagestory.Server
{
	internal class ServerSystemInventory : ServerSystem
	{
		public ServerSystemInventory(ServerMain server)
			: base(server)
		{
			server.RegisterGameTickListener(new Action<float>(this.SendDirtySlots), 30, 0);
			server.RegisterGameTickListener(new Action<float>(this.OnUsingTick), 20, 0);
			server.RegisterGameTickListener(new Action<float>(this.UpdateTransitionStates), 4000, 0);
			server.PacketHandlers[7] = new ClientPacketHandler<Packet_Client, ConnectedClient>(this.HandleActivateInventorySlot);
			server.PacketHandlers[10] = new ClientPacketHandler<Packet_Client, ConnectedClient>(this.HandleCreateItemstack);
			server.PacketHandlers[8] = new ClientPacketHandler<Packet_Client, ConnectedClient>(this.HandleMoveItemstack);
			server.PacketHandlers[9] = new ClientPacketHandler<Packet_Client, ConnectedClient>(this.HandleFlipItemStacks);
			server.PacketHandlers[25] = new ClientPacketHandler<Packet_Client, ConnectedClient>(this.HandleHandInteraction);
			server.PacketHandlers[27] = new ClientPacketHandler<Packet_Client, ConnectedClient>(this.HandleToolMode);
			server.PacketHandlers[30] = new ClientPacketHandler<Packet_Client, ConnectedClient>(this.HandleInvOpenClose);
		}

		public override void OnPlayerDisconnect(ServerPlayer player)
		{
			base.OnPlayerDisconnect(player);
			ServerPlayerInventoryManager serverPlayerInventoryManager = player.InventoryManager as ServerPlayerInventoryManager;
			if (serverPlayerInventoryManager == null)
			{
				return;
			}
			serverPlayerInventoryManager.OnPlayerDisconnect();
		}

		private void HandleInvOpenClose(Packet_Client packet, ConnectedClient client)
		{
			ServerPlayer player = client.Player;
			string invId = packet.InvOpenedClosed.InventoryId;
			InventoryBase inv;
			if (player.InventoryManager.GetInventory(invId, out inv))
			{
				if (packet.InvOpenedClosed.Opened > 0)
				{
					player.InventoryManager.OpenInventory(inv);
					return;
				}
				player.InventoryManager.CloseInventory(inv);
			}
		}

		private void HandleToolMode(Packet_Client packet, ConnectedClient client)
		{
			ServerPlayer player = client.Player;
			ItemSlot slot = player.InventoryManager.ActiveHotbarSlot;
			if (slot.Itemstack == null)
			{
				return;
			}
			Packet_ToolMode pt = packet.ToolMode;
			BlockSelection sele = new BlockSelection
			{
				Position = new BlockPos(pt.X, pt.Y, pt.Z),
				Face = BlockFacing.ALLFACES[pt.Face],
				HitPosition = new Vec3d(CollectibleNet.DeserializeDouble(pt.HitX), CollectibleNet.DeserializeDouble(pt.HitY), CollectibleNet.DeserializeDouble(pt.HitZ)),
				SelectionBoxIndex = pt.SelectionBoxIndex
			};
			slot.Itemstack.Collectible.SetToolMode(slot, player, sele, packet.ToolMode.Mode);
		}

		private void OnUsingTick(float dt)
		{
			foreach (ServerPlayer player in this.server.PlayersByUid.Values)
			{
				if (player.ConnectionState == EnumClientState.Playing)
				{
					ItemSlot slot = player.inventoryMgr.ActiveHotbarSlot;
					if (!player.Entity.Controls.LeftMouseDown || player.WorldData.CurrentGameMode != EnumGameMode.Survival)
					{
						goto IL_00B2;
					}
					BlockSelection currentBlockSelection = player.CurrentBlockSelection;
					if (!(((currentBlockSelection != null) ? currentBlockSelection.Position : null) != null) || slot.Itemstack == null)
					{
						goto IL_00B2;
					}
					slot.Itemstack.Collectible.OnBlockBreaking(player, player.CurrentBlockSelection, slot, 99f, dt, player.blockBreakingCounter);
					player.blockBreakingCounter++;
					IL_00B9:
					if (!player.Entity.LeftHandItemSlot.Empty)
					{
						player.Entity.LeftHandItemSlot.Itemstack.Collectible.OnHeldIdle(player.Entity.LeftHandItemSlot, player.Entity);
					}
					if ((player.Entity.Controls.HandUse == EnumHandInteract.None || player.Entity.Controls.HandUse == EnumHandInteract.BlockInteract) && slot != null)
					{
						if (slot.Itemstack != null)
						{
							slot.Itemstack.Collectible.OnHeldIdle(slot, player.Entity);
							continue;
						}
						continue;
					}
					else
					{
						if (slot == null || slot.Itemstack == null)
						{
							player.Entity.Controls.HandUse = EnumHandInteract.None;
							continue;
						}
						float secondsPassed = (float)(this.server.ElapsedMilliseconds - player.Entity.Controls.UsingBeginMS) / 1000f;
						int stackSize = slot.StackSize;
						this.callOnUsing(slot, player, player.CurrentUsingBlockSelection ?? player.CurrentBlockSelection, player.CurrentUsingEntitySelection ?? player.CurrentEntitySelection, ref secondsPassed, true);
						if (slot.StackSize <= 0)
						{
							slot.Itemstack = null;
						}
						if (stackSize != slot.StackSize)
						{
							slot.MarkDirty();
							continue;
						}
						continue;
					}
					IL_00B2:
					player.blockBreakingCounter = 0;
					goto IL_00B9;
				}
			}
		}

		private void UpdateTransitionStates(float dt)
		{
			foreach (ConnectedClient client in this.server.Clients.Values)
			{
				if (client.IsPlayingClient)
				{
					foreach (InventoryBase inv in client.Player.inventoryMgr.Inventories.Values)
					{
						if (inv is InventoryBasePlayer && !(inv is InventoryPlayerCreative))
						{
							foreach (ItemSlot slot in inv)
							{
								ItemStack itemstack = slot.Itemstack;
								if (itemstack != null)
								{
									CollectibleObject collectible = itemstack.Collectible;
									if (collectible != null)
									{
										collectible.UpdateAndGetTransitionStates(this.server, slot);
									}
								}
							}
						}
					}
				}
			}
		}

		private void SendDirtySlots(float dt)
		{
			foreach (ConnectedClient client in this.server.Clients.Values)
			{
				if (client.IsPlayingClient)
				{
					foreach (InventoryBase inv in client.Player.inventoryMgr.Inventories.Values)
					{
						if (inv.IsDirty)
						{
							if (inv is InventoryCharacter)
							{
								client.Player.BroadcastPlayerData(false);
							}
							foreach (int slotId in inv.dirtySlots)
							{
								Packet_Server slotUpdate = (inv.InvNetworkUtil as InventoryNetworkUtil).getSlotUpdatePacket(client.Player, slotId);
								if (slotUpdate != null)
								{
									this.server.SendPacket(client.Id, slotUpdate);
									ItemSlot slot = inv[slotId];
									if (slot != null && slot == client.Player.inventoryMgr.ActiveHotbarSlot)
									{
										client.Player.inventoryMgr.BroadcastHotbarSlot();
									}
								}
							}
							this.dirtySlots2Clear.Add(inv);
						}
					}
				}
			}
			foreach (InventoryBase inventoryBase in this.dirtySlots2Clear)
			{
				inventoryBase.dirtySlots.Clear();
			}
			this.dirtySlots2Clear.Clear();
		}

		public override void OnPlayerJoin(ServerPlayer player)
		{
			foreach (InventoryBase inventoryBase in player.inventoryMgr.Inventories.Values)
			{
				inventoryBase.AfterBlocksLoaded(this.server);
			}
			for (int i = 0; i < PlayerInventoryManager.defaultInventories.Length; i++)
			{
				string key = PlayerInventoryManager.defaultInventories[i] + "-" + player.WorldData.PlayerUID;
				if (!player.InventoryManager.Inventories.ContainsKey(key))
				{
					this.CreateNewInventory(player, PlayerInventoryManager.defaultInventories[i]);
				}
				if (player.WorldData.CurrentGameMode == EnumGameMode.Creative || PlayerInventoryManager.defaultInventories[i] != "creative")
				{
					player.inventoryMgr.Inventories[key].Open(player);
				}
			}
			this.OnPlayerSwitchGameMode(player);
		}

		private string CreateNewInventory(ServerPlayer player, string inventoryClassName)
		{
			string invId = inventoryClassName + "-" + player.PlayerUID;
			InventoryBasePlayer inv = (InventoryBasePlayer)ServerMain.ClassRegistry.CreateInventory(inventoryClassName, invId, this.server.api);
			player.SetInventory(inv);
			inv.AfterBlocksLoaded(this.server);
			return invId;
		}

		public override void OnPlayerSwitchGameMode(ServerPlayer player)
		{
			IInventory creativeInv = player.InventoryManager.GetOwnInventory("creative");
			IInventory backPackCraftingInv = player.InventoryManager.GetOwnInventory("craftinggrid");
			if (player.WorldData.CurrentGameMode == EnumGameMode.Creative)
			{
				if (creativeInv != null)
				{
					creativeInv.Open(player);
				}
				if (backPackCraftingInv != null)
				{
					backPackCraftingInv.Close(player);
				}
			}
			if (player.WorldData.CurrentGameMode == EnumGameMode.Guest || player.WorldData.CurrentGameMode == EnumGameMode.Survival)
			{
				if (creativeInv != null)
				{
					creativeInv.Close(player);
				}
				if (backPackCraftingInv != null)
				{
					backPackCraftingInv.Open(player);
				}
			}
		}

		private void HandleHandInteraction(Packet_Client packet, ConnectedClient client)
		{
			ServerPlayer player = client.Player;
			Packet_ClientHandInteraction p = packet.HandInteraction;
			if (p.EnumHandInteract >= 4)
			{
				this.server.OnHandleBlockInteract(packet, client);
				return;
			}
			string invId = p.InventoryId;
			ItemSlot slot = null;
			if (invId == null)
			{
				if (p.SlotId >= 10)
				{
					invId = "backpack-" + player.PlayerUID;
					InventoryBase invFound;
					if (player.InventoryManager.GetInventory(invId, out invFound))
					{
						slot = invFound[p.SlotId - 10];
					}
				}
				else
				{
					slot = player.inventoryMgr.GetHotbarInventory()[p.SlotId];
				}
			}
			else
			{
				slot = player.InventoryManager.Inventories[invId][p.SlotId];
			}
			if (slot != null)
			{
				if (slot.Itemstack == null)
				{
					return;
				}
				BlockSelection blockSel = null;
				EntitySelection entitySel = null;
				EnumHandInteract useType = (EnumHandInteract)p.UseType;
				if (useType == EnumHandInteract.None)
				{
					return;
				}
				if (p.MouseButton == 2)
				{
					BlockPos pos = new BlockPos(p.X, p.Y, p.Z);
					BlockFacing facing = BlockFacing.ALLFACES[p.OnBlockFace];
					Vec3d hitPos = new Vec3d(CollectibleNet.DeserializeDoublePrecise(p.HitX), CollectibleNet.DeserializeDoublePrecise(p.HitY), CollectibleNet.DeserializeDoublePrecise(p.HitZ));
					if (p.X != 0 || p.Y != 0 || p.Z != 0)
					{
						blockSel = new BlockSelection
						{
							Position = pos,
							Face = facing,
							HitPosition = hitPos,
							SelectionBoxIndex = p.SelectionBoxIndex
						};
					}
					if (p.OnEntityId != 0L)
					{
						Entity entity;
						this.server.LoadedEntities.TryGetValue(p.OnEntityId, out entity);
						if (entity == null)
						{
							return;
						}
						entitySel = new EntitySelection
						{
							Face = facing,
							HitPosition = hitPos,
							Entity = entity,
							Position = entity.ServerPos.XYZ
						};
					}
					player.CurrentUsingBlockSelection = blockSel;
					player.CurrentUsingEntitySelection = entitySel;
					EntityControls controls = player.Entity.Controls;
					float secondsPassed = (float)(this.server.ElapsedMilliseconds - controls.UsingBeginMS) / 1000f;
					switch (p.EnumHandInteract)
					{
					case 0:
					{
						EnumHandHandling handling = EnumHandHandling.NotHandled;
						slot.Itemstack.Collectible.OnHeldUseStart(slot, player.Entity, blockSel, entitySel, useType, p.FirstEvent > 0, ref handling);
						controls.HandUse = ((handling == EnumHandHandling.NotHandled) ? EnumHandInteract.None : useType);
						controls.UsingBeginMS = this.server.ElapsedMilliseconds;
						controls.UsingCount = 0;
						break;
					}
					case 1:
					{
						int i = 0;
						while (controls.HandUse != EnumHandInteract.None && controls.UsingCount < p.UsingCount && i++ < 5000)
						{
							this.callOnUsing(slot, player, blockSel, entitySel, ref secondsPassed, false);
						}
						if (i >= 5000)
						{
							LoggerBase logger = ServerMain.Logger;
							string text = "CancelHeldItemUse packet: Excess (5000+) UseStep calls from {2} on item {0}, would require {1} more steps to complete. Will abort.";
							object[] array = new object[3];
							int num = 0;
							ItemStack itemstack = slot.Itemstack;
							array[num] = ((itemstack != null) ? itemstack.GetName() : null);
							array[1] = p.UsingCount - controls.UsingCount;
							array[2] = player.PlayerName;
							logger.Warning(text, array);
						}
						EnumItemUseCancelReason cancelReason = (EnumItemUseCancelReason)p.CancelReason;
						if (slot.Itemstack == null)
						{
							controls.HandUse = EnumHandInteract.None;
						}
						else
						{
							controls.HandUse = slot.Itemstack.Collectible.OnHeldUseCancel(secondsPassed, slot, player.Entity, blockSel, entitySel, cancelReason);
						}
						break;
					}
					case 2:
						if (controls.HandUse != EnumHandInteract.None)
						{
							int j = 0;
							while (controls.HandUse != EnumHandInteract.None && controls.UsingCount < p.UsingCount && j++ < 5000)
							{
								this.callOnUsing(slot, player, blockSel, entitySel, ref secondsPassed, true);
							}
							if (j >= 5000)
							{
								LoggerBase logger2 = ServerMain.Logger;
								string text2 = "StopHeldItemUse packet: Excess (5000+) UseStep calls from {2} on item {0}, would require {1} more steps to complete. Will abort.";
								object[] array2 = new object[3];
								int num2 = 0;
								ItemStack itemstack2 = slot.Itemstack;
								array2[num2] = ((itemstack2 != null) ? itemstack2.GetName() : null);
								array2[1] = p.UsingCount - controls.UsingCount;
								array2[2] = player.PlayerName;
								logger2.Warning(text2, array2);
							}
							controls.HandUse = EnumHandInteract.None;
							ItemStack itemstack3 = slot.Itemstack;
							if (itemstack3 != null)
							{
								itemstack3.Collectible.OnHeldUseStop(secondsPassed, slot, player.Entity, blockSel, entitySel, useType);
							}
						}
						break;
					}
					if (slot.StackSize <= 0)
					{
						slot.Itemstack = null;
					}
				}
			}
		}

		private void HandleFlipItemStacks(Packet_Client packet, ConnectedClient client)
		{
			ServerPlayer player = client.Player;
			string invId = packet.Flipitemstacks.SourceInventoryId;
			InventoryBase invFound;
			if (player.InventoryManager.GetInventory(invId, out invFound))
			{
				(invFound.InvNetworkUtil as InventoryNetworkUtil).HandleClientPacket(player, packet.Id, packet);
			}
			if (player.inventoryMgr.IsVisibleHandSlot(invId, packet.Flipitemstacks.TargetSlot))
			{
				this.server.BroadcastHotbarSlot(player, true);
			}
		}

		private void HandleMoveItemstack(Packet_Client packet, ConnectedClient client)
		{
			ServerPlayer player = client.Player;
			string sinvId = packet.MoveItemstack.SourceInventoryId;
			string tinvId = packet.MoveItemstack.TargetInventoryId;
			InventoryBase inv;
			if (player.InventoryManager.GetInventory(sinvId, out inv))
			{
				(inv.InvNetworkUtil as InventoryNetworkUtil).HandleClientPacket(player, packet.Id, packet);
				if (player.inventoryMgr.IsVisibleHandSlot(sinvId, packet.MoveItemstack.SourceSlot))
				{
					this.server.BroadcastHotbarSlot(player, true);
				}
			}
			if (player.inventoryMgr.IsVisibleHandSlot(tinvId, packet.MoveItemstack.TargetSlot))
			{
				this.server.BroadcastHotbarSlot(player, true);
			}
		}

		private void HandleCreateItemstack(Packet_Client packet, ConnectedClient client)
		{
			ServerPlayer player = client.Player;
			Packet_CreateItemstack createpacket = packet.CreateItemstack;
			InventoryBase inv;
			player.InventoryManager.GetInventory(createpacket.TargetInventoryId, out inv);
			ItemSlot slot = ((inv != null) ? inv[createpacket.TargetSlot] : null);
			if (player.WorldData.CurrentGameMode == EnumGameMode.Creative && slot != null)
			{
				ItemStack itemstack = StackConverter.FromPacket(createpacket.Itemstack, this.server);
				slot.Itemstack = itemstack;
				slot.MarkDirty();
				ServerMain.Logger.Audit("{0} creative mode created item stack {1}x{2}", new object[]
				{
					player.PlayerName,
					itemstack.StackSize,
					itemstack.GetName()
				});
				return;
			}
			Packet_Server revertPacket = (((InventoryBase)player.InventoryManager.Inventories[createpacket.TargetInventoryId]).InvNetworkUtil as InventoryNetworkUtil).getSlotUpdatePacket(player, createpacket.TargetSlot);
			if (revertPacket != null)
			{
				this.server.SendPacket(player.ClientId, revertPacket);
			}
		}

		private void HandleActivateInventorySlot(Packet_Client packet, ConnectedClient client)
		{
			ServerPlayer player = client.Player;
			string invId = packet.ActivateInventorySlot.TargetInventoryId;
			InventoryBase invFound;
			if (player.InventoryManager.GetInventory(invId, out invFound))
			{
				(invFound.InvNetworkUtil as InventoryNetworkUtil).HandleClientPacket(player, packet.Id, packet);
			}
			else
			{
				ServerMain.Logger.Warning("Got activate inventory slot packet on inventory " + invId + " but no such inventory currently opened?");
			}
			if (player.inventoryMgr.IsVisibleHandSlot(invId, packet.ActivateInventorySlot.TargetSlot))
			{
				this.server.BroadcastHotbarSlot(player, true);
			}
		}

		private void callOnUsing(ItemSlot slot, ServerPlayer player, BlockSelection blockSel, EntitySelection entitySel, ref float secondsPassed, bool callStop = true)
		{
			EntityControls controls = player.Entity.Controls;
			EnumHandInteract useType = controls.HandUse;
			if (!slot.Empty)
			{
				controls.UsingCount++;
				controls.HandUse = slot.Itemstack.Collectible.OnHeldUseStep(secondsPassed, slot, player.Entity, blockSel, entitySel);
				if (callStop && controls.HandUse == EnumHandInteract.None)
				{
					ItemStack itemstack = slot.Itemstack;
					if (itemstack != null)
					{
						itemstack.Collectible.OnHeldUseStop(secondsPassed, slot, player.Entity, blockSel, entitySel, useType);
					}
				}
			}
			secondsPassed += 0.02f;
		}

		private List<InventoryBase> dirtySlots2Clear = new List<InventoryBase>();
	}
}
