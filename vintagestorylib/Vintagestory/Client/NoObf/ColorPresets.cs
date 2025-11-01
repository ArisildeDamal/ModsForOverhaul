using System;
using System.Collections.Generic;
using System.Globalization;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.Client.NoObf
{
	public class ColorPresets : IColorPresets
	{
		public ColorPresets(ClientMain game, ClientCoreAPI api)
		{
			this.game = game;
			this.api = api;
		}

		public int GetColor(string key)
		{
			if (this.currentPreset == null)
			{
				this.OnUpdateSetting();
			}
			int result;
			if (this.currentPreset != null && this.currentPreset.TryGetValue(key, out result))
			{
				return result;
			}
			return (key.GetHashCode() & 16777215) | -16777216;
		}

		public void SetFromClientsettings()
		{
			this.SetCurrent(this.api.Settings.Int["guiColorsPreset"]);
		}

		public void OnUpdateSetting()
		{
			this.SetFromClientsettings();
			ClientEventManager eventManager = this.game.eventManager;
			if (eventManager == null)
			{
				return;
			}
			eventManager.TriggerColorPresetChanged();
		}

		private void SetCurrent(int setting)
		{
			switch (setting)
			{
			case 1:
				this.currentPreset = this.Preset1;
				return;
			case 2:
				this.currentPreset = this.Preset2;
				return;
			case 3:
				this.currentPreset = this.Preset3;
				return;
			default:
				this.currentPreset = this.Preset1;
				return;
			}
		}

		public void Initialize(IAsset configfile)
		{
			Dictionary<string, Dictionary<string, string>> config = configfile.ToObject<Dictionary<string, Dictionary<string, string>>>(null);
			foreach (KeyValuePair<string, Dictionary<string, string>> entry in config)
			{
				config[entry.Key.ToLowerInvariant()] = entry.Value;
			}
			this.InitializeFromConfig(config, ref this.Preset1, "preset1");
			this.InitializeFromConfig(config, ref this.Preset2, "preset2");
			this.InitializeFromConfig(config, ref this.Preset3, "preset3");
			this.SetFromClientsettings();
		}

		private void InitializeFromConfig(Dictionary<string, Dictionary<string, string>> config, ref Dictionary<string, int> dict, string key)
		{
			if (dict == null)
			{
				dict = new Dictionary<string, int>();
			}
			Dictionary<string, string> presetsettings;
			if (config.TryGetValue(key, out presetsettings))
			{
				foreach (KeyValuePair<string, string> entry in presetsettings)
				{
					dict[entry.Key] = this.HexConvert(entry.Value);
				}
			}
		}

		private int HexConvert(string arg)
		{
			if (arg.StartsWith("0x"))
			{
				arg = arg.Substring(2);
			}
			int val;
			if (int.TryParse(arg, NumberStyles.HexNumber, GlobalConstants.DefaultCultureInfo, out val))
			{
				return val;
			}
			return 0;
		}

		private ClientMain game;

		private ICoreClientAPI api;

		private Dictionary<string, int> Preset1;

		private Dictionary<string, int> Preset2;

		private Dictionary<string, int> Preset3;

		private Dictionary<string, int> currentPreset;
	}
}
