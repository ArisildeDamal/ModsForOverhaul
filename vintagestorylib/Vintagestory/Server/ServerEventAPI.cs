using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace Vintagestory.Server
{
	internal class ServerEventAPI : ServerAPIComponentBase, IServerEventAPI, IEventAPI
	{
		public event TestBlockAccessDelegate OnTestBlockAccess;

		public event TestBlockAccessClaimDelegate OnTestBlockAccessClaim;

		public event ChunkDirtyDelegate ChunkDirty;

		public event ChunkColumnBeginLoadChunkThread BeginChunkColumnLoadChunkThread;

		public event ChunkColumnLoadedDelegate ChunkColumnLoaded;

		public event ChunkColumnUnloadDelegate ChunkColumnUnloaded;

		public event MapRegionLoadedDelegate MapRegionLoaded;

		public event MapRegionUnloadDelegate MapRegionUnloaded;

		public event EntityDeathDelegate OnEntityDeath;

		public event EntityMountDelegate EntityMounted;

		public event EntityMountDelegate EntityUnmounted;

		public event MountGaitReceivedDelegate MountGaitReceived;

		public bool TriggerTrySpawnEntity(IBlockAccessor blockAccessor, ref EntityProperties properties, Vec3d position, long herdId)
		{
			return this.server.ModEventManager.TriggerTrySpawnEntity(blockAccessor, ref properties, position, herdId);
		}

		public event TrySpawnEntityDelegate OnTrySpawnEntity
		{
			add
			{
				this.server.ModEventManager.OnTrySpawnEntity += value;
			}
			remove
			{
				this.server.ModEventManager.OnTrySpawnEntity -= value;
			}
		}

		public event OnInteractDelegate OnPlayerInteractEntity
		{
			add
			{
				this.server.ModEventManager.OnPlayerInteractEntity += value;
			}
			remove
			{
				this.server.ModEventManager.OnPlayerInteractEntity -= value;
			}
		}

		public event EntityDelegate OnEntitySpawn
		{
			add
			{
				this.server.ModEventManager.OnEntitySpawn += value;
			}
			remove
			{
				this.server.ModEventManager.OnEntitySpawn -= value;
			}
		}

		public event EntityDelegate OnEntityLoaded
		{
			add
			{
				this.server.ModEventManager.OnEntityLoaded += value;
			}
			remove
			{
				this.server.ModEventManager.OnEntityLoaded -= value;
			}
		}

		public event EntityDespawnDelegate OnEntityDespawn
		{
			add
			{
				this.server.ModEventManager.OnEntityDespawn += value;
			}
			remove
			{
				this.server.ModEventManager.OnEntityDespawn -= value;
			}
		}

		public event PlayerDelegate PlayerCreate
		{
			add
			{
				this.server.ModEventManager.OnPlayerCreate += value;
			}
			remove
			{
				this.server.ModEventManager.OnPlayerCreate -= value;
			}
		}

		public event PlayerDelegate PlayerRespawn
		{
			add
			{
				this.server.ModEventManager.OnPlayerRespawn += value;
			}
			remove
			{
				this.server.ModEventManager.OnPlayerRespawn -= value;
			}
		}

		public event PlayerDelegate PlayerJoin
		{
			add
			{
				this.server.ModEventManager.OnPlayerJoin += value;
			}
			remove
			{
				this.server.ModEventManager.OnPlayerJoin -= value;
			}
		}

		public event PlayerDelegate PlayerNowPlaying
		{
			add
			{
				this.server.ModEventManager.OnPlayerNowPlaying += value;
			}
			remove
			{
				this.server.ModEventManager.OnPlayerNowPlaying -= value;
			}
		}

		public event PlayerDelegate PlayerLeave
		{
			add
			{
				this.server.ModEventManager.OnPlayerLeave += value;
			}
			remove
			{
				this.server.ModEventManager.OnPlayerLeave -= value;
			}
		}

		public event PlayerDelegate PlayerDisconnect
		{
			add
			{
				this.server.ModEventManager.OnPlayerDisconnect += value;
			}
			remove
			{
				this.server.ModEventManager.OnPlayerDisconnect -= value;
			}
		}

		public event PlayerChatDelegate PlayerChat
		{
			add
			{
				this.server.ModEventManager.OnPlayerChat += value;
			}
			remove
			{
				this.server.ModEventManager.OnPlayerChat -= value;
			}
		}

		public event PlayerDeathDelegate PlayerDeath
		{
			add
			{
				this.server.ModEventManager.OnPlayerDeath += value;
			}
			remove
			{
				this.server.ModEventManager.OnPlayerDeath -= value;
			}
		}

		public event PlayerDelegate PlayerSwitchGameMode
		{
			add
			{
				this.server.ModEventManager.OnPlayerChangeGamemode += value;
			}
			remove
			{
				this.server.ModEventManager.OnPlayerChangeGamemode -= value;
			}
		}

		public event Func<IServerPlayer, ActiveSlotChangeEventArgs, EnumHandling> BeforeActiveSlotChanged
		{
			add
			{
				this.server.ModEventManager.BeforeActiveSlotChanged += value;
			}
			remove
			{
				this.server.ModEventManager.BeforeActiveSlotChanged -= value;
			}
		}

		public event Action<IServerPlayer, ActiveSlotChangeEventArgs> AfterActiveSlotChanged
		{
			add
			{
				this.server.ModEventManager.AfterActiveSlotChanged += value;
			}
			remove
			{
				this.server.ModEventManager.AfterActiveSlotChanged -= value;
			}
		}

		public event BlockPlacedDelegate DidPlaceBlock
		{
			add
			{
				this.server.ModEventManager.DidPlaceBlock += value;
			}
			remove
			{
				this.server.ModEventManager.DidPlaceBlock -= value;
			}
		}

		public event BlockBrokenDelegate DidBreakBlock
		{
			add
			{
				this.server.ModEventManager.DidBreakBlock += value;
			}
			remove
			{
				this.server.ModEventManager.DidBreakBlock -= value;
			}
		}

		public event BlockBreakDelegate BreakBlock
		{
			add
			{
				this.server.ModEventManager.BreakBlock += value;
			}
			remove
			{
				this.server.ModEventManager.BreakBlock -= value;
			}
		}

		public event BlockUsedDelegate DidUseBlock
		{
			add
			{
				this.server.ModEventManager.DidUseBlock += value;
			}
			remove
			{
				this.server.ModEventManager.DidUseBlock -= value;
			}
		}

		public event CanUseDelegate CanUseBlock
		{
			add
			{
				this.server.ModEventManager.CanUseBlock += value;
			}
			remove
			{
				this.server.ModEventManager.CanUseBlock -= value;
			}
		}

		public event CanPlaceOrBreakDelegate CanPlaceOrBreakBlock
		{
			add
			{
				this.server.ModEventManager.CanPlaceOrBreakBlock += value;
			}
			remove
			{
				this.server.ModEventManager.CanPlaceOrBreakBlock -= value;
			}
		}

		public event MatchGridRecipeDelegate MatchesGridRecipe;

		public event SuspendServerDelegate ServerSuspend;

		public event ResumeServerDelegate ServerResume;

		public event PlayerCommonDelegate PlayerDimensionChanged;

		public event OnGetClimateDelegate OnGetClimate
		{
			add
			{
				this.server.ModEventManager.OnGetClimate += value;
			}
			remove
			{
				this.server.ModEventManager.OnGetClimate -= value;
			}
		}

		public event OnGetWindSpeedDelegate OnGetWindSpeed
		{
			add
			{
				this.server.ModEventManager.OnGetWindSpeed += value;
			}
			remove
			{
				this.server.ModEventManager.OnGetWindSpeed -= value;
			}
		}

		public ServerEventAPI(ServerMain server)
			: base(server)
		{
		}

		event Action IServerEventAPI.SaveGameLoaded
		{
			add
			{
				this.server.ModEventManager.OnSaveGameLoaded += value;
			}
			remove
			{
				this.server.ModEventManager.OnSaveGameLoaded -= value;
			}
		}

		event Action IServerEventAPI.SaveGameCreated
		{
			add
			{
				this.server.ModEventManager.OnSaveGameCreated += value;
			}
			remove
			{
				this.server.ModEventManager.OnSaveGameCreated -= value;
			}
		}

		event Action IServerEventAPI.WorldgenStartup
		{
			add
			{
				this.server.ModEventManager.OnWorldgenStartup += value;
			}
			remove
			{
				this.server.ModEventManager.OnWorldgenStartup -= value;
			}
		}

		event Action IServerEventAPI.PhysicsThreadStart
		{
			add
			{
				this.server.EventManager.OnStartPhysicsThread += value;
			}
			remove
			{
				this.server.EventManager.OnStartPhysicsThread -= value;
			}
		}

		event Action IServerEventAPI.AssetsFinalizers
		{
			add
			{
				this.server.ModEventManager.AssetsFinalizer += value;
			}
			remove
			{
				this.server.ModEventManager.AssetsFinalizer -= value;
			}
		}

		event Action IServerEventAPI.GameWorldSave
		{
			add
			{
				this.server.ModEventManager.OnGameWorldBeingSaved += value;
			}
			remove
			{
				this.server.ModEventManager.OnGameWorldBeingSaved -= value;
			}
		}

		public IWorldGenHandler GetRegisteredWorldGenHandlers(string worldType)
		{
			WorldGenHandler handler;
			this.server.ModEventManager.WorldgenHandlers.TryGetValue(worldType, out handler);
			return handler;
		}

		public bool CanSuspendServer()
		{
			bool canSuspend = true;
			if (this.ServerSuspend == null)
			{
				return true;
			}
			Delegate[] invocationList = this.ServerSuspend.GetInvocationList();
			for (int i = 0; i < invocationList.Length; i++)
			{
				if (((SuspendServerDelegate)invocationList[i])() == EnumSuspendState.Wait)
				{
					canSuspend = false;
				}
			}
			return canSuspend;
		}

		public void ResumeServer()
		{
			ResumeServerDelegate serverResume = this.ServerResume;
			if (serverResume == null)
			{
				return;
			}
			serverResume();
		}

		internal void OnServerStage(EnumServerRunPhase runPhase)
		{
			foreach (Action action in this.server.ModEventManager.serverRunPhaseDelegates[runPhase])
			{
				action();
			}
		}

		public EnumWorldAccessResponse TriggerTestBlockAccess(IPlayer player, BlockSelection blockSel, EnumBlockAccessFlags accessType, ref string claimant, LandClaim claim, EnumWorldAccessResponse response)
		{
			if (this.OnTestBlockAccess != null)
			{
				Delegate[] array = this.OnTestBlockAccess.GetInvocationList();
				for (int i = 0; i < array.Length; i++)
				{
					response = ((TestBlockAccessDelegate)array[i])(player, blockSel, accessType, ref claimant, response);
				}
			}
			if (this.OnTestBlockAccessClaim != null)
			{
				Delegate[] array = this.OnTestBlockAccessClaim.GetInvocationList();
				for (int i = 0; i < array.Length; i++)
				{
					response = ((TestBlockAccessClaimDelegate)array[i])(player, blockSel, accessType, ref claimant, claim, response);
				}
			}
			return response;
		}

		public void TriggerChunkDirty(Vec3i chunkCoord, IWorldChunk chunk, EnumChunkDirtyReason reason)
		{
			ChunkDirtyDelegate chunkDirty = this.ChunkDirty;
			if (chunkDirty == null)
			{
				return;
			}
			chunkDirty(chunkCoord, chunk, reason);
		}

		public void TriggerBeginChunkColumnLoadChunkThread(IServerMapChunk mapChunk, int chunkX, int chunkZ, IWorldChunk[] chunks)
		{
			ChunkColumnBeginLoadChunkThread beginChunkColumnLoadChunkThread = this.BeginChunkColumnLoadChunkThread;
			if (beginChunkColumnLoadChunkThread == null)
			{
				return;
			}
			beginChunkColumnLoadChunkThread(mapChunk, chunkX, chunkZ, chunks);
		}

		public void TriggerChunkColumnLoaded(Vec2i chunkCoord, IWorldChunk[] chunks)
		{
			ChunkColumnLoadedDelegate chunkColumnLoaded = this.ChunkColumnLoaded;
			if (chunkColumnLoaded == null)
			{
				return;
			}
			chunkColumnLoaded(chunkCoord, chunks);
		}

		public void TriggerChunkColumnUnloaded(Vec3i chunkCoord)
		{
			ChunkColumnUnloadDelegate chunkColumnUnloaded = this.ChunkColumnUnloaded;
			if (chunkColumnUnloaded == null)
			{
				return;
			}
			chunkColumnUnloaded(chunkCoord);
		}

		public void TriggerMapRegionLoaded(Vec2i mapCoord, IMapRegion region)
		{
			MapRegionLoadedDelegate mapRegionLoaded = this.MapRegionLoaded;
			if (mapRegionLoaded == null)
			{
				return;
			}
			mapRegionLoaded(mapCoord, region);
		}

		public void TriggerMapRegionUnloaded(Vec2i mapCoord, IMapRegion region)
		{
			MapRegionUnloadDelegate mapRegionUnloaded = this.MapRegionUnloaded;
			if (mapRegionUnloaded == null)
			{
				return;
			}
			mapRegionUnloaded(mapCoord, region);
		}

		public void Timer(Action a, double interval)
		{
			this.server.Timers[new Timer
			{
				Interval = interval
			}] = delegate
			{
				a();
			};
		}

		public void GetWorldgenBlockAccessor(WorldGenThreadDelegate f)
		{
			this.server.ModEventManager.WorldgenBlockAccessor.Add(f);
		}

		public void MapRegionGeneration(MapRegionGeneratorDelegate handler, string worldType)
		{
			this.server.ModEventManager.GetOrCreateWorldGenHandler(worldType).OnMapRegionGen.Add(handler);
		}

		public void MapChunkGeneration(MapChunkGeneratorDelegate handler, string worldType)
		{
			this.server.ModEventManager.GetOrCreateWorldGenHandler(worldType).OnMapChunkGen.Add(handler);
		}

		public void ChunkColumnGeneration(ChunkColumnGenerationDelegate handler, EnumWorldGenPass pass, string worldType)
		{
			this.server.ModEventManager.GetWorldGenHandler(worldType).OnChunkColumnGen[(int)pass].Add(handler);
		}

		public void InitWorldGenerator(Action handler, string worldType)
		{
			this.server.ModEventManager.GetOrCreateWorldGenHandler(worldType).OnInitWorldGen.Add(handler);
		}

		public object TriggerInitWorldGen()
		{
			WorldGenHandler worldgenHandler;
			this.server.ModEventManager.WorldgenHandlers.TryGetValue(this.server.SaveGameData.WorldType, out worldgenHandler);
			if (worldgenHandler == null)
			{
				this.server.api.Logger.Error("This save game requires world generator " + this.server.SaveGameData.WorldType + " but no such generator was found! No terrain will generate!");
				worldgenHandler = new WorldGenHandler();
			}
			foreach (Action val in worldgenHandler.OnInitWorldGen)
			{
				try
				{
					this.server.api.Logger.VerboseDebug("Init worldgen for " + val.Target.GetType().Name);
					val();
				}
				catch (Exception e)
				{
					this.server.api.Logger.Error("Error during Init worldgen for " + val.Target.GetType().FullName);
					this.server.api.Logger.Error(e);
				}
			}
			this.server.api.Logger.VerboseDebug("Done all worldgens");
			return worldgenHandler;
		}

		public void WorldgenHook(WorldGenHookDelegate handler, string worldType, string hook)
		{
			this.server.ModEventManager.GetOrCreateWorldGenHandler(worldType).SpecialHooks[hook] = handler;
		}

		public void TriggerWorldgenHook(string hook, IBlockAccessor blockAccessor, BlockPos pos, string param)
		{
			WorldGenHandler worldgenHandler;
			this.server.ModEventManager.WorldgenHandlers.TryGetValue(this.server.SaveGameData.WorldType, out worldgenHandler);
			WorldGenHookDelegate handler;
			if (worldgenHandler != null && worldgenHandler.SpecialHooks.TryGetValue(hook, out handler))
			{
				handler(blockAccessor, pos, param);
			}
		}

		public void ServerRunPhase(EnumServerRunPhase runPhase, Action action)
		{
			this.server.ModEventManager.serverRunPhaseDelegates[runPhase].Add(action);
		}

		public void PushEvent(string eventName, IAttribute data = null)
		{
			EnumHandling handling = EnumHandling.PassThrough;
			for (int i = 0; i < this.server.ModEventManager.EventBusListeners.Count; i++)
			{
				EventBusListener listener = this.server.ModEventManager.EventBusListeners[i];
				if (listener.filterByName == null || listener.filterByName.Equals(eventName))
				{
					listener.handler(eventName, ref handling, data);
				}
				if (handling == EnumHandling.PreventSubsequent)
				{
					break;
				}
			}
		}

		public void RegisterEventBusListener(EventBusListenerDelegate OnEvent, double priority = 0.5, string filterByEventName = null)
		{
			for (int i = 0; i < this.server.ModEventManager.EventBusListeners.Count; i++)
			{
				if (this.server.ModEventManager.EventBusListeners[i].priority < priority)
				{
					this.server.ModEventManager.EventBusListeners.Insert(i, new EventBusListener
					{
						handler = OnEvent,
						priority = priority,
						filterByName = filterByEventName
					});
					return;
				}
			}
			this.server.ModEventManager.EventBusListeners.Add(new EventBusListener
			{
				handler = OnEvent,
				priority = priority,
				filterByName = filterByEventName
			});
		}

		public long RegisterGameTickListener(Action<float> OnGameTick, int millisecondInterval, int initialDelayOffsetMs = 0)
		{
			return this.server.RegisterGameTickListener(OnGameTick, millisecondInterval, initialDelayOffsetMs);
		}

		public long RegisterGameTickListener(Action<float> OnGameTick, Action<Exception> errorHandler, int millisecondInterval, int initialDelayOffsetMs = 0)
		{
			return this.server.RegisterGameTickListener(OnGameTick, errorHandler, millisecondInterval, initialDelayOffsetMs);
		}

		public long RegisterGameTickListener(Action<IWorldAccessor, BlockPos, float> OnGameTick, BlockPos pos, int millisecondInterval, int initialDelayOffsetMs = 0)
		{
			return this.server.RegisterGameTickListener(OnGameTick, pos, millisecondInterval, initialDelayOffsetMs);
		}

		public long RegisterCallback(Action<float> OnTimePassed, int millisecondDelay)
		{
			return this.server.RegisterCallback(OnTimePassed, millisecondDelay);
		}

		public long RegisterCallback(Action<float> OnTimePassed, int millisecondDelay, bool permittedWhilePaused)
		{
			return this.server.RegisterCallback(OnTimePassed, millisecondDelay);
		}

		public long RegisterCallback(Action<IWorldAccessor, BlockPos, float> OnTimePassed, BlockPos pos, int millisecondDelay)
		{
			return this.server.RegisterCallback(OnTimePassed, pos, millisecondDelay);
		}

		public void UnregisterCallback(long listenerId)
		{
			this.server.UnregisterCallback(listenerId);
		}

		public void UnregisterGameTickListener(long listenerId)
		{
			this.server.UnregisterGameTickListener(listenerId);
		}

		public void EnqueueMainThreadTask(Action action, string code)
		{
			this.server.EnqueueMainThreadTask(action);
		}

		public void TriggerEntityDeath(Entity entity, DamageSource damageSourceForDeath)
		{
			EntityDeathDelegate onEntityDeath = this.OnEntityDeath;
			if (onEntityDeath == null)
			{
				return;
			}
			onEntityDeath(entity, damageSourceForDeath);
		}

		public bool TriggerMatchesRecipe(IPlayer forPlayer, GridRecipe gridRecipe, ItemSlot[] ingredients, int gridWidth)
		{
			return this.MatchesGridRecipe == null || this.MatchesGridRecipe(forPlayer, gridRecipe, ingredients, gridWidth);
		}

		public void TriggerPlayerDimensionChanged(IPlayer player)
		{
			PlayerCommonDelegate playerDimensionChanged = this.PlayerDimensionChanged;
			if (playerDimensionChanged == null)
			{
				return;
			}
			playerDimensionChanged(player);
		}

		public void PlayerChunkTransition(IServerPlayer player)
		{
			ServerSystemClientAwareness clientAwarenessSystem = this.server.clientAwarenessSystem;
			if (clientAwarenessSystem == null)
			{
				return;
			}
			clientAwarenessSystem.TriggerEvent(EnumClientAwarenessEvent.ChunkTransition, player.ClientId);
		}

		public void TriggerEntityMounted(EntityAgent entityAgent, IMountableSeat entityRideableSeat)
		{
			EntityMountDelegate entityMounted = this.EntityMounted;
			if (entityMounted == null)
			{
				return;
			}
			entityMounted(entityAgent, entityRideableSeat);
		}

		public void TriggerEntityUnmounted(EntityAgent entityAgent, IMountableSeat entityRideableSeat)
		{
			EntityMountDelegate entityUnmounted = this.EntityUnmounted;
			if (entityUnmounted == null)
			{
				return;
			}
			entityUnmounted(entityAgent, entityRideableSeat);
		}

		public void TriggerMountGaitReceived(Entity mountEntity, string gaitCode)
		{
			MountGaitReceivedDelegate mountGaitReceived = this.MountGaitReceived;
			if (mountGaitReceived == null)
			{
				return;
			}
			mountGaitReceived(mountEntity, gaitCode);
		}
	}
}
