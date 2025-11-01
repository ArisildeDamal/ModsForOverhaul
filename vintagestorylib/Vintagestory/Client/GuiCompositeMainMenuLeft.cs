using System;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;

namespace Vintagestory.Client
{
	public class GuiCompositeMainMenuLeft : GuiComposite
	{
		public double Width
		{
			get
			{
				return this.sidebarBounds.OuterWidth;
			}
		}

		public GuiCompositeMainMenuLeft(ScreenManager screenManager)
		{
			this.screenManager = screenManager;
			this.particleSystem = new ParticleRenderer2D(screenManager, screenManager.api, 1000);
			this.Compose();
		}

		internal void SetHasNewVersion(string versionnumber)
		{
			((GuiElementNewVersionText)this.screenManager.mainMenuComposer.GetElement("newversion")).Activate(versionnumber);
		}

		public void Compose()
		{
			this.particleSystem.Compose("textures/particle/white-spec.png");
			this.sidebarBounds = new ElementBounds();
			this.sidebarBounds.horizontalSizing = ElementSizing.Fixed;
			this.sidebarBounds.verticalSizing = ElementSizing.Percentual;
			this.sidebarBounds.percentHeight = 1.0;
			this.sidebarBounds.fixedWidth = (double)(ElementStdBounds.mainMenuUnscaledLogoSize + 2 * ElementStdBounds.mainMenuUnscaledLogoHorPadding + ElementStdBounds.mainMenuUnscaledWoodPlankWidth);
			ElementBounds logoBounds = ElementBounds.Fixed(0.0, 25.0, (double)ElementStdBounds.mainMenuUnscaledLogoSize, (double)ElementStdBounds.mainMenuUnscaledLogoSize).WithFixedPadding((double)ElementStdBounds.mainMenuUnscaledLogoHorPadding, (double)ElementStdBounds.mainMenuUnscaledLogoVerPadding);
			ElementBounds button = ElementBounds.Fixed(0.0, (double)(ElementStdBounds.mainMenuUnscaledLogoSize + 2 * ElementStdBounds.mainMenuUnscaledLogoVerPadding + 25 + 50), (double)(ElementStdBounds.mainMenuUnscaledLogoSize + 2 * ElementStdBounds.mainMenuUnscaledLogoHorPadding) - 2.0 * GuiElementTextButton.Padding + 2.0, 33.0);
			CairoFont buttonFont = CairoFont.ButtonText().WithFontSize(22f).WithWeight(FontWeight.Normal);
			button.fixedHeight = buttonFont.GetFontExtents().Height / (double)ClientSettings.GUIScale + 2.0 * GuiElementTextButton.Padding + 5.0;
			CairoFont loginFont = CairoFont.WhiteSmallText();
			loginFont.Color = GuiStyle.ButtonTextColor;
			ElementBounds leftBottomText = ElementBounds.Fixed(EnumDialogArea.LeftBottom, 0.0, 0.0, 300.0, 30.0).WithFixedAlignmentOffset(13.0, -8.0);
			ElementBounds newVersionBounds = ElementBounds.Fixed(0.0, 0.0, (double)(ElementStdBounds.mainMenuUnscaledLogoSize + 2 * ElementStdBounds.mainMenuUnscaledLogoHorPadding - 11), 60.0).WithFixedPadding(5.0);
			string loginText = Lang.Get("mainmenu-loggedin", new object[] { ClientSettings.PlayerName });
			if (this.screenManager.ClientIsOffline)
			{
				loginText = loginText + "<br>" + Lang.Get("mainmenu-offline", Array.Empty<object>());
			}
			GuiComposer mainMenuComposer = this.screenManager.mainMenuComposer;
			if (mainMenuComposer != null)
			{
				mainMenuComposer.Dispose();
			}
			this.screenManager.mainMenuComposer = ScreenManager.GuiComposers.Create("compositemainmenu", ElementBounds.Fill).AddShadedDialogBG(this.sidebarBounds, false, 5.0, 0.75f).BeginChildElements()
				.AddStaticCustomDraw(logoBounds, new DrawDelegateWithBounds(this.OnDrawTree))
				.AddButton(Lang.Get("mainmenu-sp", Array.Empty<object>()), new ActionConsumable(this.OnSingleplayer), button, buttonFont, EnumButtonStyle.MainMenu, "singleplayer")
				.AddButton(Lang.Get("mainmenu-mp", Array.Empty<object>()), new ActionConsumable(this.OnMultiplayer), button = button.BelowCopy(0.0, 0.0, 0.0, 0.0), buttonFont, EnumButtonStyle.MainMenu, "multiplayer")
				.AddIf(ClientSettings.HasGameServer)
				.AddButton(Lang.Get("mainmenu-gameserver", Array.Empty<object>()), new ActionConsumable(this.OnGameServer), button = button.BelowCopy(0.0, 0.0, 0.0, 0.0), buttonFont, EnumButtonStyle.MainMenu, "gameserver")
				.EndIf()
				.AddStaticCustomDraw(ElementBounds.Fill, new DrawDelegateWithBounds(this.OnDrawSidebar))
				.AddButton(Lang.Get("mainmenu-settings", Array.Empty<object>()), new ActionConsumable(this.OnSettings), button = button.BelowCopy(0.0, 25.0, 0.0, 0.0), buttonFont, EnumButtonStyle.MainMenu, "settings")
				.AddButton(Lang.Get("mainmenu-mods", Array.Empty<object>()), new ActionConsumable(this.OnMods), button = button.BelowCopy(0.0, 0.0, 0.0, 0.0), buttonFont, EnumButtonStyle.MainMenu, "mods")
				.AddButton(Lang.Get("mainmenu-credits", Array.Empty<object>()), new ActionConsumable(this.OnCredits), button = button.BelowCopy(0.0, 0.0, 0.0, 0.0), buttonFont, EnumButtonStyle.MainMenu, "credits")
				.AddButton(Lang.Get("mainmenu-quit", Array.Empty<object>()), new ActionConsumable(this.OnQuit), button.BelowCopy(0.0, 45.0, 0.0, 0.0), buttonFont, EnumButtonStyle.MainMenu, "quit")
				.AddRichtext(loginText, loginFont, leftBottomText, new Action<LinkTextComponent>(this.didClickLink), "logintext")
				.AddInteractiveElement(new GuiElementNewVersionText(this.screenManager.api, CairoFont.WhiteDetailText().WithWeight(FontWeight.Bold).WithColor(GuiStyle.DarkBrownColor), newVersionBounds), "newversion")
				.EndChildElements()
				.Compose(true);
			(this.screenManager.mainMenuComposer.GetElement("newversion") as GuiElementNewVersionText).OnClicked = new Action<string>(this.onUpdateGame);
			GuiElementRichtext.DebugLogging = false;
			this.screenManager.GamePlatform.Logger.VerboseDebug("Left bottom main menu text is at {0}/{1} w/h {2},{3}", new object[] { leftBottomText.absX, leftBottomText.absY, leftBottomText.OuterWidth, leftBottomText.OuterHeight });
			int numScreenshots = 6;
			string filename = "textures/gui/backgrounds/mainmenu" + (1 + (int)(this.UnixTimeNow() / 604800L) % numScreenshots).ToString() + ".png";
			int day = DateTime.Now.Day;
			bool xmas = DateTime.Now.Month == 12 && day >= 20 && day <= 30;
			bool flag = (DateTime.Now.Month == 10 && day > 18) || (DateTime.Now.Month == 11 && day < 12);
			if (xmas)
			{
				filename = "textures/gui/backgrounds/mainmenu-xmas.png";
			}
			if (flag)
			{
				filename = "textures/gui/backgrounds/mainmenu-halloween.png";
			}
			IAsset asset = this.screenManager.GamePlatform.AssetManager.TryGet_BaseAssets(new AssetLocation(filename), true);
			BitmapRef bmp = ((asset != null) ? asset.ToBitmap(this.screenManager.api) : null);
			if (bmp != null)
			{
				this.bgtex = new LoadedTexture(this.screenManager.api, this.screenManager.GamePlatform.LoadTexture(bmp, true, 0, false), bmp.Width, bmp.Height);
				bmp.Dispose();
			}
			else
			{
				this.bgtex = new LoadedTexture(this.screenManager.api, 0, 1, 1);
			}
			ClientSettings.Inst.AddWatcher<float>("guiScale", new OnSettingsChanged<float>(this.OnGuiScaleChanged));
			byte[] pngdata = ScreenManager.Platform.AssetManager.Get("textures/gui/logo.png").Data;
			BitmapExternal bitmap = (BitmapExternal)ScreenManager.Platform.CreateBitmapFromPng(pngdata, pngdata.Length);
			ImageSurface logosurface = new ImageSurface(Format.Argb32, bitmap.Width, bitmap.Height);
			logosurface.Image(bitmap, 0, 0, bitmap.Width, bitmap.Height);
			bitmap.Dispose();
			LoadedTexture loadedTexture = this.logoTexture;
			if (loadedTexture != null)
			{
				loadedTexture.Dispose();
			}
			this.logoTexture = new LoadedTexture(this.screenManager.api);
			this.screenManager.api.Gui.LoadOrUpdateCairoTexture(logosurface, true, ref this.logoTexture);
			logosurface.Dispose();
		}

		private void onUpdateGame(string versionnumber)
		{
			if (RuntimeEnv.OS == OS.Windows)
			{
				this.screenManager.LoadScreen(new GuiScreenConfirmAction(Lang.Get("mainmenu-confirm-updategame", new object[] { versionnumber }), delegate(bool ok)
				{
					this.OnConfirmUpdateGame(ok, versionnumber);
				}, this.screenManager, this.screenManager.mainScreen, false));
				return;
			}
			NetUtil.OpenUrlInBrowser("https://account.vintagestory.at");
		}

		private void OnConfirmUpdateGame(bool ok, string versionnumber)
		{
			if (!ok)
			{
				this.screenManager.StartMainMenu();
				return;
			}
			this.screenManager.LoadScreen(new GuiScreenGetUpdate(versionnumber, this.screenManager, this.screenManager.mainScreen));
		}

		public long UnixTimeNow()
		{
			return (long)(DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
		}

		private void OnGuiScaleChanged(float newValue)
		{
			LoadedTexture versionNumberTexture = this.screenManager.versionNumberTexture;
			if (versionNumberTexture != null)
			{
				versionNumberTexture.Dispose();
			}
			this.screenManager.versionNumberTexture = this.screenManager.api.Gui.TextTexture.GenUnscaledTextTexture(GameVersion.LongGameVersion, CairoFont.WhiteDetailText(), null);
		}

		public void updateButtonActiveFlag(string key)
		{
			this.screenManager.mainMenuComposer.GetButton("singleplayer").SetActive(key == "singleplayer");
			this.screenManager.mainMenuComposer.GetButton("multiplayer").SetActive(key == "multiplayer");
			GuiElementTextButton button = this.screenManager.mainMenuComposer.GetButton("gameserver");
			if (button != null)
			{
				button.SetActive(key == "gameserver");
			}
			this.screenManager.mainMenuComposer.GetButton("settings").SetActive(key == "settings");
			this.screenManager.mainMenuComposer.GetButton("credits").SetActive(key == "credits");
			this.screenManager.mainMenuComposer.GetButton("mods").SetActive(key == "mods");
			this.screenManager.mainMenuComposer.GetButton("quit").SetActive(key == "quit");
		}

		private void OnDrawTree(Context ctx, ImageSurface surface, ElementBounds currentBounds)
		{
			ctx.Antialias = Antialias.Best;
			double paddedWidth = GuiElement.scaled((double)(ElementStdBounds.mainMenuUnscaledLogoSize + 2 * ElementStdBounds.mainMenuUnscaledLogoHorPadding));
			double paddedHeight = GuiElement.scaled((double)(ElementStdBounds.mainMenuUnscaledLogoSize + 2 * ElementStdBounds.mainMenuUnscaledLogoVerPadding));
			double height = GuiElement.scaled(100.0);
			LinearGradient gradient = new LinearGradient(0.0, 0.0, 0.0, paddedHeight + height);
			gradient.AddColorStop(0.0, new Color(0.2196078431372549, 0.17647058823529413, 0.12549019607843137, 0.5));
			gradient.AddColorStop(0.5, new Color(0.28627450980392155, 0.22745098039215686, 0.1607843137254902, 0.5));
			gradient.AddColorStop(1.0, new Color(0.28627450980392155, 0.22745098039215686, 0.1607843137254902, 0.0));
			GuiElement.Rectangle(ctx, 0.0, 0.0, paddedWidth, paddedHeight + height);
			ctx.SetSource(gradient);
			ctx.Fill();
			gradient.Dispose();
			byte[] pngdata = ScreenManager.Platform.AssetManager.Get("textures/gui/tree.png").Data;
			BitmapExternal bitmap = (BitmapExternal)ScreenManager.Platform.CreateBitmapFromPng(pngdata, pngdata.Length);
			surface.Image(bitmap, (int)currentBounds.drawX, (int)currentBounds.drawY, (int)currentBounds.InnerWidth, (int)currentBounds.InnerHeight);
			bitmap.Dispose();
		}

		private void OnDrawSidebar(Context ctx, ImageSurface surface, ElementBounds currentBounds)
		{
			double woodPlankWidth = GuiElement.scaled((double)ElementStdBounds.mainMenuUnscaledWoodPlankWidth);
			double paddedWidth = GuiElement.scaled((double)(ElementStdBounds.mainMenuUnscaledLogoSize + 2 * ElementStdBounds.mainMenuUnscaledLogoHorPadding)) + woodPlankWidth;
			SurfacePattern pattern = GuiElement.getPattern(this.screenManager.api, new AssetLocation("gui/backgrounds/oak.png"), true, 255, 0.125f);
			GuiElement.Rectangle(ctx, paddedWidth - woodPlankWidth, 0.0, woodPlankWidth, currentBounds.OuterHeight);
			ctx.SetSource(pattern);
			ctx.Fill();
			LinearGradient gradient = new LinearGradient(paddedWidth - 5.0 - woodPlankWidth, 0.0, paddedWidth - woodPlankWidth, 0.0);
			gradient.AddColorStop(0.0, new Color(0.0, 0.0, 0.0, 0.0));
			gradient.AddColorStop(0.6, new Color(0.0, 0.0, 0.0, 0.38));
			gradient.AddColorStop(1.0, new Color(0.0, 0.0, 0.0, 0.38));
			ctx.Operator = Operator.Multiply;
			GuiElement.Rectangle(ctx, paddedWidth - 5.0 - woodPlankWidth, 0.0, 5.0, currentBounds.OuterHeight);
			ctx.SetSource(gradient);
			ctx.Fill();
			gradient.Dispose();
			ctx.Operator = Operator.Over;
		}

		private void didClickLink(LinkTextComponent comp)
		{
			string href = comp.Href;
			if (href.StartsWithOrdinal("https://"))
			{
				NetUtil.OpenUrlInBrowser(href);
			}
			if (href.Contains("logout"))
			{
				this.OnLogout();
			}
		}

		private void OnLogout()
		{
			this.screenManager.sessionManager.DoLogout();
			ClientSettings.UserEmail = "";
			ClientSettings.PlayerName = "";
			ClientSettings.Sessionkey = "";
			ClientSettings.SessionSignature = "";
			ClientSettings.MpToken = "";
			ClientSettings.Inst.Save(true);
			this.screenManager.LoadAndCacheScreen(typeof(GuiScreenLogin));
		}

		private bool OnCredits()
		{
			this.updateButtonActiveFlag("credits");
			this.screenManager.LoadAndCacheScreen(typeof(GuiScreenCredits));
			return true;
		}

		private bool OnMods()
		{
			this.updateButtonActiveFlag("mods");
			this.screenManager.LoadAndCacheScreen(typeof(GuiScreenMods));
			return true;
		}

		private bool OnSettings()
		{
			this.updateButtonActiveFlag("settings");
			this.screenManager.LoadAndCacheScreen(typeof(GuiScreenSettings));
			return true;
		}

		public bool OnSingleplayer()
		{
			this.updateButtonActiveFlag("singleplayer");
			this.screenManager.LoadAndCacheScreen(typeof(GuiScreenSingleplayer));
			return true;
		}

		public bool OnMultiplayer()
		{
			this.updateButtonActiveFlag("multiplayer");
			this.screenManager.LoadAndCacheScreen(typeof(GuiScreenMultiplayer));
			return true;
		}

		private bool OnGameServer()
		{
			this.updateButtonActiveFlag("gameserver");
			this.screenManager.LoadAndCacheScreen(typeof(GuiScreenServerDashboard));
			return true;
		}

		public bool OnQuit()
		{
			ClientSettings.Inst.Save(true);
			this.updateButtonActiveFlag("quit");
			ParticleRenderer2D particleRenderer2D = this.particleSystem;
			if (particleRenderer2D != null)
			{
				particleRenderer2D.Dispose();
			}
			this.screenManager.GamePlatform.WindowExit("Main screen quit button was pressed");
			return true;
		}

		public void OnMouseDown(MouseEvent e)
		{
			if ((double)e.X < GuiElement.scaled((double)(ElementStdBounds.mainMenuUnscaledLogoSize + ElementStdBounds.mainMenuUnscaledLogoHorPadding)) && (double)e.Y < GuiElement.scaled((double)ElementStdBounds.mainMenuUnscaledLogoSize) && e.Y > 50)
			{
				this.screenManager.LoadScreen(this.screenManager.mainScreen);
				e.Handled = true;
				return;
			}
			this.screenManager.mainMenuComposer.OnMouseDown(e);
		}

		public void OnMouseUp(MouseEvent e)
		{
			this.screenManager.mainMenuComposer.OnMouseUp(e);
		}

		internal void OnMouseMove(MouseEvent e)
		{
			this.screenManager.mainMenuComposer.OnMouseMove(e);
		}

		public void RenderBg(float dt, bool mainMenuVisible)
		{
			this.Render(dt, this.screenManager.GamePlatform.EllapsedMs, mainMenuVisible, false);
		}

		protected void Render(float dt, long ellapsedMs, bool mainMenuVisible, bool onlyBackground = false)
		{
			if (this.renderStartMs == 0L)
			{
				this.renderStartMs = ellapsedMs;
			}
			float baseIter = (float)((double)ellapsedMs / 1500.0);
			float easein = GameMath.Clamp((float)(ellapsedMs - this.renderStartMs) / 60000f, 0f, 1f);
			float dx = (GameMath.Sin(baseIter / 2.4f) * 12f + GameMath.Sin(baseIter / 2f) * 8f + GameMath.Sin(baseIter / 1.2f) * 4f) / 2f;
			float num = (GameMath.Sin(baseIter / 2.3f) * 9f + GameMath.Sin(baseIter / 1.5f) * 11f + GameMath.Sin(baseIter / 1.4f) * 4f) / 2f;
			float winWidth = (float)Math.Max(10, this.screenManager.GamePlatform.WindowSize.Width);
			float winHeight = (float)Math.Max(10, this.screenManager.GamePlatform.WindowSize.Height);
			float mouseRelx = (float)this.screenManager.api.inputapi.MouseX / winWidth;
			float mouseRely = (float)this.screenManager.api.inputapi.MouseY / winHeight;
			float dtCapped = Math.Min(0.033333335f, dt);
			this.curdx += (-30f * mouseRelx + 15f - this.curdx) * 5f * dtCapped;
			this.curdy += (-30f * mouseRely + 15f - this.curdy) * 1.5f * dtCapped;
			dx += this.curdx;
			float num2 = num + this.curdy;
			float zoom = Math.Max(1f, (winWidth + 80f) / winWidth + (1f + GameMath.Sin(baseIter / 5f) + GameMath.Sin(baseIter / 6f)) / 5f) + 0.05f;
			dx *= 1f + zoom - (winWidth + 40f) / winWidth;
			float num3 = num2 * (1f + zoom - (winWidth + 40f) / winWidth);
			dx = GameMath.Clamp(dx, -100f, 100f);
			float num4 = GameMath.Clamp(num3, -100f, 100f);
			double ratioW = (double)(winWidth / (float)this.bgtex.Width);
			double ratioH = (double)(winHeight / (float)this.bgtex.Height);
			double ratio = ((ratioW > ratioH) ? ratioW : ratioH);
			float renderWidth = (float)((double)this.bgtex.Width * ratio);
			float renderHeight = (float)((double)this.bgtex.Height * ratio);
			dx *= easein;
			float num5 = num4 * easein;
			zoom = 1f + (zoom - 1f) * easein;
			float x = dx + (1f - zoom) * renderWidth / 2f;
			float y = num5 + (1f - zoom) * renderHeight / 2f;
			this.screenManager.api.Render.Render2DTexture(this.bgtex.TextureId, x, y, renderWidth * zoom, renderHeight * zoom, 10f, null);
			ShaderPrograms.Gui.Stop();
			this.screenManager.GamePlatform.GlDepthMask(false);
			this.spawnParticles(dtCapped);
			float[] pmat = this.screenManager.api.renderapi.pMatrix;
			this.particleSystem.pMatrix = pmat;
			this.particleSystem.mvMatrix = Mat4f.Identity(new float[16]);
			Mat4f.Translate(this.particleSystem.mvMatrix, this.particleSystem.mvMatrix, 2f * this.curdx, 2f * this.curdy, 0f);
			Mat4f.Translate(this.particleSystem.mvMatrix, this.particleSystem.mvMatrix, renderWidth / 2f, renderHeight / 2f, 0f);
			Mat4f.Scale(this.particleSystem.mvMatrix, this.particleSystem.mvMatrix, zoom, zoom, zoom);
			Mat4f.Translate(this.particleSystem.mvMatrix, this.particleSystem.mvMatrix, -renderWidth / 2f, -renderHeight / 2f, 0f);
			this.particleSystem.Render(dt);
			this.screenManager.GamePlatform.GlDepthMask(true);
			float windowSizeX = (float)this.screenManager.GamePlatform.WindowSize.Width;
			double lx = this.screenManager.guiMainmenuLeft.Width + GuiElement.scaled(15.0);
			float lwidth = (windowSizeX - (float)lx) * 0.8f;
			lx += (double)((windowSizeX - lwidth) / 4f);
			if (!mainMenuVisible)
			{
				lx = (double)(windowSizeX * 0.15f);
				lwidth = windowSizeX * 0.7f;
			}
			float lheight = (float)this.logoTexture.Height * (lwidth / (float)this.logoTexture.Width);
			this.screenManager.api.Render.Render2DTexture(this.logoTexture.TextureId, (float)lx + (float)Math.Sin((double)ellapsedMs / 2000.0) * 10f, (float)GuiElement.scaled(25.0) + (float)Math.Sin(20.0 + (double)ellapsedMs / 2220.0) * 10f, lwidth, lheight, 20f, null);
		}

		private void spawnParticles(float dt)
		{
			ClientPlatformAbstract plt = this.screenManager.GamePlatform;
			this.minPos.X = 0.0;
			this.minPos.Y = (double)((float)plt.WindowSize.Height * 0.5f);
			this.minPos.Z = -50.0;
			this.addPos.X = (double)plt.WindowSize.Width;
			this.addPos.Y = (double)((float)plt.WindowSize.Height * 0.75f);
			if (this.prop == null)
			{
				this.prop = new SimpleParticleProperties(0.025f, 0.125f, ColorUtil.ToRgba(40, 255, 255, 255), new Vec3d(0.0, 0.0, 0.0), new Vec3d(), new Vec3f(), new Vec3f(), 5f, 0f, 0f, 0.4f, EnumParticleModel.Cube);
				this.prop.MinPos = this.minPos;
				this.prop.AddPos = this.addPos;
				this.prop.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.CLAMPEDPOSITIVESINUS, 3.1415927f);
				this.minPos.X = 0.0;
				this.minPos.Y = (double)((float)plt.WindowSize.Height * 0f);
				this.addPos.X = (double)plt.WindowSize.Width;
				this.addPos.Y = (double)((float)plt.WindowSize.Height * 1.25f);
				for (int i = 0; i < 1000; i++)
				{
					float nearness = 3f * (2.5f + (float)this.rand.NextDouble() * 4f);
					this.prop.MinVelocity.Set(-12f * (0.5f + nearness / 13f), -3f - nearness * 2f, 0f);
					this.prop.AddVelocity.Set(24f - 12f * (1f - nearness / 13f), 3f, 0f);
					this.prop.MinSize = Math.Max(1f, (float)Math.Pow((double)nearness, 1.6) / 17f);
					this.prop.MaxSize = this.prop.MinSize * 1.1f;
					this.prop.MinQuantity = 0.025f * dt * 33f;
					this.prop.AddQuantity = 0.1f * dt * 33f;
					this.PrepareParticleProps(dt);
					this.particleSystem.Spawn(this.prop);
				}
				for (ParticleBase particle = this.particleSystem.Pool.ParticlesPool.FirstAlive; particle != null; particle = particle.Next)
				{
					particle.SecondsAlive = (float)this.rand.NextDouble() * particle.LifeLength;
				}
			}
			this.PrepareParticleProps(dt);
			this.particleSystem.Spawn(this.prop);
		}

		private void PrepareParticleProps(float dt)
		{
			float nearness = 2.2f * (2.5f + (float)this.rand.NextDouble() * 4f);
			this.prop.MinVelocity.Set(-12f * (0.5f + nearness / 13f), -3f - nearness * 2f, 0f);
			this.prop.AddVelocity.Set(24f - 12f * (1f - nearness / 13f), 3f, 0f);
			this.prop.MinSize = Math.Max(1f, (float)Math.Pow((double)nearness, 1.6) / 17f);
			this.prop.MaxSize = this.prop.MinSize * 1.1f;
			this.prop.MinQuantity = 0.025f * dt * 33f;
			this.prop.AddQuantity = 0.1f * dt * 33f;
		}

		public void Dispose()
		{
			LoadedTexture loadedTexture = this.bgtex;
			if (loadedTexture != null)
			{
				loadedTexture.Dispose();
			}
			this.bgtex = null;
			LoadedTexture loadedTexture2 = this.logoTexture;
			if (loadedTexture2 != null)
			{
				loadedTexture2.Dispose();
			}
			ParticleRenderer2D particleRenderer2D = this.particleSystem;
			if (particleRenderer2D == null)
			{
				return;
			}
			particleRenderer2D.Dispose();
		}

		private ElementBounds sidebarBounds;

		private ScreenManager screenManager;

		private LoadedTexture bgtex;

		private LoadedTexture logoTexture;

		private ParticleRenderer2D particleSystem;

		private long renderStartMs;

		private float curdx;

		private float curdy;

		private SimpleParticleProperties prop;

		private Vec3d minPos = new Vec3d();

		private Vec3d addPos = new Vec3d();

		private Random rand = new Random();
	}
}
