using System;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class ShaderProgramSsao : ShaderProgram
	{
		public int GPosition2D
		{
			set
			{
				base.BindTexture2D("gPosition", value, 0);
			}
		}

		public int GNormal2D
		{
			set
			{
				base.BindTexture2D("gNormal", value, 1);
			}
		}

		public int TexNoise2D
		{
			set
			{
				base.BindTexture2D("texNoise", value, 2);
			}
		}

		public void SamplesArray(int count, float[] values)
		{
			base.Uniforms3("samples", count, values);
		}

		public Vec2f ScreenSize
		{
			set
			{
				base.Uniform("screenSize", value);
			}
		}

		public int Revealage2D
		{
			set
			{
				base.BindTexture2D("revealage", value, 3);
			}
		}

		public float[] Projection
		{
			set
			{
				base.UniformMatrix("projection", value);
			}
		}
	}
}
