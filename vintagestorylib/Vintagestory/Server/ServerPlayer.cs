using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace Vintagestory.Server
{
	public class ServerPlayer : IServerPlayer, IPlayer
	{
		public event OnEntityAction InWorldAction;

		public List<Entitlement> Entitlements { get; set; } = new List<Entitlement>();

		public int ActiveSlot
		{
			get
			{
				return this.inventoryMgr.ActiveHotbarSlotNumber;
			}
			set
			{
				this.inventoryMgr.ActiveHotbarSlotNumber = value;
				this.worlddata.SelectedHotbarSlot = value;
			}
		}

		public virtual string PlayerUID
		{
			get
			{
				ServerWorldPlayerData serverWorldPlayerData = this.worlddata;
				if (serverWorldPlayerData == null)
				{
					return null;
				}
				return serverWorldPlayerData.PlayerUID;
			}
		}

		public bool ImmersiveFpMode { get; set; }

		public int ItemCollectMode { get; set; }

		public int ClientId
		{
			get
			{
				if (this.client != null)
				{
					return this.client.Id;
				}
				return 0;
			}
		}

		public virtual EnumClientState ConnectionState
		{
			get
			{
				if (this.client != null)
				{
					return this.client.State;
				}
				return EnumClientState.Offline;
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

		public string LanguageCode { get; set; }

		public string IpAddress
		{
			get
			{
				if (this.client == null)
				{
					return null;
				}
				return this.client.Socket.RemoteEndPoint().Address.ToString();
			}
		}

		public float Ping
		{
			get
			{
				if (this.client != null)
				{
					return this.client.LastPing;
				}
				return float.NaN;
			}
		}

		public string PlayerName
		{
			get
			{
				if (this.client != null)
				{
					return this.client.PlayerName;
				}
				return null;
			}
		}

		public FuzzyEntityPos GetSpawnPosition(bool consumeSpawnUse)
		{
			return this.server.GetSpawnPosition(this.worlddata.PlayerUID, false, consumeSpawnUse);
		}

		public IWorldPlayerData WorldData
		{
			get
			{
				return this.worlddata;
			}
		}

		public PlayerGroupMembership[] Groups
		{
			get
			{
				return this.serverdata.PlayerGroupMemberShips.Values.ToArray<PlayerGroupMembership>();
			}
		}

		public ServerPlayer(ServerMain server, ServerWorldPlayerData worlddata)
		{
			this.server = server;
			this.worlddata = worlddata;
			this.LanguageCode = Lang.CurrentLocale;
			this.Init();
		}

		protected virtual void Init()
		{
			this.inventoryMgr = new ServerPlayerInventoryManager(this.worlddata.inventories, this, this.server);
			this.inventoryMgr.ActiveHotbarSlotNumber = this.worlddata.SelectedHotbarSlot;
			if (this.inventoryMgr.ActiveHotbarSlot == null)
			{
				this.inventoryMgr.ActiveHotbarSlotNumber = 0;
			}
			this.serverdata = this.server.PlayerDataManager.GetOrCreateServerPlayerData(this.worlddata.PlayerUID, null);
		}

		public void SetInventory(InventoryBasePlayer inv)
		{
			this.inventoryMgr.Inventories[inv.InventoryID] = inv;
		}

		public virtual void BroadcastPlayerData(bool sendInventory = false)
		{
			this.server.BroadcastPlayerData(this, sendInventory, false);
		}

		public virtual void Disconnect()
		{
			this.server.DisconnectPlayer(this.client, null, null);
		}

		public virtual void Disconnect(string message)
		{
			this.server.DisconnectPlayer(this.client, message, null);
		}

		public virtual IPlayerRole Role
		{
			get
			{
				return this.serverdata.GetPlayerRole(this.server);
			}
			set
			{
				this.server.api.Permissions.SetRole(this, value);
			}
		}

		public IServerPlayerData ServerData
		{
			get
			{
				return this.serverdata;
			}
		}

		public string[] Privileges
		{
			get
			{
				return this.serverdata.GetAllPrivilegeCodes(this.server.Config).ToArray<string>();
			}
		}

		public int CurrentChunkSentRadius
		{
			get
			{
				ConnectedClient connectedClient = this.client;
				if (connectedClient == null)
				{
					return 0;
				}
				return connectedClient.CurrentChunkSentRadius;
			}
			set
			{
				if (this.client != null)
				{
					this.client.CurrentChunkSentRadius = value;
				}
			}
		}

		public BlockSelection CurrentBlockSelection
		{
			get
			{
				return this.Entity.BlockSelection;
			}
		}

		public EntitySelection CurrentEntitySelection
		{
			get
			{
				return this.Entity.EntitySelection;
			}
		}

		public BlockSelection CurrentUsingBlockSelection { get; set; }

		public EntitySelection CurrentUsingEntitySelection { get; set; }

		public long LastReceivedClientPosition { get; set; }

		public virtual bool HasPrivilege(string privilegeCode)
		{
			return this.serverdata.HasPrivilege(privilegeCode, this.server.Config.RolesByCode);
		}

		public void SendIngameError(string code, string message = null, params object[] langparams)
		{
			this.server.SendIngameError(this, code, message, langparams);
		}

		public void SendMessage(int groupId, string message, EnumChatType chatType, string data = null)
		{
			this.server.SendMessage(this, groupId, message, chatType, data);
		}

		public void SendLocalisedMessage(int groupId, string message, params object[] args)
		{
			this.server.SendMessage(this, groupId, Lang.GetL(this.LanguageCode, message, args), EnumChatType.Notification, null);
		}

		public void SetSpawnPosition(PlayerSpawnPos pos)
		{
			this.worlddata.SpawnPosition = pos;
			this.server.SendOwnPlayerData(this, false, true);
		}

		public void ClearSpawnPosition()
		{
			this.worlddata.SpawnPosition = null;
		}

		public PlayerGroupMembership[] GetGroups()
		{
			return this.serverdata.PlayerGroupMemberShips.Values.ToArray<PlayerGroupMembership>();
		}

		public PlayerGroupMembership GetGroup(int groupId)
		{
			PlayerGroupMembership mems;
			this.serverdata.PlayerGroupMemberShips.TryGetValue(groupId, out mems);
			return mems;
		}

		public void SetRole(string roleCode)
		{
			this.server.api.Permissions.SetRole(this, roleCode);
		}

		public void SetModdata(string key, byte[] data)
		{
			this.worlddata.SetModdata(key, data);
		}

		public void RemoveModdata(string key)
		{
			this.worlddata.RemoveModdata(key);
		}

		public byte[] GetModdata(string key)
		{
			return this.worlddata.GetModdata(key);
		}

		public void SetModData<T>(string key, T data)
		{
			this.SetModdata(key, SerializerUtil.Serialize<T>(data));
		}

		public T GetModData<T>(string key, T defaultValue = default(T))
		{
			byte[] data = this.GetModdata(key);
			if (data == null)
			{
				return defaultValue;
			}
			return SerializerUtil.Deserialize<T>(data);
		}

		public EnumHandling TriggerInWorldAction(EnumEntityAction action, bool on)
		{
			EnumHandling handling = EnumHandling.PassThrough;
			if (this.InWorldAction == null)
			{
				return handling;
			}
			Delegate[] invocationList = this.InWorldAction.GetInvocationList();
			for (int i = 0; i < invocationList.Length; i++)
			{
				((OnEntityAction)invocationList[i])(action, on, ref handling);
				if (handling == EnumHandling.PreventSubsequent)
				{
					return handling;
				}
			}
			return handling;
		}

		private ServerMain server;

		private ServerWorldPlayerData worlddata;

		public ServerPlayerData serverdata;

		internal ConnectedClient client;

		internal PlayerInventoryManager inventoryMgr;

		internal int blockBreakingCounter;
	}
}
