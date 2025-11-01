using System;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class SystemScreenshot : ClientSystem
	{
		public override string Name
		{
			get
			{
				return "scr";
			}
		}

		public SystemScreenshot(ClientMain game)
			: base(game)
		{
			this.takeScreenshot = false;
			ScreenManager.hotkeyManager.SetHotKeyHandler("megascreenshot", new ActionConsumable<KeyCombination>(this.takeMegaScreenshot), true);
			ScreenManager.hotkeyManager.SetHotKeyHandler("screenshot", delegate(KeyCombination viaKeyComb)
			{
				this.usePrimaryFramebuffer = ClientSettings.ScaleScreenshot;
				this.takeScreenshot = true;
				return true;
			}, true);
			game.eventManager.RegisterRenderer(new Action<float>(this.AfterFinalCompo), EnumRenderStage.AfterFinalComposition, this.Name, 2.0);
			game.eventManager.RegisterRenderer(new Action<float>(this.OnFrameDone), EnumRenderStage.Done, this.Name, 2.0);
		}

		private bool takeMegaScreenshot(KeyCombination t1)
		{
			if (this.doTakeMegaScreenshot > 0)
			{
				return true;
			}
			this.usePrimaryFramebuffer = true;
			this.doTakeMegaScreenshot = 2;
			this.scaleScreenshotbefore = ClientSettings.ScaleScreenshot;
			ClientSettings.ScaleScreenshot = false;
			this.ssaabefore = ClientSettings.SSAA;
			ClientSettings.SSAA = ClientSettings.MegaScreenshotSizeMul;
			ScreenManager.Platform.RebuildFrameBuffers();
			return true;
		}

		private void AfterFinalCompo(float dt)
		{
			if (this.usePrimaryFramebuffer && this.takeScreenshot && this.game.Platform.EllapsedMs - this.nextScreenshotdelay > 1000L)
			{
				this.DoScreenshot(null);
				this.takeScreenshot = false;
				this.nextScreenshotdelay = this.game.Platform.EllapsedMs;
				this.game.PlaySound(new AssetLocation("sounds/camerasnap"), false, 1f);
				return;
			}
			if (this.game.timelapse > 0f)
			{
				string timelapsePath = Path.Combine(GamePaths.Screenshots, "timelapse");
				Directory.CreateDirectory(timelapsePath);
				this.DoScreenshot(timelapsePath);
			}
		}

		private void DoScreenshot(string path)
		{
			this.game.Platform.LoadFrameBuffer(EnumFrameBuffer.Primary);
			try
			{
				string filename = this.game.Platform.SaveScreenshot(path, null, false, ClientSettings.FlipScreenshot, this.GetMetaData());
				if (path == null)
				{
					string text = ((this.doTakeMegaScreenshot > 0) ? Lang.Get("screenshottaken-mega", new object[] { filename }) : Lang.Get("screenshottaken-normal", new object[] { filename }));
					this.game.ShowChatMessage(text);
				}
			}
			catch (Exception e)
			{
				this.game.Logger.Error("Screenshot failed:");
				this.game.Logger.Error(e);
				this.game.ShowChatMessage(Lang.Get("Unable to take screenshot. Check client-main.log file for error.", Array.Empty<object>()));
			}
			this.game.Platform.UnloadFrameBuffer(EnumFrameBuffer.Default);
		}

		public void OnFrameDone(float dt)
		{
			if (!this.usePrimaryFramebuffer && this.takeScreenshot && this.game.Platform.EllapsedMs - this.nextScreenshotdelay > 1000L)
			{
				this.game.Platform.LoadFrameBuffer(EnumFrameBuffer.Default);
				this.takeScreenshot = false;
				this.nextScreenshotdelay = this.game.Platform.EllapsedMs;
				this.game.PlaySound(new AssetLocation("sounds/camerasnap"), false, 1f);
				try
				{
					string filename = this.game.Platform.SaveScreenshot(null, null, false, ClientSettings.FlipScreenshot, this.GetMetaData());
					string text = ((this.doTakeMegaScreenshot > 0) ? Lang.Get("screenshottaken-mega", new object[] { filename }) : Lang.Get("screenshottaken-normal", new object[] { filename }));
					this.game.ShowChatMessage(text);
				}
				catch (Exception e)
				{
					this.game.Logger.Error("Screenshot failed:");
					this.game.Logger.Error(e);
					this.game.ShowChatMessage(Lang.Get("Unable to take screenshot. Check client-main.log log file for error.", Array.Empty<object>()));
				}
				this.game.Platform.UnloadFrameBuffer(EnumFrameBuffer.Default);
			}
			if (this.doTakeMegaScreenshot > 0)
			{
				this.doTakeMegaScreenshot--;
				if (this.doTakeMegaScreenshot == 0)
				{
					ClientSettings.ScaleScreenshot = this.scaleScreenshotbefore;
					ClientSettings.SSAA = this.ssaabefore;
					ScreenManager.Platform.RebuildFrameBuffers();
					return;
				}
				this.takeScreenshot = true;
			}
		}

		public string GetMetaData()
		{
			if (ClientSettings.ScreenshotExifDataMode > 0)
			{
				return JsonUtil.ToString<ScreenshotLocationMetaData>(new ScreenshotLocationMetaData
				{
					Pos = this.game.player.Entity.Pos.XYZ,
					RollYawPitch = new Vec3f(this.game.player.Entity.Pos.Roll, this.game.player.Entity.Pos.Yaw, this.game.player.Entity.Pos.Pitch),
					WorldSeed = this.game.Seed,
					WorldConfig = this.game.Config.ToJsonToken()
				});
			}
			return "";
		}

		public override EnumClientSystemType GetSystemType()
		{
			return EnumClientSystemType.Misc;
		}

		private bool takeScreenshot;

		private int doTakeMegaScreenshot;

		private long nextScreenshotdelay;

		private bool scaleScreenshotbefore;

		private float ssaabefore;

		private bool usePrimaryFramebuffer;
	}
}
