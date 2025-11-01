using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class TCTCache : IGeometryTester
	{
		public TCTCache(ChunkTesselator tct)
		{
			this.tct = tct;
			this.blockFaceVertices = CubeFaceVertices.blockFaceVertices;
			this.occ = 0.67f;
			this.halfoccInverted = 0.0196875f;
			this.neighbourLightRGBS = new int[9];
			this.CurrentLightRGBByCorner = new int[4];
		}

		internal void Start(ClientMain game)
		{
			this.shapes = game.TesselatorManager;
		}

		internal void SetDimension(int dim)
		{
			this.dimension = dim;
			this.tmpPos.SetDimension(dim);
		}

		internal long CalcBlockFaceLight(int tileSide, int extNeibIndex3d)
		{
			int extIndex3d = this.extIndex3d;
			int[] CurrentLightRGBByCorner = this.CurrentLightRGBByCorner;
			if (!this.aoAndSmoothShadows || !this.block.SideAo[tileSide])
			{
				int rgb = this.tct.currentChunkRgbsExt[extNeibIndex3d];
				if (this.block.DrawType == EnumDrawType.JSON && !this.block.SideAo[tileSide])
				{
					Block block = this.tct.currentChunkBlocksExt[extNeibIndex3d];
					int? num = ((block != null) ? new int?(block.LightAbsorption) : null);
					int absorption = (int)(GameMath.Clamp(((num != null) ? new float?((float)num.GetValueOrDefault() / 32f) : null).GetValueOrDefault(), 0f, 1f) * 255f);
					int num2 = this.tct.currentChunkRgbsExt[extIndex3d];
					int newSunLight = Math.Max((num2 >> 24) & 255, ((rgb >> 24) & 255) - absorption);
					int hsv = ColorUtil.Rgb2HSv(rgb);
					int oldV = hsv & 255;
					int v = Math.Max(0, oldV - absorption);
					if (v != oldV)
					{
						hsv = (hsv & 16776960) | v;
						rgb = ColorUtil.Hsv2Rgb(hsv);
					}
					int newR = (int)Math.Max((byte)(num2 >> 16), (byte)(rgb >> 16));
					int newG = (int)Math.Max((byte)(num2 >> 8), (byte)(rgb >> 8));
					int newB = (int)Math.Max((byte)num2, (byte)rgb);
					rgb = (newSunLight << 24) | (newR << 16) | (newG << 8) | newB;
				}
				CurrentLightRGBByCorner[0] = (CurrentLightRGBByCorner[1] = (CurrentLightRGBByCorner[2] = (CurrentLightRGBByCorner[3] = rgb)));
				return (long)(rgb * 4);
			}
			int[] neighbourLightRGBS = this.neighbourLightRGBS;
			int[] currentChunkRgbsExt = this.tct.currentChunkRgbsExt;
			Block[] currentChunkBlocksExt = this.tct.currentChunkBlocksExt;
			Block[] currentChunkFluidBlocksExt = this.tct.currentChunkFluidBlocksExt;
			Vec3iAndFacingFlags[] vNeighbors = CubeFaceVertices.blockFaceVerticesCentered[tileSide];
			int blockRGB = currentChunkRgbsExt[extNeibIndex3d];
			bool thisIsALeaf = this.block.BlockMaterial == EnumBlockMaterial.Leaves;
			Block blockFront = currentChunkFluidBlocksExt[extNeibIndex3d];
			bool ao;
			if (blockFront.LightAbsorption > 0)
			{
				ao = true;
			}
			else
			{
				BlockFacing frontFacing = BlockFacing.ALLFACES[tileSide];
				blockFront = currentChunkBlocksExt[extNeibIndex3d];
				ao = blockFront.DoEmitSideAo(this, frontFacing.Opposite);
			}
			float frontAo = (ao ? this.occ : 1f);
			int neighbourLighter = 0;
			int frontCornersLighter = 0;
			int i = 0;
			while (i < 8)
			{
				neighbourLighter <<= 1;
				Vec3iAndFacingFlags neibOffset = vNeighbors[i];
				int dirExtIndex3d = extIndex3d + neibOffset.extIndexOffset;
				Block nblock = currentChunkFluidBlocksExt[dirExtIndex3d];
				if (nblock.LightAbsorption > 0)
				{
					ao = false;
					neighbourLighter |= 1;
					if (i <= 3)
					{
						neighbourLighter <<= 1;
						neighbourLighter |= 1;
					}
					else
					{
						frontCornersLighter <<= 1;
						if (!blockFront.DoEmitSideAoByFlag(this, vNeighbors[8], neibOffset.FacingFlags) || (blockFront.ForFluidsLayer && blockFront.LightAbsorption > 0))
						{
							frontCornersLighter |= 1;
						}
					}
				}
				else
				{
					nblock = currentChunkBlocksExt[dirExtIndex3d];
					if (i <= 3)
					{
						ao = nblock.DoEmitSideAoByFlag(this, neibOffset, neibOffset.OppositeFlagsUpperOrLeft) || (thisIsALeaf && nblock.BlockMaterial == EnumBlockMaterial.Leaves);
						if (!ao)
						{
							neighbourLighter |= 1;
						}
						neighbourLighter <<= 1;
						if (!nblock.DoEmitSideAoByFlag(this, neibOffset, neibOffset.OppositeFlagsLowerOrRight) && (!thisIsALeaf || nblock.BlockMaterial != EnumBlockMaterial.Leaves))
						{
							neighbourLighter |= 1;
							ao = false;
						}
					}
					else
					{
						frontCornersLighter <<= 1;
						ao = nblock.DoEmitSideAoByFlag(this, neibOffset, neibOffset.OppositeFlags) || (thisIsALeaf && nblock.BlockMaterial == EnumBlockMaterial.Leaves);
						if (!ao)
						{
							neighbourLighter |= 1;
						}
						if (!blockFront.DoEmitSideAoByFlag(this, vNeighbors[8], neibOffset.FacingFlags) || (blockFront.ForFluidsLayer && blockFront.LightAbsorption > 0))
						{
							frontCornersLighter |= 1;
						}
					}
				}
				i++;
				neighbourLightRGBS[i] = (ao ? blockRGB : currentChunkRgbsExt[dirExtIndex3d]);
			}
			int doBottomRight = 8 * (neighbourLighter & 1);
			int doBottomLeft = 7 * ((neighbourLighter >>= 1) & 1);
			int doTopRight = 6 * ((neighbourLighter >>= 1) & 1);
			int doTopLeft = 5 * ((neighbourLighter >>= 1) & 1);
			int doRightLower = 4 * ((neighbourLighter >>= 1) & 1);
			int doRightUpper = 4 * ((neighbourLighter >>= 1) & 1);
			int doLeftLower = 3 * ((neighbourLighter >>= 1) & 1);
			int doLeftUpper = 3 * ((neighbourLighter >>= 1) & 1);
			int doBottomLHS = 2 * ((neighbourLighter >>= 1) & 1);
			int doBottomRHS = 2 * ((neighbourLighter >>= 1) & 1);
			int doTopLHS = (neighbourLighter >>= 1) & 1;
			int doTopRHS = neighbourLighter >> 1;
			if (tileSide >= 4)
			{
				int num3 = doTopRHS;
				doTopRHS = doTopLHS;
				doTopLHS = num3;
				int num4 = doBottomRHS;
				doBottomRHS = doBottomLHS;
				doBottomLHS = num4;
			}
			ushort sunLight = (ushort)((blockRGB >> 24) & 255);
			ushort r = (ushort)((blockRGB >> 16) & 255);
			ushort g = (ushort)((blockRGB >> 8) & 255);
			ushort b = (ushort)(blockRGB & 255);
			return (long)(CurrentLightRGBByCorner[0] = this.CornerAoRGB(doTopLHS, doLeftUpper, doTopLeft, frontCornersLighter & 1, frontAo, sunLight, r, g, b)) + (long)(CurrentLightRGBByCorner[1] = this.CornerAoRGB(doTopRHS, doRightUpper, doTopRight, (frontCornersLighter >> 1) & 1, frontAo, sunLight, r, g, b)) + (long)(CurrentLightRGBByCorner[2] = this.CornerAoRGB(doBottomLHS, doLeftLower, doBottomLeft, (frontCornersLighter >> 2) & 1, frontAo, sunLight, r, g, b)) + (long)(CurrentLightRGBByCorner[3] = this.CornerAoRGB(doBottomRHS, doRightLower, doBottomRight, (frontCornersLighter >> 3) & 1, frontAo, sunLight, r, g, b));
		}

		private int CornerAoRGB(int ndir1, int ndir2, int ndirbetween, int frontCorner, float frontAo, ushort s, ushort r, ushort g, ushort b)
		{
			float cornerAO;
			if (ndir1 + ndir2 == 0 || frontCorner + ndirbetween == 0)
			{
				float brightnessloss = this.halfoccInverted * (float)GameMath.Clamp(this.block.LightAbsorption, 0, 32);
				cornerAO = Math.Min(this.occ, 1f - brightnessloss);
			}
			else
			{
				cornerAO = ((ndir1 * ndir2 * ndirbetween == 0) ? this.occ : frontAo);
				int facesconsidered = 1;
				if (ndir1 > 0)
				{
					int blockRGB = this.neighbourLightRGBS[ndir1];
					s += (ushort)((blockRGB >> 24) & 255);
					r += (ushort)((blockRGB >> 16) & 255);
					g += (ushort)((blockRGB >> 8) & 255);
					b += (ushort)(blockRGB & 255);
					facesconsidered++;
				}
				if (ndir2 > 0)
				{
					int blockRGB = this.neighbourLightRGBS[ndir2];
					s += (ushort)((blockRGB >> 24) & 255);
					r += (ushort)((blockRGB >> 16) & 255);
					g += (ushort)((blockRGB >> 8) & 255);
					b += (ushort)(blockRGB & 255);
					facesconsidered++;
				}
				if (ndirbetween > 0)
				{
					int blockRGB = this.neighbourLightRGBS[ndirbetween];
					s += (ushort)((blockRGB >> 24) & 255);
					r += (ushort)((blockRGB >> 16) & 255);
					g += (ushort)((blockRGB >> 8) & 255);
					b += (ushort)(blockRGB & 255);
					facesconsidered++;
				}
				cornerAO /= (float)facesconsidered;
			}
			return ((int)((float)s * cornerAO) << 24) | ((int)((float)r * cornerAO) << 16) | ((int)((float)g * cornerAO) << 8) | (int)((float)b * cornerAO);
		}

		public BlockEntity GetCurrentBlockEntityOnSide(BlockFacing side)
		{
			this.tmpPos.Set(this.posX, this.posY, this.posZ).Offset(side);
			return this.tct.game.BlockAccessor.GetBlockEntity(this.tmpPos);
		}

		public BlockEntity GetCurrentBlockEntityOnSide(Vec3iAndFacingFlags neibOffset)
		{
			this.tmpPos.Set(this.posX + neibOffset.X, this.posY + neibOffset.Y, this.posZ + neibOffset.Z);
			return this.tct.game.BlockAccessor.GetBlockEntity(this.tmpPos);
		}

		public void UpdateChunkMinMax(float x, float y, float z)
		{
			if (x < this.xMin)
			{
				this.xMin = x;
			}
			else if (x > this.xMax)
			{
				this.xMax = x;
			}
			if (y < this.yMin)
			{
				this.yMin = y;
			}
			else if (y > this.yMax)
			{
				this.yMax = y;
			}
			if (z < this.zMin)
			{
				this.zMin = z;
				return;
			}
			if (z > this.zMax)
			{
				this.zMax = z;
			}
		}

		public const long DARK = 789516L;

		public FastVec3f[][] blockFaceVertices = CubeFaceVertices.blockFaceVertices;

		public int lx;

		public int ly;

		public int lz;

		public int posX;

		public int posY;

		public int posZ;

		public int dimension;

		public int extIndex3d;

		public int index3d;

		public float finalX;

		public float finalY;

		public float finalZ;

		public float xMin;

		public float xMax;

		public float yMin;

		public float yMax;

		public float zMin;

		public float zMax;

		public int drawFaceFlags;

		public int blockId;

		public Block block;

		public ShapeTesselatorManager shapes;

		public float[] preRotationMatrix;

		public int textureSubId;

		public float textureVOffset;

		public int decorSubPosition;

		public int decorRotationData;

		public ColorMapData ColorMapData;

		public int VertexFlags;

		public EnumChunkRenderPass RenderPass;

		public float occ;

		public float halfoccInverted;

		private readonly int[] neighbourLightRGBS;

		public readonly int[] CurrentLightRGBByCorner;

		public ChunkTesselator tct;

		public TextureAtlasPosition[] textureAtlasPositionsByTextureSubId;

		public int[] fastBlockTextureSubidsByFace;

		public bool aoAndSmoothShadows;

		public const int chunkSize = 32;

		public const int extChunkSize = 34;

		public const int extMovey = 1156;

		internal Dictionary<BlockPos, BlockEntity> blockEntitiesOfChunk = new Dictionary<BlockPos, BlockEntity>();

		private BlockPos tmpPos = new BlockPos();

		public ushort[] rainHeightMap;

		public int OceanityFlagTL;

		public int OceanityFlagTR;

		public int OceanityFlagBL;

		public int OceanityFlagBR;
	}
}
