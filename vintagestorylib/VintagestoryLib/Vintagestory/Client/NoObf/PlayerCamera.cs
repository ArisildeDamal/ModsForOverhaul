using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class PlayerCamera : Camera
	{
		public PlayerCamera(ClientMain game)
		{
			this.game = game;
			HotkeyManager hotkeyManager = ScreenManager.hotkeyManager;
			hotkeyManager.SetHotKeyHandler("zoomout", new ActionConsumable<KeyCombination>(this.KeyZoomOut), true);
			hotkeyManager.SetHotKeyHandler("zoomin", new ActionConsumable<KeyCombination>(this.KeyZoomIn), true);
			hotkeyManager.SetHotKeyHandler("cyclecamera", new ActionConsumable<KeyCombination>(this.KeyCycleCameraModes), true);
			game.eventManager.RegisterRenderer(new Action<float>(this.OnBeforeRenderFrame3D), EnumRenderStage.Before, "camera", 0.0);
			this.targetCameraDistance = this.Tppcameradistance;
		}

		public void OnPlayerPhysicsTick(float nextAccum, Vec3d prevPos)
		{
			this.physicsUnsimulatedSeconds = (double)nextAccum;
			this.prevPos.Set(prevPos);
		}

		public void OnBeforeRenderFrame3D(float dt)
		{
			this.Tppcameradistance = GameMath.Clamp(this.Tppcameradistance + (this.targetCameraDistance - this.Tppcameradistance) * dt * 5f, (float)this.TppCameraDistanceMin, (float)this.TppCameraDistanceMax);
			if (this.game.IsPaused)
			{
				return;
			}
			this.game.EntityPlayer.OnSelfBeforeRender(dt);
			base.Yaw = (double)this.game.mouseYaw;
			base.Pitch = (double)this.game.mousePitch;
			this.PlayerHeight = (double)this.game.EntityPlayer.SelectionBox.Y2;
			this.CameraShakeStrength = Math.Max(0f, this.CameraShakeStrength - dt);
			this.deltaSum += (double)dt;
			this.deltaSum = GameMath.Mod(this.deltaSum, 6.283185307179586);
			float physicsStepTime = 0.016666668f;
			double alpha = this.physicsUnsimulatedSeconds / (double)physicsStepTime;
			this.physicsUnsimulatedSeconds += (double)dt;
			Vec3d camPos = this.game.EntityPlayer.CameraPos;
			this.curPos.Set(this.game.EntityPlayer.Pos.X, this.game.EntityPlayer.Pos.Y, this.game.EntityPlayer.Pos.Z);
			if (this.UpdateCameraPos)
			{
				Vec3d offset = this.game.EntityPlayer.CameraPosOffset;
				camPos.Set(this.prevPos.X + (this.curPos.X - this.prevPos.X) * alpha + (double)this.CameraShakeStrength * GameMath.Cos(this.deltaSum * 100.0) / 10.0 + offset.X, this.prevPos.Y + (this.curPos.Y - this.prevPos.Y) * alpha - (double)this.CameraShakeStrength * GameMath.Cos(this.deltaSum * 100.0) / 10.0 + offset.Y, this.prevPos.Z + (this.curPos.Z - this.prevPos.Z) * alpha + (double)this.CameraShakeStrength * GameMath.Sin(this.deltaSum * 100.0) / 10.0 + offset.Z);
			}
			camPos.Y += (double)this.game.EntityPlayer.Pos.DimensionYAdjustment;
			base.CamSourcePosition.Set(camPos.X, camPos.Y, camPos.Z);
			base.OriginPosition.Set(0.0, 0.0, 0.0);
			base.Update(dt, this.game.interesectionTester);
			Vec3d pos = camPos;
			if (this.game.shUniforms.playerReferencePos == null)
			{
				this.game.shUniforms.playerReferencePos = new Vec3d((double)(this.game.BlockAccessor.MapSizeX / 2), 0.0, (double)(this.game.BlockAccessor.MapSizeZ / 2));
				this.game.shUniforms.playerReferencePosForFoam = new Vec3d((double)(this.game.BlockAccessor.MapSizeX / 2), 0.0, (double)(this.game.BlockAccessor.MapSizeZ / 2));
			}
			if ((double)this.game.shUniforms.playerReferencePos.HorizontalSquareDistanceTo(pos.X, pos.Z) > 400000000.0)
			{
				this.game.shUniforms.playerReferencePos.Set((double)((float)pos.X), 0.0, (double)((float)pos.Z));
			}
			if ((double)this.game.shUniforms.playerReferencePosForFoam.HorizontalSquareDistanceTo(pos.X, pos.Z) > 40000.0)
			{
				this.game.shUniforms.playerReferencePosForFoam.Set((double)((float)pos.X), 0.0, (double)((float)pos.Z));
			}
			this.game.shUniforms.PlayerPos.Set(pos.SubCopy(this.game.shUniforms.playerReferencePos));
			this.game.shUniforms.PlayerPosForFoam.Set(pos.SubCopy(this.game.shUniforms.playerReferencePosForFoam));
		}

		internal override void SetMode(EnumCameraMode mode)
		{
			this.CameraMode = mode;
		}

		public void CycleMode()
		{
			if (this.CameraMode == EnumCameraMode.FirstPerson)
			{
				this.CameraMode = EnumCameraMode.ThirdPerson;
				return;
			}
			if (this.CameraMode == EnumCameraMode.ThirdPerson)
			{
				this.CameraMode = EnumCameraMode.Overhead;
				if (!this.game.EntityPlayer.Controls.TriesToMove)
				{
					this.game.EntityPlayer.WalkYaw = this.game.EntityPlayer.Pos.Yaw;
					return;
				}
			}
			else
			{
				this.CameraMode = EnumCameraMode.FirstPerson;
			}
		}

		private bool KeyZoomOut(KeyCombination viaKeyComb)
		{
			if (this.CameraMode != EnumCameraMode.FirstPerson)
			{
				this.targetCameraDistance += (float)(this.game.api.inputapi.KeyboardKeyState[3] ? 10 : 1);
			}
			this.targetCameraDistance = GameMath.Clamp(this.targetCameraDistance, (float)this.TppCameraDistanceMin, (float)this.TppCameraDistanceMax);
			return true;
		}

		private bool KeyZoomIn(KeyCombination viaKeyComb)
		{
			if (this.CameraMode != EnumCameraMode.FirstPerson)
			{
				this.targetCameraDistance -= (float)(this.game.api.inputapi.KeyboardKeyState[3] ? 10 : 1);
			}
			this.targetCameraDistance = GameMath.Clamp(this.targetCameraDistance, (float)this.TppCameraDistanceMin, (float)this.TppCameraDistanceMax);
			return true;
		}

		private bool KeyCycleCameraModes(KeyCombination viaKeyComb)
		{
			this.CycleMode();
			return true;
		}

		private ClientMain game;

		public float CameraShakeStrength;

		public bool UpdateCameraPos = true;

		private double physicsUnsimulatedSeconds;

		private Vec3d prevPos = new Vec3d();

		private Vec3d curPos = new Vec3d();

		private float targetCameraDistance;

		public double deltaSum;
	}
}
