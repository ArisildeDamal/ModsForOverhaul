using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Common.Database;

namespace Vintagestory.Common
{
	public class BlockAccessorBulkMinimalUpdate : BlockAccessorRelaxedBulkUpdate
	{
		public BlockAccessorBulkMinimalUpdate(WorldMap worldmap, IWorldAccessor worldAccessor, bool synchronize, bool debug)
			: base(worldmap, worldAccessor, synchronize, false, debug)
		{
			this.debug = debug;
			if (worldAccessor.Side == EnumAppSide.Client)
			{
				this.dirtyNeighbourChunkPositions = new HashSet<Xyz>();
			}
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
			int prevChunkX = -1;
			int prevChunkY = -1;
			int prevChunkZ = -1;
			this.dirtyChunkPositions.Clear();
			HashSet<Xyz> hashSet = this.dirtyNeighbourChunkPositions;
			if (hashSet != null)
			{
				hashSet.Clear();
			}
			WorldMap worldmap = this.worldmap;
			IList<Block> blockList = worldmap.Blocks;
			foreach (KeyValuePair<BlockPos, BlockUpdate> val in this.StagedBlocks)
			{
				BlockUpdate blockUpdate = val.Value;
				int newblockid = blockUpdate.NewSolidBlockId;
				if (newblockid >= 0 || blockUpdate.NewFluidBlockId >= 0)
				{
					BlockPos pos = val.Key;
					int chunkX = pos.X / 32;
					int chunkY = pos.Y / 32;
					int chunkZ = pos.Z / 32;
					if (this.dirtyNeighbourChunkPositions != null)
					{
						if ((pos.X + 1) % 32 < 2)
						{
							this.dirtyNeighbourChunkPositions.Add(new Xyz((pos.X % 32 == 0) ? (chunkX - 1) : (chunkX + 1), chunkY, chunkZ));
						}
						if ((pos.Y + 1) % 32 < 2)
						{
							this.dirtyNeighbourChunkPositions.Add(new Xyz(chunkX, (pos.Y % 32 == 0) ? (chunkY - 1) : (chunkY + 1), chunkZ));
						}
						if ((pos.Z + 1) % 32 < 2)
						{
							this.dirtyNeighbourChunkPositions.Add(new Xyz(chunkX, chunkY, (pos.Z % 32 == 0) ? (chunkZ - 1) : (chunkZ + 1)));
						}
					}
					if (chunkX != prevChunkX || chunkY != prevChunkY || chunkZ != prevChunkZ)
					{
						chunk = worldmap.GetChunk(prevChunkX = chunkX, prevChunkY = chunkY, prevChunkZ = chunkZ);
						if (chunk == null)
						{
							continue;
						}
						chunk.Unpack();
						this.dirtyChunkPositions.Add(new ChunkPosCompact(chunkX, chunkY, chunkZ));
						int belowChunkY = (pos.Y - 1) / 32;
						if (belowChunkY != chunkY && belowChunkY >= 0)
						{
							this.dirtyChunkPositions.Add(new ChunkPosCompact(chunkX, belowChunkY, chunkZ));
						}
						if (newblockid > 0 || blockUpdate.NewFluidBlockId > 0)
						{
							chunk.Empty = false;
						}
					}
					if (chunk != null)
					{
						int index3d = worldmap.ChunkSizedIndex3D(pos.X & 31, pos.Y & 31, pos.Z & 31);
						Block newBlock = null;
						if (blockUpdate.NewSolidBlockId >= 0)
						{
							int oldid = (blockUpdate.OldBlockId = chunk.Data[index3d]);
							if (!blockUpdate.ExchangeOnly)
							{
								blockList[oldid].OnBlockRemoved(this.worldAccessor, pos);
							}
							chunk.Data[index3d] = newblockid;
							newBlock = blockList[newblockid];
							if (!blockUpdate.ExchangeOnly)
							{
								newBlock.OnBlockPlaced(this.worldAccessor, pos, null);
							}
						}
						if (blockUpdate.NewFluidBlockId >= 0)
						{
							if (blockUpdate.NewSolidBlockId < 0)
							{
								blockUpdate.OldBlockId = chunk.Data.GetFluid(index3d);
							}
							chunk.Data.SetFluid(index3d, blockUpdate.NewFluidBlockId);
							if (blockUpdate.NewFluidBlockId > 0 || newBlock == null)
							{
								newBlock = blockList[blockUpdate.NewFluidBlockId];
							}
						}
						if (blockUpdate.ExchangeOnly && newBlock.EntityClass != null)
						{
							BlockEntity localBlockEntityAtBlockPos = chunk.GetLocalBlockEntityAtBlockPos(pos);
							if (localBlockEntityAtBlockPos != null)
							{
								localBlockEntityAtBlockPos.OnExchanged(newBlock);
							}
						}
						base.UpdateRainHeightMap(blockList[blockUpdate.OldBlockId], newBlock, pos, chunk.MapChunk);
					}
				}
			}
			foreach (ChunkPosCompact cp in this.dirtyChunkPositions)
			{
				worldmap.MarkChunkDirty(cp.X, cp.Y, cp.Z, false, false, null, true, false);
			}
			if (this.dirtyNeighbourChunkPositions != null)
			{
				foreach (Xyz cp2 in this.dirtyNeighbourChunkPositions)
				{
					worldmap.MarkChunkDirty(cp2.X, cp2.Y, cp2.Z, false, false, null, false, true);
				}
			}
			if (this.synchronize)
			{
				worldmap.SendBlockUpdateBulkMinimal(this.StagedBlocks);
			}
			this.StagedBlocks.Clear();
			this.dirtyChunkPositions.Clear();
		}

		protected HashSet<Xyz> dirtyNeighbourChunkPositions;
	}
}
