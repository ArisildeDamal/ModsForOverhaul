using System;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class ShaderProgramChunkliquiddepth : ShaderProgram
	{
		public Vec3f Origin
		{
			set
			{
				base.Uniform("origin", value);
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

		public float ViewDistance
		{
			set
			{
				base.Uniform("viewDistance", value);
			}
		}

		public float TimeCounter
		{
			set
			{
				base.Uniform("timeCounter", value);
			}
		}

		public float WindWaveCounter
		{
			set
			{
				base.Uniform("windWaveCounter", value);
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
