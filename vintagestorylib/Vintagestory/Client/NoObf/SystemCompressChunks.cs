using System;
using System.Collections.Generic;
using System.Runtime;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	internal class SystemCompressChunks : ClientSystem
	{
		public override string Name
		{
			get
			{
				return "cc";
			}
		}

		public SystemCompressChunks(ClientMain game)
			: base(game)
		{
			game.RegisterGameTickListener(new Action<float>(this.TryCompactLargeObjectHeap), 1000, 0);
			game.eventManager.RegisterRenderer(new Action<float>(this.OnFinalizeFrame), EnumRenderStage.Done, "cc", 0.999);
		}

		private void TryCompactLargeObjectHeap(float dt)
		{
			if (ClientSettings.OptimizeRamMode != 2)
			{
				return;
			}
			int secondsPassed = Environment.TickCount / 1000 - this.lastCompactionTime;
			if ((secondsPassed >= 602 || (secondsPassed >= 30 && !this.game.Platform.IsFocused)) && (float)(GC.GetTotalMemory(false) / 1024L) / 1024f - this.megabytesMinimum > 512f)
			{
				GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
				GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized);
				this.lastCompactionTime = Environment.TickCount / 1000;
				float mbafter = (float)(GC.GetTotalMemory(false) / 1024L) / 1024f;
				if (mbafter < this.megabytesMinimum || this.megabytesMinimum == 0f)
				{
					this.megabytesMinimum = mbafter;
				}
			}
		}

		public override int SeperateThreadTickIntervalMs()
		{
			return 20;
		}

		public override void OnSeperateThreadGameTick(float dt)
		{
			object compactSyncLock = this.game.compactSyncLock;
			lock (compactSyncLock)
			{
				long index3d = 0L;
				if (this.compactableClientChunks.Count > 0)
				{
					index3d = this.compactableClientChunks.Dequeue();
				}
				if (index3d != 0L)
				{
					ClientChunk chunk = null;
					object chunksLock = this.game.WorldMap.chunksLock;
					lock (chunksLock)
					{
						this.game.WorldMap.chunks.TryGetValue(index3d, out chunk);
					}
					if (chunk != null)
					{
						chunk.Pack();
						if (!chunk.ChunkHasData())
						{
							Vec3i vec = new Vec3i();
							MapUtil.PosInt3d(index3d, (long)this.game.WorldMap.index3dMulX, (long)this.game.WorldMap.index3dMulZ, vec);
							throw new Exception(string.Format("ACP: Chunk {0} {1} {2} has no more block data.", vec.X, vec.Y, vec.Z));
						}
						this.game.compactedClientChunks.Enqueue(index3d);
					}
				}
			}
		}

		public void OnFinalizeFrame(float dt)
		{
			if (this.game.extendedDebugInfo)
			{
				this.game.DebugScreenInfo["compactqueuesize"] = "Client Chunks in compact queue: " + this.compactableClientChunks.Count.ToString();
				this.game.DebugScreenInfo["compactratio"] = "Client chunk compression ratio: " + (this.compressionRatio * 100f).ToString("0.#") + "%";
			}
			else
			{
				this.game.DebugScreenInfo["compactqueuesize"] = "";
				this.game.DebugScreenInfo["compactratio"] = "";
			}
			long cur = this.game.Platform.EllapsedMs;
			if (cur - this.chunkCompressScanTimer < 4000L)
			{
				return;
			}
			this.chunkCompressScanTimer = cur;
			int ttl = this.ttlsByRamMode[ClientSettings.OptimizeRamMode];
			object compactSyncLock = this.game.compactSyncLock;
			lock (compactSyncLock)
			{
				object obj = this.game.WorldMap.chunksLock;
				lock (obj)
				{
					while (this.game.compactedClientChunks.Count > 0)
					{
						long index3d = this.game.compactedClientChunks.Dequeue();
						ClientChunk chunk;
						this.game.WorldMap.chunks.TryGetValue(index3d, out chunk);
						if (chunk != null)
						{
							chunk.TryCommitPackAndFree(8000);
						}
					}
				}
				Vec3i chunkpos = new Vec3i();
				Vec3i plrChunkPos = new Vec3i((int)this.game.EntityPlayer.Pos.X, (int)this.game.EntityPlayer.Pos.Y, (int)this.game.EntityPlayer.Pos.Z) / this.game.WorldMap.ClientChunkSize;
				if (this.compactableClientChunks.Count == 0)
				{
					this.compressed = 0;
					obj = this.game.WorldMap.chunksLock;
					lock (obj)
					{
						foreach (KeyValuePair<long, ClientChunk> val in this.game.WorldMap.chunks)
						{
							if (val.Value.IsPacked())
							{
								this.compressed++;
							}
							else if ((long)Environment.TickCount - val.Value.lastReadOrWrite > (long)ttl && val.Value.centerModelPoolLocations != null && val.Value.edgeModelPoolLocations != null)
							{
								MapUtil.PosInt3d(val.Key, (long)this.game.WorldMap.index3dMulX, (long)this.game.WorldMap.index3dMulZ, chunkpos);
								if (Math.Abs(plrChunkPos.X - chunkpos.X) < 2 && Math.Abs(plrChunkPos.Z - chunkpos.Z) < 2 && !val.Value.Empty)
								{
									val.Value.MarkFresh();
								}
								else
								{
									this.compactableClientChunks.Enqueue(val.Key);
								}
							}
						}
					}
				}
				this.compressionRatio = (float)this.compressed / (float)this.game.WorldMap.chunks.Count;
			}
		}

		public override EnumClientSystemType GetSystemType()
		{
			return EnumClientSystemType.Misc;
		}

		private long chunkCompressScanTimer;

		private float compressionRatio;

		private Queue<long> compactableClientChunks = new Queue<long>();

		private int lastCompactionTime;

		private float megabytesMinimum;

		private int compressed;

		private int[] ttlsByRamMode = new int[] { 80000, 8000, 4000 };
	}
}
