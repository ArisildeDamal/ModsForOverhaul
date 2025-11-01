using System;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class ShaderProgramGodrays : ShaderProgram
	{
		public int InputTexture2D
		{
			set
			{
				base.BindTexture2D("inputTexture", value, 0);
			}
		}

		public int GlowParts2D
		{
			set
			{
				base.BindTexture2D("glowParts", value, 1);
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

		public float IGlobalTimeIn
		{
			set
			{
				base.Uniform("iGlobalTimeIn", value);
			}
		}

		public float DirectionIn
		{
			set
			{
				base.Uniform("directionIn", value);
			}
		}

		public int Dusk
		{
			set
			{
				base.Uniform("dusk", value);
			}
		}
	}
}
