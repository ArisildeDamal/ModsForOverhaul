using System;

namespace Vintagestory.Client.NoObf
{
	public class ShaderProgramAutocamera : ShaderProgram
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

		public float ShadowRangeFar
		{
			set
			{
				base.Uniform("shadowRangeFar", value);
			}
		}

		public float[] ToShadowMapSpaceMatrixFar
		{
			set
			{
				base.UniformMatrix("toShadowMapSpaceMatrixFar", value);
			}
		}

		public float ShadowRangeNear
		{
			set
			{
				base.Uniform("shadowRangeNear", value);
			}
		}

		public float[] ToShadowMapSpaceMatrixNear
		{
			set
			{
				base.UniformMatrix("toShadowMapSpaceMatrixNear", value);
			}
		}

		public float FlatFogDensity
		{
			set
			{
				base.Uniform("flatFogDensity", value);
			}
		}

		public float FlatFogStart
		{
			set
			{
				base.Uniform("flatFogStart", value);
			}
		}

		public float ViewDistance
		{
			set
			{
				base.Uniform("viewDistance", value);
			}
		}

		public float ViewDistanceLod0
		{
			set
			{
				base.Uniform("viewDistanceLod0", value);
			}
		}

		public float GlitchStrengthFL
		{
			set
			{
				base.Uniform("glitchStrengthFL", value);
			}
		}

		public float NightVisionStrength
		{
			set
			{
				base.Uniform("nightVisionStrength", value);
			}
		}

		public void PointLightsArray(int count, float[] values)
		{
			base.Uniforms3("pointLights", count, values);
		}

		public void PointLightColorsArray(int count, float[] values)
		{
			base.Uniforms3("pointLightColors", count, values);
		}

		public int PointLightQuantity
		{
			set
			{
				base.Uniform("pointLightQuantity", value);
			}
		}

		public float[] FogSpheres
		{
			set
			{
				base.Uniform("fogSpheres", value.Length, value);
			}
		}

		public int FogSphereQuantity
		{
			set
			{
				base.Uniform("fogSphereQuantity", value);
			}
		}
	}
}
