using System;
using Vintagestory.API.Client;

namespace Vintagestory.Client.NoObf
{
	public class TerrainMesherHelper : ITerrainMeshPool, IMeshPoolSupplier
	{
		public void AddMeshData(MeshData sourceMesh, float[] transformationMatrix, int lodLevel = 1)
		{
			if (sourceMesh == null)
			{
				return;
			}
			this.tess.AddJsonModelDataToMesh(sourceMesh, lodLevel, this.vars, this, transformationMatrix);
		}

		public void AddMeshData(MeshData sourceMesh, int lodLevel = 1)
		{
			if (sourceMesh == null)
			{
				return;
			}
			this.tess.AddJsonModelDataToMesh(sourceMesh, lodLevel, this.vars, this, null);
		}

		public void AddMeshData(MeshData sourceMesh, ColorMapData colorMapData, int lodlevel = 1)
		{
			this.vars.ColorMapData = colorMapData;
			this.AddMeshData(sourceMesh, lodlevel);
		}

		public MeshData GetMeshPoolForPass(int textureId, EnumChunkRenderPass forRenderPass, int lodLevel)
		{
			return this.vars.tct.GetMeshPoolForPass(textureId, forRenderPass, lodLevel);
		}

		internal TCTCache vars;

		internal JsonTesselator tess;
	}
}
