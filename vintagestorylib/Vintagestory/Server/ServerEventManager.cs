using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace Vintagestory.Server
{
	public class ServerEventManager : EventManager
	{
		public override ILogger Logger
		{
			get
			{
				return ServerMain.Logger;
			}
		}

		public override string CommandPrefix
		{
			get
			{
				return "/";
			}
		}

		public override long InWorldEllapsedMs
		{
			get
			{
				return this.server.ElapsedMilliseconds;
			}
		}

		public event Action OnSaveGameCreated;

		public event Action OnSaveGameLoaded;

		public event Action AssetsFirstLoaded;

		public event Action AssetsFinalizer;

		public event Action OnGameWorldBeingSaved;

		public event Action OnWorldgenStartup;

		public event Action OnStartPhysicsThread;

		public event UpnpCompleteDelegate OnUpnpComplete;

		public event PlayerDelegate OnPlayerRespawn;

		public event PlayerDelegate OnPlayerJoin;

		public event PlayerDelegate OnPlayerNowPlaying;

		public event PlayerDelegate OnPlayerLeave;

		public event PlayerDelegate OnPlayerDisconnect;

		public event PlayerChatDelegate OnPlayerChat;

		public event PlayerDeathDelegate OnPlayerDeath;

		public event PlayerDelegate OnPlayerChangeGamemode;

		public event Func<IServerPlayer, ActiveSlotChangeEventArgs, EnumHandling> BeforeActiveSlotChanged;

		public event Action<IServerPlayer, ActiveSlotChangeEventArgs> AfterActiveSlotChanged;

		public event PlayerDelegate OnPlayerCreate;

		public event CanUseDelegate CanUseBlock;

		public event CanPlaceOrBreakDelegate CanPlaceOrBreakBlock;

		public event BlockUsedDelegate DidUseBlock;

		public event BlockPlacedDelegate DidPlaceBlock;

		public event BlockBrokenDelegate DidBreakBlock;

		public event BlockBreakDelegate BreakBlock;

		public event OnInteractDelegate OnPlayerInteractEntity;

		public event EntityDelegate OnEntitySpawn;

		public event EntityDespawnDelegate OnEntityDespawn;

		public event EntityDelegate OnEntityLoaded;

		public event TrySpawnEntityDelegate OnTrySpawnEntity;

		public ServerEventManager(ServerMain server)
		{
			this.server = server;
			this.Init();
		}

		private void Init()
		{
			this.serverRunPhaseDelegates = new Dictionary<EnumServerRunPhase, List<Action>>();
			foreach (object obj in Enum.GetValues(typeof(EnumServerRunPhase)))
			{
				EnumServerRunPhase stage = (EnumServerRunPhase)obj;
				this.serverRunPhaseDelegates[stage] = new List<Action>();
			}
		}

		public override bool HasPrivilege(string playerUid, string privilegecode)
		{
			return this.server.GetServerPlayerData(playerUid).HasPrivilege(privilegecode, this.server.Config.RolesByCode) || playerUid == "console";
		}

		internal void RegisterOnServerRunPhase(EnumServerRunPhase runPhase, Action handler)
		{
			this.server.ModEventManager.serverRunPhaseDelegates[runPhase].Add(handler);
		}

		public virtual void TriggerUpnpComplete(bool success)
		{
			UpnpCompleteDelegate onUpnpComplete = this.OnUpnpComplete;
			this.Trigger<UpnpCompleteDelegate>((onUpnpComplete != null) ? onUpnpComplete.GetInvocationList() : null, "OnUpnpComplete", delegate(UpnpCompleteDelegate dele)
			{
				if (dele != null)
				{
					dele(success);
				}
			}, null);
		}

		public virtual void TriggerEntityLoaded(Entity entity)
		{
			EntityDelegate onEntityLoaded = this.OnEntityLoaded;
			this.Trigger<EntityDelegate>((onEntityLoaded != null) ? onEntityLoaded.GetInvocationList() : null, "OnEntityLoaded", delegate(EntityDelegate dele)
			{
				if (dele != null)
				{
					dele(entity);
				}
			}, null);
		}

		public virtual void TriggerEntitySpawned(Entity entity)
		{
			EntityDelegate onEntitySpawn = this.OnEntitySpawn;
			this.Trigger<EntityDelegate>((onEntitySpawn != null) ? onEntitySpawn.GetInvocationList() : null, "OnEntitySpawn", delegate(EntityDelegate dele)
			{
				if (dele != null)
				{
					dele(entity);
				}
			}, null);
		}

		public virtual bool TriggerTrySpawnEntity(IBlockAccessor blockaccessor, ref EntityProperties properties, Vec3d position, long herdId)
		{
			if (this.OnTrySpawnEntity == null)
			{
				return true;
			}
			bool allow = true;
			foreach (TrySpawnEntityDelegate dele in this.OnTrySpawnEntity.GetInvocationList())
			{
				try
				{
					allow &= dele(blockaccessor, ref properties, position, herdId);
				}
				catch (Exception e)
				{
					ServerMain.Logger.Error("Exception thrown during handling event OnTrySpawnEntity. Will skip over.");
					ServerMain.Logger.Error(e);
				}
			}
			return allow;
		}

		public virtual void TriggerEntityDespawned(Entity entity, EntityDespawnData reason)
		{
			EntityDespawnDelegate onEntityDespawn = this.OnEntityDespawn;
			this.Trigger<EntityDespawnDelegate>((onEntityDespawn != null) ? onEntityDespawn.GetInvocationList() : null, "OnEntityDespawned", delegate(EntityDespawnDelegate dele)
			{
				if (dele != null)
				{
					dele(entity, reason);
				}
			}, null);
		}

		public virtual void OnAssetsFirstLoaded()
		{
			Action assetsFirstLoaded = this.AssetsFirstLoaded;
			this.Trigger<Action>((assetsFirstLoaded != null) ? assetsFirstLoaded.GetInvocationList() : null, "AssetsFirstLoaded", delegate(Action dele)
			{
				if (dele != null)
				{
					dele();
				}
			}, null);
		}

		public virtual void TriggerFinalizeAssets()
		{
			Action assetsFinalizer = this.AssetsFinalizer;
			this.Trigger<Action>((assetsFinalizer != null) ? assetsFinalizer.GetInvocationList() : null, "FinalizeAssets", delegate(Action dele)
			{
				if (dele != null)
				{
					dele();
				}
			}, null);
		}

		public virtual void TriggerWorldgenStartup()
		{
			Action onWorldgenStartup = this.OnWorldgenStartup;
			this.Trigger<Action>((onWorldgenStartup != null) ? onWorldgenStartup.GetInvocationList() : null, "OnWorldgenStartup", delegate(Action dele)
			{
				if (dele != null)
				{
					dele();
				}
			}, null);
		}

		public virtual void TriggerPhysicsThreadStart()
		{
			Action onStartPhysicsThread = this.OnStartPhysicsThread;
			if (onStartPhysicsThread == null)
			{
				return;
			}
			onStartPhysicsThread();
		}

		public virtual void TriggerSaveGameLoaded()
		{
			Action onSaveGameLoaded = this.OnSaveGameLoaded;
			this.Trigger<Action>((onSaveGameLoaded != null) ? onSaveGameLoaded.GetInvocationList() : null, "OnSaveGameLoaded", delegate(Action dele)
			{
				if (dele != null)
				{
					dele();
				}
			}, null);
		}

		public virtual void TriggerSaveGameCreated()
		{
			Action onSaveGameCreated = this.OnSaveGameCreated;
			this.Trigger<Action>((onSaveGameCreated != null) ? onSaveGameCreated.GetInvocationList() : null, "OnSaveGameCreated", delegate(Action dele)
			{
				if (dele != null)
				{
					dele();
				}
			}, null);
		}

		public virtual void TriggerGameWorldBeingSaved()
		{
			if (this.OnGameWorldBeingSaved == null)
			{
				return;
			}
			foreach (Action val in this.OnGameWorldBeingSaved.GetInvocationList())
			{
				try
				{
					val();
					FrameProfilerUtil frameProfiler = ServerMain.FrameProfiler;
					if (frameProfiler != null && frameProfiler.Enabled)
					{
						ServerMain.FrameProfiler.Mark("beingsaved-", val.Target.ToString());
					}
				}
				catch (Exception e)
				{
					ServerMain.Logger.Error("Exception thrown during handling event OnGameWorldBeingSaved. Will skip over.");
					ServerMain.Logger.Error(e);
				}
			}
		}

		public virtual void TriggerDidUseBlock(IServerPlayer player, BlockSelection blockSel)
		{
			BlockUsedDelegate didUseBlock = this.DidUseBlock;
			this.Trigger<BlockUsedDelegate>((didUseBlock != null) ? didUseBlock.GetInvocationList() : null, "DidUseBlock", delegate(BlockUsedDelegate dele)
			{
				if (dele != null)
				{
					dele(player, blockSel);
				}
			}, null);
		}

		public virtual void TriggerDidBreakBlock(IServerPlayer player, int oldBlockId, BlockSelection blockSel)
		{
			BlockBrokenDelegate didBreakBlock = this.DidBreakBlock;
			this.Trigger<BlockBrokenDelegate>((didBreakBlock != null) ? didBreakBlock.GetInvocationList() : null, "DidBreakBlock", delegate(BlockBrokenDelegate dele)
			{
				if (dele != null)
				{
					dele(player, oldBlockId, blockSel);
				}
			}, null);
		}

		public virtual void TriggerBreakBlock(IServerPlayer player, BlockSelection blockSel, ref float dropQuantityMultiplier, ref EnumHandling handling)
		{
			if (this.BreakBlock == null)
			{
				return;
			}
			foreach (BlockBreakDelegate dele in this.BreakBlock.GetInvocationList())
			{
				try
				{
					if (dele != null)
					{
						dele(player, blockSel, ref dropQuantityMultiplier, ref handling);
					}
					if (handling == EnumHandling.PreventSubsequent)
					{
						break;
					}
				}
				catch (Exception ex)
				{
					ServerMain.Logger.Error("Mod exception during event BreakBlock. Will skip over");
					ServerMain.Logger.Error(ex);
				}
			}
		}

		public virtual void TriggerPlayerInteractEntity(Entity entity, IPlayer byPlayer, ItemSlot slot, Vec3d hitPosition, int mode, ref EnumHandling handling)
		{
			if (this.OnPlayerInteractEntity == null)
			{
				return;
			}
			foreach (OnInteractDelegate dele in this.OnPlayerInteractEntity.GetInvocationList())
			{
				try
				{
					if (dele != null)
					{
						dele(entity, byPlayer, slot, hitPosition, mode, ref handling);
					}
					if (handling == EnumHandling.PreventSubsequent)
					{
						break;
					}
				}
				catch (Exception ex)
				{
					ServerMain.Logger.Error("Mod exception during event BreakBlock. Will skip over");
					ServerMain.Logger.Error(ex);
				}
			}
		}

		public virtual void TriggerDidPlaceBlock(IServerPlayer player, int oldBlockId, BlockSelection blockSel, ItemStack withItemStack)
		{
			BlockPlacedDelegate didPlaceBlock = this.DidPlaceBlock;
			this.Trigger<BlockPlacedDelegate>((didPlaceBlock != null) ? didPlaceBlock.GetInvocationList() : null, "DidPlaceBlock", delegate(BlockPlacedDelegate dele)
			{
				if (dele != null)
				{
					dele(player, oldBlockId, blockSel, withItemStack);
				}
			}, null);
		}

		public virtual void TriggerPlayerLeave(IServerPlayer player)
		{
			PlayerDelegate onPlayerLeave = this.OnPlayerLeave;
			this.Trigger<PlayerDelegate>((onPlayerLeave != null) ? onPlayerLeave.GetInvocationList() : null, "OnPlayerLeave", delegate(PlayerDelegate dele)
			{
				if (dele != null)
				{
					dele(player);
				}
			}, null);
		}

		public virtual void TriggerPlayerDisconnect(IServerPlayer player)
		{
			PlayerDelegate onPlayerDisconnect = this.OnPlayerDisconnect;
			this.Trigger<PlayerDelegate>((onPlayerDisconnect != null) ? onPlayerDisconnect.GetInvocationList() : null, "OnPlayerDisconnect", delegate(PlayerDelegate dele)
			{
				if (dele != null)
				{
					dele(player);
				}
			}, null);
		}

		public virtual void TriggerPlayerCreate(IServerPlayer player)
		{
			PlayerDelegate onPlayerCreate = this.OnPlayerCreate;
			this.Trigger<PlayerDelegate>((onPlayerCreate != null) ? onPlayerCreate.GetInvocationList() : null, "OnPlayerCreate", delegate(PlayerDelegate dele)
			{
				if (dele != null)
				{
					dele(player);
				}
			}, null);
		}

		public virtual void TriggerPlayerJoin(IServerPlayer player)
		{
			PlayerDelegate onPlayerJoin = this.OnPlayerJoin;
			this.Trigger<PlayerDelegate>((onPlayerJoin != null) ? onPlayerJoin.GetInvocationList() : null, "OnPlayerJoin", delegate(PlayerDelegate dele)
			{
				if (dele != null)
				{
					dele(player);
				}
			}, null);
		}

		public virtual void TriggerPlayerNowPlaying(IServerPlayer player)
		{
			PlayerDelegate onPlayerNowPlaying = this.OnPlayerNowPlaying;
			this.Trigger<PlayerDelegate>((onPlayerNowPlaying != null) ? onPlayerNowPlaying.GetInvocationList() : null, "OnPlayerNowPlaying", delegate(PlayerDelegate dele)
			{
				if (dele != null)
				{
					dele(player);
				}
			}, null);
		}

		public virtual void TriggerPlayerRespawn(IServerPlayer player)
		{
			PlayerDelegate onPlayerRespawn = this.OnPlayerRespawn;
			this.Trigger<PlayerDelegate>((onPlayerRespawn != null) ? onPlayerRespawn.GetInvocationList() : null, "OnPlayerRespawn", delegate(PlayerDelegate dele)
			{
				if (dele != null)
				{
					dele(player);
				}
			}, null);
		}

		public virtual bool TriggerBeforeActiveSlotChanged(IServerPlayer player, int fromSlot, int toSlot)
		{
			ActiveSlotChangeEventArgs args = new ActiveSlotChangeEventArgs(fromSlot, toSlot);
			Func<IServerPlayer, ActiveSlotChangeEventArgs, EnumHandling> beforeActiveSlotChanged = this.BeforeActiveSlotChanged;
			return beforeActiveSlotChanged == null || beforeActiveSlotChanged.InvokeSafeCancellable(this.Logger, "BeforeActiveSlotChanged", player, args);
		}

		public virtual void TriggerAfterActiveSlotChanged(IServerPlayer player, int fromSlot, int toSlot)
		{
			ActiveSlotChangeEventArgs args = new ActiveSlotChangeEventArgs(fromSlot, toSlot);
			Action<IServerPlayer, ActiveSlotChangeEventArgs> afterActiveSlotChanged = this.AfterActiveSlotChanged;
			this.Trigger<Action<IServerPlayer, ActiveSlotChangeEventArgs>>((afterActiveSlotChanged != null) ? afterActiveSlotChanged.GetInvocationList() : null, "AfterActiveSlotChanged", delegate(Action<IServerPlayer, ActiveSlotChangeEventArgs> dele)
			{
				if (dele != null)
				{
					dele(player, args);
				}
			}, null);
		}

		public virtual void TriggerOnplayerChat(IServerPlayer player, int channelId, ref string message, ref string data, BoolRef consumed)
		{
			if (this.OnPlayerChat == null)
			{
				return;
			}
			foreach (PlayerChatDelegate dele in this.OnPlayerChat.GetInvocationList())
			{
				try
				{
					dele(player, channelId, ref message, ref data, consumed);
				}
				catch (Exception ex)
				{
					this.Logger.Error("Mod exception: OnPlayerChat");
					this.Logger.Error(ex);
				}
				if (consumed.value)
				{
					break;
				}
			}
		}

		public virtual void TriggerPlayerDeath(IServerPlayer player, DamageSource source)
		{
			PlayerDeathDelegate onPlayerDeath = this.OnPlayerDeath;
			this.Trigger<PlayerDeathDelegate>((onPlayerDeath != null) ? onPlayerDeath.GetInvocationList() : null, "OnPlayerDeath", delegate(PlayerDeathDelegate dele)
			{
				if (dele != null)
				{
					dele(player, source);
				}
			}, null);
		}

		public virtual void TriggerPlayerChangeGamemode(IServerPlayer player)
		{
			PlayerDelegate onPlayerChangeGamemode = this.OnPlayerChangeGamemode;
			this.Trigger<PlayerDelegate>((onPlayerChangeGamemode != null) ? onPlayerChangeGamemode.GetInvocationList() : null, "OnPlayerChangeGamemode", delegate(PlayerDelegate dele)
			{
				if (dele != null)
				{
					dele(player);
				}
			}, null);
		}

		public virtual bool TriggerCanPlaceOrBreak(IServerPlayer player, BlockSelection blockSel, out string claimant)
		{
			claimant = null;
			if (this.CanPlaceOrBreakBlock == null)
			{
				return true;
			}
			bool retval = true;
			foreach (CanPlaceOrBreakDelegate dele in this.CanPlaceOrBreakBlock.GetInvocationList())
			{
				try
				{
					retval = retval && dele(player, blockSel, out claimant);
				}
				catch (Exception ex)
				{
					this.Logger.Error("Mod exception during CanPlaceOrBreak");
					this.Logger.Error(ex);
					retval = false;
					break;
				}
			}
			return retval;
		}

		public virtual bool TriggerCanUse(IServerPlayer player, BlockSelection blockSel)
		{
			bool retval = true;
			CanUseDelegate canUseBlock = this.CanUseBlock;
			this.Trigger<CanUseDelegate>((canUseBlock != null) ? canUseBlock.GetInvocationList() : null, "CanUse", delegate(CanUseDelegate dele)
			{
				retval = retval && dele(player, blockSel);
			}, delegate
			{
				retval = false;
			});
			return retval;
		}

		public void Trigger<T>(Delegate[] delegates, string eventName, Action<T> onDele, Action onException = null) where T : Delegate
		{
			if (delegates == null)
			{
				return;
			}
			for (int i = 0; i < delegates.Length; i++)
			{
				T dele = (T)((object)delegates[i]);
				try
				{
					onDele(dele);
				}
				catch (Exception ex)
				{
					ServerMain.Logger.Error("Mod exception during event " + eventName + ". Will skip to next event");
					ServerMain.Logger.Error(ex);
					if (onException != null)
					{
						onException();
					}
				}
			}
		}

		public void Trigger<T>(Delegate[] delegates, string eventName, ActionBoolReturn<T> onDele, Action onException = null) where T : Delegate
		{
			if (delegates == null)
			{
				return;
			}
			for (int i = 0; i < delegates.Length; i++)
			{
				T dele = (T)((object)delegates[i]);
				try
				{
					if (!onDele(dele))
					{
						break;
					}
				}
				catch (Exception ex)
				{
					ServerMain.Logger.Error("Mod exception during event " + eventName + ". Will skip to next event");
					ServerMain.Logger.Error(ex);
					if (onException != null)
					{
						onException();
					}
				}
			}
		}

		public WorldGenHandler GetWorldGenHandler(string worldType)
		{
			WorldGenHandler handler;
			this.WorldgenHandlers.TryGetValue(worldType, out handler);
			return handler;
		}

		public WorldGenHandler GetOrCreateWorldGenHandler(string worldType)
		{
			WorldGenHandler handler;
			if (!this.WorldgenHandlers.TryGetValue(worldType, out handler))
			{
				handler = (this.WorldgenHandlers[worldType] = new WorldGenHandler());
			}
			return handler;
		}

		public void WipeAllDelegates()
		{
			this.serverRunPhaseDelegates.Clear();
			this.AssetsFirstLoaded = null;
			this.AssetsFinalizer = null;
			this.OnSaveGameLoaded = null;
			this.OnGameWorldBeingSaved = null;
			this.DidUseBlock = null;
			this.DidPlaceBlock = null;
			this.DidBreakBlock = null;
			this.OnPlayerRespawn = null;
			this.OnPlayerCreate = null;
			this.OnPlayerJoin = null;
			this.OnPlayerNowPlaying = null;
			this.OnPlayerLeave = null;
			this.OnPlayerDisconnect = null;
			this.OnPlayerChat = null;
			this.OnPlayerDeath = null;
			this.OnEntityLoaded = null;
			this.OnEntityDespawn = null;
			this.OnEntitySpawn = null;
			this.OnTrySpawnEntity = null;
			this.OnPlayerInteractEntity = null;
			this.CanUseBlock = null;
			this.CanPlaceOrBreakBlock = null;
			this.GameTickListenersEntity.Clear();
			this.DelayedCallbacksEntity.Clear();
			this.GameTickListenersBlock.Clear();
			this.DelayedCallbacksBlock.Clear();
			this.Logger.ClearWatchers();
			this.OnPlayerChangeGamemode = null;
			this.BeforeActiveSlotChanged = null;
			this.AfterActiveSlotChanged = null;
			foreach (KeyValuePair<string, WorldGenHandler> val in this.WorldgenHandlers)
			{
				val.Value.WipeAllHandlers();
			}
			this.Init();
		}

		public override long AddGameTickListener(Action<float> handler, int millisecondInterval, int initialDelayOffsetMs = 0)
		{
			return this.AddGameTickListener(handler, null, millisecondInterval, initialDelayOffsetMs);
		}

		public override long AddGameTickListener(Action<float> handler, Action<Exception> errorHandler, int millisecondInterval, int initialDelayOffsetMs = 0)
		{
			return base.AddGameTickListener(handler, errorHandler, millisecondInterval, initialDelayOffsetMs);
		}

		public override long AddDelayedCallback(Action<IWorldAccessor, BlockPos, float> handler, BlockPos pos, long callAfterEllapsedMS)
		{
			return base.AddDelayedCallback(handler, pos, callAfterEllapsedMS);
		}

		internal override long AddSingleDelayedCallback(Action<IWorldAccessor, BlockPos, float> handler, BlockPos pos, long callAfterEllapsedMs)
		{
			return base.AddSingleDelayedCallback(handler, pos, callAfterEllapsedMs);
		}

		internal ServerMain server;

		public Dictionary<EnumServerRunPhase, List<Action>> serverRunPhaseDelegates;

		public List<WorldGenThreadDelegate> WorldgenBlockAccessor = new List<WorldGenThreadDelegate>();

		public Dictionary<string, WorldGenHandler> WorldgenHandlers = new Dictionary<string, WorldGenHandler>();

		public List<EventBusListener> EventBusListeners = new List<EventBusListener>();
	}
}
