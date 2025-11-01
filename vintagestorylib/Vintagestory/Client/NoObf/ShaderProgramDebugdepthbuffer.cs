using System;

namespace Vintagestory.Client.NoObf
{
	public class ShaderProgramDebugdepthbuffer : ShaderProgram
	{
		public int DepthSampler2D
		{
			set
			{
				base.BindTexture2D("depthSampler", value, 0);
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
