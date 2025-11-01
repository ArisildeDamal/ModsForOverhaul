using System;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class SystemRenderPlayerAimAcc : ClientSystem
	{
		public SystemRenderPlayerAimAcc(ClientMain game)
			: base(game)
		{
			game.eventManager.RegisterRenderer(new Action<float>(this.OnRenderFrame2DOverlay), EnumRenderStage.Ortho, this.Name, 0.98);
			this.GenAim();
		}

		public override string Name
		{
			get
			{
				return "repa";
			}
		}

		public override EnumClientSystemType GetSystemType()
		{
			return EnumClientSystemType.Render;
		}

		private void OnRenderFrame2DOverlay(float dt)
		{
			if (!this.game.MouseGrabbed || this.game.EntityPlayer.Attributes.GetInt("aiming", 0) == 0)
			{
				return;
			}
			this.game.guiShaderProg.RgbaIn = new Vec4f(1f, 1f, 1f, 1f);
			this.game.guiShaderProg.ExtraGlow = 0;
			this.game.guiShaderProg.ApplyColor = 1;
			this.game.guiShaderProg.Tex2d2D = 0;
			this.game.guiShaderProg.NoTexture = 1f;
			ScreenManager.Platform.CheckGlError(null);
			this.game.Platform.GLLineWidth(0.5f);
			this.game.Platform.SmoothLines(true);
			ScreenManager.Platform.CheckGlError(null);
			this.game.Platform.GlToggleBlend(true, EnumBlendMode.Standard);
			this.game.Platform.BindTexture2d(0);
			ScreenManager.Platform.CheckGlError(null);
			this.game.GlPushMatrix();
			this.game.GlTranslate((double)(this.game.Width / 2), (double)(this.game.Height / 2), 50.0);
			float aimAcc = Math.Max(0.01f, 1f - this.game.EntityPlayer.Attributes.GetFloat("aimingAccuracy", 0f));
			float scale = 800f * aimAcc;
			this.game.GlScale((double)scale, (double)scale, 0.0);
			this.game.guiShaderProg.ModelViewMatrix = this.game.CurrentModelViewMatrix;
			this.game.Platform.RenderMesh(this.aimRectangleRef);
			this.game.GlPopMatrix();
			this.game.Platform.GLLineWidth(1f);
			this.game.GlPushMatrix();
			this.game.GlTranslate((double)(this.game.Width / 2), (double)(this.game.Height / 2), 50.0);
			this.game.GlScale(20.0, 20.0, 0.0);
			this.game.GlTranslate(0.0, (double)(-10f * aimAcc + 0.5f), 0.0);
			this.game.guiShaderProg.ModelViewMatrix = this.game.CurrentModelViewMatrix;
			this.game.Platform.RenderMesh(this.aimLinesRef[0]);
			this.game.GlTranslate(0.0, (double)(20f * aimAcc - 1f), 0.0);
			this.game.guiShaderProg.ModelViewMatrix = this.game.CurrentModelViewMatrix;
			this.game.Platform.RenderMesh(this.aimLinesRef[1]);
			this.game.GlTranslate((double)(-10f * aimAcc + 0.5f), (double)(-10f * aimAcc + 0.5f), 0.0);
			this.game.guiShaderProg.ModelViewMatrix = this.game.CurrentModelViewMatrix;
			this.game.Platform.RenderMesh(this.aimLinesRef[2]);
			this.game.GlTranslate((double)(20f * aimAcc - 1f), 0.0, 0.0);
			this.game.guiShaderProg.ModelViewMatrix = this.game.CurrentModelViewMatrix;
			this.game.Platform.RenderMesh(this.aimLinesRef[3]);
			this.game.GlPopMatrix();
		}

		public void GenAim()
		{
			for (int i = 0; i < 4; i++)
			{
				MeshData mesh = new MeshData(true);
				mesh.SetMode(EnumDrawMode.Lines);
				mesh.Rgba = new byte[8];
				mesh.xyz = new float[6];
				mesh.Indices = new int[2];
				mesh.Uv = new float[4];
				if (i == 0)
				{
					LineMeshUtil.AddLine2D(mesh, 0f, -0.5f, 0f, -1f, -1);
				}
				if (i == 1)
				{
					LineMeshUtil.AddLine2D(mesh, 0f, 0.5f, 0f, 1f, -1);
				}
				if (i == 2)
				{
					LineMeshUtil.AddLine2D(mesh, -1f, 0f, -0.5f, 0f, -1);
				}
				if (i == 3)
				{
					LineMeshUtil.AddLine2D(mesh, 0.5f, 0f, 1f, 0f, -1);
				}
				this.aimLinesRef[i] = this.game.Platform.UploadMesh(mesh);
			}
			MeshData meshrect = new MeshData(true);
			meshrect.SetMode(EnumDrawMode.Lines);
			meshrect.xyz = new float[48];
			meshrect.Rgba = new byte[64];
			meshrect.Indices = new int[16];
			meshrect.Uv = new float[32];
			int color = ColorUtil.ToRgba(128, 255, 255, 255);
			LineMeshUtil.AddLine2D(meshrect, -0.5f, -0.5f, -0.05f, -0.5f, color);
			LineMeshUtil.AddLine2D(meshrect, -0.5f, -0.5f, -0.5f, -0.05f, color);
			LineMeshUtil.AddLine2D(meshrect, 0.05f, -0.5f, 0.5f, -0.5f, color);
			LineMeshUtil.AddLine2D(meshrect, 0.5f, -0.5f, 0.5f, -0.05f, color);
			LineMeshUtil.AddLine2D(meshrect, 0.5f, 0.05f, 0.5f, 0.5f, color);
			LineMeshUtil.AddLine2D(meshrect, 0.5f, 0.5f, 0.05f, 0.5f, color);
			LineMeshUtil.AddLine2D(meshrect, -0.05f, 0.5f, -0.5f, 0.5f, color);
			LineMeshUtil.AddLine2D(meshrect, -0.5f, 0.5f, -0.5f, 0.05f, color);
			this.aimRectangleRef = this.game.Platform.UploadMesh(meshrect);
		}

		public override void Dispose(ClientMain game)
		{
			game.Platform.DeleteMesh(this.aimRectangleRef);
			for (int i = 0; i < this.aimLinesRef.Length; i++)
			{
				game.Platform.DeleteMesh(this.aimLinesRef[i]);
			}
		}

		private MeshRef[] aimLinesRef = new MeshRef[4];

		private MeshRef aimRectangleRef;
	}
}
