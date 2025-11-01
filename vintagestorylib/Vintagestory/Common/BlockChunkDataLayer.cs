using System;
using Vintagestory.API.Common;

namespace Vintagestory.Common
{
	public class BlockChunkDataLayer : ChunkDataLayer
	{
		public BlockChunkDataLayer(ChunkDataPool chunkDataPool)
			: base(chunkDataPool)
		{
		}

		internal void UpdateToFluidsLayer(BlockChunkDataLayer fluidsLayer)
		{
			GameMain game = this.pool.Game;
			for (int i = 1; i < this.paletteCount; i++)
			{
				Block block = game.Blocks[this.palette[i]];
				if (block.ForFluidsLayer)
				{
					this.MoveToOtherLayer(i, this.palette[i], fluidsLayer);
					base.DeleteFromPalette(i);
					i--;
				}
				else if (block.RemapToLiquidsLayer != null)
				{
					Block waterBlock = game.GetBlock(new AssetLocation(block.RemapToLiquidsLayer));
					if (waterBlock != null)
					{
						this.AddToOtherLayer(i, waterBlock.BlockId, fluidsLayer);
					}
				}
			}
		}

		internal void MoveToOtherLayer(int search, int fluidBlockId, BlockChunkDataLayer fluidsLayer)
		{
			int fluidPaletteIndex = fluidsLayer.GetPaletteIndex(fluidBlockId);
			this.readWriteLock.AcquireWriteLock();
			int bbs = this.bitsize;
			for (int index3d = 0; index3d < 32768; index3d += 32)
			{
				int intIndex = index3d / 32;
				int mask = -1;
				for (int i = 0; i < bbs; i++)
				{
					int v = this.dataBits[i][intIndex];
					mask &= ((((search >> i) & 1) == 1) ? v : (~v));
				}
				if (mask != 0)
				{
					fluidsLayer.Write(fluidPaletteIndex, intIndex, mask);
					int unsetMask = ~mask;
					for (int j = 0; j < bbs; j++)
					{
						this.dataBits[j][intIndex] &= unsetMask;
					}
				}
			}
			this.readWriteLock.ReleaseWriteLock();
		}

		internal void AddToOtherLayer(int search, int fluidBlockId, BlockChunkDataLayer fluidsLayer)
		{
			int fluidPaletteIndex = fluidsLayer.GetPaletteIndex(fluidBlockId);
			this.readWriteLock.AcquireReadLock();
			int bbs = this.bitsize;
			for (int index3d = 0; index3d < 32768; index3d += 32)
			{
				int intIndex = index3d / 32;
				int mask = -1;
				for (int i = 0; i < bbs; i++)
				{
					int v = this.dataBits[i][intIndex];
					mask &= ((((search >> i) & 1) == 1) ? v : (~v));
				}
				if (mask != 0)
				{
					fluidsLayer.Write(fluidPaletteIndex, intIndex, mask);
				}
			}
			this.readWriteLock.ReleaseReadLock();
		}

		private int GetPaletteIndex(int value)
		{
			int paletteIndex;
			if (this.palette != null)
			{
				for (paletteIndex = 0; paletteIndex < this.paletteCount; paletteIndex++)
				{
					if (this.palette[paletteIndex] == value)
					{
						return paletteIndex;
					}
				}
				int[] palette = this.palette;
				lock (palette)
				{
					if (paletteIndex == this.palette.Length)
					{
						paletteIndex = base.MakeSpaceInPalette();
					}
					this.palette[paletteIndex] = value;
					this.paletteCount++;
					return paletteIndex;
				}
			}
			if (value == 0)
			{
				return 0;
			}
			base.NewDataBitsWithFirstValue(value);
			paletteIndex = 1;
			return paletteIndex;
		}

		private Block getBlockOne(int index3d)
		{
			int bitIndex = index3d % 32;
			index3d /= 32;
			return BlockChunkDataLayer.blocksByPaletteIndex[(this.dataBit0[index3d] >> bitIndex) & 1];
		}

		private Block getBlockTwo(int index3d)
		{
			int bitIndex = index3d % 32;
			index3d /= 32;
			return BlockChunkDataLayer.blocksByPaletteIndex[((this.dataBit0[index3d] >> bitIndex) & 1) + 2 * ((this.dataBit1[index3d] >> bitIndex) & 1)];
		}

		private Block getBlockThree(int index3d)
		{
			int bitIndex = index3d % 32;
			index3d /= 32;
			return BlockChunkDataLayer.blocksByPaletteIndex[((this.dataBit0[index3d] >> bitIndex) & 1) + 2 * ((this.dataBit1[index3d] >> bitIndex) & 1) + 4 * ((this.dataBit2[index3d] >> bitIndex) & 1)];
		}

		private Block getBlockFour(int index3d)
		{
			int bitIndex = index3d % 32;
			index3d /= 32;
			return BlockChunkDataLayer.blocksByPaletteIndex[((this.dataBit0[index3d] >> bitIndex) & 1) + 2 * ((this.dataBit1[index3d] >> bitIndex) & 1) + 4 * ((this.dataBit2[index3d] >> bitIndex) & 1) + 8 * ((this.dataBit3[index3d] >> bitIndex) & 1)];
		}

		private Block getBlockFive(int index3d)
		{
			int bitIndex = index3d % 32;
			index3d /= 32;
			return BlockChunkDataLayer.blocksByPaletteIndex[((this.dataBit0[index3d] >> bitIndex) & 1) + 2 * ((this.dataBit1[index3d] >> bitIndex) & 1) + 4 * ((this.dataBit2[index3d] >> bitIndex) & 1) + 8 * ((this.dataBit3[index3d] >> bitIndex) & 1) + 16 * ((this.dataBits[4][index3d] >> bitIndex) & 1)];
		}

		private Block getBlockGeneralCase(int index3d)
		{
			int bitIndex = index3d % 32;
			index3d /= 32;
			int bitValue = 1;
			int idx = 0;
			for (int i = 0; i < this.bitsize; i++)
			{
				idx += ((this.dataBits[i][index3d] >> bitIndex) & 1) * bitValue;
				bitValue *= 2;
			}
			return BlockChunkDataLayer.blocksByPaletteIndex[idx];
		}

		public Func<int, Block> SelectDelegateBlockClient(Func<int, Block> getBlockAir)
		{
			switch (this.bitsize)
			{
			case 0:
				return getBlockAir;
			case 1:
				return new Func<int, Block>(this.getBlockOne);
			case 2:
				return new Func<int, Block>(this.getBlockTwo);
			case 3:
				return new Func<int, Block>(this.getBlockThree);
			case 4:
				return new Func<int, Block>(this.getBlockFour);
			case 5:
				return new Func<int, Block>(this.getBlockFive);
			default:
				return new Func<int, Block>(this.getBlockGeneralCase);
			}
		}

		public static void Dispose()
		{
			BlockChunkDataLayer.blocksByPaletteIndex = null;
		}

		public static Block[] blocksByPaletteIndex;
	}
}
