using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Vintagestory.API.Client;

namespace Vintagestory.Client.NoObf
{
	public class ShaderProgram : ShaderProgramBase, IShaderProgram, IDisposable
	{
		public override bool Compile()
		{
			bool ok = true;
			HashSet<string> uniformNames = new HashSet<string>();
			Shader vertexShader = this.VertexShader;
			if (vertexShader != null)
			{
				vertexShader.EnsureVersionSupported();
			}
			Shader geometryShader = this.GeometryShader;
			if (geometryShader != null)
			{
				geometryShader.EnsureVersionSupported();
			}
			Shader fragmentShader = this.FragmentShader;
			if (fragmentShader != null)
			{
				fragmentShader.EnsureVersionSupported();
			}
			if (this.VertexShader != null)
			{
				ok = ok && this.VertexShader.Compile();
				this.collectUniformNames(this.VertexShader.Code, uniformNames);
			}
			if (this.FragmentShader != null)
			{
				ok = ok && this.FragmentShader.Compile();
				this.collectUniformNames(this.FragmentShader.Code, uniformNames);
			}
			if (this.GeometryShader != null)
			{
				ok = ok && this.GeometryShader.Compile();
				this.collectUniformNames(this.GeometryShader.Code, uniformNames);
			}
			ok = ok && ScreenManager.Platform.CreateShaderProgram(this);
			string notFoundUniforms = "";
			foreach (string uniformName in uniformNames)
			{
				this.uniformLocations[uniformName] = ScreenManager.Platform.GetUniformLocation(this, uniformName);
				if (this.uniformLocations[uniformName] == -1)
				{
					if (notFoundUniforms.Length > 0)
					{
						notFoundUniforms += ", ";
					}
					notFoundUniforms += uniformName;
				}
			}
			if (notFoundUniforms.Length > 0 && ScreenManager.Platform.GlDebugMode)
			{
				ScreenManager.Platform.Logger.Notification("Shader {0}: Uniform locations for variables {1} not found (or not used).", new object[] { this.PassName, notFoundUniforms });
			}
			return ok;
		}

		private void collectUniformNames(string code, HashSet<string> list)
		{
			foreach (object obj in Regex.Matches(code, "(\\s|\\r\\n)uniform\\s*(?<type>float|int|ivec2|ivec3|ivec4|vec2|vec3|vec4|sampler2DShadow|sampler2D|samplerCube|mat3|mat4x3|mat4)\\s*(\\[[\\d\\w]+\\])?\\s*(?<var>[\\d\\w]+)", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture))
			{
				Match match = (Match)obj;
				string varname = match.Groups["var"].Value;
				list.Add(varname);
				if (match.Groups["type"].ToString().Contains("sampler"))
				{
					this.textureLocations[varname] = this.textureLocations.Count;
				}
			}
		}

		public Dictionary<int, string> attributes = new Dictionary<int, string>();

		public bool LoadFromFile = true;
	}
}
