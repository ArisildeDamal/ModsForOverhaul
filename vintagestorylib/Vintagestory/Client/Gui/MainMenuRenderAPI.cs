using System;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace Vintagestory.Client.Gui
{
	public class MainMenuRenderAPI : RenderAPIBase
	{
		public MainMenuRenderAPI(ScreenManager screenManager)
			: base(screenManager.GamePlatform)
		{
			this.screenManager = screenManager;
			this.mvMatrix = Mat4f.Create();
			this.pMatrix = Mat4f.Create();
			this.useSSBOs = false;
		}

		public override void GlTranslate(double x, double y, double z)
		{
			Mat4f.Translate(this.mvMatrix, this.mvMatrix, new float[]
			{
				(float)x,
				(float)y,
				(float)z
			});
		}

		public override void GlTranslate(float x, float y, float z)
		{
			Mat4f.Translate(this.mvMatrix, this.mvMatrix, new float[] { x, y, z });
		}

		public override ICoreClientAPI Api
		{
			get
			{
				return this.screenManager.api;
			}
		}

		public override void Render2DTexture(int textureid, ElementBounds bounds, float z = 50f, Vec4f color = null)
		{
			this.Render2DTexture(textureid, (float)bounds.renderX, (float)bounds.renderY, (float)bounds.OuterWidth, (float)bounds.OuterHeight, z, color);
		}

		public override void Render2DTexturePremultipliedAlpha(int textureid, float posX, float posY, float width, float height, float z = 50f, Vec4f color = null)
		{
			this.plat.GlToggleBlend(true, EnumBlendMode.PremultipliedAlpha);
			this.Render2DTexture(textureid, posX, posY, width, height, z, color);
			this.plat.GlToggleBlend(true, EnumBlendMode.Standard);
		}

		public override void Render2DTexturePremultipliedAlpha(int textureid, ElementBounds bounds, float z = 50f, Vec4f color = null)
		{
			this.plat.GlToggleBlend(true, EnumBlendMode.PremultipliedAlpha);
			this.Render2DTexture(textureid, (float)((int)bounds.renderX), (float)((int)bounds.renderY), (float)((int)bounds.OuterWidth), (float)((int)bounds.OuterHeight), z, color);
			this.plat.GlToggleBlend(true, EnumBlendMode.Standard);
		}

		public override void RenderTexture(int textureid, double posX, double posY, double width, double height, float z = 50f, Vec4f color = null)
		{
			this.Render2DTexture(textureid, (float)posX, (float)posY, (float)width, (float)height, z, color);
		}

		public override void Render2DTexturePremultipliedAlpha(int textureid, double posX, double posY, double width, double height, float z = 50f, Vec4f color = null)
		{
			this.plat.GlToggleBlend(true, EnumBlendMode.PremultipliedAlpha);
			this.Render2DTexture(textureid, (float)posX, (float)posY, (float)width, (float)height, z, color);
			this.plat.GlToggleBlend(true, EnumBlendMode.Standard);
		}

		public override void Render2DTexture(int textureid, float x1, float y1, float width, float height, float z = 50f)
		{
			this.Render2DTexture(textureid, x1, y1, width, height, z, null);
		}

		public override void Render2DTexture(int textureid, float posX, float posY, float width, float height, float z = 50f, Vec4f color = null)
		{
			ShaderProgramGui guiShaderProg = ShaderPrograms.Gui;
			if (this.quadModel == null)
			{
				this.quadModel = this.plat.UploadMesh(QuadMeshUtilExt.GetQuadModelData());
			}
			if (guiShaderProg != null)
			{
				guiShaderProg.Use();
				guiShaderProg.RgbaIn = ((color == null) ? ColorUtil.WhiteArgbVec : color);
				guiShaderProg.ExtraGlow = 0;
				guiShaderProg.ApplyColor = ((color != null) ? 1 : 0);
				guiShaderProg.Tex2d2D = textureid;
				guiShaderProg.NoTexture = 0f;
				guiShaderProg.OverlayOpacity = 0f;
				guiShaderProg.DarkEdges = 0;
				guiShaderProg.NormalShaded = 0;
				guiShaderProg.DamageEffect = 0f;
				Mat4f.Identity(this.mvMatrix);
				Mat4f.Translate(this.mvMatrix, this.mvMatrix, Vec3Utilsf.FromValues(posX, posY, z - 20000f + 151f));
				Mat4f.Scale(this.mvMatrix, this.mvMatrix, Vec3Utilsf.FromValues(width, height, 0f));
				Mat4f.Scale(this.mvMatrix, this.mvMatrix, Vec3Utilsf.FromValues(0.5f, 0.5f, 0f));
				Mat4f.Translate(this.mvMatrix, this.mvMatrix, Vec3Utilsf.FromValues(1f, 1f, 0f));
				guiShaderProg.ProjectionMatrix = this.pMatrix;
				guiShaderProg.ModelViewMatrix = this.mvMatrix;
				this.plat.BindTexture2d(textureid);
				this.plat.RenderMesh(this.quadModel);
				guiShaderProg.Stop();
				return;
			}
			ShaderProgramMinimalGui minimalGuiShader = this.plat.MinimalGuiShader;
			minimalGuiShader.Use();
			minimalGuiShader.Tex2d2D = textureid;
			Mat4f.Identity(this.mvMatrix);
			Mat4f.Translate(this.mvMatrix, this.mvMatrix, Vec3Utilsf.FromValues(posX, posY, 0f));
			Mat4f.Scale(this.mvMatrix, this.mvMatrix, Vec3Utilsf.FromValues(width, height, 0f));
			Mat4f.Scale(this.mvMatrix, this.mvMatrix, Vec3Utilsf.FromValues(0.5f, 0.5f, 0f));
			Mat4f.Translate(this.mvMatrix, this.mvMatrix, Vec3Utilsf.FromValues(1f, 1f, z));
			minimalGuiShader.ProjectionMatrix = this.pMatrix;
			minimalGuiShader.ModelViewMatrix = this.mvMatrix;
			this.plat.BindTexture2d(textureid);
			this.plat.RenderMesh(this.quadModel);
			minimalGuiShader.Stop();
		}

		public void Draw2DShadedEdges(float posX, float posY, float width, float height, float z = 50f)
		{
			ShaderProgramGui gui = ShaderPrograms.Gui;
			if (this.quadModel == null)
			{
				this.quadModel = this.plat.UploadMesh(QuadMeshUtilExt.GetQuadModelData());
			}
			gui.Use();
			gui.RgbaIn = ColorUtil.WhiteArgbVec;
			gui.ExtraGlow = 0;
			gui.ApplyColor = 0;
			gui.Tex2d2D = 0;
			gui.NoTexture = 0f;
			gui.OverlayOpacity = 0f;
			gui.DarkEdges = 1;
			gui.NormalShaded = 0;
			Mat4f.Identity(this.mvMatrix);
			Mat4f.Translate(this.mvMatrix, this.mvMatrix, Vec3Utilsf.FromValues(posX, posY, 0f));
			Mat4f.Scale(this.mvMatrix, this.mvMatrix, Vec3Utilsf.FromValues(width, height, 0f));
			Mat4f.Scale(this.mvMatrix, this.mvMatrix, Vec3Utilsf.FromValues(0.5f, 0.5f, 0f));
			Mat4f.Translate(this.mvMatrix, this.mvMatrix, Vec3Utilsf.FromValues(1f, 1f, z));
			gui.ProjectionMatrix = this.pMatrix;
			gui.ModelViewMatrix = this.mvMatrix;
			this.plat.RenderMesh(this.quadModel);
			gui.Stop();
		}

		public override void Render2DLoadedTexture(LoadedTexture textTexture, float posX, float posY, float z = 50f)
		{
			this.Render2DTexture(textTexture.TextureId, posX, posY, (float)textTexture.Width, (float)textTexture.Height, z);
		}

		public override void RenderRectangle(float posX, float posY, float posZ, float width, float height, int color)
		{
			ShaderProgramGui gui = ShaderPrograms.Gui;
			gui.Use();
			if (this.whiteRectangleRef == null)
			{
				MeshData mesh = LineMeshUtil.GetRectangle(-1);
				this.whiteRectangleRef = this.plat.UploadMesh(mesh);
			}
			MeshRef modelRef = this.whiteRectangleRef;
			Vec4f vec = new Vec4f();
			gui.RgbaIn = ColorUtil.ToRGBAVec4f(color, ref vec);
			gui.ExtraGlow = 0;
			gui.ApplyColor = 0;
			gui.Tex2d2D = 0;
			gui.NoTexture = 1f;
			gui.OverlayOpacity = 0f;
			gui.DarkEdges = 0;
			gui.NormalShaded = 0;
			Mat4f.Identity(this.mvMatrix);
			Mat4f.Translate(this.mvMatrix, this.mvMatrix, Vec3Utilsf.FromValues(posX, posY, 0f));
			Mat4f.Scale(this.mvMatrix, this.mvMatrix, Vec3Utilsf.FromValues(width, height, 0f));
			Mat4f.Scale(this.mvMatrix, this.mvMatrix, Vec3Utilsf.FromValues(0.5f, 0.5f, 0f));
			Mat4f.Translate(this.mvMatrix, this.mvMatrix, Vec3Utilsf.FromValues(1f, 1f, posZ));
			gui.ProjectionMatrix = this.pMatrix;
			gui.ModelViewMatrix = this.mvMatrix;
			this.plat.GLLineWidth(1f);
			this.plat.SmoothLines(false);
			this.plat.RenderMesh(modelRef);
			gui.Stop();
		}

		private float[] mvMatrix;

		public float[] pMatrix;

		private MeshRef quadModel;

		private ScreenManager screenManager;
	}
}
