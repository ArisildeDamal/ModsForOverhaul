using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace Vintagestory.Server
{
	[ProtoContract]
	public class ServerWorldPlayerData : IWorldPlayerData
	{
		string IWorldPlayerData.PlayerUID
		{
			get
			{
				return this.PlayerUID;
			}
		}

		public EntityPlayer EntityPlayer
		{
			get
			{
				return this.Entityplayer;
			}
			set
			{
				this.Entityplayer = value;
			}
		}

		public PlayerSpawnPos SpawnPosition
		{
			get
			{
				return this.spawnPosition;
			}
			set
			{
				this.spawnPosition = value;
			}
		}

		public EntityControls EntityControls
		{
			get
			{
				return this.Entityplayer.Controls;
			}
		}

		public int LastApprovedViewDistance
		{
			get
			{
				return this.Viewdistance;
			}
			set
			{
				this.LastApprovedViewDistance = value;
			}
		}

		EnumGameMode IWorldPlayerData.CurrentGameMode
		{
			get
			{
				if (!this.connected)
				{
					return EnumGameMode.Spectator;
				}
				return this.GameMode;
			}
			set
			{
				this.GameMode = value;
				EntityPlayer entityPlayer = this.EntityPlayer;
				if (entityPlayer == null)
				{
					return;
				}
				entityPlayer.UpdatePartitioning();
			}
		}

		bool IWorldPlayerData.FreeMove
		{
			get
			{
				return this.FreeMove;
			}
			set
			{
				this.FreeMove = value;
			}
		}

		bool IWorldPlayerData.NoClip
		{
			get
			{
				return this.NoClip;
			}
			set
			{
				this.NoClip = value;
				EntityPlayer entityPlayer = this.EntityPlayer;
				if (entityPlayer == null)
				{
					return;
				}
				entityPlayer.UpdatePartitioning();
			}
		}

		public bool AreaSelectionMode
		{
			get
			{
				return this.areaSelectionMode;
			}
			set
			{
				this.areaSelectionMode = value;
			}
		}

		public EnumFreeMovAxisLock FreeMovePlaneLock
		{
			get
			{
				return this.freeMovePlaneLock;
			}
			set
			{
				this.freeMovePlaneLock = value;
			}
		}

		float IWorldPlayerData.MoveSpeedMultiplier
		{
			get
			{
				return this.MoveSpeedMultiplier;
			}
			set
			{
				this.MoveSpeedMultiplier = value;
			}
		}

		float IWorldPlayerData.PickingRange
		{
			get
			{
				return this.PickingRange;
			}
			set
			{
				this.PickingRange = value;
			}
		}

		public int CurrentClientId
		{
			get
			{
				return this.currentClientId;
			}
		}

		public bool Connected
		{
			get
			{
				return this.connected;
			}
		}

		public bool DidSelectSkin
		{
			get
			{
				return this.didSelectSkin;
			}
			set
			{
				this.didSelectSkin = value;
			}
		}

		public int SelectedHotbarSlot
		{
			get
			{
				return this.selectedHotbarslot;
			}
			set
			{
				this.selectedHotbarslot = value;
			}
		}

		public int DesiredViewDistance { get; set; }

		int IWorldPlayerData.Deaths
		{
			get
			{
				return this.Deaths;
			}
		}

		public static ServerWorldPlayerData CreateNew(string playername, string playerUID)
		{
			return new ServerWorldPlayerData
			{
				Entityplayer = new EntityPlayer(),
				GameMode = EnumGameMode.Survival,
				MoveSpeedMultiplier = 1f,
				PickingRange = GlobalConstants.DefaultPickingRange,
				PlayerUID = playerUID,
				ModData = new Dictionary<string, byte[]>()
			};
		}

		public void Init(ServerMain server)
		{
			if (this.inventoriesSerialized == null)
			{
				this.inventoriesSerialized = new Dictionary<string, byte[]>();
			}
			bool clearinv = server.ClearPlayerInvs.Remove(this.PlayerUID);
			foreach (KeyValuePair<string, byte[]> val in this.inventoriesSerialized)
			{
				string[] parts = val.Key.Split('-', StringSplitOptions.None);
				try
				{
					InventoryBase inv = ServerMain.ClassRegistry.CreateInventory(parts[0], val.Key, server.api);
					BinaryReader reader = new BinaryReader(new MemoryStream(val.Value));
					TreeAttribute tree = new TreeAttribute();
					tree.FromBytes(reader);
					inv.FromTreeAttributes(tree);
					this.inventories.Add(val.Key, inv);
					if (clearinv)
					{
						inv.Clear();
					}
				}
				catch (Exception e)
				{
					ServerMain.Logger.Error("Could not load a players inventory. Will ignore.");
					ServerMain.Logger.Error(e);
				}
			}
			if (this.EntityPlayerSerialized != null && this.EntityPlayerSerialized.Length != 0)
			{
				using (MemoryStream ms = new MemoryStream(this.EntityPlayerSerialized))
				{
					BinaryReader reader2 = new BinaryReader(ms);
					string className = reader2.ReadString();
					try
					{
						this.Entityplayer = (EntityPlayer)ServerMain.ClassRegistry.CreateEntity(className);
						this.Entityplayer.Code = GlobalConstants.EntityPlayerTypeCode;
						this.Entityplayer.FromBytes(reader2, false);
						this.Entityplayer.Pos.FromBytes(reader2);
						if (server.Config.RepairMode && this.Entityplayer.Code.Path != "player")
						{
							ServerMain.Logger.Error("Something derped with the player entity, its code is not 'player'. We are in repair mode so will reset this to 'player'. Will also reset their position to spawn for safety");
							this.Entityplayer.ServerPos.SetFrom(server.DefaultSpawnPosition);
							this.Entityplayer.Pos.SetFrom(server.DefaultSpawnPosition);
							this.Entityplayer.Code = AssetLocation.Create("game:player", "game");
						}
					}
					catch (Exception e2)
					{
						ServerMain.Logger.Error("Could not load an entityplayer. Will not read it's stored properties. Exception:");
						ServerMain.Logger.Error(e2);
					}
				}
			}
			this.EntityPlayerSerialized = null;
			if (this.PickingRange == 0f)
			{
				this.PickingRange = GlobalConstants.DefaultPickingRange;
			}
		}

		public void BeforeSerialization()
		{
			if (this.inventories != null)
			{
				Dictionary<string, byte[]> dicts = new Dictionary<string, byte[]>();
				FastMemoryStream fastMemoryStream;
				if ((fastMemoryStream = ServerWorldPlayerData.reusableSerializationStream) == null)
				{
					fastMemoryStream = (ServerWorldPlayerData.reusableSerializationStream = new FastMemoryStream());
				}
				using (FastMemoryStream ms = fastMemoryStream)
				{
					foreach (KeyValuePair<string, InventoryBase> val in this.inventories)
					{
						if (val.Value is InventoryBasePlayer)
						{
							ms.Reset();
							BinaryWriter writer = new BinaryWriter(ms);
							TreeAttribute tree = new TreeAttribute();
							val.Value.ToTreeAttributes(tree);
							tree.ToBytes(writer);
							dicts.Add(val.Key, ms.ToArray());
						}
					}
					this.inventoriesSerialized = dicts;
					if (this.Entityplayer != null)
					{
						ms.Reset();
						BinaryWriter writer2 = new BinaryWriter(ms);
						writer2.Write(ServerMain.ClassRegistry.entityTypeToClassNameMapping[this.Entityplayer.GetType()]);
						this.Entityplayer.ToBytes(writer2, false);
						this.Entityplayer.Pos.ToBytes(writer2);
						this.EntityPlayerSerialized = ms.ToArray();
					}
				}
			}
		}

		[ProtoAfterDeserialization]
		private void afterDeserialization()
		{
			if (this.ModData == null)
			{
				this.ModData = new Dictionary<string, byte[]>();
			}
		}

		public Packet_Server ToPacket(IServerPlayer owningPlayer, bool sendInventory = true, bool sendPrivileges = true)
		{
			FuzzyEntityPos spawnPos = owningPlayer.GetSpawnPosition(false);
			Packet_Server packet_Server = new Packet_Server();
			packet_Server.Id = 41;
			Packet_PlayerData packet_PlayerData = new Packet_PlayerData();
			packet_PlayerData.ClientId = owningPlayer.ClientId;
			packet_PlayerData.EntityId = this.Entityplayer.EntityId;
			packet_PlayerData.GameMode = (int)this.GameMode;
			packet_PlayerData.MoveSpeed = CollectibleNet.SerializeFloat(this.MoveSpeedMultiplier);
			packet_PlayerData.FreeMove = ((this.FreeMove > false) ? 1 : 0);
			packet_PlayerData.PickingRange = CollectibleNet.SerializeFloat(this.PickingRange);
			packet_PlayerData.NoClip = ((this.NoClip > false) ? 1 : 0);
			packet_PlayerData.Deaths = this.Deaths;
			packet_PlayerData.InventoryContents = null;
			packet_PlayerData.InventoryContentsCount = 0;
			packet_PlayerData.InventoryContentsLength = 0;
			packet_PlayerData.PlayerUID = this.PlayerUID;
			packet_PlayerData.HotbarSlotId = owningPlayer.InventoryManager.ActiveHotbarSlotNumber;
			packet_PlayerData.Entitlements = string.Join(",", owningPlayer.Entitlements.Select((Entitlement e) => e.Code).ToArray<string>());
			packet_PlayerData.FreeMovePlaneLock = (int)this.freeMovePlaneLock;
			packet_PlayerData.AreaSelectionMode = ((this.areaSelectionMode > false) ? 1 : 0);
			packet_PlayerData.Spawnx = spawnPos.XInt;
			packet_PlayerData.Spawny = spawnPos.YInt;
			packet_PlayerData.Spawnz = spawnPos.ZInt;
			packet_Server.PlayerData = packet_PlayerData;
			Packet_Server packet = packet_Server;
			if (sendPrivileges)
			{
				packet.PlayerData.SetPrivileges(owningPlayer.Privileges);
				packet.PlayerData.RoleCode = owningPlayer.Role.Code;
			}
			if (sendInventory)
			{
				List<Packet_InventoryContents> invpackets = new List<Packet_InventoryContents>();
				foreach (InventoryBase inv in this.inventories.ValuesOrdered)
				{
					if (inv is InventoryBasePlayer)
					{
						invpackets.Add((inv.InvNetworkUtil as InventoryNetworkUtil).ToPacket(owningPlayer));
					}
				}
				packet.PlayerData.SetInventoryContents(invpackets.ToArray());
			}
			return packet;
		}

		public Packet_Server ToPacketForOtherPlayers(IServerPlayer owningPlayer)
		{
			List<InventoryBasePlayer> visibleInventories = new List<InventoryBasePlayer>();
			foreach (InventoryBase inv in this.inventories.ValuesOrdered)
			{
				if (inv is InventoryBasePlayer && (inv.ClassName == "hotbar" || inv.ClassName == "character" || inv.ClassName == "backpack"))
				{
					visibleInventories.Add((InventoryBasePlayer)inv);
				}
			}
			Packet_InventoryContents[] contents = new Packet_InventoryContents[visibleInventories.Count];
			int i = 0;
			foreach (InventoryBasePlayer inv2 in visibleInventories)
			{
				contents[i++] = (inv2.InvNetworkUtil as InventoryNetworkUtil).ToPacket(owningPlayer);
			}
			Packet_Server packet_Server = new Packet_Server();
			packet_Server.Id = 41;
			Packet_PlayerData packet_PlayerData = new Packet_PlayerData();
			packet_PlayerData.ClientId = owningPlayer.ClientId;
			packet_PlayerData.EntityId = this.Entityplayer.EntityId;
			packet_PlayerData.PlayerUID = this.PlayerUID;
			packet_PlayerData.PlayerName = owningPlayer.PlayerName;
			packet_PlayerData.GameMode = (int)this.GameMode;
			packet_PlayerData.MoveSpeed = CollectibleNet.SerializeFloat(this.MoveSpeedMultiplier);
			packet_PlayerData.PickingRange = CollectibleNet.SerializeFloat(this.PickingRange);
			packet_PlayerData.FreeMove = ((this.FreeMove > false) ? 1 : 0);
			packet_PlayerData.NoClip = ((this.NoClip > false) ? 1 : 0);
			packet_PlayerData.InventoryContents = contents;
			packet_PlayerData.HotbarSlotId = owningPlayer.InventoryManager.ActiveHotbarSlotNumber;
			packet_PlayerData.Entitlements = string.Join(",", owningPlayer.Entitlements.Select((Entitlement e) => e.Code).ToArray<string>());
			packet_PlayerData.InventoryContentsCount = contents.Length;
			packet_PlayerData.InventoryContentsLength = contents.Length;
			packet_PlayerData.FreeMovePlaneLock = (int)this.freeMovePlaneLock;
			packet_PlayerData.AreaSelectionMode = ((this.areaSelectionMode > false) ? 1 : 0);
			packet_Server.PlayerData = packet_PlayerData;
			return packet_Server;
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

		public void SetModdata(string key, byte[] data)
		{
			this.ModData[key] = data;
		}

		public void RemoveModdata(string key)
		{
			this.ModData.Remove(key);
		}

		public byte[] GetModdata(string key)
		{
			byte[] data;
			this.ModData.TryGetValue(key, out data);
			return data;
		}

		[ThreadStatic]
		private static FastMemoryStream reusableSerializationStream;

		[ProtoIgnore]
		internal int currentClientId;

		[ProtoIgnore]
		internal bool connected;

		[ProtoMember(1)]
		internal string PlayerUID;

		[ProtoMember(2)]
		private Dictionary<string, byte[]> inventoriesSerialized;

		[ProtoMember(3)]
		private byte[] EntityPlayerSerialized;

		[ProtoMember(4)]
		public EnumGameMode GameMode;

		[ProtoMember(5)]
		public float MoveSpeedMultiplier;

		[ProtoMember(11)]
		public float PickingRange;

		[ProtoMember(6)]
		public bool FreeMove;

		[ProtoMember(7)]
		public bool NoClip;

		[ProtoMember(8)]
		public int Viewdistance;

		[ProtoMember(9)]
		private int selectedHotbarslot;

		[ProtoMember(10)]
		private EnumFreeMovAxisLock freeMovePlaneLock;

		[ProtoMember(12)]
		private bool areaSelectionMode;

		[ProtoMember(13)]
		private bool didSelectSkin;

		[ProtoMember(14)]
		private PlayerSpawnPos spawnPosition;

		[ProtoMember(15)]
		public Dictionary<string, byte[]> ModData;

		[ProtoMember(16)]
		public float PreviousPickingRange = 100f;

		[ProtoMember(17)]
		public int Deaths;

		[ProtoMember(18)]
		public bool RenderMetaBlocks;

		private EntityPlayer Entityplayer;

		internal OrderedDictionary<string, InventoryBase> inventories = new OrderedDictionary<string, InventoryBase>();
	}
}
