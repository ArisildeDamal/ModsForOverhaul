using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf
{
	public class SystemRenderParticles : ClientSystem, IAsyncParticleManager
	{
		public override string Name
		{
			get
			{
				return "rep";
			}
		}

		public IBlockAccessor BlockAccess { get; set; }

		public SystemRenderParticles(ClientMain game)
			: base(game)
		{
			this.InitAtlasAndModelPool();
			int div = ((ClientSettings.OptimizeRamMode >= 2) ? 2 : 1);
			this.mainthreadpools = new IParticlePool[]
			{
				new ParticlePoolQuads(ClientSettings.MaxQuadParticles / div, game, false),
				new ParticlePoolCubes(ClientSettings.MaxCubeParticles / div, game, false)
			};
			this.offthreadpools = new IParticlePool[]
			{
				new ParticlePoolQuads(ClientSettings.MaxAsyncQuadParticles / div, game, true),
				new ParticlePoolCubes(ClientSettings.MaxAsyncCubeParticles / div, game, true)
			};
			game.particleManager.Init(this);
			CommandArgumentParsers parsers = game.api.ChatCommands.Parsers;
			game.api.ChatCommands.GetOrCreate("debug").BeginSubCommand("fountain").WithDescription("Toggle Particle fountain")
				.WithArgs(new ICommandArgumentParser[]
				{
					parsers.WordRange("type", new string[] { "quad", "cube" }),
					parsers.OptionalInt("quantity", 1)
				})
				.HandleWith(new OnCommandDelegate(this.OnToggleParticleFountain))
				.EndSubCommand()
				.BeginSubCommand("asyncfountain")
				.WithDescription("Toggle async Particle fountain")
				.WithArgs(new ICommandArgumentParser[]
				{
					parsers.WordRange("type", new string[] { "quad", "cube" }),
					parsers.OptionalInt("quantity", 1)
				})
				.HandleWith(new OnCommandDelegate(this.OnToggleAsyncParticleFountain))
				.EndSubCommand();
			this.renderParticles = ClientSettings.RenderParticles;
			game.eventManager.RegisterRenderer(new Action<float>(this.OnRenderFrame3D), EnumRenderStage.Opaque, "rep-opa", 0.6);
			game.eventManager.RegisterRenderer(new Action<float>(this.OnRenderFrame3DOIT), EnumRenderStage.OIT, "rep-oit", 0.6);
		}

		internal override void OnLevelFinalize()
		{
			base.OnLevelFinalize();
			this.ready = true;
			this.BlockAccess = new BlockAccessorCaching(this.game.WorldMap, this.game, false, false);
		}

		private void InitAtlasAndModelPool()
		{
		}

		private TextCommandResult OnToggleParticleFountain(TextCommandCallingArgs args)
		{
			int quantityFountainParticles = (int)args[1];
			this.fountainParticle.MinPos = this.game.EntityPlayer.Pos.XYZ;
			this.fountainParticle.MinQuantity = (float)quantityFountainParticles;
			if (args[0] as string == "quad")
			{
				this.fountainParticle.ParticleModel = EnumParticleModel.Quad;
			}
			else
			{
				this.fountainParticle.ParticleModel = EnumParticleModel.Cube;
			}
			return TextCommandResult.Success("", null);
		}

		private TextCommandResult OnToggleAsyncParticleFountain(TextCommandCallingArgs args)
		{
			this.OnToggleParticleFountain(args);
			if (!this.asyncfountain)
			{
				this.asyncfountain = true;
				this.game.api.eventapi.RegisterAsyncParticleSpawner(delegate(float dt, IAsyncParticleManager mgr)
				{
					mgr.Spawn(this.fountainParticle);
					return this.asyncfountain;
				});
			}
			else
			{
				this.asyncfountain = false;
				this.fountainParticle.MinQuantity = 0f;
			}
			return TextCommandResult.Success("", null);
		}

		private void UpdateParticleFountain()
		{
			if (this.game.IsPaused || this.asyncfountain)
			{
				return;
			}
			if (this.fountainParticle.MinQuantity > 0f)
			{
				this.mainthreadpools[(int)this.fountainParticle.ParticleModel].SpawnParticles(this.fountainParticle);
			}
		}

		public void OnRenderFrame3D(float deltaTime)
		{
			this.UpdateParticleFountain();
			ShaderProgramParticlescube particlescube = ShaderPrograms.Particlescube;
			particlescube.Use();
			this.game.Platform.GlToggleBlend(true, EnumBlendMode.Standard);
			this.Render(1, deltaTime);
			particlescube.Stop();
		}

		public void OnRenderFrame3DOIT(float deltaTime)
		{
			ShaderProgramParticlesquad particlesquad = ShaderPrograms.Particlesquad;
			particlesquad.Use();
			this.Render(0, deltaTime);
			particlesquad.Stop();
		}

		private void Render(int poolindex, float dt)
		{
			if (!this.renderParticles)
			{
				return;
			}
			this.game.GlPushMatrix();
			this.game.GlLoadMatrix(this.game.MainCamera.CameraMatrixOrigin);
			ShaderProgramBase currentShaderProgram = ShaderProgramBase.CurrentShaderProgram;
			((IShaderProgram)currentShaderProgram).Uniform("rgbaFogIn", this.game.AmbientManager.BlendedFogColor);
			((IShaderProgram)currentShaderProgram).Uniform("rgbaAmbientIn", this.game.AmbientManager.BlendedAmbientColor);
			((IShaderProgram)currentShaderProgram).Uniform("fogMinIn", this.game.AmbientManager.BlendedFogMin);
			((IShaderProgram)currentShaderProgram).Uniform("fogDensityIn", this.game.AmbientManager.BlendedFogDensity);
			((IShaderProgram)currentShaderProgram).UniformMatrix("projectionMatrix", this.game.CurrentProjectionMatrix);
			((IShaderProgram)currentShaderProgram).UniformMatrix("modelViewMatrix", this.game.CurrentModelViewMatrix);
			IParticlePool poolm = this.mainthreadpools[poolindex];
			IParticlePool poolo = this.offthreadpools[poolindex];
			poolm.OnNewFrame(dt, this.game.EntityPlayer.CameraPos);
			poolo.OnNewFrame(dt, this.game.EntityPlayer.CameraPos);
			this.game.Platform.RenderMeshInstanced(poolm.Model, poolm.QuantityAlive);
			this.game.Platform.RenderMeshInstanced(poolo.Model, poolo.QuantityAlive);
			((IShaderProgram)currentShaderProgram).Stop();
			this.game.GlPopMatrix();
		}

		public override int SeperateThreadTickIntervalMs()
		{
			return 20;
		}

		public override void OnSeperateThreadGameTick(float dt)
		{
			if (!this.ready || this.game.IsPaused)
			{
				return;
			}
			dt = Math.Min(1f, dt);
			this.accumSpawn += dt;
			this.accumSpawn = Math.Min(this.accumSpawn, 1f);
			while (this.accumSpawn >= 0.033f)
			{
				List<ContinousParticleSpawnTaskDelegate> list = this.game.asyncParticleSpawners;
				int count = list.Count;
				for (int i = 0; i < count; i++)
				{
					object obj = this.game.asyncParticleSpawnersLock;
					ContinousParticleSpawnTaskDelegate handler;
					lock (obj)
					{
						handler = list[i];
					}
					if (!handler(dt, this))
					{
						obj = this.game.asyncParticleSpawnersLock;
						lock (obj)
						{
							list.RemoveAt(i);
							i--;
							count--;
						}
					}
				}
				this.accumSpawn -= 0.033f;
			}
			this.offthreadpools[0].OnNewFrameOffThread(dt, this.game.EntityPlayer.CameraPos);
			this.offthreadpools[1].OnNewFrameOffThread(dt, this.game.EntityPlayer.CameraPos);
		}

		public int Spawn(IParticlePropertiesProvider particleProperties)
		{
			return this.game.particleManager.SpawnParticlesOffThread(particleProperties);
		}

		public int ParticlesAlive(EnumParticleModel model)
		{
			return this.game.particleManager.ParticlesAlive(model);
		}

		public override EnumClientSystemType GetSystemType()
		{
			return EnumClientSystemType.Misc;
		}

		public override void Dispose(ClientMain game)
		{
			this.ready = false;
			for (int i = 0; i < this.mainthreadpools.Length; i++)
			{
				this.mainthreadpools[i].Dipose();
			}
			for (int j = 0; j < this.offthreadpools.Length; j++)
			{
				this.offthreadpools[j].Dipose();
			}
			ICachingBlockAccessor cachingBlockAccessor = this.BlockAccess as ICachingBlockAccessor;
			if (cachingBlockAccessor == null)
			{
				return;
			}
			cachingBlockAccessor.Dispose();
		}

		internal IParticlePool[] mainthreadpools;

		internal IParticlePool[] offthreadpools;

		private bool renderParticles = true;

		private bool ready;

		private SimpleParticleProperties fountainParticle = new SimpleParticleProperties
		{
			AddPos = new Vec3d(),
			AddQuantity = 0f,
			Color = ColorUtil.ToRgba(255, 0, 200, 50),
			GravityEffect = 1f,
			LifeLength = 1f,
			MinVelocity = new Vec3f(-4f, 10f, -4f),
			AddVelocity = new Vec3f(8f, 15f, 8f),
			MinSize = 0.1f,
			MaxSize = 1f
		};

		private bool asyncfountain;

		private float accumSpawn;
	}
}
