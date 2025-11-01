using System;

namespace Vintagestory.Server
{
	internal class ChunkLookupRequest
	{
		public ChunkLookupRequest(int chunkX, int chunkY, int chunkZ, Action<bool> onTested)
		{
			this.chunkX = chunkX;
			this.chunkY = chunkY;
			this.chunkZ = chunkZ;
			this.onTested = onTested;
		}

		public EnumChunkType Type;

		public int chunkX;

		public int chunkY;

		public int chunkZ;

		public Action<bool> onTested;
	}
}
