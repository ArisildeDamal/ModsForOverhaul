using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Vintagestory.Client.NoObf
{
	public class ObjTesselator
	{
		public void Load(IAsset asset, out MeshData mesh, TextureAtlasPosition pos, TesselationMetaData meta, short renderPass)
		{
			mesh = new MeshData(24, 36, false, true, true, true).WithColorMaps().WithRenderpasses();
			if (meta.WithJointIds)
			{
				mesh.CustomInts = new CustomMeshDataPartInt();
				mesh.CustomInts.InterleaveSizes = new int[] { 1 };
				mesh.CustomInts.InterleaveOffsets = new int[1];
				mesh.CustomInts.InterleaveStride = 0;
			}
			else
			{
				mesh.CustomInts = null;
			}
			if (meta.WithDamageEffect)
			{
				mesh.CustomFloats = new CustomMeshDataPartFloat();
				mesh.CustomFloats.InterleaveSizes = new int[] { 1 };
				mesh.CustomFloats.InterleaveOffsets = new int[1];
				mesh.CustomFloats.InterleaveStride = 0;
			}
			this.faceVertices.Clear();
			this.temp_vertices.Clear();
			this.temp_uvs.Clear();
			this.temp_normals.Clear();
			this.facesCount = 0;
			using (MemoryStream ms = new MemoryStream(asset.Data))
			{
				using (StreamReader reader = new StreamReader(ms))
				{
					while (!reader.EndOfStream)
					{
						this.parseLine(reader);
					}
				}
			}
			mesh = new MeshData(this.faceVertices.Count, this.faceVertices.Count, false, true, true, true);
			mesh.WithXyzFaces();
			mesh.WithColorMaps();
			mesh.IndicesPerFace = 3;
			mesh.VerticesPerFace = 3;
			float uwdt = pos.x2 - pos.x1;
			float uhgt = pos.y2 - pos.y1;
			for (int i = 0; i < this.facesCount; i++)
			{
				mesh.AddXyzFace(0);
				mesh.AddTextureId(pos.atlasTextureId);
			}
			mesh.xyz = this.temp_vertices.ToArray();
			mesh.Rgba.Fill(byte.MaxValue);
			for (int vertIndex = 0; vertIndex < this.faceVertices.Count; vertIndex++)
			{
				ObjFaceVertex face = this.faceVertices[vertIndex];
				float normalx = this.temp_normals[3 * face.NormalIndex];
				float normaly = this.temp_normals[3 * face.NormalIndex + 1];
				float normalz = this.temp_normals[3 * face.NormalIndex + 2];
				mesh.Flags[face.VertexIndex] = VertexFlags.PackNormal((double)normalx, (double)normaly, (double)normalz) + meta.GeneralGlowLevel;
				mesh.Uv[2 * face.VertexIndex] = pos.x1 + this.temp_uvs[2 * face.UvIndex] * uwdt;
				mesh.Uv[2 * face.VertexIndex + 1] = pos.y1 + this.temp_uvs[2 * face.UvIndex + 1] * uhgt;
				mesh.AddIndex(face.VertexIndex);
				if (mesh.ClimateColorMapIds != null)
				{
					mesh.AddColorMapIndex(meta.ClimateColorMapId, meta.SeasonColorMapId);
				}
				if (mesh.RenderPassesAndExtraBits != null)
				{
					mesh.AddRenderPass(renderPass);
				}
			}
			mesh.VerticesCount = this.faceVertices.Count;
		}

		private void parseLine(StreamReader reader)
		{
			string currentLine = reader.ReadLine();
			if (string.IsNullOrEmpty(currentLine) || currentLine[0] == '#')
			{
				return;
			}
			string[] array = currentLine.Split(new char[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
			string keyword = array[0];
			string[] parts = array[1].Split(' ', StringSplitOptions.None);
			if (keyword == "f")
			{
				this.parseFace(parts);
				return;
			}
			if (keyword == "vn")
			{
				this.parseNormal(parts);
				return;
			}
			if (keyword == "vt")
			{
				this.parseTextureUV(parts);
				return;
			}
			if (!(keyword == "v"))
			{
				return;
			}
			this.parseVertex(parts);
		}

		private void parseFace(string[] parts)
		{
			if (parts.Length != 3)
			{
				throw new FormatException("Cannot read .obj file. The f section needs to contain 9 values");
			}
			this.facesCount++;
			for (int i = 0; i < 3; i++)
			{
				string[] fields = parts[i].Split(new char[] { '/' }, StringSplitOptions.None);
				this.faceVertices.Add(new ObjFaceVertex
				{
					VertexIndex = fields[0].ToInt(0) - 1,
					UvIndex = fields[1].ToInt(0) - 1,
					NormalIndex = fields[2].ToInt(0) - 1
				});
			}
		}

		private void parseNormal(string[] parts)
		{
			this.temp_normals.Add(parts[0].ToFloat(0f));
			this.temp_normals.Add(parts[1].ToFloat(0f));
			this.temp_normals.Add(parts[2].ToFloat(0f));
		}

		private void parseTextureUV(string[] parts)
		{
			this.temp_uvs.Add(parts[0].ToFloat(0f));
			this.temp_uvs.Add(parts[1].ToFloat(0f));
		}

		private void parseVertex(string[] parts)
		{
			this.temp_vertices.Add(parts[0].ToFloat(0f) + 0.5f);
			this.temp_vertices.Add(parts[1].ToFloat(0f) + 0.5f);
			this.temp_vertices.Add(parts[2].ToFloat(0f) + 0.5f);
		}

		private List<ObjFaceVertex> faceVertices = new List<ObjFaceVertex>();

		private List<float> temp_vertices = new List<float>();

		private List<float> temp_uvs = new List<float>();

		private List<float> temp_normals = new List<float>();

		private int facesCount;
	}
}
