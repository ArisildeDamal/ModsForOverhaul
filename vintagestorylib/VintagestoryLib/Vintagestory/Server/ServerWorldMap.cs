using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SkiaSharp;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.Common.Database;

namespace Vintagestory.Server
{
	public sealed class ServerWorldMap : WorldMap, IChunkProvider, ILandClaimAPI
	{
		public override ILogger Logger
		{
			get
			{
				return ServerMain.Logger;
			}
		}

		ILogger IChunkProvider.Logger
		{
			get
			{
				return ServerMain.Logger;
			}
		}

		public override IList<Block> Blocks
		{
			get
			{
				return this.server.Blocks;
			}
		}

		public override Dictionary<AssetLocation, Block> BlocksByCode
		{
			get
			{
				return this.server.BlocksByCode;
			}
		}

		public override int ChunkSize
		{
			get
			{
				return 32;
			}
		}

		public override int RegionSize
		{
			get
			{
				return 32 * MagicNum.ChunkRegionSizeInChunks;
			}
		}

		public override Vec3i MapSize
		{
			get
			{
				return this.mapsize;
			}
		}

		public override int MapSizeX
		{
			get
			{
				return this.mapsize.X;
			}
		}

		public override int MapSizeY
		{
			get
			{
				return this.mapsize.Y;
			}
		}

		public override int MapSizeZ
		{
			get
			{
				return this.mapsize.Z;
			}
		}

		public override int RegionMapSizeX
		{
			get
			{
				return this.regionMapSizeX;
			}
		}

		public override int RegionMapSizeY
		{
			get
			{
				return this.regionMapSizeY;
			}
		}

		public override int RegionMapSizeZ
		{
			get
			{
				return this.regionMapSizeZ;
			}
		}

		public override IWorldAccessor World
		{
			get
			{
				return this.server;
			}
		}

		public override int ChunkSizeMask
		{
			get
			{
				return 31;
			}
		}

		public ServerWorldMap(ServerMain server)
		{
			this.server = server;
			this.chunkIlluminatorWorldGen = new ChunkIlluminator(null, new BlockAccessorRelaxed(this, server, false, false), MagicNum.ServerChunkSize);
			this.chunkIlluminatorMainThread = new ChunkIlluminator(this, new BlockAccessorRelaxed(this, server, false, false), MagicNum.ServerChunkSize);
			this.RelaxedBlockAccess = new BlockAccessorRelaxed(this, server, true, true);
			this.RawRelaxedBlockAccess = new BlockAccessorRelaxed(this, server, false, false);
			this.StrictBlockAccess = new BlockAccessorStrict(this, server, true, true, false);
			this.BulkBlockAccess = new BlockAccessorRelaxedBulkUpdate(this, server, true, true, false);
			this.PrefetchBlockAccess = new BlockAccessorPrefetch(this, server, true, true);
		}

		public void Init(int sizex, int sizey, int sizez)
		{
			this.mapsize = new Vec3i(sizex, sizey, sizez);
			this.chunkMapSizeY = sizey / 32;
			this.index3dMulX = 2097152;
			this.index3dMulZ = 2097152;
			this.chunkIlluminatorWorldGen.InitForWorld(this.server.Blocks, (ushort)this.server.sunBrightness, sizex, sizey, sizez);
			this.chunkIlluminatorMainThread.InitForWorld(this.server.Blocks, (ushort)this.server.sunBrightness, sizex, sizey, sizez);
			if (GameVersion.IsAtLeastVersion(this.server.SaveGameData.CreatedGameVersion, "1.12.9"))
			{
				this.regionMapSizeX = (int)Math.Ceiling((double)this.mapsize.X / (double)MagicNum.MapRegionSize);
				this.regionMapSizeY = (int)Math.Ceiling((double)this.mapsize.Y / (double)MagicNum.MapRegionSize);
				this.regionMapSizeZ = (int)Math.Ceiling((double)this.mapsize.Z / (double)MagicNum.MapRegionSize);
			}
			else
			{
				this.regionMapSizeX = this.mapsize.X / MagicNum.MapRegionSize;
				this.regionMapSizeY = this.mapsize.Y / MagicNum.MapRegionSize;
				this.regionMapSizeZ = this.mapsize.Z / MagicNum.MapRegionSize;
			}
			this.landClaims = new List<LandClaim>(this.server.SaveGameData.LandClaims);
			base.RebuildLandClaimPartitions();
		}

		public ChunkPos MapRegionPosFromIndex2D(long index)
		{
			return new ChunkPos((int)index, 0, (int)(index >> 32), 0);
		}

		public void MapRegionPosFromIndex2D(long index, out int x, out int z)
		{
			x = (int)index;
			z = (int)(index >> 32);
		}

		public Vec2i MapChunkPosFromChunkIndex2D(long chunkIndex2d)
		{
			return new Vec2i((int)(chunkIndex2d % (long)base.ChunkMapSizeX), (int)(chunkIndex2d / (long)base.ChunkMapSizeX));
		}

		public Dictionary<long, WorldChunk> PositionsToUniqueChunks(List<BlockPos> positions)
		{
			FastSetOfLongs indices = new FastSetOfLongs();
			foreach (BlockPos pos in positions)
			{
				indices.Add(base.ChunkIndex3D(pos.X / 32, pos.InternalY / 32, pos.Z / 32));
			}
			Dictionary<long, WorldChunk> result = new Dictionary<long, WorldChunk>(indices.Count);
			foreach (long chunkIndex in indices)
			{
				result.Add(chunkIndex, this.GetChunk(chunkIndex) as WorldChunk);
			}
			return result;
		}

		public override IWorldChunk GetChunkAtPos(int posX, int posY, int posZ)
		{
			return this.GetServerChunk(posX / MagicNum.ServerChunkSize, posY / MagicNum.ServerChunkSize, posZ / MagicNum.ServerChunkSize);
		}

		public override WorldChunk GetChunk(BlockPos pos)
		{
			return this.GetServerChunk(pos.X / MagicNum.ServerChunkSize, pos.InternalY / MagicNum.ServerChunkSize, pos.Z / MagicNum.ServerChunkSize);
		}

		public override IWorldChunk GetChunk(long index3d)
		{
			return this.GetServerChunk(index3d);
		}

		public ServerChunk GetServerChunk(int chunkX, int chunkY, int chunkZ)
		{
			return this.GetServerChunk(base.ChunkIndex3D(chunkX, chunkY, chunkZ));
		}

		public ServerChunk GetServerChunk(long chunkIndex3d)
		{
			return this.server.GetLoadedChunk(chunkIndex3d);
		}

		public override IWorldChunk GetChunk(int chunkX, int chunkY, int chunkZ)
		{
			return this.GetServerChunk(base.ChunkIndex3D(chunkX, chunkY, chunkZ));
		}

		public IWorldChunk GetUnpackedChunkFast(int chunkX, int chunkY, int chunkZ, bool notRecentlyAccessed = false)
		{
			ServerChunk chunk = null;
			Dictionary<long, ServerChunk> loadedChunks = this.server.loadedChunks;
			lock (loadedChunks)
			{
				if (chunkX == this.prevChunkX && chunkY == this.prevChunkY && chunkZ == this.prevChunkZ)
				{
					if (!notRecentlyAccessed)
					{
						return this.prevChunk;
					}
					chunk = (ServerChunk)this.prevChunk;
				}
				else
				{
					this.prevChunkX = chunkX;
					this.prevChunkY = chunkY;
					this.prevChunkZ = chunkZ;
					chunk = this.server.GetLoadedChunk(base.ChunkIndex3D(chunkX, chunkY, chunkZ));
					this.prevChunk = chunk;
				}
			}
			if (chunk != null)
			{
				chunk.Unpack();
			}
			return chunk;
		}

		public override IWorldChunk GetChunkNonLocking(int chunkX, int chunkY, int chunkZ)
		{
			ServerChunk chunk;
			this.server.loadedChunks.TryGetValue(base.ChunkIndex3D(chunkX, chunkY, chunkZ), out chunk);
			return chunk;
		}

		public override IMapRegion GetMapRegion(int regionX, int regionZ)
		{
			ServerMapRegion reg;
			this.server.loadedMapRegions.TryGetValue(base.MapRegionIndex2D(regionX, regionZ), out reg);
			return reg;
		}

		public IMapRegion GetMapRegion(BlockPos pos)
		{
			ServerMapRegion reg;
			this.server.loadedMapRegions.TryGetValue(base.MapRegionIndex2D(pos.X / this.RegionSize, pos.Z / this.RegionSize), out reg);
			return reg;
		}

		public override IMapChunk GetMapChunk(int chunkX, int chunkZ)
		{
			ServerMapChunk mpc;
			this.server.loadedMapChunks.TryGetValue(base.MapChunkIndex2D(chunkX, chunkZ), out mpc);
			return mpc;
		}

		public override void SendSetBlock(int blockId, int posX, int posY, int posZ)
		{
			this.server.SendSetBlock(blockId, posX, posY, posZ, -1, false);
		}

		public override void SendExchangeBlock(int blockId, int posX, int posY, int posZ)
		{
			this.server.SendSetBlock(blockId, posX, posY, posZ, -1, true);
		}

		public override void SendDecorUpdateBulk(IEnumerable<BlockPos> updatedDecorPositions)
		{
			foreach (BlockPos val in updatedDecorPositions)
			{
				this.MarkDecorsDirty(val);
			}
		}

		public override void SendBlockUpdateBulk(IEnumerable<BlockPos> blockUpdates, bool doRelight)
		{
			foreach (BlockPos val in blockUpdates)
			{
				this.MarkBlockModified(val, doRelight);
			}
		}

		public override void SendBlockUpdateBulkMinimal(Dictionary<BlockPos, BlockUpdate> updates)
		{
			foreach (KeyValuePair<BlockPos, BlockUpdate> val in updates)
			{
				this.server.ModifiedBlocksMinimal.Add(val.Key);
			}
		}

		public void SendBlockUpdateExcept(int blockId, int posX, int posY, int posZ, int clientId)
		{
			this.server.SendSetBlock(blockId, posX, posY, posZ, clientId, false);
		}

		public int GetTerrainGenSurfacePosY(int posX, int posZ)
		{
			long chunkIndex3d = this.server.WorldMap.ChunkIndex3D(posX / MagicNum.ServerChunkSize, 0, posZ / MagicNum.ServerChunkSize);
			ServerChunk chunk = this.GetServerChunk(chunkIndex3d);
			if (chunk == null || chunk.MapChunk == null)
			{
				return 0;
			}
			return (int)(chunk.MapChunk.WorldGenTerrainHeightMap[posZ % MagicNum.ServerChunkSize * MagicNum.ServerChunkSize + posX % MagicNum.ServerChunkSize] + 1);
		}

		public void MarkChunksDirty(BlockPos blockPos, int blockRange)
		{
			int num = (blockPos.X - blockRange) / MagicNum.ServerChunkSize;
			int maxcx = (blockPos.X + blockRange) / MagicNum.ServerChunkSize;
			int mincy = (blockPos.Y - blockRange) / MagicNum.ServerChunkSize;
			int maxcy = (blockPos.Y + blockRange) / MagicNum.ServerChunkSize;
			int mincz = (blockPos.Z - blockRange) / MagicNum.ServerChunkSize;
			int maxcz = (blockPos.Z + blockRange) / MagicNum.ServerChunkSize;
			for (int cx = num; cx <= maxcx; cx++)
			{
				for (int cy = mincy; cy <= maxcy; cy++)
				{
					for (int cz = mincz; cz <= maxcz; cz++)
					{
						ServerChunk chunk = this.GetServerChunk(cx, cy, cz);
						if (chunk != null)
						{
							chunk.MarkModified();
						}
					}
				}
			}
		}

		public override void MarkChunkDirty(int chunkX, int chunkY, int chunkZ, bool priority = false, bool sunRelight = false, Action OnRetesselated = null, bool fireDirtyEvent = true, bool edgeOnly = false)
		{
			ServerChunk chunk = this.GetServerChunk(chunkX, chunkY, chunkZ);
			if (chunk != null)
			{
				chunk.MarkModified();
				if (fireDirtyEvent)
				{
					this.server.api.eventapi.TriggerChunkDirty(new Vec3i(chunkX, chunkY, chunkZ), chunk, EnumChunkDirtyReason.MarkedDirty);
				}
			}
		}

		public override void UpdateLighting(int oldblockid, int newblockid, BlockPos pos)
		{
			long mapchunkindex2d = this.server.WorldMap.MapChunkIndex2D(pos.X / 32, pos.Z / 32);
			ServerMapChunk mapchunk;
			this.server.loadedMapChunks.TryGetValue(mapchunkindex2d, out mapchunk);
			if (mapchunk == null)
			{
				return;
			}
			mapchunk.MarkFresh();
			object lightingTasksLock = this.LightingTasksLock;
			lock (lightingTasksLock)
			{
				this.LightingTasks.Enqueue(new UpdateLightingTask
				{
					oldBlockId = oldblockid,
					newBlockId = newblockid,
					pos = pos.Copy()
				});
			}
		}

		public override void RemoveBlockLight(byte[] oldLightHsV, BlockPos pos)
		{
			object lightingTasksLock = this.LightingTasksLock;
			lock (lightingTasksLock)
			{
				this.LightingTasks.Enqueue(new UpdateLightingTask
				{
					removeLightHsv = oldLightHsV,
					pos = pos.Copy()
				});
			}
			this.server.BroadcastPacket(new Packet_Server
			{
				Id = 72,
				RemoveBlockLight = new Packet_RemoveBlockLight
				{
					PosX = pos.X,
					PosY = pos.Y,
					PosZ = pos.Z,
					LightH = (int)oldLightHsV[0],
					LightS = (int)oldLightHsV[1],
					LightV = (int)oldLightHsV[2]
				}
			}, Array.Empty<IServerPlayer>());
		}

		public override void UpdateLightingAfterAbsorptionChange(int oldAbsorption, int newAbsorption, BlockPos pos)
		{
			long mapchunkindex2d = this.server.WorldMap.MapChunkIndex2D(pos.X / 32, pos.Z / 32);
			ServerMapChunk mapchunk;
			this.server.loadedMapChunks.TryGetValue(mapchunkindex2d, out mapchunk);
			if (mapchunk == null)
			{
				return;
			}
			mapchunk.MarkFresh();
			object lightingTasksLock = this.LightingTasksLock;
			lock (lightingTasksLock)
			{
				this.LightingTasks.Enqueue(new UpdateLightingTask
				{
					oldBlockId = 0,
					newBlockId = 0,
					oldAbsorb = (byte)oldAbsorption,
					newAbsorb = (byte)newAbsorption,
					pos = pos.Copy(),
					absorbUpdate = true
				});
			}
		}

		public override void UpdateLightingBulk(Dictionary<BlockPos, BlockUpdate> blockUpdates)
		{
			foreach (KeyValuePair<BlockPos, BlockUpdate> val in blockUpdates)
			{
				int newblockid = val.Value.NewFluidBlockId;
				if (newblockid < 0)
				{
					newblockid = val.Value.NewSolidBlockId;
				}
				if (newblockid >= 0)
				{
					this.UpdateLighting(val.Value.OldBlockId, newblockid, val.Key);
				}
			}
		}

		public float? GetMaxTimeAwareLightLevelAt(int posX, int posY, int posZ)
		{
			if (!base.IsValidPos(posX, posY, posZ))
			{
				return new float?((float)this.server.SunBrightness);
			}
			IWorldChunk chunk = this.GetChunkAtPos(posX, posY, posZ);
			if (chunk == null)
			{
				return null;
			}
			ushort lightBytes = chunk.Unpack_AndReadLight(base.ChunkSizedIndex3D(posX % 32, posY % 32, posZ % 32));
			float dayLightStrength = this.server.Calendar.GetDayLightStrength((double)posX, (double)posZ);
			return new float?(Math.Max((float)(lightBytes & 31) * dayLightStrength, (float)((lightBytes >> 5) & 31)));
		}

		public override void PrintChunkMap(Vec2i markChunkPos = null)
		{
			SKBitmap bmp = new SKBitmap(this.server.WorldMap.ChunkMapSizeX, this.server.WorldMap.ChunkMapSizeZ, false);
			SKColor color = new SKColor(0, byte.MaxValue, 0, byte.MaxValue);
			this.server.loadedChunksLock.AcquireReadLock();
			try
			{
				foreach (long index3d in this.server.loadedChunks.Keys)
				{
					ChunkPos vec = this.server.WorldMap.ChunkPosFromChunkIndex3D(index3d);
					if (vec.Dimension <= 0)
					{
						bmp.SetPixel(vec.X, vec.Z, color);
					}
				}
			}
			finally
			{
				this.server.loadedChunksLock.ReleaseReadLock();
			}
			int i = 0;
			while (File.Exists("serverchunks" + i.ToString() + ".png"))
			{
				i++;
			}
			if (markChunkPos != null)
			{
				bmp.SetPixel(markChunkPos.X, markChunkPos.Y, new SKColor(byte.MaxValue, 0, 0, byte.MaxValue));
			}
			bmp.Save("serverchunks" + i.ToString() + ".png");
		}

		IWorldChunk IChunkProvider.GetChunk(int chunkX, int chunkY, int chunkZ)
		{
			return this.GetServerChunk(chunkX, chunkY, chunkZ);
		}

		public override BlockEntity GetBlockEntity(BlockPos position)
		{
			WorldChunk chunk = this.GetChunk(position);
			if (chunk == null)
			{
				return null;
			}
			return chunk.GetLocalBlockEntityAtBlockPos(position);
		}

		public override void SpawnBlockEntity(string classname, BlockPos position, ItemStack byItemStack = null)
		{
			WorldChunk chunk = this.GetChunk(position);
			if (chunk == null)
			{
				return;
			}
			if (chunk.GetLocalBlockEntityAtBlockPos(position) != null)
			{
				this.RemoveBlockEntity(position);
			}
			Block block = chunk.GetLocalBlockAtBlockPos(this.server, position);
			BlockEntity be = ServerMain.ClassRegistry.CreateBlockEntity(classname);
			be.Pos = position.Copy();
			be.CreateBehaviors(block, this.server);
			be.Initialize(this.server.api);
			chunk.AddBlockEntity(be);
			be.OnBlockPlaced(byItemStack);
			chunk.MarkModified();
			this.MarkBlockEntityDirty(be.Pos);
		}

		public override void SpawnBlockEntity(BlockEntity be)
		{
			WorldChunk chunk = this.GetChunk(be.Pos);
			if (chunk == null)
			{
				return;
			}
			if (chunk.GetLocalBlockEntityAtBlockPos(be.Pos) != null)
			{
				this.RemoveBlockEntity(be.Pos);
			}
			chunk.AddBlockEntity(be);
			chunk.MarkModified();
			this.MarkBlockEntityDirty(be.Pos);
		}

		public override void RemoveBlockEntity(BlockPos pos)
		{
			WorldChunk chunk = this.GetChunk(pos);
			if (chunk == null)
			{
				return;
			}
			BlockEntity blockEntity = this.GetBlockEntity(pos);
			chunk.RemoveBlockEntity(pos);
			if (blockEntity != null)
			{
				blockEntity.OnBlockRemoved();
			}
			chunk.MarkModified();
		}

		public override void MarkBlockModified(BlockPos pos, bool doRelight = true)
		{
			if (doRelight)
			{
				this.server.ModifiedBlocks.Enqueue(pos);
				return;
			}
			this.server.ModifiedBlocksNoRelight.Enqueue(pos);
		}

		public override void MarkBlockDirty(BlockPos pos, Action onRetesselated)
		{
			this.server.DirtyBlocks.Enqueue(new Vec4i(pos, -1));
		}

		public override void MarkBlockDirty(BlockPos pos, IPlayer skipPlayer = null)
		{
			this.server.DirtyBlocks.Enqueue(new Vec4i(pos, (skipPlayer == null) ? (-1) : (skipPlayer as ServerPlayer).ClientId));
		}

		public override void MarkBlockEntityDirty(BlockPos pos)
		{
			this.server.DirtyBlockEntities.Enqueue(pos.Copy());
			ServerChunk serverChunk = this.GetServerChunk(pos.X / 32, pos.InternalY / 32, pos.Z / 32);
			if (serverChunk == null)
			{
				return;
			}
			serverChunk.MarkModified();
		}

		public override void MarkDecorsDirty(BlockPos pos)
		{
			this.server.ModifiedDecors.Enqueue(pos.Copy());
		}

		public override void TriggerNeighbourBlockUpdate(BlockPos pos)
		{
			this.server.UpdatedBlocks.Enqueue(pos.Copy());
		}

		public override ClimateCondition GetClimateAt(BlockPos pos, EnumGetClimateMode mode = EnumGetClimateMode.NowValues, double totalDays = 0.0)
		{
			ClimateCondition outClimate = this.getWorldGenClimateAt(pos, mode >= EnumGetClimateMode.ForSuppliedDate_TemperatureOnly);
			if (outClimate != null)
			{
				if (mode == EnumGetClimateMode.NowValues)
				{
					totalDays = this.server.Calendar.TotalDays;
				}
				this.server.EventManager.TriggerOnGetClimate(ref outClimate, pos, mode, totalDays);
				return outClimate;
			}
			if (mode != EnumGetClimateMode.ForSuppliedDate_TemperatureOnly)
			{
				return null;
			}
			return new ClimateCondition
			{
				Temperature = 4f,
				WorldGenTemperature = 4f
			};
		}

		public override ClimateCondition GetClimateAt(BlockPos pos, ClimateCondition baseClimate, EnumGetClimateMode mode, double totalDays)
		{
			baseClimate.Temperature = baseClimate.WorldGenTemperature;
			baseClimate.Rainfall = baseClimate.WorldgenRainfall;
			this.server.EventManager.TriggerOnGetClimate(ref baseClimate, pos, mode, totalDays);
			return baseClimate;
		}

		public override ClimateCondition GetClimateAt(BlockPos pos, int climate)
		{
			float temp = Climate.GetScaledAdjustedTemperatureFloat((climate >> 16) & 255, pos.Y - this.server.seaLevel);
			float rain = (float)Climate.GetRainFall((climate >> 8) & 255, pos.Y) / 255f;
			float heightRel = ((float)pos.Y - (float)this.server.seaLevel) / ((float)this.MapSizeY - (float)this.server.seaLevel);
			ClimateCondition outclimate = new ClimateCondition
			{
				Temperature = temp,
				Rainfall = rain,
				Fertility = (float)Climate.GetFertility((int)rain, temp, heightRel) / 255f
			};
			this.server.EventManager.TriggerOnGetClimate(ref outclimate, pos, EnumGetClimateMode.NowValues, this.server.Calendar.TotalDays);
			return outclimate;
		}

		public override Vec3d GetWindSpeedAt(BlockPos pos)
		{
			return this.GetWindSpeedAt(new Vec3d((double)pos.X, (double)pos.Y, (double)pos.Z));
		}

		public override Vec3d GetWindSpeedAt(Vec3d pos)
		{
			Vec3d windspeed = new Vec3d();
			this.server.EventManager.TriggerOnGetWindSpeed(pos, ref windspeed);
			return windspeed;
		}

		public ClimateCondition getWorldGenClimateAt(BlockPos pos, bool temperatureRainfallOnly)
		{
			if (!base.IsValidPos(pos))
			{
				return null;
			}
			IMapRegion mapregion = this.GetMapRegion(pos);
			bool flag;
			if (mapregion == null)
			{
				flag = null != null;
			}
			else
			{
				IntDataMap2D climateMap = mapregion.ClimateMap;
				flag = ((climateMap != null) ? climateMap.Data : null) != null;
			}
			if (!flag || mapregion.ClimateMap.Size == 0)
			{
				return null;
			}
			float normXInRegionClimate = (float)((double)pos.X / (double)this.RegionSize % 1.0);
			float normZInRegionClimate = (float)((double)pos.Z / (double)this.RegionSize % 1.0);
			int climate = mapregion.ClimateMap.GetUnpaddedColorLerpedForNormalizedPos(normXInRegionClimate, normZInRegionClimate);
			float temp = Climate.GetScaledAdjustedTemperatureFloat((climate >> 16) & 255, pos.Y - this.server.seaLevel);
			float rain = (float)Climate.GetRainFall((climate >> 8) & 255, pos.Y);
			int intRain = (int)rain;
			rain /= 255f;
			ClimateCondition conds = new ClimateCondition
			{
				Temperature = temp,
				Rainfall = rain,
				WorldgenRainfall = rain,
				WorldGenTemperature = temp
			};
			if (!temperatureRainfallOnly)
			{
				float heightRel = ((float)pos.Y - (float)this.server.seaLevel) / ((float)this.MapSizeY - (float)this.server.seaLevel);
				conds.Fertility = (float)Climate.GetFertilityFromUnscaledTemp(intRain, (climate >> 16) & 255, heightRel) / 255f;
				conds.GeologicActivity = (float)(climate & 255) / 255f;
				this.AddWorldGenForestShrub(conds, mapregion, pos);
			}
			return conds;
		}

		public void AddWorldGenForestShrub(ClimateCondition conds, IMapRegion mapregion, BlockPos pos)
		{
			float normX = (float)((double)pos.X / (double)this.RegionSize % 1.0);
			float normZ = (float)((double)pos.Z / (double)this.RegionSize % 1.0);
			int forest = mapregion.ForestMap.GetUnpaddedColorLerpedForNormalizedPos(normX, normZ);
			conds.ForestDensity = (float)forest / 255f;
			int shrub = mapregion.ShrubMap.GetUnpaddedColorLerpedForNormalizedPos(normX, normZ) & 255;
			conds.ShrubDensity = (float)shrub / 255f;
		}

		public long ChunkIndex3dToIndex2d(long index3d)
		{
			long chunkX = index3d % (long)this.index3dMulX;
			return index3d / (long)this.index3dMulX % (long)this.index3dMulZ * (long)base.ChunkMapSizeX + chunkX;
		}

		public override void DamageBlock(BlockPos pos, BlockFacing facing, float damage, IPlayer dualCallByPlayer = null)
		{
			Packet_Server packet = new Packet_Server
			{
				Id = 64,
				BlockDamage = new Packet_BlockDamage
				{
					PosX = pos.X,
					PosY = pos.Y,
					PosZ = pos.Z,
					Damage = CollectibleNet.SerializeFloat(damage),
					Facing = facing.Index
				}
			};
			foreach (ConnectedClient client in this.server.Clients.Values)
			{
				if (client.ShouldReceiveUpdatesForPos(pos) && (dualCallByPlayer == null || client.Id != dualCallByPlayer.ClientId))
				{
					this.server.SendPacket(client.Id, packet);
				}
			}
		}

		public void Add(LandClaim claim)
		{
			HashSet<long> regions = new HashSet<long>();
			int regionSize = this.server.WorldMap.RegionSize;
			foreach (Cuboidi cuboidi in claim.Areas)
			{
				int minx = cuboidi.MinX / regionSize;
				int maxx = cuboidi.MaxX / regionSize;
				int minz = cuboidi.MinZ / regionSize;
				int maxz = cuboidi.MaxZ / regionSize;
				for (int x = minx; x <= maxx; x++)
				{
					for (int z = minz; z <= maxz; z++)
					{
						regions.Add(this.server.WorldMap.MapRegionIndex2D(x, z));
					}
				}
			}
			foreach (long index2d in regions)
			{
				List<LandClaim> claims;
				if (!this.LandClaimByRegion.TryGetValue(index2d, out claims))
				{
					claims = (this.LandClaimByRegion[index2d] = new List<LandClaim>());
				}
				claims.Add(claim);
			}
			this.landClaims.Add(claim);
			this.BroadcastClaims(null, new LandClaim[] { claim });
		}

		public bool Remove(LandClaim claim)
		{
			foreach (KeyValuePair<long, List<LandClaim>> val in this.LandClaimByRegion)
			{
				val.Value.Remove(claim);
			}
			bool flag = this.landClaims.Remove(claim);
			if (flag)
			{
				this.BroadcastClaims(this.landClaims, null);
			}
			return flag;
		}

		public void UpdateClaim(LandClaim oldClaim, LandClaim newClaim)
		{
			this.Remove(oldClaim);
			this.Add(newClaim);
			this.BroadcastClaims(this.landClaims, null);
		}

		public void BroadcastClaims(IEnumerable<LandClaim> allClaims, IEnumerable<LandClaim> addClaims)
		{
			Packet_LandClaims landClaims = new Packet_LandClaims();
			if (allClaims != null)
			{
				landClaims.SetAllclaims(allClaims.Select(delegate(LandClaim claim)
				{
					Packet_LandClaim packet_LandClaim = new Packet_LandClaim();
					packet_LandClaim.SetData(SerializerUtil.Serialize<LandClaim>(claim));
					return packet_LandClaim;
				}).ToArray<Packet_LandClaim>());
			}
			if (addClaims != null)
			{
				landClaims.SetAddclaims(addClaims.Select(delegate(LandClaim claim)
				{
					Packet_LandClaim packet_LandClaim2 = new Packet_LandClaim();
					packet_LandClaim2.SetData(SerializerUtil.Serialize<LandClaim>(claim));
					return packet_LandClaim2;
				}).ToArray<Packet_LandClaim>());
			}
			this.server.BroadcastPacket(new Packet_Server
			{
				Id = 75,
				LandClaims = landClaims
			}, Array.Empty<IServerPlayer>());
		}

		public void SendClaims(IServerPlayer player, IEnumerable<LandClaim> allClaims, IEnumerable<LandClaim> addClaims)
		{
			Packet_LandClaims landClaims = new Packet_LandClaims();
			if (allClaims != null)
			{
				landClaims.SetAllclaims(allClaims.Select(delegate(LandClaim claim)
				{
					Packet_LandClaim packet_LandClaim = new Packet_LandClaim();
					packet_LandClaim.SetData(SerializerUtil.Serialize<LandClaim>(claim));
					return packet_LandClaim;
				}).ToArray<Packet_LandClaim>());
			}
			if (addClaims != null)
			{
				landClaims.SetAddclaims(addClaims.Select(delegate(LandClaim claim)
				{
					Packet_LandClaim packet_LandClaim2 = new Packet_LandClaim();
					packet_LandClaim2.SetData(SerializerUtil.Serialize<LandClaim>(claim));
					return packet_LandClaim2;
				}).ToArray<Packet_LandClaim>());
			}
			this.server.SendPacket(player, new Packet_Server
			{
				Id = 75,
				LandClaims = landClaims
			});
		}

		public override List<LandClaim> All
		{
			get
			{
				return this.landClaims;
			}
		}

		public override bool DebugClaimPrivileges
		{
			get
			{
				return this.server.DebugPrivileges;
			}
		}

		internal ServerMain server;

		private Vec3i mapsize = new Vec3i();

		public ChunkIlluminator chunkIlluminatorWorldGen;

		public ChunkIlluminator chunkIlluminatorMainThread;

		public IBlockAccessor StrictBlockAccess;

		public IBlockAccessor RelaxedBlockAccess;

		public BlockAccessorRelaxedBulkUpdate BulkBlockAccess;

		public IBlockAccessor RawRelaxedBlockAccess;

		public BlockAccessorPrefetch PrefetchBlockAccess;

		private int prevChunkX = -1;

		private int prevChunkY = -1;

		private int prevChunkZ = -1;

		private IWorldChunk prevChunk;

		public object LightingTasksLock = new object();

		public Queue<UpdateLightingTask> LightingTasks = new Queue<UpdateLightingTask>();

		private int regionMapSizeX;

		private int regionMapSizeY;

		private int regionMapSizeZ;

		private List<LandClaim> landClaims;
	}
}
