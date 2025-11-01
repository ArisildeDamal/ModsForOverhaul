using System;
using System.Collections.Generic;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.Client.NoObf
{
	public class GuiAPI : IGuiAPI
	{
		public TextTextureUtil TextTexture
		{
			get
			{
				return this.textutil;
			}
		}

		public IconUtil Icons
		{
			get
			{
				return this.iconutil;
			}
		}

		public TextDrawUtil Text
		{
			get
			{
				return this.prober;
			}
		}

		public List<GuiDialog> LoadedGuis
		{
			get
			{
				return this.game.LoadedGuis;
			}
		}

		List<GuiDialog> IGuiAPI.OpenedGuis
		{
			get
			{
				return this.game.OpenedGuis;
			}
		}

		public MeshRef QuadMeshRef
		{
			get
			{
				return this.game.quadModel;
			}
		}

		public ElementBounds WindowBounds
		{
			get
			{
				return new ElementWindowBounds();
			}
		}

		public GuiAPI(ClientMain game, ICoreClientAPI capi)
		{
			this.game = game;
			this.prober = new TextDrawUtil();
			this.textutil = new TextTextureUtil(capi);
			this.iconutil = new IconUtil(capi);
			this.svgLoader = new SvgLoader(capi);
		}

		public GuiComposer CreateCompo(string dialogName, ElementBounds bounds)
		{
			return this.game.GuiComposers.Create(dialogName, bounds);
		}

		public void DeleteTexture(int textureid)
		{
			this.game.Platform.GLDeleteTexture(textureid);
		}

		public void DrawSvg(IAsset svgAsset, ImageSurface intoSurface, int posx, int posy, int width = 0, int height = 0, int? color = 0)
		{
			this.svgLoader.DrawSvg(svgAsset, intoSurface, posx, posy, width, height, color);
		}

		public void DrawSvg(IAsset svgAsset, ImageSurface intoSurface, Matrix matrix, int posx, int posy, int width = 0, int height = 0, int? color = 0)
		{
			this.svgLoader.DrawSvg(svgAsset, intoSurface, matrix, posx, posy, width, height, color);
		}

		public LoadedTexture LoadSvg(AssetLocation loc, int textureWidth, int textureHeight, int width = 0, int height = 0, int? color = null)
		{
			IAsset asset = this.game.AssetManager.TryGet(loc, true);
			if (asset == null)
			{
				return null;
			}
			return this.svgLoader.LoadSvg(asset, textureWidth, textureHeight, width, height, color);
		}

		public LoadedTexture LoadSvgWithPadding(AssetLocation loc, int textureWidth, int textureHeight, int padding = 0, int? color = 0)
		{
			return this.LoadSvg(loc, textureWidth + 2 * padding, textureHeight + 2 * padding, textureWidth, textureHeight, color);
		}

		public int LoadCairoTexture(ImageSurface surface, bool linearMag)
		{
			return this.game.Platform.LoadCairoTexture(surface, linearMag);
		}

		public void LoadOrUpdateCairoTexture(ImageSurface surface, bool linearMag, ref LoadedTexture intoTexture)
		{
			this.game.Platform.LoadOrUpdateCairoTexture(surface, linearMag, ref intoTexture);
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
			this.game.PlaySound(new AssetLocation("sounds/" + soundname), randomizePitch, volume);
		}

		public void PlaySound(AssetLocation soundname, bool randomizePitch = false, float volume = 1f)
		{
			this.game.PlaySound(soundname, randomizePitch, volume);
		}

		public void RequestFocus(GuiDialog guiDialog)
		{
			this.guimgr.RequestFocus(guiDialog);
		}

		public void TriggerDialogOpened(GuiDialog guiDialog)
		{
			ClientEventManager eventManager = this.game.eventManager;
			if (eventManager == null)
			{
				return;
			}
			eventManager.TriggerDialogOpened(guiDialog);
		}

		public void TriggerDialogClosed(GuiDialog guiDialog)
		{
			ClientEventManager eventManager = this.game.eventManager;
			if (eventManager == null)
			{
				return;
			}
			eventManager.TriggerDialogClosed(guiDialog);
		}

		public void RegisterDialog(params GuiDialog[] dialogs)
		{
			this.game.RegisterDialog(dialogs);
		}

		public List<ElementBounds> GetDialogBoundsInArea(EnumDialogArea area)
		{
			List<ElementBounds> bounds = new List<ElementBounds>();
			foreach (GuiDialog guiDialog in this.game.OpenedGuis)
			{
				foreach (GuiComposer composer in guiDialog.Composers.Values)
				{
					if (composer.Bounds.Alignment == area)
					{
						bounds.Add(composer.Bounds);
					}
				}
			}
			return bounds;
		}

		public void OpenLink(string href)
		{
			Action<bool> <>9__1;
			this.game.EnqueueMainThreadTask(delegate
			{
				ICoreClientAPI api = this.game.api;
				string text = Lang.Get("Open below external link in a browser?", Array.Empty<object>()) + "\n\n\n" + href;
				Action<bool> action;
				if ((action = <>9__1) == null)
				{
					action = (<>9__1 = delegate(bool val)
					{
						if (val)
						{
							NetUtil.OpenUrlInBrowser(href);
						}
					});
				}
				new GuiDialogConfirm(api, text, action).TryOpen();
			}, "openlink");
		}

		private ClientMain game;

		public GuiManager guimgr;

		private TextTextureUtil textutil;

		private IconUtil iconutil;

		private TextDrawUtil prober;

		private SvgLoader svgLoader;
	}
}
