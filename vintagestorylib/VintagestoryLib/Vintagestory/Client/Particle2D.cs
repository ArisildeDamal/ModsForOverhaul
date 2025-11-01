using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client
{
	public class Particle2D : ParticleBase
	{
		public override void TickFixedStep(float dt, ICoreClientAPI api, ParticlePhysics physicsSim)
		{
			this.TickNow(dt, dt, api, physicsSim);
		}

		public override void TickNow(float lifedt, float pdt, ICoreClientAPI api, ParticlePhysics physicsSim)
		{
			this.SecondsAlive += lifedt;
			if (this.SecondsAlive > this.LifeLength)
			{
				this.Alive = false;
			}
			if (this.VelocityEvolve != null)
			{
				float relLife = this.SecondsAlive / this.LifeLength;
				this.Position.X += (double)(this.Velocity.X * this.VelocityEvolve[0].nextFloat(0f, relLife) * pdt);
				this.Position.Y += (double)(this.Velocity.Y * this.VelocityEvolve[1].nextFloat(0f, relLife) * pdt);
				this.Position.Z += (double)(this.Velocity.Z * this.VelocityEvolve[2].nextFloat(0f, relLife) * pdt);
			}
			else
			{
				this.Position.X += (double)(this.Velocity.X * pdt);
				this.Position.Y += (double)(this.Velocity.Y * pdt);
				this.Position.Z += (double)(this.Velocity.Z * pdt);
			}
			if (this.ParentVelocity != null)
			{
				this.Position.Add((double)(this.ParentVelocity.X * this.ParentVelocityWeight * pdt), (double)(this.ParentVelocity.Y * this.ParentVelocityWeight * pdt), (double)(this.ParentVelocity.Z * this.ParentVelocityWeight * pdt));
			}
			if (this.randomVelocityChange)
			{
				if (this.seq > 0f)
				{
					this.Velocity.X += this.dir * this.AccelerationX.nextFloat(0f, this.seq) * pdt * 10f * this.SizeMultiplier;
					this.Velocity.Y += this.AccelerationY.nextFloat(0f, this.seq) * pdt * 3f * this.SizeMultiplier;
					this.seq += pdt / 3f;
					if (this.seq > 2f)
					{
						this.seq = 0f;
					}
					if (Particle2D.rand.NextDouble() < 0.005)
					{
						this.seq = 0f;
					}
				}
				else
				{
					this.Velocity.X += (this.StartingVelocity.X - this.Velocity.X) * pdt;
					this.Velocity.Y += (this.StartingVelocity.Y - this.Velocity.Y) * pdt;
				}
				if (Particle2D.rand.NextDouble() < 0.005)
				{
					this.seq = (float)Particle2D.rand.NextDouble() * 0.8f;
					this.dir = (float)(Particle2D.rand.Next(2) * 2 - 1);
				}
			}
			this.Alive = this.SecondsAlive < this.LifeLength;
		}

		public override void UpdateBuffers(MeshData buffer, Vec3d cameraPos, ref int posPosition, ref int rgbaPosition, ref int flagPosition)
		{
			float relLife = this.SecondsAlive / this.LifeLength;
			byte alpha = this.Color[3];
			if (this.OpacityEvolve != null)
			{
				alpha = (byte)GameMath.Clamp(this.OpacityEvolve.nextFloat((float)alpha, relLife), 0f, 255f);
			}
			float[] values = buffer.CustomFloats.Values;
			int num = posPosition;
			posPosition = num + 1;
			values[num] = (float)this.Color[0] / 255f;
			float[] values2 = buffer.CustomFloats.Values;
			num = posPosition;
			posPosition = num + 1;
			values2[num] = (float)this.Color[1] / 255f;
			float[] values3 = buffer.CustomFloats.Values;
			num = posPosition;
			posPosition = num + 1;
			values3[num] = (float)this.Color[2] / 255f;
			float[] values4 = buffer.CustomFloats.Values;
			num = posPosition;
			posPosition = num + 1;
			values4[num] = (float)alpha / 255f;
			float[] values5 = buffer.CustomFloats.Values;
			num = posPosition;
			posPosition = num + 1;
			values5[num] = (float)this.Position.X;
			float[] values6 = buffer.CustomFloats.Values;
			num = posPosition;
			posPosition = num + 1;
			values6[num] = (float)this.Position.Y;
			float[] values7 = buffer.CustomFloats.Values;
			num = posPosition;
			posPosition = num + 1;
			values7[num] = (float)this.Position.Z;
			float[] values8 = buffer.CustomFloats.Values;
			num = posPosition;
			posPosition = num + 1;
			values8[num] = ((this.SizeEvolve != null) ? this.SizeEvolve.nextFloat(this.SizeMultiplier, relLife) : this.SizeMultiplier);
			float[] values9 = buffer.CustomFloats.Values;
			num = posPosition;
			posPosition = num + 1;
			values9[num] = (float)this.VertexFlags / 255f;
		}

		public void SetAlive(float GravityEffect)
		{
			this.Alive = true;
			this.SecondsAlive = 0f;
		}

		public Vec3f StartingVelocity = new Vec3f(0f, 0f, 0f);

		public Vec3f ParentVelocity;

		public float ParentVelocityWeight;

		private float seq;

		private float dir = 1f;

		public bool randomVelocityChange = true;

		private EvolvingNatFloat AccelerationX = EvolvingNatFloat.create(EnumTransformFunction.SINUS, 6.2831855f);

		private EvolvingNatFloat AccelerationY = EvolvingNatFloat.create(EnumTransformFunction.COSINUS, 7.539823f);

		public float SizeMultiplier = 1f;

		public float ParticleHeight;

		public EvolvingNatFloat SizeEvolve;

		public EvolvingNatFloat[] VelocityEvolve;

		public byte[] Color;

		public EvolvingNatFloat OpacityEvolve;

		private static Random rand = new Random();
	}
}
