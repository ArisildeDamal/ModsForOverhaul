using System;
using Vintagestory.API.Client;

namespace Vintagestory.Client.NoObf
{
	public class QuadMeshUtilExt
	{
		public static MeshData GetQuadModelData()
		{
			MeshData i = new MeshData(4, 6, false, true, false, false);
			for (int j = 0; j < 4; j++)
			{
				i.AddVertex((float)QuadMeshUtilExt.quadVertices[j * 3], (float)QuadMeshUtilExt.quadVertices[j * 3 + 1], (float)QuadMeshUtilExt.quadVertices[j * 3 + 2], (float)QuadMeshUtilExt.quadTextureCoords[j * 2], (float)QuadMeshUtilExt.quadTextureCoords[j * 2 + 1]);
			}
			for (int k = 0; k < 6; k++)
			{
				i.AddIndex(QuadMeshUtilExt.quadVertexIndices[k]);
			}
			return i;
		}

		public static MeshData GetCustomQuadModelData(float x, float y, float z, float dw, float dh, byte r, byte g, byte b, byte a, int textureId = 0)
		{
			MeshData i = new MeshData(4, 6, false, true, true, false);
			for (int j = 0; j < 4; j++)
			{
				i.AddVertex(x + ((QuadMeshUtilExt.quadVertices[j * 3] > 0) ? dw : 0f), y + ((QuadMeshUtilExt.quadVertices[j * 3 + 1] > 0) ? dh : 0f), z, (float)QuadMeshUtilExt.quadTextureCoords[j * 2], (float)QuadMeshUtilExt.quadTextureCoords[j * 2 + 1], new byte[] { r, g, b, a });
			}
			i.AddTextureId(textureId);
			for (int k = 0; k < 6; k++)
			{
				i.AddIndex(QuadMeshUtilExt.quadVertexIndices[k]);
			}
			return i;
		}

		public static MeshData GetCustomQuadModelDataHorizontal(float x, float y, float z, float dw, float dl, byte r, byte g, byte b, byte a)
		{
			MeshData i = new MeshData(4, 6, false, true, true, false);
			for (int j = 0; j < 4; j++)
			{
				i.AddVertex(x + ((QuadMeshUtilExt.quadVertices[j * 3] > 0) ? dw : 0f), y + 0f, z + ((QuadMeshUtilExt.quadVertices[j * 3 + 2] > 0) ? dl : 0f), (float)QuadMeshUtilExt.quadTextureCoords[j * 2], (float)QuadMeshUtilExt.quadTextureCoords[j * 2 + 1], new byte[] { r, g, b, a });
			}
			for (int k = 0; k < 6; k++)
			{
				i.AddIndex(QuadMeshUtilExt.quadVertexIndices[k]);
			}
			return i;
		}

		public static MeshData GetCustomQuadModelData(float u, float v, float uWidth, float vHeight, float x, float y, float dw, float dh, byte r, byte g, byte b, byte a)
		{
			MeshData i = new MeshData(4, 6, false, true, true, false);
			for (int j = 0; j < 4; j++)
			{
				i.AddVertex(x + ((QuadMeshUtilExt.quadVertices[j * 3] > 0) ? dw : 0f), y + ((QuadMeshUtilExt.quadVertices[j * 3 + 1] > 0) ? dh : 0f), 0f, (float)QuadMeshUtilExt.quadTextureCoords[j * 2], (float)QuadMeshUtilExt.quadTextureCoords[j * 2 + 1], new byte[] { r, g, b, a });
			}
			for (int k = 0; k < 6; k++)
			{
				i.AddIndex(QuadMeshUtilExt.quadVertexIndices[k]);
			}
			i.Uv[0] = u;
			i.Uv[1] = v;
			i.Uv[2] = u + uWidth;
			i.Uv[3] = v;
			i.Uv[4] = u + uWidth;
			i.Uv[5] = v + vHeight;
			i.Uv[6] = u;
			i.Uv[7] = v + vHeight;
			return i;
		}

		private static int[] quadVertices = new int[]
		{
			-1, -1, 0, 1, -1, 0, 1, 1, 0, -1,
			1, 0
		};

		private static int[] quadTextureCoords = new int[] { 0, 0, 1, 0, 1, 1, 0, 1 };

		private static int[] quadVertexIndices = new int[] { 0, 1, 2, 0, 2, 3 };
	}
}
