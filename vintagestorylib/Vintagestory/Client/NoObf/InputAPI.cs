using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.Client.NoObf
{
	public class InputAPI : IInputAPI
	{
		public event OnEntityAction InWorldAction;

		public InputAPI(ClientMain game)
		{
			this.game = game;
		}

		public bool[] KeyboardKeyState
		{
			get
			{
				return this.game.KeyboardState;
			}
		}

		public int MouseX
		{
			get
			{
				return this.game.MouseCurrentX;
			}
		}

		public int MouseY
		{
			get
			{
				return this.game.MouseCurrentY;
			}
		}

		public void TriggerInWorldAction(EnumEntityAction action, bool on, ref EnumHandling handling)
		{
			if (this.InWorldAction == null)
			{
				return;
			}
			Delegate[] invocationList = this.InWorldAction.GetInvocationList();
			for (int i = 0; i < invocationList.Length; i++)
			{
				((OnEntityAction)invocationList[i])(action, on, ref handling);
				if (handling == EnumHandling.PreventSubsequent)
				{
					return;
				}
			}
		}

		public void TriggerOnMouseEnterSlot(ItemSlot slot)
		{
			ClientEventManager eventManager = this.game.eventManager;
			if (eventManager == null)
			{
				return;
			}
			eventManager.TriggerOnMouseEnterSlot(this.game, slot);
		}

		public void TriggerOnMouseLeaveSlot(ItemSlot itemSlot)
		{
			ClientEventManager eventManager = this.game.eventManager;
			if (eventManager == null)
			{
				return;
			}
			eventManager.TriggerOnMouseLeaveSlot(this.game, itemSlot);
		}

		public void TriggerOnMouseClickSlot(ItemSlot itemSlot)
		{
			using (List<GuiDialog>.Enumerator enumerator = this.game.LoadedGuis.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current.OnMouseClickSlot(itemSlot))
					{
						return;
					}
				}
			}
			for (int i = 0; i < this.game.clientSystems.Length; i++)
			{
				if (this.game.clientSystems[i].OnMouseClickSlot(itemSlot))
				{
					return;
				}
			}
		}

		public bool MouseWorldInteractAnyway
		{
			get
			{
				return this.game.mouseWorldInteractAnyway;
			}
			set
			{
				this.game.mouseWorldInteractAnyway = value;
			}
		}

		public void RegisterHotKey(string hotkeyCode, string name, GlKeys key, HotkeyType type = HotkeyType.CharacterControls, bool altPressed = false, bool ctrlPressed = false, bool shiftPressed = false)
		{
			ScreenManager.hotkeyManager.RegisterHotKey(hotkeyCode, name, key, type, altPressed, ctrlPressed, shiftPressed, false);
		}

		public void RegisterHotKeyFirst(string hotkeyCode, string name, GlKeys key, HotkeyType type = HotkeyType.CharacterControls, bool altPressed = false, bool ctrlPressed = false, bool shiftPressed = false)
		{
			ScreenManager.hotkeyManager.RegisterHotKey(hotkeyCode, name, key, type, altPressed, ctrlPressed, shiftPressed, true);
		}

		public void SetHotKeyHandler(string hotkeyCode, ActionConsumable<KeyCombination> handler)
		{
			ScreenManager.hotkeyManager.SetHotKeyHandler(hotkeyCode, handler, true);
		}

		public HotKey GetHotKeyByCode(string toggleKeyCombinationCode)
		{
			return ScreenManager.hotkeyManager.GetHotKeyByCode(toggleKeyCombinationCode);
		}

		public void AddHotkeyListener(OnHotKeyDelegate handler)
		{
			ScreenManager.hotkeyManager.AddHotkeyListener(handler);
		}

		public OrderedDictionary<string, HotKey> HotKeys
		{
			get
			{
				return ScreenManager.hotkeyManager.HotKeys;
			}
		}

		[Obsolete("This is the raw state of mouse button presses. It by-passes the hotkeys configuration system. In almost all situations InWorldMouseButton should be used instead")]
		public MouseButtonState MouseButton
		{
			get
			{
				return this.game.MouseStateRaw;
			}
		}

		public MouseButtonState InWorldMouseButton
		{
			get
			{
				return this.game.InWorldMouseState;
			}
		}

		public bool[] KeyboardKeyStateRaw
		{
			get
			{
				return this.game.KeyboardStateRaw;
			}
		}

		public bool MouseGrabbed
		{
			get
			{
				return this.game.MouseGrabbed;
			}
		}

		public float MouseYaw
		{
			get
			{
				return this.game.mouseYaw;
			}
			set
			{
				this.game.mouseYaw = value;
			}
		}

		public float MousePitch
		{
			get
			{
				return this.game.mousePitch;
			}
			set
			{
				this.game.mousePitch = value;
			}
		}

		public string ClipboardText
		{
			get
			{
				return this.game.Platform.XPlatInterface.GetClipboardText();
			}
			set
			{
				this.game.Platform.XPlatInterface.SetClipboardText(value);
			}
		}

		public bool IsHotKeyPressed(string hotKeyCode)
		{
			return this.IsHotKeyPressed(this.game.api.Input.GetHotKeyByCode(hotKeyCode));
		}

		public bool IsHotKeyPressed(HotKey hotKey)
		{
			bool flag = this.KeyboardKeyState[hotKey.CurrentMapping.KeyCode];
			bool sec = hotKey.CurrentMapping.SecondKeyCode == null || this.KeyboardKeyState[hotKey.CurrentMapping.SecondKeyCode.Value];
			bool alt = !hotKey.CurrentMapping.Alt || this.KeyboardKeyState[5];
			bool ctrl = !hotKey.CurrentMapping.Ctrl || this.KeyboardKeyState[3];
			bool shift = !hotKey.CurrentMapping.Shift || this.KeyboardKeyState[1];
			return flag && sec && alt && ctrl && shift;
		}

		private ClientMain game;
	}
}
