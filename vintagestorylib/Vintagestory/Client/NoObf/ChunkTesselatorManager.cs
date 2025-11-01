using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class ChunkTesselatorManager : ClientSystem
	{
		public override string Name
		{
			get
			{
				return "tete";
			}
		}

		public ChunkTesselatorManager(ClientMain game)
			: base(game)
		{
			this.chunksize = game.WorldMap.ClientChunkSize;
			game.eventManager.RegisterRenderer(new Action<float>(this.OnBeforeFrame), EnumRenderStage.Before, "chtema", 0.99);
		}

		public override void Dispose(ClientMain game)
		{
			game.ShouldTesselateTerrain = false;
			object obj = this.tessChunksQueueLock;
			lock (obj)
			{
				SortableQueue<TesselatedChunk> sortableQueue = this.tessChunksQueue;
				if (sortableQueue != null)
				{
					sortableQueue.Clear();
				}
				this.tessChunksQueue = null;
			}
			obj = this.tessChunksQueuePriorityLock;
			lock (obj)
			{
				Queue<TesselatedChunk> queue = this.tessChunksQueuePriority;
				if (queue != null)
				{
					queue.Clear();
				}
				this.tessChunksQueuePriority = null;
			}
		}

		public override int SeperateThreadTickIntervalMs()
		{
			return 0;
		}

		public override void OnBlockTexturesLoaded()
		{
			this.game.TerrainChunkTesselator.BlockTexturesLoaded();
		}

		public void OnBeforeFrame(float dt)
		{
			RuntimeStats.chunksAwaitingTesselation = this.game.dirtyChunksPriority.Count + this.game.dirtyChunks.Count + this.game.dirtyChunksLast.Count;
			RuntimeStats.chunksAwaitingPooling = this.tessChunksQueuePriority.Count + this.tessChunksQueue.Count;
			int tickMaxVerticesBase = this.game.frustumCuller.ViewDistanceSq / 48 + 350;
			int totalVertices = 0;
			object obj;
			if (this.processPrioQueue)
			{
				obj = this.tessChunksQueuePriorityLock;
				lock (obj)
				{
					while (this.tessChunksQueuePriority.Count > 0)
					{
						TesselatedChunk tesschunk = this.tessChunksQueuePriority.Dequeue();
						tesschunk.chunk.queuedForUpload = false;
						ClientChunk chunk = this.game.WorldMap.GetChunkAtBlockPos(tesschunk.positionX, tesschunk.positionYAndDimension, tesschunk.positionZ);
						if (chunk != null)
						{
							this.game.chunkRenderer.AddTesselatedChunk(tesschunk, chunk);
							this.singleUploadDelayCounter = 10;
							totalVertices += tesschunk.VerticesCount;
							this.tmpPos.Set(tesschunk.positionX / 32, tesschunk.positionYAndDimension / 32, tesschunk.positionZ / 32);
							ClientEventManager eventManager = this.game.eventManager;
							if (eventManager != null)
							{
								eventManager.TriggerChunkRetesselated(this.tmpPos, chunk);
							}
						}
						else
						{
							tesschunk.UnusedDispose();
						}
					}
				}
				this.processPrioQueue = false;
			}
			int tcqc = this.tessChunksQueue.Count;
			int maxVertices = tickMaxVerticesBase * (3 + tcqc / (1 << ClientSettings.ChunkVerticesUploadRateLimiter));
			if (totalVertices >= maxVertices)
			{
				return;
			}
			if (tcqc < 2)
			{
				if (tcqc != 0)
				{
					int num = this.singleUploadDelayCounter;
					this.singleUploadDelayCounter = num + 1;
					if (num >= 10)
					{
						goto IL_01AE;
					}
				}
				return;
			}
			IL_01AE:
			this.singleUploadDelayCounter = 0;
			obj = this.tessChunksQueueLock;
			lock (obj)
			{
				this.tessChunksQueue.RunForEach(delegate(TesselatedChunk eachTC)
				{
					eachTC.RecalcPriority(this.game.player);
				});
				this.tessChunksQueue.Sort();
				while (this.tessChunksQueue.Count > 0 && totalVertices < maxVertices)
				{
					TesselatedChunk tesschunk = this.tessChunksQueue.Dequeue();
					tesschunk.chunk.queuedForUpload = false;
					ClientChunk chunk2 = this.game.WorldMap.GetChunkAtBlockPos(tesschunk.positionX, tesschunk.positionYAndDimension, tesschunk.positionZ);
					if (chunk2 != null)
					{
						this.game.chunkRenderer.AddTesselatedChunk(tesschunk, chunk2);
						totalVertices += tesschunk.VerticesCount;
						this.tmpPos.Set(tesschunk.positionX / 32, tesschunk.positionYAndDimension / 32, tesschunk.positionZ / 32);
						ClientEventManager eventManager2 = this.game.eventManager;
						if (eventManager2 != null)
						{
							eventManager2.TriggerChunkRetesselated(this.tmpPos, chunk2);
						}
					}
					else
					{
						tesschunk.UnusedDispose();
					}
				}
			}
		}

		public override void OnSeperateThreadGameTick(float dt)
		{
			if (!this.game.TerrainChunkTesselator.started)
			{
				return;
			}
			MeshDataRecycler currentRecycler = MeshData.Recycler;
			if (!this.game.ShouldTesselateTerrain)
			{
				if (currentRecycler != null)
				{
					currentRecycler.DoRecycling();
				}
				return;
			}
			long index3dAndEdgeFlag = 0L;
			int count = this.game.dirtyChunksPriority.Count;
			while (count-- > 0)
			{
				object obj = this.game.dirtyChunksPriorityLock;
				lock (obj)
				{
					index3dAndEdgeFlag = this.game.dirtyChunksPriority.Dequeue();
				}
				long index3d = index3dAndEdgeFlag;
				if (index3dAndEdgeFlag < 0L)
				{
					index3d = index3dAndEdgeFlag & long.MaxValue;
					if (this.game.dirtyChunksPriority.Contains(index3d))
					{
						continue;
					}
				}
				MapUtil.PosInt3d(index3d, (long)this.game.WorldMap.index3dMulX, (long)this.game.WorldMap.index3dMulZ, this.chunkPos);
				if (!this.game.ShouldTesselateTerrain)
				{
					break;
				}
				bool requeue;
				this.TesselateChunk(this.chunkPos.X, this.chunkPos.Y, this.chunkPos.Z, true, index3dAndEdgeFlag < 0L, out requeue);
				if (requeue)
				{
					obj = this.game.dirtyChunksPriorityLock;
					lock (obj)
					{
						this.game.dirtyChunksPriority.Enqueue(index3dAndEdgeFlag);
					}
				}
			}
			int TICKMAXVERTICES = (this.game.frustumCuller.ViewDistanceSq + 16800) * 3 / 2;
			int totalVertices = 0;
			count = this.game.dirtyChunks.Count;
			while (count-- > 0 && totalVertices < TICKMAXVERTICES)
			{
				object obj = this.game.dirtyChunksLock;
				lock (obj)
				{
					if (this.game.dirtyChunks.Count <= 0)
					{
						break;
					}
					index3dAndEdgeFlag = this.game.dirtyChunks.Dequeue();
				}
				long index3d2 = index3dAndEdgeFlag;
				if (index3dAndEdgeFlag < 0L)
				{
					index3d2 = index3dAndEdgeFlag & long.MaxValue;
					if (this.game.dirtyChunks.Contains(index3d2))
					{
						continue;
					}
				}
				if (!this.game.ShouldTesselateTerrain)
				{
					break;
				}
				MapUtil.PosInt3d(index3d2, (long)this.game.WorldMap.index3dMulX, (long)this.game.WorldMap.index3dMulZ, this.chunkPos);
				bool requeue2;
				totalVertices += this.TesselateChunk(this.chunkPos.X, this.chunkPos.Y, this.chunkPos.Z, false, index3dAndEdgeFlag < 0L, out requeue2);
				if (requeue2)
				{
					obj = this.game.dirtyChunksLock;
					lock (obj)
					{
						this.game.dirtyChunks.Enqueue(index3dAndEdgeFlag);
					}
				}
			}
			int i = 5;
			while (this.game.dirtyChunksLast.Count > 0 && i-- > 0)
			{
				object obj = this.game.dirtyChunksLastLock;
				lock (obj)
				{
					index3dAndEdgeFlag = this.game.dirtyChunksLast.Dequeue();
				}
				MapUtil.PosInt3d(index3dAndEdgeFlag & long.MaxValue, (long)this.game.WorldMap.index3dMulX, (long)this.game.WorldMap.index3dMulZ, this.chunkPos);
				if (!this.game.ShouldTesselateTerrain)
				{
					break;
				}
				bool requeue3;
				this.TesselateChunk(this.chunkPos.X, this.chunkPos.Y, this.chunkPos.Z, false, index3dAndEdgeFlag < 0L, out requeue3);
				if (requeue3)
				{
					obj = this.game.dirtyChunksLastLock;
					lock (obj)
					{
						this.game.dirtyChunksLast.Enqueue(index3dAndEdgeFlag);
					}
				}
			}
			if (currentRecycler != null)
			{
				currentRecycler.DoRecycling();
			}
		}

		public int TesselateChunk(int chunkX, int chunkY, int chunkZ, bool priority, bool skipChunkCenter, out bool requeue)
		{
			requeue = false;
			ClientChunk chunk = this.game.WorldMap.GetClientChunk(chunkX, chunkY, chunkZ);
			if (chunk == null || chunk.Empty)
			{
				if (chunk != null)
				{
					chunk.quantityDrawn++;
					chunk.enquedForRedraw = false;
				}
				return 0;
			}
			ChunkTesselator terrainChunkTesselator = this.game.TerrainChunkTesselator;
			object packUnpackLock = chunk.packUnpackLock;
			lock (packUnpackLock)
			{
				if (!chunk.loadedFromServer)
				{
					requeue = true;
					return 0;
				}
				if (chunk.Unpack_ReadOnly())
				{
					RuntimeStats.TCTpacked++;
				}
				else
				{
					RuntimeStats.TCTunpacked++;
				}
				chunk.queuedForUpload = true;
				chunk.lastTesselationMs = this.game.Platform.EllapsedMs;
				chunk.enquedForRedraw = false;
				chunk.quantityDrawn++;
				terrainChunkTesselator.vars.blockEntitiesOfChunk = chunk.BlockEntities;
				TCTCache vars = terrainChunkTesselator.vars;
				IMapChunk mapChunk = chunk.MapChunk;
				vars.rainHeightMap = ((mapChunk != null) ? mapChunk.RainHeightMap : null) ?? this.CreateDummyHeightMap();
			}
			if (RuntimeStats.chunksTesselatedTotal == 0)
			{
				RuntimeStats.tesselationStart = this.game.Platform.EllapsedMs;
			}
			RuntimeStats.chunksTesselatedPerSecond++;
			RuntimeStats.chunksTesselatedTotal++;
			if (skipChunkCenter)
			{
				RuntimeStats.chunksTesselatedEdgeOnly++;
			}
			if (chunk.shouldSunRelight)
			{
				this.game.terrainIlluminator.SunRelightChunk(chunk, chunkX, chunkY, chunkZ);
			}
			int verticesCount = 0;
			TesselatedChunk tessChunk = new TesselatedChunk
			{
				chunk = chunk,
				CullVisible = chunk.CullVisible,
				positionX = chunkX * this.chunksize,
				positionYAndDimension = chunkY * this.chunksize,
				positionZ = chunkZ * this.chunksize
			};
			verticesCount = terrainChunkTesselator.NowProcessChunk(chunkX, chunkY, chunkZ, tessChunk, skipChunkCenter);
			tessChunk.VerticesCount = verticesCount;
			if (priority)
			{
				packUnpackLock = this.tessChunksQueuePriorityLock;
				lock (packUnpackLock)
				{
					Queue<TesselatedChunk> queue = this.tessChunksQueuePriority;
					if (queue != null)
					{
						queue.Enqueue(tessChunk);
					}
				}
				this.processPrioQueue = true;
			}
			else
			{
				packUnpackLock = this.tessChunksQueueLock;
				lock (packUnpackLock)
				{
					SortableQueue<TesselatedChunk> sortableQueue = this.tessChunksQueue;
					if (sortableQueue != null)
					{
						sortableQueue.EnqueueOrMerge(tessChunk);
					}
				}
			}
			chunk.lastTesselationMs = 0L;
			return verticesCount;
		}

		private ushort[] CreateDummyHeightMap()
		{
			ushort[] newHeightMap = new ushort[this.game.WorldMap.MapChunkSize * this.game.WorldMap.MapChunkSize];
			ushort maxHeight = (ushort)(this.game.WorldMap.MapSizeY - 1);
			for (int i = 0; i < newHeightMap.Length; i++)
			{
				newHeightMap[i] = maxHeight;
				newHeightMap[++i] = maxHeight;
			}
			return newHeightMap;
		}

		public override EnumClientSystemType GetSystemType()
		{
			return EnumClientSystemType.Misc;
		}

		internal int chunksize;

		private object tessChunksQueueLock = new object();

		private SortableQueue<TesselatedChunk> tessChunksQueue = new SortableQueue<TesselatedChunk>();

		private object tessChunksQueuePriorityLock = new object();

		private Queue<TesselatedChunk> tessChunksQueuePriority = new Queue<TesselatedChunk>();

		private Vec3i chunkPos = new Vec3i();

		private Vec3i tmpPos = new Vec3i();

		private int singleUploadDelayCounter;

		private bool processPrioQueue;

		public static long cumulativeTime;

		public static int cumulativeCount;
	}
}
