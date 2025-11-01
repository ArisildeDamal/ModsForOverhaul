using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace Vintagestory.Client
{
	public class ParticlePool2D
	{
		public virtual bool RenderTransparent
		{
			get
			{
				return true;
			}
		}

		public MeshRef Model
		{
			get
			{
				return this.particleModelRef;
			}
		}

		public int QuantityAlive
		{
			get
			{
				return this.ParticlesPool.AliveCount;
			}
		}

		internal virtual float ParticleHeight
		{
			get
			{
				return 0.5f;
			}
		}

		public virtual MeshData LoadModel()
		{
			MeshData customQuadModelData = QuadMeshUtilExt.GetCustomQuadModelData(0f, 0f, 0f, 10f, 10f, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, 0);
			customQuadModelData.Flags = null;
			return customQuadModelData;
		}

		public ParticlePool2D(ICoreClientAPI capi, int poolSize)
		{
			this.capi = capi;
			this.poolSize = poolSize;
			this.ParticlesPool = new FastParticlePool(poolSize, () => new Particle2D());
			MeshData particleModel = this.LoadModel();
			particleModel.CustomFloats = new CustomMeshDataPartFloat
			{
				Instanced = true,
				StaticDraw = false,
				Values = new float[poolSize * 9],
				InterleaveSizes = new int[] { 4, 3, 1, 1 },
				InterleaveStride = 36,
				InterleaveOffsets = new int[] { 0, 16, 28, 32 },
				Count = poolSize * 9
			};
			this.particleModelRef = ScreenManager.Platform.UploadMesh(particleModel);
			this.particleData = new MeshData(true);
			this.particleData.CustomFloats = new CustomMeshDataPartFloat
			{
				Values = new float[poolSize * 9],
				Count = poolSize * 9
			};
		}

		public int Spawn(IParticlePropertiesProvider particleProperties)
		{
			float speed = 5f;
			int spawned = 0;
			if (this.QuantityAlive >= this.poolSize)
			{
				return 0;
			}
			float quantity = particleProperties.Quantity;
			while ((float)spawned < quantity && this.ParticlesPool.FirstDead != null && this.rand.NextDouble() <= (double)(quantity - (float)spawned))
			{
				Particle2D particle = this.ParticlesPool.ReviveOne() as Particle2D;
				particle.ParentVelocityWeight = particleProperties.ParentVelocityWeight;
				Particle2D particle2D = particle;
				Vec3f parentVelocity = particleProperties.ParentVelocity;
				particle2D.ParentVelocity = ((parentVelocity != null) ? parentVelocity.Clone() : null);
				particleProperties.BeginParticle();
				particle.Position.Set(particleProperties.Pos);
				particle.Velocity.Set(particleProperties.GetVelocity(particle.Position));
				particle.StartingVelocity = particle.Velocity.Clone();
				particle.SizeMultiplier = particleProperties.Size;
				particle.ParticleHeight = this.ParticleHeight;
				particle.Color = ColorUtil.ToRGBABytes(particleProperties.GetRgbaColor(null));
				particle.VertexFlags = particleProperties.VertexFlags;
				particle.LifeLength = particleProperties.LifeLength * speed;
				particle.SetAlive(particleProperties.GravityEffect);
				particle.OpacityEvolve = particleProperties.OpacityEvolve;
				particle.SizeEvolve = particleProperties.SizeEvolve;
				particle.VelocityEvolve = particleProperties.VelocityEvolve;
				spawned++;
			}
			return spawned;
		}

		internal void TransformNextUpdate(Matrixf mat)
		{
			this.mat = mat;
		}

		public bool ShouldRender()
		{
			return this.ParticlesPool.AliveCount > 0;
		}

		public void OnNewFrame(float dt)
		{
			ParticleBase particle = this.ParticlesPool.FirstAlive;
			int posPosition = 0;
			int unused = 0;
			int unused2 = 0;
			while (particle != null)
			{
				particle.TickFixedStep(dt, this.capi, null);
				if (this.mat != null)
				{
					this.tmpVec.Set(particle.Position.X, particle.Position.Y, particle.Position.Z, 1.0);
					this.tmpVec = this.mat.TransformVector(this.tmpVec);
					particle.Position.X = this.tmpVec.X;
					particle.Position.Y = this.tmpVec.Y;
					particle.Position.Z = this.tmpVec.Z;
				}
				if (!particle.Alive)
				{
					ParticleBase next = particle.Next;
					this.ParticlesPool.Kill(particle);
					particle = next;
				}
				else
				{
					particle.UpdateBuffers(this.particleData, null, ref posPosition, ref unused, ref unused2);
					particle = particle.Next;
				}
			}
			this.particleData.CustomFloats.Count = this.ParticlesPool.AliveCount * 9;
			this.particleData.VerticesCount = this.ParticlesPool.AliveCount;
			ScreenManager.Platform.UpdateMesh(this.particleModelRef, this.particleData);
			this.mat = null;
		}

		public void Dispose()
		{
			this.particleModelRef.Dispose();
		}

		public FastParticlePool ParticlesPool;

		protected MeshRef particleModelRef;

		protected MeshData particleData;

		protected int poolSize;

		private ICoreClientAPI capi;

		protected Random rand = new Random();

		private Matrixf mat;

		private Vec4d tmpVec = new Vec4d();
	}
}
