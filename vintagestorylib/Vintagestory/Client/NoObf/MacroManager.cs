using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.Client.NoObf
{
	public class MacroManager : IMacroManager
	{
		public SortedDictionary<int, IMacroBase> MacrosByIndex { get; set; } = new SortedDictionary<int, IMacroBase>();

		public MacroManager(ClientMain game)
		{
			this.game = game;
			this.LoadMacros();
		}

		public void LoadMacros()
		{
			SortedDictionary<string, Macro> macrosByFilename = new SortedDictionary<string, Macro>();
			foreach (string file in Directory.EnumerateFiles(GamePaths.Macros, "*.json"))
			{
				string contents = File.ReadAllText(file);
				try
				{
					Macro macro = JsonConvert.DeserializeObject<Macro>(contents);
					macrosByFilename.Add(file, macro);
				}
				catch (Exception e)
				{
					ScreenManager.Platform.Logger.Warning("Failed deserializing macro " + file + ": " + e.Message);
				}
			}
			foreach (Macro macro2 in macrosByFilename.Values)
			{
				this.MacrosByIndex[macro2.Index] = macro2;
				this.SetupHotKey(macro2.Index, macro2, this.game);
			}
		}

		private bool SetupHotKey(int macroIndex, IMacroBase macro, ClientMain game)
		{
			if (macro.KeyCombination == null || macro.KeyCombination.KeyCode < 0)
			{
				return false;
			}
			HotKey hotkey = ScreenManager.hotkeyManager.GetHotkeyByKeyCombination(macro.KeyCombination);
			string hotkeyCode = "macro-" + macro.Code;
			if (hotkey != null && hotkey.Code != hotkeyCode)
			{
				ScreenManager.Platform.Logger.Warning("Can't register hotkey {0} for macro {1} because it is aready in use by hotkey {2}", new object[] { macro.KeyCombination, macro.Code, hotkey.Code });
				return false;
			}
			ScreenManager.hotkeyManager.RegisterHotKey(hotkeyCode, "Macro: " + macro.Name, macro.KeyCombination, HotkeyType.DevTool);
			ScreenManager.hotkeyManager.SetHotKeyHandler(hotkeyCode, delegate(KeyCombination viaKeyComb)
			{
				this.RunMacro(macroIndex, game);
				return true;
			}, true);
			return true;
		}

		public void DeleteMacro(int macroIndex)
		{
			IMacroBase macro;
			this.MacrosByIndex.TryGetValue(macroIndex, out macro);
			if (macro == null)
			{
				return;
			}
			File.Delete(Path.Combine(GamePaths.Macros, macroIndex.ToString() + "-" + macro.Code + ".json"));
			this.MacrosByIndex.Remove(macroIndex);
			string hotkeyCode = "macro-" + macro.Code;
			ScreenManager.hotkeyManager.RemoveHotKey(hotkeyCode);
		}

		public void SetMacro(int macroIndex, IMacroBase macro)
		{
			this.MacrosByIndex[macroIndex] = macro;
			this.SaveMacro(macroIndex);
			this.SetupHotKey(macroIndex, macro, this.game);
		}

		public virtual bool SaveMacro(int macroIndex)
		{
			IMacroBase macro;
			this.MacrosByIndex.TryGetValue(macroIndex, out macro);
			if (macro == null)
			{
				return false;
			}
			string filename = Path.Combine(GamePaths.Macros, macroIndex.ToString() + "-" + macro.Code + ".json");
			try
			{
				using (TextWriter textWriter = new StreamWriter(filename))
				{
					textWriter.Write(JsonConvert.SerializeObject(macro, Formatting.Indented));
					textWriter.Close();
				}
			}
			catch (IOException)
			{
				return false;
			}
			this.SetupHotKey(macroIndex, macro, this.game);
			return true;
		}

		public bool RunMacro(int macroIndex, IClientWorldAccessor world)
		{
			if (!this.MacrosByIndex.ContainsKey(macroIndex))
			{
				return false;
			}
			string[] commands = this.MacrosByIndex[macroIndex].Commands;
			for (int i = 0; i < commands.Length; i++)
			{
				ClientEventManager eventManager = (world as ClientMain).eventManager;
				if (eventManager != null)
				{
					eventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, commands[i], EnumChatType.Macro, null);
				}
			}
			return true;
		}

		private ClientMain game;
	}
}
