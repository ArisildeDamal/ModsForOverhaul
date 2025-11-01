using System;
using Vintagestory.Common.Convert;

namespace Vintagestory.Common
{
	public class CompressionZSTD : ICompression
	{
		public byte[] Buffer
		{
			get
			{
				byte[] array;
				if ((array = CompressionZSTD.reusableBuffer) == null)
				{
					array = (CompressionZSTD.reusableBuffer = new byte[4096]);
				}
				return array;
			}
		}

		private IZStdCompressor ConstructCompressor()
		{
			return ZStdWrapper.ConstructCompressor(-3);
		}

		private byte[] GetOrCreateBuffer(int size)
		{
			byte[] buffer = CompressionZSTD.reusableBuffer;
			if (buffer == null || buffer.Length < size)
			{
				buffer = new byte[size];
				CompressionZSTD.reusableBuffer = buffer;
				CompressionZSTD.largebufferUnusedCounter1 = 0;
				CompressionZSTD.largebufferMaxUsed1 = 0;
			}
			else if (buffer.Length > 524288)
			{
				if (size > CompressionZSTD.largebufferMaxUsed1)
				{
					CompressionZSTD.largebufferMaxUsed1 = size;
				}
				if (size > buffer.Length * 3 / 4)
				{
					CompressionZSTD.largebufferUnusedCounter1 = 0;
				}
				else if (CompressionZSTD.largebufferUnusedCounter1++ >= 100)
				{
					CompressionZSTD.largebufferUnusedCounter1 = 0;
					if (Environment.TickCount64 > CompressionZSTD.largebufferLastReductionTime1 + 960000L)
					{
						CompressionZSTD.largebufferLastReductionTime1 = Environment.TickCount64;
						buffer = new byte[Math.Max(CompressionZSTD.largebufferMaxUsed1 + 32768, 524288)];
						CompressionZSTD.reusableBuffer = buffer;
						CompressionZSTD.largebufferMaxUsed1 = 0;
					}
				}
			}
			return buffer;
		}

		private byte[] GetOrCreateBuffer2(int size)
		{
			byte[] buffer = CompressionZSTD.reusableBuffer2;
			if (buffer == null || buffer.Length < size)
			{
				buffer = new byte[size];
				CompressionZSTD.reusableBuffer2 = buffer;
				CompressionZSTD.largebufferUnusedCounter2 = 0;
				CompressionZSTD.largebufferMaxUsed2 = 0;
			}
			else if (buffer.Length > 524288)
			{
				if (size > CompressionZSTD.largebufferMaxUsed2)
				{
					CompressionZSTD.largebufferMaxUsed2 = size;
				}
				if (size > buffer.Length * 3 / 4)
				{
					CompressionZSTD.largebufferUnusedCounter2 = 0;
				}
				else if (CompressionZSTD.largebufferUnusedCounter2++ >= 100)
				{
					CompressionZSTD.largebufferUnusedCounter2 = 0;
					if (Environment.TickCount64 > CompressionZSTD.largebufferLastReductionTime2 + 960000L)
					{
						CompressionZSTD.largebufferLastReductionTime2 = Environment.TickCount64;
						buffer = new byte[Math.Max(CompressionZSTD.largebufferMaxUsed2 + 32768, 524288)];
						CompressionZSTD.reusableBuffer2 = buffer;
						CompressionZSTD.largebufferMaxUsed2 = 0;
					}
				}
			}
			return buffer;
		}

		public byte[] Compress(byte[] data)
		{
			int compressMaxBufferSize = (int)ZstdNative.ZSTD_compressBound((UIntPtr)((IntPtr)data.Length));
			byte[] buffer = this.GetOrCreateBuffer(compressMaxBufferSize);
			IZStdCompressor izstdCompressor;
			if ((izstdCompressor = CompressionZSTD.reusableCompressor) == null)
			{
				izstdCompressor = (CompressionZSTD.reusableCompressor = this.ConstructCompressor());
			}
			int compressedSize = izstdCompressor.Compress(buffer, data);
			return ArrayConvert.ByteToByte(buffer, compressedSize);
		}

		public byte[] Compress(byte[] data, int length, int reserveOffset)
		{
			int compressMaxBufferSize = (int)ZstdNative.ZSTD_compressBound((UIntPtr)((IntPtr)length));
			byte[] buffer = this.GetOrCreateBuffer(compressMaxBufferSize);
			ReadOnlySpan<byte> input = new ReadOnlySpan<byte>(data, 0, length);
			IZStdCompressor izstdCompressor;
			if ((izstdCompressor = CompressionZSTD.reusableCompressor) == null)
			{
				izstdCompressor = (CompressionZSTD.reusableCompressor = this.ConstructCompressor());
			}
			int compressedSize = izstdCompressor.Compress(buffer, input);
			return ArrayConvert.ByteToByte(buffer, compressedSize, reserveOffset);
		}

		public unsafe byte[] Compress(ushort[] ushortdata)
		{
			int len = ushortdata.Length * 2;
			int compressMaxBufferSize = (int)ZstdNative.ZSTD_compressBound((UIntPtr)((IntPtr)len));
			byte[] buffer = this.GetOrCreateBuffer(compressMaxBufferSize);
			IZStdCompressor izstdCompressor;
			if ((izstdCompressor = CompressionZSTD.reusableCompressor) == null)
			{
				izstdCompressor = (CompressionZSTD.reusableCompressor = this.ConstructCompressor());
			}
			int compressedSize;
			fixed (ushort[] array = ushortdata)
			{
				ushort* pData;
				if (ushortdata == null || array.Length == 0)
				{
					pData = null;
				}
				else
				{
					pData = &array[0];
				}
				ReadOnlySpan<byte> bytedata = new ReadOnlySpan<byte>((void*)pData, len);
				compressedSize = izstdCompressor.Compress(buffer, bytedata);
			}
			return ArrayConvert.ByteToByte(buffer, compressedSize);
		}

		public unsafe byte[] Compress(int[] intdata, int length)
		{
			int len = length * 4;
			int compressMaxBufferSize = (int)ZstdNative.ZSTD_compressBound((UIntPtr)((IntPtr)len));
			byte[] buffer = this.GetOrCreateBuffer(compressMaxBufferSize);
			IZStdCompressor izstdCompressor;
			if ((izstdCompressor = CompressionZSTD.reusableCompressor) == null)
			{
				izstdCompressor = (CompressionZSTD.reusableCompressor = this.ConstructCompressor());
			}
			int compressedSize;
			fixed (int[] array = intdata)
			{
				int* pData;
				if (intdata == null || array.Length == 0)
				{
					pData = null;
				}
				else
				{
					pData = &array[0];
				}
				ReadOnlySpan<byte> bytedata = new ReadOnlySpan<byte>((void*)pData, len);
				compressedSize = izstdCompressor.Compress(buffer, bytedata);
			}
			return ArrayConvert.ByteToByte(buffer, compressedSize);
		}

		public unsafe byte[] Compress(uint[] uintdata, int length)
		{
			int len = length * 4;
			int compressMaxBufferSize = (int)ZstdNative.ZSTD_compressBound((UIntPtr)((IntPtr)len));
			byte[] buffer = this.GetOrCreateBuffer(compressMaxBufferSize);
			IZStdCompressor izstdCompressor;
			if ((izstdCompressor = CompressionZSTD.reusableCompressor) == null)
			{
				izstdCompressor = (CompressionZSTD.reusableCompressor = this.ConstructCompressor());
			}
			int compressedSize;
			fixed (uint[] array = uintdata)
			{
				uint* pData;
				if (uintdata == null || array.Length == 0)
				{
					pData = null;
				}
				else
				{
					pData = &array[0];
				}
				ReadOnlySpan<byte> bytedata = new ReadOnlySpan<byte>((void*)pData, len);
				compressedSize = izstdCompressor.Compress(buffer, bytedata);
			}
			return ArrayConvert.ByteToByte(buffer, compressedSize);
		}

		internal unsafe int CompressAndSize(int[] intdata, int length)
		{
			int len = length * 4;
			int compressMaxBufferSize = (int)ZstdNative.ZSTD_compressBound((UIntPtr)((IntPtr)len));
			byte[] buffer = this.GetOrCreateBuffer(compressMaxBufferSize);
			IZStdCompressor izstdCompressor;
			if ((izstdCompressor = CompressionZSTD.reusableCompressor) == null)
			{
				izstdCompressor = (CompressionZSTD.reusableCompressor = this.ConstructCompressor());
			}
			int num;
			fixed (int[] array = intdata)
			{
				int* pData;
				if (intdata == null || array.Length == 0)
				{
					pData = null;
				}
				else
				{
					pData = &array[0];
				}
				ReadOnlySpan<byte> bytedata = new ReadOnlySpan<byte>((void*)pData, len);
				num = izstdCompressor.Compress(buffer, bytedata);
			}
			return num;
		}

		internal unsafe int Compress_To2ndBuffer(int[] intdata, int intLength)
		{
			int len = intLength * 4;
			int compressMaxBufferSize = (int)ZstdNative.ZSTD_compressBound((UIntPtr)((IntPtr)len));
			byte[] bufferInts = this.GetOrCreateBuffer2(compressMaxBufferSize);
			IZStdCompressor izstdCompressor;
			if ((izstdCompressor = CompressionZSTD.reusableCompressor) == null)
			{
				izstdCompressor = (CompressionZSTD.reusableCompressor = this.ConstructCompressor());
			}
			int num;
			fixed (int[] array = intdata)
			{
				int* pData;
				if (intdata == null || array.Length == 0)
				{
					pData = null;
				}
				else
				{
					pData = &array[0];
				}
				ReadOnlySpan<byte> bytedata = new ReadOnlySpan<byte>((void*)pData, len);
				num = izstdCompressor.Compress(bufferInts, bytedata);
			}
			return num;
		}

		public void Decompress(byte[] fi, byte[] dest)
		{
			IZStdDecompressor izstdDecompressor;
			if ((izstdDecompressor = CompressionZSTD.reusableDecompressor) == null)
			{
				izstdDecompressor = (CompressionZSTD.reusableDecompressor = ZStdWrapper.CreateDecompressor());
			}
			izstdDecompressor.Decompress(dest, fi);
		}

		public byte[] Decompress(byte[] fi)
		{
			byte[] buffer;
			int decompressedSize = this.DecompressAndSize(fi, out buffer);
			if (decompressedSize < 0)
			{
				return Array.Empty<byte>();
			}
			return ArrayConvert.ByteToByte(buffer, decompressedSize);
		}

		public byte[] Decompress(byte[] fi, int offset, int length)
		{
			byte[] buffer;
			int decompressedSize = this.DecompressAndSize(fi, offset, length, out buffer);
			if (decompressedSize < 0)
			{
				return Array.Empty<byte>();
			}
			return ArrayConvert.ByteToByte(buffer, decompressedSize);
		}

		public int DecompressAndSize(byte[] compressedData, out byte[] buffer)
		{
			ReadOnlySpan<byte> compressedFrame = new ReadOnlySpan<byte>(compressedData);
			int decompressBufferSize = (int)ZStdWrapper.GetDecompressedSize(compressedFrame);
			buffer = this.GetOrCreateBuffer(decompressBufferSize);
			IZStdDecompressor izstdDecompressor;
			if ((izstdDecompressor = CompressionZSTD.reusableDecompressor) == null)
			{
				izstdDecompressor = (CompressionZSTD.reusableDecompressor = ZStdWrapper.CreateDecompressor());
			}
			return izstdDecompressor.Decompress(buffer, compressedFrame);
		}

		public int DecompressAndSize(byte[] compressedData, int offset, int length, out byte[] buffer)
		{
			ReadOnlySpan<byte> compressedFrame = new ReadOnlySpan<byte>(compressedData, offset, length);
			int decompressBufferSize = (int)ZStdWrapper.GetDecompressedSize(compressedFrame);
			buffer = this.GetOrCreateBuffer(decompressBufferSize);
			IZStdDecompressor izstdDecompressor;
			if ((izstdDecompressor = CompressionZSTD.reusableDecompressor) == null)
			{
				izstdDecompressor = (CompressionZSTD.reusableDecompressor = ZStdWrapper.CreateDecompressor());
			}
			return izstdDecompressor.Decompress(buffer, compressedFrame);
		}

		public const int ZSTDCompressionLevel = -3;

		private const int MaxUnusedLargeBufferCount = 100;

		private const int LargeBufferResetInterval = 960000;

		private const int LargeBufferSize = 524288;

		private const int LargeBufferHeadroom = 32768;

		[ThreadStatic]
		private static IZStdCompressor reusableCompressor;

		[ThreadStatic]
		private static IZStdDecompressor reusableDecompressor;

		[ThreadStatic]
		private static byte[] reusableBuffer;

		[ThreadStatic]
		internal static byte[] reusableBuffer2;

		[ThreadStatic]
		private static int largebufferUnusedCounter1;

		[ThreadStatic]
		private static int largebufferUnusedCounter2;

		[ThreadStatic]
		private static int largebufferMaxUsed1;

		[ThreadStatic]
		private static int largebufferMaxUsed2;

		[ThreadStatic]
		private static long largebufferLastReductionTime1;

		[ThreadStatic]
		private static long largebufferLastReductionTime2;
	}
}
