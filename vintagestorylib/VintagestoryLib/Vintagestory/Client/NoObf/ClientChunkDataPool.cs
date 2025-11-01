using System;
using Vintagestory.API.Common;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf
{
	public class ClientChunkDataPool : ChunkDataPool
	{
		public override bool ShuttingDown
		{
			get
			{
				return this.game.threadsShouldExit;
			}
		}

		public override GameMain Game
		{
			get
			{
				return this.game;
			}
		}

		public override ILogger Logger
		{
			get
			{
				return this.game.Logger;
			}
		}

		public ClientChunkDataPool(int chunksize, ClientMain game)
		{
			this.chunksize = chunksize;
			this.BlackHoleData = ClientChunkData.CreateNew(chunksize, this);
			this.OnlyAirBlocksData = NoChunkData.CreateNew(chunksize);
			this.game = game;
		}

		public override ChunkData Request()
		{
			this.quantityRequestsSinceLastSlowDispose++;
			return ClientChunkData.CreateNew(this.chunksize, this);
		}

		public ClientMain game;
	}
}
