using System;
using Vintagestory.API.MathTools;

namespace Vintagestory.Server
{
	public class ClientStatistics
	{
		internal EnumClientAwarenessEvent? DetectChanges()
		{
			EnumClientAwarenessEvent? returnEvent = null;
			BlockPos chunkPos = this.client.ChunkPos;
			if (chunkPos.X != this.lastChunkX || chunkPos.InternalY != this.lastChunkY || chunkPos.Z != this.lastChunkZ)
			{
				returnEvent = new EnumClientAwarenessEvent?(EnumClientAwarenessEvent.ChunkTransition);
			}
			this.lastChunkX = chunkPos.X;
			this.lastChunkY = chunkPos.InternalY;
			this.lastChunkZ = chunkPos.Z;
			return returnEvent;
		}

		public ConnectedClient client;

		public int lastChunkX;

		public int lastChunkY;

		public int lastChunkZ;
	}
}
