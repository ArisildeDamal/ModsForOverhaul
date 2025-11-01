using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.Common.Database;

namespace Vintagestory.Common
{
	public class BlockAccessorRelaxedBulkUpdate : BlockAccessorBase, IBulkBlockAccessor, IBlockAccessor
	{
		public event Action<IBulkBlockAccessor> BeforeCommit;

		public bool ReadFromStagedByDefault { get; set; }

		Dictionary<BlockPos, BlockUpdate> IBulkBlockAccessor.StagedBlocks
		{
			get
			{
				return this.StagedBlocks;
			}
		}

		public BlockAccessorRelaxedBulkUpdate(WorldMap worldmap, IWorldAccessor worldAccessor, bool synchronize, bool relight, bool debug)
			: base(worldmap, worldAccessor)
		{
			this.synchronize = synchronize;
			this.relight = relight;
			this.debug = debug;
		}

		public override int GetBlockId(int posX, int posY, int posZ, int layer)
		{
			BlockUpdate bd;
			if (this.ReadFromStagedByDefault && this.StagedBlocks.TryGetValue(new BlockPos(posX, posY, posZ), out bd))
			{
				switch (layer)
				{
				default:
					if (bd.NewSolidBlockId >= 0)
					{
						return bd.NewSolidBlockId;
					}
					break;
				case 2:
				case 3:
					if (bd.NewFluidBlockId >= 0)
					{
						return bd.NewFluidBlockId;
					}
					break;
				case 4:
					return this.GetMostSolidBlock(posX, posY, posZ).Id;
				}
			}
			return this.GetNonStagedBlockId(posX, posY, posZ, layer);
		}

		public override int GetBlockId(BlockPos pos, int layer)
		{
			BlockUpdate bd;
			if (this.ReadFromStagedByDefault && this.StagedBlocks.TryGetValue(pos, out bd))
			{
				switch (layer)
				{
				default:
					if (bd.NewSolidBlockId >= 0)
					{
						return bd.NewSolidBlockId;
					}
					break;
				case 2:
				case 3:
					if (bd.NewFluidBlockId >= 0)
					{
						return bd.NewFluidBlockId;
					}
					break;
				case 4:
					return this.GetMostSolidBlock(pos).Id;
				}
			}
			return this.GetNonStagedBlockId(pos.X, pos.InternalY, pos.Z, layer);
		}

		public override Block GetMostSolidBlock(int x, int y, int z)
		{
			BlockUpdate bd;
			if (this.ReadFromStagedByDefault && this.StagedBlocks.TryGetValue(new BlockPos(x, y, z), out bd))
			{
				if (bd.NewSolidBlockId >= 0)
				{
					return this.worldmap.Blocks[bd.NewSolidBlockId];
				}
				if (bd.NewFluidBlockId > 0)
				{
					Block block = this.worldmap.Blocks[bd.NewFluidBlockId];
					if (block.SideSolid.Any)
					{
						return block;
					}
				}
			}
			return base.GetMostSolidBlock(x, y, z);
		}

		protected virtual int GetNonStagedBlockId(int posX, int posY, int posZ, int layer)
		{
			if ((posX | posY | posZ) < 0 || posX >= this.worldmap.MapSizeX || posZ >= this.worldmap.MapSizeZ)
			{
				return 0;
			}
			IWorldChunk chunk = this.worldmap.GetChunkAtPos(posX, posY, posZ);
			if (chunk != null)
			{
				return chunk.UnpackAndReadBlock(this.worldmap.ChunkSizedIndex3D(posX & 31, posY & 31, posZ & 31), layer);
			}
			return 0;
		}

		public override Block GetBlockOrNull(int posX, int posY, int posZ, int layer = 4)
		{
			if ((posX | posY | posZ) < 0 || posX >= this.worldmap.MapSizeX || posZ >= this.worldmap.MapSizeZ)
			{
				return null;
			}
			BlockUpdate bd;
			if (this.ReadFromStagedByDefault && this.StagedBlocks.TryGetValue(new BlockPos(posX, posY, posZ), out bd) && bd.NewSolidBlockId >= 0)
			{
				return this.worldmap.Blocks[bd.NewSolidBlockId];
			}
			IWorldChunk chunk = this.worldmap.GetChunkAtPos(posX, posY, posZ);
			if (chunk != null)
			{
				return this.worldmap.Blocks[chunk.UnpackAndReadBlock(this.worldmap.ChunkSizedIndex3D(posX & 31, posY & 31, posZ & 31), layer)];
			}
			return null;
		}

		public override void SetBlock(int newBlockId, BlockPos pos, ItemStack byItemstack = null)
		{
			if (this.worldmap.Blocks[newBlockId].ForFluidsLayer)
			{
				this.SetFluidBlock(newBlockId, pos);
				return;
			}
			if ((pos.X | pos.Y | pos.Z) < 0 || (pos.dimension == 0 && (pos.X >= this.worldmap.MapSizeX || pos.Y >= this.worldmap.MapSizeY || pos.Z >= this.worldmap.MapSizeZ)))
			{
				return;
			}
			BlockUpdate bu;
			if (this.StagedBlocks.TryGetValue(pos, out bu))
			{
				bu.NewSolidBlockId = newBlockId;
				bu.ByStack = byItemstack;
				return;
			}
			BlockPos copied = pos.Copy();
			this.StagedBlocks[copied] = new BlockUpdate
			{
				NewSolidBlockId = newBlockId,
				ByStack = byItemstack,
				Pos = copied
			};
		}

		public override void SetBlock(int blockId, BlockPos pos, int layer)
		{
			if (layer == 2)
			{
				this.SetFluidBlock(blockId, pos);
				return;
			}
			if (layer == 1)
			{
				base.SetBlock(blockId, pos);
				return;
			}
			throw new ArgumentException("Layer must be solid or fluid");
		}

		public void SetFluidBlock(int blockId, BlockPos pos)
		{
			if ((pos.X | pos.Y | pos.Z) < 0 || (pos.dimension == 0 && (pos.X >= this.worldmap.MapSizeX || pos.Y >= this.worldmap.MapSizeY || pos.Z >= this.worldmap.MapSizeZ)))
			{
				return;
			}
			BlockUpdate bu;
			if (this.StagedBlocks.TryGetValue(pos, out bu))
			{
				bu.NewFluidBlockId = blockId;
				return;
			}
			BlockPos copied = pos.Copy();
			this.StagedBlocks[copied] = new BlockUpdate
			{
				NewFluidBlockId = blockId,
				Pos = copied
			};
		}

		public override bool SetDecor(Block block, BlockPos pos, BlockFacing onFace)
		{
			return this.SetDecor(block, pos, new DecorBits(onFace));
		}

		public override bool SetDecor(Block block, BlockPos pos, int decorIndex)
		{
			if ((pos.X | pos.Y | pos.Z) < 0 || (pos.dimension == 0 && (pos.X >= this.worldmap.MapSizeX || pos.Y >= this.worldmap.MapSizeY || pos.Z >= this.worldmap.MapSizeZ)))
			{
				return false;
			}
			DecorUpdate decorUpdate = new DecorUpdate
			{
				faceAndSubposition = decorIndex,
				decorId = block.Id
			};
			BlockUpdate blockUpdate;
			if (this.StagedBlocks.TryGetValue(pos, out blockUpdate))
			{
				BlockUpdate blockUpdate2 = blockUpdate;
				if (blockUpdate2.Decors == null)
				{
					blockUpdate2.Decors = new List<DecorUpdate>();
				}
				blockUpdate.Decors.Add(new DecorUpdate
				{
					faceAndSubposition = decorIndex,
					decorId = block.Id
				});
			}
			else
			{
				BlockPos copied = pos.Copy();
				List<DecorUpdate> list = new List<DecorUpdate>();
				list.Add(decorUpdate);
				this.StagedBlocks[copied] = new BlockUpdate
				{
					Pos = copied,
					Decors = list
				};
			}
			return true;
		}

		public override List<BlockUpdate> Commit()
		{
			Action<IBulkBlockAccessor> beforeCommit = this.BeforeCommit;
			if (beforeCommit != null)
			{
				beforeCommit(this);
			}
			this.ReadFromStagedByDefault = false;
			IWorldChunk chunk = null;
			int prevChunkX = -1;
			int prevChunkY = -1;
			int prevChunkZ = -1;
			List<BlockUpdate> updatedBlocks = new List<BlockUpdate>(this.StagedBlocks.Count);
			HashSet<BlockPos> updatedBlockPositions = new HashSet<BlockPos>();
			List<BlockPos> updatedDecorPositions = new List<BlockPos>();
			this.dirtyChunkPositions.Clear();
			WorldMap worldmap = this.worldmap;
			IList<Block> blockList = worldmap.Blocks;
			if (this._blockBreakTasks.Count == 0 && this.StagedBlocks.Count == 0 && this.LightSources.Count == 0)
			{
				return updatedBlocks;
			}
			foreach (BlockBreakTask val in this._blockBreakTasks)
			{
				BlockPos pos = val.Pos;
				int chunkX = pos.X / 32;
				int chunkY = pos.InternalY / 32;
				int chunkZ = pos.Z / 32;
				bool newChunk = false;
				if (chunkX != prevChunkX || chunkY != prevChunkY || chunkZ != prevChunkZ)
				{
					chunk = worldmap.GetChunk(prevChunkX = chunkX, prevChunkY = chunkY, prevChunkZ = chunkZ);
					if (chunk == null)
					{
						continue;
					}
					chunk.Unpack();
					newChunk = true;
				}
				if (chunk != null)
				{
					int index3d = worldmap.ChunkSizedIndex3D(pos.X & 31, pos.Y & 31, pos.Z & 31);
					blockList[chunk.Data[index3d]].OnBlockBroken(this.worldAccessor, val.Pos, val.byPlayer, val.DropQuantityMultiplier);
					if (newChunk)
					{
						this.dirtyChunkPositions.Add(new ChunkPosCompact(chunkX, chunkY, chunkZ));
					}
				}
			}
			foreach (KeyValuePair<BlockPos, BlockUpdate> keyValuePair in this.StagedBlocks)
			{
				BlockPos blockPos;
				BlockUpdate blockUpdate4;
				keyValuePair.Deconstruct(out blockPos, out blockUpdate4);
				BlockPos pos2 = blockPos;
				BlockUpdate blockUpdate = blockUpdate4;
				int chunkX2 = pos2.X / 32;
				int chunkY2 = pos2.InternalY / 32;
				int chunkZ2 = pos2.Z / 32;
				if (chunkX2 != prevChunkX || chunkY2 != prevChunkY || chunkZ2 != prevChunkZ)
				{
					chunk = worldmap.GetChunk(prevChunkX = chunkX2, prevChunkY = chunkY2, prevChunkZ = chunkZ2);
					if (chunk == null)
					{
						continue;
					}
					chunk.Unpack();
					this.dirtyChunkPositions.Add(new ChunkPosCompact(chunkX2, chunkY2, chunkZ2));
				}
				if (chunk != null)
				{
					int index3d2 = worldmap.ChunkSizedIndex3D(pos2.X & 31, pos2.Y & 31, pos2.Z & 31);
					int newBLockId = ((blockUpdate.NewSolidBlockId >= 0) ? blockUpdate.NewSolidBlockId : blockUpdate.NewFluidBlockId);
					if (newBLockId < 0)
					{
						newBLockId = 0;
					}
					Block newBlock = blockList[newBLockId];
					blockUpdate.OldBlockId = chunk.Data[index3d2];
					Dictionary<int, Block> decors = chunk.GetSubDecors(this, pos2);
					if (decors != null && decors.Count > 0)
					{
						blockUpdate4 = blockUpdate;
						if (blockUpdate4.OldDecors == null)
						{
							blockUpdate4.OldDecors = new List<DecorUpdate>();
						}
						foreach (KeyValuePair<int, Block> keyValuePair2 in decors)
						{
							int num;
							Block block2;
							keyValuePair2.Deconstruct(out num, out block2);
							int i = num;
							Block block = block2;
							blockUpdate.OldDecors.Add(new DecorUpdate
							{
								faceAndSubposition = i,
								decorId = block.BlockId
							});
						}
					}
					if (this.storeOldBlockEntityData && this.worldAccessor.Blocks[blockUpdate.OldBlockId].EntityClass != null)
					{
						TreeAttribute tree = new TreeAttribute();
						BlockEntity blockEntity = this.GetBlockEntity(blockUpdate.Pos);
						if (blockEntity != null)
						{
							blockEntity.ToTreeAttributes(tree);
						}
						blockUpdate.OldBlockEntityData = tree.ToBytes();
					}
					blockUpdate.OldFluidBlockId = chunk.Data.GetFluid(index3d2);
					if (blockUpdate.NewSolidBlockId >= 0)
					{
						chunk.Data[index3d2] = blockUpdate.NewSolidBlockId;
					}
					if (blockUpdate.NewFluidBlockId >= 0)
					{
						chunk.Data.SetFluid(index3d2, blockUpdate.NewFluidBlockId);
						if (blockUpdate.NewSolidBlockId == 0)
						{
							newBlock = blockList[blockUpdate.NewFluidBlockId];
						}
					}
					chunk.BreakAllDecorFast(this.worldAccessor, pos2, index3d2, false);
					updatedBlocks.Add(blockUpdate);
					updatedBlockPositions.Add(blockUpdate.Pos);
					if (blockUpdate.NewSolidBlockId > 0 || blockUpdate.NewFluidBlockId > 0)
					{
						chunk.Empty = false;
					}
					if (this.relight && newBlock.GetLightHsv(this, pos2, null)[2] > 0)
					{
						this.LightSources[pos2] = blockUpdate;
					}
					if (pos2.dimension == 0)
					{
						base.UpdateRainHeightMap(blockList[blockUpdate.OldBlockId], newBlock, pos2, chunk.MapChunk);
					}
				}
			}
			foreach (KeyValuePair<BlockPos, BlockUpdate> keyValuePair in this.StagedBlocks)
			{
				BlockPos blockPos;
				BlockUpdate blockUpdate4;
				keyValuePair.Deconstruct(out blockPos, out blockUpdate4);
				BlockPos pos3 = blockPos;
				BlockUpdate blockUpdate2 = blockUpdate4;
				int solidBlockId = blockUpdate2.NewSolidBlockId;
				if (solidBlockId >= 0 && (!blockUpdate2.ExchangeOnly || blockList[solidBlockId].EntityClass != null) && (blockUpdate2.OldBlockId != solidBlockId || (blockUpdate2.ByStack != null && blockList[blockUpdate2.OldBlockId].EntityClass != null)))
				{
					int chunkX3 = pos3.X / 32;
					int chunkY3 = pos3.InternalY / 32;
					int chunkZ3 = pos3.Z / 32;
					if (chunkX3 != prevChunkX || chunkY3 != prevChunkY || chunkZ3 != prevChunkZ)
					{
						chunk = worldmap.GetChunk(prevChunkX = chunkX3, prevChunkY = chunkY3, prevChunkZ = chunkZ3);
						if (chunk == null)
						{
							continue;
						}
						chunk.Unpack();
					}
					if (chunk != null)
					{
						if (blockUpdate2.ExchangeOnly)
						{
							chunk.GetLocalBlockEntityAtBlockPos(pos3).OnExchanged(blockList[solidBlockId]);
						}
						else
						{
							blockList[blockUpdate2.OldBlockId].OnBlockRemoved(worldmap.World, pos3);
							blockList[solidBlockId].OnBlockPlaced(worldmap.World, pos3, blockUpdate2.ByStack);
						}
					}
				}
			}
			foreach (KeyValuePair<BlockPos, BlockUpdate> keyValuePair in this.StagedBlocks.Where((KeyValuePair<BlockPos, BlockUpdate> b) => b.Value.Decors != null))
			{
				BlockPos blockPos;
				BlockUpdate blockUpdate4;
				keyValuePair.Deconstruct(out blockPos, out blockUpdate4);
				BlockPos pos4 = blockPos;
				BlockUpdate blockUpdate3 = blockUpdate4;
				int chunkX4 = pos4.X / 32;
				int chunkY4 = pos4.InternalY / 32;
				int chunkZ4 = pos4.Z / 32;
				if (chunkX4 != prevChunkX || chunkY4 != prevChunkY || chunkZ4 != prevChunkZ)
				{
					chunk = worldmap.GetChunk(prevChunkX = chunkX4, prevChunkY = chunkY4, prevChunkZ = chunkZ4);
					if (chunk == null)
					{
						continue;
					}
					chunk.Unpack();
					this.dirtyChunkPositions.Add(new ChunkPosCompact(chunkX4, chunkY4, chunkZ4));
				}
				if (chunk != null)
				{
					int index3d3 = worldmap.ChunkSizedIndex3D(pos4.X & 31, pos4.Y & 31, pos4.Z & 31);
					foreach (DecorUpdate decorUpdate in blockUpdate3.Decors)
					{
						int newdecorId = decorUpdate.decorId;
						Block newDecorBlock = blockList[newdecorId];
						if (newdecorId > 0)
						{
							chunk.SetDecor(newDecorBlock, index3d3, decorUpdate.faceAndSubposition);
							chunk.Empty = false;
						}
					}
					updatedDecorPositions.Add(pos4.Copy());
				}
			}
			if (this.relight)
			{
				foreach (BlockPos pos5 in this.LightSources.Keys)
				{
					this.StagedBlocks.Remove(pos5);
				}
				worldmap.UpdateLightingBulk(this.StagedBlocks);
				worldmap.UpdateLightingBulk(this.LightSources);
			}
			foreach (ChunkPosCompact cp in this.dirtyChunkPositions)
			{
				worldmap.MarkChunkDirty(cp.X, cp.Y, cp.Z, true, false, null, true, false);
			}
			if (this.synchronize)
			{
				worldmap.SendBlockUpdateBulk(updatedBlockPositions, this.relight);
				worldmap.SendDecorUpdateBulk(updatedDecorPositions);
			}
			this.StagedBlocks.Clear();
			this.LightSources.Clear();
			this.dirtyChunkPositions.Clear();
			this._blockBreakTasks.Clear();
			return updatedBlocks;
		}

		public override void Rollback()
		{
			this.StagedBlocks.Clear();
			this.LightSources.Clear();
			this._blockBreakTasks.Clear();
		}

		public override void ExchangeBlock(int blockId, BlockPos pos)
		{
			if ((pos.X | pos.Y | pos.Z) < 0 || (pos.dimension == 0 && (pos.X >= this.worldmap.MapSizeX || pos.Y >= this.worldmap.MapSizeY || pos.Z >= this.worldmap.MapSizeZ)))
			{
				return;
			}
			BlockPos copied = pos.Copy();
			this.StagedBlocks[copied] = new BlockUpdate
			{
				NewSolidBlockId = blockId,
				Pos = copied,
				ExchangeOnly = true
			};
		}

		public override void BreakBlock(BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
		{
			this._blockBreakTasks.Enqueue(new BlockBreakTask
			{
				Pos = pos,
				byPlayer = byPlayer,
				DropQuantityMultiplier = dropQuantityMultiplier
			});
		}

		public int GetStagedBlockId(int posX, int posY, int posZ)
		{
			BlockUpdate bd;
			if (this.StagedBlocks.TryGetValue(new BlockPos(posX, posY, posZ), out bd) && bd.NewSolidBlockId >= 0)
			{
				return bd.NewSolidBlockId;
			}
			return this.GetNonStagedBlockId(posX, posY, posZ, 1);
		}

		public int GetStagedBlockId(BlockPos pos)
		{
			BlockUpdate bd;
			if (this.StagedBlocks.TryGetValue(pos, out bd) && bd.NewSolidBlockId >= 0)
			{
				return bd.NewSolidBlockId;
			}
			return this.GetNonStagedBlockId(pos.X, pos.InternalY, pos.Z, 1);
		}

		public void SetChunks(Vec2i chunkCoord, IWorldChunk[] chunksCol)
		{
			throw new NotImplementedException();
		}

		public void PostCommitCleanup(List<BlockUpdate> updatedBlocks)
		{
			this.FixWaterfalls(updatedBlocks);
		}

		private void FixWaterfalls(List<BlockUpdate> updatedBlocks)
		{
			Dictionary<BlockPos, BlockPos> updateNeighbours = new Dictionary<BlockPos, BlockPos>();
			BlockPos updTmpPos = new BlockPos();
			HashSet<BlockPos> blockPos = new HashSet<BlockPos>(updatedBlocks.Select((BlockUpdate b) => b.Pos).ToList<BlockPos>());
			List<int> fluidBlockIds = (from b in this.worldmap.Blocks
				where b.IsLiquid()
				select b.Id).ToList<int>();
			foreach (BlockUpdate upd in updatedBlocks)
			{
				if (upd.OldFluidBlockId > 0 && fluidBlockIds.Contains(upd.OldFluidBlockId))
				{
					foreach (BlockFacing face in BlockFacing.ALLFACES)
					{
						updTmpPos.Set(upd.Pos).Offset(face);
						if (!blockPos.Contains(updTmpPos))
						{
							updateNeighbours.TryAdd(updTmpPos.Copy(), upd.Pos.Copy());
						}
					}
				}
			}
			int prevChunkX = -1;
			int prevChunkY = -1;
			int prevChunkZ = -1;
			IWorldChunk chunk = null;
			foreach (KeyValuePair<BlockPos, BlockPos> pos in updateNeighbours)
			{
				int chunkX = pos.Value.X / 32;
				int chunkY = pos.Value.InternalY / 32;
				int chunkZ = pos.Value.Z / 32;
				if (chunkX != prevChunkX || chunkY != prevChunkY || chunkZ != prevChunkZ)
				{
					chunk = this.worldmap.GetChunk(prevChunkX = chunkX, prevChunkY = chunkY, prevChunkZ = chunkZ);
					if (chunk == null)
					{
						continue;
					}
					chunk.Unpack();
					this.dirtyChunkPositions.Add(new ChunkPosCompact(chunkX, chunkY, chunkZ));
				}
				if (chunk != null)
				{
					int index3d = this.worldmap.ChunkSizedIndex3D(pos.Key.X & 31, pos.Key.Y & 31, pos.Key.Z & 31);
					Block block = this.worldmap.Blocks[chunk.Data[index3d]];
					if (block.IsLiquid())
					{
						block.OnNeighbourBlockChange(this.worldAccessor, pos.Key, pos.Value);
					}
					else
					{
						this.worldmap.Blocks[chunk.Data.GetFluid(index3d)].OnNeighbourBlockChange(this.worldAccessor, pos.Key, pos.Value);
					}
				}
			}
			foreach (ChunkPosCompact cp in this.dirtyChunkPositions)
			{
				this.worldmap.MarkChunkDirty(cp.X, cp.Y, cp.Z, true, false, null, true, false);
			}
			this.dirtyChunkPositions.Clear();
			this.worldmap.SendBlockUpdateBulk(updateNeighbours.Keys, this.relight);
		}

		protected bool synchronize;

		protected bool relight;

		protected bool debug;

		protected bool storeOldBlockEntityData;

		public readonly Dictionary<BlockPos, BlockUpdate> StagedBlocks = new Dictionary<BlockPos, BlockUpdate>();

		public readonly Dictionary<BlockPos, BlockUpdate> LightSources = new Dictionary<BlockPos, BlockUpdate>();

		private readonly Queue<BlockBreakTask> _blockBreakTasks = new Queue<BlockBreakTask>();

		protected readonly HashSet<ChunkPosCompact> dirtyChunkPositions = new HashSet<ChunkPosCompact>();
	}
}
