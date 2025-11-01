using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf
{
	public class ClientWorldPlayerData : IWorldPlayerData
	{
		public EnumGameMode CurrentGameMode
		{
			get
			{
				return this.gameMode;
			}
			set
			{
				this.gameMode = value;
			}
		}

		public bool RenderMetablocks { get; set; }

		public float MoveSpeedMultiplier
		{
			get
			{
				return this.moveSpeedMultiplier;
			}
			set
			{
				this.moveSpeedMultiplier = value;
			}
		}

		public float PickingRange
		{
			get
			{
				return this.pickingRange;
			}
			set
			{
				this.pickingRange = value;
			}
		}

		public bool FreeMove
		{
			get
			{
				return this.freeMove;
			}
			set
			{
				this.freeMove = value;
			}
		}

		public bool NoClip
		{
			get
			{
				return this.noClip;
			}
			set
			{
				this.noClip = value;
			}
		}

		public int Deaths
		{
			get
			{
				return this.deaths;
			}
			set
			{
				this.deaths = value;
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
				return this.entityplayer;
			}
			set
			{
				this.entityplayer = value;
			}
		}

		public EntityControls EntityControls
		{
			get
			{
				return this.entityplayer.Controls;
			}
		}

		public int LastApprovedViewDistance
		{
			get
			{
				return this.prevViewDistance;
			}
			set
			{
				this.prevViewDistance = value;
			}
		}

		public int CurrentClientId
		{
			get
			{
				return this.ClientId;
			}
		}

		public bool Connected
		{
			get
			{
				return true;
			}
		}

		public bool DidSelectSkin
		{
			get
			{
				return false;
			}
			set
			{
			}
		}

		public int DesiredViewDistance { get; set; }

		private ClientWorldPlayerData()
		{
			this.DesiredViewDistance = ClientSettings.ViewDistance;
			this.RenderMetablocks = ClientSettings.RenderMetaBlocks;
		}

		public void RequestNewViewDistance(ClientMain game)
		{
			this.RequestMode(game, this.moveSpeedMultiplier, this.pickingRange, this.gameMode, this.freeMove, this.noClip, this.freeMovePlaneLock, this.RenderMetablocks);
		}

		public void RequestMode(ClientMain game)
		{
			this.RequestMode(game, this.moveSpeedMultiplier, this.pickingRange, this.gameMode, this.freeMove, this.noClip, this.freeMovePlaneLock, this.RenderMetablocks);
		}

		public void SetMode(ClientMain game, float moveSpeedMultiplier)
		{
			this.RequestMode(game, moveSpeedMultiplier, this.pickingRange, this.gameMode, this.freeMove, this.noClip, this.freeMovePlaneLock, this.RenderMetablocks);
		}

		public void RequestMode(ClientMain game, bool noClip, bool freeMove)
		{
			this.RequestMode(game, this.moveSpeedMultiplier, this.pickingRange, this.gameMode, freeMove, noClip, this.freeMovePlaneLock, this.RenderMetablocks);
		}

		public void RequestMode(ClientMain game, EnumFreeMovAxisLock FreeMovePlaneLock)
		{
			this.RequestMode(game, this.moveSpeedMultiplier, this.pickingRange, this.gameMode, this.freeMove, this.noClip, FreeMovePlaneLock, this.RenderMetablocks);
		}

		public void RequestModeNoClip(ClientMain game, bool noClip)
		{
			this.RequestMode(game, this.moveSpeedMultiplier, this.pickingRange, this.gameMode, this.freeMove, noClip, this.freeMovePlaneLock, this.RenderMetablocks);
		}

		public void RequestModeFreeMove(ClientMain game, bool freeMove)
		{
			this.RequestMode(game, this.moveSpeedMultiplier, this.pickingRange, this.gameMode, freeMove, this.noClip, this.freeMovePlaneLock, this.RenderMetablocks);
		}

		public void RequestMode(ClientMain game, float moveSpeed, float pickRange, EnumGameMode gameMode, bool freeMove, bool noClip, EnumFreeMovAxisLock freeMovePlaneLock, bool renderMetaBlocks)
		{
			this.DesiredViewDistance = ClientSettings.ViewDistance;
			Packet_Client packet = new Packet_Client
			{
				Id = 20,
				RequestModeChange = new Packet_PlayerMode
				{
					PlayerUID = this.PlayerUID,
					GameMode = (int)gameMode,
					FreeMove = ((freeMove > false) ? 1 : 0),
					NoClip = ((noClip > false) ? 1 : 0),
					MoveSpeed = CollectibleNet.SerializeFloat(moveSpeed),
					PickingRange = CollectibleNet.SerializeFloat(pickRange),
					ViewDistance = ClientSettings.ViewDistance,
					FreeMovePlaneLock = (int)freeMovePlaneLock,
					RenderMetaBlocks = ((renderMetaBlocks > false) ? 1 : 0)
				}
			};
			game.SendPacketClient(packet);
		}

		public static ClientWorldPlayerData CreateNew()
		{
			return new ClientWorldPlayerData();
		}

		public ClientWorldPlayerData Clone()
		{
			return new ClientWorldPlayerData
			{
				ClientId = this.ClientId,
				EntityPlayer = this.entityplayer,
				freeMove = this.freeMove,
				freeMovePlaneLock = this.freeMovePlaneLock,
				gameMode = this.gameMode,
				moveSpeedMultiplier = this.moveSpeedMultiplier,
				noClip = this.noClip,
				pickingRange = this.pickingRange,
				PlayerUID = this.PlayerUID,
				areaSelectionMode = this.areaSelectionMode
			};
		}

		public void UpdateFromPacket(ClientMain game, Packet_PlayerData packet)
		{
			this.gameMode = (EnumGameMode)packet.GameMode;
			this.moveSpeedMultiplier = CollectibleNet.DeserializeFloat(packet.MoveSpeed);
			this.pickingRange = CollectibleNet.DeserializeFloat(packet.PickingRange);
			this.areaSelectionMode = packet.AreaSelectionMode > 0;
			this.freeMovePlaneLock = (EnumFreeMovAxisLock)packet.FreeMovePlaneLock;
			this.freeMove = packet.FreeMove > 0;
			this.noClip = packet.NoClip > 0;
			this.deaths = packet.Deaths;
			this.PlayerUID = packet.PlayerUID;
			this.PlayerName = packet.PlayerName;
			this.ClientId = packet.ClientId;
			Entity entity;
			game.LoadedEntities.TryGetValue(packet.EntityId, out entity);
			if (entity == null)
			{
				return;
			}
			this.EntityPlayer = (EntityPlayer)entity;
			this.EntityPlayer.UpdatePartitioning();
		}

		public void UpdateFromPacket(ClientMain game, Packet_PlayerMode mode)
		{
			this.moveSpeedMultiplier = CollectibleNet.DeserializeFloat(mode.MoveSpeed);
			this.pickingRange = CollectibleNet.DeserializeFloat(mode.PickingRange);
			this.gameMode = (EnumGameMode)mode.GameMode;
			this.freeMove = mode.FreeMove > 0;
			this.noClip = mode.NoClip > 0;
			this.freeMovePlaneLock = (EnumFreeMovAxisLock)mode.FreeMovePlaneLock;
			if (this.ClientId == game.player.ClientId && mode.ViewDistance != ClientSettings.ViewDistance)
			{
				this.LastApprovedViewDistance = mode.ViewDistance;
				ClientSettings.ViewDistance = mode.ViewDistance;
			}
			game.player.Entity.UpdatePartitioning();
		}

		public void SetModdata(string key, byte[] data)
		{
			throw new NotImplementedException();
		}

		public void RemoveModdata(string key)
		{
			throw new NotImplementedException();
		}

		public byte[] GetModdata(string key)
		{
			throw new NotImplementedException();
		}

		public void SetModData<T>(string key, T data)
		{
			throw new NotImplementedException();
		}

		public T GetModData<T>(string key, T defaultValue = default(T))
		{
			throw new NotImplementedException();
		}

		private EnumGameMode gameMode;

		private float moveSpeedMultiplier;

		private float pickingRange;

		private bool freeMove;

		private bool noClip;

		private int deaths;

		private bool areaSelectionMode;

		private EnumFreeMovAxisLock freeMovePlaneLock;

		public int ClientId;

		public string PlayerUID;

		public string PlayerName;

		private int prevViewDistance;

		private EntityPlayer entityplayer;
	}
}
