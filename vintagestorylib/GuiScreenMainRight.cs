using System;
using System.Collections.Generic;
using System.Runtime;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.Client;
using Vintagestory.Client.NoObf;

public class GuiScreenMainRight : GuiScreen
{
	public GuiScreenMainRight(ScreenManager screenManager, GuiScreen parent)
		: base(screenManager, parent)
	{
		this.ShowMainMenu = true;
		this.quote = this.getQuote();
	}

	public override void OnScreenLoaded()
	{
		this.ScreenManager.guiMainmenuLeft.updateButtonActiveFlag("home");
		this.gcCollectAttempts = 0;
		this.gcCollectAccum = -5f;
	}

	public void Compose()
	{
		ElementBounds dlgBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightBottom).WithFixedAlignmentOffset(-50.0, -50.0);
		ElementBounds textBounds = ElementBounds.Fixed(0.0, 0.0, 450.0, 170.0);
		CairoFont font = CairoFont.WhiteDetailText().WithFontSize(15f).WithLineHeightMultiplier(1.100000023841858);
		this.ElementComposer = ScreenManager.GuiComposers.Create("welcomedialog", dlgBounds).AddShadedDialogBG(ElementStdBounds.DialogBackground().WithFixedPadding(GuiStyle.ElementToDialogPadding, GuiStyle.ElementToDialogPadding), false, 5.0, 0.8f).BeginChildElements()
			.AddRichtext(Lang.Get("mainmenu-greeting", Array.Empty<object>()), font, textBounds.FlatCopy(), null)
			.EndChildElements()
			.Compose(true);
		this.quoteTexture = this.ScreenManager.api.Gui.TextTexture.GenUnscaledTextTexture("„" + this.quote + "‟", CairoFont.WhiteDetailText().WithSlant(FontSlant.Italic), null);
		this.grayBg = new LoadedTexture(this.ScreenManager.api);
		ImageSurface surface = new ImageSurface(Format.Argb32, 1, 1);
		Context context = new Context(surface);
		context.SetSourceRGBA(0.0, 0.0, 0.0, 0.25);
		context.Paint();
		this.ScreenManager.api.Gui.LoadOrUpdateCairoTexture(surface, true, ref this.grayBg);
		context.Dispose();
		surface.Dispose();
	}

	public override void RenderToDefaultFramebuffer(float dt)
	{
		this.Render(dt, this.ScreenManager.GamePlatform.EllapsedMs, false);
	}

	public void Render(float dt, long ellapsedMs, bool onlyBackground = false)
	{
		if (this.renderStartMs == 0L)
		{
			this.renderStartMs = ellapsedMs;
		}
		this.ensureLOHCompacted(dt);
		float windowSizeX = (float)this.ScreenManager.GamePlatform.WindowSize.Width;
		float windowSizeY = (float)this.ScreenManager.GamePlatform.WindowSize.Height;
		double x = this.ScreenManager.guiMainmenuLeft.Width + GuiElement.scaled(15.0);
		if (onlyBackground)
		{
			return;
		}
		this.ElementComposer.Render(dt);
		if (this.ElementComposer.MouseOverCursor != null)
		{
			this.FocusedMouseCursor = this.ElementComposer.MouseOverCursor;
		}
		this.ScreenManager.api.Render.Render2DTexturePremultipliedAlpha(this.grayBg.TextureId, this.ScreenManager.guiMainmenuLeft.Width, (double)(windowSizeY - (float)this.quoteTexture.Height) - GuiElement.scaled(10.0), (double)windowSizeX, (double)this.quoteTexture.Height + GuiElement.scaled(10.0), 50f, null);
		this.ScreenManager.RenderMainMenuParts(dt, this.ElementComposer.Bounds, this.ShowMainMenu, false);
		if (this.ScreenManager.mainMenuComposer.MouseOverCursor != null)
		{
			this.FocusedMouseCursor = this.ScreenManager.mainMenuComposer.MouseOverCursor;
		}
		this.ScreenManager.api.Render.Render2DTexturePremultipliedAlpha(this.quoteTexture.TextureId, x, (double)(windowSizeY - (float)this.quoteTexture.Height) - GuiElement.scaled(5.0), (double)this.quoteTexture.Width, (double)this.quoteTexture.Height, 50f, null);
		this.ElementComposer.PostRender(dt);
		this.ScreenManager.GamePlatform.UseMouseCursor((this.FocusedMouseCursor == null) ? "normal" : this.FocusedMouseCursor, false);
	}

	private void ensureLOHCompacted(float dt)
	{
		if (this.ScreenManager.CurrentScreen is GuiScreenConnectingToServer || this.ScreenManager.CurrentScreen is GuiScreenExitingServer || this.gcCollectAttempts > 6)
		{
			return;
		}
		int thresholdMb = 300;
		long memMegaBytes = GC.GetTotalMemory(false) / 1024L / 1024L;
		this.gcCollectAccum += dt;
		if (this.gcCollectAccum > 1f && memMegaBytes > (long)thresholdMb)
		{
			if (ClientSettings.OptimizeRamMode > 0)
			{
				GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
				GC.Collect();
			}
			this.gcCollectAccum = 0f;
			this.gcCollectAttempts++;
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		LoadedTexture loadedTexture = this.quoteTexture;
		if (loadedTexture != null)
		{
			loadedTexture.Dispose();
		}
		this.quoteTexture = null;
		LoadedTexture loadedTexture2 = this.grayBg;
		if (loadedTexture2 != null)
		{
			loadedTexture2.Dispose();
		}
		this.grayBg = null;
	}

	private string getQuote()
	{
		List<string> quotes = new List<string>();
		int i = 1;
		while (Lang.HasTranslation("mainscreen-quote" + i.ToString(), false, true))
		{
			quotes.Add(Lang.Get("mainscreen-quote" + i.ToString(), Array.Empty<object>()));
			i++;
		}
		Random rand = new Random();
		if (quotes.Count == 0)
		{
			return "";
		}
		return quotes[rand.Next(quotes.Count)];
	}

	private LoadedTexture grayBg;

	private LoadedTexture quoteTexture;

	private float gcCollectAccum = -2f;

	private int gcCollectAttempts;

	private long renderStartMs;

	private string quote;
}
