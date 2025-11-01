using System;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace Vintagestory.Client.NoObf
{
	public class HudIngameError : HudElement
	{
		public override double InputOrder
		{
			get
			{
				return 1.0;
			}
		}

		public override string ToggleKeyCombinationCode
		{
			get
			{
				return null;
			}
		}

		public HudIngameError(ICoreClientAPI capi)
			: base(capi)
		{
			capi.Event.InGameError += this.Event_InGameError;
			capi.Event.RegisterGameTickListener(new Action<float>(this.OnGameTick), 20, 0);
		}

		private void Event_InGameError(object sender, string errorCode, string text)
		{
			if (this.elem == null)
			{
				return;
			}
			this.errorTextActiveMs = this.capi.InWorldEllapsedMilliseconds;
			this.elem.SetNewText(text);
			this.elem.SetVisible(true);
			this.x = this.elem.Bounds.absFixedX;
			this.y = this.elem.Bounds.absFixedX;
		}

		private void OnGameTick(float dt)
		{
			if (this.errorTextActiveMs == 0L)
			{
				return;
			}
			if (this.capi.InWorldEllapsedMilliseconds - this.errorTextActiveMs > 5000L)
			{
				this.errorTextActiveMs = 0L;
				this.elem.SetVisible(false);
			}
			if (this.capi.InWorldEllapsedMilliseconds - this.errorTextActiveMs < 500L)
			{
				float intensity = Math.Min(0.25f, 1f - (float)(this.capi.InWorldEllapsedMilliseconds - this.errorTextActiveMs) / 500f) * RuntimeEnv.GUIScale;
				this.Composers["ingameerror"].Bounds.absFixedX = this.x + (double)intensity * (this.capi.World.Rand.NextDouble() * 10.0 - 5.0);
				this.Composers["ingameerror"].Bounds.absFixedY = this.y + (double)intensity * (this.capi.World.Rand.NextDouble() * 10.0 - 5.0);
				return;
			}
			this.Composers["ingameerror"].Bounds.absFixedX = this.x;
			this.Composers["ingameerror"].Bounds.absFixedY = this.y;
		}

		public override void OnOwnPlayerDataReceived()
		{
			this.ComposeGuis();
		}

		public void ComposeGuis()
		{
			ElementBounds dialogBounds = new ElementBounds
			{
				Alignment = EnumDialogArea.CenterBottom,
				BothSizing = ElementSizing.Fixed,
				fixedWidth = 600.0,
				fixedHeight = 5.0
			};
			ElementBounds iteminfoBounds = ElementBounds.Fixed(0.0, -155.0, 600.0, 30.0);
			base.ClearComposers();
			CairoFont font = CairoFont.WhiteSmallText().WithColor(GuiStyle.ErrorTextColor).WithStroke(GuiStyle.DialogBorderColor, 2.0)
				.WithOrientation(EnumTextOrientation.Center);
			this.Composers["ingameerror"] = this.capi.Gui.CreateCompo("ingameerror", dialogBounds.FlatCopy()).BeginChildElements(dialogBounds).AddTranspHoverText("", font, 600, iteminfoBounds, "errortext")
				.EndChildElements()
				.Compose(true);
			this.elem = this.Composers["ingameerror"].GetHoverText("errortext");
			this.elem.ZPosition = 100f;
			this.elem.SetFollowMouse(false);
			this.elem.SetAutoWidth(false);
			this.elem.SetAutoDisplay(false);
			this.elem.fillBounds = true;
			this.TryOpen();
		}

		public override bool TryClose()
		{
			return false;
		}

		public override bool ShouldReceiveKeyboardEvents()
		{
			return true;
		}

		public override void OnRenderGUI(float deltaTime)
		{
			base.OnRenderGUI(deltaTime);
		}

		public override bool Focusable
		{
			get
			{
				return false;
			}
		}

		protected override void OnFocusChanged(bool on)
		{
		}

		private long errorTextActiveMs;

		private double x;

		private double y;

		private GuiElementHoverText elem;
	}
}
