using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf
{
	public class TextureAtlas
	{
		public TextureAtlas(int width, int height, float subPixelPaddingx, float subPixelPaddingy)
		{
			this.atlasPixels = new int[width * height];
			this.subPixelPaddingx = subPixelPaddingx;
			this.subPixelPaddingy = subPixelPaddingy;
			this.width = width;
			this.height = height;
			this.rootNode = new TextureAtlasNode(0, 0, width, height);
		}

		public bool InsertTexture(int textureSubId, ICoreClientAPI capi, IAsset asset)
		{
			return this.InsertTexture(textureSubId, asset.ToBitmap(capi), true);
		}

		public bool InsertTexture(int textureSubId, IBitmap bmp, bool copyPixels = true)
		{
			if (copyPixels)
			{
				return this.InsertTexture(textureSubId, bmp.Width, bmp.Height, bmp.Pixels);
			}
			return this.InsertTexture(textureSubId, bmp.Width, bmp.Height, null);
		}

		public bool InsertTexture(int textureSubId, int width, int height, int[] pixels)
		{
			TextureAtlasNode node = this.rootNode.GetFreeNode(textureSubId, width, height);
			if (node != null)
			{
				node.textureSubId = new int?(textureSubId);
				int bX = node.bounds.x1;
				int bY = node.bounds.y1;
				int atlasWidth = this.AtlasWidth();
				if (pixels != null)
				{
					if (pixels.Length % 4 == 0)
					{
						int row = atlasWidth - width;
						int indexBase = bY * atlasWidth + bX - row;
						for (int i = 0; i < pixels.Length; i += 4)
						{
							if (i % width == 0)
							{
								indexBase += row;
							}
							this.atlasPixels[indexBase] = pixels[i];
							this.atlasPixels[indexBase + 1] = pixels[i + 1];
							this.atlasPixels[indexBase + 2] = pixels[i + 2];
							this.atlasPixels[indexBase + 3] = pixels[i + 3];
							indexBase += 4;
						}
					}
					else
					{
						for (int y = 0; y < height; y++)
						{
							int indexBase2 = (bY + y) * atlasWidth + bX;
							for (int x = 0; x < width; x++)
							{
								this.atlasPixels[indexBase2 + x] = pixels[y * width + x];
							}
						}
					}
				}
				return true;
			}
			return false;
		}

		public void UpdateTexture(TextureAtlasPosition tpos, int[] pixels)
		{
			int atlasWidth = this.AtlasWidth();
			int atlasHeight = this.AtlasHeight();
			int x = (int)(tpos.x1 * (float)atlasWidth);
			int y = (int)(tpos.y1 * (float)atlasHeight);
			int w = (int)Math.Round((double)((tpos.x2 - tpos.x1 + 2f * this.subPixelPaddingx) * (float)atlasWidth));
			int h = (int)Math.Round((double)((tpos.y2 - tpos.y1 + 2f * this.subPixelPaddingy) * (float)atlasHeight));
			for (int dx = 0; dx < w; dx++)
			{
				for (int dy = 0; dy < h; dy++)
				{
					this.atlasPixels[(dy + y) * atlasWidth + x + dx] = pixels[dy * w + dx];
				}
			}
		}

		public TextureAtlasPosition AllocateTextureSpace(int textureSubId, int width, int height)
		{
			TextureAtlasNode node = this.rootNode.GetFreeNode(textureSubId, width, height);
			if (node != null)
			{
				node.textureSubId = new int?(textureSubId);
				return new TextureAtlasPosition
				{
					x1 = (float)node.bounds.x1 / (float)this.AtlasWidth(),
					y1 = (float)node.bounds.y1 / (float)this.AtlasHeight(),
					x2 = (float)node.bounds.x2 / (float)this.AtlasWidth(),
					y2 = (float)node.bounds.y2 / (float)this.AtlasHeight()
				};
			}
			return null;
		}

		public bool FreeTextureSpace(int textureSubId)
		{
			return this.FreeTextureSpace(this.rootNode, textureSubId);
		}

		private bool FreeTextureSpace(TextureAtlasNode node, int textureSubId)
		{
			int? textureSubId2 = node.textureSubId;
			if ((textureSubId2.GetValueOrDefault() == textureSubId) & (textureSubId2 != null))
			{
				node.textureSubId = null;
				return true;
			}
			return (node.left != null && this.FreeTextureSpace(node.left, textureSubId)) || (node.right != null && this.FreeTextureSpace(node.right, textureSubId));
		}

		public int AtlasWidth()
		{
			return this.width;
		}

		public int AtlasHeight()
		{
			return this.height;
		}

		public void Export(string filename, ClientMain game, int atlasTextureId)
		{
			ShaderProgramBase oldprog = ShaderProgramBase.CurrentShaderProgram;
			if (oldprog != null)
			{
				oldprog.Stop();
			}
			ShaderProgramGui prog = ShaderPrograms.Gui;
			prog.Use();
			FrameBufferRef fb = game.Platform.CreateFramebuffer(new FramebufferAttrs("PngExport", this.width, this.height)
			{
				Attachments = new FramebufferAttrsAttachment[]
				{
					new FramebufferAttrsAttachment
					{
						AttachmentType = EnumFramebufferAttachment.ColorAttachment0,
						Texture = new RawTexture
						{
							Width = this.width,
							Height = this.height,
							PixelFormat = EnumTexturePixelFormat.Rgba,
							PixelInternalFormat = EnumTextureInternalFormat.Rgba8
						}
					}
				}
			});
			game.Platform.LoadFrameBuffer(fb);
			game.Platform.GlEnableDepthTest();
			game.Platform.GlDisableCullFace();
			game.Platform.GlToggleBlend(true, EnumBlendMode.Standard);
			game.OrthoMode(this.width, this.height, false);
			float[] clearCol = new float[4];
			game.Platform.ClearFrameBuffer(fb, clearCol, true, true);
			game.api.renderapi.Render2DTexture(atlasTextureId, 0f, 0f, (float)this.width, (float)this.height, 50f);
			BitmapRef bitmap = game.Platform.GrabScreenshot(this.width, this.height, false, true, true);
			game.OrthoMode(game.Width, game.Height, false);
			game.Platform.UnloadFrameBuffer(fb);
			game.Platform.DisposeFrameBuffer(fb, true);
			if (File.Exists(filename + ".png"))
			{
				bitmap.Save(filename + "2.png");
			}
			else
			{
				bitmap.Save(filename + ".png");
			}
			prog.Stop();
			if (oldprog != null)
			{
				oldprog.Use();
			}
		}

		public int GetPixel(float x, float y)
		{
			int xi = (int)GameMath.Clamp(x * (float)this.width, 0f, (float)(this.width - 1));
			int yi = (int)GameMath.Clamp(y * (float)this.height, 0f, (float)(this.height - 1));
			return this.atlasPixels[yi * this.width + xi];
		}

		public LoadedTexture Upload(ClientMain game)
		{
			LoadedTexture tex = new LoadedTexture(game.api, 0, this.AtlasWidth(), this.AtlasHeight());
			game.Platform.LoadOrUpdateTextureFromBgra_DeferMipMap(this.atlasPixels, false, 1, ref tex);
			this.textureId = tex.TextureId;
			return tex;
		}

		public void PopulateAtlasPositions(TextureAtlasPosition[] positions, int atlasNumber)
		{
			this.rootNode.PopulateAtlasPositions(positions, this.textureId, atlasNumber, (float)this.AtlasWidth(), (float)this.AtlasHeight(), this.subPixelPaddingx, this.subPixelPaddingy);
		}

		public void DrawToTexture(ClientPlatformAbstract platform, LoadedTexture texAtlas)
		{
			platform.LoadOrUpdateTextureFromBgra(this.atlasPixels, false, 1, ref texAtlas);
		}

		public void DisposePixels()
		{
			this.atlasPixels = null;
		}

		public void ReinitPixels()
		{
			this.atlasPixels = new int[this.width * this.height];
		}

		public Dictionary<int, QuadBoundsf> textureBounds;

		public bool Full;

		private TextureAtlasNode rootNode;

		private int[] atlasPixels;

		internal int textureId;

		public int width;

		public int height;

		private float subPixelPaddingx;

		private float subPixelPaddingy;
	}
}
