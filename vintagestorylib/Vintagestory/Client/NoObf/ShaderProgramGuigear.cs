using System;

namespace Vintagestory.Client.NoObf
{
	public class ShaderProgramGuigear : ShaderProgram
	{
		public int Tex2d2D
		{
			set
			{
				base.BindTexture2D("tex2d", value, 0);
			}
		}

		public float GearCounter
		{
			set
			{
				base.Uniform("gearCounter", value);
			}
		}

		public float StabilityLevel
		{
			set
			{
				base.Uniform("stabilityLevel", value);
			}
		}

		public float ShadeYPos
		{
			set
			{
				base.Uniform("shadeYPos", value);
			}
		}

		public float HotbarYPos
		{
			set
			{
				base.Uniform("hotbarYPos", value);
			}
		}

		public float GearHeight
		{
			set
			{
				base.Uniform("gearHeight", value);
			}
		}

		public float[] ProjectionMatrix
		{
			set
			{
				base.UniformMatrix("projectionMatrix", value);
			}
		}

		public float[] ModelViewMatrix
		{
			set
			{
				base.UniformMatrix("modelViewMatrix", value);
			}
		}
	}
}
