using System;
using System.Collections.Generic;
using System.Threading;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace Vintagestory.Server
{
	public class ChunkColumnLoadRequest : ILongIndex, IChunkColumnGenerateRequest
	{
		internal bool Disposed
		{
			get
			{
				return (this.disposeOrRequeueFlags & 1) == 1;
			}
		}

		public ChunkColumnLoadRequest(long index2d, int chunkX, int chunkZ, int clientId, int untilPass, IShutDownMonitor server)
		{
			this.mapIndex2d = index2d;
			this.clientIds = new HashSet<int>();
			this.clientIds.Add(clientId);
			this.chunkX = chunkX;
			this.chunkZ = chunkZ;
			this.untilPass = untilPass;
			this.generatingLock = new FastRWLock(server);
			long num = ChunkColumnLoadRequest.counter;
			ChunkColumnLoadRequest.counter = num + 1L;
			this.creationTime = num;
		}

		public long Index
		{
			get
			{
				return this.mapIndex2d;
			}
		}

		public EnumWorldGenPass GenerateUntilPass
		{
			get
			{
				return (EnumWorldGenPass)this.untilPass;
			}
		}

		public EnumWorldGenPass CurrentIncompletePass
		{
			get
			{
				if (this.MapChunk != null)
				{
					return this.MapChunk.CurrentIncompletePass;
				}
				return EnumWorldGenPass.None;
			}
			set
			{
				this.MapChunk.CurrentIncompletePass = value;
			}
		}

		public int CurrentIncompletePass_AsInt
		{
			get
			{
				if (this.MapChunk != null)
				{
					return this.MapChunk.currentpass;
				}
				return 0;
			}
		}

		IServerChunk[] IChunkColumnGenerateRequest.Chunks
		{
			get
			{
				return this.Chunks;
			}
		}

		public int ChunkX
		{
			get
			{
				return this.chunkX;
			}
		}

		public int ChunkZ
		{
			get
			{
				return this.chunkZ;
			}
		}

		public ITreeAttribute ChunkGenParams
		{
			get
			{
				return this.chunkGenParams;
			}
		}

		public ushort[][] NeighbourTerrainHeight { get; set; }

		public bool RequiresChunkBorderSmoothing { get; set; }

		public void FlagToDispose()
		{
			Interlocked.Or(ref this.disposeOrRequeueFlags, 1);
		}

		public void FlagToRequeue()
		{
			Interlocked.Or(ref this.disposeOrRequeueFlags, 2);
		}

		internal void Unpack()
		{
			ServerChunk[] chunks = this.Chunks;
			if (chunks == null)
			{
				return;
			}
			for (int i = 0; i < chunks.Length; i++)
			{
				chunks[i].Unpack();
			}
		}

		internal void PackAndCommit()
		{
			ServerChunk[] chunks = this.Chunks;
			if (chunks == null)
			{
				return;
			}
			for (int i = 0; i < chunks.Length; i++)
			{
				chunks[i].TryPackAndCommit(8000);
			}
		}

		internal long LastReadWrite()
		{
			ServerChunk[] chunks = this.Chunks;
			if (chunks == null)
			{
				return 0L;
			}
			return chunks[0].lastReadOrWrite;
		}

		internal bool IsPacked()
		{
			ServerChunk[] chunks = this.Chunks;
			return chunks == null || chunks[0].IsPacked();
		}

		internal long mapIndex2d;

		internal HashSet<int> clientIds;

		internal ServerChunk[] Chunks;

		internal ServerMapChunk MapChunk;

		internal int untilPass;

		internal int chunkX;

		internal int chunkZ;

		internal int dimension;

		internal ITreeAttribute chunkGenParams;

		internal int disposeOrRequeueFlags;

		internal FastRWLock generatingLock;

		internal long creationTime;

		internal bool prettified;

		internal bool blockingRequest;

		private static long counter;
	}
}
