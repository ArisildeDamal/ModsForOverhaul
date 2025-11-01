using System;
using System.IO;
using System.IO.Compression;

namespace Vintagestory.Common
{
	public class CompressionDeflate : ICompression
	{
		public byte[] Compress(MemoryStream input)
		{
			if (CompressionDeflate.buffer == null)
			{
				CompressionDeflate.buffer = new byte[4096];
			}
			MemoryStream output = new MemoryStream();
			input.Position = 0L;
			using (DeflateStream compress = new DeflateStream(output, CompressionLevel.Fastest))
			{
				int numRead;
				while ((numRead = input.Read(CompressionDeflate.buffer, 0, 4096)) != 0)
				{
					compress.Write(CompressionDeflate.buffer, 0, numRead);
				}
			}
			return output.ToArray();
		}

		public byte[] Compress(byte[] data)
		{
			int len = data.Length;
			MemoryStream output = new MemoryStream();
			using (DeflateStream compress = new DeflateStream(output, CompressionLevel.Fastest))
			{
				int i = 0;
				int penultimate = len - 4096;
				while (i < penultimate)
				{
					compress.Write(data, i, 4096);
					i += 4096;
				}
				compress.Write(data, i, len - i);
			}
			return output.ToArray();
		}

		public byte[] Compress(byte[] data, int len, int reserveOffset)
		{
			MemoryStream output = new MemoryStream((len / 2048 + 1) * 256);
			for (int i = 0; i < reserveOffset; i++)
			{
				output.WriteByte(0);
			}
			using (DeflateStream compress = new DeflateStream(output, CompressionLevel.Fastest))
			{
				int j = 0;
				int penultimate = len - 4096;
				while (j < penultimate)
				{
					compress.Write(data, j, 4096);
					j += 4096;
				}
				compress.Write(data, j, len - j);
			}
			return output.ToArray();
		}

		public unsafe byte[] Compress(ushort[] ushortdata)
		{
			if (CompressionDeflate.buffer == null)
			{
				CompressionDeflate.buffer = new byte[4096];
			}
			MemoryStream output = new MemoryStream();
			byte[] array;
			byte* bBuffer;
			if ((array = CompressionDeflate.buffer) == null || array.Length == 0)
			{
				bBuffer = null;
			}
			else
			{
				bBuffer = &array[0];
			}
			ushort* pBuffer = (ushort*)bBuffer;
			using (DeflateStream compress = new DeflateStream(output, CompressionLevel.Fastest))
			{
				int inpos = 0;
				int outpos = 0;
				while (inpos < ushortdata.Length)
				{
					pBuffer[(IntPtr)(outpos++) * 2] = ushortdata[inpos++];
					if (outpos == 2048 || inpos == ushortdata.Length)
					{
						compress.Write(CompressionDeflate.buffer, 0, outpos * 2);
						outpos = 0;
					}
				}
			}
			array = null;
			return output.ToArray();
		}

		public unsafe byte[] Compress(int[] intdata, int length)
		{
			if (CompressionDeflate.buffer == null)
			{
				CompressionDeflate.buffer = new byte[4096];
			}
			MemoryStream output = new MemoryStream();
			byte[] array;
			byte* bBuffer;
			if ((array = CompressionDeflate.buffer) == null || array.Length == 0)
			{
				bBuffer = null;
			}
			else
			{
				bBuffer = &array[0];
			}
			int* pBuffer = (int*)bBuffer;
			using (DeflateStream compress = new DeflateStream(output, CompressionLevel.Fastest))
			{
				int inpos = 0;
				int outpos = 0;
				while (inpos < length)
				{
					pBuffer[(IntPtr)(outpos++) * 4] = intdata[inpos++];
					if (outpos == 1024 || inpos == length)
					{
						compress.Write(CompressionDeflate.buffer, 0, outpos * 4);
						outpos = 0;
					}
				}
			}
			array = null;
			return output.ToArray();
		}

		public unsafe byte[] Compress(uint[] uintdata, int length)
		{
			if (CompressionDeflate.buffer == null)
			{
				CompressionDeflate.buffer = new byte[4096];
			}
			MemoryStream output = new MemoryStream();
			byte[] array;
			byte* bBuffer;
			if ((array = CompressionDeflate.buffer) == null || array.Length == 0)
			{
				bBuffer = null;
			}
			else
			{
				bBuffer = &array[0];
			}
			uint* pBuffer = (uint*)bBuffer;
			using (DeflateStream compress = new DeflateStream(output, CompressionLevel.Fastest))
			{
				int inpos = 0;
				int outpos = 0;
				while (inpos < length)
				{
					pBuffer[(IntPtr)(outpos++) * 4] = uintdata[inpos++];
					if (outpos == 1024 || inpos == length)
					{
						compress.Write(CompressionDeflate.buffer, 0, outpos * 4);
						outpos = 0;
					}
				}
			}
			array = null;
			return output.ToArray();
		}

		public byte[] Decompress(byte[] fi)
		{
			if (CompressionDeflate.buffer == null)
			{
				CompressionDeflate.buffer = new byte[4096];
			}
			MemoryStream ms = new MemoryStream();
			using (MemoryStream inFile = new MemoryStream(fi))
			{
				using (DeflateStream Decompress = new DeflateStream(inFile, CompressionMode.Decompress))
				{
					int numRead;
					while ((numRead = Decompress.Read(CompressionDeflate.buffer, 0, 4096)) != 0)
					{
						ms.Write(CompressionDeflate.buffer, 0, numRead);
					}
				}
			}
			return ms.ToArray();
		}

		public void Decompress(byte[] fi, byte[] dest)
		{
			using (MemoryStream inFile = new MemoryStream(fi))
			{
				using (DeflateStream Decompress = new DeflateStream(inFile, CompressionMode.Decompress))
				{
					int i = 0;
					int penultimate = dest.Length - 4096;
					int numRead;
					while ((numRead = Decompress.Read(dest, i, 4096)) != 0)
					{
						if ((i += numRead) > penultimate)
						{
							if (i < dest.Length)
							{
								Decompress.Read(dest, i, dest.Length - i);
								break;
							}
							break;
						}
					}
				}
			}
		}

		public byte[] Decompress(byte[] fi, int offset, int length)
		{
			if (CompressionDeflate.buffer == null)
			{
				CompressionDeflate.buffer = new byte[4096];
			}
			MemoryStream ms = new MemoryStream();
			using (MemoryStream inFile = new MemoryStream(fi, offset, length))
			{
				using (DeflateStream Decompress = new DeflateStream(inFile, CompressionMode.Decompress))
				{
					int numRead;
					while ((numRead = Decompress.Read(CompressionDeflate.buffer, 0, 4096)) != 0)
					{
						ms.Write(CompressionDeflate.buffer, 0, numRead);
					}
				}
			}
			return ms.ToArray();
		}

		public int DecompressAndSize(byte[] fi, out byte[] buffer)
		{
			buffer = this.Decompress(fi);
			return buffer.Length;
		}

		public int DecompressAndSize(byte[] compressedData, int offset, int length, out byte[] buffer)
		{
			buffer = this.Decompress(compressedData, offset, length);
			return buffer.Length;
		}

		private const int SIZE = 4096;

		[ThreadStatic]
		private static byte[] buffer;
	}
}
