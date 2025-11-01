using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Vintagestory.API.Client;

namespace Vintagestory.Common
{
	[JsonObject(MemberSerialization.OptIn)]
	public abstract class SettingsBase : SettingsBaseNoObf, ISettings
	{
		public void AddWatcher<T>(string key, OnSettingsChanged<T> handler)
		{
			if (typeof(T) == typeof(bool))
			{
				this.BoolSettings.AddWatcher(key, handler as OnSettingsChanged<bool>);
			}
			if (typeof(T) == typeof(string))
			{
				this.StringSettings.AddWatcher(key, handler as OnSettingsChanged<string>);
			}
			if (typeof(T) == typeof(int))
			{
				this.IntSettings.AddWatcher(key, handler as OnSettingsChanged<int>);
			}
			if (typeof(T) == typeof(float))
			{
				this.FloatSettings.AddWatcher(key, handler as OnSettingsChanged<float>);
			}
			if (typeof(T) == typeof(List<string>))
			{
				this.StringListSettings.AddWatcher(key, handler as OnSettingsChanged<List<string>>);
			}
		}

		public virtual void ClearWatchers()
		{
			this.StringListSettings.Watchers.Clear();
			this.BoolSettings.Watchers.Clear();
			this.IntSettings.Watchers.Clear();
			this.FloatSettings.Watchers.Clear();
			this.StringSettings.Watchers.Clear();
		}

		public string GetStringSetting(string key, string defaultValue = null)
		{
			string value = defaultValue;
			base.stringSettings.TryGetValue(key.ToLowerInvariant(), out value);
			return value;
		}

		public List<string> GetStringListSetting(string key, List<string> defaultValue = null)
		{
			List<string> value = defaultValue;
			base.stringListSettings.TryGetValue(key.ToLowerInvariant(), out value);
			return value;
		}

		public int GetIntSetting(string key)
		{
			int value;
			base.intSettings.TryGetValue(key.ToLowerInvariant(), out value);
			return value;
		}

		public float GetFloatSetting(string key)
		{
			float value;
			base.floatSettings.TryGetValue(key.ToLowerInvariant(), out value);
			return value;
		}

		public bool GetBoolSetting(string key)
		{
			bool value;
			base.boolSettings.TryGetValue(key.ToLowerInvariant(), out value);
			return value;
		}

		public bool HasSetting(string name)
		{
			name = name.ToLowerInvariant();
			return base.stringSettings.ContainsKey(name) || base.intSettings.ContainsKey(name) || base.floatSettings.ContainsKey(name) || base.boolSettings.ContainsKey(name);
		}

		public Type GetSettingType(string name)
		{
			name = name.ToLowerInvariant();
			if (base.stringSettings.ContainsKey(name))
			{
				return typeof(string);
			}
			if (base.intSettings.ContainsKey(name))
			{
				return typeof(int);
			}
			if (base.floatSettings.ContainsKey(name))
			{
				return typeof(float);
			}
			if (base.boolSettings.ContainsKey(name))
			{
				return typeof(bool);
			}
			return null;
		}

		internal object GetSetting(string name)
		{
			name = name.ToLowerInvariant();
			if (base.stringSettings.ContainsKey(name))
			{
				return this.GetStringSetting(name, null);
			}
			if (base.intSettings.ContainsKey(name))
			{
				return this.GetIntSetting(name);
			}
			if (base.floatSettings.ContainsKey(name))
			{
				return this.GetFloatSetting(name);
			}
			if (base.boolSettings.ContainsKey(name))
			{
				return this.GetBoolSetting(name);
			}
			return null;
		}

		protected SettingsBase()
		{
			StringComparer comparer = StringComparer.OrdinalIgnoreCase;
			base.stringSettings = new Dictionary<string, string>(comparer);
			base.intSettings = new Dictionary<string, int>(comparer);
			base.boolSettings = new Dictionary<string, bool>(comparer);
			base.floatSettings = new Dictionary<string, float>(comparer);
			base.stringListSettings = new Dictionary<string, List<string>>(comparer);
		}

		[OnDeserializing]
		internal void OnDeserializingMethod(StreamingContext context)
		{
			this.LoadDefaultValues();
		}

		[OnDeserialized]
		internal void OnDeserializedMethod(StreamingContext context)
		{
			StringComparer comparer = StringComparer.OrdinalIgnoreCase;
			if (base.stringSettings == null)
			{
				base.stringSettings = new Dictionary<string, string>(comparer);
			}
			if (base.intSettings == null)
			{
				base.intSettings = new Dictionary<string, int>(comparer);
			}
			if (base.boolSettings == null)
			{
				base.boolSettings = new Dictionary<string, bool>(comparer);
			}
			if (base.floatSettings == null)
			{
				base.floatSettings = new Dictionary<string, float>(comparer);
			}
			if (base.stringListSettings == null)
			{
				base.stringListSettings = new Dictionary<string, List<string>>(comparer);
			}
			this.DidDeserialize();
		}

		internal virtual void DidDeserialize()
		{
		}

		public abstract string FileName { get; }

		public abstract string TempFileName { get; }

		public abstract string BkpFileName { get; }

		public virtual void Load()
		{
			this.LoadDefaultValues();
			if (!File.Exists(this.FileName) && File.Exists(this.BkpFileName))
			{
				File.Move(this.BkpFileName, this.FileName);
			}
			if (File.Exists(this.FileName))
			{
				try
				{
					string fileContents;
					using (TextReader textReader = new StreamReader(this.FileName))
					{
						fileContents = textReader.ReadToEnd();
						textReader.Close();
					}
					JsonConvert.PopulateObject(fileContents, this);
				}
				catch (Exception)
				{
					this.isnewfile = true;
				}
				return;
			}
			this.OtherDirty = true;
			this.isnewfile = true;
		}

		public bool IsDirty
		{
			get
			{
				return this.BoolSettings.Dirty || this.StringSettings.Dirty || this.StringListSettings.Dirty || this.FloatSettings.Dirty || this.IntSettings.Dirty || this.OtherDirty;
			}
		}

		public virtual bool Save(bool force = false)
		{
			if (!this.IsDirty && !force)
			{
				return true;
			}
			try
			{
				using (TextWriter textWriter = new StreamWriter(this.TempFileName))
				{
					textWriter.Write(JsonConvert.SerializeObject(this, Formatting.Indented));
					textWriter.Close();
				}
				if (!File.Exists(this.FileName))
				{
					File.Move(this.TempFileName, this.FileName);
				}
				else
				{
					File.Replace(this.TempFileName, this.FileName, this.BkpFileName);
				}
			}
			catch (IOException)
			{
				return false;
			}
			this.OtherDirty = false;
			this.BoolSettings.Dirty = false;
			this.StringSettings.Dirty = false;
			this.StringListSettings.Dirty = false;
			this.FloatSettings.Dirty = false;
			this.IntSettings.Dirty = false;
			return true;
		}

		public abstract void LoadDefaultValues();

		protected bool isnewfile;
	}
}
