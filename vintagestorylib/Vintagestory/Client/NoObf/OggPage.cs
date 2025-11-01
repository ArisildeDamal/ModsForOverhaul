using System;
using csogg;

namespace Vintagestory.Client.NoObf
{
	public class OggPage : Page
	{
		private static uint crc_entry(uint index)
		{
			uint r = index << 24;
			for (int i = 0; i < 8; i++)
			{
				if ((r & 2147483648U) != 0U)
				{
					r = (r << 1) ^ 79764919U;
				}
				else
				{
					r <<= 1;
				}
			}
			return r & uint.MaxValue;
		}

		internal int version()
		{
			return (int)(this.header_base[this.header + 4] & byte.MaxValue);
		}

		internal int continued()
		{
			return (int)(this.header_base[this.header + 5] & 1);
		}

		public new int bos()
		{
			return (int)(this.header_base[this.header + 5] & 2);
		}

		public new int eos()
		{
			return (int)(this.header_base[this.header + 5] & 4);
		}

		public new long granulepos()
		{
			return ((((((((((((((long)(this.header_base[this.header + 13] & byte.MaxValue) << 8) | (long)((ulong)(this.header_base[this.header + 12] & byte.MaxValue))) << 8) | (long)((ulong)(this.header_base[this.header + 11] & byte.MaxValue))) << 8) | (long)((ulong)(this.header_base[this.header + 10] & byte.MaxValue))) << 8) | (long)((ulong)(this.header_base[this.header + 9] & byte.MaxValue))) << 8) | (long)((ulong)(this.header_base[this.header + 8] & byte.MaxValue))) << 8) | (long)((ulong)(this.header_base[this.header + 7] & byte.MaxValue))) << 8) | (long)((ulong)(this.header_base[this.header + 6] & byte.MaxValue));
		}

		public new int serialno()
		{
			return (int)(this.header_base[this.header + 14] & byte.MaxValue) | ((int)(this.header_base[this.header + 15] & byte.MaxValue) << 8) | ((int)(this.header_base[this.header + 16] & byte.MaxValue) << 16) | ((int)(this.header_base[this.header + 17] & byte.MaxValue) << 24);
		}

		internal int pageno()
		{
			return (int)(this.header_base[this.header + 18] & byte.MaxValue) | ((int)(this.header_base[this.header + 19] & byte.MaxValue) << 8) | ((int)(this.header_base[this.header + 20] & byte.MaxValue) << 16) | ((int)(this.header_base[this.header + 21] & byte.MaxValue) << 24);
		}

		internal void checksum()
		{
			uint crc_reg = 0U;
			for (int i = 0; i < this.header_len; i++)
			{
				uint a = (uint)(this.header_base[this.header + i] & byte.MaxValue);
				uint b = (crc_reg >> 24) & 255U;
				crc_reg = (crc_reg << 8) ^ OggPage.crc_lookup[(int)(a ^ b)];
			}
			for (int j = 0; j < this.body_len; j++)
			{
				uint a = (uint)(this.body_base[this.body + j] & byte.MaxValue);
				uint b = (crc_reg >> 24) & 255U;
				crc_reg = (crc_reg << 8) ^ OggPage.crc_lookup[(int)(a ^ b)];
			}
			this.header_base[this.header + 22] = (byte)crc_reg;
			this.header_base[this.header + 23] = (byte)(crc_reg >> 8);
			this.header_base[this.header + 24] = (byte)(crc_reg >> 16);
			this.header_base[this.header + 25] = (byte)(crc_reg >> 24);
		}

		public OggPage()
		{
			if (OggPage.crc_lookup == null)
			{
				OggPage.crc_lookup = new uint[256];
				uint i = 0U;
				while ((ulong)i < (ulong)((long)OggPage.crc_lookup.Length))
				{
					OggPage.crc_lookup[(int)i] = OggPage.crc_entry(i);
					i += 1U;
				}
			}
		}

		[ThreadStatic]
		private static uint[] crc_lookup;
	}
}
