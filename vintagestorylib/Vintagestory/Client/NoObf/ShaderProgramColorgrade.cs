using System;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class ShaderProgramColorgrade : ShaderProgram
	{
		public int PrimaryScene2D
		{
			set
			{
				base.BindTexture2D("primaryScene", value, 0);
			}
		}

		public float GammaLevel
		{
			set
			{
				base.Uniform("gammaLevel", value);
			}
		}

		public float BrightnessLevel
		{
			set
			{
				base.Uniform("brightnessLevel", value);
			}
		}

		public float SepiaLevel
		{
			set
			{
				base.Uniform("sepiaLevel", value);
			}
		}

		public float DamageVignetting
		{
			set
			{
				base.Uniform("damageVignetting", value);
			}
		}

		public float Minlight
		{
			set
			{
				base.Uniform("minlight", value);
			}
		}

		public float Maxlight
		{
			set
			{
				base.Uniform("maxlight", value);
			}
		}

		public float Minsat
		{
			set
			{
				base.Uniform("minsat", value);
			}
		}

		public float Maxsat
		{
			set
			{
				base.Uniform("maxsat", value);
			}
		}

		public Vec2f InvFrameSizeIn
		{
			set
			{
				base.Uniform("invFrameSizeIn", value);
			}
		}
	}
}
