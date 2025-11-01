using System;

namespace Vintagestory.Client.NoObf
{
	public class FloatRef
	{
		public static FloatRef Create(float value_)
		{
			return new FloatRef
			{
				value = value_
			};
		}

		public float GetValue()
		{
			return this.value;
		}

		public void SetValue(float value_)
		{
			this.value = value_;
		}

		internal float value;
	}
}
