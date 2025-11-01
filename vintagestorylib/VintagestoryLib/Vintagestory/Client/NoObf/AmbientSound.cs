using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class AmbientSound : IEquatable<AmbientSound>, IEqualityComparer<AmbientSound>
	{
		public double DistanceTo(AmbientSound sound)
		{
			double minDistance = 9999999.0;
			for (int i = 0; i < this.BoundingBoxes.Count; i++)
			{
				for (int j = 0; j < sound.BoundingBoxes.Count; j++)
				{
					minDistance = Math.Min(minDistance, this.BoundingBoxes[i].ShortestDistanceFrom(sound.BoundingBoxes[j]));
				}
			}
			return minDistance;
		}

		public float AdjustedVolume
		{
			get
			{
				return GameMath.Clamp(GameMath.Sqrt((float)this.QuantityNearbyBlocks) / this.Ratio, 1f / this.Ratio, 1f) * this.VolumeMul;
			}
		}

		public bool Equals(AmbientSound other)
		{
			return this.AssetLoc.Equals(other.AssetLoc) && this.SectionPos.Equals(other.SectionPos);
		}

		public bool Equals(AmbientSound x, AmbientSound y)
		{
			return x.Equals(y);
		}

		public override bool Equals(object obj)
		{
			return obj is AmbientSound && (obj as AmbientSound).Equals(this);
		}

		public override int GetHashCode()
		{
			return this.AssetLoc.GetHashCode() * 23 + this.SectionPos.GetHashCode();
		}

		public void FadeToNewVolumne()
		{
			float newVolumne = this.AdjustedVolume;
			if ((double)Math.Abs(newVolumne - this.Sound.Params.Volume) > 0.02)
			{
				this.Sound.FadeTo((double)newVolumne, 1f, null);
			}
		}

		public int GetHashCode(AmbientSound obj)
		{
			return obj.AssetLoc.GetHashCode() * 23 + obj.SectionPos.GetHashCode();
		}

		internal void updatePosition(EntityPos position)
		{
			double minDist = 999999.0;
			this.tmpout.Set(-99999f, -99999f, -99999f);
			foreach (Cuboidi box in this.BoundingBoxes)
			{
				this.tmp.X = (float)GameMath.Clamp(position.X, (double)box.X1, (double)box.X2);
				this.tmp.Y = (float)GameMath.Clamp(position.Y, (double)box.Y1, (double)box.Y2);
				this.tmp.Z = (float)GameMath.Clamp(position.Z, (double)box.Z1, (double)box.Z2);
				double dist = this.tmp.DistanceSq(position.X, position.Y, position.Z);
				if (dist < minDist)
				{
					minDist = dist;
					this.tmpout.Set(this.tmp);
				}
			}
			this.Sound.SetPosition(this.tmpout);
		}

		public void RenderWireFrame(ClientMain game, WireframeCube wireframe)
		{
			foreach (Cuboidi box in this.BoundingBoxes)
			{
				float scaleX = (float)(box.X2 - box.X1);
				float scaleY = (float)(box.Y2 - box.Y1);
				float scaleZ = (float)(box.Z2 - box.Z1);
				float x = (float)box.X1;
				float y = (float)box.Y1;
				float z = (float)box.Z1;
				wireframe.Render(game.api, (double)x, (double)y, (double)z, scaleX, scaleY, scaleZ, 1f, null);
			}
		}

		public ILoadedSound Sound;

		public int QuantityNearbyBlocks;

		public AssetLocation AssetLoc;

		public List<Cuboidi> BoundingBoxes = new List<Cuboidi>();

		public Vec3i SectionPos;

		public float Ratio = 10f;

		public float VolumeMul = 1f;

		public EnumSoundType SoundType = EnumSoundType.Ambient;

		public double MaxDistanceMerge = 3.0;

		private Vec3f tmp = new Vec3f();

		private Vec3f tmpout = new Vec3f();
	}
}
