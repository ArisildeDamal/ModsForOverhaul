using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Vintagestory.Client.NoObf
{
	public class GuiDialogDead : GuiDialog
	{
		public override string ToggleKeyCombinationCode
		{
			get
			{
				return null;
			}
		}

		public GuiDialogDead(ICoreClientAPI capi)
			: base(capi)
		{
			this.game = capi.World as ClientMain;
			this.game.RegisterGameTickListener(new Action<float>(this.OnGameTick), 250, 0);
		}

		private void OnGameTick(float dt)
		{
			if (!this.game.EntityPlayer.Alive)
			{
				this.secondsDead += dt;
			}
			else
			{
				this.secondsDead = 0f;
			}
			if (this.secondsDead >= 2.5f && !this.game.EntityPlayer.Alive && !this.IsOpened())
			{
				int lives = this.game.Config.GetString("playerlives", "-1").ToInt(-1);
				this.livesLeft = lives - this.game.Player.WorldData.Deaths;
				this.ComposeDialog();
				this.TryOpen();
			}
			if (this.secondsDead > 0f)
			{
				this.ingameMinutesRevivableLeft = this.game.player.Entity.RevivableIngameHoursLeft() * 60.0;
				if (this.prevIngMinLeftInt != (int)this.ingameMinutesRevivableLeft && this.ingameMinutesRevivableLeft >= 0.0 && this.Composers["menu"] != null)
				{
					GuiElementDynamicText ele = this.Composers["menu"].GetDynamicText("reviveCountdown");
					if (this.ingameMinutesRevivableLeft <= 0.0)
					{
						if (ele != null)
						{
							ele.SetNewText("", false, false, false);
						}
					}
					else if (ele != null)
					{
						ele.SetNewText(Lang.Get("playerrevival-remainingtime", new object[] { (int)this.ingameMinutesRevivableLeft }), false, false, false);
					}
				}
				this.prevIngMinLeftInt = (int)this.ingameMinutesRevivableLeft;
			}
			if (this.IsOpened() && this.game.EntityPlayer.Alive)
			{
				this.respawning = false;
				this.TryClose();
			}
		}

		private void ComposeDialog()
		{
			base.ClearComposers();
			this.Composers["backgroundd"] = this.game.GuiComposers.Create("deadbg", ElementBounds.Fill).AddGrayBG(ElementBounds.Fill).Compose(true);
			string deadMsg = Lang.Get("Congratulations, you died!", Array.Empty<object>());
			if (this.livesLeft > 0)
			{
				deadMsg = Lang.Get("Congratulations, you died! {0} lives left.", new object[] { this.livesLeft });
			}
			if (this.livesLeft == 0)
			{
				deadMsg = Lang.Get("Congratulations, you died! Forever!", Array.Empty<object>());
			}
			string respText = (this.respawning ? Lang.Get("Respawning...", Array.Empty<object>()) : Lang.Get("Respawn", Array.Empty<object>()));
			CairoFont reviveFont = CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center);
			double buttonWidth = 300.0;
			this.Composers["menu"] = this.game.GuiComposers.Create("deadmenu", ElementStdBounds.AutosizedMainDialog.WithFixedAlignmentOffset(0.0, 40.0)).AddShadedDialogBG(ElementStdBounds.DialogBackground().WithFixedPadding(0.0, GuiStyle.ElementToDialogPadding), false, 5.0, 0.75f).BeginChildElements()
				.AddStaticText(deadMsg, CairoFont.WhiteSmallishText(), EnumTextOrientation.Center, ElementStdBounds.MenuButton(0f, EnumDialogArea.CenterFixed).WithFixedWidth(350.0), null)
				.AddDynamicText("", reviveFont, ElementStdBounds.MenuButton(0.35f, EnumDialogArea.CenterFixed).WithFixedSize(350.0, 25.0), "reviveCountdown")
				.AddIf(this.livesLeft != 0)
				.AddButton(respText, new ActionConsumable(this.OnRespawn), ElementStdBounds.MenuButton(1f, EnumDialogArea.CenterFixed).WithFixedWidth(buttonWidth), EnumButtonStyle.Normal, "respawnbtn")
				.EndIf()
				.AddIf(this.livesLeft == 0 && this.game.IsSingleplayer)
				.AddButton(Lang.Get("Delete World", Array.Empty<object>()), new ActionConsumable(this.OnDeleteWorld), ElementStdBounds.MenuButton(1f, EnumDialogArea.CenterFixed).WithFixedWidth(buttonWidth), EnumButtonStyle.Normal, "deletebtn")
				.EndIf()
				.AddButton(Lang.Get("Rage Quit", Array.Empty<object>()), new ActionConsumable(this.OnLeaveWorld), ElementStdBounds.MenuButton(2f, EnumDialogArea.CenterFixed).WithFixedWidth(buttonWidth), EnumButtonStyle.Normal, null)
				.EndChildElements()
				.Compose(true);
			if (this.Composers["menu"].GetButton("respawnbtn") != null)
			{
				this.Composers["menu"].GetButton("respawnbtn").Enabled = !this.respawning;
			}
			if (!this.respawning && (!this.game.IsSingleplayer || this.game.OpenedToLan))
			{
				this.ingameMinutesRevivableLeft = this.game.player.Entity.RevivableIngameHoursLeft() * 60.0;
				this.prevIngMinLeftInt = (int)this.ingameMinutesRevivableLeft;
				GuiElementDynamicText dynamicText = this.Composers["menu"].GetDynamicText("reviveCountdown");
				if (dynamicText == null)
				{
					return;
				}
				dynamicText.SetNewText(Lang.Get("{0} ingame minutes left for player revival", new object[] { (int)this.ingameMinutesRevivableLeft }), false, false, false);
			}
		}

		private bool OnDeleteWorld()
		{
			this.game.SendLeave(0);
			this.game.exitReason = "delete world button pressed";
			this.game.deleteWorld = true;
			this.game.DestroyGameSession(false);
			return true;
		}

		private bool OnLeaveWorld()
		{
			this.game.SendLeave(0);
			this.game.exitReason = "rage quit button pressed";
			this.game.DestroyGameSession(false);
			return true;
		}

		private bool OnRespawn()
		{
			this.respawning = true;
			this.ComposeDialog();
			this.game.Respawn();
			return true;
		}

		public override bool CaptureAllInputs()
		{
			return this.IsOpened();
		}

		public override void OnGuiOpened()
		{
			base.OnGuiOpened();
			if (this.Composers["menu"].GetButton("respawnbtn") != null)
			{
				this.Composers["menu"].GetButton("respawnbtn").Enabled = true;
			}
			this.game.ShouldRender2DOverlays = true;
		}

		public override bool TryClose()
		{
			return this.game.EntityPlayer.Alive && base.TryClose();
		}

		private ClientMain game;

		private bool respawning;

		private int livesLeft = -1;

		private float secondsDead;

		private double ingameMinutesRevivableLeft;

		private int prevIngMinLeftInt;
	}
}
