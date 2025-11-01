using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf
{
	public class ParticlePoolQuads : IParticlePool
	{
		public MeshRef Model
		{
			get
			{
				return this.particleModelRef;
			}
		}

		public int QuantityAlive { get; set; }

		internal virtual float ParticleHeight
		{
			get
			{
				return 0.5f;
			}
		}

		public IBlockAccessor BlockAccess
		{
			get
			{
				return this.partPhysics.BlockAccess;
			}
		}

		public virtual MeshData LoadModel()
		{
			return QuadMeshUtilExt.GetCustomQuadModelData(0f, 0f, 0f, 0.25f, 0.25f, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, 0);
		}

		public ParticlePoolQuads(int poolSize, ClientMain game, bool offthread)
		{
			this.offthread = offthread;
			this.poolSize = poolSize;
			this.game = game;
			this.partPhysics = new ParticlePhysics(new BlockAccessorReadLockfree(game.WorldMap, game));
			if (offthread)
			{
				this.partPhysics.PhysicsTickTime = 0.125f;
			}
			this.ParticlesPool = new FastParticlePool(poolSize, () => new ParticleGeneric());
			MeshData particleModel = this.LoadModel();
			particleModel.CustomFloats = new CustomMeshDataPartFloat
			{
				Instanced = true,
				StaticDraw = false,
				Values = new float[poolSize * 4],
				InterleaveSizes = new int[] { 3, 1 },
				InterleaveStride = 16,
				InterleaveOffsets = new int[] { 0, 12 },
				Count = poolSize * 4
			};
			particleModel.CustomBytes = new CustomMeshDataPartByte
			{
				Conversion = DataConversion.NormalizedFloat,
				Instanced = true,
				StaticDraw = false,
				Values = new byte[poolSize * 12],
				InterleaveSizes = new int[] { 4, 4, 4 },
				InterleaveStride = 12,
				InterleaveOffsets = new int[] { 0, 4, 8 },
				Count = poolSize * 12
			};
			particleModel.Flags = new int[poolSize];
			particleModel.FlagsInstanced = true;
			this.particleModelRef = game.Platform.UploadMesh(particleModel);
			if (offthread)
			{
				this.updateBuffers = new MeshData[5];
				this.cameraPos = new Vec3d[5];
				this.tickTimes = new float[5];
				this.velocities = new float[5][];
				for (int i = 0; i < 5; i++)
				{
					this.tickTimes[i] = this.partPhysics.PhysicsTickTime;
					this.velocities[i] = new float[3 * poolSize];
					this.cameraPos[i] = new Vec3d();
					this.updateBuffers[i] = this.genUpdateBuffer();
				}
				return;
			}
			this.updateBuffer = this.genUpdateBuffer();
		}

		private MeshData genUpdateBuffer()
		{
			return new MeshData(true)
			{
				CustomFloats = new CustomMeshDataPartFloat
				{
					Values = new float[this.poolSize * 4],
					Count = this.poolSize * 4
				},
				CustomBytes = new CustomMeshDataPartByte
				{
					Values = new byte[this.poolSize * 12],
					Count = this.poolSize * 12
				},
				Flags = new int[this.poolSize],
				FlagsInstanced = true
			};
		}

		public int SpawnParticles(IParticlePropertiesProvider particleProperties)
		{
			float speed = 5f / GameMath.Sqrt(this.currentGamespeed);
			int spawned = 0;
			if (this.QuantityAlive * 100 >= this.game.particleLevel * this.poolSize)
			{
				return 0;
			}
			float quantity = particleProperties.Quantity * this.currentGamespeed;
			while ((float)spawned < quantity && this.ParticlesPool.FirstDead != null && this.rand.NextDouble() <= (double)(quantity - (float)spawned))
			{
				int color = particleProperties.GetRgbaColor(this.game.api);
				if (color == 0)
				{
					quantity -= 0.5f;
				}
				else
				{
					ParticleGeneric particle = (ParticleGeneric)this.ParticlesPool.ReviveOne();
					particle.SecondaryParticles = particleProperties.SecondaryParticles;
					particle.DeathParticles = particleProperties.DeathParticles;
					particleProperties.BeginParticle();
					particle.Position.Set(particleProperties.Pos);
					particle.Velocity.Set(particleProperties.GetVelocity(particle.Position));
					particle.ParentVelocity = particleProperties.ParentVelocity;
					particle.ParentVelocityWeight = particleProperties.ParentVelocityWeight;
					particle.Bounciness = particleProperties.Bounciness;
					particle.StartingVelocity.Set(particle.Velocity);
					particle.SizeMultiplier = particleProperties.Size;
					particle.ParticleHeight = this.ParticleHeight;
					particle.ColorRed = (byte)color;
					particle.ColorGreen = (byte)(color >> 8);
					particle.ColorBlue = (byte)(color >> 16);
					particle.ColorAlpha = (byte)(color >> 24);
					particle.LightEmission = particleProperties.LightEmission;
					particle.VertexFlags = particleProperties.VertexFlags;
					particle.SelfPropelled = particleProperties.SelfPropelled;
					particle.LifeLength = particleProperties.LifeLength * speed;
					particle.TerrainCollision = particleProperties.TerrainCollision;
					particle.GravityStrength = particleProperties.GravityEffect * GlobalConstants.GravityStrengthParticle * 40f;
					particle.SwimOnLiquid = particleProperties.SwimOnLiquid;
					particle.DieInLiquid = particleProperties.DieInLiquid;
					particle.DieInAir = particleProperties.DieInAir;
					particle.DieOnRainHeightmap = particleProperties.DieOnRainHeightmap;
					particle.OpacityEvolve = particleProperties.OpacityEvolve;
					particle.RedEvolve = particleProperties.RedEvolve;
					particle.GreenEvolve = particleProperties.GreenEvolve;
					particle.BlueEvolve = particleProperties.BlueEvolve;
					particle.SizeEvolve = particleProperties.SizeEvolve;
					particle.VelocityEvolve = particleProperties.VelocityEvolve;
					particle.RandomVelocityChange = particleProperties.RandomVelocityChange;
					particle.Spawned(this.partPhysics);
					spawned++;
				}
			}
			return spawned;
		}

		public bool ShouldRender()
		{
			return this.ParticlesPool.AliveCount > 0;
		}

		public void OnNewFrame(float dt, Vec3d cameraPos)
		{
			if (this.game.IsPaused)
			{
				return;
			}
			if (this.offthread)
			{
				this.ProcessParticlesFromOffThread(dt, cameraPos);
				return;
			}
			this.currentGamespeed = this.game.Calendar.SpeedOfTime / 60f;
			dt *= this.currentGamespeed;
			ParticleBase particle = this.ParticlesPool.FirstAlive;
			int posPosition = 0;
			int rgbaPosition = 0;
			int flagPosition = 0;
			while (particle != null)
			{
				particle.TickFixedStep(dt, this.game.api, this.partPhysics);
				if (!particle.Alive)
				{
					ParticleBase next = particle.Next;
					this.ParticlesPool.Kill(particle);
					particle = next;
				}
				else
				{
					particle.UpdateBuffers(this.updateBuffer, cameraPos, ref posPosition, ref rgbaPosition, ref flagPosition);
					particle = particle.Next;
				}
			}
			((IWorldAccessor)this.game).FrameProfiler.Mark("particles-tick");
			this.updateBuffer.CustomFloats.Count = this.ParticlesPool.AliveCount * 4;
			this.updateBuffer.CustomBytes.Count = this.ParticlesPool.AliveCount * 12;
			this.updateBuffer.VerticesCount = this.ParticlesPool.AliveCount;
			this.QuantityAlive = this.ParticlesPool.AliveCount;
			this.UpdateDebugScreen();
			this.game.Platform.UpdateMesh(this.particleModelRef, this.updateBuffer);
			((IWorldAccessor)this.game).FrameProfiler.Mark("particles-updatemesh");
		}

		private void ProcessParticlesFromOffThread(float dt, Vec3d cameraPos)
		{
			this.accumPhysics += dt;
			float ticktime = this.tickTimes[this.readPosition];
			if (this.accumPhysics >= ticktime)
			{
				object obj = this.advanceCountLock;
				lock (obj)
				{
					if (this.advanceCount > 0)
					{
						this.readPosition = (this.readPosition + 1) % this.updateBuffers.Length;
						this.advanceCount--;
						this.accumPhysics -= ticktime;
						ticktime = this.tickTimes[this.readPosition];
					}
				}
				if (this.accumPhysics > 1f)
				{
					this.accumPhysics = 0f;
				}
			}
			float step = dt / ticktime;
			MeshData buffer = this.updateBuffers[this.readPosition];
			float[] velocity = this.velocities[this.readPosition];
			int cnt = (this.QuantityAlive = buffer.VerticesCount);
			float camdX = (float)(this.cameraPos[this.readPosition].X - cameraPos.X);
			float camdY = (float)(this.cameraPos[this.readPosition].Y - cameraPos.Y);
			float camdZ = (float)(this.cameraPos[this.readPosition].Z - cameraPos.Z);
			this.cameraPos[this.readPosition].X -= (double)camdX;
			this.cameraPos[this.readPosition].Y -= (double)camdY;
			this.cameraPos[this.readPosition].Z -= (double)camdZ;
			float[] nowFloats = buffer.CustomFloats.Values;
			for (int i = 0; i < cnt; i++)
			{
				int a = i * 4;
				nowFloats[a] += camdX + velocity[i * 3] * step;
				a++;
				nowFloats[a] += camdY + velocity[i * 3 + 1] * step;
				a++;
				nowFloats[a] += camdZ + velocity[i * 3 + 2] * step;
			}
			this.game.Platform.UpdateMesh(this.particleModelRef, buffer);
			if (this.ModelType == EnumParticleModel.Quad)
			{
				if (this.game.extendedDebugInfo)
				{
					this.game.DebugScreenInfo["asyncquadparticlepool"] = "Async Quad Particle pool: " + this.ParticlesPool.AliveCount.ToString() + " / " + ((int)((float)this.poolSize * (float)this.game.particleLevel / 100f)).ToString();
				}
				else
				{
					this.game.DebugScreenInfo["asyncquadparticlepool"] = "";
				}
			}
			else if (this.game.extendedDebugInfo)
			{
				this.game.DebugScreenInfo["asynccubeparticlepool"] = "Async Cube Particle pool: " + this.ParticlesPool.AliveCount.ToString() + " / " + ((int)((float)this.poolSize * (float)this.game.particleLevel / 100f)).ToString();
			}
			else
			{
				this.game.DebugScreenInfo["asynccubeparticlepool"] = "";
			}
			((IWorldAccessor)this.game).FrameProfiler.Mark("otparticles-tick");
		}

		public void OnNewFrameOffThread(float dt, Vec3d cameraPos)
		{
			if (this.game.IsPaused || !this.offthread)
			{
				return;
			}
			object obj = this.advanceCountLock;
			lock (obj)
			{
				if (this.advanceCount >= this.updateBuffers.Length - 1)
				{
					return;
				}
			}
			this.currentGamespeed = this.game.Calendar.SpeedOfTime / 60f;
			ParticleBase particle = this.ParticlesPool.FirstAlive;
			int posPosition = 0;
			int rgbaPosition = 0;
			int flagPosition = 0;
			MeshData updateBuffer = this.updateBuffers[this.writePosition];
			float[] velocity = this.velocities[this.writePosition];
			Vec3d curCamPos = this.cameraPos[this.writePosition].Set(cameraPos);
			if (this.ParticlesPool.AliveCount < 20000)
			{
				this.partPhysics.PhysicsTickTime = 0.0625f;
			}
			else
			{
				this.partPhysics.PhysicsTickTime = 0.125f;
			}
			float pdt = Math.Max(this.partPhysics.PhysicsTickTime, dt);
			float spdt = pdt * this.currentGamespeed;
			int i = 0;
			while (particle != null)
			{
				double x = particle.Position.X;
				double y = particle.Position.Y;
				double z = particle.Position.Z;
				particle.TickNow(spdt, spdt, this.game.api, this.partPhysics);
				if (!particle.Alive)
				{
					ParticleBase next = particle.Next;
					this.ParticlesPool.Kill(particle);
					particle = next;
				}
				else
				{
					velocity[i * 3] = (particle.prevPosDeltaX = (float)(particle.Position.X - x));
					velocity[i * 3 + 1] = (particle.prevPosDeltaY = (float)(particle.Position.Y - y));
					velocity[i * 3 + 2] = (particle.prevPosDeltaZ = (float)(particle.Position.Z - z));
					i++;
					particle.UpdateBuffers(updateBuffer, curCamPos, ref posPosition, ref rgbaPosition, ref flagPosition);
					particle = particle.Next;
				}
			}
			updateBuffer.CustomFloats.Count = i * 4;
			updateBuffer.CustomBytes.Count = i * 12;
			updateBuffer.VerticesCount = i;
			this.tickTimes[this.writePosition] = Math.Min(pdt, 1f);
			this.writePosition = (this.writePosition + 1) % this.updateBuffers.Length;
			obj = this.advanceCountLock;
			lock (obj)
			{
				this.advanceCount++;
			}
		}

		internal virtual void UpdateDebugScreen()
		{
			if (this.game.extendedDebugInfo)
			{
				this.game.DebugScreenInfo["quadparticlepool"] = "Quad Particle pool: " + this.ParticlesPool.AliveCount.ToString() + " / " + ((int)((float)this.poolSize * (float)this.game.particleLevel / 100f)).ToString();
				return;
			}
			this.game.DebugScreenInfo["quadparticlepool"] = "";
		}

		public void Dipose()
		{
			this.particleModelRef.Dispose();
		}

		public FastParticlePool ParticlesPool;

		protected MeshRef particleModelRef;

		protected MeshData updateBuffer;

		protected MeshData[] updateBuffers;

		protected Vec3d[] cameraPos;

		protected float[] tickTimes;

		protected float[][] velocities;

		private int writePosition = 1;

		private int readPosition;

		private object advanceCountLock = new object();

		private int advanceCount;

		protected int poolSize;

		protected ClientMain game;

		protected Random rand = new Random();

		private float currentGamespeed;

		private ParticlePhysics partPhysics;

		private bool offthread;

		protected EnumParticleModel ModelType;

		private float accumPhysics;
	}
}
