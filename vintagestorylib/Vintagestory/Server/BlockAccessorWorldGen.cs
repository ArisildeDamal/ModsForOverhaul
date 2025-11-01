using System;
using System.Collections.Generic;
using System.Diagnostics;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace Vintagestory.Server
{
	public class BlockAccessorWorldGen : BlockAccessorBase, IWorldGenBlockAccessor, IBlockAccessor
	{
		public IServerWorldAccessor WorldgenWorldAccessor
		{
			get
			{
				IServerWorldAccessor serverWorldAccessor;
				if ((serverWorldAccessor = this.worldgenWorldAccessor) == null)
				{
					serverWorldAccessor = (this.worldgenWorldAccessor = new WorldgenWorldAccessor((IServerWorldAccessor)this.worldAccessor, this));
				}
				return serverWorldAccessor;
			}
		}

		public BlockAccessorWorldGen(ServerMain server, ChunkServerThread chunkdbthread)
			: base(server.WorldMap, null)
		{
			this.chunkdbthread = chunkdbthread;
			this.server = server;
			this.worldAccessor = server;
		}

		public void ScheduleBlockLightUpdate(BlockPos pos, int oldBlockid, int newBlockId)
		{
			ServerMapChunk mc = (ServerMapChunk)this.GetMapChunk(pos.X / 32, pos.Z / 32);
			if (mc == null)
			{
				ServerMain.Logger.Worldgen("Mapchunk was null when scheduling a blocklight update at " + ((pos != null) ? pos.ToString() : null));
				return;
			}
			if (mc.ScheduledBlockLightUpdates == null)
			{
				mc.ScheduledBlockLightUpdates = new List<Vec4i>();
			}
			mc.ScheduledBlockLightUpdates.Add(new Vec4i(pos, newBlockId));
		}

		public void RunScheduledBlockLightUpdates(int chunkx, int chunkz)
		{
			ServerMapChunk mc = (ServerMapChunk)this.GetMapChunk(chunkx, chunkz);
			if (mc == null)
			{
				ServerMain.Logger.Worldgen("Mapchunk was null when attempting scheduled blocklight updates at " + chunkx.ToString() + "," + chunkz.ToString());
				return;
			}
			List<Vec4i> scheduledBlockLightUpdates = mc.ScheduledBlockLightUpdates;
			if (scheduledBlockLightUpdates == null || scheduledBlockLightUpdates.Count == 0)
			{
				return;
			}
			BlockPos pos = new BlockPos();
			foreach (Vec4i posAndBlockId in scheduledBlockLightUpdates)
			{
				CollectibleObject collectibleObject = this.server.Blocks[posAndBlockId.W];
				pos.SetAndCorrectDimension(posAndBlockId.X, posAndBlockId.Y, posAndBlockId.Z);
				byte[] lightHsv = collectibleObject.GetLightHsv(this, pos, null);
				if (lightHsv[2] > 0)
				{
					this.server.WorldMap.chunkIlluminatorWorldGen.PlaceBlockLight(lightHsv, pos.X, pos.InternalY, pos.Z);
				}
			}
			mc.ScheduledBlockLightUpdates = null;
		}

		public void ScheduleBlockUpdate(BlockPos pos)
		{
			ChunkColumnLoadRequest req = this.chunkdbthread.GetChunkRequestAtPos(pos.X, pos.Z);
			if (((req != null) ? req.MapChunk : null) == null)
			{
				return;
			}
			req.MapChunk.ScheduledBlockUpdates.Add(pos.Copy());
		}

		public override IMapChunk GetMapChunk(Vec2i chunkPos)
		{
			return this.GetMapChunk(chunkPos.X, chunkPos.Y);
		}

		public override IMapChunk GetMapChunk(int chunkX, int chunkZ)
		{
			long index2d = this.server.WorldMap.MapChunkIndex2D(chunkX, chunkZ);
			if (BlockAccessorWorldGen.cachedChunkIndex2d == index2d)
			{
				return BlockAccessorWorldGen.mapchunkCached;
			}
			ServerMapChunk mapchunk = this.chunkdbthread.GetMapChunk(index2d);
			if (mapchunk != null)
			{
				BlockAccessorWorldGen.cachedChunkIndex2d = index2d;
				BlockAccessorWorldGen.mapchunkCached = mapchunk;
			}
			return mapchunk;
		}

		public override IWorldChunk GetChunk(int chunkX, int chunkY, int chunkZ)
		{
			return this.chunkdbthread.GetGeneratingChunk(chunkX, chunkY, chunkZ);
		}

		[Obsolete("Please use BlockPos version instead for dimension awareness")]
		public override IWorldChunk GetChunkAtBlockPos(int posX, int posY, int posZ)
		{
			return this.chunkdbthread.GetGeneratingChunk(posX / 32, posY / 32, posZ / 32);
		}

		public override IWorldChunk GetChunkAtBlockPos(BlockPos pos)
		{
			return this.chunkdbthread.GetGeneratingChunk(pos.X / 32, pos.Y / 32, pos.Z / 32);
		}

		public override IMapRegion GetMapRegion(int regionX, int regionZ)
		{
			return this.chunkdbthread.GetMapRegion(regionX, regionZ);
		}

		public override int GetBlockId(int posX, int posY, int posZ, int layer)
		{
			long nowChunkIndex3d = this.worldmap.ChunkIndex3D(posX / 32, posY / 32, posZ / 32);
			ServerChunk chunk;
			if (BlockAccessorWorldGen.cachedChunkIndex3d == nowChunkIndex3d)
			{
				chunk = BlockAccessorWorldGen.chunkCached;
			}
			else
			{
				chunk = this.chunkdbthread.GetGeneratingChunkAtPos(posX, posY, posZ);
				if (chunk == null)
				{
					chunk = this.worldmap.GetChunkAtPos(posX, posY, posZ) as ServerChunk;
				}
				if (chunk != null)
				{
					chunk.Unpack();
					BlockAccessorWorldGen.cachedChunkIndex3d = nowChunkIndex3d;
					BlockAccessorWorldGen.chunkCached = chunk;
				}
			}
			if (chunk != null)
			{
				return chunk.Data.GetBlockId(this.worldmap.ChunkSizedIndex3D(posX & MagicNum.ServerChunkSizeMask, posY & MagicNum.ServerChunkSizeMask, posZ & MagicNum.ServerChunkSizeMask), layer);
			}
			if (RuntimeEnv.DebugOutOfRangeBlockAccess)
			{
				ServerMain.Logger.Notification("Tried to get block outside generating chunks! (at pos {0}, {1}, {2} = chunk {3}, {4}, {5}). ", new object[]
				{
					posX,
					posY,
					posZ,
					posX / MagicNum.ServerChunkSize,
					posY / MagicNum.ServerChunkSize,
					posZ / MagicNum.ServerChunkSize
				});
				LoggerBase logger = ServerMain.Logger;
				StackTrace stackTrace = new StackTrace();
				logger.Notification(((stackTrace != null) ? stackTrace.ToString() : null) ?? "");
			}
			else
			{
				ServerMain.Logger.Notification("Tried to get block outside generating chunks! Set RuntimeEnv.DebugOutOfRangeBlockAccess to debug.");
			}
			return 0;
		}

		public override Block GetBlockOrNull(int posX, int posY, int posZ, int layer = 4)
		{
			if (posX < 0 || posY < 0 || posZ < 0 || posX >= this.worldmap.MapSizeX || posZ >= this.worldmap.MapSizeZ)
			{
				return null;
			}
			ServerChunk chunk = this.chunkdbthread.GetGeneratingChunkAtPos(posX, posY, posZ);
			if (chunk != null)
			{
				chunk.Unpack();
				return this.worldmap.Blocks[chunk.Data[this.worldmap.ChunkSizedIndex3D(posX & 31, posY & 31, posZ & 31)]];
			}
			return null;
		}

		public override void SetBlock(int blockId, BlockPos pos, ItemStack byItemstack = null)
		{
			Block newBlock = this.worldmap.Blocks[blockId];
			if (newBlock.ForFluidsLayer)
			{
				this.SetFluidBlock(blockId, pos);
				return;
			}
			ServerChunk chunk = this.chunkdbthread.GetGeneratingChunkAtPos(pos);
			if (chunk != null)
			{
				this.SetSolidBlock(chunk, pos, newBlock, blockId);
				return;
			}
			chunk = this.worldmap.GetChunkAtPos(pos.X, pos.Y, pos.Z) as ServerChunk;
			if (chunk != null)
			{
				int prevBlockID = this.SetSolidBlock(chunk, pos, newBlock, blockId);
				if (newBlock.LightHsv[2] > 0)
				{
					this.ScheduleBlockLightUpdate(pos, prevBlockID, blockId);
				}
				return;
			}
			if (RuntimeEnv.DebugOutOfRangeBlockAccess)
			{
				ServerMain.Logger.Notification("Tried to set block outside generating chunks! (at pos {0}, {1}, {2} = chunk {3}, {4}, {5}) when placing {6}", new object[]
				{
					pos.X,
					pos.Y,
					pos.Z,
					pos.X / MagicNum.ServerChunkSize,
					pos.Y / MagicNum.ServerChunkSize,
					pos.Z / MagicNum.ServerChunkSize,
					this.worldAccessor.GetBlock(blockId)
				});
				ServerMain.Logger.VerboseDebug("Tried to set block outside generating chunks! (at pos {0}, {1}, {2} = chunk {3}, {4}, {5}) when placing {6}", new object[]
				{
					pos.X,
					pos.Y,
					pos.Z,
					pos.X / MagicNum.ServerChunkSize,
					pos.Y / MagicNum.ServerChunkSize,
					pos.Z / MagicNum.ServerChunkSize,
					this.worldAccessor.GetBlock(blockId)
				});
				LoggerBase logger = ServerMain.Logger;
				StackTrace stackTrace = new StackTrace();
				logger.VerboseDebug(((stackTrace != null) ? stackTrace.ToString() : null) ?? "");
				return;
			}
			ServerMain.Logger.Notification("Tried to set block outside generating chunks! Set RuntimeEnv.DebugOutOfRangeBlockAccess to debug.");
		}

		protected int SetSolidBlock(ServerChunk chunk, BlockPos pos, Block newBlock, int blockId)
		{
			chunk.Unpack();
			int index3d = this.worldmap.ChunkSizedIndex3D(pos.X & MagicNum.ServerChunkSizeMask, pos.Y & MagicNum.ServerChunkSizeMask, pos.Z & MagicNum.ServerChunkSizeMask);
			int prevBlockID = chunk.Data.GetBlockId(index3d, 1);
			if (prevBlockID != 0 && this.worldmap.Blocks[prevBlockID].EntityClass != null)
			{
				chunk.RemoveBlockEntity(pos);
				((ServerMapChunk)chunk.MapChunk).NewBlockEntities.Remove(pos);
			}
			chunk.Data[index3d] = blockId;
			if (newBlock.DisplacesLiquids(this, pos))
			{
				chunk.Data.SetFluid(index3d, 0);
			}
			chunk.DirtyForSaving = true;
			return prevBlockID;
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
			ServerChunk chunk = this.chunkdbthread.GetGeneratingChunkAtPos(pos);
			if (chunk != null)
			{
				chunk.Unpack();
				int index3d = this.worldmap.ChunkSizedIndex3D(pos.X & MagicNum.ServerChunkSizeMask, pos.Y & MagicNum.ServerChunkSizeMask, pos.Z & MagicNum.ServerChunkSizeMask);
				chunk.Data.SetFluid(index3d, blockId);
				return;
			}
			chunk = this.worldmap.GetChunkAtPos(pos.X, pos.Y, pos.Z) as ServerChunk;
			if (chunk != null)
			{
				chunk.Unpack();
				int index3d2 = this.worldmap.ChunkSizedIndex3D(pos.X & MagicNum.ServerChunkSizeMask, pos.Y & MagicNum.ServerChunkSizeMask, pos.Z & MagicNum.ServerChunkSizeMask);
				chunk.Data.SetFluid(index3d2, blockId);
				chunk.DirtyForSaving = true;
				return;
			}
			if (RuntimeEnv.DebugOutOfRangeBlockAccess)
			{
				ServerMain.Logger.Notification("Tried to set block outside generating chunks! (at pos {0}, {1}, {2} = chunk {3}, {4}, {5}) when placing {6}", new object[]
				{
					pos.X,
					pos.Y,
					pos.Z,
					pos.X / MagicNum.ServerChunkSize,
					pos.Y / MagicNum.ServerChunkSize,
					pos.Z / MagicNum.ServerChunkSize,
					this.worldAccessor.GetBlock(blockId)
				});
				ServerMain.Logger.VerboseDebug("Tried to set block outside generating chunks! (at pos {0}, {1}, {2} = chunk {3}, {4}, {5}) when placing {6}", new object[]
				{
					pos.X,
					pos.Y,
					pos.Z,
					pos.X / MagicNum.ServerChunkSize,
					pos.Y / MagicNum.ServerChunkSize,
					pos.Z / MagicNum.ServerChunkSize,
					this.worldAccessor.GetBlock(blockId)
				});
				LoggerBase logger = ServerMain.Logger;
				StackTrace stackTrace = new StackTrace();
				logger.VerboseDebug(((stackTrace != null) ? stackTrace.ToString() : null) ?? "");
				return;
			}
			ServerMain.Logger.Notification("Tried to set block outside generating chunks! Set RuntimeEnv.DebugOutOfRangeBlockAccess to debug.");
		}

		public override List<BlockUpdate> Commit()
		{
			return null;
		}

		public override void ExchangeBlock(int blockId, BlockPos pos)
		{
			base.SetBlock(blockId, pos);
		}

		public override void MarkChunkDecorsModified(BlockPos pos)
		{
			if (this.chunkdbthread.GetGeneratingChunkAtPos(pos) != null)
			{
				return;
			}
			base.MarkChunkDecorsModified(pos);
		}

		public override void SpawnBlockEntity(string classname, BlockPos position, ItemStack byItemStack = null)
		{
			ServerChunk chunk = this.chunkdbthread.GetGeneratingChunkAtPos(position);
			if (chunk == null)
			{
				return;
			}
			BlockEntity entity = ServerMain.ClassRegistry.CreateBlockEntity(classname);
			Block block = chunk.GetLocalBlockAtBlockPos(this.server, position);
			entity.CreateBehaviors(block, this.server);
			entity.Pos = position.Copy();
			chunk.AddBlockEntity(entity);
			entity.stackForWorldgen = byItemStack;
			((ServerMapChunk)chunk.MapChunk).NewBlockEntities.Add(position.Copy());
		}

		public void AddEntity(Entity entity)
		{
			ServerChunk chunk = this.chunkdbthread.GetGeneratingChunkAtPos(entity.ServerPos.AsBlockPos);
			if (chunk == null)
			{
				return;
			}
			SaveGame saveGameData = this.server.SaveGameData;
			long num = saveGameData.LastEntityId + 1L;
			saveGameData.LastEntityId = num;
			long entityid = num;
			entity.EntityId = entityid;
			chunk.AddEntity(entity);
		}

		public override BlockEntity GetBlockEntity(BlockPos position)
		{
			ServerChunk chunk = this.chunkdbthread.GetGeneratingChunkAtPos(position);
			if (chunk == null)
			{
				return null;
			}
			return chunk.GetLocalBlockEntityAtBlockPos(position);
		}

		public override void RemoveBlockEntity(BlockPos position)
		{
			ServerChunk chunk = this.chunkdbthread.GetGeneratingChunkAtPos(position);
			if (chunk == null)
			{
				return;
			}
			chunk.RemoveBlockEntity(position);
		}

		public void BeginColumn()
		{
			BlockAccessorWorldGen.cachedChunkIndex3d = -1L;
			BlockAccessorWorldGen.cachedChunkIndex2d = -1L;
		}

		public static void ThreadDispose()
		{
			BlockAccessorWorldGen.chunkCached = null;
			BlockAccessorWorldGen.mapchunkCached = null;
		}

		protected override ChunkData[] LoadChunksToCache(int mincx, int mincy, int mincz, int maxcx, int maxcy, int maxcz, Action<int, int, int> onChunkMissing)
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
						IWorldChunk chunk = this.chunkdbthread.GetGeneratingChunk(cx, cy, cz);
						if (chunk == null)
						{
							chunk = this.worldmap.GetChunk(cx, cy, cz);
						}
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

		internal ChunkServerThread chunkdbthread;

		internal ServerMain server;

		[ThreadStatic]
		private static ServerChunk chunkCached;

		[ThreadStatic]
		private static long cachedChunkIndex3d;

		[ThreadStatic]
		private static ServerMapChunk mapchunkCached;

		[ThreadStatic]
		private static long cachedChunkIndex2d;

		private IServerWorldAccessor worldgenWorldAccessor;
	}
}
