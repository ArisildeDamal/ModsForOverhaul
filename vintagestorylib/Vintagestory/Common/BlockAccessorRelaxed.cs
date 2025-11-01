using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common
{
	public class BlockAccessorRelaxed : BlockAccessorBase
	{
		public BlockAccessorRelaxed(WorldMap worldmap, IWorldAccessor worldAccessor, bool synchronize, bool relight)
			: base(worldmap, worldAccessor)
		{
			this.synchronize = synchronize;
			this.relight = relight;
		}

		public override int GetBlockId(int posX, int posY, int posZ, int layer)
		{
			if ((posX | posY | posZ) < 0)
			{
				return 0;
			}
			IWorldChunk chunk = this.worldmap.GetChunkAtPos(posX, posY, posZ);
			if (chunk != null)
			{
				int index3d = this.worldmap.ChunkSizedIndex3D(posX & 31, posY & 31, posZ & 31);
				return chunk.UnpackAndReadBlock(index3d, layer);
			}
			return 0;
		}

		public override Block GetBlockOrNull(int posX, int posY, int posZ, int layer = 4)
		{
			if ((posX | posY | posZ) < 0)
			{
				return null;
			}
			IWorldChunk chunk = this.worldmap.GetChunkAtPos(posX, posY, posZ);
			if (chunk != null)
			{
				return this.worldmap.Blocks[chunk.UnpackAndReadBlock(this.worldmap.ChunkSizedIndex3D(posX & 31, posY & 31, posZ & 31), layer)];
			}
			return null;
		}

		public override void SetBlock(int blockId, BlockPos pos, ItemStack byItemstack = null)
		{
			if ((pos.X | pos.Y | pos.Z) < 0)
			{
				return;
			}
			IWorldChunk chunk = this.worldmap.GetChunk(pos);
			if (chunk == null)
			{
				return;
			}
			chunk.Unpack();
			base.SetBlockInternal(blockId, pos, chunk, this.synchronize, this.relight, 0, byItemstack);
		}

		public override void SetBlock(int blockId, BlockPos pos, int layer)
		{
			if ((pos.X | pos.Y | pos.Z) < 0)
			{
				return;
			}
			IWorldChunk chunk = this.worldmap.GetChunk(pos);
			if (chunk == null)
			{
				return;
			}
			chunk.Unpack();
			base.SetBlockInternal(blockId, pos, chunk, this.synchronize, this.relight, layer, null);
		}

		public override void ExchangeBlock(int blockId, BlockPos pos)
		{
			if ((pos.X | pos.Y | pos.Z) < 0)
			{
				return;
			}
			IWorldChunk chunk = this.worldmap.GetChunk(pos);
			if (chunk != null)
			{
				chunk.Unpack();
				int index3d = this.worldmap.ChunkSizedIndex3D(pos.X & 31, pos.Y & 31, pos.Z & 31);
				Block block = this.worldmap.Blocks[blockId];
				int oldblockid;
				if (block.ForFluidsLayer)
				{
					oldblockid = (chunk.Data as ChunkData).GetFluid(index3d);
					(chunk.Data as ChunkData).SetFluid(index3d, blockId);
				}
				else
				{
					oldblockid = (chunk.Data as ChunkData).GetSolidBlock(index3d);
					chunk.Data[index3d] = blockId;
				}
				this.worldmap.MarkChunkDirty(pos.X / 32, pos.InternalY / 32, pos.Z / 32, true, false, null, true, false);
				if (!block.ForFluidsLayer)
				{
					BlockEntity localBlockEntityAtBlockPos = chunk.GetLocalBlockEntityAtBlockPos(pos);
					if (localBlockEntityAtBlockPos != null)
					{
						localBlockEntityAtBlockPos.OnExchanged(block);
					}
				}
				if (this.synchronize)
				{
					this.worldmap.SendExchangeBlock(blockId, pos.X, pos.InternalY, pos.Z);
				}
				if (this.relight)
				{
					this.worldmap.UpdateLighting(oldblockid, blockId, pos);
				}
			}
		}

		protected bool synchronize;

		protected bool relight;
	}
}
