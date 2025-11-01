using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common
{
	public abstract class EventManager
	{
		public abstract ILogger Logger { get; }

		public abstract string CommandPrefix { get; }

		public abstract bool HasPrivilege(string playerUid, string privilegecode);

		public abstract long InWorldEllapsedMs { get; }

		public event OnGetClimateDelegate OnGetClimate;

		public event OnGetWindSpeedDelegate OnGetWindSpeed;

		public virtual void TriggerOnGetClimate(ref ClimateCondition climate, BlockPos pos, EnumGetClimateMode mode = EnumGetClimateMode.WorldGenValues, double totalDays = 0.0)
		{
			if (this.OnGetClimate == null)
			{
				return;
			}
			Delegate[] invocationList = this.OnGetClimate.GetInvocationList();
			for (int i = 0; i < invocationList.Length; i++)
			{
				((OnGetClimateDelegate)invocationList[i])(ref climate, pos, mode, totalDays);
			}
		}

		public virtual void TriggerOnGetWindSpeed(Vec3d pos, ref Vec3d windSpeed)
		{
			if (this.OnGetWindSpeed == null)
			{
				return;
			}
			Delegate[] invocationList = this.OnGetWindSpeed.GetInvocationList();
			for (int i = 0; i < invocationList.Length; i++)
			{
				((OnGetWindSpeedDelegate)invocationList[i])(pos, ref windSpeed);
			}
		}

		public virtual void TriggerGameTick(long ellapsedMilliseconds, IWorldAccessor world)
		{
			FrameProfilerUtil FrameProfiler = world.FrameProfiler;
			List<GameTickListener> GameTickListenersEntity = this.GameTickListenersEntity;
			if (FrameProfiler.Enabled)
			{
				world.FrameProfiler.Enter("tick-entitylisteners (mainly BlockEntities)");
				for (int i = 0; i < GameTickListenersEntity.Count; i++)
				{
					GameTickListener listener = GameTickListenersEntity[i];
					if (listener != null && ellapsedMilliseconds - listener.LastUpdateMilliseconds > (long)listener.Millisecondinterval)
					{
						listener.OnTriggered(ellapsedMilliseconds);
						FrameProfiler.Mark("gmle", listener.Origin().GetType());
					}
				}
				world.FrameProfiler.Leave();
			}
			else
			{
				for (int j = 0; j < GameTickListenersEntity.Count; j++)
				{
					GameTickListener listener2 = GameTickListenersEntity[j];
					if (listener2 != null && ellapsedMilliseconds - listener2.LastUpdateMilliseconds > (long)listener2.Millisecondinterval)
					{
						listener2.OnTriggered(ellapsedMilliseconds);
					}
				}
			}
			FrameProfiler.Mark("tick-gtentity");
			List<GameTickListenerBlock> GameTickListenersBlock = this.GameTickListenersBlock;
			for (int k = 0; k < GameTickListenersBlock.Count; k++)
			{
				GameTickListenerBlock listener3 = GameTickListenersBlock[k];
				if (listener3 != null && ellapsedMilliseconds - listener3.LastUpdateMilliseconds > (long)listener3.Millisecondinterval)
				{
					listener3.Handler(world, listener3.Pos, (float)(ellapsedMilliseconds - listener3.LastUpdateMilliseconds) / 1000f);
					listener3.LastUpdateMilliseconds = ellapsedMilliseconds;
				}
			}
			FrameProfiler.Mark("tick-gtblock");
			this.deletable.Clear();
			foreach (KeyValuePair<long, DelayedCallback> entry in this.DelayedCallbacksEntity)
			{
				if (ellapsedMilliseconds - entry.Value.CallAtEllapsedMilliseconds >= 0L)
				{
					DelayedCallback callback = entry.Value;
					callback.Handler((float)(ellapsedMilliseconds - callback.CallAtEllapsedMilliseconds) / 1000f);
					this.deletable.Add(callback);
				}
			}
			FrameProfiler.Mark("tick-dcentity");
			foreach (DelayedCallback callback2 in this.deletable)
			{
				DelayedCallback delayedCallback;
				this.DelayedCallbacksEntity.TryRemove(callback2.ListenerId, out delayedCallback);
			}
			List<DelayedCallbackBlock> DelayedCallbacksBlock = this.DelayedCallbacksBlock;
			for (int l = 0; l < DelayedCallbacksBlock.Count; l++)
			{
				DelayedCallbackBlock callback3 = DelayedCallbacksBlock[l];
				if (ellapsedMilliseconds - callback3.CallAtEllapsedMilliseconds >= 0L)
				{
					DelayedCallbacksBlock.RemoveAt(l);
					l--;
					callback3.Handler(world, callback3.Pos, (float)(ellapsedMilliseconds - callback3.CallAtEllapsedMilliseconds) / 1000f);
				}
			}
			Dictionary<BlockPos, DelayedCallbackBlock> SingleDelayedCallbacksBlock = this.SingleDelayedCallbacksBlock;
			if (SingleDelayedCallbacksBlock.Count > 0)
			{
				foreach (BlockPos pos in new List<BlockPos>(SingleDelayedCallbacksBlock.Keys))
				{
					DelayedCallbackBlock callback4 = SingleDelayedCallbacksBlock[pos];
					if (ellapsedMilliseconds - callback4.CallAtEllapsedMilliseconds >= 0L)
					{
						SingleDelayedCallbacksBlock.Remove(pos);
						callback4.Handler(world, callback4.Pos, (float)(ellapsedMilliseconds - callback4.CallAtEllapsedMilliseconds) / 1000f);
					}
				}
			}
			FrameProfiler.Mark("tick-dcblock");
		}

		public virtual void TriggerGameTickDebug(long ellapsedMilliseconds, IWorldAccessor world)
		{
			List<GameTickListener> GameTickListenersEntity = this.GameTickListenersEntity;
			for (int i = 0; i < GameTickListenersEntity.Count; i++)
			{
				GameTickListener listener = GameTickListenersEntity[i];
				if (listener != null && ellapsedMilliseconds - listener.LastUpdateMilliseconds > (long)listener.Millisecondinterval)
				{
					listener.OnTriggered(ellapsedMilliseconds);
					world.FrameProfiler.Mark("gmle", listener.Origin().GetType());
				}
			}
			List<GameTickListenerBlock> GameTickListenersBlock = this.GameTickListenersBlock;
			for (int j = 0; j < GameTickListenersBlock.Count; j++)
			{
				GameTickListenerBlock listener2 = GameTickListenersBlock[j];
				if (listener2 != null && ellapsedMilliseconds - listener2.LastUpdateMilliseconds > (long)listener2.Millisecondinterval)
				{
					listener2.Handler(world, listener2.Pos, (float)(ellapsedMilliseconds - listener2.LastUpdateMilliseconds) / 1000f);
					listener2.LastUpdateMilliseconds = ellapsedMilliseconds;
					world.FrameProfiler.Mark("gmlb", listener2.Handler.Target.GetType());
				}
			}
			this.deletable.Clear();
			foreach (KeyValuePair<long, DelayedCallback> entry in this.DelayedCallbacksEntity)
			{
				if (ellapsedMilliseconds - entry.Value.CallAtEllapsedMilliseconds >= 0L)
				{
					DelayedCallback callback = entry.Value;
					callback.Handler((float)(ellapsedMilliseconds - callback.CallAtEllapsedMilliseconds) / 1000f);
					this.deletable.Add(callback);
					world.FrameProfiler.Mark("dce", callback.Handler.Target.GetType());
				}
			}
			foreach (DelayedCallback callback2 in this.deletable)
			{
				DelayedCallback delayedCallback;
				this.DelayedCallbacksEntity.TryRemove(callback2.ListenerId, out delayedCallback);
			}
			List<DelayedCallbackBlock> DelayedCallbacksBlock = this.DelayedCallbacksBlock;
			for (int k = 0; k < DelayedCallbacksBlock.Count; k++)
			{
				DelayedCallbackBlock callback3 = DelayedCallbacksBlock[k];
				if (ellapsedMilliseconds - callback3.CallAtEllapsedMilliseconds >= 0L)
				{
					DelayedCallbacksBlock.RemoveAt(k);
					k--;
					callback3.Handler(world, callback3.Pos, (float)(ellapsedMilliseconds - callback3.CallAtEllapsedMilliseconds) / 1000f);
					world.FrameProfiler.Mark("dcb", callback3.Handler.Target.GetType());
				}
			}
			Dictionary<BlockPos, DelayedCallbackBlock> SingleDelayedCallbacksBlock = this.SingleDelayedCallbacksBlock;
			if (SingleDelayedCallbacksBlock.Count > 0)
			{
				foreach (BlockPos pos in new List<BlockPos>(SingleDelayedCallbacksBlock.Keys))
				{
					DelayedCallbackBlock callback4 = SingleDelayedCallbacksBlock[pos];
					if (ellapsedMilliseconds - callback4.CallAtEllapsedMilliseconds >= 0L)
					{
						SingleDelayedCallbacksBlock.Remove(pos);
						callback4.Handler(world, callback4.Pos, (float)(ellapsedMilliseconds - callback4.CallAtEllapsedMilliseconds) / 1000f);
						world.FrameProfiler.Mark("sdcb", callback4.Handler.Target.GetType());
					}
				}
			}
		}

		public virtual long AddGameTickListener(Action<float> handler, int millisecondInterval, int initialDelayOffsetMs = 0)
		{
			return this.AddGameTickListener(handler, null, millisecondInterval, initialDelayOffsetMs);
		}

		public virtual long AddGameTickListener(Action<float> handler, Action<Exception> errorHandler, int millisecondInterval, int initialDelayOffsetMs = 0)
		{
			long num = this.listenerId + 1L;
			this.listenerId = num;
			long newListenerId = num;
			GameTickListener listener = new GameTickListener
			{
				Handler = handler,
				ErrorHandler = errorHandler,
				Millisecondinterval = millisecondInterval,
				ListenerId = newListenerId,
				LastUpdateMilliseconds = this.InWorldEllapsedMs + (long)initialDelayOffsetMs
			};
			List<GameTickListener> GameTickListenersEntity = this.GameTickListenersEntity;
			int i = 0;
			while (i < GameTickListenersEntity.Count)
			{
				if (GameTickListenersEntity[i] == null)
				{
					GameTickListenersEntity[i] = listener;
					this.GameTickListenersEntityIndices[newListenerId] = i;
					if (GameTickListenersEntity[this.GameTickListenersEntityIndices[newListenerId]] != listener)
					{
						throw new InvalidOperationException("Failed to add listener properly");
					}
					return newListenerId;
				}
				else
				{
					i++;
				}
			}
			GameTickListenersEntity.Add(listener);
			this.GameTickListenersEntityIndices[newListenerId] = GameTickListenersEntity.Count - 1;
			if (GameTickListenersEntity[this.GameTickListenersEntityIndices[newListenerId]] != listener)
			{
				throw new InvalidOperationException("Failed to add listener properly");
			}
			return newListenerId;
		}

		public long AddGameTickListener(Action<IWorldAccessor, BlockPos, float> handler, BlockPos pos, int millisecondInterval, int initialDelayOffsetMs = 0)
		{
			long num = this.listenerId + 1L;
			this.listenerId = num;
			long newListenerId = num;
			GameTickListenerBlock listener = new GameTickListenerBlock
			{
				Handler = handler,
				Millisecondinterval = millisecondInterval,
				ListenerId = newListenerId,
				LastUpdateMilliseconds = this.InWorldEllapsedMs + (long)initialDelayOffsetMs,
				Pos = pos.Copy()
			};
			List<GameTickListenerBlock> GameTickListenersBlock = this.GameTickListenersBlock;
			for (int i = 0; i < GameTickListenersBlock.Count; i++)
			{
				if (GameTickListenersBlock[i] == null)
				{
					GameTickListenersBlock[i] = listener;
					this.GameTickListenersBlockIndices[newListenerId] = i;
					return newListenerId;
				}
			}
			GameTickListenersBlock.Add(listener);
			this.GameTickListenersBlockIndices[newListenerId] = GameTickListenersBlock.Count - 1;
			return newListenerId;
		}

		public virtual long AddDelayedCallback(Action<float> handler, long callAfterEllapsedMS)
		{
			long newCallbackId = Interlocked.Increment(ref this.callBackId);
			DelayedCallback newCallback = new DelayedCallback
			{
				CallAtEllapsedMilliseconds = this.InWorldEllapsedMs + callAfterEllapsedMS,
				Handler = handler,
				ListenerId = newCallbackId
			};
			this.DelayedCallbacksEntity[newCallbackId] = newCallback;
			return newCallbackId;
		}

		public virtual long AddDelayedCallback(Action<IWorldAccessor, BlockPos, float> handler, BlockPos pos, long callAfterEllapsedMS)
		{
			long newCallbackId = Interlocked.Increment(ref this.callBackId);
			this.DelayedCallbacksBlock.Add(new DelayedCallbackBlock
			{
				CallAtEllapsedMilliseconds = this.InWorldEllapsedMs + callAfterEllapsedMS,
				Handler = handler,
				ListenerId = newCallbackId,
				Pos = pos.Copy()
			});
			return newCallbackId;
		}

		internal virtual long AddSingleDelayedCallback(Action<IWorldAccessor, BlockPos, float> handler, BlockPos pos, long callAfterEllapsedMs)
		{
			BlockPos cpos = pos.Copy();
			long newCallbackId = Interlocked.Increment(ref this.callBackId);
			this.SingleDelayedCallbacksBlock[cpos] = new DelayedCallbackBlock
			{
				CallAtEllapsedMilliseconds = this.InWorldEllapsedMs + callAfterEllapsedMs,
				Handler = handler,
				ListenerId = newCallbackId,
				Pos = cpos
			};
			return newCallbackId;
		}

		public void RemoveDelayedCallback(long callbackId)
		{
			if (callbackId == 0L)
			{
				return;
			}
			DelayedCallback delayedCallback;
			if (this.DelayedCallbacksEntity.TryRemove(callbackId, out delayedCallback))
			{
				return;
			}
			foreach (DelayedCallbackBlock val in this.DelayedCallbacksBlock)
			{
				if (val.ListenerId == callbackId)
				{
					this.DelayedCallbacksBlock.Remove(val);
					return;
				}
			}
			foreach (KeyValuePair<BlockPos, DelayedCallbackBlock> val2 in this.SingleDelayedCallbacksBlock)
			{
				if (val2.Value.ListenerId == callbackId)
				{
					this.SingleDelayedCallbacksBlock.Remove(val2.Key);
					break;
				}
			}
		}

		public void RemoveGameTickListener(long listenerId)
		{
			if (listenerId == 0L)
			{
				return;
			}
			int index;
			int indexB;
			if (this.GameTickListenersEntityIndices.TryRemove(listenerId, out index))
			{
				GameTickListener listener = this.GameTickListenersEntity[index];
				if (listener != null && listener.ListenerId == listenerId)
				{
					this.GameTickListenersEntity[index] = null;
					return;
				}
			}
			else if (this.GameTickListenersBlockIndices.TryRemove(listenerId, out indexB))
			{
				GameTickListenerBlock listener2 = this.GameTickListenersBlock[indexB];
				if (listener2 != null && listener2.ListenerId == listenerId)
				{
					this.GameTickListenersBlock[indexB] = null;
					return;
				}
			}
			List<GameTickListener> GameTickListenersEntityLocal = this.GameTickListenersEntity;
			for (int i = 0; i < GameTickListenersEntityLocal.Count; i++)
			{
				GameTickListener listener3 = GameTickListenersEntityLocal[i];
				if (listener3 != null && listener3.ListenerId == listenerId)
				{
					GameTickListenersEntityLocal[i] = null;
					return;
				}
			}
			List<GameTickListenerBlock> GameTickListenersBlockLocal = this.GameTickListenersBlock;
			for (int j = 0; j < GameTickListenersBlockLocal.Count; j++)
			{
				GameTickListenerBlock listener4 = GameTickListenersBlockLocal[j];
				if (listener4 != null && listener4.ListenerId == listenerId)
				{
					GameTickListenersBlockLocal[j] = null;
					return;
				}
			}
		}

		private long listenerId;

		private long callBackId;

		internal List<GameTickListener> GameTickListenersEntity = new List<GameTickListener>();

		internal ConcurrentDictionary<long, DelayedCallback> DelayedCallbacksEntity = new ConcurrentDictionary<long, DelayedCallback>();

		internal List<GameTickListenerBlock> GameTickListenersBlock = new List<GameTickListenerBlock>();

		internal List<DelayedCallbackBlock> DelayedCallbacksBlock = new List<DelayedCallbackBlock>();

		internal ConcurrentDictionary<long, int> GameTickListenersEntityIndices = new ConcurrentDictionary<long, int>();

		internal ConcurrentDictionary<long, int> GameTickListenersBlockIndices = new ConcurrentDictionary<long, int>();

		internal Dictionary<BlockPos, DelayedCallbackBlock> SingleDelayedCallbacksBlock = new Dictionary<BlockPos, DelayedCallbackBlock>();

		private List<DelayedCallback> deletable = new List<DelayedCallback>();

		protected Thread serverThread;
	}
}
