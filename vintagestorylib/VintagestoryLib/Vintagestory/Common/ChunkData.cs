using System;
using System.Collections;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common
{
	public class ChunkData : IChunkBlocks, IChunkLight, IEnumerable<int>, IEnumerable
	{
		protected ChunkData(ChunkDataPool chunkDataPool)
		{
			this.pool = chunkDataPool;
		}

		public static ChunkData CreateNew(int chunksize, ChunkDataPool chunkDataPool)
		{
			if (32768 != chunksize * chunksize * chunksize)
			{
				throw new Exception("Server and client chunksizes do not match, this isn't going to work!");
			}
			return new ChunkData(chunkDataPool);
		}

		public int GetBlockId(int index, int layer)
		{
			switch (layer)
			{
			default:
			{
				int id = this.GetSolidBlock(index);
				if (id != 0)
				{
					return id;
				}
				return this.GetFluid(index);
			}
			case 1:
				return this.GetSolidBlock(index);
			case 2:
				return this.GetFluid(index);
			case 3:
			{
				int blockId = this.GetFluid(index);
				if (blockId != 0)
				{
					return blockId;
				}
				return this.GetSolidBlock(index);
			}
			case 4:
			{
				int blockId2 = this.GetFluid(index);
				if (blockId2 == 0 || !this.pool.Game.Blocks[blockId2].SideSolid.Any)
				{
					blockId2 = this.GetSolidBlock(index);
				}
				return blockId2;
			}
			}
		}

		public int GetSolidBlock(int index3d)
		{
			BlockChunkDataLayer blockChunkDataLayer = this.blocksLayer;
			if (blockChunkDataLayer == null)
			{
				return 0;
			}
			return blockChunkDataLayer.Get(index3d);
		}

		public int this[int index3d]
		{
			get
			{
				int id = this.GetSolidBlock(index3d);
				if (id != 0)
				{
					return id;
				}
				return this.GetFluid(index3d);
			}
			set
			{
				if (this.blocksLayer == null)
				{
					this.blocksLayer = new BlockChunkDataLayer(this.pool);
				}
				this.blocksLayer.Set(index3d, value);
			}
		}

		public int GetFluid(int index3d)
		{
			BlockChunkDataLayer blockChunkDataLayer = this.fluidsLayer;
			if (blockChunkDataLayer == null)
			{
				return 0;
			}
			return blockChunkDataLayer.Get(index3d);
		}

		public void SetFluid(int index3d, int value)
		{
			if (this.fluidsLayer == null)
			{
				this.fluidsLayer = new BlockChunkDataLayer(this.pool);
			}
			this.fluidsLayer.Set(index3d, value);
		}

		public void SetLight(int index3d, uint value)
		{
			if (this.lightLayer == null)
			{
				this.lightLayer = new ChunkDataLayer(this.pool);
			}
			this.lightLayer.Set(index3d, (int)value);
		}

		public int GetBlockIdUnsafe(int index3d)
		{
			BlockChunkDataLayer blockChunkDataLayer = this.blocksLayer;
			if (blockChunkDataLayer == null)
			{
				return 0;
			}
			return blockChunkDataLayer.GetUnsafe(index3d);
		}

		public int GetBlockIdUnsafe(int index3d, int layer)
		{
			switch (layer)
			{
			default:
			{
				BlockChunkDataLayer blockChunkDataLayer = this.blocksLayer;
				int id = ((blockChunkDataLayer != null) ? blockChunkDataLayer.GetUnsafe_PaletteCheck(index3d) : 0);
				if (id != 0)
				{
					return id;
				}
				BlockChunkDataLayer blockChunkDataLayer2 = this.fluidsLayer;
				if (blockChunkDataLayer2 == null)
				{
					return 0;
				}
				return blockChunkDataLayer2.GetUnsafe_PaletteCheck(index3d);
			}
			case 1:
			{
				BlockChunkDataLayer blockChunkDataLayer3 = this.blocksLayer;
				if (blockChunkDataLayer3 == null)
				{
					return 0;
				}
				return blockChunkDataLayer3.GetUnsafe_PaletteCheck(index3d);
			}
			case 2:
			{
				BlockChunkDataLayer blockChunkDataLayer4 = this.fluidsLayer;
				if (blockChunkDataLayer4 == null)
				{
					return 0;
				}
				return blockChunkDataLayer4.GetUnsafe_PaletteCheck(index3d);
			}
			case 3:
			{
				BlockChunkDataLayer blockChunkDataLayer5 = this.fluidsLayer;
				int blockId = ((blockChunkDataLayer5 != null) ? blockChunkDataLayer5.GetUnsafe_PaletteCheck(index3d) : 0);
				if (blockId != 0)
				{
					return blockId;
				}
				BlockChunkDataLayer blockChunkDataLayer6 = this.blocksLayer;
				if (blockChunkDataLayer6 == null)
				{
					return 0;
				}
				return blockChunkDataLayer6.GetUnsafe_PaletteCheck(index3d);
			}
			case 4:
			{
				BlockChunkDataLayer blockChunkDataLayer7 = this.fluidsLayer;
				int blockId2 = ((blockChunkDataLayer7 != null) ? blockChunkDataLayer7.GetUnsafe_PaletteCheck(index3d) : 0);
				if (blockId2 == 0 || !this.pool.Game.Blocks[blockId2].SideSolid.Any)
				{
					BlockChunkDataLayer blockChunkDataLayer8 = this.blocksLayer;
					blockId2 = ((blockChunkDataLayer8 != null) ? blockChunkDataLayer8.GetUnsafe_PaletteCheck(index3d) : 0);
				}
				return blockId2;
			}
			}
		}

		public void TakeBulkReadLock()
		{
			BlockChunkDataLayer blockChunkDataLayer = this.blocksLayer;
			if (blockChunkDataLayer != null)
			{
				blockChunkDataLayer.readWriteLock.AcquireReadLock();
			}
			BlockChunkDataLayer blockChunkDataLayer2 = this.fluidsLayer;
			if (blockChunkDataLayer2 == null)
			{
				return;
			}
			blockChunkDataLayer2.readWriteLock.AcquireReadLock();
		}

		public void ReleaseBulkReadLock()
		{
			BlockChunkDataLayer blockChunkDataLayer = this.fluidsLayer;
			if (blockChunkDataLayer != null)
			{
				blockChunkDataLayer.readWriteLock.ReleaseReadLock();
			}
			BlockChunkDataLayer blockChunkDataLayer2 = this.blocksLayer;
			if (blockChunkDataLayer2 == null)
			{
				return;
			}
			blockChunkDataLayer2.readWriteLock.ReleaseReadLock();
		}

		public void SetBlockBulk(int index3d, int lenX, int lenZ, int value)
		{
			if (this.blocksLayer == null)
			{
				if (value == 0)
				{
					return;
				}
				this.blocksLayer = new BlockChunkDataLayer(this.pool);
			}
			this.blocksLayer.SetBulk(index3d, lenX, lenZ, value);
		}

		public void SetBlockUnsafe(int index3d, int value)
		{
			if (this.blocksLayer == null)
			{
				if (value == 0)
				{
					return;
				}
				this.blocksLayer = new BlockChunkDataLayer(this.pool);
			}
			this.blocksLayer.SetUnsafe(index3d, value);
		}

		public void SetBlockAir(int index3d)
		{
			BlockChunkDataLayer blockChunkDataLayer = this.blocksLayer;
			if (blockChunkDataLayer == null)
			{
				return;
			}
			blockChunkDataLayer.SetZero(index3d);
		}

		public int Length
		{
			get
			{
				return 32768;
			}
		}

		internal virtual void EmptyAndReuseArrays(List<int[]> datas)
		{
			this.EmptyBlocksData(datas);
			this.EmptyLightData(datas);
			this.EmptyFluidsData(datas);
		}

		private void EmptyBlocksData(List<int[]> datas)
		{
			ChunkDataLayer old = this.blocksLayer;
			if (old != null)
			{
				this.blocksLayer = null;
				old.Clear(datas);
			}
		}

		private void EmptyFluidsData(List<int[]> datas)
		{
			ChunkDataLayer old = this.fluidsLayer;
			if (old != null)
			{
				this.fluidsLayer = null;
				old.Clear(datas);
			}
		}

		private void EmptyLightData(List<int[]> datas)
		{
			ChunkDataLayer old = this.lightLayer;
			if (old != null)
			{
				this.lightLayer = null;
				old.Clear(datas);
			}
		}

		public void ClearBlocks()
		{
			this.pool.FreeArraysAndReset(this);
			this.blocksLayer = null;
		}

		public void ClearBlocksAndPrepare()
		{
			this.ClearBlocks();
			this.blocksLayer = new BlockChunkDataLayer(this.pool);
			this.blocksLayer.PopulateWithAir();
		}

		public void ClearWithSunlight(ushort sunlight)
		{
			this.pool.FreeArraysAndReset(this);
			this.lightLayer = new ChunkDataLayer(this.pool);
			this.lightLayer.FillWithInitialValue((int)sunlight);
		}

		public void FloodWithSunlight(ushort sunlight)
		{
			this.ClearLight();
			this.lightLayer = new ChunkDataLayer(this.pool);
			this.lightLayer.FillWithInitialValue((int)sunlight);
		}

		public void ClearAllSunlight()
		{
			if (this.lightLayer == null)
			{
				return;
			}
			for (int i = 0; i < 32768; i++)
			{
				this.lightLayer.Set(i, this.lightLayer.Get(i) & -32);
			}
		}

		public void ClearLight()
		{
			if (this.lightLayer != null)
			{
				ChunkDataLayer old = this.lightLayer;
				this.lightLayer = null;
				this.pool.FreeArrays(old);
			}
		}

		public IEnumerator<int> GetEnumerator()
		{
			return new BlocksCompositeIdEnumerator(this);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return new BlocksCompositeIdEnumerator(this);
		}

		internal void CompressInto(ref byte[] blocksCompressed, ref byte[] lightCompressed, ref byte[] lightPaletteCompressed, ref byte[] fluidsCompressed, int chunkdataVersion)
		{
			if (ChunkData.arrayStatic == null)
			{
				ChunkData.arrayStatic = new int[15360];
			}
			blocksCompressed = ChunkDataLayer.Compress(this.blocksLayer, ChunkData.arrayStatic);
			fluidsCompressed = ChunkDataLayer.Compress(this.fluidsLayer, ChunkData.arrayStatic);
			ChunkDataLayer lightLayer = this.lightLayer;
			if (lightLayer == null)
			{
				lightCompressed = Array.Empty<byte>();
				lightPaletteCompressed = Array.Empty<byte>();
				return;
			}
			lightCompressed = lightLayer.CompressSeparate(ref lightPaletteCompressed, ChunkData.arrayStatic, chunkdataVersion);
		}

		internal void DecompressFrom(byte[] blocksCompressed, byte[] lightCompressed, byte[] lightPaletteCompressed, byte[] fluidsCompressed, int chunkdataVersion)
		{
			if (chunkdataVersion == 0)
			{
				this.OldStyleUnpack(blocksCompressed, lightCompressed, lightPaletteCompressed);
				return;
			}
			if (blocksCompressed != null)
			{
				this.blocksLayer = new BlockChunkDataLayer(this.pool);
				this.blocksLayer.Decompress(blocksCompressed);
			}
			else
			{
				this.blocksLayer = null;
			}
			if (fluidsCompressed != null)
			{
				this.fluidsLayer = new BlockChunkDataLayer(this.pool);
				this.fluidsLayer.Decompress(fluidsCompressed);
			}
			else
			{
				this.fluidsLayer = null;
			}
			this.UpdateFluids();
			if (lightCompressed == null || lightPaletteCompressed == null || lightCompressed.Length == 0 || lightPaletteCompressed.Length == 0)
			{
				this.lightLayer = null;
				return;
			}
			ChunkDataLayer lightLayerNew = new ChunkDataLayer(this.pool);
			lightLayerNew.DecompressSeparate(lightCompressed, lightPaletteCompressed);
			this.lightLayer = lightLayerNew;
		}

		private void OldStyleUnpack(byte[] blocksCompressed, byte[] lightCompressed, byte[] lightSatCompressed)
		{
			if (ChunkData.oldBlocksTemp == null)
			{
				ChunkData.oldBlocksTemp = new byte[65536];
			}
			if (ChunkData.oldBlocks == null)
			{
				ChunkData.oldBlocks = new ushort[32768];
			}
			if (ChunkData.oldLight == null)
			{
				ChunkData.oldLight = new ushort[32768];
			}
			if (ChunkData.oldLightSat == null)
			{
				ChunkData.oldLightSat = new byte[32768];
			}
			Compression.DecompressToUshort(blocksCompressed, ChunkData.oldBlocks, ChunkData.oldBlocksTemp, 0);
			Compression.DecompressToUshort(lightCompressed, ChunkData.oldLight, ChunkData.oldBlocksTemp, 0);
			Compression.Decompress(lightSatCompressed, ChunkData.oldLightSat, 0);
			this.CreateNewDataFromOld();
		}

		private void CreateNewDataFromOld()
		{
			this.pool.FreeArraysAndReset(this);
			this.blocksLayer = new BlockChunkDataLayer(this.pool);
			this.blocksLayer.PopulateFrom(ChunkData.oldBlocks, ChunkData.oldLightSat);
			for (int i = 0; i < 32768; i++)
			{
				this.SetLight(i, (uint)((int)ChunkData.oldLight[i] | ((int)(ChunkData.oldLightSat[i] & 7) << 16)));
			}
		}

		internal bool IsEmpty()
		{
			return (this.blocksLayer == null && this.fluidsLayer == null) || ((this.blocksLayer == null || !this.blocksLayer.HasContents()) && (this.fluidsLayer == null || !this.fluidsLayer.HasContents()));
		}

		internal bool HasData()
		{
			return true;
		}

		internal void CopyBlocksTo(int[] blocksOut)
		{
			if (this.blocksLayer == null || this.blocksLayer.palette == null)
			{
				for (int i = 0; i < blocksOut.Length; i += 4)
				{
					blocksOut[i] = 0;
					blocksOut[i + 1] = 0;
					blocksOut[i + 2] = 0;
					blocksOut[i + 3] = 0;
				}
				return;
			}
			this.blocksLayer.CopyBlocksTo(blocksOut);
		}

		internal static void UnpackBlocksTo(int[] blocksOut, byte[] blocksCompressed, byte[] lightSatCompressed, int chunkdataVersion)
		{
			if (chunkdataVersion == 0)
			{
				if (ChunkData.oldBlocksTemp == null)
				{
					ChunkData.oldBlocksTemp = new byte[blocksOut.Length * 2];
				}
				if (ChunkData.oldBlocks == null)
				{
					ChunkData.oldBlocks = new ushort[blocksOut.Length];
				}
				if (ChunkData.oldLightSat == null)
				{
					ChunkData.oldLightSat = new byte[blocksOut.Length];
				}
				Compression.DecompressToUshort(blocksCompressed, ChunkData.oldBlocks, ChunkData.oldBlocksTemp, chunkdataVersion);
				Compression.Decompress(lightSatCompressed, ChunkData.oldLightSat, chunkdataVersion);
				for (int i = 0; i < blocksOut.Length; i += 4)
				{
					blocksOut[i] = (int)ChunkData.oldBlocksTemp[i] | (((int)ChunkData.oldLightSat[i] & 65528) << 13);
					blocksOut[i + 1] = (int)ChunkData.oldBlocksTemp[i + 1] | (((int)ChunkData.oldLightSat[i + 1] & 65528) << 13);
					blocksOut[i + 2] = (int)ChunkData.oldBlocksTemp[i + 2] | (((int)ChunkData.oldLightSat[i + 2] & 65528) << 13);
					blocksOut[i + 3] = (int)ChunkData.oldBlocksTemp[i + 3] | (((int)ChunkData.oldLightSat[i + 3] & 65528) << 13);
				}
				return;
			}
			if (ChunkData.blocksTemp == null)
			{
				ChunkData.blocksTemp = new int[15][];
				for (int j = 0; j < 15; j++)
				{
					ChunkData.blocksTemp[j] = new int[1024];
				}
			}
			int bpcUnused = 0;
			int[] blocksPalette = Compression.DecompressCombined(blocksCompressed, ref ChunkData.blocksTemp, ref bpcUnused, null);
			if (blocksPalette == null)
			{
				for (int k = 0; k < blocksOut.Length; k += 4)
				{
					blocksOut[k] = 0;
					blocksOut[k + 1] = 0;
					blocksOut[k + 2] = 0;
					blocksOut[k + 3] = 0;
				}
				return;
			}
			int blocksBitsize = 0;
			int bc = blocksPalette.Length;
			while ((bc >>= 1) > 0)
			{
				blocksBitsize++;
			}
			for (int index3d = 0; index3d < blocksOut.Length; index3d += 32)
			{
				int intIndex = index3d / 32;
				for (int bitIndex = 0; bitIndex < 32; bitIndex++)
				{
					int idx = 0;
					int bitValue = 1;
					for (int l = 0; l < blocksBitsize; l++)
					{
						idx += ((ChunkData.blocksTemp[l][intIndex] >> bitIndex) & 1) * bitValue;
						bitValue *= 2;
					}
					blocksOut[index3d + bitIndex] = blocksPalette[idx];
				}
			}
		}

		internal void UpdateFluids()
		{
			if (this.blocksLayer == null)
			{
				return;
			}
			if (this.fluidsLayer == null)
			{
				this.fluidsLayer = new BlockChunkDataLayer(this.pool);
			}
			this.blocksLayer.UpdateToFluidsLayer(this.fluidsLayer);
		}

		internal ushort ReadLight(int index, out int lightSat)
		{
			ChunkDataLayer lightLayer = this.lightLayer;
			if (lightLayer == null)
			{
				lightSat = 0;
				return 0;
			}
			uint i = (uint)lightLayer.Get(index);
			lightSat = (int)((i >> 16) & 7U);
			return (ushort)i;
		}

		internal ushort ReadLight(int index)
		{
			ChunkDataLayer lightLayer = this.lightLayer;
			if (lightLayer == null)
			{
				return 0;
			}
			return (ushort)lightLayer.Get(index);
		}

		internal uint Light(int index)
		{
			ChunkDataLayer lightLayer = this.lightLayer;
			if (lightLayer == null)
			{
				return 0U;
			}
			return (uint)lightLayer.Get(index);
		}

		public virtual int GetSunlight(int index3d)
		{
			ChunkDataLayer lightLayer = this.lightLayer;
			if (lightLayer == null)
			{
				return 0;
			}
			return lightLayer.Get(index3d) & 31;
		}

		public virtual void SetSunlight(int index3d, int sunLevel)
		{
			ChunkDataLayer lightLayer = this.lightLayer;
			if (lightLayer == null)
			{
				this.lightLayer = new ChunkDataLayer(this.pool);
				this.lightLayer.Set(index3d, sunLevel);
				return;
			}
			lightLayer.Set(index3d, (lightLayer.Get(index3d) & -32) | sunLevel);
		}

		public virtual void SetSunlight_Buffered(int index3d, int sunLevel)
		{
			ChunkDataLayer lightLayer = this.lightLayer;
			if (lightLayer == null)
			{
				this.lightLayer = new ChunkDataLayer(this.pool);
				this.lightLayer.Set(index3d, sunLevel);
				return;
			}
			lightLayer.Set(index3d, (lightLayer.Get(index3d) & -32) | sunLevel);
		}

		public virtual int GetBlocklight(int index3d)
		{
			ChunkDataLayer lightLayer = this.lightLayer;
			if (lightLayer == null)
			{
				return 0;
			}
			return (lightLayer.Get(index3d) >> 5) & 31;
		}

		public virtual void SetBlocklight(int index3d, int lightLevel)
		{
			ChunkDataLayer lightLayer = this.lightLayer;
			if (lightLayer == null)
			{
				this.lightLayer = new ChunkDataLayer(this.pool);
				this.lightLayer.Set(index3d, lightLevel);
				return;
			}
			lightLayer.Set(index3d, (lightLayer.Get(index3d) & 31) | lightLevel);
		}

		public virtual void SetBlocklight_Buffered(int index3d, int lightLevel)
		{
			ChunkDataLayer lightLayer = this.lightLayer;
			if (lightLayer == null)
			{
				this.lightLayer = new ChunkDataLayer(this.pool);
				this.lightLayer.Set(index3d, lightLevel);
				return;
			}
			lightLayer.Set(index3d, (lightLayer.Get(index3d) & 31) | lightLevel);
		}

		internal BlockPos FindFirst(List<int> searchIds)
		{
			if (this.blocksLayer == null)
			{
				return null;
			}
			return this.blocksLayer.FindFirst(searchIds);
		}

		public bool ContainsBlock(int id)
		{
			return this.blocksLayer != null && this.blocksLayer.Contains(id);
		}

		public void FuzzyListBlockIds(List<int> reusableList)
		{
			BlockChunkDataLayer blockChunkDataLayer = this.blocksLayer;
			if (blockChunkDataLayer == null)
			{
				return;
			}
			blockChunkDataLayer.ListAllPaletteValues(reusableList);
		}

		protected const uint LIGHTSATMASK = 7U;

		protected const uint NON_LIGHTSAT_MASK = 4294508543U;

		protected const uint SUNLIGHT_MASK = 4294901791U;

		protected const int NON_SUNLIGHT_MASK = -32;

		protected const int SUNLIGHT_MASK_INT = 31;

		protected const int NON_SUNLIGHT_MASK_INT = 65504;

		public const int NON_LIGHTSAT_MASK_INT = 65528;

		private const int chunksize = 32;

		protected const int length = 32768;

		protected const int INTSIZE = 32;

		public BlockChunkDataLayer blocksLayer;

		public BlockChunkDataLayer fluidsLayer;

		public ChunkDataLayer lightLayer;

		protected ChunkDataPool pool;

		[ThreadStatic]
		private static int[][] blocksTemp;

		[ThreadStatic]
		private static int[] arrayStatic;

		[ThreadStatic]
		private static ushort[] oldBlocks;

		[ThreadStatic]
		private static ushort[] oldLight;

		[ThreadStatic]
		private static byte[] oldLightSat;

		[ThreadStatic]
		private static byte[] oldBlocksTemp;
	}
}
