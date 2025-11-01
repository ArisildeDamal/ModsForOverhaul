using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf
{
	public class GltfTesselator
	{
		public void Load(GltfType asset, out MeshData mesh, TextureAtlasPosition pos, int generalGlowLevel, byte climateColorMapIndex, byte seasonColorMapIndex, short renderPass, out byte[][][] bakedTextures)
		{
			this.meshPieces.Clear();
			this.capacities = new int[2];
			this.ParseGltf(asset, pos, generalGlowLevel, climateColorMapIndex, seasonColorMapIndex, renderPass, out bakedTextures);
			mesh = new MeshData(this.capacities[0] + 32, this.capacities[1] + 32, false, true, true, true).WithXyzFaces().WithRenderpasses().WithColorMaps();
			mesh.IndicesPerFace = 3;
			mesh.VerticesPerFace = 3;
			mesh.CustomFloats = new CustomMeshDataPartFloat
			{
				Values = new float[this.meshPieces.Count * 5],
				InterleaveSizes = new int[] { 3, 2 },
				InterleaveStride = 20,
				InterleaveOffsets = new int[] { 0, 12 },
				Count = 5
			};
			mesh.CustomInts = new CustomMeshDataPartInt
			{
				Values = new int[this.meshPieces.Count],
				InterleaveSizes = new int[this.meshPieces.Count].Fill(1),
				InterleaveStride = 4,
				InterleaveOffsets = new int[this.meshPieces.Count],
				Count = 0
			};
			for (int i = 0; i < mesh.CustomInts.Values.Length; i++)
			{
				mesh.CustomInts.InterleaveOffsets[i] = 4 * i;
			}
			for (int j = 0; j < this.meshPieces.Count; j++)
			{
				MeshData piece = this.meshPieces[j];
				mesh.CustomFloats.Values[j * 5] = piece.CustomFloats.Values[0];
				mesh.CustomFloats.Values[j * 5 + 1] = piece.CustomFloats.Values[1];
				mesh.CustomFloats.Values[j * 5 + 2] = piece.CustomFloats.Values[2];
				mesh.CustomFloats.Values[j * 5 + 3] = piece.CustomFloats.Values[3];
				mesh.CustomFloats.Values[j * 5 + 4] = piece.CustomFloats.Values[4];
				mesh.AddMeshData(piece);
			}
		}

		public void ParseGltf(GltfType gltf, TextureAtlasPosition pos, int generalGlowLevel, byte climateColorMapIndex, byte seasonColorMapIndex, short renderPass, out byte[][][] bakedTextures)
		{
			GltfBuffer[] buffers = gltf.Buffers;
			GltfBufferView[] bufferViews = gltf.BufferViews;
			GltfMaterial[] materials2 = gltf.Materials;
			int? materials = ((materials2 != null) ? new int?(materials2.Length) : null);
			bakedTextures = ((materials != null) ? new byte[materials.Value][][] : null);
			long matIndex = 0L;
			checked
			{
				foreach (long node in gltf.Scenes[(int)((IntPtr)gltf.Scene)].Nodes)
				{
					GltfNode gltfNode = gltf.Nodes[(int)((IntPtr)node)];
					foreach (GltfPrimitive primitive in gltf.Meshes[(int)((IntPtr)gltf.Nodes[(int)((IntPtr)node)].Mesh)].Primitives)
					{
						Dictionary<string, long> accvalues = new Dictionary<string, long>();
						Dictionary<string, byte[]> buffdat = new Dictionary<string, byte[]>();
						float[] colorFactor = new float[] { 1f, 1f, 1f };
						float[] pbrFactor = new float[] { 0f, 1f };
						long? vtIndex = primitive.Attributes.Position;
						long? uvIndex = primitive.Attributes.Texcoord0;
						long? nmIndex = primitive.Attributes.Normal;
						long? vcIndex = primitive.Attributes.VertexColor;
						long? vgIndex = primitive.Attributes.GlowLevel;
						long? vrIndex = primitive.Attributes.Reflective;
						long? bmwlIndex = primitive.Attributes.BMWindLeaves;
						long? bmwlwbIndex = primitive.Attributes.BMWindLeavesWeakBend;
						long? bmwnIndex = primitive.Attributes.BMWindNormal;
						long? bmwwIndex = primitive.Attributes.BMWindWater;
						long? bmwwbIndex = primitive.Attributes.BMWindWeakBend;
						long? bmwwwIndex = primitive.Attributes.BMWindWeakWind;
						long? idIndex = primitive.Indices;
						long? mtIndex = primitive.Material;
						if (vtIndex != null)
						{
							accvalues.Add("vtx", vtIndex.Value);
						}
						if (uvIndex != null)
						{
							accvalues.Add("uvs", uvIndex.Value);
						}
						if (vcIndex != null)
						{
							accvalues.Add("vtc", vcIndex.Value);
						}
						if (vgIndex != null)
						{
							accvalues.Add("vtg", vgIndex.Value);
						}
						if (vrIndex != null)
						{
							accvalues.Add("vtr", vrIndex.Value);
						}
						if (bmwlIndex != null)
						{
							accvalues.Add("wa", bmwlIndex.Value);
						}
						if (bmwlwbIndex != null)
						{
							accvalues.Add("wb", bmwlwbIndex.Value);
						}
						if (bmwnIndex != null)
						{
							accvalues.Add("wc", bmwnIndex.Value);
						}
						if (bmwwIndex != null)
						{
							accvalues.Add("wd", bmwwIndex.Value);
						}
						if (bmwwbIndex != null)
						{
							accvalues.Add("we", bmwwbIndex.Value);
						}
						if (bmwwwIndex != null)
						{
							accvalues.Add("wf", bmwwwIndex.Value);
						}
						if (nmIndex != null)
						{
							accvalues.Add("nrm", nmIndex.Value);
						}
						if (idIndex != null)
						{
							accvalues.Add("ind", idIndex.Value);
						}
						if (mtIndex != null)
						{
							accvalues.Add("mat", mtIndex.Value);
						}
						GltfMaterial[] materials3 = gltf.Materials;
						GltfMaterial mat = ((materials3 != null) ? materials3[(int)((IntPtr)mtIndex.Value)] : null);
						if (mat != null)
						{
							new Dictionary<string, long>();
							if (((mat != null) ? mat.PbrMetallicRoughness : null) != null)
							{
								GltfPbrMetallicRoughness pbr = mat.PbrMetallicRoughness;
								colorFactor = mat.PbrMetallicRoughness.BaseColorFactor ?? colorFactor;
								pbrFactor = mat.PbrMetallicRoughness.PbrFactor ?? pbrFactor;
								GltfMatTexture baseColorTexture = pbr.BaseColorTexture;
								bool flag;
								if (baseColorTexture == null)
								{
									flag = false;
								}
								else
								{
									long index = baseColorTexture.Index;
									flag = true;
								}
								if (flag)
								{
									accvalues.Add("bcr", gltf.Images[(int)((IntPtr)pbr.BaseColorTexture.Index)].BufferView);
								}
								GltfMatTexture metallicRoughnessTexture = pbr.MetallicRoughnessTexture;
								bool flag2;
								if (metallicRoughnessTexture == null)
								{
									flag2 = false;
								}
								else
								{
									long index2 = metallicRoughnessTexture.Index;
									flag2 = true;
								}
								if (flag2)
								{
									accvalues.Add("pbr", gltf.Images[(int)((IntPtr)pbr.MetallicRoughnessTexture.Index)].BufferView);
								}
							}
							if (mat != null)
							{
								GltfMatTexture normalTexture = mat.NormalTexture;
								bool flag3;
								if (normalTexture == null)
								{
									flag3 = false;
								}
								else
								{
									long index3 = normalTexture.Index;
									flag3 = true;
								}
								if (flag3)
								{
									accvalues.Add("ntx", gltf.Images[(int)((IntPtr)mat.NormalTexture.Index)].BufferView);
								}
							}
						}
						foreach (KeyValuePair<string, long> dict in accvalues)
						{
							GltfBufferView bufferview = bufferViews[(int)((IntPtr)dict.Value)];
							GltfBuffer gltfBuffer = buffers[(int)((IntPtr)bufferview.Buffer)];
							byte[] bytes;
							if (!buffdat.TryGetValue(dict.Key, out bytes))
							{
								buffdat.Add(dict.Key, new byte[bufferview.ByteLength]);
							}
							bytes = buffdat[dict.Key];
							byte[] bufferdat = Convert.FromBase64String(gltfBuffer.Uri.Replace("data:application/octet-stream;base64,", "")).Copy(bufferview.ByteOffset, bufferview.ByteLength);
							unchecked
							{
								for (int i = 0; i < bufferdat.Length; i++)
								{
									bytes[i] = bufferdat[i];
								}
							}
						}
						byte[] vtBytes;
						if (buffdat.TryGetValue("vtx", out vtBytes))
						{
							this.temp_vertices.AddRange(vtBytes.ToVec3fs());
						}
						byte[] uvBytes;
						if (buffdat.TryGetValue("uvs", out uvBytes))
						{
							this.temp_uvs.AddRange(uvBytes.ToFloats());
						}
						unchecked
						{
							byte[] nmBytes;
							if (buffdat.TryGetValue("nrm", out nmBytes))
							{
								foreach (Vec3f val in nmBytes.ToVec3fs())
								{
									this.temp_normals.Add(VertexFlags.PackNormal(val) + generalGlowLevel);
								}
							}
							byte[] idBytes;
							if (buffdat.TryGetValue("ind", out idBytes))
							{
								this.temp_indices.AddRange(idBytes.ToUShorts().ToInts());
							}
							byte[] mtBytes;
							if (buffdat.TryGetValue("mat", out mtBytes))
							{
								this.temp_material.AddRange(mtBytes.ToVec3fs());
							}
							byte[] vcBytes;
							if (buffdat.TryGetValue("vtc", out vcBytes))
							{
								this.temp_vertexcolor.AddRange(vcBytes.ToVec4uss());
							}
							byte[] datBytes;
							buffdat.TryGetValue("vtg", out datBytes);
							ulong[] vgLongs = ((datBytes != null) ? datBytes.BytesToULongs() : null);
							buffdat.TryGetValue("vtr", out datBytes);
							ulong[] vrLongs = ((datBytes != null) ? datBytes.BytesToULongs() : null);
							buffdat.TryGetValue("wa", out datBytes);
							ulong[] waLongs = ((datBytes != null) ? datBytes.BytesToULongs() : null);
							buffdat.TryGetValue("wb", out datBytes);
							ulong[] wbLongs = ((datBytes != null) ? datBytes.BytesToULongs() : null);
							buffdat.TryGetValue("wc", out datBytes);
							ulong[] wcLongs = ((datBytes != null) ? datBytes.BytesToULongs() : null);
							buffdat.TryGetValue("wd", out datBytes);
							ulong[] wdLongs = ((datBytes != null) ? datBytes.BytesToULongs() : null);
							buffdat.TryGetValue("we", out datBytes);
							ulong[] weLongs = ((datBytes != null) ? datBytes.BytesToULongs() : null);
							buffdat.TryGetValue("wf", out datBytes);
							ulong[] wfLongs = ((datBytes != null) ? datBytes.BytesToULongs() : null);
							for (int j = 0; j < this.temp_vertices.Count; j++)
							{
								this.tempFlag.All = 0;
								this.tempFlag.GlowLevel = ((vgLongs != null && vgLongs[j] > 0UL) ? ((byte)((vgLongs[j] >> 16) / 281474976710655.0 * 255.0)) : 0);
								this.tempFlag.Reflective = ((vrLongs != null) ? vrLongs[j] : 0UL) >> 16 > 0UL;
								this.tempFlag.WindMode = ((((waLongs != null) ? waLongs[j] : 0UL) >> 16 > 0UL) ? EnumWindBitMode.Leaves : ((((wbLongs != null) ? wbLongs[j] : 0UL) >> 16 > 0UL) ? EnumWindBitMode.TallBend : ((((wcLongs != null) ? wcLongs[j] : 0UL) >> 16 > 0UL) ? EnumWindBitMode.NormalWind : ((((wdLongs != null) ? wdLongs[j] : 0UL) >> 16 > 0UL) ? EnumWindBitMode.Water : ((((weLongs != null) ? weLongs[j] : 0UL) >> 16 > 0UL) ? EnumWindBitMode.Bend : ((((wfLongs != null) ? wfLongs[j] : 0UL) >> 16 > 0UL) ? EnumWindBitMode.WeakWind : EnumWindBitMode.NoWind))))));
								this.temp_flags.Add(this.tempFlag.All);
							}
							if (bakedTextures != null)
							{
								byte[] clrtex = (buffdat.ContainsKey("bcr") ? buffdat["bcr"] : null);
								byte[] pbrtex = (buffdat.ContainsKey("pbr") ? buffdat["pbr"] : null);
								byte[] nrmtex = (buffdat.ContainsKey("pbr") ? buffdat["pbr"] : null);
								bakedTextures[(int)(checked((IntPtr)matIndex))] = new byte[][] { clrtex, pbrtex, nrmtex };
							}
							matIndex += 1L;
							this.BuildMeshDataPart(gltfNode, pos, climateColorMapIndex, seasonColorMapIndex, renderPass, colorFactor, pbrFactor);
						}
					}
				}
			}
		}

		public void BuildMeshDataPart(GltfNode node, TextureAtlasPosition pos, byte climateColorMapIndex, byte seasonColorMapIndex, short renderPass, float[] colorFactor, float[] pbrFactor)
		{
			MeshData meshPiece = new MeshData(this.temp_vertices.Count, this.temp_vertices.Count, false, true, true, true);
			meshPiece.WithXyzFaces();
			meshPiece.WithRenderpasses();
			meshPiece.WithColorMaps();
			meshPiece.IndicesPerFace = 3;
			meshPiece.VerticesPerFace = 3;
			meshPiece.Rgba.Fill(byte.MaxValue);
			this.capacities[0] += this.temp_vertices.Count * 3;
			meshPiece.Flags = new int[this.temp_vertices.Count];
			for (int i = 0; i < this.temp_vertices.Count; i++)
			{
				meshPiece.Flags[i] = this.temp_flags[i];
				meshPiece.Flags[i] |= this.temp_normals[i];
				if (this.temp_vertexcolor.Count > 0)
				{
					Vec4us col = this.temp_vertexcolor[i];
					int intCol = ((int)((byte)((float)col.W / 65535f * 255f)) << 24) | ((int)((byte)((float)col.X / 65535f * 255f)) << 16) | ((int)((byte)((float)col.Y / 65535f * 255f)) << 8) | (int)((byte)((float)col.Z / 65535f * 255f));
					meshPiece.AddVertexSkipTex(this.temp_vertices[i].X + 0.5f, this.temp_vertices[i].Y + 0.5f, this.temp_vertices[i].Z + 0.5f, intCol);
				}
				else
				{
					meshPiece.AddVertexSkipTex(this.temp_vertices[i].X + 0.5f, this.temp_vertices[i].Y + 0.5f, this.temp_vertices[i].Z + 0.5f, -1);
				}
			}
			for (int j = 0; j < this.temp_indices.Count / 3; j++)
			{
				meshPiece.AddXyzFace(0);
				meshPiece.AddTextureId(pos.atlasTextureId);
				if (meshPiece.ClimateColorMapIds != null)
				{
					meshPiece.AddColorMapIndex(climateColorMapIndex, seasonColorMapIndex);
				}
				if (meshPiece.RenderPassesAndExtraBits != null)
				{
					meshPiece.AddRenderPass(renderPass);
				}
			}
			meshPiece.Uv = this.temp_uvs.ToArray();
			meshPiece.AddIndices(this.temp_indices.ToArray());
			this.capacities[1] += this.temp_indices.Count;
			meshPiece.VerticesCount = this.temp_vertices.Count;
			if (pos != null)
			{
				meshPiece.SetTexPos(pos);
			}
			meshPiece.XyzFacesCount = this.temp_indices.Count / 3;
			Vec3f origin = new Vec3f(0.5f, 0.5f, 0.5f);
			if (node.Rotation != null)
			{
				Vec3f rot = GameMath.ToEulerAngles(new Vec4f((float)node.Rotation[0], (float)node.Rotation[1], (float)node.Rotation[2], (float)node.Rotation[3]));
				meshPiece.Rotate(origin, rot.X, rot.Y, rot.Z);
			}
			if (node.Scale != null)
			{
				meshPiece.Scale(origin, (float)node.Scale[0], (float)node.Scale[1], (float)node.Scale[2]);
			}
			if (node.Translation != null)
			{
				meshPiece.Translate((float)node.Translation[0], (float)node.Translation[1], (float)node.Translation[2]);
			}
			meshPiece.CustomFloats = new CustomMeshDataPartFloat
			{
				Values = new float[]
				{
					colorFactor[0],
					colorFactor[1],
					colorFactor[2],
					pbrFactor[0],
					pbrFactor[1]
				},
				InterleaveSizes = new int[] { 3, 2 },
				InterleaveStride = 20,
				InterleaveOffsets = new int[] { 0, 12 },
				Count = 5
			};
			meshPiece.CustomInts = new CustomMeshDataPartInt
			{
				Values = new int[] { this.meshPieces.Count * meshPiece.XyzCount },
				InterleaveSizes = new int[] { 1 },
				InterleaveStride = 4,
				InterleaveOffsets = new int[1],
				Count = 1
			};
			this.meshPieces.Add(meshPiece);
			this.temp_vertices.Clear();
			this.temp_uvs.Clear();
			this.temp_normals.Clear();
			this.temp_indices.Clear();
			this.temp_material.Clear();
			this.temp_vertexcolor.Clear();
			this.temp_flags.Clear();
		}

		private List<Vec3f> temp_vertices = new List<Vec3f>();

		private List<float> temp_uvs = new List<float>();

		private List<int> temp_normals = new List<int>();

		private List<int> temp_indices = new List<int>();

		private List<Vec3f> temp_material = new List<Vec3f>();

		private List<Vec4us> temp_vertexcolor = new List<Vec4us>();

		private List<int> temp_flags = new List<int>();

		private List<MeshData> meshPieces = new List<MeshData>();

		private int[] capacities;

		private VertexFlags tempFlag = new VertexFlags();
	}
}
