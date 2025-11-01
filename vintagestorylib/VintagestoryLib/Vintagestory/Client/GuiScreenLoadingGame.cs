using System;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace Vintagestory.Client
{
	public class GuiScreenLoadingGame : GuiScreen
	{
		public GuiScreenLoadingGame(ScreenManager screenManager, GuiScreen parentScreen)
			: base(screenManager, parentScreen)
		{
			this.ElementComposer = ScreenManager.GuiComposers.Create("gameloadingscreen", ElementStdBounds.AutosizedMainDialogAtPos(150.0)).AddGrayBG(ElementBounds.Fill).AddDynamicText(Lang.Get("Loading game", Array.Empty<object>()), CairoFont.WhiteMediumText().WithOrientation(EnumTextOrientation.Center), ElementBounds.FixedSize(EnumDialogArea.CenterMiddle, 400.0, 400.0), "loadingtext")
				.Compose(true);
			this.textElem = this.ElementComposer.GetDynamicText("loadingtext");
			ScreenManager.Platform.ToggleOffscreenBuffer(false);
		}

		public override void RenderToDefaultFramebuffer(float dt)
		{
			if (this.ScreenManager.loadingText == null && !this.loadingShaders)
			{
				this.textElem.SetNewText("Loading shaders", false, false, false);
				this.ElementComposer.Render(dt);
				this.ElementComposer.PostRender(dt);
				this.loadingShaders = true;
				ScreenManager.Platform.Logger.Notification("Begin loading shaders");
				return;
			}
			this.ElementComposer.Render(dt);
			this.ElementComposer.PostRender(dt);
			LoadedTexture tex = this.ScreenManager.versionNumberTexture;
			float windowSizeX = (float)this.ScreenManager.GamePlatform.WindowSize.Width;
			float windowSizeY = (float)this.ScreenManager.GamePlatform.WindowSize.Height;
			this.ScreenManager.api.Render.Render2DTexturePremultipliedAlpha(tex.TextureId, windowSizeX - (float)tex.Width - 10f, windowSizeY - (float)tex.Height - 10f, (float)tex.Width, (float)tex.Height, 50f, null);
			if (this.loadingShaders)
			{
				ScreenManager.Platform.Logger.Notification("Load shaders now");
				this.ScreenManager.DoGameInitStage2();
				this.loadingShaders = false;
				this.shadersLoaded = true;
			}
			if (this.ScreenManager.loadingText != this.textElem.GetText())
			{
				this.textElem.SetNewText(this.ScreenManager.loadingText, false, false, false);
			}
			if (!this.shadersLoaded)
			{
				this.accum += dt;
				if (this.accum > 0.5f)
				{
					ScreenManager.Platform.Logger.Notification("Waiting for async sound loading...");
					this.accum = 0f;
				}
			}
		}

		public override void OnScreenLoaded()
		{
		}

		public GuiElementDynamicText textElem;

		private bool loadingShaders;

		private float accum;

		private bool shadersLoaded;
	}
}
