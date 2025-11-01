using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf
{
	public sealed class ClientWorldMap : WorldMap, IChunkProvider, ILandClaimAPI
	{
		public override Vec3i MapSize
		{
			get
			{
				return this.mapsize;
			}
		}

		public ClientWorldMap(ClientMain game)
		{
			this.game = game;
			this.ClientChunkSize = 32;
			this.chunkDataPool = new ClientChunkDataPool(this.ClientChunkSize, game);
			game.RegisterGameTickListener(delegate(float dt)
			{
				this.chunkDataPool.SlowDispose();
			}, 1033, 0);
			ClientSettings.Inst.AddWatcher<int>("optimizeRamMode", new OnSettingsChanged<int>(this.updateChunkDataPoolTresholds));
			this.updateChunkDataPoolTresholds(ClientSettings.OptimizeRamMode);
			this.chunkIlluminator = new ChunkIlluminator(this, new BlockAccessorRelaxed(this, game, false, false), this.ClientChunkSize);
			this.RelaxedBlockAccess = new BlockAccessorRelaxed(this, game, false, true);
			this.CachingBlockAccess = new BlockAccessorCaching(this, game, false, true);
			this.BulkBlockAccess = new BlockAccessorRelaxedBulkUpdate(this, game, false, true, false);
			this.NoRelightBulkBlockAccess = new BlockAccessorRelaxedBulkUpdate(this, game, false, false, false);
			this.BulkMinimalBlockAccess = new BlockAccessorBulkMinimalUpdate(this, game, false, false);
		}

		private void updateChunkDataPoolTresholds(int optimizerammode)
		{
			if (optimizerammode == 2)
			{
				this.chunkDataPool.CacheSize = 1500;
				this.chunkDataPool.SlowDisposeThreshold = 1000;
				return;
			}
			if (optimizerammode == 1)
			{
				this.chunkDataPool.CacheSize = 2000;
				this.chunkDataPool.SlowDisposeThreshold = 1350;
				return;
			}
			this.chunkDataPool.CacheSize = 5000;
			this.chunkDataPool.SlowDisposeThreshold = 3500;
		}

		private void switchRedAndBlueChannels(int[] pixels)
		{
			for (int i = 0; i < pixels.Length; i++)
			{
				int color = pixels[i];
				int r = color & 255;
				int b = (color >> 16) & 255;
				pixels[i] = (int)((long)color & (long)((ulong)(-16711936))) | (r << 16) | b;
			}
		}

		public void OnLightLevelsReceived()
		{
			if (this.blockTexturesGo)
			{
				this.OnBlocksAndLightLevelsReceived();
				return;
			}
			this.lightsGo = true;
		}

		public void OnBlocksAndLightLevelsReceived()
		{
			this.EmptyChunk = ClientChunk.CreateNew(this.chunkDataPool);
			ushort sunLit = (ushort)this.SunBrightness;
			this.EmptyChunk.Lighting.FloodWithSunlight(sunLit);
			this.chunkIlluminator.InitForWorld(this.game.Blocks, sunLit, this.MapSizeX, this.MapSizeY, this.MapSizeZ);
			this.EmptyChunk.Empty = true;
		}

		public void BlockTexturesLoaded()
		{
			this.PopulateColorMaps();
			if (this.lightsGo)
			{
				this.OnBlocksAndLightLevelsReceived();
				return;
			}
			this.blockTexturesGo = true;
		}

		public int LoadColorMaps()
		{
			int rectid = 0;
			foreach (KeyValuePair<string, ColorMap> val in this.game.ColorMaps)
			{
				if (this.game.disposed)
				{
					return rectid;
				}
				ColorMap cmap = val.Value;
				CompositeTexture texture = cmap.Texture;
				if (((texture != null) ? texture.Base : null) == null)
				{
					this.game.Logger.Warning("Incorrect texture definition for color map entry {0}", new object[] { this.game.ColorMaps.IndexOfKey(val.Key) });
					cmap.LoadIntoBlockTextureAtlas = false;
				}
				else
				{
					AssetLocationAndSource loc = new AssetLocationAndSource(cmap.Texture.Base.Clone().WithPathPrefixOnce("textures/").WithPathAppendixOnce(".png"));
					loc.AddToAllAtlasses = true;
					BitmapRef map = this.game.Platform.CreateBitmapFromPng(this.game.AssetManager.Get(loc));
					if (this.game.disposed)
					{
						return rectid;
					}
					if (cmap.LoadIntoBlockTextureAtlas)
					{
						cmap.BlockAtlasTextureSubId = this.game.BlockAtlasManager.GetOrAddTextureLocation(loc);
						loc.loadedAlready = 2;
						cmap.RectIndex = rectid + (cmap.ExtraFlags << 6);
						rectid++;
					}
					if (this.game.disposed)
					{
						return rectid;
					}
					cmap.Pixels = map.Pixels;
					cmap.OuterSize = new Size2i(map.Width, map.Height);
					this.switchRedAndBlueChannels(cmap.Pixels);
				}
			}
			return rectid;
		}

		public void PopulateColorMaps()
		{
			float[] mapRects = (this.game.shUniforms.ColorMapRects4 = new float[160]);
			int i = 0;
			foreach (KeyValuePair<string, ColorMap> val in this.game.ColorMaps)
			{
				ColorMap cmap = val.Value;
				if (cmap.LoadIntoBlockTextureAtlas)
				{
					float padx = (float)cmap.Padding / (float)this.game.BlockAtlasManager.Size.Width;
					float pady = (float)cmap.Padding / (float)this.game.BlockAtlasManager.Size.Height;
					TextureAtlasPosition texPos = this.game.BlockAtlasManager.Positions[cmap.BlockAtlasTextureSubId];
					mapRects[i++] = texPos.x1 + padx;
					mapRects[i++] = texPos.y1 + pady;
					mapRects[i++] = texPos.x2 - texPos.x1 - 2f * padx;
					mapRects[i++] = texPos.y2 - texPos.y1 - 2f * pady;
				}
			}
		}

		ILogger IChunkProvider.Logger
		{
			get
			{
				return ScreenManager.Platform.Logger;
			}
		}

		public override ILogger Logger
		{
			get
			{
				return ScreenManager.Platform.Logger;
			}
		}

		public override int ChunkSize
		{
			get
			{
				return this.ClientChunkSize;
			}
		}

		public override int ChunkSizeMask
		{
			get
			{
				return this.ClientChunkSize - 1;
			}
		}

		public int MapRegionSizeInChunks
		{
			get
			{
				return this.RegionSize / this.ServerChunkSize;
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

		internal int MapChunkMapSizeX
		{
			get
			{
				return this.mapsize.X / this.ServerChunkSize;
			}
		}

		internal int MapChunkMapSizeY
		{
			get
			{
				return this.mapsize.Y / this.ServerChunkSize;
			}
		}

		internal int MapChunkMapSizeZ
		{
			get
			{
				return this.mapsize.Z / this.ServerChunkSize;
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

		public override IList<Block> Blocks
		{
			get
			{
				return this.game.Blocks;
			}
		}

		public override Dictionary<AssetLocation, Block> BlocksByCode
		{
			get
			{
				return this.game.BlocksByCode;
			}
		}

		public override IWorldAccessor World
		{
			get
			{
				return this.game;
			}
		}

		public override int RegionSize
		{
			get
			{
				return this.regionSize;
			}
		}

		public override List<LandClaim> All
		{
			get
			{
				return this.LandClaims;
			}
		}

		public override bool DebugClaimPrivileges
		{
			get
			{
				return false;
			}
		}

		public long MapRegionIndex2DFromClientChunkCoord(int chunkX, int chunkZ)
		{
			chunkX *= this.ClientChunkSize;
			chunkZ *= this.ClientChunkSize;
			return base.MapRegionIndex2D(chunkX / this.ServerChunkSize / this.MapRegionSizeInChunks, chunkZ / this.ServerChunkSize / this.MapRegionSizeInChunks);
		}

		public int[] LoadOrCreateLerpedClimateMap(int chunkX, int chunkZ)
		{
			object lerpedClimateMapsLock = this.LerpedClimateMapsLock;
			int[] array;
			lock (lerpedClimateMapsLock)
			{
				long index2d = this.MapRegionIndex2DFromClientChunkCoord(chunkX, chunkZ);
				int[] lerpedClimateMap = this.LerpedClimateMaps[index2d];
				if (lerpedClimateMap == null)
				{
					int num = chunkX * this.ClientChunkSize / this.ServerChunkSize / this.MapRegionSizeInChunks;
					int num2 = chunkZ * this.ClientChunkSize / this.ServerChunkSize / this.MapRegionSizeInChunks;
					ClientMapRegion mapRegion;
					this.game.WorldMap.MapRegions.TryGetValue(index2d, out mapRegion);
					if (mapRegion == null || mapRegion.ClimateMap == null || mapRegion.ClimateMap.InnerSize <= 0)
					{
						if (this.placeHolderClimateMap == null)
						{
							this.placeHolderClimateMap = new int[this.RegionSize * this.RegionSize];
							this.placeHolderClimateMap.Fill(11842740);
						}
						return this.placeHolderClimateMap;
					}
					lerpedClimateMap = GameMath.BiLerpColorMap(mapRegion.ClimateMap, this.RegionSize / mapRegion.ClimateMap.InnerSize);
					this.LerpedClimateMaps[index2d] = lerpedClimateMap;
				}
				array = lerpedClimateMap;
			}
			return array;
		}

		public float[] LoadOceanityCorners(int chunkX, int chunkZ)
		{
			long index2d = this.MapRegionIndex2DFromClientChunkCoord(chunkX, chunkZ);
			ClientMapRegion mapRegion;
			this.game.WorldMap.MapRegions.TryGetValue(index2d, out mapRegion);
			if (((mapRegion != null) ? mapRegion.OceanMap : null) != null && mapRegion.OceanMap.InnerSize > 0)
			{
				IntDataMap2D om = mapRegion.OceanMap;
				float lxA = (float)(chunkX * 32 % this.regionSize) / (float)this.regionSize * (float)om.InnerSize;
				float lzA = (float)(chunkZ * 32 % this.regionSize) / (float)this.regionSize * (float)om.InnerSize;
				float lxB = lxA + 1f;
				float lzB = lzA + 1f;
				return new float[]
				{
					om.GetIntLerpedCorrectly(lxA, lzA),
					om.GetIntLerpedCorrectly(lxB, lzA),
					om.GetIntLerpedCorrectly(lxA, lzB),
					om.GetIntLerpedCorrectly(lxB, lzB)
				};
			}
			return null;
		}

		public ColorMapData getColorMapData(Block block, int posX, int posY, int posZ)
		{
			int rndX = GameMath.MurmurHash3Mod(posX, 0, posZ, 3);
			int rndZ = GameMath.MurmurHash3Mod(posX, 1, posZ, 3);
			int climate = this.GetClimate(posX + rndX, posZ + rndZ);
			int temp = Climate.GetAdjustedTemperature((climate >> 16) & 255, posY - ClientWorldMap.seaLevel);
			int rain = Climate.GetRainFall((climate >> 8) & 255, posY);
			int seasonMapIndex = 0;
			ColorMap sval;
			if (block.SeasonColorMap != null && this.game.ColorMaps.TryGetValue(block.SeasonColorMap, out sval))
			{
				seasonMapIndex = sval.RectIndex + 1;
			}
			int climateMapIndex = 0;
			ColorMap cval;
			if (block.ClimateColorMap != null && this.game.ColorMaps.TryGetValue(block.ClimateColorMap, out cval))
			{
				climateMapIndex = cval.RectIndex + 1;
			}
			return new ColorMapData((byte)seasonMapIndex, (byte)climateMapIndex, (byte)temp, (byte)rain, block.Frostable);
		}

		public int ApplyColorMapOnRgba(string climateColorMap, string seasonColorMap, int color, int posX, int posY, int posZ, bool flipRb)
		{
			ColorMap climateMap = ((climateColorMap == null) ? null : this.game.ColorMaps[climateColorMap]);
			ColorMap seasonMap = ((seasonColorMap == null) ? null : this.game.ColorMaps[seasonColorMap]);
			return this.ApplyColorMapOnRgba(climateMap, seasonMap, color, posX, posY, posZ, flipRb);
		}

		public int ApplyColorMapOnRgba(ColorMap climateMap, ColorMap seasonMap, int color, int posX, int posY, int posZ, bool flipRb)
		{
			int rndX = GameMath.MurmurHash3Mod(posX, 0, posZ, 3);
			int rndZ = GameMath.MurmurHash3Mod(posX, 1, posZ, 3);
			int climate = this.GetClimate(posX + rndX, posZ + rndZ);
			int temp = (climate >> 16) & 255;
			int rain = Climate.GetRainFall((climate >> 8) & 255, posY);
			EnumHemisphere hemi = this.game.Calendar.GetHemisphere(new BlockPos(posX, posY, posZ));
			return this.ApplyColorMapOnRgba(climateMap, seasonMap, color, rain, temp, flipRb, (float)GameMath.MurmurHash3Mod(posX, posY, posZ, 100) / 100f, (hemi == EnumHemisphere.South) ? 0.5f : 0f, posY - ClientWorldMap.seaLevel);
		}

		public int ApplyColorMapOnRgba(string climateColorMap, string seasonColorMap, int color, int rain, int temp, bool flipRb, float seasonYPixelRel = 0f, float seasonXOffset = 0f)
		{
			ColorMap climateMap = ((climateColorMap == null) ? null : this.game.ColorMaps[climateColorMap]);
			ColorMap seasonMap = ((seasonColorMap == null) ? null : this.game.ColorMaps[seasonColorMap]);
			return this.ApplyColorMapOnRgba(climateMap, seasonMap, color, rain, temp, flipRb, seasonYPixelRel, seasonXOffset, 0);
		}

		public int ApplyColorMapOnRgba(ColorMap climateMap, ColorMap seasonMap, int color, int rain, int temp, bool flipRb, float seasonYPixelRel, float seasonXOffset, int heightAboveSealevel)
		{
			int tintColor = -1;
			if (climateMap != null)
			{
				float winner = (float)(climateMap.OuterSize.Width - 2 * climateMap.Padding);
				float hinner = (float)(climateMap.OuterSize.Height - 2 * climateMap.Padding);
				int x = (int)GameMath.Clamp((float)Climate.GetAdjustedTemperature(temp, heightAboveSealevel) / 255f * winner, (float)(-(float)climateMap.Padding), (float)(climateMap.OuterSize.Width - 1));
				int y = (int)GameMath.Clamp((float)rain / 255f * hinner, (float)(-(float)climateMap.Padding), (float)(climateMap.OuterSize.Height - 1));
				tintColor = climateMap.Pixels[(y + climateMap.Padding) * climateMap.OuterSize.Width + x + climateMap.Padding];
				if (flipRb)
				{
					int r = tintColor & 255;
					int g = (tintColor >> 8) & 255;
					int b = (tintColor >> 16) & 255;
					tintColor = (((tintColor >> 24) & 255) << 24) | (r << 16) | (g << 8) | b;
				}
			}
			if (seasonMap != null)
			{
				int x2 = (int)(GameMath.Mod(this.game.Calendar.YearRel + seasonXOffset, 1f) * (float)(seasonMap.OuterSize.Width - seasonMap.Padding));
				int y2 = (int)(seasonYPixelRel * (float)seasonMap.OuterSize.Height);
				int seasonColor = seasonMap.Pixels[(y2 + seasonMap.Padding) * seasonMap.OuterSize.Width + x2 + seasonMap.Padding];
				if (flipRb)
				{
					int r2 = seasonColor & 255;
					int g2 = (seasonColor >> 8) & 255;
					int b2 = (seasonColor >> 16) & 255;
					seasonColor = (((seasonColor >> 24) & 255) << 24) | (r2 << 16) | (g2 << 8) | b2;
				}
				float seasonWeight = GameMath.Clamp(0.5f - GameMath.Cos((float)temp / 42f) / 2.3f + (float)(Math.Max(0, 128 - temp) / 512) - (float)(Math.Max(0, temp - 130) / 200), 0f, 1f);
				tintColor = ColorUtil.ColorOverlay(tintColor, seasonColor, seasonWeight);
			}
			return ColorUtil.ColorMultiplyEach(color, tintColor);
		}

		public int GetClimate(int posX, int posZ)
		{
			if (posX < 0 || posZ < 0 || posX >= this.MapSizeX || posZ >= this.MapSizeZ)
			{
				return 0;
			}
			return this.LoadOrCreateLerpedClimateMap(posX / this.ClientChunkSize, posZ / this.ClientChunkSize)[posZ % this.RegionSize * this.RegionSize + posX % this.RegionSize];
		}

		public int GetClimateFast(int[] map, int inRegionX, int inRegionZ)
		{
			return map[inRegionZ * this.RegionSize + inRegionX];
		}

		internal ClientChunk GetChunkAtBlockPos(int posX, int posY, int posZ)
		{
			int num = posX >> 5;
			int cy = posY >> 5;
			int cz = posZ >> 5;
			long index3d = MapUtil.Index3dL(num, cy, cz, (long)this.index3dMulX, (long)this.index3dMulZ);
			ClientChunk chunk = null;
			object obj = this.chunksLock;
			lock (obj)
			{
				this.chunks.TryGetValue(index3d, out chunk);
			}
			return chunk;
		}

		public override IWorldChunk GetChunk(long index3d)
		{
			ClientChunk chunk = null;
			object obj = this.chunksLock;
			lock (obj)
			{
				this.chunks.TryGetValue(index3d, out chunk);
			}
			return chunk;
		}

		public override WorldChunk GetChunk(BlockPos pos)
		{
			return this.GetClientChunk(pos.X / this.ClientChunkSize, pos.InternalY / this.ClientChunkSize, pos.Z / this.ClientChunkSize);
		}

		internal ClientChunk GetClientChunkAtBlockPos(BlockPos pos)
		{
			return this.GetClientChunk(pos.X / this.ClientChunkSize, pos.InternalY / this.ClientChunkSize, pos.Z / this.ClientChunkSize);
		}

		internal ClientChunk GetClientChunk(int chunkX, int chunkY, int chunkZ)
		{
			long index3d = MapUtil.Index3dL(chunkX, chunkY, chunkZ, (long)this.index3dMulX, (long)this.index3dMulZ);
			ClientChunk chunk = null;
			object obj = this.chunksLock;
			lock (obj)
			{
				this.chunks.TryGetValue(index3d, out chunk);
			}
			return chunk;
		}

		public override IWorldChunk GetChunk(int chunkX, int chunkY, int chunkZ)
		{
			long index3d = MapUtil.Index3dL(chunkX, chunkY, chunkZ, (long)this.index3dMulX, (long)this.index3dMulZ);
			ClientChunk chunk = null;
			object obj = this.chunksLock;
			lock (obj)
			{
				this.chunks.TryGetValue(index3d, out chunk);
			}
			return chunk;
		}

		internal ClientChunk GetClientChunk(long index3d)
		{
			ClientChunk chunk = null;
			object obj = this.chunksLock;
			lock (obj)
			{
				this.chunks.TryGetValue(index3d, out chunk);
			}
			return chunk;
		}

		internal void LoadChunkFromPacket(Packet_ServerChunk p)
		{
			int cx = p.X;
			int cy = p.Y;
			int cz = p.Z;
			byte[] blocks = p.Blocks;
			byte[] light = p.Light;
			byte[] lightSat = p.LightSat;
			byte[] liquid = p.Liquids;
			long chunkIndex3d = MapUtil.Index3dL(cx, cy, cz, (long)this.index3dMulX, (long)this.index3dMulZ);
			ClientChunk chunk = null;
			try
			{
				chunk = ClientChunk.CreateNewCompressed(this.chunkDataPool, blocks, light, lightSat, liquid, p.Moddata, p.Compver);
				chunk.Empty = p.Empty > 0;
				chunk.clientmapchunk = this.GetMapChunk(cx, cz) as ClientMapChunk;
				chunk.LightPositions = new HashSet<int>();
				for (int i = 0; i < p.LightPositionsCount; i++)
				{
					chunk.LightPositions.Add(p.LightPositions[i]);
				}
			}
			catch (Exception e)
			{
				this.game.Logger.Error("Unable to load client chunk at chunk coordinates {0},{1},{2}. Will ignore and replace with empty chunk. Thrown exception: {3}", new object[]
				{
					cx,
					cy,
					cz,
					e.ToString()
				});
				chunk = ClientChunk.CreateNew(this.chunkDataPool);
			}
			chunk.PreLoadBlockEntitiesFromPacket(p.BlockEntities, p.BlockEntitiesCount, this.game);
			if (p.DecorsPos != null && p.DecorsIds != null)
			{
				if (p.DecorsIdsCount < p.DecorsPosCount)
				{
					p.DecorsPosCount = p.DecorsIdsCount;
				}
				chunk.Decors = new Dictionary<int, Block>(p.DecorsPosCount);
				for (int j = 0; j < p.DecorsPosCount; j++)
				{
					chunk.Decors[p.DecorsPos[j]] = this.game.GetBlock(p.DecorsIds[j]);
				}
			}
			this.game.EnqueueMainThreadTask(delegate
			{
				bool exists = false;
				object obj = this.chunksLock;
				lock (obj)
				{
					exists = this.chunks.ContainsKey(chunkIndex3d);
				}
				if (exists)
				{
					this.OverloadChunkMT(p, chunk);
					return;
				}
				this.loadChunkMT(p, chunk);
			}, "loadchunk");
		}

		private void loadChunkMT(Packet_ServerChunk p, ClientChunk chunk)
		{
			int cx = p.X;
			int cy = p.Y;
			int cz = p.Z;
			long chunkIndex3d = MapUtil.Index3dL(cx, cy, cz, (long)this.index3dMulX, (long)this.index3dMulZ);
			object obj = this.chunksLock;
			lock (obj)
			{
				this.chunks[chunkIndex3d] = chunk;
			}
			chunk.InitBlockEntitiesFromPacket(this.game);
			chunk.loadedFromServer = true;
			ClientPlayer player = this.game.player;
			Vec3d vec3d;
			if (player == null)
			{
				vec3d = null;
			}
			else
			{
				EntityPlayer entity = player.Entity;
				if (entity == null)
				{
					vec3d = null;
				}
				else
				{
					EntityPos pos2 = entity.Pos;
					vec3d = ((pos2 != null) ? pos2.XYZ : null);
				}
			}
			Vec3d pos = vec3d;
			bool priority = pos != null && pos.HorizontalSquareDistanceTo((double)(cx * 32), (double)(cz * 32)) < 4096f;
			this.MarkChunkDirty(cx, cy, cz, priority, false, null, false, false);
			if (cy / 1024 == 1)
			{
				this.GetOrCreateDimension(cx, cy, cz).ReceiveClientChunk(chunkIndex3d, chunk, this.World);
			}
			else
			{
				this.SetChunksAroundDirty(cx, cy, cz);
			}
			Vec3i vec = new Vec3i(cx, cy, cz);
			this.game.api.eventapi.TriggerChunkDirty(vec, chunk, EnumChunkDirtyReason.NewlyLoaded);
			ClientEventManager eventManager = this.game.eventManager;
			if (eventManager == null)
			{
				return;
			}
			eventManager.TriggerChunkLoaded(vec);
		}

		public IMiniDimension GetOrCreateDimension(int subDimensionId, Vec3d pos)
		{
			IMiniDimension dim;
			if (!this.dimensions.TryGetValue(subDimensionId, out dim))
			{
				dim = new BlockAccessorMovable((BlockAccessorBase)this.World.BlockAccessor, pos);
				this.dimensions[subDimensionId] = dim;
				dim.SetSubDimensionId(subDimensionId);
			}
			return dim;
		}

		public IMiniDimension GetOrCreateDimension(int cx, int cy, int cz)
		{
			int subDimensionId = BlockAccessorMovable.CalcSubDimensionId(cx, cz);
			return this.GetOrCreateDimension(subDimensionId, new Vec3d((double)(cx * 32 % 16384), (double)(cy % 1024 * 32), (double)(cz * 32 % 16384)));
		}

		private void OverloadChunkMT(Packet_ServerChunk p, ClientChunk newchunk)
		{
			int cx = p.X;
			int cy = p.Y;
			int cz = p.Z;
			long chunkIndex3d = MapUtil.Index3dL(cx, cy, cz, (long)this.index3dMulX, (long)this.index3dMulZ);
			object obj = this.chunksLock;
			ClientChunk prevchunk;
			lock (obj)
			{
				this.chunks.TryGetValue(chunkIndex3d, out prevchunk);
			}
			if (prevchunk == null)
			{
				this.loadChunkMT(p, newchunk);
				return;
			}
			prevchunk.loadedFromServer = false;
			if (this.game.Platform.EllapsedMs - prevchunk.lastTesselationMs < 500L)
			{
				this.game.EnqueueMainThreadTask(delegate
				{
					this.OverloadChunkMT(p, newchunk);
				}, "overloadchunkrequeue");
				return;
			}
			obj = this.chunksLock;
			lock (obj)
			{
				prevchunk.RemoveDataPoolLocations(this.game.chunkRenderer);
				foreach (KeyValuePair<BlockPos, BlockEntity> val in prevchunk.BlockEntities)
				{
					BlockEntity value = val.Value;
					if (value != null)
					{
						value.OnBlockUnloaded();
					}
				}
				this.chunks[chunkIndex3d] = newchunk;
				newchunk.Entities = prevchunk.Entities;
				newchunk.EntitiesCount = prevchunk.EntitiesCount;
				newchunk.InitBlockEntitiesFromPacket(this.game);
				newchunk.loadedFromServer = true;
				newchunk.quantityOverloads++;
			}
			if (!this.game.IsPaused)
			{
				this.game.RegisterCallback(delegate(float dt)
				{
					prevchunk.TryPackAndCommit(8000);
				}, 5000);
			}
			ClientEventManager eventManager = this.game.eventManager;
			if (eventManager != null)
			{
				eventManager.TriggerChunkLoaded(new Vec3i(cx, cy, cz));
			}
			if (cy / 1024 == 1)
			{
				this.GetOrCreateDimension(cx, cy, cz).ReceiveClientChunk(chunkIndex3d, newchunk, this.World);
			}
			this.MarkChunkDirty(cx, cy, cz, false, false, null, true, false);
			this.SetChunksAroundDirty(cx, cy, cz);
		}

		internal void GetNeighbouringChunks(ClientChunk[] neibchunks, int chunkX, int chunkY, int chunkZ)
		{
			object obj = this.chunksLock;
			lock (obj)
			{
				int index = 0;
				for (int dx = -1; dx <= 1; dx++)
				{
					for (int dy = -1; dy <= 1; dy++)
					{
						for (int dz = -1; dz <= 1; dz++)
						{
							long chunkIndex3d = MapUtil.Index3dL(chunkX + dx, chunkY + dy, chunkZ + dz, (long)this.index3dMulX, (long)this.index3dMulZ);
							ClientChunk chunk;
							this.chunks.TryGetValue(chunkIndex3d, out chunk);
							if (chunk == null || chunk.Empty)
							{
								chunk = this.EmptyChunk;
							}
							if (!chunk.ChunkHasData())
							{
								throw new Exception(string.Format("GEC: Chunk {0} {1} {2} has no more block data.", chunkX + dx, chunkY + dy, chunkZ + dz));
							}
							neibchunks[index++] = chunk;
						}
					}
				}
			}
		}

		public void SetChunkDirty(long index3d, bool priority = false, bool relight = false, bool edgeOnly = false)
		{
			ClientChunk chunk = null;
			object obj = this.chunksLock;
			lock (obj)
			{
				this.chunks.TryGetValue(index3d, out chunk);
			}
			if (chunk == null)
			{
				return;
			}
			chunk.shouldSunRelight = chunk.shouldSunRelight || relight;
			if (!relight)
			{
				chunk.FinishLightDoubleBuffering();
			}
			if (priority)
			{
				obj = this.game.dirtyChunksPriorityLock;
				lock (obj)
				{
					if (edgeOnly)
					{
						if (!this.game.dirtyChunksPriority.Contains(index3d))
						{
							this.game.dirtyChunksPriority.Enqueue(index3d | long.MinValue);
						}
					}
					else
					{
						this.game.dirtyChunksPriority.Enqueue(index3d);
					}
					return;
				}
			}
			obj = this.game.dirtyChunksLock;
			lock (obj)
			{
				if (edgeOnly)
				{
					if (!this.game.dirtyChunks.Contains(index3d))
					{
						this.game.dirtyChunks.Enqueue(index3d | long.MinValue);
					}
				}
				else
				{
					this.game.dirtyChunks.Enqueue(index3d);
				}
			}
		}

		public override void MarkChunkDirty(int cx, int cy, int cz, bool priority = false, bool sunRelight = false, Action OnRetesselated = null, bool fireEvent = true, bool edgeOnly = false)
		{
			if (!base.IsValidChunkPos(cx, cy, cz))
			{
				return;
			}
			long index3d = MapUtil.Index3dL(cx, cy, cz, (long)this.index3dMulX, (long)this.index3dMulZ);
			ClientChunk chunk = null;
			object obj = this.chunksLock;
			lock (obj)
			{
				this.chunks.TryGetValue(index3d, out chunk);
			}
			if (chunk == null)
			{
				return;
			}
			int qDrawn = chunk.quantityDrawn;
			if (chunk.enquedForRedraw)
			{
				if (OnRetesselated != null)
				{
					ClientEventManager eventManager = this.game.eventManager;
					if (eventManager != null)
					{
						eventManager.RegisterOnChunkRetesselated(new Vec3i(cx, cy, cz), qDrawn, OnRetesselated);
					}
				}
				if (fireEvent)
				{
					this.game.api.eventapi.TriggerChunkDirty(new Vec3i(cx, cy, cz), chunk, EnumChunkDirtyReason.MarkedDirty);
				}
				return;
			}
			chunk.shouldSunRelight = sunRelight;
			if (fireEvent)
			{
				this.game.api.eventapi.TriggerChunkDirty(new Vec3i(cx, cy, cz), chunk, EnumChunkDirtyReason.MarkedDirty);
			}
			int dist = Math.Max(Math.Abs(cx - this.game.player.Entity.Pos.XInt / 32), Math.Abs(cz - this.game.player.Entity.Pos.ZInt / 32));
			if ((priority && dist <= 2) || cy / 1024 == 1)
			{
				obj = this.game.dirtyChunksPriorityLock;
				lock (obj)
				{
					if (edgeOnly)
					{
						if (!this.game.dirtyChunksPriority.Contains(index3d))
						{
							this.game.dirtyChunksPriority.Enqueue(index3d | long.MinValue);
						}
					}
					else
					{
						this.game.dirtyChunksPriority.Enqueue(index3d);
						chunk.enquedForRedraw = true;
					}
					if (OnRetesselated != null)
					{
						ClientEventManager eventManager2 = this.game.eventManager;
						if (eventManager2 != null)
						{
							eventManager2.RegisterOnChunkRetesselated(new Vec3i(cx, cy, cz), chunk.quantityDrawn, OnRetesselated);
						}
					}
					return;
				}
			}
			obj = this.game.dirtyChunksLock;
			lock (obj)
			{
				if (edgeOnly)
				{
					if (!this.game.dirtyChunks.Contains(index3d))
					{
						this.game.dirtyChunks.Enqueue(index3d | long.MinValue);
					}
				}
				else
				{
					this.game.dirtyChunks.Enqueue(index3d);
					chunk.enquedForRedraw = true;
				}
				if (OnRetesselated != null)
				{
					ClientEventManager eventManager3 = this.game.eventManager;
					if (eventManager3 != null)
					{
						eventManager3.RegisterOnChunkRetesselated(new Vec3i(cx, cy, cz), chunk.quantityDrawn, OnRetesselated);
					}
				}
			}
		}

		public void SetChunksAroundDirty(int cx, int cy, int cz)
		{
			if (base.IsValidChunkPos(cx, cy, cz))
			{
				this.MarkChunkDirty_OnNeighbourChunkLoad(cx, cy, cz);
			}
			if (base.IsValidChunkPos(cx - 1, cy, cz))
			{
				this.MarkChunkDirty_OnNeighbourChunkLoad(cx - 1, cy, cz);
			}
			if (base.IsValidChunkPos(cx + 1, cy, cz))
			{
				this.MarkChunkDirty_OnNeighbourChunkLoad(cx + 1, cy, cz);
			}
			if (BlockAccessorMovable.ChunkCoordsInSameDimension(cy, cy - 1) && base.IsValidChunkPos(cx, cy - 1, cz))
			{
				this.MarkChunkDirty_OnNeighbourChunkLoad(cx, cy - 1, cz);
			}
			if (BlockAccessorMovable.ChunkCoordsInSameDimension(cy, cy + 1) && base.IsValidChunkPos(cx, cy + 1, cz))
			{
				this.MarkChunkDirty_OnNeighbourChunkLoad(cx, cy + 1, cz);
			}
			if (base.IsValidChunkPos(cx, cy, cz - 1))
			{
				this.MarkChunkDirty_OnNeighbourChunkLoad(cx, cy, cz - 1);
			}
			if (base.IsValidChunkPos(cx, cy, cz + 1))
			{
				this.MarkChunkDirty_OnNeighbourChunkLoad(cx, cy, cz + 1);
			}
		}

		private void MarkChunkDirty_OnNeighbourChunkLoad(int cx, int cy, int cz)
		{
			this.MarkChunkDirty(cx, cy, cz, false, false, null, false, true);
		}

		public bool IsValidChunkPosFast(int chunkX, int chunkY, int chunkZ)
		{
			return chunkX >= 0 && chunkY >= 0 && chunkZ >= 0 && chunkX < base.ChunkMapSizeX && chunkY < this.chunkMapSizeY && chunkZ < base.ChunkMapSizeZ;
		}

		public bool IsChunkRendered(int cx, int cy, int cz)
		{
			ClientChunk chunk = null;
			object obj = this.chunksLock;
			lock (obj)
			{
				this.chunks.TryGetValue(MapUtil.Index3dL(cx, cy, cz, (long)this.index3dMulX, (long)this.index3dMulZ), out chunk);
			}
			return chunk != null && chunk.quantityDrawn > 0;
		}

		public int UncheckedGetBlockId(int x, int y, int z)
		{
			ClientChunk chunk = this.GetChunkAtBlockPos(x, y, z);
			if (chunk != null)
			{
				int pos = MapUtil.Index3d(x & 31, y & 31, z & 31, 32, 32);
				chunk.Unpack();
				return chunk.Data[pos];
			}
			return 0;
		}

		IWorldChunk IChunkProvider.GetChunk(int chunkX, int chunkY, int chunkZ)
		{
			ClientChunk chunk = this.GetClientChunk(chunkX, chunkY, chunkZ);
			if (chunk != null)
			{
				chunk.Unpack();
			}
			return chunk;
		}

		IWorldChunk IChunkProvider.GetUnpackedChunkFast(int chunkX, int chunkY, int chunkZ, bool notRecentlyAccessed)
		{
			ClientChunk chunk = null;
			object obj = this.chunksLock;
			lock (obj)
			{
				if (chunkX == this.prevChunkX && chunkY == this.prevChunkY && chunkZ == this.prevChunkZ)
				{
					if (notRecentlyAccessed && this.prevChunk != null)
					{
						this.prevChunk.Unpack();
					}
					return this.prevChunk;
				}
				this.prevChunkX = chunkX;
				this.prevChunkY = chunkY;
				this.prevChunkZ = chunkZ;
				long index3d = MapUtil.Index3dL(chunkX, chunkY, chunkZ, (long)this.index3dMulX, (long)this.index3dMulZ);
				this.chunks.TryGetValue(index3d, out chunk);
				this.prevChunk = chunk;
			}
			if (chunk != null)
			{
				chunk.Unpack();
			}
			return chunk;
		}

		public override IWorldChunk GetChunkNonLocking(int chunkX, int chunkY, int chunkZ)
		{
			long index3d = MapUtil.Index3dL(chunkX, chunkY, chunkZ, (long)this.index3dMulX, (long)this.index3dMulZ);
			ClientChunk chunk;
			this.chunks.TryGetValue(index3d, out chunk);
			return chunk;
		}

		public void OnMapSizeReceived(Vec3i mapSize, Vec3i regionMapSize)
		{
			this.mapsize = new Vec3i(mapSize.X, mapSize.Y, mapSize.Z);
			this.chunks = new Dictionary<long, ClientChunk>();
			this.chunkMapSizeY = mapSize.Y / 32;
			this.index3dMulX = 2097152;
			this.index3dMulZ = 2097152;
			this.regionMapSizeX = regionMapSize.X;
			this.regionMapSizeY = regionMapSize.Y;
			this.regionMapSizeZ = regionMapSize.Z;
		}

		public override IWorldChunk GetChunkAtPos(int posX, int posY, int posZ)
		{
			return this.GetChunkAtBlockPos(posX, posY, posZ);
		}

		public override void SpawnBlockEntity(string classname, BlockPos position, ItemStack byItemStack = null)
		{
			ClientChunk chunk = (ClientChunk)this.GetChunk(position);
			if (chunk == null)
			{
				return;
			}
			Block block = chunk.GetLocalBlockAtBlockPos(this.game, position);
			BlockEntity entity = ClientMain.ClassRegistry.CreateBlockEntity(classname);
			entity.Pos = position.Copy();
			entity.CreateBehaviors(block, this.game);
			entity.Initialize(this.game.api);
			chunk.AddBlockEntity(entity);
			entity.OnBlockPlaced(byItemStack);
			chunk.MarkModified();
			this.MarkBlockEntityDirty(entity.Pos);
		}

		public override void SpawnBlockEntity(BlockEntity be)
		{
			ClientChunk chunk = this.GetChunkAtBlockPos(be.Pos.X, be.Pos.Y, be.Pos.Z);
			if (chunk == null)
			{
				return;
			}
			chunk.AddBlockEntity(be);
			chunk.MarkModified();
			this.MarkBlockEntityDirty(be.Pos);
		}

		public override void RemoveBlockEntity(BlockPos position)
		{
			ClientChunk chunk = this.GetClientChunkAtBlockPos(position);
			if (chunk == null)
			{
				return;
			}
			BlockEntity blockEntity = this.GetBlockEntity(position);
			if (blockEntity != null)
			{
				blockEntity.OnBlockRemoved();
			}
			chunk.RemoveBlockEntity(position);
		}

		public override BlockEntity GetBlockEntity(BlockPos position)
		{
			ClientChunk chunk = this.GetClientChunkAtBlockPos(position);
			if (chunk == null)
			{
				return null;
			}
			return chunk.GetLocalBlockEntityAtBlockPos(position);
		}

		public override void SendSetBlock(int blockId, int posX, int posY, int posZ)
		{
		}

		public override void SendExchangeBlock(int blockId, int posX, int posY, int posZ)
		{
		}

		public override void UpdateLighting(int oldblockid, int newblockid, BlockPos pos)
		{
			object lightingTasksLock = this.LightingTasksLock;
			lock (lightingTasksLock)
			{
				this.LightingTasks.Enqueue(new UpdateLightingTask
				{
					oldBlockId = oldblockid,
					newBlockId = newblockid,
					pos = pos
				});
			}
			ClientEventManager eventManager = this.game.eventManager;
			if (eventManager == null)
			{
				return;
			}
			eventManager.TriggerLightingUpdate(oldblockid, newblockid, pos, null);
		}

		public override void RemoveBlockLight(byte[] oldLightHsV, BlockPos pos)
		{
			object lightingTasksLock = this.LightingTasksLock;
			lock (lightingTasksLock)
			{
				this.LightingTasks.Enqueue(new UpdateLightingTask
				{
					removeLightHsv = oldLightHsV,
					pos = pos
				});
			}
			ClientEventManager eventManager = this.game.eventManager;
			if (eventManager == null)
			{
				return;
			}
			eventManager.TriggerLightingUpdate(0, 0, pos, null);
		}

		public override void UpdateLightingAfterAbsorptionChange(int oldAbsorption, int newAbsorption, BlockPos pos)
		{
			object lightingTasksLock = this.LightingTasksLock;
			lock (lightingTasksLock)
			{
				this.LightingTasks.Enqueue(new UpdateLightingTask
				{
					oldBlockId = 0,
					newBlockId = 0,
					oldAbsorb = (byte)oldAbsorption,
					newAbsorb = (byte)newAbsorption,
					pos = pos,
					absorbUpdate = true
				});
			}
			ClientEventManager eventManager = this.game.eventManager;
			if (eventManager == null)
			{
				return;
			}
			eventManager.TriggerLightingUpdate(0, 0, pos, null);
		}

		public override void UpdateLightingBulk(Dictionary<BlockPos, BlockUpdate> blockUpdates)
		{
			this.game.ShouldTesselateTerrain = false;
			object lightingTasksLock = this.LightingTasksLock;
			lock (lightingTasksLock)
			{
				foreach (KeyValuePair<BlockPos, BlockUpdate> val in blockUpdates)
				{
					int id = ((val.Value.NewFluidBlockId >= 0) ? val.Value.NewFluidBlockId : val.Value.NewSolidBlockId);
					if (id >= 0)
					{
						this.LightingTasks.Enqueue(new UpdateLightingTask
						{
							oldBlockId = val.Value.OldBlockId,
							newBlockId = id,
							pos = val.Key
						});
					}
				}
			}
			this.game.ShouldTesselateTerrain = true;
			ClientEventManager eventManager = this.game.eventManager;
			if (eventManager == null)
			{
				return;
			}
			eventManager.TriggerLightingUpdate(0, 0, null, blockUpdates);
		}

		public override void SendBlockUpdateBulk(IEnumerable<BlockPos> blockUpdates, bool doRelight)
		{
		}

		public override void SendBlockUpdateBulkMinimal(Dictionary<BlockPos, BlockUpdate> blockUpdates)
		{
		}

		public override void MarkBlockDirty(BlockPos pos, IPlayer skipPlayer = null)
		{
			ClientEventManager eventManager = this.game.eventManager;
			if (eventManager != null)
			{
				eventManager.TriggerBlockChanged(this.game, pos, null);
			}
			this.MarkChunkDirty(pos.X / this.ClientChunkSize, pos.InternalY / this.ClientChunkSize, pos.Z / this.ClientChunkSize, true, false, null, true, false);
		}

		public override void MarkBlockModified(BlockPos pos, bool doRelight = true)
		{
			ClientEventManager eventManager = this.game.eventManager;
			if (eventManager != null)
			{
				eventManager.TriggerBlockChanged(this.game, pos, null);
			}
			this.MarkChunkDirty(pos.X / this.ClientChunkSize, pos.InternalY / this.ClientChunkSize, pos.Z / this.ClientChunkSize, true, false, null, true, false);
		}

		public override void MarkBlockDirty(BlockPos pos, Action OnRetesselated)
		{
			ClientEventManager eventManager = this.game.eventManager;
			if (eventManager != null)
			{
				eventManager.TriggerBlockChanged(this.game, pos, null);
			}
			this.MarkChunkDirty(pos.X / this.ClientChunkSize, pos.InternalY / this.ClientChunkSize, pos.Z / this.ClientChunkSize, true, false, OnRetesselated, true, false);
		}

		public override void MarkBlockEntityDirty(BlockPos pos)
		{
		}

		public override void TriggerNeighbourBlockUpdate(BlockPos pos)
		{
		}

		public override IMapRegion GetMapRegion(int regionX, int regionZ)
		{
			ClientMapRegion reg;
			this.MapRegions.TryGetValue(base.MapRegionIndex2D(regionX, regionZ), out reg);
			return reg;
		}

		public override IMapChunk GetMapChunk(int chunkX, int chunkZ)
		{
			long index2d = base.MapChunkIndex2D(chunkX, chunkZ);
			ClientMapChunk mpc;
			this.MapChunks.TryGetValue(index2d, out mpc);
			return mpc;
		}

		public void UnloadMapRegion(int regionX, int regionZ)
		{
			long regionIndex = base.MapRegionIndex2D(regionX, regionZ);
			ClientMapRegion oldregion;
			if (this.MapRegions.TryGetValue(regionIndex, out oldregion))
			{
				this.game.api.eventapi.TriggerMapregionUnloaded(new Vec2i(regionX, regionZ), oldregion);
				this.MapRegions.Remove(regionIndex);
			}
		}

		public override ClimateCondition GetClimateAt(BlockPos pos, EnumGetClimateMode mode = EnumGetClimateMode.WorldGenValues, double totalDays = 0.0)
		{
			int climate = this.GetClimate(pos.X, pos.Z);
			float heightRel = ((float)pos.Y - (float)ClientWorldMap.seaLevel) / ((float)this.MapSizeY - (float)ClientWorldMap.seaLevel);
			float temp = Climate.GetScaledAdjustedTemperatureFloatClient((climate >> 16) & 255, pos.Y - ClientWorldMap.seaLevel);
			float rain = (float)Climate.GetRainFall((climate >> 8) & 255, pos.Y);
			float fertility = (float)Climate.GetFertility((int)rain, temp, heightRel) / 255f;
			rain /= 255f;
			ClimateCondition outclimate = new ClimateCondition
			{
				Temperature = temp,
				Rainfall = rain,
				WorldgenRainfall = rain,
				WorldGenTemperature = temp,
				Fertility = fertility,
				GeologicActivity = (float)(climate & 255) / 255f
			};
			if (mode == EnumGetClimateMode.NowValues)
			{
				totalDays = this.game.Calendar.TotalDays;
			}
			ClientEventManager eventManager = this.game.eventManager;
			if (eventManager != null)
			{
				eventManager.TriggerOnGetClimate(ref outclimate, pos, mode, totalDays);
			}
			return outclimate;
		}

		public override ClimateCondition GetClimateAt(BlockPos pos, ClimateCondition baseClimate, EnumGetClimateMode mode, double totalDays)
		{
			baseClimate.Rainfall = baseClimate.WorldgenRainfall;
			baseClimate.Temperature = baseClimate.WorldGenTemperature;
			ClientEventManager eventManager = this.game.eventManager;
			if (eventManager != null)
			{
				eventManager.TriggerOnGetClimate(ref baseClimate, pos, mode, totalDays);
			}
			return baseClimate;
		}

		public override ClimateCondition GetClimateAt(BlockPos pos, int climate)
		{
			float temp = Climate.GetScaledAdjustedTemperatureFloatClient((climate >> 16) & 255, pos.Y - ClientWorldMap.seaLevel);
			float rain = (float)Climate.GetRainFall((climate >> 8) & 255, pos.Y);
			float heightRel = ((float)pos.Y - (float)ClientWorldMap.seaLevel) / ((float)this.MapSizeY - (float)ClientWorldMap.seaLevel);
			ClimateCondition outclimate = new ClimateCondition
			{
				Temperature = temp,
				Rainfall = rain / 255f,
				Fertility = (float)Climate.GetFertility((int)rain, temp, heightRel) / 255f
			};
			ClientEventManager eventManager = this.game.eventManager;
			if (eventManager != null)
			{
				eventManager.TriggerOnGetClimate(ref outclimate, pos, EnumGetClimateMode.NowValues, this.game.Calendar.TotalDays);
			}
			return outclimate;
		}

		public override Vec3d GetWindSpeedAt(BlockPos pos)
		{
			return this.GetWindSpeedAt(new Vec3d((double)pos.X, (double)pos.Y, (double)pos.Z));
		}

		public override Vec3d GetWindSpeedAt(Vec3d pos)
		{
			Vec3d windspeed = new Vec3d();
			ClientEventManager eventManager = this.game.eventManager;
			if (eventManager != null)
			{
				eventManager.TriggerOnGetWindSpeed(pos, ref windspeed);
			}
			return windspeed;
		}

		public override void DamageBlock(BlockPos pos, BlockFacing facing, float damage, IPlayer dualCallByPlayer = null)
		{
			Block block = this.RelaxedBlockAccess.GetBlock(pos);
			if (block.Id == 0)
			{
				return;
			}
			BlockDamage blockDamage;
			this.game.damagedBlocks.TryGetValue(pos, out blockDamage);
			if (blockDamage == null)
			{
				blockDamage = new BlockDamage
				{
					Position = pos,
					Block = block,
					Facing = facing,
					RemainingResistance = block.GetResistance(this.RelaxedBlockAccess, pos),
					LastBreakEllapsedMs = this.game.ElapsedMilliseconds,
					ByPlayer = this.game.player
				};
				this.game.damagedBlocks[pos.Copy()] = blockDamage;
			}
			blockDamage.RemainingResistance = GameMath.Clamp(blockDamage.RemainingResistance - damage, 0f, blockDamage.RemainingResistance);
			blockDamage.Facing = facing;
			if (blockDamage.Block != block)
			{
				blockDamage.RemainingResistance = block.GetResistance(this.RelaxedBlockAccess, pos);
			}
			blockDamage.Block = block;
			if (blockDamage.RemainingResistance > 0f)
			{
				ClientEventManager eventManager = this.game.eventManager;
				if (eventManager != null)
				{
					eventManager.TriggerBlockBreaking(blockDamage);
				}
			}
			blockDamage.LastBreakEllapsedMs = this.game.ElapsedMilliseconds;
		}

		public override void MarkDecorsDirty(BlockPos pos)
		{
		}

		internal IPlayerRole GetRole(string roleCode)
		{
			PlayerRole role;
			this.RolesByCode.TryGetValue(roleCode, out role);
			return role;
		}

		public void Add(LandClaim claim)
		{
			throw new InvalidOperationException("Not available on the client");
		}

		public bool Remove(LandClaim claim)
		{
			throw new InvalidOperationException("Not available on the client");
		}

		internal void Dispose()
		{
			ICachingBlockAccessor cachingBlockAccessor = this.CachingBlockAccess as ICachingBlockAccessor;
			if (cachingBlockAccessor == null)
			{
				return;
			}
			cachingBlockAccessor.Dispose();
		}

		public override void SendDecorUpdateBulk(IEnumerable<BlockPos> updatedDecorPositions)
		{
			throw new NotImplementedException();
		}

		private ClientMain game;

		private ClientChunk EmptyChunk;

		internal ChunkIlluminator chunkIlluminator;

		internal ClientChunkDataPool chunkDataPool;

		public int ClientChunkSize;

		public int ServerChunkSize;

		public int MapChunkSize;

		public int regionSize;

		public int MaxViewDistance;

		internal ConcurrentDictionary<long, ClientMapRegion> MapRegions = new ConcurrentDictionary<long, ClientMapRegion>();

		internal Dictionary<long, ClientMapChunk> MapChunks = new Dictionary<long, ClientMapChunk>();

		internal object chunksLock = new object();

		internal Dictionary<long, ClientChunk> chunks = new Dictionary<long, ClientChunk>();

		internal Dictionary<int, IMiniDimension> dimensions = new Dictionary<int, IMiniDimension>();

		private Vec3i mapsize = new Vec3i();

		public List<LandClaim> LandClaims = new List<LandClaim>();

		public Dictionary<string, PlayerRole> RolesByCode = new Dictionary<string, PlayerRole>();

		private int prevChunkX = -1;

		private int prevChunkY = -1;

		private int prevChunkZ = -1;

		private IWorldChunk prevChunk;

		private object LerpedClimateMapsLock = new object();

		private LimitedDictionary<long, int[]> LerpedClimateMaps = new LimitedDictionary<long, int[]>(10);

		public IBlockAccessor RelaxedBlockAccess;

		public IBlockAccessor CachingBlockAccess;

		public IBulkBlockAccessor BulkBlockAccess;

		public IBlockAccessor NoRelightBulkBlockAccess;

		public IBlockAccessor BulkMinimalBlockAccess;

		public object LightingTasksLock = new object();

		public Queue<UpdateLightingTask> LightingTasks = new Queue<UpdateLightingTask>();

		private bool lightsGo;

		private bool blockTexturesGo;

		private int regionMapSizeX;

		private int regionMapSizeY;

		private int regionMapSizeZ;

		private int[] placeHolderClimateMap;

		public static int seaLevel = 110;
	}
}
