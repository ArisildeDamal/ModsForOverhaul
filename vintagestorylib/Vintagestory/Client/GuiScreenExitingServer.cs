using System;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;
using Vintagestory.Server;

namespace Vintagestory.Client
{
	internal class GuiScreenExitingServer : GuiScreenConnectingToServer
	{
		public GuiScreenExitingServer(ScreenManager screenManager, GuiScreen parent)
			: base(true, screenManager, null)
		{
			if (ClientSettings.DeveloperMode)
			{
				base.ComposeDeveloperLogDialog("exitingspserver", Lang.Get("Shutting down singleplayer server...", Array.Empty<object>()), "");
			}
			else
			{
				base.ComposePlayerLogDialog("exitingspserver", Lang.Get("It pauses.", Array.Empty<object>()));
			}
			if (ServerMain.Logger != null)
			{
				ServerMain.Logger.EntryAdded += base.LogAdded;
			}
		}

		public override void RenderToDefaultFramebuffer(float dt)
		{
			if (!ClientSettings.DeveloperMode)
			{
				this.ScreenManager.mainScreen.Render(dt, 0L, true);
			}
			if (ScreenManager.Platform.EllapsedMs - this.lastLogfileCheck > 400L)
			{
				base.updateLogText();
				this.lastLogfileCheck = ScreenManager.Platform.EllapsedMs;
			}
			this.ElementComposer.Render(dt);
			this.ElementComposer.PostRender(dt);
			if (!ScreenManager.Platform.IsServerRunning)
			{
				this.ScreenManager.StartMainMenu();
				return;
			}
		}
	}
}
