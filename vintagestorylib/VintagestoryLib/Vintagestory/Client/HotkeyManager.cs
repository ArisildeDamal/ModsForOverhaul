using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.Client.NoObf;

namespace Vintagestory.Client
{
	public class HotkeyManager
	{
		private event OnHotKeyDelegate listeners;

		public virtual void RegisterDefaultHotKeys()
		{
			this.HotKeys.Clear();
			this.RegisterHotKey("primarymouse", Lang.Get("Primary mouse button", Array.Empty<object>()), EnumMouseButton.Left, HotkeyType.MouseControls, false, false, false, false);
			this.RegisterHotKey("secondarymouse", Lang.Get("Second mouse button", Array.Empty<object>()), EnumMouseButton.Right, HotkeyType.MouseControls, false, false, false, false);
			this.RegisterHotKey("middlemouse", Lang.Get("Middle mouse button", Array.Empty<object>()), EnumMouseButton.Middle, HotkeyType.MouseControls, false, false, false, false);
			this.RegisterHotKey("walkforward", Lang.Get("Walk forward", Array.Empty<object>()), GlKeys.W, HotkeyType.MovementControls, false, false, false, false);
			this.RegisterHotKey("walkbackward", Lang.Get("Walk backward", Array.Empty<object>()), GlKeys.S, HotkeyType.MovementControls, false, false, false, false);
			this.RegisterHotKey("walkleft", Lang.Get("Walk left", Array.Empty<object>()), GlKeys.A, HotkeyType.MovementControls, false, false, false, false);
			this.RegisterHotKey("walkright", Lang.Get("Walk right", Array.Empty<object>()), GlKeys.D, HotkeyType.MovementControls, false, false, false, false);
			this.RegisterHotKey("sneak", Lang.Get("Sneak", Array.Empty<object>()), GlKeys.LShift, HotkeyType.MovementControls, false, false, false, false);
			this.RegisterHotKey("sprint", Lang.Get("Sprint", Array.Empty<object>()), GlKeys.LControl, HotkeyType.MovementControls, false, false, false, false);
			this.RegisterHotKey("shift", Lang.Get("Shift-click", Array.Empty<object>()), GlKeys.LShift, HotkeyType.MouseModifiers, false, false, false, false);
			this.RegisterHotKey("ctrl", Lang.Get("Ctrl-click", Array.Empty<object>()), GlKeys.LControl, HotkeyType.MouseModifiers, false, false, false, false);
			this.RegisterHotKey("jump", Lang.Get("Jump", Array.Empty<object>()), GlKeys.Space, HotkeyType.MovementControls, false, false, false, false);
			this.RegisterHotKey("sitdown", Lang.Get("Sit down", Array.Empty<object>()), GlKeys.G, HotkeyType.MovementControls, false, false, false, false);
			this.RegisterHotKey("inventorydialog", Lang.Get("Open Inventory", Array.Empty<object>()), GlKeys.E, HotkeyType.CharacterControls, false, false, false, false);
			this.RegisterHotKey("characterdialog", Lang.Get("Open character Inventory", Array.Empty<object>()), GlKeys.C, HotkeyType.CharacterControls, false, false, false, false);
			this.RegisterHotKey("dropitem", Lang.Get("Drop one item", Array.Empty<object>()), GlKeys.Q, HotkeyType.CharacterControls, false, false, false, false);
			this.RegisterHotKey("dropitems", Lang.Get("Drop all items", Array.Empty<object>()), GlKeys.Q, HotkeyType.CharacterControls, false, true, false, false);
			this.RegisterHotKey("toolmodeselect", Lang.Get("Select Tool Mode", Array.Empty<object>()), GlKeys.F, HotkeyType.CharacterControls, false, false, false, false);
			this.RegisterHotKey("coordinateshud", Lang.Get("Show/Hide distance to spawn", Array.Empty<object>()), GlKeys.V, HotkeyType.HelpAndOverlays, false, true, false, false);
			this.RegisterHotKey("blockinfohud", Lang.Get("Show/Hide block and entity info overlay", Array.Empty<object>()), GlKeys.B, HotkeyType.HelpAndOverlays, false, true, false, false);
			this.RegisterHotKey("blockinteractionhelp", Lang.Get("Show/Hide block and entity interaction info overlay", Array.Empty<object>()), GlKeys.N, HotkeyType.HelpAndOverlays, false, true, false, false);
			this.RegisterHotKey("escapemenudialog", Lang.Get("Show/Hide escape menu dialog", Array.Empty<object>()), GlKeys.Escape, HotkeyType.GUIOrOtherControls, false, false, false, false);
			this.RegisterHotKey("togglehud", Lang.Get("Hide/Show HUD", Array.Empty<object>()), GlKeys.F4, HotkeyType.GUIOrOtherControls, false, false, false, false);
			this.RegisterHotKey("cyclecamera", Lang.Get("First-, Third-person or Overhead camera", Array.Empty<object>()), GlKeys.F5, HotkeyType.GUIOrOtherControls, false, false, false, false);
			this.RegisterHotKey("zoomout", Lang.Get("3rd Person Camera: Zoom out", Array.Empty<object>()), GlKeys.Minus, HotkeyType.GUIOrOtherControls, false, false, false, false);
			this.RegisterHotKey("zoomin", Lang.Get("3rd Person Camera: Zoom in", Array.Empty<object>()), GlKeys.Plus, HotkeyType.GUIOrOtherControls, false, false, false, false);
			this.RegisterHotKey("togglemousecontrol", Lang.Get("Lock/Unlock Mouse Cursor", Array.Empty<object>()), GlKeys.AltLeft, HotkeyType.GUIOrOtherControls, false, false, false, false);
			this.RegisterHotKey("beginchat", Lang.Get("Chat: Begin Typing", Array.Empty<object>()), GlKeys.T, HotkeyType.GUIOrOtherControls, false, false, false, false);
			this.RegisterHotKey("beginclientcommand", Lang.Get("Chat: Begin Typing a client command", Array.Empty<object>()), GlKeys.Period, HotkeyType.GUIOrOtherControls, false, false, false, false);
			this.RegisterHotKey("beginservercommand", Lang.Get("Chat: Begin Typing a server command", Array.Empty<object>()), GlKeys.Slash, HotkeyType.GUIOrOtherControls, false, false, false, false);
			this.RegisterHotKey("chatdialog", Lang.Get("Chat: Show/Hide chat dialog", Array.Empty<object>()), GlKeys.Tab, HotkeyType.GUIOrOtherControls, false, false, false, false);
			this.RegisterHotKey("macroeditor", Lang.Get("Open Macro Editor", Array.Empty<object>()), GlKeys.M, HotkeyType.GUIOrOtherControls, false, true, false, false);
			this.RegisterHotKey("togglefullscreen", Lang.Get("Toggle Fullscreen mode", Array.Empty<object>()), GlKeys.F11, HotkeyType.GUIOrOtherControls, false, false, false, false);
			this.RegisterHotKey("screenshot", Lang.Get("Take screenshot", Array.Empty<object>()), GlKeys.F12, HotkeyType.GUIOrOtherControls, false, false, false, false);
			this.RegisterHotKey("megascreenshot", Lang.Get("Take mega screenshot", Array.Empty<object>()), GlKeys.F12, HotkeyType.GUIOrOtherControls, false, true, false, false);
			this.RegisterHotKey("fliphandslots", Lang.Get("Flip left/right hand contents", Array.Empty<object>()), GlKeys.X, HotkeyType.InventoryHotkeys, false, false, false, false);
			this.RegisterHotKey("hotbarslot1", Lang.Get("Select Hotbar Slot {0}", new object[] { 1 }), GlKeys.Number1, HotkeyType.InventoryHotkeys, false, false, false, false);
			this.RegisterHotKey("hotbarslot2", Lang.Get("Select Hotbar Slot {0}", new object[] { 2 }), GlKeys.Number2, HotkeyType.InventoryHotkeys, false, false, false, false);
			this.RegisterHotKey("hotbarslot3", Lang.Get("Select Hotbar Slot {0}", new object[] { 3 }), GlKeys.Number3, HotkeyType.InventoryHotkeys, false, false, false, false);
			this.RegisterHotKey("hotbarslot4", Lang.Get("Select Hotbar Slot {0}", new object[] { 4 }), GlKeys.Number4, HotkeyType.InventoryHotkeys, false, false, false, false);
			this.RegisterHotKey("hotbarslot5", Lang.Get("Select Hotbar Slot {0}", new object[] { 5 }), GlKeys.Number5, HotkeyType.InventoryHotkeys, false, false, false, false);
			this.RegisterHotKey("hotbarslot6", Lang.Get("Select Hotbar Slot {0}", new object[] { 6 }), GlKeys.Number6, HotkeyType.InventoryHotkeys, false, false, false, false);
			this.RegisterHotKey("hotbarslot7", Lang.Get("Select Hotbar Slot {0}", new object[] { 7 }), GlKeys.Number7, HotkeyType.InventoryHotkeys, false, false, false, false);
			this.RegisterHotKey("hotbarslot8", Lang.Get("Select Hotbar Slot {0}", new object[] { 8 }), GlKeys.Number8, HotkeyType.InventoryHotkeys, false, false, false, false);
			this.RegisterHotKey("hotbarslot9", Lang.Get("Select Hotbar Slot {0}", new object[] { 9 }), GlKeys.Number9, HotkeyType.InventoryHotkeys, false, false, false, false);
			this.RegisterHotKey("hotbarslot10", Lang.Get("Select Hotbar Slot {0}", new object[] { 10 }), GlKeys.Number0, HotkeyType.InventoryHotkeys, false, false, false, false);
			this.RegisterHotKey("hotbarslot11", Lang.Get("Select Backpack Slot {0}", new object[] { 1 }), GlKeys.Number1, HotkeyType.InventoryHotkeys, false, true, false, false);
			this.RegisterHotKey("hotbarslot12", Lang.Get("Select Backpack Slot {0}", new object[] { 2 }), GlKeys.Number2, HotkeyType.InventoryHotkeys, false, true, false, false);
			this.RegisterHotKey("hotbarslot13", Lang.Get("Select Backpack Slot {0}", new object[] { 3 }), GlKeys.Number3, HotkeyType.InventoryHotkeys, false, true, false, false);
			this.RegisterHotKey("hotbarslot14", Lang.Get("Select Backpack Slot {0}", new object[] { 4 }), GlKeys.Number4, HotkeyType.InventoryHotkeys, false, true, false, false);
			this.RegisterHotKey("decspeed", Lang.Get("-1 Fly/Move Speed", Array.Empty<object>()), GlKeys.F1, HotkeyType.CreativeOrSpectatorTool, false, false, false, false);
			this.RegisterHotKey("incspeed", Lang.Get("+1 Fly/Move Speed", Array.Empty<object>()), GlKeys.F2, HotkeyType.CreativeOrSpectatorTool, false, false, false, false);
			this.RegisterHotKey("decspeedfrac", Lang.Get("-0.1 Fly/Move Speed", Array.Empty<object>()), GlKeys.F1, HotkeyType.CreativeOrSpectatorTool, false, false, true, false);
			this.RegisterHotKey("incspeedfrac", Lang.Get("+0.1 Fly/Move Speed", Array.Empty<object>()), GlKeys.F2, HotkeyType.CreativeOrSpectatorTool, false, false, true, false);
			this.RegisterHotKey("cycleflymodes", Lang.Get("Cycle through 3 fly modes", Array.Empty<object>()), GlKeys.F3, HotkeyType.CreativeTool, false, false, false, false);
			this.RegisterHotKey("fly", Lang.Get("Fly Mode On/Off", Array.Empty<object>()), 51, new int?(51), HotkeyType.CreativeTool, false, false, false);
			this.RegisterHotKey("rendermetablocks", Lang.Get("Show/Hide Meta Blocks", Array.Empty<object>()), GlKeys.F4, HotkeyType.CreativeTool, false, true, false, false);
			this.RegisterHotKey("fpsgraph", Lang.Get("FPS graph", Array.Empty<object>()), GlKeys.F3, HotkeyType.DevTool, true, false, false, false);
			this.RegisterHotKey("debugscreenandgraph", Lang.Get("Debug screen + FPS graph", Array.Empty<object>()), GlKeys.F3, HotkeyType.DevTool, false, true, false, false);
			this.RegisterHotKey("reloadworld", Lang.Get("Reload world", Array.Empty<object>()), GlKeys.F1, HotkeyType.DevTool, false, true, false, false);
			this.RegisterHotKey("reloadshaders", Lang.Get("Reload shaders", Array.Empty<object>()), GlKeys.F1, HotkeyType.DevTool, true, false, false, false);
			this.RegisterHotKey("reloadtextures", Lang.Get("Reload textures", Array.Empty<object>()), GlKeys.F2, HotkeyType.DevTool, true, false, false, false);
			this.RegisterHotKey("compactheap", Lang.Get("Compact large object heap", Array.Empty<object>()), GlKeys.F8, HotkeyType.DevTool, true, false, false, false);
			this.RegisterHotKey("recomposeallguis", Lang.Get("Recompose all dialogs", Array.Empty<object>()), GlKeys.F9, HotkeyType.DevTool, true, false, false, false);
			this.RegisterHotKey("cycledialogoutlines", Lang.Get("Cycle Dialog Outline Modes", Array.Empty<object>()), GlKeys.F10, HotkeyType.DevTool, true, false, false, false);
			this.RegisterHotKey("tickprofiler", Lang.Get("Toggle Tick Profiler", Array.Empty<object>()), GlKeys.F10, HotkeyType.DevTool, false, true, false, false);
			this.RegisterHotKey("pickblock", Lang.Get("Pick block", Array.Empty<object>()), EnumMouseButton.Middle, HotkeyType.GUIOrOtherControls, false, false, false, false);
			this.HotKeys["reloadworld"].IsGlobalHotkey = true;
			this.HotKeys["togglefullscreen"].IsGlobalHotkey = true;
			this.HotKeys["cycledialogoutlines"].IsGlobalHotkey = true;
			this.HotKeys["recomposeallguis"].IsGlobalHotkey = true;
			this.HotKeys["compactheap"].IsGlobalHotkey = true;
			this.HotKeys["screenshot"].IsGlobalHotkey = true;
			this.HotKeys["megascreenshot"].IsGlobalHotkey = true;
			this.HotKeys["primarymouse"].IsGlobalHotkey = true;
			this.HotKeys["secondarymouse"].IsGlobalHotkey = true;
		}

		internal void ResetKeyMapping()
		{
			foreach (HotKey hk in this.HotKeys.Values)
			{
				hk.CurrentMapping = hk.DefaultMapping.Clone();
				ClientSettings.Inst.SetKeyMapping(hk.Code, hk.CurrentMapping);
			}
		}

		internal bool TriggerGlobalHotKey(KeyEvent keyEventargs, IWorldAccessor world, IPlayer player, bool keyUp)
		{
			return this.ShouldTriggerHotkeys && (this.TriggerHotKey(keyEventargs, world, player, false, true, false, keyUp) || this.TriggerHotKey(keyEventargs, world, player, false, true, true, keyUp));
		}

		public bool TriggerHotKey(KeyEvent keyEventargs, IWorldAccessor world, IPlayer player, bool allowCharacterControls, bool keyUp)
		{
			return this.ShouldTriggerHotkeys && (this.TriggerHotKey(keyEventargs, world, player, allowCharacterControls, false, false, keyUp) || this.TriggerHotKey(keyEventargs, world, player, allowCharacterControls, false, true, keyUp));
		}

		private bool TriggerHotKey(KeyEvent keyEventargs, IWorldAccessor world, IPlayer player, bool allowCharacterControls, bool isGlobal, bool fallBack, bool keyup)
		{
			foreach (HotKey hotkey in this.HotKeys.ValuesOrdered)
			{
				if (hotkey.CurrentMapping.KeyCode == keyEventargs.KeyCode && (!keyup || hotkey.TriggerOnUpAlso) && (hotkey.KeyCombinationType != HotkeyType.CreativeTool || player == null || player.WorldData.CurrentGameMode == EnumGameMode.Creative) && (hotkey.KeyCombinationType != HotkeyType.CreativeOrSpectatorTool || player == null || player.WorldData.CurrentGameMode == EnumGameMode.Creative || player.WorldData.CurrentGameMode == EnumGameMode.Spectator) && ((!isGlobal || hotkey.IsGlobalHotkey) && (fallBack ? hotkey.FallbackDidPress(keyEventargs, world, player, allowCharacterControls) : hotkey.DidPress(keyEventargs, world, player, allowCharacterControls))) && hotkey.Handler != null)
				{
					keyEventargs.Handled = true;
					hotkey.CurrentMapping.OnKeyUp = keyup;
					if (hotkey.Handler(hotkey.CurrentMapping))
					{
						OnHotKeyDelegate onHotKeyDelegate = this.listeners;
						if (onHotKeyDelegate != null)
						{
							onHotKeyDelegate(hotkey.Code, hotkey.CurrentMapping);
						}
						return true;
					}
				}
			}
			return false;
		}

		public bool IsHotKeyRegistered(KeyCombination keyCombMap)
		{
			return this.HotKeys.Values.Any((HotKey kc) => kc.CurrentMapping.ToString() == keyCombMap.ToString());
		}

		public HotKey GetHotkeyByKeyCombination(KeyCombination keyCombMap)
		{
			return this.HotKeys.Values.FirstOrDefault((HotKey kc) => kc.CurrentMapping.ToString() == keyCombMap.ToString());
		}

		public HotKey GetHotKeyByCode(string code)
		{
			if (code == null)
			{
				return null;
			}
			return this.HotKeys.TryGetValue(code);
		}

		public void RemoveHotKey(string code)
		{
			this.HotKeys.Remove(code);
		}

		public void RegisterHotKey(HotKey keyComb)
		{
			keyComb.SetDefaultMapping();
			this.HotKeys[keyComb.Code] = keyComb;
		}

		public void RegisterHotKey(string code, string name, KeyCombination keyComb, HotkeyType type = HotkeyType.CharacterControls)
		{
			this.RegisterHotKey(code, name, keyComb.KeyCode, keyComb.SecondKeyCode, type, keyComb.Alt, keyComb.Ctrl, keyComb.Shift);
		}

		public void RegisterHotKey(string code, string name, GlKeys key, HotkeyType type = HotkeyType.CharacterControls, bool altPressed = false, bool ctrlPressed = false, bool shiftPressed = false, bool insertFirst = false)
		{
			this.RegisterHotKey(code, name, (int)key, type, altPressed, ctrlPressed, shiftPressed, insertFirst, false);
		}

		public void RegisterHotKey(string code, string name, EnumMouseButton button, HotkeyType type = HotkeyType.CharacterControls, bool altPressed = false, bool ctrlPressed = false, bool shiftPressed = false, bool insertFirst = false)
		{
			this.RegisterHotKey(code, name, (int)(button + 240), type, altPressed, ctrlPressed, shiftPressed, insertFirst, true);
		}

		public void RegisterHotKey(string code, string name, int keyCode, HotkeyType type = HotkeyType.CharacterControls, bool altPressed = false, bool ctrlPressed = false, bool shiftPressed = false, bool insertFirst = false, bool triggerOnUpAlso = false)
		{
			HotKey hKey = new HotKey
			{
				Code = code,
				Name = name,
				KeyCombinationType = type,
				CurrentMapping = new KeyCombination
				{
					KeyCode = keyCode,
					Ctrl = ctrlPressed,
					Alt = altPressed,
					Shift = shiftPressed
				},
				TriggerOnUpAlso = triggerOnUpAlso
			};
			if (insertFirst)
			{
				this.HotKeys.Insert(0, code, hKey);
			}
			else
			{
				this.HotKeys[code] = hKey;
			}
			hKey.SetDefaultMapping();
			KeyCombination comb;
			if (ClientSettings.KeyMapping.TryGetValue(code, out comb))
			{
				hKey.CurrentMapping = comb;
			}
		}

		public void RegisterHotKey(string code, string name, int keyCode, int? keyCode2, HotkeyType type = HotkeyType.CharacterControls, bool altPressed = false, bool ctrlPressed = false, bool shiftPressed = false)
		{
			this.HotKeys[code] = new HotKey
			{
				Code = code,
				Name = name,
				KeyCombinationType = type,
				CurrentMapping = new KeyCombination
				{
					KeyCode = keyCode,
					SecondKeyCode = keyCode2,
					Ctrl = ctrlPressed,
					Alt = altPressed,
					Shift = shiftPressed
				}
			};
			this.HotKeys[code].SetDefaultMapping();
			KeyCombination comb;
			if (ClientSettings.KeyMapping.TryGetValue(code, out comb))
			{
				this.HotKeys[code].CurrentMapping = comb;
			}
		}

		public void SetHotKeyHandler(string code, ActionConsumable<KeyCombination> handler, bool isIngameHotkey = true)
		{
			if (this.HotKeys.ContainsKey(code))
			{
				this.HotKeys[code].Handler = handler;
				this.HotKeys[code].IsIngameHotkey = isIngameHotkey;
			}
		}

		public void ClearInGameHotKeyHandlers()
		{
			foreach (HotKey hotkey in this.HotKeys.Values)
			{
				if (hotkey.IsIngameHotkey)
				{
					hotkey.Handler = null;
				}
			}
			this.listeners = null;
		}

		public void AddHotkeyListener(OnHotKeyDelegate handler)
		{
			this.listeners += handler;
		}

		public bool OnMouseButton(ClientMain game, EnumMouseButton button, int modifiers, bool buttonDown)
		{
			KeyEvent args = new KeyEvent
			{
				KeyCode = (int)(button + 240)
			};
			args.CtrlPressed = (modifiers & 2) != 0;
			args.ShiftPressed = (modifiers & 1) != 0;
			args.AltPressed = (modifiers & 4) != 0;
			args.CommandPressed = (modifiers & 8) != 0;
			return this.TriggerHotKey(args, game, game.player, game.AllowCharacterControl, !buttonDown);
		}

		public OrderedDictionary<string, HotKey> HotKeys = new OrderedDictionary<string, HotKey>();

		public bool ShouldTriggerHotkeys = true;
	}
}
