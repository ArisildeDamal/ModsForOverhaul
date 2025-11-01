using System;

namespace Vintagestory.Client.NoObf
{
	public class ShaderProgramTexture2texture : ShaderProgram
	{
		public int Tex2d2D
		{
			set
			{
				base.BindTexture2D("tex2d", value, 0);
			}
		}

		public float Texu
		{
			set
			{
				base.Uniform("texu", value);
			}
		}

		public float Texv
		{
			set
			{
				base.Uniform("texv", value);
			}
		}

		public float Texw
		{
			set
			{
				base.Uniform("texw", value);
			}
		}

		public float Texh
		{
			set
			{
				base.Uniform("texh", value);
			}
		}

		public float AlphaTest
		{
			set
			{
				base.Uniform("alphaTest", value);
			}
		}

		public float Xs
		{
			set
			{
				base.Uniform("xs", value);
			}
		}

		public float Ys
		{
			set
			{
				base.Uniform("ys", value);
			}
		}

		public float Width
		{
			set
			{
				base.Uniform("width", value);
			}
		}

		public float Height
		{
			set
			{
				base.Uniform("height", value);
			}
		}
	}
}
