using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class CrossTesselator : IBlockTesselator
	{
		public void Tesselate(TCTCache vars)
		{
			vars.drawFaceFlags = 3;
			CrossTesselator.DrawCross(vars, 1.41f);
		}

		public static void DrawCross(TCTCache vars, float vScaleY)
		{
			Block block = vars.block;
			TextureAtlasPosition[] textureAtlasPositionsByTextureSubId = vars.textureAtlasPositionsByTextureSubId;
			int[] fastBlockTextureSubidsByFace = vars.fastBlockTextureSubidsByFace;
			bool hasAlternates = block.HasAlternates;
			bool randomizeRotations = block.RandomizeRotations;
			int colorMapDataValue = vars.ColorMapData.Value;
			int downRenderFlags = block.VertexFlags.All & -503316481;
			int upRenderFlags = block.VertexFlags.All;
			downRenderFlags |= BlockFacing.UP.NormalPackedFlags;
			upRenderFlags |= BlockFacing.UP.NormalPackedFlags;
			int blockRgb = vars.tct.currentChunkRgbsExt[vars.extIndex3d];
			BakedCompositeTexture[][] textures = null;
			int randomSelector = 0;
			if (hasAlternates || randomizeRotations)
			{
				if (hasAlternates)
				{
					textures = block.FastTextureVariants;
				}
				randomSelector = GameMath.MurmurHash3(vars.posX, (block.RandomizeAxes == EnumRandomizeAxes.XYZ) ? vars.posY : 0, vars.posZ);
			}
			MeshData[] meshPool = vars.tct.GetPoolForPass(block.RenderPass, vars.block.DoNotRenderAtLod2 ? 2 : 1);
			int i = 0;
			while (i < 2)
			{
				int tileSide = i * 2;
				if (!hasAlternates)
				{
					goto IL_0115;
				}
				BakedCompositeTexture[] variants = textures[tileSide];
				if (variants == null)
				{
					goto IL_0115;
				}
				int textureSubId = variants[GameMath.Mod(randomSelector, variants.Length)].TextureSubId;
				IL_011B:
				TextureAtlasPosition texPos = textureAtlasPositionsByTextureSubId[textureSubId];
				MeshData meshData = meshPool[(int)texPos.atlasNumber];
				int lastelement = meshData.VerticesCount;
				float xPos = vars.finalX;
				float yPos = vars.finalY;
				float zPos = vars.finalZ;
				float xPosEnd;
				float yPosEnd;
				float zPosEnd;
				if (randomizeRotations)
				{
					float[] array = TesselationMetaData.randomRotMatrices[GameMath.Mod(randomSelector, TesselationMetaData.randomRotations.Length)];
					Mat4f.MulWithVec3_Position(array, (float)i, 0f, 0f, CrossTesselator.startRot);
					Mat4f.MulWithVec3_Position(array, 1f - (float)i, vScaleY, 1f, CrossTesselator.endRot);
					xPosEnd = CrossTesselator.endRot.X + xPos;
					xPos += CrossTesselator.startRot.X;
					yPosEnd = CrossTesselator.endRot.Y + yPos;
					yPos += CrossTesselator.startRot.Y;
					zPosEnd = CrossTesselator.endRot.Z + zPos;
					zPos += CrossTesselator.startRot.Z;
				}
				else
				{
					xPosEnd = xPos + (1f - (float)i);
					yPosEnd = yPos + vScaleY;
					zPosEnd = zPos + 1f;
					xPos += (float)i;
				}
				meshData.AddVertexWithFlags(xPosEnd, yPos, zPosEnd, texPos.x2, texPos.y2, blockRgb, downRenderFlags);
				meshData.AddVertexWithFlags(xPosEnd, yPosEnd, zPosEnd, texPos.x2, texPos.y1, blockRgb, upRenderFlags);
				meshData.AddVertexWithFlags(xPos, yPosEnd, zPos, texPos.x1, texPos.y1, blockRgb, upRenderFlags);
				meshData.AddVertexWithFlags(xPos, yPos, zPos, texPos.x1, texPos.y2, blockRgb, downRenderFlags);
				vars.UpdateChunkMinMax(xPos, yPos, zPos);
				vars.UpdateChunkMinMax(xPosEnd, yPosEnd, zPosEnd);
				meshData.CustomInts.Add4(colorMapDataValue);
				meshData.AddQuadIndices(lastelement);
				i++;
				continue;
				IL_0115:
				textureSubId = fastBlockTextureSubidsByFace[tileSide];
				goto IL_011B;
			}
		}

		private static Vec3f startRot = new Vec3f();

		private static Vec3f endRot = new Vec3f();
	}
}
