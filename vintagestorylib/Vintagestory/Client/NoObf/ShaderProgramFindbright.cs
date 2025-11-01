using System;

namespace Vintagestory.Client.NoObf
{
	public class ShaderProgramFindbright : ShaderProgram
	{
		public int ColorTex2D
		{
			set
			{
				base.BindTexture2D("colorTex", value, 0);
			}
		}

		public int GlowTex2D
		{
			set
			{
				base.BindTexture2D("glowTex", value, 1);
			}
		}

		public float ExtraBloom
		{
			set
			{
				base.Uniform("extraBloom", value);
			}
		}

		public float AmbientBloomLevel
		{
			set
			{
				base.Uniform("ambientBloomLevel", value);
			}
		}
	}
}
