using System;
using Vintagestory.API.Client;

namespace Vintagestory.Client
{
	public class HotkeyCapturer
	{
		public bool BeginCapture()
		{
			this.WasCancelled = false;
			if (this.CapturingKeyComb != null)
			{
				return false;
			}
			this.CapturedKeyComb = null;
			this.CapturingKeyComb = new KeyCombination();
			this.controlKeyCode = null;
			this.altKeyCode = null;
			this.shiftKeyCode = null;
			this.otherKeyCode = null;
			this.secondotherKeyCode = null;
			ScreenManager.hotkeyManager.ShouldTriggerHotkeys = false;
			return true;
		}

		public bool IsCapturing()
		{
			return this.CapturingKeyComb != null;
		}

		public void EndCapture(bool wasCancelled = false)
		{
			this.controlKeyCode = null;
			this.altKeyCode = null;
			this.shiftKeyCode = null;
			this.otherKeyCode = null;
			this.secondotherKeyCode = null;
			this.CapturingKeyComb = null;
			this.WasCancelled = wasCancelled;
			ScreenManager.hotkeyManager.ShouldTriggerHotkeys = true;
		}

		public bool OnKeyDown(KeyEvent eventArgs)
		{
			if (this.CapturingKeyComb == null)
			{
				return false;
			}
			eventArgs.Handled = true;
			return this.HandleKeyCode(eventArgs.KeyCode);
		}

		public bool OnKeyUp(KeyEvent eventArgs, Action OnCaptureEnded)
		{
			if (this.CapturingKeyComb == null || (this.otherKeyCode == null && !this.IsShiftCtrlOrAlt(this.CapturingKeyComb.KeyCode)))
			{
				return false;
			}
			eventArgs.Handled = true;
			return this.HandleCaptureEnded(OnCaptureEnded);
		}

		public bool OnMouseDown(MouseEvent eventArgs)
		{
			if (this.CapturingKeyComb == null)
			{
				return false;
			}
			eventArgs.Handled = true;
			return this.HandleKeyCode((int)(eventArgs.Button + 240));
		}

		public bool OnMouseUp(MouseEvent eventArgs, Action OnCaptureEnded)
		{
			if (this.CapturingKeyComb == null || this.otherKeyCode == null)
			{
				return false;
			}
			eventArgs.Handled = true;
			return this.HandleCaptureEnded(OnCaptureEnded);
		}

		private bool IsShiftCtrlOrAlt(int keyCode)
		{
			return keyCode == 3 || keyCode == 4 || (keyCode == 5 || keyCode == 6) || (keyCode == 1 || keyCode == 2);
		}

		private bool HandleKeyCode(int keyCode)
		{
			if (keyCode == 50)
			{
				this.EndCapture(false);
				this.WasCancelled = true;
				return true;
			}
			if (keyCode == 3 || keyCode == 4)
			{
				this.controlKeyCode = new int?(keyCode);
				this.InterpretKeyPresses();
				return true;
			}
			if (keyCode == 5 || keyCode == 6)
			{
				this.altKeyCode = new int?(keyCode);
				this.InterpretKeyPresses();
				return true;
			}
			if (keyCode == 1 || keyCode == 2)
			{
				this.shiftKeyCode = new int?(keyCode);
				this.InterpretKeyPresses();
				return true;
			}
			if (this.otherKeyCode == null)
			{
				this.otherKeyCode = new int?(keyCode);
			}
			else
			{
				int? num = this.otherKeyCode;
				if (!((keyCode == num.GetValueOrDefault()) & (num != null)))
				{
					return true;
				}
				this.secondotherKeyCode = new int?(keyCode);
			}
			this.InterpretKeyPresses();
			return true;
		}

		private bool HandleCaptureEnded(Action OnCaptureEnded)
		{
			this.InterpretKeyPresses();
			if (this.secondotherKeyCode != null)
			{
				this.CapturedKeyComb = this.CapturingKeyComb.Clone();
				this.EndCapture(false);
				OnCaptureEnded();
				this.lastKeyUpMs = ScreenManager.Platform.EllapsedMs;
				return true;
			}
			this.lastKeyUpMs = ScreenManager.Platform.EllapsedMs;
			ScreenManager.EnqueueMainThreadTask(delegate
			{
				this.TryEndCapture(OnCaptureEnded);
			});
			return true;
		}

		private void TryEndCapture(Action OnCaptureEnded)
		{
			if (!this.IsCapturing())
			{
				return;
			}
			if (ScreenManager.Platform.EllapsedMs - this.lastKeyUpMs > 150L)
			{
				this.CapturedKeyComb = this.CapturingKeyComb.Clone();
				this.EndCapture(false);
				OnCaptureEnded();
				this.lastKeyUpMs = ScreenManager.Platform.EllapsedMs;
				return;
			}
			ScreenManager.EnqueueMainThreadTask(delegate
			{
				this.TryEndCapture(OnCaptureEnded);
			});
		}

		private void InterpretKeyPresses()
		{
			if (this.otherKeyCode != null)
			{
				this.CapturingKeyComb.KeyCode = this.otherKeyCode.Value;
				this.CapturingKeyComb.SecondKeyCode = this.secondotherKeyCode;
				this.CapturingKeyComb.Ctrl = this.controlKeyCode != null;
				this.CapturingKeyComb.Alt = this.altKeyCode != null;
				this.CapturingKeyComb.Shift = this.shiftKeyCode != null;
				return;
			}
			if (this.shiftKeyCode != null)
			{
				this.CapturingKeyComb.Ctrl = this.controlKeyCode != null;
				this.CapturingKeyComb.Alt = this.altKeyCode != null;
				this.CapturingKeyComb.Shift = false;
				this.CapturingKeyComb.KeyCode = this.shiftKeyCode.Value;
				return;
			}
			if (this.altKeyCode != null)
			{
				this.CapturingKeyComb.Ctrl = this.controlKeyCode != null;
				this.CapturingKeyComb.Alt = false;
				this.CapturingKeyComb.Shift = false;
				this.CapturingKeyComb.KeyCode = this.altKeyCode.Value;
				return;
			}
			if (this.controlKeyCode != null)
			{
				this.CapturingKeyComb.Ctrl = false;
				this.CapturingKeyComb.Alt = false;
				this.CapturingKeyComb.Shift = false;
				this.CapturingKeyComb.KeyCode = this.controlKeyCode.Value;
				return;
			}
		}

		public bool WasCancelled;

		public KeyCombination CapturedKeyComb;

		internal KeyCombination CapturingKeyComb;

		private int? controlKeyCode;

		private int? altKeyCode;

		private int? shiftKeyCode;

		private int? otherKeyCode;

		private int? secondotherKeyCode;

		private long lastKeyUpMs;
	}
}
