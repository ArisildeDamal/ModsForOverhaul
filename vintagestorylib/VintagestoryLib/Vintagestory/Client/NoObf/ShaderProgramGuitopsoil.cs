using System;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class ShaderProgramGuitopsoil : ShaderProgram
	{
		public int TerrainTex2D
		{
			set
			{
				base.BindTexture2D("terrainTex", value, 0);
			}
		}

		public float BlockTextureSize
		{
			set
			{
				base.Uniform("blockTextureSize", value);
			}
		}

		public float NoTexture
		{
			set
			{
				base.Uniform("noTexture", value);
			}
		}

		public float AlphaTest
		{
			set
			{
				base.Uniform("alphaTest", value);
			}
		}

		public Vec4f RgbaIn
		{
			set
			{
				base.Uniform("rgbaIn", value);
			}
		}

		public int ExtraGlow
		{
			set
			{
				base.Uniform("extraGlow", value);
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

		public int ApplyColor
		{
			set
			{
				base.Uniform("applyColor", value);
			}
		}
	}
}
