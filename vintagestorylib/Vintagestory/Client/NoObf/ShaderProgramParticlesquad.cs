using System;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class ShaderProgramParticlesquad : ShaderProgram
	{
		public int ParticleTex2D
		{
			set
			{
				base.BindTexture2D("particleTex", value, 0);
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

		public int LiquidDepth2D
		{
			set
			{
				base.BindTexture2D("liquidDepth", value, 3);
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

		public Vec4f RgbaFogIn
		{
			set
			{
				base.Uniform("rgbaFogIn", value);
			}
		}

		public Vec3f RgbaAmbientIn
		{
			set
			{
				base.Uniform("rgbaAmbientIn", value);
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
