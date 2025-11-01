using System;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class ShaderProgramFinal : ShaderProgram
	{
		public int PrimaryScene2D
		{
			set
			{
				base.BindTexture2D("primaryScene", value, 0);
			}
		}

		public int GlowParts2D
		{
			set
			{
				base.BindTexture2D("glowParts", value, 1);
			}
		}

		public int BloomParts2D
		{
			set
			{
				base.BindTexture2D("bloomParts", value, 2);
			}
		}

		public int GodrayParts2D
		{
			set
			{
				base.BindTexture2D("godrayParts", value, 3);
			}
		}

		public int SsaoScene2D
		{
			set
			{
				base.BindTexture2D("ssaoScene", value, 4);
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

		public float ContrastLevel
		{
			set
			{
				base.Uniform("contrastLevel", value);
			}
		}

		public float SepiaLevel
		{
			set
			{
				base.Uniform("sepiaLevel", value);
			}
		}

		public float AmbientBloomLevel
		{
			set
			{
				base.Uniform("ambientBloomLevel", value);
			}
		}

		public float DamageVignetting
		{
			set
			{
				base.Uniform("damageVignetting", value);
			}
		}

		public float DamageVignettingSide
		{
			set
			{
				base.Uniform("damageVignettingSide", value);
			}
		}

		public float FrostVignetting
		{
			set
			{
				base.Uniform("frostVignetting", value);
			}
		}

		public float ExtraGamma
		{
			set
			{
				base.Uniform("extraGamma", value);
			}
		}

		public float WindWaveCounter
		{
			set
			{
				base.Uniform("windWaveCounter", value);
			}
		}

		public float GlitchEffectStrength
		{
			set
			{
				base.Uniform("glitchEffectStrength", value);
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

		public Vec3f SunPosScreenIn
		{
			set
			{
				base.Uniform("sunPosScreenIn", value);
			}
		}

		public Vec3f SunPos3dIn
		{
			set
			{
				base.Uniform("sunPos3dIn", value);
			}
		}

		public Vec3f PlayerViewVector
		{
			set
			{
				base.Uniform("playerViewVector", value);
			}
		}
	}
}
