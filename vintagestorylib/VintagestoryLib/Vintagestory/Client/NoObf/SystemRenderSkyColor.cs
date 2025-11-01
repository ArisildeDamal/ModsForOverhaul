using System;
using SkiaSharp;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	internal class SystemRenderSkyColor : ClientSystem
	{
		public override string Name
		{
			get
			{
				return "resc";
			}
		}

		public SystemRenderSkyColor(ClientMain game)
			: base(game)
		{
			MeshData modeldata = ModelIcosahedronUtil.genIcosahedron(3, 250f);
			modeldata.Uv = null;
			this.skyIcosahedron = game.Platform.UploadMesh(modeldata);
			game.eventManager.RegisterRenderer(new Action<float>(this.OnRenderFrame3D), EnumRenderStage.Opaque, this.Name, 0.2);
		}

		internal override void OnLevelFinalize()
		{
			this.skyTexture = this.game.Platform.CreateBitmapFromPng(this.game.AssetManager.Get("textures/environment/sky.png"));
			this.game.skyTextureId = this.game.Platform.LoadTexture(this.skyTexture, false, 1, false);
			IAsset asset = ScreenManager.Platform.AssetManager.Get("textures/environment/sunlight.png");
			BitmapRef bmp = this.game.Platform.CreateBitmapFromPng(asset);
			this.game.skyGlowTextureId = this.game.Platform.LoadTexture(bmp, true, 1, false);
			bmp.Dispose();
		}

		public void OnRenderFrame3D(float deltaTime)
		{
			WireframeModes wfmodes = this.game.api.renderapi.WireframeDebugRender;
			if (this.game.Width == 0 || this.game.Height == 0 || wfmodes.Vertex || this.skyTexture == null)
			{
				return;
			}
			float daylightadjusted = 1.25f * GameMath.Max(this.game.GameWorldCalendar.DayLightStrength - this.game.GameWorldCalendar.MoonLightStrength / 2f, 0.05f);
			float space = (float)GameMath.Clamp((this.game.Player.Entity.Pos.Y - (double)this.game.SeaLevel - 1000.0) / 30000.0, 0.0, 1.0);
			daylightadjusted = Math.Max(0f, daylightadjusted * (1f - space));
			this.game.shUniforms.SkyDaylight = daylightadjusted;
			this.game.shUniforms.DitherSeed = (this.game.frameSeed + 1) % Math.Max(1, this.game.Width * this.game.Height);
			this.game.shUniforms.SkyTextureId = this.game.skyTextureId;
			this.game.shUniforms.GlowTextureId = this.game.skyGlowTextureId;
			Vec3f Sn = this.game.GameWorldCalendar.SunPositionNormalized;
			Vec3f viewVector = EntityPos.GetViewVector(this.game.mouseYaw, this.game.mousePitch);
			this.game.GlMatrixModeModelView();
			ShaderProgramSky sky = ShaderPrograms.Sky;
			sky.Use();
			sky.Sky2D = this.game.skyTextureId;
			sky.Glow2D = this.game.skyGlowTextureId;
			sky.SunPosition = Sn;
			sky.DayLight = this.game.shUniforms.SkyDaylight;
			sky.PlayerPos = this.game.EntityPlayer.Pos.XYZ.ToVec3f();
			sky.DitherSeed = (this.game.frameSeed = (this.game.frameSeed + 1) % (this.game.Width * this.game.Height));
			sky.HorizontalResolution = this.game.Width;
			sky.PlayerToSealevelOffset = (float)this.game.EntityPlayer.Pos.Y - (float)this.game.SeaLevel;
			sky.RgbaFogIn = this.game.AmbientManager.BlendedFogColor;
			sky.RgbaAmbientIn = this.game.AmbientManager.BlendedAmbientColor;
			sky.FogDensityIn = this.game.AmbientManager.BlendedFogDensity;
			sky.FogMinIn = this.game.AmbientManager.BlendedFogMin;
			sky.HorizonFog = this.game.AmbientManager.BlendedCloudDensity;
			sky.ProjectionMatrix = this.game.CurrentProjectionMatrix;
			sky.SunsetMod = (this.game.shUniforms.SunsetMod = this.game.Calendar.SunsetMod);
			this.calcSunColor(Sn, viewVector);
			this.game.Platform.GlDisableDepthTest();
			this.game.GlPushMatrix();
			MatrixToolsd.MatFollowPlayer(this.game.MvMatrix.Top);
			sky.ModelViewMatrix = this.game.CurrentModelViewMatrix;
			this.game.Platform.RenderMesh(this.skyIcosahedron);
			this.game.GlPopMatrix();
			this.game.Reset3DProjection();
			sky.Stop();
			this.game.Platform.GlEnableDepthTest();
		}

		protected void calcSunColor(Vec3f Sn, Vec3f viewVector)
		{
			float sunIntensity = (GameMath.Clamp(Sn.Y * 1.5f, -1f, 1f) + 1f) / 2f;
			float moonIntensity = GameMath.Max(0f, (GameMath.Clamp(-Sn.Y * 1.5f, -1f, 1f) + 0.9f) / 13f);
			SKColor Ks = this.skyTexture.GetPixelRel(sunIntensity, 0.99f);
			Vec3f sun = this.game.GameWorldCalendar.ReflectColor.Clone();
			Vec3f fog = this.game.FogColorSky.Set((float)Ks.Red / 255f + moonIntensity / 2f, (float)Ks.Green / 255f + moonIntensity / 2f, (float)Ks.Blue / 255f + moonIntensity / 2f);
			float f = (sun.R + sun.G + sun.B) / 3f;
			float diff = this.game.GameWorldCalendar.DayLightStrength - f;
			float colorLoss = GameMath.Clamp(GameMath.Max(this.game.AmbientManager.BlendedFlatFogDensity * 40f, this.game.AmbientManager.BlendedCloudDensity * this.game.AmbientManager.BlendedCloudDensity), 0f, 1f);
			sun.R = colorLoss * f + (1f - colorLoss) * sun.R;
			sun.G = colorLoss * f + (1f - colorLoss) * sun.G;
			sun.B = colorLoss * f + (1f - colorLoss) * sun.B;
			this.game.AmbientManager.Sunglow.AmbientColor.Value[0] = sun.R + diff;
			this.game.AmbientManager.Sunglow.AmbientColor.Value[1] = sun.G + diff;
			this.game.AmbientManager.Sunglow.AmbientColor.Value[2] = sun.B + diff;
			this.game.AmbientManager.Sunglow.AmbientColor.Weight = 1f;
			float fac = (float)Math.Sqrt((double)((Math.Abs(Sn.Y) + 0.2f) * (Math.Abs(Sn.Y) + 0.2f) + (viewVector.Z - Sn.Z) * (viewVector.Z - Sn.Z))) / 2f;
			this.game.AmbientManager.Sunglow.FogColor.Weight = 1f - sunIntensity;
			float r = fac * fog.R + (1f - fac) * sun.R;
			float g = fac * fog.G + (1f - fac) * sun.G;
			float b = fac * fog.B + (1f - fac) * sun.B;
			r = colorLoss * f + (1f - colorLoss) * r;
			g = colorLoss * f + (1f - colorLoss) * g;
			b = colorLoss * f + (1f - colorLoss) * b;
			this.game.AmbientManager.Sunglow.FogColor.Value[0] = r;
			this.game.AmbientManager.Sunglow.FogColor.Value[1] = g;
			this.game.AmbientManager.Sunglow.FogColor.Value[2] = b;
		}

		public override EnumClientSystemType GetSystemType()
		{
			return EnumClientSystemType.Render;
		}

		public override void Dispose(ClientMain game)
		{
			game.Platform.DeleteMesh(this.skyIcosahedron);
			game.Platform.GLDeleteTexture(game.skyGlowTextureId);
			game.Platform.GLDeleteTexture(game.skyTextureId);
			BitmapRef bitmapRef = this.skyTexture;
			if (bitmapRef == null)
			{
				return;
			}
			bitmapRef.Dispose();
		}

		private MeshRef skyIcosahedron;

		private BitmapRef skyTexture;
	}
}
