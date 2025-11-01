using System;
using Vintagestory.Common.Convert;

namespace Vintagestory.Common
{
	public class ZStdDecompressorImpl : IZStdDecompressor
	{
		public ZStdDecompressorImpl()
		{
			this.dCtx = ZstdNative.ZSTD_createDCtx();
		}

		public unsafe void Decompress(byte[] output, byte[] input)
		{
			fixed (byte[] array = output)
			{
				byte* pDst;
				if (output == null || array.Length == 0)
				{
					pDst = null;
				}
				else
				{
					pDst = &array[0];
				}
				fixed (byte[] array2 = input)
				{
					byte* pSrc;
					if (input == null || array2.Length == 0)
					{
						pSrc = null;
					}
					else
					{
						pSrc = &array2[0];
					}
					ZstdNative.ZSTD_decompressDCtx(this.dCtx, (void*)pDst, (UIntPtr)((IntPtr)output.Length), (void*)pSrc, (UIntPtr)((IntPtr)input.Length));
				}
			}
		}

		public unsafe int Decompress(byte[] output, ReadOnlySpan<byte> input)
		{
			byte* pDst;
			if (output == null || output.Length == 0)
			{
				pDst = null;
			}
			else
			{
				pDst = &output[0];
			}
			fixed (byte* pinnableReference = input.GetPinnableReference())
			{
				byte* pSrc = pinnableReference;
				return (int)ZstdNative.ZSTD_decompressDCtx(this.dCtx, (void*)pDst, (UIntPtr)((IntPtr)output.Length), (void*)pSrc, (UIntPtr)((IntPtr)input.Length));
			}
		}

		private unsafe readonly ZstdNative.ZstdDCtx* dCtx;
	}
}
