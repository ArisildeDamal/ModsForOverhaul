using System;

namespace Vintagestory.Client.NoObf
{
	public class ShaderProgramLuma : ShaderProgram
	{
		public int Scene2D
		{
			set
			{
				base.BindTexture2D("scene", value, 0);
			}
		}
	}
}
