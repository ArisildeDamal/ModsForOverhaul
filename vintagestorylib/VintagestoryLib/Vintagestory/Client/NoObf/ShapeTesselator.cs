using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.Client.NoObf
{
	public class ShapeTesselator : ITesselatorAPI
	{
		public ShapeTesselator(ClientMain game, OrderedDictionary<AssetLocation, UnloadableShape> shapes, OrderedDictionary<AssetLocation, IAsset> objs, OrderedDictionary<AssetLocation, GltfType> gltfs)
		{
			this.shapes = shapes;
			this.objs = objs;
			this.game = game;
			this.gltfs = gltfs;
		}

		public void TesselateShape(string type, AssetLocation sourceName, CompositeShape compositeShape, out MeshData modeldata, ITexPositionSource texSource, int generalGlowLevel = 0, byte climateColorMapIndex = 0, byte seasonColorMapIndex = 0, int? quantityElements = null, string[] selectiveElements = null)
		{
			if (quantityElements == null)
			{
				int? quantityElements2 = compositeShape.QuantityElements;
				int num = 0;
				if ((quantityElements2.GetValueOrDefault() > num) & (quantityElements2 != null))
				{
					quantityElements = compositeShape.QuantityElements;
				}
			}
			if (selectiveElements == null)
			{
				selectiveElements = compositeShape.SelectiveElements;
			}
			this.meta.UsesColorMap = false;
			this.meta.TypeForLogging = type + " " + sourceName;
			this.meta.TexSource = texSource;
			this.meta.GeneralGlowLevel = generalGlowLevel;
			this.meta.QuantityElements = quantityElements;
			this.meta.WithJointIds = false;
			this.meta.SelectiveElements = selectiveElements;
			this.meta.IgnoreElements = compositeShape.IgnoreElements;
			this.meta.ClimateColorMapId = climateColorMapIndex;
			this.meta.SeasonColorMapId = seasonColorMapIndex;
			switch (compositeShape.Format)
			{
			case EnumShapeFormat.Obj:
				this.objTesselator.Load(this.objs[compositeShape.Base], out modeldata, texSource["obj"], this.meta, -1);
				this.ApplyCompositeShapeModifiers(ref modeldata, compositeShape);
				return;
			case EnumShapeFormat.GltfEmbedded:
			{
				TextureAtlasPosition texPos = ((texSource["gltf"] == this.game.api.BlockTextureAtlas[new AssetLocation("unknown")]) ? null : texSource["gltf"]);
				byte[][][] bakedTextures;
				this.gltfTesselator.Load(this.gltfs[compositeShape.Base], out modeldata, texPos, generalGlowLevel, climateColorMapIndex, seasonColorMapIndex, -1, out bakedTextures);
				if (compositeShape.InsertBakedTextures)
				{
					this.gltfs[compositeShape.Base].BaseTextures = new TextureAtlasPosition[bakedTextures.Length];
					this.gltfs[compositeShape.Base].PBRTextures = new TextureAtlasPosition[bakedTextures.Length];
					this.gltfs[compositeShape.Base].NormalTextures = new TextureAtlasPosition[bakedTextures.Length];
					for (int i = 0; i < bakedTextures.Length; i++)
					{
						byte[][] bytes = bakedTextures[i];
						if (bytes[0] != null)
						{
							int id;
							TextureAtlasPosition position;
							if (!this.game.api.BlockTextureAtlas.InsertTexture(bytes[0], out id, out position, 0f))
							{
								this.game.Logger.Debug("Failed adding baked in gltf base texture to atlas from: {0}, texture probably too large.", new object[] { compositeShape.Base });
								this.gltfs[compositeShape.Base].BaseTextures[i] = this.game.api.BlockTextureAtlas[new AssetLocation("unknown")];
							}
							else
							{
								this.gltfs[compositeShape.Base].BaseTextures[i] = position;
								if (texPos == null)
								{
									modeldata.SetTexPos(position);
								}
							}
						}
						if (bytes[1] != null)
						{
							int id2;
							TextureAtlasPosition position2;
							if (!this.game.api.BlockTextureAtlas.InsertTexture(bytes[1], out id2, out position2, 0f))
							{
								this.game.Logger.Debug("Failed adding baked in gltf pbr texture to atlas from: {0}, texture probably too large.", new object[] { compositeShape.Base });
							}
							else
							{
								this.gltfs[compositeShape.Base].PBRTextures[i] = position2;
							}
						}
						if (bytes[2] != null)
						{
							int id3;
							TextureAtlasPosition position3;
							if (!this.game.api.BlockTextureAtlas.InsertTexture(bytes[2], out id3, out position3, 0f))
							{
								this.game.Logger.Debug("Failed adding baked in gltf normal texture to atlas from: {0}, texture probably too large.", new object[] { compositeShape.Base });
							}
							else
							{
								this.gltfs[compositeShape.Base].NormalTextures[i] = position3;
							}
						}
					}
				}
				this.ApplyCompositeShapeModifiers(ref modeldata, compositeShape);
				return;
			}
			}
			UnloadableShape shape;
			if (this.shapes.TryGetValue(compositeShape.Base, out shape))
			{
				if (!shape.Loaded)
				{
					shape.Load(this.game, new AssetLocationAndSource(compositeShape.Base));
				}
				this.rotationVec.Set(compositeShape.rotateX, compositeShape.rotateY, compositeShape.rotateZ);
				this.offsetVec.Set(compositeShape.offsetX, compositeShape.offsetY, compositeShape.offsetZ);
				this.TesselateShape(shape, out modeldata, this.rotationVec, this.offsetVec, compositeShape.Scale, this.meta);
				if (compositeShape.Overlays != null)
				{
					for (int j = 0; j < compositeShape.Overlays.Length; j++)
					{
						CompositeShape ovCompShape = compositeShape.Overlays[j];
						this.meta.QuantityElements = quantityElements;
						this.rotationVec.Set(ovCompShape.rotateX, ovCompShape.rotateY, ovCompShape.rotateZ);
						this.offsetVec.Set(ovCompShape.offsetX, ovCompShape.offsetY, ovCompShape.offsetZ);
						MeshData ovModelData;
						this.TesselateShape(this.shapes[ovCompShape.Base], out ovModelData, this.rotationVec, this.offsetVec, compositeShape.Scale, this.meta);
						modeldata.AddMeshData(ovModelData);
					}
				}
				return;
			}
			if (this.shapes.Count < 2)
			{
				throw new Exception("Something went wrong in the startup process, no " + type + " shapes have been loaded at all. Please try disabling all mods apart from Essentials, Survival, Creative. If that solves the issue, check which mod is causing this. If that does not solve the issue, please report.");
			}
			this.game.Logger.Error("Could not find shape {0} for {1} {2}", new object[] { compositeShape.Base, type, sourceName });
			AssetLocationAndSource als = compositeShape.Base as AssetLocationAndSource;
			if (als != null)
			{
				this.game.Logger.Notification(als.Source.ToString());
			}
			throw new FileNotFoundException(string.Concat(new string[] { "Could not find shape file: ", compositeShape.Base, " in ", type, "type ", sourceName, ".  Possibly a broken mod (", sourceName.Domain, ") or different versions of that mod between server and client?" }));
		}

		public void TesselateShape(CollectibleObject collObj, Shape shape, out MeshData modeldata, Vec3f rotation = null, int? quantityElements = null, string[] selectiveElements = null)
		{
			if (collObj.ItemClass == EnumItemClass.Item)
			{
				TextureSource texSource = new TextureSource(this.game, this.game.ItemAtlasManager.Size, collObj as Item);
				this.TesselateShape("item shape", shape, out modeldata, texSource, rotation, 0, 0, 0, quantityElements, selectiveElements);
				return;
			}
			TextureSource texSource2 = new TextureSource(this.game, this.game.BlockAtlasManager.Size, collObj as Block, false);
			this.TesselateShape("block shape", shape, out modeldata, texSource2, rotation, 0, 0, 0, quantityElements, selectiveElements);
		}

		public void TesselateShape(string typeForLogging, Shape shapeBase, out MeshData modeldata, ITexPositionSource texSource, Vec3f wholeMeshRotation = null, int generalGlowLevel = 0, byte climateColorMapId = 0, byte seasonColorMapId = 0, int? quantityElements = null, string[] selectiveElements = null)
		{
			this.meta.TypeForLogging = typeForLogging;
			this.meta.TexSource = texSource;
			this.meta.GeneralGlowLevel = generalGlowLevel;
			this.meta.GeneralWindMode = 0;
			this.meta.ClimateColorMapId = climateColorMapId;
			this.meta.SeasonColorMapId = seasonColorMapId;
			this.meta.QuantityElements = quantityElements;
			this.meta.SelectiveElements = selectiveElements;
			this.meta.IgnoreElements = null;
			this.meta.WithJointIds = false;
			this.meta.WithDamageEffect = false;
			this.TesselateShape(shapeBase, out modeldata, wholeMeshRotation, null, 1f, this.meta);
		}

		public void TesselateShapeWithJointIds(string typeForLogging, Shape shapeBase, out MeshData modeldata, ITexPositionSource texSource, Vec3f rotation, int? quantityElements, string[] selectiveElements)
		{
			this.meta.TypeForLogging = typeForLogging;
			this.meta.TexSource = texSource;
			this.meta.GeneralGlowLevel = 0;
			this.meta.GeneralWindMode = 0;
			this.meta.ClimateColorMapId = 0;
			this.meta.SeasonColorMapId = 0;
			this.meta.QuantityElements = quantityElements;
			this.meta.SelectiveElements = selectiveElements;
			this.meta.IgnoreElements = null;
			this.meta.WithJointIds = true;
			this.meta.WithDamageEffect = false;
			this.TesselateShape(shapeBase, out modeldata, rotation, null, 1f, this.meta);
		}

		public void TesselateShape(TesselationMetaData meta, Shape shapeBase, out MeshData modeldata)
		{
			this.meta.TypeForLogging = meta.TypeForLogging;
			this.meta.TexSource = meta.TexSource;
			this.meta.GeneralGlowLevel = meta.GeneralGlowLevel;
			this.meta.GeneralWindMode = meta.GeneralWindMode;
			this.meta.ClimateColorMapId = meta.ClimateColorMapId;
			this.meta.SeasonColorMapId = meta.SeasonColorMapId;
			this.meta.QuantityElements = meta.QuantityElements;
			this.meta.SelectiveElements = meta.SelectiveElements;
			this.meta.IgnoreElements = meta.IgnoreElements;
			this.meta.WithJointIds = meta.WithJointIds;
			this.meta.WithDamageEffect = meta.WithDamageEffect;
			this.TesselateShape(shapeBase, out modeldata, meta.Rotation, null, 1f, meta);
		}

		public void TesselateShape(Shape shapeBase, out MeshData modeldata, Vec3f wholeMeshRotation, Vec3f wholeMeshOffset, float wholeMeshScale, TesselationMetaData meta)
		{
			if (wholeMeshRotation == null)
			{
				wholeMeshRotation = this.noRotation;
			}
			modeldata = new MeshData(24, 36, false, true, true, true).WithColorMaps().WithRenderpasses();
			if (meta.WithJointIds)
			{
				modeldata.CustomInts = new CustomMeshDataPartInt();
				modeldata.CustomInts.InterleaveSizes = new int[] { 1 };
				modeldata.CustomInts.InterleaveOffsets = new int[1];
				modeldata.CustomInts.InterleaveStride = 0;
				this.elementMeshData.CustomInts = new CustomMeshDataPartInt();
			}
			else
			{
				this.elementMeshData.CustomInts = null;
			}
			if (meta.WithDamageEffect)
			{
				modeldata.CustomFloats = new CustomMeshDataPartFloat();
				modeldata.CustomFloats.InterleaveSizes = new int[] { 1 };
				modeldata.CustomFloats.InterleaveOffsets = new int[1];
				modeldata.CustomFloats.InterleaveStride = 0;
				this.elementMeshData.CustomFloats = new CustomMeshDataPartFloat();
			}
			this.stackMatrix.Clear();
			this.stackMatrix.PushIdentity();
			Dictionary<string, int[]> texturesSizes = shapeBase.TextureSizes;
			meta.TexturesSizes = texturesSizes;
			meta.defaultTextureSize = new int[] { shapeBase.TextureWidth, shapeBase.TextureHeight };
			this.TesselateShapeElements(modeldata, shapeBase.Elements, meta);
			if (wholeMeshScale != 1f)
			{
				modeldata.Scale(this.constantCenterXZ, wholeMeshScale, wholeMeshScale, wholeMeshScale);
			}
			if (wholeMeshRotation.X != 0f || wholeMeshRotation.Y != 0f || wholeMeshRotation.Z != 0f)
			{
				modeldata.Rotate(this.constantCenter, wholeMeshRotation.X * 0.017453292f, wholeMeshRotation.Y * 0.017453292f, wholeMeshRotation.Z * 0.017453292f);
			}
			if (wholeMeshOffset != null && !wholeMeshOffset.IsZero)
			{
				modeldata.Translate(wholeMeshOffset);
			}
		}

		private void TesselateShapeElements(MeshData meshdata, ShapeElement[] elements, TesselationMetaData meta)
		{
			int i = 0;
			string[] childIgnoreElements = null;
			foreach (ShapeElement element in elements)
			{
				if (meta.QuantityElements != null)
				{
					int? quantityElements = meta.QuantityElements;
					meta.QuantityElements = quantityElements - 1;
					int? num = quantityElements;
					int num2 = 0;
					if ((num.GetValueOrDefault() <= num2) & (num != null))
					{
						break;
					}
				}
				string[] childSelectiveElements;
				if (this.SelectiveMatch(element.Name, meta.SelectiveElements, out childSelectiveElements) && (meta.IgnoreElements == null || !this.SelectiveMatch(element.Name, meta.IgnoreElements, out childIgnoreElements)))
				{
					if (element.From == null || element.From.Length != 3)
					{
						ScreenManager.Platform.Logger.Warning(meta.TypeForLogging + ": shape element " + i.ToString() + " has illegal from coordinates (not set or not length 3). Ignoring element.");
						return;
					}
					if (element.To == null || element.To.Length != 3)
					{
						ScreenManager.Platform.Logger.Warning(meta.TypeForLogging + ": shape element " + i.ToString() + " has illegal to coordinates (not set or not length 3). Ignoring element.");
						return;
					}
					this.stackMatrix.Push();
					double rotationOrigin0;
					double rotationOrigin;
					double rotationOrigin2;
					if (element.RotationOrigin == null)
					{
						rotationOrigin0 = 0.0;
						rotationOrigin = 0.0;
						rotationOrigin2 = 0.0;
					}
					else
					{
						rotationOrigin0 = element.RotationOrigin[0];
						rotationOrigin = element.RotationOrigin[1] * (double)meta.drawnHeight;
						rotationOrigin2 = element.RotationOrigin[2];
						this.stackMatrix.Translate(rotationOrigin0 / 16.0, rotationOrigin / 16.0, rotationOrigin2 / 16.0);
					}
					if (element.RotationX != 0.0)
					{
						this.stackMatrix.Rotate(element.RotationX * 0.017453292519943295, 1.0, 0.0, 0.0);
					}
					if (element.RotationY != 0.0)
					{
						this.stackMatrix.Rotate(element.RotationY * 0.017453292519943295, 0.0, 1.0, 0.0);
					}
					if (element.RotationZ != 0.0)
					{
						this.stackMatrix.Rotate(element.RotationZ * 0.017453292519943295, 0.0, 0.0, 1.0);
					}
					if (element.ScaleX != 1.0 || element.ScaleY != 1.0 || element.ScaleZ != 1.0)
					{
						this.stackMatrix.Scale(element.ScaleX, element.ScaleY, element.ScaleZ);
					}
					this.stackMatrix.Translate((element.From[0] - rotationOrigin0) / 16.0, (element.From[1] - rotationOrigin) / 16.0, (element.From[2] - rotationOrigin2) / 16.0);
					if (element.HasFaces())
					{
						this.elementMeshData.Clear();
						this.TesselateShapeElement(i, this.elementMeshData, element, meta);
						this.elementMeshData.MatrixTransform(this.stackMatrix.Top);
						meshdata.AddMeshData(this.elementMeshData);
					}
					i++;
					if (element.Children != null)
					{
						TesselationMetaData cmeta = meta;
						if (childSelectiveElements != null || childIgnoreElements != null)
						{
							cmeta = meta.Clone();
							cmeta.SelectiveElements = childSelectiveElements;
							cmeta.IgnoreElements = childIgnoreElements;
						}
						this.TesselateShapeElements(meshdata, element.Children, cmeta);
					}
					this.stackMatrix.Pop();
				}
			}
		}

		private void TesselateShapeElement(int indexForLogging, MeshData meshdata, ShapeElement element, TesselationMetaData meta)
		{
			Size2i atlasSize = meta.TexSource.AtlasSize;
			this.xyzVec.Set((float)(element.To[0] - element.From[0]) / 16f, (float)(element.To[1] - element.From[1]) / 16f * meta.drawnHeight, (float)(element.To[2] - element.From[2]) / 16f);
			Vec3f sizeXyz = this.xyzVec;
			if (sizeXyz.IsZero)
			{
				return;
			}
			this.centerVec.Set(sizeXyz.X / 2f, sizeXyz.Y / 2f, sizeXyz.Z / 2f);
			Vec3f relativeCenterXyz = this.centerVec;
			byte climateColorMapId = 0;
			byte seasonColorMapId = 0;
			short renderPass = element.RenderPass;
			if (element.DisableRandomDrawOffset)
			{
				renderPass += 1024;
			}
			bool firstRenderedFace = true;
			for (int f = 0; f < 6; f++)
			{
				ShapeElementFace face = element.FacesResolved[f];
				if (face != null)
				{
					BlockFacing facing = BlockFacing.ALLFACES[f];
					if (firstRenderedFace)
					{
						firstRenderedFace = false;
						climateColorMapId = ((element.ClimateColorMap == null || element.ClimateColorMap.Length == 0) ? meta.ClimateColorMapId : ((byte)(this.game.ColorMaps.IndexOfKey(element.ClimateColorMap) + 1)));
						ColorMap scm;
						if (element.SeasonColorMap == null || element.SeasonColorMap.Length == 0)
						{
							seasonColorMapId = meta.SeasonColorMapId;
						}
						else if (this.game.ColorMaps.TryGetValue(element.SeasonColorMap, out scm))
						{
							seasonColorMapId = (byte)(scm.RectIndex + 1);
						}
						else
						{
							seasonColorMapId = 0;
						}
						meta.UsesColorMap |= climateColorMapId + seasonColorMapId > 0;
					}
					float uvCoords0;
					float uvCoords;
					float uvCoords2;
					float uvCoords3;
					if (face.Uv == null)
					{
						uvCoords0 = 0f;
						uvCoords = 0f;
						uvCoords2 = 0f;
						uvCoords3 = 0f;
						if (facing.Axis == EnumAxis.Y)
						{
							uvCoords2 = sizeXyz.X * 16f;
							uvCoords3 = sizeXyz.Z * 16f;
						}
						else if (facing.Axis == EnumAxis.X)
						{
							uvCoords2 = sizeXyz.Z * 16f;
							uvCoords3 = sizeXyz.Y * 16f;
						}
						else if (facing.Axis == EnumAxis.Z)
						{
							uvCoords2 = sizeXyz.X * 16f;
							uvCoords3 = sizeXyz.Y * 16f;
						}
					}
					else
					{
						if (face.Uv.Length != 4)
						{
							ScreenManager.Platform.Logger.Warning(string.Concat(new string[]
							{
								meta.TypeForLogging,
								", shape element ",
								indexForLogging.ToString(),
								": Facing '",
								facing.Code,
								"' doesn't have exactly 4 uv values. Ignoring face."
							}));
							goto IL_073C;
						}
						uvCoords0 = face.Uv[0];
						uvCoords = face.Uv[3] + (face.Uv[1] - face.Uv[3]) * meta.drawnHeight;
						uvCoords2 = face.Uv[2];
						uvCoords3 = face.Uv[3];
					}
					string texturecode = face.Texture;
					TextureAtlasPosition texPos = meta.TexSource[texturecode];
					if (texPos == null)
					{
						throw new ArgumentNullException(string.Concat(new string[] { "Unable to find a texture for texture code '", texturecode, "' in ", meta.TypeForLogging, ". Giving up. Sorry." }));
					}
					int[] textureSize;
					if (!meta.TexturesSizes.TryGetValue(texturecode, out textureSize))
					{
						textureSize = meta.defaultTextureSize;
					}
					float ratiox = (texPos.x2 - texPos.x1) * (float)atlasSize.Width / (float)textureSize[0];
					float ratioy = (texPos.y2 - texPos.y1) * (float)atlasSize.Height / (float)textureSize[1];
					uvCoords0 *= ratiox;
					uvCoords *= ratioy;
					uvCoords2 *= ratiox;
					uvCoords3 *= ratioy;
					if (uvCoords == uvCoords3)
					{
						uvCoords3 += 0.03125f;
					}
					if (uvCoords0 == uvCoords2)
					{
						uvCoords2 += 0.03125f;
					}
					int rot = (int)(face.Rotation / 90f);
					Vec2f originUv = new Vec2f(texPos.x1 + uvCoords0 / (float)atlasSize.Width, texPos.y1 + uvCoords3 / (float)atlasSize.Height);
					Vec2f sizeUv = new Vec2f((uvCoords2 - uvCoords0) / (float)atlasSize.Width, (uvCoords - uvCoords3) / (float)atlasSize.Height);
					sizeUv.X -= Math.Max(0f, originUv.X + sizeUv.X - texPos.x2);
					sizeUv.Y -= Math.Max(0f, originUv.Y + sizeUv.Y - texPos.y2);
					ModelCubeUtilExt.EnumShadeMode shade = ModelCubeUtilExt.EnumShadeMode.On;
					int baseFlags = ((int)(element.ZOffset & 7) << 8) | (meta.GeneralGlowLevel + face.Glow);
					if (face.ReflectiveMode != EnumReflectiveMode.None)
					{
						baseFlags |= 2048;
						sbyte i = (sbyte)Math.Max(0, face.ReflectiveMode - EnumReflectiveMode.Weak);
						face.WindData = new sbyte[] { i, i, i, i };
					}
					if (element.Shade)
					{
						baseFlags |= BlockFacing.AllVertexFlagsNormals[facing.Index];
					}
					else if (element.GradientShade)
					{
						shade = ModelCubeUtilExt.EnumShadeMode.Gradient;
					}
					else
					{
						shade = ModelCubeUtilExt.EnumShadeMode.Off;
						baseFlags |= BlockFacing.UP.NormalPackedFlags;
					}
					this.flags[0] = (this.flags[1] = (this.flags[2] = (this.flags[3] = baseFlags)));
					if (face.WindMode == null)
					{
						int wind = meta.GeneralWindMode << 25;
						if (wind != 0)
						{
							this.flags[0] |= wind;
							this.flags[1] |= wind;
							this.flags[2] |= wind;
							this.flags[3] |= wind;
							meshdata.HasAnyWindModeSet = true;
						}
					}
					else
					{
						for (int j = 0; j < this.flags.Length; j++)
						{
							int windMode = (int)face.WindMode[(f == 4) ? ((j + 2) % 4) : ((f / 2 == 1) ? ((j + 1) % 4) : j)];
							if (windMode > 0)
							{
								VertexFlags.SetWindMode(ref this.flags[j], windMode);
								meshdata.HasAnyWindModeSet = true;
							}
						}
					}
					if (face.WindData != null)
					{
						for (int k = 0; k < this.flags.Length; k++)
						{
							int windData = (int)face.WindData[(f == 4) ? ((k + 2) % 4) : ((f / 2 == 1) ? ((k + 1) % 4) : k)];
							if (windData > 0)
							{
								VertexFlags.SetWindData(ref this.flags[k], windData);
							}
						}
					}
					ModelCubeUtilExt.AddFace(meshdata, facing, relativeCenterXyz, sizeXyz, originUv, sizeUv, texPos.atlasTextureId, element.Color, shade, this.flags, 1f, rot % 4, climateColorMapId, seasonColorMapId, renderPass);
					if (meta.WithJointIds)
					{
						meshdata.CustomInts.Add(new int[] { element.JointId, element.JointId, element.JointId, element.JointId });
					}
					if (meta.WithDamageEffect)
					{
						meshdata.CustomFloats.Add(new float[] { element.DamageEffect, element.DamageEffect, element.DamageEffect, element.DamageEffect });
					}
				}
				IL_073C:;
			}
		}

		public void TesselateBlock(Block block, out MeshData meshdata)
		{
			TextureSource texSource = new TextureSource(this.game, this.game.BlockAtlasManager.Size, block, false);
			this.TesselateBlock(block, out meshdata, texSource, null, null);
		}

		public void TesselateBlock(Block block, out MeshData modeldata, TextureSource textureSource, int? quantityElements = null, string[] selectiveElements = null)
		{
			this.TesselateBlock(block, block.Shape, out modeldata, textureSource, quantityElements, selectiveElements);
		}

		public void TesselateBlock(Block block, CompositeShape compositeShape, out MeshData modeldata, TextureSource texSource, int? quantityElements = null, string[] selectiveElements = null)
		{
			byte climateColorMapId = ((block.ClimateColorMapResolved == null) ? 0 : ((byte)(block.ClimateColorMapResolved.RectIndex + 1)));
			byte seasonColorMapId = ((block.SeasonColorMapResolved == null) ? 0 : ((byte)(block.SeasonColorMapResolved.RectIndex + 1)));
			this.meta.GeneralWindMode = (int)block.VertexFlags.WindMode;
			TesselationMetaData tesselationMetaData = this.meta;
			IWithDrawnHeight iwdh = block as IWithDrawnHeight;
			tesselationMetaData.drawnHeight = ((iwdh != null && iwdh.drawnHeight > 0) ? ((float)iwdh.drawnHeight / 48f) : 1f);
			this.TesselateShape("block", block.Code, compositeShape, out modeldata, texSource, (int)block.VertexFlags.GlowLevel, climateColorMapId, seasonColorMapId, quantityElements, selectiveElements);
			this.meta.drawnHeight = 1f;
			if (compositeShape.Format == EnumShapeFormat.VintageStory)
			{
				block.ShapeUsesColormap |= this.meta.UsesColorMap || block.ClimateColorMap != null || block.SeasonColorMap != null;
			}
		}

		public void TesselateItem(Item item, out MeshData modeldata, ITexPositionSource texSource)
		{
			this.meta.GeneralWindMode = 0;
			if (item.Shape == null || item.Shape.VoxelizeTexture)
			{
				CompositeTexture texture = item.FirstTexture;
				CompositeShape shape = item.Shape;
				if (((shape != null) ? shape.Base : null) != null)
				{
					texture = item.Textures[item.Shape.Base.Path.ToString()];
				}
				BakedBitmap bcBmp = TextureAtlasManager.LoadCompositeBitmap(this.game, texture.Baked.BakedName.ToString());
				TextureAtlasPosition pos = texSource[texture.Baked.BakedName.ToString()];
				modeldata = ShapeTesselator.VoxelizeTextureStatic(bcBmp.TexturePixels, bcBmp.Width, bcBmp.Height, pos, null);
				return;
			}
			this.TesselateShape("item", item.Code, item.Shape, out modeldata, texSource, 0, 0, 0, null, null);
		}

		public void TesselateItem(Item item, out MeshData modeldata)
		{
			this.TesselateItem(item, item.Shape, out modeldata);
		}

		public void TesselateItem(Item item, CompositeShape forShape, out MeshData modeldata)
		{
			this.meta.GeneralWindMode = 0;
			if (item == null || item.Code == null)
			{
				modeldata = this.unknownItemModelData;
				return;
			}
			if (forShape != null && !forShape.VoxelizeTexture)
			{
				TextureSource texSource = new TextureSource(this.game, this.game.BlockAtlasManager.Size, item);
				this.TesselateShape("item", item.Code, forShape, out modeldata, texSource, 0, 0, 0, null, null);
				return;
			}
			CompositeTexture texture = item.FirstTexture;
			if (((forShape != null) ? forShape.Base : null) != null && !item.Textures.TryGetValue(forShape.Base.ToShortString(), out texture))
			{
				ScreenManager.Platform.Logger.Warning("Item {0} has no shape defined and has no texture definition. Will use unknown texture.", new object[] { item.Code });
			}
			if (texture != null)
			{
				int textureSubId = texture.Baked.TextureSubId;
				TextureAtlasPosition pos = this.game.ItemAtlasManager.TextureAtlasPositionsByTextureSubId[textureSubId];
				BakedBitmap bcBmp = TextureAtlasManager.LoadCompositeBitmap(this.game, new AssetLocationAndSource(texture.Baked.BakedName, "Item code ", item.Code, -1));
				modeldata = ShapeTesselator.VoxelizeTextureStatic(bcBmp.TexturePixels, bcBmp.Width, bcBmp.Height, pos, null);
				return;
			}
			modeldata = this.unknownItemModelData;
		}

		public static MeshData VoxelizeTextureStatic(int[] texturePixels, int width, int height, TextureAtlasPosition pos, Vec3f rotation = null)
		{
			MeshData modeldata = new MeshData(20, 20, false, true, true, true);
			if (rotation == null)
			{
				rotation = new Vec3f();
			}
			if (pos == null)
			{
				pos = new TextureAtlasPosition();
			}
			float scale = 1.5f;
			float uvWidth = pos.x2 - pos.x1;
			float uvHeight = pos.y2 - pos.y1;
			Vec3f centerXyz = new Vec3f(0f, 0f, 0.5f);
			Vec3f sizeXyz = new Vec3f(scale / (float)width, scale / (float)height, scale / 24f);
			Vec2f originUv = new Vec2f(0f, 0f);
			Vec2f sizeUv = new Vec2f(uvWidth / (float)width, uvHeight / (float)height);
			int[] faceColors = new int[6];
			ModelCubeUtilExt.EnumShadeMode shade = ModelCubeUtilExt.EnumShadeMode.On;
			for (int i = 0; i < 6; i++)
			{
				float brightness = BlockFacing.ALLFACES[i].GetFaceBrightness(rotation.X, rotation.Y, rotation.Z, CubeMeshUtil.DefaultBlockSideShadingsByFacing);
				faceColors[i] = ColorUtil.ColorMultiply3(-1, brightness);
			}
			int textureId = pos.atlasTextureId;
			for (int x = 0; x < width; x++)
			{
				for (int y = 0; y < height; y++)
				{
					centerXyz.X = scale * (float)x / (float)width - (scale - 1f) / 4f;
					centerXyz.Y = scale * (float)y / (float)height - (scale - 1f) / 4f;
					originUv.X = pos.x1 + uvWidth * (float)x / (float)width;
					originUv.Y = pos.y1 + uvHeight * (float)y / (float)height;
					if (((texturePixels[y * width + x] >> 24) & 255) > 5)
					{
						bool flag = x > 0 && ((texturePixels[y * width + x - 1] >> 24) & 255) > 5;
						bool downOpaque = y > 0 && ((texturePixels[(y - 1) * width + x] >> 24) & 255) > 5;
						bool flag2 = x < width - 1 && ((texturePixels[y * width + x + 1] >> 24) & 255) > 5;
						bool topOpaque = y < height - 1 && ((texturePixels[(y + 1) * width + x] >> 24) & 255) > 5;
						if (!flag2)
						{
							ModelCubeUtilExt.AddFace(modeldata, BlockFacing.EAST, centerXyz, sizeXyz, originUv, sizeUv, textureId, faceColors[BlockFacing.EAST.Index], shade, ShapeTesselator.noFlags, 1f, 0, 0, 0, -1);
						}
						if (!flag)
						{
							ModelCubeUtilExt.AddFace(modeldata, BlockFacing.WEST, centerXyz, sizeXyz, originUv, sizeUv, textureId, faceColors[BlockFacing.WEST.Index], shade, ShapeTesselator.noFlags, 1f, 0, 0, 0, -1);
						}
						if (!topOpaque)
						{
							ModelCubeUtilExt.AddFace(modeldata, BlockFacing.UP, centerXyz, sizeXyz, originUv, sizeUv, textureId, faceColors[BlockFacing.DOWN.Index], shade, ShapeTesselator.noFlags, 1f, 0, 0, 0, -1);
						}
						if (!downOpaque)
						{
							ModelCubeUtilExt.AddFace(modeldata, BlockFacing.DOWN, centerXyz, sizeXyz, originUv, sizeUv, textureId, faceColors[BlockFacing.UP.Index], shade, ShapeTesselator.noFlags, 1f, 0, 0, 0, -1);
						}
						ModelCubeUtilExt.AddFace(modeldata, BlockFacing.NORTH, centerXyz, sizeXyz, originUv, sizeUv, textureId, faceColors[BlockFacing.SOUTH.Index], shade, ShapeTesselator.noFlags, 1f, 0, 0, 0, -1);
						ModelCubeUtilExt.AddFace(modeldata, BlockFacing.SOUTH, centerXyz, sizeXyz, originUv, sizeUv, textureId, faceColors[BlockFacing.NORTH.Index], shade, ShapeTesselator.noFlags, 1f, 0, 0, 0, -1);
					}
				}
			}
			return modeldata;
		}

		public MeshData VoxelizeTexture(CompositeTexture texture, Size2i atlasSize, TextureAtlasPosition atlasPos)
		{
			BakedBitmap bcBmp = TextureAtlasManager.LoadCompositeBitmap(this.game, new AssetLocationAndSource(texture.Baked.BakedName));
			return ShapeTesselator.VoxelizeTextureStatic(bcBmp.TexturePixels, bcBmp.Width, bcBmp.Height, atlasPos, null);
		}

		public MeshData VoxelizeTexture(int[] texturePixels, int width, int height, Size2i atlasSize, TextureAtlasPosition atlasPos)
		{
			return ShapeTesselator.VoxelizeTextureStatic(texturePixels, width, height, atlasPos, null);
		}

		public int AltTexturesCount(Block block)
		{
			int cnt = 0;
			foreach (CompositeTexture compositeTexture in block.Textures.Values)
			{
				BakedCompositeTexture baked = compositeTexture.Baked;
				BakedCompositeTexture[] variants = ((baked != null) ? baked.BakedVariants : null);
				if (variants != null && variants.Length > cnt)
				{
					cnt = variants.Length;
				}
			}
			return cnt;
		}

		public int TileTexturesCount(Block block)
		{
			int cnt = 0;
			foreach (CompositeTexture compositeTexture in block.Textures.Values)
			{
				BakedCompositeTexture baked = compositeTexture.Baked;
				BakedCompositeTexture[] tiles = ((baked != null) ? baked.BakedTiles : null);
				if (tiles != null && tiles.Length > cnt)
				{
					cnt = tiles.Length;
				}
			}
			return cnt;
		}

		public ITexPositionSource GetTexSource(Block block, int altTextureNumber = 0, bool returnNullWhenMissing = false)
		{
			return this.GetTextureSource(block, altTextureNumber, returnNullWhenMissing);
		}

		public ITexPositionSource GetTextureSource(Block block, int altTextureNumber = 0, bool returnNullWhenMissing = false)
		{
			return new TextureSource(this.game, this.game.BlockAtlasManager.Size, block, altTextureNumber)
			{
				returnNullWhenMissing = returnNullWhenMissing
			};
		}

		public ITexPositionSource GetTextureSource(Item item, bool returnNullWhenMissing = false)
		{
			return new TextureSource(this.game, this.game.ItemAtlasManager.Size, item)
			{
				returnNullWhenMissing = returnNullWhenMissing
			};
		}

		public ITexPositionSource GetTextureSource(Entity entity, Dictionary<string, CompositeTexture> extraTextures = null, int altTextureNumber = 0, bool returnNullWhenMissing = false)
		{
			return new TextureSource(this.game, this.game.EntityAtlasManager.Size, entity, extraTextures, altTextureNumber)
			{
				returnNullWhenMissing = returnNullWhenMissing
			};
		}

		public void ApplyCompositeShapeModifiers(ref MeshData modeldata, CompositeShape compositeShape)
		{
			if (compositeShape.Scale != 1f)
			{
				modeldata.Scale(this.constantCenterXZ, compositeShape.Scale, compositeShape.Scale, compositeShape.Scale);
			}
			if (compositeShape.rotateX != 0f || compositeShape.rotateY != 0f || compositeShape.rotateZ != 0f)
			{
				modeldata.Rotate(this.constantCenter, compositeShape.rotateX * 0.017453292f, compositeShape.rotateY * 0.017453292f, compositeShape.rotateZ * 0.017453292f);
			}
			if (compositeShape.offsetX != 0f || compositeShape.offsetY != 0f || compositeShape.offsetZ != 0f)
			{
				modeldata.Translate(new Vec3f(compositeShape.offsetX, compositeShape.offsetY, compositeShape.offsetZ));
			}
		}

		private bool SelectiveMatch(string needle, string[] haystackElements, out string[] childHaystackElements)
		{
			childHaystackElements = null;
			if (haystackElements == null)
			{
				return true;
			}
			for (int i = 0; i < haystackElements.Length; i++)
			{
				string haystack = haystackElements[i];
				if (haystack.Length != 0)
				{
					if (haystack == needle)
					{
						childHaystackElements = Array.Empty<string>();
						return true;
					}
					if (haystack == "*" || haystack.EqualsFast(needle + "/*") || (haystack[haystack.Length - 1] == '*' && needle.StartsWithFast(haystack.Substring(0, haystack.Length - 1))))
					{
						childHaystackElements = new string[] { "*" };
						return true;
					}
					if (haystack.IndexOf('/') == needle.Length && haystack.StartsWithFast(needle))
					{
						int childSelectionsCount = 0;
						for (int j = i; j < haystackElements.Length; j++)
						{
							if (haystackElements[j].IndexOf('/') == needle.Length && haystackElements[j].StartsWithFast(needle))
							{
								childSelectionsCount++;
							}
						}
						childHaystackElements = new string[childSelectionsCount];
						if (childSelectionsCount > 0)
						{
							int cSEIndex = 0;
							for (int k = i; k < haystackElements.Length; k++)
							{
								haystack = haystackElements[k];
								int slashIndex = haystack.IndexOf('/');
								if (slashIndex == needle.Length && haystack.StartsWithFast(needle))
								{
									childHaystackElements[cSEIndex++] = haystack.Substring(slashIndex + 1);
								}
							}
						}
						return true;
					}
				}
			}
			return false;
		}

		public OrderedDictionary<AssetLocation, UnloadableShape> shapes;

		public OrderedDictionary<AssetLocation, IAsset> objs;

		public OrderedDictionary<AssetLocation, GltfType> gltfs;

		private ClientMain game;

		private Vec3f noRotation = new Vec3f();

		private Vec3f constantCenter = new Vec3f(0.5f, 0.5f, 0.5f);

		private Vec3f constantCenterXZ = new Vec3f(0.5f, 0f, 0.5f);

		private Vec3f rotationVec = new Vec3f();

		private Vec3f offsetVec = new Vec3f();

		private Vec3f xyzVec = new Vec3f();

		private Vec3f centerVec = new Vec3f();

		public MeshData unknownItemModelData = QuadMeshUtilExt.GetCustomQuadModelData(0f, 0f, 0f, 1f, 1f, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, 0);

		private ObjTesselator objTesselator = new ObjTesselator();

		private GltfTesselator gltfTesselator = new GltfTesselator();

		private TesselationMetaData meta = new TesselationMetaData();

		private MeshData elementMeshData = new MeshData(24, 36, false, true, true, true).WithColorMaps().WithRenderpasses();

		private StackMatrix4 stackMatrix = new StackMatrix4(64);

		private int[] flags = new int[4];

		private static int[] noFlags = new int[4];
	}
}
