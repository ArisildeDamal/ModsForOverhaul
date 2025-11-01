using System;
using System.Collections.Generic;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace Vintagestory.Client
{
	public abstract class RenderAPIBase : IRenderAPI
	{
		public abstract ICoreClientAPI Api { get; }

		public bool UseSSBOs
		{
			get
			{
				return this.useSSBOs;
			}
		}

		public RenderAPIBase(ClientPlatformAbstract plat)
		{
			this.plat = plat;
			MeshData rectangle = LineMeshUtil.GetRectangle(-1);
			this.whiteRectangleRef = plat.UploadMesh(rectangle);
		}

		public BitmapExternal BitmapCreateFromPng(byte[] pngdata)
		{
			return (BitmapExternal)this.plat.CreateBitmapFromPng(pngdata, pngdata.Length);
		}

		public string DecorativeFontName
		{
			get
			{
				return GuiStyle.DecorativeFontName;
			}
		}

		public string StandardFontName
		{
			get
			{
				return GuiStyle.StandardFontName;
			}
		}

		public int FrameWidth
		{
			get
			{
				return this.plat.WindowSize.Width;
			}
		}

		public int FrameHeight
		{
			get
			{
				return this.plat.WindowSize.Height;
			}
		}

		public float LineWidth
		{
			set
			{
				this.plat.GLLineWidth(value);
			}
		}

		public void CheckGlError(string message = "")
		{
			this.plat.CheckGlError(message);
		}

		public string GlGetError()
		{
			return this.plat.GlGetError();
		}

		public void BindTexture2d(int textureid)
		{
			this.plat.BindTexture2d(textureid);
		}

		public MeshRef UploadMesh(MeshData data)
		{
			return this.plat.UploadMesh(data);
		}

		public MultiTextureMeshRef UploadMultiTextureMesh(MeshData data)
		{
			MeshData[] meshes = data.SplitByTextureId();
			MeshRef[] meshrefs = new MeshRef[meshes.Length];
			for (int i = 0; i < meshrefs.Length; i++)
			{
				meshrefs[i] = this.plat.UploadMesh(meshes[i]);
			}
			return new MultiTextureMeshRef(meshrefs, data.TextureIds);
		}

		public void DeleteMesh(MeshRef meshref)
		{
			this.plat.DeleteMesh(meshref);
		}

		public void RenderMeshInstanced(MeshRef meshRef, int quantity = 1)
		{
			this.plat.RenderMeshInstanced(meshRef, quantity);
		}

		public void RenderMesh(MeshRef meshRef)
		{
			this.plat.RenderMesh(meshRef);
		}

		public void RenderMultiTextureMesh(MultiTextureMeshRef mmr, string textureSampleName, int textureNumber = 0)
		{
			for (int i = 0; i < mmr.meshrefs.Length; i++)
			{
				MeshRef j = mmr.meshrefs[i];
				this.CurrentActiveShader.BindTexture2D(textureSampleName, mmr.textureids[i], textureNumber);
				this.plat.RenderMesh(j);
			}
		}

		public void RenderMesh(MeshRef meshRef, int[] indicesStarts, int[] indicesSizes, int groupCount)
		{
			this.plat.RenderMesh(meshRef, indicesStarts, indicesSizes, groupCount, this.useSSBOs);
		}

		public CairoFont GetFont(double unscaledFontSize, string fontName, double[] color, double[] strokeColor = null)
		{
			return new CairoFont(unscaledFontSize, fontName, color, strokeColor);
		}

		public int GetUniformLocation(int shaderProgramNumber, string name)
		{
			return this.plat.GetUniformLocation(ShaderRegistry.getProgram((EnumShaderProgram)shaderProgramNumber), name);
		}

		public void GLDeleteTexture(int textureId)
		{
			this.plat.GLDeleteTexture(textureId);
		}

		public void GlDisableCullFace()
		{
			this.plat.GlDisableCullFace();
		}

		public void GlGenerateTex2DMipmaps()
		{
			this.plat.GlGenerateTex2DMipmaps();
		}

		public void GlToggleBlend(bool blend, EnumBlendMode blendMode = EnumBlendMode.Standard)
		{
			this.plat.GlToggleBlend(blend, blendMode);
		}

		public void UpdateMesh(MeshRef meshRef, MeshData updatedata)
		{
			this.plat.UpdateMesh(meshRef, updatedata);
		}

		public void UpdateChunkMesh(MeshRef meshRef, MeshData updatedata)
		{
			if (this.useSSBOs && (updatedata.CustomInts == null || updatedata.CustomInts.InterleaveStride < 8))
			{
				this.plat.UpdateSSBOMesh(meshRef, updatedata);
				return;
			}
			this.plat.UpdateMesh(meshRef, updatedata);
		}

		public MeshRef AllocateEmptyMesh(int xyzSize, int normalsSize, int uvSize, int rgbaSize, int flagsSize, int indicesSize, CustomMeshDataPartFloat customFloats, CustomMeshDataPartShort customShorts, CustomMeshDataPartByte customBytes, CustomMeshDataPartInt customInts, EnumDrawMode drawMode = EnumDrawMode.Triangles, bool staticDraw = true)
		{
			if (this.useSSBOs && (customInts == null || customInts.InterleaveStride < 8))
			{
				return this.plat.AllocateEmptySSBOMesh(xyzSize, normalsSize, uvSize, rgbaSize, flagsSize, indicesSize, customFloats, customShorts, customBytes, customInts, drawMode, staticDraw);
			}
			return this.plat.AllocateEmptyMesh(xyzSize, normalsSize, uvSize, rgbaSize, flagsSize, indicesSize, customFloats, customShorts, customBytes, customInts, drawMode, staticDraw);
		}

		public void GlEnableCullFace()
		{
			this.plat.GlEnableCullFace();
		}

		public void GLDisableDepthTest()
		{
			this.plat.GlDisableDepthTest();
		}

		public void GLEnableDepthTest()
		{
			this.plat.GlEnableDepthTest();
		}

		public IShaderProgram GetEngineShader(EnumShaderProgram program)
		{
			return ShaderRegistry.getProgram(program);
		}

		public IShaderProgram GetShader(int shaderProgramNumber)
		{
			return ShaderRegistry.getProgram(shaderProgramNumber);
		}

		public IStandardShaderProgram StandardShader
		{
			get
			{
				return (IStandardShaderProgram)ShaderRegistry.getProgram(EnumShaderProgram.Standard);
			}
		}

		public IShaderProgram CurrentActiveShader
		{
			get
			{
				return ShaderProgramBase.CurrentShaderProgram;
			}
		}

		public Stack<ElementBounds> ScissorStack
		{
			get
			{
				return this.scissorBoundsStacks;
			}
		}

		public void PushScissor(ElementBounds bounds, bool stacking = false)
		{
			if (bounds == null)
			{
				this.plat.GlScissorFlag(false);
			}
			else
			{
				if (stacking && this.scissorBoundsStacks.Count > 0)
				{
					ElementBounds prevbounds = this.scissorBoundsStacks.Peek();
					double prevx = prevbounds.renderX;
					double prevy = prevbounds.renderY;
					double prevx2 = prevx + prevbounds.InnerWidth;
					double prevy2 = prevy + prevbounds.InnerHeight;
					double x = bounds.renderX;
					double y = bounds.renderY;
					double num = x + bounds.InnerWidth;
					double y2 = y + bounds.InnerHeight;
					int x2 = (int)Math.Max(x, prevx);
					int y3 = (int)((double)this.plat.WindowSize.Height - Math.Max(y, prevy) - (Math.Min(y2, prevy2) - Math.Max(y, prevy)));
					int w = (int)(Math.Min(num, prevx2) - Math.Max(x, prevx));
					int h = (int)(Math.Min(y2, prevy2) - Math.Max(y, prevy));
					this.plat.GlScissor(x2, y3, Math.Max(0, w), Math.Max(0, h));
				}
				else
				{
					this.plat.GlScissor((int)bounds.renderX, (int)((double)this.plat.WindowSize.Height - bounds.renderY - bounds.InnerHeight), (int)bounds.InnerWidth, (int)bounds.InnerHeight);
				}
				this.plat.GlScissorFlag(true);
			}
			this.scissorBoundsStacks.Push(bounds);
		}

		public void PopScissor()
		{
			this.scissorBoundsStacks.Pop();
			if (this.scissorBoundsStacks.Count <= 0)
			{
				this.plat.GlScissorFlag(false);
				return;
			}
			ElementBounds bounds = this.scissorBoundsStacks.Peek();
			if (bounds == null)
			{
				this.plat.GlScissorFlag(false);
				return;
			}
			this.plat.GlScissor((int)bounds.renderX, (int)((double)this.plat.WindowSize.Height - bounds.renderY - bounds.InnerHeight), (int)bounds.InnerWidth, (int)bounds.InnerHeight);
			this.plat.GlScissorFlag(true);
		}

		public void GlScissor(int x, int y, int width, int height)
		{
			this.plat.GlScissor(x, y, width, height);
		}

		public void GlScissorFlag(bool enable)
		{
			this.plat.GlScissorFlag(enable);
		}

		public int GlGetMaxTextureSize()
		{
			return this.plat.GlGetMaxTextureSize();
		}

		public virtual int LoadCairoTexture(ImageSurface surface, bool linearMag)
		{
			return this.plat.LoadCairoTexture(surface, linearMag);
		}

		public int LoadTextureFromBgra(int[] bgraPixels, int width, int height, bool linearMag, int clampMode)
		{
			LoadedTexture tex = new LoadedTexture(null, 0, width, height);
			this.plat.LoadOrUpdateTextureFromBgra(bgraPixels, linearMag, clampMode, ref tex);
			tex.TextureId = 0;
			return tex.TextureId;
		}

		public int LoadTextureFromRgba(int[] rgbaPixels, int width, int height, bool linearMag, int clampMode)
		{
			LoadedTexture tex = new LoadedTexture(null, 0, width, height);
			this.plat.LoadOrUpdateTextureFromRgba(rgbaPixels, linearMag, clampMode, ref tex);
			tex.TextureId = 0;
			return tex.TextureId;
		}

		public void LoadOrUpdateTextureFromBgra(int[] bgraPixels, bool linearMag, int clampMode, ref LoadedTexture intoTexture)
		{
			this.plat.LoadOrUpdateTextureFromBgra(bgraPixels, linearMag, clampMode, ref intoTexture);
		}

		public void LoadOrUpdateTextureFromRgba(int[] rgbaPixels, bool linearMag, int clampMode, ref LoadedTexture intoTexture)
		{
			this.plat.LoadOrUpdateTextureFromRgba(rgbaPixels, linearMag, clampMode, ref intoTexture);
		}

		internal virtual void Dispose()
		{
			MeshRef meshRef = this.whiteRectangleRef;
			if (meshRef == null)
			{
				return;
			}
			meshRef.Dispose();
		}

		public virtual EnumRenderStage CurrentRenderStage
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public virtual double[] PerspectiveViewMat
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public virtual double[] PerspectiveProjectionMat
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public virtual EnumCameraMode CameraType
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public virtual StackMatrix4 MvMatrix
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public virtual StackMatrix4 PMatrix
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public virtual float[] CurrentModelviewMatrix
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public virtual double[] CameraMatrixOrigin
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public virtual float[] CameraMatrixOriginf
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public virtual float[] CurrentProjectionMatrix
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public virtual float[] CurrentShadowProjectionMatrix
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public virtual Vec3f AmbientColor
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public virtual Vec4f FogColor
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public virtual float FogMin
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public virtual float FogDensity
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public virtual ModelTransform CameraOffset
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public virtual DefaultShaderUniforms ShaderUniforms
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public List<FrameBufferRef> FrameBuffers
		{
			get
			{
				return ScreenManager.Platform.FrameBuffers;
			}
		}

		public virtual int TextureSize
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public virtual FrustumCulling DefaultFrustumCuller
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public virtual PerceptionEffects PerceptionEffects
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public FrameBufferRef FrameBuffer
		{
			set
			{
				ScreenManager.Platform.LoadFrameBuffer(value);
			}
		}

		public WireframeModes WireframeDebugRender { get; set; } = new WireframeModes();

		public bool CameraStuck { get; set; }

		public virtual ItemRenderInfo GetItemStackRenderInfo(ItemStack itemstack, EnumItemRenderTarget ground, float dt)
		{
			throw new NotImplementedException();
		}

		public virtual ItemRenderInfo GetItemStackRenderInfo(ItemSlot inSlot, EnumItemRenderTarget ground, float dt)
		{
			throw new NotImplementedException();
		}

		public virtual void GlMatrixModeModelView()
		{
			throw new NotImplementedException();
		}

		public virtual void GlPushMatrix()
		{
		}

		public virtual void GlPopMatrix()
		{
		}

		public virtual void GlLoadMatrix(double[] matrix)
		{
			throw new NotImplementedException();
		}

		public virtual void GlTranslate(float x, float y, float z)
		{
		}

		public virtual void GlTranslate(double x, double y, double z)
		{
			throw new NotImplementedException();
		}

		public virtual void GlScale(float x, float y, float z)
		{
			throw new NotImplementedException();
		}

		public virtual void GlRotate(float angle, float x, float y, float z)
		{
			throw new NotImplementedException();
		}

		public virtual Vec4f GetLightRGBs(int x, int y, int z)
		{
			throw new NotImplementedException();
		}

		public virtual bool RemoveTexture(AssetLocation name)
		{
			throw new NotImplementedException();
		}

		public virtual IStandardShaderProgram PreparedStandardShader(int posX, int posY, int posZ, Vec4f colorMul = null)
		{
			throw new NotImplementedException();
		}

		public virtual void RenderTextureIntoTexture(LoadedTexture fromTexture, float sourceX, float sourceY, float sourceWidth, float sourceHeight, LoadedTexture intoTexture, float targetX, float targetY, float alphaTest = 0.005f)
		{
			throw new NotImplementedException();
		}

		public virtual void RenderItemstackToGui(ItemStack itemstack, double posX, double posY, double posZ, float size, int color, bool shading = true, bool rotate = false, bool showStackSize = true)
		{
			throw new NotImplementedException();
		}

		public virtual void RenderItemstackToGui(ItemSlot inSlot, double posX, double posY, double posZ, float size, int color, bool shading = true, bool rotate = false, bool showStackSize = true)
		{
			throw new NotImplementedException();
		}

		public virtual void RenderItemstackToGui(ItemSlot inSlot, double posX, double posY, double posZ, float size, int color, float dt, bool shading = true, bool rotate = false, bool showStackSize = true)
		{
			throw new NotImplementedException();
		}

		public virtual void Render2DTexture(int textureid, float x1, float y1, float width, float height, float z = 50f)
		{
			throw new NotImplementedException();
		}

		public virtual void Render2DTexture(MeshRef quadModel, int textureid, float x1, float y1, float width, float height, float z = 50f)
		{
			throw new NotImplementedException();
		}

		public virtual void Render2DTexture(MultiTextureMeshRef quadModel, float x1, float y1, float width, float height, float z = 50f)
		{
			throw new NotImplementedException();
		}

		public virtual void Render2DTexturePremultipliedAlpha(int textureid, float posX, float posY, float width, float height, float z = 50f, Vec4f color = null)
		{
			throw new NotImplementedException();
		}

		public virtual void Render2DTexturePremultipliedAlpha(int textureid, double posX, double posY, double width, double height, float z = 50f, Vec4f color = null)
		{
			throw new NotImplementedException();
		}

		public virtual void Render2DTexturePremultipliedAlpha(int textureid, ElementBounds bounds, float z = 50f, Vec4f color = null)
		{
			throw new NotImplementedException();
		}

		public virtual void RenderTexture(int textureid, double posX, double posY, double width, double height, float z = 50f, Vec4f color = null)
		{
			throw new NotImplementedException();
		}

		public virtual void Render2DTexture(int textureid, float posX, float posY, float width, float height, float z = 50f, Vec4f color = null)
		{
			throw new NotImplementedException();
		}

		public virtual void Render2DTexture(int textureid, ElementBounds bounds, float z = 50f, Vec4f color = null)
		{
			throw new NotImplementedException();
		}

		public virtual void Render2DLoadedTexture(LoadedTexture textTexture, float posX, float posY, float z = 50f)
		{
			throw new NotImplementedException();
		}

		public virtual void RenderRectangle(float posX, float posY, float posZ, float width, float height, int color)
		{
			throw new NotImplementedException();
		}

		public virtual void RenderEntityToGui(float dt, Entity entity, double posX, double posY, double posZ, float yawDelta, float size, int color)
		{
			throw new NotImplementedException();
		}

		public void GLDepthMask(bool on)
		{
			this.plat.GlDepthMask(on);
		}

		public virtual void GetOrLoadTexture(AssetLocation name, ref LoadedTexture intoTexture)
		{
			throw new NotImplementedException();
		}

		public virtual void GetOrLoadTexture(AssetLocation name, BitmapRef bmp, ref LoadedTexture intoTexture)
		{
			throw new NotImplementedException();
		}

		public virtual int GetOrLoadTexture(AssetLocation name)
		{
			throw new NotImplementedException();
		}

		public virtual TextureAtlasPosition GetTextureAtlasPosition(ItemStack itemstack)
		{
			throw new NotImplementedException();
		}

		public virtual void RenderLine(BlockPos origin, float posX1, float posY1, float posZ1, float posX2, float posY2, float posZ2, int color)
		{
			throw new NotImplementedException();
		}

		public virtual FrameBufferRef CreateFrameBuffer(LoadedTexture intoTexture)
		{
			throw new NotImplementedException();
		}

		public virtual void RenderTextureIntoFrameBuffer(int atlasTextureId, LoadedTexture fromTexture, float sourceX, float sourceY, float sourceWidth, float sourceHeight, FrameBufferRef fb, float targetX, float targetY, float alphaTest = 0.005f)
		{
			throw new NotImplementedException();
		}

		public virtual void DestroyFrameBuffer(FrameBufferRef fb)
		{
			throw new NotImplementedException();
		}

		public virtual bool RenderItemStackToAtlas(ItemStack stack, ITextureAtlasAPI atlas, int size, Action<int> onComplete, int color = -1, float sepiaLevel = 0f, float scale = 1f)
		{
			throw new NotImplementedException();
		}

		public virtual void AddPointLight(IPointLight pointlight)
		{
			throw new NotImplementedException();
		}

		public virtual void RemovePointLight(IPointLight pointlight)
		{
			throw new NotImplementedException();
		}

		public void GlViewport(int x, int y, int width, int height)
		{
			this.plat.GlViewport(x, y, width, height);
		}

		public void LoadTexture(IBitmap bmp, ref LoadedTexture intoTexture, bool linearMag = false, int clampMode = 0, bool generateMipmaps = false)
		{
			int textureId = this.plat.LoadTexture(bmp, linearMag, clampMode, generateMipmaps);
			if (textureId != intoTexture.TextureId && intoTexture.TextureId != 0)
			{
				intoTexture.Dispose();
			}
			intoTexture.TextureId = textureId;
			intoTexture.Width = bmp.Width;
			intoTexture.Height = bmp.Height;
		}

		public virtual void Reset3DProjection()
		{
			throw new NotImplementedException();
		}

		public virtual void Set3DProjection(float zfar, float fov)
		{
			throw new NotImplementedException();
		}

		public UBORef CreateUBO(IShaderProgram shaderProgram, int bindingPoint, string blockName, int size)
		{
			return this.plat.CreateUBO((shaderProgram as ShaderProgramBase).ProgramId, bindingPoint, blockName, size);
		}

		internal ClientPlatformAbstract plat;

		internal MeshRef whiteRectangleRef;

		internal bool useSSBOs = true;

		private Stack<ElementBounds> scissorBoundsStacks = new Stack<ElementBounds>();
	}
}
