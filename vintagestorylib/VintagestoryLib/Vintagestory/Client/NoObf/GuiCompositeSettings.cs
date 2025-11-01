using System;
using System.Collections.Generic;
using System.Linq;
using Cairo;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Client.Gui;

namespace Vintagestory.Client.NoObf
{
	public class GuiCompositeSettings : GuiComposite
	{
		public bool IsCapturingHotKey
		{
			get
			{
				return this.hotkeyCapturer.IsCapturing();
			}
		}

		public GuiCompositeSettings(IGameSettingsHandler handler, bool onMainScreen)
		{
			this.handler = handler;
			this.onMainscreen = onMainScreen;
		}

		private GuiComposer ComposerHeader(string dialogName, string currentTab)
		{
			CairoFont fnt = CairoFont.ButtonText();
			this.updateButtonBounds();
			GuiComposer composerHeader;
			if (this.onMainscreen)
			{
				int width2 = ScreenManager.Platform.WindowSize.Width;
				int height = ScreenManager.Platform.WindowSize.Height;
				ElementBounds containerBounds = ElementBounds.Fixed(0.0, 0.0, 950.0, 740.0);
				this.aButtonBounds.ParentBounds = containerBounds;
				this.gButtonBounds.ParentBounds = containerBounds;
				this.mButtonBounds.ParentBounds = containerBounds;
				this.cButtonBounds.ParentBounds = containerBounds;
				this.sButtonBounds.ParentBounds = containerBounds;
				this.iButtonBounds.ParentBounds = containerBounds;
				this.dButtonBounds.ParentBounds = containerBounds;
				composerHeader = this.handler.dialogBase(dialogName + "main", containerBounds.fixedWidth, containerBounds.fixedHeight).BeginChildElements(containerBounds).AddToggleButton(Lang.Get("setting-graphics-header", Array.Empty<object>()), fnt, new Action<bool>(this.OnGraphicsOptions), this.gButtonBounds, "graphics")
					.AddToggleButton(Lang.Get("setting-mouse-header", Array.Empty<object>()), fnt, new Action<bool>(this.OnMouseOptions), this.mButtonBounds, "mouse")
					.AddToggleButton(Lang.Get("setting-controls-header", Array.Empty<object>()), fnt, new Action<bool>(this.OnControlOptions), this.cButtonBounds, "controls")
					.AddToggleButton(Lang.Get("setting-accessibility-header", Array.Empty<object>()), fnt, new Action<bool>(this.OnAccessibilityOptions), this.aButtonBounds, "accessibility")
					.AddToggleButton(Lang.Get("setting-sound-header", Array.Empty<object>()), fnt, new Action<bool>(this.OnSoundOptions), this.sButtonBounds, "sounds")
					.AddToggleButton(Lang.Get("setting-interface-header", Array.Empty<object>()), fnt, new Action<bool>(this.OnInterfaceOptions), this.iButtonBounds, "interface")
					.AddIf(ClientSettings.DeveloperMode)
					.AddToggleButton(Lang.Get("setting-dev-header", Array.Empty<object>()), fnt, new Action<bool>(this.OnDeveloperOptions), this.dButtonBounds, "developer")
					.EndIf();
			}
			else
			{
				ElementBounds dlgBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterFixed).WithFixedPosition(0.0, 75.0);
				double width = this.backButtonBounds.fixedX + this.backButtonBounds.fixedWidth + 35.0;
				dlgBounds.horizontalSizing = ElementSizing.Fixed;
				dlgBounds.fixedWidth = width;
				ElementBounds bgBounds = new ElementBounds().WithSizing(ElementSizing.FitToChildren).WithFixedPadding(GuiStyle.ElementToDialogPadding);
				bgBounds.horizontalSizing = ElementSizing.Fixed;
				bgBounds.fixedWidth = width - 2.0 * GuiStyle.ElementToDialogPadding;
				this.gButtonBounds.ParentBounds = bgBounds;
				this.aButtonBounds.ParentBounds = bgBounds;
				this.mButtonBounds.ParentBounds = bgBounds;
				this.cButtonBounds.ParentBounds = bgBounds;
				this.sButtonBounds.ParentBounds = bgBounds;
				this.iButtonBounds.ParentBounds = bgBounds;
				this.dButtonBounds.ParentBounds = bgBounds;
				this.backButtonBounds.ParentBounds = bgBounds;
				composerHeader = this.handler.GuiComposers.Create(dialogName + "ingame", dlgBounds).AddShadedDialogBG(bgBounds, false, 5.0, 0.75f).AddStaticCustomDraw(bgBounds, delegate(Context ctx, ImageSurface surface, ElementBounds bounds)
				{
					ctx.SetSourceRGBA(1.0, 1.0, 1.0, 0.1);
					GuiElement.RoundRectangle(ctx, GuiElement.scaled(5.0) + bounds.bgDrawX, GuiElement.scaled(5.0) + bounds.bgDrawY, bounds.OuterWidth - GuiElement.scaled(10.0), GuiElement.scaled(75.0), 1.0);
					ctx.Fill();
				})
					.BeginChildElements()
					.AddToggleButton(Lang.Get("setting-graphics-header", Array.Empty<object>()), fnt, new Action<bool>(this.OnGraphicsOptions), this.gButtonBounds, "graphics")
					.AddToggleButton(Lang.Get("setting-mouse-header", Array.Empty<object>()), fnt, new Action<bool>(this.OnMouseOptions), this.mButtonBounds, "mouse")
					.AddToggleButton(Lang.Get("setting-controls-header", Array.Empty<object>()), fnt, new Action<bool>(this.OnControlOptions), this.cButtonBounds, "controls")
					.AddToggleButton(Lang.Get("setting-accessibility-header", Array.Empty<object>()), fnt, new Action<bool>(this.OnAccessibilityOptions), this.aButtonBounds, "accessibility")
					.AddToggleButton(Lang.Get("setting-sound-header", Array.Empty<object>()), fnt, new Action<bool>(this.OnSoundOptions), this.sButtonBounds, "sounds")
					.AddToggleButton(Lang.Get("setting-interface-header", Array.Empty<object>()), fnt, new Action<bool>(this.OnInterfaceOptions), this.iButtonBounds, "interface")
					.AddIf(ClientSettings.DeveloperMode)
					.AddToggleButton(Lang.Get("setting-dev-header", Array.Empty<object>()), fnt, new Action<bool>(this.OnDeveloperOptions), this.dButtonBounds, "developer")
					.EndIf()
					.AddButton(Lang.Get("general-back", Array.Empty<object>()), delegate
					{
						this.clickedItemIndex = null;
						HotkeyCapturer hotkeyCapturer = this.hotkeyCapturer;
						if (hotkeyCapturer != null)
						{
							hotkeyCapturer.EndCapture(true);
						}
						return this.handler.OnBackPressed();
					}, this.backButtonBounds, EnumButtonStyle.Normal, null);
			}
			composerHeader.GetToggleButton("graphics").SetValue(currentTab == "graphics");
			composerHeader.GetToggleButton("mouse").SetValue(currentTab == "mouse");
			composerHeader.GetToggleButton("controls").SetValue(currentTab == "controls");
			composerHeader.GetToggleButton("accessibility").SetValue(currentTab == "accessibility");
			composerHeader.GetToggleButton("sounds").SetValue(currentTab == "sounds");
			composerHeader.GetToggleButton("interface").SetValue(currentTab == "interface");
			GuiElementToggleButton toggleButton = composerHeader.GetToggleButton("developer");
			if (toggleButton != null)
			{
				toggleButton.SetValue(currentTab == "developer");
			}
			return composerHeader;
		}

		private void updateButtonBounds()
		{
			CairoFont cairoFont = CairoFont.ButtonText();
			double gWidth = cairoFont.GetTextExtents(Lang.Get("setting-graphics-header", Array.Empty<object>())).Width / (double)ClientSettings.GUIScale + 15.0;
			double mWidth = cairoFont.GetTextExtents(Lang.Get("setting-mouse-header", Array.Empty<object>())).Width / (double)ClientSettings.GUIScale + 15.0;
			double cWidth = cairoFont.GetTextExtents(Lang.Get("setting-controls-header", Array.Empty<object>())).Width / (double)ClientSettings.GUIScale + 15.0;
			double aWidth = cairoFont.GetTextExtents(Lang.Get("setting-accessibility-header", Array.Empty<object>())).Width / (double)ClientSettings.GUIScale + 15.0;
			double sWidth = cairoFont.GetTextExtents(Lang.Get("setting-sound-header", Array.Empty<object>())).Width / (double)ClientSettings.GUIScale + 15.0;
			double iWidth = cairoFont.GetTextExtents(Lang.Get("setting-interface-header", Array.Empty<object>())).Width / (double)ClientSettings.GUIScale + 15.0;
			double dWidth = cairoFont.GetTextExtents(Lang.Get("setting-dev-header", Array.Empty<object>())).Width / (double)ClientSettings.GUIScale + 15.0;
			double backWidth = cairoFont.GetTextExtents(Lang.Get("general-back", Array.Empty<object>())).Width / (double)ClientSettings.GUIScale + 15.0;
			this.gButtonBounds.WithFixedWidth(gWidth);
			this.mButtonBounds.WithFixedWidth(mWidth).FixedRightOf(this.gButtonBounds, 15.0);
			this.cButtonBounds.WithFixedWidth(cWidth).FixedRightOf(this.mButtonBounds, 15.0);
			this.aButtonBounds.WithFixedWidth(aWidth).FixedRightOf(this.cButtonBounds, 15.0);
			this.sButtonBounds.WithFixedWidth(sWidth).FixedRightOf(this.aButtonBounds, 15.0);
			this.iButtonBounds.WithFixedWidth(iWidth).FixedRightOf(this.sButtonBounds, 15.0);
			this.dButtonBounds.WithFixedWidth(dWidth).FixedRightOf(this.iButtonBounds, 15.0);
			this.backButtonBounds.WithFixedWidth(backWidth).FixedRightOf(ClientSettings.DeveloperMode ? this.dButtonBounds : this.iButtonBounds, 25.0);
		}

		internal bool OpenSettingsMenu()
		{
			this.OnGraphicsOptions(true);
			return true;
		}

		internal void Refresh()
		{
			if (ClientSettings.DynamicColorGrading && this.composer != null)
			{
				DefaultShaderUniforms ShaderUniforms = ScreenManager.Platform.ShaderUniforms;
				if (ShaderUniforms != null)
				{
					GuiElementSlider slider = this.composer.GetSlider("sepiaSlider");
					if (slider != null)
					{
						slider.SetValue((int)(ShaderUniforms.SepiaLevel * 100f));
					}
					GuiElementSlider slider2 = this.composer.GetSlider("contrastSlider");
					if (slider2 == null)
					{
						return;
					}
					slider2.SetValue((int)(ShaderUniforms.ExtraContrastLevel * 100f) + 100);
				}
			}
		}

		internal void OnGraphicsOptions(bool on)
		{
			int sliderWidth = 160;
			ElementBounds leftColumnLeftText = ElementBounds.Fixed(0.0, 82.0, 225.0, 42.0);
			ElementBounds leftColumnRightSlider = ElementBounds.Fixed(235.0, 85.0, (double)sliderWidth, 20.0);
			ElementBounds rightColumnLeftText = ElementBounds.Fixed(470.0, 90.0, 225.0, 42.0);
			ElementBounds rightColumnRightSlider = ElementBounds.Fixed(705.0, 119.0, (double)sliderWidth, 20.0);
			ElementBounds.Fixed(0.0, 0.0, 30.0, 30.0).WithFixedPadding(10.0, 2.0);
			string[] hoverTexts = new string[]
			{
				(this.handler.MaxViewDistanceAlarmValue == null) ? Lang.Get("setting-hover-viewdist-singleplayer", Array.Empty<object>()) : Lang.Get("setting-hover-viewdist", Array.Empty<object>()),
				Lang.Get("setting-hover-gamma", Array.Empty<object>()),
				Lang.Get("setting-hover-sepia", Array.Empty<object>()),
				Lang.Get("setting-hover-fov", Array.Empty<object>()),
				Lang.Get("setting-hover-guiscale", Array.Empty<object>()),
				Lang.Get("setting-hover-maxfps", Array.Empty<object>()),
				Lang.Get("setting-hover-resolution", Array.Empty<object>()),
				Lang.Get("setting-hover-smoothshadows", Array.Empty<object>()),
				Lang.Get("setting-hover-vsync", Array.Empty<object>()),
				Lang.Get("setting-hover-fxaa", Array.Empty<object>()),
				Lang.Get("setting-hover-bloom", Array.Empty<object>()),
				Lang.Get("setting-hover-abloom", Array.Empty<object>()),
				Lang.Get("setting-hover-godrays", Array.Empty<object>()),
				Lang.Get("setting-hover-particles", Array.Empty<object>()),
				Lang.Get("setting-hover-grasswaves", Array.Empty<object>()),
				Lang.Get("setting-hover-dynalight", Array.Empty<object>()),
				Lang.Get("setting-hover-dynashade", Array.Empty<object>()),
				Lang.Get("setting-hover-contrast", Array.Empty<object>()),
				Lang.Get("setting-hover-hqanimation", Array.Empty<object>()),
				Lang.Get("setting-hover-optimizeram", Array.Empty<object>()),
				Lang.Get("setting-hover-occlusionculling", Array.Empty<object>()),
				Lang.Get("setting-hover-foamandshinyeffect", Array.Empty<object>()),
				Lang.Get("setting-hover-ssao", Array.Empty<object>()),
				"setting-hover-radeonhdfix",
				Lang.Get("setting-hover-instancedgrass", Array.Empty<object>()),
				Lang.Get("setting-hover-chunkuploadratelimiter", Array.Empty<object>())
			};
			string[] presetIds = GraphicsPreset.Presets.Select((GraphicsPreset p) => p.PresetId.ToString() ?? "").ToArray<string>();
			string[] presetNames = GraphicsPreset.Presets.Select((GraphicsPreset p) => Lang.Get(p.Langcode, Array.Empty<object>()) ?? "").ToArray<string>();
			string linktext = (ClientSettings.ShowMoreGfxOptions ? Lang.Get("general-lessoptions", Array.Empty<object>()) : Lang.Get("general-moreoptions", Array.Empty<object>()));
			CairoFont titleFont = CairoFont.WhiteSmallishText().Clone().WithWeight(FontWeight.Bold);
			titleFont.Color[3] = 0.6;
			this.composer = this.ComposerHeader("gamesettings-graphics", "graphics").AddRichtext(new RichTextComponentBase[]
			{
				new LinkTextComponent(this.handler.Api, linktext, CairoFont.WhiteDetailText(), new Action<LinkTextComponent>(this.OnMoreOptions))
			}, rightColumnLeftText = rightColumnLeftText.FlatCopy(), null).AddStaticText(Lang.Get("setting-column-appear", Array.Empty<object>()), titleFont, rightColumnLeftText = rightColumnLeftText.BelowCopy(0.0, 10.0, 0.0, 0.0).WithFixedWidth(250.0), null)
				.AddStaticText(Lang.Get("setting-name-gamma", Array.Empty<object>()), CairoFont.WhiteSmallishText(), rightColumnLeftText = rightColumnLeftText.BelowCopy(0.0, -4.0, 0.0, 0.0).WithFixedWidth(200.0), null)
				.AddSlider(new ActionConsumable<int>(this.onGammaChanged), rightColumnRightSlider = rightColumnRightSlider.BelowCopy(0.0, 45.0, 0.0, 0.0), "gammaSlider")
				.AddHoverText(hoverTexts[1], CairoFont.WhiteSmallText(), 250, rightColumnLeftText.FlatCopy().WithFixedHeight(25.0), null)
				.AddStaticText(Lang.Get("setting-name-dynamiccolorgrading", Array.Empty<object>()), CairoFont.WhiteSmallishText(), rightColumnLeftText = rightColumnLeftText.BelowCopy(0.0, -8.0, 0.0, 0.0), null)
				.AddHoverText(Lang.Get("setting-hover-dynamiccolorgrading", Array.Empty<object>()), CairoFont.WhiteSmallText(), 250, rightColumnLeftText.FlatCopy().WithFixedHeight(25.0), null)
				.AddSwitch(new Action<bool>(this.onDynamicGradingToggled), rightColumnRightSlider = rightColumnRightSlider.BelowCopy(0.0, 15.0, 0.0, 0.0), "dynamicColorGradingSwitch", 30.0, 4.0)
				.AddStaticText(Lang.Get("setting-name-contrast", Array.Empty<object>()), CairoFont.WhiteSmallishText(), rightColumnLeftText = rightColumnLeftText.BelowCopy(0.0, 10.0, 0.0, 0.0), null)
				.AddSlider(new ActionConsumable<int>(this.onContrastChanged), rightColumnRightSlider = rightColumnRightSlider.BelowCopy(0.0, 21.0, 0.0, 0.0).WithFixedSize((double)sliderWidth, 20.0), "contrastSlider")
				.AddHoverText(hoverTexts[17], CairoFont.WhiteSmallText(), 250, rightColumnLeftText.FlatCopy().WithFixedHeight(25.0), null)
				.AddStaticText(Lang.Get("setting-name-sepia", Array.Empty<object>()), CairoFont.WhiteSmallishText(), rightColumnLeftText = rightColumnLeftText.BelowCopy(0.0, 0.0, 0.0, 0.0), null)
				.AddSlider(new ActionConsumable<int>(this.onSepiaLevelChanged), rightColumnRightSlider = rightColumnRightSlider.BelowCopy(0.0, 21.0, 0.0, 0.0), "sepiaSlider")
				.AddHoverText(hoverTexts[2], CairoFont.WhiteSmallText(), 250, rightColumnLeftText.FlatCopy().WithFixedHeight(25.0), null)
				.AddStaticText(Lang.Get("setting-name-abloom", Array.Empty<object>()), CairoFont.WhiteSmallishText(), rightColumnLeftText = rightColumnLeftText.BelowCopy(0.0, 0.0, 0.0, 0.0), null)
				.AddSlider(new ActionConsumable<int>(this.onAmbientBloomChanged), rightColumnRightSlider = rightColumnRightSlider.BelowCopy(0.0, 21.0, 0.0, 0.0).WithFixedSize((double)sliderWidth, 20.0), "ambientBloomSlider")
				.AddHoverText(hoverTexts[11], CairoFont.WhiteSmallText(), 250, rightColumnLeftText.FlatCopy().WithFixedHeight(25.0), null)
				.AddStaticText(Lang.Get("setting-name-fov", Array.Empty<object>()), CairoFont.WhiteSmallishText(), rightColumnLeftText = rightColumnLeftText.BelowCopy(0.0, 0.0, 0.0, 0.0), null)
				.AddSlider(new ActionConsumable<int>(this.onVowChanged), rightColumnRightSlider = rightColumnRightSlider.BelowCopy(0.0, 21.0, 0.0, 0.0), "fovSlider")
				.AddHoverText(hoverTexts[3], CairoFont.WhiteSmallText(), 250, rightColumnLeftText.FlatCopy().WithFixedHeight(25.0), null)
				.AddStaticText(Lang.Get("setting-name-windowmode", Array.Empty<object>()), CairoFont.WhiteSmallishText(), rightColumnLeftText = rightColumnLeftText.BelowCopy(0.0, -2.0, 0.0, 0.0), null)
				.AddDropDown(new string[] { "0", "1", "2", "3" }, new string[]
				{
					Lang.Get("windowmode-normal", Array.Empty<object>()),
					Lang.Get("windowmode-fullscreen", Array.Empty<object>()),
					Lang.Get("windowmode-maxborderless", Array.Empty<object>()),
					Lang.Get("windowmode-fullscreen-ontop", Array.Empty<object>())
				}, GuiCompositeSettings.GetWindowModeIndex(), new SelectionChangedDelegate(this.OnWindowModeChanged), rightColumnRightSlider = rightColumnRightSlider.BelowCopy(0.0, 18.0, 0.0, 0.0).WithFixedSize((double)sliderWidth, 26.0), "windowModeSwitch");
			if (ClientSettings.ShowMoreGfxOptions)
			{
				this.composer.AddStaticText(Lang.Get("setting-name-maxfps", Array.Empty<object>()), CairoFont.WhiteSmallishText(), rightColumnLeftText = rightColumnLeftText.BelowCopy(0.0, -3.0, 0.0, 0.0).WithFixedHeight(40.0), null).AddSlider(new ActionConsumable<int>(this.onMaxFpsChanged), rightColumnRightSlider = rightColumnRightSlider.BelowCopy(0.0, 15.0, 0.0, 0.0).WithFixedSize((double)sliderWidth, 20.0), "maxFpsSlider").AddHoverText(hoverTexts[5], CairoFont.WhiteSmallText(), 250, rightColumnLeftText.FlatCopy().WithFixedHeight(25.0), null)
					.AddStaticText(Lang.Get("setting-name-vsync", Array.Empty<object>()), CairoFont.WhiteSmallishText(), rightColumnLeftText = rightColumnLeftText.BelowCopy(0.0, 4.0, 0.0, 0.0), null)
					.AddHoverText(hoverTexts[8], CairoFont.WhiteSmallText(), 250, rightColumnLeftText.FlatCopy().WithFixedHeight(25.0), null)
					.AddDropDown(new string[] { "0", "1", "2" }, new string[]
					{
						Lang.Get("Off", Array.Empty<object>()),
						Lang.Get("On", Array.Empty<object>()),
						Lang.Get("On + Sleep", Array.Empty<object>())
					}, ClientSettings.VsyncMode, new SelectionChangedDelegate(this.onVsyncChanged), rightColumnRightSlider = rightColumnRightSlider.BelowCopy(0.0, 18.0, 0.0, 0.0).WithFixedSize((double)sliderWidth, 26.0), "vsyncMode")
					.AddStaticText(Lang.Get("setting-name-optimizeram", Array.Empty<object>()), CairoFont.WhiteSmallishText(), rightColumnLeftText = rightColumnLeftText.BelowCopy(0.0, 4.0, 0.0, 0.0), null)
					.AddHoverText(hoverTexts[19], CairoFont.WhiteSmallText(), 250, rightColumnLeftText.FlatCopy().WithFixedHeight(25.0), null)
					.AddDropDown(new string[] { "1", "2" }, new string[]
					{
						Lang.Get("Optimize somewhat", Array.Empty<object>()),
						Lang.Get("Aggressively optimize ram", Array.Empty<object>())
					}, ClientSettings.OptimizeRamMode - 1, new SelectionChangedDelegate(this.onOptimizeRamChanged), rightColumnRightSlider = rightColumnRightSlider.BelowCopy(0.0, 18.0, 0.0, 0.0).WithFixedSize((double)sliderWidth, 26.0), "optimizeRamMode")
					.AddStaticText(Lang.Get("setting-name-occlusionculling", Array.Empty<object>()), CairoFont.WhiteSmallishText(), rightColumnLeftText = rightColumnLeftText.BelowCopy(0.0, 3.0, 0.0, 0.0), null)
					.AddHoverText(hoverTexts[20], CairoFont.WhiteSmallText(), 250, rightColumnLeftText.FlatCopy().WithFixedHeight(25.0), null)
					.AddSwitch(new Action<bool>(this.onOcclusionCullingChanged), rightColumnRightSlider = rightColumnRightSlider.BelowCopy(0.0, 17.0, 0.0, 0.0), "occlusionCullingSwitch", 30.0, 4.0)
					.AddStaticText(Lang.Get("setting-name-lodbiasfar", Array.Empty<object>()), CairoFont.WhiteSmallishText(), rightColumnLeftText = rightColumnLeftText.BelowCopy(0.0, 4.0, 0.0, 0.0), null)
					.AddHoverText(Lang.Get("setting-hover-lodbiasfar", Array.Empty<object>()), CairoFont.WhiteSmallText(), 250, rightColumnLeftText.FlatCopy().WithFixedHeight(25.0), null)
					.AddSlider(new ActionConsumable<int>(this.onLodbiasFarChanged), rightColumnRightSlider = rightColumnRightSlider.BelowCopy(0.0, 21.0, 0.0, 0.0).WithFixedSize((double)sliderWidth, 20.0), "lodbiasfarSlider")
					.AddStaticText(Lang.Get("setting-name-windowborder", Array.Empty<object>()), CairoFont.WhiteSmallishText(), rightColumnLeftText.BelowCopy(0.0, 4.0, 0.0, 0.0), null)
					.AddDropDown(new string[] { "0", "1", "2" }, new string[]
					{
						Lang.Get("windowborder-resizable", Array.Empty<object>()),
						Lang.Get("windowborder-fixed", Array.Empty<object>()),
						Lang.Get("windowborder-hidden", Array.Empty<object>())
					}, (int)ScreenManager.Platform.WindowBorder, new SelectionChangedDelegate(this.OnWindowBorderChanged), rightColumnRightSlider.BelowCopy(0.0, 18.0, 0.0, 0.0).WithFixedSize((double)sliderWidth, 26.0), "windowBorder");
			}
			this.composer.AddStaticText(Lang.Get("setting-name-preset", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftColumnLeftText = leftColumnLeftText.FlatCopy().WithFixedOffset(0.0, 5.0), null).AddDropDown(presetIds, presetNames, ClientSettings.GraphicsPresetId, new SelectionChangedDelegate(this.onPresetChanged), leftColumnRightSlider = leftColumnRightSlider.FlatCopy().WithFixedSize((double)sliderWidth, 30.0), "graphicsPreset").AddStaticText(Lang.Get("setting-column-graphics", Array.Empty<object>()), titleFont, leftColumnLeftText = leftColumnLeftText.BelowCopy(0.0, 15.0, 0.0, 0.0), null)
				.AddStaticText(Lang.Get("setting-name-viewdist", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftColumnLeftText = leftColumnLeftText.BelowCopy(0.0, -6.0, 0.0, 0.0), null)
				.AddSlider(new ActionConsumable<int>(this.onViewdistanceChanged), leftColumnRightSlider = leftColumnRightSlider.BelowCopy(0.0, 68.0, 0.0, 0.0).WithFixedSize((double)sliderWidth, 20.0), "viewDistanceSlider")
				.AddHoverText(hoverTexts[0], CairoFont.WhiteSmallText(), 250, leftColumnLeftText.FlatCopy().WithFixedHeight(25.0), null);
			if (ClientSettings.ShowMoreGfxOptions)
			{
				this.composer.AddStaticText(Lang.Get("setting-name-smoothshadows", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftColumnLeftText = leftColumnLeftText.BelowCopy(0.0, 0.0, 0.0, 0.0), null).AddHoverText(hoverTexts[7], CairoFont.WhiteSmallText(), 250, leftColumnLeftText.FlatCopy().WithFixedHeight(25.0), null).AddSwitch(new Action<bool>(this.onSmoothShadowsToggled), leftColumnRightSlider = leftColumnRightSlider.BelowCopy(0.0, 15.0, 0.0, 0.0), "smoothShadowsLever", 30.0, 4.0)
					.AddStaticText(Lang.Get("setting-name-fxaa", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftColumnLeftText = leftColumnLeftText.BelowCopy(0.0, 0.0, 0.0, 0.0), null)
					.AddHoverText(hoverTexts[9], CairoFont.WhiteSmallText(), 250, leftColumnLeftText.FlatCopy().WithFixedHeight(25.0), null)
					.AddSwitch(new Action<bool>(this.onFxaaChanged), leftColumnRightSlider = leftColumnRightSlider.BelowCopy(0.0, 12.0, 0.0, 0.0), "FxaaSwitch", 30.0, 4.0)
					.AddStaticText(Lang.Get("setting-name-grasswaves", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftColumnLeftText = leftColumnLeftText.BelowCopy(0.0, 0.0, 0.0, 0.0), null)
					.AddHoverText(hoverTexts[14], CairoFont.WhiteSmallText(), 250, leftColumnLeftText.FlatCopy().WithFixedHeight(25.0), null)
					.AddSwitch(new Action<bool>(this.onWavingFoliageChanged), leftColumnRightSlider = leftColumnRightSlider.BelowCopy(0.0, 12.0, 0.0, 0.0), "wavingFoliageSwitch", 30.0, 4.0)
					.AddStaticText(Lang.Get("setting-name-foamandshinyeffect", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftColumnLeftText = leftColumnLeftText.BelowCopy(0.0, 0.0, 0.0, 0.0), null)
					.AddHoverText(hoverTexts[21], CairoFont.WhiteSmallText(), 250, leftColumnLeftText.FlatCopy().WithFixedHeight(25.0), null)
					.AddSwitch(new Action<bool>(this.onFoamAndShinyEffectChanged), leftColumnRightSlider = leftColumnRightSlider.BelowCopy(0.0, 12.0, 0.0, 0.0), "liquidFoamEffectSwitch", 30.0, 4.0)
					.AddStaticText(Lang.Get("setting-name-bloom", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftColumnLeftText = leftColumnLeftText.BelowCopy(0.0, 0.0, 0.0, 0.0), null)
					.AddHoverText(hoverTexts[10], CairoFont.WhiteSmallText(), 250, leftColumnLeftText.FlatCopy().WithFixedHeight(25.0), null)
					.AddSwitch(new Action<bool>(this.onBloomChanged), leftColumnRightSlider = leftColumnRightSlider.BelowCopy(0.0, 12.0, 0.0, 0.0), "BloomSwitch", 30.0, 4.0)
					.AddStaticText(Lang.Get("setting-name-clouds", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftColumnLeftText = leftColumnLeftText.BelowCopy(0.0, 0.0, 0.0, 0.0).WithFixedHeight(39.0), null)
					.AddHoverText(Lang.Get("settings-hover-clouds", Array.Empty<object>()), CairoFont.WhiteSmallText(), 250, leftColumnLeftText.FlatCopy().WithFixedHeight(25.0), null)
					.AddDropDown(new string[] { "0", "1", "2" }, new string[]
					{
						Lang.Get("settings-clouds-off", Array.Empty<object>()),
						Lang.Get("settings-clouds-volumetric", Array.Empty<object>()),
						Lang.Get("settings-clouds-classic", Array.Empty<object>())
					}, ClientSettings.CloudRenderMode, new SelectionChangedDelegate(this.onCloudsChanged), leftColumnRightSlider = leftColumnRightSlider.BelowCopy(0.0, 18.0, 0.0, 0.0).WithFixedSize((double)sliderWidth, 26.0), "clouds")
					.AddStaticText(Lang.Get("setting-name-godrays", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftColumnLeftText = leftColumnLeftText.BelowCopy(0.0, 3.0, 0.0, 0.0).WithFixedHeight(39.0), null)
					.AddSwitch(new Action<bool>(this.onGodRaysToggled), leftColumnRightSlider = leftColumnRightSlider.BelowCopy(0.0, 15.0, 0.0, 0.0).WithFixedSize((double)sliderWidth, 20.0), "godraySwitch", 30.0, 4.0)
					.AddHoverText(hoverTexts[12], CairoFont.WhiteSmallText(), 250, leftColumnLeftText.FlatCopy().WithFixedHeight(25.0), null)
					.AddStaticText(Lang.Get("setting-name-ssao", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftColumnLeftText = leftColumnLeftText.BelowCopy(0.0, 4.0, 0.0, 0.0).WithFixedHeight(36.0), null)
					.AddHoverText(hoverTexts[22], CairoFont.WhiteSmallText(), 250, leftColumnLeftText.FlatCopy().WithFixedHeight(25.0), null)
					.AddSlider(new ActionConsumable<int>(this.onSsaoChanged), leftColumnRightSlider = leftColumnRightSlider.BelowCopy(0.0, 17.0, 0.0, 0.0).WithFixedSize((double)sliderWidth, 20.0), "ssaoSlider")
					.AddStaticText(Lang.Get("setting-name-shadows", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftColumnLeftText = leftColumnLeftText.BelowCopy(0.0, 4.0, 0.0, 0.0), null)
					.AddSlider(new ActionConsumable<int>(this.onShadowsChanged), leftColumnRightSlider = leftColumnRightSlider.BelowCopy(0.0, 21.0, 0.0, 0.0).WithFixedSize((double)sliderWidth, 20.0), "shadowsSlider")
					.AddHoverText(hoverTexts[16], CairoFont.WhiteSmallText(), 250, leftColumnLeftText.FlatCopy().WithFixedHeight(25.0), null)
					.AddStaticText(Lang.Get("setting-name-particles", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftColumnLeftText = leftColumnLeftText.BelowCopy(0.0, 4.0, 0.0, 0.0), null)
					.AddSlider(new ActionConsumable<int>(this.onParticleLevelChanged), leftColumnRightSlider = leftColumnRightSlider.BelowCopy(0.0, 21.0, 0.0, 0.0).WithFixedSize((double)sliderWidth, 20.0), "particleSlider")
					.AddHoverText(hoverTexts[13], CairoFont.WhiteSmallText(), 250, leftColumnLeftText.FlatCopy().WithFixedHeight(25.0), null)
					.AddStaticText(Lang.Get("setting-name-dynalight", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftColumnLeftText = leftColumnLeftText.BelowCopy(0.0, 4.0, 0.0, 0.0), null)
					.AddSlider(new ActionConsumable<int>(this.onDynamicLightsChanged), leftColumnRightSlider = leftColumnRightSlider.BelowCopy(0.0, 21.0, 0.0, 0.0).WithFixedSize((double)sliderWidth, 20.0), "dynamicLightsSlider")
					.AddHoverText(hoverTexts[15], CairoFont.WhiteSmallText(), 250, leftColumnLeftText.FlatCopy().WithFixedHeight(25.0), null)
					.AddStaticText(Lang.Get("setting-name-resolution", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftColumnLeftText = leftColumnLeftText.BelowCopy(0.0, 4.0, 0.0, 0.0), null)
					.AddHoverText(hoverTexts[6], CairoFont.WhiteSmallText(), 250, leftColumnLeftText.FlatCopy().WithFixedHeight(25.0), null)
					.AddSlider(new ActionConsumable<int>(this.onResolutionChanged), leftColumnRightSlider.BelowCopy(0.0, 21.0, 0.0, 0.0), "resolutionSlider")
					.AddRichtext(Lang.Get("help-framerateissues", Array.Empty<object>()), CairoFont.WhiteDetailText(), leftColumnLeftText.BelowCopy(0.0, 5.0, 0.0, 0.0), null)
					.EndChildElements();
			}
			else
			{
				this.composer.AddRichtext(Lang.Get("help-moresettingsavailable", new object[] { linktext }), CairoFont.WhiteDetailText(), leftColumnLeftText = leftColumnLeftText.BelowCopy(0.0, 225.0, 440.0, 0.0), null).AddRichtext(Lang.Get("help-framerateissues", Array.Empty<object>()), CairoFont.WhiteDetailText(), leftColumnLeftText.BelowCopy(0.0, 125.0, 0.0, 0.0), null);
			}
			this.composer.GetDropDown("graphicsPreset").listMenu.MaxHeight = 330;
			this.composer.Compose(true);
			this.handler.LoadComposer(this.composer);
			this.SetGfxValues();
		}

		private void onCloudsChanged(string newvalue, bool selected)
		{
			ClientSettings.CloudRenderMode = newvalue.ToInt(0);
		}

		private void onDynamicGradingToggled(bool on)
		{
			ClientSettings.DynamicColorGrading = on;
			if (!on)
			{
				ScreenManager.Platform.ShaderUniforms.SepiaLevel = ClientSettings.SepiaLevel;
				ScreenManager.Platform.ShaderUniforms.ExtraContrastLevel = ClientSettings.ExtraContrastLevel;
			}
			this.composer.GetSlider("sepiaSlider").SetValue((int)(ScreenManager.Platform.ShaderUniforms.SepiaLevel * 100f));
			this.composer.GetSlider("sepiaSlider").Enabled = !ClientSettings.DynamicColorGrading;
			this.composer.GetSlider("contrastSlider").SetValue((int)(ScreenManager.Platform.ShaderUniforms.ExtraContrastLevel * 100f) + 100);
			this.composer.GetSlider("contrastSlider").Enabled = !ClientSettings.DynamicColorGrading;
		}

		private void onVsyncChanged(string newvalue, bool selected)
		{
			ClientSettings.VsyncMode = newvalue.ToInt(0);
		}

		private void OnMoreOptions(LinkTextComponent comp)
		{
			ClientSettings.ShowMoreGfxOptions = !ClientSettings.ShowMoreGfxOptions;
			this.OnGraphicsOptions(true);
		}

		private void SetGfxValues()
		{
			this.composer.GetSlider("viewDistanceSlider").SetValues(ClientSettings.ViewDistance, 32, 1536, 32, " blocks");
			this.composer.GetSlider("viewDistanceSlider").OnSliderTooltip = delegate(int value)
			{
				string val = Lang.Get("createworld-worldheight", new object[] { value });
				if (value <= 512)
				{
					return val;
				}
				return val + "\n" + Lang.Get("vram-warning", Array.Empty<object>());
			};
			this.composer.GetSlider("viewDistanceSlider").TriggerOnlyOnMouseUp(true);
			if (this.handler.MaxViewDistanceAlarmValue != null)
			{
				this.composer.GetSlider("viewDistanceSlider").SetAlarmValue(this.handler.MaxViewDistanceAlarmValue.Value);
			}
			if (ClientSettings.ShowMoreGfxOptions)
			{
				this.composer.GetSwitch("smoothShadowsLever").On = ClientSettings.SmoothShadows;
				this.composer.GetSwitch("FxaaSwitch").On = ClientSettings.FXAA;
				this.composer.GetDropDown("optimizeRamMode").SetSelectedIndex(ClientSettings.OptimizeRamMode - 1);
				this.composer.GetSwitch("occlusionCullingSwitch").On = ClientSettings.Occlusionculling;
				this.composer.GetSwitch("wavingFoliageSwitch").On = ClientSettings.WavingFoliage;
				this.composer.GetSwitch("liquidFoamEffectSwitch").On = ClientSettings.LiquidFoamAndShinyEffect;
				this.composer.GetSwitch("BloomSwitch").On = ClientSettings.Bloom;
				this.composer.GetSwitch("godraySwitch").On = ClientSettings.GodRayQuality > 0;
				this.composer.GetDropDown("windowModeSwitch").SetSelectedIndex(ClientSettings.CloudRenderMode);
				this.composer.GetSlider("ambientBloomSlider").SetValues((int)ClientSettings.AmbientBloomLevel, 0, 100, 10, "%");
				this.composer.GetSlider("ambientBloomSlider").TriggerOnlyOnMouseUp(true);
				this.composer.GetSlider("ssaoSlider").SetValues(ClientSettings.SSAOQuality, 0, 2, 1, "");
				string[] qualityssao = new string[]
				{
					Lang.Get("Off", Array.Empty<object>()),
					Lang.Get("Medium quality", Array.Empty<object>()),
					Lang.Get("High quality", Array.Empty<object>())
				};
				this.composer.GetSlider("ssaoSlider").OnSliderTooltip = (int value) => qualityssao[value];
				this.composer.GetSlider("ssaoSlider").ComposeHoverTextElement();
				this.composer.GetSlider("ssaoSlider").TriggerOnlyOnMouseUp(true);
				this.composer.GetSlider("shadowsSlider").SetValues(ClientSettings.ShadowMapQuality, 0, 4, 1, "");
				string[] quality2 = new string[]
				{
					Lang.Get("Off", Array.Empty<object>()),
					Lang.Get("Low quality", Array.Empty<object>()),
					Lang.Get("Medium quality", Array.Empty<object>()),
					Lang.Get("High quality", Array.Empty<object>()),
					Lang.Get("Very high quality", Array.Empty<object>())
				};
				this.composer.GetSlider("shadowsSlider").OnSliderTooltip = (int value) => quality2[value];
				this.composer.GetSlider("shadowsSlider").ComposeHoverTextElement();
				this.composer.GetSlider("shadowsSlider").TriggerOnlyOnMouseUp(true);
				this.composer.GetSlider("particleSlider").SetValues(ClientSettings.ParticleLevel, 0, 100, 2, " %");
				this.composer.GetSlider("dynamicLightsSlider").SetValues(ClientSettings.MaxDynamicLights, 0, 100, 1, " " + Lang.Get("units-lightsources", Array.Empty<object>()));
				this.composer.GetSlider("dynamicLightsSlider").OnSliderTooltip = delegate(int value)
				{
					if (value != 0)
					{
						return value.ToString() + " " + Lang.Get("units-lightsources", Array.Empty<object>());
					}
					return Lang.Get("disabled", Array.Empty<object>());
				};
				this.composer.GetSlider("dynamicLightsSlider").TriggerOnlyOnMouseUp(true);
				this.composer.GetSlider("resolutionSlider").SetValues((int)(ClientSettings.SSAA * 100f), 25, 100, 25, " %");
				this.composer.GetSlider("resolutionSlider").OnSliderTooltip = delegate(int value)
				{
					float ssaa = (float)value / 100f;
					return ssaa.ToString() + "x (" + ((int)(ssaa * ssaa * 100f)).ToString() + "%)";
				};
				this.composer.GetSlider("resolutionSlider").TriggerOnlyOnMouseUp(true);
				this.composer.GetSlider("lodbiasfarSlider").SetValues((int)(ClientSettings.LodBiasFar * 100f), 35, 100, 1, " %");
				this.composer.GetSlider("lodbiasfarSlider").OnSliderTooltip = delegate(int value)
				{
					float lldb = (float)value / 100f;
					return lldb.ToString() + "x (" + ((int)(lldb * lldb * 100f)).ToString() + "%)";
				};
				this.composer.GetSlider("lodbiasfarSlider").TriggerOnlyOnMouseUp(true);
			}
			this.composer.GetSlider("gammaSlider").Enabled = true;
			this.composer.GetSlider("gammaSlider").OnSliderTooltip = null;
			this.composer.GetSlider("gammaSlider").ComposeHoverTextElement();
			this.composer.GetSlider("gammaSlider").SetValues((int)Math.Round((double)(ClientSettings.GammaLevel * 100f)), 30, 300, 5, "");
			this.composer.GetSwitch("dynamicColorGradingSwitch").On = ClientSettings.DynamicColorGrading;
			this.composer.GetSlider("sepiaSlider").SetValues((int)(ScreenManager.Platform.ShaderUniforms.SepiaLevel * 100f), 0, 100, 5, "");
			this.composer.GetSlider("sepiaSlider").Enabled = !ClientSettings.DynamicColorGrading;
			this.composer.GetSlider("contrastSlider").SetValues((int)(ScreenManager.Platform.ShaderUniforms.ExtraContrastLevel * 100f) + 100, 100, 200, 10, "%");
			this.composer.GetSlider("contrastSlider").Enabled = !ClientSettings.DynamicColorGrading;
			this.composer.GetSlider("fovSlider").SetValues(ClientSettings.FieldOfView, 20, 150, 1, "°");
			this.composer.GetDropDown("windowModeSwitch").SetSelectedIndex(GuiCompositeSettings.GetWindowModeIndex());
			if (ClientSettings.ShowMoreGfxOptions)
			{
				this.composer.GetSlider("maxFpsSlider").SetValues(GameMath.Clamp(ClientSettings.MaxFPS, 15, 241), 15, 241, 1, "");
				this.composer.GetSlider("maxFpsSlider").OnSliderTooltip = delegate(int value)
				{
					if (value != 241)
					{
						return value.ToString();
					}
					return Lang.Get("unlimited", Array.Empty<object>());
				};
				this.composer.GetSlider("maxFpsSlider").ComposeHoverTextElement();
				this.composer.GetDropDown("vsyncMode").SetSelectedIndex(ClientSettings.VsyncMode);
			}
		}

		internal static int GetWindowModeIndex()
		{
			int windowMode = ClientSettings.GameWindowMode;
			if (ClientSettings.GameWindowMode == 2 && ScreenManager.Platform.WindowBorder != EnumWindowBorder.Hidden)
			{
				windowMode = 0;
			}
			return windowMode;
		}

		private void onPresetChanged(string id, bool on)
		{
			GraphicsPreset preset = GraphicsPreset.Presets[int.Parse(id)];
			if (preset.Langcode == "preset-custom")
			{
				return;
			}
			ClientSettings.GraphicsPresetId = preset.PresetId;
			ClientSettings.ViewDistance = preset.ViewDistance;
			ClientSettings.SmoothShadows = preset.SmoothLight;
			ClientSettings.FXAA = preset.FXAA;
			ClientSettings.SSAOQuality = preset.SSAO;
			ClientSettings.WavingFoliage = preset.WavingFoliage;
			ClientSettings.LiquidFoamAndShinyEffect = preset.LiquidFoamEffect;
			ClientSettings.Bloom = preset.Bloom;
			ClientSettings.GodRayQuality = ((preset.GodRays > false) ? 1 : 0);
			ClientSettings.ShadowMapQuality = preset.ShadowMapQuality;
			ClientSettings.ParticleLevel = preset.ParticleLevel;
			ClientSettings.MaxDynamicLights = preset.DynamicLights;
			ClientSettings.SSAA = preset.Resolution;
			ClientSettings.LodBiasFar = preset.LodBiasFar;
			this.SetGfxValues();
			ScreenManager.Platform.RebuildFrameBuffers();
			this.handler.ReloadShaders();
		}

		private void SetCustomPreset()
		{
			GraphicsPreset preset = GraphicsPreset.Presets.Where((GraphicsPreset p) => p.Langcode == "preset-custom").FirstOrDefault<GraphicsPreset>();
			ClientSettings.GraphicsPresetId = preset.PresetId;
			this.composer.GetDropDown("graphicsPreset").SetSelectedIndex(preset.PresetId);
		}

		private void OnWindowModeChanged(string code, bool selected)
		{
			GuiCompositeSettings.SetWindowMode(code.ToInt(0));
		}

		internal static void SetWindowMode(int mode)
		{
			switch (mode)
			{
			case 1:
				ScreenManager.Platform.SetWindowAttribute(WindowAttribute.AutoIconify, true);
				ScreenManager.Platform.SetWindowState(WindowState.Fullscreen);
				ClientSettings.GameWindowMode = 1;
				return;
			case 2:
				ClientSettings.WindowBorder = 2;
				ScreenManager.Platform.WindowBorder = EnumWindowBorder.Hidden;
				if (ScreenManager.Platform.GetWindowState() == WindowState.Maximized)
				{
					ScreenManager.Platform.SetWindowState(WindowState.Normal);
				}
				ScreenManager.Platform.SetWindowState(WindowState.Maximized);
				ClientSettings.GameWindowMode = 2;
				return;
			case 3:
				ScreenManager.Platform.SetWindowAttribute(WindowAttribute.AutoIconify, false);
				ScreenManager.Platform.SetWindowState(WindowState.Fullscreen);
				ClientSettings.GameWindowMode = 3;
				return;
			default:
				ScreenManager.Platform.SetWindowAttribute(WindowAttribute.AutoIconify, false);
				ScreenManager.Platform.SetWindowState(WindowState.Normal);
				if (ScreenManager.Platform.WindowBorder != EnumWindowBorder.Resizable)
				{
					ScreenManager.Platform.WindowBorder = EnumWindowBorder.Resizable;
					ClientSettings.WindowBorder = 0;
				}
				ClientSettings.GameWindowMode = 0;
				return;
			}
		}

		private void OnWindowBorderChanged(string newval, bool on)
		{
			int val;
			int.TryParse(newval, out val);
			ClientSettings.WindowBorder = val;
			if (ClientSettings.GameWindowMode == 2 && val != 2)
			{
				ClientSettings.GameWindowMode = 0;
			}
		}

		private void onOptimizeRamChanged(string code, bool selected)
		{
			ClientSettings.OptimizeRamMode = code.ToInt(0);
		}

		private void onOcclusionCullingChanged(bool on)
		{
			ClientSettings.Occlusionculling = on;
		}

		private bool onResolutionChanged(int newval)
		{
			ClientSettings.SSAA = (float)newval / 100f;
			ScreenManager.Platform.RebuildFrameBuffers();
			this.SetCustomPreset();
			return true;
		}

		private bool onLodbiasFarChanged(int newval)
		{
			ClientSettings.LodBiasFar = (float)newval / 100f;
			this.SetCustomPreset();
			return true;
		}

		private bool onDynamicLightsChanged(int value)
		{
			ClientSettings.MaxDynamicLights = value;
			this.handler.ReloadShaders();
			this.SetCustomPreset();
			return true;
		}

		private void onWavingFoliageChanged(bool on)
		{
			ClientSettings.WavingFoliage = on;
			this.handler.ReloadShaders();
			this.SetCustomPreset();
		}

		private void onFoamAndShinyEffectChanged(bool on)
		{
			ClientSettings.LiquidFoamAndShinyEffect = on;
			this.handler.ReloadShaders();
			this.SetCustomPreset();
		}

		private bool onParticleLevelChanged(int level)
		{
			ClientSettings.ParticleLevel = level;
			this.SetCustomPreset();
			return true;
		}

		private bool onMaxFpsChanged(int fps)
		{
			ClientSettings.MaxFPS = fps;
			return true;
		}

		private bool onSepiaLevelChanged(int value)
		{
			ClientSettings.SepiaLevel = (float)value / 100f;
			return true;
		}

		private bool onGammaChanged(int value)
		{
			ClientSettings.GammaLevel = (float)value / 100f;
			return true;
		}

		private void onGodRaysToggled(bool on)
		{
			ClientSettings.GodRayQuality = ((on > false) ? 1 : 0);
			this.handler.ReloadShaders();
			this.SetCustomPreset();
		}

		private bool onShadowsChanged(int newvalue)
		{
			ClientSettings.ShadowMapQuality = newvalue;
			ScreenManager.Platform.RebuildFrameBuffers();
			this.handler.ReloadShaders();
			this.SetCustomPreset();
			return true;
		}

		private bool onLagspikeReductionChanged(int newvalue)
		{
			ClientSettings.ChunkVerticesUploadRateLimiter = newvalue;
			return true;
		}

		private bool onAmbientBloomChanged(int newvalue)
		{
			ClientSettings.AmbientBloomLevel = (float)newvalue;
			this.handler.ReloadShaders();
			this.SetCustomPreset();
			return true;
		}

		private bool onContrastChanged(int newvalue)
		{
			ClientSettings.ExtraContrastLevel = (float)(newvalue - 100) / 100f;
			this.SetCustomPreset();
			return true;
		}

		private void onBloomChanged(bool on)
		{
			ClientSettings.Bloom = on;
			this.handler.ReloadShaders();
			this.SetCustomPreset();
		}

		private bool onVowChanged(int newvalue)
		{
			ClientSettings.FieldOfView = newvalue;
			return true;
		}

		private bool onGuiScaleChanged(int newsize)
		{
			ClientSettings.GUIScale = (float)newsize / 8f;
			this.updateButtonBounds();
			return true;
		}

		private void onFxaaChanged(bool fxaa)
		{
			ClientSettings.FXAA = fxaa;
			this.handler.ReloadShaders();
			this.SetCustomPreset();
		}

		private bool onSsaoChanged(int ssao)
		{
			ClientSettings.SSAOQuality = ssao;
			if (!this.handler.IsIngame)
			{
				ScreenManager.Platform.RebuildFrameBuffers();
				this.handler.ReloadShaders();
			}
			this.SetCustomPreset();
			return true;
		}

		internal void onSmoothShadowsToggled(bool newstate)
		{
			ClientSettings.SmoothShadows = newstate;
			this.SetCustomPreset();
		}

		internal bool onViewdistanceChanged(int newvalue)
		{
			ClientSettings.ViewDistance = newvalue;
			this.SetCustomPreset();
			return true;
		}

		private void OnMouseOptions(bool on)
		{
			this.mousecontrolsTabActive = true;
			this.LoadMouseCombinations();
			ElementBounds leftText = ElementBounds.Fixed(0.0, 85.0, 320.0, 42.0);
			ElementBounds rightSlider = ElementBounds.Fixed(340.0, 89.0, 200.0, 20.0);
			ElementBounds configListBounds = ElementBounds.Fixed(0.0, 0.0, 900.0 - 2.0 * GuiStyle.ElementToDialogPadding - 35.0, (double)(this.onMainscreen ? 140 : 114));
			ElementBounds insetBounds = configListBounds.ForkBoundingParent(5.0, 5.0, 5.0, 5.0);
			ElementBounds clipBounds = configListBounds.FlatCopy().WithParent(insetBounds);
			this.composer = this.ComposerHeader("gamesettings-mouse", "mouse").AddStaticText(Lang.Get("setting-name-mousesensivity", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftText.FlatCopy(), null).AddSlider(new ActionConsumable<int>(this.onMouseSensivityChanged), rightSlider = rightSlider.FlatCopy(), "mouseSensivitySlider")
				.AddStaticText(Lang.Get("setting-name-mousesmoothing", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 0.0, 0.0, 0.0), null)
				.AddSlider(new ActionConsumable<int>(this.onMouseSmoothingChanged), rightSlider = rightSlider.BelowCopy(0.0, 21.0, 0.0, 0.0), "mouseSmoothingSlider")
				.AddStaticText(Lang.Get("setting-name-mousewheelsensivity", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 0.0, 0.0, 0.0), null)
				.AddSlider(new ActionConsumable<int>(this.onMouseWheelSensivityChanged), rightSlider = rightSlider.BelowCopy(0.0, 21.0, 0.0, 0.0), "mouseWheelSensivitySlider")
				.AddStaticText(Lang.Get("setting-name-directmousemode", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 3.0, 0.0, 0.0), null)
				.AddSwitch(new Action<bool>(this.onMouseModeChanged), rightSlider = rightSlider.BelowCopy(0.0, 21.0, 0.0, 0.0), "directMouseModeSwitch", 30.0, 4.0)
				.AddHoverText(Lang.Get("setting-hover-directmousemode", Array.Empty<object>()), CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0), null)
				.AddStaticText(Lang.Get("setting-name-invertyaxis", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 3.0, 0.0, 0.0), null)
				.AddSwitch(new Action<bool>(this.onInvertYAxisChanged), rightSlider = rightSlider.BelowCopy(0.0, 21.0, 0.0, 0.0), "invertYAxisSwitch", 30.0, 4.0)
				.AddStaticText(Lang.Get("setting-name-itemCollectMode", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 2.0, 0.0, 0.0), null)
				.AddDropDown(new string[] { "0", "1" }, new string[]
				{
					Lang.Get("Always collect items", Array.Empty<object>()),
					Lang.Get("Only collect items when sneaking", Array.Empty<object>())
				}, ClientSettings.ItemCollectMode, new SelectionChangedDelegate(this.onCollectionModeChange), rightSlider.BelowCopy(0.0, 12.0, 0.0, 0.0).WithFixedWidth(200.0), "itemCollectionMode")
				.AddStaticText(Lang.Get("mousecontrols", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 20.0, 0.0, 0.0), null)
				.AddHoverText(Lang.Get("hover-mousecontrols", Array.Empty<object>()), CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(60.0), null)
				.AddInset(insetBounds.FixedUnder(leftText, -8.0), 3, 0.8f)
				.BeginClip(clipBounds)
				.AddConfigList(this.mousecontrolItems, new ConfigItemClickDelegate(this.OnMouseControlItemClick), CairoFont.WhiteSmallText().WithFontSize(18f), configListBounds, "configlist")
				.EndClip()
				.AddIf(this.onMainscreen)
				.AddStaticText(Lang.Get("mousecontrols-mainmenuwarning", Array.Empty<object>()), CairoFont.WhiteSmallText(), leftText.BelowCopy(0.0, 112.0, 500.0, 0.0), null)
				.EndIf()
				.EndChildElements()
				.Compose(true);
			this.handler.LoadComposer(this.composer);
			this.composer.GetSlider("mouseWheelSensivitySlider").SetValues((int)(ClientSettings.MouseWheelSensivity * 10f), 1, 100, 1, "");
			this.composer.GetSlider("mouseWheelSensivitySlider").OnSliderTooltip = (int value) => ((float)value / 10f).ToString() + "x";
			this.composer.GetSlider("mouseWheelSensivitySlider").ComposeHoverTextElement();
			this.composer.GetSlider("mouseSensivitySlider").SetValues(ClientSettings.MouseSensivity, 1, 200, 5, "");
			this.composer.GetSlider("mouseSmoothingSlider").SetValues(100 - ClientSettings.MouseSmoothing, 0, 95, 5, "");
			this.composer.GetSwitch("directMouseModeSwitch").SetValue(ClientSettings.DirectMouseMode);
			this.composer.GetSwitch("invertYAxisSwitch").SetValue(ClientSettings.InvertMouseYAxis);
		}

		private void OnMouseControlItemClick(int index, int indexNoTitle)
		{
			if (this.clickedItemIndex != null)
			{
				return;
			}
			this.mousecontrolItems[index].Value = "?";
			this.clickedItemIndex = new int?(index);
			int hotkeyIndex = (int)this.mousecontrolItems[this.clickedItemIndex.Value].Data;
			this.composer.GetConfigList("configlist").Refresh();
			string code = ScreenManager.hotkeyManager.HotKeys.GetKeyAtIndex(hotkeyIndex);
			HotKey keyComb = ScreenManager.hotkeyManager.HotKeys[code];
			this.keyCombClone = keyComb.Clone();
			this.hotkeyCapturer.BeginCapture();
			this.keyCombClone.CurrentMapping = this.hotkeyCapturer.CapturingKeyComb;
		}

		private void LoadMouseCombinations()
		{
			int hotkeyIndex = -1;
			int count = this.mousecontrolItems.Count;
			int? num = this.clickedItemIndex;
			if ((count >= num.GetValueOrDefault()) & (num != null))
			{
				hotkeyIndex = (int)this.mousecontrolItems[this.clickedItemIndex.Value].Data;
			}
			this.mousecontrolItems.Clear();
			int i = 0;
			List<ConfigItem>[] sortedItems = new List<ConfigItem>[this.sortOrder.Count];
			for (int j = 0; j < sortedItems.Length; j++)
			{
				sortedItems[j] = new List<ConfigItem>();
			}
			this.mousecontrolItems.Add(new ConfigItem
			{
				Type = EnumItemType.Title,
				Key = Lang.Get("mouseactions", Array.Empty<object>())
			});
			foreach (KeyValuePair<string, HotKey> val in ScreenManager.hotkeyManager.HotKeys)
			{
				HotKey kc = val.Value;
				if (this.clickedItemIndex != null && i == hotkeyIndex)
				{
					kc = this.keyCombClone;
				}
				string text = "?";
				if (kc.CurrentMapping != null)
				{
					text = kc.CurrentMapping.ToString();
				}
				ConfigItem item = new ConfigItem
				{
					Code = val.Key,
					Key = kc.Name,
					Value = text,
					Data = i
				};
				int index = this.mousecontrolItems.FindIndex((ConfigItem configitem) => configitem.Value == text);
				if (index != -1)
				{
					item.error = true;
					this.mousecontrolItems[index].error = true;
				}
				sortedItems[this.sortOrder[kc.KeyCombinationType]].Add(item);
				i++;
			}
			for (int k = 9; k < sortedItems.Length; k++)
			{
				this.mousecontrolItems.AddRange(sortedItems[k]);
			}
		}

		private void OnControlOptions(bool on)
		{
			this.mousecontrolsTabActive = false;
			this.LoadKeyCombinations();
			ElementBounds configListBounds = ElementBounds.Fixed(0.0, 0.0, 900.0 - 2.0 * GuiStyle.ElementToDialogPadding - 35.0, 400.0);
			ElementBounds insetBounds = configListBounds.ForkBoundingParent(5.0, 5.0, 5.0, 5.0);
			ElementBounds clipBounds = configListBounds.FlatCopy().WithParent(insetBounds);
			ElementBounds scrollbarBounds = ElementStdBounds.VerticalScrollbar(insetBounds);
			ElementBounds leftText = ElementBounds.Fixed(0.0, 41.0, 360.0, 42.0);
			ElementBounds rightSlider = ElementBounds.Fixed(490.0, 38.0, 200.0, 20.0);
			this.composer = this.ComposerHeader("gamesettings-controls", "controls").AddStaticText(Lang.Get("setting-name-noseparatectrlkeys", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 10.0, 120.0, 0.0), null).AddSwitch(new Action<bool>(this.onSeparateCtrl), rightSlider.BelowCopy(0.0, 32.0, 0.0, 0.0), "separateCtrl", 30.0, 4.0)
				.AddHoverText(Lang.Get("setting-hover-noseparatectrlkeys", Array.Empty<object>()), CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0), null)
				.AddStaticText(Lang.Get("keycontrols", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 5.0, -120.0, 0.0), null)
				.AddTextInput(leftText = leftText.BelowCopy(0.0, 5.0, 0.0, 0.0), delegate(string text)
				{
					if (!(this.currentSearchText == text))
					{
						this.currentSearchText = text;
						this.ReLoadKeyCombinations();
					}
				}, null, "searchField")
				.AddVerticalScrollbar(new Action<float>(this.OnNewScrollbarValue), scrollbarBounds.FixedUnder(leftText, 10.0), "scrollbar")
				.AddInset(insetBounds.FixedUnder(leftText, 10.0), 3, 0.8f)
				.BeginClip(clipBounds)
				.AddConfigList(this.keycontrolItems, new ConfigItemClickDelegate(this.OnKeyControlItemClick), CairoFont.WhiteSmallText().WithFontSize(18f), configListBounds, "configlist")
				.EndClip()
				.AddButton(Lang.Get("setting-name-setdefault", Array.Empty<object>()), new ActionConsumable(this.OnResetControls), ElementStdBounds.MenuButton(0f, EnumDialogArea.LeftFixed).FixedUnder(insetBounds, 10.0).WithFixedPadding(10.0, 2.0), EnumButtonStyle.Normal, null)
				.AddIf(this.handler.IsIngame)
				.AddButton(Lang.Get("setting-name-macroeditor", Array.Empty<object>()), new ActionConsumable(this.OnMacroEditor), ElementStdBounds.MenuButton(0f, EnumDialogArea.RightFixed).FixedUnder(insetBounds, 10.0).WithFixedPadding(10.0, 2.0), EnumButtonStyle.Normal, null)
				.EndIf()
				.EndChildElements()
				.Compose(true);
			this.handler.LoadComposer(this.composer);
			this.composer.GetSwitch("separateCtrl").SetValue(!ClientSettings.SeparateCtrl);
			this.composer.GetTextInput("searchField").SetPlaceHolderText(Lang.Get("Search...", Array.Empty<object>()));
			this.composer.GetTextInput("searchField").SetValue("", true);
			GuiElementConfigList configlist = this.composer.GetConfigList("configlist");
			configlist.errorFont = configlist.stdFont.Clone();
			configlist.errorFont.Color = GuiStyle.ErrorTextColor;
			configlist.Bounds.CalcWorldBounds();
			clipBounds.CalcWorldBounds();
			this.ReLoadKeyCombinations();
			this.composer.GetScrollbar("scrollbar").SetHeights((float)clipBounds.fixedHeight, (float)configlist.innerBounds.fixedHeight);
		}

		private bool OnMacroEditor()
		{
			this.handler.OnMacroEditor();
			return true;
		}

		private void onCollectionModeChange(string code, bool selected)
		{
			ClientSettings.ItemCollectMode = code.ToInt(0);
		}

		private void onMouseModeChanged(bool on)
		{
			ClientSettings.DirectMouseMode = on;
			ScreenManager.Platform.SetDirectMouseMode(on);
		}

		private void onInvertYAxisChanged(bool on)
		{
			ClientSettings.InvertMouseYAxis = on;
		}

		private void onSeparateCtrl(bool on)
		{
			ClientSettings.SeparateCtrl = !on;
			if (on)
			{
				HotKey keyComb = ScreenManager.hotkeyManager.HotKeys["shift"];
				keyComb.CurrentMapping = ScreenManager.hotkeyManager.HotKeys["sneak"].CurrentMapping;
				ClientSettings.Inst.SetKeyMapping("shift", keyComb.CurrentMapping);
				keyComb = ScreenManager.hotkeyManager.HotKeys["ctrl"];
				keyComb.CurrentMapping = ScreenManager.hotkeyManager.HotKeys["sprint"].CurrentMapping;
				ClientSettings.Inst.SetKeyMapping("ctrl", keyComb.CurrentMapping);
			}
			else
			{
				HotKey keyComb2 = ScreenManager.hotkeyManager.HotKeys["shift"];
				keyComb2.CurrentMapping = new KeyCombination
				{
					KeyCode = 1
				};
				ClientSettings.Inst.SetKeyMapping("shift", keyComb2.CurrentMapping);
				keyComb2 = ScreenManager.hotkeyManager.HotKeys["ctrl"];
				keyComb2.CurrentMapping = new KeyCombination
				{
					KeyCode = 3
				};
				ClientSettings.Inst.SetKeyMapping("ctrl", keyComb2.CurrentMapping);
			}
			this.OnControlOptions(true);
		}

		private bool onMouseWheelSensivityChanged(int val)
		{
			ClientSettings.MouseWheelSensivity = (float)val / 10f;
			return true;
		}

		private void ReLoadKeyCombinations()
		{
			if (this.mousecontrolsTabActive)
			{
				this.LoadMouseCombinations();
			}
			else
			{
				this.LoadKeyCombinations();
			}
			GuiElementConfigList configlist = this.composer.GetConfigList("configlist");
			if (configlist != null)
			{
				configlist.Refresh();
				GuiElementScrollbar scrollbar = this.composer.GetScrollbar("scrollbar");
				if (scrollbar != null)
				{
					scrollbar.SetNewTotalHeight((float)configlist.innerBounds.OuterHeight);
				}
				GuiElementScrollbar scrollbar2 = this.composer.GetScrollbar("scrollbar");
				if (scrollbar2 == null)
				{
					return;
				}
				scrollbar2.TriggerChanged();
			}
		}

		private void LoadKeyCombinations()
		{
			int hotkeyIndex = -1;
			int count = this.keycontrolItems.Count;
			int? num = this.clickedItemIndex;
			if ((count >= num.GetValueOrDefault()) & (num != null))
			{
				hotkeyIndex = (int)this.keycontrolItems[this.clickedItemIndex.Value].Data;
			}
			this.keycontrolItems.Clear();
			int i = 0;
			List<ConfigItem>[] sortedItems = new List<ConfigItem>[this.sortOrder.Count];
			for (int j = 0; j < sortedItems.Length; j++)
			{
				sortedItems[j] = new List<ConfigItem>();
			}
			foreach (KeyValuePair<string, HotKey> val in ScreenManager.hotkeyManager.HotKeys)
			{
				HotKey kc = val.Value;
				if (this.clickedItemIndex != null && i == hotkeyIndex)
				{
					kc = this.keyCombClone;
				}
				string text = "?";
				if (kc.CurrentMapping != null)
				{
					text = kc.CurrentMapping.ToString();
				}
				ConfigItem item = new ConfigItem
				{
					Code = val.Key,
					Key = kc.Name,
					Value = text,
					Data = i
				};
				int index = this.keycontrolItems.FindIndex((ConfigItem configitem) => configitem.Value == text);
				if (index != -1)
				{
					item.error = true;
					this.keycontrolItems[index].error = true;
				}
				sortedItems[this.sortOrder[kc.KeyCombinationType]].Add(item);
				i++;
			}
			for (int k = 0; k < sortedItems.Length; k++)
			{
				List<ConfigItem> filteredSortedItems = new List<ConfigItem>();
				string text2 = this.currentSearchText;
				string searchText = ((text2 != null) ? text2.ToSearchFriendly().ToLowerInvariant() : null);
				bool canSearch = !string.IsNullOrEmpty(searchText);
				if ((k != 1 || ClientSettings.SeparateCtrl) && k != 9)
				{
					if (canSearch)
					{
						foreach (ConfigItem item2 in sortedItems[k])
						{
							if (item2.Key.ToSearchFriendly().ToLowerInvariant().Contains(searchText))
							{
								filteredSortedItems.Add(item2);
							}
						}
						if (filteredSortedItems != null && !filteredSortedItems.Any<ConfigItem>())
						{
							goto IL_0292;
						}
					}
					if (k != 7)
					{
						this.keycontrolItems.Add(new ConfigItem
						{
							Type = EnumItemType.Title,
							Key = this.titles[k]
						});
					}
					this.keycontrolItems.AddRange(canSearch ? filteredSortedItems : sortedItems[k]);
				}
				IL_0292:;
			}
		}

		private void OnKeyControlItemClick(int index, int indexNoTitle)
		{
			if (this.clickedItemIndex != null)
			{
				return;
			}
			this.keycontrolItems[index].Value = "?";
			this.clickedItemIndex = new int?(index);
			int hotkeyIndex = (int)this.keycontrolItems[this.clickedItemIndex.Value].Data;
			this.composer.GetConfigList("configlist").Refresh();
			GuiElementScrollbar scrollbar = this.composer.GetScrollbar("scrollbar");
			if (scrollbar != null)
			{
				scrollbar.TriggerChanged();
			}
			string code = ScreenManager.hotkeyManager.HotKeys.GetKeyAtIndex(hotkeyIndex);
			HotKey keyComb = ScreenManager.hotkeyManager.HotKeys[code];
			this.keyCombClone = keyComb.Clone();
			this.hotkeyCapturer.BeginCapture();
			this.keyCombClone.CurrentMapping = this.hotkeyCapturer.CapturingKeyComb;
		}

		public bool ShouldCaptureAllInputs()
		{
			return this.hotkeyCapturer.IsCapturing();
		}

		public void OnKeyDown(KeyEvent eventArgs)
		{
			if (this.hotkeyCapturer.OnKeyDown(eventArgs))
			{
				if (!this.hotkeyCapturer.IsCapturing())
				{
					this.clickedItemIndex = null;
					this.keyCombClone = null;
				}
				this.ReLoadKeyCombinations();
			}
		}

		public void OnKeyUp(KeyEvent eventArgs)
		{
			this.hotkeyCapturer.OnKeyUp(eventArgs, new Action(this.CompletedCapture));
		}

		public void OnMouseDown(MouseEvent eventArgs)
		{
			if (this.hotkeyCapturer.OnMouseDown(eventArgs))
			{
				if (!this.hotkeyCapturer.IsCapturing())
				{
					this.clickedItemIndex = null;
					this.keyCombClone = null;
				}
				this.ReLoadKeyCombinations();
			}
		}

		public void OnMouseUp(MouseEvent eventArgs)
		{
			this.hotkeyCapturer.OnMouseUp(eventArgs, new Action(this.CompletedCapture));
		}

		private void CompletedCapture()
		{
			int hotkeyIndex = (this.mousecontrolsTabActive ? ((int)this.mousecontrolItems[this.clickedItemIndex.Value].Data) : ((int)this.keycontrolItems[this.clickedItemIndex.Value].Data));
			string code = ScreenManager.hotkeyManager.HotKeys.GetKeyAtIndex(hotkeyIndex);
			if (!this.hotkeyCapturer.WasCancelled)
			{
				this.keyCombClone.CurrentMapping = this.hotkeyCapturer.CapturedKeyComb;
				ScreenManager.hotkeyManager.HotKeys[code] = this.keyCombClone;
				ClientSettings.Inst.SetKeyMapping(code, this.keyCombClone.CurrentMapping);
				if (code == "sneak" && !ClientSettings.SeparateCtrl)
				{
					ScreenManager.hotkeyManager.HotKeys["shift"].CurrentMapping = this.keyCombClone.CurrentMapping;
					this.ShiftOrCtrlChanged();
				}
				if (code == "sprint" && !ClientSettings.SeparateCtrl)
				{
					ScreenManager.hotkeyManager.HotKeys["ctrl"].CurrentMapping = this.keyCombClone.CurrentMapping;
					this.ShiftOrCtrlChanged();
				}
				if (code == "shift" || code == "ctrl" || code == "primarymouse" || code == "secondarymouse" || code == "toolmodeselect")
				{
					this.ShiftOrCtrlChanged();
				}
			}
			this.clickedItemIndex = null;
			this.keyCombClone = null;
			this.ReLoadKeyCombinations();
		}

		private void ShiftOrCtrlChanged()
		{
			ClientCoreAPI clientCoreAPI = this.handler.Api as ClientCoreAPI;
			if (clientCoreAPI == null)
			{
				return;
			}
			clientCoreAPI.eventapi.TriggerHotkeysChanged();
		}

		private void OnNewScrollbarValue(float value)
		{
			ElementBounds innerBounds = this.composer.GetConfigList("configlist").innerBounds;
			innerBounds.fixedY = (double)(5f - value);
			innerBounds.CalcWorldBounds();
		}

		private bool onMouseSmoothingChanged(int value)
		{
			ClientSettings.MouseSmoothing = 100 - value;
			return true;
		}

		private bool onMouseSensivityChanged(int value)
		{
			ClientSettings.MouseSensivity = value;
			return true;
		}

		private bool OnResetControls()
		{
			this.composer = this.ComposerHeader("gamesettings-confirmreset", "controls").AddStaticText(Lang.Get("Please Confirm", Array.Empty<object>()), CairoFont.WhiteSmallishText(), ElementStdBounds.Rowed(1.5f, 0.0, EnumDialogArea.LeftFixed).WithFixedWidth(600.0), null).AddStaticText(Lang.Get("Really reset key controls to default settings?", Array.Empty<object>()), CairoFont.WhiteSmallishText(), ElementStdBounds.Rowed(2f, 0.0, EnumDialogArea.LeftFixed).WithFixedSize(600.0, 100.0), null)
				.AddButton(Lang.Get("Cancel", Array.Empty<object>()), new ActionConsumable(this.OnCancelReset), ElementStdBounds.Rowed(3.7f, 0.0, EnumDialogArea.LeftFixed).WithFixedPadding(10.0, 2.0), EnumButtonStyle.Normal, null)
				.AddButton(Lang.Get("Confirm", Array.Empty<object>()), new ActionConsumable(this.OnConfirmReset), ElementStdBounds.Rowed(3.7f, 0.0, EnumDialogArea.RightFixed).WithFixedPadding(10.0, 2.0), EnumButtonStyle.Normal, null)
				.EndChildElements()
				.Compose(true);
			this.handler.LoadComposer(this.composer);
			return true;
		}

		private bool OnConfirmReset()
		{
			ClientSettings.KeyMapping.Clear();
			ScreenManager.hotkeyManager.ResetKeyMapping();
			this.OnControlOptions(true);
			return true;
		}

		private bool OnCancelReset()
		{
			this.OnControlOptions(true);
			return true;
		}

		private void OnAccessibilityOptions(bool on)
		{
			ElementBounds leftText = ElementBounds.Fixed(0.0, 85.0, 450.0, 42.0);
			ElementBounds rightSlider = ElementBounds.Fixed(470.0, 138.0, 200.0, 20.0);
			this.composer = this.ComposerHeader("gamesettings-accessibility", "accessibility").AddStaticText(Lang.Get("setting-accessibility-notes", Array.Empty<object>()), CairoFont.WhiteSmallText(), leftText.FlatCopy().WithFixedWidth(800.0), null).AddStaticText(Lang.Get("setting-name-togglesprint", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 12.0, 0.0, 0.0).WithFixedWidth(360.0), null)
				.AddHoverText(Lang.Get("setting-hover-togglesprint", Array.Empty<object>()), CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0), null)
				.AddSwitch(new Action<bool>(this.onToggleSprint), rightSlider.FlatCopy(), "toggleSprint", 30.0, 4.0)
				.AddStaticText(Lang.Get("setting-name-bobblehead", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 2.0, 0.0, 0.0), null)
				.AddHoverText(Lang.Get("setting-hover-bobblehead", Array.Empty<object>()), CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0), null)
				.AddSwitch(new Action<bool>(this.onViewBobbingChanged), rightSlider = rightSlider.BelowCopy(0.0, 20.0, 0.0, 0.0), "viewBobbingSwitch", 30.0, 4.0)
				.AddStaticText(Lang.Get("setting-name-camerashake", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 2.0, 0.0, 0.0), null)
				.AddSlider(new ActionConsumable<int>(this.onCameraShakeChanged), rightSlider = rightSlider.BelowCopy(0.0, 18.0, 0.0, 0.0).WithFixedSize(200.0, 25.0), "cameraShakeSlider")
				.AddHoverText(Lang.Get("setting-hover-camerashake", Array.Empty<object>()), CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0), null)
				.AddStaticText(Lang.Get("setting-name-wireframethickness", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 2.0, 0.0, 0.0), null)
				.AddSlider(new ActionConsumable<int>(this.onWireframeThicknessChanged), rightSlider = rightSlider.BelowCopy(0.0, 19.0, 0.0, 0.0).WithFixedSize(200.0, 25.0), "wireframethicknessSlider")
				.AddHoverText(Lang.Get("setting-hover-wireframethickness", Array.Empty<object>()), CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0), null)
				.AddStaticText(Lang.Get("setting-name-wireframecolors", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 2.0, 0.0, 0.0), null)
				.AddDropDown(new string[] { "Preset1", "Preset2", "Preset3" }, new string[]
				{
					Lang.Get("Preset 1", Array.Empty<object>()),
					Lang.Get("Preset 2", Array.Empty<object>()),
					Lang.Get("Preset 3", Array.Empty<object>())
				}, ClientSettings.guiColorsPreset - 1, new SelectionChangedDelegate(this.onWireframeColorsChanged), rightSlider = rightSlider.BelowCopy(0.0, 19.0, 0.0, 0.0).WithFixedSize(100.0, 25.0), "wireframecolorsDropdown")
				.AddHoverText(Lang.Get("setting-hover-wireframecolors", Array.Empty<object>()), CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0), null)
				.AddStaticText(Lang.Get("setting-name-instabilityWavingStrength", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 2.0, 0.0, 0.0), null)
				.AddSlider(new ActionConsumable<int>(this.onInstabilityStrengthChanged), rightSlider.BelowCopy(0.0, 19.0, 0.0, 0.0).WithFixedSize(200.0, 25.0), "instabilityWavingStrengthSlider")
				.AddHoverText(Lang.Get("setting-hover-instabilityWavingStrength", Array.Empty<object>()), CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0), null)
				.AddRichtext(Lang.Get("help-accessibility", Array.Empty<object>()), CairoFont.WhiteDetailText(), leftText.BelowCopy(0.0, 23.0, 0.0, 0.0), null)
				.EndChildElements()
				.Compose(true);
			this.composer.GetSwitch("viewBobbingSwitch").On = ClientSettings.ViewBobbing;
			this.composer.GetSwitch("toggleSprint").SetValue(ClientSettings.ToggleSprint);
			this.composer.GetSlider("cameraShakeSlider").SetValues((int)(ClientSettings.CameraShakeStrength * 100f), 0, 100, 1, " %");
			this.composer.GetSlider("wireframethicknessSlider").SetValues((int)(ClientSettings.Wireframethickness * 2f), 1, 16, 1, "x");
			this.composer.GetSlider("wireframethicknessSlider").OnSliderTooltip = (int value) => ((float)value / 2f).ToString() + "x";
			this.composer.GetSlider("wireframethicknessSlider").ComposeHoverTextElement();
			this.composer.GetSlider("instabilityWavingStrengthSlider").SetValues((int)(ClientSettings.InstabilityWavingStrength * 100f), 0, 150, 1, " %");
			this.handler.LoadComposer(this.composer);
		}

		private bool onInstabilityStrengthChanged(int value)
		{
			ClientSettings.InstabilityWavingStrength = (float)value / 100f;
			return true;
		}

		private bool onWireframeThicknessChanged(int value)
		{
			ClientSettings.Wireframethickness = (float)value / 2f;
			return true;
		}

		private void onWireframeColorsChanged(string code, bool selected)
		{
			ClientSettings.guiColorsPreset = (int)(code[code.Length - 1] - '0');
			IColorPresets colorPreset = this.handler.Api.ColorPreset;
			if (colorPreset == null)
			{
				return;
			}
			colorPreset.OnUpdateSetting();
		}

		private bool onCameraShakeChanged(int value)
		{
			ClientSettings.CameraShakeStrength = (float)value / 100f;
			return true;
		}

		private void onViewBobbingChanged(bool val)
		{
			ClientSettings.ViewBobbing = val;
		}

		private void onToggleSprint(bool on)
		{
			ClientSettings.ToggleSprint = on;
		}

		internal void OnSoundOptions(bool on)
		{
			ElementBounds leftText = ElementBounds.Fixed(0.0, 87.0, 320.0, 40.0);
			ElementBounds rightSlider = ElementBounds.Fixed(340.0, 89.0, 330.0, 20.0);
			string[] devices = new string[1].Append(ScreenManager.Platform.AvailableAudioDevices.ToArray<string>());
			string[] devicesnames = new string[] { "Default" }.Append(ScreenManager.Platform.AvailableAudioDevices.ToArray<string>());
			this.composer = this.ComposerHeader("gamesettings-soundoptions", "sounds").AddStaticText(Lang.Get("setting-name-mastersoundlevel", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftText = leftText.FlatCopy(), null).AddSlider(new ActionConsumable<int>(this.onMasterSoundLevelChanged), rightSlider = rightSlider.FlatCopy(), "mastersoundLevel")
				.AddStaticText(Lang.Get("setting-name-soundlevel", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 25.0, 0.0, 0.0), null)
				.AddSlider(new ActionConsumable<int>(this.onSoundLevelChanged), rightSlider = rightSlider.BelowCopy(0.0, 46.0, 0.0, 0.0), "soundLevel")
				.AddStaticText(Lang.Get("setting-name-entitysoundlevel", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 0.0, 0.0, 0.0), null)
				.AddSlider(new ActionConsumable<int>(this.onEntitySoundLevelChanged), rightSlider = rightSlider.BelowCopy(0.0, 21.0, 0.0, 0.0), "entitySoundLevel")
				.AddStaticText(Lang.Get("setting-name-ambientsoundlevel", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 0.0, 0.0, 0.0), null)
				.AddSlider(new ActionConsumable<int>(this.onAmbientSoundLevelChanged), rightSlider = rightSlider.BelowCopy(0.0, 21.0, 0.0, 0.0), "ambientSoundLevel")
				.AddStaticText(Lang.Get("setting-name-weathersoundlevel", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 0.0, 0.0, 0.0), null)
				.AddSlider(new ActionConsumable<int>(this.onWeatherSoundLevelChanged), rightSlider = rightSlider.BelowCopy(0.0, 21.0, 0.0, 0.0), "weatherSoundLevel")
				.AddStaticText(Lang.Get("setting-name-musiclevel", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 22.0, 0.0, 0.0), null)
				.AddSlider(new ActionConsumable<int>(this.onMusicLevelChanged), rightSlider = rightSlider.BelowCopy(0.0, 41.0, 0.0, 0.0), "musicLevel")
				.AddStaticText(Lang.Get("setting-name-musicfrequency", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 0.0, 0.0, 0.0), null)
				.AddSlider(new ActionConsumable<int>(this.onMusicFrequencyChanged), rightSlider = rightSlider.BelowCopy(0.0, 21.0, 0.0, 0.0), "musicFrequency")
				.AddStaticText(Lang.Get("setting-name-hrtfmode", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 26.0, 0.0, 0.0), null)
				.AddHoverText(Lang.Get("setting-hover-hrtfmode", Array.Empty<object>()), CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(30.0), null)
				.AddSwitch(new Action<bool>(this.onHRTFMode), rightSlider = rightSlider.BelowCopy(0.0, 34.0, 0.0, 0.0), "hrtfmode", 30.0, 4.0)
				.AddStaticText(Lang.Get("setting-name-audiooutputdevice", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftText.BelowCopy(0.0, 5.0, 0.0, 0.0), null)
				.AddDropDown(devices, devicesnames, 0, new SelectionChangedDelegate(this.onAudioDeviceChanged), rightSlider.BelowCopy(0.0, 16.0, 0.0, 0.0).WithFixedSize(300.0, 30.0), "audiooutputdevice")
				.EndChildElements()
				.Compose(true);
			this.handler.LoadComposer(this.composer);
			this.composer.GetSlider("mastersoundLevel").SetValues(ClientSettings.MasterSoundLevel, 0, 100, 1, "%");
			this.composer.GetSlider("soundLevel").SetValues(ClientSettings.SoundLevel, 0, 100, 1, "%");
			this.composer.GetSlider("entitySoundLevel").SetValues(ClientSettings.EntitySoundLevel, 0, 100, 1, "%");
			this.composer.GetSlider("ambientSoundLevel").SetValues(ClientSettings.AmbientSoundLevel, 0, 100, 1, "%");
			this.composer.GetSlider("weatherSoundLevel").SetValues(ClientSettings.WeatherSoundLevel, 0, 100, 1, "%");
			this.composer.GetSlider("musicLevel").SetValues(ClientSettings.MusicLevel, 0, 100, 1, "%");
			string[] frequencies = new string[]
			{
				Lang.Get("setting-musicfrequency-low", Array.Empty<object>()),
				Lang.Get("setting-musicfrequency-medium", Array.Empty<object>()),
				Lang.Get("setting-musicfrequency-often", Array.Empty<object>()),
				Lang.Get("setting-musicfrequency-veryoften", Array.Empty<object>())
			};
			this.composer.GetSlider("musicFrequency").OnSliderTooltip = (int value) => frequencies[value] ?? "";
			this.composer.GetSlider("musicFrequency").SetValues(ClientSettings.MusicFrequency, 0, 3, 1, "");
			this.composer.GetSwitch("hrtfmode").SetValue(ClientSettings.UseHRTFAudio);
			this.composer.GetDropDown("audiooutputdevice").SetSelectedIndex(Math.Max(0, devices.IndexOf(ClientSettings.AudioDevice)));
		}

		private void onAudioDeviceChanged(string code, bool selected)
		{
			ClientSettings.AudioDevice = code;
		}

		private bool onMusicFrequencyChanged(int val)
		{
			ClientSettings.MusicFrequency = val;
			return true;
		}

		private bool onMasterSoundLevelChanged(int soundLevel)
		{
			ClientSettings.MasterSoundLevel = soundLevel;
			return true;
		}

		private bool onSoundLevelChanged(int soundLevel)
		{
			ClientSettings.SoundLevel = soundLevel;
			return true;
		}

		private bool onEntitySoundLevelChanged(int soundLevel)
		{
			ClientSettings.EntitySoundLevel = soundLevel;
			return true;
		}

		private bool onAmbientSoundLevelChanged(int soundLevel)
		{
			ClientSettings.AmbientSoundLevel = soundLevel;
			return true;
		}

		private bool onWeatherSoundLevelChanged(int soundLevel)
		{
			ClientSettings.WeatherSoundLevel = soundLevel;
			return true;
		}

		private bool onMusicLevelChanged(int musicLevel)
		{
			ClientSettings.MusicLevel = musicLevel;
			return true;
		}

		private void onHRTFMode(bool val)
		{
			ClientSettings.UseHRTFAudio = val;
		}

		public static void getLanguages(out string[] languageCodes, out string[] languageNames)
		{
			GuiCompositeSettings.LanguageConfig[] configs = ScreenManager.Platform.AssetManager.Get<GuiCompositeSettings.LanguageConfig[]>(new AssetLocation("lang/languages.json"));
			languageCodes = new string[configs.Length];
			languageNames = new string[configs.Length];
			for (int i = 0; i < configs.Length; i++)
			{
				languageCodes[i] = configs[i].Code;
				languageNames[i] = configs[i].Name + " / " + configs[i].Englishname;
			}
		}

		internal void OnInterfaceOptions(bool on)
		{
			ElementBounds leftText = ElementBounds.Fixed(0.0, 85.0, 475.0, 42.0);
			ElementBounds rightSlider = ElementBounds.Fixed(495.0, 89.0, 200.0, 20.0);
			EnumWindowBorder windowBorder = ScreenManager.Platform.WindowBorder;
			string currentLanguage = ClientSettings.Language;
			string[] languageCodes;
			string[] languageNames;
			GuiCompositeSettings.getLanguages(out languageCodes, out languageNames);
			int langIndex = languageCodes.IndexOf(currentLanguage);
			this.composer = this.ComposerHeader("gamesettings-interfaceoptions", "interface").AddStaticText(Lang.Get("setting-name-guiscale", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftText, null).AddHoverText(Lang.Get("setting-hover-guiscale", Array.Empty<object>()), CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0), null)
				.AddSlider(new ActionConsumable<int>(this.onGuiScaleChanged), rightSlider, "guiScaleSlider")
				.AddStaticText(Lang.Get("setting-name-language", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 2.0, 0.0, 0.0), null)
				.AddHoverText(Lang.Get("setting-hover-language", Array.Empty<object>()), CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0), null)
				.AddDropDown(languageCodes, languageNames, langIndex, new SelectionChangedDelegate(this.onLanguageChanged), rightSlider = rightSlider.BelowCopy(0.0, 17.0, 0.0, 0.0).WithFixedSize(330.0, 30.0), null)
				.AddStaticText(Lang.Get("setting-name-autochat", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 1.0, 0.0, 0.0), null)
				.AddHoverText(Lang.Get("setting-hover-autochat", Array.Empty<object>()), CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0), null)
				.AddSwitch(new Action<bool>(this.onAutoChatChanged), rightSlider = rightSlider.BelowCopy(0.0, 15.0, 0.0, 0.0), "autoChatSwitch", 30.0, 4.0)
				.AddStaticText(Lang.Get("setting-name-autochat-selected", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 1.0, 0.0, 0.0), null)
				.AddHoverText(Lang.Get("setting-hover-autochat-selected", Array.Empty<object>()), CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0), null)
				.AddSwitch(new Action<bool>(this.onAutoChatOpenSelectedChanged), rightSlider = rightSlider.BelowCopy(0.0, 15.0, 0.0, 0.0), "autoChatOpenSelectedSwitch", 30.0, 4.0)
				.AddStaticText(Lang.Get("setting-name-blockinfohud", Array.Empty<object>()) + this.HotkeyReminder("blockinfohud"), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 2.0, 0.0, 0.0), null)
				.AddHoverText(Lang.Get("setting-hover-blockinfohud", Array.Empty<object>()), CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0), null)
				.AddSwitch(new Action<bool>(this.onBlockInfoHudChanged), rightSlider = rightSlider.BelowCopy(0.0, 14.0, 0.0, 0.0), "blockinfohudSwitch", 30.0, 4.0)
				.AddStaticText(Lang.Get("setting-name-blockinteractioninfohud", Array.Empty<object>()) + this.HotkeyReminder("blockinteractionhelp"), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 2.0, 0.0, 0.0), null)
				.AddHoverText(Lang.Get("setting-hover-blockinteractioninfohud", Array.Empty<object>()), CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0), null)
				.AddSwitch(new Action<bool>(this.onBlockInteractionInfoHudChanged), rightSlider = rightSlider.BelowCopy(0.0, 14.0, 0.0, 0.0), "blockinteractioninfohudSwitch", 30.0, 4.0)
				.AddStaticText(Lang.Get("setting-name-coordinatehud", Array.Empty<object>()) + this.HotkeyReminder("coordinateshud"), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 2.0, 0.0, 0.0), null)
				.AddHoverText(Lang.Get("setting-hover-coordinatehud", Array.Empty<object>()), CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0), null)
				.AddSwitch(new Action<bool>(this.onCoordinateHudChanged), rightSlider = rightSlider.BelowCopy(0.0, 14.0, 0.0, 0.0), "coordinatehudSwitch", 30.0, 4.0);
			if (this.composer.Api is MainMenuAPI || this.composer.Api.World.Config.GetBool("allowMap", true))
			{
				this.composer = this.composer.AddStaticText(Lang.Get("setting-name-minimaphud", Array.Empty<object>()) + this.HotkeyReminder("worldmaphud"), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 2.0, 0.0, 0.0), null).AddHoverText(Lang.Get("setting-hover-minimaphud", Array.Empty<object>()), CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0), null).AddSwitch(new Action<bool>(this.onMinimapHudChanged), rightSlider = rightSlider.BelowCopy(0.0, 14.0, 0.0, 0.0), "minimaphudSwitch", 30.0, 4.0);
			}
			this.composer = this.composer.AddStaticText(Lang.Get("setting-name-immersivemousemode", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 4.0, 0.0, 0.0), null).AddHoverText(Lang.Get("setting-hover-immersivemousemode", Array.Empty<object>()), CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0), null).AddSwitch(new Action<bool>(this.onImmersiveMouseModeChanged), rightSlider = rightSlider.BelowCopy(0.0, 17.0, 0.0, 0.0), "immersiveMouseModeSwitch", 30.0, 4.0)
				.AddStaticText(Lang.Get("setting-name-immersivefpmode", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 5.0, 0.0, 0.0), null)
				.AddHoverText(Lang.Get("setting-hover-immersivefpmode", Array.Empty<object>()), CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0), null)
				.AddSwitch(new Action<bool>(this.onImmersiveFpModeChanged), rightSlider = rightSlider.BelowCopy(0.0, 17.0, 0.0, 0.0), "immersiveFpModeSwitch", 30.0, 4.0)
				.AddStaticText(Lang.Get("setting-name-fpmodeyoffset", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 5.0, 0.0, 0.0), null)
				.AddHoverText(Lang.Get("setting-hover-fpmodeyoffset", Array.Empty<object>()), CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0), null)
				.AddSlider(new ActionConsumable<int>(this.onFpModeYOffsetChanged), rightSlider = rightSlider.BelowCopy(0.0, 19.0, 0.0, 0.0).WithFixedSize(150.0, 20.0), "fpmodeYOffsetSlider")
				.AddStaticText(Lang.Get("setting-name-fpmodefov", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 5.0, 0.0, 0.0), null)
				.AddHoverText(Lang.Get("setting-hover-fpmodefov", Array.Empty<object>()), CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0), null)
				.AddSlider(new ActionConsumable<int>(this.onFpModeFoVChanged), rightSlider = rightSlider.BelowCopy(0.0, 28.0, 0.0, 0.0).WithFixedSize(150.0, 20.0), "fpmodefovSlider")
				.AddStaticText(Lang.Get("setting-name-developermode", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 4.0, 0.0, 0.0), null)
				.AddHoverText(Lang.Get("setting-hover-developermode", Array.Empty<object>()), CairoFont.WhiteSmallText(), 250, rightSlider = rightSlider.BelowCopy(0.0, 20.0, 0.0, 0.0).WithFixedHeight(25.0), null)
				.AddSwitch(new Action<bool>(this.onDeveloperModeChanged), rightSlider, "developerSwitch", 30.0, 4.0)
				.AddRichtext((this.startupLanguage != "en") ? Lang.Get("setting-notice-lang-communitycreated", Array.Empty<object>()) : "", CairoFont.WhiteSmallishText(), leftText.BelowCopy(0.0, 0.0, 0.0, 0.0).WithFixedMargin(0.0, 25.0).WithFixedSize(880.0, 110.0), "restartText")
				.EndChildElements()
				.Compose(true);
			this.handler.LoadComposer(this.composer);
			if (ScreenManager.Platform.ScreenSize.Width > 3000)
			{
				this.composer.GetSlider("guiScaleSlider").SetValues((int)(8f * ClientSettings.GUIScale), 4, 24, 1, "");
			}
			else
			{
				this.composer.GetSlider("guiScaleSlider").SetValues((int)(8f * ClientSettings.GUIScale), 4, 16, 1, "");
			}
			this.composer.GetSlider("guiScaleSlider").TriggerOnlyOnMouseUp(true);
			this.composer.GetSlider("fpmodeYOffsetSlider").SetValues((int)(ClientSettings.FpHandsYOffset * 100f), -100, 10, 1, "");
			this.composer.GetSlider("fpmodefovSlider").SetValues(ClientSettings.FpHandsFoV, 70, 90, 1, "°");
			this.composer.GetSwitch("immersiveMouseModeSwitch").SetValue(ClientSettings.ImmersiveMouseMode);
			this.composer.GetSwitch("immersiveFpModeSwitch").SetValue(ClientSettings.ImmersiveFpMode);
			this.composer.GetSwitch("autoChatSwitch").SetValue(ClientSettings.AutoChat);
			this.composer.GetSwitch("autoChatOpenSelectedSwitch").SetValue(ClientSettings.AutoChatOpenSelected);
			this.composer.GetSwitch("blockinfohudSwitch").SetValue(ClientSettings.ShowBlockInfoHud);
			this.composer.GetSwitch("blockinteractioninfohudSwitch").SetValue(ClientSettings.ShowBlockInteractionHelp);
			this.composer.GetSwitch("coordinatehudSwitch").SetValue(ClientSettings.ShowCoordinateHud);
			GuiElementSwitch @switch = this.composer.GetSwitch("minimaphudSwitch");
			if (@switch != null)
			{
				@switch.SetValue(this.composer.Api.Settings.Bool["showMinimapHud"]);
			}
			this.composer.GetSwitch("developerSwitch").SetValue(ClientSettings.DeveloperMode);
		}

		private bool onFpModeYOffsetChanged(int pos)
		{
			ClientSettings.FpHandsYOffset = (float)pos / 100f;
			return true;
		}

		private bool onFpModeFoVChanged(int pos)
		{
			ClientSettings.FpHandsFoV = pos;
			return true;
		}

		private string HotkeyReminder(string key)
		{
			HotKey hotkey;
			if (!ScreenManager.hotkeyManager.HotKeys.TryGetValue(key, out hotkey))
			{
				return "";
			}
			if (hotkey.CurrentMapping == null)
			{
				return "";
			}
			string text = " (";
			KeyCombination currentMapping = hotkey.CurrentMapping;
			return text + ((currentMapping != null) ? currentMapping.ToString() : null) + ")";
		}

		private void onMinimapHudChanged(bool on)
		{
			this.composer.Api.Settings.Bool["showMinimapHud"] = on;
		}

		private void onCoordinateHudChanged(bool on)
		{
			ClientSettings.ShowCoordinateHud = on;
		}

		private void onBlockInteractionInfoHudChanged(bool on)
		{
			ClientSettings.ShowBlockInteractionHelp = on;
		}

		private void onBlockInfoHudChanged(bool on)
		{
			ClientSettings.ShowBlockInfoHud = on;
		}

		private void onImmersiveMouseModeChanged(bool on)
		{
			ClientSettings.ImmersiveMouseMode = on;
		}

		private void onImmersiveFpModeChanged(bool on)
		{
			ClientSettings.ImmersiveFpMode = on;
		}

		private void onAutoChatChanged(bool on)
		{
			ClientSettings.AutoChat = on;
		}

		private void onAutoChatOpenSelectedChanged(bool on)
		{
			ClientSettings.AutoChatOpenSelected = on;
		}

		private void onLanguageChanged(string lang, bool on)
		{
			bool save = false;
			if (lang != ClientSettings.Language)
			{
				if (lang != "en")
				{
					this.composer.GetRichtext("restartText").SetNewText(Lang.GetL(lang, "setting-notice-restart", Array.Empty<object>()) + " " + Lang.GetL(lang, "setting-notice-lang-communitycreated", Array.Empty<object>()), CairoFont.WhiteSmallishText(), null);
				}
				else
				{
					this.composer.GetRichtext("restartText").SetNewText(Lang.GetL(lang, "setting-notice-restart", Array.Empty<object>()), CairoFont.WhiteSmallishText(), null);
				}
				save = true;
			}
			if (lang == this.startupLanguage)
			{
				this.composer.GetRichtext("restartText").SetNewText((lang != "en") ? Lang.Get("setting-notice-lang-communitycreated", Array.Empty<object>()) : "", CairoFont.WhiteSmallishText(), null);
			}
			ClientSettings.Language = lang;
			if (lang.StartsWithOrdinal("zh-") || lang == "ar" || lang == "ja" || lang == "ko" || lang == "th")
			{
				if (RuntimeEnv.OS != OS.Windows)
				{
					if (lang != this.startupLanguage && ClientSettings.DefaultFontName == "sans-serif")
					{
						ClientSettings.DecorativeFontName = "sans-serif";
						this.composer.GetRichtext("restartText").SetNewText(string.Concat(new string[]
						{
							Lang.GetL(this.startupLanguage, "setting-notice-restart", Array.Empty<object>()),
							" ",
							Lang.GetL(this.startupLanguage, "setting-notice-lang-communitycreated", Array.Empty<object>()),
							"\n",
							Lang.GetL(this.startupLanguage, "setting-notice-lang-nonwindowsfonts", Array.Empty<object>())
						}), CairoFont.WhiteSmallishText(), null);
					}
				}
				else if (lang == "ko")
				{
					this.SetupLocalizedFonts(lang, "Malgun Gothic", "Malgun Gothic");
					save = true;
				}
				else if (lang == "th")
				{
					this.SetupLocalizedFonts(lang, "Leelawadee UI Semilight", "Leelawadee UI");
					save = true;
				}
				else if (lang == "ja")
				{
					this.SetupLocalizedFonts(lang, "meiryo", "meiryo");
					save = true;
				}
				else if (lang == "zh-cn")
				{
					this.SetupLocalizedFonts(lang, "Microsoft YaHei Light", "Microsoft YaHei");
					save = true;
				}
				else if (lang == "zh-tw")
				{
					this.SetupLocalizedFonts(lang, "Microsoft JhengHei UI Light", "Microsoft JhengHei UI");
					save = true;
				}
				else
				{
					ClientSettings.DecorativeFontName = "sans-serif";
					save = true;
				}
			}
			else
			{
				if (ClientSettings.DefaultFontName == "meiryo" || ClientSettings.DefaultFontName == "Malgun Gothic" || ClientSettings.DefaultFontName == "Leelawadee UI Semilight" || ClientSettings.DefaultFontName == "Microsoft YaHei Light" || ClientSettings.DefaultFontName == "Microsoft JhengHei UI Light")
				{
					ClientSettings.DefaultFontName = "sans-serif";
					save = true;
				}
				if (ClientSettings.DefaultFontName == "sans-serif")
				{
					ClientSettings.DecorativeFontName = "Lora";
					save = true;
				}
			}
			if (save)
			{
				ClientSettings.Inst.Save(true);
			}
		}

		private void SetupLocalizedFonts(string lang, string baseFont, string decorativeFont)
		{
			ClientSettings.DefaultFontName = baseFont;
			ClientSettings.DecorativeFontName = decorativeFont;
			string restartText = ((lang != this.startupLanguage) ? (Lang.GetL(lang, "setting-notice-restart", Array.Empty<object>()) + " " + Lang.GetL(lang, "setting-notice-lang-communitycreated", Array.Empty<object>())) : Lang.GetL(lang, "setting-notice-lang-communitycreated", Array.Empty<object>()));
			if (lang != this.startupLanguage)
			{
				this.composer.GetRichtext("restartText").SetNewText(restartText, CairoFont.WhiteSmallishText(baseFont), null);
				return;
			}
			this.composer.GetRichtext("restartText").SetNewText(restartText, CairoFont.WhiteSmallishText(baseFont), null);
		}

		private void OnDeveloperOptions(bool on)
		{
			ElementBounds leftText = ElementBounds.Fixed(0.0, 42.0, 425.0, 42.0);
			ElementBounds rightSlider = ElementBounds.Fixed(450.0, 45.0, 200.0, 20.0);
			string[] hoverTexts = new string[]
			{
				Lang.Get("setting-hover-errorreporter", Array.Empty<object>()),
				Lang.Get("setting-hover-extdebuginfo", Array.Empty<object>()),
				Lang.Get("setting-hover-opengldebug", Array.Empty<object>()),
				Lang.Get("setting-hover-openglerrorchecking", Array.Empty<object>()),
				Lang.Get("setting-hover-debugtexturedispose", Array.Empty<object>()),
				Lang.Get("setting-hover-debugvaodispose", Array.Empty<object>()),
				Lang.Get("setting-hover-debugsounddispose", Array.Empty<object>()),
				Lang.Get("setting-hover-fasterstartup", Array.Empty<object>())
			};
			this.composer = this.ComposerHeader("gamesettings-developeroptions", "developer").AddStaticText(Lang.Get("setting-name-errorreporter", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 0.0, 0.0, 0.0), null).AddHoverText(hoverTexts[0], CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0), null)
				.AddSwitch(new Action<bool>(this.onErrorReporterChanged), rightSlider = rightSlider.BelowCopy(0.0, 16.0, 0.0, 0.0), "errorReporterSwitch", 30.0, 4.0)
				.AddStaticText(Lang.Get("setting-name-extdebuginfo", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 4.0, 0.0, 0.0), null)
				.AddHoverText(hoverTexts[1], CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0), null)
				.AddSwitch(new Action<bool>(this.onExtDebugInfoChanged), rightSlider = rightSlider.BelowCopy(0.0, 16.0, 0.0, 0.0), "extDbgInfoSwitch", 30.0, 4.0)
				.AddStaticText(Lang.Get("setting-name-opengldebug", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 4.0, 0.0, 0.0), null)
				.AddHoverText(hoverTexts[2], CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0), null)
				.AddSwitch(new Action<bool>(this.onOpenGLDebugChanged), rightSlider = rightSlider.BelowCopy(0.0, 16.0, 0.0, 0.0), "openglDebugSwitch", 30.0, 4.0)
				.AddStaticText(Lang.Get("setting-name-openglerrorchecking", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 4.0, 0.0, 0.0), null)
				.AddHoverText(hoverTexts[3], CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0), null)
				.AddSwitch(new Action<bool>(this.onOpenGLErrorCheckingChanged), rightSlider = rightSlider.BelowCopy(0.0, 16.0, 0.0, 0.0), "openglErrorCheckingSwitch", 30.0, 4.0)
				.AddStaticText(Lang.Get("setting-name-debugtexturedispose", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 4.0, 0.0, 0.0), null)
				.AddHoverText(hoverTexts[4], CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0), null)
				.AddSwitch(new Action<bool>(this.onDebugTextureDisposeChanged), rightSlider = rightSlider.BelowCopy(0.0, 16.0, 0.0, 0.0), "debugTextureDisposeSwitch", 30.0, 4.0)
				.AddStaticText(Lang.Get("setting-name-debugvaodispose", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 4.0, 0.0, 0.0), null)
				.AddHoverText(hoverTexts[5], CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0), null)
				.AddSwitch(new Action<bool>(this.onDebugVaoDisposeChanged), rightSlider = rightSlider.BelowCopy(0.0, 16.0, 0.0, 0.0), "debugVaoDisposeSwitch", 30.0, 4.0)
				.AddStaticText(Lang.Get("setting-name-debugsounddispose", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 4.0, 0.0, 0.0), null)
				.AddHoverText(hoverTexts[6], CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0), null)
				.AddSwitch(new Action<bool>(this.onDebugSoundDisposeChanged), rightSlider = rightSlider.BelowCopy(0.0, 16.0, 0.0, 0.0), "debugSoundDisposeSwitch", 30.0, 4.0)
				.AddStaticText(Lang.Get("setting-name-fasterstartup", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 4.0, 0.0, 0.0), null)
				.AddHoverText(hoverTexts[7], CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0), null)
				.AddSwitch(new Action<bool>(this.onFasterStartupChanged), rightSlider.BelowCopy(0.0, 16.0, 0.0, 0.0), "fasterStartupSwitch", 30.0, 4.0)
				.EndChildElements()
				.Compose(true);
			this.handler.LoadComposer(this.composer);
			this.composer.GetSwitch("errorReporterSwitch").SetValue(ClientSettings.StartupErrorDialog);
			this.composer.GetSwitch("extDbgInfoSwitch").SetValue(ClientSettings.ExtendedDebugInfo);
			this.composer.GetSwitch("openglDebugSwitch").SetValue(ClientSettings.GlDebugMode);
			this.composer.GetSwitch("openglErrorCheckingSwitch").SetValue(ClientSettings.GlErrorChecking);
			this.composer.GetSwitch("debugTextureDisposeSwitch").SetValue(RuntimeEnv.DebugTextureDispose);
			this.composer.GetSwitch("debugVaoDisposeSwitch").SetValue(RuntimeEnv.DebugVAODispose);
			this.composer.GetSwitch("debugSoundDisposeSwitch").SetValue(RuntimeEnv.DebugSoundDispose);
			this.composer.GetSwitch("fasterStartupSwitch").SetValue(ClientSettings.OffThreadMipMapCreation);
		}

		private void onErrorReporterChanged(bool on)
		{
			ClientSettings.StartupErrorDialog = on;
		}

		private void onDebugSoundDisposeChanged(bool on)
		{
			RuntimeEnv.DebugSoundDispose = on;
		}

		private void onDebugVaoDisposeChanged(bool on)
		{
			RuntimeEnv.DebugVAODispose = on;
		}

		private void onDebugTextureDisposeChanged(bool on)
		{
			RuntimeEnv.DebugTextureDispose = on;
		}

		private void onOpenGLDebugChanged(bool on)
		{
			ClientSettings.GlDebugMode = on;
			ScreenManager.Platform.GlDebugMode = on;
		}

		private void onOpenGLErrorCheckingChanged(bool on)
		{
			ClientSettings.GlErrorChecking = on;
			ScreenManager.Platform.GlErrorChecking = on;
		}

		private void onExtDebugInfoChanged(bool on)
		{
			ClientSettings.ExtendedDebugInfo = on;
		}

		private void onFasterStartupChanged(bool on)
		{
			ClientSettings.OffThreadMipMapCreation = on;
		}

		private void onDeveloperModeChanged(bool on)
		{
			if (!on)
			{
				ClientSettings.DeveloperMode = on;
				ClientSettings.StartupErrorDialog = false;
				ClientSettings.ExtendedDebugInfo = false;
				ClientSettings.GlDebugMode = false;
				ClientSettings.GlErrorChecking = false;
				RuntimeEnv.DebugTextureDispose = false;
				RuntimeEnv.DebugVAODispose = false;
				RuntimeEnv.DebugSoundDispose = false;
				this.OnInterfaceOptions(true);
				return;
			}
			this.composer = this.ComposerHeader("gamesettings-confirmdevelopermode", "developer").AddStaticText(Lang.Get("Please Confirm", Array.Empty<object>()), CairoFont.WhiteSmallishText(), ElementStdBounds.Rowed(1.5f, 0.0, EnumDialogArea.LeftFixed).WithFixedWidth(600.0), null).AddStaticText(Lang.Get("confirmEnableDevMode", Array.Empty<object>()), CairoFont.WhiteSmallishText(), ElementStdBounds.Rowed(2f, 0.0, EnumDialogArea.LeftFixed).WithFixedSize(600.0, 100.0), null)
				.AddButton(Lang.Get("Cancel", Array.Empty<object>()), new ActionConsumable(this.OnCancelDevMode), ElementStdBounds.Rowed(3.7f, 0.0, EnumDialogArea.LeftFixed).WithFixedPadding(10.0, 2.0), EnumButtonStyle.Normal, null)
				.AddButton(Lang.Get("Confirm", Array.Empty<object>()), new ActionConsumable(this.OnConfirmDevMode), ElementStdBounds.Rowed(3.7f, 0.0, EnumDialogArea.RightFixed).WithFixedPadding(10.0, 2.0), EnumButtonStyle.Normal, null)
				.EndChildElements()
				.Compose(true);
			this.handler.LoadComposer(this.composer);
		}

		private bool OnCancelDevMode()
		{
			this.OnInterfaceOptions(true);
			return true;
		}

		private bool OnConfirmDevMode()
		{
			ClientSettings.DeveloperMode = true;
			this.OnDeveloperOptions(true);
			return true;
		}

		private IGameSettingsHandler handler;

		private bool onMainscreen;

		private GuiComposer composer;

		private string startupLanguage = ClientSettings.Language;

		public bool IsInCreativeMode;

		private ElementBounds gButtonBounds = ElementBounds.Fixed(0.0, 0.0, 0.0, 40.0).WithFixedPadding(0.0, 3.0);

		private ElementBounds mButtonBounds = ElementBounds.Fixed(0.0, 0.0, 0.0, 40.0).WithFixedPadding(0.0, 3.0);

		private ElementBounds aButtonBounds = ElementBounds.Fixed(0.0, 0.0, 0.0, 40.0).WithFixedPadding(0.0, 3.0);

		private ElementBounds cButtonBounds = ElementBounds.Fixed(0.0, 0.0, 0.0, 40.0).WithFixedPadding(0.0, 3.0);

		private ElementBounds sButtonBounds = ElementBounds.Fixed(0.0, 0.0, 0.0, 40.0).WithFixedPadding(0.0, 3.0);

		private ElementBounds iButtonBounds = ElementBounds.Fixed(0.0, 0.0, 0.0, 40.0).WithFixedPadding(0.0, 3.0);

		private ElementBounds dButtonBounds = ElementBounds.Fixed(0.0, 0.0, 0.0, 40.0).WithFixedPadding(0.0, 3.0);

		private ElementBounds backButtonBounds = ElementBounds.Fixed(0.0, 0.0, 0.0, 40.0).WithFixedPadding(0.0, 3.0);

		private List<ConfigItem> mousecontrolItems = new List<ConfigItem>();

		private bool mousecontrolsTabActive;

		private List<ConfigItem> keycontrolItems = new List<ConfigItem>();

		private HotKey keyCombClone;

		private int? clickedItemIndex;

		private HotkeyCapturer hotkeyCapturer = new HotkeyCapturer();

		public string currentSearchText;

		private Dictionary<HotkeyType, int> sortOrder = new Dictionary<HotkeyType, int>
		{
			{
				HotkeyType.MovementControls,
				0
			},
			{
				HotkeyType.MouseModifiers,
				1
			},
			{
				HotkeyType.CharacterControls,
				2
			},
			{
				HotkeyType.HelpAndOverlays,
				3
			},
			{
				HotkeyType.GUIOrOtherControls,
				4
			},
			{
				HotkeyType.InventoryHotkeys,
				5
			},
			{
				HotkeyType.CreativeOrSpectatorTool,
				6
			},
			{
				HotkeyType.CreativeTool,
				7
			},
			{
				HotkeyType.DevTool,
				8
			},
			{
				HotkeyType.MouseControls,
				9
			}
		};

		private string[] titles = new string[]
		{
			Lang.Get("Movement controls", Array.Empty<object>()),
			Lang.Get("Mouse click modifiers", Array.Empty<object>()),
			Lang.Get("Actions", Array.Empty<object>()),
			Lang.Get("In-game Help and Overlays", Array.Empty<object>()),
			Lang.Get("User interface & More", Array.Empty<object>()),
			Lang.Get("Inventory hotkeys", Array.Empty<object>()),
			Lang.Get("Creative mode", Array.Empty<object>()),
			Lang.Get("Creative mode", Array.Empty<object>()),
			Lang.Get("Debug and Macros", Array.Empty<object>())
		};

		public class LanguageConfig
		{
			public string Code;

			public string Englishname;

			public string Name;
		}
	}
}
