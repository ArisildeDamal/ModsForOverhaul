using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class BlockTextureAtlasManager : TextureAtlasManager, IBlockTextureAtlasAPI, ITextureAtlasAPI
	{
		public BlockTextureAtlasManager(ClientMain game)
			: base(game)
		{
		}

		internal void CollectTextures(IList<Block> blocks, OrderedDictionary<AssetLocation, UnloadableShape> shapes)
		{
			Block snowLayerBlock = null;
			int snowTextureSubId = 0;
			AssetLocation snowlayerloc = new AssetLocation("snowlayer-1");
			Dictionary<AssetLocation, CompositeTexture> shapeTexturesCache = new Dictionary<AssetLocation, CompositeTexture>();
			foreach (Block block in blocks)
			{
				if (!(block.Code == null))
				{
					block.EnsureValidTextures(this.game.Logger);
					this.ResolveTextureCodes(block, shapes, shapeTexturesCache);
				}
			}
			foreach (Block block2 in blocks)
			{
				if (this.game.disposed)
				{
					return;
				}
				if (!(block2.Code == null) && block2.DrawType == EnumDrawType.TopSoil)
				{
					this.collectTexturesForBlock(block2, shapes);
				}
			}
			foreach (Block block3 in blocks)
			{
				if (this.game.disposed)
				{
					return;
				}
				if (!(block3.Code == null) && block3.DrawType != EnumDrawType.TopSoil)
				{
					this.collectTexturesForBlock(block3, shapes);
					if (snowLayerBlock == null && block3.Code.Equals(snowlayerloc))
					{
						snowLayerBlock = block3;
						this.compose(snowLayerBlock, 0);
						snowTextureSubId = ((snowLayerBlock != null) ? snowLayerBlock.Textures[BlockFacing.UP.Code].Baked.TextureSubId : 0);
					}
				}
			}
			foreach (Block block4 in blocks)
			{
				if (block4 != null && !(block4.Code == null))
				{
					this.compose(block4, snowTextureSubId);
				}
			}
		}

		private void collectTexturesForBlock(Block block, OrderedDictionary<AssetLocation, UnloadableShape> shapes)
		{
			block.OnCollectTextures(this.game.api, this);
			if (block.Shape != null)
			{
				this.collectAndBakeTexturesFromShape(block, block.Shape, false, shapes);
			}
			if (block.ShapeInventory != null)
			{
				this.collectAndBakeTexturesFromShape(block, block.ShapeInventory, true, shapes);
			}
		}

		private void compose(Block block, int snowtextureSubid)
		{
			int blockId = block.BlockId;
			foreach (KeyValuePair<string, CompositeTexture> val in block.TexturesInventory)
			{
				val.Value.Baked.TextureSubId = this.textureNamesDict[val.Value.Baked.BakedName];
			}
			foreach (KeyValuePair<string, CompositeTexture> val2 in block.Textures)
			{
				BakedCompositeTexture bct = val2.Value.Baked;
				bct.TextureSubId = this.textureNamesDict[bct.BakedName];
				if (bct.BakedVariants != null)
				{
					for (int i = 0; i < bct.BakedVariants.Length; i++)
					{
						bct.BakedVariants[i].TextureSubId = this.textureNamesDict[bct.BakedVariants[i].BakedName];
					}
				}
				if (bct.BakedTiles != null)
				{
					for (int j = 0; j < bct.BakedTiles.Length; j++)
					{
						bct.BakedTiles[j].TextureSubId = this.textureNamesDict[bct.BakedTiles[j].BakedName];
					}
				}
			}
			if (block.DrawType != EnumDrawType.JSON)
			{
				foreach (BlockFacing facing in BlockFacing.ALLFACES)
				{
					CompositeTexture faceTexture;
					if (block.Textures.TryGetValue(facing.Code, out faceTexture))
					{
						int textureSubid = faceTexture.Baked.TextureSubId;
						this.game.FastBlockTextureSubidsByBlockAndFace[blockId][facing.Index] = textureSubid;
					}
				}
				CompositeTexture secondTexture;
				if (block.Textures.TryGetValue("specialSecondTexture", out secondTexture))
				{
					this.game.FastBlockTextureSubidsByBlockAndFace[blockId][6] = secondTexture.Baked.TextureSubId;
				}
				else
				{
					this.game.FastBlockTextureSubidsByBlockAndFace[blockId][6] = this.game.FastBlockTextureSubidsByBlockAndFace[blockId][BlockFacing.UP.Index];
				}
			}
			if (block.DrawType == EnumDrawType.JSONAndSnowLayer || block.DrawType == EnumDrawType.CrossAndSnowlayer || block.DrawType == EnumDrawType.CrossAndSnowlayer_2 || block.DrawType == EnumDrawType.CrossAndSnowlayer_3 || block.DrawType == EnumDrawType.CrossAndSnowlayer_4)
			{
				this.game.FastBlockTextureSubidsByBlockAndFace[blockId][6] = snowtextureSubid;
			}
		}

		private void collectAndBakeTexturesFromShape(Block block, CompositeShape shape, bool inv, OrderedDictionary<AssetLocation, UnloadableShape> shapes)
		{
			UnloadableShape compositeShape;
			if (shapes.TryGetValue(shape.Base, out compositeShape))
			{
				IDictionary<string, CompositeTexture> targetDict = (inv ? block.TexturesInventory : block.Textures);
				this.CollectAndBakeTexturesFromShape(compositeShape, targetDict, shape.Base);
			}
			if (shape.BakedAlternates != null)
			{
				foreach (CompositeShape val in shape.BakedAlternates)
				{
					this.collectAndBakeTexturesFromShape(block, val, inv, shapes);
				}
			}
		}

		public override void CollectAndBakeTexturesFromShape(Shape compositeShape, IDictionary<string, CompositeTexture> targetDict, AssetLocation baseLoc)
		{
			Dictionary<string, AssetLocation> shapeTextures = compositeShape.Textures;
			if (shapeTextures != null)
			{
				foreach (KeyValuePair<string, AssetLocation> val in shapeTextures)
				{
					if (!targetDict.ContainsKey(val.Key))
					{
						CompositeTexture ct = new CompositeTexture(val.Value);
						ct.Bake(this.game.AssetManager);
						AssetLocationAndSource locS = new AssetLocationAndSource(ct.Baked.BakedName, "Shape file ", baseLoc, -1);
						if (val.Key == "specialSecondTexture")
						{
							locS.AddToAllAtlasses = true;
						}
						ct.Baked.TextureSubId = base.GetOrAddTextureLocation(locS);
						targetDict[val.Key] = ct;
					}
				}
			}
		}

		public void ResolveTextureCodes(Block block, OrderedDictionary<AssetLocation, UnloadableShape> blockShapes, Dictionary<AssetLocation, CompositeTexture> basicTexturesCache)
		{
			UnloadableShape baseShape;
			blockShapes.TryGetValue(block.Shape.Base, out baseShape);
			UnloadableShape inventoryShape = ((block.ShapeInventory == null) ? null : blockShapes[block.ShapeInventory.Base]);
			if (baseShape != null && !baseShape.Loaded)
			{
				baseShape.Load(this.game, new AssetLocationAndSource(block.Shape.Base));
			}
			if (inventoryShape != null && !inventoryShape.Loaded)
			{
				inventoryShape.Load(this.game, new AssetLocationAndSource(block.Shape.Base));
			}
			bool blockTexturesContainsAll = block.Textures.ContainsKey("all");
			bool invTexturesContainsAll = block.TexturesInventory.ContainsKey("all");
			if (blockTexturesContainsAll || invTexturesContainsAll)
			{
				this.LoadAllTextureCodes(block, baseShape);
			}
			if (block.Textures.Count > 0)
			{
				base.ResolveTextureDict((TextureDictionary)block.Textures);
			}
			if (block.TexturesInventory.Count > 0)
			{
				base.ResolveTextureDict((TextureDictionary)block.TexturesInventory);
			}
			if (((baseShape != null) ? baseShape.Textures : null) != null)
			{
				if (baseShape.TexturesResolved == null)
				{
					baseShape.ResolveTextures(basicTexturesCache);
				}
				foreach (KeyValuePair<string, CompositeTexture> val in baseShape.TexturesResolved)
				{
					string textureCode = val.Key;
					CompositeTexture shapefileTexture = val.Value;
					CompositeTexture tex;
					if (block.Textures.TryGetValue(textureCode, out tex))
					{
						if (tex.Base.Path == "inherit")
						{
							tex.Base = shapefileTexture.Base;
						}
						if (tex.BlendedOverlays != null)
						{
							BlendedOverlayTexture[] overlays = tex.BlendedOverlays;
							for (int i = 0; i < overlays.Length; i++)
							{
								if (overlays[i].Base.Path == "inherit")
								{
									overlays[i].Base = shapefileTexture.Base;
								}
							}
						}
					}
					else if (!blockTexturesContainsAll || !(textureCode == "all"))
					{
						block.Textures.Add(textureCode, shapefileTexture);
					}
				}
			}
			this.replacements.Clear();
			foreach (KeyValuePair<string, CompositeTexture> val2 in block.Textures)
			{
				CompositeTexture tex2 = val2.Value;
				if (tex2.IsBasic())
				{
					CompositeTexture cachedTex;
					if (basicTexturesCache.TryGetValue(tex2.Base, out cachedTex))
					{
						if (tex2 != cachedTex)
						{
							this.replacements.Add(new KeyValuePair<string, CompositeTexture>(val2.Key, cachedTex));
							tex2 = cachedTex;
						}
					}
					else
					{
						basicTexturesCache.Add(tex2.Base, tex2);
					}
				}
				((TextureDictionary)block.TexturesInventory).AddIfNotPresent(val2.Key, tex2);
			}
			foreach (KeyValuePair<string, CompositeTexture> val3 in this.replacements)
			{
				block.Textures[val3.Key] = val3.Value;
			}
			if (inventoryShape != null && inventoryShape.Textures != null)
			{
				if (inventoryShape.TexturesResolved == null)
				{
					inventoryShape.ResolveTextures(basicTexturesCache);
				}
				foreach (KeyValuePair<string, CompositeTexture> val4 in inventoryShape.TexturesResolved)
				{
					string textureCode2 = val4.Key;
					if (!invTexturesContainsAll || !(textureCode2 == "all"))
					{
						((TextureDictionary)block.TexturesInventory).AddIfNotPresent(textureCode2, val4.Value);
					}
				}
			}
		}

		public void LoadAllTextureCodes(Block block, Shape blockShape)
		{
			this.LoadShapeTextureCodes(blockShape);
			if (block.DrawType == EnumDrawType.Cube)
			{
				this.textureCodes.Add("west");
				this.textureCodes.Add("east");
				this.textureCodes.Add("north");
				this.textureCodes.Add("south");
				this.textureCodes.Add("up");
				this.textureCodes.Add("down");
			}
		}

		public TextureAtlasPosition GetPosition(Block block, string textureName, bool returnNullWhenMissing = false)
		{
			return new TextureSource(this.game, base.Size, block, false)
			{
				returnNullWhenMissing = returnNullWhenMissing
			}[textureName];
		}

		public override TextureAtlas RuntimeCreateNewAtlas(string itemclass)
		{
			TextureAtlas atlas = base.RuntimeCreateNewAtlas(itemclass);
			int[] textureIds = this.game.TerrainChunkTesselator.RuntimeCreateNewBlockTextureAtlas(atlas.textureId);
			this.game.chunkRenderer.RuntimeAddBlockTextureAtlas(textureIds);
			return atlas;
		}

		private List<KeyValuePair<string, CompositeTexture>> replacements = new List<KeyValuePair<string, CompositeTexture>>();
	}
}
