using System;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class ShaderProgramBlur : ShaderProgram
	{
		public int InputTexture2D
		{
			set
			{
				base.BindTexture2D("inputTexture", value, 0);
			}
		}

		public Vec2f FrameSize
		{
			set
			{
				base.Uniform("frameSize", value);
			}
		}

		public int IsVertical
		{
			set
			{
				base.Uniform("isVertical", value);
			}
		}
	}
}
