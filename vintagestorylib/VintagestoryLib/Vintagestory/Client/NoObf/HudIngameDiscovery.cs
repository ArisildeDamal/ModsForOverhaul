using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class HudIngameDiscovery : HudElement
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

		public HudIngameDiscovery(ICoreClientAPI capi)
			: base(capi)
		{
			capi.Event.InGameDiscovery += this.Event_InGameDiscovery;
			capi.Event.RegisterGameTickListener(new Action<float>(this.OnGameTick), 20, 0);
		}

		private void Event_InGameDiscovery(object sender, string errorCode, string text)
		{
			this.messageQueue.Enqueue(text);
			this.x = this.elem.Bounds.absFixedX;
			this.y = this.elem.Bounds.absFixedX;
		}

		private void OnGameTick(float dt)
		{
			if (this.textActiveMs == 0L && this.messageQueue.Count == 0)
			{
				return;
			}
			if (this.textActiveMs == 0L)
			{
				this.textActiveMs = this.capi.InWorldEllapsedMilliseconds;
				this.fadeCol.A = 0f;
				this.elem.SetNewText(this.messageQueue.Dequeue());
				this.elem.SetVisible(true);
				return;
			}
			long visibleMsPassed = this.capi.InWorldEllapsedMilliseconds - this.textActiveMs;
			long visibleMsLeft = (long)this.durationVisibleMs - visibleMsPassed;
			if (visibleMsLeft <= 0L)
			{
				this.textActiveMs = 0L;
				this.elem.SetVisible(false);
				return;
			}
			if (visibleMsPassed < 250L)
			{
				this.fadeCol.A = (float)visibleMsPassed / 240f;
			}
			else
			{
				this.fadeCol.A = 1f;
			}
			if (visibleMsLeft < 1000L)
			{
				this.fadeCol.A = (float)visibleMsLeft / 990f;
			}
		}

		public override void OnOwnPlayerDataReceived()
		{
			this.ComposeGuis();
		}

		public void ComposeGuis()
		{
			ElementBounds dialogBounds = new ElementBounds
			{
				Alignment = EnumDialogArea.CenterMiddle,
				BothSizing = ElementSizing.Fixed,
				fixedWidth = 600.0,
				fixedHeight = 5.0
			};
			ElementBounds iteminfoBounds = ElementBounds.Fixed(0.0, -155.0, 700.0, 30.0);
			base.ClearComposers();
			CairoFont font = CairoFont.WhiteMediumText().WithFont(GuiStyle.DecorativeFontName).WithColor(GuiStyle.DiscoveryTextColor)
				.WithStroke(GuiStyle.DialogBorderColor, 2.0)
				.WithOrientation(EnumTextOrientation.Center);
			this.Composers["ingameerror"] = this.capi.Gui.CreateCompo("ingameerror", dialogBounds.FlatCopy()).PremultipliedAlpha(false).BeginChildElements(dialogBounds)
				.AddTranspHoverText("", font, 700, iteminfoBounds, "discoverytext")
				.EndChildElements()
				.Compose(true);
			this.elem = this.Composers["ingameerror"].GetHoverText("discoverytext");
			this.elem.SetFollowMouse(false);
			this.elem.SetAutoWidth(false);
			this.elem.SetAutoDisplay(false);
			this.elem.fillBounds = true;
			this.elem.RenderColor = this.fadeCol;
			this.elem.ZPosition = 60f;
			this.elem.RenderAsPremultipliedAlpha = false;
			this.TryOpen();
		}

		public override bool TryClose()
		{
			return false;
		}

		public override bool ShouldReceiveKeyboardEvents()
		{
			return false;
		}

		public override bool ShouldReceiveMouseEvents()
		{
			return false;
		}

		public override void OnRenderGUI(float deltaTime)
		{
			if (this.fadeCol.A > 0f)
			{
				base.OnRenderGUI(deltaTime);
			}
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

		private double x;

		private double y;

		private GuiElementHoverText elem;

		private Vec4f fadeCol = new Vec4f(1f, 1f, 1f, 1f);

		private long textActiveMs;

		private int durationVisibleMs = 6000;

		private Queue<string> messageQueue = new Queue<string>();
	}
}
