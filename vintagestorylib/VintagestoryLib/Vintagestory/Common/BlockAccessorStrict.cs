using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common
{
	internal class BlockAccessorStrict : BlockAccessorBase
	{
		public BlockAccessorStrict(WorldMap worldmap, IWorldAccessor worldAccessor, bool synchronize, bool relight, bool debug)
			: base(worldmap, worldAccessor)
		{
			this.synchronize = synchronize;
			this.relight = relight;
			this.debug = debug;
		}

		public override int GetBlockId(int posX, int posY, int posZ, int layer)
		{
			if ((posX | posY | posZ) < 0 || posX >= this.worldmap.MapSizeX || posZ >= this.worldmap.MapSizeZ)
			{
				this.worldmap.Logger.VerboseDebug("Tried to get block outside map! (at pos {0}, {1}, {2})", new object[] { posX, posY, posZ });
				return 0;
			}
			IWorldChunk chunk = this.worldmap.GetChunkAtPos(posX, posY, posZ);
			if (chunk != null)
			{
				return chunk.UnpackAndReadBlock(this.worldmap.ChunkSizedIndex3D(posX & 31, posY & 31, posZ & 31), layer);
			}
			this.worldmap.Logger.VerboseDebug("Tried to get block outside loaded chunks! (at pos {0}, {1}, {2} = chunk {3}, {4}, {5})", new object[]
			{
				posX,
				posY,
				posZ,
				posX / 32,
				posY / 32,
				posZ / 32
			});
			if (this.debug)
			{
				this.worldmap.PrintChunkMap(new Vec2i(posX / 32, posZ / 32));
				throw new AccessViolationException("Tried to get block outside loaded chunks. Current chunk map exported for debug");
			}
			return 0;
		}

		public override Block GetBlockOrNull(int posX, int posY, int posZ, int layer = 4)
		{
			if ((posX | posY | posZ) < 0 || posX >= this.worldmap.MapSizeX || posZ >= this.worldmap.MapSizeZ)
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

		public override void SetBlock(int newblockId, BlockPos pos, ItemStack byItemstack = null)
		{
			if ((pos.X | pos.Y | pos.Z) < 0 || (pos.dimension != 1 && (pos.X >= this.worldmap.MapSizeX || pos.Y >= this.worldmap.MapSizeY || pos.Z >= this.worldmap.MapSizeZ)))
			{
				this.worldmap.Logger.Notification("Tried to set block outside map! (at pos {0}, {1}, {2})", new object[] { pos.X, pos.Y, pos.Z });
				return;
			}
			IWorldChunk chunk = this.worldmap.GetChunk(pos);
			if (chunk != null)
			{
				chunk.Unpack();
				base.SetBlockInternal(newblockId, pos, chunk, this.synchronize, this.relight, 0, byItemstack);
				return;
			}
			this.worldmap.Logger.VerboseDebug("Tried to set block outside loaded chunks! (at pos {0}, {1}, {2} = chunk {3}, {4}, {5})", new object[]
			{
				pos.X,
				pos.Y,
				pos.Z,
				pos.X / 32,
				pos.Y / 32,
				pos.Z / 32
			});
			if (this.debug)
			{
				this.worldmap.PrintChunkMap(new Vec2i(pos.X / 32, pos.Z / 32));
				throw new AccessViolationException("Tried to set block outside loaded chunks. Current chunk map exported for debug");
			}
		}

		public override void SetBlock(int fluidBlockid, BlockPos pos, int layer)
		{
			if ((pos.X | pos.Y | pos.Z) < 0 || (pos.dimension != 1 && (pos.X >= this.worldmap.MapSizeX || pos.Y >= this.worldmap.MapSizeY || pos.Z >= this.worldmap.MapSizeZ)))
			{
				this.worldmap.Logger.Notification("Tried to set liquid block outside map! (at pos {0}, {1}, {2})", new object[] { pos.X, pos.Y, pos.Z });
				return;
			}
			IWorldChunk chunk = this.worldmap.GetChunk(pos);
			if (chunk != null)
			{
				chunk.Unpack();
				base.SetBlockInternal(fluidBlockid, pos, chunk, this.synchronize, this.relight, layer, null);
				return;
			}
		}

		public override void ExchangeBlock(int blockId, BlockPos pos)
		{
			if ((pos.X | pos.Y | pos.Z) < 0 || (pos.dimension != 1 && (pos.X >= this.worldmap.MapSizeX || pos.Y >= this.worldmap.MapSizeY || pos.Z >= this.worldmap.MapSizeZ)))
			{
				return;
			}
			IWorldChunk chunk = this.worldmap.GetChunkAtPos(pos.X, pos.InternalY, pos.Z);
			if (chunk == null)
			{
				return;
			}
			chunk.Unpack();
			int index3d = this.worldmap.ChunkSizedIndex3D(pos.X & 31, pos.InternalY & 31, pos.Z & 31);
			int oldblockid = chunk.Data[index3d];
			chunk.Data[index3d] = blockId;
			chunk.MarkModified();
			Block block = this.worldmap.Blocks[blockId];
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
				this.worldmap.UpdateLighting(oldblockid, blockId, new BlockPos(pos.X, pos.InternalY, pos.Z));
			}
		}

		private bool synchronize;

		private bool relight;

		private bool debug;
	}
}
