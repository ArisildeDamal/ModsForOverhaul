using System;
using OpenTK.Graphics.OpenGL;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class ChunkRenderer
	{
		public ChunkRenderer(int[] textureIds, ClientMain game)
		{
			this.textureIds = textureIds;
			this.platform = game.Platform;
			this.game = game;
			this.culler = new ChunkCuller(game);
			game.api.eventapi.ReloadShader += this.Eventapi_ReloadShader;
			this.twoCustomFloats = new CustomMeshDataPartFloat
			{
				InterleaveOffsets = new int[1],
				InterleaveSizes = new int[] { 2 },
				InterleaveStride = 8
			};
			this.twoCustomInts = new CustomMeshDataPartInt
			{
				InterleaveOffsets = new int[] { 0, 4 },
				InterleaveSizes = new int[] { 1, 1 },
				InterleaveStride = 8,
				Conversion = DataConversion.Integer
			};
			this.oneCustomInt = new CustomMeshDataPartInt
			{
				InterleaveOffsets = new int[1],
				InterleaveSizes = new int[] { 1 },
				InterleaveStride = 4,
				Conversion = DataConversion.Integer
			};
			this.twoShortsNormalised = new CustomMeshDataPartShort
			{
				InterleaveOffsets = new int[1],
				InterleaveSizes = new int[] { 2 },
				InterleaveStride = 4,
				Conversion = DataConversion.NormalizedFloat
			};
			this.masterPool = new MeshDataPoolMasterManager(game.api);
			this.masterPool.DelayedPoolLocationRemoval = true;
			Array passes = Enum.GetValues(typeof(EnumChunkRenderPass));
			this.poolsByRenderPass = new MeshDataPoolManager[passes.Length][];
			int MaxVertices = ClientSettings.ModelDataPoolMaxVertexSize;
			int MaxIndices = ClientSettings.ModelDataPoolMaxIndexSize;
			int MaxPartsPerPool = ClientSettings.ModelDataPoolMaxParts * 2;
			foreach (object obj in passes)
			{
				EnumChunkRenderPass pass = (EnumChunkRenderPass)obj;
				this.poolsByRenderPass[(int)pass] = new MeshDataPoolManager[textureIds.Length + 3];
				for (int i = 0; i < textureIds.Length; i++)
				{
					this.AddPoolsForAtlasAndPass(i, pass, MaxVertices, MaxIndices, MaxPartsPerPool);
				}
			}
			this.blockTextureSize.X = (float)game.textureSize / (float)game.BlockAtlasManager.Size.Width;
			this.blockTextureSize.Y = (float)game.textureSize / (float)game.BlockAtlasManager.Size.Height;
		}

		internal void RuntimeAddBlockTextureAtlas(int[] textureIds)
		{
			Array values = Enum.GetValues(typeof(EnumChunkRenderPass));
			int MaxVertices = ClientSettings.ModelDataPoolMaxVertexSize;
			int MaxIndices = ClientSettings.ModelDataPoolMaxIndexSize;
			int MaxPartsPerPool = ClientSettings.ModelDataPoolMaxParts * 2;
			int index = textureIds.Length - 1;
			foreach (object obj in values)
			{
				EnumChunkRenderPass pass = (EnumChunkRenderPass)obj;
				this.AddPoolsForAtlasAndPass(index, pass, MaxVertices, MaxIndices, MaxPartsPerPool);
			}
			this.textureIds = textureIds;
		}

		private void AddPoolsForAtlasAndPass(int atlas, EnumChunkRenderPass pass, int maxVertices, int maxIndices, int maxPartsPerPool)
		{
			if (pass == EnumChunkRenderPass.Liquid)
			{
				this.poolsByRenderPass[(int)pass][atlas] = new MeshDataPoolManager(this.masterPool, this.game.frustumCuller, this.game.api, maxVertices, maxIndices, maxPartsPerPool, this.twoCustomFloats, null, null, this.twoCustomInts);
				return;
			}
			if (pass == EnumChunkRenderPass.TopSoil)
			{
				this.poolsByRenderPass[(int)pass][atlas] = new MeshDataPoolManager(this.masterPool, this.game.frustumCuller, this.game.api, maxVertices, maxIndices, maxPartsPerPool, null, this.twoShortsNormalised, null, this.oneCustomInt);
				return;
			}
			this.poolsByRenderPass[(int)pass][atlas] = new MeshDataPoolManager(this.masterPool, this.game.frustumCuller, this.game.api, maxVertices, maxIndices, maxPartsPerPool, null, null, null, this.oneCustomInt);
		}

		private bool Eventapi_ReloadShader()
		{
			this.lastSetRainFall = -1f;
			return true;
		}

		internal void SwapVisibleBuffers()
		{
			ClientChunk.bufIndex = (ClientChunk.bufIndex + 1) % 2;
			ModelDataPoolLocation.VisibleBufIndex = ClientChunk.bufIndex;
		}

		public void OnSeperateThreadTick(float dt)
		{
			this.culler.CullInvisibleChunks();
		}

		public void OnRenderBefore(float dt)
		{
			this.game.Platform.LoadFrameBuffer(EnumFrameBuffer.LiquidDepth);
			this.game.Platform.ClearFrameBuffer(EnumFrameBuffer.LiquidDepth);
			this.subPixelPaddingX = this.game.BlockAtlasManager.SubPixelPaddingX;
			this.subPixelPaddingY = this.game.BlockAtlasManager.SubPixelPaddingY;
			Vec3d playerPos = this.game.EntityPlayer.CameraPos;
			this.game.GlPushMatrix();
			this.game.GlLoadMatrix(this.game.MainCamera.CameraMatrixOrigin);
			ShaderProgramChunkliquiddepth progLide = ShaderPrograms.Chunkliquiddepth;
			progLide.Use();
			progLide.ViewDistance = (float)ClientSettings.ViewDistance;
			progLide.ProjectionMatrix = this.game.CurrentProjectionMatrix;
			progLide.ModelViewMatrix = this.game.CurrentModelViewMatrix;
			bool tempSSBOs = this.game.api.renderapi.useSSBOs;
			this.game.api.renderapi.useSSBOs = false;
			for (int i = 0; i < this.textureIds.Length; i++)
			{
				this.poolsByRenderPass[4][i].Render(playerPos, "origin", EnumFrustumCullMode.CullNormal);
			}
			this.game.api.renderapi.useSSBOs = tempSSBOs;
			progLide.Stop();
			ScreenManager.FrameProfiler.Mark("rend3D-ret-lide");
			this.game.GlPopMatrix();
			this.game.Platform.UnloadFrameBuffer(EnumFrameBuffer.LiquidDepth);
			this.game.Platform.LoadFrameBuffer(EnumFrameBuffer.Primary);
		}

		public void OnBeforeRenderOpaque(float dt)
		{
			this.masterPool.OnFrame(dt, this.game.CurrentModelViewMatrix, this.game.shadowMvpMatrix);
			RuntimeStats.renderedTriangles = 0;
			RuntimeStats.availableTriangles = 0;
			this.accum += dt;
			if (this.accum > 5f)
			{
				this.accum = 0f;
				ClimateCondition conds = this.game.BlockAccessor.GetClimateAt(this.game.EntityPlayer.Pos.AsBlockPos, EnumGetClimateMode.NowValues, 0.0);
				float raininess = GameMath.Clamp((conds.Temperature + 1f) / 4f, 0f, 1f);
				this.curRainFall = conds.Rainfall * raininess;
			}
		}

		public void RenderShadow(float dt)
		{
			ShaderProgramShadowmapgeneric prog = ShaderPrograms.Chunkshadowmap;
			prog.Uniform("subpixelPaddingX", this.subPixelPaddingX);
			prog.Uniform("subpixelPaddingY", this.subPixelPaddingY);
			Vec3d playerPos = this.game.EntityPlayer.CameraPos;
			ScreenManager.FrameProfiler.Mark("rend3D-rets-begin");
			this.platform.GlDepthMask(true);
			this.platform.GlToggleBlend(false, EnumBlendMode.Standard);
			this.platform.GlEnableDepthTest();
			this.platform.GlDisableCullFace();
			EnumFrustumCullMode cullMode = ((this.game.currentRenderStage == EnumRenderStage.ShadowFar) ? EnumFrustumCullMode.CullInstantShadowPassFar : EnumFrustumCullMode.CullInstantShadowPassNear);
			for (int i = 0; i < this.textureIds.Length; i++)
			{
				prog.Tex2d2D = this.textureIds[i];
				this.poolsByRenderPass[0][i].Render(playerPos, "origin", cullMode);
			}
			ScreenManager.FrameProfiler.Mark("rend3D-rets-op");
			for (int j = 0; j < this.textureIds.Length; j++)
			{
				prog.Tex2d2D = this.textureIds[j];
				this.poolsByRenderPass[5][j].Render(playerPos, "origin", cullMode);
			}
			ScreenManager.FrameProfiler.Mark("rend3D-rets-tpp");
			this.platform.GlDisableCullFace();
			for (int k = 0; k < this.textureIds.Length; k++)
			{
				prog.Tex2d2D = this.textureIds[k];
				this.poolsByRenderPass[2][k].Render(playerPos, "origin", cullMode);
			}
			for (int l = 0; l < this.textureIds.Length; l++)
			{
				prog.Tex2d2D = this.textureIds[l];
				this.poolsByRenderPass[1][l].Render(playerPos, "origin", cullMode);
			}
			this.platform.GlToggleBlend(true, EnumBlendMode.Standard);
		}

		public void RenderOpaque(float dt)
		{
			Vec3d playerCamPos = this.game.EntityPlayer.CameraPos;
			ScreenManager.FrameProfiler.Mark("rend3D-ret-begin");
			this.platform.GlDepthMask(true);
			this.platform.GlEnableDepthTest();
			this.platform.GlToggleBlend(true, EnumBlendMode.Standard);
			this.platform.GlEnableCullFace();
			this.game.GlMatrixModeModelView();
			this.game.GlPushMatrix();
			this.game.GlLoadMatrix(this.game.MainCamera.CameraMatrixOrigin);
			ShaderProgramChunkopaque progOp = ShaderPrograms.Chunkopaque;
			progOp.Use();
			progOp.CameraUnderwater = this.game.shUniforms.CameraUnderwater;
			progOp.RgbaFogIn = this.game.AmbientManager.BlendedFogColor;
			progOp.RgbaAmbientIn = this.game.AmbientManager.BlendedAmbientColor;
			progOp.FogDensityIn = this.game.AmbientManager.BlendedFogDensity;
			progOp.FogMinIn = this.game.AmbientManager.BlendedFogMin;
			progOp.ProjectionMatrix = this.game.CurrentProjectionMatrix;
			progOp.AlphaTest = 0.001f;
			progOp.HaxyFade = 0;
			progOp.LiquidDepth2D = this.game.Platform.FrameBuffers[5].DepthTextureId;
			progOp.ModelViewMatrix = this.game.CurrentModelViewMatrix;
			progOp.Uniform("subpixelPaddingX", this.subPixelPaddingX);
			progOp.Uniform("subpixelPaddingY", this.subPixelPaddingY);
			for (int i = 0; i < this.textureIds.Length; i++)
			{
				progOp.TerrainTex2D = this.textureIds[i];
				progOp.TerrainTexLinear2D = this.textureIds[i];
				this.poolsByRenderPass[0][i].Render(playerCamPos, "origin", EnumFrustumCullMode.CullNormal);
			}
			ScreenManager.FrameProfiler.Mark("rend3D-ret-op");
			progOp.Stop();
			ShaderProgramChunktopsoil progTs = ShaderPrograms.Chunktopsoil;
			progTs.Use();
			progTs.RgbaFogIn = this.game.AmbientManager.BlendedFogColor;
			progTs.RgbaAmbientIn = this.game.AmbientManager.BlendedAmbientColor;
			progTs.FogDensityIn = this.game.AmbientManager.BlendedFogDensity;
			progTs.FogMinIn = this.game.AmbientManager.BlendedFogMin;
			progTs.ProjectionMatrix = this.game.CurrentProjectionMatrix;
			progTs.ModelViewMatrix = this.game.CurrentModelViewMatrix;
			progTs.BlockTextureSize = this.blockTextureSize;
			progTs.Uniform("subpixelPaddingX", this.subPixelPaddingX);
			progTs.Uniform("subpixelPaddingY", this.subPixelPaddingY);
			for (int j = 0; j < this.textureIds.Length; j++)
			{
				progTs.TerrainTex2D = this.textureIds[j];
				progTs.TerrainTexLinear2D = this.textureIds[j];
				this.poolsByRenderPass[5][j].Render(playerCamPos, "origin", EnumFrustumCullMode.CullNormal);
			}
			ScreenManager.FrameProfiler.Mark("rend3D-ret-tpp");
			progTs.Stop();
			this.platform.GlDisableCullFace();
			progOp.Use();
			progOp.RgbaFogIn = this.game.AmbientManager.BlendedFogColor;
			progOp.RgbaAmbientIn = this.game.AmbientManager.BlendedAmbientColor;
			progOp.FogDensityIn = this.game.AmbientManager.BlendedFogDensity;
			progOp.FogMinIn = this.game.AmbientManager.BlendedFogMin;
			progOp.ProjectionMatrix = this.game.CurrentProjectionMatrix;
			progOp.ModelViewMatrix = this.game.CurrentModelViewMatrix;
			progOp.AlphaTest = 0.25f;
			progOp.HaxyFade = 0;
			this.platform.GlToggleBlend(true, EnumBlendMode.Standard);
			for (int k = 0; k < this.textureIds.Length; k++)
			{
				progOp.TerrainTex2D = this.textureIds[k];
				progOp.TerrainTexLinear2D = this.textureIds[k];
				this.poolsByRenderPass[2][k].Render(playerCamPos, "origin", EnumFrustumCullMode.CullNormal);
			}
			this.platform.GlToggleBlend(false, EnumBlendMode.Standard);
			progOp.AlphaTest = 0.42f;
			progOp.SunPosition = this.game.GameWorldCalendar.SunPositionNormalized;
			progOp.DayLight = this.game.shUniforms.SkyDaylight;
			progOp.HorizonFog = this.game.AmbientManager.BlendedCloudDensity;
			progOp.HaxyFade = 1;
			for (int l = 0; l < this.textureIds.Length; l++)
			{
				progOp.TerrainTex2D = this.textureIds[l];
				progOp.TerrainTexLinear2D = this.textureIds[l];
				this.poolsByRenderPass[1][l].Render(playerCamPos, "origin", EnumFrustumCullMode.CullNormal);
			}
			progOp.Stop();
			ScreenManager.FrameProfiler.Mark("rend3D-ret-opnc");
			this.game.GlPopMatrix();
			if (this.game.unbindSamplers)
			{
				GL.BindSampler(0, 0);
				GL.BindSampler(1, 0);
				GL.BindSampler(2, 0);
				GL.BindSampler(3, 0);
				GL.BindSampler(4, 0);
				GL.BindSampler(5, 0);
				GL.BindSampler(6, 0);
				GL.BindSampler(7, 0);
				GL.BindSampler(8, 0);
			}
		}

		internal void RenderOIT(float deltaTime)
		{
			Vec3d playerPos = this.game.EntityPlayer.CameraPos;
			this.game.GlPushMatrix();
			this.game.GlLoadMatrix(this.game.MainCamera.CameraMatrixOrigin);
			this.game.GlPushMatrix();
			ShaderProgramChunkliquid progLi = ShaderPrograms.Chunkliquid;
			progLi.Use();
			progLi.WaterStillCounter = this.game.shUniforms.WaterStillCounter;
			progLi.WaterFlowCounter = this.game.shUniforms.WaterFlowCounter;
			progLi.WindWaveIntensity = this.game.shUniforms.WindWaveIntensity;
			progLi.SunPosRel = this.game.shUniforms.SunPosition3D;
			progLi.SunColor = this.game.Calendar.SunColor;
			progLi.ReflectColor = this.game.Calendar.ReflectColor;
			progLi.PlayerPosForFoam = this.game.shUniforms.PlayerPosForFoam;
			progLi.CameraUnderwater = this.game.shUniforms.CameraUnderwater;
			progLi.Uniform("subpixelPaddingX", this.subPixelPaddingX);
			progLi.Uniform("subpixelPaddingY", this.subPixelPaddingY);
			if (Math.Abs(this.lastSetRainFall - this.curRainFall) > 0.05f || this.curRainFall == 0f)
			{
				progLi.DropletIntensity = (this.lastSetRainFall = this.curRainFall);
			}
			FrameBufferRef framebuffer = this.game.api.Render.FrameBuffers[0];
			progLi.RgbaFogIn = this.game.AmbientManager.BlendedFogColor;
			progLi.RgbaAmbientIn = this.game.AmbientManager.BlendedAmbientColor;
			progLi.FogDensityIn = this.game.AmbientManager.BlendedFogDensity;
			progLi.FogMinIn = this.game.AmbientManager.BlendedFogMin;
			progLi.BlockTextureSize = this.blockTextureSize;
			progLi.TextureAtlasSize = new Vec2f(this.game.BlockAtlasManager.Size);
			progLi.ToShadowMapSpaceMatrixFar = this.game.toShadowMapSpaceMatrixFar;
			progLi.ToShadowMapSpaceMatrixNear = this.game.toShadowMapSpaceMatrixNear;
			progLi.ProjectionMatrix = this.game.CurrentProjectionMatrix;
			progLi.ModelViewMatrix = this.game.CurrentModelViewMatrix;
			progLi.PlayerViewVec = this.game.shUniforms.PlayerViewVector;
			progLi.DepthTex2D = framebuffer.DepthTextureId;
			progLi.FrameSize = new Vec2f((float)framebuffer.Width, (float)framebuffer.Height);
			progLi.SunSpecularIntensity = this.game.shUniforms.SunSpecularIntensity;
			bool tempSSBOs = this.game.api.renderapi.useSSBOs;
			this.game.api.renderapi.useSSBOs = false;
			for (int i = 0; i < this.textureIds.Length; i++)
			{
				progLi.TerrainTex2D = this.textureIds[i];
				this.poolsByRenderPass[4][i].Render(playerPos, "origin", EnumFrustumCullMode.CullNormal);
			}
			this.game.api.renderapi.useSSBOs = tempSSBOs;
			progLi.Stop();
			ScreenManager.FrameProfiler.Mark("rend3D-ret-lp");
			this.game.GlPopMatrix();
			ShaderProgramChunktransparent progTp = ShaderPrograms.Chunktransparent;
			progTp.Use();
			progTp.RgbaFogIn = this.game.AmbientManager.BlendedFogColor;
			progTp.RgbaAmbientIn = this.game.AmbientManager.BlendedAmbientColor;
			progTp.FogDensityIn = this.game.AmbientManager.BlendedFogDensity;
			progTp.FogMinIn = this.game.AmbientManager.BlendedFogMin;
			progTp.ProjectionMatrix = this.game.CurrentProjectionMatrix;
			progTp.ModelViewMatrix = this.game.CurrentModelViewMatrix;
			progTp.Uniform("subpixelPaddingX", this.subPixelPaddingX);
			progTp.Uniform("subpixelPaddingY", this.subPixelPaddingY);
			for (int j = 0; j < this.textureIds.Length; j++)
			{
				progTp.TerrainTex2D = this.textureIds[j];
				this.poolsByRenderPass[3][j].Render(playerPos, "origin", EnumFrustumCullMode.CullNormal);
				if (ClientSettings.RenderMetaBlocks)
				{
					this.poolsByRenderPass[6][j].Render(playerPos, "origin", EnumFrustumCullMode.CullNormal);
				}
			}
			progTp.Stop();
			this.game.GlPopMatrix();
			ScreenManager.FrameProfiler.Mark("rend3D-ret-tp");
		}

		internal void RenderAfterOIT(float deltaTime)
		{
			this.game.GlPushMatrix();
			this.game.GlLoadMatrix(this.game.MainCamera.CameraMatrixOrigin);
			Vec3d playerCamPos = this.game.EntityPlayer.CameraPos;
			ShaderProgramChunkopaque progOp = ShaderPrograms.Chunkopaque;
			this.platform.GlDisableCullFace();
			this.platform.GlToggleBlend(false, EnumBlendMode.Standard);
			this.platform.GlEnableDepthTest();
			progOp.Use();
			progOp.RgbaFogIn = this.game.AmbientManager.BlendedFogColor;
			progOp.RgbaAmbientIn = this.game.AmbientManager.BlendedAmbientColor;
			progOp.FogDensityIn = this.game.AmbientManager.BlendedFogDensity;
			progOp.FogMinIn = this.game.AmbientManager.BlendedFogMin;
			progOp.ProjectionMatrix = this.game.CurrentProjectionMatrix;
			progOp.ModelViewMatrix = this.game.CurrentModelViewMatrix;
			progOp.AlphaTest = 0.42f;
			progOp.SunPosition = this.game.GameWorldCalendar.SunPositionNormalized;
			progOp.DayLight = this.game.shUniforms.SkyDaylight;
			progOp.HorizonFog = this.game.AmbientManager.BlendedCloudDensity;
			progOp.HaxyFade = 1;
			progOp.Uniform("subpixelPaddingX", this.subPixelPaddingX);
			progOp.Uniform("subpixelPaddingY", this.subPixelPaddingY);
			for (int i = 0; i < this.textureIds.Length; i++)
			{
				progOp.TerrainTex2D = this.textureIds[i];
				progOp.TerrainTexLinear2D = this.textureIds[i];
				this.poolsByRenderPass[7][i].Render(playerCamPos, "origin", EnumFrustumCullMode.CullNormal);
			}
			progOp.Stop();
			this.game.GlPopMatrix();
		}

		public void Dispose()
		{
			this.masterPool.DisposeAllPools(this.game.api);
		}

		public void AddTesselatedChunk(TesselatedChunk tesschunk, ClientChunk hostChunk)
		{
			Vec3i chunkOrigin = new Vec3i(tesschunk.positionX, tesschunk.positionYAndDimension % 32768, tesschunk.positionZ);
			int dimension = tesschunk.positionYAndDimension / 32768;
			Sphere boundingSphere = tesschunk.boundingSphere;
			tesschunk.AddCenterToPools(this, chunkOrigin, dimension, boundingSphere, hostChunk);
			tesschunk.AddEdgeToPools(this, chunkOrigin, dimension, boundingSphere, hostChunk);
			tesschunk.centerParts = null;
			tesschunk.edgeParts = null;
			tesschunk.chunk = null;
		}

		public void RemoveDataPoolLocations(ModelDataPoolLocation[] locations)
		{
			this.masterPool.RemoveDataPoolLocations(locations);
		}

		public void GetStats(out long usedVideoMemory, out long renderedTris, out long allocatedTris)
		{
			usedVideoMemory = 0L;
			renderedTris = 0L;
			allocatedTris = 0L;
			foreach (object obj in Enum.GetValues(typeof(EnumChunkRenderPass)))
			{
				EnumChunkRenderPass pass = (EnumChunkRenderPass)obj;
				for (int i = 0; i < this.textureIds.Length; i++)
				{
					this.poolsByRenderPass[(int)pass][i].GetStats(ref usedVideoMemory, ref renderedTris, ref allocatedTris);
				}
			}
		}

		public float CalcFragmentation()
		{
			return this.masterPool.CalcFragmentation();
		}

		public int QuantityModelDataPools()
		{
			return this.masterPool.QuantityModelDataPools();
		}

		internal void SetInterleaveStrides(MeshData modelDataLod0, EnumChunkRenderPass pass)
		{
			if (pass == EnumChunkRenderPass.Liquid)
			{
				modelDataLod0.CustomFloats.InterleaveStride = this.twoCustomFloats.InterleaveStride;
				modelDataLod0.CustomInts.InterleaveStride = this.twoCustomInts.InterleaveStride;
				return;
			}
			modelDataLod0.CustomInts.InterleaveStride = this.oneCustomInt.InterleaveStride;
			if (pass == EnumChunkRenderPass.TopSoil)
			{
				modelDataLod0.CustomShorts.InterleaveStride = this.twoShortsNormalised.InterleaveStride;
			}
		}

		protected MeshDataPoolMasterManager masterPool;

		protected ClientPlatformAbstract platform;

		protected ClientMain game;

		protected ChunkCuller culler;

		protected CustomMeshDataPartFloat twoCustomFloats;

		protected CustomMeshDataPartInt twoCustomInts;

		protected CustomMeshDataPartInt oneCustomInt;

		protected CustomMeshDataPartShort twoShortsNormalised;

		protected Vec2f blockTextureSize = new Vec2f();

		public int[] textureIds;

		public int QuantityRenderingChunks;

		public MeshDataPoolManager[][] poolsByRenderPass;

		private float subPixelPaddingX;

		private float subPixelPaddingY;

		private float curRainFall;

		private float lastSetRainFall;

		private float accum;
	}
}
