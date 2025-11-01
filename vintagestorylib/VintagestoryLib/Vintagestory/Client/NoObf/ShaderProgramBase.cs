using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public abstract class ShaderProgramBase : IShaderProgram, IDisposable
	{
		public bool Disposed
		{
			get
			{
				return this.disposed;
			}
		}

		int IShaderProgram.PassId
		{
			get
			{
				return this.PassId;
			}
		}

		string IShaderProgram.PassName
		{
			get
			{
				return this.PassName;
			}
		}

		public bool ClampTexturesToEdge
		{
			get
			{
				return this.clampTToEdge;
			}
			set
			{
				this.clampTToEdge = value;
			}
		}

		IShader IShaderProgram.VertexShader
		{
			get
			{
				return this.VertexShader;
			}
			set
			{
				this.VertexShader = (Shader)value;
			}
		}

		IShader IShaderProgram.FragmentShader
		{
			get
			{
				return this.FragmentShader;
			}
			set
			{
				this.FragmentShader = (Shader)value;
			}
		}

		IShader IShaderProgram.GeometryShader
		{
			get
			{
				return this.GeometryShader;
			}
			set
			{
				this.GeometryShader = (Shader)value;
			}
		}

		public bool LoadError { get; set; }

		public OrderedDictionary<string, UBORef> UBOs
		{
			get
			{
				return this.ubos;
			}
		}

		public string AssetDomain { get; set; }

		int IShaderProgram.ProgramId
		{
			get
			{
				return this.ProgramId;
			}
		}

		public void SetCustomSampler(string uniformName, bool isLinear)
		{
			int samplerId = ScreenManager.Platform.GenSampler(isLinear);
			this.customSamplers.Add(uniformName, samplerId);
		}

		public void Uniform(string uniformName, float value)
		{
			ShaderProgramBase currentShaderProgram = ShaderProgramBase.CurrentShaderProgram;
			int? num = ((currentShaderProgram != null) ? new int?(currentShaderProgram.ProgramId) : null);
			int programId = this.ProgramId;
			if (!((num.GetValueOrDefault() == programId) & (num != null)))
			{
				throw new InvalidOperationException("Can't set uniform on not active shader " + this.PassName + "!");
			}
			GL.Uniform1(this.uniformLocations[uniformName], value);
		}

		public void Uniform(string uniformName, int count, float[] value)
		{
			ShaderProgramBase currentShaderProgram = ShaderProgramBase.CurrentShaderProgram;
			int? num = ((currentShaderProgram != null) ? new int?(currentShaderProgram.ProgramId) : null);
			int programId = this.ProgramId;
			if (!((num.GetValueOrDefault() == programId) & (num != null)))
			{
				throw new InvalidOperationException("Can't set uniform on not active shader " + this.PassName + "!");
			}
			GL.Uniform1(this.uniformLocations[uniformName], count, value);
		}

		public void Uniform(string uniformName, int value)
		{
			ShaderProgramBase currentShaderProgram = ShaderProgramBase.CurrentShaderProgram;
			int? num = ((currentShaderProgram != null) ? new int?(currentShaderProgram.ProgramId) : null);
			int programId = this.ProgramId;
			if (!((num.GetValueOrDefault() == programId) & (num != null)))
			{
				throw new InvalidOperationException("Can't set uniform on not active shader " + this.PassName + "!");
			}
			GL.Uniform1(this.uniformLocations[uniformName], value);
		}

		public void Uniform(string uniformName, Vec2f value)
		{
			ShaderProgramBase currentShaderProgram = ShaderProgramBase.CurrentShaderProgram;
			int? num = ((currentShaderProgram != null) ? new int?(currentShaderProgram.ProgramId) : null);
			int programId = this.ProgramId;
			if (!((num.GetValueOrDefault() == programId) & (num != null)))
			{
				throw new InvalidOperationException("Can't set uniform on not active shader " + this.PassName + "!");
			}
			GL.Uniform2(this.uniformLocations[uniformName], value.X, value.Y);
		}

		public void Uniform(string uniformName, Vec3f value)
		{
			ShaderProgramBase currentShaderProgram = ShaderProgramBase.CurrentShaderProgram;
			int? num = ((currentShaderProgram != null) ? new int?(currentShaderProgram.ProgramId) : null);
			int programId = this.ProgramId;
			if (!((num.GetValueOrDefault() == programId) & (num != null)))
			{
				throw new InvalidOperationException("Can't set uniform on not active shader " + this.PassName + "!");
			}
			GL.Uniform3(this.uniformLocations[uniformName], value.X, value.Y, value.Z);
		}

		public void Uniform(string uniformName, Vec3i value)
		{
			ShaderProgramBase currentShaderProgram = ShaderProgramBase.CurrentShaderProgram;
			int? num = ((currentShaderProgram != null) ? new int?(currentShaderProgram.ProgramId) : null);
			int programId = this.ProgramId;
			if (!((num.GetValueOrDefault() == programId) & (num != null)))
			{
				throw new InvalidOperationException("Can't set uniform on not active shader " + this.PassName + "!");
			}
			GL.Uniform3(this.uniformLocations[uniformName], value.X, value.Y, value.Z);
		}

		public void Uniforms2(string uniformName, int count, float[] values)
		{
			ShaderProgramBase currentShaderProgram = ShaderProgramBase.CurrentShaderProgram;
			int? num = ((currentShaderProgram != null) ? new int?(currentShaderProgram.ProgramId) : null);
			int programId = this.ProgramId;
			if (!((num.GetValueOrDefault() == programId) & (num != null)))
			{
				throw new InvalidOperationException("Can't set uniform on not active shader " + this.PassName + "!");
			}
			GL.Uniform2(this.uniformLocations[uniformName], count, values);
		}

		public void Uniforms3(string uniformName, int count, float[] values)
		{
			ShaderProgramBase currentShaderProgram = ShaderProgramBase.CurrentShaderProgram;
			int? num = ((currentShaderProgram != null) ? new int?(currentShaderProgram.ProgramId) : null);
			int programId = this.ProgramId;
			if (!((num.GetValueOrDefault() == programId) & (num != null)))
			{
				throw new InvalidOperationException("Can't set uniform on not active shader " + this.PassName + "!");
			}
			GL.Uniform3(this.uniformLocations[uniformName], count, values);
		}

		public void Uniform(string uniformName, Vec4f value)
		{
			ShaderProgramBase currentShaderProgram = ShaderProgramBase.CurrentShaderProgram;
			int? num = ((currentShaderProgram != null) ? new int?(currentShaderProgram.ProgramId) : null);
			int programId = this.ProgramId;
			if (!((num.GetValueOrDefault() == programId) & (num != null)))
			{
				throw new InvalidOperationException("Can't set uniform on not active shader " + this.PassName + "!");
			}
			GL.Uniform4(this.uniformLocations[uniformName], value.X, value.Y, value.Z, value.W);
		}

		public void Uniforms4(string uniformName, int count, float[] values)
		{
			ShaderProgramBase currentShaderProgram = ShaderProgramBase.CurrentShaderProgram;
			int? num = ((currentShaderProgram != null) ? new int?(currentShaderProgram.ProgramId) : null);
			int programId = this.ProgramId;
			if (!((num.GetValueOrDefault() == programId) & (num != null)))
			{
				throw new InvalidOperationException("Can't set uniform on not active shader " + this.PassName + "!");
			}
			GL.Uniform4(this.uniformLocations[uniformName], count, values);
		}

		public void UniformMatrix(string uniformName, float[] matrix)
		{
			ShaderProgramBase currentShaderProgram = ShaderProgramBase.CurrentShaderProgram;
			int? num = ((currentShaderProgram != null) ? new int?(currentShaderProgram.ProgramId) : null);
			int programId = this.ProgramId;
			if (!((num.GetValueOrDefault() == programId) & (num != null)))
			{
				throw new InvalidOperationException("Can't set uniform on not active shader " + this.PassName + "!");
			}
			GL.UniformMatrix4(this.uniformLocations[uniformName], 1, false, matrix);
		}

		public void UniformMatrix(string uniformName, ref Matrix4 matrix)
		{
			ShaderProgramBase currentShaderProgram = ShaderProgramBase.CurrentShaderProgram;
			int? num = ((currentShaderProgram != null) ? new int?(currentShaderProgram.ProgramId) : null);
			int programId = this.ProgramId;
			if (!((num.GetValueOrDefault() == programId) & (num != null)))
			{
				throw new InvalidOperationException("Can't set uniform on not active shader " + this.PassName + "!");
			}
			GL.UniformMatrix4(this.uniformLocations[uniformName], false, ref matrix);
		}

		public bool HasUniform(string uniformName)
		{
			return this.uniformLocations.ContainsKey(uniformName);
		}

		public void BindTexture2D(string samplerName, int textureId, int textureNumber)
		{
			GL.Uniform1(this.uniformLocations[samplerName], textureNumber);
			GL.ActiveTexture(TextureUnit.Texture0 + textureNumber);
			GL.BindTexture(TextureTarget.Texture2D, textureId);
			int sampler;
			if (this.customSamplers.TryGetValue(samplerName, out sampler))
			{
				GL.BindSampler(textureNumber, sampler);
			}
			if (this.clampTToEdge)
			{
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, Convert.ToInt32(TextureWrapMode.ClampToEdge));
			}
		}

		public void BindTexture2D(string samplerName, int textureId)
		{
			this.BindTexture2D(samplerName, textureId, this.textureLocations[samplerName]);
		}

		public void BindTextureCube(string samplerName, int textureId, int textureNumber)
		{
			GL.Uniform1(this.uniformLocations[samplerName], textureNumber);
			GL.ActiveTexture(TextureUnit.Texture0 + textureNumber);
			GL.BindTexture(TextureTarget.TextureCubeMap, textureId);
			if (this.clampTToEdge)
			{
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, Convert.ToInt32(TextureWrapMode.ClampToEdge));
			}
		}

		public void UniformMatrices4x3(string uniformName, int count, float[] matrix)
		{
			GL.UniformMatrix4x3(this.uniformLocations[uniformName], count, false, matrix);
		}

		public void UniformMatrices(string uniformName, int count, float[] matrix)
		{
			GL.UniformMatrix4(this.uniformLocations[uniformName], count, false, matrix);
		}

		public void Use()
		{
			if (ShaderProgramBase.CurrentShaderProgram != null && ShaderProgramBase.CurrentShaderProgram != this)
			{
				throw new InvalidOperationException("Already a different shader (" + ShaderProgramBase.CurrentShaderProgram.PassName + ") in use!");
			}
			if (this.disposed)
			{
				throw new InvalidOperationException("Can't use a disposed shader!");
			}
			GL.UseProgram(this.ProgramId);
			ShaderProgramBase.CurrentShaderProgram = this;
			DefaultShaderUniforms shUniforms = ScreenManager.Platform.ShaderUniforms;
			if (this.includes.Contains("fogandlight.fsh"))
			{
				this.Uniform("zNear", shUniforms.ZNear);
				this.Uniform("zFar", shUniforms.ZFar);
				this.Uniform("lightPosition", shUniforms.LightPosition3D);
				this.Uniform("shadowIntensity", shUniforms.DropShadowIntensity);
				this.Uniform("glitchStrength", shUniforms.GlitchStrength);
				if (ShaderProgramBase.shadowmapQuality > 0)
				{
					FrameBufferRef farFb = ScreenManager.Platform.FrameBuffers[11];
					FrameBufferRef nearFb = ScreenManager.Platform.FrameBuffers[12];
					this.BindTexture2D("shadowMapFar", farFb.DepthTextureId);
					this.BindTexture2D("shadowMapNear", nearFb.DepthTextureId);
					this.Uniform("shadowMapWidthInv", 1f / (float)farFb.Width);
					this.Uniform("shadowMapHeightInv", 1f / (float)farFb.Height);
					this.Uniform("viewDistance", (float)ClientSettings.ViewDistance);
					this.Uniform("viewDistanceLod0", (float)Math.Min(640, ClientSettings.ViewDistance) * ClientSettings.LodBias);
				}
			}
			if (this.includes.Contains("fogandlight.vsh"))
			{
				int fcnt = shUniforms.FogSphereQuantity;
				this.Uniform("fogSphereQuantity", fcnt);
				this.Uniform("fogSpheres", fcnt * 8, shUniforms.FogSpheres);
				int cnt = shUniforms.PointLightsCount;
				this.Uniform("pointLightQuantity", cnt);
				this.Uniforms3("pointLights", cnt, shUniforms.PointLights3);
				this.Uniforms3("pointLightColors", cnt, shUniforms.PointLightColors3);
				this.Uniform("flatFogDensity", shUniforms.FlagFogDensity);
				this.Uniform("flatFogStart", shUniforms.FlatFogStartYPos - shUniforms.PlayerPos.Y);
				this.Uniform("glitchStrengthFL", shUniforms.GlitchStrength);
				this.Uniform("viewDistance", (float)ClientSettings.ViewDistance);
				this.Uniform("viewDistanceLod0", (float)Math.Min(640, ClientSettings.ViewDistance) * ClientSettings.LodBias);
				this.Uniform("nightVisionStrength", shUniforms.NightVisionStrength);
			}
			if (this.includes.Contains("shadowcoords.vsh"))
			{
				this.Uniform("shadowRangeNear", shUniforms.ShadowRangeNear);
				this.Uniform("shadowRangeFar", shUniforms.ShadowRangeFar);
				this.UniformMatrix("toShadowMapSpaceMatrixNear", shUniforms.ToShadowMapSpaceMatrixNear);
				this.UniformMatrix("toShadowMapSpaceMatrixFar", shUniforms.ToShadowMapSpaceMatrixFar);
			}
			if (this.includes.Contains("vertexwarp.vsh"))
			{
				this.Uniform("timeCounter", shUniforms.TimeCounter);
				this.Uniform("windWaveCounter", shUniforms.WindWaveCounter);
				this.Uniform("windWaveCounterHighFreq", shUniforms.WindWaveCounterHighFreq);
				this.Uniform("windSpeed", shUniforms.WindSpeed);
				this.Uniform("waterWaveCounter", shUniforms.WaterWaveCounter);
				this.Uniform("playerpos", shUniforms.PlayerPos);
				this.Uniform("globalWarpIntensity", shUniforms.GlobalWorldWarp);
				this.Uniform("glitchWaviness", shUniforms.GlitchWaviness);
				this.Uniform("windWaveIntensity", shUniforms.WindWaveIntensity);
				this.Uniform("waterWaveIntensity", shUniforms.WaterWaveIntensity);
				this.Uniform("perceptionEffectId", shUniforms.PerceptionEffectId);
				this.Uniform("perceptionEffectIntensity", shUniforms.PerceptionEffectIntensity);
			}
			if (this.includes.Contains("skycolor.fsh"))
			{
				this.Uniform("fogWaveCounter", shUniforms.FogWaveCounter);
				this.BindTexture2D("sky", shUniforms.SkyTextureId);
				this.BindTexture2D("glow", shUniforms.GlowTextureId);
				this.Uniform("sunsetMod", shUniforms.SunsetMod);
				this.Uniform("ditherSeed", shUniforms.DitherSeed);
				this.Uniform("horizontalResolution", shUniforms.FrameWidth);
				this.Uniform("playerToSealevelOffset", shUniforms.PlayerToSealevelOffset);
			}
			if (this.includes.Contains("colormap.vsh"))
			{
				this.Uniforms4("colorMapRects", 40, shUniforms.ColorMapRects4);
				this.Uniform("seasonRel", shUniforms.SeasonRel);
				this.Uniform("seaLevel", shUniforms.SeaLevel);
				this.Uniform("atlasHeight", shUniforms.BlockAtlasHeight);
				this.Uniform("seasonTemperature", shUniforms.SeasonTemperature);
			}
			if (this.includes.Contains("underwatereffects.fsh"))
			{
				FrameBufferRef fb = ScreenManager.Platform.FrameBuffers[5];
				this.BindTexture2D("liquidDepth", fb.DepthTextureId);
				this.Uniform("cameraUnderwater", shUniforms.CameraUnderwater);
				this.Uniform("waterMurkColor", shUniforms.WaterMurkColor);
				FrameBufferRef pfb = ScreenManager.Platform.FrameBuffers[0];
				this.Uniform("frameSize", new Vec2f((float)pfb.Width, (float)pfb.Height));
			}
			if (this == ShaderPrograms.Gui)
			{
				ShaderPrograms.Gui.LightPosition = new Vec3f(1f, -1f, 0f).Normalize();
			}
			foreach (KeyValuePair<string, UBORef> val in this.ubos)
			{
				val.Value.Bind();
			}
		}

		public void Stop()
		{
			GL.UseProgram(0);
			for (int i = 0; i < this.customSamplers.Count; i++)
			{
				GL.BindSampler(i, 0);
			}
			foreach (KeyValuePair<string, UBORef> val in this.ubos)
			{
				val.Value.Unbind();
			}
			ShaderProgramBase.CurrentShaderProgram = null;
		}

		public void Dispose()
		{
			if (this.disposed)
			{
				return;
			}
			this.disposed = true;
			if (this.VertexShader != null)
			{
				GL.DetachShader(this.ProgramId, this.VertexShader.ShaderId);
				GL.DeleteShader(this.VertexShader.ShaderId);
			}
			if (this.FragmentShader != null)
			{
				GL.DetachShader(this.ProgramId, this.FragmentShader.ShaderId);
				GL.DeleteShader(this.FragmentShader.ShaderId);
			}
			if (this.GeometryShader != null)
			{
				GL.DetachShader(this.ProgramId, this.GeometryShader.ShaderId);
				GL.DeleteShader(this.GeometryShader.ShaderId);
			}
			foreach (KeyValuePair<string, int> val in this.customSamplers)
			{
				GL.DeleteSampler(val.Value);
			}
			GL.DeleteProgram(this.ProgramId);
		}

		public abstract bool Compile();

		public static int shadowmapQuality;

		public static ShaderProgramBase CurrentShaderProgram;

		public int PassId;

		public int ProgramId;

		public string PassName;

		public Shader VertexShader;

		public Shader GeometryShader;

		public Shader FragmentShader;

		public Dictionary<string, int> uniformLocations = new Dictionary<string, int>();

		public Dictionary<string, int> textureLocations = new Dictionary<string, int>();

		public OrderedDictionary<string, UBORef> ubos = new OrderedDictionary<string, UBORef>();

		public bool clampTToEdge;

		public HashSet<string> includes = new HashSet<string>();

		public Dictionary<string, int> customSamplers = new Dictionary<string, int>();

		private bool disposed;
	}
}
