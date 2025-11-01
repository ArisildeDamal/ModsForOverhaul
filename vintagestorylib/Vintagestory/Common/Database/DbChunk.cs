using System;

namespace Vintagestory.Common.Database
{
	public class DbChunk
	{
		public DbChunk()
		{
		}

		public DbChunk(ChunkPos pos, byte[] data)
		{
			this.Position = pos;
			this.Data = data;
		}

		public ChunkPos Position;

		public byte[] Data;
	}
}
