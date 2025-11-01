using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Common.Database;

namespace Vintagestory.Common
{
	public abstract class BlockAccessorBase : IBlockAccessor
	{
		public int MapSizeX
		{
			get
			{
				return this.worldmap.MapSizeX;
			}
		}

		public int MapSizeY
		{
			get
			{
				return this.worldmap.MapSizeY;
			}
		}

		public int MapSizeZ
		{
			get
			{
				return this.worldmap.MapSizeZ;
			}
		}

		public int ChunkSize
		{
			get
			{
				return 32;
			}
		}

		public int RegionSize
		{
			get
			{
				return this.worldmap.RegionSize;
			}
		}

		public Vec3i MapSize
		{
			get
			{
				return this.worldmap.MapSize;
			}
		}

		public int RegionMapSizeX
		{
			get
			{
				return this.worldmap.RegionMapSizeX;
			}
		}

		public int RegionMapSizeY
		{
			get
			{
				return this.worldmap.RegionMapSizeY;
			}
		}

		public int RegionMapSizeZ
		{
			get
			{
				return this.worldmap.RegionMapSizeZ;
			}
		}

		public bool UpdateSnowAccumMap { get; set; } = true;

		public BlockAccessorBase(WorldMap worldmap, IWorldAccessor worldAccessor)
		{
			this.worldmap = worldmap;
			this.worldAccessor = worldAccessor;
		}

		public virtual int GetBlockId(int posX, int posY, int posZ)
		{
			return this.GetBlockId(posX, posY, posZ, 0);
		}

		public virtual int GetBlockId(BlockPos pos)
		{
			return this.GetBlockId(pos.X, pos.InternalY, pos.Z, 0);
		}

		public virtual Block GetBlock(int posX, int posY, int posZ)
		{
			return this.GetBlockRaw(posX, posY, posZ, 0);
		}

		public virtual Block GetBlock(BlockPos pos)
		{
			return this.GetBlock(pos, 0);
		}

		public Block GetBlock(BlockPos pos, int layer = 0)
		{
			return this.worldmap.Blocks[this.GetBlockId(pos.X, pos.InternalY, pos.Z, layer)];
		}

		public virtual int GetBlockId(BlockPos pos, int layer)
		{
			return this.GetBlockId(pos.X, pos.InternalY, pos.Z, layer);
		}

		public abstract int GetBlockId(int posX, int posY, int posZ, int layer);

		public abstract Block GetBlockOrNull(int posX, int posY, int posZ, int layer = 4);

		public virtual Block GetBlock(int blockid)
		{
			return this.worldmap.Blocks[blockid];
		}

		public virtual Block GetBlock(int posX, int posY, int posZ, int layer = 0)
		{
			return this.GetBlockRaw(posX, posY, posZ, layer);
		}

		public virtual Block GetBlockRaw(int posX, int posY, int posZ, int layer = 0)
		{
			return this.worldmap.Blocks[this.GetBlockId(posX, posY, posZ, layer)];
		}

		public virtual Block GetMostSolidBlock(BlockPos pos)
		{
			return this.GetBlock(pos, 4);
		}

		public virtual Block GetMostSolidBlock(int posX, int posY, int posZ)
		{
			return this.GetBlockRaw(posX, posY, posZ, 4);
		}

		public void SetBlockInternal(int blockId, BlockPos pos, IWorldChunk chunk, bool synchronize, bool relight, int layer, ItemStack byItemstack = null)
		{
			Block newBlock = this.worldmap.Blocks[blockId];
			if (layer == 2 || (layer == 0 && newBlock.ForFluidsLayer))
			{
				if (layer == 0)
				{
					this.SetSolidBlockInternal(0, pos, chunk, synchronize, relight, byItemstack);
				}
				this.SetFluidBlockInternal(blockId, pos, chunk, synchronize, relight);
				return;
			}
			if (layer != 0 && layer != 1)
			{
				throw new ArgumentException("Layer must be solid or fluid");
			}
			this.SetSolidBlockInternal(blockId, pos, chunk, synchronize, relight, byItemstack);
		}

		protected void SetSolidBlockInternal(int blockId, BlockPos pos, IWorldChunk chunk, bool synchronize, bool relight, ItemStack byItemstack)
		{
			int cx = pos.X / 32;
			int cy = pos.InternalY / 32;
			int cz = pos.Z / 32;
			int lx = pos.X & 31;
			int ly = pos.Y & 31;
			int lz = pos.Z & 31;
			int index3d = this.worldmap.ChunkSizedIndex3D(lx, ly, lz);
			int oldblockid = (chunk.Data as ChunkData).GetSolidBlock(index3d);
			chunk.Data[index3d] = blockId;
			if (blockId != 0)
			{
				chunk.Empty = false;
				this.worldmap.MarkChunkDirty(cx, cy, cz, true, false, null, true, false);
			}
			Block newBlock = this.worldmap.Blocks[blockId];
			Block oldBlock = this.worldmap.Blocks[oldblockid];
			this.UpdateRainHeightMap(oldBlock, newBlock, pos, chunk.MapChunk);
			this.MarkAdjacentNeighboursDirty(cx, cy, cz, lx, ly, lz, pos);
			if (blockId == 0)
			{
				this.worldmap.MarkChunkDirty(cx, cy, cz, true, false, null, true, false);
			}
			if (synchronize)
			{
				this.worldmap.SendSetBlock(blockId, pos.X, pos.InternalY, pos.Z);
			}
			if (relight)
			{
				this.worldmap.UpdateLighting(oldblockid, blockId, pos);
			}
			if (blockId != oldblockid)
			{
				chunk.BreakAllDecorFast(this.worldAccessor, pos, index3d, true);
				oldBlock.OnBlockRemoved(this.worldmap.World, pos);
				newBlock.OnBlockPlaced(this.worldmap.World, pos, byItemstack);
				if (this.worldAccessor.GetBlock(blockId).DisplacesLiquids(this, pos))
				{
					chunk.Data.SetFluid(index3d, 0);
					return;
				}
				int liqId = chunk.Data.GetFluid(index3d);
				if (liqId != 0)
				{
					this.worldAccessor.GetBlock(liqId).OnNeighbourBlockChange(this.worldAccessor, pos, pos);
				}
			}
		}

		protected void SetFluidBlockInternal(int fluidBlockid, BlockPos pos, IWorldChunk chunk, bool synchronize, bool relight)
		{
			int cx = pos.X / 32;
			int cy = pos.InternalY / 32;
			int cz = pos.Z / 32;
			int lx = pos.X & 31;
			int ly = pos.Y & 31;
			int lz = pos.Z & 31;
			int index3d = this.worldmap.ChunkSizedIndex3D(lx, ly, lz);
			int oldblockid = chunk.Data.GetFluid(index3d);
			if (fluidBlockid == oldblockid)
			{
				return;
			}
			chunk.Data.SetFluid(index3d, fluidBlockid);
			if (fluidBlockid != 0)
			{
				chunk.Empty = false;
				this.worldmap.MarkChunkDirty(cx, cy, cz, true, false, null, true, false);
			}
			if (this.worldmap.Blocks[(chunk.Data as ChunkData).GetSolidBlock(index3d)].RainPermeable)
			{
				this.UpdateRainHeightMap(this.worldmap.Blocks[oldblockid], this.worldmap.Blocks[fluidBlockid], pos, chunk.MapChunk);
			}
			this.MarkAdjacentNeighboursDirty(cx, cy, cz, lx, ly, lz, pos);
			if (fluidBlockid == 0)
			{
				this.worldmap.MarkChunkDirty(cx, cy, cz, true, false, null, true, false);
			}
			if (synchronize)
			{
				this.worldmap.SendSetBlock(-fluidBlockid - 1, pos.X, pos.InternalY, pos.Z);
			}
			if (fluidBlockid != oldblockid)
			{
				this.worldmap.Blocks[fluidBlockid].OnBlockPlaced(this.worldmap.World, pos, null);
			}
		}

		public void WalkStructures(BlockPos minpos, BlockPos maxpos, Action<GeneratedStructure> onStructure)
		{
			int mapRegionSizeX = this.worldmap.MapSizeX / this.worldmap.RegionSize;
			int mapRegionSizeZ = this.worldmap.MapSizeZ / this.worldmap.RegionSize;
			Cuboidi section = new Cuboidi(minpos, maxpos);
			int maxStructureSize = 256;
			int num = GameMath.Clamp((minpos.X - maxStructureSize) / this.worldmap.RegionSize, 0, mapRegionSizeX);
			int minrz = GameMath.Clamp((minpos.Z - maxStructureSize) / this.worldmap.RegionSize, 0, mapRegionSizeZ);
			int maxrx = GameMath.Clamp((maxpos.X + maxStructureSize) / this.worldmap.RegionSize, 0, mapRegionSizeX);
			int maxrz = GameMath.Clamp((maxpos.Z + maxStructureSize) / this.worldmap.RegionSize, 0, mapRegionSizeZ);
			for (int rx = num; rx <= maxrx; rx++)
			{
				for (int rz = minrz; rz <= maxrz; rz++)
				{
					IMapRegion mapregion = this.worldmap.GetMapRegion(rx, rz);
					if (mapregion != null)
					{
						foreach (GeneratedStructure val in mapregion.GeneratedStructures)
						{
							if (val.Location.IntersectsOrTouches(section))
							{
								onStructure(val);
							}
						}
					}
				}
			}
		}

		public void WalkStructures(BlockPos pos, Action<GeneratedStructure> onStructure)
		{
			int mapRegionSizeX = this.worldmap.MapSizeX / this.worldmap.RegionSize;
			int mapRegionSizeZ = this.worldmap.MapSizeZ / this.worldmap.RegionSize;
			int maxStructureSize = 256;
			int num = GameMath.Clamp((pos.X - maxStructureSize) / this.worldmap.RegionSize, 0, mapRegionSizeX);
			int minrz = GameMath.Clamp((pos.Z - maxStructureSize) / this.worldmap.RegionSize, 0, mapRegionSizeZ);
			int maxrx = GameMath.Clamp((pos.X + maxStructureSize) / this.worldmap.RegionSize, 0, mapRegionSizeX);
			int maxrz = GameMath.Clamp((pos.Z + maxStructureSize) / this.worldmap.RegionSize, 0, mapRegionSizeZ);
			for (int rx = num; rx <= maxrx; rx++)
			{
				for (int rz = minrz; rz <= maxrz; rz++)
				{
					IMapRegion mapregion = this.worldmap.GetMapRegion(rx, rz);
					if (mapregion != null)
					{
						foreach (GeneratedStructure val in mapregion.GeneratedStructures)
						{
							if (val.Location.Contains(pos))
							{
								onStructure(val);
							}
						}
					}
				}
			}
		}

		public void WalkBlocks(BlockPos minPos, BlockPos maxPos, Action<Block, int, int, int> onBlock, bool centerOrder = false)
		{
			int worldSize = this.worldmap.MapSizeX;
			int minx = GameMath.Clamp(Math.Min(minPos.X, maxPos.X), 0, worldSize);
			int maxx = GameMath.Clamp(Math.Max(minPos.X, maxPos.X), 0, worldSize);
			worldSize = this.worldmap.MapSizeY;
			int miny = GameMath.Clamp(Math.Min(minPos.Y, maxPos.Y), 0, worldSize);
			int maxy = GameMath.Clamp(Math.Max(minPos.Y, maxPos.Y), 0, worldSize);
			worldSize = this.worldmap.MapSizeZ;
			int minz = GameMath.Clamp(Math.Min(minPos.Z, maxPos.Z), 0, worldSize);
			int maxz = GameMath.Clamp(Math.Max(minPos.Z, maxPos.Z), 0, worldSize);
			int mincx = minx / 32;
			int mincy = miny / 32;
			int mincz = minz / 32;
			int maxcx = maxx / 32;
			int maxcy = maxy / 32;
			int maxcz = maxz / 32;
			int dimensionOffsetY = minPos.dimension * 1024;
			ChunkData[] chunks = this.LoadChunksToCache(mincx, mincy + dimensionOffsetY, mincz, maxcx, maxcy + dimensionOffsetY, maxcz, null);
			int cxCount = maxcx - mincx + 1;
			int czCount = maxcz - mincz + 1;
			if (centerOrder)
			{
				int width = maxx - minx;
				int height = maxy - miny;
				int length = maxz - minz;
				int midX = width / 2;
				int midY = height / 2;
				int midZ = length / 2;
				for (int x = 0; x <= width; x++)
				{
					int px = x & 1;
					px = midX - (1 - px * 2) * (x + px) / 2;
					int posX = px + minx;
					int cix = posX / 32 - mincx;
					for (int y = 0; y <= height; y++)
					{
						int py = y & 1;
						py = midY - (1 - py * 2) * (y + py) / 2;
						int posY = py + miny;
						int index3dBase = posY % 32 * 32 * 32 + posX % 32;
						int ciBase = (posY / 32 - mincy) * czCount - mincz;
						for (int z = 0; z <= length; z++)
						{
							int pz = z & 1;
							pz = midZ - (1 - pz * 2) * (z + pz) / 2;
							int posZ = pz + minz;
							ChunkData chunkBlocks = chunks[(ciBase + posZ / 32) * cxCount + cix];
							if (chunkBlocks != null)
							{
								int index3d = index3dBase + posZ % 32 * 32;
								int blockId = chunkBlocks.GetFluid(index3d);
								if (blockId != 0)
								{
									onBlock(this.worldmap.Blocks[blockId], posX, posY, posZ);
								}
								blockId = chunkBlocks.GetSolidBlock(index3d);
								onBlock(this.worldmap.Blocks[blockId], posX, posY, posZ);
							}
						}
					}
				}
				return;
			}
			for (int y2 = miny; y2 <= maxy; y2++)
			{
				int ciy = (y2 / 32 - mincy) * czCount - mincz;
				for (int z2 = minz; z2 <= maxz; z2++)
				{
					int chunkIndexBase = (ciy + z2 / 32) * cxCount - mincx;
					int index3dBase2 = (y2 % 32 * 32 + z2 % 32) * 32;
					for (int x2 = minx; x2 <= maxx; x2++)
					{
						ChunkData chunkBlocks2 = chunks[chunkIndexBase + x2 / 32];
						if (chunkBlocks2 != null)
						{
							int index3d2 = index3dBase2 + x2 % 32;
							int blockId2 = chunkBlocks2.GetFluid(index3d2);
							if (blockId2 != 0)
							{
								onBlock(this.worldmap.Blocks[blockId2], x2, y2, z2);
							}
							blockId2 = chunkBlocks2.GetSolidBlock(index3d2);
							onBlock(this.worldmap.Blocks[blockId2], x2, y2, z2);
						}
					}
				}
			}
		}

		protected virtual ChunkData[] LoadChunksToCache(int mincx, int mincy, int mincz, int maxcx, int maxcy, int maxcz, Action<int, int, int> onChunkMissing)
		{
			int cxCount = maxcx - mincx + 1;
			int cyCount = maxcy - mincy + 1;
			int czCount = maxcz - mincz + 1;
			ChunkData[] chunks = new ChunkData[cxCount * cyCount * czCount];
			for (int cy = mincy; cy <= maxcy; cy++)
			{
				int ciy = (cy - mincy) * czCount - mincz;
				for (int cz = mincz; cz <= maxcz; cz++)
				{
					int chunkIndexBase = (ciy + cz) * cxCount - mincx;
					for (int cx = mincx; cx <= maxcx; cx++)
					{
						IWorldChunk chunk = this.worldmap.GetChunk(cx, cy, cz);
						if (chunk == null)
						{
							chunks[chunkIndexBase + cx] = null;
							if (onChunkMissing != null)
							{
								onChunkMissing(cx, cy, cz);
							}
						}
						else
						{
							chunk.Unpack();
							chunks[chunkIndexBase + cx] = chunk.Data as ChunkData;
						}
					}
				}
			}
			return chunks;
		}

		public void SearchBlocks(BlockPos minPos, BlockPos maxPos, ActionConsumable<Block, BlockPos> onBlock, Action<int, int, int> onChunkMissing = null)
		{
			BlockPos tmpPos = new BlockPos();
			int minx = GameMath.Clamp(Math.Min(minPos.X, maxPos.X), 0, this.worldmap.MapSizeX);
			int miny = GameMath.Clamp(Math.Min(minPos.Y, maxPos.Y), 0, this.worldmap.MapSizeY);
			int minz = GameMath.Clamp(Math.Min(minPos.Z, maxPos.Z), 0, this.worldmap.MapSizeZ);
			int maxx = GameMath.Clamp(Math.Max(minPos.X, maxPos.X), 0, this.worldmap.MapSizeX);
			int maxy = GameMath.Clamp(Math.Max(minPos.Y, maxPos.Y), 0, this.worldmap.MapSizeY);
			int maxz = GameMath.Clamp(Math.Max(minPos.Z, maxPos.Z), 0, this.worldmap.MapSizeZ);
			int mincx = minx / 32;
			int mincy = miny / 32;
			int mincz = minz / 32;
			int maxcx = maxx / 32;
			int maxcy = maxy / 32;
			int maxcz = maxz / 32;
			int dimensionOffsetY = minPos.dimension * 1024;
			ChunkData[] chunks = this.LoadChunksToCache(mincx, mincy + dimensionOffsetY, mincz, maxcx, maxcy + dimensionOffsetY, maxcz, onChunkMissing);
			int cwdt = maxcx - mincx + 1;
			int clgt = maxcz - mincz + 1;
			for (int x = minx; x <= maxx; x++)
			{
				for (int y = miny; y <= maxy; y++)
				{
					for (int z = minz; z <= maxz; z++)
					{
						tmpPos.Set(x, y, z);
						int index3d = (y % 32 * 32 + z % 32) * 32 + x % 32;
						int cix = x / 32 - mincx;
						int ciy = y / 32 - mincy;
						int ciz = z / 32 - mincz;
						IChunkBlocks chunkBlocks = chunks[(ciy * clgt + ciz) * cwdt + cix];
						if (chunkBlocks != null)
						{
							Block block = this.worldmap.Blocks[chunkBlocks[index3d]];
							if (!onBlock(block, tmpPos))
							{
								return;
							}
						}
					}
				}
			}
		}

		public void SearchFluidBlocks(BlockPos minPos, BlockPos maxPos, ActionConsumable<Block, BlockPos> onBlock, Action<int, int, int> onChunkMissing = null)
		{
			BlockPos tmpPos = new BlockPos();
			int minx = GameMath.Clamp(Math.Min(minPos.X, maxPos.X), 0, this.worldmap.MapSizeX);
			int miny = GameMath.Clamp(Math.Min(minPos.Y, maxPos.Y), 0, this.worldmap.MapSizeY);
			int minz = GameMath.Clamp(Math.Min(minPos.Z, maxPos.Z), 0, this.worldmap.MapSizeZ);
			int maxx = GameMath.Clamp(Math.Max(minPos.X, maxPos.X), 0, this.worldmap.MapSizeX);
			int maxy = GameMath.Clamp(Math.Max(minPos.Y, maxPos.Y), 0, this.worldmap.MapSizeY);
			int maxz = GameMath.Clamp(Math.Max(minPos.Z, maxPos.Z), 0, this.worldmap.MapSizeZ);
			int mincx = minx / 32;
			int mincy = miny / 32;
			int mincz = minz / 32;
			int maxcx = maxx / 32;
			int maxcy = maxy / 32;
			int maxcz = maxz / 32;
			int dimensionOffsetY = minPos.dimension * 1024;
			ChunkData[] chunks = this.LoadChunksToCache(mincx, mincy + dimensionOffsetY, mincz, maxcx, maxcy + dimensionOffsetY, maxcz, onChunkMissing);
			int cwdt = maxcx - mincx + 1;
			int clgt = maxcz - mincz + 1;
			for (int x = minx; x <= maxx; x++)
			{
				for (int y = miny; y <= maxy; y++)
				{
					for (int z = minz; z <= maxz; z++)
					{
						tmpPos.Set(x, y, z);
						int index3d = (y % 32 * 32 + z % 32) * 32 + x % 32;
						int cix = x / 32 - mincx;
						int ciy = y / 32 - mincy;
						int ciz = z / 32 - mincz;
						ChunkData chunkBlocks = chunks[(ciy * clgt + ciz) * cwdt + cix];
						if (chunkBlocks != null)
						{
							Block block = this.worldmap.Blocks[chunkBlocks.GetFluid(index3d)];
							if (!onBlock(block, tmpPos))
							{
								return;
							}
						}
					}
				}
			}
		}

		public Block GetBlock(AssetLocation code)
		{
			Block block;
			this.worldmap.BlocksByCode.TryGetValue(code, out block);
			return block;
		}

		public void SetBlock(int blockId, BlockPos pos)
		{
			this.SetBlock(blockId, pos, null);
		}

		public abstract void SetBlock(int blockId, BlockPos pos, ItemStack byItemstack);

		public abstract void SetBlock(int blockId, BlockPos pos, int layer);

		public abstract void ExchangeBlock(int blockId, BlockPos pos);

		protected void MarkAdjacentNeighboursDirty(int cx, int cy, int cz, int lx, int ly, int lz, BlockPos pos)
		{
			lx = (lx * 2 - 31) / 31;
			ly = (ly * 2 - 31) / 31;
			lz = (lz * 2 - 31) / 31;
			if (lx != 0)
			{
				this.worldmap.MarkChunkDirty(cx + lx, cy, cz, true, false, null, true, true);
			}
			if (ly != 0)
			{
				this.worldmap.MarkChunkDirty(cx, cy + ly, cz, true, false, null, true, true);
			}
			if (lz != 0)
			{
				this.worldmap.MarkChunkDirty(cx, cy, cz + lz, true, false, null, true, true);
			}
		}

		[Obsolete("Please use BlockPos version instead for dimension awareness")]
		public bool IsValidPos(int posX, int posY, int posZ)
		{
			return this.worldmap.IsValidPos(posX, posY, posZ);
		}

		public bool IsValidPos(BlockPos pos)
		{
			return this.worldmap.IsValidPos(pos.X, pos.InternalY, pos.Z);
		}

		public virtual void BreakBlock(BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
		{
			this.GetBlock(pos).OnBlockBroken(this.worldAccessor, pos, byPlayer, dropQuantityMultiplier);
			this.worldmap.TriggerNeighbourBlockUpdate(pos);
		}

		public bool IsNotTraversable(BlockPos pos)
		{
			return this.worldmap.IsMovementRestrictedPos((double)pos.X, (double)pos.Y, (double)pos.Z, pos.dimension);
		}

		public bool IsNotTraversable(double x, double y, double z)
		{
			return this.worldmap.IsMovementRestrictedPos(x, y, z, 0);
		}

		public bool IsNotTraversable(double x, double y, double z, int dimension)
		{
			return this.worldmap.IsMovementRestrictedPos(x, y, z, dimension);
		}

		public virtual IWorldChunk GetChunk(int chunkX, int chunkY, int chunkZ)
		{
			return this.worldmap.GetChunk(chunkX, chunkY, chunkZ);
		}

		[Obsolete("Please use BlockPos version instead for dimension awareness")]
		public virtual IWorldChunk GetChunkAtBlockPos(int posX, int posY, int posZ)
		{
			return this.worldmap.GetChunk(posX / 32, posY / 32, posZ / 32);
		}

		public virtual IWorldChunk GetChunkAtBlockPos(BlockPos pos)
		{
			return this.worldmap.GetChunk(pos.X / 32, pos.InternalY / 32, pos.Z / 32);
		}

		public virtual void MarkChunkDecorsModified(BlockPos pos)
		{
			if (this.worldAccessor.Side == EnumAppSide.Client)
			{
				this.worldAccessor.BlockAccessor.MarkBlockDirty(pos, null);
			}
			this.worldmap.MarkDecorsDirty(pos);
		}

		public virtual IMapChunk GetMapChunk(int chunkX, int chunkZ)
		{
			return this.worldmap.GetMapChunk(chunkX, chunkZ);
		}

		public virtual IMapChunk GetMapChunk(Vec2i chunkPos)
		{
			return this.worldmap.GetMapChunk(chunkPos.X, chunkPos.Y);
		}

		public virtual IMapRegion GetMapRegion(int regionX, int regionZ)
		{
			return this.worldmap.GetMapRegion(regionX, regionZ);
		}

		public virtual List<BlockUpdate> Commit()
		{
			return null;
		}

		public virtual void Rollback()
		{
		}

		public virtual void SpawnBlockEntity(string classname, BlockPos position, ItemStack byItemStack = null)
		{
			this.worldmap.SpawnBlockEntity(classname, position, byItemStack);
		}

		public virtual void SpawnBlockEntity(BlockEntity be)
		{
			this.worldmap.SpawnBlockEntity(be);
		}

		public virtual void RemoveBlockEntity(BlockPos position)
		{
			this.worldmap.RemoveBlockEntity(position);
		}

		public virtual BlockEntity GetBlockEntity(BlockPos position)
		{
			return this.worldmap.GetBlockEntity(position);
		}

		public void MarkBlockEntityDirty(BlockPos pos)
		{
			this.worldmap.MarkBlockEntityDirty(pos);
		}

		public void MarkBlockDirty(BlockPos pos, IPlayer skipPlayer = null)
		{
			this.worldmap.MarkBlockDirty(pos, skipPlayer);
		}

		public void MarkBlockModified(BlockPos pos)
		{
			this.worldmap.MarkBlockModified(pos, true);
		}

		public void MarkBlockDirty(BlockPos pos, Action OnRetesselated)
		{
			this.worldmap.MarkBlockDirty(pos, OnRetesselated);
		}

		public void TriggerNeighbourBlockUpdate(BlockPos pos)
		{
			this.worldmap.TriggerNeighbourBlockUpdate(pos);
		}

		public int GetLightLevel(int posX, int posY, int posZ, EnumLightLevelType type)
		{
			IWorldChunk chunk = this.GetChunkAtBlockPos(posX, posY, posZ);
			if (chunk == null || !this.worldmap.IsValidPos(posX, posY, posZ))
			{
				return this.worldAccessor.SunBrightness;
			}
			int chunksize = 32;
			int index3d = (posY % chunksize * chunksize + posZ % chunksize) * chunksize + posX % chunksize;
			ushort num = chunk.Unpack_AndReadLight(index3d);
			int blockLight = (num >> 5) & 31;
			int sunLight = (int)(num & 31);
			switch (type)
			{
			case EnumLightLevelType.OnlyBlockLight:
				return blockLight;
			case EnumLightLevelType.OnlySunLight:
				return sunLight;
			case EnumLightLevelType.MaxLight:
				return Math.Max(sunLight, blockLight);
			case EnumLightLevelType.MaxTimeOfDayLight:
			{
				float daylightStrength = this.worldAccessor.Calendar.GetDayLightStrength((double)posX, (double)posZ);
				return Math.Max((int)((float)sunLight * daylightStrength), blockLight);
			}
			case EnumLightLevelType.TimeOfDaySunLight:
			{
				float daylightStrength = this.worldAccessor.Calendar.GetDayLightStrength((double)posX, (double)posZ);
				return (int)((float)sunLight * daylightStrength);
			}
			case EnumLightLevelType.Sunbrightness:
				return (int)(32f * this.worldAccessor.Calendar.GetDayLightStrength((double)posX, (double)posZ));
			default:
				return -1;
			}
		}

		public int GetLightLevel(BlockPos pos, EnumLightLevelType type)
		{
			return this.GetLightLevel(pos.X, pos.InternalY, pos.Z, type);
		}

		public int GetTerrainMapheightAt(BlockPos pos)
		{
			int chunksize = 32;
			IMapChunk mapchunk = this.GetMapChunk(pos.X / chunksize, pos.Z / chunksize);
			if (mapchunk == null || !this.worldmap.IsValidPos(pos.X, 0, pos.Z))
			{
				return 0;
			}
			int index2d = pos.Z % chunksize * chunksize + pos.X % chunksize;
			return (int)mapchunk.WorldGenTerrainHeightMap[index2d];
		}

		public int GetRainMapHeightAt(int posX, int posZ)
		{
			IMapChunk mapchunk = this.GetMapChunk(posX / 32, posZ / 32);
			if (mapchunk == null || !this.worldmap.IsValidPos(posX, 0, posZ))
			{
				return 0;
			}
			int index2d = posZ % 32 * 32 + posX % 32;
			return (int)mapchunk.RainHeightMap[index2d];
		}

		public int GetRainMapHeightAt(BlockPos pos)
		{
			IMapChunk mapchunk = this.GetMapChunk(pos.X / 32, pos.Z / 32);
			if (mapchunk == null || !this.worldmap.IsValidPos(pos.X, 0, pos.Z))
			{
				return 0;
			}
			int index2d = pos.Z % 32 * 32 + pos.X % 32;
			return (int)mapchunk.RainHeightMap[index2d];
		}

		public IMapChunk GetMapChunkAtBlockPos(BlockPos pos)
		{
			IMapChunk mapchunk = this.GetMapChunk(pos.X / 32, pos.Z / 32);
			if (!this.worldmap.IsValidPos(pos.X, 0, pos.Z))
			{
				return null;
			}
			return mapchunk;
		}

		public ClimateCondition GetClimateAt(BlockPos pos, EnumGetClimateMode mode = EnumGetClimateMode.NowValues, double totalDays = 0.0)
		{
			if (mode == EnumGetClimateMode.NowValues)
			{
				totalDays = this.worldAccessor.Calendar.TotalDays;
			}
			return this.worldmap.GetClimateAt(pos, mode, totalDays);
		}

		public ClimateCondition GetClimateAt(BlockPos pos, ClimateCondition baseClimate, EnumGetClimateMode mode, double totalDays)
		{
			return this.worldmap.GetClimateAt(pos, baseClimate, mode, totalDays);
		}

		public ClimateCondition GetClimateAt(BlockPos pos, int climate)
		{
			return this.worldmap.GetClimateAt(pos, climate);
		}

		public Vec3d GetWindSpeedAt(Vec3d pos)
		{
			return this.worldmap.GetWindSpeedAt(pos);
		}

		public Vec3d GetWindSpeedAt(BlockPos pos)
		{
			return this.worldmap.GetWindSpeedAt(pos);
		}

		public void DamageBlock(BlockPos pos, BlockFacing facing, float damage)
		{
			this.worldmap.DamageBlock(pos, facing, damage, null);
		}

		public void UpdateRainHeightMap(Block oldBlock, Block newBlock, BlockPos pos, IMapChunk mapchunk)
		{
			if (mapchunk == null || pos.InternalY >= 32768)
			{
				return;
			}
			int lx = pos.X & 31;
			int lz = pos.Z & 31;
			int rainPermeable = (oldBlock.RainPermeable ? 1 : 0);
			bool newRainPermeable = newBlock.RainPermeable;
			ushort prevrainy = mapchunk.RainHeightMap[lz * 32 + lx];
			ushort nowrainy = prevrainy;
			if (!newRainPermeable)
			{
				nowrainy = (mapchunk.RainHeightMap[lz * 32 + lx] = Math.Max(prevrainy, (ushort)pos.Y));
				if ((int)prevrainy < pos.Y)
				{
					mapchunk.MarkDirty();
					if (this.UpdateSnowAccumMap && mapchunk.SnowAccum != null)
					{
						mapchunk.SnowAccum[new Vec2i(pos.X, pos.Z)] = newBlock.GetSnowLevel(pos);
					}
				}
			}
			if (rainPermeable == 0 && newRainPermeable && (int)prevrainy == pos.Y && pos.Y > 0)
			{
				int hereY = pos.Y - 1;
				while (this.worldmap.Blocks[this.GetBlockId(pos.X, hereY, pos.Z, 3)].RainPermeable && hereY > 0)
				{
					hereY--;
				}
				nowrainy = (mapchunk.RainHeightMap[lz * 32 + lx] = (ushort)hereY);
				mapchunk.MarkDirty();
			}
			if (pos.Y > (int)mapchunk.YMax && newBlock.BlockId != 0)
			{
				mapchunk.YMax = (ushort)pos.Y;
				mapchunk.MarkDirty();
			}
			if (this.UpdateSnowAccumMap && nowrainy <= prevrainy)
			{
				ConcurrentDictionary<Vec2i, float> snowAccum = mapchunk.SnowAccum;
				if (snowAccum == null)
				{
					return;
				}
				float num;
				snowAccum.TryRemove(new Vec2i(pos.X, pos.Z), out num);
			}
		}

		public Vec4f GetLightRGBs(int posX, int posY, int posZ)
		{
			return this.worldmap.GetLightRGBSVec4f(posX, posY, posZ);
		}

		public int GetLightRGBsAsInt(int posX, int posY, int posZ)
		{
			return this.worldmap.GetLightRGBsAsInt(posX, posY, posZ);
		}

		public Vec4f GetLightRGBs(BlockPos pos)
		{
			return this.worldmap.GetLightRGBSVec4f(pos.X, pos.InternalY, pos.Z);
		}

		public IWorldChunk GetChunk(long chunkIndex3D)
		{
			return this.worldmap.GetChunk(chunkIndex3D);
		}

		public bool IsSideSolid(int x, int y, int z, BlockFacing facing)
		{
			int blockId = this.GetBlockId(x, y, z, 2);
			if (blockId == 0 || !this.worldmap.Blocks[blockId].SideSolid.Any)
			{
				blockId = this.GetBlockId(x, y, z, 1);
			}
			return this.worldmap.Blocks[blockId].SideSolid[facing.Index];
		}

		public int GetDistanceToRainFall(BlockPos pos, int horziontalSearchWidth = 4, int verticalSearchWidth = 1)
		{
			if (this.GetRainMapHeightAt(pos) <= pos.Y)
			{
				return 0;
			}
			BlockPos tmpPos = new BlockPos();
			Queue<Vec3i> queue = new Queue<Vec3i>();
			HashSet<Vec3i> visited = new HashSet<Vec3i>();
			queue.Enqueue(new Vec3i(pos.X, pos.Y, pos.Z));
			while (queue.Count > 0)
			{
				Vec3i vec = queue.Dequeue();
				for (int i = 0; i < 6; i++)
				{
					BlockFacing facing = BlockFacing.ALLFACES[i];
					Vec3i nvec = new Vec3i(vec.X + facing.Normali.X, vec.Y + facing.Normali.Y, vec.Z + facing.Normali.Z);
					int mhdist = Math.Abs(nvec.X - pos.X) + Math.Abs(nvec.Z - pos.Z);
					int vertDist = Math.Abs(nvec.Y - pos.Y);
					if (mhdist < horziontalSearchWidth && vertDist < verticalSearchWidth && !visited.Contains(nvec))
					{
						visited.Add(nvec);
						tmpPos.Set(nvec.X, nvec.Y, nvec.Z);
						Block block = this.GetBlock(tmpPos);
						if (!block.SideSolid[facing.Index] && !block.SideSolid[facing.Opposite.Index] && block.GetRetention(tmpPos, facing, EnumRetentionType.Sound) == 0 && block.GetRetention(tmpPos, facing.Opposite, EnumRetentionType.Sound) == 0)
						{
							if (this.GetRainMapHeightAt(tmpPos) <= nvec.Y)
							{
								return mhdist + vertDist;
							}
							queue.Enqueue(nvec);
						}
					}
				}
			}
			return 99;
		}

		public void MarkAbsorptionChanged(int oldAbsorption, int newAbsorption, BlockPos pos)
		{
			this.worldmap.UpdateLightingAfterAbsorptionChange(oldAbsorption, newAbsorption, pos);
		}

		public void RemoveBlockLight(byte[] oldLightHsv, BlockPos pos)
		{
			this.worldmap.RemoveBlockLight(oldLightHsv, pos);
		}

		public virtual bool SetDecor(Block block, BlockPos pos, BlockFacing onFace)
		{
			return this.SetDecor(block, pos, new DecorBits(onFace));
		}

		public virtual bool SetDecor(Block block, BlockPos pos, int decorIndex)
		{
			IWorldChunk chunk = this.GetChunkAtBlockPos(pos);
			if (chunk == null)
			{
				return false;
			}
			int lx = pos.X & 31;
			int ly = pos.Y & 31;
			int lz = pos.Z & 31;
			int index3d = this.worldmap.ChunkSizedIndex3D(lx, ly, lz);
			if (chunk.SetDecor(block, index3d, decorIndex))
			{
				this.MarkChunkDecorsModified(pos);
				chunk.MarkModified();
				return true;
			}
			return false;
		}

		public Block[] GetDecors(BlockPos position)
		{
			IWorldChunk chunkAtBlockPos = this.GetChunkAtBlockPos(position);
			if (chunkAtBlockPos == null)
			{
				return null;
			}
			return chunkAtBlockPos.GetDecors(this, position);
		}

		public Dictionary<int, Block> GetSubDecors(BlockPos position)
		{
			IWorldChunk chunkAtBlockPos = this.GetChunkAtBlockPos(position);
			if (chunkAtBlockPos == null)
			{
				return null;
			}
			return chunkAtBlockPos.GetSubDecors(this, position);
		}

		public Block GetDecor(BlockPos position, int faceAndSubPosition)
		{
			IWorldChunk chunkAtBlockPos = this.GetChunkAtBlockPos(position);
			if (chunkAtBlockPos == null)
			{
				return null;
			}
			return chunkAtBlockPos.GetDecor(this, position, faceAndSubPosition);
		}

		public virtual bool BreakDecor(BlockPos pos, BlockFacing side = null, int? faceAndSubposition = null)
		{
			IWorldChunk chunk = this.GetChunkAtBlockPos(pos);
			if (chunk != null && chunk.BreakDecor(this.worldAccessor, pos, side, faceAndSubposition))
			{
				this.MarkChunkDecorsModified(pos);
				chunk.MarkModified();
				return true;
			}
			return false;
		}

		public virtual void SetDecorsBulk(long chunkIndex, Dictionary<int, Block> newDecors)
		{
			IWorldChunk chunk = this.GetChunk(chunkIndex);
			if (chunk != null)
			{
				chunk.SetDecors(newDecors);
			}
			ChunkPos pos = this.worldmap.ChunkPosFromChunkIndex3D(chunkIndex);
			this.worldmap.MarkChunkDirty(pos.X, pos.InternalY, pos.Z, true, false, null, false, false);
		}

		public T GetBlockEntity<T>(BlockPos position) where T : BlockEntity
		{
			return this.worldmap.GetBlockEntity(position) as T;
		}

		public IMiniDimension CreateMiniDimension(Vec3d position)
		{
			return new BlockAccessorMovable(this, position);
		}

		protected const int chunksize = 32;

		protected const int chunksizemask = 31;

		internal readonly WorldMap worldmap;

		internal IWorldAccessor worldAccessor;
	}
}
