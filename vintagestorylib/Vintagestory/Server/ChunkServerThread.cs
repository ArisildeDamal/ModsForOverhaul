using System;
using System.Threading;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace Vintagestory.Server
{
	public class ChunkServerThread : ServerThread, IChunkProvider
	{
		public ILogger Logger
		{
			get
			{
				return ServerMain.Logger;
			}
		}

		public ChunkServerThread(ServerMain server, string threadname, CancellationToken cancellationToken)
			: base(server, threadname, cancellationToken)
		{
			int availablePasses = 5;
			this.additionalWorldGenThreadsCount = Math.Min(availablePasses, MagicNum.MaxWorldgenThreads - 1);
			if (server.ReducedServerThreads)
			{
				this.additionalWorldGenThreadsCount = 0;
			}
			if (this.additionalWorldGenThreadsCount < 0)
			{
				this.additionalWorldGenThreadsCount = 0;
			}
		}

		protected override void UpdatePausedStatus(bool newpause)
		{
			if (this.ShouldPause != this.additionalThreadsPaused)
			{
				this.TogglePause(!this.additionalThreadsPaused);
			}
			base.UpdatePausedStatus(newpause);
		}

		private void TogglePause(bool paused)
		{
			ServerSystemSupplyChunks supplychunks = (ServerSystemSupplyChunks)this.serversystems[0];
			if (paused)
			{
				supplychunks.PauseAllWorldgenThreads(1500);
				supplychunks.FullyClearGeneratingQueue();
			}
			else
			{
				supplychunks.ResumeAllWorldgenThreads();
				if (this.additionalWorldGenThreadsCount > 0)
				{
					ServerMain.Logger.VerboseDebug("Un-pausing all worldgen threads.");
				}
			}
			this.additionalThreadsPaused = paused;
		}

		public ServerChunk GetGeneratingChunkAtPos(int posX, int posY, int posZ)
		{
			return this.GetGeneratingChunk(posX / MagicNum.ServerChunkSize, posY / MagicNum.ServerChunkSize, posZ / MagicNum.ServerChunkSize);
		}

		public ServerChunk GetGeneratingChunkAtPos(BlockPos pos)
		{
			return this.GetGeneratingChunk(pos.X / MagicNum.ServerChunkSize, pos.Y / MagicNum.ServerChunkSize, pos.Z / MagicNum.ServerChunkSize);
		}

		public ChunkColumnLoadRequest GetChunkRequestAtPos(int posX, int posZ)
		{
			long index2d = this.server.WorldMap.MapChunkIndex2D(posX / MagicNum.ServerChunkSize, posZ / MagicNum.ServerChunkSize);
			if (!this.peekMode)
			{
				return this.requestedChunkColumns.GetByIndex(index2d);
			}
			return this.peekingChunkColumns.GetByIndex(index2d);
		}

		internal ServerChunk GetGeneratingChunk(int chunkX, int chunkY, int chunkZ)
		{
			long index2d = this.server.WorldMap.MapChunkIndex2D(chunkX, chunkZ);
			ChunkColumnLoadRequest chunkReq = (this.peekMode ? this.peekingChunkColumns.GetByIndex(index2d) : this.requestedChunkColumns.GetByIndex(index2d));
			if (chunkReq != null && chunkReq.CurrentIncompletePass > EnumWorldGenPass.None && chunkY >= 0 && chunkY < chunkReq.Chunks.Length)
			{
				return chunkReq.Chunks[chunkY];
			}
			return null;
		}

		internal ServerMapChunk GetMapChunk(long index2d)
		{
			ServerMapChunk mapchunk;
			if (this.server.loadedMapChunks.TryGetValue(index2d, out mapchunk))
			{
				return mapchunk;
			}
			ChunkColumnLoadRequest chunkColumnLoadRequest = (this.peekMode ? this.peekingChunkColumns.GetByIndex(index2d) : this.requestedChunkColumns.GetByIndex(index2d));
			if (chunkColumnLoadRequest == null)
			{
				return null;
			}
			return chunkColumnLoadRequest.MapChunk;
		}

		internal ServerMapRegion GetMapRegion(int regionX, int regionZ)
		{
			ServerMapRegion mapregion;
			if (this.server.loadedMapRegions.TryGetValue(this.server.WorldMap.MapRegionIndex2D(regionX, regionZ), out mapregion))
			{
				return mapregion;
			}
			int blockx = regionX * this.server.WorldMap.RegionSize;
			int num = regionZ * this.server.WorldMap.RegionSize;
			int chunkx = blockx / 32;
			int chunkz = num / 32;
			long index2d = this.server.WorldMap.MapChunkIndex2D(chunkx, chunkz);
			ChunkColumnLoadRequest chunkColumnLoadRequest = (this.peekMode ? this.peekingChunkColumns.GetByIndex(index2d) : this.requestedChunkColumns.GetByIndex(index2d));
			if (chunkColumnLoadRequest == null)
			{
				return null;
			}
			ServerMapChunk mapChunk = chunkColumnLoadRequest.MapChunk;
			if (mapChunk == null)
			{
				return null;
			}
			return mapChunk.MapRegion;
		}

		IWorldChunk IChunkProvider.GetChunk(int chunkX, int chunkY, int chunkZ)
		{
			return this.GetGeneratingChunk(chunkX, chunkY, chunkZ);
		}

		IWorldChunk IChunkProvider.GetUnpackedChunkFast(int chunkX, int chunkY, int chunkZ, bool notRecentlyAccessed)
		{
			ServerChunk generatingChunk = this.GetGeneratingChunk(chunkX, chunkY, chunkZ);
			if (generatingChunk == null)
			{
				return generatingChunk;
			}
			((IWorldChunk)generatingChunk).Unpack();
			return generatingChunk;
		}

		internal bool addChunkColumnRequest(long index2d, int chunkX, int chunkZ, int clientid, EnumWorldGenPass untilPass = EnumWorldGenPass.Done, ITreeAttribute chunkLoadParams = null)
		{
			return this.addChunkColumnRequest(new ChunkColumnLoadRequest(index2d, chunkX, chunkZ, clientid, (int)untilPass, this.server)
			{
				chunkGenParams = chunkLoadParams
			});
		}

		internal bool addChunkColumnRequest(ChunkColumnLoadRequest chunkRequest)
		{
			ChunkColumnLoadRequest prevChunkReq = this.requestedChunkColumns.elementsByIndex.GetOrAdd(chunkRequest.mapIndex2d, chunkRequest);
			if (prevChunkReq != chunkRequest)
			{
				if (prevChunkReq.untilPass < chunkRequest.untilPass)
				{
					prevChunkReq.untilPass = chunkRequest.untilPass;
				}
				if (prevChunkReq.CurrentIncompletePass < chunkRequest.CurrentIncompletePass)
				{
					prevChunkReq.Chunks = chunkRequest.Chunks;
				}
				if (prevChunkReq.creationTime < chunkRequest.creationTime)
				{
					prevChunkReq.creationTime = chunkRequest.creationTime;
				}
				if (chunkRequest.blockingRequest && !prevChunkReq.blockingRequest)
				{
					prevChunkReq.blockingRequest = true;
				}
			}
			else
			{
				this.requestedChunkColumns.EnqueueWithoutAddingToIndex(chunkRequest);
			}
			return !prevChunkReq.Disposed;
		}

		internal bool EnsureMinimumWorldgenPassAt(long index2d, int chunkX, int chunkZ, int minPass, long requirorTime)
		{
			ServerMapChunk mapchunk;
			this.server.loadedMapChunks.TryGetValue(index2d, out mapchunk);
			if (mapchunk != null && mapchunk.CurrentIncompletePass == EnumWorldGenPass.Done)
			{
				return true;
			}
			ChunkColumnLoadRequest prevChunkReq = this.requestedChunkColumns.elementsByIndex.GetOrAdd(index2d, (long index2d) => new ChunkColumnLoadRequest(index2d, chunkX, chunkZ, this.server.serverConsoleId, -1, this.server));
			if (prevChunkReq.CurrentIncompletePass_AsInt < minPass)
			{
				if (prevChunkReq.untilPass < minPass)
				{
					if (prevChunkReq.untilPass < 0)
					{
						this.requestedChunkColumns.EnqueueWithoutAddingToIndex(prevChunkReq);
					}
					prevChunkReq.untilPass = minPass;
					if (prevChunkReq.creationTime < requirorTime)
					{
						prevChunkReq.creationTime = requirorTime;
					}
				}
				return false;
			}
			return true;
		}

		public long ChunkIndex3D(int chunkX, int chunkY, int chunkZ)
		{
			return ((long)chunkY * (long)this.server.WorldMap.index3dMulZ + (long)chunkZ) * (long)this.server.WorldMap.index3dMulX + (long)chunkX;
		}

		public long ChunkIndex3D(EntityPos pos)
		{
			return this.server.WorldMap.ChunkIndex3D(pos);
		}

		internal GameDatabase gameDatabase;

		internal ConcurrentIndexedFifoQueue<ChunkColumnLoadRequest> requestedChunkColumns;

		internal IndexedFifoQueue<ChunkColumnLoadRequest> peekingChunkColumns;

		internal ServerSystemSupplyChunks loadsavechunks;

		internal ServerSystemLoadAndSaveGame loadsavegame;

		internal IBlockAccessor worldgenBlockAccessor;

		public bool runOffThreadSaveNow;

		public bool BackupInProgress;

		public bool peekMode;

		public int additionalWorldGenThreadsCount;

		private bool additionalThreadsPaused;
	}
}
