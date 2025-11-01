using System;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class ShaderProgramEntityanimated : ShaderProgram
	{
		public int EntityTex2D
		{
			set
			{
				base.BindTexture2D("entityTex", value, 0);
			}
		}

		public float AlphaTest
		{
			set
			{
				base.Uniform("alphaTest", value);
			}
		}

		public float GlitchEffectStrength
		{
			set
			{
				base.Uniform("glitchEffectStrength", value);
			}
		}

		public int EntityId
		{
			set
			{
				base.Uniform("entityId", value);
			}
		}

		public int GlitchFlicker
		{
			set
			{
				base.Uniform("glitchFlicker", value);
			}
		}

		public float DepthOffset
		{
			set
			{
				base.Uniform("depthOffset", value);
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

		public Vec4f RenderColor
		{
			set
			{
				base.Uniform("renderColor", value);
			}
		}

		public int AddRenderFlags
		{
			set
			{
				base.Uniform("addRenderFlags", value);
			}
		}

		public float FrostAlpha
		{
			set
			{
				base.Uniform("frostAlpha", value);
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

		public int ExtraGlow
		{
			set
			{
				base.Uniform("extraGlow", value);
			}
		}

		public int SkipRenderJointId
		{
			set
			{
				base.Uniform("skipRenderJointId", value);
			}
		}

		public int SkipRenderJointId2
		{
			set
			{
				base.Uniform("skipRenderJointId2", value);
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

		public override bool Compile()
		{
			bool flag = base.Compile();
			if (flag)
			{
				this.initUbos();
			}
			return flag;
		}

		public void initUbos()
		{
			foreach (UBORef uboref in this.ubos.Values)
			{
				uboref.Dispose();
			}
			this.ubos.Clear();
			this.ubos["Animation"] = ScreenManager.Platform.CreateUBO(this.ProgramId, 0, "Animation", GlobalConstants.MaxAnimatedElements * 16 * 4);
		}
	}
}
