using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.Client.NoObf
{
	public class SystemRenderDecals : ClientSystem
	{
		public override string Name
		{
			get
			{
				return "rede";
			}
		}

		public SystemRenderDecals(ClientMain game)
			: base(game)
		{
			game.api.ChatCommands.GetOrCreate("debug").BeginSubCommand("spawndecal").WithDescription("Spawn a decal at position")
				.HandleWith(new OnCommandDelegate(this.OnSpawnDecal))
				.EndSubCommand();
			game.eventManager.OnPlayerBreakingBlock.Add(new Action<BlockDamage>(this.OnPlayerBreakingBlock));
			game.eventManager.OnUnBreakingBlock.Add(new Action<BlockDamage>(this.OnUnBreakingBlock));
			game.eventManager.OnPlayerBrokenBlock.Add(new Action<BlockDamage>(this.OnPlayerBrokenBlock));
			game.RegisterGameTickListener(new Action<float>(this.OnGameTick), 500, 0);
			game.eventManager.OnBlockChanged.Add(new BlockChangedDelegate(this.OnBlockChanged));
			game.eventManager.OnReloadShapes += this.TesselateDecalsFromBlockShapes;
			game.eventManager.RegisterRenderer(new Action<float>(this.OnRenderFrame3D), EnumRenderStage.AfterOIT, "decals", 0.5);
		}

		public override void OnBlockTexturesLoaded()
		{
			this.InitAtlasAndModelPool();
			this.TesselateDecalsFromBlockShapes();
		}

		private void TesselateDecalsFromBlockShapes()
		{
			this.decalModeldatas = new MeshData[this.game.Blocks.Count][];
		}

		private void tesselateBlockDecal(int blockId)
		{
			Block block = this.game.Blocks[blockId];
			TextureSource texSource = new TextureSource(this.game, this.decalAtlasSize, block, false);
			texSource.isDecalUv = true;
			int altShapeCount = ((block.Shape.BakedAlternates == null) ? 0 : block.Shape.BakedAlternates.Length);
			try
			{
				if (altShapeCount > 0)
				{
					this.decalModeldatas[block.BlockId] = new MeshData[block.Shape.BakedAlternates.Length];
					for (int i = 0; i < block.Shape.BakedAlternates.Length; i++)
					{
						MeshData altModeldata;
						this.game.TesselatorManager.Tesselator.TesselateBlock(block, block.Shape.BakedAlternates[i % altShapeCount], out altModeldata, texSource, null, null);
						this.addLod0Mesh(altModeldata, block, texSource, i);
						this.decalModeldatas[block.BlockId][i] = altModeldata;
					}
				}
				else
				{
					MeshData altModeldata2;
					this.game.TesselatorManager.Tesselator.TesselateBlock(block, block.Shape, out altModeldata2, texSource, null, null);
					this.addLod0Mesh(altModeldata2, block, texSource, 0);
					this.decalModeldatas[block.BlockId] = new MeshData[] { altModeldata2 };
				}
				foreach (MeshData mesh in this.decalModeldatas[block.BlockId])
				{
					this.addZOffset(block, mesh);
				}
			}
			catch (Exception e)
			{
				this.game.Platform.Logger.Error("Exception thrown when trying to tesselate block for decal system {0}. Will use invisible decal.", new object[] { block });
				this.game.Platform.Logger.Error(e);
				this.decalModeldatas[block.BlockId] = new MeshData[]
				{
					new MeshData(4, 6, false, true, true, true)
				};
			}
		}

		private void addZOffset(Block block, MeshData mesh)
		{
			int zoffs = (int)block.VertexFlags.ZOffset << 8;
			for (int i = 0; i < mesh.FlagsCount; i++)
			{
				mesh.Flags[i] |= zoffs;
			}
		}

		private void addLod0Mesh(MeshData altModeldata, Block block, TextureSource texSource, int alternateIndex)
		{
			if (block.Lod0Shape == null)
			{
				return;
			}
			MeshData lod0DecalMesh;
			this.game.TesselatorManager.Tesselator.TesselateBlock(block, block.Lod0Shape.BakedAlternates[alternateIndex], out lod0DecalMesh, texSource, null, null);
			altModeldata.AddMeshData(lod0DecalMesh);
		}

		private TextCommandResult OnSpawnDecal(TextCommandCallingArgs textCommandCallingArgs)
		{
			if (this.game.BlockSelection != null)
			{
				this.AddBlockBreakDecal(this.game.BlockSelection.Position, 3);
			}
			return TextCommandResult.Success("", null);
		}

		private void OnGameTick(float dt)
		{
		}

		private void OnBlockChanged(BlockPos pos, Block oldBlock)
		{
			if (this.decals.Count == 0)
			{
				return;
			}
			List<int> foundDecals = new List<int>();
			foreach (KeyValuePair<int, BlockDecal> val in this.decals)
			{
				if (val.Value.pos.Equals(pos))
				{
					foundDecals.Add(val.Key);
				}
			}
			foreach (int decalid in foundDecals)
			{
				BlockDecal decal = this.decals[decalid];
				if (decal.PoolLocation != null)
				{
					this.decalPool.RemoveLocation(decal.PoolLocation);
				}
				decal.PoolLocation = null;
				this.decals.Remove(decalid);
			}
		}

		private void OnPlayerBrokenBlock(BlockDamage blockDamage)
		{
			if (blockDamage.DecalId == 0)
			{
				return;
			}
			BlockDecal decal;
			this.decals.TryGetValue(blockDamage.DecalId, out decal);
			if (decal != null && decal.PoolLocation != null)
			{
				this.decalPool.RemoveLocation(decal.PoolLocation);
				decal.PoolLocation = null;
			}
			this.decals.Remove(blockDamage.DecalId);
		}

		private void OnUnBreakingBlock(BlockDamage blockDamage)
		{
			if (blockDamage.DecalId == 0)
			{
				return;
			}
			if (blockDamage.RemainingResistance >= blockDamage.Block.GetResistance(this.game.BlockAccessor, blockDamage.Position))
			{
				this.OnPlayerBrokenBlock(blockDamage);
				return;
			}
			this.OnPlayerBreakingBlock(blockDamage);
		}

		private void OnPlayerBreakingBlock(BlockDamage blockDamage)
		{
			float resi = blockDamage.Block.GetResistance(this.game.BlockAccessor, blockDamage.Position);
			if (blockDamage.RemainingResistance == resi)
			{
				return;
			}
			if (blockDamage.DecalId != 0 && this.decals.ContainsKey(blockDamage.DecalId))
			{
				BlockDecal decal = this.decals[blockDamage.DecalId];
				int stages = 10;
				int animationStage = decal.AnimationStage;
				int stage = (int)((float)stages * (resi - blockDamage.RemainingResistance) / resi);
				decal.AnimationStage = GameMath.Clamp(stage, 1, stages - 1);
				decal.LastModifiedMilliseconds = this.game.ElapsedMilliseconds;
				if (animationStage != decal.AnimationStage)
				{
					this.UpdateDecal(decal);
				}
				return;
			}
			BlockDecal decal2 = this.AddBlockBreakDecal(blockDamage.Position, 0);
			if (decal2 == null)
			{
				return;
			}
			blockDamage.DecalId = decal2.DecalId;
		}

		internal BlockDecal AddBlockBreakDecal(BlockPos pos, int stage)
		{
			BlockDecal blockDecal = new BlockDecal();
			blockDecal.AnimationStage = stage;
			int num = this.nextDecalId;
			this.nextDecalId = num + 1;
			blockDecal.DecalId = num;
			blockDecal.pos = pos.Copy();
			blockDecal.LastModifiedMilliseconds = this.game.ElapsedMilliseconds;
			BlockDecal decal = blockDecal;
			if (this.UpdateDecal(decal))
			{
				this.decals.Add(decal.DecalId, decal);
				return decal;
			}
			return null;
		}

		internal bool UpdateDecal(BlockDecal decal)
		{
			if (decal.PoolLocation != null)
			{
				this.decalPool.RemoveLocation(decal.PoolLocation);
			}
			int textureSubId;
			this.TextureNameToIdMapping.TryGetValue("destroy_stage_" + decal.AnimationStage.ToString() + ".png", out textureSubId);
			Block block = this.game.WorldMap.RelaxedBlockAccess.GetBlock(decal.pos);
			if (block.BlockId == 0)
			{
				decal.PoolLocation = null;
				this.decals.Remove(decal.DecalId);
				return false;
			}
			MeshData blockModelData = null;
			try
			{
				blockModelData = this.game.TesselatorManager.GetDefaultBlockMesh(block);
			}
			catch (Exception)
			{
				this.game.Logger.Error("An exception was thrown getting the shape for {0}, likely the shape file was not found", new object[] { block.Code.ToShortString() });
				return false;
			}
			if (this.decalModeldatas[block.BlockId] == null)
			{
				this.tesselateBlockDecal(block.BlockId);
			}
			MeshData decalModelData;
			if (block.HasAlternates)
			{
				int num = GameMath.MurmurHash3(decal.pos.X, (block.RandomizeAxes == EnumRandomizeAxes.XYZ) ? decal.pos.Y : 0, decal.pos.Z);
				int index = GameMath.Mod(num, this.decalModeldatas[block.BlockId].Length);
				decalModelData = this.decalModeldatas[block.BlockId][index].Clone();
				int index2 = GameMath.Mod(num, this.game.TesselatorManager.altblockModelDatasLod1[block.BlockId].Length);
				blockModelData = this.game.TesselatorManager.altblockModelDatasLod1[block.BlockId][index2];
				if (block.Lod0Shape != null)
				{
					blockModelData = blockModelData.Clone();
					blockModelData.AddMeshData(this.game.TesselatorManager.altblockModelDatasLod0[block.BlockId][index2]);
				}
			}
			else
			{
				decalModelData = this.decalModeldatas[block.BlockId][0].Clone();
				if (block.Lod0Shape != null)
				{
					blockModelData = blockModelData.Clone();
					blockModelData.AddMeshData(block.Lod0Mesh);
				}
			}
			TextureSource texSource = new TextureSource(this.game, this.decalAtlasSize, block, false);
			texSource.isDecalUv = true;
			block.GetDecal(this.game, decal.pos, texSource, ref decalModelData, ref blockModelData);
			decalModelData.CustomFloats = new CustomMeshDataPartFloat(4 * decalModelData.VerticesCount)
			{
				InterleaveSizes = new int[] { 2, 2, 2 },
				InterleaveStride = 24,
				InterleaveOffsets = new int[] { 0, 8, 16 }
			};
			if (decalModelData.VerticesCount == 0)
			{
				decal.PoolLocation = null;
				return false;
			}
			double offX = 0.0;
			double offZ = 0.0;
			if (block.RandomDrawOffset != 0)
			{
				offX = (double)((float)(GameMath.oaatHash(decal.pos.X, 0, decal.pos.Z) % 12) / (24f + 12f * (float)block.RandomDrawOffset));
				offZ = (double)((float)(GameMath.oaatHash(decal.pos.X, 1, decal.pos.Z) % 12) / (24f + 12f * (float)block.RandomDrawOffset));
			}
			if (block.RandomizeRotations)
			{
				int rnd = GameMath.MurmurHash3Mod(-decal.pos.X, (block.RandomizeAxes == EnumRandomizeAxes.XYZ) ? decal.pos.Y : 0, decal.pos.Z, TesselationMetaData.randomRotations.Length);
				decalModelData = decalModelData.MatrixTransform(TesselationMetaData.randomRotMatrices[rnd], this.floatpool, null);
			}
			int lightrgbs = this.game.WorldMap.GetLightRGBsAsInt(decal.pos.X, decal.pos.Y, decal.pos.Z);
			for (int i = 0; i < 6; i++)
			{
				BlockFacing face = BlockFacing.ALLFACES[i];
				Block nblock = this.game.BlockAccessor.GetBlockOnSide(decal.pos, face, 0);
				this.leavesWaveTileSide[i] = !nblock.SideSolid[face.Opposite.Index] || nblock.BlockMaterial == EnumBlockMaterial.Leaves;
			}
			byte sunBright = (byte)(lightrgbs >> 24);
			byte blockR = (byte)(lightrgbs >> 16);
			byte blockG = (byte)(lightrgbs >> 8);
			byte blockB = (byte)lightrgbs;
			int uvIndex = 0;
			int rgbaIndex = 0;
			int xyzIndex = 0;
			for (int j = 0; j < decalModelData.VerticesCount; j++)
			{
				TextureAtlasPosition decalTexPos = this.DecalTextureAtlasPositionsByTextureSubId[textureSubId];
				decalModelData.Uv[uvIndex] = GameMath.Clamp(decalModelData.Uv[uvIndex] + decalTexPos.x1, 0f, 1f);
				uvIndex++;
				decalModelData.Uv[uvIndex] = GameMath.Clamp(decalModelData.Uv[uvIndex] + decalTexPos.y1, 0f, 1f);
				uvIndex++;
				if (blockModelData.UvCount > 2 * j + 1)
				{
					decalModelData.CustomFloats.Add(blockModelData.Uv[2 * j]);
					decalModelData.CustomFloats.Add(blockModelData.Uv[2 * j + 1]);
				}
				decalModelData.CustomFloats.Add(decalTexPos.x2 - decalTexPos.x1);
				decalModelData.CustomFloats.Add(decalTexPos.y2 - decalTexPos.y1);
				decalModelData.CustomFloats.Add(decalTexPos.x1);
				decalModelData.CustomFloats.Add(decalTexPos.y1);
				decalModelData.Rgba[rgbaIndex++] = blockR;
				decalModelData.Rgba[rgbaIndex++] = blockG;
				decalModelData.Rgba[rgbaIndex++] = blockB;
				decalModelData.Rgba[rgbaIndex++] = sunBright;
				decalModelData.Flags[j] = decalModelData.Flags[j];
			}
			block.OnDecalTesselation(this.game, decalModelData, decal.pos);
			for (int k = 0; k < decalModelData.VerticesCount; k++)
			{
				decalModelData.xyz[xyzIndex++] += (float)((double)decal.pos.X + offX - this.decalOrigin.X);
				decalModelData.xyz[xyzIndex++] += (float)((double)decal.pos.Y - this.decalOrigin.Y);
				decalModelData.xyz[xyzIndex++] += (float)((double)decal.pos.Z + offZ - this.decalOrigin.Z);
			}
			Sphere boundingSphere = Sphere.BoundingSphereForCube((float)decal.pos.X, (float)decal.pos.Y, (float)decal.pos.Z, 1f);
			ModelDataPoolLocation location = this.decalPool.TryAdd(this.game.api, decalModelData, null, 0, boundingSphere);
			decal.PoolLocation = location;
			return location != null;
		}

		internal void UpdateAllDecals()
		{
			foreach (BlockDecal decal in new List<BlockDecal>(this.decals.Values))
			{
				this.UpdateDecal(decal);
			}
		}

		public void OnRenderFrame3D(float deltaTime)
		{
			Vec3d playerPos = this.game.EntityPlayer.CameraPos;
			if (this.decalOrigin.SquareDistanceTo(playerPos) > 1000000f)
			{
				this.decalOrigin = playerPos.Clone();
				this.UpdateAllDecals();
			}
			if (this.decals.Count > 0)
			{
				this.game.GlPushMatrix();
				this.game.GlLoadMatrix(this.game.MainCamera.CameraMatrixOrigin);
				this.game.Platform.GlToggleBlend(true, EnumBlendMode.Standard);
				this.game.Platform.GlDisableCullFace();
				ShaderProgramDecals shaderProgramDecals = ShaderPrograms.Decals;
				shaderProgramDecals.Use();
				shaderProgramDecals.WindWaveCounter = this.game.shUniforms.WindWaveCounter;
				shaderProgramDecals.WindWaveCounterHighFreq = this.game.shUniforms.WindWaveCounterHighFreq;
				shaderProgramDecals.BlockTexture2D = this.game.BlockAtlasManager.AtlasTextures[0].TextureId;
				shaderProgramDecals.DecalTexture2D = this.decalTextureAtlas.TextureId;
				shaderProgramDecals.RgbaFogIn = this.game.AmbientManager.BlendedFogColor;
				shaderProgramDecals.RgbaAmbientIn = this.game.AmbientManager.BlendedAmbientColor;
				shaderProgramDecals.FogDensityIn = this.game.AmbientManager.BlendedFogDensity;
				shaderProgramDecals.FogMinIn = this.game.AmbientManager.BlendedFogMin;
				shaderProgramDecals.Origin = new Vec3f((float)(this.decalOrigin.X - playerPos.X), (float)(this.decalOrigin.Y - playerPos.Y), (float)(this.decalOrigin.Z - playerPos.Z));
				shaderProgramDecals.ProjectionMatrix = this.game.CurrentProjectionMatrix;
				shaderProgramDecals.ModelViewMatrix = this.game.CurrentModelViewMatrix;
				this.decalPool.Draw(this.game.api, this.game.frustumCuller, EnumFrustumCullMode.CullInstant);
				shaderProgramDecals.Stop();
				this.game.Platform.GlToggleBlend(true, EnumBlendMode.Standard);
				this.game.Platform.GlEnableCullFace();
				this.game.GlPopMatrix();
			}
		}

		private void InitAtlasAndModelPool()
		{
			List<IAsset> assets = this.game.Platform.AssetManager.GetManyInCategory("textures", "decal/", null, true);
			int size = this.game.textureSize * (int)Math.Ceiling(Math.Sqrt((double)assets.Count));
			this.decalAtlasSize = new Size2i(size, size);
			this.game.Logger.Notification("Texture size is {0} so decal atlas size of {1}x{2} should suffice", new object[]
			{
				this.game.textureSize,
				this.decalAtlasSize.Width,
				this.decalAtlasSize.Height
			});
			TextureAtlas decalAtlas = new TextureAtlas(this.decalAtlasSize.Width, this.decalAtlasSize.Height, 0f, 0f);
			this.DecalTextureAtlasPositionsByTextureSubId = new TextureAtlasPosition[assets.Count];
			this.TextureNameToIdMapping = new Dictionary<string, int>();
			for (int i = 0; i < assets.Count; i++)
			{
				if (!decalAtlas.InsertTexture(i, this.game.api, assets[i]))
				{
					throw new Exception("Texture decal atlas overflow. Did you create a high res texture pack without setting the correct textureSize value in the modinfo.json?");
				}
				this.TextureNameToIdMapping[assets[i].Name] = i;
			}
			this.decalTextureAtlas = decalAtlas.Upload(this.game);
			this.game.Platform.BuildMipMaps(this.decalTextureAtlas.TextureId);
			decalAtlas.PopulateAtlasPositions(this.DecalTextureAtlasPositionsByTextureSubId, 0);
			int quantityVertices = SystemRenderDecals.decalPoolSize * 24 * 10;
			CustomMeshDataPartFloat blockUvFloats = new CustomMeshDataPartFloat
			{
				Instanced = false,
				StaticDraw = true,
				InterleaveSizes = new int[] { 2, 2, 2 },
				InterleaveStride = 24,
				InterleaveOffsets = new int[] { 0, 8, 16 },
				Count = quantityVertices
			};
			this.decalPool = MeshDataPool.AllocateNewPool(this.game.api, quantityVertices, (int)((float)quantityVertices * 1.5f), 2 * SystemRenderDecals.decalPoolSize, blockUvFloats, null, null, null);
		}

		public override EnumClientSystemType GetSystemType()
		{
			return EnumClientSystemType.Render;
		}

		public override void Dispose(ClientMain game)
		{
			MeshDataPool meshDataPool = this.decalPool;
			if (meshDataPool != null)
			{
				meshDataPool.Dispose(game.api);
			}
			LoadedTexture loadedTexture = this.decalTextureAtlas;
			if (loadedTexture == null)
			{
				return;
			}
			loadedTexture.Dispose();
		}

		private MeshDataPool decalPool;

		public MeshData[][] decalModeldatas;

		private LoadedTexture decalTextureAtlas;

		private int nextDecalId = 1;

		private Size2i decalAtlasSize = new Size2i(512, 512);

		internal static int decalPoolSize = 200;

		internal Dictionary<int, BlockDecal> decals = new Dictionary<int, BlockDecal>(SystemRenderDecals.decalPoolSize);

		internal TextureAtlasPosition[] DecalTextureAtlasPositionsByTextureSubId;

		internal Dictionary<string, int> TextureNameToIdMapping;

		private Vec3d decalOrigin = new Vec3d();

		private float[] floatpool = new float[4];

		private bool[] leavesWaveTileSide = new bool[6];
	}
}
