using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf
{
	internal class ClientChunkData : ChunkData
	{
		private ClientChunkData(ChunkDataPool chunkDataPool)
			: base(chunkDataPool)
		{
			this.GetBlockAsBlock = new Func<int, Block>(this.getBlockAir);
			this.light2Lock = new FastRWLock(chunkDataPool);
		}

		public new static ClientChunkData CreateNew(int chunksize, ChunkDataPool chunkDataPool)
		{
			return new ClientChunkData(chunkDataPool);
		}

		public void BuildFastBlockAccessArray(Block[] blocks)
		{
			int count;
			if (this.blocksLayer != null && (count = this.blocksLayer.paletteCount) > 0)
			{
				int[] bp = this.blocksLayer.palette;
				this.GetBlockAsBlock = this.blocksLayer.SelectDelegateBlockClient(new Func<int, Block>(this.getBlockAir));
				if (BlockChunkDataLayer.blocksByPaletteIndex == null || BlockChunkDataLayer.blocksByPaletteIndex.Length < count)
				{
					BlockChunkDataLayer.blocksByPaletteIndex = new Block[count];
				}
				for (int i = 0; i < count; i++)
				{
					BlockChunkDataLayer.blocksByPaletteIndex[i] = blocks[bp[i]];
				}
				this.blocksArrayCount = count;
			}
			else
			{
				this.GetBlockAsBlock = new Func<int, Block>(this.getBlockAir);
			}
			this.blockAir = blocks[0];
		}

		protected Block getBlockAir(int index3d)
		{
			return this.blockAir;
		}

		public int GetOne(out ushort lightOut, out int lightSatOut, out int fluidBlockId, int index3d)
		{
			this.light2Lock.AcquireReadLock();
			uint i = ((this.light2 != null) ? this.Light2(index3d) : base.Light(index3d));
			this.light2Lock.ReleaseReadLock();
			lightOut = (ushort)i;
			lightSatOut = (int)((i >> 16) & 7U);
			fluidBlockId = base.GetFluid(index3d);
			return base.GetSolidBlock(index3d);
		}

		public void GetRange(Block[] currentChunkBlocksExt, Block[] currentChunkFluidsExt, int[] currentChunkRgbsExt, int extIndex3d, int index3d, int index3dEnd, Block[] blocksFast, ColorUtil.LightUtil lightConverter)
		{
			BlockChunkDataLayer bl = this.blocksLayer;
			if (bl == null)
			{
				this.blockAir = blocksFast[0];
				this.light2Lock.AcquireReadLock();
				try
				{
					do
					{
						uint i = ((this.light2 != null) ? this.Light2(index3d) : base.Light(index3d));
						currentChunkBlocksExt[++extIndex3d] = this.blockAir;
						currentChunkRgbsExt[extIndex3d] = lightConverter.ToRgba((uint)((ushort)i), (int)((i >> 16) & 7U));
						int blockId = base.GetFluid(index3d);
						currentChunkFluidsExt[extIndex3d] = blocksFast[blockId];
					}
					while (++index3d < index3dEnd);
				}
				finally
				{
					this.light2Lock.ReleaseReadLock();
				}
				return;
			}
			bl.readWriteLock.AcquireReadLock();
			this.light2Lock.AcquireReadLock();
			try
			{
				do
				{
					uint j = ((this.light2 != null) ? this.Light2(index3d) : base.Light(index3d));
					int blockId2 = bl.GetUnsafe(index3d);
					currentChunkBlocksExt[++extIndex3d] = blocksFast[blockId2];
					currentChunkRgbsExt[extIndex3d] = lightConverter.ToRgba((uint)((ushort)j), (int)((j >> 16) & 7U));
					blockId2 = base.GetFluid(index3d);
					currentChunkFluidsExt[extIndex3d] = blocksFast[blockId2];
				}
				while (++index3d < index3dEnd);
			}
			finally
			{
				this.light2Lock.ReleaseReadLock();
				bl.readWriteLock.ReleaseReadLock();
			}
		}

		public void GetRange_Faster(Block[] currentChunkBlocksExt, Block[] currentChunkFluidsExt, int[] currentChunkRgbsExt, int extIndex3d, int index3d, int index3dEnd, Block[] blocksFast, ColorUtil.LightUtil lightConverter)
		{
			BlockChunkDataLayer bl = this.blocksLayer;
			if (bl == null)
			{
				this.light2Lock.AcquireReadLock();
				try
				{
					do
					{
						currentChunkBlocksExt[++extIndex3d] = this.blockAir;
						uint i = ((this.light2 != null) ? this.Light2(index3d) : base.Light(index3d));
						currentChunkRgbsExt[extIndex3d] = lightConverter.ToRgba((uint)((ushort)i), (int)((i >> 16) & 7U));
						int fluidId = base.GetFluid(index3d);
						currentChunkFluidsExt[extIndex3d] = blocksFast[fluidId];
					}
					while (++index3d < index3dEnd);
				}
				finally
				{
					this.light2Lock.ReleaseReadLock();
				}
				return;
			}
			if (bl.paletteCount == this.blocksArrayCount)
			{
				bl.readWriteLock.AcquireReadLock();
				this.light2Lock.AcquireReadLock();
				try
				{
					do
					{
						currentChunkBlocksExt[++extIndex3d] = this.GetBlockAsBlock(index3d);
						uint j = ((this.light2 != null) ? this.Light2(index3d) : base.Light(index3d));
						currentChunkRgbsExt[extIndex3d] = lightConverter.ToRgba((uint)((ushort)j), (int)((j >> 16) & 7U));
						int fluidId2 = base.GetFluid(index3d);
						currentChunkFluidsExt[extIndex3d] = blocksFast[fluidId2];
					}
					while (++index3d < index3dEnd);
				}
				finally
				{
					this.light2Lock.ReleaseReadLock();
					bl.readWriteLock.ReleaseReadLock();
				}
				return;
			}
			this.GetRange(currentChunkBlocksExt, currentChunkFluidsExt, currentChunkRgbsExt, extIndex3d, index3d, index3dEnd, blocksFast, lightConverter);
		}

		internal override void EmptyAndReuseArrays(List<int[]> datas)
		{
			this.GetBlockAsBlock = new Func<int, Block>(this.getBlockAir);
			base.EmptyAndReuseArrays(datas);
			int[][] light2Copy = this.light2;
			if (light2Copy != null)
			{
				this.light2Lock.AcquireWriteLock();
				this.light2 = null;
				for (int i = 0; i < light2Copy.Length; i++)
				{
					int[] lighting = light2Copy[i];
					if (lighting != null)
					{
						if (datas != null)
						{
							datas.Add(lighting);
						}
						light2Copy[i] = null;
					}
				}
				this.light2Lock.ReleaseWriteLock();
			}
		}

		public override void SetSunlight_Buffered(int index3d, int sunLevel)
		{
			if (this.lightLayer == null)
			{
				this.lightLayer = new ChunkDataLayer(this.pool);
				this.lightLayer.Set(index3d, sunLevel);
				return;
			}
			if (this.light2 == null)
			{
				this.StartDoubleBuffering();
			}
			this.lightLayer.Set(index3d, (this.lightLayer.Get(index3d) & -32) | sunLevel);
		}

		public override void SetBlocklight_Buffered(int index3d, int lightLevel)
		{
			if (this.lightLayer == null)
			{
				this.lightLayer = new ChunkDataLayer(this.pool);
				this.lightLayer.Set(index3d, lightLevel);
				return;
			}
			if (this.light2 == null)
			{
				this.StartDoubleBuffering();
			}
			this.lightLayer.Set(index3d, (this.lightLayer.Get(index3d) & 31) | lightLevel);
		}

		public uint Light2(int index3d)
		{
			ChunkDataLayer lightLayer = this.lightLayer;
			int[] palette = ((lightLayer != null) ? lightLayer.palette : null);
			if (index3d < 0 || palette == null)
			{
				return 0U;
			}
			int[][] light2Data = this.light2;
			int bitIndex = index3d % 32;
			index3d = index3d / 32 % 1024;
			int idx = 0;
			int bitValue = 1;
			for (int i = 0; i < light2Data.Length; i++)
			{
				idx += ((light2Data[i][index3d] >> bitIndex) & 1) * bitValue;
				bitValue *= 2;
			}
			return (uint)palette[idx];
		}

		private void StartDoubleBuffering()
		{
			this.light2 = this.lightLayer.CopyData();
		}

		public void FinishLightDoubleBuffering()
		{
			int[][] array = this.light2;
			if (array != null)
			{
				this.light2Lock.AcquireWriteLock();
				this.light2 = null;
				for (int i = 0; i < array.Length; i++)
				{
					if (array[i] != null)
					{
						this.pool.Return(array[i]);
						array[i] = null;
					}
				}
				this.light2Lock.ReleaseWriteLock();
			}
		}

		private int[][] light2;

		private FastRWLock light2Lock;

		private int blocksArrayCount;

		private Block blockAir;

		private Func<int, Block> GetBlockAsBlock;
	}
}
