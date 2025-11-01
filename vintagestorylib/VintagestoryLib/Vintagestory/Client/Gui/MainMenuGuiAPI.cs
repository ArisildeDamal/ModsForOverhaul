using System;
using System.Collections.Generic;
using System.Diagnostics;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;

namespace Vintagestory.Client.Gui
{
	public class MainMenuGuiAPI : IGuiAPI
	{
		public TextTextureUtil TextTexture
		{
			get
			{
				return this.textutil;
			}
		}

		public TextDrawUtil Text
		{
			get
			{
				return this.prober;
			}
		}

		public IconUtil Icons
		{
			get
			{
				return this.iconutil;
			}
		}

		public List<GuiDialog> LoadedGuis
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public List<GuiDialog> OpenedGuis
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public MeshRef QuadMeshRef
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public ElementBounds WindowBounds
		{
			get
			{
				return new ElementWindowBounds();
			}
		}

		public MainMenuGuiAPI(ScreenManager screenManager, ICoreClientAPI capi)
		{
			MainMenuGuiAPI <>4__this = this;
			this.screenManager = screenManager;
			this.prober = new TextDrawUtil();
			this.textutil = new TextTextureUtil(capi);
			this.iconutil = new IconUtil(capi);
			this.svgLoader = new SvgLoader(capi);
			this.Icons.CustomIcons["copy"] = delegate(Context ctx, int x, int y, float w, float h, double[] rgba)
			{
				<>4__this.DrawSvg(screenManager.GamePlatform.AssetManager.TryGet_BaseAssets("textures/icons/copy.svg", true), ctx.GetTarget() as ImageSurface, ctx.Matrix, x, y, (int)w, (int)h, new int?(ColorUtil.FromRGBADoubles(rgba)));
			};
			this.Icons.CustomIcons["paste"] = delegate(Context ctx, int x, int y, float w, float h, double[] rgba)
			{
				<>4__this.DrawSvg(screenManager.GamePlatform.AssetManager.TryGet_BaseAssets("textures/icons/paste.svg", true), ctx.GetTarget() as ImageSurface, ctx.Matrix, x, y, (int)w, (int)h, new int?(ColorUtil.FromRGBADoubles(rgba)));
			};
		}

		public void DeleteTexture(int texid)
		{
			ScreenManager.Platform.GLDeleteTexture(texid);
		}

		public int LoadCairoTexture(ImageSurface surface, bool linearMag)
		{
			return ScreenManager.Platform.LoadCairoTexture(surface, linearMag);
		}

		public void LoadOrUpdateCairoTexture(ImageSurface surface, bool linearMag, ref LoadedTexture intoTexture)
		{
			ScreenManager.Platform.LoadOrUpdateCairoTexture(surface, linearMag, ref intoTexture);
		}

		public Vec2i GetDialogPosition(string key)
		{
			return ClientSettings.Inst.GetDialogPosition(key);
		}

		public void SetDialogPosition(string key, Vec2i pos)
		{
			ClientSettings.Inst.SetDialogPosition(key, pos);
		}

		public void PlaySound(string soundname, bool randomizePitch = false, float volume = 1f)
		{
			ScreenManager.PlaySound(soundname);
		}

		public GuiComposer CreateCompo(string dialogName, ElementBounds bounds)
		{
			return ScreenManager.GuiComposers.Create(dialogName, bounds);
		}

		public void RegisterDialog(params GuiDialog[] dialogs)
		{
			throw new NotImplementedException();
		}

		public void PlaySound(AssetLocation soundname, bool randomizePitch = false, float volume = 1f)
		{
			throw new NotImplementedException();
		}

		public void RequestFocus(GuiDialog guiDialog)
		{
			throw new NotImplementedException();
		}

		public void TriggerDialogOpened(GuiDialog guiDialog)
		{
			throw new NotImplementedException();
		}

		public void TriggerDialogClosed(GuiDialog guiDialog)
		{
			throw new NotImplementedException();
		}

		public List<ElementBounds> GetDialogBoundsInArea(EnumDialogArea area)
		{
			throw new NotImplementedException();
		}

		public void OpenLink(string href)
		{
			GuiScreen screen = this.screenManager.CurrentScreen;
			if (screen is GuiScreenLogin)
			{
				Process.Start(href);
				return;
			}
			this.screenManager.LoadScreen(new GuiScreenConfirmAction("Please Confirm", Lang.Get("Open below external link in a browser?", Array.Empty<object>()) + "\n\n" + href, "Cancel", "Confirm", delegate(bool val)
			{
				if (val)
				{
					NetUtil.OpenUrlInBrowser(href);
				}
				this.screenManager.LoadScreen(screen);
			}, this.screenManager, screen, "openurl", false));
		}

		public LoadedTexture LoadSvg(AssetLocation loc, int textureWidth, int textureHeight, int width = 0, int height = 0, int? color = null)
		{
			IAsset asset = this.screenManager.api.Assets.TryGet(loc, true);
			if (asset == null)
			{
				this.screenManager.api.Logger.Warning("LoadSvg(): No such file in assets - " + loc);
				return null;
			}
			return this.svgLoader.LoadSvg(asset, textureWidth, textureHeight, width, height, color);
		}

		public void DrawSvg(IAsset svgAsset, ImageSurface intoSurface, int posx, int posy, int width = 0, int height = 0, int? color = 0)
		{
			this.svgLoader.DrawSvg(svgAsset, intoSurface, posx, posy, width, height, color);
		}

		public void DrawSvg(IAsset svgAsset, ImageSurface intoSurface, Matrix matrix, int posx, int posy, int width = 0, int height = 0, int? color = 0)
		{
			this.svgLoader.DrawSvg(svgAsset, intoSurface, matrix, posx, posy, width, height, color);
		}

		public LoadedTexture LoadSvgWithPadding(AssetLocation loc, int textureWidth, int textureHeight, int padding = 0, int? color = 0)
		{
			return this.LoadSvg(loc, textureWidth + 2 * padding, textureHeight + 2 * padding, textureWidth, textureHeight, color);
		}

		private ScreenManager screenManager;

		private TextTextureUtil textutil;

		private IconUtil iconutil;

		private TextDrawUtil prober;

		private SvgLoader svgLoader;
	}
}
