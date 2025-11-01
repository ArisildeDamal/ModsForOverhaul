using System;
using System.IO;

namespace Vintagestory.Common
{
	public class Compression
	{
		public static void Reset()
		{
			Compression.compressor = Compression.Compression1;
		}

		public static byte[] Compress(byte[] data)
		{
			return Compression.compressor.Compress(data);
		}

		public static byte[] CompressOffset4(byte[] data, int len)
		{
			return Compression.compressor.Compress(data, len, 4);
		}

		public static byte[] Compress(byte[] data, int version)
		{
			return ((version != 0) ? Compression.Compression1 : Compression.Compression0).Compress(data);
		}

		public static byte[] Compress(ushort[] data, int version)
		{
			return ((version != 0) ? Compression.Compression1 : Compression.Compression0).Compress(data);
		}

		public static byte[] Compress(int[] data, int length, int version)
		{
			return ((version != 0) ? Compression.Compression1 : Compression.Compression0).Compress(data, length);
		}

		public static byte[] Compress(uint[] data, int length, int version)
		{
			return ((version != 0) ? Compression.Compression1 : Compression.Compression0).Compress(data, length);
		}

		public static byte[] CompressAndCombine(int[] data, int[] intdata, int intLength)
		{
			int blocksBitsize = 0;
			int bc = intdata.Length;
			while ((bc >>= 1) > 0)
			{
				blocksBitsize++;
			}
			int dataLength = blocksBitsize * 1024;
			CompressionZSTD zstdCompression = Compression.Compression1 as CompressionZSTD;
			if (zstdCompression != null)
			{
				int compressedSize = zstdCompression.CompressAndSize(data, dataLength);
				if (intLength > 18)
				{
					return ArrayConvert.Build(zstdCompression.Compress_To2ndBuffer(intdata, intLength), CompressionZSTD.reusableBuffer2, zstdCompression.Buffer, compressedSize);
				}
				return ArrayConvert.Build(intLength, intdata, zstdCompression.Buffer, compressedSize);
			}
			else
			{
				byte[] dataCompressed = Compression.Compression1.Compress(data, dataLength);
				if (intLength > 18)
				{
					byte[] intdataCompressed = Compression.Compression1.Compress(intdata, intLength);
					return ArrayConvert.Build(intdataCompressed.Length, intdataCompressed, dataCompressed, dataCompressed.Length);
				}
				return ArrayConvert.Build(intLength, intdata, dataCompressed, dataCompressed.Length);
			}
		}

		public static byte[] Decompress(byte[] data)
		{
			return Compression.compressor.Decompress(data);
		}

		public static byte[] Decompress(byte[] data, int offset, int length)
		{
			return Compression.compressor.Decompress(data, offset, length);
		}

		public static void Decompress(byte[] data, byte[] dest, int version)
		{
			if (version != 0)
			{
				Compression.Compression1.Decompress(data, dest);
				return;
			}
			Compression.Compression0.Decompress(data, dest);
		}

		public static void DecompressToUshort(byte[] data, ushort[] container, byte[] reusableBytes, int version)
		{
			if (version != 0)
			{
				Compression.Compression1.Decompress(data, reusableBytes);
			}
			else
			{
				Compression.Compression0.Decompress(data, reusableBytes);
			}
			ArrayConvert.ByteToUshort(reusableBytes, container);
		}

		internal static int[] DecompressCombined(byte[] blocksCompressed, ref int[][] blocks, ref int refCount, Func<int[]> newArray)
		{
			int compressedSizeInts = ArrayConvert.GetInt(blocksCompressed);
			if (compressedSizeInts == 0)
			{
				return null;
			}
			int intCount;
			int[] paletteArray;
			if (compressedSizeInts < 0)
			{
				compressedSizeInts *= -1;
				intCount = compressedSizeInts / 4;
				if (intCount == 1)
				{
					return null;
				}
				paletteArray = new int[ArrayConvert.GetRoundedUpSize(intCount)];
				ArrayConvert.ByteToInt(blocksCompressed, 4, paletteArray, intCount);
			}
			else
			{
				byte[] buffer;
				intCount = Compression.Compression1.DecompressAndSize(blocksCompressed, 4, compressedSizeInts, out buffer) / 4;
				if (intCount <= 1)
				{
					return null;
				}
				paletteArray = new int[ArrayConvert.GetRoundedUpSize(intCount)];
				ArrayConvert.ByteToInt(buffer, paletteArray, intCount);
			}
			int blocksBitsize = 0;
			int len = paletteArray.Length;
			while ((len >>= 1) > 0)
			{
				blocksBitsize++;
			}
			if (blocks == null)
			{
				blocks = new int[15][];
			}
			byte[] array = Compression.Compression1.Decompress(blocksCompressed, compressedSizeInts + 4, blocksCompressed.Length - (compressedSizeInts + 4));
			if (array.Length < blocksBitsize * 1024 * 4)
			{
				throw new InvalidDataException();
			}
			ArrayConvert.ByteToIntArrays(array, blocks, blocksBitsize, newArray);
			refCount = intCount;
			return paletteArray;
		}

		internal static int[] DecompressToInts(byte[] dataCompressed, ref int intCount)
		{
			byte[] buffer;
			int decompressedSize = Compression.Compression1.DecompressAndSize(dataCompressed, out buffer);
			if (decompressedSize < 0)
			{
				return null;
			}
			intCount = decompressedSize / 4;
			int[] result = new int[ArrayConvert.GetRoundedUpSize(intCount)];
			ArrayConvert.ByteToInt(buffer, result, intCount);
			return result;
		}

		internal static int Decompress(byte[] dataCompressed, ref int[][] output, Func<int[]> createNewSlice)
		{
			byte[] buffer;
			int size = Compression.Compression1.DecompressAndSize(dataCompressed, out buffer);
			if (size < 0)
			{
				return 0;
			}
			int slicesCount = size / 4096;
			if (size != 4096 * slicesCount)
			{
				throw new InvalidDataException("size was " + size.ToString() + ", should be " + (4096 * slicesCount).ToString());
			}
			if (output == null)
			{
				output = new int[15][];
			}
			ArrayConvert.ByteToIntArrays(buffer, output, slicesCount, createNewSlice);
			return slicesCount;
		}

		private static ICompression Compression0 = new CompressionDeflate();

		private static ICompression Compression1 = new CompressionZSTD();

		public static ICompression compressor = Compression.Compression1;
	}
}
