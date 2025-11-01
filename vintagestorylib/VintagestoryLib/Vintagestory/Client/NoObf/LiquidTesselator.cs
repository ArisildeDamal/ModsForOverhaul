using System;
using System.Runtime.CompilerServices;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.Client.NoObf
{
	public class LiquidTesselator : IBlockTesselator
	{
		public LiquidTesselator(ChunkTesselator tct)
		{
			this.chunksize = 32;
			this.extChunkSize = 34;
			this.extChunkDataFluids = tct.currentChunkFluidBlocksExt;
			this.extChunkDataBlocks = tct.currentChunkBlocksExt;
			this.moveUp = this.extChunkSize * this.extChunkSize;
			this.moveSouth = this.extChunkSize;
			this.moveNorthWest = -this.extChunkSize - 1;
			this.moveNorthEast = -this.extChunkSize + 1;
			this.moveSouthWest = this.extChunkSize - 1;
			this.moveSouthEast = this.extChunkSize + 1;
			this.moveAboveNorth = (this.extChunkSize - 1) * this.extChunkSize;
			this.moveAboveSouth = (this.extChunkSize + 1) * this.extChunkSize;
			this.moveAboveEast = this.extChunkSize * this.extChunkSize + 1;
			this.moveAboveWest = this.extChunkSize * this.extChunkSize - 1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool SideSolid(int extIndex3d, BlockFacing facing)
		{
			bool sideSolid = this.extChunkDataFluids[extIndex3d].SideSolid[facing.Index];
			if (!sideSolid)
			{
				sideSolid = this.extChunkDataBlocks[extIndex3d].SideSolid[facing.Index];
			}
			return sideSolid;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool IsSameLiquid(int extIndex3d)
		{
			return this.isLiquidBlock[this.extChunkDataFluids[extIndex3d].BlockId];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int SameLiquidLevelAt(int extIndex3d)
		{
			return this.extChunkDataFluids[extIndex3d].LiquidLevel;
		}

		public void Tesselate(TCTCache vars)
		{
			if (this.isLiquidBlock == null)
			{
				this.isLiquidBlock = vars.tct.isLiquidBlock;
			}
			int extIndex3d = vars.extIndex3d;
			int liquidLevel = vars.block.LiquidLevel;
			float waterLevel = ChunkTesselator.waterLevels[liquidLevel];
			IBlockFlowing blockFlowing = vars.block as IBlockFlowing;
			this.lavaFlag = ((blockFlowing != null && blockFlowing.IsLava) ? 134217728 : 0);
			this.extraFlags = 0;
			Block aboveLiquid = this.extChunkDataFluids[extIndex3d + this.moveUp];
			Block block = this.extChunkDataFluids[extIndex3d - this.moveUp];
			Block belowBlock = this.extChunkDataBlocks[extIndex3d - this.moveUp];
			Block inBlock = this.extChunkDataBlocks[extIndex3d];
			this.upFlowVectors.Fill(0f);
			float[] waterStillFlowVector = this.waterStillFlowVector;
			if (!belowBlock.SideSolid.OnSide(BlockFacing.UP) || belowBlock.Replaceable >= 6000)
			{
				this.flowVectorsN = (this.SideSolid(extIndex3d - this.moveSouth, BlockFacing.SOUTH) ? waterStillFlowVector : this.waterDownFlowVector);
				this.flowVectorsE = (this.SideSolid(extIndex3d + 1, BlockFacing.WEST) ? waterStillFlowVector : this.waterDownFlowVector);
				this.flowVectorsS = (this.SideSolid(extIndex3d + this.moveSouth, BlockFacing.NORTH) ? waterStillFlowVector : this.waterDownFlowVector);
				this.flowVectorsW = (this.SideSolid(extIndex3d - 1, BlockFacing.EAST) ? waterStillFlowVector : this.waterDownFlowVector);
			}
			else
			{
				this.flowVectorsN = (this.flowVectorsE = (this.flowVectorsS = (this.flowVectorsW = waterStillFlowVector)));
			}
			float northWestLevel = 1f;
			float southWestLevel = 1f;
			float northEastLevel = 1f;
			float southEastLevel = 1f;
			int waveNorthEast = 2;
			int waveNorthWest = 2;
			int waveSouthEast = 2;
			int waveSouthWest = 2;
			if (aboveLiquid.MatterState != EnumMatterState.Liquid)
			{
				float[] upFlowVectors = this.upFlowVectors;
				if (liquidLevel == 7)
				{
					bool aboveWestWater = this.IsSameLiquid(extIndex3d + this.moveAboveWest);
					bool aboveEastWater = this.IsSameLiquid(extIndex3d + this.moveAboveEast);
					bool flag = this.IsSameLiquid(extIndex3d + this.moveAboveSouth);
					bool aboveNorthWater = this.IsSameLiquid(extIndex3d + this.moveAboveNorth);
					bool aboveNorthEastWater = aboveNorthWater || aboveEastWater || this.IsSameLiquid(extIndex3d + this.moveAboveNorth + 1);
					bool aboveSouthEastWater = flag || aboveEastWater || this.IsSameLiquid(extIndex3d + this.moveAboveSouth + 1);
					bool aboveSouthWestWater = flag || aboveWestWater || this.IsSameLiquid(extIndex3d + this.moveAboveSouth - 1);
					object obj = aboveNorthWater || aboveWestWater || this.IsSameLiquid(extIndex3d + this.moveAboveNorth - 1);
					waveNorthEast = (aboveNorthEastWater ? 2 : 3);
					object obj2 = obj;
					waveNorthWest = ((obj2 != null) ? 2 : 3);
					waveSouthEast = (aboveSouthEastWater ? 2 : 3);
					waveSouthWest = (aboveSouthWestWater ? 2 : 3);
					northWestLevel = ((obj2 != null) ? 1f : waterLevel);
					southWestLevel = (aboveSouthWestWater ? 1f : waterLevel);
					northEastLevel = (aboveNorthEastWater ? 1f : waterLevel);
					southEastLevel = (aboveSouthEastWater ? 1f : waterLevel);
					if ((obj2 & aboveSouthWestWater & aboveNorthEastWater & aboveSouthEastWater) != null && !vars.tct.isPartiallyTransparent[aboveLiquid.BlockId] && aboveLiquid.SideOpaque[5] && vars.drawFaceFlags == 16)
					{
						return;
					}
					Vec3i normali = ((blockFlowing != null) ? blockFlowing.FlowNormali : null) ?? null;
					if (normali != null)
					{
						float flowVectorX = (float)normali.X / 2f;
						float flowVectorZ = (float)normali.Z / 2f;
						upFlowVectors[0] = flowVectorX;
						upFlowVectors[1] = flowVectorZ;
						upFlowVectors[2] = flowVectorX;
						upFlowVectors[3] = flowVectorZ;
						upFlowVectors[4] = flowVectorX;
						upFlowVectors[5] = flowVectorZ;
						upFlowVectors[6] = flowVectorX;
						upFlowVectors[7] = flowVectorZ;
					}
				}
				else
				{
					int westWater = this.SameLiquidLevelAt(extIndex3d - 1);
					int eastWater = this.SameLiquidLevelAt(extIndex3d + 1);
					int southWater = this.SameLiquidLevelAt(extIndex3d + this.moveSouth);
					int northWater = this.SameLiquidLevelAt(extIndex3d - this.moveSouth);
					int nwWater = this.SameLiquidLevelAt(extIndex3d + this.moveNorthWest);
					int neWater = this.SameLiquidLevelAt(extIndex3d + this.moveNorthEast);
					int swWater = this.SameLiquidLevelAt(extIndex3d + this.moveSouthWest);
					int esWater = this.SameLiquidLevelAt(extIndex3d + this.moveSouthEast);
					int aboveWestWater2 = (this.IsSameLiquid(extIndex3d + this.moveAboveWest) ? 8 : 0);
					int aboveEastWater2 = (this.IsSameLiquid(extIndex3d + this.moveAboveEast) ? 8 : 0);
					int aboveSouthWater = (this.IsSameLiquid(extIndex3d + this.moveAboveSouth) ? 8 : 0);
					int aboveNorthWater2 = (this.IsSameLiquid(extIndex3d + this.moveAboveNorth) ? 8 : 0);
					int aboveNorthWestWater = (this.IsSameLiquid(extIndex3d + this.moveAboveNorth - 1) ? 8 : 0);
					int aboveNorthEastWater2 = (this.IsSameLiquid(extIndex3d + this.moveAboveNorth + 1) ? 8 : 0);
					int aboveSouthWestWater2 = (this.IsSameLiquid(extIndex3d + this.moveAboveSouth - 1) ? 8 : 0);
					int aboveSouthEastWater2 = (this.IsSameLiquid(extIndex3d + this.moveAboveSouth + 1) ? 8 : 0);
					northWestLevel = ChunkTesselator.waterLevels[GameMath.Max(new int[] { liquidLevel, northWater, westWater, nwWater, aboveWestWater2, aboveNorthWater2, aboveNorthWestWater })];
					southWestLevel = ChunkTesselator.waterLevels[GameMath.Max(new int[] { liquidLevel, southWater, westWater, swWater, aboveSouthWater, aboveWestWater2, aboveSouthWestWater2 })];
					northEastLevel = ChunkTesselator.waterLevels[GameMath.Max(new int[] { liquidLevel, northWater, eastWater, neWater, aboveEastWater2, aboveNorthWater2, aboveNorthEastWater2 })];
					southEastLevel = ChunkTesselator.waterLevels[GameMath.Max(new int[] { liquidLevel, southWater, eastWater, esWater, aboveEastWater2, aboveSouthWater, aboveSouthEastWater2 })];
					waveNorthEast = ((northEastLevel < 1f && aboveNorthEastWater2 == 0 && aboveNorthWater2 == 0 && aboveEastWater2 == 0) ? 3 : 2);
					waveNorthWest = ((northWestLevel < 1f && aboveNorthWestWater == 0 && aboveWestWater2 == 0 && aboveNorthWater2 == 0) ? 3 : 2);
					waveSouthEast = ((southEastLevel < 1f && aboveSouthEastWater2 == 0 && aboveSouthWater == 0 && aboveEastWater2 == 0) ? 3 : 2);
					waveSouthWest = ((southWestLevel < 1f && aboveSouthWestWater2 == 0 && aboveSouthWater == 0 && aboveWestWater2 == 0) ? 3 : 2);
					Vec3i normali2 = ((blockFlowing != null) ? blockFlowing.FlowNormali : null) ?? null;
					float flowVectorX2;
					float flowVectorZ2;
					if (normali2 != null)
					{
						flowVectorX2 = (float)normali2.X / 2f;
						flowVectorZ2 = (float)normali2.Z / 2f;
					}
					else
					{
						float nWestToEastFlow = this.Cmp(northWestLevel, northEastLevel);
						float sWestToEastFlow = this.Cmp(southWestLevel, southEastLevel);
						float num = this.Cmp(northWestLevel, southWestLevel);
						float eNorthToSouthFlow = this.Cmp(northEastLevel, southEastLevel);
						flowVectorX2 = nWestToEastFlow + sWestToEastFlow;
						flowVectorZ2 = num + eNorthToSouthFlow;
					}
					upFlowVectors[0] = flowVectorX2;
					upFlowVectors[1] = flowVectorZ2;
					upFlowVectors[2] = flowVectorX2;
					upFlowVectors[3] = flowVectorZ2;
					upFlowVectors[4] = flowVectorX2;
					upFlowVectors[5] = flowVectorZ2;
					upFlowVectors[6] = flowVectorX2;
					upFlowVectors[7] = flowVectorZ2;
				}
			}
			int[] array = this.shouldWave;
			array[16] = waveSouthWest;
			array[17] = waveSouthEast;
			array[18] = waveNorthWest;
			array[19] = waveNorthEast;
			array[0] = waveNorthWest;
			array[1] = waveNorthEast;
			array[8] = waveSouthEast;
			array[9] = waveSouthWest;
			array[4] = waveNorthEast;
			array[5] = waveSouthEast;
			array[12] = waveSouthWest;
			array[13] = waveNorthWest;
			int verts = 0;
			int drawFaceFlags = vars.drawFaceFlags;
			MeshData[] meshPools = vars.tct.GetPoolForPass(EnumChunkRenderPass.Liquid, 1);
			bool flag2 = (1 & drawFaceFlags) != 0;
			bool renderE = (2 & drawFaceFlags) != 0;
			bool renderS = (4 & drawFaceFlags) != 0;
			bool renderW = (8 & drawFaceFlags) != 0;
			bool nSolid = false;
			bool eSolid = false;
			bool wSolid = false;
			bool sSolid = false;
			if (inBlock.Id != 0)
			{
				this.tmpPos.Set(vars.posX, vars.posY, vars.posZ);
				this.tmpPos.SetDimension(vars.dimension);
				nSolid = inBlock.SideIsSolid(this.tmpPos, BlockFacing.NORTH.Index) && !this.IsSameLiquid(extIndex3d - this.moveSouth);
				eSolid = inBlock.SideIsSolid(this.tmpPos, BlockFacing.EAST.Index) && !this.IsSameLiquid(extIndex3d + 1);
				sSolid = inBlock.SideIsSolid(this.tmpPos, BlockFacing.SOUTH.Index) && !this.IsSameLiquid(extIndex3d + this.moveSouth);
				wSolid = inBlock.SideIsSolid(this.tmpPos, BlockFacing.WEST.Index) && !this.IsSameLiquid(extIndex3d - 1);
			}
			if ((32 & drawFaceFlags) != 0)
			{
				vars.CalcBlockFaceLight(5, extIndex3d - this.moveUp);
				this.DrawLiquidBlockFace(vars, 5, 1f, 1f, 1f, 1f, vars.blockFaceVertices[5], this.upFlowVectors, 20, 1f, 1f, meshPools);
				verts += 4;
			}
			if ((16 & drawFaceFlags) != 0)
			{
				if (vars.block.LiquidLevel == 7 && (int)vars.rainHeightMap[vars.posZ % this.chunksize * this.chunksize + vars.posX % this.chunksize] <= vars.posY)
				{
					this.extraFlags = int.MinValue;
				}
				vars.CalcBlockFaceLight(4, extIndex3d + this.moveUp);
				FastVec3f[] quadOffsetsUp = vars.blockFaceVertices[4];
				if (nSolid || eSolid || sSolid || wSolid)
				{
					float i = (nSolid ? 0.01f : 0f);
					float e = (eSolid ? 0.99f : 1f);
					float s = (sSolid ? 0.99f : 1f);
					float w = (wSolid ? 0.01f : 0f);
					this.upQuadOffsets[4] = new FastVec3f(e, 1f, s);
					this.upQuadOffsets[5] = new FastVec3f(w, 1f, s);
					this.upQuadOffsets[6] = new FastVec3f(e, 1f, i);
					this.upQuadOffsets[7] = new FastVec3f(w, 1f, i);
					quadOffsetsUp = this.upQuadOffsets;
				}
				this.DrawLiquidBlockFace(vars, 4, southEastLevel, southWestLevel, northEastLevel, northWestLevel, quadOffsetsUp, this.upFlowVectors, 16, 1f, 1f, meshPools);
				verts += 4;
				this.extraFlags = 0;
			}
			if (flag2 && !nSolid)
			{
				vars.CalcBlockFaceLight(0, extIndex3d - this.moveSouth);
				this.DrawLiquidBlockFace(vars, 0, northEastLevel, northWestLevel, 0f, 0f, vars.blockFaceVertices[0], this.flowVectorsN, 0, northWestLevel, northEastLevel, meshPools);
				verts += 4;
			}
			if (renderE && !eSolid)
			{
				vars.CalcBlockFaceLight(1, extIndex3d + 1);
				this.DrawLiquidBlockFace(vars, 1, southEastLevel, northEastLevel, 0f, 0f, vars.blockFaceVertices[1], this.flowVectorsE, 4, northEastLevel, southEastLevel, meshPools);
				verts += 4;
			}
			if (renderW && !wSolid)
			{
				vars.CalcBlockFaceLight(3, extIndex3d - 1);
				this.DrawLiquidBlockFace(vars, 3, northWestLevel, southWestLevel, 0f, 0f, vars.blockFaceVertices[3], this.flowVectorsW, 12, northWestLevel, southWestLevel, meshPools);
				verts += 4;
			}
			if (renderS && !sSolid)
			{
				vars.CalcBlockFaceLight(2, extIndex3d + this.moveSouth);
				this.DrawLiquidBlockFace(vars, 2, southWestLevel, southEastLevel, 0f, 0f, vars.blockFaceVertices[2], this.flowVectorsS, 8, southWestLevel, southEastLevel, meshPools);
				verts += 4;
			}
		}

		private void DrawLiquidBlockFace(TCTCache vars, int tileSide, float northSouthLevel, float southWestLevel, float northEastLevel, float southEastLevel, FastVec3f[] quadOffsets, float[] flowVectors, int shouldWaveOffset, float texHeightLeftRel, float texHeightRightRel, MeshData[] meshPools)
		{
			int colorMapDataValue = vars.ColorMapData.Value;
			bool flag = vars.RenderPass == EnumChunkRenderPass.Liquid;
			int textureSubId = vars.fastBlockTextureSubidsByFace[tileSide];
			TextureAtlasPosition texPos = vars.textureAtlasPositionsByTextureSubId[textureSubId];
			MeshData toreturn = meshPools[(int)texPos.atlasNumber];
			CustomMeshDataPartInt customInts = toreturn.CustomInts;
			int lastelement = toreturn.VerticesCount;
			float maxTexHeight = texPos.y2 - texPos.y1;
			int flags = vars.VertexFlags | BlockFacing.AllVertexFlagsNormals[tileSide] | this.extraFlags;
			float x = (float)vars.lx;
			float y = (float)vars.ly;
			float z = (float)vars.lz;
			FastVec3f tmpv = quadOffsets[7];
			toreturn.AddVertexWithFlags(x + tmpv.X, y + tmpv.Y * southEastLevel, z + tmpv.Z, texPos.x2, texPos.y1 + maxTexHeight * texHeightLeftRel, vars.CurrentLightRGBByCorner[3], flags);
			customInts.Add(colorMapDataValue);
			if (flag)
			{
				byte height = (byte)(texHeightLeftRel * 255f);
				customInts.Add(this.shouldWave[shouldWaveOffset + 2] | 261120 | ((int)height << 18) | this.lavaFlag | vars.OceanityFlagTL);
			}
			tmpv = quadOffsets[5];
			toreturn.AddVertexWithFlags(x + tmpv.X, y + tmpv.Y * southWestLevel, z + tmpv.Z, texPos.x2, texPos.y1, vars.CurrentLightRGBByCorner[1], flags);
			customInts.Add(colorMapDataValue);
			if (flag)
			{
				customInts.Add(this.shouldWave[shouldWaveOffset] | 261120 | this.lavaFlag | vars.OceanityFlagBL);
			}
			tmpv = quadOffsets[4];
			toreturn.AddVertexWithFlags(x + tmpv.X, y + tmpv.Y * northSouthLevel, z + tmpv.Z, texPos.x1, texPos.y1, vars.CurrentLightRGBByCorner[0], flags);
			customInts.Add(colorMapDataValue);
			if (flag)
			{
				toreturn.CustomInts.Add(this.shouldWave[shouldWaveOffset + 1] | this.lavaFlag | vars.OceanityFlagBR);
			}
			tmpv = quadOffsets[6];
			toreturn.AddVertexWithFlags(x + tmpv.X, y + tmpv.Y * northEastLevel, z + tmpv.Z, texPos.x1, texPos.y1 + maxTexHeight * texHeightRightRel, vars.CurrentLightRGBByCorner[2], flags);
			customInts.Add(colorMapDataValue);
			if (flag)
			{
				byte height2 = (byte)(texHeightRightRel * 255f);
				customInts.Add(this.shouldWave[shouldWaveOffset + 3] | ((int)height2 << 18) | this.lavaFlag | vars.OceanityFlagTR);
				toreturn.CustomFloats.Add(flowVectors);
			}
			toreturn.AddQuadIndices(lastelement);
			vars.UpdateChunkMinMax(x, y, z);
			vars.UpdateChunkMinMax(x + 1f, y + 1f, z + 1f);
		}

		private float Cmp(float val1, float val2)
		{
			if (val1 > val2)
			{
				return 0.5f;
			}
			if (val2 != val1)
			{
				return -0.5f;
			}
			return 0f;
		}

		private readonly int extChunkSize;

		private readonly Block[] extChunkDataFluids;

		private readonly Block[] extChunkDataBlocks;

		internal bool[] isLiquidBlock;

		private readonly int moveUp;

		private readonly int moveSouth;

		private readonly int moveNorthWest;

		private readonly int moveNorthEast;

		private readonly int moveSouthWest;

		private readonly int moveSouthEast;

		private readonly int moveAboveNorth;

		private readonly int moveAboveSouth;

		private readonly int moveAboveEast;

		private readonly int moveAboveWest;

		private const int byte0 = 2;

		private const int byte1 = 3;

		private int lavaFlag;

		private int extraFlags;

		private int chunksize;

		private BlockPos tmpPos = new BlockPos();

		private readonly float[] waterStillFlowVector = new float[8];

		private readonly float[] waterDownFlowVector = new float[] { 0f, -1f, 0f, -1f, 0f, -1f, 0f, -1f };

		private readonly int[] shouldWave = new int[24];

		private float[] flowVectorsN;

		private float[] flowVectorsE;

		private float[] flowVectorsS;

		private float[] flowVectorsW;

		private float[] upFlowVectors = new float[8];

		private FastVec3f[] upQuadOffsets = new FastVec3f[8];
	}
}
