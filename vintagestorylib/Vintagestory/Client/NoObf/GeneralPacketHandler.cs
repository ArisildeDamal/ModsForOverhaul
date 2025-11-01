using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf
{
	public class GeneralPacketHandler : ClientSystem
	{
		public GeneralPacketHandler(ClientMain game)
			: base(game)
		{
			game.PacketHandlers[2] = new ServerPacketHandler<Packet_Server>(this.HandlePing);
			game.PacketHandlers[3] = new ServerPacketHandler<Packet_Server>(this.HandlePlayerPing);
			game.PacketHandlers[58] = new ServerPacketHandler<Packet_Server>(this.HandleExchangeBlock);
			game.PacketHandlers[46] = new ServerPacketHandler<Packet_Server>(this.HandleModeChange);
			game.PacketHandlers[45] = new ServerPacketHandler<Packet_Server>(this.HandlePlayerDeath);
			game.PacketHandlers[8] = new ServerPacketHandler<Packet_Server>(this.HandleChatLine);
			game.PacketHandlers[9] = new ServerPacketHandler<Packet_Server>(this.HandleDisconnectPlayer);
			game.PacketHandlers[18] = new ServerPacketHandler<Packet_Server>(this.HandleSound);
			game.PacketHandlers[29] = new ServerPacketHandler<Packet_Server>(this.HandleServerRedirect);
			game.PacketHandlers[41] = new ServerPacketHandler<Packet_Server>(this.HandlePlayerData);
			game.PacketHandlers[30] = new ServerPacketHandler<Packet_Server>(this.HandleInventoryContents);
			game.PacketHandlers[31] = new ServerPacketHandler<Packet_Server>(this.HandleInventoryUpdate);
			game.PacketHandlers[32] = new ServerPacketHandler<Packet_Server>(this.HandleInventoryDoubleUpdate);
			game.PacketHandlers[66] = new ServerPacketHandler<Packet_Server>(this.HandleNotifyItemSlot);
			game.PacketHandlers[7] = new ServerPacketHandler<Packet_Server>(this.HandleSetBlock);
			game.PacketHandlers[48] = new ServerPacketHandler<Packet_Server>(this.HandleBlockEntities);
			game.PacketHandlers[44] = new ServerPacketHandler<Packet_Server>(this.HandleBlockEntityMessage);
			game.PacketHandlers[51] = new ServerPacketHandler<Packet_Server>(this.HandleSpawnPosition);
			game.PacketHandlers[53] = new ServerPacketHandler<Packet_Server>(this.HandleSelectedHotbarSlot);
			game.PacketHandlers[59] = new ServerPacketHandler<Packet_Server>(this.HandleStopMovement);
			game.PacketHandlers[61] = new ServerPacketHandler<Packet_Server>(this.HandleSpawnParticles);
			game.PacketHandlers[64] = new ServerPacketHandler<Packet_Server>(this.HandleBlockDamage);
			game.PacketHandlers[65] = new ServerPacketHandler<Packet_Server>(this.HandleAmbient);
			game.PacketHandlers[68] = new ServerPacketHandler<Packet_Server>(this.HandleIngameError);
			game.PacketHandlers[69] = new ServerPacketHandler<Packet_Server>(this.HandleIngameDiscovery);
			game.PacketHandlers[72] = new ServerPacketHandler<Packet_Server>(this.RemoveBlockLight);
			game.PacketHandlers[75] = new ServerPacketHandler<Packet_Server>(this.HandleLandClaims);
			game.PacketHandlers[76] = new ServerPacketHandler<Packet_Server>(this.HandleRoles);
		}

		public override string Name
		{
			get
			{
				return "gph";
			}
		}

		public override EnumClientSystemType GetSystemType()
		{
			return EnumClientSystemType.Misc;
		}

		private void HandlePing(Packet_Server packet)
		{
			this.game.SendPingReply();
			this.game.ServerInfo.ServerPing.OnSend(this.game.Platform.EllapsedMs);
		}

		private void HandlePlayerPing(Packet_Server packet)
		{
			this.game.ServerInfo.ServerPing.OnReceive(this.game.Platform.EllapsedMs);
			Packet_ServerPlayerPing p = packet.PlayerPing;
			Dictionary<int, float> pings = new Dictionary<int, float>();
			for (int i = 0; i < packet.PlayerPing.ClientIdsCount; i++)
			{
				int clientid = p.ClientIds[i];
				pings[clientid] = (float)p.Pings[i] / 1000f;
			}
			foreach (KeyValuePair<string, ClientPlayer> plr in this.game.PlayersByUid)
			{
				float val;
				if (pings.TryGetValue(plr.Value.ClientId, out val))
				{
					plr.Value.Ping = val;
				}
			}
		}

		private void HandleSetBlock(Packet_Server packet)
		{
			BlockPos pos = new BlockPos(packet.SetBlock.X, packet.SetBlock.Y, packet.SetBlock.Z);
			int blockId = packet.SetBlock.BlockType;
			if (blockId < 0)
			{
				int oldLiquidBlockId = this.game.WorldMap.RelaxedBlockAccess.GetBlock(pos, 2).BlockId;
				blockId = -(blockId + 1);
				if (blockId != oldLiquidBlockId)
				{
					this.game.WorldMap.RelaxedBlockAccess.SetBlock(blockId, pos, 2);
				}
				return;
			}
			int oldBlockId = this.game.WorldMap.RelaxedBlockAccess.GetBlockId(pos);
			if (blockId != oldBlockId)
			{
				this.game.WorldMap.RelaxedBlockAccess.SetBlock(blockId, pos);
				ClientEventManager eventManager = this.game.eventManager;
				if (eventManager == null)
				{
					return;
				}
				eventManager.TriggerBlockChanged(this.game, pos, this.game.WorldMap.Blocks[oldBlockId]);
			}
		}

		private void HandleExchangeBlock(Packet_Server packet)
		{
			BlockPos pos = new BlockPos(packet.ExchangeBlock.X, packet.ExchangeBlock.Y, packet.ExchangeBlock.Z);
			int oldBlockId = this.game.WorldMap.RelaxedBlockAccess.GetBlockId(pos);
			int blockId = packet.ExchangeBlock.BlockType;
			this.game.WorldMap.RelaxedBlockAccess.ExchangeBlock(blockId, pos);
			ClientEventManager eventManager = this.game.eventManager;
			if (eventManager == null)
			{
				return;
			}
			eventManager.TriggerBlockChanged(this.game, pos, this.game.WorldMap.Blocks[oldBlockId]);
		}

		private void HandleModeChange(Packet_Server packet)
		{
			ClientPlayer player;
			this.game.PlayersByUid.TryGetValue(packet.ModeChange.PlayerUID, out player);
			if (player != null)
			{
				player.UpdateFromPacket(this.game, packet.ModeChange);
			}
		}

		private void HandlePlayerDeath(Packet_Server packet)
		{
			if (this.game.EntityPlayer == null)
			{
				return;
			}
			this.game.EntityPlayer.TryStopHandAction(true, EnumItemUseCancelReason.Death);
			ClientEventManager eventManager = this.game.eventManager;
			if (eventManager == null)
			{
				return;
			}
			eventManager.TriggerPlayerDeath(packet.PlayerDeath.ClientId, packet.PlayerDeath.LivesLeft);
		}

		private void HandleChatLine(Packet_Server packet)
		{
			ClientEventManager eventManager = this.game.eventManager;
			if (eventManager != null)
			{
				eventManager.TriggerNewServerChatLine(packet.Chatline.Groupid, packet.Chatline.Message, (EnumChatType)packet.Chatline.ChatType, packet.Chatline.Data);
			}
			this.game.Logger.Chat("{0} @ {1}", new object[]
			{
				packet.Chatline.Message,
				packet.Chatline.Groupid
			});
		}

		private void HandleDisconnectPlayer(Packet_Server packet)
		{
			this.game.Logger.Notification("Disconnected by the server ({0})", new object[] { packet.DisconnectPlayer.DisconnectReason });
			string reason = packet.DisconnectPlayer.DisconnectReason;
			this.game.exitReason = "exit command by server";
			if ((reason != null && reason.Contains("Bad game session")) || reason == Lang.Get("Bad game session, try relogging", Array.Empty<object>()))
			{
				reason += "\n\nThis error can be caused when trying to connect to a server on version 1.18.3 or older. Please ask the server owner to update.";
			}
			this.game.disconnectReason = reason;
			this.game.DestroyGameSession(true);
		}

		private void HandleSound(Packet_Server packet)
		{
			this.game.PlaySoundAt(new AssetLocation(packet.Sound.Name), (double)CollectibleNet.DeserializeFloat(packet.Sound.X), (double)CollectibleNet.DeserializeFloat(packet.Sound.Y), (double)CollectibleNet.DeserializeFloat(packet.Sound.Z), null, (EnumSoundType)packet.Sound.SoundType, CollectibleNet.DeserializeFloatPrecise(packet.Sound.Pitch), CollectibleNet.DeserializeFloat(packet.Sound.Range), CollectibleNet.DeserializeFloatPrecise(packet.Sound.Volume));
		}

		private void HandleServerRedirect(Packet_Server packet)
		{
			this.game.Logger.Notification("Received server redirect");
			this.game.SendLeave(0);
			this.game.ExitAndSwitchServer(new MultiplayerServerEntry
			{
				host = packet.Redirect.Host,
				name = packet.Redirect.Name
			});
			this.game.Logger.VerboseDebug("Received server redirect packet");
		}

		private void HandlePlayerData(Packet_Server packet)
		{
			if (!this.game.BlocksReceivedAndLoaded)
			{
				this.game.Logger.VerboseDebug("Startup sequence wrong, playerdata packet handled before BlocksReceivedAndLoaded; player may be null");
				return;
			}
			string uid = packet.PlayerData.PlayerUID;
			if (packet.PlayerData.ClientId <= -99)
			{
				this.game.Logger.VerboseDebug("Received player data deletion for playeruid " + uid);
				ClientPlayer plr;
				if (this.game.PlayersByUid.TryGetValue(uid, out plr))
				{
					this.game.api.eventapi.TriggerPlayerEntityDespawn(plr);
					plr.worlddata.EntityPlayer = null;
					this.game.api.eventapi.TriggerPlayerLeave(plr);
					this.game.PlayersByUid.Remove(uid);
					this.game.PlayersByUid_Threadsafe.Remove(uid);
				}
				return;
			}
			this.game.Logger.VerboseDebug("Received player data for playeruid " + uid);
			ClientPlayer clientPlayer;
			bool isNew = !this.game.PlayersByUid.TryGetValue(uid, out clientPlayer);
			if (isNew)
			{
				clientPlayer = (this.game.PlayersByUid[uid] = new ClientPlayer(this.game));
				this.game.PlayersByUid_Threadsafe[uid] = clientPlayer;
			}
			else
			{
				clientPlayer.WarnIfEntityChanged(packet.PlayerData.EntityId, "playerData");
			}
			clientPlayer.UpdateFromPacket(this.game, packet.PlayerData);
			if (ClientSettings.PlayerUID == uid && !this.game.Spawned)
			{
				this.game.player = clientPlayer;
				this.game.mouseYaw = this.game.EntityPlayer.SidedPos.Yaw;
				this.game.mousePitch = this.game.EntityPlayer.SidedPos.Pitch;
				this.game.Logger.VerboseDebug("Informing clientsystems playerdata received");
				this.game.OnOwnPlayerDataReceived();
				this.game.Spawned = true;
				this.game.SendPacketClient(new Packet_Client
				{
					Id = 26
				});
			}
			if (packet.PlayerData.Privileges != null)
			{
				string[] privileges = packet.PlayerData.Privileges;
				int count = packet.PlayerData.PrivilegesCount;
				string[] array = (clientPlayer.Privileges = new string[count]);
				for (int i = 0; i < count; i++)
				{
					array[i] = privileges[i];
				}
			}
			if (packet.PlayerData.RoleCode != null)
			{
				clientPlayer.RoleCode = packet.PlayerData.RoleCode;
			}
			if (isNew)
			{
				this.game.api.eventapi.TriggerPlayerJoin(clientPlayer);
				if (clientPlayer.Entity != null)
				{
					this.game.api.eventapi.TriggerPlayerEntitySpawn(clientPlayer);
				}
			}
			if (ClientSettings.PlayerUID == uid)
			{
				ClientEventManager eventManager = this.game.eventManager;
				if (eventManager != null)
				{
					eventManager.TriggerPlayerModeChange();
				}
				if (this.game.player.worlddata.CurrentGameMode != EnumGameMode.Creative)
				{
					ClientSettings.RenderMetaBlocks = false;
				}
				if (this.game.player.worlddata.CurrentGameMode == EnumGameMode.Spectator)
				{
					this.game.MainCamera.SetMode(EnumCameraMode.FirstPerson);
				}
				if (!this.game.clientPlayingFired && this.game.api.eventapi.TriggerIsPlayerReady())
				{
					this.game.clientPlayingFired = true;
					this.game.SendPacketClient(new Packet_Client
					{
						Id = 29
					});
				}
			}
			this.game.Logger.VerboseDebug("Done handling playerdata packet");
		}

		private void HandleInventoryContents(Packet_Server packet)
		{
			string invId = packet.InventoryContents.InventoryId;
			this.game.Logger.VerboseDebug("Received inventory contents " + invId);
			ClientPlayer player = this.game.GetPlayerFromClientId(packet.InventoryContents.ClientId);
			if (player == null)
			{
				this.game.Logger.Error("Server sent me inventory contents for a player that i don't have? Ignoring. Clientid was " + packet.InventoryContents.ClientId.ToString());
				return;
			}
			if (!player.inventoryMgr.Inventories.ContainsKey(invId))
			{
				if (!ClientMain.ClassRegistry.inventoryClassToTypeMapping.ContainsKey(packet.InventoryContents.InventoryClass))
				{
					this.game.Logger.Error("Server sent me inventory contents from with an inventory class name '{0}' - no idea how to instantiate that. Ignoring.", new object[] { packet.InventoryContents.InventoryClass });
					return;
				}
				player.inventoryMgr.Inventories[invId] = ClientMain.ClassRegistry.CreateInventory(packet.InventoryContents.InventoryClass, packet.InventoryContents.InventoryId, this.game.api);
				player.inventoryMgr.Inventories[invId].AfterBlocksLoaded(this.game);
			}
			(player.inventoryMgr.Inventories[invId].InvNetworkUtil as InventoryNetworkUtil).UpdateFromPacket(this.game, packet.InventoryContents);
		}

		private void HandleInventoryUpdate(Packet_Server packet)
		{
			string invId = packet.InventoryUpdate.InventoryId;
			ClientPlayer player = this.game.GetPlayerFromClientId(packet.InventoryUpdate.ClientId);
			InventoryBase inv;
			if (player != null && player.inventoryMgr.Inventories.TryGetValue(invId, out inv))
			{
				if (inv.InventoryID != invId)
				{
					player.inventoryMgr.Inventories.TryGetValue(invId, out inv);
					if (inv.InventoryID != invId)
					{
						throw new Exception("Inventory manager inventories mismatched: key " + invId + " had value " + inv.InventoryID);
					}
				}
				(inv.InvNetworkUtil as InventoryNetworkUtil).UpdateFromPacket(this.game, packet.InventoryUpdate);
			}
		}

		private void HandleNotifyItemSlot(Packet_Server packet)
		{
			string invId = packet.NotifySlot.InventoryId;
			ClientPlayer player = this.game.player;
			bool flag;
			if (player == null)
			{
				flag = null != null;
			}
			else
			{
				ClientPlayerInventoryManager inventoryMgr = player.inventoryMgr;
				flag = ((inventoryMgr != null) ? inventoryMgr.Inventories : null) != null;
			}
			if (flag && this.game.player.inventoryMgr.Inventories.ContainsKey(invId))
			{
				InventoryBase inventoryBase = this.game.player.inventoryMgr.Inventories[invId];
				if (inventoryBase == null)
				{
					return;
				}
				inventoryBase.PerformNotifySlot(packet.NotifySlot.SlotId);
			}
		}

		private void HandleInventoryDoubleUpdate(Packet_Server packet)
		{
			if (((packet != null) ? packet.InventoryDoubleUpdate : null) == null)
			{
				this.game.Logger.Warning("Received inventory double update with packet set to null?");
				return;
			}
			string invId = packet.InventoryDoubleUpdate.InventoryId1;
			string invId2 = packet.InventoryDoubleUpdate.InventoryId2;
			ClientPlayer playerFromClientId = this.game.GetPlayerFromClientId(packet.InventoryDoubleUpdate.ClientId);
			ClientPlayerInventoryManager inventoryMgr = ((playerFromClientId != null) ? playerFromClientId.inventoryMgr : null);
			if (inventoryMgr == null)
			{
				ILogger logger = this.game.Logger;
				string text = "Received inventory double update for a client whose inventory i dont have? for clientid ";
				int? num;
				if (packet == null)
				{
					num = null;
				}
				else
				{
					Packet_InventoryContents inventoryContents = packet.InventoryContents;
					num = ((inventoryContents != null) ? new int?(inventoryContents.ClientId) : null);
				}
				int? num2 = num;
				logger.Warning(text + num2.ToString());
				return;
			}
			InventoryBase invFound;
			if (inventoryMgr.GetInventory(invId, out invFound))
			{
				InventoryNetworkUtil inventoryNetworkUtil = invFound.InvNetworkUtil as InventoryNetworkUtil;
				if (inventoryNetworkUtil != null)
				{
					inventoryNetworkUtil.UpdateFromPacket(this.game, packet.InventoryDoubleUpdate);
				}
			}
			InventoryBase invFound2;
			if (invId != invId2 && inventoryMgr.GetInventory(invId, out invFound2))
			{
				InventoryNetworkUtil inventoryNetworkUtil2 = invFound2.InvNetworkUtil as InventoryNetworkUtil;
				if (inventoryNetworkUtil2 == null)
				{
					return;
				}
				inventoryNetworkUtil2.UpdateFromPacket(this.game, packet.InventoryDoubleUpdate);
			}
		}

		private void HandleBlockEntities(Packet_Server packet)
		{
			Packet_BlockEntity[] blockentities = packet.BlockEntities.BlockEntitites;
			for (int i = 0; i < packet.BlockEntities.BlockEntititesCount; i++)
			{
				Packet_BlockEntity p = blockentities[i];
				ClientChunk chunk = this.game.WorldMap.GetChunkAtBlockPos(p.PosX, p.PosY, p.PosZ);
				if (chunk != null)
				{
					chunk.AddOrUpdateBlockEntityFromPacket(p, this.game);
					BlockPos pos = new BlockPos(p.PosX, p.PosY, p.PosZ);
					ClientEventManager eventManager = this.game.eventManager;
					if (eventManager != null)
					{
						eventManager.TriggerBlockChanged(this.game, pos, this.game.BlockAccessor.GetBlock(pos));
					}
				}
			}
		}

		private void HandleBlockEntityMessage(Packet_Server packet)
		{
			Packet_BlockEntityMessage p = packet.BlockEntityMessage;
			BlockEntity be = this.game.WorldMap.GetBlockEntity(new BlockPos(p.X, p.Y, p.Z));
			if (be != null)
			{
				be.OnReceivedServerPacket(p.PacketId, p.Data);
			}
		}

		private void HandleSpawnPosition(Packet_Server packet)
		{
			EntityPos spawnpos = ClientSystemEntities.entityPosFromPacket(packet.EntityPosition);
			this.game.SpawnPosition = spawnpos;
		}

		private void HandleSelectedHotbarSlot(Packet_Server packet)
		{
			int clientid = packet.SelectedHotbarSlot.ClientId;
			try
			{
				foreach (ClientPlayer player in this.game.PlayersByUid.Values)
				{
					if (player.ClientId == clientid)
					{
						Packet_SelectedHotbarSlot shpPacket = packet.SelectedHotbarSlot;
						player.inventoryMgr.SetActiveHotbarSlotNumberFromServer(shpPacket.SlotNumber);
						ItemStack stack = null;
						if (shpPacket.Itemstack != null && shpPacket.Itemstack.ItemClass != -1 && shpPacket.Itemstack.ItemId != 0)
						{
							stack = StackConverter.FromPacket(shpPacket.Itemstack, this.game);
						}
						ItemStack offstack = null;
						if (shpPacket.OffhandStack != null && shpPacket.OffhandStack.ItemClass != -1 && shpPacket.OffhandStack.ItemId != 0)
						{
							offstack = StackConverter.FromPacket(shpPacket.OffhandStack, this.game);
						}
						player.inventoryMgr.ActiveHotbarSlot.Itemstack = stack;
						EntityPlayer entity = player.Entity;
						if (((entity != null) ? entity.LeftHandItemSlot : null) != null)
						{
							player.Entity.LeftHandItemSlot.Itemstack = offstack;
							break;
						}
						break;
					}
				}
			}
			catch (Exception e)
			{
				string text = "Handling server packet HandleSelectedHotbarSlot threw an exception while trying to update the slot of clientid ";
				string text2 = clientid.ToString();
				string text3 = " with itemstack ";
				Packet_ItemStack itemstack = packet.SelectedHotbarSlot.Itemstack;
				string msg = text + text2 + text3 + ((itemstack != null) ? itemstack.ToString() : null);
				msg += "Exception thrown: ";
				this.game.Logger.Fatal(msg);
				this.game.Logger.Fatal(e);
			}
		}

		private void HandleStopMovement(Packet_Server packet)
		{
			EntityPlayer entityPlayer = this.game.EntityPlayer;
			if (((entityPlayer != null) ? entityPlayer.Controls : null) != null)
			{
				this.game.EntityPlayer.Controls.StopAllMovement();
			}
		}

		private void HandleSpawnParticles(Packet_Server packet)
		{
			Packet_SpawnParticles p = packet.SpawnParticles;
			IParticlePropertiesProvider propprovider = ClientMain.ClassRegistry.CreateParticlePropertyProvider(p.ParticlePropertyProviderClassName);
			using (MemoryStream ms = new MemoryStream(p.Data))
			{
				BinaryReader reader = new BinaryReader(ms);
				propprovider.FromBytes(reader, this.game);
			}
			this.game.SpawnParticles(propprovider, null);
		}

		private void HandleBlockDamage(Packet_Server packet)
		{
			BlockPos pos = new BlockPos(packet.BlockDamage.PosX, packet.BlockDamage.PosY, packet.BlockDamage.PosZ);
			this.game.WorldMap.DamageBlock(pos, BlockFacing.ALLFACES[packet.BlockDamage.Facing], CollectibleNet.DeserializeFloat(packet.BlockDamage.Damage), null);
		}

		private void HandleAmbient(Packet_Server packet)
		{
			using (MemoryStream ms = new MemoryStream(packet.Ambient.Data))
			{
				AmbientModifier s = new AmbientModifier().EnsurePopulated();
				s.FromBytes(new BinaryReader(ms));
				this.game.AmbientManager.CurrentModifiers["serverambient"] = s.EnsurePopulated();
			}
		}

		private void HandleIngameError(Packet_Server packet)
		{
			Packet_IngameError p = packet.IngameError;
			string message = p.Message;
			if (message == null)
			{
				if (p.LangParams == null)
				{
					message = Lang.Get("ingameerror-" + p.Code, Array.Empty<object>());
				}
				else
				{
					string text = "ingameerror-" + p.Code;
					object[] langParams = p.LangParams;
					message = Lang.Get(text, langParams);
				}
			}
			ClientEventManager eventManager = this.game.eventManager;
			if (eventManager == null)
			{
				return;
			}
			eventManager.TriggerIngameError(this, p.Code, message);
		}

		private void HandleIngameDiscovery(Packet_Server packet)
		{
			Packet_IngameDiscovery p = packet.IngameDiscovery;
			string message = p.Message;
			if (message == null)
			{
				string text = "ingamediscovery-" + p.Code;
				object[] langParams = p.LangParams;
				message = Lang.Get(text, langParams);
			}
			ClientEventManager eventManager = this.game.eventManager;
			if (eventManager == null)
			{
				return;
			}
			eventManager.TriggerIngameDiscovery(this, p.Code, message);
		}

		private void RemoveBlockLight(Packet_Server packet)
		{
			Packet_RemoveBlockLight pkt = packet.RemoveBlockLight;
			this.game.BlockAccessor.RemoveBlockLight(new byte[]
			{
				(byte)pkt.LightH,
				(byte)pkt.LightS,
				(byte)pkt.LightV
			}, new BlockPos(pkt.PosX, pkt.PosY, pkt.PosZ));
		}

		private void HandleLandClaims(Packet_Server packet)
		{
			Packet_LandClaims pkt = packet.LandClaims;
			if (pkt.Allclaims != null && pkt.Allclaims.Length != 0)
			{
				this.game.WorldMap.LandClaims = (from b in pkt.Allclaims
					where b != null
					select b into claim
					select SerializerUtil.Deserialize<LandClaim>(claim.Data)).ToList<LandClaim>();
			}
			else if (pkt.Addclaims != null)
			{
				this.game.WorldMap.LandClaims.AddRange(from b in pkt.Addclaims
					where b != null
					select b into claim
					select SerializerUtil.Deserialize<LandClaim>(claim.Data));
			}
			this.game.WorldMap.RebuildLandClaimPartitions();
		}

		private void HandleRoles(Packet_Server packet)
		{
			Packet_Roles pkt = packet.Roles;
			this.game.WorldMap.RolesByCode = new Dictionary<string, PlayerRole>();
			for (int i = 0; i < pkt.RolesCount; i++)
			{
				Packet_Role rolePkt = pkt.Roles[i];
				this.game.WorldMap.RolesByCode[rolePkt.Code] = new PlayerRole
				{
					Code = rolePkt.Code,
					PrivilegeLevel = rolePkt.PrivilegeLevel
				};
			}
		}
	}
}
