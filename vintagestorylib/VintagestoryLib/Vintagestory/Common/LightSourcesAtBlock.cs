using System;

namespace Vintagestory.Common
{
	public class LightSourcesAtBlock
	{
		public byte LastBrightness
		{
			get
			{
				return this.lightHsvs[(int)(this.lightCount - 1)];
			}
		}

		public void AddHsv(byte h, byte s, byte v)
		{
			if (this.lightCount > 14)
			{
				return;
			}
			this.lightHsvs[(int)(3 * this.lightCount)] = h;
			this.lightHsvs[(int)(3 * this.lightCount + 1)] = s;
			this.lightHsvs[(int)(3 * this.lightCount + 2)] = v;
			this.lightCount += 1;
		}

		public byte[] lightHsvs = new byte[45];

		public byte lightCount;
	}
}
