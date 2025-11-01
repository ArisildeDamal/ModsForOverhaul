using System;

namespace Vintagestory.Client.NoObf
{
	public class ShaderProgramParticlesquad2d : ShaderProgram
	{
		public int ParticleTex2D
		{
			set
			{
				base.BindTexture2D("particleTex", value, 0);
			}
		}

		public int OitPass
		{
			set
			{
				base.Uniform("oitPass", value);
			}
		}

		public int WithTexture
		{
			set
			{
				base.Uniform("withTexture", value);
			}
		}

		public int HeldItemMode
		{
			set
			{
				base.Uniform("heldItemMode", value);
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
