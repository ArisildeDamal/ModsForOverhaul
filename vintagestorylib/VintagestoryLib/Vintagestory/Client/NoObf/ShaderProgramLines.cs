using System;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class ShaderProgramLines : ShaderProgram
	{
		public Vec4f Color
		{
			set
			{
				base.Uniform("color", value);
			}
		}

		public float GlowLevel
		{
			set
			{
				base.Uniform("glowLevel", value);
			}
		}

		public float LineWidth
		{
			set
			{
				base.Uniform("lineWidth", value);
			}
		}

		public float[] Projection
		{
			set
			{
				base.UniformMatrix("projection", value);
			}
		}

		public float[] View
		{
			set
			{
				base.UniformMatrix("view", value);
			}
		}

		public Vec3f Origin
		{
			set
			{
				base.Uniform("origin", value);
			}
		}
	}
}
