using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class RenderAPIGame : RenderAPIBase
	{
		public RenderAPIGame(ICoreClientAPI capi, ClientMain game)
			: base(game.Platform)
		{
			this.game = game;
			this.inventoryItemRenderer = new InventoryItemRenderer(game);
			this.perceptionEffects = new PerceptionEffects(capi);
			this.useSSBOs = ClientSettings.AllowSSBOs;
			ScreenManager.Platform.UseSSBOs = this.useSSBOs;
			ClientPlatformAbstract.singleIndexBufferId = 0;
			ClientPlatformAbstract.singleIndexBufferSize = 0;
		}

		public override DefaultShaderUniforms ShaderUniforms
		{
			get
			{
				return this.game.shUniforms;
			}
		}

		public override ICoreClientAPI Api
		{
			get
			{
				return this.game.api;
			}
		}

		public override int TextureSize
		{
			get
			{
				return this.game.textureSize;
			}
		}

		public override PerceptionEffects PerceptionEffects
		{
			get
			{
				return this.perceptionEffects;
			}
		}

		public override ModelTransform CameraOffset
		{
			get
			{
				return this.game.MainCamera.CameraOffset;
			}
		}

		public override double[] CameraMatrixOrigin
		{
			get
			{
				return this.game.MainCamera.CameraMatrixOrigin;
			}
		}

		public override float[] CameraMatrixOriginf
		{
			get
			{
				return this.game.MainCamera.CameraMatrixOriginf;
			}
		}

		public override Vec3f AmbientColor
		{
			get
			{
				return this.game.AmbientManager.BlendedAmbientColor;
			}
		}

		public override Vec4f FogColor
		{
			get
			{
				return this.game.AmbientManager.BlendedFogColor;
			}
		}

		public override float FogMin
		{
			get
			{
				return this.game.AmbientManager.BlendedFogMin;
			}
		}

		public override float FogDensity
		{
			get
			{
				return this.game.AmbientManager.BlendedFogDensity;
			}
		}

		public override EnumCameraMode CameraType
		{
			get
			{
				return this.game.MainCamera.CameraMode;
			}
		}

		public override StackMatrix4 MvMatrix
		{
			get
			{
				return this.game.MvMatrix;
			}
		}

		public override StackMatrix4 PMatrix
		{
			get
			{
				return this.game.PMatrix;
			}
		}

		public override double[] PerspectiveViewMat
		{
			get
			{
				return this.game.PerspectiveViewMat;
			}
		}

		public override double[] PerspectiveProjectionMat
		{
			get
			{
				return this.game.PerspectiveProjectionMat;
			}
		}

		public override void GlLoadMatrix(double[] matrix)
		{
			this.game.GlLoadMatrix(matrix);
		}

		public override void GlMatrixModeModelView()
		{
			this.game.GlMatrixModeModelView();
		}

		public override void GlPopMatrix()
		{
			this.game.GlPopMatrix();
		}

		public override void GlPushMatrix()
		{
			this.game.GlPushMatrix();
		}

		public override void GlRotate(float angle, float x, float y, float z)
		{
			this.game.GlRotate(angle, (double)x, (double)y, (double)z);
		}

		public override void GlScale(float x, float y, float z)
		{
			this.game.GlScale((double)x, (double)y, (double)z);
		}

		public override void GlTranslate(float x, float y, float z)
		{
			this.game.GlTranslate((double)x, (double)y, (double)z);
		}

		public override void GlTranslate(double x, double y, double z)
		{
			this.game.GlTranslate(x, y, z);
		}

		public override float[] CurrentModelviewMatrix
		{
			get
			{
				return this.game.CurrentModelViewMatrix;
			}
		}

		public override float[] CurrentProjectionMatrix
		{
			get
			{
				return this.game.CurrentProjectionMatrix;
			}
		}

		public override void GetOrLoadTexture(AssetLocation name, ref LoadedTexture intoTexture)
		{
			this.game.GetOrLoadCachedTexture(name, ref intoTexture);
		}

		public override int GetOrLoadTexture(AssetLocation name)
		{
			return this.game.GetOrLoadCachedTexture(name);
		}

		public override void GetOrLoadTexture(AssetLocation name, BitmapRef bmp, ref LoadedTexture intoTexture)
		{
			this.game.GetOrLoadCachedTexture(name, bmp, ref intoTexture);
		}

		public override bool RemoveTexture(AssetLocation name)
		{
			return this.game.DeleteCachedTexture(name);
		}

		public override void Render2DTexture(int textureid, float x1, float y1, float width, float height, float z = 50f)
		{
			this.game.Render2DTexture(textureid, x1, y1, width, height, z, null);
		}

		public override ItemRenderInfo GetItemStackRenderInfo(ItemStack stack, EnumItemRenderTarget target, float dt)
		{
			return InventoryItemRenderer.GetItemStackRenderInfo(this.game, new DummySlot(stack), target, dt);
		}

		public override TextureAtlasPosition GetTextureAtlasPosition(ItemStack itemstack)
		{
			return InventoryItemRenderer.GetTextureAtlasPosition(this.game, itemstack);
		}

		public override ItemRenderInfo GetItemStackRenderInfo(ItemSlot inSlot, EnumItemRenderTarget target, float dt)
		{
			return InventoryItemRenderer.GetItemStackRenderInfo(this.game, inSlot, target, dt);
		}

		public override EnumRenderStage CurrentRenderStage
		{
			get
			{
				return this.game.currentRenderStage;
			}
		}

		public override float[] CurrentShadowProjectionMatrix
		{
			get
			{
				return this.game.shadowMvpMatrix;
			}
		}

		public override FrustumCulling DefaultFrustumCuller
		{
			get
			{
				return this.game.frustumCuller;
			}
		}

		public override IStandardShaderProgram PreparedStandardShader(int posX, int posY, int posZ, Vec4f colorMul = null)
		{
			Vec4f lightrgbs = this.game.WorldMap.GetLightRGBSVec4f(posX, posY, posZ);
			IStandardShaderProgram standardShader = base.StandardShader;
			standardShader.Use();
			standardShader.RgbaTint = ColorUtil.WhiteArgbVec;
			standardShader.RgbaAmbientIn = this.AmbientColor;
			standardShader.RgbaLightIn = ((colorMul == null) ? lightrgbs : lightrgbs.Mul(colorMul));
			standardShader.RgbaFogIn = this.FogColor;
			standardShader.NormalShaded = 1;
			standardShader.ExtraGlow = 0;
			standardShader.FogMinIn = this.FogMin;
			standardShader.FogDensityIn = this.FogDensity;
			standardShader.DontWarpVertices = 0;
			standardShader.AddRenderFlags = 0;
			standardShader.ExtraZOffset = 0f;
			standardShader.OverlayOpacity = 0f;
			standardShader.DamageEffect = 0f;
			standardShader.ExtraGodray = 0f;
			standardShader.ProjectionMatrix = this.CurrentProjectionMatrix;
			return standardShader;
		}

		public override void RenderTextureIntoTexture(LoadedTexture fromTexture, float sourceX, float sourceY, float sourceWidth, float sourceHeight, LoadedTexture intoTexture, float targetX, float targetY, float alphaTest = 0.005f)
		{
			this.game.RenderTextureIntoTexture(fromTexture, sourceX, sourceY, sourceWidth, sourceHeight, intoTexture, targetX, targetY, alphaTest);
		}

		public override Vec4f GetLightRGBs(int x, int y, int z)
		{
			return this.game.WorldMap.GetLightRGBSVec4f(x, y, z);
		}

		internal override void Dispose()
		{
			base.Dispose();
			this.inventoryItemRenderer.Dispose();
			ClientPlatformAbstract.DisposeIndexBuffer();
		}

		public override bool RenderItemStackToAtlas(ItemStack stack, ITextureAtlasAPI atlas, int size, Action<int> onComplete, int color = -1, float sepiaLevel = 0f, float scale = 1f)
		{
			return this.inventoryItemRenderer.RenderItemStackToAtlas(stack, atlas, size, onComplete, color, sepiaLevel, scale);
		}

		public override void RenderItemstackToGui(ItemStack itemstack, double posX, double posY, double posZ, float size, int color, bool shading = true, bool rotate = false, bool showStacksize = true)
		{
			this.inventoryItemRenderer.RenderItemstackToGui(new DummySlot(itemstack), posX, posY, posZ, size, color, shading, rotate, showStacksize);
		}

		public override void RenderItemstackToGui(ItemSlot inSlot, double posX, double posY, double posZ, float size, int color, float dt, bool shading = true, bool rotate = false, bool showStackSize = true)
		{
			this.inventoryItemRenderer.RenderItemstackToGui(inSlot, posX, posY, posZ, size, color, dt, shading, rotate, showStackSize);
		}

		public override void RenderItemstackToGui(ItemSlot inSlot, double posX, double posY, double posZ, float size, int color, bool shading = true, bool rotate = false, bool showStacksize = true)
		{
			this.inventoryItemRenderer.RenderItemstackToGui(inSlot, posX, posY, posZ, size, color, shading, rotate, showStacksize);
		}

		public override void RenderEntityToGui(float dt, Entity entity, double posX, double posY, double posZ, float yawDelta, float size, int color)
		{
			this.inventoryItemRenderer.RenderEntityToGui(dt, entity, posX, posY, posZ, yawDelta, size, color);
		}

		public override void Render2DTexture(int textureid, ElementBounds bounds, float z = 50f, Vec4f color = null)
		{
			this.Render2DTexture(textureid, (float)((int)bounds.renderX), (float)((int)bounds.renderY), (float)bounds.OuterWidthInt, (float)bounds.OuterHeightInt, z, color);
		}

		public override void Render2DTexture(MeshRef quadModel, int textureid, float x1, float y1, float width, float height, float z = 50f)
		{
			this.game.Render2DTexture(quadModel, textureid, x1, y1, width, height, z, null);
		}

		public override void Render2DTexture(MultiTextureMeshRef quadModel, float x1, float y1, float width, float height, float z = 50f)
		{
			this.game.Render2DTexture(quadModel, x1, y1, width, height, z, null);
		}

		public override void Render2DTexturePremultipliedAlpha(int textureid, ElementBounds bounds, float z = 50f, Vec4f color = null)
		{
			this.plat.GlToggleBlend(true, EnumBlendMode.PremultipliedAlpha);
			this.Render2DTexture(textureid, (float)((int)bounds.renderX), (float)((int)bounds.renderY), (float)bounds.OuterWidthInt, (float)bounds.OuterHeightInt, z, color);
			this.plat.GlToggleBlend(true, EnumBlendMode.Standard);
		}

		public override void Render2DTexturePremultipliedAlpha(int textureid, float x1, float y1, float width, float height, float z = 50f, Vec4f color = null)
		{
			this.plat.GlToggleBlend(true, EnumBlendMode.PremultipliedAlpha);
			this.game.Render2DTexture(textureid, (float)((int)x1), (float)((int)y1), width, height, z, color);
			this.plat.GlToggleBlend(true, EnumBlendMode.Standard);
		}

		public override void Render2DTexturePremultipliedAlpha(int textureid, double posX, double posY, double width, double height, float z = 50f, Vec4f color = null)
		{
			this.plat.GlToggleBlend(true, EnumBlendMode.PremultipliedAlpha);
			this.Render2DTexture(textureid, (float)((int)posX), (float)((int)posY), (float)width, (float)height, z, color);
			this.plat.GlToggleBlend(true, EnumBlendMode.Standard);
		}

		public override void Render2DTexture(int textureid, float x1, float y1, float width, float height, float z = 50f, Vec4f color = null)
		{
			this.game.Render2DTexture(textureid, x1, y1, width, height, z, color);
		}

		public override void RenderTexture(int textureid, double posX, double posY, double width, double height, float z = 50f, Vec4f color = null)
		{
			this.Render2DTexture(textureid, (float)((int)posX), (float)((int)posY), (float)width, (float)height, z, color);
		}

		public override FrameBufferRef CreateFrameBuffer(LoadedTexture intoTexture)
		{
			return this.game.CreateFrameBuffer(intoTexture);
		}

		public override void RenderTextureIntoFrameBuffer(int atlasTextureId, LoadedTexture fromTexture, float sourceX, float sourceY, float sourceWidth, float sourceHeight, FrameBufferRef fb, float targetX, float targetY, float alphaTest = 0.005f)
		{
			this.game.RenderTextureIntoFrameBuffer(atlasTextureId, fromTexture, sourceX, sourceY, sourceWidth, sourceHeight, fb, targetX, targetY, alphaTest);
		}

		public override void DestroyFrameBuffer(FrameBufferRef fb)
		{
			this.game.DestroyFrameBuffer(fb);
		}

		public override void RenderRectangle(float posX, float posY, float posZ, float width, float height, int color)
		{
			MeshRef modelRef = this.whiteRectangleRef;
			Vec4f vec = new Vec4f();
			this.game.guiShaderProg.RgbaIn = ColorUtil.ToRGBAVec4f(color, ref vec);
			this.game.guiShaderProg.ExtraGlow = 0;
			this.game.guiShaderProg.ApplyColor = 0;
			this.game.guiShaderProg.Tex2d2D = 0;
			this.game.guiShaderProg.NoTexture = 1f;
			this.game.guiShaderProg.OverlayOpacity = 0f;
			this.game.GlPushMatrix();
			this.game.GlTranslate((double)posX, (double)posY, (double)posZ);
			this.game.GlScale((double)width, (double)height, 0.0);
			this.game.GlScale(0.5, 0.5, 0.0);
			this.game.GlTranslate(1.0, 1.0, 0.0);
			this.plat.GLLineWidth(1f);
			this.plat.SmoothLines(false);
			this.plat.GlToggleBlend(true, EnumBlendMode.Standard);
			this.game.guiShaderProg.ProjectionMatrix = this.game.CurrentProjectionMatrix;
			this.game.guiShaderProg.ModelViewMatrix = this.game.CurrentModelViewMatrix;
			this.plat.RenderMesh(modelRef);
			this.game.GlPopMatrix();
			this.game.guiShaderProg.NoTexture = 0f;
		}

		public override void RenderLine(BlockPos origin, float posX1, float posY1, float posZ1, float posX2, float posY2, float posZ2, int color)
		{
			MeshData mesh = new MeshData(4, 4, false, false, true, true);
			mesh.SetMode(EnumDrawMode.LineStrip);
			int vertexIndex = 0;
			mesh.AddVertexSkipTex(posX1, posY1, posZ1, color);
			mesh.AddIndex(vertexIndex++);
			mesh.AddVertexSkipTex(posX2, posY2, posZ2, color);
			mesh.AddIndex(vertexIndex++);
			MeshRef meshref = this.game.api.renderapi.UploadMesh(mesh);
			ShaderProgramAutocamera autocamera = ShaderPrograms.Autocamera;
			autocamera.Use();
			this.game.Platform.GLLineWidth(2f);
			this.game.Platform.BindTexture2d(0);
			this.game.GlPushMatrix();
			this.game.GlLoadMatrix(this.game.MainCamera.CameraMatrixOrigin);
			Vec3d cameraPos = this.game.EntityPlayer.CameraPos;
			this.game.GlTranslate((double)((float)((double)origin.X - cameraPos.X)), (double)((float)((double)origin.Y - cameraPos.Y)), (double)((float)((double)origin.Z - cameraPos.Z)));
			autocamera.ProjectionMatrix = this.game.CurrentProjectionMatrix;
			autocamera.ModelViewMatrix = this.game.CurrentModelViewMatrix;
			this.plat.RenderMesh(meshref);
			autocamera.Stop();
			meshref.Dispose();
			this.game.GlPopMatrix();
		}

		public override void Render2DLoadedTexture(LoadedTexture textTexture, float posX, float posY, float z = 50f)
		{
			this.game.Render2DLoadedTexture(textTexture, posX, posY, z, null);
		}

		public override void AddPointLight(IPointLight pointlight)
		{
			this.game.pointlights.Add(pointlight);
		}

		public override void RemovePointLight(IPointLight pointlight)
		{
			this.game.pointlights.Remove(pointlight);
		}

		public override void Reset3DProjection()
		{
			this.game.Reset3DProjection();
		}

		public override void Set3DProjection(float zfar, float fov)
		{
			this.game.Set3DProjection(zfar, fov);
		}

		private ClientMain game;

		internal InventoryItemRenderer inventoryItemRenderer;

		internal PerceptionEffects perceptionEffects;
	}
}
