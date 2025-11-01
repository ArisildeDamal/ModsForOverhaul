using System;

namespace Vintagestory.Common
{
	public class BoxedPacket : BoxedArray
	{
		internal int Serialize(IPacket p)
		{
			CitoMemoryStream ms = new CitoMemoryStream(this);
			p.SerializeTo(ms);
			return this.Length = ms.Position();
		}

		public override void Dispose()
		{
			this.buffer = null;
			this.Length = 0;
			this.LengthSent = 0;
		}

		internal byte[] Clone(int destOffset)
		{
			int len = this.Length;
			byte[] dest = new byte[len + destOffset];
			if (len > 256)
			{
				Array.Copy(this.buffer, 0, dest, destOffset, len);
			}
			else
			{
				int fastLoopLength = len - len % 4;
				int i;
				for (i = 0; i < fastLoopLength; i += 4)
				{
					dest[destOffset] = this.buffer[i];
					dest[destOffset + 1] = this.buffer[i + 1];
					dest[destOffset + 2] = this.buffer[i + 2];
					dest[destOffset + 3] = this.buffer[i + 3];
					destOffset += 4;
				}
				while (i < len)
				{
					dest[destOffset++] = this.buffer[i];
					i++;
				}
			}
			return dest;
		}

		public int Length;

		public int LengthSent;
	}
}
