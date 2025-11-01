using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class SystemRenderShadowMap : ClientSystem
	{
		public SystemRenderShadowMap(ClientMain game)
		{
			double[] array = new double[3];
			array[1] = 1.0;
			this.up = array;
			this.forward = new Vec3d();
			base..ctor(game);
			game.eventManager.RegisterRenderer(new Action<float>(this.OnRenderBefore), EnumRenderStage.Before, this.Name, 0.0);
			game.eventManager.RegisterRenderer(new Action<float>(this.OnRenderShadowFar), EnumRenderStage.ShadowFar, this.Name, 0.0);
			game.eventManager.RegisterRenderer(new Action<float>(this.OnRenderShadowFarDone), EnumRenderStage.ShadowFarDone, this.Name, 1.0);
			game.eventManager.RegisterRenderer(new Action<float>(this.OnRenderShadowNear), EnumRenderStage.ShadowNear, this.Name, 0.0);
			game.eventManager.RegisterRenderer(new Action<float>(this.OnRenderShadowNearDone), EnumRenderStage.ShadowNearDone, this.Name, 1.0);
			this.shadowBox = new ShadowBox(this.lightViewMatrix, game);
		}

		private void OnRenderBefore(float dt)
		{
			Vec3f sunPosition = this.game.Calendar.SunPosition;
			Vec3f moonPos = this.game.Calendar.MoonPosition;
			AmbientModifier amb = this.game.AmbientManager.CurrentModifiers["weather"];
			double diffuseStrength = -0.1;
			if (amb != null)
			{
				diffuseStrength = (double)((amb.FogDensity.Weight * amb.FogDensity.Value + amb.FogMin.Value * amb.FogMin.Weight + amb.FlatFogDensity.Weight * amb.FlatFogDensity.Value * Math.Max(0f, amb.FlatFogYPos.Weight * (amb.FlatFogYPos.Value > 0f))) * 12f);
			}
			amb = this.game.AmbientManager.CurrentModifiers["serverambient"];
			if (amb != null)
			{
				diffuseStrength += (double)((amb.FogDensity.Weight * amb.FogDensity.Value + amb.FogMin.Value * amb.FogMin.Weight + amb.FlatFogDensity.Weight * amb.FlatFogDensity.Value * Math.Max(0f, amb.FlatFogYPos.Weight * (amb.FlatFogYPos.Value > 0f))) * 12f);
			}
			float dropShadowIntensity = Math.Max(GameMath.Clamp(sunPosition.Y / 20f, 0f, 1f), GameMath.Clamp(moonPos.Y / 4f, 0f, 1f) * Math.Min(1f, 2f * this.game.Calendar.MoonPhaseBrightness - 0.2f)) - (float)Math.Max(0.0, diffuseStrength);
			if ((double)dropShadowIntensity < 0.12)
			{
				this.game.shUniforms.DropShadowIntensity = (this.game.AmbientManager.DropShadowIntensity = Math.Max(0f, this.game.AmbientManager.DropShadowIntensity - dt / 4f));
				return;
			}
			this.game.shUniforms.DropShadowIntensity = (this.game.AmbientManager.DropShadowIntensity = dropShadowIntensity);
		}

		private void OnRenderShadowNear(float dt)
		{
			if ((double)this.game.AmbientManager.DropShadowIntensity <= 0.01)
			{
				return;
			}
			double shadowRange = (double)(30 + 3 * (ClientSettings.ShadowMapQuality - 1));
			ShadowBox.ShadowBoxZExtend = (double)(50f + 50f * Math.Abs(1f - this.game.Calendar.SunPositionNormalized.Y) + 100f);
			this.game.shUniforms.ShadowRangeNear = (float)shadowRange;
			this.game.shUniforms.ShadowZExtendNear = 0f;
			this.PrepareForShadowRendering(shadowRange, EnumFrameBuffer.ShadowmapNear, 16f);
			Mat4d.Mul(this.tmp, this.offset, this.projectionViewMatrix);
			for (int i = 0; i < 16; i++)
			{
				this.game.toShadowMapSpaceMatrixNear[i] = (float)this.tmp[i];
			}
			this.game.shUniforms.ToShadowMapSpaceMatrixNear = this.game.toShadowMapSpaceMatrixNear;
		}

		private void OnRenderShadowFar(float dt)
		{
			if ((double)this.game.AmbientManager.DropShadowIntensity <= 0.01)
			{
				return;
			}
			int q = ClientSettings.ShadowMapQuality;
			double shadowRange;
			if (q == 1)
			{
				shadowRange = 60.0;
			}
			else
			{
				shadowRange = (double)(150 + 120 * (q - 1));
			}
			ShadowBox.ShadowBoxZExtend = (double)(100f + 60f * Math.Abs(1f - this.game.Calendar.SunPositionNormalized.Y));
			this.game.shUniforms.ShadowRangeFar = (float)shadowRange;
			this.PrepareForShadowRendering((q > 1) ? (shadowRange / 2.0) : shadowRange, EnumFrameBuffer.ShadowmapFar, 0f);
			Mat4d.Mul(this.tmp, this.offset, this.projectionViewMatrix);
			for (int i = 0; i < 16; i++)
			{
				this.game.toShadowMapSpaceMatrixFar[i] = (float)this.tmp[i];
			}
			this.game.shUniforms.ToShadowMapSpaceMatrixFar = this.game.toShadowMapSpaceMatrixFar;
		}

		private void PrepareForShadowRendering(double shadowDistance, EnumFrameBuffer fb, float cullExtraRange)
		{
			EntityPlayer plr = this.game.EntityPlayer;
			ShadowBox.SHADOW_DISTANCE = shadowDistance;
			this.shadowBox.calculateWidthsAndHeights();
			this.shadowBox.update();
			this.game.frustumCuller.shadowRangeX = shadowDistance + ShadowBox.ShadowBoxZExtend + (double)cullExtraRange;
			this.game.frustumCuller.shadowRangeZ = shadowDistance + (double)cullExtraRange;
			Vec3f lightPosRel = ((this.game.Calendar.MoonLightStrength > this.game.Calendar.SunLightStrength) ? this.game.Calendar.MoonPosition : this.game.Calendar.SunPosition);
			this.loadOrthoModeMatrix(this.projectionMatrix, this.shadowBox.Width, this.shadowBox.Height, this.shadowBox.Length);
			double[] array = this.lightViewMatrix;
			double[] array2 = lightPosRel.ToDoubleArray();
			double[] array3 = new double[4];
			double[] array4 = new double[3];
			array4[1] = 1.0;
			Mat4d.LookAt(array, array2, array3, array4);
			Mat4d.Mul(this.projectionViewMatrix, this.projectionMatrix, this.lightViewMatrix);
			this.game.Platform.LoadFrameBuffer(fb);
			this.game.Platform.ClearFrameBuffer(fb);
			ShaderProgramShadowmapgeneric prog = ShaderPrograms.Chunkshadowmap;
			prog.Use();
			this.game.PMatrix.Push(this.projectionMatrix);
			this.game.MvMatrix.Push(this.lightViewMatrix);
			for (int i = 0; i < 16; i++)
			{
				this.game.shadowMvpMatrix[i] = (float)this.projectionViewMatrix[i];
			}
			prog.MvpMatrix = this.game.shadowMvpMatrix;
			double[] mvMat = Mat4d.Create();
			VectorTool.ToVectorInFixedSystem(0.0, 0.0, 0.0, (double)plr.Pos.Pitch, (double)(1.5707964f - plr.Pos.Yaw), this.forward);
			this.center[0] = plr.CameraPos.X;
			this.center[1] = plr.CameraPos.Y;
			this.center[2] = plr.CameraPos.Z;
			this.targetPos[0] = plr.CameraPos.X + (double)lightPosRel.X;
			this.targetPos[1] = plr.CameraPos.Y + (double)lightPosRel.Y;
			this.targetPos[2] = plr.CameraPos.Z + (double)lightPosRel.Z;
			Mat4d.LookAt(mvMat, this.targetPos, this.center, this.up);
			this.game.frustumCuller.CalcFrustumEquations(plr.Pos.AsBlockPos, this.game.PMatrix.Top, mvMat);
		}

		private void OnRenderShadowNearDone(float dt)
		{
			ShaderProgramBase.CurrentShaderProgram.Stop();
			this.game.Platform.UnloadFrameBuffer(EnumFrameBuffer.ShadowmapNear);
			this.game.Platform.LoadFrameBuffer(EnumFrameBuffer.Primary);
			this.game.PMatrix.Pop();
			this.game.MvMatrix.Pop();
		}

		private void OnRenderShadowFarDone(float dt)
		{
			ShaderProgramBase.CurrentShaderProgram.Stop();
			this.game.Platform.UnloadFrameBuffer(EnumFrameBuffer.ShadowmapFar);
			this.game.Platform.LoadFrameBuffer(EnumFrameBuffer.Primary);
			this.game.PMatrix.Pop();
			this.game.MvMatrix.Pop();
		}

		private void loadOrthoModeMatrix(double[] projectionMatrix, double width, double height, double length)
		{
			Mat4d.Identity(projectionMatrix);
			projectionMatrix[0] = 2.0 / width;
			projectionMatrix[5] = 2.0 / height;
			projectionMatrix[10] = -2.0 / length;
			projectionMatrix[15] = 1.0;
		}

		private static double[] createOffset()
		{
			double[] offset = Mat4d.Create();
			Mat4d.Translate(offset, offset, new double[] { 0.5, 0.5, 0.5 });
			Mat4d.Scale(offset, offset, new double[] { 0.5, 0.5, 0.5 });
			return offset;
		}

		public override string Name
		{
			get
			{
				return "res";
			}
		}

		public override EnumClientSystemType GetSystemType()
		{
			return EnumClientSystemType.Render;
		}

		private ShadowBox shadowBox;

		private double[] projectionMatrix = Mat4d.Create();

		private double[] lightViewMatrix = Mat4d.Create();

		private double[] projectionViewMatrix = Mat4d.Create();

		private double[] offset = SystemRenderShadowMap.createOffset();

		private double[] tmp = Mat4d.Create();

		private double[] targetPos = new double[3];

		private double[] center = new double[3];

		private double[] up;

		private Vec3d forward;
	}
}
