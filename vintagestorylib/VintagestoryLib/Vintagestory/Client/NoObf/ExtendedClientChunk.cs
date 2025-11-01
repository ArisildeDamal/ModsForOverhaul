using System;

namespace Vintagestory.Client.NoObf
{
	internal class ExtendedClientChunk
	{
		public ushort this[int x, int y, int z]
		{
			get
			{
				return 0;
			}
		}

		private ClientChunk[,,] chunks = new ClientChunk[3, 3, 3];
	}
}
