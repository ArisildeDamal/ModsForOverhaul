using System;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class ShaderProgramCloudvolumetric : ShaderProgram
	{
		public float[] IMvpMatrix
		{
			set
			{
				base.UniformMatrix("iMvpMatrix", value);
			}
		}

		public int DepthTex2D
		{
			set
			{
				base.BindTexture2D("depthTex", value, 0);
			}
		}

		public int CloudMap2D
		{
			set
			{
				base.BindTexture2D("cloudMap", value, 1);
			}
		}

		public int CloudCol2D
		{
			set
			{
				base.BindTexture2D("cloudCol", value, 2);
			}
		}

		public float CloudMapWidth
		{
			set
			{
				base.Uniform("cloudMapWidth", value);
			}
		}

		public Vec3f CloudOffset
		{
			set
			{
				base.Uniform("cloudOffset", value);
			}
		}

		public int Frame
		{
			set
			{
				base.Uniform("frame", value);
			}
		}

		public float Time
		{
			set
			{
				base.Uniform("time", value);
			}
		}

		public int FrameWidth
		{
			set
			{
				base.Uniform("FrameWidth", value);
			}
		}

		public float PerceptionEffectIntensity
		{
			set
			{
				base.Uniform("PerceptionEffectIntensity", value);
			}
		}
	}
}
