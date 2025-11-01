using System;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class ShaderProgramCloudmap : ShaderProgram
	{
		public float DayLight
		{
			set
			{
				base.Uniform("dayLight", value);
			}
		}

		public float GlobalCloudBrightness
		{
			set
			{
				base.Uniform("globalCloudBrightness", value);
			}
		}

		public float Time
		{
			set
			{
				base.Uniform("time", value);
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

		public Vec3f SunPosition
		{
			set
			{
				base.Uniform("sunPosition", value);
			}
		}

		public float NightVisionStrength
		{
			set
			{
				base.Uniform("nightVisionStrength", value);
			}
		}

		public float Alpha
		{
			set
			{
				base.Uniform("alpha", value);
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
				base.BindTexture2D("shadowMapFar", value, 0);
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
				base.BindTexture2D("shadowMapNear", value, 1);
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

		public float PlayerToSealevelOffset
		{
			set
			{
				base.Uniform("playerToSealevelOffset", value);
			}
		}

		public int DitherSeed
		{
			set
			{
				base.Uniform("ditherSeed", value);
			}
		}

		public int HorizontalResolution
		{
			set
			{
				base.Uniform("horizontalResolution", value);
			}
		}

		public float FogWaveCounter
		{
			set
			{
				base.Uniform("fogWaveCounter", value);
			}
		}

		public int Glow2D
		{
			set
			{
				base.BindTexture2D("glow", value, 2);
			}
		}

		public int Sky2D
		{
			set
			{
				base.BindTexture2D("sky", value, 3);
			}
		}

		public float SunsetMod
		{
			set
			{
				base.Uniform("sunsetMod", value);
			}
		}

		public float Width
		{
			set
			{
				base.Uniform("width", value);
			}
		}

		public int MapData12D
		{
			set
			{
				base.BindTexture2D("mapData1", value, 4);
			}
		}

		public int MapData22D
		{
			set
			{
				base.BindTexture2D("mapData2", value, 5);
			}
		}

		public Vec3f MapOffset
		{
			set
			{
				base.Uniform("mapOffset", value);
			}
		}

		public Vec2f MapOffsetCentre
		{
			set
			{
				base.Uniform("mapOffsetCentre", value);
			}
		}

		public float[] ViewMatrix
		{
			set
			{
				base.UniformMatrix("viewMatrix", value);
			}
		}

		public int PointLightQuantity
		{
			set
			{
				base.Uniform("pointLightQuantity", value);
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
	}
}
