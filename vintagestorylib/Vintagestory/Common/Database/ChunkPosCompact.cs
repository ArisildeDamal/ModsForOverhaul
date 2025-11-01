using System;

namespace Vintagestory.Common.Database
{
	public struct ChunkPosCompact
	{
		public readonly int X
		{
			get
			{
				return (int)this.compacted & 2097151;
			}
		}

		public readonly int Y
		{
			get
			{
				return (int)(this.compacted >> 42);
			}
		}

		public readonly int Z
		{
			get
			{
				return (int)(this.compacted >> 21) & 2097151;
			}
		}

		public ChunkPosCompact(int cx, int cy, int cz)
		{
			this.compacted = (long)((ulong)cx | (ulong)((ulong)((long)cz) << 21) | (ulong)((ulong)((long)cy) << 42));
		}

		public override int GetHashCode()
		{
			return (int)this.compacted + (int)(this.compacted >> 32) * 13;
		}

		public override bool Equals(object obj)
		{
			if (obj is ChunkPosCompact)
			{
				ChunkPosCompact other = (ChunkPosCompact)obj;
				return this.compacted == other.compacted;
			}
			return base.Equals(obj);
		}

		public static bool operator ==(ChunkPosCompact left, ChunkPosCompact right)
		{
			return left.compacted == right.compacted;
		}

		public static bool operator !=(ChunkPosCompact left, ChunkPosCompact right)
		{
			return left.compacted != right.compacted;
		}

		private const int bitSizeXZ = 21;

		private const int bitMask = 2097151;

		private readonly long compacted;
	}
}
