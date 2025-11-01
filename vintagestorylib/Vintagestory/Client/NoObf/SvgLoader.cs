using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Cairo;
using NanoSvg;
using OpenTK.Graphics.OpenGL;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class SvgLoader
	{
		public SvgLoader(ICoreClientAPI _capi)
		{
			this.capi = _capi;
			this.rasterizer = SvgNativeMethods.nsvgCreateRasterizer();
			if (this.rasterizer == IntPtr.Zero)
			{
				throw new Win32Exception(Marshal.GetLastWin32Error());
			}
		}

		public unsafe void DrawSvg(IAsset svgAsset, ImageSurface intoSurface, int posx, int posy, int width = 0, int height = 0, int? color = 0)
		{
			byte[] array = this.rasterizeSvg(svgAsset, width, height, width, height, color);
			int len = intoSurface.Width * intoSurface.Height;
			IntPtr ptr = intoSurface.DataPtr;
			fixed (byte[] array2 = array)
			{
				byte* srcPointerbyte;
				if (array == null || array2.Length == 0)
				{
					srcPointerbyte = null;
				}
				else
				{
					srcPointerbyte = &array2[0];
				}
				int* srcPointer = (int*)srcPointerbyte;
				int* dstPointer = (int*)ptr.ToPointer();
				for (int x = 0; x < width; x++)
				{
					for (int y = 0; y < height; y++)
					{
						int srcPixel = srcPointer[y * width + x];
						int dstPos = (posy + y) * intoSurface.Width + posx + x;
						if (dstPos >= 0 && dstPos < len)
						{
							int dstPixel = dstPointer[dstPos];
							dstPointer[dstPos] = ColorUtil.ColorOver(srcPixel, dstPixel);
						}
					}
				}
			}
			intoSurface.MarkDirty();
		}

		public unsafe void DrawSvg(IAsset svgAsset, ImageSurface intoSurface, Matrix matrix, int posx, int posy, int width = 0, int height = 0, int? color = 0)
		{
			byte[] array = this.rasterizeSvg(svgAsset, width, height, width, height, color);
			int len = intoSurface.Width * intoSurface.Height;
			IntPtr ptr = intoSurface.DataPtr;
			fixed (byte[] array2 = array)
			{
				byte* srcPointerbyte;
				if (array == null || array2.Length == 0)
				{
					srcPointerbyte = null;
				}
				else
				{
					srcPointerbyte = &array2[0];
				}
				int* srcPointer = (int*)srcPointerbyte;
				int* dstPointer = (int*)ptr.ToPointer();
				for (int x = 0; x < width; x++)
				{
					for (int y = 0; y < height; y++)
					{
						int srcPixel = srcPointer[y * width + x];
						double rx = (double)(posx + x);
						double ry = (double)(posy + y);
						matrix.TransformPoint(ref rx, ref ry);
						int destx = (int)rx;
						int dstPos = (int)ry * intoSurface.Width + destx;
						if (dstPos >= 0 && dstPos < len)
						{
							int dstPixel = dstPointer[dstPos];
							dstPointer[dstPos] = ColorUtil.ColorOver(srcPixel, dstPixel);
						}
					}
				}
			}
			intoSurface.MarkDirty();
		}

		public unsafe LoadedTexture LoadSvg(IAsset svgAsset, int textureWidth, int textureHeight, int width = 0, int height = 0, int? color = 0)
		{
			int id = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, id);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9729);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, 9729);
			byte[] array;
			byte* p;
			if ((array = this.rasterizeSvg(svgAsset, textureWidth, textureHeight, width, height, color)) == null || array.Length == 0)
			{
				p = null;
			}
			else
			{
				p = &array[0];
			}
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, textureWidth, textureHeight, 0, PixelFormat.Rgba, PixelType.UnsignedByte, p);
			array = null;
			return new LoadedTexture(this.capi, id, width, height);
		}

		public unsafe byte[] rasterizeSvg(IAsset svgAsset, int textureWidth, int textureHeight, int width = 0, int height = 0, int? color = 0)
		{
			float scale = 1f;
			float dpi = 96f;
			byte[] cb = ((color == null) ? null : ColorUtil.ToRGBABytes(color.Value));
			int offX = 0;
			int offY = 0;
			if (this.rasterizer == IntPtr.Zero)
			{
				throw new ObjectDisposedException("SvgLoader is already disposed!");
			}
			if (svgAsset.Data == null)
			{
				throw new ArgumentNullException("Asset Data is null. Is the asset loaded?");
			}
			IntPtr image = SvgNativeMethods.nsvgParse(svgAsset.ToText(), "px", dpi);
			if (image == IntPtr.Zero)
			{
				throw new Win32Exception(Marshal.GetLastWin32Error());
			}
			if (SvgNativeMethods.nsvgImageGetSize(image) == IntPtr.Zero)
			{
				throw new Win32Exception(Marshal.GetLastWin32Error());
			}
			NsvgSize size = Marshal.PtrToStructure<NsvgSize>(SvgNativeMethods.nsvgImageGetSize(image));
			if (width == 0 && height == 0)
			{
				width = (int)(size.width * scale);
				height = (int)(size.height * scale);
			}
			else if (width == 0)
			{
				scale = (float)height / size.height;
				width = (int)(size.width * scale);
			}
			else if (height == 0)
			{
				scale = (float)width / size.width;
				height = (int)(size.height * scale);
			}
			else
			{
				float scaleX = (float)width / size.width;
				float scaleY = (float)height / size.height;
				scale = ((scaleX < scaleY) ? scaleX : scaleY);
				offX = (int)((float)textureWidth - size.width * scale) / 2;
				offY = (int)((float)textureHeight - size.height * scale) / 2;
			}
			byte[] buffer = new byte[textureWidth * textureHeight * 4];
			byte[] array;
			byte* p;
			if ((array = buffer) == null || array.Length == 0)
			{
				p = null;
			}
			else
			{
				p = &array[0];
			}
			SvgNativeMethods.nsvgRasterize(this.rasterizer, image, (float)offX, (float)offY, scale, p, textureWidth, textureHeight, textureWidth * 4);
			if (cb != null)
			{
				for (int i = 0; i < buffer.Length - 1; i += 4)
				{
					float a = (float)buffer[i + 3] / 255f;
					buffer[i] = (byte)(a * (float)cb[0]);
					buffer[i + 1] = (byte)(a * (float)cb[1]);
					buffer[i + 2] = (byte)(a * (float)cb[2]);
					buffer[i + 3] = (byte)(a * (float)cb[3]);
				}
			}
			else
			{
				for (int j = 0; j < buffer.Length - 1; j += 4)
				{
					byte r = buffer[j];
					byte g = buffer[j + 1];
					byte b = buffer[j + 2];
					buffer[j] = b;
					buffer[j + 1] = g;
					buffer[j + 2] = r;
				}
			}
			array = null;
			SvgNativeMethods.nsvgDelete(image);
			return buffer;
		}

		~SvgLoader()
		{
			if (this.rasterizer != IntPtr.Zero)
			{
				SvgNativeMethods.nsvgDeleteRasterizer(this.rasterizer);
				this.rasterizer = IntPtr.Zero;
			}
		}

		private readonly ICoreClientAPI capi;

		private IntPtr rasterizer;
	}
}
