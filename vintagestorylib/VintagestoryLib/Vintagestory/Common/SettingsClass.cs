using System;
using System.Collections.Generic;
using Vintagestory.API.Client;

namespace Vintagestory.Common
{
	public class SettingsClass<T> : ISettingsClass<T>
	{
		public SettingsClass()
		{
			StringComparer comparer = StringComparer.OrdinalIgnoreCase;
			this.values = new Dictionary<string, T>(comparer);
		}

		public bool Exists(string key)
		{
			return this.values.ContainsKey(key);
		}

		public T Get(string key, T defaultValue = default(T))
		{
			T val;
			if (this.values.TryGetValue(key, out val))
			{
				return val;
			}
			return defaultValue;
		}

		public T this[string key]
		{
			get
			{
				T val;
				if (!this.values.TryGetValue(key, out val))
				{
					val = this.defaultValue;
				}
				return val;
			}
			set
			{
				this.Set(key, value, this.ShouldTriggerWatchers);
			}
		}

		public void Set(string key, T value, bool shouldTriggerWatchers)
		{
			if (this.values.ContainsKey(key) && EqualityComparer<T>.Default.Equals(this.values[key], value))
			{
				return;
			}
			this.values[key] = value;
			if (shouldTriggerWatchers)
			{
				this.TriggerWatcher(key);
			}
			this.Dirty = true;
		}

		public void TriggerWatcher(string key)
		{
			string lowerkey = key.ToLowerInvariant();
			T value = this.values[key];
			foreach (SettingsChangedWatcher<T> watcher in this.Watchers)
			{
				if (watcher.key == lowerkey)
				{
					watcher.handler(value);
				}
			}
		}

		public void AddWatcher(string key, OnSettingsChanged<T> handler)
		{
			if (handler == null)
			{
				throw new ArgumentNullException("handler cannot be null!");
			}
			this.Watchers.Add(new SettingsChangedWatcher<T>
			{
				key = key.ToLowerInvariant(),
				handler = handler
			});
		}

		public bool RemoveWatcher(string key, OnSettingsChanged<T> handler)
		{
			for (int i = 0; i < this.Watchers.Count; i++)
			{
				SettingsChangedWatcher<T> var = this.Watchers[i];
				if (var.key == key.ToLowerInvariant() && var.handler == handler)
				{
					this.Watchers.RemoveAt(i);
					return true;
				}
			}
			return false;
		}

		public Dictionary<string, T> values;

		public T defaultValue;

		public List<SettingsChangedWatcher<T>> Watchers = new List<SettingsChangedWatcher<T>>();

		public bool Dirty;

		public bool ShouldTriggerWatchers = true;
	}
}
