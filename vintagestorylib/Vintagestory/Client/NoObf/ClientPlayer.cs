using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf
{
	public class ClientPlayer : IClientPlayer, IPlayer
	{
		public ClientPlayer(ClientMain game)
		{
			this.worlddata = ClientWorldPlayerData.CreateNew();
			this.inventoryMgr = new ClientPlayerInventoryManager(new OrderedDictionary<string, InventoryBase>(), this, game);
			this.game = game;
		}

		public IPlayerRole Role
		{
			get
			{
				return this.game.WorldMap.GetRole(this.RoleCode);
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		public string PlayerUID
		{
			get
			{
				ClientWorldPlayerData clientWorldPlayerData = this.worlddata;
				if (clientWorldPlayerData == null)
				{
					return null;
				}
				return clientWorldPlayerData.PlayerUID;
			}
		}

		public int ClientId
		{
			get
			{
				return this.worlddata.ClientId;
			}
		}

		public EntityPlayer Entity
		{
			get
			{
				return this.worlddata.EntityPlayer;
			}
		}

		public IPlayerInventoryManager InventoryManager
		{
			get
			{
				return this.inventoryMgr;
			}
		}

		public string PlayerName
		{
			get
			{
				EntityPlayer entity = this.Entity;
				string name = ((entity != null) ? entity.GetName() : null);
				if (name != null)
				{
					return name;
				}
				return this.worlddata.PlayerName;
			}
		}

		BlockSelection IPlayer.CurrentBlockSelection
		{
			get
			{
				return this.game.BlockSelection;
			}
		}

		EntitySelection IPlayer.CurrentEntitySelection
		{
			get
			{
				return this.game.EntitySelection;
			}
		}

		public IWorldPlayerData WorldData
		{
			get
			{
				return this.worlddata;
			}
		}

		public BlockPos SpawnPosition { get; set; }

		public float CameraYaw
		{
			get
			{
				return this.game.mouseYaw;
			}
			set
			{
				this.game.mouseYaw = value;
			}
		}

		public float CameraPitch
		{
			get
			{
				return this.game.mousePitch;
			}
			set
			{
				this.game.mousePitch = value;
			}
		}

		public float CameraRoll
		{
			get
			{
				return (float)this.game.MainCamera.Roll;
			}
			set
			{
				this.game.MainCamera.Roll = (double)value;
			}
		}

		public EnumCameraMode CameraMode
		{
			get
			{
				EnumCameraMode? overrideCameraMode = this.OverrideCameraMode;
				if (overrideCameraMode == null)
				{
					return this.game.MainCamera.CameraMode;
				}
				return overrideCameraMode.GetValueOrDefault();
			}
		}

		string[] IPlayer.Privileges
		{
			get
			{
				return this.Privileges;
			}
		}

		public List<Entitlement> Entitlements { get; set; } = new List<Entitlement>();

		public bool ImmersiveFpMode
		{
			get
			{
				if (!ClientSettings.ImmersiveFpMode)
				{
					EntityPlayer entity = this.Entity;
					return entity != null && !entity.Alive;
				}
				return true;
			}
		}

		private void AddOrUpdateInventory(ClientMain game, Packet_InventoryContents packet)
		{
			string invId = packet.InventoryId;
			ScreenManager.Platform.Logger.VerboseDebug("Received inventory contents " + invId);
			if (packet.InventoryClass == null)
			{
				throw new Exception("Illegal inventory contents packet, classname is null! " + packet.InventoryId);
			}
			if (!this.inventoryMgr.Inventories.ContainsKey(invId))
			{
				this.inventoryMgr.Inventories[invId] = (InventoryBasePlayer)ClientMain.ClassRegistry.CreateInventory(packet.InventoryClass, packet.InventoryId, game.api);
			}
			(this.inventoryMgr.Inventories[invId].InvNetworkUtil as InventoryNetworkUtil).UpdateFromPacket(game, packet);
		}

		public void UpdateFromPacket(ClientMain game, Packet_PlayerData packet)
		{
			if (packet.Entitlements != null)
			{
				this.Entitlements.Clear();
				foreach (string entitlement in packet.Entitlements.Split(',', StringSplitOptions.None))
				{
					this.Entitlements.Add(new Entitlement
					{
						Code = entitlement,
						Name = Lang.Get("entitlement-" + entitlement, Array.Empty<object>())
					});
				}
			}
			this.InventoryManager.ActiveHotbarSlotNumber = packet.HotbarSlotId;
			this.worlddata.UpdateFromPacket(game, packet);
			for (int i = 0; i < packet.InventoryContentsCount; i++)
			{
				this.AddOrUpdateInventory(game, packet.InventoryContents[i]);
			}
			this.SpawnPosition = new BlockPos(packet.Spawnx, packet.Spawny, packet.Spawnz);
		}

		public void UpdateFromPacket(ClientMain game, Packet_PlayerMode mode)
		{
			this.worlddata.UpdateFromPacket(game, mode);
		}

		public void ShowChatNotification(string message)
		{
			this.game.ShowChatMessage(message);
		}

		public void TriggerFpAnimation(EnumHandInteract anim)
		{
			if (anim == EnumHandInteract.HeldItemInteract)
			{
				this.game.HandSetAttackBuild = true;
			}
			if (anim == EnumHandInteract.HeldItemAttack)
			{
				this.game.HandSetAttackDestroy = true;
			}
		}

		public PlayerGroupMembership[] GetGroups()
		{
			if (this.game.player.PlayerUID != this.PlayerUID)
			{
				throw new NotImplementedException("On the client side you can only query the current players group, not those of other players");
			}
			return this.game.OwnPlayerGroupMemembershipsById.Values.ToArray<PlayerGroupMembership>();
		}

		public PlayerGroupMembership GetGroup(int groupId)
		{
			if (this.game.player.PlayerUID != this.PlayerUID)
			{
				throw new NotImplementedException("On the client side you can only query the current players group, not those of other players");
			}
			PlayerGroupMembership mems;
			this.game.OwnPlayerGroupMemembershipsById.TryGetValue(groupId, out mems);
			return mems;
		}

		public PlayerGroupMembership[] Groups
		{
			get
			{
				return this.game.OwnPlayerGroupMemembershipsById.Values.ToArray<PlayerGroupMembership>();
			}
		}

		public bool HasPrivilege(string privilegeCode)
		{
			return this.Privileges.Contains(privilegeCode);
		}

		internal void WarnIfEntityChanged(long newId, string packetName)
		{
			if (this.Entity != null && this.Entity.EntityId != newId)
			{
				this.game.Logger.Warning("ClientPlayer entityId change detected in {0} packet for {1}", new object[] { packetName, this.PlayerName });
				long oldId = this.Entity.EntityId;
				Entity oldEntity;
				bool hasOld = this.game.LoadedEntities.TryGetValue(oldId, out oldEntity);
				this.game.Logger.Warning("Old entityID {0} loaded: {1}.  New entityID {2} loaded: {3}.", new object[]
				{
					oldId,
					hasOld,
					newId,
					this.game.LoadedEntities.ContainsKey(newId)
				});
				if (hasOld && oldEntity != null)
				{
					this.game.Logger.Warning("Attempting to despawn the old entityID");
					try
					{
						EntityDespawnData despawnReason = new EntityDespawnData
						{
							Reason = EnumDespawnReason.Unload,
							DamageSourceForDeath = new DamageSource
							{
								Source = EnumDamageSource.Unknown
							}
						};
						oldEntity.OnEntityDespawn(despawnReason);
						this.game.RemoveEntityRenderer(oldEntity);
						oldEntity.Properties.Client.Renderer = null;
						ClientChunk clientChunk = this.game.WorldMap.GetClientChunk(oldEntity.InChunkIndex3d);
						if (clientChunk != null)
						{
							clientChunk.RemoveEntity(oldId);
						}
						this.game.LoadedEntities.Remove(oldId);
					}
					catch (Exception e)
					{
						this.game.Logger.Error(e);
					}
				}
			}
		}

		internal ClientWorldPlayerData worlddata;

		internal ClientPlayerInventoryManager inventoryMgr;

		private ClientMain game;

		public string[] Privileges;

		public string RoleCode;

		public float Ping;

		public EnumCameraMode? OverrideCameraMode;
	}
}
