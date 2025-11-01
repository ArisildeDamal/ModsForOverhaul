using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace Vintagestory.Common
{
	public class BlockAccessorCaching : BlockAccessorRelaxed, ICachingBlockAccessor, IBlockAccessor
	{
		public bool LastChunkLoaded { get; private set; }

		public BlockAccessorCaching(WorldMap worldmap, IWorldAccessor worldAccessor, bool synchronize, bool relight)
			: base(worldmap, worldAccessor, synchronize, relight)
		{
			ClientCoreAPI capi = worldAccessor.Api as ClientCoreAPI;
			if (capi != null)
			{
				capi.eventapi.LeftWorld += this.Dispose;
			}
		}

		public override int GetBlockId(int posX, int posY, int posZ, int layer)
		{
			if ((posX | posY | posZ) < 0 || posX >= this.worldmap.MapSizeX || posZ >= this.worldmap.MapSizeZ)
			{
				return 0;
			}
			long nowChunkIndex3d = this.worldmap.ChunkIndex3D(posX / 32, posY / 32, posZ / 32);
			if (this.chunkIndex3d != nowChunkIndex3d)
			{
				if (this.chunk2Index3d == nowChunkIndex3d)
				{
					this.chunk2Index3d = this.chunkIndex3d;
					this.chunkIndex3d = nowChunkIndex3d;
					IWorldChunk tmp = this.chunk2;
					this.chunk2 = this.chunk;
					this.chunk = tmp;
					if (tmp != null)
					{
						this.LastChunkLoaded = true;
						tmp.Unpack();
						this.chunkDataBlocks = (tmp as WorldChunk).Data;
					}
					else
					{
						this.LastChunkLoaded = false;
					}
				}
				else
				{
					this.chunk2Index3d = this.chunkIndex3d;
					this.chunk2 = this.chunk;
					this.chunkIndex3d = nowChunkIndex3d;
					IWorldChunk tmp2 = (this.chunk = this.worldmap.GetChunk(nowChunkIndex3d));
					if (tmp2 != null)
					{
						this.LastChunkLoaded = true;
						tmp2.Unpack();
						this.chunkDataBlocks = (tmp2 as WorldChunk).Data;
					}
					else
					{
						this.LastChunkLoaded = false;
					}
				}
			}
			IWorldChunk tmp3 = this.chunk;
			if (tmp3 != null)
			{
				tmp3.MarkFresh();
				int index = this.worldmap.ChunkSizedIndex3D(posX & 31, posY & 31, posZ & 31);
				return (tmp3 as WorldChunk).Data.GetBlockId(index, layer);
			}
			return 0;
		}

		public override void SetBlock(int blockId, BlockPos pos, ItemStack byItemstack = null)
		{
			if ((pos.X | pos.Y | pos.Z) < 0 || (pos.dimension == 0 && (pos.X >= this.worldmap.MapSizeX || pos.Y >= this.worldmap.MapSizeY || pos.Z >= this.worldmap.MapSizeZ)))
			{
				return;
			}
			int cx = pos.X / 32;
			int cy = pos.Y / 32;
			int cz = pos.Z / 32;
			long nowChunkIndex3d = this.worldmap.ChunkIndex3D(cx, cy, cz);
			if (this.chunkIndex3d != nowChunkIndex3d)
			{
				if (this.chunk2Index3d == nowChunkIndex3d)
				{
					this.chunk2Index3d = this.chunkIndex3d;
					this.chunkIndex3d = nowChunkIndex3d;
					IWorldChunk tmp = this.chunk2;
					this.chunk2 = this.chunk;
					this.chunk = tmp;
				}
				else
				{
					this.chunk2Index3d = this.chunkIndex3d;
					this.chunkIndex3d = nowChunkIndex3d;
					this.chunk2 = this.chunk;
					this.chunk = this.worldmap.GetChunk(this.chunkIndex3d);
					this.chunk.Unpack();
				}
				if (this.chunk != null)
				{
					this.chunkDataBlocks = (this.chunk as WorldChunk).Data;
				}
			}
			if (this.chunk != null)
			{
				this.LastChunkLoaded = true;
				base.SetBlockInternal(blockId, pos, this.chunk, this.synchronize, this.relight, 0, byItemstack);
				return;
			}
			this.LastChunkLoaded = false;
		}

		public override void SetBlock(int blockId, BlockPos pos, int layer)
		{
			if ((pos.X | pos.Y | pos.Z) < 0 || (pos.dimension == 0 && (pos.X >= this.worldmap.MapSizeX || pos.Y >= this.worldmap.MapSizeY || pos.Z >= this.worldmap.MapSizeZ)))
			{
				return;
			}
			int cx = pos.X / 32;
			int cy = pos.Y / 32;
			int cz = pos.Z / 32;
			long nowChunkIndex3d = this.worldmap.ChunkIndex3D(cx, cy, cz);
			if (this.chunkIndex3d != nowChunkIndex3d)
			{
				if (this.chunk2Index3d == nowChunkIndex3d)
				{
					this.chunk2Index3d = this.chunkIndex3d;
					this.chunkIndex3d = nowChunkIndex3d;
					IWorldChunk tmp = this.chunk2;
					this.chunk2 = this.chunk;
					this.chunk = tmp;
				}
				else
				{
					this.chunk2Index3d = this.chunkIndex3d;
					this.chunkIndex3d = nowChunkIndex3d;
					this.chunk2 = this.chunk;
					this.chunk = this.worldmap.GetChunk(this.chunkIndex3d);
					this.chunk.Unpack();
				}
				if (this.chunk != null)
				{
					this.chunkDataBlocks = (this.chunk as WorldChunk).Data;
				}
			}
			if (this.chunk != null)
			{
				this.LastChunkLoaded = true;
				base.SetFluidBlockInternal(blockId, pos, this.chunk, this.synchronize, this.relight);
				return;
			}
			this.LastChunkLoaded = false;
		}

		public override void ExchangeBlock(int blockId, BlockPos pos)
		{
			if ((pos.X | pos.Y | pos.Z) < 0 || (pos.dimension == 0 && (pos.X >= this.worldmap.MapSizeX || pos.Y >= this.worldmap.MapSizeY || pos.Z >= this.worldmap.MapSizeZ)))
			{
				return;
			}
			long nowChunkIndex3d = this.worldmap.ChunkIndex3D(pos.X / 32, pos.InternalY / 32, pos.Z / 32);
			if (this.chunkIndex3d != nowChunkIndex3d)
			{
				if (this.chunk2Index3d == nowChunkIndex3d)
				{
					this.chunk2Index3d = this.chunkIndex3d;
					this.chunkIndex3d = nowChunkIndex3d;
					IWorldChunk tmp = this.chunk2;
					this.chunk2 = this.chunk;
					this.chunk = tmp;
				}
				else
				{
					this.chunk2Index3d = this.chunkIndex3d;
					this.chunkIndex3d = nowChunkIndex3d;
					this.chunk2 = this.chunk;
					this.chunk = this.worldmap.GetChunk(this.chunkIndex3d);
					this.chunk.Unpack();
				}
				if (this.chunk != null)
				{
					this.chunkDataBlocks = (this.chunk as WorldChunk).Data;
				}
			}
			if (this.chunk != null)
			{
				int index3d = this.worldmap.ChunkSizedIndex3D(pos.X & 31, pos.InternalY & 31, pos.Z & 31);
				int oldblockid = this.chunk.Data[index3d];
				this.chunk.Data[index3d] = blockId;
				this.chunk.MarkModified();
				Block block = this.worldmap.Blocks[blockId];
				if (!block.ForFluidsLayer)
				{
					BlockEntity localBlockEntityAtBlockPos = this.chunk.GetLocalBlockEntityAtBlockPos(pos);
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

		public void Begin()
		{
			this.chunkIndex3d = -1L;
			this.chunk2Index3d = -1L;
		}

		public void Dispose()
		{
			this.chunk = null;
			this.chunk2 = null;
			this.chunkDataBlocks = null;
		}

		private long chunkIndex3d = -1L;

		private long chunk2Index3d = -1L;

		private IWorldChunk chunk;

		private IWorldChunk chunk2;

		private IChunkBlocks chunkDataBlocks;
	}
}
