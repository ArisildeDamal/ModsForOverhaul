using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common
{
	public class BlockAccessorMapChunkLoading : BlockAccessorRelaxedBulkUpdate, IBulkBlockAccessor, IBlockAccessor
	{
		public BlockAccessorMapChunkLoading(WorldMap worldmap, IWorldAccessor worldAccessor, bool synchronize, bool debug)
			: base(worldmap, worldAccessor, synchronize, false, debug)
		{
			this.debug = debug;
		}

		public new void SetChunks(Vec2i chunkCoord, IWorldChunk[] chunksCol)
		{
			this.chunks = chunksCol;
			this.chunkX = chunkCoord.X;
			this.chunkZ = chunkCoord.Y;
		}

		public override List<BlockUpdate> Commit()
		{
			this.FastCommit();
			return null;
		}

		public void FastCommit()
		{
			base.ReadFromStagedByDefault = false;
			IWorldChunk chunk = null;
			this.dirtyChunkPositions.Clear();
			int prevChunkY = -99999;
			foreach (KeyValuePair<BlockPos, BlockUpdate> val in this.StagedBlocks)
			{
				int newblockid = val.Value.NewSolidBlockId;
				BlockPos pos = val.Key;
				int chunkY = pos.Y / 32;
				if (chunkY != prevChunkY)
				{
					chunk = this.chunks[chunkY];
					chunk.Unpack();
					chunk.MarkModified();
					int belowChunkY = (pos.Y - 1) / 32;
					if (belowChunkY != chunkY && belowChunkY >= 0)
					{
						this.chunks[belowChunkY].MarkModified();
					}
					if (newblockid > 0 || val.Value.NewFluidBlockId > 0)
					{
						chunk.Empty = false;
					}
					prevChunkY = chunkY;
				}
				int index3d = this.worldmap.ChunkSizedIndex3D(pos.X & 31, pos.Y & 31, pos.Z & 31);
				Block newBlock = null;
				if (val.Value.NewSolidBlockId >= 0)
				{
					val.Value.OldBlockId = chunk.Data[index3d];
					chunk.Data[index3d] = newblockid;
					newBlock = this.worldmap.Blocks[newblockid];
				}
				if (val.Value.NewFluidBlockId >= 0)
				{
					if (val.Value.NewSolidBlockId < 0)
					{
						val.Value.OldBlockId = chunk.Data.GetFluid(index3d);
					}
					chunk.Data.SetFluid(index3d, val.Value.NewFluidBlockId);
					if (val.Value.NewFluidBlockId > 0 || newBlock == null)
					{
						newBlock = this.worldmap.Blocks[val.Value.NewFluidBlockId];
					}
				}
				base.UpdateRainHeightMap(this.worldmap.Blocks[val.Value.OldBlockId], newBlock, pos, chunk.MapChunk);
			}
			this.StagedBlocks.Clear();
		}

		protected override int GetNonStagedBlockId(int posX, int posY, int posZ, int layer)
		{
			if ((posX | posY | posZ) < 0 || posX >= this.worldmap.MapSizeX || posZ >= this.worldmap.MapSizeZ)
			{
				return 0;
			}
			IWorldChunk chunk;
			if (posX / 32 == this.chunkX && posZ / 32 == this.chunkZ)
			{
				chunk = this.chunks[posY / 32];
			}
			else
			{
				chunk = this.worldmap.GetChunkAtPos(posX, posY, posZ);
			}
			if (chunk != null)
			{
				return chunk.UnpackAndReadBlock(this.worldmap.ChunkSizedIndex3D(posX & 31, posY & 31, posZ & 31), layer);
			}
			return 0;
		}

		private int chunkX;

		private int chunkZ;

		private IWorldChunk[] chunks;
	}
}
