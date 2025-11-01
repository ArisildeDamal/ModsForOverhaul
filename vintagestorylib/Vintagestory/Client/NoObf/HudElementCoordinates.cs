using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class HudElementCoordinates : HudElement
	{
		public override string ToggleKeyCombinationCode
		{
			get
			{
				return "coordinateshud";
			}
		}

		public HudElementCoordinates(ICoreClientAPI capi)
			: base(capi)
		{
		}

		public override void OnOwnPlayerDataReceived()
		{
			ElementBounds textBounds = ElementBounds.Fixed(EnumDialogArea.None, 0.0, 0.0, 190.0, 48.0);
			ElementBounds overlayBounds = textBounds.ForkBoundingParent(5.0, 5.0, 5.0, 5.0);
			ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightTop).WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, GuiStyle.DialogToScreenPadding);
			base.SingleComposer = this.capi.Gui.CreateCompo("coordinateshud", dialogBounds).AddGameOverlay(overlayBounds, null).AddDynamicText("", CairoFont.WhiteSmallishText().WithOrientation(EnumTextOrientation.Center), textBounds, "text")
				.Compose(true);
			if (ClientSettings.ShowCoordinateHud)
			{
				this.TryOpen();
			}
		}

		public override void OnBlockTexturesLoaded()
		{
			base.OnBlockTexturesLoaded();
			if (!this.capi.World.Config.GetBool("allowCoordinateHud", true))
			{
				(this.capi.World as ClientMain).EnqueueMainThreadTask(delegate
				{
					(this.capi.World as ClientMain).UnregisterDialog(this);
					this.capi.Input.SetHotKeyHandler("coordinateshud", null);
					this.Dispose();
				}, "unreg");
				return;
			}
			this.capi.Event.RegisterGameTickListener(new Action<float>(this.Every250ms), 250, 0);
			ClientSettings.Inst.AddWatcher<bool>("showCoordinateHud", delegate(bool on)
			{
				if (on)
				{
					this.TryOpen();
					return;
				}
				this.TryClose();
			});
		}

		private void Every250ms(float dt)
		{
			if (!this.IsOpened())
			{
				return;
			}
			BlockPos pos = this.capi.World.Player.Entity.Pos.AsBlockPos;
			int ypos = pos.Y;
			pos.Sub(this.capi.World.DefaultSpawnPosition.AsBlockPos);
			string facing = BlockFacing.HorizontalFromYaw(this.capi.World.Player.Entity.Pos.Yaw).ToString();
			facing = Lang.Get("facing-" + facing, Array.Empty<object>());
			string coords = string.Concat(new string[]
			{
				pos.X.ToString(),
				", ",
				ypos.ToString(),
				", ",
				pos.Z.ToString(),
				"\n",
				facing
			});
			if (ClientSettings.ExtendedDebugInfo)
			{
				string text;
				if (!(facing == "North"))
				{
					if (!(facing == "East"))
					{
						if (!(facing == "South"))
						{
							if (!(facing == "West"))
							{
								text = string.Empty;
							}
							else
							{
								text = " / X-";
							}
						}
						else
						{
							text = " / Z+";
						}
					}
					else
					{
						text = " / X+";
					}
				}
				else
				{
					text = " / Z-";
				}
				string info = text;
				coords += info;
			}
			base.SingleComposer.GetDynamicText("text").SetNewText(coords, false, false, false);
			List<ElementBounds> boundsList = this.capi.Gui.GetDialogBoundsInArea(EnumDialogArea.RightTop);
			base.SingleComposer.Bounds.absOffsetY = GuiStyle.DialogToScreenPadding;
			for (int i = 0; i < boundsList.Count; i++)
			{
				if (boundsList[i] != base.SingleComposer.Bounds)
				{
					ElementBounds bounds = boundsList[i];
					base.SingleComposer.Bounds.absOffsetY = GuiStyle.DialogToScreenPadding + bounds.absY + bounds.OuterHeight;
					return;
				}
			}
		}

		public override void OnGuiOpened()
		{
			base.OnGuiOpened();
			ClientSettings.ShowCoordinateHud = true;
		}

		public override void OnGuiClosed()
		{
			base.OnGuiClosed();
			ClientSettings.ShowCoordinateHud = false;
		}

		public override void OnRenderGUI(float deltaTime)
		{
			base.OnRenderGUI(deltaTime);
		}
	}
}
