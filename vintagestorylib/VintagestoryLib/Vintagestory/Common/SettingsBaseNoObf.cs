using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common
{
	public abstract class SettingsBaseNoObf
	{
		public bool ShouldTriggerWatchers
		{
			set
			{
				SettingsClass<string> stringSettings = this.StringSettings;
				SettingsClass<List<string>> stringListSettings = this.StringListSettings;
				SettingsClass<int> intSettings = this.IntSettings;
				SettingsClass<float> floatSettings = this.FloatSettings;
				this.BoolSettings.ShouldTriggerWatchers = value;
				floatSettings.ShouldTriggerWatchers = value;
				intSettings.ShouldTriggerWatchers = value;
				stringListSettings.ShouldTriggerWatchers = value;
				stringSettings.ShouldTriggerWatchers = value;
			}
		}

		public ISettingsClass<bool> Bool
		{
			get
			{
				return this.BoolSettings;
			}
		}

		public ISettingsClass<int> Int
		{
			get
			{
				return this.IntSettings;
			}
		}

		public ISettingsClass<float> Float
		{
			get
			{
				return this.FloatSettings;
			}
		}

		public ISettingsClass<string> String
		{
			get
			{
				return this.StringSettings;
			}
		}

		public ISettingsClass<List<string>> Strings
		{
			get
			{
				return this.StringListSettings;
			}
		}

		[JsonProperty]
		protected Dictionary<string, string> stringSettings
		{
			get
			{
				return this.StringSettings.values;
			}
			set
			{
				this.StringSettings.values = value;
			}
		}

		[JsonProperty]
		protected Dictionary<string, int> intSettings
		{
			get
			{
				return this.IntSettings.values;
			}
			set
			{
				this.IntSettings.values = value;
			}
		}

		[JsonProperty]
		protected Dictionary<string, bool> boolSettings
		{
			get
			{
				return this.BoolSettings.values;
			}
			set
			{
				this.BoolSettings.values = value;
			}
		}

		[JsonProperty]
		protected Dictionary<string, float> floatSettings
		{
			get
			{
				return this.FloatSettings.values;
			}
			set
			{
				this.FloatSettings.values = value;
			}
		}

		[JsonProperty]
		protected Dictionary<string, List<string>> stringListSettings
		{
			get
			{
				return this.StringListSettings.values;
			}
			set
			{
				this.StringListSettings.values = value;
			}
		}

		protected SettingsClass<string> StringSettings = new SettingsClass<string>();

		protected SettingsClass<List<string>> StringListSettings = new SettingsClass<List<string>>();

		protected SettingsClass<int> IntSettings = new SettingsClass<int>();

		protected SettingsClass<float> FloatSettings = new SettingsClass<float>();

		protected SettingsClass<bool> BoolSettings = new SettingsClass<bool>();

		[JsonProperty]
		protected Dictionary<string, KeyCombination> keyMapping;

		[JsonProperty]
		protected Dictionary<string, Vec2i> dialogPositions = new Dictionary<string, Vec2i>();

		public bool OtherDirty;
	}
}
