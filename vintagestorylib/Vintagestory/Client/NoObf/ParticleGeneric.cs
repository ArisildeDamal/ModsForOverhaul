using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public sealed class ParticleGeneric : ParticleBase
	{
		public ParticleGeneric()
		{
			this.SecondarySpawnTimers = new float[4];
		}

		public override void TickNow(float lifedt, float pdt, ICoreClientAPI api, ParticlePhysics physicsSim)
		{
			this.SecondsAlive += lifedt;
			if (this.SecondaryParticles != null)
			{
				for (int i = 0; i < this.SecondaryParticles.Length; i++)
				{
					this.SecondarySpawnTimers[i] += pdt;
					IParticlePropertiesProvider particleProps = this.SecondaryParticles[i];
					if (this.SecondarySpawnTimers[i] > particleProps.SecondarySpawnInterval)
					{
						this.SecondarySpawnTimers[i] = 0f;
						particleProps.PrepareForSecondarySpawn(this);
						api.World.SpawnParticles(particleProps, null);
					}
				}
			}
			if (this.TerrainCollision && this.SelfPropelled)
			{
				this.Velocity.X += (this.StartingVelocity.X - this.Velocity.X) * 0.02f;
				this.Velocity.Y += (this.StartingVelocity.Y - this.Velocity.Y) * 0.02f;
				this.Velocity.Z += (this.StartingVelocity.Z - this.Velocity.Z) * 0.02f;
			}
			this.Velocity.Y -= this.GravityStrength * pdt;
			float height = this.ParticleHeight * this.SizeMultiplier;
			physicsSim.HandleBoyancy(this.Position, this.Velocity, this.SwimOnLiquid, this.GravityStrength, pdt, height);
			if (this.VelocityEvolve != null)
			{
				float relLife = this.SecondsAlive / this.LifeLength;
				this.motion.Set(this.Velocity.X * this.VelocityEvolve[0].nextFloat(0f, relLife) * pdt, this.Velocity.Y * this.VelocityEvolve[1].nextFloat(0f, relLife) * pdt, this.Velocity.Z * this.VelocityEvolve[2].nextFloat(0f, relLife) * pdt);
			}
			else
			{
				this.motion.Set(this.Velocity.X * pdt, this.Velocity.Y * pdt, this.Velocity.Z * pdt);
			}
			if (this.ParentVelocity != null)
			{
				this.motion.Add(this.ParentVelocity.X * this.ParentVelocityWeight * pdt * this.tdragnow, this.ParentVelocity.Y * this.ParentVelocityWeight * pdt * this.tdragnow, this.ParentVelocity.Z * this.ParentVelocityWeight * pdt * this.tdragnow);
			}
			if (this.TerrainCollision)
			{
				base.updatePositionWithCollision(pdt, api, physicsSim, height);
			}
			else
			{
				this.Position.X += (double)this.motion.X;
				this.Position.Y += (double)this.motion.Y;
				this.Position.Z += (double)this.motion.Z;
			}
			if (this.RandomVelocityChange)
			{
				if (this.seq > 0f)
				{
					this.Velocity.X += this.dir * ParticleGeneric.AccelerationX.nextFloat(0f, this.seq) * pdt * 4f * this.SizeMultiplier;
					this.Velocity.Z += ParticleGeneric.AccelerationZ.nextFloat(0f, this.seq) * pdt * 3f * this.SizeMultiplier;
					this.Velocity.Y += (this.dir * ParticleGeneric.AccelerationX.nextFloat(0f, this.seq) * pdt * 10f * this.SizeMultiplier - this.dir * ParticleGeneric.AccelerationZ.nextFloat(0f, this.seq) * pdt * 3f * this.SizeMultiplier) / 10f;
					this.seq += pdt / 3f;
					if (this.seq > 2f)
					{
						this.seq = 0f;
					}
					if (api.World.Rand.NextDouble() < 0.005)
					{
						this.seq = 0f;
					}
				}
				else
				{
					this.Velocity.X += (this.StartingVelocity.X - this.Velocity.X) * pdt;
					this.Velocity.Z += (this.StartingVelocity.Z - this.Velocity.Z) * pdt;
				}
				if (api.World.Rand.NextDouble() < 0.005)
				{
					this.seq = (float)api.World.Rand.NextDouble() * 0.5f;
					this.dir = (float)(api.World.Rand.Next(2) * 2 - 1);
				}
			}
			this.Alive = this.SecondsAlive < this.LifeLength && (!this.DieInAir || physicsSim.BlockAccess.GetBlockRaw((int)this.Position.X, (int)(this.Position.Y + 0.15000000596046448), (int)this.Position.Z, 2).IsLiquid());
			this.tickCount += 1;
			if (this.tickCount > 2)
			{
				this.lightrgbs = ((this.LightEmission == int.MaxValue) ? this.LightEmission : physicsSim.BlockAccess.GetLightRGBsAsInt((int)this.Position.X, (int)this.Position.Y, (int)this.Position.Z));
				if (this.LightEmission != 0)
				{
					this.lightrgbs = Math.Max(this.lightrgbs & 255, this.LightEmission & 255) | Math.Max(this.lightrgbs & 65280, this.LightEmission & 65280) | Math.Max(this.lightrgbs & 16711680, this.LightEmission & 16711680);
				}
				if (this.DieOnRainHeightmap)
				{
					float f = 1f - this.prevPosAdvance;
					double veloX = (double)(this.prevPosDeltaX * f * pdt * 8f);
					double veloY = (double)(this.prevPosDeltaY * f * pdt * 12f);
					double veloZ = (double)(this.prevPosDeltaZ * f * pdt * 8f);
					this.Alive &= (double)physicsSim.BlockAccess.GetRainMapHeightAt((int)(this.Position.X + veloX), (int)(this.Position.Z + veloZ)) - veloY < this.Position.Y;
				}
				this.Alive &= !this.DieInLiquid || !physicsSim.BlockAccess.GetBlockRaw((int)this.Position.X, (int)(this.Position.Y + 0.15000000596046448), (int)this.Position.Z, 2).IsLiquid();
				this.tickCount = 0;
				float len = this.Velocity.Length();
				this.dirNormalizedX = (byte)(this.Velocity.X / len * 128f);
				this.dirNormalizedY = (byte)(this.Velocity.Y / len * 128f);
				this.dirNormalizedZ = (byte)(this.Velocity.Z / len * 128f);
			}
			if (!this.Alive && this.DeathParticles != null)
			{
				for (int j = 0; j < this.DeathParticles.Length; j++)
				{
					IParticlePropertiesProvider particleProps2 = this.DeathParticles[j];
					particleProps2.PrepareForSecondarySpawn(this);
					api.World.SpawnParticles(particleProps2, null);
				}
			}
		}

		public override void UpdateBuffers(MeshData buffer, Vec3d cameraPos, ref int posPosition, ref int rgbaPosition, ref int flagPosition)
		{
			float relLife = this.SecondsAlive / this.LifeLength;
			float f = 1f - this.prevPosAdvance;
			float[] values = buffer.CustomFloats.Values;
			int num = posPosition;
			posPosition = num + 1;
			values[num] = (float)(this.Position.X - (double)(this.prevPosDeltaX * f) - cameraPos.X);
			float[] values2 = buffer.CustomFloats.Values;
			num = posPosition;
			posPosition = num + 1;
			values2[num] = (float)(this.Position.Y - (double)(this.prevPosDeltaY * f) - cameraPos.Y);
			float[] values3 = buffer.CustomFloats.Values;
			num = posPosition;
			posPosition = num + 1;
			values3[num] = (float)(this.Position.Z - (double)(this.prevPosDeltaZ * f) - cameraPos.Z);
			float[] values4 = buffer.CustomFloats.Values;
			num = posPosition;
			posPosition = num + 1;
			values4[num] = ((this.SizeEvolve != null) ? this.SizeEvolve.nextFloat(this.SizeMultiplier, relLife) : this.SizeMultiplier);
			byte alpha = this.ColorAlpha;
			if (this.OpacityEvolve != null)
			{
				alpha = (byte)GameMath.Clamp(this.OpacityEvolve.nextFloat((float)alpha, relLife), 0f, 255f);
			}
			byte[] values5 = buffer.CustomBytes.Values;
			num = rgbaPosition;
			rgbaPosition = num + 1;
			values5[num] = this.dirNormalizedX;
			byte[] values6 = buffer.CustomBytes.Values;
			num = rgbaPosition;
			rgbaPosition = num + 1;
			values6[num] = this.dirNormalizedY;
			byte[] values7 = buffer.CustomBytes.Values;
			num = rgbaPosition;
			rgbaPosition = num + 1;
			values7[num] = this.dirNormalizedZ;
			rgbaPosition++;
			byte[] values8 = buffer.CustomBytes.Values;
			num = rgbaPosition;
			rgbaPosition = num + 1;
			values8[num] = (byte)this.lightrgbs;
			byte[] values9 = buffer.CustomBytes.Values;
			num = rgbaPosition;
			rgbaPosition = num + 1;
			values9[num] = (byte)(this.lightrgbs >> 8);
			byte[] values10 = buffer.CustomBytes.Values;
			num = rgbaPosition;
			rgbaPosition = num + 1;
			values10[num] = (byte)(this.lightrgbs >> 16);
			byte[] values11 = buffer.CustomBytes.Values;
			num = rgbaPosition;
			rgbaPosition = num + 1;
			values11[num] = (byte)(this.lightrgbs >> 24);
			byte[] values12 = buffer.CustomBytes.Values;
			num = rgbaPosition;
			rgbaPosition = num + 1;
			values12[num] = (byte)((float)this.ColorBlue + ((this.BlueEvolve == null) ? 0f : this.BlueEvolve.nextFloat((float)this.ColorBlue, relLife)));
			byte[] values13 = buffer.CustomBytes.Values;
			num = rgbaPosition;
			rgbaPosition = num + 1;
			values13[num] = (byte)((float)this.ColorGreen + ((this.GreenEvolve == null) ? 0f : this.GreenEvolve.nextFloat((float)this.ColorGreen, relLife)));
			byte[] values14 = buffer.CustomBytes.Values;
			num = rgbaPosition;
			rgbaPosition = num + 1;
			values14[num] = (byte)((float)this.ColorRed + ((this.RedEvolve == null) ? 0f : this.RedEvolve.nextFloat((float)this.ColorRed, relLife)));
			byte[] values15 = buffer.CustomBytes.Values;
			num = rgbaPosition;
			rgbaPosition = num + 1;
			values15[num] = alpha;
			int[] flags = buffer.Flags;
			num = flagPosition;
			flagPosition = num + 1;
			flags[num] = this.VertexFlags;
		}

		public void Spawned(ParticlePhysics physicsSim)
		{
			this.Alive = true;
			this.SecondsAlive = 0f;
			this.accum = physicsSim.PhysicsTickTime;
			this.lightrgbs = physicsSim.BlockAccess.GetLightRGBsAsInt((int)this.Position.X, (int)this.Position.Y, (int)this.Position.Z);
			if (this.SecondaryParticles != null)
			{
				for (int i = 0; i < this.SecondaryParticles.Length; i++)
				{
					this.SecondarySpawnTimers[i] = 0f;
				}
			}
		}

		private static EvolvingNatFloat AccelerationX = EvolvingNatFloat.create(EnumTransformFunction.SINUS, 6.2831855f);

		private static EvolvingNatFloat AccelerationZ = EvolvingNatFloat.create(EnumTransformFunction.COSINUS, 7.539823f);

		public Vec3f StartingVelocity = new Vec3f(0f, 0f, 0f);

		public Vec3f ParentVelocity;

		public float ParentVelocityWeight;

		public float SizeMultiplier = 1f;

		public float ParticleHeight;

		public EvolvingNatFloat SizeEvolve;

		public EvolvingNatFloat[] VelocityEvolve;

		public EvolvingNatFloat OpacityEvolve;

		public EvolvingNatFloat GreenEvolve;

		public EvolvingNatFloat RedEvolve;

		public EvolvingNatFloat BlueEvolve;

		public int LightEmission;

		public float GravityStrength;

		public bool TerrainCollision;

		public bool SelfPropelled;

		public bool DieInLiquid;

		public bool DieInAir;

		public bool DieOnRainHeightmap;

		public bool SwimOnLiquid;

		public bool RandomVelocityChange;

		public IParticlePropertiesProvider[] SecondaryParticles;

		public float[] SecondarySpawnTimers;

		public IParticlePropertiesProvider[] DeathParticles;

		private byte dirNormalizedX;

		private byte dirNormalizedY;

		private byte dirNormalizedZ;

		private float seq;

		private float dir = 1f;
	}
}
