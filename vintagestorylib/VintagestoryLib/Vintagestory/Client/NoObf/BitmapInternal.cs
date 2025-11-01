using System;
using Vintagestory.API.Common;

namespace Vintagestory.Client.NoObf
{
	public class BitmapInternal
	{
		public static BitmapInternal Create(int width, int height)
		{
			return new BitmapInternal
			{
				width = width,
				height = height,
				argb = new int[width * height]
			};
		}

		public static BitmapInternal CreateFromBitmap(ClientPlatformAbstract platform, BitmapRef bitmapref)
		{
			return new BitmapInternal
			{
				width = bitmapref.Width,
				height = bitmapref.Height,
				argb = bitmapref.Pixels
			};
		}

		public void SetPixel(int x, int y, int color)
		{
			this.argb[x + y * this.width] = color;
		}

		public int GetPixel(int x, int y)
		{
			return this.argb[x + y * this.width];
		}

		public BitmapRef ToBitmap(ClientPlatformAbstract platform)
		{
			BitmapRef bmp = platform.CreateBitmap(this.width, this.height);
			platform.SetBitmapPixelsArgb(bmp, this.argb);
			return bmp;
		}

		internal int[] argb;

		internal int width;

		internal int height;
	}
}
