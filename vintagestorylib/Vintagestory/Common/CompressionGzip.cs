using System;
using System.IO;
using System.IO.Compression;

namespace Vintagestory.Common
{
	public class CompressionGzip : ICompression
	{
		public byte[] Compress(MemoryStream input)
		{
			if (CompressionGzip.buffer == null)
			{
				CompressionGzip.buffer = new byte[4096];
			}
			MemoryStream output = new MemoryStream();
			input.Position = 0L;
			using (GZipStream compress = new GZipStream(output, CompressionMode.Compress))
			{
				int numRead;
				while ((numRead = input.Read(CompressionGzip.buffer, 0, 4096)) != 0)
				{
					compress.Write(CompressionGzip.buffer, 0, numRead);
				}
			}
			return output.ToArray();
		}

		public byte[] Compress(byte[] data)
		{
			int len = data.Length;
			MemoryStream output = new MemoryStream();
			using (GZipStream compress = new GZipStream(output, CompressionMode.Compress))
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
			using (GZipStream compress = new GZipStream(output, CompressionMode.Compress))
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
			if (CompressionGzip.buffer == null)
			{
				CompressionGzip.buffer = new byte[4096];
			}
			MemoryStream output = new MemoryStream();
			byte[] array;
			byte* pBuffer;
			if ((array = CompressionGzip.buffer) == null || array.Length == 0)
			{
				pBuffer = null;
			}
			else
			{
				pBuffer = &array[0];
			}
			ushort* pBuffer2 = (ushort*)pBuffer;
			using (GZipStream compress = new GZipStream(output, CompressionMode.Compress))
			{
				int inpos = 0;
				int outpos = 0;
				while (inpos < ushortdata.Length)
				{
					pBuffer2[(IntPtr)(outpos++) * 2] = ushortdata[inpos++];
					if (outpos == 2048 || inpos == ushortdata.Length)
					{
						compress.Write(CompressionGzip.buffer, 0, outpos * 2);
						outpos = 0;
					}
				}
			}
			array = null;
			return output.ToArray();
		}

		public byte[] Compress(int[] data, int length)
		{
			throw new NotImplementedException();
		}

		public byte[] Compress(uint[] data, int length)
		{
			throw new NotImplementedException();
		}

		public byte[] Decompress(byte[] fi)
		{
			if (CompressionGzip.buffer == null)
			{
				CompressionGzip.buffer = new byte[4096];
			}
			MemoryStream ms = new MemoryStream();
			using (MemoryStream inFile = new MemoryStream(fi))
			{
				using (GZipStream Decompress = new GZipStream(inFile, CompressionMode.Decompress))
				{
					int numRead;
					while ((numRead = Decompress.Read(CompressionGzip.buffer, 0, 4096)) != 0)
					{
						ms.Write(CompressionGzip.buffer, 0, numRead);
					}
				}
			}
			return ms.ToArray();
		}

		public void Decompress(byte[] fi, byte[] dest)
		{
			using (MemoryStream inFile = new MemoryStream(fi))
			{
				using (GZipStream Decompress = new GZipStream(inFile, CompressionMode.Decompress))
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
			throw new NotImplementedException();
		}

		public int DecompressAndSize(byte[] fi, out byte[] buffer)
		{
			throw new NotImplementedException();
		}

		public int DecompressAndSize(byte[] compressedData, int offset, int length, out byte[] buffer)
		{
			throw new NotImplementedException();
		}

		public byte[] DecompressFromOffset(byte[] compressedData, int offset)
		{
			throw new NotImplementedException();
		}

		private const int SIZE = 4096;

		[ThreadStatic]
		private static byte[] buffer;
	}
}
