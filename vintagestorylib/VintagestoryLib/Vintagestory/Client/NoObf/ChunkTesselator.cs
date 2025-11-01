using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Vintagestory.API.Client;
using Vintagestory.API.Client.Tesselation;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf
{
	public class ChunkTesselator : IMeshPoolSupplier
	{
		public ChunkTesselator(ClientMain game)
		{
			this.game = game;
			this.vars = new TCTCache(this);
			int extendedCube = 39304;
			this.currentChunkRgbsExt = new int[extendedCube];
			this.currentChunkBlocksExt = new Block[extendedCube];
			this.currentChunkFluidBlocksExt = new Block[extendedCube];
			this.chunksNearby = new ClientChunk[27];
			this.chunkdatasNearby = new ClientChunkData[27];
			this.blockTesselators[1] = new CubeTesselator(0.125f);
			this.blockTesselators[2] = new CubeTesselator(0.25f);
			this.blockTesselators[3] = new CubeTesselator(0.375f);
			this.blockTesselators[4] = new CubeTesselator(0.5f);
			this.blockTesselators[5] = new CubeTesselator(0.625f);
			this.blockTesselators[6] = new CubeTesselator(0.75f);
			this.blockTesselators[7] = new CubeTesselator(0.875f);
			this.blockTesselators[8] = (this.jsonTesselator = new JsonTesselator());
			this.blockTesselators[10] = new CubeTesselator(1f);
			this.blockTesselators[11] = new CrossTesselator();
			this.blockTesselators[12] = new CubeTesselator(1f);
			this.blockTesselators[13] = new LiquidTesselator(this);
			this.blockTesselators[14] = new TopsoilTesselator();
			this.blockTesselators[15] = new CrossAndSnowlayerTesselator(0.125f);
			this.blockTesselators[18] = new CrossAndSnowlayerTesselator(0.25f);
			this.blockTesselators[19] = new CrossAndSnowlayerTesselator(0.375f);
			this.blockTesselators[20] = new CrossAndSnowlayerTesselator(0.5f);
			this.blockTesselators[21] = new SurfaceLayerTesselator();
			this.blockTesselators[16] = new JsonAndLiquidTesselator(this);
			this.blockTesselators[17] = new JsonAndSnowLayerTesselator();
			ClientSettings.Inst.AddWatcher<bool>("smoothShadows", new OnSettingsChanged<bool>(this.OnSmoothShadowsChanged));
			this.AoAndSmoothShadows = ClientSettings.SmoothShadows;
			this.SetUpDecorRotationMatrices();
		}

		private void OnSmoothShadowsChanged(bool newValue)
		{
			this.AoAndSmoothShadows = ClientSettings.SmoothShadows;
		}

		public void LightlevelsReceived()
		{
			this.lightsGo = true;
			this.Start();
		}

		public void BlockTexturesLoaded()
		{
			this.blockTexturesGo = true;
			this.Start();
		}

		public void Start()
		{
			if (!this.lightsGo || !this.blockTexturesGo)
			{
				return;
			}
			this.lightConverter = new ColorUtil.LightUtil(this.game.WorldMap.BlockLightLevels, this.game.WorldMap.SunLightLevels, this.game.WorldMap.hueLevels, this.game.WorldMap.satLevels);
			this.regionSize = this.game.WorldMap.RegionSize;
			this.seaLevel = ClientWorldMap.seaLevel;
			this.vars.Start(this.game);
			this.blocksFast = (this.game.Blocks as BlockList).BlocksFast;
			for (int i = 0; i < this.blocksFast.Length; i++)
			{
				if (this.blocksFast[i] == null)
				{
					this.game.Logger.Debug("BlockList null at position " + i.ToString());
					this.blocksFast[i] = this.blocksFast[0];
				}
			}
			this.offthreadTesselator = this.game.TesselatorManager.GetNewTesselator();
			TileSideEnum.MoveIndex[0] = -34;
			TileSideEnum.MoveIndex[1] = 1;
			TileSideEnum.MoveIndex[2] = 34;
			TileSideEnum.MoveIndex[3] = -1;
			TileSideEnum.MoveIndex[4] = 1156;
			TileSideEnum.MoveIndex[5] = -1156;
			this.currentChunkDraw32 = new byte[32768];
			this.currentChunkDrawFluids = new byte[32768];
			this.mapsizex = this.game.WorldMap.MapSizeX;
			this.mapsizey = this.game.WorldMap.MapSizeY;
			this.mapsizez = this.game.WorldMap.MapSizeZ;
			this.mapsizeChunksx = this.mapsizex / 32;
			this.mapsizeChunksy = this.mapsizey / 32;
			this.mapsizeChunksz = this.mapsizez / 32;
			Array passes = Enum.GetValues(typeof(EnumChunkRenderPass));
			this.centerModeldataByRenderPassByLodLevel = new MeshData[4][][];
			this.edgeModeldataByRenderPassByLodLevel = new MeshData[4][][];
			for (int j = 0; j < 4; j++)
			{
				this.centerModeldataByRenderPassByLodLevel[j] = new MeshData[passes.Length][];
				this.edgeModeldataByRenderPassByLodLevel[j] = new MeshData[passes.Length][];
			}
			this.ReloadTextures();
			int maxBlockId = this.game.Blocks.Count;
			this.isPartiallyTransparent = new bool[maxBlockId];
			this.isLiquidBlock = new bool[maxBlockId];
			for (int blockId = 0; blockId < maxBlockId; blockId++)
			{
				Block block = this.game.Blocks[blockId];
				this.isPartiallyTransparent[blockId] = !block.AllSidesOpaque;
				this.isLiquidBlock[blockId] = block.MatterState == EnumMatterState.Liquid;
			}
			ClientEventManager em = this.game.eventManager;
			if (em != null)
			{
				em.OnReloadTextures += this.ReloadTextures;
			}
			this.started = true;
		}

		public int[] RuntimeCreateNewBlockTextureAtlas(int textureId)
		{
			this.UpdateForAtlasses(this.TextureIdToReturnNum.Append(textureId));
			return this.TextureIdToReturnNum;
		}

		public void ReloadTextures()
		{
			List<LoadedTexture> atlasTextures = this.game.BlockAtlasManager.AtlasTextures;
			int quantityAtlasses = atlasTextures.Count;
			int[] textureIDs = new int[quantityAtlasses];
			for (int i = 0; i < quantityAtlasses; i++)
			{
				textureIDs[i] = atlasTextures[i].TextureId;
			}
			this.UpdateForAtlasses(textureIDs);
		}

		private void UpdateForAtlasses(int[] textureIDs)
		{
			object reloadLock = this.ReloadLock;
			lock (reloadLock)
			{
				this.quantityAtlasses = textureIDs.Length;
				this.TextureIdToReturnNum = textureIDs;
				this.fastBlockTextureSubidsByBlockAndFace = this.game.FastBlockTextureSubidsByBlockAndFace;
				Array passes = Enum.GetValues(typeof(EnumChunkRenderPass));
				this.ret = new TesselatedChunkPart[passes.Length * this.quantityAtlasses];
				foreach (object obj in passes)
				{
					EnumChunkRenderPass pass = (EnumChunkRenderPass)obj;
					for (int lod = 0; lod < 4; lod++)
					{
						MeshData[][] chunkModeldataByRenderPass = this.centerModeldataByRenderPassByLodLevel[lod];
						MeshData[][] edgeModeldataByRenderPass = this.edgeModeldataByRenderPassByLodLevel[lod];
						chunkModeldataByRenderPass[(int)pass] = new MeshData[this.quantityAtlasses];
						edgeModeldataByRenderPass[(int)pass] = new MeshData[this.quantityAtlasses];
						this.InitialiseRenderPassPools(chunkModeldataByRenderPass[(int)pass], pass, 1024);
						this.InitialiseRenderPassPools(edgeModeldataByRenderPass[(int)pass], pass, 1024);
					}
				}
			}
		}

		private void InitialiseRenderPassPools(MeshData[] renderPassModeldata, EnumChunkRenderPass pass, int startCapacity)
		{
			for (int i = 0; i < this.quantityAtlasses; i++)
			{
				renderPassModeldata[i] = new MeshData(true);
				renderPassModeldata[i].xyz = new float[startCapacity * 3];
				renderPassModeldata[i].Uv = new float[startCapacity * 2];
				renderPassModeldata[i].Rgba = new byte[startCapacity * 4];
				renderPassModeldata[i].Flags = new int[startCapacity];
				renderPassModeldata[i].Indices = new int[startCapacity];
				renderPassModeldata[i].VerticesMax = startCapacity;
				renderPassModeldata[i].IndicesMax = startCapacity;
				if (pass == EnumChunkRenderPass.Liquid)
				{
					renderPassModeldata[i].CustomFloats = new CustomMeshDataPartFloat(startCapacity * 2);
					renderPassModeldata[i].CustomInts = new CustomMeshDataPartInt(startCapacity * 2);
				}
				else
				{
					renderPassModeldata[i].CustomInts = new CustomMeshDataPartInt(startCapacity);
					if (pass == EnumChunkRenderPass.TopSoil)
					{
						renderPassModeldata[i].CustomShorts = new CustomMeshDataPartShort(startCapacity * 2);
					}
				}
			}
		}

		public bool BeginProcessChunk(int chunkX, int chunkY, int chunkZ, ClientChunk chunk, bool skipChunkCenter)
		{
			if (!this.started)
			{
				throw new Exception("not started");
			}
			this.vars.aoAndSmoothShadows = this.AoAndSmoothShadows;
			this.vars.xMin = 32f;
			this.vars.xMax = 0f;
			this.vars.yMin = 32f;
			this.vars.yMax = 0f;
			this.vars.zMin = 32f;
			this.vars.zMax = 0f;
			this.vars.SetDimension(chunkY / 1024);
			try
			{
				this.BuildExtendedChunkData(chunk, chunkX, chunkY, chunkZ, chunkX < 1 || chunkZ < 1 || chunkX >= this.game.WorldMap.ChunkMapSizeX - 1 || chunkZ >= this.game.WorldMap.ChunkMapSizeZ - 1, skipChunkCenter);
			}
			catch (ThreadAbortException)
			{
				throw;
			}
			catch (Exception e)
			{
				if (this.game.Platform.IsShuttingDown)
				{
					return false;
				}
				throw new Exception(string.Format("Exception thrown when trying to tesselate chunk {0}/{1}/{2}. Exception: {3}", new object[] { chunkX, chunkY, chunkZ, e }));
			}
			bool flag = this.CalculateVisibleFaces_Fluids(skipChunkCenter, chunkX * 32, chunkY * 32, chunkZ * 32) | this.CalculateVisibleFaces(skipChunkCenter, chunkX * 32, chunkY * 32, chunkZ * 32);
			if (flag)
			{
				this.currentClimateRegionMap = this.game.WorldMap.LoadOrCreateLerpedClimateMap(chunkX, chunkZ);
				float[] corners = this.game.WorldMap.LoadOceanityCorners(chunkX, chunkZ);
				if (corners != null)
				{
					this.currentOceanityMapTL = corners[0];
					this.currentOceanityMapTR = corners[1];
					this.currentOceanityMapBL = corners[2];
					this.currentOceanityMapBR = corners[3];
				}
			}
			return flag;
		}

		public int NowProcessChunk(int chunkX, int chunkY, int chunkZ, TesselatedChunk tessChunk, bool skipChunkCenter)
		{
			if (chunkX < 0 || chunkY < 0 || chunkZ < 0 || (chunkY < 1024 && (chunkX >= this.mapsizeChunksx || chunkZ >= this.mapsizeChunksz)))
			{
				return 0;
			}
			if (!this.BeginProcessChunk(chunkX, chunkY, chunkZ, tessChunk.chunk, skipChunkCenter))
			{
				if (!skipChunkCenter)
				{
					tessChunk.centerParts = this.emptyParts;
				}
				tessChunk.edgeParts = this.emptyParts;
				return 0;
			}
			this.tmpPos.dimension = chunkY / 1024;
			Dictionary<int, Block> decors = null;
			if (tessChunk.chunk.Decors != null)
			{
				decors = new Dictionary<int, Block>();
				Dictionary<int, Block> decors2 = tessChunk.chunk.Decors;
				lock (decors2)
				{
					this.CullVisibleFacesWithDecor(tessChunk.chunk.Decors, decors);
				}
			}
			object reloadLock = this.ReloadLock;
			int k;
			lock (reloadLock)
			{
				this.vars.textureAtlasPositionsByTextureSubId = this.game.BlockAtlasManager.TextureAtlasPositionsByTextureSubId;
				foreach (EnumChunkRenderPass renderPass in this.passes)
				{
					for (int i = 0; i < this.quantityAtlasses; i++)
					{
						for (int j = 0; j < 4; j++)
						{
							this.edgeModeldataByRenderPassByLodLevel[j][(int)renderPass][i].Clear();
							if (!skipChunkCenter)
							{
								this.centerModeldataByRenderPassByLodLevel[j][(int)renderPass][i].Clear();
							}
						}
					}
				}
				try
				{
					if (skipChunkCenter)
					{
						this.BuildBlockPolygons_EdgeOnly(chunkX, chunkY, chunkZ);
					}
					else
					{
						this.BuildBlockPolygons(chunkX, chunkY, chunkZ);
					}
				}
				catch (Exception ex)
				{
					this.game.Logger.Error(ex);
				}
				if (decors != null)
				{
					this.vars.blockEntitiesOfChunk = null;
					this.BuildDecorPolygons(chunkX, chunkY, chunkZ, decors, skipChunkCenter);
				}
				int verticesCount = 0;
				if (!skipChunkCenter)
				{
					verticesCount += this.populateTesselatedChunkPart(this.centerModeldataByRenderPassByLodLevel, out tessChunk.centerParts);
				}
				verticesCount += this.populateTesselatedChunkPart(this.edgeModeldataByRenderPassByLodLevel, out tessChunk.edgeParts);
				tessChunk.SetBounds(this.vars.xMin, this.vars.xMax, this.vars.yMin, this.vars.yMax, this.vars.zMin, this.vars.zMax);
				k = verticesCount;
			}
			return k;
		}

		private int populateTesselatedChunkPart(MeshData[][][] modeldataByRenderPassByLodLevel, out TesselatedChunkPart[] tessChunkParts)
		{
			int retCount = 0;
			int verticesCount = 0;
			MeshData.Recycler.DoRecycling();
			foreach (EnumChunkRenderPass renderpass in this.passes)
			{
				for (int i = 0; i < this.quantityAtlasses; i++)
				{
					MeshData chunkModeldataLod0 = modeldataByRenderPassByLodLevel[0][(int)renderpass][i];
					MeshData chunkModeldataLod = modeldataByRenderPassByLodLevel[1][(int)renderpass][i];
					MeshData chunkModeldataLod2Near = modeldataByRenderPassByLodLevel[2][(int)renderpass][i];
					MeshData chunkModeldataLod2Far = modeldataByRenderPassByLodLevel[3][(int)renderpass][i];
					int count0 = chunkModeldataLod0.VerticesCount;
					int count = chunkModeldataLod.VerticesCount;
					int count2 = chunkModeldataLod2Near.VerticesCount;
					int count3 = chunkModeldataLod2Far.VerticesCount;
					if (count0 + count + count2 + count3 > 0)
					{
						this.ret[retCount++] = new TesselatedChunkPart
						{
							atlasNumber = i,
							modelDataLod0 = ((count0 == 0) ? null : chunkModeldataLod0.CloneUsingRecycler()),
							modelDataLod1 = ((count == 0) ? null : chunkModeldataLod.CloneUsingRecycler()),
							modelDataNotLod2Far = ((count2 == 0) ? null : chunkModeldataLod2Near.CloneUsingRecycler()),
							modelDataLod2Far = ((count3 == 0) ? null : chunkModeldataLod2Far.CloneUsingRecycler()),
							pass = renderpass
						};
						verticesCount += count0 + count;
					}
				}
			}
			if (retCount > 0)
			{
				Array array2 = this.ret;
				TesselatedChunkPart[] array3;
				tessChunkParts = (array3 = new TesselatedChunkPart[retCount]);
				Array.Copy(array2, array3, retCount);
				for (int j = 0; j < retCount; j++)
				{
					this.ret[j] = null;
				}
			}
			else
			{
				tessChunkParts = this.emptyParts;
			}
			return verticesCount;
		}

		public bool CalculateVisibleFaces(bool skipChunkCenter, int baseX, int baseY, int baseZ)
		{
			byte[] currentChunkDraw32 = this.currentChunkDraw32;
			int extIndex3d = 0;
			Block blockAir = this.blocksFast[0];
			for (int y = 0; y < 32; y++)
			{
				int index3d = y * 32 * 32;
				for (int z = 0; z < 32; z++)
				{
					int extIndex3dBase = (y * 34 + z) * 34 + 1191;
					int zeroIfYZEdge = y * (y ^ 31) * z * (z ^ 31);
					for (int x = 0; x < 32; x++)
					{
						Block curBlock;
						if ((curBlock = this.currentChunkBlocksExt[extIndex3dBase + x]) == blockAir)
						{
							currentChunkDraw32[index3d + x] = 0;
						}
						else if (!skipChunkCenter || x * (x ^ 31) * zeroIfYZEdge == 0)
						{
							extIndex3d = extIndex3dBase + x;
							int faceVisibilityFlags = 0;
							EnumFaceCullMode cullMode = curBlock.FaceCullMode;
							SmallBoolArray curBlock_SideOpaque = curBlock.SideOpaque;
							int tileSide = 5;
							do
							{
								faceVisibilityFlags <<= 1;
								Block nBlock = this.currentChunkBlocksExt[extIndex3d + TileSideEnum.MoveIndex[tileSide]];
								int tileSideOpposite = TileSideEnum.GetOpposite(tileSide);
								bool neighbourOpaque = nBlock.SideOpaque[tileSideOpposite];
								if (tileSide == 4 && (nBlock.DrawType == EnumDrawType.JSONAndSnowLayer && neighbourOpaque) && !curBlock.AllowSnowCoverage(this.game, this.tmpPos.Set(baseX + x, baseY + y, baseZ + z)))
								{
									neighbourOpaque = false;
								}
								switch (cullMode)
								{
								case EnumFaceCullMode.Default:
									if (!neighbourOpaque || (!curBlock_SideOpaque[tileSide] && curBlock.DrawType != EnumDrawType.JSON && curBlock.DrawType != EnumDrawType.JSONAndSnowLayer))
									{
										faceVisibilityFlags++;
									}
									break;
								case EnumFaceCullMode.NeverCull:
									faceVisibilityFlags++;
									break;
								case EnumFaceCullMode.Merge:
									if (nBlock != curBlock && (!curBlock_SideOpaque[tileSide] || !neighbourOpaque))
									{
										faceVisibilityFlags++;
									}
									break;
								case EnumFaceCullMode.Collapse:
									if ((nBlock == curBlock && (tileSide == 4 || tileSide == 0 || tileSide == 3)) || (nBlock != curBlock && (!curBlock_SideOpaque[tileSide] || !neighbourOpaque)))
									{
										faceVisibilityFlags++;
									}
									break;
								case EnumFaceCullMode.MergeMaterial:
									if (!curBlock.SideSolid[tileSide] || (nBlock.BlockMaterial != curBlock.BlockMaterial && (!curBlock_SideOpaque[tileSide] || !neighbourOpaque)) || !nBlock.SideSolid[tileSideOpposite])
									{
										faceVisibilityFlags++;
									}
									break;
								case EnumFaceCullMode.CollapseMaterial:
									if (nBlock.BlockMaterial == curBlock.BlockMaterial)
									{
										if (tileSide == 0 || tileSide == 3)
										{
											faceVisibilityFlags++;
										}
									}
									else if (!neighbourOpaque || (tileSide < 4 && !curBlock_SideOpaque[tileSide]))
									{
										faceVisibilityFlags++;
									}
									break;
								case EnumFaceCullMode.Liquid:
									if (nBlock.BlockMaterial != curBlock.BlockMaterial)
									{
										if (tileSide == 4)
										{
											faceVisibilityFlags++;
										}
										else
										{
											FastVec3i offset = TileSideEnum.OffsetByTileSide[tileSide];
											if (!nBlock.SideIsSolid(this.tmpPos.Set(baseX + x + offset.X, baseY + y + offset.Y, baseZ + z + offset.Z), tileSideOpposite))
											{
												faceVisibilityFlags++;
											}
										}
									}
									break;
								case EnumFaceCullMode.Callback:
									if (!curBlock.ShouldMergeFace(tileSide, nBlock, index3d + x))
									{
										faceVisibilityFlags++;
									}
									break;
								case EnumFaceCullMode.MergeSnowLayer:
								{
									int indexBelowNeighbour = extIndex3d + TileSideEnum.MoveIndex[tileSide] - 1156;
									if (tileSide == 4 || (!neighbourOpaque && (tileSide == 5 || nBlock.GetSnowLevel(null) < curBlock.GetSnowLevel(null))) || (nBlock.DrawType == EnumDrawType.JSONAndSnowLayer && indexBelowNeighbour >= 0 && indexBelowNeighbour < this.currentChunkBlocksExt.Length && !this.currentChunkBlocksExt[indexBelowNeighbour].AllowSnowCoverage(this.game, this.tmpPos.Set(baseX + x, baseY + y, baseZ + z))))
									{
										faceVisibilityFlags++;
									}
									break;
								}
								case EnumFaceCullMode.FlushExceptTop:
									if (tileSide == 4 || ((tileSide == 5 || nBlock != curBlock) && !neighbourOpaque))
									{
										faceVisibilityFlags++;
									}
									break;
								case EnumFaceCullMode.Stairs:
									if ((!neighbourOpaque && (nBlock != curBlock || curBlock.SideOpaque[tileSide])) || tileSide == 4)
									{
										faceVisibilityFlags++;
									}
									break;
								}
							}
							while (tileSide-- != 0);
							if (curBlock.DrawType == EnumDrawType.JSONAndWater)
							{
								faceVisibilityFlags |= 64;
							}
							currentChunkDraw32[index3d + x] = (byte)faceVisibilityFlags;
						}
					}
					index3d += 32;
				}
			}
			return extIndex3d > 0;
		}

		public bool CalculateVisibleFaces_Fluids(bool skipChunkCenter, int baseX, int baseY, int baseZ)
		{
			byte[] currentChunkDraw32 = this.currentChunkDrawFluids;
			int extIndex3d = 0;
			Block blockAir = this.blocksFast[0];
			for (int y = 0; y < 32; y++)
			{
				int index3d = y * 32 * 32;
				for (int z = 0; z < 32; z++)
				{
					int extIndex3dBase = (y * 34 + z) * 34 + 1191;
					int zeroIfYZEdge = y * (y ^ 31) * z * (z ^ 31);
					for (int x = 0; x < 32; x++)
					{
						Block curBlock;
						if ((curBlock = this.currentChunkFluidBlocksExt[extIndex3dBase + x]) == blockAir)
						{
							currentChunkDraw32[index3d + x] = 0;
						}
						else if (!skipChunkCenter || x * (x ^ 31) * zeroIfYZEdge == 0)
						{
							extIndex3d = extIndex3dBase + x;
							int faceVisibilityFlags = 0;
							int faceCullMode = (int)curBlock.FaceCullMode;
							int tileSide = 5;
							if (faceCullMode == 6)
							{
								do
								{
									faceVisibilityFlags <<= 1;
									if (this.currentChunkFluidBlocksExt[extIndex3d + TileSideEnum.MoveIndex[tileSide]].BlockMaterial != curBlock.BlockMaterial)
									{
										if (tileSide == 4)
										{
											faceVisibilityFlags++;
										}
										else
										{
											Block block = this.currentChunkBlocksExt[extIndex3d + TileSideEnum.MoveIndex[tileSide]];
											FastVec3i offset = TileSideEnum.OffsetByTileSide[tileSide];
											if (!block.SideIsSolid(this.tmpPos.Set(baseX + x + offset.X, baseY + y + offset.Y, baseZ + z + offset.Z), TileSideEnum.GetOpposite(tileSide)))
											{
												faceVisibilityFlags++;
											}
										}
									}
								}
								while (tileSide-- != 0);
							}
							else
							{
								do
								{
									faceVisibilityFlags <<= 1;
									Block nLiquid = this.currentChunkFluidBlocksExt[extIndex3d + TileSideEnum.MoveIndex[tileSide]];
									if (!curBlock.ShouldMergeFace(tileSide, nLiquid, index3d + x))
									{
										faceVisibilityFlags++;
									}
								}
								while (tileSide-- != 0);
							}
							currentChunkDraw32[index3d + x] = (byte)faceVisibilityFlags;
						}
					}
					index3d += 32;
				}
			}
			return extIndex3d > 0;
		}

		public void BuildBlockPolygons(int chunkX, int chunkY, int chunkZ)
		{
			int baseX = chunkX * 32;
			int baseY = chunkY * 32 % 32768;
			int baseZ = chunkZ * 32;
			if (baseY == 0 && chunkY / 1024 != 1)
			{
				int layerSize = 1024;
				for (int i = 0; i < layerSize; i++)
				{
					byte[] array = this.currentChunkDraw32;
					int num = i;
					array[num] &= 223;
					byte[] array2 = this.currentChunkDrawFluids;
					int num2 = i;
					array2[num2] &= 223;
				}
			}
			TCTCache vars = this.vars;
			this.currentModeldataByRenderPassByLodLevel = this.edgeModeldataByRenderPassByLodLevel;
			int index3d = -1;
			for (int lY = 0; lY < 32; lY++)
			{
				int extLzBase = (lY + 1) * 34 + 1;
				vars.posY = baseY + lY;
				vars.finalY = (float)lY;
				vars.ly = lY;
				int zeroIfYEdge = lY * (lY ^ 31);
				int lZ = 0;
				do
				{
					vars.lz = lZ;
					int posZ = (vars.posZ = baseZ + lZ);
					MeshData[][][] centerXModeldataByRenderPassByLodLevel;
					if (zeroIfYEdge * lZ * (lZ ^ 31) == 0)
					{
						centerXModeldataByRenderPassByLodLevel = this.edgeModeldataByRenderPassByLodLevel;
					}
					else
					{
						centerXModeldataByRenderPassByLodLevel = this.centerModeldataByRenderPassByLodLevel;
					}
					int extIndex3dBase = (extLzBase + lZ) * 34 + 1;
					this.TesselateBlock(++index3d, extIndex3dBase, 0, baseX, posZ);
					this.currentModeldataByRenderPassByLodLevel = centerXModeldataByRenderPassByLodLevel;
					int lX = 1;
					do
					{
						this.TesselateBlock(++index3d, extIndex3dBase, lX, baseX, posZ);
					}
					while (++lX < 31);
					this.currentModeldataByRenderPassByLodLevel = this.edgeModeldataByRenderPassByLodLevel;
					this.TesselateBlock(++index3d, extIndex3dBase, lX, baseX, posZ);
				}
				while (++lZ < 32);
			}
		}

		public void BuildBlockPolygons_EdgeOnly(int chunkX, int chunkY, int chunkZ)
		{
			int baseX = chunkX * 32;
			int baseY = chunkY * 32 % 32768;
			int baseZ = chunkZ * 32;
			if (baseY == 0)
			{
				int layerSize = 1024;
				for (int i = 0; i < layerSize; i++)
				{
					byte[] array = this.currentChunkDraw32;
					int num = i;
					array[num] &= 223;
				}
			}
			this.currentModeldataByRenderPassByLodLevel = this.edgeModeldataByRenderPassByLodLevel;
			TCTCache vars = this.vars;
			int index3d = -1;
			for (int lY = 0; lY < 32; lY++)
			{
				int extLzBase = (lY + 1) * 34 + 1;
				vars.posY = baseY + lY;
				vars.finalY = (float)lY;
				vars.ly = lY;
				int zeroIfYEdge = lY * (lY ^ 31);
				int lZ = 0;
				do
				{
					vars.lz = lZ;
					int posZ = (vars.posZ = baseZ + lZ);
					int extIndex3dBase = (extLzBase + lZ) * 34 + 1;
					if (zeroIfYEdge * lZ * (lZ ^ 31) == 0)
					{
						int lX = 0;
						do
						{
							this.TesselateBlock(++index3d, extIndex3dBase, lX, baseX, posZ);
						}
						while (++lX < 32);
					}
					else
					{
						this.TesselateBlock(++index3d, extIndex3dBase, 0, baseX, posZ);
						index3d += 31;
						this.TesselateBlock(index3d, extIndex3dBase, 31, baseX, posZ);
					}
				}
				while (++lZ < 32);
			}
		}

		private void CullVisibleFacesWithDecor(Dictionary<int, Block> decors, Dictionary<int, Block> drawnDecors)
		{
			foreach (KeyValuePair<int, Block> val in decors)
			{
				Block block = val.Value;
				if (block != null)
				{
					int decorFlags = (int)(block.IsMissing ? 1 : block.decorBehaviorFlags);
					if ((decorFlags & 1) != 0)
					{
						int indexAndFace = val.Key;
						int index3d = DecorBits.Index3dFromIndex(indexAndFace);
						BlockFacing face = DecorBits.FacingFromIndex(indexAndFace);
						if ((this.currentChunkDraw32[index3d] & face.Flag) != 0 || (decorFlags & 2) != 0)
						{
							drawnDecors[indexAndFace] = block;
						}
					}
				}
			}
		}

		private void BuildDecorPolygons(int chunkX, int chunkY, int chunkZ, Dictionary<int, Block> decors, bool edgeonly)
		{
			int chunkSizeMask = 31;
			int baseX = chunkX * 32;
			int baseY = chunkY * 32 % 32768;
			int baseZ = chunkZ * 32;
			TCTCache vars = this.vars;
			foreach (KeyValuePair<int, Block> val in decors)
			{
				int packedIndex = val.Key;
				Block block = val.Value;
				BlockFacing face = DecorBits.FacingFromIndex(packedIndex);
				int index3d = DecorBits.Index3dFromIndex(packedIndex);
				int lX = index3d % 32;
				int lY = index3d / 32 / 32;
				int lZ = index3d / 32 % 32;
				if (lX * (lX ^ chunkSizeMask) * lY * (lY ^ chunkSizeMask) * lZ * (lZ ^ chunkSizeMask) == 0)
				{
					this.currentModeldataByRenderPassByLodLevel = this.edgeModeldataByRenderPassByLodLevel;
				}
				else
				{
					if (edgeonly)
					{
						continue;
					}
					this.currentModeldataByRenderPassByLodLevel = this.centerModeldataByRenderPassByLodLevel;
				}
				vars.extIndex3d = ((lY + 1) * 34 + lZ + 1) * 34 + lX + 1;
				vars.index3d = index3d;
				Vec3i delta = face.Normali;
				lY += delta.Y;
				lZ += delta.Z;
				lX += delta.X;
				vars.posX = baseX + lX;
				vars.posY = baseY + lY;
				vars.posZ = baseZ + lZ;
				vars.finalY = (float)lY;
				IDrawYAdjustable idya = block as IDrawYAdjustable;
				if (idya != null)
				{
					vars.finalY += idya.AdjustYPosition(new BlockPos(vars.posX, vars.posY, vars.posZ), this.currentChunkBlocksExt, vars.extIndex3d);
				}
				vars.ly = lY;
				vars.lz = lZ;
				int facesToDrawFlag = (int)(63 - face.Opposite.Flag);
				vars.decorSubPosition = DecorBits.SubpositionFromIndex(packedIndex);
				vars.decorRotationData = DecorBits.RotationFromIndex(packedIndex);
				int drawType = (int)(block.IsMissing ? EnumDrawType.SurfaceLayer : block.DrawType);
				if (drawType == 8)
				{
					int i = face.Index;
					int rot = vars.decorRotationData % 4;
					if ((block.decorBehaviorFlags & 32) != 0)
					{
						if (rot > 0)
						{
							switch (face.Index)
							{
							case 0:
								i = (rot * 2 + 1) % 6;
								break;
							case 1:
								if (rot == 2)
								{
									rot = 0;
									i = 5;
								}
								else
								{
									rot--;
									i = rot;
								}
								break;
							case 2:
								rot = 4 - rot;
								i = (rot * 2 + 1) % 6;
								break;
							case 3:
								rot = 4 - rot;
								if (rot == 2)
								{
									rot = 0;
									i = 5;
								}
								else
								{
									rot--;
									i = rot;
								}
								break;
							case 5:
								i = 4;
								break;
							}
						}
						else
						{
							i = 4;
						}
					}
					vars.preRotationMatrix = this.decorRotationMatrices[i + rot * 6];
				}
				else
				{
					vars.preRotationMatrix = null;
				}
				if ((block.decorBehaviorFlags & 4) != 0 && ((lZ & 1) ^ (lX & 1)) == 1)
				{
					byte zOffsetSave = block.VertexFlags.ZOffset;
					block.VertexFlags.ZOffset = zOffsetSave + 2;
					this.TesselateBlock(block, lX, facesToDrawFlag, baseX + lX, baseZ + lZ, drawType);
					block.VertexFlags.ZOffset = zOffsetSave;
				}
				else
				{
					this.TesselateBlock(block, lX, facesToDrawFlag, baseX + lX, baseZ + lZ, drawType);
				}
				vars.decorSubPosition = 0;
				vars.decorRotationData = 0;
				vars.preRotationMatrix = null;
			}
		}

		private void SetUpDecorRotationMatrices()
		{
			for (int rot = 0; rot < 4; rot++)
			{
				float[] matrix = Mat4f.Create();
				Mat4f.Translate(matrix, matrix, 0f, 0.5f, 0.5f);
				Mat4f.RotateX(matrix, matrix, 4.712389f);
				Mat4f.Translate(matrix, matrix, 0f, -0.5f, -0.5f);
				this.SetDecorRotationMatrix(matrix, rot, 0);
				for (int i = 1; i < 4; i++)
				{
					matrix = Mat4f.Create();
					Mat4f.Translate(matrix, matrix, 0.5f, 0.5f, 0.5f);
					Mat4f.RotateY(matrix, matrix, 1.5707964f * (float)(4 - i));
					Mat4f.RotateX(matrix, matrix, 4.712389f);
					Mat4f.Translate(matrix, matrix, -0.5f, -0.5f, -0.5f);
					this.SetDecorRotationMatrix(matrix, rot, i);
				}
				this.SetDecorRotationMatrix((rot == 0) ? null : Mat4f.Create(), rot, 4);
				matrix = Mat4f.Create();
				Mat4f.Translate(matrix, matrix, 0f, 0.5f, 0.5f);
				Mat4f.RotateX(matrix, matrix, 3.1415927f);
				Mat4f.Translate(matrix, matrix, 0f, -0.5f, -0.5f);
				this.SetDecorRotationMatrix(matrix, rot, 5);
			}
		}

		private void SetDecorRotationMatrix(float[] matrix, int rot, int i)
		{
			if (rot > 0)
			{
				Mat4f.Translate(matrix, matrix, 0.5f, 0f, 0.5f);
				Mat4f.RotateY(matrix, matrix, (float)(4 - rot) * 1.5707964f);
				Mat4f.Translate(matrix, matrix, -0.5f, 0f, -0.5f);
			}
			this.decorRotationMatrices[rot * 6 + i] = matrix;
		}

		private void TesselateBlock(int index3d, int extIndex3dBase, int lX, int baseX, int posZ)
		{
			int flags;
			if ((flags = (int)this.currentChunkDraw32[index3d]) != 0)
			{
				this.vars.index3d = index3d;
				Block block = this.currentChunkBlocksExt[this.vars.extIndex3d = extIndex3dBase + lX];
				this.TesselateBlock(block, lX, flags, baseX + lX, posZ, (int)block.DrawType);
			}
			if ((flags = (int)this.currentChunkDrawFluids[index3d]) != 0)
			{
				this.vars.index3d = index3d;
				Block block2 = this.currentChunkFluidBlocksExt[this.vars.extIndex3d = extIndex3dBase + lX];
				this.TesselateBlock(block2, lX, flags, baseX + lX, posZ, (int)block2.DrawType);
			}
		}

		private void TesselateBlock(Block block, int lX, int faceflags, int posX, int posZ, int drawType)
		{
			if (block.DrawType == EnumDrawType.Empty)
			{
				return;
			}
			this.vars.block = block;
			this.vars.drawFaceFlags = faceflags;
			this.vars.posX = posX;
			this.vars.lx = lX;
			this.vars.finalX = (float)lX;
			this.vars.finalY = (float)this.vars.ly;
			IDrawYAdjustable idya = block as IDrawYAdjustable;
			if (idya != null)
			{
				this.vars.finalY += idya.AdjustYPosition(new BlockPos(this.vars.posX, this.vars.posY, this.vars.posZ), this.currentChunkBlocksExt, this.vars.extIndex3d);
			}
			this.vars.finalZ = (float)this.vars.lz;
			int id = (this.vars.blockId = block.BlockId);
			this.vars.textureSubId = 0;
			this.vars.VertexFlags = block.VertexFlags.All;
			this.vars.RenderPass = block.RenderPass;
			this.vars.fastBlockTextureSubidsByFace = this.fastBlockTextureSubidsByBlockAndFace[id];
			if (block.RandomDrawOffset != 0)
			{
				this.vars.finalX += (float)(GameMath.oaatHash(posX, 0, posZ) % 12) / (24f + 12f * (float)block.RandomDrawOffset);
				this.vars.finalZ += (float)(GameMath.oaatHash(posX, 1, posZ) % 12) / (24f + 12f * (float)block.RandomDrawOffset);
			}
			if (block.ShapeUsesColormap || block.LoadColorMapAnyway || block.Frostable)
			{
				int x = posX + GameMath.MurmurHash3Mod(posX, 0, posZ, 5) - 2;
				int z = posZ + GameMath.MurmurHash3Mod(posX, 1, posZ, 5) - 2;
				int regionx = posX / this.regionSize;
				int regionz = posZ / this.regionSize;
				int climate = this.currentClimateRegionMap[GameMath.Clamp(z - regionz * this.regionSize, 0, this.regionSize - 1) * this.regionSize + GameMath.Clamp(x - regionx * this.regionSize, 0, this.regionSize - 1)];
				TCTCache tctcache = this.vars;
				ColorMap seasonColorMapResolved = block.SeasonColorMapResolved;
				int num = ((seasonColorMapResolved != null) ? (seasonColorMapResolved.RectIndex + 1) : 0);
				ColorMap climateColorMapResolved = block.ClimateColorMapResolved;
				tctcache.ColorMapData = new ColorMapData(num, (climateColorMapResolved != null) ? (climateColorMapResolved.RectIndex + 1) : 0, Climate.GetAdjustedTemperature((climate >> 16) & 255, this.vars.posY - this.seaLevel), Climate.GetRainFall((climate >> 8) & 255, this.vars.posY), block.Frostable);
			}
			else
			{
				this.vars.ColorMapData = this.defaultColorMapData;
			}
			if (block.DrawType == EnumDrawType.Liquid)
			{
				if (this.vars.posY == this.seaLevel - 1)
				{
					Block neighborNorth = this.currentChunkFluidBlocksExt[this.vars.extIndex3d + TileSideEnum.MoveIndex[0]];
					Block neighborSouth = this.currentChunkFluidBlocksExt[this.vars.extIndex3d + TileSideEnum.MoveIndex[2]];
					Block neighborWest = this.currentChunkFluidBlocksExt[this.vars.extIndex3d + TileSideEnum.MoveIndex[3]];
					Block neighborEast = this.currentChunkFluidBlocksExt[this.vars.extIndex3d + TileSideEnum.MoveIndex[1]];
					Block neighborNorthWest = this.currentChunkFluidBlocksExt[this.vars.extIndex3d + TileSideEnum.MoveIndex[0] + TileSideEnum.MoveIndex[3]];
					Block neighborSouthWest = this.currentChunkFluidBlocksExt[this.vars.extIndex3d + TileSideEnum.MoveIndex[2] + TileSideEnum.MoveIndex[3]];
					Block neighborNorthEast = this.currentChunkFluidBlocksExt[this.vars.extIndex3d + TileSideEnum.MoveIndex[0] + TileSideEnum.MoveIndex[1]];
					Block neighborSouthEast = this.currentChunkFluidBlocksExt[this.vars.extIndex3d + TileSideEnum.MoveIndex[2] + TileSideEnum.MoveIndex[1]];
					if (neighborNorthWest.Id == 0 && this.vars.lx == 0 && this.vars.lz == 0 && this.currentChunkBlocksExt[this.vars.extIndex3d + TileSideEnum.MoveIndex[0] + TileSideEnum.MoveIndex[3]].Id == 0)
					{
						neighborNorthWest = block;
					}
					if (neighborSouthWest.Id == 0 && this.vars.lx == 0 && this.vars.lz == 31 && this.currentChunkBlocksExt[this.vars.extIndex3d + TileSideEnum.MoveIndex[2] + TileSideEnum.MoveIndex[3]].Id == 0)
					{
						neighborSouthWest = block;
					}
					if (neighborNorthEast.Id == 0 && this.vars.lx == 31 && this.vars.lz == 0 && this.currentChunkBlocksExt[this.vars.extIndex3d + TileSideEnum.MoveIndex[0] + TileSideEnum.MoveIndex[1]].Id == 0)
					{
						neighborNorthEast = block;
					}
					if (neighborSouthEast.Id == 0 && this.vars.lx == 31 && this.vars.lz == 31 && this.currentChunkBlocksExt[this.vars.extIndex3d + TileSideEnum.MoveIndex[2] + TileSideEnum.MoveIndex[1]].Id == 0)
					{
						neighborSouthEast = block;
					}
					this.vars.OceanityFlagTL = ((neighborNorth == block && neighborNorthWest == block && neighborWest == block) ? ((int)((byte)GameMath.BiLerp(this.currentOceanityMapTL, this.currentOceanityMapTR, this.currentOceanityMapBL, this.currentOceanityMapBR, (float)this.vars.lx / 32f, (float)this.vars.lz / 32f)) << 2) : 0);
					this.vars.OceanityFlagTR = ((neighborNorth == block && neighborNorthEast == block && neighborEast == block) ? ((int)((byte)GameMath.BiLerp(this.currentOceanityMapTL, this.currentOceanityMapTR, this.currentOceanityMapBL, this.currentOceanityMapBR, (float)(this.vars.lx + 1) / 32f, (float)this.vars.lz / 32f)) << 2) : 0);
					this.vars.OceanityFlagBL = ((neighborSouth == block && neighborSouthWest == block && neighborWest == block) ? ((int)((byte)GameMath.BiLerp(this.currentOceanityMapTL, this.currentOceanityMapTR, this.currentOceanityMapBL, this.currentOceanityMapBR, (float)this.vars.lx / 32f, (float)(this.vars.lz + 1) / 32f)) << 2) : 0);
					this.vars.OceanityFlagBR = ((neighborSouth == block && neighborSouthEast == block && neighborEast == block) ? ((int)((byte)GameMath.BiLerp(this.currentOceanityMapTL, this.currentOceanityMapTR, this.currentOceanityMapBL, this.currentOceanityMapBR, (float)(this.vars.lx + 1) / 32f, (float)(this.vars.lz + 1) / 32f)) << 2) : 0);
				}
				else
				{
					this.vars.OceanityFlagTL = 0;
					this.vars.OceanityFlagTR = 0;
					this.vars.OceanityFlagBL = 0;
					this.vars.OceanityFlagBR = 0;
				}
			}
			this.vars.textureVOffset = ((block.alternatingVOffset && (((block.alternatingVOffsetFaces & 10) > 0 && posX % 2 == 1) || ((block.alternatingVOffsetFaces & 48) > 0 && this.vars.posY % 2 == 1) || ((block.alternatingVOffsetFaces & 5) > 0 && posZ % 2 == 1))) ? 1f : 0f);
			this.blockTesselators[drawType].Tesselate(this.vars);
		}

		private void BuildExtendedChunkData(ClientChunk curChunk, int chunkX, int chunkY, int chunkZ, bool atMapEdge, bool skipChunkCenter)
		{
			int extendedChunkSize = 34;
			int validBlocks = this.game.Blocks.Count;
			this.game.WorldMap.GetNeighbouringChunks(this.chunksNearby, chunkX, chunkY, chunkZ);
			for (int i = 26; i >= 0; i--)
			{
				this.chunksNearby[i].Unpack();
				this.chunkdatasNearby[i] = (ClientChunkData)this.chunksNearby[i].Data;
				BlockChunkDataLayer blocksLayer = this.chunkdatasNearby[i].blocksLayer;
				if (blocksLayer != null)
				{
					blocksLayer.ClearPaletteOutsideMaxValue(validBlocks);
				}
			}
			this.chunkdatasNearby[13].BuildFastBlockAccessArray(this.blocksFast);
			int maxEdge = extendedChunkSize - 1;
			ClientChunkData chunkdata = (ClientChunkData)curChunk.Data;
			int index3d = 0;
			int constOffset = 1190;
			int extIndex3d;
			if (skipChunkCenter)
			{
				for (int y = 0; y < 32; y++)
				{
					for (int z = 0; z < 32; z++)
					{
						extIndex3d = (y * 34 + z) * 34 + constOffset;
						if ((y + 2) % 32 <= 3 || (z + 2) % 32 <= 3)
						{
							chunkdata.GetRange_Faster(this.currentChunkBlocksExt, this.currentChunkFluidBlocksExt, this.currentChunkRgbsExt, extIndex3d, index3d, index3d + 32, this.blocksFast, this.lightConverter);
							index3d += 32;
						}
						else
						{
							chunkdata.GetRange_Faster(this.currentChunkBlocksExt, this.currentChunkFluidBlocksExt, this.currentChunkRgbsExt, extIndex3d, index3d, index3d + 2, this.blocksFast, this.lightConverter);
							extIndex3d += 30;
							index3d += 30;
							chunkdata.GetRange_Faster(this.currentChunkBlocksExt, this.currentChunkFluidBlocksExt, this.currentChunkRgbsExt, extIndex3d, index3d, index3d + 2, this.blocksFast, this.lightConverter);
							index3d += 2;
						}
					}
				}
			}
			else
			{
				for (int y2 = 0; y2 < 32; y2++)
				{
					for (int z2 = 0; z2 < 32; z2++)
					{
						extIndex3d = (y2 * 34 + z2) * 34 + constOffset;
						chunkdata.GetRange_Faster(this.currentChunkBlocksExt, this.currentChunkFluidBlocksExt, this.currentChunkRgbsExt, extIndex3d, index3d, index3d + 32, this.blocksFast, this.lightConverter);
						index3d += 32;
					}
				}
			}
			extIndex3d = -1;
			for (int extendedLY = 0; extendedLY < extendedChunkSize; extendedLY++)
			{
				bool edgeY = extendedLY == 0 || extendedLY == maxEdge;
				for (int extendedLZ = 0; extendedLZ < extendedChunkSize; extendedLZ++)
				{
					bool flag = extendedLZ == 0 || extendedLZ == maxEdge;
					int iy = (edgeY ? ((extendedLY == 0) ? 0 : 2) : 1);
					int iz = (flag ? ((extendedLZ == 0) ? 0 : 2) : 1);
					int num = (extendedLY - 1) & 31;
					int z3 = (extendedLZ - 1) & 31;
					index3d = (num * 32 + z3) * 32;
					int cqaIndex = iy * 3 + iz;
					chunkdata = this.chunkdatasNearby[cqaIndex];
					ushort light;
					int lightSat;
					int fluidId;
					int blockId = chunkdata.GetOne(out light, out lightSat, out fluidId, index3d + 31);
					this.currentChunkBlocksExt[++extIndex3d] = this.blocksFast[blockId];
					this.currentChunkFluidBlocksExt[extIndex3d] = this.blocksFast[fluidId];
					this.currentChunkRgbsExt[extIndex3d] = this.lightConverter.ToRgba((uint)light, lightSat);
					cqaIndex += 9;
					if (cqaIndex == 13)
					{
						extIndex3d += 32;
					}
					else
					{
						chunkdata = this.chunkdatasNearby[cqaIndex];
						chunkdata.GetRange(this.currentChunkBlocksExt, this.currentChunkFluidBlocksExt, this.currentChunkRgbsExt, extIndex3d, index3d, index3d + 32, this.blocksFast, this.lightConverter);
						extIndex3d += 32;
					}
					cqaIndex += 9;
					chunkdata = this.chunkdatasNearby[cqaIndex];
					blockId = chunkdata.GetOne(out light, out lightSat, out fluidId, index3d);
					this.currentChunkBlocksExt[++extIndex3d] = this.blocksFast[blockId];
					this.currentChunkFluidBlocksExt[extIndex3d] = this.blocksFast[fluidId];
					this.currentChunkRgbsExt[extIndex3d] = this.lightConverter.ToRgba((uint)light, lightSat);
				}
			}
			for (int j = 0; j < this.currentChunkBlocksExt.Length; j++)
			{
				if (this.currentChunkBlocksExt[j] == null)
				{
					this.currentChunkBlocksExt[j] = this.blocksFast[0];
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public MeshData GetMeshPoolForPass(int textureid, EnumChunkRenderPass renderPass, int lodLevel)
		{
			int atlasNum = 0;
			while (this.TextureIdToReturnNum[atlasNum] != textureid)
			{
				if (++atlasNum >= this.quantityAtlasses)
				{
					return null;
				}
			}
			return this.currentModeldataByRenderPassByLodLevel[lodLevel][(int)renderPass][atlasNum];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public MeshData[] GetPoolForPass(EnumChunkRenderPass renderPass, int lodLevel)
		{
			return this.currentModeldataByRenderPassByLodLevel[lodLevel][(int)renderPass];
		}

		public const int LODPOOLS = 4;

		internal int[] TextureIdToReturnNum;

		private const int chunkSize = 32;

		internal ClientMain game;

		internal readonly Block[] currentChunkBlocksExt;

		internal readonly Block[] currentChunkFluidBlocksExt;

		internal readonly int[] currentChunkRgbsExt;

		internal byte[] currentChunkDraw32;

		internal byte[] currentChunkDrawFluids;

		internal int[] currentClimateRegionMap;

		internal float currentOceanityMapTL;

		internal float currentOceanityMapTR;

		internal float currentOceanityMapBL;

		internal float currentOceanityMapBR;

		internal bool started;

		internal int mapsizex;

		internal int mapsizey;

		internal int mapsizez;

		internal int mapsizeChunksx;

		internal int mapsizeChunksy;

		internal int mapsizeChunksz;

		private int quantityAtlasses;

		internal bool[] isPartiallyTransparent;

		internal bool[] isLiquidBlock;

		internal MeshData[][][] currentModeldataByRenderPassByLodLevel;

		internal MeshData[][][] centerModeldataByRenderPassByLodLevel;

		internal MeshData[][][] edgeModeldataByRenderPassByLodLevel;

		private int[][] fastBlockTextureSubidsByBlockAndFace;

		private TesselatedChunkPart[] ret;

		private TesselatedChunkPart[] emptyParts = Array.Empty<TesselatedChunkPart>();

		internal static readonly float[] waterLevels = new float[] { 0f, 0.125f, 0.25f, 0.375f, 0.5f, 0.625f, 0.75f, 0.875f, 1f };

		private int seaLevel;

		internal int regionSize;

		internal const int extChunkSize = 34;

		internal const int maxX = 31;

		internal bool AoAndSmoothShadows;

		internal Block[] blocksFast;

		internal readonly TCTCache vars;

		private ColorUtil.LightUtil lightConverter;

		private readonly IBlockTesselator[] blockTesselators = new IBlockTesselator[40];

		public JsonTesselator jsonTesselator;

		internal ITesselatorAPI offthreadTesselator;

		internal readonly ClientChunk[] chunksNearby;

		internal readonly ClientChunkData[] chunkdatasNearby;

		public object ReloadLock = new object();

		private ColorMapData defaultColorMapData;

		private float[][] decorRotationMatrices = new float[24][];

		private bool lightsGo;

		private bool blockTexturesGo;

		private EnumChunkRenderPass[] passes = (EnumChunkRenderPass[])Enum.GetValues(typeof(EnumChunkRenderPass));

		private BlockPos tmpPos = new BlockPos();
	}
}
