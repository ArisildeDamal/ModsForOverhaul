using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf
{
	public class ClientEventManager : EventManager
	{
		public override ILogger Logger
		{
			get
			{
				return ScreenManager.Platform.Logger;
			}
		}

		public override string CommandPrefix
		{
			get
			{
				return ".";
			}
		}

		public override long InWorldEllapsedMs
		{
			get
			{
				return this.game.InWorldEllapsedMs;
			}
		}

		public event HighlightBlocksDelegate OnHighlightBlocks;

		public event UpdateLightingDelegate OnUpdateLighting;

		public event ChunkLoadedDelegate OnChunkLoaded;

		public event Action OnReloadShapes;

		public event Action OnReloadTextures;

		public event IngameErrorDelegate InGameError;

		public event IngameDiscoveryDelegate InGameDiscovery;

		public event Action ColorPresetChanged;

		public ClientEventManager(ClientMain game)
		{
			this.game = game;
			this.renderersByStage = new List<RenderHandler>[Enum.GetNames(typeof(EnumRenderStage)).Length];
			for (int i = 0; i < this.renderersByStage.Length; i++)
			{
				this.renderersByStage[i] = new List<RenderHandler>();
			}
			this.renderOrderReservationsByStage = new List<RenderOrderReservation>[this.renderersByStage.Length];
			for (int j = 0; j < this.renderOrderReservationsByStage.Length; j++)
			{
				this.renderOrderReservationsByStage[j] = new List<RenderOrderReservation>();
			}
		}

		public void RegisterRenderer(Action<float> handler, EnumRenderStage stage, string profilingName, double renderOrder)
		{
			this.RegisterRenderer(new DummyRenderer
			{
				action = handler,
				RenderOrder = renderOrder
			}, stage, profilingName);
		}

		public void RegisterRenderer(IRenderer handler, EnumRenderStage stage, string profilingName)
		{
			List<RenderHandler> renderers = this.renderersByStage[(int)stage];
			int pos = 0;
			double newRenderOrder = handler.RenderOrder;
			foreach (RenderOrderReservation reservation in this.renderOrderReservationsByStage[(int)stage])
			{
				if (reservation.Conflicts(handler, newRenderOrder))
				{
					throw new Exception(string.Concat(new string[]
					{
						"When registering ",
						handler.GetType().Name,
						", its RenderOrder fell within a range ",
						reservation.rangeStart.ToString(),
						"-",
						reservation.rangeEnd.ToString(),
						" which is already reserved"
					}));
				}
			}
			foreach (RenderHandler renderer in renderers)
			{
				if (newRenderOrder <= renderer.Renderer.RenderOrder)
				{
					break;
				}
				pos++;
			}
			renderers.Insert(pos, new RenderHandler
			{
				Renderer = handler,
				ProfilingName = stage.ToString() + "-" + profilingName
			});
		}

		public void ReserveRenderOrderRange(IRenderer renderer, EnumRenderStage stage, double reservedFirstOrder, double reservedLastOrder, Type otherType)
		{
			this.renderOrderReservationsByStage[(int)stage].Add(new RenderOrderReservation
			{
				rangeStart = reservedFirstOrder,
				rangeEnd = reservedLastOrder,
				allowedType1 = renderer.GetType(),
				allowedType2 = otherType
			});
		}

		public void UnregisterRenderer(IRenderer handler, EnumRenderStage stage)
		{
			List<RenderHandler> renderers = this.renderersByStage[(int)stage];
			RenderHandler rh = renderers.FirstOrDefault((RenderHandler x) => x.Renderer == handler);
			if (rh != null)
			{
				renderers.Remove(rh);
			}
		}

		public void TriggerHighlightBlocks(IPlayer player, int slotId, List<BlockPos> blocks, List<int> colors, EnumHighlightBlocksMode mode = EnumHighlightBlocksMode.Absolute, EnumHighlightShape shape = EnumHighlightShape.Arbitrary, float scale = 1f)
		{
			HighlightBlocksDelegate onHighlightBlocks = this.OnHighlightBlocks;
			if (onHighlightBlocks == null)
			{
				return;
			}
			onHighlightBlocks(player, slotId, blocks, colors, mode, shape, scale);
		}

		public void TriggerRenderStage(EnumRenderStage stage, float dt)
		{
			List<RenderHandler> renderers = this.renderersByStage[(int)stage];
			if (this.game.extendedDebugInfo)
			{
				for (int i = 0; i < renderers.Count; i++)
				{
					renderers[i].Renderer.OnRenderFrame(dt, stage);
					ScreenManager.Platform.CheckGlError(renderers[i].ProfilingName);
					ScreenManager.FrameProfiler.Mark(renderers[i].ProfilingName);
				}
				return;
			}
			for (int j = 0; j < renderers.Count; j++)
			{
				renderers[j].Renderer.OnRenderFrame(dt, stage);
			}
		}

		public void TriggerLightingUpdate(int oldBlockId, int newBlockId, BlockPos pos, Dictionary<BlockPos, BlockUpdate> blockUpdatesBulk = null)
		{
			UpdateLightingDelegate onUpdateLighting = this.OnUpdateLighting;
			if (onUpdateLighting == null)
			{
				return;
			}
			onUpdateLighting(oldBlockId, newBlockId, pos, blockUpdatesBulk);
		}

		public void TriggerChunkLoaded(Vec3i chunkpos)
		{
			ChunkLoadedDelegate onChunkLoaded = this.OnChunkLoaded;
			if (onChunkLoaded == null)
			{
				return;
			}
			onChunkLoaded(chunkpos);
		}

		public void RegisterReloadShaders(ActionBoolReturn handler)
		{
			this.OnReloadShaders.Add(handler);
		}

		public void UnregisterReloadShaders(ActionBoolReturn handler)
		{
			this.OnReloadShaders.Remove(handler);
		}

		public void RegisterOnChunkRetesselated(Vec3i chunkPos, int atQuantityDrawn, Action listener)
		{
			if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
			{
				this.game.EnqueueMainThreadTask(delegate
				{
					List<RetesselationListener> listeners2;
					if (!this.OnChunkRetesselated.TryGetValue(chunkPos, out listeners2))
					{
						listeners2 = new List<RetesselationListener>();
						this.OnChunkRetesselated[chunkPos] = listeners2;
					}
					if (listener == null)
					{
						throw new ArgumentNullException("Listener cannot be null");
					}
					listeners2.Add(new RetesselationListener
					{
						AtDrawCount = atQuantityDrawn,
						Handler = listener
					});
				}, "reg-chunkretess");
				return;
			}
			List<RetesselationListener> listeners;
			if (!this.OnChunkRetesselated.TryGetValue(chunkPos, out listeners))
			{
				listeners = new List<RetesselationListener>();
				this.OnChunkRetesselated[chunkPos] = listeners;
			}
			if (listener == null)
			{
				throw new ArgumentNullException("Listener cannot be null");
			}
			listeners.Add(new RetesselationListener
			{
				AtDrawCount = atQuantityDrawn,
				Handler = listener
			});
		}

		public void TriggerChunkRetesselated(Vec3i chunkPos, ClientChunk chunk)
		{
			List<RetesselationListener> listeners;
			if (this.OnChunkRetesselated.TryGetValue(chunkPos, out listeners))
			{
				int i = 0;
				try
				{
					for (i = 0; i < listeners.Count; i++)
					{
						RetesselationListener listener = listeners[i];
						if (listener.AtDrawCount < chunk.quantityDrawn)
						{
							listener.Handler();
							listeners.RemoveAt(i);
							i--;
						}
					}
				}
				catch (Exception e)
				{
					string[] array = new string[10];
					array[0] = "Chunk retesselated listener number ";
					array[1] = i.ToString();
					array[2] = " threw an exception (a=";
					array[3] = (listeners == null).ToString();
					array[4] = ", b=";
					array[5] = (((listeners != null) ? listeners[i] : null) == null).ToString();
					array[6] = ", b=";
					int num = 7;
					object obj;
					if (listeners == null)
					{
						obj = null;
					}
					else
					{
						RetesselationListener retesselationListener = listeners[i];
						obj = ((retesselationListener != null) ? retesselationListener.Handler : null);
					}
					array[num] = (obj == null).ToString();
					array[8] = ")\n";
					int num2 = 9;
					Exception ex = e;
					array[num2] = ((ex != null) ? ex.ToString() : null);
					throw new Exception(string.Concat(array));
				}
			}
		}

		public void TriggerDialogOpened(GuiDialog dialog)
		{
			foreach (Action<GuiDialog> action in this.OnDialogOpened)
			{
				action(dialog);
			}
		}

		public void TriggerDialogClosed(GuiDialog dialog)
		{
			foreach (Action<GuiDialog> action in this.OnDialogClosed)
			{
				action(dialog);
			}
		}

		public void TriggerGameWindowFocus(bool focus)
		{
			foreach (Action<bool> action in this.OnGameWindowFocus)
			{
				action(focus);
			}
		}

		public void TriggerNewModChatLine(int groupid, string message, EnumChatType chattype, string data)
		{
			foreach (ChatLineDelegate chatLineDelegate in this.OnNewClientOnlyChatLine)
			{
				chatLineDelegate(groupid, message, chattype, data);
			}
		}

		public void TriggerNewClientChatLine(int groupid, string message, EnumChatType chattype, string data)
		{
			foreach (ChatLineDelegate chatLineDelegate in this.OnNewClientToServerChatLine)
			{
				chatLineDelegate(groupid, message, chattype, data);
			}
		}

		public void TriggerIngameError(object sender, string errorCode, string text)
		{
			IngameErrorDelegate inGameError = this.InGameError;
			if (inGameError == null)
			{
				return;
			}
			inGameError(sender, errorCode, text);
		}

		public void TriggerIngameDiscovery(object sender, string errorCode, string text)
		{
			IngameDiscoveryDelegate inGameDiscovery = this.InGameDiscovery;
			if (inGameDiscovery == null)
			{
				return;
			}
			inGameDiscovery(sender, errorCode, text);
		}

		public void TriggerColorPresetChanged()
		{
			Action colorPresetChanged = this.ColorPresetChanged;
			if (colorPresetChanged == null)
			{
				return;
			}
			colorPresetChanged();
		}

		public void TriggerNewServerChatLine(int groupid, string message, EnumChatType chattype, string data)
		{
			foreach (ChatLineDelegate chatLineDelegate in this.OnNewServerToClientChatLine)
			{
				chatLineDelegate(groupid, message, chattype, data);
			}
		}

		public bool TriggerBeforeActiveSlotChanged(ClientMain game, int fromSlot, int toSlot)
		{
			if (!game.api.eventapi.TriggerBeforeActiveSlotChanged(game.Logger, fromSlot, toSlot))
			{
				return false;
			}
			foreach (Action action in this.OnActiveSlotChanged)
			{
				action();
			}
			return true;
		}

		public void TriggerAfterActiveSlotChanged(ClientMain game, int fromSlot, int toSlot)
		{
			game.api.eventapi.TriggerAfterActiveSlotChanged(game.Logger, fromSlot, toSlot);
		}

		public void TriggerBlockBreaking(BlockDamage blockDamage)
		{
			foreach (Action<BlockDamage> action in this.OnPlayerBreakingBlock)
			{
				action(blockDamage);
			}
		}

		internal void TriggerBlockUnbreaking(BlockDamage damagedBlock)
		{
			foreach (Action<BlockDamage> action in this.OnUnBreakingBlock)
			{
				action(damagedBlock);
			}
		}

		public void TriggerBlockBroken(BlockDamage blockDamage)
		{
			foreach (Action<BlockDamage> action in this.OnPlayerBrokenBlock)
			{
				action(blockDamage);
			}
		}

		public void TriggerPlayerModeChange()
		{
			foreach (Action action in this.OnPlayerModeChange)
			{
				action();
			}
		}

		public void TriggerReloadShapes()
		{
			Action onReloadShapes = this.OnReloadShapes;
			if (onReloadShapes == null)
			{
				return;
			}
			onReloadShapes();
		}

		public void TriggerReloadTextures()
		{
			Action onReloadTextures = this.OnReloadTextures;
			if (onReloadTextures == null)
			{
				return;
			}
			onReloadTextures();
		}

		public bool TriggerReloadShaders()
		{
			bool ok = true;
			foreach (ActionBoolReturn handler in this.OnReloadShaders)
			{
				ok &= handler();
			}
			return ok;
		}

		public void TriggerEntitySpawn(Entity entity)
		{
			foreach (EntityDelegate entityDelegate in this.OnEntitySpawn)
			{
				entityDelegate(entity);
			}
		}

		public void TriggerEntityLoaded(Entity entity)
		{
			foreach (EntityDelegate entityDelegate in this.OnEntityLoaded)
			{
				entityDelegate(entity);
			}
		}

		public void TriggerEntityDespawn(Entity entity, EntityDespawnData despawnReason)
		{
			foreach (EntityDespawnDelegate entityDespawnDelegate in this.OnEntityDespawn)
			{
				entityDespawnDelegate(entity, despawnReason);
			}
		}

		public void RegisterPlayerPropertyChangedWatcher(EnumProperty property, OnPlayerPropertyChanged handler)
		{
			List<OnPlayerPropertyChanged> watchers;
			this.OnPlayerPropertyChanged.TryGetValue(property, out watchers);
			if (watchers == null)
			{
				watchers = (this.OnPlayerPropertyChanged[property] = new List<OnPlayerPropertyChanged>());
			}
			watchers.Add(handler);
		}

		internal void TriggerPlayerDeath(int clientId, int livesLeft)
		{
			foreach (PlayerDeathDelegate playerDeathDelegate in this.OnPlayerDeath)
			{
				playerDeathDelegate(clientId, livesLeft);
			}
		}

		internal void TriggerBlockChanged(ClientMain game, BlockPos pos, Block oldBlock)
		{
			game.api.eventapi.TriggerBlockChanged(pos, oldBlock);
			foreach (BlockChangedDelegate blockChangedDelegate in this.OnBlockChanged)
			{
				blockChangedDelegate(pos, oldBlock);
			}
		}

		public override bool HasPrivilege(string playerUid, string privilegecode)
		{
			return true;
		}

		public void TriggerOnMouseEnterSlot(ClientMain game, ItemSlot slot)
		{
			game.player.inventoryMgr.currentHoveredSlot = slot;
			using (List<GuiDialog>.Enumerator enumerator = game.LoadedGuis.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current.OnMouseEnterSlot(slot))
					{
						return;
					}
				}
			}
			for (int i = 0; i < game.clientSystems.Length; i++)
			{
				if (game.clientSystems[i].OnMouseEnterSlot(slot))
				{
					return;
				}
			}
		}

		public void TriggerOnMouseLeaveSlot(ClientMain game, ItemSlot itemSlot)
		{
			game.player.inventoryMgr.currentHoveredSlot = null;
			using (List<GuiDialog>.Enumerator enumerator = game.LoadedGuis.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current.OnMouseLeaveSlot(itemSlot))
					{
						return;
					}
				}
			}
			for (int i = 0; i < game.clientSystems.Length; i++)
			{
				if (game.clientSystems[i].OnMouseLeaveSlot(itemSlot))
				{
					return;
				}
			}
		}

		internal void Dispose()
		{
			List<RenderHandler>[] array = this.renderersByStage;
			for (int i = 0; i < array.Length; i++)
			{
				foreach (RenderHandler renderHandler in new List<RenderHandler>(array[i]))
				{
					renderHandler.Renderer.Dispose();
				}
			}
			this.ColorPresetChanged = null;
		}

		public OnAmbientSoundScanCompleteDelegate OnAmbientSoundsScanComplete;

		public CurrentTrackSupplierDelegate CurrentTrackSupplier;

		public TrackStarterDelegate TrackStarter;

		public TrackStarterLoadedDelegate TrackStarterLoaded;

		public Dictionary<Vec3i, List<RetesselationListener>> OnChunkRetesselated = new Dictionary<Vec3i, List<RetesselationListener>>();

		public List<Action<bool>> OnGameWindowFocus = new List<Action<bool>>();

		public List<Action<GuiDialog>> OnDialogOpened = new List<Action<GuiDialog>>();

		public List<Action<GuiDialog>> OnDialogClosed = new List<Action<GuiDialog>>();

		public List<Action<BlockDamage>> OnUnBreakingBlock = new List<Action<BlockDamage>>();

		public List<Action<BlockDamage>> OnPlayerBreakingBlock = new List<Action<BlockDamage>>();

		public List<Action<BlockDamage>> OnPlayerBrokenBlock = new List<Action<BlockDamage>>();

		public List<BlockChangedDelegate> OnBlockChanged = new List<BlockChangedDelegate>();

		public List<Action> OnPlayerModeChange = new List<Action>();

		public List<ActionBoolReturn> OnReloadShaders = new List<ActionBoolReturn>();

		public List<EntityDelegate> OnEntitySpawn = new List<EntityDelegate>();

		public List<EntityDelegate> OnEntityLoaded = new List<EntityDelegate>();

		public List<EntityDespawnDelegate> OnEntityDespawn = new List<EntityDespawnDelegate>();

		public List<PlayerDeathDelegate> OnPlayerDeath = new List<PlayerDeathDelegate>();

		public Dictionary<EnumProperty, List<OnPlayerPropertyChanged>> OnPlayerPropertyChanged = new Dictionary<EnumProperty, List<OnPlayerPropertyChanged>>();

		public List<Action> OnActiveSlotChanged = new List<Action>();

		public List<ChatLineDelegate> OnNewServerToClientChatLine = new List<ChatLineDelegate>();

		public List<ChatLineDelegate> OnNewClientToServerChatLine = new List<ChatLineDelegate>();

		public List<ChatLineDelegate> OnNewClientOnlyChatLine = new List<ChatLineDelegate>();

		public List<EventBusListener> EventBusListeners = new List<EventBusListener>();

		public List<RenderHandler>[] renderersByStage;

		public List<RenderOrderReservation>[] renderOrderReservationsByStage;

		private ClientMain game;
	}
}
