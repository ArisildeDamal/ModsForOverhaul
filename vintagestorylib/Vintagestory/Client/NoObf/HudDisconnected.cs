using System;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace Vintagestory.Client.NoObf
{
	public class HudDisconnected : HudElement
	{
		public HudDisconnected(ICoreClientAPI capi)
			: base(capi)
		{
		}

		public override void OnBlockTexturesLoaded()
		{
			ElementBounds counterBounds = ElementBounds.Fixed(EnumDialogArea.RightBottom, 0.0, 0.0, 45.0, 20.0);
			ElementBounds reasonBounds = ElementBounds.Fixed(10.0, 10.0, 330.0, 50.0);
			string causes = (ScreenManager.Platform.IsServerRunning ? "Server overloaded or crashed" : "Bad connection or server overloaded/crashed");
			this.disconnectedDialog = this.capi.Gui.CreateCompo("disconnecteddialog", ElementBounds.Fixed(EnumDialogArea.RightTop, 0.0, 0.0, 380.0, 60.0).WithFixedAlignmentOffset(-10.0, 10.0)).AddShadedDialogBG(ElementBounds.Fill, false, 5.0, 0.75f).AddDynamicText("", CairoFont.WhiteDetailText().WithOrientation(EnumTextOrientation.Center), counterBounds, "countertext")
				.AddStaticText(Lang.Get("Host not responding. Possible causes: {0}", new object[] { Lang.Get(causes, Array.Empty<object>()) }), CairoFont.WhiteDetailText(), reasonBounds, null)
				.AddStaticCustomDraw(ElementBounds.Fixed(EnumDialogArea.RightTop, 0.0, 0.0, 25.0, 25.0).WithFixedPadding(10.0, 10.0), new DrawDelegateWithBounds(this.OnDialogDraw))
				.Compose(true);
			this.TryOpen();
			ClientMain clientMain = this.capi.World as ClientMain;
			clientMain.LastReceivedMilliseconds = clientMain.ElapsedMilliseconds;
		}

		public override bool TryClose()
		{
			return false;
		}

		public override void OnRenderGUI(float deltaTime)
		{
			ClientMain game = this.capi.World as ClientMain;
			if (game.IsPaused)
			{
				return;
			}
			float lagSeconds = (float)(game.ElapsedMilliseconds - game.LastReceivedMilliseconds) / 1000f;
			if (this.disconnectedDialog != null && (lagSeconds >= 5f || (game.IsSingleplayer && !ScreenManager.Platform.IsServerRunning)))
			{
				this.disconnectedDialog.GetDynamicText("countertext").SetNewText(((int)lagSeconds).ToString() ?? "", false, false, false);
				this.disconnectedDialog.Render(deltaTime);
				this.disconnectedDialog.PostRender(deltaTime);
			}
		}

		private void OnDialogDraw(Context ctx, ImageSurface surface, ElementBounds currentBounds)
		{
			this.capi.Gui.Icons.DrawConnectionQuality(ctx, currentBounds.drawX, currentBounds.drawY, 0, currentBounds.InnerWidth);
		}

		public override bool Focusable
		{
			get
			{
				return false;
			}
		}

		public override void Dispose()
		{
			base.Dispose();
			GuiComposer guiComposer = this.disconnectedDialog;
			if (guiComposer == null)
			{
				return;
			}
			guiComposer.Dispose();
		}

		private GuiComposer disconnectedDialog;
	}
}
