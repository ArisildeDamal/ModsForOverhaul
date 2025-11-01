using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace Vintagestory.Client
{
	public class ParticleRenderer2D
	{
		public ParticleRenderer2D(ScreenManager screenManager, ICoreClientAPI api, int poolSize = 1000)
		{
			this.screenManager = screenManager;
			this.Pool = new ParticlePool2D(api, poolSize);
		}

		public void Compose(string texture)
		{
			if (texture != null)
			{
				BitmapRef bmp = this.screenManager.GamePlatform.AssetManager.Get(texture).ToBitmap(this.screenManager.api);
				this.particleTex = ScreenManager.Platform.LoadTexture(bmp, false, 0, false);
				bmp.Dispose();
			}
		}

		public void Spawn(IParticlePropertiesProvider prop)
		{
			this.Pool.Spawn(prop);
		}

		public void Render(float dt)
		{
			if (this.oitPass == 0)
			{
				this.screenManager.GamePlatform.GlToggleBlend(true, EnumBlendMode.Standard);
			}
			else
			{
				this.screenManager.GamePlatform.GlDepthMask(true);
			}
			this.Pool.OnNewFrame(dt);
			ShaderProgramParticlesquad2d particlesquad2d = ShaderPrograms.Particlesquad2d;
			particlesquad2d.Use();
			particlesquad2d.ParticleTex2D = this.particleTex;
			particlesquad2d.WithTexture = ((this.particleTex > 0) ? 1 : 0);
			particlesquad2d.OitPass = this.oitPass;
			particlesquad2d.HeldItemMode = this.heldItemMode;
			particlesquad2d.ProjectionMatrix = this.pMatrix;
			particlesquad2d.ModelViewMatrix = this.mvMatrix;
			ScreenManager.Platform.RenderMeshInstanced(this.Pool.Model, this.Pool.QuantityAlive);
			particlesquad2d.Stop();
			this.screenManager.GamePlatform.GlDepthMask(false);
		}

		public void Dispose()
		{
			if (this.particleTex > 0)
			{
				this.screenManager.GamePlatform.GLDeleteTexture(this.particleTex);
			}
			this.Pool.Dispose();
		}

		private int particleTex;

		public ParticlePool2D Pool;

		public float[] mvMatrix = Mat4f.Create();

		public float[] pMatrix;

		public int oitPass;

		public int heldItemMode;

		private ScreenManager screenManager;
	}
}
