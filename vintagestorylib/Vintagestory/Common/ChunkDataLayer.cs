using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common
{
	public class ChunkDataLayer
	{
		public ChunkDataLayer(ChunkDataPool chunkDataPool)
		{
			this.Get = new Func<int, int>(this.GetFromBits0);
			this.readWriteLock = new FastRWLock(chunkDataPool);
			this.pool = chunkDataPool;
		}

		protected int GetGeneralCase(int index3d)
		{
			int intIndex = (index3d & 32767) / 32;
			int bitValue = 1;
			int idx = 0;
			this.readWriteLock.AcquireReadLock();
			for (int i = 0; i < this.bitsize; i++)
			{
				idx += ((this.dataBits[i][intIndex] >> index3d) & 1) * bitValue;
				bitValue *= 2;
			}
			this.readWriteLock.ReleaseReadLock();
			return this.palette[idx];
		}

		protected int GetFromBits0(int index3d)
		{
			return 0;
		}

		private int GetFromBits1(int index3d)
		{
			int intIndex = (index3d & 32767) / 32;
			return this.palette[(this.dataBit0[intIndex] >> index3d) & 1];
		}

		private int GetFromBits2(int index3d)
		{
			int intIndex = (index3d & 32767) / 32;
			this.readWriteLock.AcquireReadLock();
			int num = this.palette[((this.dataBit0[intIndex] >> index3d) & 1) + 2 * ((this.dataBit1[intIndex] >> index3d) & 1)];
			this.readWriteLock.ReleaseReadLock();
			return num;
		}

		private int GetFromBits3(int index3d)
		{
			int intIndex = (index3d & 32767) / 32;
			this.readWriteLock.AcquireReadLock();
			int num = this.palette[((this.dataBit0[intIndex] >> index3d) & 1) + 2 * ((this.dataBit1[intIndex] >> index3d) & 1) + 4 * ((this.dataBit2[intIndex] >> index3d) & 1)];
			this.readWriteLock.ReleaseReadLock();
			return num;
		}

		private int GetFromBits4(int index3d)
		{
			int intIndex = (index3d & 32767) / 32;
			this.readWriteLock.AcquireReadLock();
			int num = this.palette[((this.dataBit0[intIndex] >> index3d) & 1) + 2 * ((this.dataBit1[intIndex] >> index3d) & 1) + 4 * ((this.dataBit2[intIndex] >> index3d) & 1) + 8 * ((this.dataBit3[intIndex] >> index3d) & 1)];
			this.readWriteLock.ReleaseReadLock();
			return num;
		}

		private int GetFromBits5(int index3d)
		{
			int intIndex = (index3d & 32767) / 32;
			this.readWriteLock.AcquireReadLock();
			int num = this.palette[((this.dataBit0[intIndex] >> index3d) & 1) + 2 * ((this.dataBit1[intIndex] >> index3d) & 1) + 4 * ((this.dataBit2[intIndex] >> index3d) & 1) + 8 * ((this.dataBit3[intIndex] >> index3d) & 1) + 16 * ((this.dataBits[4][intIndex] >> index3d) & 1)];
			this.readWriteLock.ReleaseReadLock();
			return num;
		}

		private Func<int, int> selectDelegate(int newBlockBitsize)
		{
			if (this.palette == null)
			{
				return new Func<int, int>(this.GetFromBits0);
			}
			switch (newBlockBitsize)
			{
			case 0:
				return new Func<int, int>(this.GetFromBits0);
			case 1:
				this.dataBit0 = this.dataBits[0];
				return new Func<int, int>(this.GetFromBits1);
			case 2:
				this.dataBit0 = this.dataBits[0];
				this.dataBit1 = this.dataBits[1];
				return new Func<int, int>(this.GetFromBits2);
			case 3:
				this.dataBit0 = this.dataBits[0];
				this.dataBit1 = this.dataBits[1];
				this.dataBit2 = this.dataBits[2];
				return new Func<int, int>(this.GetFromBits3);
			case 4:
				this.dataBit0 = this.dataBits[0];
				this.dataBit1 = this.dataBits[1];
				this.dataBit2 = this.dataBits[2];
				this.dataBit3 = this.dataBits[3];
				return new Func<int, int>(this.GetFromBits4);
			case 5:
				this.dataBit0 = this.dataBits[0];
				this.dataBit1 = this.dataBits[1];
				this.dataBit2 = this.dataBits[2];
				this.dataBit3 = this.dataBits[3];
				return new Func<int, int>(this.GetFromBits5);
			default:
				this.dataBit0 = this.dataBits[0];
				return new Func<int, int>(this.GetGeneralCase);
			}
		}

		internal int[][] CopyData()
		{
			this.readWriteLock.AcquireReadLock();
			int[][] dataCopy = new int[this.bitsize][];
			for (int i = 0; i < dataCopy.Length; i++)
			{
				int[] newarray = (dataCopy[i] = this.pool.NewData_NoClear());
				int[] oldArray = this.dataBits[i];
				for (int j = 0; j < newarray.Length; j += 4)
				{
					newarray[j] = oldArray[j];
					newarray[j + 1] = oldArray[j + 1];
					newarray[j + 2] = oldArray[j + 2];
					newarray[j + 3] = oldArray[j + 3];
				}
			}
			this.readWriteLock.ReleaseReadLock();
			return dataCopy;
		}

		public int GetUnsafe_PaletteCheck(int index3d)
		{
			if (this.palette == null)
			{
				return 0;
			}
			return this.GetUnsafe(index3d);
		}

		public int GetUnsafe(int index3d)
		{
			int num;
			try
			{
				int intIndex = index3d / 32;
				switch (this.bitsize)
				{
				case 0:
					num = 0;
					break;
				case 1:
					num = this.palette[(this.dataBit0[intIndex] >> index3d) & 1];
					break;
				case 2:
					num = this.palette[((this.dataBit0[intIndex] >> index3d) & 1) + 2 * ((this.dataBit1[intIndex] >> index3d) & 1)];
					break;
				case 3:
					num = this.palette[((this.dataBit0[intIndex] >> index3d) & 1) + 2 * ((this.dataBit1[intIndex] >> index3d) & 1) + 4 * ((this.dataBit2[intIndex] >> index3d) & 1)];
					break;
				case 4:
					num = this.palette[((this.dataBit0[intIndex] >> index3d) & 1) + 2 * ((this.dataBit1[intIndex] >> index3d) & 1) + 4 * ((this.dataBit2[intIndex] >> index3d) & 1) + 8 * ((this.dataBit3[intIndex] >> index3d) & 1)];
					break;
				case 5:
					num = this.palette[((this.dataBit0[intIndex] >> index3d) & 1) + 2 * ((this.dataBit1[intIndex] >> index3d) & 1) + 4 * ((this.dataBit2[intIndex] >> index3d) & 1) + 8 * ((this.dataBit3[intIndex] >> index3d) & 1) + 16 * ((this.dataBits[4][intIndex] >> index3d) & 1)];
					break;
				default:
				{
					int bitValue = 1;
					int idx = (this.dataBit0[intIndex] >> index3d) & 1;
					for (int i = 1; i < this.bitsize; i++)
					{
						bitValue *= 2;
						idx += ((this.dataBits[i][intIndex] >> index3d) & 1) * bitValue;
					}
					num = this.palette[idx];
					break;
				}
				}
			}
			catch (NullReferenceException)
			{
				if (this.palette == null)
				{
					throw new Exception("ChunkDataLayer: palette was null, bitsize is " + this.bitsize.ToString());
				}
				if (this.bitsize > 0 && this.dataBit0 == null)
				{
					throw new Exception("ChunkDataLayer: dataBit0 was null, bitsize is " + this.bitsize.ToString() + ", dataBits[0] null:" + (this.dataBits[0] == null).ToString());
				}
				if (this.bitsize > 1 && this.dataBit1 == null)
				{
					throw new Exception("ChunkDataLayer: dataBit1 was null, bitsize is " + this.bitsize.ToString() + ", dataBits[1] null:" + (this.dataBits[1] == null).ToString());
				}
				if (this.bitsize > 2 && this.dataBit2 == null)
				{
					throw new Exception("ChunkDataLayer: dataBit2 was null, bitsize is " + this.bitsize.ToString() + ", dataBits[2] null:" + (this.dataBits[2] == null).ToString());
				}
				if (this.bitsize > 3 && this.dataBit3 == null)
				{
					throw new Exception("ChunkDataLayer: dataBit3 was null, bitsize is " + this.bitsize.ToString() + ", dataBits[3] null:" + (this.dataBits[3] == null).ToString());
				}
				if (this.bitsize > 4 && this.dataBits[4] == null)
				{
					throw new Exception("ChunkDataLayer: dataBits[4] was null, bitsize is " + this.bitsize.ToString());
				}
				throw new Exception("ChunkDataLayer: other null exception, bitsize is " + this.bitsize.ToString());
			}
			return num;
		}

		public void Set(int index3d, int value)
		{
			if (index3d == (index3d & 32767))
			{
				int paletteIndex;
				if (this.palette != null)
				{
					if (value != 0)
					{
						if (value != this.setBlockValueCached)
						{
							int count = this.paletteCount;
							for (paletteIndex = 1; paletteIndex < count; paletteIndex++)
							{
								if (this.palette[paletteIndex] == value)
								{
									IL_00A2:
									this.setBlockIndexCached = paletteIndex;
									this.setBlockValueCached = value;
									goto IL_00CF;
								}
							}
							int[] array = this.palette;
							lock (array)
							{
								if (paletteIndex == this.palette.Length)
								{
									paletteIndex = this.MakeSpaceInPalette();
								}
								this.palette[paletteIndex] = value;
								this.paletteCount++;
							}
							goto IL_00A2;
						}
						paletteIndex = this.setBlockIndexCached;
					}
					else
					{
						if (this.palette.Length == 1)
						{
							return;
						}
						paletteIndex = 0;
					}
				}
				else
				{
					if (value == 0)
					{
						return;
					}
					this.NewDataBitsWithFirstValue(value);
					paletteIndex = 1;
				}
				IL_00CF:
				int bitMask = 1 << index3d;
				int unsetMask = ~bitMask;
				index3d /= 32;
				this.readWriteLock.AcquireWriteLock();
				if ((paletteIndex & 1) != 0)
				{
					this.dataBit0[index3d] |= bitMask;
				}
				else
				{
					this.dataBit0[index3d] &= unsetMask;
				}
				for (int i = 1; i < this.bitsize; i++)
				{
					if ((paletteIndex & (1 << i)) != 0)
					{
						this.dataBits[i][index3d] |= bitMask;
					}
					else
					{
						this.dataBits[i][index3d] &= unsetMask;
					}
				}
				this.readWriteLock.ReleaseWriteLock();
				return;
			}
			throw new IndexOutOfRangeException("Chunk blocks index3d must be between 0 and " + 32767.ToString() + ", was " + index3d.ToString());
		}

		public void SetUnsafe(int index3d, int value)
		{
			int bitMask = 1 << index3d;
			index3d /= 32;
			int unsetMask = ~bitMask;
			if (value != 0)
			{
				int paletteIndex;
				if (value == this.setBlockValueCached)
				{
					paletteIndex = this.setBlockIndexCached;
				}
				else
				{
					if (this.palette != null)
					{
						int count = this.paletteCount;
						for (paletteIndex = 1; paletteIndex < count; paletteIndex++)
						{
							if (this.palette[paletteIndex] == value)
							{
								IL_008E:
								this.setBlockIndexCached = paletteIndex;
								this.setBlockValueCached = value;
								goto IL_00D9;
							}
						}
						if (paletteIndex == this.palette.Length)
						{
							paletteIndex = this.MakeSpaceInPalette();
						}
						this.palette[paletteIndex] = value;
						this.paletteCount++;
						goto IL_008E;
					}
					this.NewDataBitsWithFirstValue(value);
					paletteIndex = 1;
				}
				IL_00D9:
				if ((paletteIndex & 1) != 0)
				{
					this.dataBit0[index3d] |= bitMask;
				}
				else
				{
					this.dataBit0[index3d] &= unsetMask;
				}
				for (int i = 1; i < this.bitsize; i++)
				{
					if ((paletteIndex & (1 << i)) != 0)
					{
						this.dataBits[i][index3d] |= bitMask;
					}
					else
					{
						this.dataBits[i][index3d] &= unsetMask;
					}
				}
				return;
			}
			this.dataBit0[index3d] &= unsetMask;
			for (int j = 1; j < this.bitsize; j++)
			{
				this.dataBits[j][index3d] &= unsetMask;
			}
		}

		public void SetZero(int index3d)
		{
			if (this.palette != null)
			{
				int num = 1 << index3d;
				index3d /= 32;
				int unsetMask = ~num;
				this.dataBit0[index3d] &= unsetMask;
				for (int i = 1; i < this.bitsize; i++)
				{
					this.dataBits[i][index3d] &= unsetMask;
				}
			}
		}

		public void SetBulk(int index3d, int lenX, int lenZ, int value)
		{
			int paletteIndex;
			if (value != 0)
			{
				if (value == this.setBlockValueCached)
				{
					paletteIndex = this.setBlockIndexCached;
				}
				else if (this.paletteCount == 0)
				{
					this.NewDataBitsWithFirstValue(value);
					paletteIndex = 1;
				}
				else
				{
					int count = this.paletteCount;
					for (paletteIndex = 1; paletteIndex < count; paletteIndex++)
					{
						if (this.palette[paletteIndex] == value)
						{
							goto IL_0080;
						}
					}
					if (paletteIndex == this.palette.Length)
					{
						paletteIndex = this.MakeSpaceInPalette();
					}
					this.palette[paletteIndex] = value;
					this.paletteCount++;
				}
			}
			else
			{
				paletteIndex = 0;
			}
			IL_0080:
			int intIndex = index3d / 32;
			for (int z = 0; z < lenZ; z++)
			{
				this.dataBit0[intIndex] = -(paletteIndex & 1);
				for (int i = 1; i < this.bitsize; i++)
				{
					this.dataBits[i][intIndex] = -((paletteIndex >> i) & 1);
				}
				intIndex++;
			}
		}

		protected void NewDataBitsWithFirstValue(int value)
		{
			if (this.dataBits == null)
			{
				this.dataBits = new int[15][];
			}
			this.dataBit0 = (this.dataBits[0] = this.pool.NewData());
			this.setBlockIndexCached = 1;
			this.setBlockValueCached = value;
			this.palette = new int[2];
			this.paletteCount = 2;
			this.palette[1] = value;
			this.Get = new Func<int, int>(this.GetFromBits1);
			this.bitsize = 1;
		}

		protected int MakeSpaceInPalette()
		{
			if (this.bitsize > 6 && this.CleanUpPalette())
			{
				return this.paletteCount;
			}
			int[] bp = this.palette;
			int currentLength = bp.Length;
			int[] newArray = new int[currentLength * 2];
			for (int i = 0; i < bp.Length; i++)
			{
				newArray[i] = bp[i];
			}
			this.palette = newArray;
			this.dataBits[this.bitsize] = this.pool.NewData();
			this.Get = this.selectDelegate(this.bitsize + 1);
			this.bitsize++;
			return currentLength;
		}

		private bool CleanUpPalette()
		{
			if (this.pool.server == null)
			{
				if (this.bitsize < 14)
				{
					return false;
				}
				throw new Exception("Oops, a client chunk had so many changes that it exceeded the maximum size.  That's not your fault!  Re-joining the game should fix it.  If you see this message repeated, please report it as a bug");
			}
			else
			{
				if (ChunkDataLayer.paletteBitmap == null)
				{
					ChunkDataLayer.paletteBitmap = new int[1024];
					ChunkDataLayer.paletteValuesBuilder = new int[32];
				}
				for (int i = 0; i < ChunkDataLayer.paletteBitmap.Length; i++)
				{
					ChunkDataLayer.paletteBitmap[i] = 0;
				}
				this.readWriteLock.AcquireReadLock();
				for (int j = 0; j < 1024; j++)
				{
					for (int k = 0; k < ChunkDataLayer.paletteValuesBuilder.Length; k++)
					{
						ChunkDataLayer.paletteValuesBuilder[k] = 0;
					}
					for (int l = 0; l < this.bitsize; l++)
					{
						int bits = this.dataBits[l][j];
						for (int m = 0; m < ChunkDataLayer.paletteValuesBuilder.Length; m++)
						{
							ChunkDataLayer.paletteValuesBuilder[m] |= ((bits >> m) & 1) << l;
						}
					}
					for (int n = 0; n < ChunkDataLayer.paletteValuesBuilder.Length; n++)
					{
						int paletteValue = ChunkDataLayer.paletteValuesBuilder[n];
						ChunkDataLayer.paletteBitmap[paletteValue / 32] |= 1 << paletteValue % 32;
					}
				}
				this.readWriteLock.ReleaseReadLock();
				int allUsed = -1;
				int maxCount = this.paletteCount / 32;
				for (int i2 = 0; i2 < maxCount; i2++)
				{
					allUsed &= ChunkDataLayer.paletteBitmap[i2];
				}
				if (allUsed == -1)
				{
					return false;
				}
				this.CleanUnusedValuesFromEndOfPalette();
				int paletteFlags = 0;
				int i3 = 0;
				while (i3 < this.paletteCount)
				{
					if (i3 % 32 != 0)
					{
						goto IL_01A6;
					}
					paletteFlags = ChunkDataLayer.paletteBitmap[i3 / 32];
					if (paletteFlags != -1)
					{
						goto IL_01A6;
					}
					i3 += 31;
					IL_01D9:
					i3++;
					continue;
					IL_01A6:
					if ((paletteFlags & (1 << i3)) == 0)
					{
						this.DeleteFromPalette(i3);
						ChunkDataLayer.paletteBitmap[i3 / 32] |= 1 << i3;
						this.CleanUnusedValuesFromEndOfPalette();
						goto IL_01D9;
					}
					goto IL_01D9;
				}
				int newBitsize = this.CalcBitsize(this.paletteCount + 1);
				int oldBitsize = this.bitsize;
				if (newBitsize < oldBitsize)
				{
					int[] newPalette = new int[1 << newBitsize];
					for (int i4 = 0; i4 < newPalette.Length; i4++)
					{
						newPalette[i4] = this.palette[i4];
					}
					this.bitsize = newBitsize;
					this.palette = newPalette;
					this.Get = this.selectDelegate(newBitsize);
					for (int i5 = newBitsize; i5 < oldBitsize; i5++)
					{
						this.pool.Return(this.dataBits[i5]);
						this.dataBits[i5] = null;
					}
				}
				return true;
			}
		}

		private int CalcBitsize(int paletteCount)
		{
			if (paletteCount == 0)
			{
				return 0;
			}
			int bc = paletteCount - 1;
			int lbs = 1;
			while ((bc >>= 1) > 0)
			{
				lbs++;
			}
			return lbs;
		}

		private void CleanUnusedValuesFromEndOfPalette()
		{
			int v2 = this.paletteCount - 1;
			for (int i = v2 / 32; i >= 0; i--)
			{
				int paletteFlags = ChunkDataLayer.paletteBitmap[i];
				if (paletteFlags == 0)
				{
					this.paletteCount -= 32;
				}
				else
				{
					int j = 31;
					if (i == v2 / 32 && v2 % 32 < j)
					{
						j = v2 % 32;
					}
					while (j >= 0)
					{
						if ((paletteFlags & (1 << j)) != 0)
						{
							return;
						}
						this.paletteCount--;
						j--;
					}
				}
			}
		}

		internal void FillWithInitialValue(int value)
		{
			this.NewDataBitsWithFirstValue(value);
			int[] array = this.dataBit0;
			for (int i = 0; i < array.Length; i += 4)
			{
				array[i] = -1;
				array[i + 1] = -1;
				array[i + 2] = -1;
				array[i + 3] = -1;
			}
		}

		public void Clear(List<int[]> datas)
		{
			this.Get = new Func<int, int>(this.GetFromBits0);
			int bbs = this.bitsize;
			this.bitsize = 0;
			if (this.dataBits != null && datas != null)
			{
				this.readWriteLock.WaitUntilFree();
				for (int i = 0; i < bbs; i++)
				{
					if (this.dataBits[i] != null)
					{
						datas.Add(this.dataBits[i]);
					}
				}
			}
			this.setBlockIndexCached = 0;
			this.setBlockValueCached = 0;
		}

		public void PopulateWithAir()
		{
			if (this.dataBits == null)
			{
				this.dataBits = new int[15][];
			}
			this.dataBit0 = (this.dataBits[0] = this.pool.NewData());
			this.setBlockIndexCached = 0;
			this.setBlockValueCached = 0;
			this.palette = new int[2];
			this.paletteCount = 1;
			this.Get = new Func<int, int>(this.GetFromBits1);
			this.bitsize = 1;
		}

		public static byte[] Compress(ChunkDataLayer layer, int[] arrayStatic)
		{
			if (layer == null || layer.palette == null)
			{
				return ChunkDataLayer.emptyCompressed;
			}
			return layer.CompressUsing(arrayStatic);
		}

		private byte[] CompressUsing(int[] arrayStatic)
		{
			int ptr = 0;
			this.readWriteLock.AcquireReadLock();
			for (int i = 0; i < this.bitsize; i++)
			{
				ArrayConvert.IntToInt(this.dataBits[i], arrayStatic, ptr);
				ptr += 1024;
			}
			this.readWriteLock.ReleaseReadLock();
			return Compression.CompressAndCombine(arrayStatic, this.palette, this.paletteCount);
		}

		internal byte[] CompressSeparate(ref byte[] paletteCompressed, int[] arrayStatic, int chunkdataVersion)
		{
			int ptr = 0;
			object obj = this.palette ?? this.readWriteLock;
			lock (obj)
			{
				this.readWriteLock.AcquireReadLock();
				for (int i = 0; i < this.bitsize; i++)
				{
					ArrayConvert.IntToInt(this.dataBits[i], arrayStatic, ptr);
					ptr += 1024;
				}
				this.readWriteLock.ReleaseReadLock();
				if (this.bitsize != this.CalcBitsize(this.paletteCount))
				{
					if (this.bitsize == 0)
					{
						paletteCompressed = ChunkDataLayer.emptyCompressed;
						return ChunkDataLayer.emptyCompressed;
					}
					throw new Exception("Likely code error! Compressing light mismatch: paletteCount " + this.paletteCount.ToString() + ", databits " + this.bitsize.ToString());
				}
				else
				{
					paletteCompressed = Compression.Compress(this.palette, this.paletteCount, chunkdataVersion);
				}
			}
			return Compression.Compress(arrayStatic, ptr, chunkdataVersion);
		}

		internal void Decompress(byte[] layerCompressed)
		{
			this.paletteCount = 0;
			this.palette = Compression.DecompressCombined(layerCompressed, ref this.dataBits, ref this.paletteCount, new Func<int[]>(this.pool.NewData));
			if (this.palette != null)
			{
				int bbs = 0;
				int bc = this.palette.Length;
				while ((bc >>= 1) > 0)
				{
					bbs++;
				}
				this.Get = this.selectDelegate(bbs);
				this.bitsize = bbs;
				return;
			}
			this.bitsize = 0;
		}

		internal void DecompressSeparate(byte[] dataCompressed, byte[] paletteCompressed)
		{
			this.palette = Compression.DecompressToInts(paletteCompressed, ref this.paletteCount);
			if (this.palette == null)
			{
				this.Get = new Func<int, int>(this.GetFromBits0);
				this.bitsize = 0;
				return;
			}
			if (this.palette.Length == 0)
			{
				this.paletteCount = 0;
				this.palette = null;
				this.Get = new Func<int, int>(this.GetFromBits0);
				this.bitsize = 0;
				return;
			}
			int lbs = 0;
			int bc = this.palette.Length;
			while ((bc >>= 1) > 0)
			{
				lbs++;
			}
			int lbsCheck = Compression.Decompress(dataCompressed, ref this.dataBits, new Func<int[]>(this.pool.NewData));
			if (lbs != lbsCheck)
			{
				if (lbs < lbsCheck)
				{
					this.pool.Logger.Debug(string.Concat(new string[]
					{
						"Info: decompressed ",
						lbsCheck.ToString(),
						" databits while palette length was only ",
						this.palette.Length.ToString(),
						", pc ",
						this.paletteCount.ToString()
					}));
				}
				else
				{
					this.pool.Logger.Error(string.Concat(new string[]
					{
						"Corrupted light data?  Decompressed ",
						lbsCheck.ToString(),
						" databits while palette length was ",
						this.palette.Length.ToString(),
						", pc ",
						this.paletteCount.ToString()
					}));
				}
				while (lbs > lbsCheck)
				{
					this.dataBits[lbsCheck++] = this.pool.NewData();
				}
			}
			this.Get = this.selectDelegate(lbs);
			this.bitsize = lbs;
		}

		internal void PopulateFrom(ushort[] oldValues, byte[] oldLightSat)
		{
			if (this.dataBits == null)
			{
				this.dataBits = new int[15][];
			}
			this.dataBit0 = (this.dataBits[0] = this.pool.NewData());
			this.palette = new int[2];
			this.paletteCount = 1;
			this.bitsize = 1;
			this.Get = new Func<int, int>(this.GetFromBits1);
			this.readWriteLock.AcquireWriteLock();
			for (int i = 0; i < 32768; i++)
			{
				byte lightSatTmp = oldLightSat[i];
				int value = (int)oldValues[i] | (((int)lightSatTmp & 65528) << 13);
				if (value != 0)
				{
					this.SetUnsafe(i, value);
				}
			}
			bool freeArrays = false;
			if (this.paletteCount == 1)
			{
				this.paletteCount = 0;
				this.palette = null;
				freeArrays = true;
				this.dataBits[0] = null;
				this.dataBit0 = null;
				this.bitsize = 0;
			}
			this.readWriteLock.ReleaseWriteLock();
			if (freeArrays)
			{
				this.pool.FreeArrays(this);
			}
		}

		internal bool HasContents()
		{
			int bbs = this.bitsize;
			for (int i = 0; i < bbs; i++)
			{
				int[] array = this.dataBits[i];
				for (int j = 0; j < array.Length; j += 4)
				{
					if (array[j] != 0)
					{
						return true;
					}
					if (array[j + 1] != 0)
					{
						return true;
					}
					if (array[j + 2] != 0)
					{
						return true;
					}
					if (array[j + 3] != 0)
					{
						return true;
					}
				}
			}
			return false;
		}

		internal void CopyBlocksTo(int[] blocksOut)
		{
			this.readWriteLock.AcquireReadLock();
			int bbs = this.bitsize;
			for (int index3d = 0; index3d < blocksOut.Length; index3d += 32)
			{
				int intIndex = index3d / 32;
				for (int bitIndex = 0; bitIndex < 32; bitIndex++)
				{
					int idx = 0;
					int bitValue = 1;
					for (int i = 0; i < bbs; i++)
					{
						idx += ((this.dataBits[i][intIndex] >> bitIndex) & 1) * bitValue;
						bitValue *= 2;
					}
					blocksOut[index3d + bitIndex] = this.palette[idx];
				}
			}
			this.readWriteLock.ReleaseReadLock();
		}

		internal BlockPos FindFirst(List<int> searchIds)
		{
			for (int i = 1; i < this.paletteCount; i++)
			{
				if (searchIds.Contains(this.palette[i]))
				{
					for (int intIndex = 0; intIndex < 1024; intIndex++)
					{
						int searchResult = this.RapidValueSearch(intIndex, i);
						if (searchResult != 0)
						{
							for (int j = 0; j < 32; j++)
							{
								if ((searchResult & (1 << j)) != 0)
								{
									return new BlockPos(j, intIndex / 32, intIndex % 32);
								}
							}
						}
					}
				}
			}
			return null;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int RapidValueSearch(int intIndex, int needle)
		{
			int searchResult = -1;
			for (int i = 0; i < this.bitsize; i++)
			{
				searchResult &= (((needle & (1 << i)) != 0) ? this.dataBits[i][intIndex] : (~this.dataBits[i][intIndex]));
			}
			return searchResult;
		}

		internal bool Contains(int id)
		{
			for (int i = 0; i < this.paletteCount; i++)
			{
				if (this.palette[i] == id)
				{
					for (int intIndex = 0; intIndex < 1024; intIndex++)
					{
						if (this.RapidValueSearch(intIndex, i) != 0)
						{
							return true;
						}
					}
					return false;
				}
			}
			return false;
		}

		internal void ListAllPaletteValues(List<int> list)
		{
			for (int i = 0; i < this.paletteCount; i++)
			{
				list.Add(this.palette[i]);
			}
		}

		protected void Write(int paletteIndex, int intIndex, int mask)
		{
			int unsetMask = ~mask;
			this.readWriteLock.AcquireWriteLock();
			for (int i = 0; i < this.bitsize; i++)
			{
				if ((paletteIndex & (1 << i)) != 0)
				{
					this.dataBits[i][intIndex] |= mask;
				}
				else
				{
					this.dataBits[i][intIndex] &= unsetMask;
				}
			}
			this.readWriteLock.ReleaseWriteLock();
		}

		protected void DeleteFromPalette(int deletePosition)
		{
			int search = this.paletteCount - 1;
			this.readWriteLock.AcquireWriteLock();
			int bbs = this.bitsize;
			for (int index3d = 0; index3d < 32768; index3d += 32)
			{
				int intIndex = index3d / 32;
				int mask = -1;
				for (int i = 0; i < bbs; i++)
				{
					int v = this.dataBits[i][intIndex];
					int searchBit = (search >> i) & 1;
					mask &= searchBit * v + (1 - searchBit) * ~v;
				}
				if (mask != 0)
				{
					int unsetMask = ~mask;
					for (int j = 0; j < bbs; j++)
					{
						if ((deletePosition & (1 << j)) != 0)
						{
							this.dataBits[j][intIndex] |= mask;
						}
						else
						{
							this.dataBits[j][intIndex] &= unsetMask;
						}
					}
				}
			}
			this.palette[deletePosition] = this.palette[search];
			this.paletteCount--;
			this.readWriteLock.ReleaseWriteLock();
		}

		public void ClearPaletteOutsideMaxValue(int maxValue)
		{
			int[] bp = this.palette;
			if (bp != null)
			{
				int count = this.paletteCount;
				for (int i = 0; i < count; i++)
				{
					if (bp[i] >= maxValue)
					{
						bp[i] = 0;
					}
				}
			}
		}

		private const int chunksize = 32;

		protected const int length = 32768;

		public const int INTSIZE = 32;

		public const int SLICESIZE = 1024;

		public const int DATASLICES = 15;

		protected int[][] dataBits;

		public int[] palette;

		public volatile int paletteCount;

		protected int bitsize;

		public FastRWLock readWriteLock;

		public Func<int, int> Get;

		protected int[] dataBit0;

		protected int[] dataBit1;

		protected int[] dataBit2;

		protected int[] dataBit3;

		private int setBlockIndexCached;

		private int setBlockValueCached;

		protected ChunkDataPool pool;

		protected static byte[] emptyCompressed = new byte[4];

		[ThreadStatic]
		private static int[] paletteBitmap;

		[ThreadStatic]
		private static int[] paletteValuesBuilder;
	}
}
