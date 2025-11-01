using System;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class ShaderProgramStandard : ShaderProgram, IStandardShaderProgram, IShaderProgram, IDisposable
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

		public float SsaoAttn
		{
			set
			{
				base.Uniform("ssaoAttn", value);
			}
		}

		public int ApplySsao
		{
			set
			{
				base.Uniform("applySsao", value);
			}
		}

		public int TempGlowMode
		{
			set
			{
				base.Uniform("tempGlowMode", value);
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

		public int NormalShaded
		{
			set
			{
				base.Uniform("normalShaded", value);
			}
		}

		public int SkyShaded
		{
			set
			{
				base.Uniform("skyShaded", value);
			}
		}

		public float DamageEffect
		{
			set
			{
				base.Uniform("damageEffect", value);
			}
		}

		public float DepthOffset
		{
			set
			{
				base.Uniform("depthOffset", value);
			}
		}

		public Vec4f AverageColor
		{
			set
			{
				base.Uniform("averageColor", value);
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
				base.BindTexture2D("shadowMapFar", value, 2);
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
				base.BindTexture2D("shadowMapNear", value, 3);
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

		public int LiquidDepth2D
		{
			set
			{
				base.BindTexture2D("liquidDepth", value, 4);
			}
		}

		public float CameraUnderwater
		{
			set
			{
				base.Uniform("cameraUnderwater", value);
			}
		}

		public Vec2f FrameSize
		{
			set
			{
				base.Uniform("frameSize", value);
			}
		}

		public Vec4f WaterMurkColor
		{
			set
			{
				base.Uniform("waterMurkColor", value);
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

		public Vec4f RgbaGlowIn
		{
			set
			{
				base.Uniform("rgbaGlowIn", value);
			}
		}

		public Vec4f RgbaFogIn
		{
			set
			{
				base.Uniform("rgbaFogIn", value);
			}
		}

		public int ExtraGlow
		{
			set
			{
				base.Uniform("extraGlow", value);
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

		public float[] ViewMatrix
		{
			set
			{
				base.UniformMatrix("viewMatrix", value);
			}
		}

		public float[] ModelMatrix
		{
			set
			{
				base.UniformMatrix("modelMatrix", value);
			}
		}

		public int DontWarpVertices
		{
			set
			{
				base.Uniform("dontWarpVertices", value);
			}
		}

		public int FadeFromSpheresFog
		{
			set
			{
				base.Uniform("fadeFromSpheresFog", value);
			}
		}

		public int AddRenderFlags
		{
			set
			{
				base.Uniform("addRenderFlags", value);
			}
		}

		public float ExtraZOffset
		{
			set
			{
				base.Uniform("extraZOffset", value);
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

		public float TimeCounter
		{
			set
			{
				base.Uniform("timeCounter", value);
			}
		}

		public float WindWaveCounterHighFreq
		{
			set
			{
				base.Uniform("windWaveCounterHighFreq", value);
			}
		}

		public float WaterWaveCounter
		{
			set
			{
				base.Uniform("waterWaveCounter", value);
			}
		}

		public float WindSpeed
		{
			set
			{
				base.Uniform("windSpeed", value);
			}
		}

		public Vec3f Playerpos
		{
			set
			{
				base.Uniform("playerpos", value);
			}
		}

		public float GlobalWarpIntensity
		{
			set
			{
				base.Uniform("globalWarpIntensity", value);
			}
		}

		public float GlitchWaviness
		{
			set
			{
				base.Uniform("glitchWaviness", value);
			}
		}

		public float WindWaveIntensity
		{
			set
			{
				base.Uniform("windWaveIntensity", value);
			}
		}

		public float WaterWaveIntensity
		{
			set
			{
				base.Uniform("waterWaveIntensity", value);
			}
		}

		public int PerceptionEffectId
		{
			set
			{
				base.Uniform("perceptionEffectId", value);
			}
		}

		public float PerceptionEffectIntensity
		{
			set
			{
				base.Uniform("perceptionEffectIntensity", value);
			}
		}
	}
}
