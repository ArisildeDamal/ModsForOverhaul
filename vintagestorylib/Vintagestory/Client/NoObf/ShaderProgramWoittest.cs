using System;

namespace Vintagestory.Client.NoObf
{
	public class ShaderProgramWoittest : ShaderProgram
	{
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
