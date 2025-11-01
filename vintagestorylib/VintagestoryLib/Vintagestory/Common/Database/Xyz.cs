using System;

namespace Vintagestory.Common.Database
{
	public struct Xyz
	{
		public Xyz(int x, int y, int z)
		{
			this.X = x;
			this.Y = y;
			this.Z = z;
		}

		public override int GetHashCode()
		{
			return this.X ^ this.Y ^ this.Z;
		}

		public override bool Equals(object obj)
		{
			if (obj is Xyz)
			{
				Xyz other = (Xyz)obj;
				return this.X == other.X && this.Y == other.Y && this.Z == other.Z;
			}
			return base.Equals(obj);
		}

		public int X;

		public int Y;

		public int Z;
	}
}
