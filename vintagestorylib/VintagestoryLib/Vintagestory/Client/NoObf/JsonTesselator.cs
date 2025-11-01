using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Client.Tesselation;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class JsonTesselator : IBlockTesselator
	{
		public JsonTesselator()
		{
			this.helper.tess = this;
		}

		public long SetUpLightRGBs(TCTCache vars)
		{
			int extIndex3d = vars.extIndex3d;
			int[] currentLightRGBByCorner = vars.CurrentLightRGBByCorner;
			int lightRGBindex = 0;
			long totalLight = 0L;
			int[] jsonLightRGB = this.jsonLightRGB;
			for (int tileSide = 0; tileSide < 6; tileSide++)
			{
				totalLight += vars.CalcBlockFaceLight(tileSide, extIndex3d + TileSideEnum.MoveIndex[tileSide]);
				jsonLightRGB[lightRGBindex++] = currentLightRGBByCorner[0];
				jsonLightRGB[lightRGBindex++] = currentLightRGBByCorner[1];
				jsonLightRGB[lightRGBindex++] = currentLightRGBByCorner[2];
				jsonLightRGB[lightRGBindex++] = currentLightRGBByCorner[3];
			}
			return totalLight + (long)(jsonLightRGB[24] = vars.tct.currentChunkRgbsExt[extIndex3d]);
		}

		public void Tesselate(TCTCache vars)
		{
			int extIndex3d = vars.extIndex3d;
			long totalLight = this.SetUpLightRGBs(vars);
			this.helper.vars = vars;
			this.pos.SetDimension(vars.dimension);
			this.pos.Set(vars.posX, vars.posY, vars.posZ);
			Dictionary<BlockPos, BlockEntity> blockEntitiesOfChunk = vars.blockEntitiesOfChunk;
			BlockEntity be;
			if (blockEntitiesOfChunk != null && blockEntitiesOfChunk.TryGetValue(this.pos, out be))
			{
				try
				{
					if (be != null && be.OnTesselation(this.helper, vars.tct.offthreadTesselator))
					{
						return;
					}
				}
				catch (Exception e)
				{
					vars.tct.game.Logger.Error("Exception thrown during OnTesselation() of block entity {0}@{1}/{2}/{3}. Block will probably not be rendered as intended.", new object[] { be, vars.posX, vars.posY, vars.posZ });
					vars.tct.game.Logger.Error(e);
				}
			}
			MeshData lod1Mesh = null;
			try
			{
				lod1Mesh = vars.shapes.GetDefaultBlockMesh(vars.block);
			}
			catch (Exception e2)
			{
				vars.tct.game.Logger.Error("Exception thrown during tesselation of block {0}@{1}/{2}/{3}. Block will be invisible.", new object[]
				{
					vars.block.Code.ToShortString(),
					vars.posX,
					vars.posY,
					vars.posZ
				});
				vars.tct.game.Logger.Error(e2);
				return;
			}
			bool doNotRenderAtLod2 = vars.block.DoNotRenderAtLod2;
			if (vars.block.Lod0Shape != null)
			{
				if (JsonTesselator.NotSurrounded(vars, extIndex3d))
				{
					this.doMesh(vars, vars.block.Lod0Mesh, 0);
				}
				else
				{
					doNotRenderAtLod2 = true;
				}
			}
			if (totalLight == 4934475L)
			{
				this.doMesh(vars, lod1Mesh, 0);
				return;
			}
			if (vars.block.Lod2Mesh == null)
			{
				this.doMesh(vars, lod1Mesh, doNotRenderAtLod2 ? 2 : 1);
				return;
			}
			this.doMesh(vars, lod1Mesh, 2);
			this.doMesh(vars, vars.block.Lod2Mesh, 3);
		}

		public static bool NotSurrounded(TCTCache vars, int extIndex3d)
		{
			if (vars.block.FaceCullMode != EnumFaceCullMode.CollapseMaterial)
			{
				return true;
			}
			for (int tileSide = 0; tileSide < 5; tileSide++)
			{
				Block nblock = vars.tct.currentChunkBlocksExt[extIndex3d + TileSideEnum.MoveIndex[tileSide]];
				if (nblock.BlockMaterial != vars.block.BlockMaterial && !nblock.SideOpaque[TileSideEnum.GetOpposite(tileSide)])
				{
					return true;
				}
			}
			return false;
		}

		public void doMesh(TCTCache vars, MeshData sourceMesh, int lodLevel)
		{
			if (sourceMesh.VerticesCount == 0)
			{
				return;
			}
			if (sourceMesh == null)
			{
				vars.block.DrawType = EnumDrawType.Cube;
				return;
			}
			MeshData[] altModels = (((lodLevel + 1) / 2 == 1) ? vars.shapes.altblockModelDatasLod1[vars.blockId] : ((lodLevel == 0) ? vars.shapes.altblockModelDatasLod0[vars.blockId] : vars.shapes.altblockModelDatasLod2[vars.blockId]));
			if (altModels != null)
			{
				int rnd = GameMath.MurmurHash3Mod(vars.posX, (vars.block.RandomizeAxes == EnumRandomizeAxes.XYZ) ? vars.posY : 0, vars.posZ, altModels.Length);
				sourceMesh = altModels[rnd];
			}
			vars.block.OnJsonTesselation(ref sourceMesh, ref this.jsonLightRGB, this.pos, vars.tct.currentChunkBlocksExt, vars.extIndex3d);
			if (vars.preRotationMatrix != null)
			{
				this.AddJsonModelDataToMesh(sourceMesh, lodLevel, vars, this.helper, vars.preRotationMatrix);
				return;
			}
			if (vars.block.RandomizeRotations || vars.block.RandomSizeAdjust != 0f)
			{
				float[] matrix = (vars.block.RandomizeRotations ? TesselationMetaData.randomRotMatrices[GameMath.MurmurHash3Mod(-vars.posX, (vars.block.RandomizeAxes == EnumRandomizeAxes.XYZ) ? vars.posY : 0, vars.posZ, TesselationMetaData.randomRotations.Length)] : JsonTesselator.reusableIdentityMatrix);
				this.AddJsonModelDataToMesh(sourceMesh, lodLevel, vars, this.helper, matrix);
				return;
			}
			this.AddJsonModelDataToMesh(sourceMesh, lodLevel, vars, this.helper, null);
		}

		public void AddJsonModelDataToMesh(MeshData sourceMesh, int lodlevel, TCTCache vars, IMeshPoolSupplier poolSupplier, float[] tfMatrix)
		{
			if (sourceMesh.VerticesCount > this.windFlagsMask.Length)
			{
				this.windFlagsMask = new int[sourceMesh.VerticesCount];
				this.windFlagsSet = new int[sourceMesh.VerticesCount];
			}
			for (int i = 0; i < sourceMesh.VerticesCount; i++)
			{
				this.windFlagsMask[i] = -1;
				this.windFlagsSet[i] = 0;
			}
			Block fluidBlock = vars.tct.currentChunkFluidBlocksExt[vars.extIndex3d + vars.block.IceCheckOffset * 34 * 34];
			if (fluidBlock.BlockId != 0)
			{
				this.AdjustWindWaveForFluids(sourceMesh, fluidBlock);
			}
			float xOff = vars.finalX;
			float yOff = vars.finalY;
			float zOff = vars.finalZ;
			int baseRenderFlags = vars.VertexFlags;
			int drawFaceFlags = vars.drawFaceFlags;
			bool frostable = vars.block.Frostable;
			byte colorLightA = 0;
			byte colorLightB = 0;
			byte colorLightG = 0;
			byte colorLightR = 0;
			float aoBright = 1f;
			byte temperature = vars.ColorMapData.Temperature;
			byte rainfall = vars.ColorMapData.Rainfall;
			int prevRenderPass = -2;
			int prevTextureId = -1;
			MeshData targetMesh = null;
			int colorTopLeft = 0;
			int colorTopRight = 0;
			int colorBottomLeft = 0;
			int colorBottomRight = 0;
			float[] tmpCoords = this.tmpCoords;
			float[] sourceMeshXYZ = sourceMesh.xyz;
			float[] sourceMeshUV = sourceMesh.Uv;
			int[] sourceMeshFlags = sourceMesh.Flags;
			int[] sourceMeshIndices = sourceMesh.Indices;
			float[] targetMeshXYZ = null;
			float[] targetMeshUV = null;
			byte[] targetMeshRgba = null;
			int[] targetMeshFlags = null;
			CustomMeshDataPartInt targetMeshCustomInts = null;
			float[] rotatedXyz = sourceMesh.xyz;
			int[] rotatedFlags = sourceMesh.Flags;
			if (poolSupplier == null)
			{
				poolSupplier = vars.tct;
			}
			int vertPerFace = sourceMesh.VerticesPerFace;
			int indPerFace = sourceMesh.IndicesPerFace;
			bool allowSSBOs = vars.tct.game.api.renderapi.useSSBOs;
			if ((allowSSBOs && vertPerFace != 4) || indPerFace != 6)
			{
				string warning = "Model " + vars.block.Code.ToShortString() + " does not have 4 vertices and 6 indices per face, will break chunk shader optimizations. Try disabling clientsetting: allowSSBOs.";
				vars.tct.game.Logger.Warning(warning);
			}
			if (tfMatrix != null)
			{
				int sourceMeshArrayCount = sourceMesh.VerticesCount * 3;
				rotatedXyz = JsonTesselator.NewFloatArray(sourceMeshArrayCount);
				float randomScalefactor = vars.block.RandomSizeAdjust;
				bool doRandomYScale = false;
				if (randomScalefactor != 0f)
				{
					if (randomScalefactor < 0f)
					{
						randomScalefactor = -randomScalefactor;
					}
					else
					{
						doRandomYScale = true;
					}
					int elementHeight = (int)randomScalefactor;
					randomScalefactor = randomScalefactor % 1f * ((float)GameMath.MurmurHash3Mod(-vars.posX, 0, vars.posZ, 2000) / 1000f - 1f);
					yOff += (float)elementHeight * randomScalefactor;
				}
				for (int j = 0; j < sourceMeshArrayCount; j += 3)
				{
					if (randomScalefactor == 0f)
					{
						Mat4f.MulWithVec3_Position(tfMatrix, sourceMeshXYZ, rotatedXyz, j);
					}
					else if (doRandomYScale)
					{
						Mat4f.MulWithVec3_Position_AndScale(tfMatrix, sourceMeshXYZ, rotatedXyz, j, 1f + randomScalefactor);
					}
					else
					{
						Mat4f.MulWithVec3_Position_AndScaleXY(tfMatrix, sourceMeshXYZ, rotatedXyz, j, 1f + randomScalefactor);
					}
				}
				sourceMeshArrayCount = sourceMesh.FlagsCount;
				rotatedFlags = sourceMeshFlags;
				if (rotatedFlags != null && tfMatrix != JsonTesselator.reusableIdentityMatrix)
				{
					rotatedFlags = JsonTesselator.NewIntArray(sourceMeshArrayCount);
					for (int k = 0; k < sourceMeshArrayCount; k++)
					{
						int flagOrig = sourceMeshFlags[k];
						VertexFlags.UnpackNormal(flagOrig, this.floatpool);
						Mat4f.MulWithVec3(tfMatrix, this.floatpool, this.floatpool);
						float len = GameMath.RootSumOfSquares(this.floatpool[0], this.floatpool[1], this.floatpool[2]);
						rotatedFlags[k] = (flagOrig & -33546241) | VertexFlags.PackNormal((double)(this.floatpool[0] / len), (double)(this.floatpool[1] / len), (double)(this.floatpool[2] / len));
					}
				}
			}
			int sourceMeshXyzFacesCount = sourceMesh.XyzFacesCount;
			short[] renderPassesAndExtraBits = sourceMesh.RenderPassesAndExtraBits;
			bool haveRenderPasses = renderPassesAndExtraBits != null && renderPassesAndExtraBits.Length != 0;
			bool prevRotatedSource = tfMatrix == null;
			int l = 0;
			while (l < sourceMeshXyzFacesCount)
			{
				int textureid = sourceMesh.TextureIds[(int)sourceMesh.TextureIndices[l * vertPerFace / sourceMesh.VerticesPerFace]];
				float currentXOff = xOff;
				float currentZOff = zOff;
				bool rotatedSource = true;
				int renderpass;
				if (haveRenderPasses)
				{
					renderpass = (int)sourceMesh.RenderPassesAndExtraBits[l];
					if (renderpass >= 1024)
					{
						renderpass &= 1023;
						rotatedSource = false;
						currentXOff = (float)((int)(xOff + 0.5f));
						currentZOff = (float)((int)(zOff + 0.5f));
					}
				}
				else
				{
					renderpass = -1;
				}
				if (rotatedSource != prevRotatedSource)
				{
					prevRotatedSource = rotatedSource;
					sourceMeshXYZ = (rotatedSource ? rotatedXyz : sourceMesh.xyz);
					sourceMeshFlags = (rotatedSource ? rotatedFlags : sourceMesh.Flags);
				}
				int currentFace = (int)(sourceMesh.XyzFaces[l] - 1);
				if (currentFace < 0)
				{
					goto IL_0531;
				}
				if (tfMatrix != null && rotatedSource && tfMatrix != JsonTesselator.reusableIdentityMatrix)
				{
					currentFace = Mat4f.MulWithVec3_BlockFacing(tfMatrix, BlockFacing.ALLFACES[currentFace].Normalf).Index;
				}
				if (((1 << currentFace) & drawFaceFlags) != 0)
				{
					goto IL_0531;
				}
				int centerIndex = l * 4 * 3 + JsonTesselator.faceCoordLookup[currentFace];
				float centerCoord = sourceMeshXYZ[centerIndex];
				if (currentFace == 1 || currentFace == 2 || currentFace == 4)
				{
					centerCoord = 1f - centerCoord;
				}
				if (centerCoord > 0.01f)
				{
					goto IL_0531;
				}
				centerCoord = sourceMeshXYZ[centerIndex + 6];
				if (currentFace == 1 || currentFace == 2 || currentFace == 4)
				{
					centerCoord = 1f - centerCoord;
				}
				if (centerCoord > 0.01f)
				{
					goto IL_0531;
				}
				bool withinBlockBounds = true;
				for (int m = 0; m < 9; m++)
				{
					if (m == 3)
					{
						m += 3;
					}
					int ix = l * 4 * 3 + m;
					if (ix != centerIndex && ix != centerIndex + 6)
					{
						centerCoord = sourceMeshXYZ[ix];
						if (centerCoord < -0.0001f || centerCoord > 1.0001f)
						{
							withinBlockBounds = false;
							break;
						}
					}
				}
				if (!withinBlockBounds)
				{
					goto IL_0531;
				}
				IL_0A70:
				l++;
				continue;
				IL_0531:
				bool isLiquid = renderpass == 4;
				bool istopsoil = renderpass == 5;
				if (prevRenderPass != renderpass || prevTextureId != textureid)
				{
					targetMesh = poolSupplier.GetMeshPoolForPass(textureid, (EnumChunkRenderPass)((renderpass >= 0) ? renderpass : ((int)vars.RenderPass)), lodlevel);
					targetMeshXYZ = targetMesh.xyz;
					targetMeshUV = targetMesh.Uv;
					targetMeshRgba = targetMesh.Rgba;
					targetMeshCustomInts = targetMesh.CustomInts;
					targetMeshFlags = targetMesh.Flags;
				}
				prevRenderPass = renderpass;
				prevTextureId = textureid;
				int colorMapDataCalculated = ColorMapData.FromValues(sourceMesh.SeasonColorMapIds[l], sourceMesh.ClimateColorMapIds[l], temperature, rainfall, (sourceMesh.FrostableBits != null) ? sourceMesh.FrostableBits[l] : frostable, vars.block.ExtraColorBits);
				int[] axesByFacing = null;
				float textureVOffset = 0f;
				int[] jsonLightRGB = this.jsonLightRGB;
				if (currentFace < 0)
				{
					int num = jsonLightRGB[24];
					colorLightA = (byte)((float)(num & 255) * aoBright);
					colorLightB = (byte)(num >> 8);
					colorLightG = (byte)(num >> 16);
					colorLightR = (byte)(num >> 24);
					aoBright = 1f;
				}
				else
				{
					axesByFacing = JsonTesselator.axesByFacingLookup[currentFace];
					int[] indexes = JsonTesselator.indexesByFacingLookup[currentFace];
					int baseIndex = currentFace * 4;
					colorTopLeft = jsonLightRGB[baseIndex + indexes[0]];
					colorTopRight = jsonLightRGB[baseIndex + indexes[1]];
					colorBottomLeft = jsonLightRGB[baseIndex + indexes[2]];
					colorBottomRight = jsonLightRGB[baseIndex + indexes[3]];
					if (vars.textureVOffset != 0f && ((1 << currentFace) & vars.block.alternatingVOffsetFaces) == 0)
					{
						textureVOffset = vars.textureVOffset / ((float)ClientSettings.MaxTextureAtlasHeight / 32f);
					}
				}
				int sourceVertexNum = l * vertPerFace;
				int targetVertexNum = targetMesh.VerticesCount;
				int lastelement = targetVertexNum - l * vertPerFace;
				int targetVertexIndex = targetVertexNum * 3;
				int sourceVertexIndex = sourceVertexNum * 3;
				int targetUVIndex = targetVertexNum * 2;
				int sourceUVIndex = sourceVertexNum * 2;
				int targetRGBAIndex = targetVertexNum * 4;
				int n = vertPerFace;
				do
				{
					if (targetVertexNum >= targetMesh.VerticesMax)
					{
						targetMesh.VerticesCount = targetVertexNum;
						targetMesh.GrowVertexBuffer();
						targetMesh.GrowNormalsBuffer();
						targetMeshXYZ = targetMesh.xyz;
						targetMeshUV = targetMesh.Uv;
						targetMeshRgba = targetMesh.Rgba;
						targetMeshCustomInts = targetMesh.CustomInts;
						targetMeshFlags = targetMesh.Flags;
					}
					float xActual = (targetMeshXYZ[targetVertexIndex++] = (tmpCoords[0] = sourceMeshXYZ[sourceVertexIndex++]) + currentXOff);
					float yActual = (targetMeshXYZ[targetVertexIndex++] = (tmpCoords[1] = sourceMeshXYZ[sourceVertexIndex++]) + yOff);
					float zActual = (targetMeshXYZ[targetVertexIndex++] = (tmpCoords[2] = sourceMeshXYZ[sourceVertexIndex++]) + currentZOff);
					vars.UpdateChunkMinMax(xActual, yActual, zActual);
					targetMeshUV[targetUVIndex++] = sourceMeshUV[sourceUVIndex++];
					targetMeshUV[targetUVIndex++] = sourceMeshUV[sourceUVIndex++] + textureVOffset;
					float f = 1f;
					if (vars.block.DrawType == EnumDrawType.JSONAndWater && tmpCoords[1] < 1f)
					{
						f = Math.Max(0.6f, tmpCoords[1]) - 0.1f;
					}
					if (currentFace >= 0)
					{
						float num2 = GameMath.Clamp(tmpCoords[axesByFacing[0]], 0f, 1f);
						float dy = GameMath.Clamp(tmpCoords[axesByFacing[1]], 0f, 1f);
						int num3 = GameMath.BiLerpRgbaColor(num2, dy, colorTopLeft, colorTopRight, colorBottomLeft, colorBottomRight);
						colorLightA = (byte)(num3 & 255);
						colorLightB = (byte)(num3 >> 8);
						colorLightG = (byte)(num3 >> 16);
						colorLightR = (byte)((float)(num3 >> 24) * aoBright);
					}
					if (isLiquid)
					{
						if (sourceMesh.CustomFloats == null)
						{
							targetMeshCustomInts.Add(colorMapDataCalculated);
							targetMeshCustomInts.Add(268435456);
							targetMesh.CustomFloats.Add(0f);
							targetMesh.CustomFloats.Add(0f);
						}
						else
						{
							targetMeshCustomInts.Add(colorMapDataCalculated);
							targetMeshCustomInts.Add(sourceMesh.CustomInts.Values[sourceVertexNum]);
							targetMesh.CustomFloats.Add(sourceMesh.CustomFloats.Values[2 * sourceVertexNum]);
							targetMesh.CustomFloats.Add(sourceMesh.CustomFloats.Values[2 * sourceVertexNum + 1]);
						}
					}
					else
					{
						targetMeshCustomInts.Add(colorMapDataCalculated);
						if (istopsoil)
						{
							targetMesh.CustomShorts.Add(sourceMesh.CustomShorts.Values[2 * sourceVertexNum]);
							targetMesh.CustomShorts.Add(sourceMesh.CustomShorts.Values[2 * sourceVertexNum + 1]);
						}
					}
					targetMeshRgba[targetRGBAIndex++] = (byte)((float)colorLightA * f);
					targetMeshRgba[targetRGBAIndex++] = (byte)((float)colorLightB * f);
					targetMeshRgba[targetRGBAIndex++] = (byte)((float)colorLightG * f);
					targetMeshRgba[targetRGBAIndex++] = (byte)((float)colorLightR * f);
					targetMeshFlags[targetVertexNum++] = (baseRenderFlags | sourceMeshFlags[sourceVertexNum] | this.windFlagsSet[sourceVertexNum]) & this.windFlagsMask[sourceVertexNum];
					sourceVertexNum++;
				}
				while (--n > 0);
				targetMesh.VerticesCount = targetVertexNum;
				if (indPerFace == 6)
				{
					int indexNum = l * indPerFace;
					targetMesh.AddIndices(allowSSBOs, lastelement + sourceMeshIndices[indexNum++], lastelement + sourceMeshIndices[indexNum++], lastelement + sourceMeshIndices[indexNum++], lastelement + sourceMeshIndices[indexNum++], lastelement + sourceMeshIndices[indexNum++], lastelement + sourceMeshIndices[indexNum]);
					goto IL_0A70;
				}
				int indexNum2 = l * indPerFace;
				for (int l2 = 0; l2 < indPerFace; l2++)
				{
					targetMesh.AddIndex(lastelement + sourceMeshIndices[indexNum2++]);
				}
				goto IL_0A70;
			}
		}

		private void AdjustWindWaveForFluids(MeshData sourceMesh, Block fluidBlock)
		{
			int clearFlags = -503316481;
			int verticesCount = sourceMesh.VerticesCount;
			int[] windFlagsMask = this.windFlagsMask;
			if (fluidBlock.SideSolid.Any)
			{
				for (int vertexNum = 0; vertexNum < verticesCount; vertexNum++)
				{
					windFlagsMask[vertexNum] = clearFlags;
				}
				return;
			}
			int[] windFlagsSet = this.windFlagsSet;
			int[] sourceMeshFlags = sourceMesh.Flags;
			for (int vertexNum2 = 0; vertexNum2 < verticesCount; vertexNum2++)
			{
				int flags = sourceMeshFlags[vertexNum2] & 503316480;
				if (flags == 33554432 || flags == 100663296)
				{
					windFlagsSet[vertexNum2] = 738197504;
					windFlagsMask[vertexNum2] = -1073741825;
				}
			}
		}

		private static float[] NewFloatArray(int size)
		{
			if (JsonTesselator.reusableFloatArray == null || JsonTesselator.reusableFloatArray.Length < size)
			{
				JsonTesselator.reusableFloatArray = new float[size];
			}
			return JsonTesselator.reusableFloatArray;
		}

		private static int[] NewIntArray(int size)
		{
			if (JsonTesselator.reusableIntArray == null || JsonTesselator.reusableIntArray.Length < size)
			{
				JsonTesselator.reusableIntArray = new int[size];
			}
			return JsonTesselator.reusableIntArray;
		}

		public const int DisableRandomsFlag = 1024;

		public const long Darkness = 4934475L;

		public TerrainMesherHelper helper = new TerrainMesherHelper();

		private BlockPos pos = new BlockPos();

		private int[] jsonLightRGB = new int[25];

		public static float[] reusableIdentityMatrix = Mat4f.Create();

		private float[] floatpool = new float[3];

		private int[] windFlagsMask = new int[64];

		private int[] windFlagsSet = new int[64];

		private static int[] faceCoordLookup = new int[] { 2, 0, 2, 0, 1, 1 };

		private float[] tmpCoords = new float[3];

		private static int[][] axesByFacingLookup = new int[][]
		{
			new int[] { 0, 1 },
			new int[] { 1, 2 },
			new int[] { 0, 1 },
			new int[] { 1, 2 },
			new int[] { 0, 2 },
			new int[] { 0, 2 }
		};

		private static int[][] indexesByFacingLookup = new int[][]
		{
			new int[] { 3, 2, 1, 0 },
			new int[] { 3, 1, 2, 0 },
			new int[] { 2, 3, 0, 1 },
			new int[] { 2, 0, 3, 1 },
			new int[] { 3, 2, 1, 0 },
			new int[] { 1, 0, 3, 2 }
		};

		[ThreadStatic]
		private static float[] reusableFloatArray;

		[ThreadStatic]
		private static int[] reusableIntArray;
	}
}
