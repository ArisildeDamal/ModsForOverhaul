using System;
using Vintagestory.Common.Convert;

namespace Vintagestory.Common
{
	public class ZStdCompressorImpl : IZStdCompressor
	{
		public ZStdCompressorImpl(int compressionLevel)
		{
			this.cctx = ZstdNative.ZSTD_createCCtx();
			this.compressionLevel = compressionLevel;
			ZstdNative.ZSTD_CCtx_setParameter(this.cctx, ZstdNative.ZstdCParameter.ZSTD_c_compressionLevel, compressionLevel);
			ZstdNative.ZSTD_CCtx_setParameter(this.cctx, ZstdNative.ZstdCParameter.ZSTD_c_contentSizeFlag, 1);
		}

		public unsafe int Compress(byte[] output, byte[] input)
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
			byte* pSrc;
			if (input == null || input.Length == 0)
			{
				pSrc = null;
			}
			else
			{
				pSrc = &input[0];
			}
			return (int)ZstdNative.ZSTD_compressCCtx(this.cctx, (void*)pDst, (UIntPtr)((IntPtr)output.Length), (void*)pSrc, (UIntPtr)((IntPtr)input.Length), this.compressionLevel);
		}

		public unsafe int Compress(byte[] output, ReadOnlySpan<byte> input)
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
				return (int)ZstdNative.ZSTD_compressCCtx(this.cctx, (void*)pDst, (UIntPtr)((IntPtr)output.Length), (void*)pSrc, (UIntPtr)((IntPtr)input.Length), this.compressionLevel);
			}
		}

		private unsafe readonly ZstdNative.ZstdCCtx* cctx;

		private readonly int compressionLevel;
	}
}
