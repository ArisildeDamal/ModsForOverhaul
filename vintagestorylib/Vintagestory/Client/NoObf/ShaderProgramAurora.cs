using System;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class ShaderProgramAurora : ShaderProgram
	{
		public int Tex2D
		{
			set
			{
				base.BindTexture2D("tex", value, 0);
			}
		}

		public float ExtraGodray
		{
			set
			{
				base.Uniform("extraGodray", value);
			}
		}

		public float AlphaTest
		{
			set
			{
				base.Uniform("alphaTest", value);
			}
		}

		public float AuroraCounter
		{
			set
			{
				base.Uniform("auroraCounter", value);
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

		public float ZNear
		{
			set
			{
				base.Uniform("zNear", value);
			}
		}

		public float ZFar
		{
			set
			{
				base.Uniform("zFar", value);
			}
		}

		public Vec3f LightPosition
		{
			set
			{
				base.Uniform("lightPosition", value);
			}
		}

		public float ShadowIntensity
		{
			set
			{
				base.Uniform("shadowIntensity", value);
			}
		}

		public int ShadowMapFar2D
		{
			set
			{
				base.BindTexture2D("shadowMapFar", value, 1);
			}
		}

		public float ShadowMapWidthInv
		{
			set
			{
				base.Uniform("shadowMapWidthInv", value);
			}
		}

		public float ShadowMapHeightInv
		{
			set
			{
				base.Uniform("shadowMapHeightInv", value);
			}
		}

		public int ShadowMapNear2D
		{
			set
			{
				base.BindTexture2D("shadowMapNear", value, 2);
			}
		}

		public float WindWaveCounter
		{
			set
			{
				base.Uniform("windWaveCounter", value);
			}
		}

		public float GlitchStrength
		{
			set
			{
				base.Uniform("glitchStrength", value);
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

		public Vec4f Color
		{
			set
			{
				base.Uniform("color", value);
			}
		}

		public Vec4f RgbaTint
		{
			set
			{
				base.Uniform("rgbaTint", value);
			}
		}

		public Vec3f RgbaAmbientIn
		{
			set
			{
				base.Uniform("rgbaAmbientIn", value);
			}
		}

		public Vec4f RgbaLightIn
		{
			set
			{
				base.Uniform("rgbaLightIn", value);
			}
		}

		public Vec4f RgbaBlockIn
		{
			set
			{
				base.Uniform("rgbaBlockIn", value);
			}
		}

		public Vec4f RgbaFogIn
		{
			set
			{
				base.Uniform("rgbaFogIn", value);
			}
		}

		public float FogMinIn
		{
			set
			{
				base.Uniform("fogMinIn", value);
			}
		}

		public float FogDensityIn
		{
			set
			{
				base.Uniform("fogDensityIn", value);
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
	}
}
