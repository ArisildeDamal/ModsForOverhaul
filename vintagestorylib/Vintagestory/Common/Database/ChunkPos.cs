using System;
using System.Runtime.CompilerServices;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common.Database
{
	public struct ChunkPos
	{
		public int InternalY
		{
			get
			{
				return this.Y + this.Dimension * 1024;
			}
		}

		public ChunkPos(int xx, int yy, int zz, int dd)
		{
			this.X = xx;
			this.Y = yy;
			this.Z = zz;
			this.Dimension = dd;
		}

		public ChunkPos(int x, int internalCY, int z)
		{
			this.X = x;
			this.Y = internalCY % 1024;
			this.Z = z;
			this.Dimension = internalCY / 1024;
		}

		public ChunkPos(Vec3i vec)
		{
			this.X = vec.X;
			this.Y = vec.Y % 1024;
			this.Z = vec.Z;
			this.Dimension = vec.Y / 1024;
		}

		[Obsolete("Not dimension aware")]
		public static ChunkPos FromPosition(int x, int y, int z)
		{
			return new ChunkPos
			{
				X = x / 32,
				Y = y / 32,
				Z = z / 32,
				Dimension = 0
			};
		}

		public static ChunkPos FromPosition(int x, int y, int z, int d)
		{
			return new ChunkPos
			{
				X = x / 32,
				Y = y / 32,
				Z = z / 32,
				Dimension = d
			};
		}

		public override int GetHashCode()
		{
			return ((391 + this.X) * 23 + this.Y) * 23 + this.Z + this.Dimension * 269023;
		}

		public ulong ToChunkIndex()
		{
			return ChunkPos.ToChunkIndex(this.X, this.Y, this.Z, this.Dimension);
		}

		public static ChunkPos FromChunkIndex_saveGamev2(ulong index)
		{
			return new ChunkPos((int)index & ChunkPos.chunkMask, (int)(index >> 54) & ChunkPos.yMask, (int)(index >> 27) & ChunkPos.chunkMask, ((int)(index >> 22) & ChunkPos.dimMaskLow5Bits) + ((int)(index >> 44) & ChunkPos.dimMaskHigh5Bits));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ulong ToChunkIndex(int x, int y, int z)
		{
			if (y >= 1024)
			{
				throw new Exception("Coding bug in dimensions system: please mention to radfast");
			}
			return (ulong)x | (ulong)((ulong)((long)z) << 27) | (ulong)((ulong)((long)y) << 54);
		}

		public static ulong ToChunkIndex(int x, int y, int z, int dim)
		{
			if (y >= 1024)
			{
				throw new Exception("Coding bug in dimensions system: please mention to radfast");
			}
			ulong index = ChunkPos.ToChunkIndex(x, y, z);
			if (dim != 0)
			{
				index |= (ulong)((ulong)((long)(dim & ChunkPos.dimMaskLow5Bits)) << 22);
				index |= (ulong)((ulong)((long)(dim & ChunkPos.dimMaskHigh5Bits)) << 44);
			}
			return index;
		}

		public Vec3i ToVec3i()
		{
			return new Vec3i(this.X, this.InternalY, this.Z);
		}

		public int X;

		public int Y;

		public int Z;

		public int Dimension;

		private static int chunkMask = 4194303;

		private static int yMask = 511;

		private static int dimMaskLow5Bits = 31;

		private static int dimMaskHigh5Bits = 992;
	}
}
