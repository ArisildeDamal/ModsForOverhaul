using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Vintagestory.Client.NoObf
{
	public class EntityTextureAtlasManager : TextureAtlasManager, ITextureAtlasAPI
	{
		public EntityTextureAtlasManager(ClientMain game)
			: base(game)
		{
		}

		internal void CollectTextures(List<EntityProperties> entityClasses)
		{
			CompositeTexture unknown = new CompositeTexture(new AssetLocation("unknown"));
			foreach (EntityProperties entityType in entityClasses)
			{
				if (this.game.disposed)
				{
					return;
				}
				if (entityType != null && entityType.Client != null)
				{
					EntityClientProperties clientConf = entityType.Client;
					IDictionary<string, CompositeTexture> collectedTextures = new FastSmallDictionary<string, CompositeTexture>(1);
					if (clientConf.Textures == null)
					{
						Shape loadedShape = clientConf.LoadedShape;
						if (((loadedShape != null) ? loadedShape.Textures : null) == null)
						{
							clientConf.Textures["all"] = unknown;
						}
					}
					Shape loadedShape2 = clientConf.LoadedShape;
					if (((loadedShape2 != null) ? loadedShape2.Textures : null) != null)
					{
						this.LoadShapeTextures(collectedTextures, clientConf.LoadedShape, clientConf.Shape);
					}
					if (clientConf.LoadedAlternateShapes != null)
					{
						for (int i = 0; i < clientConf.LoadedAlternateShapes.Length; i++)
						{
							Shape shape = clientConf.LoadedAlternateShapes[i];
							CompositeShape cshape = clientConf.Shape.Alternates[i];
							if (((shape != null) ? shape.Textures : null) != null)
							{
								this.LoadShapeTextures(collectedTextures, shape, cshape);
							}
						}
					}
					this.ResolveTextureCodes(clientConf, clientConf.LoadedShape);
					if (clientConf.Textures != null)
					{
						foreach (KeyValuePair<string, CompositeTexture> val in clientConf.Textures)
						{
							val.Value.Bake(this.game.AssetManager);
							if (val.Value.Baked.BakedVariants != null)
							{
								for (int j = 0; j < val.Value.Baked.BakedVariants.Length; j++)
								{
									base.GetOrAddTextureLocation(new AssetLocationAndSource(val.Value.Baked.BakedVariants[j].BakedName, "Entity type ", entityType.Code, -1));
								}
							}
							base.GetOrAddTextureLocation(new AssetLocationAndSource(val.Value.Base, "Entity type ", entityType.Code, -1));
							collectedTextures[val.Key] = val.Value;
						}
					}
					clientConf.Textures = collectedTextures;
				}
			}
			foreach (EntityProperties entityClass in entityClasses)
			{
				if (entityClass != null && entityClass.Client != null)
				{
					foreach (KeyValuePair<string, CompositeTexture> val2 in entityClass.Client.Textures)
					{
						BakedCompositeTexture bct = val2.Value.Baked;
						bct.TextureSubId = this.textureNamesDict[val2.Value.Baked.BakedName];
						if (bct.BakedVariants != null)
						{
							for (int k = 0; k < bct.BakedVariants.Length; k++)
							{
								bct.BakedVariants[k].TextureSubId = this.textureNamesDict[bct.BakedVariants[k].BakedName];
							}
						}
					}
				}
			}
		}

		private void LoadShapeTextures(IDictionary<string, CompositeTexture> collectedTextures, Shape shape, CompositeShape cshape)
		{
			foreach (KeyValuePair<string, AssetLocation> val in shape.Textures)
			{
				CompositeTexture ctex = new CompositeTexture
				{
					Base = val.Value
				};
				ctex.Bake(this.game.AssetManager);
				if (ctex.Baked.BakedVariants != null)
				{
					for (int i = 0; i < ctex.Baked.BakedVariants.Length; i++)
					{
						base.GetOrAddTextureLocation(new AssetLocationAndSource(ctex.Baked.BakedVariants[i].BakedName, "Shape file ", cshape.Base, -1));
					}
				}
				else
				{
					base.GetOrAddTextureLocation(new AssetLocationAndSource(val.Value, "Shape file ", cshape.Base, -1));
					collectedTextures[val.Key] = ctex;
				}
			}
		}

		public void ResolveTextureCodes(EntityClientProperties typeClient, Shape shape)
		{
			if (typeClient.Textures.ContainsKey("all"))
			{
				this.LoadShapeTextureCodes(shape);
			}
			base.ResolveTextureDict((FastSmallDictionary<string, CompositeTexture>)typeClient.Textures);
		}
	}
}
