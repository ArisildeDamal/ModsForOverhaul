using System;
using Vintagestory.API.Client;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf
{
	internal class TextureAtlasNode
	{
		public TextureAtlasNode(int x1, int y1, int x2, int y2)
		{
			this.bounds = new QuadBoundsi
			{
				x1 = x1,
				y1 = y1,
				x2 = x2,
				y2 = y2
			};
		}

		public void PopulateAtlasPositions(TextureAtlasPosition[] positions, int atlasTextureId, int atlasNumber, float atlasWidth, float atlasHeight, float subPixelPaddingx, float subPixelPaddingy)
		{
			if (this.textureSubId != null)
			{
				positions[this.textureSubId.Value] = new TextureAtlasPosition
				{
					atlasTextureId = atlasTextureId,
					atlasNumber = (byte)atlasNumber,
					x1 = (float)this.bounds.x1 / atlasWidth + subPixelPaddingx,
					y1 = (float)this.bounds.y1 / atlasHeight + subPixelPaddingy,
					x2 = (float)this.bounds.x2 / atlasWidth - subPixelPaddingx,
					y2 = (float)this.bounds.y2 / atlasHeight - subPixelPaddingy
				};
			}
			if (this.left != null)
			{
				this.left.PopulateAtlasPositions(positions, atlasTextureId, atlasNumber, atlasWidth, atlasHeight, subPixelPaddingx, subPixelPaddingy);
			}
			if (this.right != null)
			{
				this.right.PopulateAtlasPositions(positions, atlasTextureId, atlasNumber, atlasWidth, atlasHeight, subPixelPaddingx, subPixelPaddingy);
			}
		}

		public TextureAtlasNode GetFreeNode(int textureSubId, int width, int height)
		{
			if (this.left != null)
			{
				TextureAtlasNode node = this.left.GetFreeNode(textureSubId, width, height);
				if (node == null)
				{
					node = this.right.GetFreeNode(textureSubId, width, height);
				}
				return node;
			}
			if (this.textureSubId != null)
			{
				return null;
			}
			int freeWidth = this.bounds.x2 - this.bounds.x1;
			int freeHeight = this.bounds.y2 - this.bounds.y1;
			if (freeWidth < width || freeHeight < height)
			{
				return null;
			}
			if (freeWidth == width && freeHeight == height)
			{
				return this;
			}
			int num = freeWidth - width;
			int remainHeight = freeHeight - height;
			if (num > remainHeight)
			{
				this.left = new TextureAtlasNode(this.bounds.x1, this.bounds.y1, this.bounds.x1 + width, this.bounds.y2);
				this.right = new TextureAtlasNode(this.bounds.x1 + width, this.bounds.y1, this.bounds.x2, this.bounds.y2);
			}
			else
			{
				this.left = new TextureAtlasNode(this.bounds.x1, this.bounds.y1, this.bounds.x2, this.bounds.y1 + height);
				this.right = new TextureAtlasNode(this.bounds.x1, this.bounds.y1 + height, this.bounds.x2, this.bounds.y2);
			}
			return this.left.GetFreeNode(textureSubId, width, height);
		}

		public TextureAtlasNode left;

		public TextureAtlasNode right;

		public QuadBoundsi bounds;

		public int? textureSubId;
	}
}
