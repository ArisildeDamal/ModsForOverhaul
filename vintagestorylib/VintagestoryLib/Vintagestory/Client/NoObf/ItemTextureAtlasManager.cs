using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf
{
	public class ItemTextureAtlasManager : TextureAtlasManager, IItemTextureAtlasAPI, ITextureAtlasAPI
	{
		public ItemTextureAtlasManager(ClientMain game)
			: base(game)
		{
		}

		internal void CollectTextures(IList<Item> items, Dictionary<AssetLocation, UnloadableShape> shapes)
		{
			CompositeTexture unknown = new CompositeTexture(new AssetLocation("unknown"));
			AssetManager assetManager = this.game.Platform.AssetManager;
			foreach (Item item in items)
			{
				if (this.game.disposed)
				{
					return;
				}
				if (!(item.Code == null))
				{
					this.ResolveTextureCodes(item, shapes);
					if (item.FirstTexture == null)
					{
						item.Textures["all"] = unknown;
					}
					foreach (KeyValuePair<string, CompositeTexture> val in item.Textures)
					{
						val.Value.Bake(this.game.Platform.AssetManager);
						if (!base.ContainsKey(val.Value.Baked.BakedName))
						{
							base.SetTextureLocation(new AssetLocationAndSource(val.Value.Baked.BakedName, "Item ", item.Code, -1));
						}
					}
					CompositeShape shape2 = item.Shape;
					UnloadableShape shape;
					if (((shape2 != null) ? shape2.Base : null) != null && shapes.TryGetValue(item.Shape.Base, out shape))
					{
						Dictionary<string, AssetLocation> shapeTextures = shape.Textures;
						if (shapeTextures != null)
						{
							foreach (KeyValuePair<string, AssetLocation> val2 in shapeTextures)
							{
								if (!base.ContainsKey(val2.Value))
								{
									base.SetTextureLocation(new AssetLocationAndSource(val2.Value, "Shape file ", item.Shape.Base, -1));
								}
								if (!item.Textures.ContainsKey(val2.Key))
								{
									CompositeTexture ct = new CompositeTexture
									{
										Base = val2.Value.Clone()
									};
									item.Textures[val2.Key] = ct;
									ct.Bake(assetManager);
								}
							}
						}
					}
				}
			}
			foreach (Item item2 in items)
			{
				if (item2 != null)
				{
					foreach (KeyValuePair<string, CompositeTexture> val3 in item2.Textures)
					{
						val3.Value.Baked.TextureSubId = this.textureNamesDict[val3.Value.Baked.BakedName];
					}
				}
			}
		}

		public TextureAtlasPosition GetPosition(Item item, string textureName = null, bool returnNullWhenMissing = false)
		{
			if (item.Shape == null || item.Shape.VoxelizeTexture)
			{
				CompositeTexture texture = item.FirstTexture;
				CompositeShape shape = item.Shape;
				if (((shape != null) ? shape.Base : null) != null && !item.Textures.TryGetValue(item.Shape.Base.Path.ToString(), out texture))
				{
					texture = item.FirstTexture;
				}
				int textureSubId = texture.Baked.TextureSubId;
				return this.TextureAtlasPositionsByTextureSubId[textureSubId];
			}
			return new TextureSource(this.game, base.Size, item)
			{
				returnNullWhenMissing = returnNullWhenMissing
			}[textureName];
		}

		public void ResolveTextureCodes(Item item, Dictionary<AssetLocation, UnloadableShape> itemShapes)
		{
			CompositeShape shape = item.Shape;
			if (((shape != null) ? shape.Base : null) == null)
			{
				return;
			}
			UnloadableShape baseShape;
			if (!itemShapes.TryGetValue(item.Shape.Base, out baseShape))
			{
				this.game.Logger.VerboseDebug("Not found item shape " + item.Shape.Base + ", for item " + item.Code);
				return;
			}
			item.CheckTextures(this.game.Logger);
			if (baseShape.Textures != null)
			{
				foreach (KeyValuePair<string, AssetLocation> val in baseShape.Textures)
				{
					string textureCode = val.Key;
					CompositeTexture tex;
					if (item.Textures.TryGetValue(textureCode, out tex))
					{
						if (tex.Base.Path == "inherit")
						{
							tex.Base = val.Value.Clone();
						}
						if (tex.BlendedOverlays != null)
						{
							BlendedOverlayTexture[] BlendedOverlays = tex.BlendedOverlays;
							for (int i = 0; i < BlendedOverlays.Length; i++)
							{
								if (BlendedOverlays[i].Base.Path == "inherit")
								{
									BlendedOverlays[i].Base = val.Value.Clone();
								}
							}
						}
					}
					else
					{
						item.Textures[textureCode] = new CompositeTexture
						{
							Base = val.Value.Clone()
						};
					}
				}
			}
		}
	}
}
