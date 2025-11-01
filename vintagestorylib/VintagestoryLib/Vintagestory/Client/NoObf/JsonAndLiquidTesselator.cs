using System;
using Vintagestory.API.Client;

namespace Vintagestory.Client.NoObf
{
	public class JsonAndLiquidTesselator : IBlockTesselator
	{
		public JsonAndLiquidTesselator(ChunkTesselator tct)
		{
			this.liquid = new LiquidTesselator(tct);
			this.json = new JsonTesselator();
		}

		public void Tesselate(TCTCache vars)
		{
			float saveRandomOffetX = vars.finalX;
			float saveRandomOffetZ = vars.finalZ;
			vars.finalX = (float)vars.lx;
			vars.finalZ = (float)vars.lz;
			vars.RenderPass = EnumChunkRenderPass.Liquid;
			byte waterMapIndex = (byte)(vars.tct.game.ColorMaps.IndexOfKey("climateWaterTint") + 1);
			ColorMapData prevColorMapData = vars.ColorMapData;
			int prevFlags = vars.VertexFlags;
			vars.ColorMapData = new ColorMapData(0, waterMapIndex, prevColorMapData.Temperature, prevColorMapData.Rainfall, false);
			vars.VertexFlags = 0;
			vars.ColorMapData = prevColorMapData;
			vars.VertexFlags = prevFlags;
			vars.RenderPass = EnumChunkRenderPass.OpaqueNoCull;
			vars.finalX = saveRandomOffetX;
			vars.finalZ = saveRandomOffetZ;
			vars.drawFaceFlags = 255;
			this.json.Tesselate(vars);
		}

		private IBlockTesselator liquid;

		private IBlockTesselator json;
	}
}
