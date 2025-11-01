using System;
using Vintagestory.API.Client;
using Vintagestory.API.Client.Tesselation;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class SurfaceLayerTesselator : IBlockTesselator
	{
		public void Tesselate(TCTCache vars)
		{
			int verts = 0;
			TextureAtlasPosition[] textureAtlasPositionsByTextureSubId = vars.textureAtlasPositionsByTextureSubId;
			int[] fastBlockTextureSubidsByFace = vars.fastBlockTextureSubidsByFace;
			bool hasAlternates = vars.block.HasAlternates;
			int colorMapDataValue = vars.ColorMapData.Value;
			int drawFaceFlags = vars.drawFaceFlags ^ 63;
			int extIndex3d = vars.extIndex3d;
			int allFlags = vars.VertexFlags;
			FastVec3f[][] blockFaceVertices = vars.blockFaceVertices;
			BakedCompositeTexture[][] textures = null;
			int randomSelector = 0;
			if (hasAlternates)
			{
				textures = vars.block.FastTextureVariants;
				randomSelector = GameMath.MurmurHash3(vars.posX, vars.posY, vars.posZ);
			}
			int rotIndex = 0;
			if (vars.block.RandomizeRotations)
			{
				rotIndex = GameMath.MurmurHash3Mod(vars.posX, vars.posY, vars.posZ, 4);
			}
			MeshData[] meshPools = vars.tct.GetPoolForPass(vars.RenderPass, vars.block.DoNotRenderAtLod2 ? 2 : 1);
			MeshData[] meshPoolsDark = vars.tct.GetPoolForPass(vars.RenderPass, 0);
			for (int tileSide = 0; tileSide < 6; tileSide++)
			{
				if ((drawFaceFlags & (1 << tileSide)) != 0)
				{
					int lightfaceSide = BlockFacing.ALLFACES[tileSide].Opposite.Index;
					long lighting = vars.CalcBlockFaceLight(lightfaceSide, extIndex3d + TileSideEnum.MoveIndex[lightfaceSide]);
					if (!hasAlternates)
					{
						goto IL_0144;
					}
					BakedCompositeTexture[] variants = textures[tileSide];
					if (variants == null)
					{
						goto IL_0144;
					}
					int textureSubId = variants[GameMath.Mod(randomSelector, variants.Length)].TextureSubId;
					IL_0149:
					this.DrawBlockFace(vars, tileSide, blockFaceVertices[tileSide], textureAtlasPositionsByTextureSubId[textureSubId], allFlags | BlockFacing.ALLFACES[TileSideEnum.GetOpposite(tileSide)].NormalPackedFlags, colorMapDataValue, (lighting == 789516L) ? meshPoolsDark : meshPools, 1f, rotIndex);
					verts += 4;
					goto IL_018C;
					IL_0144:
					textureSubId = fastBlockTextureSubidsByFace[tileSide];
					goto IL_0149;
				}
				IL_018C:;
			}
		}

		public void DrawBlockFace(TCTCache vars, int tileSide, FastVec3f[] quadOffsets, TextureAtlasPosition texPos, int flags, int colorMapDataValue, MeshData[] meshPools, float blockHeight = 1f, int rotIndex = 0)
		{
			MeshData toreturn = meshPools[(int)texPos.atlasNumber];
			int lastelement = toreturn.VerticesCount;
			int[] currentLightRGBByCorner = vars.CurrentLightRGBByCorner;
			float uvx = texPos.x1;
			float uvy = texPos.y1;
			float uvx2 = texPos.x2;
			float uvy2 = texPos.y2;
			if (rotIndex > 1)
			{
				uvx = texPos.x2;
				uvy = texPos.y2;
				uvx2 = texPos.x1;
				uvy2 = texPos.y1;
			}
			if (rotIndex == 1 || rotIndex == 3)
			{
				float uvwdt = uvx2 - uvx;
				float uvhgt = uvy2 - uvy;
				float uvbx = uvx;
				float uvby = uvy;
				uvx = uvbx + uvwdt;
				uvy = uvby;
				uvx2 = uvbx;
				uvy2 = uvby + uvhgt;
			}
			Vec3f normalf = BlockFacing.ALLFACES[tileSide].Normalf;
			float x = vars.finalX - normalf.X * SurfaceLayerTesselator.decorFaceOffset;
			float y = vars.finalY - normalf.Y * SurfaceLayerTesselator.decorFaceOffset;
			float z = vars.finalZ - normalf.Z * SurfaceLayerTesselator.decorFaceOffset;
			float factor = 1.0001f;
			float dxTL = 0f;
			float dxBL = 0f;
			float dxTR = 0f;
			float dxBR = 0f;
			float dyT = 0f;
			float dyB = 0f;
			float dzTL = 0f;
			float dzBL = 0f;
			float dzTR = 0f;
			float dzBR = 0f;
			int uvIndex = (8 - vars.decorRotationData % 4 * 2) % 8;
			float[] uv = this.uv;
			if (vars.decorSubPosition > 0)
			{
				string path = vars.block.Code.Path;
				float xSize = (uvx2 - uvx) / (float)GlobalConstants.CaveArtColsPerRow;
				float ySize = (uvy2 - uvy) / (float)GlobalConstants.CaveArtColsPerRow;
				uvx += (float)(path[path.Length - 3] - '1') * xSize;
				uvy += (float)(path[path.Length - 1] - '1') * ySize;
				uvx2 = uvx + xSize;
				uvy2 = uvy + ySize;
				int num = vars.decorSubPosition - 1;
				factor /= 16f;
				float xx = (float)(num % 16) * factor;
				float yy = (float)(num / 16) * factor;
				float nearOne = 0.9375f;
				factor *= 4f;
				float minLimit = factor;
				float maxLimit = 1f - factor;
				switch (tileSide)
				{
				case 0:
					x += xx - factor;
					y += nearOne - yy - factor;
					if (xx > maxLimit)
					{
						if (!this.CaveArtBlockOnSide(vars, tileSide, BlockFacing.EAST))
						{
							float excess = (xx - maxLimit) / 0.5f;
							this.cropRightSide(ref uvx, ref uvx2, ref uvy, ref uvy2, xSize, ySize, excess, vars.decorRotationData);
							dxTL = -factor * 2f * excess;
							dxBL = -factor * 2f * excess;
						}
					}
					else if (xx < minLimit && !this.CaveArtBlockOnSide(vars, tileSide, BlockFacing.WEST))
					{
						float excess2 = (minLimit - xx) / 0.5f;
						this.cropLeftSide(ref uvx, ref uvx2, ref uvy, ref uvy2, xSize, ySize, excess2, vars.decorRotationData);
						dxTR = factor * 2f * excess2;
						dxBR = factor * 2f * excess2;
					}
					if (nearOne - yy > maxLimit)
					{
						if (!this.CaveArtBlockOnSide(vars, tileSide, BlockFacing.UP))
						{
							float excess3 = (nearOne - yy - maxLimit) / 0.5f;
							this.cropTopSide(ref uvx, ref uvx2, ref uvy, ref uvy2, xSize, ySize, excess3, vars.decorRotationData);
							dyT = -factor * 2f * excess3;
						}
					}
					else if (nearOne - yy < minLimit && !this.CaveArtBlockOnSide(vars, tileSide, BlockFacing.DOWN))
					{
						float excess4 = (minLimit - nearOne + yy) / 0.5f;
						this.cropBottomSide(ref uvx, ref uvx2, ref uvy, ref uvy2, xSize, ySize, excess4, vars.decorRotationData);
						dyB = factor * 2f * excess4;
					}
					break;
				case 1:
					z += xx - factor;
					y += nearOne - yy - factor;
					x += 0.5f;
					if (xx > maxLimit)
					{
						if (!this.CaveArtBlockOnSide(vars, tileSide, BlockFacing.SOUTH))
						{
							float excess5 = (xx - maxLimit) / 0.5f;
							this.cropRightSide(ref uvx, ref uvx2, ref uvy, ref uvy2, xSize, ySize, excess5, vars.decorRotationData);
							dzTL = -factor * 2f * excess5;
							dzBL = -factor * 2f * excess5;
						}
					}
					else if (xx < minLimit && !this.CaveArtBlockOnSide(vars, tileSide, BlockFacing.NORTH))
					{
						float excess6 = (minLimit - xx) / 0.5f;
						this.cropLeftSide(ref uvx, ref uvx2, ref uvy, ref uvy2, xSize, ySize, excess6, vars.decorRotationData);
						dzTR = factor * 2f * excess6;
						dzBR = factor * 2f * excess6;
					}
					if (nearOne - yy > maxLimit)
					{
						if (!this.CaveArtBlockOnSide(vars, tileSide, BlockFacing.UP))
						{
							float excess7 = (nearOne - yy - maxLimit) / 0.5f;
							this.cropTopSide(ref uvx, ref uvx2, ref uvy, ref uvy2, xSize, ySize, excess7, vars.decorRotationData);
							dyT = -factor * 2f * excess7;
						}
					}
					else if (nearOne - yy < minLimit && !this.CaveArtBlockOnSide(vars, tileSide, BlockFacing.DOWN))
					{
						float excess8 = (minLimit - nearOne + yy) / 0.5f;
						this.cropBottomSide(ref uvx, ref uvx2, ref uvy, ref uvy2, xSize, ySize, excess8, vars.decorRotationData);
						dyB = factor * 2f * excess8;
					}
					break;
				case 2:
					x += nearOne - xx - factor;
					y += nearOne - yy - factor;
					z += 0.5f;
					if (nearOne - xx > maxLimit)
					{
						if (!this.CaveArtBlockOnSide(vars, tileSide, BlockFacing.EAST))
						{
							float excess9 = (nearOne - xx - maxLimit) / 0.5f;
							this.cropLeftSide(ref uvx, ref uvx2, ref uvy, ref uvy2, xSize, ySize, excess9, vars.decorRotationData);
							dxTR = -factor * 2f * excess9;
							dxBR = -factor * 2f * excess9;
						}
					}
					else if (nearOne - xx < minLimit && !this.CaveArtBlockOnSide(vars, tileSide, BlockFacing.WEST))
					{
						float excess10 = (minLimit - nearOne + xx) / 0.5f;
						this.cropRightSide(ref uvx, ref uvx2, ref uvy, ref uvy2, xSize, ySize, excess10, vars.decorRotationData);
						dxTL = factor * 2f * excess10;
						dxBL = factor * 2f * excess10;
					}
					if (nearOne - yy > maxLimit)
					{
						if (!this.CaveArtBlockOnSide(vars, tileSide, BlockFacing.UP))
						{
							float excess11 = (nearOne - yy - maxLimit) / 0.5f;
							this.cropTopSide(ref uvx, ref uvx2, ref uvy, ref uvy2, xSize, ySize, excess11, vars.decorRotationData);
							dyT = -factor * 2f * excess11;
						}
					}
					else if (nearOne - yy < minLimit && !this.CaveArtBlockOnSide(vars, tileSide, BlockFacing.DOWN))
					{
						float excess12 = (minLimit - nearOne + yy) / 0.5f;
						this.cropBottomSide(ref uvx, ref uvx2, ref uvy, ref uvy2, xSize, ySize, excess12, vars.decorRotationData);
						dyB = factor * 2f * excess12;
					}
					break;
				case 3:
					z += nearOne - xx - factor;
					y += nearOne - yy - factor;
					if (nearOne - xx > maxLimit)
					{
						if (!this.CaveArtBlockOnSide(vars, tileSide, BlockFacing.SOUTH))
						{
							float excess13 = (nearOne - xx - maxLimit) / 0.5f;
							this.cropLeftSide(ref uvx, ref uvx2, ref uvy, ref uvy2, xSize, ySize, excess13, vars.decorRotationData);
							dzTR = -factor * 2f * excess13;
							dzBR = -factor * 2f * excess13;
						}
					}
					else if (nearOne - xx < minLimit && !this.CaveArtBlockOnSide(vars, tileSide, BlockFacing.NORTH))
					{
						float excess14 = (minLimit - nearOne + xx) / 0.5f;
						this.cropRightSide(ref uvx, ref uvx2, ref uvy, ref uvy2, xSize, ySize, excess14, vars.decorRotationData);
						dzTL = factor * 2f * excess14;
						dzBL = factor * 2f * excess14;
					}
					if (nearOne - yy > maxLimit)
					{
						if (!this.CaveArtBlockOnSide(vars, tileSide, BlockFacing.UP))
						{
							float excess15 = (nearOne - yy - maxLimit) / 0.5f;
							this.cropTopSide(ref uvx, ref uvx2, ref uvy, ref uvy2, xSize, ySize, excess15, vars.decorRotationData);
							dyT = -factor * 2f * excess15;
						}
					}
					else if (nearOne - yy < minLimit && !this.CaveArtBlockOnSide(vars, tileSide, BlockFacing.DOWN))
					{
						float excess16 = (minLimit - nearOne + yy) / 0.5f;
						this.cropBottomSide(ref uvx, ref uvx2, ref uvy, ref uvy2, xSize, ySize, excess16, vars.decorRotationData);
						dyB = factor * 2f * excess16;
					}
					break;
				case 4:
					x += xx - factor;
					z += nearOne - yy - factor;
					y += 0.5f;
					if (xx > maxLimit)
					{
						if (!this.CaveArtBlockOnSide(vars, tileSide, BlockFacing.EAST))
						{
							float excess17 = (xx - maxLimit) / 0.5f;
							this.cropRightSide(ref uvx, ref uvx2, ref uvy, ref uvy2, xSize, ySize, excess17, vars.decorRotationData);
							dxTL = -factor * 2f * excess17;
							dxBL = -factor * 2f * excess17;
						}
					}
					else if (xx < minLimit && !this.CaveArtBlockOnSide(vars, tileSide, BlockFacing.WEST))
					{
						float excess18 = (minLimit - xx) / 0.5f;
						this.cropLeftSide(ref uvx, ref uvx2, ref uvy, ref uvy2, xSize, ySize, excess18, vars.decorRotationData);
						dxTR = factor * 2f * excess18;
						dxBR = factor * 2f * excess18;
					}
					if (nearOne - yy > maxLimit)
					{
						if (!this.CaveArtBlockOnSide(vars, tileSide, BlockFacing.SOUTH))
						{
							float excess19 = (nearOne - yy - maxLimit) / 0.5f;
							this.cropTopSide(ref uvx, ref uvx2, ref uvy, ref uvy2, xSize, ySize, excess19, vars.decorRotationData);
							dzTR = -factor * 2f * excess19;
							dzTL = -factor * 2f * excess19;
						}
					}
					else if (nearOne - yy < minLimit && !this.CaveArtBlockOnSide(vars, tileSide, BlockFacing.NORTH))
					{
						float excess20 = (minLimit - nearOne + yy) / 0.5f;
						this.cropBottomSide(ref uvx, ref uvx2, ref uvy, ref uvy2, xSize, ySize, excess20, vars.decorRotationData);
						dzBR = factor * 2f * excess20;
						dzBL = factor * 2f * excess20;
					}
					break;
				case 5:
					x += xx - factor;
					z += yy - factor;
					if (xx > maxLimit)
					{
						if (!this.CaveArtBlockOnSide(vars, tileSide, BlockFacing.EAST))
						{
							float excess21 = (xx - maxLimit) / 0.5f;
							this.cropRightSide(ref uvx, ref uvx2, ref uvy, ref uvy2, xSize, ySize, excess21, vars.decorRotationData);
							dxTL = -factor * 2f * excess21;
							dxBL = -factor * 2f * excess21;
						}
					}
					else if (xx < minLimit && !this.CaveArtBlockOnSide(vars, tileSide, BlockFacing.WEST))
					{
						float excess22 = (minLimit - xx) / 0.5f;
						this.cropLeftSide(ref uvx, ref uvx2, ref uvy, ref uvy2, xSize, ySize, excess22, vars.decorRotationData);
						dxTR = factor * 2f * excess22;
						dxBR = factor * 2f * excess22;
					}
					if (yy > maxLimit)
					{
						if (!this.CaveArtBlockOnSide(vars, tileSide, BlockFacing.SOUTH))
						{
							float excess23 = (yy - maxLimit) / 0.5f;
							this.cropBottomSide(ref uvx, ref uvx2, ref uvy, ref uvy2, xSize, ySize, excess23, vars.decorRotationData);
							dzBR = -factor * 2f * excess23;
							dzBL = -factor * 2f * excess23;
						}
					}
					else if (yy < minLimit && !this.CaveArtBlockOnSide(vars, tileSide, BlockFacing.NORTH))
					{
						float excess24 = (minLimit - yy) / 0.5f;
						this.cropTopSide(ref uvx, ref uvx2, ref uvy, ref uvy2, xSize, ySize, excess24, vars.decorRotationData);
						dzTR = factor * 2f * excess24;
						dzTL = factor * 2f * excess24;
					}
					break;
				}
				factor *= 2f;
			}
			if ((vars.decorRotationData & 4) > 0)
			{
				uv[0] = (uv[6] = uvx);
				uv[2] = (uv[4] = uvx2);
			}
			else
			{
				uv[0] = (uv[6] = uvx2);
				uv[2] = (uv[4] = uvx);
			}
			uv[1] = (uv[3] = uvy);
			uv[5] = (uv[7] = uvy2);
			FastVec3f tmpv = quadOffsets[6];
			toreturn.AddVertexWithFlags(x + tmpv.X * factor + dxBL, y + tmpv.Y * factor + dyB, z + tmpv.Z * factor + dzBL, uv[(uvIndex + 4) % 8], uv[(uvIndex + 5) % 8], currentLightRGBByCorner[(tileSide > 3) ? 0 : 3], flags);
			tmpv = quadOffsets[4];
			toreturn.AddVertexWithFlags(x + tmpv.X * factor + dxTL, y + tmpv.Y * factor + dyT, z + tmpv.Z * factor + dzTL, uv[(uvIndex + 2) % 8], uv[(uvIndex + 3) % 8], currentLightRGBByCorner[(tileSide > 3) ? 2 : 1], flags);
			tmpv = quadOffsets[5];
			toreturn.AddVertexWithFlags(x + tmpv.X * factor + dxTR, y + tmpv.Y * factor + dyT, z + tmpv.Z * factor + dzTR, uv[uvIndex], uv[uvIndex + 1], currentLightRGBByCorner[(tileSide > 3) ? 3 : 0], flags);
			tmpv = quadOffsets[7];
			toreturn.AddVertexWithFlags(x + tmpv.X * factor + dxBR, y + tmpv.Y * factor + dyB, z + tmpv.Z * factor + dzBR, uv[(uvIndex + 6) % 8], uv[(uvIndex + 7) % 8], currentLightRGBByCorner[(tileSide > 3) ? 1 : 2], flags);
			toreturn.CustomInts.Add4(colorMapDataValue);
			toreturn.AddQuadIndices(lastelement);
		}

		private void cropRightSide(ref float uvx1, ref float uvx2, ref float uvy1, ref float uvy2, float xSize, float ySize, float excess, int rot)
		{
			switch (rot % 8)
			{
			case 0:
			case 6:
				uvx1 += xSize * excess;
				return;
			case 1:
			case 5:
				uvy1 += ySize * excess;
				return;
			case 2:
			case 4:
				uvx2 -= xSize * excess;
				return;
			case 3:
			case 7:
				uvy2 -= ySize * excess;
				return;
			default:
				return;
			}
		}

		private void cropLeftSide(ref float uvx1, ref float uvx2, ref float uvy1, ref float uvy2, float xSize, float ySize, float excess, int rot)
		{
			switch (rot % 8)
			{
			case 0:
			case 6:
				uvx2 -= xSize * excess;
				return;
			case 1:
			case 5:
				uvy2 -= ySize * excess;
				return;
			case 2:
			case 4:
				uvx1 += xSize * excess;
				return;
			case 3:
			case 7:
				uvy1 += ySize * excess;
				return;
			default:
				return;
			}
		}

		private void cropTopSide(ref float uvx1, ref float uvx2, ref float uvy1, ref float uvy2, float xSize, float ySize, float excess, int rot)
		{
			switch (rot % 8)
			{
			case 0:
			case 4:
				uvy1 += ySize * excess;
				return;
			case 1:
			case 7:
				uvx2 -= xSize * excess;
				return;
			case 2:
			case 6:
				uvy2 -= ySize * excess;
				return;
			case 3:
			case 5:
				uvx1 += xSize * excess;
				return;
			default:
				return;
			}
		}

		private void cropBottomSide(ref float uvx1, ref float uvx2, ref float uvy1, ref float uvy2, float xSize, float ySize, float excess, int rot)
		{
			switch (rot % 8)
			{
			case 0:
			case 4:
				uvy2 -= ySize * excess;
				return;
			case 1:
			case 7:
				uvx1 += xSize * excess;
				return;
			case 2:
			case 6:
				uvy1 += ySize * excess;
				return;
			case 3:
			case 5:
				uvx2 -= xSize * excess;
				return;
			default:
				return;
			}
		}

		private bool CaveArtBlockOnSide(TCTCache vars, int tileSide, BlockFacing neibDir)
		{
			int extIndex3d = vars.extIndex3d;
			EnumBlockMaterial baseMaterial = vars.tct.currentChunkBlocksExt[extIndex3d].BlockMaterial;
			switch (neibDir.Index)
			{
			case 0:
				extIndex3d -= 34;
				break;
			case 1:
				extIndex3d++;
				break;
			case 2:
				extIndex3d += 34;
				break;
			case 3:
				extIndex3d--;
				break;
			case 4:
				extIndex3d += 1156;
				break;
			case 5:
				extIndex3d -= 1156;
				break;
			}
			Block neib = vars.tct.currentChunkBlocksExt[extIndex3d];
			return neib.SideSolid[tileSide] && neib.BlockMaterial == baseMaterial;
		}

		private const int caveArtPerBlock = 16;

		private static float decorFaceOffset = 0.002f;

		private float[] uv = new float[8];
	}
}
