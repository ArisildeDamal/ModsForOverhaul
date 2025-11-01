using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Vintagestory.Client.NoObf
{
	public class SystemPlayerControl : ClientSystem
	{
		public override string Name
		{
			get
			{
				return "plco";
			}
		}

		public SystemPlayerControl(ClientMain game)
			: base(game)
		{
			this.game.RegisterGameTickListener(new Action<float>(this.OnGameTick), 20, 0);
			ClientSettings.Inst.AddKeyCombinationUpdatedWatcher(delegate(string code, KeyCombination keymap)
			{
				this.LoadKeyCodes();
			});
			this.LoadKeyCodes();
			this.prevControls = new EntityControls();
		}

		private void LoadKeyCodes()
		{
			this.forwardKey = ScreenManager.hotkeyManager.HotKeys["walkforward"].CurrentMapping.KeyCode;
			this.backwardKey = ScreenManager.hotkeyManager.HotKeys["walkbackward"].CurrentMapping.KeyCode;
			this.leftKey = ScreenManager.hotkeyManager.HotKeys["walkleft"].CurrentMapping.KeyCode;
			this.rightKey = ScreenManager.hotkeyManager.HotKeys["walkright"].CurrentMapping.KeyCode;
			this.sneakKey = ScreenManager.hotkeyManager.HotKeys["sneak"].CurrentMapping.KeyCode;
			this.sprintKey = ScreenManager.hotkeyManager.HotKeys["sprint"].CurrentMapping.KeyCode;
			this.jumpKey = ScreenManager.hotkeyManager.HotKeys["jump"].CurrentMapping.KeyCode;
			this.sittingKey = ScreenManager.hotkeyManager.HotKeys["sitdown"].CurrentMapping.KeyCode;
			this.ctrlKey = ScreenManager.hotkeyManager.HotKeys["ctrl"].CurrentMapping.KeyCode;
			this.shiftKey = ScreenManager.hotkeyManager.HotKeys["shift"].CurrentMapping.KeyCode;
		}

		public override void OnKeyDown(KeyEvent args)
		{
			EntityPlayer entity = this.game.EntityPlayer;
			if (args.KeyCode == this.sittingKey && !entity.Controls.TriesToMove && !entity.Controls.IsFlying)
			{
				this.nowFloorSitting = !entity.Controls.FloorSitting;
			}
			if (args.KeyCode == this.jumpKey || args.KeyCode == this.forwardKey || args.KeyCode == this.backwardKey || args.KeyCode == this.leftKey || args.KeyCode == this.rightKey)
			{
				this.nowFloorSitting = false;
			}
		}

		public void OnGameTick(float dt)
		{
			EntityControls controls = ((this.game.EntityPlayer.MountedOn == null) ? this.game.EntityPlayer.Controls : this.game.EntityPlayer.MountedOn.Controls);
			if (controls == null)
			{
				return;
			}
			this.game.EntityPlayer.Controls.OnAction = new OnEntityAction(this.game.api.inputapi.TriggerInWorldAction);
			bool flag;
			if (!this.game.MouseGrabbed)
			{
				if (this.game.api.Settings.Bool["immersiveMouseMode"])
				{
					flag = this.game.OpenedGuis.All((GuiDialog gui) => !gui.PrefersUngrabbedMouse);
				}
				else
				{
					flag = false;
				}
			}
			else
			{
				flag = true;
			}
			bool allMovementCaptured = flag;
			controls.Forward = this.game.KeyboardState[this.forwardKey];
			controls.Backward = this.game.KeyboardState[this.backwardKey];
			controls.Left = this.game.KeyboardState[this.leftKey];
			controls.Right = this.game.KeyboardState[this.rightKey];
			controls.Jump = this.game.KeyboardState[this.jumpKey] && allMovementCaptured && (this.game.EntityPlayer.PrevFrameCanStandUp || this.game.player.worlddata.NoClip);
			controls.Sneak = this.game.KeyboardState[this.sneakKey] && allMovementCaptured;
			bool wasSprint = controls.Sprint;
			controls.Sprint = (this.game.KeyboardState[this.sprintKey] || (wasSprint && controls.TriesToMove && ClientSettings.ToggleSprint)) && allMovementCaptured;
			controls.CtrlKey = this.game.KeyboardState[this.ctrlKey];
			controls.ShiftKey = this.game.KeyboardState[this.shiftKey];
			controls.DetachedMode = this.game.player.worlddata.FreeMove || this.game.EntityPlayer.IsEyesSubmerged();
			controls.FlyPlaneLock = this.game.player.worlddata.FreeMovePlaneLock;
			controls.Up = controls.DetachedMode && controls.Jump;
			controls.Down = controls.DetachedMode && controls.Sneak;
			controls.MovespeedMultiplier = this.game.player.worlddata.MoveSpeedMultiplier;
			controls.IsFlying = this.game.player.worlddata.FreeMove;
			controls.NoClip = this.game.player.worlddata.NoClip;
			controls.LeftMouseDown = this.game.InWorldMouseState.Left;
			controls.RightMouseDown = this.game.InWorldMouseState.Right;
			controls.FloorSitting = this.nowFloorSitting;
			this.SendServerPackets(this.prevControls, controls);
		}

		private void SendServerPackets(EntityControls before, EntityControls now)
		{
			for (int i = 0; i < before.Flags.Length; i++)
			{
				if (before.Flags[i] != now.Flags[i])
				{
					this.game.SendPacketClient(new Packet_Client
					{
						Id = 21,
						MoveKeyChange = new Packet_MoveKeyChange
						{
							Down = ((now.Flags[i] > false) ? 1 : 0),
							Key = i
						}
					});
					before.Flags[i] = now.Flags[i];
				}
			}
		}

		public override EnumClientSystemType GetSystemType()
		{
			return EnumClientSystemType.Misc;
		}

		private int forwardKey;

		private int backwardKey;

		private int leftKey;

		private int rightKey;

		private int jumpKey;

		private int sneakKey;

		private int sprintKey;

		private int sittingKey;

		private int ctrlKey;

		private int shiftKey;

		private bool nowFloorSitting;

		private EntityControls prevControls;
	}
}
