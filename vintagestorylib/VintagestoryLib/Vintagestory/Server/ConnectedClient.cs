using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace Vintagestory.Server
{
	public class ConnectedClient
	{
		public virtual ServerPlayerData ServerData
		{
			get
			{
				ServerPlayer player = this.Player;
				if (player == null)
				{
					return null;
				}
				return player.serverdata;
			}
		}

		public EntityPlayer Entityplayer
		{
			get
			{
				ServerWorldPlayerData serverWorldPlayerData = this.worlddata;
				if (serverWorldPlayerData == null)
				{
					return null;
				}
				return serverWorldPlayerData.EntityPlayer;
			}
		}

		public ServerWorldPlayerData WorldData
		{
			get
			{
				return this.worlddata;
			}
			set
			{
				this.worlddata = value;
				this.worlddata.currentClientId = this.Id;
			}
		}

		public EntityPos Position
		{
			get
			{
				ServerWorldPlayerData serverWorldPlayerData = this.worlddata;
				if (serverWorldPlayerData == null)
				{
					return null;
				}
				EntityPlayer entityPlayer = serverWorldPlayerData.EntityPlayer;
				if (entityPlayer == null)
				{
					return null;
				}
				return entityPlayer.ServerPos;
			}
		}

		public BlockPos ChunkPos
		{
			get
			{
				EntityPos Pos = this.Position;
				return new BlockPos((int)Pos.X / 32, (int)Pos.Y / 32, (int)Pos.Z / 32, Pos.Dimension);
			}
		}

		public string PlayerName
		{
			get
			{
				if (this.ServerData == null || this.ServerData.LastKnownPlayername == null)
				{
					return this.FallbackPlayerName;
				}
				return this.ServerData.LastKnownPlayername;
			}
		}

		public virtual bool IsPlayingClient
		{
			get
			{
				return this.State == EnumClientState.Playing;
			}
		}

		public virtual bool ServerAssetsSent { get; set; }

		public EnumClientState State
		{
			get
			{
				return this.clientStateOnServer;
			}
			set
			{
				this.clientStateOnServer = value;
				if (this.worlddata != null)
				{
					this.worlddata.connected = true;
				}
			}
		}

		public long LastChatMessageTotalMs { get; set; }

		public NetConnection Socket
		{
			get
			{
				return this.socket;
			}
			set
			{
				this.socket = value;
				this.ipAddress = this.socket.RemoteEndPoint().Address.ToString();
				this.IsSinglePlayerClient = this.socket is DummyNetConnection;
				this.IsLocalConnection = this.ipAddress == "127.0.0.1" || this.ipAddress.StartsWithFast("::1") || this.ipAddress.StartsWithFast("::ffff:127.0.0.1");
			}
		}

		public ConnectedClient(int clientId)
		{
			this.Id = clientId;
			this.State = EnumClientState.Connecting;
			this.IsNewClient = true;
			this.Ping = new Ping();
		}

		public void Initialise()
		{
			this.ChunkSent = new HashSet<long>();
			this.MapChunkSent = new HashSet<long>();
			this.MapRegionSent = new HashSet<long>();
			this.IsInventoryDirty = true;
			this.IsPlayerStatsDirty = true;
			this.TrackedEntities = new HashSet<long>(100);
			this.EntitySpawnsToSend = new List<Entity>();
		}

		public void LoadOrCreatePlayerData(ServerMain server, string playername, string playerUid)
		{
			this.worlddata = null;
			if (server.PlayerDataManager.WorldDataByUID.TryGetValue(playerUid, out this.worlddata))
			{
				this.Player = new ServerPlayer(server, this.worlddata);
				server.PlayersByUid[this.worlddata.PlayerUID] = this.Player;
			}
			else
			{
				byte[] data = server.chunkThread.gameDatabase.GetPlayerData(playerUid);
				if (data != null)
				{
					try
					{
						this.worlddata = SerializerUtil.Deserialize<ServerWorldPlayerData>(data);
						this.worlddata.Init(server);
					}
					catch (Exception e)
					{
						ServerMain.Logger.Notification("Unable to deserlialize and init player data for playeruid {0}. Will create new one.", new object[] { playerUid });
						ServerMain.Logger.Notification(LoggerBase.CleanStackTrace(e.ToString()));
					}
				}
				if (data == null)
				{
					this.worlddata = ServerWorldPlayerData.CreateNew(playername, playerUid);
					this.worlddata.Init(server);
					EntityProperties type = server.GetEntityType(GlobalConstants.EntityPlayerTypeCode);
					if (type == null)
					{
						throw new Exception("Cannot init player, there is no entity type with code " + GlobalConstants.EntityPlayerTypeCode + " was loaded!");
					}
					this.worlddata.EntityPlayer.Code = type.Code;
					this.IsNewEntityPlayer = true;
				}
				this.Player = new ServerPlayer(server, this.worlddata);
				server.PlayerDataManager.WorldDataByUID[playerUid] = this.worlddata;
				server.PlayersByUid[this.worlddata.PlayerUID] = this.Player;
			}
			if (this.worlddata.EntityPlayer == null)
			{
				ServerMain.Logger.Warning("Player had no entityplayer assigned to it? Creating new one.");
				this.worlddata.EntityPlayer = new EntityPlayer();
				EntityProperties type2 = server.GetEntityType(GlobalConstants.EntityPlayerTypeCode);
				if (type2 == null)
				{
					throw new Exception("Cannot init player, there is no entity type with code " + GlobalConstants.EntityPlayerTypeCode + " was loaded!");
				}
				this.worlddata.EntityPlayer.Code = type2.Code;
				this.IsNewEntityPlayer = true;
			}
			this.ServerData.LastKnownPlayername = playername;
		}

		public bool DidSendChunk(long index3d)
		{
			return this.ChunkSent.Contains(index3d);
		}

		public bool DidSendMapChunk(long index2d)
		{
			return this.MapChunkSent.Contains(index2d);
		}

		public bool DidSendMapRegion(long index2d)
		{
			return this.MapRegionSent.Contains(index2d);
		}

		public void SetMapRegionSent(long index2d)
		{
			this.MapRegionSent.Add(index2d);
		}

		public void SetChunkSent(long index3d)
		{
			this.ChunkSent.Add(index3d);
		}

		public void SetMapChunkSent(long index2d)
		{
			this.MapChunkSent.Add(index2d);
		}

		public void RemoveMapRegionSent(long index2d)
		{
			this.MapRegionSent.Remove(index2d);
		}

		public void RemoveChunkSent(long index3d)
		{
			this.ChunkSent.Remove(index3d);
		}

		public void RemoveMapChunkSent(long index2d)
		{
			this.MapChunkSent.Remove(index2d);
		}

		public override string ToString()
		{
			string name = this.Entityplayer.WatchedAttributes.GetString("name", null);
			return string.Format("{0}:{1}:{2} {3}", new object[]
			{
				name,
				this.ServerData.RoleCode,
				PlayerRole.PrivilegesString(this.ServerData.PermaPrivileges.ToList<string>()),
				this.ipAddress
			});
		}

		public void CloseConnection()
		{
			if (this.Socket == null)
			{
				return;
			}
			this.Socket.Shutdown();
			TyronThreadPool.QueueLongDurationTask(delegate
			{
				Thread.Sleep(1000);
				NetConnection netConnection = this.Socket;
				if (netConnection == null)
				{
					return;
				}
				netConnection.Close();
			}, "connectedclientclose");
		}

		public bool ShouldReceiveUpdatesForPos(BlockPos pos)
		{
			return this.State == EnumClientState.Playing && this.Entityplayer != null && this.Entityplayer.ServerPos.InRangeOf(pos, (float)this.worlddata.Viewdistance);
		}

		public int AuditFlySuspicion;

		public long AuditFlySuspicionPrintedTotalMs = -99999L;

		public long LastAuditFlySuspicionTotalMs;

		public int TotalFlySuspicions;

		public int TotalTeleSupicions;

		public string LoginToken;

		private ServerWorldPlayerData worlddata;

		public EntityControls previousControls = new EntityControls();

		public ServerPlayer Player;

		public string SentPlayerUid;

		public bool IsNewEntityPlayer;

		public bool FallBackToTcp;

		public List<Entity> EntitySpawnsToSend;

		public bool stopSent;

		public bool IsLocalConnection;

		public bool IsSinglePlayerClient;

		public long MillisecsAtConnect;

		public HashSet<long> ChunkSent;

		public HashSet<long> MapChunkSent;

		public HashSet<long> MapRegionSent;

		public int CurrentChunkSentRadius;

		public bool IsInventoryDirty;

		public bool IsPlayerStatsDirty;

		public bool ServerDidReceiveUdp;

		public List<EntityDespawn> entitiesNowOutOfRange = new List<EntityDespawn>();

		public List<EntityInRange> entitiesNowInRange = new List<EntityInRange>();

		public HashSet<long> TrackedEntities;

		public List<Entity>[] threadedTrackedEntities;

		public int Id;

		public HashSet<long> forceSendChunks = new HashSet<long>();

		public HashSet<long> forceSendMapChunks = new HashSet<long>();

		private EnumClientState clientStateOnServer;

		private NetConnection socket;

		private string ipAddress = "";

		public bool IsNewClient;

		public Ping Ping;

		public float LastPing;

		public string FallbackPlayerName = "Unknown";
	}
}
