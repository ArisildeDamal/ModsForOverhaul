using System;
using System.IO;
using System.Runtime.CompilerServices;
using CompactExifLib;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Desktop;
using SkiaSharp;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;

namespace Vintagestory.ClientNative
{
	public class Screenshot
	{
		public string SaveScreenshot(ClientPlatformAbstract platform, Size2i size, string path = null, string filename = null, bool withAlpha = false, bool flip = true, string metadataStr = null)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(0, 1);
			defaultInterpolatedStringHandler.AppendFormatted<DateTime>(DateTime.Now, "yyyy-MM-dd_HH-mm-ss");
			string text = defaultInterpolatedStringHandler.ToStringAndClear();
			if (path == null)
			{
				if (!Directory.Exists(GamePaths.Screenshots))
				{
					Directory.CreateDirectory(GamePaths.Screenshots);
				}
				path = GamePaths.Screenshots;
			}
			if (filename == null)
			{
				filename = Path.Combine(path, text + ".png");
			}
			if (!GameDatabase.HaveWriteAccessFolder(path))
			{
				throw new Exception("No write access to " + path);
			}
			using (SKBitmap bitmap = this.GrabScreenshot(size, ClientSettings.ScaleScreenshot, flip, withAlpha))
			{
				bitmap.Save(filename);
			}
			if (metadataStr != null)
			{
				ExifData exifData = new ExifData(filename, (ExifLoadOptions)0);
				exifData.SetTagValue(ExifTag.Make, metadataStr, StrCoding.UsAscii);
				exifData.SetTagValue(ExifTag.ImageDescription, metadataStr, StrCoding.Utf8);
				exifData.Save(filename, (ExifSaveOptions)0);
			}
			return Path.GetFileName(filename);
		}

		public SKBitmap GrabScreenshot(Size2i size, bool scaleScreenshot, bool flip, bool withAlpha)
		{
			SKBitmap bitmap = new SKBitmap(new SKImageInfo(size.Width, size.Height, SKColorType.Bgra8888, withAlpha ? SKAlphaType.Unpremul : SKAlphaType.Opaque));
			GL.ReadPixels(0, 0, size.Width, size.Height, PixelFormat.Bgra, PixelType.UnsignedByte, bitmap.GetPixels());
			if (scaleScreenshot)
			{
				bitmap = bitmap.Resize(new SKImageInfo(this.d_GameWindow.ClientSize.X, this.d_GameWindow.ClientSize.Y), new SKSamplingOptions(SKCubicResampler.Mitchell));
			}
			if (!flip)
			{
				return bitmap;
			}
			SKBitmap rotated = new SKBitmap(bitmap.Width, bitmap.Height, bitmap.ColorType, withAlpha ? SKAlphaType.Unpremul : SKAlphaType.Opaque);
			SKBitmap skbitmap;
			using (SKCanvas surface = new SKCanvas(rotated))
			{
				surface.Translate((float)bitmap.Width, (float)bitmap.Height);
				surface.RotateDegrees(180f);
				surface.Scale(-1f, 1f, (float)bitmap.Width / 2f, 0f);
				surface.DrawBitmap(bitmap, 0f, 0f, null);
				skbitmap = rotated;
			}
			return skbitmap;
		}

		public GameWindow d_GameWindow;
	}
}
