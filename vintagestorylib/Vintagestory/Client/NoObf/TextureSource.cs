using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class TextureSource : ITexPositionSource
	{
		public Size2i AtlasSize
		{
			get
			{
				return this.atlasSize;
			}
		}

		public TextureSource(ClientMain game, Size2i atlasSize, Block block, bool forInventory = false)
		{
			this.game = game;
			this.atlasSize = atlasSize;
			this.block = block;
			this.atlasMgr = game.BlockAtlasManager;
			try
			{
				IDictionary<string, CompositeTexture> textures = block.Textures;
				if (forInventory)
				{
					textures = block.TexturesInventory;
				}
				this.textureCodeToIdMapping = new MiniDictionary(textures.Count);
				foreach (KeyValuePair<string, CompositeTexture> val in textures)
				{
					this.textureCodeToIdMapping[val.Key] = val.Value.Baked.TextureSubId;
				}
			}
			catch (Exception)
			{
				game.Logger.Error("Unable to initialize TextureSource for block {0}. Will crash now.", new object[] { (block != null) ? block.Code : null });
				throw;
			}
		}

		public TextureSource(ClientMain game, Size2i atlasSize, Item item)
		{
			this.game = game;
			this.atlasSize = atlasSize;
			this.item = item;
			this.atlasMgr = game.ItemAtlasManager;
			Dictionary<string, CompositeTexture> textures = item.Textures;
			this.textureCodeToIdMapping = new MiniDictionary(textures.Count);
			foreach (KeyValuePair<string, CompositeTexture> val in textures)
			{
				this.textureCodeToIdMapping[val.Key] = val.Value.Baked.TextureSubId;
			}
		}

		public TextureSource(ClientMain game, Size2i atlasSize, Entity entity, Dictionary<string, CompositeTexture> extraTextures = null, int altTextureNumber = 0)
		{
			this.game = game;
			this.atlasSize = atlasSize;
			this.entity = entity;
			this.atlasMgr = game.EntityAtlasManager;
			IDictionary<string, CompositeTexture> textures = entity.Properties.Client.Textures;
			this.textureCodeToIdMapping = new MiniDictionary(textures.Count);
			foreach (KeyValuePair<string, CompositeTexture> val in textures)
			{
				BakedCompositeTexture bct = val.Value.Baked;
				if (bct.BakedVariants == null)
				{
					this.textureCodeToIdMapping[val.Key] = bct.TextureSubId;
				}
				else
				{
					BakedCompositeTexture bctVariant = bct.BakedVariants[altTextureNumber % bct.BakedVariants.Length];
					this.textureCodeToIdMapping[val.Key] = bctVariant.TextureSubId;
				}
			}
			if (extraTextures != null)
			{
				foreach (KeyValuePair<string, CompositeTexture> val2 in extraTextures)
				{
					extraTextures[val2.Key] = val2.Value;
				}
			}
		}

		public TextureSource(ClientMain game, Size2i atlasSize, Block block, int altTextureNumber)
			: this(game, atlasSize, block, false)
		{
			if (altTextureNumber == -1)
			{
				return;
			}
			foreach (KeyValuePair<string, CompositeTexture> val in block.Textures)
			{
				BakedCompositeTexture bct = val.Value.Baked;
				if (bct.BakedVariants != null)
				{
					BakedCompositeTexture bctVariant = bct.BakedVariants[altTextureNumber % bct.BakedVariants.Length];
					this.textureCodeToIdMapping[val.Key] = bctVariant.TextureSubId;
				}
			}
		}

		public void UpdateVariant(Block block, int altTextureNumber)
		{
			foreach (KeyValuePair<string, CompositeTexture> val in block.Textures)
			{
				BakedCompositeTexture[] variants = val.Value.Baked.BakedVariants;
				if (variants != null && variants.Length != 0)
				{
					this.textureCodeToIdMapping[val.Key] = variants[altTextureNumber % variants.Length].TextureSubId;
				}
			}
		}

		public TextureAtlasPosition this[string textureCode]
		{
			get
			{
				if (textureCode == null)
				{
					return this.atlasMgr.UnknownTexturePos;
				}
				int textureSubId = this.textureCodeToIdMapping[textureCode];
				TextureAtlasPosition texPos;
				if (textureSubId == -1 && (this.returnNullWhenMissing || (textureSubId = this.textureCodeToIdMapping["all"]) == -1))
				{
					if (this.returnNullWhenMissing)
					{
						return null;
					}
					if (this.block != null)
					{
						this.game.Platform.Logger.Error(string.Concat(new string[]
						{
							"Missing mapping for texture code #",
							textureCode,
							" during shape tesselation of block ",
							this.block.Code,
							" using shape ",
							this.block.Shape.Base,
							", or one of its alternates"
						}));
					}
					if (this.item != null)
					{
						this.game.Platform.Logger.Error(string.Concat(new string[]
						{
							"Missing mapping for texture code #",
							textureCode,
							" during shape tesselation of item ",
							this.item.Code,
							" using shape ",
							this.item.Shape.Base
						}));
					}
					if (this.entity != null)
					{
						this.game.Platform.Logger.Error(string.Concat(new string[]
						{
							"Missing mapping for texture code #",
							textureCode,
							" during shape tesselation of entity ",
							this.entity.Code,
							" using shape ",
							this.entity.Properties.Client.Shape.Base,
							", or one of its alternates"
						}));
					}
					texPos = this.atlasMgr.UnknownTexturePos;
				}
				else
				{
					texPos = this.atlasMgr.TextureAtlasPositionsByTextureSubId[textureSubId];
				}
				if (this.isDecalUv)
				{
					return new TextureAtlasPosition
					{
						atlasNumber = 0,
						atlasTextureId = 0,
						x1 = 0f,
						y1 = 0f,
						x2 = (texPos.x2 - texPos.x1) * (float)this.atlasMgr.Size.Width / (float)this.atlasSize.Width,
						y2 = (texPos.y2 - texPos.y1) * (float)this.atlasMgr.Size.Height / (float)this.atlasSize.Height
					};
				}
				return texPos;
			}
		}

		private ClientMain game;

		public Size2i atlasSize;

		public Entity entity;

		public Block block;

		public Item item;

		private MiniDictionary textureCodeToIdMapping;

		public bool isDecalUv;

		public bool returnNullWhenMissing;

		internal CompositeShape blockShape;

		public TextureAtlasManager atlasMgr;
	}
}
