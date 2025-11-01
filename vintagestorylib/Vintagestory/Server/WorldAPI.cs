using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace Vintagestory.Server
{
	internal class WorldAPI : ServerAPIComponentBase, IWorldManagerAPI
	{
		public WorldAPI(ServerMain server)
			: base(server)
		{
		}

		public PlayStyle CurrentPlayStyle
		{
			get
			{
				foreach (Mod mod in this.server.api.ModLoader.Mods)
				{
					if (mod.WorldConfig != null)
					{
						foreach (PlayStyle ps in mod.WorldConfig.PlayStyles)
						{
							if (ps.Code == this.server.SaveGameData.PlayStyle)
							{
								return ps;
							}
						}
					}
				}
				return null;
			}
		}

		public IMapRegion GetMapRegion(int regionX, int regionZ)
		{
			ServerMapRegion mapreg;
			this.server.loadedMapRegions.TryGetValue(this.server.WorldMap.MapRegionIndex2D(regionX, regionZ), out mapreg);
			return mapreg;
		}

		public IMapRegion GetMapRegion(long index2d)
		{
			ServerMapRegion mapreg;
			this.server.loadedMapRegions.TryGetValue(index2d, out mapreg);
			return mapreg;
		}

		public IServerMapChunk GetMapChunk(int chunkX, int chunkZ)
		{
			ServerMapChunk mapchunk;
			this.server.loadedMapChunks.TryGetValue(this.server.WorldMap.MapChunkIndex2D(chunkX, chunkZ), out mapchunk);
			return mapchunk;
		}

		public IMapChunk GetMapChunk(long index2d)
		{
			ServerMapChunk mapchunk;
			this.server.loadedMapChunks.TryGetValue(index2d, out mapchunk);
			return mapchunk;
		}

		public long MapChunkIndex2D(int chunkX, int chunkZ)
		{
			return this.server.WorldMap.MapChunkIndex2D(chunkX, chunkZ);
		}

		public IServerChunk GetChunk(int chunkX, int chunkY, int chunkZ)
		{
			return this.server.WorldMap.GetServerChunk(chunkX, chunkY, chunkZ);
		}

		public IServerChunk GetChunk(BlockPos pos)
		{
			return (IServerChunk)this.server.WorldMap.GetChunk(pos);
		}

		public string CurrentWorldName
		{
			get
			{
				return this.server.GetSaveFilename();
			}
		}

		public int MapSizeX
		{
			get
			{
				return this.server.WorldMap.MapSizeX;
			}
		}

		public int MapSizeY
		{
			get
			{
				return this.server.WorldMap.MapSizeY;
			}
		}

		public int MapSizeZ
		{
			get
			{
				return this.server.WorldMap.MapSizeZ;
			}
		}

		public int ChunkSize
		{
			get
			{
				return MagicNum.ServerChunkSize;
			}
		}

		public int RegionSize
		{
			get
			{
				return MagicNum.ServerChunkSize * MagicNum.ChunkRegionSizeInChunks;
			}
		}

		public int? GetSurfacePosY(int posX, int posZ)
		{
			return new int?(this.server.WorldMap.GetTerrainGenSurfacePosY(posX, posZ));
		}

		public ICachingBlockAccessor GetCachingBlockAccessor(bool synchronize, bool relight)
		{
			return new BlockAccessorCaching(this.server.WorldMap, this.server, synchronize, relight);
		}

		public IBlockAccessor GetBlockAccessor(bool synchronize, bool relight, bool strict, bool debug = false)
		{
			if (strict)
			{
				return new BlockAccessorStrict(this.server.WorldMap, this.server, synchronize, relight, debug);
			}
			return new BlockAccessorRelaxed(this.server.WorldMap, this.server, synchronize, relight);
		}

		public IBulkBlockAccessor GetBlockAccessorBulkUpdate(bool synchronize, bool relight, bool debug = false)
		{
			return new BlockAccessorRelaxedBulkUpdate(this.server.WorldMap, this.server, synchronize, relight, debug);
		}

		public IBlockAccessorRevertable GetBlockAccessorRevertable(bool synchronize, bool relight, bool debug = false)
		{
			return new BlockAccessorRevertable(this.server.WorldMap, this.server, synchronize, relight, debug);
		}

		public IBlockAccessorPrefetch GetBlockAccessorPrefetch(bool synchronize, bool relight)
		{
			return new BlockAccessorPrefetch(this.server.WorldMap, this.server, synchronize, relight);
		}

		public int GetBlockId(AssetLocation code)
		{
			Block block;
			if (!this.server.BlocksByCode.TryGetValue(code, out block))
			{
				ServerMain.Logger.Error("GetBlockId(): Block with code '{0}' does not exist, defaulting to 0 for air", new object[] { code });
				return 0;
			}
			return block.BlockId;
		}

		public void SetBlockLightLevels(float[] lightLevels)
		{
			this.server.SetBlockLightLevels(lightLevels);
		}

		public void SetSunLightLevels(float[] lightLevels)
		{
			this.server.SetSunLightLevels(lightLevels);
		}

		public void SetSunBrightness(int lightlevel)
		{
			this.server.SetSunBrightness(lightlevel);
		}

		public void SetSeaLevel(int sealevel)
		{
			this.server.SetSeaLevel(sealevel);
		}

		[Obsolete("Please use BlockPos version instead for dimension awareness")]
		public bool IsValidPos(int x, int y, int z)
		{
			return this.server.WorldMap.IsValidPos(x, y, z);
		}

		public int Seed
		{
			get
			{
				if (this.server.SaveGameData == null)
				{
					throw new Exception("Game world not initialized yet, you need to call this method after the world has loaded, use the event GameWorldLoad.");
				}
				return this.server.SaveGameData.Seed;
			}
		}

		public bool AutoGenerateChunks
		{
			get
			{
				return this.server.AutoGenerateChunks;
			}
			set
			{
				this.server.AutoGenerateChunks = value;
			}
		}

		public bool SendChunks
		{
			get
			{
				return this.server.SendChunks;
			}
			set
			{
				this.server.SendChunks = value;
			}
		}

		public byte[] GetData(string name)
		{
			return this.server.SaveGameData.GetData(name);
		}

		public void StoreData(string name, byte[] value)
		{
			this.server.SaveGameData.StoreData(name, value);
		}

		public int[] DefaultSpawnPosition
		{
			get
			{
				return new int[]
				{
					this.server.SaveGameData.DefaultSpawn.x,
					this.server.SaveGameData.DefaultSpawn.y.Value,
					this.server.SaveGameData.DefaultSpawn.z
				};
			}
		}

		public ISaveGame SaveGame
		{
			get
			{
				return this.server.SaveGameData;
			}
		}

		public Dictionary<long, IMapChunk> AllLoadedMapchunks
		{
			get
			{
				Dictionary<long, IMapChunk> dict = new Dictionary<long, IMapChunk>();
				foreach (KeyValuePair<long, ServerMapChunk> val in this.server.loadedMapChunks)
				{
					dict[val.Key] = val.Value;
				}
				return dict;
			}
		}

		public Dictionary<long, IServerChunk> AllLoadedChunks
		{
			get
			{
				Dictionary<long, IServerChunk> dict = new Dictionary<long, IServerChunk>();
				this.server.loadedChunksLock.AcquireReadLock();
				Dictionary<long, IServerChunk> dictionary;
				try
				{
					foreach (KeyValuePair<long, ServerChunk> val in this.server.loadedChunks)
					{
						dict[val.Key] = val.Value;
					}
					dictionary = dict;
				}
				finally
				{
					this.server.loadedChunksLock.ReleaseReadLock();
				}
				return dictionary;
			}
		}

		public int CurrentGeneratingChunkCount
		{
			get
			{
				return this.server.chunkThread.requestedChunkColumns.Count;
			}
		}

		public Dictionary<long, IMapRegion> AllLoadedMapRegions
		{
			get
			{
				Dictionary<long, IMapRegion> dict = new Dictionary<long, IMapRegion>();
				foreach (KeyValuePair<long, ServerMapRegion> val in this.server.loadedMapRegions)
				{
					dict[val.Key] = val.Value;
				}
				return dict;
			}
		}

		public List<LandClaim> LandClaims
		{
			get
			{
				return this.server.WorldMap.All;
			}
		}

		public void SetDefaultSpawnPosition(int x, int y, int z)
		{
			if (this.IsValidPos(x, y, z))
			{
				this.server.SaveGameData.DefaultSpawn.x = x;
				this.server.SaveGameData.DefaultSpawn.y = new int?(y);
				this.server.SaveGameData.DefaultSpawn.z = z;
				this.server.ConfigNeedsSaving = true;
				return;
			}
			ServerMain.Logger.Error("[Mod API] Invalid default spawn position suppplied!");
		}

		public void SunFloodChunkColumnForWorldGen(IWorldChunk[] chunks, int chunkX, int chunkZ)
		{
			int chunkY = (int)chunks[0].MapChunk.YMax / this.ChunkSize;
			ushort sunLight = (ushort)this.server.sunBrightness;
			for (int cy = chunkY + 1; cy < this.server.WorldMap.ChunkMapSizeY; cy++)
			{
				IWorldChunk worldChunk = chunks[cy];
				worldChunk.Unpack();
				worldChunk.Lighting.FloodWithSunlight(sunLight);
			}
			this.server.WorldMap.chunkIlluminatorWorldGen.Sunlight(chunks, chunkX, chunkY, chunkZ, 0);
			this.server.WorldMap.chunkIlluminatorWorldGen.SunlightFlood(chunks, chunkX, chunkY, chunkZ);
		}

		public void SunFloodChunkColumnNeighboursForWorldGen(IWorldChunk[] chunks, int chunkX, int chunkZ)
		{
			IMapChunk mp = this.GetMapChunk(chunkX + 1, chunkZ);
			IMapChunk mp2 = this.GetMapChunk(chunkX - 1, chunkZ);
			IMapChunk mp3 = this.GetMapChunk(chunkX, chunkZ + 1);
			IMapChunk mp4 = this.GetMapChunk(chunkX, chunkZ - 1);
			int worldheight = this.server.WorldMap.MapSizeY;
			int chunkY = GameMath.Max(new int[]
			{
				(int)chunks[0].MapChunk.YMax,
				(mp == null) ? (worldheight - 1) : ((int)mp.YMax),
				(mp2 == null) ? (worldheight - 1) : ((int)mp2.YMax),
				(mp3 == null) ? (worldheight - 1) : ((int)mp3.YMax),
				(mp4 == null) ? (worldheight - 1) : ((int)mp4.YMax)
			}) / this.ChunkSize;
			this.server.WorldMap.chunkIlluminatorWorldGen.SunLightFloodNeighbourChunks(chunks, chunkX, chunkY, chunkZ, 0);
		}

		public void LoadChunkColumn(int chunkX, int chunkZ, bool keepLoaded = false)
		{
			this.server.LoadChunkColumn(chunkX, chunkZ, keepLoaded);
		}

		public void LoadChunkColumnFast(int chunkX, int chunkZ, ChunkLoadOptions options = null)
		{
			this.server.LoadChunkColumnFast(chunkX, chunkZ, options);
		}

		public void LoadChunkColumnPriority(int chunkX, int chunkZ, ChunkLoadOptions options = null)
		{
			this.server.LoadChunkColumnFast(chunkX, chunkZ, options);
		}

		public void LoadChunkColumnFast(int chunkX1, int chunkZ1, int chunkX2, int chunkZ2, ChunkLoadOptions options = null)
		{
			this.server.LoadChunkColumnFast(chunkX1, chunkZ1, chunkX2, chunkZ2, options);
		}

		public void LoadChunkColumnPriority(int chunkX1, int chunkZ1, int chunkX2, int chunkZ2, ChunkLoadOptions options = null)
		{
			this.server.LoadChunkColumnFast(chunkX1, chunkZ1, chunkX2, chunkZ2, options);
		}

		public void PeekChunkColumn(int chunkX, int chunkZ, ChunkPeekOptions options)
		{
			this.server.PeekChunkColumn(chunkX, chunkZ, options);
		}

		public void TestChunkExists(int chunkX, int chunkY, int chunkZ, Action<bool> onTested)
		{
			this.server.TestChunkExists(chunkX, chunkY, chunkZ, onTested, EnumChunkType.Chunk);
		}

		public void TestMapChunkExists(int chunkX, int chunkZ, Action<bool> onTested)
		{
			this.server.TestChunkExists(chunkX, 0, chunkZ, onTested, EnumChunkType.MapChunk);
		}

		public void TestMapRegionExists(int regionX, int regionZ, Action<bool> onTested)
		{
			this.server.TestChunkExists(regionX, 0, regionZ, onTested, EnumChunkType.MapRegion);
		}

		public void BroadcastChunk(int chunkX, int chunkY, int chunkZ, bool onlyIfInRange)
		{
			this.server.BroadcastChunk(chunkX, chunkY, chunkZ, onlyIfInRange);
		}

		public void SendChunk(int chunkX, int chunkY, int chunkZ, IServerPlayer player, bool onlyIfInRange)
		{
			this.server.SendChunk(chunkX, chunkY, chunkZ, player, onlyIfInRange);
		}

		public bool HasChunk(int chunkX, int chunkY, int chunkZ, IServerPlayer player)
		{
			return (player as ServerPlayer).client.DidSendChunk(this.server.WorldMap.ChunkIndex3D(chunkX, chunkY, chunkZ));
		}

		public void ResendMapChunk(int chunkX, int chunkZ, bool onlyIfInRange)
		{
			this.server.ResendMapChunk(chunkX, chunkZ, onlyIfInRange);
		}

		public void UnloadChunkColumn(int chunkX, int chunkZ)
		{
			long index2d = this.server.WorldMap.MapChunkIndex2D(chunkX, chunkZ);
			this.server.RemoveChunkColumnFromForceLoadedList(index2d);
			for (int cy = 0; cy < this.server.WorldMap.ChunkMapSizeY; cy++)
			{
				long chunkIndex3d = this.server.WorldMap.ChunkIndex3D(chunkX, cy, chunkZ);
				ServerChunk chunk = this.server.WorldMap.GetServerChunk(chunkIndex3d);
				this.server.api.eventapi.TriggerChunkColumnUnloaded(new Vec3i(chunkX, cy, chunkZ));
				if (chunk != null)
				{
					this.server.loadedChunksLock.AcquireWriteLock();
					try
					{
						if (this.server.loadedChunks.Remove(chunkIndex3d))
						{
							this.server.ChunkColumnRequested.Remove(index2d);
						}
					}
					finally
					{
						this.server.loadedChunksLock.ReleaseWriteLock();
					}
					Entity[] entities = chunk.Entities;
					if (entities != null)
					{
						for (int i = 0; i < entities.Length; i++)
						{
							Entity e = entities[i];
							if (e == null)
							{
								if (i >= chunk.EntitiesCount)
								{
									break;
								}
							}
							else if (!(e is EntityPlayer))
							{
								this.server.DespawnEntity(e, new EntityDespawnData
								{
									Reason = EnumDespawnReason.Unload
								});
							}
						}
					}
					foreach (KeyValuePair<BlockPos, BlockEntity> val in chunk.BlockEntities)
					{
						val.Value.OnBlockUnloaded();
					}
					chunk.Dispose();
				}
			}
			for (int cy2 = 0; cy2 < this.server.WorldMap.ChunkMapSizeY; cy2++)
			{
				long chunkIndex3d2 = this.server.WorldMap.ChunkIndex3D(chunkX, cy2, chunkZ);
				this.server.unloadedChunks.Enqueue(chunkIndex3d2);
			}
		}

		public void DeleteMapRegion(int regionX, int regionZ)
		{
			this.server.deleteMapRegions.Enqueue(this.server.WorldMap.MapRegionIndex2D(regionX, regionZ));
		}

		public void DeleteChunkColumn(int chunkX, int chunkZ)
		{
			this.UnloadChunkColumn(chunkX, chunkZ);
			this.server.deleteChunkColumns.Enqueue(this.server.WorldMap.MapChunkIndex2D(chunkX, chunkZ));
		}

		public void FullRelight(BlockPos minPos, BlockPos maxPos)
		{
			this.server.WorldMap.chunkIlluminatorMainThread.FullRelight(minPos, maxPos);
			int minx = GameMath.Clamp(Math.Min(minPos.X, maxPos.X) - 32, 0, this.server.WorldMap.MapSizeX);
			int miny = GameMath.Clamp(Math.Min(minPos.Y, maxPos.Y) - 32, 0, this.server.WorldMap.MapSizeY);
			int minz = GameMath.Clamp(Math.Min(minPos.Z, maxPos.Z) - 32, 0, this.server.WorldMap.MapSizeZ);
			int maxx = GameMath.Clamp(Math.Max(minPos.X, maxPos.X) + 32, 0, this.server.WorldMap.MapSizeX);
			int maxy = GameMath.Clamp(Math.Max(minPos.Y, maxPos.Y) + 32, 0, this.server.WorldMap.MapSizeY);
			int num = GameMath.Clamp(Math.Max(minPos.Z, maxPos.Z) + 32, 0, this.server.WorldMap.MapSizeZ);
			int mincx = minx / 32;
			int mincy = miny / 32;
			int mincz = minz / 32;
			int maxcx = maxx / 32;
			int maxcy = maxy / 32;
			int maxcz = num / 32;
			for (int cx = mincx; cx <= maxcx; cx++)
			{
				for (int cy = mincy; cy <= maxcy; cy++)
				{
					for (int cz = mincz; cz <= maxcz; cz++)
					{
						this.server.BroadcastChunk(cx, cy, cz, true);
						this.server.WorldMap.MarkChunkDirty(cx, cy, cz, false, false, null, true, false);
					}
				}
			}
		}

		public void FullRelight(BlockPos minPos, BlockPos maxPos, bool resendToClients)
		{
			if (resendToClients)
			{
				this.FullRelight(minPos, maxPos);
				return;
			}
			this.server.WorldMap.chunkIlluminatorMainThread.FullRelight(minPos, maxPos);
		}

		public long GetNextUniqueId()
		{
			return this.server.GetNextHerdId();
		}

		public long ChunkIndex3D(int chunkX, int chunkY, int chunkZ)
		{
			return ((long)chunkY * (long)this.server.WorldMap.index3dMulZ + (long)chunkZ) * (long)this.server.WorldMap.index3dMulX + (long)chunkX;
		}

		public long MapRegionIndex2D(int regionX, int regionZ)
		{
			return ((long)regionZ << 32) + (long)regionX;
		}

		public Vec2i MapChunkPosFromChunkIndex2D(long chunkIndex2d)
		{
			return new Vec2i((int)(chunkIndex2d % (long)this.server.WorldMap.ChunkMapSizeX), (int)(chunkIndex2d / (long)this.server.WorldMap.ChunkMapSizeX));
		}

		public Vec3i MapRegionPosFromIndex2D(long index)
		{
			return new Vec3i((int)index, 0, (int)(index >> 32));
		}

		public long MapRegionIndex2DByBlockPos(int posX, int posZ)
		{
			int regionX = posX / this.RegionSize;
			int regionZ = posZ / this.RegionSize;
			return this.MapRegionIndex2D(regionX, regionZ);
		}

		internal int RegionMapSizeX
		{
			get
			{
				return this.server.WorldMap.RegionMapSizeX;
			}
		}

		public int ChunkDeletionsInQueue
		{
			get
			{
				return this.server.deleteChunkColumns.Count;
			}
		}

		public IServerChunk GetChunk(long chunkIndex3d)
		{
			return this.server.GetLoadedChunk(chunkIndex3d);
		}

		public void CreateChunkColumnForDimension(int cx, int cz, int dim)
		{
			this.server.CreateChunkColumnForDimension(cx, cz, dim);
		}

		public void LoadChunkColumnForDimension(int cx, int cz, int dim)
		{
			this.server.LoadChunkColumnForDimension(cx, cz, dim);
		}

		public void ForceSendChunkColumn(IServerPlayer player, int cx, int cz, int dimension)
		{
			this.server.ForceSendChunkColumn(player, cx, cz, dimension);
		}

		public bool BlockingTestMapRegionExists(int regionX, int regionZ)
		{
			if (this.server.RunPhase >= EnumServerRunPhase.RunGame)
			{
				throw new InvalidOperationException("Can not be executed after EnumServerRunPhase.WorldReady");
			}
			return this.server.BlockingTestMapRegionExists(regionX, regionZ);
		}

		public bool BlockingTestMapChunkExists(int chunkX, int chunkZ)
		{
			if (this.server.RunPhase >= EnumServerRunPhase.RunGame)
			{
				throw new InvalidOperationException("Can not be executed after EnumServerRunPhase.WorldReady");
			}
			return this.server.BlockingTestMapChunkExists(chunkX, chunkZ);
		}

		public IServerChunk[] BlockingLoadChunkColumn(int chunkX, int chunkZ)
		{
			if (this.server.RunPhase >= EnumServerRunPhase.RunGame)
			{
				throw new InvalidOperationException("Can not be executed after EnumServerRunPhase.WorldReady");
			}
			return this.server.BlockingLoadChunkColumn(chunkX, chunkZ);
		}
	}
}
