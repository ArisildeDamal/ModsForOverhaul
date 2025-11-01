using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	internal class SystemRenderNightSky : ClientSystem
	{
		public override string Name
		{
			get
			{
				return "rens";
			}
		}

		public SystemRenderNightSky(ClientMain game)
			: base(game)
		{
			MeshData modeldata = CubeMeshUtil.GetCubeOnlyScaleXyz(75f, 75f, new Vec3f());
			modeldata.Uv = null;
			modeldata.Rgba = null;
			this.nightSkyBox = game.Platform.UploadMesh(modeldata);
			game.eventManager.RegisterRenderer(new Action<float>(this.OnRenderFrame3D), EnumRenderStage.Opaque, this.Name, 0.1);
		}

		public override void OnBlockTexturesLoaded()
		{
			this.bmps = new BitmapRef[6];
			TyronThreadPool.QueueTask(new Action(this.LoadBitMaps));
		}

		private void LoadBitMaps()
		{
			this.bmps[0] = this.game.Platform.CreateBitmapFromPng(this.game.AssetManager.Get("textures/environment/stars-ft.png"));
			this.bmps[1] = this.game.Platform.CreateBitmapFromPng(this.game.AssetManager.Get("textures/environment/stars-bg.png"));
			this.bmps[2] = this.game.Platform.CreateBitmapFromPng(this.game.AssetManager.Get("textures/environment/stars-up.png"));
			this.bmps[3] = this.game.Platform.CreateBitmapFromPng(this.game.AssetManager.Get("textures/environment/stars-dn.png"));
			this.bmps[4] = this.game.Platform.CreateBitmapFromPng(this.game.AssetManager.Get("textures/environment/stars-lf.png"));
			this.bmps[5] = this.game.Platform.CreateBitmapFromPng(this.game.AssetManager.Get("textures/environment/stars-rt.png"));
			this.game.EnqueueGameLaunchTask(new Action(this.FinishBitMaps), "nightsky");
		}

		private void FinishBitMaps()
		{
			this.textureId = this.game.Platform.Load3DTextureCube(this.bmps);
			BitmapRef[] array = this.bmps;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Dispose();
			}
		}

		public void OnRenderFrame3D(float deltaTime)
		{
			float daylightAdjusted = 1.25f * GameMath.Max(this.game.GameWorldCalendar.DayLightStrength - this.game.GameWorldCalendar.MoonLightStrength / 2f, 0.05f);
			EntityPos pos = this.game.EntityPlayer.Pos;
			float space = (float)GameMath.Clamp((pos.Y - (double)this.game.SeaLevel - 1000.0) / 30000.0, 0.0, 1.0);
			daylightAdjusted = Math.Max(0f, daylightAdjusted * (1f - space));
			if (this.game.Width == 0 || (double)daylightAdjusted > 0.99)
			{
				return;
			}
			this.game.GlMatrixModeModelView();
			this.game.Platform.GlDisableCullFace();
			this.game.Platform.GlDisableDepthTest();
			ShaderProgramNightsky prog = ShaderPrograms.Nightsky;
			prog.Use();
			prog.CtexCube = this.textureId;
			prog.DayLight = daylightAdjusted;
			prog.RgbaFog = this.game.AmbientManager.BlendedFogColor;
			prog.HorizonFog = this.game.AmbientManager.BlendedCloudDensity;
			prog.PlayerToSealevelOffset = (float)pos.Y - (float)this.game.SeaLevel;
			prog.DitherSeed = (this.frameSeed = (this.frameSeed + 1) % (this.game.Width * this.game.Height));
			prog.HorizontalResolution = this.game.Width;
			prog.ProjectionMatrix = this.game.CurrentProjectionMatrix;
			prog.FogDensityIn = this.game.AmbientManager.BlendedFogDensity;
			prog.FogMinIn = this.game.AmbientManager.BlendedFogMin;
			double totalDays = this.game.GameWorldCalendar.TotalDays;
			float yearRel = this.game.GameWorldCalendar.YearRel;
			float siderealRotationAngle = (float)GameMath.Mod(totalDays - (double)yearRel, 1.0) * 6.2831855f;
			float latitude = (float)this.game.GameWorldCalendar.OnGetLatitude(pos.Z);
			float theta = (float)Math.Acos((double)(GameMath.Sin(latitude * 1.5707964f) * GameMath.Sin(0.40910518f) + GameMath.Cos(latitude * 1.5707964f) * GameMath.Cos(0.40910518f)));
			Mat4f.Identity(this.modelMatrix);
			Mat4f.Rotate(this.modelMatrix, this.modelMatrix, siderealRotationAngle, new float[]
			{
				0f,
				-GameMath.Sin(theta),
				GameMath.Cos(theta)
			});
			prog.ModelMatrix = this.modelMatrix;
			this.game.GlPushMatrix();
			MatrixToolsd.MatFollowPlayer(this.game.MvMatrix.Top);
			prog.ViewMatrix = this.game.CurrentModelViewMatrix;
			this.game.Platform.RenderMesh(this.nightSkyBox);
			this.game.GlPopMatrix();
			prog.Stop();
			this.game.Platform.GlEnableDepthTest();
			this.game.Platform.UnBindTextureCubeMap();
		}

		public override void Dispose(ClientMain game)
		{
			game.Platform.DeleteMesh(this.nightSkyBox);
			game.Platform.GLDeleteTexture(this.textureId);
		}

		public override EnumClientSystemType GetSystemType()
		{
			return EnumClientSystemType.Render;
		}

		private MeshRef nightSkyBox;

		private int textureId;

		private int frameSeed;

		private float[] modelMatrix = Mat4f.Create();

		private BitmapRef[] bmps;
	}
}
