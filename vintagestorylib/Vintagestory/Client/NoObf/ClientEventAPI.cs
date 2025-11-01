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
	public class ClientEventAPI : IClientEventAPI, IEventAPI
	{
		public event MouseEventDelegate MouseDown;

		public event MouseEventDelegate MouseUp;

		public event MouseEventDelegate MouseMove;

		public event KeyEventDelegate KeyDown;

		public event KeyEventDelegate KeyUp;

		public event FileDropDelegate FileDrop;

		public event PlayerEventDelegate PlayerJoin;

		public event PlayerEventDelegate PlayerLeave;

		public event PlayerEventDelegate PlayerEntitySpawn;

		public event PlayerEventDelegate PlayerEntityDespawn;

		public event Action BlockTexturesLoaded;

		public event IsPlayerReadyDelegate IsPlayerReady;

		public event OnGamePauseResume PauseResume;

		public event Action LeaveWorld;

		public event Action LeftWorld;

		public event ChatLineDelegate ChatMessage;

		public event BlockChangedDelegate BlockChanged;

		public event Func<ActiveSlotChangeEventArgs, EnumHandling> BeforeActiveSlotChanged;

		public event Action<ActiveSlotChangeEventArgs> AfterActiveSlotChanged;

		public event ChunkDirtyDelegate ChunkDirty;

		public event Action LevelFinalize;

		public event Action HotkeysChanged;

		public event ClientChatLineDelegate OnSendChatMessage;

		public event EntityDeathDelegate OnEntityDeath;

		public event MatchGridRecipeDelegate MatchesGridRecipe;

		public event PlayerEventDelegate PlayerDeath;

		public event TestBlockAccessDelegate OnTestBlockAccess;

		public event TestBlockAccessClaimDelegate OnTestBlockAccessClaim;

		public event MapRegionLoadedDelegate MapRegionLoaded;

		public event MapRegionUnloadDelegate MapRegionUnloaded;

		public event PlayerCommonDelegate PlayerDimensionChanged;

		public event EntityMountDelegate EntityMounted;

		public event EntityMountDelegate EntityUnmounted;

		public event OnGetClimateDelegate OnGetClimate
		{
			add
			{
				ClientEventManager em = this.game.eventManager;
				if (em != null)
				{
					em.OnGetClimate += value;
				}
			}
			remove
			{
				ClientEventManager em = this.game.eventManager;
				if (em != null)
				{
					em.OnGetClimate -= value;
				}
			}
		}

		public event TestBlockAccessDelegate TestBlockAccess
		{
			add
			{
				this.OnTestBlockAccess += value;
			}
			remove
			{
				this.OnTestBlockAccess -= value;
			}
		}

		public event OnGetWindSpeedDelegate OnGetWindSpeed
		{
			add
			{
				ClientEventManager em = this.game.eventManager;
				if (em != null)
				{
					em.OnGetWindSpeed += value;
				}
			}
			remove
			{
				ClientEventManager em = this.game.eventManager;
				if (em != null)
				{
					em.OnGetWindSpeed -= value;
				}
			}
		}

		public event IngameErrorDelegate InGameError
		{
			add
			{
				ClientEventManager em = this.game.eventManager;
				if (em != null)
				{
					em.InGameError += value;
				}
			}
			remove
			{
				ClientEventManager em = this.game.eventManager;
				if (em != null)
				{
					em.InGameError -= value;
				}
			}
		}

		public event IngameDiscoveryDelegate InGameDiscovery
		{
			add
			{
				ClientEventManager em = this.game.eventManager;
				if (em != null)
				{
					em.InGameDiscovery += value;
				}
			}
			remove
			{
				ClientEventManager em = this.game.eventManager;
				if (em != null)
				{
					em.InGameDiscovery -= value;
				}
			}
		}

		public event Action ColorsPresetChanged
		{
			add
			{
				ClientEventManager em = this.game.eventManager;
				if (em != null)
				{
					em.ColorPresetChanged += value;
				}
			}
			remove
			{
				ClientEventManager em = this.game.eventManager;
				if (em != null)
				{
					em.ColorPresetChanged -= value;
				}
			}
		}

		public event EntityDelegate OnEntitySpawn
		{
			add
			{
				ClientEventManager eventManager = this.game.eventManager;
				if (eventManager == null)
				{
					return;
				}
				eventManager.OnEntitySpawn.Add(value);
			}
			remove
			{
				ClientEventManager eventManager = this.game.eventManager;
				if (eventManager == null)
				{
					return;
				}
				eventManager.OnEntitySpawn.Remove(value);
			}
		}

		public event EntityDelegate OnEntityLoaded
		{
			add
			{
				ClientEventManager eventManager = this.game.eventManager;
				if (eventManager == null)
				{
					return;
				}
				eventManager.OnEntityLoaded.Add(value);
			}
			remove
			{
				ClientEventManager eventManager = this.game.eventManager;
				if (eventManager == null)
				{
					return;
				}
				eventManager.OnEntityLoaded.Remove(value);
			}
		}

		public event EntityDespawnDelegate OnEntityDespawn
		{
			add
			{
				ClientEventManager eventManager = this.game.eventManager;
				if (eventManager == null)
				{
					return;
				}
				eventManager.OnEntityDespawn.Add(value);
			}
			remove
			{
				ClientEventManager eventManager = this.game.eventManager;
				if (eventManager == null)
				{
					return;
				}
				eventManager.OnEntityDespawn.Remove(value);
			}
		}

		public event ActionBoolReturn ReloadShader
		{
			add
			{
				ClientEventManager eventManager = this.game.eventManager;
				if (eventManager == null)
				{
					return;
				}
				eventManager.OnReloadShaders.Add(value);
			}
			remove
			{
				ClientEventManager eventManager = this.game.eventManager;
				if (eventManager == null)
				{
					return;
				}
				eventManager.OnReloadShaders.Remove(value);
			}
		}

		public event Action ReloadTextures
		{
			add
			{
				ClientEventManager em = this.game.eventManager;
				if (em != null)
				{
					em.OnReloadTextures += value;
				}
			}
			remove
			{
				ClientEventManager em = this.game.eventManager;
				if (em != null)
				{
					em.OnReloadTextures -= value;
				}
			}
		}

		public event Action ReloadShapes
		{
			add
			{
				ClientEventManager em = this.game.eventManager;
				if (em != null)
				{
					em.OnReloadShapes += value;
				}
			}
			remove
			{
				ClientEventManager em = this.game.eventManager;
				if (em != null)
				{
					em.OnReloadShapes -= value;
				}
			}
		}

		public ClientEventAPI(ClientMain game)
		{
			this.game = game;
			ClientEventManager eventManager = game.eventManager;
			if (eventManager != null)
			{
				eventManager.OnNewServerToClientChatLine.Add(new ChatLineDelegate(this.onChatLine));
			}
			ClientEventManager eventManager2 = game.eventManager;
			if (eventManager2 != null)
			{
				eventManager2.OnPlayerDeath.Add(new PlayerDeathDelegate(this.playerDeath));
			}
			int len = Enum.GetNames(typeof(EnumItemRenderTarget)).Length;
			this.itemStackRenderersByTarget = new Dictionary<int, ItemRenderDelegate>[2][];
			this.itemStackRenderersByTarget[0] = new Dictionary<int, ItemRenderDelegate>[len];
			this.itemStackRenderersByTarget[1] = new Dictionary<int, ItemRenderDelegate>[len];
			for (int i = 0; i < len; i++)
			{
				this.itemStackRenderersByTarget[0][i] = new Dictionary<int, ItemRenderDelegate>();
				this.itemStackRenderersByTarget[1][i] = new Dictionary<int, ItemRenderDelegate>();
			}
		}

		private void playerDeath(int clientid, int livesLeft)
		{
			PlayerEventDelegate playerDeath = this.PlayerDeath;
			if (playerDeath == null)
			{
				return;
			}
			playerDeath(this.game.AllPlayers.FirstOrDefault((IPlayer plr) => plr.ClientId == clientid) as IClientPlayer);
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

		public void TriggerMapregionLoaded(Vec2i mapCoord, IMapRegion mapregion)
		{
			MapRegionLoadedDelegate mapRegionLoaded = this.MapRegionLoaded;
			if (mapRegionLoaded == null)
			{
				return;
			}
			mapRegionLoaded(mapCoord, mapregion);
		}

		public void TriggerMapregionUnloaded(Vec2i mapCoord, IMapRegion mapregion)
		{
			MapRegionUnloadDelegate mapRegionUnloaded = this.MapRegionUnloaded;
			if (mapRegionUnloaded == null)
			{
				return;
			}
			mapRegionUnloaded(mapCoord, mapregion);
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

		private void onChatLine(int groupId, string message, EnumChatType chattype, string data)
		{
			ChatLineDelegate chatMessage = this.ChatMessage;
			if (chatMessage == null)
			{
				return;
			}
			chatMessage(groupId, message, chattype, data);
		}

		public void TriggerBlockChanged(BlockPos pos, Block oldBlock)
		{
			BlockChangedDelegate blockChanged = this.BlockChanged;
			if (blockChanged == null)
			{
				return;
			}
			blockChanged(pos, oldBlock);
		}

		public void TriggerPlayerJoin(IClientPlayer plr)
		{
			PlayerEventDelegate playerJoin = this.PlayerJoin;
			if (playerJoin == null)
			{
				return;
			}
			playerJoin(plr);
		}

		public void TriggerPlayerLeave(IClientPlayer plr)
		{
			PlayerEventDelegate playerLeave = this.PlayerLeave;
			if (playerLeave == null)
			{
				return;
			}
			playerLeave(plr);
		}

		public void TriggerPlayerEntitySpawn(IClientPlayer plr)
		{
			PlayerEventDelegate playerEntitySpawn = this.PlayerEntitySpawn;
			if (playerEntitySpawn == null)
			{
				return;
			}
			playerEntitySpawn(plr);
		}

		public void TriggerPlayerEntityDespawn(IClientPlayer plr)
		{
			PlayerEventDelegate playerEntityDespawn = this.PlayerEntityDespawn;
			if (playerEntityDespawn == null)
			{
				return;
			}
			playerEntityDespawn(plr);
		}

		public bool TriggerFileDrop(string filename)
		{
			FileDropEvent ev = new FileDropEvent
			{
				Filename = filename
			};
			Delegate[] invocationList = this.FileDrop.GetInvocationList();
			for (int i = 0; i < invocationList.Length; i++)
			{
				((FileDropDelegate)invocationList[i])(ev);
				if (ev.Handled)
				{
					break;
				}
			}
			return ev.Handled;
		}

		public void TriggerPauseResume(bool pause)
		{
			OnGamePauseResume pauseResume = this.PauseResume;
			if (pauseResume == null)
			{
				return;
			}
			pauseResume(pause);
		}

		public void TriggerMouseDown(MouseEvent ev)
		{
			if (this.MouseDown == null)
			{
				return;
			}
			Delegate[] invocationList = this.MouseDown.GetInvocationList();
			for (int i = 0; i < invocationList.Length; i++)
			{
				((MouseEventDelegate)invocationList[i])(ev);
				if (ev.Handled)
				{
					break;
				}
			}
		}

		public void TriggerMouseUp(MouseEvent ev)
		{
			if (this.MouseUp == null)
			{
				return;
			}
			Delegate[] invocationList = this.MouseUp.GetInvocationList();
			for (int i = 0; i < invocationList.Length; i++)
			{
				((MouseEventDelegate)invocationList[i])(ev);
				if (ev.Handled)
				{
					break;
				}
			}
		}

		public void TriggerMouseMove(MouseEvent ev)
		{
			if (this.MouseMove == null)
			{
				return;
			}
			Delegate[] invocationList = this.MouseMove.GetInvocationList();
			for (int i = 0; i < invocationList.Length; i++)
			{
				((MouseEventDelegate)invocationList[i])(ev);
				if (ev.Handled)
				{
					break;
				}
			}
		}

		public void TriggerKeyDown(KeyEvent ev)
		{
			if (this.KeyDown == null)
			{
				return;
			}
			Delegate[] invocationList = this.KeyDown.GetInvocationList();
			for (int i = 0; i < invocationList.Length; i++)
			{
				((KeyEventDelegate)invocationList[i])(ev);
				if (ev.Handled)
				{
					break;
				}
			}
		}

		public void TriggerKeyUp(KeyEvent ev)
		{
			if (this.KeyUp == null)
			{
				return;
			}
			Delegate[] invocationList = this.KeyUp.GetInvocationList();
			for (int i = 0; i < invocationList.Length; i++)
			{
				((KeyEventDelegate)invocationList[i])(ev);
				if (ev.Handled)
				{
					break;
				}
			}
		}

		internal bool TriggerBeforeActiveSlotChanged(ILogger logger, int fromSlot, int toSlot)
		{
			ActiveSlotChangeEventArgs args = new ActiveSlotChangeEventArgs(fromSlot, toSlot);
			Func<ActiveSlotChangeEventArgs, EnumHandling> beforeActiveSlotChanged = this.BeforeActiveSlotChanged;
			return beforeActiveSlotChanged == null || beforeActiveSlotChanged.InvokeSafeCancellable(logger, "BeforeActiveSlotChanged", args);
		}

		internal void TriggerAfterActiveSlotChanged(ILogger logger, int fromSlot, int toSlot)
		{
			ActiveSlotChangeEventArgs args = new ActiveSlotChangeEventArgs(fromSlot, toSlot);
			Action<ActiveSlotChangeEventArgs> afterActiveSlotChanged = this.AfterActiveSlotChanged;
			if (afterActiveSlotChanged == null)
			{
				return;
			}
			afterActiveSlotChanged.InvokeSafe(logger, "AfterActiveSlotChanged", args);
		}

		public void TriggerLevelFinalize()
		{
			Action levelFinalize = this.LevelFinalize;
			if (levelFinalize == null)
			{
				return;
			}
			levelFinalize();
		}

		public void TriggerLeaveWorld()
		{
			Action leaveWorld = this.LeaveWorld;
			if (leaveWorld == null)
			{
				return;
			}
			leaveWorld();
		}

		public void TriggerLeftWorld()
		{
			Action leftWorld = this.LeftWorld;
			if (leftWorld == null)
			{
				return;
			}
			leftWorld();
		}

		public void TriggerHotkeysChanged()
		{
			Action hotkeysChanged = this.HotkeysChanged;
			if (hotkeysChanged == null)
			{
				return;
			}
			hotkeysChanged();
		}

		public void TriggerBlockTexturesLoaded()
		{
			Action blockTexturesLoaded = this.BlockTexturesLoaded;
			if (blockTexturesLoaded == null)
			{
				return;
			}
			blockTexturesLoaded();
		}

		public bool TriggerIsPlayerReady()
		{
			if (this.IsPlayerReady == null)
			{
				return true;
			}
			EnumHandling handling = EnumHandling.PassThrough;
			bool ok = true;
			Delegate[] invocationList = this.IsPlayerReady.GetInvocationList();
			for (int i = 0; i < invocationList.Length; i++)
			{
				bool hereOk = ((IsPlayerReadyDelegate)invocationList[i])(ref handling);
				if (handling != EnumHandling.PassThrough)
				{
					ok = ok && hereOk;
				}
				if (handling == EnumHandling.PreventSubsequent)
				{
					break;
				}
			}
			return ok;
		}

		public void RegisterRenderer(IRenderer renderer, EnumRenderStage stage, string profilingName = null)
		{
			if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
			{
				throw new InvalidOperationException("Cannot call this method outside the main thread. Not thread safe to do so.");
			}
			ClientEventManager eventManager = this.game.eventManager;
			if (eventManager == null)
			{
				return;
			}
			eventManager.RegisterRenderer(renderer, stage, profilingName);
		}

		public void RegisterRenderer(IRenderer renderer, EnumRenderStage renderStage, string profilingName, double reservedFirstOrder, double reservedLastOrder, Type firstType)
		{
			this.RegisterRenderer(renderer, renderStage, profilingName);
			ClientEventManager eventManager = this.game.eventManager;
			if (eventManager == null)
			{
				return;
			}
			eventManager.ReserveRenderOrderRange(renderer, renderStage, reservedFirstOrder, reservedLastOrder, firstType);
		}

		public void RegisterItemstackRenderer(CollectibleObject forObj, ItemRenderDelegate rendererDelegate, EnumItemRenderTarget target)
		{
			if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
			{
				throw new InvalidOperationException("Cannot call this method outside the main thread. Not thread safe to do so.");
			}
			this.itemStackRenderersByTarget[(int)forObj.ItemClass][(int)target][forObj.Id] = rendererDelegate;
		}

		public void UnregisterItemstackRenderer(CollectibleObject forObj, EnumItemRenderTarget target)
		{
			if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
			{
				throw new InvalidOperationException("Cannot call this method outside the main thread. Not thread safe to do so.");
			}
			this.itemStackRenderersByTarget[(int)forObj.ItemClass][(int)target].Remove(forObj.Id);
		}

		public void UnregisterRenderer(IRenderer renderer, EnumRenderStage stage)
		{
			if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
			{
				throw new InvalidOperationException("Cannot call this method outside the main thread. Not thread safe to do so.");
			}
			ClientEventManager eventManager = this.game.eventManager;
			if (eventManager == null)
			{
				return;
			}
			eventManager.UnregisterRenderer(renderer, stage);
		}

		public void RegisterReloadShapes(Action handler)
		{
			ClientEventManager em = this.game.eventManager;
			if (em != null)
			{
				em.OnReloadShapes += handler;
			}
		}

		public void UnregisterReloadShapes(Action handler)
		{
			ClientEventManager em = this.game.eventManager;
			if (em != null)
			{
				em.OnReloadShapes -= handler;
			}
		}

		public void RegisterOnLeaveWorld(Action handler)
		{
			this.LeaveWorld += handler;
		}

		public void PushEvent(string eventName, IAttribute data = null)
		{
			EnumHandling handling = EnumHandling.PassThrough;
			int i = 0;
			for (;;)
			{
				int num = i;
				ClientEventManager eventManager = this.game.eventManager;
				int? num2 = ((eventManager != null) ? new int?(eventManager.EventBusListeners.Count) : null);
				if (!((num < num2.GetValueOrDefault()) & (num2 != null)))
				{
					break;
				}
				ClientEventManager eventManager2 = this.game.eventManager;
				EventBusListener listener = ((eventManager2 != null) ? eventManager2.EventBusListeners[i] : null);
				if (listener.filterByName == null || listener.filterByName.Equals(eventName))
				{
					listener.handler(eventName, ref handling, data);
				}
				if (handling == EnumHandling.PreventSubsequent)
				{
					break;
				}
				i++;
			}
		}

		public void RegisterEventBusListener(EventBusListenerDelegate OnEvent, double priority = 0.5, string filterByEventName = null)
		{
			if (this.game.eventManager == null)
			{
				return;
			}
			if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
			{
				throw new InvalidOperationException(ClientEventAPI.strThreadException);
			}
			int i = 0;
			while (i < this.game.eventManager.EventBusListeners.Count)
			{
				if (this.game.eventManager.EventBusListeners[i].priority < priority)
				{
					ClientEventManager eventManager = this.game.eventManager;
					if (eventManager == null)
					{
						return;
					}
					eventManager.EventBusListeners.Insert(i, new EventBusListener
					{
						handler = OnEvent,
						priority = priority,
						filterByName = filterByEventName
					});
					return;
				}
				else
				{
					i++;
				}
			}
			ClientEventManager eventManager2 = this.game.eventManager;
			if (eventManager2 == null)
			{
				return;
			}
			eventManager2.EventBusListeners.Add(new EventBusListener
			{
				handler = OnEvent,
				priority = priority,
				filterByName = filterByEventName
			});
		}

		public long RegisterGameTickListener(Action<float> OnGameTick, int millisecondInterval, int initialDelayOffsetMs = 0)
		{
			if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
			{
				throw new InvalidOperationException(ClientEventAPI.strThreadException);
			}
			return this.game.RegisterGameTickListener(OnGameTick, millisecondInterval, initialDelayOffsetMs);
		}

		public long RegisterGameTickListener(Action<float> OnGameTick, Action<Exception> errorHandler, int millisecondInterval, int initialDelayOffsetMs = 0)
		{
			if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
			{
				throw new InvalidOperationException(ClientEventAPI.strThreadException);
			}
			return this.game.RegisterGameTickListener(OnGameTick, errorHandler, millisecondInterval, initialDelayOffsetMs);
		}

		public long RegisterGameTickListener(Action<IWorldAccessor, BlockPos, float> OnGameTick, BlockPos pos, int millisecondInterval, int initialDelayOffsetMs = 0)
		{
			if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
			{
				throw new InvalidOperationException(ClientEventAPI.strThreadException);
			}
			return this.game.RegisterGameTickListener(OnGameTick, pos, millisecondInterval, initialDelayOffsetMs);
		}

		public long RegisterCallback(Action<float> OnTimePassed, int millisecondDelay)
		{
			if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
			{
				throw new InvalidOperationException(ClientEventAPI.strThreadException);
			}
			return this.game.RegisterCallback(OnTimePassed, millisecondDelay);
		}

		public long RegisterCallback(Action<float> OnTimePassed, int millisecondDelay, bool permittedWhilePaused)
		{
			if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
			{
				throw new InvalidOperationException(ClientEventAPI.strThreadException);
			}
			return this.game.RegisterCallback(OnTimePassed, millisecondDelay, permittedWhilePaused);
		}

		public long RegisterCallback(Action<IWorldAccessor, BlockPos, float> OnTimePassed, BlockPos pos, int millisecondDelay)
		{
			if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
			{
				throw new InvalidOperationException(ClientEventAPI.strThreadException);
			}
			return this.game.RegisterCallback(OnTimePassed, pos, millisecondDelay);
		}

		public void UnregisterCallback(long listenerId)
		{
			if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
			{
				throw new InvalidOperationException(ClientEventAPI.strThreadException);
			}
			this.game.UnregisterCallback(listenerId);
		}

		public void UnregisterGameTickListener(long listenerId)
		{
			if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
			{
				throw new InvalidOperationException(ClientEventAPI.strThreadException);
			}
			this.game.UnregisterGameTickListener(listenerId);
		}

		public void EnqueueMainThreadTask(Action action, string code)
		{
			this.game.EnqueueMainThreadTask(action, code);
		}

		internal void TriggerSendChatMessage(int groupId, ref string message, ref EnumHandling handling)
		{
			if (this.OnSendChatMessage == null)
			{
				return;
			}
			Delegate[] invocationList = this.OnSendChatMessage.GetInvocationList();
			for (int i = 0; i < invocationList.Length; i++)
			{
				((ClientChatLineDelegate)invocationList[i])(groupId, ref message, ref handling);
				if (handling == EnumHandling.PreventSubsequent)
				{
					break;
				}
			}
		}

		public void RegisterAsyncParticleSpawner(ContinousParticleSpawnTaskDelegate handler)
		{
			object asyncParticleSpawnersLock = this.game.asyncParticleSpawnersLock;
			lock (asyncParticleSpawnersLock)
			{
				this.game.asyncParticleSpawners.Add(handler);
			}
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

		private static string strThreadException = "Cannot call this method outside the main thread. Not thread safe to do so. You can use the thread safe method .EnqueueMainThreadTask() to queue up the register on the main thread instead.";

		private ClientMain game;

		internal Dictionary<int, ItemRenderDelegate>[][] itemStackRenderersByTarget;
	}
}
