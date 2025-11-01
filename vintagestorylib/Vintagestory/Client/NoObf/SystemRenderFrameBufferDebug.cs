using System;
using OpenTK.Graphics.OpenGL;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.Client.NoObf
{
	internal class SystemRenderFrameBufferDebug : ClientSystem
	{
		public override string Name
		{
			get
			{
				return "debwt";
			}
		}

		public SystemRenderFrameBufferDebug(ClientMain game)
			: base(game)
		{
			game.api.ChatCommands.GetOrCreate("debug").BeginSubCommand("fbdeb").WithDescription("Toggle Framebuffer/WOIT Debug mode")
				.HandleWith(new OnCommandDelegate(this.CmdWoit))
				.EndSubCommand();
			float sc = RuntimeEnv.GUIScale;
			MeshData redQuad = QuadMeshUtilExt.GetCustomQuadModelData(0f, 0f, 0f, sc * 10f, sc * 10f, 220, 0, 0, 191, 0);
			MeshData yellowQuad = QuadMeshUtilExt.GetCustomQuadModelData(sc * 2f, sc * 2f, sc * 2f, sc * 10f, sc * 10f, 220, 220, 0, 191, 0);
			MeshData blueQuad = QuadMeshUtilExt.GetCustomQuadModelData(sc * 4f, sc * 4f, sc * 4f, sc * 10f, sc * 10f, 0, 0, 220, 191, 0);
			MeshData quads = new MeshData(12, 12, false, true, true, true);
			quads.AddMeshData(redQuad);
			quads.AddMeshData(yellowQuad);
			quads.AddMeshData(blueQuad);
			quads.Uv = null;
			this.coloredPlanesRef = game.Platform.UploadMesh(quads);
			this.quadModel = game.Platform.UploadMesh(QuadMeshUtilExt.GetQuadModelData());
			game.eventManager.RegisterRenderer(new Action<float>(this.OnRenderFrame3DTransparent), EnumRenderStage.OIT, "debwt-oit", 0.2);
			game.eventManager.RegisterRenderer(new Action<float>(this.OnRenderFrame2DOverlay), EnumRenderStage.Ortho, "debwt-ortho", 0.2);
			CairoFont font = CairoFont.WhiteDetailText();
			TextBackground bg = new TextBackground
			{
				FillColor = new double[] { 0.2, 0.2, 0.2, 0.3 },
				Padding = (int)(sc * 2f)
			};
			this.labels = new LoadedTexture[]
			{
				game.api.Gui.TextTexture.GenUnscaledTextTexture("Shadow Map Far", font, bg),
				game.api.Gui.TextTexture.GenUnscaledTextTexture("WOIT Accum", font, bg),
				game.api.Gui.TextTexture.GenUnscaledTextTexture("WOIT Reveal", font, bg),
				game.api.Gui.TextTexture.GenUnscaledTextTexture("Findbright (A.Bloom)", font, bg),
				game.api.Gui.TextTexture.GenUnscaledTextTexture("Primary FB Color", font, bg),
				game.api.Gui.TextTexture.GenUnscaledTextTexture("Primary FB Depth", font, bg),
				game.api.Gui.TextTexture.GenUnscaledTextTexture("Primary FB Depthlinear", font, bg),
				game.api.Gui.TextTexture.GenUnscaledTextTexture("Luma", font, bg),
				game.api.Gui.TextTexture.GenUnscaledTextTexture("Glow (red=bloom,green=godray)", font, bg),
				game.api.Gui.TextTexture.GenUnscaledTextTexture("Shadow Map Near", font, bg),
				game.api.Gui.TextTexture.GenUnscaledTextTexture("Primary FB GNormal", font, bg),
				game.api.Gui.TextTexture.GenUnscaledTextTexture("Primary FB GPosition", font, bg),
				game.api.Gui.TextTexture.GenUnscaledTextTexture("SSAO", font, bg),
				game.api.Gui.TextTexture.GenUnscaledTextTexture("Liquid depth", font, bg)
			};
		}

		public void OnRenderFrame3DTransparent(float deltaTime)
		{
			if (this.framebufferDebug)
			{
				this.game.Platform.GlDisableDepthTest();
				ShaderProgramWoittest woittest = ShaderPrograms.Woittest;
				woittest.Use();
				woittest.ProjectionMatrix = this.game.CurrentProjectionMatrix;
				this.game.GlMatrixModeModelView();
				this.game.GlPushMatrix();
				this.game.GlTranslate(5000.0, 120.0, 5000.0);
				woittest.ModelViewMatrix = this.game.CurrentModelViewMatrix;
				this.game.Platform.RenderMesh(this.coloredPlanesRef);
				this.game.GlPopMatrix();
				woittest.Stop();
				this.game.Platform.GlEnableDepthTest();
			}
		}

		public void OnRenderFrame2DOverlay(float deltaTime)
		{
			if (this.framebufferDebug)
			{
				this.game.Platform.GlToggleBlend(true, EnumBlendMode.PremultipliedAlpha);
				float sc = RuntimeEnv.GUIScale;
				FrameBufferRef fb = this.game.Platform.FrameBuffers[1];
				this.game.Render2DTextureFlipped(fb.ColorTextureIds[0], sc * 10f, sc * 10f, sc * 150f, sc * 150f, 10f, null);
				this.game.Render2DLoadedTexture(this.labels[1], sc * 10f, sc * 10f, 50f, null);
				this.game.Render2DTextureFlipped(fb.ColorTextureIds[1], sc * 10f, sc * 160f, sc * 150f, sc * 150f, 10f, null);
				this.game.Render2DLoadedTexture(this.labels[2], sc * 10f, sc * 160f, 50f, null);
				fb = this.game.Platform.FrameBuffers[10];
				this.game.Render2DTextureFlipped(fb.ColorTextureIds[0], sc * 10f, sc * 310f, sc * 150f, sc * 150f, 10f, null);
				this.game.Render2DLoadedTexture(this.labels[7], sc * 10f, sc * 310f, 50f, null);
				fb = this.game.Platform.FrameBuffers[4];
				this.game.Render2DTextureFlipped(fb.ColorTextureIds[0], sc * 10f, sc * 460f, sc * 150f, sc * 150f, 10f, null);
				this.game.Render2DLoadedTexture(this.labels[3], sc * 10f, sc * 460f, 50f, null);
				fb = this.game.Platform.FrameBuffers[0];
				this.game.Render2DTextureFlipped(fb.ColorTextureIds[1], sc * 10f, sc * 610f, sc * 150f, sc * 150f, 10f, null);
				this.game.Render2DLoadedTexture(this.labels[8], sc * 10f, sc * 610f, 50f, null);
				fb = this.game.Platform.FrameBuffers[0];
				int y = 10;
				this.game.Render2DTextureFlipped(fb.ColorTextureIds[0], (float)this.game.Width - sc * 160f, sc * (float)y, sc * 150f, sc * 150f, 10f, null);
				this.game.Render2DLoadedTexture(this.labels[4], (float)this.game.Width - sc * 160f, sc * (float)y, 50f, null);
				y += 155;
				if (ClientSettings.SSAOQuality > 0)
				{
					this.game.Render2DTextureFlipped(this.game.Platform.FrameBuffers[13].ColorTextureIds[0], (float)this.game.Width - sc * 320f, sc * 10f, sc * 150f, sc * 150f, 10f, null);
					this.game.Render2DLoadedTexture(this.labels[12], (float)this.game.Width - sc * 320f, sc * 10f, 50f, null);
					this.game.Render2DTextureFlipped(fb.ColorTextureIds[2], (float)this.game.Width - sc * 160f, sc * (float)y, sc * 150f, sc * 150f, 10f, null);
					this.game.Render2DLoadedTexture(this.labels[10], (float)this.game.Width - sc * 160f, sc * (float)y, 50f, null);
					y += 155;
					this.game.Render2DTextureFlipped(fb.ColorTextureIds[3], (float)this.game.Width - sc * 160f, sc * (float)y, sc * 150f, sc * 150f, 10f, null);
					this.game.Render2DLoadedTexture(this.labels[11], (float)this.game.Width - sc * 160f, sc * (float)y, 50f, null);
					y += 155;
				}
				this.game.Render2DTextureFlipped(fb.DepthTextureId, (float)this.game.Width - sc * 160f, sc * (float)y, sc * 150f, sc * 150f, 10f, null);
				this.game.Render2DLoadedTexture(this.labels[5], (float)this.game.Width - sc * 160f, sc * (float)y, 50f, null);
				y += 155;
				this.game.guiShaderProg.Stop();
				ShaderProgramDebugdepthbuffer prog = ShaderPrograms.Debugdepthbuffer;
				prog.Use();
				prog.DepthSampler2D = fb.DepthTextureId;
				this.game.GlPushMatrix();
				this.game.GlTranslate((double)((float)this.game.Width - sc * 160f), (double)(sc * (float)y), (double)(sc * 50f));
				this.game.GlScale((double)(sc * 150f), (double)(sc * 150f), 0.0);
				this.game.GlScale(0.5, 0.5, 0.0);
				this.game.GlTranslate(1.0, 1.0, 0.0);
				this.game.GlRotate(180f, 1.0, 0.0, 0.0);
				prog.ProjectionMatrix = this.game.CurrentProjectionMatrix;
				prog.ModelViewMatrix = this.game.CurrentModelViewMatrix;
				this.game.Platform.RenderMesh(this.quadModel);
				this.game.GlPopMatrix();
				int shadowMapQuality = ClientSettings.ShadowMapQuality;
				if (shadowMapQuality > 0)
				{
					fb = this.game.Platform.FrameBuffers[11];
					prog.DepthSampler2D = fb.DepthTextureId;
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareMode, 0);
					this.game.GlPushMatrix();
					this.game.GlTranslate((double)(sc * 170f), (double)(sc * 10f), (double)(sc * 50f));
					this.game.GlScale((double)(sc * 300f), (double)(sc * 300f), 0.0);
					this.game.GlScale(0.5, 0.5, 0.0);
					this.game.GlTranslate(1.0, 1.0, 0.0);
					this.game.GlRotate(180f, 1.0, 0.0, 0.0);
					prog.ProjectionMatrix = this.game.CurrentProjectionMatrix;
					prog.ModelViewMatrix = this.game.CurrentModelViewMatrix;
					this.game.Platform.RenderMesh(this.quadModel);
					this.game.GlPopMatrix();
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareMode, 34894);
				}
				if (shadowMapQuality > 1)
				{
					fb = this.game.Platform.FrameBuffers[12];
					prog.DepthSampler2D = fb.DepthTextureId;
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareMode, 0);
					this.game.GlPushMatrix();
					this.game.GlTranslate((double)(sc * 170f), (double)(sc * 320f), (double)(sc * 50f));
					this.game.GlScale((double)(sc * 300f), (double)(sc * 300f), 0.0);
					this.game.GlScale(0.5, 0.5, 0.0);
					this.game.GlTranslate(1.0, 1.0, 0.0);
					this.game.GlRotate(180f, 1.0, 0.0, 0.0);
					prog.ProjectionMatrix = this.game.CurrentProjectionMatrix;
					prog.ModelViewMatrix = this.game.CurrentModelViewMatrix;
					this.game.Platform.RenderMesh(this.quadModel);
					this.game.GlPopMatrix();
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareMode, 34894);
				}
				fb = this.game.Platform.FrameBuffers[5];
				prog.DepthSampler2D = fb.DepthTextureId;
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareMode, 0);
				this.game.GlPushMatrix();
				this.game.GlTranslate((double)(sc * 170f), (double)(sc * 630f), (double)(sc * 50f));
				this.game.GlScale((double)(sc * 300f), (double)(sc * 300f), 0.0);
				this.game.GlScale(0.5, 0.5, 0.0);
				this.game.GlTranslate(1.0, 1.0, 0.0);
				this.game.GlRotate(180f, 1.0, 0.0, 0.0);
				prog.ProjectionMatrix = this.game.CurrentProjectionMatrix;
				prog.ModelViewMatrix = this.game.CurrentModelViewMatrix;
				this.game.Platform.RenderMesh(this.quadModel);
				this.game.GlPopMatrix();
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareMode, 34894);
				prog.Stop();
				this.game.guiShaderProg.Use();
				this.game.Platform.GlDisableDepthTest();
				this.game.Render2DLoadedTexture(this.labels[13], sc * 170f, sc * 630f, 50f, null);
				if (shadowMapQuality > 0)
				{
					this.game.Render2DLoadedTexture(this.labels[0], sc * 170f, sc * 10f, 50f, null);
				}
				if (shadowMapQuality > 1)
				{
					this.game.Render2DLoadedTexture(this.labels[9], sc * 170f, sc * 320f, 50f, null);
				}
				this.game.Platform.GlEnableDepthTest();
				this.game.Render2DLoadedTexture(this.labels[6], (float)this.game.Width - sc * 170f, sc * (float)y, 50f, null);
				this.game.Platform.GlToggleBlend(true, EnumBlendMode.Standard);
			}
		}

		private TextCommandResult CmdWoit(TextCommandCallingArgs textCommandCallingArgs)
		{
			this.framebufferDebug = !this.framebufferDebug;
			return TextCommandResult.Success("", null);
		}

		public override void Dispose(ClientMain game)
		{
			for (int i = 0; i < this.labels.Length; i++)
			{
				LoadedTexture loadedTexture = this.labels[i];
				if (loadedTexture != null)
				{
					loadedTexture.Dispose();
				}
			}
			MeshRef meshRef = this.quadModel;
			if (meshRef != null)
			{
				meshRef.Dispose();
			}
			MeshRef meshRef2 = this.coloredPlanesRef;
			if (meshRef2 == null)
			{
				return;
			}
			meshRef2.Dispose();
		}

		public override EnumClientSystemType GetSystemType()
		{
			return EnumClientSystemType.Misc;
		}

		private bool framebufferDebug;

		private MeshRef coloredPlanesRef;

		private MeshRef quadModel;

		private LoadedTexture[] labels;
	}
}
