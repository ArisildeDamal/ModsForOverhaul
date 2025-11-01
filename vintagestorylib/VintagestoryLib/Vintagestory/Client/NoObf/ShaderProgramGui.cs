using System;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class ShaderProgramGui : ShaderProgram
	{
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

		public int DarkEdges
		{
			set
			{
				base.Uniform("darkEdges", value);
			}
		}

		public int TempGlowMode
		{
			set
			{
				base.Uniform("tempGlowMode", value);
			}
		}

		public int TransparentCenter
		{
			set
			{
				base.Uniform("transparentCenter", value);
			}
		}

		public int Tex2d2D
		{
			set
			{
				base.BindTexture2D("tex2d", value, 0);
			}
		}

		public int NormalShaded
		{
			set
			{
				base.Uniform("normalShaded", value);
			}
		}

		public float SepiaLevel
		{
			set
			{
				base.Uniform("sepiaLevel", value);
			}
		}

		public float DamageEffect
		{
			set
			{
				base.Uniform("damageEffect", value);
			}
		}

		public int Tex2dOverlay2D
		{
			set
			{
				base.BindTexture2D("tex2dOverlay", value, 1);
			}
		}

		public float OverlayOpacity
		{
			set
			{
				base.Uniform("overlayOpacity", value);
			}
		}

		public Vec2f OverlayTextureSize
		{
			set
			{
				base.Uniform("overlayTextureSize", value);
			}
		}

		public Vec2f BaseTextureSize
		{
			set
			{
				base.Uniform("baseTextureSize", value);
			}
		}

		public Vec2f BaseUvOrigin
		{
			set
			{
				base.Uniform("baseUvOrigin", value);
			}
		}

		public Vec3f LightPosition
		{
			set
			{
				base.Uniform("lightPosition", value);
			}
		}

		public Vec4f RgbaIn
		{
			set
			{
				base.Uniform("rgbaIn", value);
			}
		}

		public Vec4f RgbaGlowIn
		{
			set
			{
				base.Uniform("rgbaGlowIn", value);
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

		public float[] ModelMatrix
		{
			set
			{
				base.UniformMatrix("modelMatrix", value);
			}
		}

		public int ApplyModelMat
		{
			set
			{
				base.Uniform("applyModelMat", value);
			}
		}

		public int ApplyColor
		{
			set
			{
				base.Uniform("applyColor", value);
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
