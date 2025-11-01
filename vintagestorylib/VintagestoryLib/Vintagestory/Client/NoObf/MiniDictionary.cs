using System;

namespace Vintagestory.Client.NoObf
{
	public class MiniDictionary
	{
		public MiniDictionary(int size)
		{
			this.keys = new string[size];
			this.values = new int[size];
		}

		public int this[string key]
		{
			get
			{
				for (int i = 0; i < this.keys.Length; i++)
				{
					if (i >= this.Count)
					{
						return -1;
					}
					if (key == this.keys[i])
					{
						return this.values[i];
					}
				}
				return -1;
			}
			set
			{
				for (int i = 0; i < this.Count; i++)
				{
					if (this.keys[i] == key)
					{
						this.values[i] = value;
						return;
					}
				}
				if (this.Count == this.keys.Length)
				{
					this.ExpandArrays();
				}
				this.keys[this.Count] = key;
				int[] array = this.values;
				int count = this.Count;
				this.Count = count + 1;
				array[count] = value;
			}
		}

		private void ExpandArrays()
		{
			int num = this.keys.Length + 3;
			string[] newKeys = new string[num];
			int[] newValues = new int[num];
			for (int i = 0; i < this.keys.Length; i++)
			{
				newKeys[i] = this.keys[i];
				newValues[i] = this.values[i];
			}
			this.values = newValues;
			this.keys = newKeys;
		}

		private string[] keys;

		private int[] values;

		public int Count;

		public const int NOTFOUND = -1;
	}
}
