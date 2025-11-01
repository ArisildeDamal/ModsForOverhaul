using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Util;

namespace Vintagestory.Client.NoObf
{
	public class TextureAtlasManager : AsyncHelper.Multithreaded, ITextureAtlasAPI, ITextureLocationDictionary
	{
		public int Count
		{
			get
			{
				return this.textureNamesDict.Count;
			}
		}

		public TextureAtlasPosition UnknownTexturePosition
		{
			get
			{
				return this.UnknownTexturePos;
			}
		}

		public int this[AssetLocationAndSource textureLoc]
		{
			get
			{
				return this.textureNamesDict[textureLoc];
			}
		}

		public Size2i Size { get; set; }

		public TextureAtlasPosition this[AssetLocation textureLocation]
		{
			get
			{
				int textureSubId;
				if (this.textureNamesDict.TryGetValue(textureLocation, out textureSubId))
				{
					return this.TextureAtlasPositionsByTextureSubId[textureSubId];
				}
				return null;
			}
		}

		public float SubPixelPaddingX
		{
			get
			{
				float subPixelPadding = 0f;
				if (this.itemclass == "items")
				{
					subPixelPadding = ClientSettings.ItemAtlasSubPixelPadding / (float)this.Size.Width;
				}
				if (this.itemclass == "blocks")
				{
					subPixelPadding = ClientSettings.BlockAtlasSubPixelPadding / (float)this.Size.Width;
				}
				if (this.itemclass == "entities")
				{
					subPixelPadding = 0f;
				}
				return subPixelPadding;
			}
		}

		public float SubPixelPaddingY
		{
			get
			{
				float subPixelPadding = 0f;
				if (this.itemclass == "items")
				{
					subPixelPadding = ClientSettings.ItemAtlasSubPixelPadding / (float)this.Size.Height;
				}
				if (this.itemclass == "blocks")
				{
					subPixelPadding = ClientSettings.BlockAtlasSubPixelPadding / (float)this.Size.Height;
				}
				if (this.itemclass == "entities")
				{
					subPixelPadding = 0f;
				}
				return subPixelPadding;
			}
		}

		public TextureAtlasPosition[] Positions
		{
			get
			{
				return this.TextureAtlasPositionsByTextureSubId;
			}
		}

		List<LoadedTexture> ITextureAtlasAPI.AtlasTextures
		{
			get
			{
				return this.AtlasTextures;
			}
		}

		public TextureAtlasManager(ClientMain game)
		{
			this.game = game;
			int maxTextureSize = game.Platform.GlGetMaxTextureSize();
			this.Size = new Size2i(GameMath.Clamp(maxTextureSize, 512, ClientSettings.MaxTextureAtlasWidth), GameMath.Clamp(maxTextureSize, 512, ClientSettings.MaxTextureAtlasHeight));
			OrderedDictionary<AssetLocation, int> orderedDictionary = this.textureNamesDict;
			AssetLocation assetLocation = new AssetLocationAndSource("unknown");
			int num = this.textureSubId;
			this.textureSubId = num + 1;
			orderedDictionary[assetLocation] = num;
		}

		public TextureAtlas CreateNewAtlas(string itemclass)
		{
			this.itemclass = itemclass;
			this.currentAtlas = new TextureAtlas(this.Size.Width, this.Size.Height, this.SubPixelPaddingX, this.SubPixelPaddingY);
			this.addCommonTextures();
			this.Atlasses.Add(this.currentAtlas);
			return this.currentAtlas;
		}

		public virtual TextureAtlas RuntimeCreateNewAtlas(string itemclass)
		{
			if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
			{
				throw new InvalidOperationException("Attempting to create an additional texture atlas outside of the main thread. This is not possible as we have only one OpenGL context!");
			}
			TextureAtlas textureAtlas = this.CreateNewAtlas(itemclass);
			LoadedTexture atlasTexture = textureAtlas.Upload(this.game);
			this.AtlasTextures.Add(atlasTexture);
			return textureAtlas;
		}

		private void addCommonTextures()
		{
			foreach (KeyValuePair<AssetLocation, int> val in this.textureNamesDict)
			{
				AssetLocationAndSource textureName = val.Key as AssetLocationAndSource;
				if (textureName.AddToAllAtlasses)
				{
					IAsset asset = this.game.AssetManager.TryGet(textureName, true);
					this.currentAtlas.InsertTexture(val.Value, this.game.api, asset);
				}
			}
		}

		public bool AddTextureLocation(AssetLocationAndSource loc)
		{
			if (this.textureNamesDict.ContainsKey(loc))
			{
				return false;
			}
			OrderedDictionary<AssetLocation, int> orderedDictionary = this.textureNamesDict;
			int num = this.textureSubId;
			this.textureSubId = num + 1;
			orderedDictionary[loc] = num;
			return true;
		}

		public void SetTextureLocation(AssetLocationAndSource loc)
		{
			OrderedDictionary<AssetLocation, int> orderedDictionary = this.textureNamesDict;
			int num = this.textureSubId;
			this.textureSubId = num + 1;
			orderedDictionary[loc] = num;
		}

		public int GetOrAddTextureLocation(AssetLocationAndSource loc)
		{
			int result;
			if (!this.textureNamesDict.TryGetValue(loc, out result))
			{
				int num = this.textureSubId;
				this.textureSubId = num + 1;
				result = num;
				this.textureNamesDict[loc] = result;
			}
			return result;
		}

		public bool ContainsKey(AssetLocation loc)
		{
			return this.textureNamesDict.ContainsKey(loc);
		}

		public void GenFramebuffer()
		{
			this.DisposeFrameBuffer();
			TextureAtlasManager.atlasFramebuffer = this.game.Platform.CreateFramebuffer(new FramebufferAttrs("Render2DLoadedTexture", this.Size.Width, this.Size.Height)
			{
				Attachments = new FramebufferAttrsAttachment[]
				{
					new FramebufferAttrsAttachment
					{
						AttachmentType = EnumFramebufferAttachment.ColorAttachment0,
						Texture = new RawTexture
						{
							Width = this.Size.Width,
							Height = this.Size.Height,
							TextureId = this.AtlasTextures[0].TextureId
						}
					}
				}
			});
		}

		public void RenderTextureIntoAtlas(int atlasTextureId, LoadedTexture fromTexture, float sourceX, float sourceY, float sourceWidth, float sourceHeight, float targetX, float targetY, float alphaTest = 0f)
		{
			if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
			{
				throw new InvalidOperationException("Attempting to insert a texture into the atlas outside of the main thread. This is not possible as we have only one OpenGL context!");
			}
			this.game.RenderTextureIntoFrameBuffer(atlasTextureId, fromTexture, sourceX, sourceY, sourceWidth, sourceHeight, TextureAtlasManager.atlasFramebuffer, targetX, targetY, alphaTest);
		}

		public bool GetOrInsertTexture(AssetLocation path, out int textureSubId, out TextureAtlasPosition texPos, CreateTextureDelegate onCreate = null, float alphaTest = 0f)
		{
			return this.GetOrInsertTexture(new AssetLocationAndSource(path), out textureSubId, out texPos, onCreate, alphaTest);
		}

		public bool GetOrInsertTexture(AssetLocationAndSource loc, out int textureSubId, out TextureAtlasPosition texPos, CreateTextureDelegate onCreate = null, float alphaTest = 0f)
		{
			if (onCreate == null)
			{
				onCreate = delegate
				{
					IBitmap bmp3 = this.LoadCompositeBitmap(loc);
					if (bmp3.Width == 0 && bmp3.Height == 0)
					{
						this.game.Logger.Warning("GetOrInsertTexture() on path {0}: Bitmap width and height is 0! Either missing or corrupt image file. Will use unknown texture.", new object[] { loc });
					}
					return bmp3;
				};
			}
			if (this.textureNamesDict.TryGetValue(loc, out textureSubId))
			{
				texPos = this.TextureAtlasPositionsByTextureSubId[textureSubId];
				if ((int)texPos.reloadIteration != this.reloadIteration)
				{
					IBitmap bmp = onCreate();
					if (bmp == null)
					{
						return false;
					}
					this.runtimeUpdateTexture(bmp, texPos, alphaTest);
				}
				return true;
			}
			texPos = null;
			textureSubId = 0;
			IBitmap bmp2 = onCreate();
			if (bmp2 == null)
			{
				return false;
			}
			bool flag = this.InsertTexture(bmp2, out textureSubId, out texPos, alphaTest);
			if (flag)
			{
				this.textureNamesDict[loc] = textureSubId;
			}
			return flag;
		}

		[Obsolete("Use GetOrInsertTexture() instead. It's more efficient to load the bmp only if the texture was not found in the cache")]
		public bool InsertTextureCached(AssetLocation path, IBitmap bmp, out int textureSubId, out TextureAtlasPosition texPos, float alphaTest = 0f)
		{
			AssetLocationAndSource loc = new AssetLocationAndSource(path);
			if (this.textureNamesDict.TryGetValue(loc, out textureSubId))
			{
				texPos = this.TextureAtlasPositionsByTextureSubId[textureSubId];
				if ((int)texPos.reloadIteration != this.reloadIteration)
				{
					this.runtimeUpdateTexture(bmp, texPos, alphaTest);
				}
				return true;
			}
			bool flag = this.InsertTexture(bmp, out textureSubId, out texPos, alphaTest);
			if (flag)
			{
				this.textureNamesDict[loc] = textureSubId;
			}
			return flag;
		}

		public bool InsertTexture(IBitmap bmp, out int textureSubId, out TextureAtlasPosition texPos, float alphaTest = 0f)
		{
			if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
			{
				throw new InvalidOperationException("Attempting to insert a texture into the atlas outside of the main thread. This is not possible as we have only one OpenGL context!");
			}
			if (!this.AllocateTextureSpace(bmp.Width, bmp.Height, out textureSubId, out texPos, null))
			{
				return false;
			}
			this.runtimeUpdateTexture(bmp, texPos, alphaTest);
			return true;
		}

		private void runtimeUpdateTexture(IBitmap bmp, TextureAtlasPosition texPos, float alphaTest = 0f)
		{
			if (alphaTest < 0.0001f)
			{
				this.game.Platform.LoadIntoTexture(bmp, texPos.atlasTextureId, (int)(texPos.x1 * (float)this.Size.Width), (int)(texPos.y1 * (float)this.Size.Height), false);
			}
			else
			{
				bool glScissorFlagEnabled = this.game.Platform.GlScissorFlagEnabled;
				if (glScissorFlagEnabled)
				{
					this.game.Platform.GlScissorFlag(false);
				}
				this.game.Platform.GlToggleBlend(false, EnumBlendMode.Standard);
				LoadedTexture tex = new LoadedTexture(this.game.api, this.game.Platform.LoadTexture(bmp, false, 0, false), bmp.Width, bmp.Height);
				this.RenderTextureIntoAtlas(texPos.atlasTextureId, tex, 0f, 0f, (float)bmp.Width, (float)bmp.Height, texPos.x1 * (float)this.Size.Width, texPos.y1 * (float)this.Size.Height, alphaTest);
				tex.Dispose();
				if (glScissorFlagEnabled)
				{
					this.game.Platform.GlScissorFlag(true);
				}
			}
			if (this.autoRegenMipMaps && !this.genMipmapsQueued)
			{
				this.genMipmapsQueued = true;
				Action <>9__1;
				this.game.EnqueueMainThreadTask(delegate
				{
					ClientMain clientMain = this.game;
					Action action;
					if ((action = <>9__1) == null)
					{
						action = (<>9__1 = delegate
						{
							this.RegenMipMaps((int)texPos.atlasNumber);
							this.genMipmapsQueued = false;
						});
					}
					clientMain.EnqueueMainThreadTask(action, "genmipmaps");
				}, "genmipmaps");
			}
			int wdt = bmp.Width;
			int hgt = bmp.Height;
			TextureAtlasManager.pixelsTmp[0] = bmp.GetPixel((int)(0.35f * (float)wdt), (int)(0.35f * (float)hgt)).ToArgb();
			TextureAtlasManager.pixelsTmp[1] = bmp.GetPixel((int)(0.65f * (float)wdt), (int)(0.35f * (float)hgt)).ToArgb();
			TextureAtlasManager.pixelsTmp[2] = bmp.GetPixel((int)(0.35f * (float)wdt), (int)(0.65f * (float)hgt)).ToArgb();
			TextureAtlasManager.pixelsTmp[3] = bmp.GetPixel((int)(0.65f * (float)wdt), (int)(0.65f * (float)hgt)).ToArgb();
			texPos.AvgColor = ColorUtil.ReverseColorBytes(ColorUtil.ColorAverage(TextureAtlasManager.pixelsTmp, TextureAtlasManager.equalWeight));
			texPos.RndColors = new int[30];
			for (int i = 0; i < 30; i++)
			{
				int color = 0;
				for (int j = 0; j < 15; j++)
				{
					color = bmp.GetPixel((int)(this.rand.NextDouble() * (double)wdt), (int)(this.rand.NextDouble() * (double)hgt)).ToArgb();
					if (((color >> 24) & 255) > 5)
					{
						break;
					}
				}
				texPos.RndColors[i] = color;
			}
		}

		public void RegenMipMaps(int atlasNumber)
		{
			this.game.Platform.BuildMipMaps(this.AtlasTextures[atlasNumber].TextureId);
		}

		public bool InsertTexture(BitmapRef bmp, AssetLocationAndSource loc, out int textureSubIdOut)
		{
			if (bmp.Width > this.Size.Width || bmp.Height > this.Size.Height)
			{
				throw new InvalidOperationException("Cannot insert texture larger than the atlas itself");
			}
			textureSubIdOut = this.GetOrAddTextureLocation(loc);
			bool added = this.currentAtlas.InsertTexture(textureSubIdOut, bmp.Width, bmp.Height, bmp.Pixels);
			if (!added)
			{
				this.RuntimeCreateNewAtlas(this.itemclass);
				return this.currentAtlas.InsertTexture(textureSubIdOut, bmp.Width, bmp.Height, bmp.Pixels);
			}
			return added;
		}

		public bool AllocateTextureSpace(int width, int height, out int textureSubId, out TextureAtlasPosition texPos, AssetLocationAndSource loc = null)
		{
			if (width > this.Size.Width || height > this.Size.Height)
			{
				throw new InvalidOperationException("Cannot create allocate texture space larger than the atlas itself");
			}
			int num;
			if (!(loc == null))
			{
				num = this.GetOrAddTextureLocation(loc);
			}
			else
			{
				int num2 = this.textureSubId;
				this.textureSubId = num2 + 1;
				num = num2;
			}
			textureSubId = num;
			TextureAtlasPosition tp = null;
			int i = 0;
			foreach (TextureAtlas textureAtlas in this.Atlasses)
			{
				tp = textureAtlas.AllocateTextureSpace(textureSubId, width, height);
				if (tp != null)
				{
					break;
				}
				i++;
			}
			if (tp == null)
			{
				tp = this.RuntimeCreateNewAtlas(this.itemclass).AllocateTextureSpace(textureSubId, width, height);
			}
			tp.atlasNumber = (byte)i;
			tp.atlasTextureId = this.AtlasTextures[i].TextureId;
			texPos = tp;
			if (textureSubId < this.TextureAtlasPositionsByTextureSubId.Length)
			{
				this.TextureAtlasPositionsByTextureSubId[textureSubId] = texPos;
			}
			else
			{
				this.TextureAtlasPositionsByTextureSubId = this.TextureAtlasPositionsByTextureSubId.Append(texPos);
			}
			return true;
		}

		public void FreeTextureSpace(int textureSubId)
		{
			using (List<TextureAtlas>.Enumerator enumerator = this.Atlasses.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current.FreeTextureSpace(textureSubId))
					{
						break;
					}
				}
			}
		}

		public void FreeTextureSpace(AssetLocationAndSource loc)
		{
			int textureSubId;
			if (this.textureNamesDict.TryGetValue(loc, out textureSubId))
			{
				this.textureNamesDict.Remove(loc);
				this.FreeTextureSpace(textureSubId);
			}
		}

		public virtual void PopulateTextureAtlassesFromTextures()
		{
			this.TextureAtlasPositionsByTextureSubId = new TextureAtlasPosition[this.textureNamesDict.Count];
			BakedBitmap[] bitmaps = new BakedBitmap[this.textureNamesDict.Count];
			if (this.itemclass != "entities")
			{
				base.StartWorkerThread(delegate
				{
					this.LoadBitmaps(bitmaps);
				});
			}
			this.LoadBitmaps(bitmaps);
			while (base.WorkerThreadsInProgress() && !this.game.disposed)
			{
				Thread.Sleep(10);
			}
			this.addCommonTextures();
			foreach (KeyValuePair<AssetLocation, int> val in this.textureNamesDict)
			{
				int textureSubId = val.Value;
				BakedBitmap bcBmp = bitmaps[textureSubId];
				if (bcBmp != null && !(val.Key as AssetLocationAndSource).AddToAllAtlasses && !this.currentAtlas.InsertTexture(textureSubId, bcBmp.Width, bcBmp.Height, bcBmp.TexturePixels))
				{
					this.CreateNewAtlas(this.itemclass);
					if (!this.currentAtlas.InsertTexture(textureSubId, bcBmp.Width, bcBmp.Height, bcBmp.TexturePixels))
					{
						throw new Exception("Texture bigger than max supported texture size!");
					}
				}
			}
			this.FinishedOverlays();
		}

		private void LoadBitmaps(BakedBitmap[] bitmaps)
		{
			foreach (KeyValuePair<AssetLocation, int> val in this.textureNamesDict)
			{
				AssetLocationAndSource textureName = val.Key as AssetLocationAndSource;
				if (AsyncHelper.CanProceedOnThisThread(ref textureName.loadedAlready))
				{
					int textureSubId = val.Value;
					BakedBitmap bcBmp = TextureAtlasManager.LoadCompositeBitmap(this.game, textureName, this.overlayTextures);
					bitmaps[textureSubId] = bcBmp;
				}
			}
		}

		public virtual void ComposeTextureAtlasses_StageA()
		{
			this.AtlasTextures = new List<LoadedTexture>();
			foreach (TextureAtlas textureAtlas in this.Atlasses)
			{
				LoadedTexture texture = textureAtlas.Upload(this.game);
				this.AtlasTextures.Add(texture);
			}
			this.game.Platform.Logger.Notification("Composed {0} {1}x{2} " + this.itemclass + " texture atlases from {3} textures", new object[]
			{
				this.AtlasTextures.Count,
				this.Size.Width,
				this.Size.Height,
				this.textureNamesDict.Count
			});
		}

		public virtual void ComposeTextureAtlasses_StageB()
		{
			foreach (TextureAtlas atlas in this.Atlasses)
			{
				this.game.Platform.BuildMipMaps(atlas.textureId);
			}
		}

		public virtual void ComposeTextureAtlasses_StageC()
		{
			int atlasId = 0;
			foreach (TextureAtlas textureAtlas in this.Atlasses)
			{
				textureAtlas.PopulateAtlasPositions(this.TextureAtlasPositionsByTextureSubId, atlasId++);
			}
			this.UnknownTexturePos = this.TextureAtlasPositionsByTextureSubId[0];
			for (int texSubId = 0; texSubId < this.TextureAtlasPositionsByTextureSubId.Length; texSubId++)
			{
				TextureAtlasPosition texPos = this.TextureAtlasPositionsByTextureSubId[texSubId];
				TextureAtlas atlas = this.Atlasses[(int)texPos.atlasNumber];
				float texWidth = texPos.x2 - texPos.x1;
				float texHeight = texPos.y2 - texPos.y1;
				TextureAtlasManager.pixelsTmp[0] = atlas.GetPixel(texPos.x1 + 0.35f * texWidth, texPos.y1 + 0.35f * texHeight);
				TextureAtlasManager.pixelsTmp[1] = atlas.GetPixel(texPos.x1 + 0.65f * texWidth, texPos.y1 + 0.35f * texHeight);
				TextureAtlasManager.pixelsTmp[2] = atlas.GetPixel(texPos.x1 + 0.35f * texWidth, texPos.y1 + 0.65f * texHeight);
				TextureAtlasManager.pixelsTmp[3] = atlas.GetPixel(texPos.x1 + 0.65f * texWidth, texPos.y1 + 0.65f * texHeight);
				texPos.AvgColor = ColorUtil.ReverseColorBytes(ColorUtil.ColorAverage(TextureAtlasManager.pixelsTmp, TextureAtlasManager.equalWeight));
				texPos.RndColors = new int[30];
				for (int i = 0; i < 30; i++)
				{
					int color = 0;
					for (int j = 0; j < 15; j++)
					{
						color = atlas.GetPixel((float)((double)texPos.x1 + this.rand.NextDouble() * (double)texWidth), (float)((double)texPos.y1 + this.rand.NextDouble() * (double)texHeight));
						if (((color >> 24) & 255) > 5)
						{
							break;
						}
					}
					texPos.RndColors[i] = color;
				}
			}
			foreach (TextureAtlas textureAtlas2 in this.Atlasses)
			{
				textureAtlas2.DisposePixels();
			}
		}

		public virtual TextureAtlasManager ReloadTextures()
		{
			this.reloadIteration++;
			foreach (TextureAtlas atlas in this.Atlasses)
			{
				atlas.ReinitPixels();
				foreach (KeyValuePair<AssetLocation, int> val in this.textureNamesDict)
				{
					TextureAtlasPosition tpos = this.TextureAtlasPositionsByTextureSubId[val.Value];
					AssetLocationAndSource textureName = val.Key as AssetLocationAndSource;
					if (textureName.AddToAllAtlasses)
					{
						this.game.AssetManager.TryGet(textureName, true);
						BitmapRef colormap = this.game.Platform.CreateBitmapFromPng(this.game.AssetManager.Get(textureName));
						atlas.UpdateTexture(tpos, colormap.Pixels);
					}
				}
			}
			foreach (KeyValuePair<AssetLocation, int> val2 in this.textureNamesDict)
			{
				TextureAtlasPosition tpos2 = this.TextureAtlasPositionsByTextureSubId[val2.Value];
				AssetLocationAndSource textureName2 = val2.Key as AssetLocationAndSource;
				if (!textureName2.AddToAllAtlasses)
				{
					int[] pixels;
					if (textureName2.loadedAlready == 2)
					{
						pixels = this.game.Platform.CreateBitmapFromPng(this.game.AssetManager.Get(textureName2)).Pixels;
					}
					else
					{
						BakedBitmap bcBmp = TextureAtlasManager.LoadCompositeBitmap(this.game, textureName2, this.overlayTextures);
						int w = (int)Math.Round((double)((tpos2.x2 - tpos2.x1 + 2f * this.SubPixelPaddingX) * (float)this.Size.Width));
						int h = (int)Math.Round((double)((tpos2.y2 - tpos2.y1 + 2f * this.SubPixelPaddingY) * (float)this.Size.Height));
						if (w != bcBmp.Width || h != bcBmp.Height)
						{
							this.game.Platform.Logger.Error("Texture {0} changed in size ({1}x{2} => {3}x{4}). Runtime reload with changing texture sizes is not supported. Will not update.", new object[] { textureName2, w, h, bcBmp.Width, bcBmp.Height });
							continue;
						}
						pixels = bcBmp.Pixels;
					}
					this.Atlasses[(int)tpos2.atlasNumber].UpdateTexture(tpos2, pixels);
				}
			}
			this.FinishedOverlays();
			for (int i = 0; i < this.Atlasses.Count; i++)
			{
				LoadedTexture texAtlas = this.AtlasTextures[i];
				this.Atlasses[i].DrawToTexture(this.game.Platform, texAtlas);
				this.Atlasses[i].DisposePixels();
			}
			return this;
		}

		private void FinishedOverlays()
		{
			foreach (BitmapRef bitmapRef in this.overlayTextures.Values)
			{
				if (bitmapRef != null)
				{
					bitmapRef.Dispose();
				}
			}
			this.overlayTextures.Clear();
		}

		public virtual TextureAtlasManager PauseRegenMipmaps()
		{
			this.autoRegenMipMaps = false;
			return this;
		}

		public virtual TextureAtlasManager ResumeRegenMipmaps()
		{
			this.autoRegenMipMaps = true;
			for (int i = 0; i < this.Atlasses.Count; i++)
			{
				this.RegenMipMaps(i);
			}
			return this;
		}

		public IBitmap LoadCompositeBitmap(AssetLocationAndSource path)
		{
			return TextureAtlasManager.LoadCompositeBitmap(this.game, path);
		}

		public static BakedBitmap LoadCompositeBitmap(ClientMain game, string compositeTextureName)
		{
			return TextureAtlasManager.LoadCompositeBitmap(game, new AssetLocationAndSource(compositeTextureName));
		}

		public static AssetLocationAndSource ToTextureAssetLocation(AssetLocationAndSource loc)
		{
			AssetLocationAndSource assetLocationAndSource = new AssetLocationAndSource(loc.Domain, "textures/" + loc.Path, loc.Source);
			assetLocationAndSource.Path = assetLocationAndSource.Path.Replace("@90", "").Replace("@180", "").Replace("@270", "");
			assetLocationAndSource.Path = Regex.Replace(assetLocationAndSource.Path, "å\\d+", "");
			assetLocationAndSource.WithPathAppendixOnce(".png");
			return assetLocationAndSource;
		}

		public static int getRotation(AssetLocationAndSource loc)
		{
			if (loc.Path.Contains("@90"))
			{
				return 90;
			}
			if (loc.Path.Contains("@180"))
			{
				return 180;
			}
			if (loc.Path.Contains("@270"))
			{
				return 270;
			}
			return 0;
		}

		public static int getAlpha(AssetLocationAndSource tex)
		{
			int index = tex.Path.IndexOf('å');
			if (index < 0)
			{
				return 255;
			}
			return tex.Path.Substring(index + 1, Math.Min(tex.Path.Length - index - 1, 3)).ToInt(255);
		}

		public static BakedBitmap LoadCompositeBitmap(ClientMain game, AssetLocationAndSource compositeTextureLocation)
		{
			return TextureAtlasManager.LoadCompositeBitmap(game, compositeTextureLocation, null);
		}

		public static BakedBitmap LoadCompositeBitmap(ClientMain game, AssetLocationAndSource compositeTextureLocation, Dictionary<AssetLocation, BitmapRef> cache)
		{
			BakedBitmap bcBmp = new BakedBitmap();
			int rot = TextureAtlasManager.getRotation(compositeTextureLocation);
			int alpha = TextureAtlasManager.getAlpha(compositeTextureLocation);
			if (!compositeTextureLocation.Path.Contains("++"))
			{
				int readWidth;
				int readHeight;
				bcBmp.TexturePixels = TextureAtlasManager.LoadBitmapPixels(game, compositeTextureLocation, rot, alpha, null, out readWidth, out readHeight);
				bcBmp.Width = readWidth;
				bcBmp.Height = readHeight;
				return bcBmp;
			}
			string[] parts = compositeTextureLocation.ToString().Split(new string[] { "++" }, StringSplitOptions.None);
			for (int i = 0; i < parts.Length; i++)
			{
				string[] subparts = parts[i].Split('~', StringSplitOptions.None);
				EnumColorBlendMode mode = (EnumColorBlendMode)((subparts.Length > 1) ? subparts[0].ToInt(0) : 0);
				AssetLocation loc = AssetLocation.Create((subparts.Length > 1) ? subparts[1] : subparts[0], compositeTextureLocation.Domain);
				if (rot != 0)
				{
					loc.WithPathAppendixOnce("@" + rot.ToString());
				}
				AssetLocationAndSource partTexture = new AssetLocationAndSource(loc, compositeTextureLocation.Source);
				int readWidth2;
				int readHeight2;
				int[] texturePixels = TextureAtlasManager.LoadBitmapPixels(game, partTexture, rot, alpha, cache, out readWidth2, out readHeight2);
				if (bcBmp.TexturePixels == null)
				{
					bcBmp.TexturePixels = texturePixels;
					bcBmp.Width = readWidth2;
					bcBmp.Height = readHeight2;
				}
				else if (bcBmp.Width != readWidth2 || bcBmp.Height != readHeight2)
				{
					game.Platform.Logger.Warning("Textureoverlay {0} ({2}x{3} pixel) is not the same width and height as base texture in composite texture {1} ({4}x{5} pixel), ignoring.", new object[] { partTexture, compositeTextureLocation, readWidth2, readHeight2, bcBmp.Width, bcBmp.Height });
				}
				else
				{
					for (int p = 0; p < bcBmp.TexturePixels.Length; p++)
					{
						bcBmp.TexturePixels[p] = ColorBlend.Blend(mode, bcBmp.TexturePixels[p], texturePixels[p]);
					}
				}
			}
			return bcBmp;
		}

		private static int[] LoadBitmapPixels(ClientMain game, AssetLocationAndSource source, int rot, int alpha, Dictionary<AssetLocation, BitmapRef> cache, out int readWidth, out int readHeight)
		{
			BitmapRef bmp;
			if (cache != null)
			{
				lock (cache)
				{
					if (!cache.TryGetValue(source, out bmp))
					{
						bmp = TextureAtlasManager.LoadBitmap(game, TextureAtlasManager.ToTextureAssetLocation(source));
						cache.Add(source, bmp);
					}
					goto IL_004C;
				}
			}
			bmp = TextureAtlasManager.LoadBitmap(game, TextureAtlasManager.ToTextureAssetLocation(source));
			IL_004C:
			if (bmp == null)
			{
				readWidth = 0;
				readHeight = 0;
				return null;
			}
			int[] pixelsTransformed = bmp.GetPixelsTransformed(rot, alpha);
			bool cw = rot % 180 == 90;
			readWidth = (cw ? bmp.Height : bmp.Width);
			readHeight = (cw ? bmp.Width : bmp.Height);
			if (cache == null)
			{
				bmp.Dispose();
			}
			return pixelsTransformed;
		}

		public static BitmapRef LoadBitmap(ClientMain game, AssetLocationAndSource textureLoc)
		{
			if (textureLoc == null)
			{
				return null;
			}
			IAsset asset = null;
			BitmapRef bitmapRef;
			try
			{
				asset = game.AssetManager.TryGet(textureLoc, true);
				byte[] fileData;
				if (asset == null)
				{
					game.Logger.Warning("Texture asset '{0}' not found (defined in {1}).", new object[] { textureLoc, textureLoc.Source });
					fileData = game.AssetManager.Get("textures/unknown.png").Data;
				}
				else
				{
					fileData = asset.Data;
				}
				BitmapRef bmp = game.Platform.CreateBitmapFromPng(fileData, fileData.Length);
				if (bmp.Width / 4 * 4 != bmp.Width)
				{
					game.Platform.Logger.Warning("Texture {0} width is not divisible by 4, will probably glitch when mipmapped", new object[] { textureLoc });
				}
				else if (bmp.Height / 4 * 4 != bmp.Height)
				{
					game.Platform.Logger.Warning("Texture {0} height is not divisible by 4, will probably glitch when mipmapped", new object[] { textureLoc });
				}
				bitmapRef = bmp;
			}
			catch (Exception)
			{
				game.Logger.Notification("The quest as to why Fulgen crashes here.");
				game.Logger.Notification("textureLoc={0}", new object[] { textureLoc });
				game.Logger.Notification("asset={0}", new object[] { asset });
				throw;
			}
			return bitmapRef;
		}

		public virtual void LoadShapeTextureCodes(Shape shape)
		{
			this.textureCodes.Clear();
			if (shape == null)
			{
				return;
			}
			foreach (ShapeElement elem in shape.Elements)
			{
				this.AddTexturesForElement(elem);
			}
		}

		private void AddTexturesForElement(ShapeElement elem)
		{
			foreach (ShapeElementFace face in elem.FacesResolved)
			{
				if (face != null && face.Texture.Length > 0)
				{
					this.textureCodes.Add(face.Texture);
				}
			}
			if (elem.Children != null)
			{
				foreach (ShapeElement child in elem.Children)
				{
					this.AddTexturesForElement(child);
				}
			}
		}

		public void ResolveTextureDict(FastSmallDictionary<string, CompositeTexture> texturesDict)
		{
			CompositeTexture ct;
			if (texturesDict.TryGetValue("sides", out ct))
			{
				texturesDict.AddIfNotPresent("west", ct);
				texturesDict.AddIfNotPresent("east", ct);
				texturesDict.AddIfNotPresent("north", ct);
				texturesDict.AddIfNotPresent("south", ct);
				texturesDict.AddIfNotPresent("up", ct);
				texturesDict.AddIfNotPresent("down", ct);
			}
			if (texturesDict.TryGetValue("horizontals", out ct))
			{
				texturesDict.AddIfNotPresent("west", ct);
				texturesDict.AddIfNotPresent("east", ct);
				texturesDict.AddIfNotPresent("north", ct);
				texturesDict.AddIfNotPresent("south", ct);
			}
			if (texturesDict.TryGetValue("verticals", out ct))
			{
				texturesDict.AddIfNotPresent("up", ct);
				texturesDict.AddIfNotPresent("down", ct);
			}
			if (texturesDict.TryGetValue("westeast", out ct))
			{
				texturesDict.AddIfNotPresent("west", ct);
				texturesDict.AddIfNotPresent("east", ct);
			}
			if (texturesDict.TryGetValue("northsouth", out ct))
			{
				texturesDict.AddIfNotPresent("north", ct);
				texturesDict.AddIfNotPresent("south", ct);
			}
			if (texturesDict.TryGetValue("all", out ct))
			{
				texturesDict.Remove("all");
				foreach (string textureCode in this.textureCodes)
				{
					texturesDict.AddIfNotPresent(textureCode, ct);
				}
			}
		}

		public virtual void Dispose()
		{
			foreach (TextureAtlas textureAtlas in this.Atlasses)
			{
				textureAtlas.DisposePixels();
			}
			if (this.AtlasTextures == null)
			{
				return;
			}
			for (int i = 0; i < this.AtlasTextures.Count; i++)
			{
				this.AtlasTextures[i].Dispose();
			}
			this.AtlasTextures.Clear();
			this.DisposeFrameBuffer();
		}

		private void DisposeFrameBuffer()
		{
			if (TextureAtlasManager.atlasFramebuffer != null)
			{
				this.game.Platform.DisposeFrameBuffer(TextureAtlasManager.atlasFramebuffer, false);
			}
			TextureAtlasManager.atlasFramebuffer = null;
		}

		public int GetRandomColor(int textureSubId)
		{
			return this.TextureAtlasPositionsByTextureSubId[textureSubId].RndColors[this.rand.Next(30)];
		}

		public int GetRandomColor(int textureSubId, int rndIndex)
		{
			TextureAtlasPosition texPos = this.TextureAtlasPositionsByTextureSubId[textureSubId];
			return this.GetRandomColor(texPos, rndIndex);
		}

		public int GetRandomColor(TextureAtlasPosition texPos, int rndIndex)
		{
			if (rndIndex < 0)
			{
				rndIndex = this.rand.Next(30);
			}
			return texPos.RndColors[rndIndex];
		}

		public int[] GetRandomColors(TextureAtlasPosition texPos)
		{
			return texPos.RndColors;
		}

		public int GetAverageColor(int textureSubId)
		{
			return this.TextureAtlasPositionsByTextureSubId[textureSubId].AvgColor;
		}

		public bool InsertTexture(byte[] bytes, out int textureSubId, out TextureAtlasPosition texPos, float alphaTest = 0f)
		{
			BitmapExternal bitmap = this.game.api.Render.BitmapCreateFromPng(bytes);
			return this.InsertTexture(bitmap, out textureSubId, out texPos, alphaTest);
		}

		public bool InsertTextureCached(AssetLocation path, byte[] bytes, out int textureSubId, out TextureAtlasPosition texPos, float alphaTest = 0f)
		{
			BitmapExternal bitmap = this.game.api.Render.BitmapCreateFromPng(bytes);
			return this.GetOrInsertTexture(path, out textureSubId, out texPos, () => bitmap, alphaTest);
		}

		public bool GetOrInsertTexture(CompositeTexture ct, out int textureSubId, out TextureAtlasPosition texPos, float alphaTest = 0f)
		{
			ct.Bake(this.game.AssetManager);
			AssetLocationAndSource alocs = new AssetLocationAndSource(ct.Baked.BakedName, "Shape file ", ct.Base, -1);
			return this.GetOrInsertTexture(ct.Baked.BakedName, out textureSubId, out texPos, () => TextureAtlasManager.LoadCompositeBitmap(this.game, alocs), alphaTest);
		}

		public virtual void CollectAndBakeTexturesFromShape(Shape compositeShape, IDictionary<string, CompositeTexture> targetDict, AssetLocation baseLoc)
		{
			throw new NotImplementedException();
		}

		internal const int UnknownTextureSubId = 0;

		public static FrameBufferRef atlasFramebuffer;

		private static float[] equalWeight = new float[] { 0.25f, 0.25f, 0.25f, 0.25f };

		private static int[] pixelsTmp = new int[4];

		public List<TextureAtlas> Atlasses = new List<TextureAtlas>();

		public List<LoadedTexture> AtlasTextures;

		public TextureAtlasPosition[] TextureAtlasPositionsByTextureSubId;

		public TextureAtlasPosition UnknownTexturePos;

		protected OrderedDictionary<AssetLocation, int> textureNamesDict = new OrderedDictionary<AssetLocation, int>();

		protected int reloadIteration;

		protected ClientMain game;

		protected Random rand = new Random();

		protected int textureSubId;

		protected HashSet<string> textureCodes = new HashSet<string>();

		private string itemclass;

		private TextureAtlas currentAtlas;

		private Dictionary<AssetLocation, BitmapRef> overlayTextures = new Dictionary<AssetLocation, BitmapRef>();

		private bool genMipmapsQueued;

		private bool autoRegenMipMaps = true;
	}
}
