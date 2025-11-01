using System;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.Client.NoObf
{
	public class SystemRenderSunMoon : ClientSystem
	{
		public override string Name
		{
			get
			{
				return "resm";
			}
		}

		public SystemRenderSunMoon(ClientMain game)
			: base(game)
		{
			this.suntextureId = -1;
			this.moontextureIds = new int[8];
			this.moontextureIds.Fill(-1);
			this.ImageSize = 256;
			MeshData meshdata = QuadMeshUtilExt.GetCustomQuadModelData(0f, 0f, 0f, (float)this.ImageSize, (float)this.ImageSize, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, 0);
			meshdata.Flags = new int[4];
			this.quadModel = game.Platform.UploadMesh(meshdata);
			game.eventManager.RegisterRenderer(new Action<float>(this.OnRenderFrame3D), EnumRenderStage.Opaque, this.Name, 0.3);
			game.eventManager.RegisterRenderer(new Action<float>(this.OnRenderFrame3DPost), EnumRenderStage.Opaque, this.Name, 999.0);
			GL.GenQueries(1, out this.occlQueryId);
		}

		private void OnRenderFrame3DPost(float obj)
		{
			ClientPlatformAbstract platform = this.game.Platform;
			platform.GlEnableDepthTest();
			platform.GlToggleBlend(true, EnumBlendMode.Standard);
			platform.GlDisableCullFace();
			platform.GlDepthMask(false);
			GL.ColorMask(false, false, false, false);
			Vec3f sunPos = this.game.Calendar.SunPosition;
			Quaternion quat = SystemRenderSunMoon.CreateLookRotation(new Vector3(sunPos.X, sunPos.Y, sunPos.Z));
			this.sunmat = Matrix4.CreateTranslation((float)(-(float)this.ImageSize / 2), (float)(-(float)this.ImageSize), (float)(-(float)this.ImageSize / 2)) * Matrix4.CreateScale(this.sunScale, this.sunScale * 7f, this.sunScale) * Matrix4.CreateFromQuaternion(quat) * Matrix4.CreateTranslation(new Vector3(sunPos.X, sunPos.Y, sunPos.Z));
			ShaderProgramStandard prog = ShaderPrograms.Standard;
			prog.Use();
			prog.RgbaTint = ColorUtil.WhiteArgbVec;
			prog.RgbaAmbientIn = ColorUtil.WhiteRgbVec;
			prog.RgbaLightIn = new Vec4f(0f, 0f, 0f, (float)Math.Sin((double)this.game.ElapsedMilliseconds / 1000.0) / 2f + 0.5f);
			prog.RgbaFogIn = this.game.AmbientManager.BlendedFogColor;
			prog.ExtraGlow = 0;
			prog.FogMinIn = this.game.AmbientManager.BlendedFogMin;
			prog.FogDensityIn = this.game.AmbientManager.BlendedFogDensity;
			prog.DontWarpVertices = 0;
			prog.AddRenderFlags = 0;
			prog.ExtraZOffset = 0f;
			prog.NormalShaded = 0;
			prog.OverlayOpacity = 0f;
			prog.ExtraGodray = 0.33333334f;
			prog.UniformMatrix("modelMatrix", ref this.sunmat);
			prog.ViewMatrix = this.game.api.renderapi.CameraMatrixOriginf;
			prog.ProjectionMatrix = this.game.api.renderapi.CurrentProjectionMatrix;
			prog.Tex2D = this.suntextureId;
			if (this.firstTickDone)
			{
				int resultReady;
				GL.GetQueryObject(this.occlQueryId, GetQueryObjectParam.QueryResultAvailable, out resultReady);
				if (resultReady > 0)
				{
					int samplesRendered;
					GL.GetQueryObject(this.occlQueryId, GetQueryObjectParam.QueryResult, out samplesRendered);
					this.targetSunSpec = GameMath.Clamp((float)samplesRendered / 1500f, 0f, 1f);
					this.nowQuerying = false;
				}
			}
			this.firstTickDone = true;
			bool didBeginQuery = false;
			if (!this.nowQuerying)
			{
				GL.BeginQuery(QueryTarget.SamplesPassed, this.occlQueryId);
				this.nowQuerying = true;
				didBeginQuery = true;
			}
			platform.RenderMesh(this.quadModel);
			prog.Stop();
			if (didBeginQuery)
			{
				GL.EndQuery(QueryTarget.SamplesPassed);
			}
			platform.GlDepthMask(true);
			GL.ColorMask(true, true, true, true);
		}

		public void OnRenderFrame3D(float dt)
		{
			ClientPlatformAbstract platform = this.game.Platform;
			if (this.suntextureId == -1)
			{
				this.suntextureId = this.game.GetOrLoadCachedTexture(new AssetLocation("environment/sun.png"));
				for (int i = 0; i < 8; i++)
				{
					this.moontextureIds[i] = this.game.GetOrLoadCachedTexture(new AssetLocation("environment/moon/" + i.ToString() + ".png"));
				}
			}
			Vec3f moonPos = this.game.Calendar.MoonPosition;
			Vec3f sunPosRel = this.game.GameWorldCalendar.SunPositionNormalized;
			this.game.shUniforms.SunPosition3D = sunPosRel;
			Vec3f moonPosRel = moonPos.Clone().Normalize();
			float moonb = this.game.GameWorldCalendar.MoonLightStrength;
			float sunb = this.game.GameWorldCalendar.SunLightStrength;
			float t = GameMath.Clamp(50f * (moonb - sunb), 0f, 1f);
			this.game.shUniforms.LightPosition3D.Set(GameMath.Lerp(sunPosRel.X, moonPosRel.X, t), GameMath.Lerp(sunPosRel.Y, moonPosRel.Y, t), GameMath.Lerp(sunPosRel.Z, moonPosRel.Z, t));
			if (sunPosRel.Y < -0.05f)
			{
				double[] projMat = this.game.PMatrix.Top;
				double[] viewMat = this.game.api.renderapi.CameraMatrixOrigin;
				double[] modelViewMat = new double[16];
				Mat4d.Mul(modelViewMat, viewMat, this.ModelMat.ValuesAsDouble);
				Vec3d screenPos = MatrixToolsd.Project(new Vec3d((double)((float)this.ImageSize / 4f), (double)((float)(-(float)this.ImageSize) / 4f), 0.0), projMat, modelViewMat, this.game.Width, this.game.Height);
				Vec3f centeredRelPos = new Vec3f((float)screenPos.X / (float)this.game.Width * 2f - 1f, (float)screenPos.Y / (float)this.game.Height * 2f - 1f, (float)screenPos.Z);
				this.game.shUniforms.SunPositionScreen = centeredRelPos;
			}
			platform.GlToggleBlend(true, EnumBlendMode.Standard);
			platform.GlDisableCullFace();
			platform.GlDisableDepthTest();
			this.prepareSunMat();
			Vec3f suncol = this.game.Calendar.SunColor.Clone();
			float f = (suncol.R + suncol.G + suncol.B) / 3f;
			float colorLoss = GameMath.Clamp(GameMath.Max(this.game.AmbientManager.BlendedFlatFogDensity * 40f, this.game.AmbientManager.BlendedCloudDensity * this.game.AmbientManager.BlendedCloudDensity), 0f, 1f);
			suncol.R = colorLoss * f + (1f - colorLoss) * suncol.R;
			suncol.G = colorLoss * f + (1f - colorLoss) * suncol.G;
			suncol.B = colorLoss * f + (1f - colorLoss) * suncol.B;
			ShaderProgramStandard prog = ShaderPrograms.Standard;
			prog.Use();
			prog.Uniform("skyShaded", 1);
			Vec4f rgbatint = new Vec4f(1f, 1f, 1f, 1f);
			DefaultShaderUniforms shu = this.game.api.renderapi.ShaderUniforms;
			if (shu.FogSphereQuantity > 0)
			{
				for (int j = 0; j < shu.FogSphereQuantity; j++)
				{
					float num = shu.FogSpheres[j * 8];
					float y = shu.FogSpheres[j * 8 + 1];
					float z = shu.FogSpheres[j * 8 + 2];
					float rad = shu.FogSpheres[j * 8 + 3];
					float dense = shu.FogSpheres[j * 8 + 4];
					double d = Math.Sqrt((double)(num * num + y * y + z * z));
					double fogAmount = (1.0 - d / (double)rad) * (double)rad * (double)dense;
					rgbatint.A = (float)GameMath.Clamp((double)rgbatint.A - fogAmount, 0.0, 1.0);
				}
			}
			prog.FadeFromSpheresFog = 1;
			prog.RgbaTint = rgbatint;
			prog.RgbaAmbientIn = ColorUtil.WhiteRgbVec;
			prog.RgbaLightIn = new Vec4f(suncol.R, suncol.G, suncol.B, 1f);
			prog.RgbaFogIn = this.game.AmbientManager.BlendedFogColor;
			prog.ExtraGlow = 0;
			prog.FogMinIn = this.game.AmbientManager.BlendedFogMin;
			prog.FogDensityIn = this.game.AmbientManager.BlendedFogDensity;
			prog.DontWarpVertices = 0;
			prog.AddRenderFlags = 0;
			prog.ExtraZOffset = 0f;
			prog.NormalShaded = 0;
			prog.OverlayOpacity = 0f;
			prog.ExtraGodray = 0.33333334f;
			prog.ShadowIntensity = 0f;
			prog.ApplySsao = 0;
			prog.AlphaTest = 0.01f;
			prog.UniformMatrix("modelMatrix", ref this.sunmat);
			prog.ViewMatrix = this.game.api.renderapi.CameraMatrixOriginf;
			prog.ProjectionMatrix = this.game.api.renderapi.CurrentProjectionMatrix;
			prog.Tex2D = this.suntextureId;
			platform.RenderMesh(this.quadModel);
			prog.Uniform("skyShaded", 0);
			prog.ExtraGodray = 0f;
			prog.ApplySsao = 1;
			prog.FadeFromSpheresFog = 0;
			prog.Stop();
			if (sunPosRel.Y >= -0.05f)
			{
				double[] projMat2 = this.game.PMatrix.Top;
				double[] viewMat2 = this.game.api.renderapi.CameraMatrixOrigin;
				double[] modelViewMat2 = new double[16];
				Mat4d.Mul(modelViewMat2, viewMat2, new double[]
				{
					(double)this.sunmat.M11,
					(double)this.sunmat.M12,
					(double)this.sunmat.M13,
					(double)this.sunmat.M14,
					(double)this.sunmat.M21,
					(double)this.sunmat.M22,
					(double)this.sunmat.M23,
					(double)this.sunmat.M24,
					(double)this.sunmat.M31,
					(double)this.sunmat.M32,
					(double)this.sunmat.M33,
					(double)this.sunmat.M34,
					(double)this.sunmat.M41,
					(double)this.sunmat.M42,
					(double)this.sunmat.M43,
					(double)this.sunmat.M44
				});
				Vec3d screenPos2 = MatrixToolsd.Project(new Vec3d((double)((float)this.ImageSize / 2f), (double)((float)this.ImageSize / 2f), 0.0), projMat2, modelViewMat2, this.game.Width, this.game.Height);
				Vec3f centeredRelPos2 = new Vec3f((float)screenPos2.X / (float)this.game.Width * 2f - 1f, (float)screenPos2.Y / (float)this.game.Height * 2f - 1f, (float)screenPos2.Z);
				this.game.shUniforms.SunPositionScreen = centeredRelPos2;
			}
			this.game.shUniforms.SunSpecularIntensity = GameMath.Clamp(this.game.shUniforms.SunSpecularIntensity + (this.targetSunSpec - this.game.shUniforms.SunSpecularIntensity) * dt * 20f, 0f, 1f);
			this.prepareMoonMat(moonPos, moonPosRel);
			float angle = this.getAngleSunFromMoon(sunPosRel.Clone().Sub(moonPosRel), moonPosRel);
			ShaderProgramCelestialobject celestialobject = ShaderPrograms.Celestialobject;
			celestialobject.Use();
			celestialobject.Sky2D = this.game.skyTextureId;
			celestialobject.Glow2D = this.game.skyGlowTextureId;
			celestialobject.SunPosition = sunPosRel;
			celestialobject.Uniform("moonPosition", moonPosRel);
			celestialobject.Uniform("moonSunAngle", angle);
			celestialobject.DayLight = this.game.shUniforms.SkyDaylight;
			celestialobject.WeirdMathToMakeMoonLookNicer = 1;
			celestialobject.DitherSeed = (this.game.frameSeed + 1) % Math.Max(1, this.game.Width * this.game.Height);
			celestialobject.HorizontalResolution = this.game.Width;
			celestialobject.PlayerToSealevelOffset = (float)this.game.EntityPlayer.Pos.Y - (float)this.game.SeaLevel;
			celestialobject.RgbaFogIn = this.game.AmbientManager.BlendedFogColor;
			celestialobject.FogMinIn = this.game.AmbientManager.BlendedFogMin;
			celestialobject.FogDensityIn = this.game.AmbientManager.BlendedFogDensity;
			celestialobject.HorizonFog = this.game.AmbientManager.BlendedCloudDensity;
			celestialobject.ExtraGlow = 0;
			celestialobject.ExtraGodray = 0.5f;
			celestialobject.UniformMatrix("modelMatrix", ref this.moonmat);
			celestialobject.ViewMatrix = this.game.api.renderapi.CameraMatrixOriginf;
			celestialobject.ProjectionMatrix = this.game.api.renderapi.CurrentProjectionMatrix;
			celestialobject.Tex2D = this.moontextureIds[4];
			platform.RenderMesh(this.quadModel);
			celestialobject.WeirdMathToMakeMoonLookNicer = 0;
			celestialobject.Stop();
			platform.GlToggleBlend(false, EnumBlendMode.Standard);
			platform.GlEnableDepthTest();
		}

		public void prepareSunMat()
		{
			Vec3f sunPos = this.game.Calendar.SunPosition;
			float sunPosY = sunPos.Y + (float)this.game.EntityPlayer.LocalEyePos.Y - ((float)this.game.EntityPlayer.Pos.Y - (float)this.game.SeaLevel) / 10000f;
			Quaternion quat = SystemRenderSunMoon.CreateLookRotation(new Vector3(sunPos.X, sunPosY, sunPos.Z));
			this.sunmat = Matrix4.CreateTranslation((float)(-(float)this.ImageSize / 2), (float)(-(float)this.ImageSize / 2), (float)(-(float)this.ImageSize / 2)) * Matrix4.CreateScale(this.sunScale) * Matrix4.CreateFromQuaternion(quat) * Matrix4.CreateTranslation(new Vector3(sunPos.X, sunPosY, sunPos.Z));
		}

		public void prepareMoonMat(Vec3f moonPos, Vec3f moonPosNormalised)
		{
			float proximityFactor = GameMath.Clamp((this.game.Calendar.SunPositionNormalized.Dot(moonPosNormalised) - 0.99f) * 40f, 0f, 0.2f);
			float moonPosY = moonPos.Y + (float)this.game.EntityPlayer.LocalEyePos.Y - ((float)this.game.EntityPlayer.Pos.Y - (float)this.game.SeaLevel) / 10000f;
			bool waning = (int)this.game.Calendar.MoonPhaseExact > 4;
			Quaternion quat = SystemRenderSunMoon.CreateLookRotationMoon(new Vector3(moonPos.X, moonPosY, moonPos.Z), this.game.Calendar.SunPositionNormalized.Clone().Sub(moonPosNormalised), moonPosNormalised, waning, proximityFactor);
			this.moonmat = Matrix4.CreateTranslation((float)(-(float)this.ImageSize / 2), (float)(-(float)this.ImageSize / 2), (float)(-(float)this.ImageSize / 2)) * Matrix4.CreateScale(this.moonScale * (1.1f + proximityFactor)) * Matrix4.CreateFromQuaternion(quat) * Matrix4.CreateTranslation(new Vector3(moonPos.X, moonPosY, moonPos.Z));
		}

		public static Quaternion CreateLookRotation(Vector3 direction)
		{
			Vector3 forwardVecXz = new Vector3(direction.X, 0f, direction.Z).Normalized();
			double rotY = Math.Atan2((double)forwardVecXz.X, (double)forwardVecXz.Z);
			Quaternion quaternion = Quaternion.FromAxisAngle(Vector3.UnitY, (float)rotY);
			float xyLen = new Vector2(direction.X, direction.Z).Length;
			Vector3 forwardVecZy = new Vector3(0f, direction.Y, xyLen).Normalized();
			double rotX = Math.Atan2((double)forwardVecZy.Y, (double)forwardVecZy.Z);
			Quaternion xQuat = Quaternion.FromAxisAngle(-Vector3.UnitX, (float)rotX);
			return quaternion * xQuat;
		}

		public static Quaternion CreateLookRotationMoon(Vector3 direction, Vec3f sunVecRel, Vec3f moonVec, bool flip, float proximityFactor)
		{
			Vector3 forwardVecXz = new Vector3(direction.X, 0f, direction.Z).Normalized();
			double rotY = Math.Atan2((double)forwardVecXz.X, (double)forwardVecXz.Z);
			Quaternion quaternion = Quaternion.FromAxisAngle(Vector3.UnitY, (float)rotY);
			float xyLen = new Vector2(direction.X, direction.Z).Length;
			Vector3 forwardVecZy = new Vector3(0f, direction.Y, xyLen).Normalized();
			double rotX = Math.Atan2((double)forwardVecZy.Y, (double)forwardVecZy.Z);
			Quaternion xQuat = Quaternion.FromAxisAngle(-Vector3.UnitX, (float)rotX);
			return quaternion * xQuat;
		}

		private float getAngleSunFromMoon(Vec3f sunVecRel, Vec3f moonVec)
		{
			sunVecRel.Sub(moonVec.Clone().Mul(moonVec.Dot(sunVecRel)));
			Vec3f axis = moonVec.Cross(SystemRenderSunMoon.YAxis);
			float scale = sunVecRel.Length() * axis.Length();
			if (scale == 0f)
			{
				return 0f;
			}
			return (float)Math.Acos((double)(sunVecRel.Dot(axis) / scale));
		}

		public override void Dispose(ClientMain game)
		{
			game.Platform.DeleteMesh(this.quadModel);
			game.Platform.GLDeleteTexture(this.suntextureId);
			for (int i = 0; i < this.moontextureIds.Length; i++)
			{
				game.Platform.GLDeleteTexture(this.moontextureIds[i]);
			}
			GL.DeleteQuery(this.occlQueryId);
		}

		public override EnumClientSystemType GetSystemType()
		{
			return EnumClientSystemType.Render;
		}

		private const float maxMoonProximityFactor = 0.2f;

		private MeshRef quadModel;

		private int suntextureId;

		private int[] moontextureIds;

		internal int ImageSize;

		public Matrixf ModelMat = new Matrixf();

		private int occlQueryId;

		private bool nowQuerying;

		private Matrix4 sunmat;

		private Matrix4 moonmat;

		private float targetSunSpec;

		private bool firstTickDone;

		public float sunScale = 0.04f;

		public float moonScale = 0.023100002f;

		private static Vec3f YAxis = new Vec3f(0f, 1f, 0f);
	}
}
