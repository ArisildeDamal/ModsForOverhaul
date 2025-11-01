using System;

namespace Vintagestory.Client.NoObf
{
	public struct FaceData
	{
		public FaceData(float[] xyz, int i, float u1, float v1, float u2, float v2, int[] flags, int flagsIndex, int colorData, bool rotateUV)
		{
			this.x = xyz[i];
			this.y = xyz[i + 1];
			this.z = xyz[i + 2];
			this.dx1 = (xyz[i + 3] - this.x) / 2f;
			this.dy1 = (xyz[i + 4] - this.y) / 2f;
			this.dz1 = (xyz[i + 5] - this.z) / 2f;
			this.dx2 = (xyz[i + 9] - this.x) / 2f;
			this.dy2 = (xyz[i + 10] - this.y) / 2f;
			this.dz2 = (xyz[i + 11] - this.z) / 2f;
			this.uv = (int)(u1 * 32768f + 0.5f) + ((int)(v1 * 32768f + 0.5f) << 16);
			if (u2 < 0f)
			{
				u2 += 1f;
			}
			if (v2 < 0f)
			{
				v2 += 1f;
			}
			this.uvSize = (int)(u2 * 32768f + 0.5f) + ((int)(v2 * 32768f + 0.5f) << 16) + (rotateUV ? 32768 : 0);
			this.renderFlags0 = flags[flagsIndex];
			this.renderFlags1 = flags[flagsIndex + 1];
			this.renderFlags2 = flags[flagsIndex + 2];
			this.renderFlags3 = flags[flagsIndex + 3];
			this.colormapData = colorData;
		}

		public const int size = 16;

		public float x;

		public float y;

		public float z;

		public int uv;

		public float dx1;

		public float dy1;

		public float dz1;

		public int uvSize;

		public int renderFlags0;

		public int renderFlags1;

		public int renderFlags2;

		public int renderFlags3;

		public float dx2;

		public float dy2;

		public float dz2;

		public int colormapData;
	}
}
