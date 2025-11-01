using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.Client.NoObf
{
	public class HudStatbar : HudElement
	{
		public override double InputOrder
		{
			get
			{
				return 1.0;
			}
		}

		public HudStatbar(ICoreClientAPI capi)
			: base(capi)
		{
			capi.Event.RegisterGameTickListener(new Action<float>(this.OnGameTick), 20, 0);
			capi.Event.RegisterGameTickListener(new Action<float>(this.OnFlashStatbars), 2500, 0);
		}

		public override string ToggleKeyCombinationCode
		{
			get
			{
				return null;
			}
		}

		private void OnGameTick(float dt)
		{
			this.UpdateHealth();
			this.UpdateOxygen();
			this.UpdateSaturation();
		}

		private void OnFlashStatbars(float dt)
		{
			ITreeAttribute healthTree = this.capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute("health");
			if (healthTree != null && this.healthbar != null)
			{
				float? health = healthTree.TryGetFloat("currenthealth");
				float? num = healthTree.TryGetFloat("maxhealth");
				float? num2 = health;
				float? num3 = num;
				double? num4 = (((num2 != null) & (num3 != null)) ? new double?((double)(num2.GetValueOrDefault() / num3.GetValueOrDefault())) : null);
				double num5 = 0.2;
				if ((num4.GetValueOrDefault() < num5) & (num4 != null))
				{
					this.healthbar.ShouldFlash = true;
				}
			}
			ITreeAttribute hungerTree = this.capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute("hunger");
			if (hungerTree != null && this.saturationbar != null)
			{
				float? saturation = hungerTree.TryGetFloat("currentsaturation");
				float? num6 = hungerTree.TryGetFloat("maxsaturation");
				float? num3 = saturation;
				float? num2 = num6;
				double? num4 = (((num3 != null) & (num2 != null)) ? new double?((double)(num3.GetValueOrDefault() / num2.GetValueOrDefault())) : null);
				double num5 = 0.2;
				if ((num4.GetValueOrDefault() < num5) & (num4 != null))
				{
					this.saturationbar.ShouldFlash = true;
				}
			}
			if (this.capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute("oxygen") != null)
			{
				float? oxygen = hungerTree.TryGetFloat("currentoxygen");
				float? num7 = hungerTree.TryGetFloat("maxoxygen");
				float? num2 = oxygen;
				float? num3 = num7;
				double? num4 = (((num2 != null) & (num3 != null)) ? new double?((double)(num2.GetValueOrDefault() / num3.GetValueOrDefault())) : null);
				double num5 = 0.2;
				if ((num4.GetValueOrDefault() < num5) & (num4 != null))
				{
					this.saturationbar.ShouldFlash = true;
				}
			}
		}

		private void UpdateHealth()
		{
			ITreeAttribute healthTree = this.capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute("health");
			if (healthTree == null)
			{
				return;
			}
			float? health = healthTree.TryGetFloat("currenthealth");
			float? futureHealth = healthTree.TryGetFloat("futureHealth");
			float previousHealthValue = healthTree.GetFloat("previousHealthValue", 0f);
			float healthChangeVelocity = healthTree.GetFloat("healthChangeVelocity", 0f);
			float? maxHealth = healthTree.TryGetFloat("maxhealth");
			if (health == null || maxHealth == null)
			{
				return;
			}
			float num = this.lastHealth;
			float? num2 = health;
			if ((num == num2.GetValueOrDefault()) & (num2 != null))
			{
				float num3 = this.lastMaxHealth;
				num2 = maxHealth;
				if (((num3 == num2.GetValueOrDefault()) & (num2 != null)) && previousHealthValue == this.lastPreviousHealthValue && this.lastHealthChangeVelocity == healthChangeVelocity)
				{
					num2 = this.lastFutureHealth;
					float? num4 = futureHealth;
					if ((num2.GetValueOrDefault() == num4.GetValueOrDefault()) & (num2 != null == (num4 != null)))
					{
						return;
					}
				}
			}
			if (this.healthbar == null)
			{
				return;
			}
			bool flag = (double)Math.Abs(this.lastPreviousHealthValue - previousHealthValue) > 0.01;
			float? prevHealthValueDisplay = new float?(previousHealthValue);
			if (flag)
			{
				if (this.capi.InWorldEllapsedMilliseconds - this.previousHealthHasChangedTotalMs < 2000L)
				{
					prevHealthValueDisplay = new float?(this.lastPreviousHealthValue);
				}
				this.previousHealthHasChangedTotalMs = this.capi.InWorldEllapsedMilliseconds;
			}
			this.healthbar.SetFutureValues(futureHealth, healthChangeVelocity);
			this.healthbar.SetPrevValue(prevHealthValueDisplay, this.previousHealthHasChangedTotalMs, () => this.capi.InWorldEllapsedMilliseconds);
			this.healthbar.SetLineInterval(1f);
			this.healthbar.SetValues(health.Value, 0f, maxHealth.Value);
			this.lastHealth = health.Value;
			this.lastMaxHealth = maxHealth.Value;
			this.lastFutureHealth = futureHealth;
			this.lastHealthChangeVelocity = healthChangeVelocity;
			this.lastPreviousHealthValue = previousHealthValue;
		}

		private void UpdateOxygen()
		{
			ITreeAttribute oxygenTree = this.capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute("oxygen");
			if (oxygenTree == null)
			{
				return;
			}
			float? oxygen = oxygenTree.TryGetFloat("currentoxygen");
			float? maxOxygen = oxygenTree.TryGetFloat("maxoxygen");
			if (oxygen == null || maxOxygen == null)
			{
				return;
			}
			float num = this.lastOxygen;
			float? num2 = oxygen;
			if ((num == num2.GetValueOrDefault()) & (num2 != null))
			{
				float num3 = this.lastMaxOxygen;
				num2 = maxOxygen;
				if ((num3 == num2.GetValueOrDefault()) & (num2 != null))
				{
					return;
				}
			}
			if (this.oxygenbar == null)
			{
				return;
			}
			this.oxygenbar.SetLineInterval(1000f);
			this.oxygenbar.SetValues(oxygen.Value, 0f, maxOxygen.Value);
			this.lastOxygen = oxygen.Value;
			this.lastMaxOxygen = maxOxygen.Value;
		}

		private void UpdateSaturation()
		{
			ITreeAttribute hungerTree = this.capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute("hunger");
			if (hungerTree == null)
			{
				return;
			}
			float? saturation = hungerTree.TryGetFloat("currentsaturation");
			float? maxSaturation = hungerTree.TryGetFloat("maxsaturation");
			if (saturation == null || maxSaturation == null)
			{
				return;
			}
			float num = this.lastSaturation;
			float? num2 = saturation;
			if ((num == num2.GetValueOrDefault()) & (num2 != null))
			{
				float num3 = this.lastMaxSaturation;
				num2 = maxSaturation;
				if ((num3 == num2.GetValueOrDefault()) & (num2 != null))
				{
					return;
				}
			}
			if (this.saturationbar == null)
			{
				return;
			}
			this.saturationbar.SetLineInterval(100f);
			this.saturationbar.SetValues(saturation.Value, 0f, maxSaturation.Value);
			this.lastSaturation = saturation.Value;
			this.lastMaxSaturation = maxSaturation.Value;
		}

		public override void OnOwnPlayerDataReceived()
		{
			this.ComposeGuis();
			this.UpdateSaturation();
		}

		public void ComposeGuis()
		{
			float width = 850f;
			ElementBounds dialogBounds = new ElementBounds
			{
				Alignment = EnumDialogArea.CenterBottom,
				BothSizing = ElementSizing.Fixed,
				fixedWidth = (double)width,
				fixedHeight = 100.0
			}.WithFixedAlignmentOffset(0.0, 5.0);
			ElementBounds healthBarBounds = ElementStdBounds.Statbar(EnumDialogArea.LeftTop, (double)width * 0.41).WithFixedHeight(10.0).WithFixedAlignmentOffset(0.0, 5.0);
			healthBarBounds.WithFixedHeight(10.0);
			ElementBounds oxygenBarBounds = ElementStdBounds.Statbar(EnumDialogArea.LeftTop, (double)width * 0.41).WithFixedHeight(10.0).WithFixedAlignmentOffset(0.0, -15.0);
			oxygenBarBounds.WithFixedHeight(10.0);
			ElementBounds foodBarBounds = ElementStdBounds.Statbar(EnumDialogArea.RightTop, (double)width * 0.41).WithFixedHeight(10.0).WithFixedAlignmentOffset(-1.0, 5.0);
			foodBarBounds.WithFixedHeight(10.0);
			ITreeAttribute hungerTree = this.capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute("hunger");
			ITreeAttribute healthTree = this.capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute("health");
			ITreeAttribute oxygenTree = this.capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute("oxygen");
			this.Composers["statbar"] = this.capi.Gui.CreateCompo("inventory-statbar", dialogBounds.FlatCopy().FixedGrow(0.0, 20.0)).BeginChildElements(dialogBounds).AddIf(healthTree != null)
				.AddStatbar(healthBarBounds, GuiStyle.HealthBarColor, "healthstatbar")
				.EndIf()
				.AddIf(oxygenTree != null)
				.AddStatbar(oxygenBarBounds, GuiStyle.OxygenBarColor, true, "oxygenstatbar")
				.EndIf()
				.AddIf(hungerTree != null)
				.AddInvStatbar(foodBarBounds, GuiStyle.FoodBarColor, "saturationstatbar")
				.EndIf()
				.EndChildElements()
				.Compose(true);
			this.healthbar = this.Composers["statbar"].GetStatbar("healthstatbar");
			this.oxygenbar = this.Composers["statbar"].GetStatbar("oxygenstatbar");
			this.oxygenbar.HideWhenFull = true;
			this.saturationbar = this.Composers["statbar"].GetStatbar("saturationstatbar");
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

		public override void OnRenderGUI(float deltaTime)
		{
			if (this.capi.World.Player.WorldData.CurrentGameMode != EnumGameMode.Spectator)
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

		public override void OnMouseDown(MouseEvent args)
		{
		}

		private float lastHealth;

		private float lastMaxHealth;

		private float lastOxygen;

		private float lastMaxOxygen;

		private float lastSaturation;

		private float lastMaxSaturation;

		private GuiElementStatbar healthbar;

		private GuiElementStatbar oxygenbar;

		private GuiElementStatbar saturationbar;

		private float lastPreviousHealthValue;

		private float? lastFutureHealth;

		private float lastHealthChangeVelocity;

		private long previousHealthHasChangedTotalMs;
	}
}
