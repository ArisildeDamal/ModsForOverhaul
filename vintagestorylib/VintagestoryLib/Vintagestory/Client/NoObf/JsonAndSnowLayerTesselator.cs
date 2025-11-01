using System;
using Vintagestory.API.Client;
using Vintagestory.API.Client.Tesselation;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class JsonAndSnowLayerTesselator : IBlockTesselator
	{
		public JsonAndSnowLayerTesselator()
		{
			this.json = new JsonTesselator();
		}

		public void Tesselate(TCTCache vars)
		{
			if (vars.tct.currentChunkBlocksExt[vars.extIndex3d + TileSideEnum.MoveIndex[5]].AllowSnowCoverage(vars.tct.game, this.tmpPos.Set(vars.posX, vars.posY - 1, vars.posZ)))
			{
				float saveRandomOffetX = vars.finalX;
				float saveRandomOffetZ = vars.finalZ;
				vars.finalX = (float)vars.lx;
				vars.finalZ = (float)vars.lz;
				int snowLayerVertexFlags = vars.VertexFlags & 33554431 & -1793;
				MeshData[] meshPools = vars.tct.GetPoolForPass(EnumChunkRenderPass.Opaque, 1);
				TextureAtlasPosition snowTexture = vars.textureAtlasPositionsByTextureSubId[vars.fastBlockTextureSubidsByFace[6]];
				for (int tileSide = 0; tileSide < 6; tileSide++)
				{
					if ((vars.drawFaceFlags & TileSideEnum.ToFlags(tileSide)) != 0)
					{
						vars.CalcBlockFaceLight(tileSide, vars.extIndex3d + TileSideEnum.MoveIndex[tileSide]);
						CubeTesselator.DrawBlockFace(vars, tileSide, vars.blockFaceVertices[tileSide], snowTexture, snowLayerVertexFlags | BlockFacing.ALLFACES[tileSide].NormalPackedFlags, 0, meshPools, 0.125f);
					}
				}
				vars.finalX = saveRandomOffetX;
				vars.finalZ = saveRandomOffetZ;
			}
			this.json.Tesselate(vars);
		}

		private IBlockTesselator json;

		private BlockPos tmpPos = new BlockPos();
	}
}
