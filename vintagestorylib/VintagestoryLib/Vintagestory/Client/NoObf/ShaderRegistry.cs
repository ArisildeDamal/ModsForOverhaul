using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf
{
	public class ShaderRegistry
	{
		private static void registerDefaultShaderPrograms()
		{
			ShaderRegistry.RegisterShaderProgram(EnumShaderProgram.Autocamera, ShaderPrograms.Autocamera = new ShaderProgramAutocamera());
			ShaderRegistry.RegisterShaderProgram(EnumShaderProgram.Bilateralblur, ShaderPrograms.Bilateralblur = new ShaderProgramBilateralblur());
			ShaderRegistry.RegisterShaderProgram(EnumShaderProgram.Blit, ShaderPrograms.Blit = new ShaderProgramBlit());
			ShaderRegistry.RegisterShaderProgram(EnumShaderProgram.Blockhighlights, ShaderPrograms.Blockhighlights = new ShaderProgramBlockhighlights());
			ShaderRegistry.RegisterShaderProgram(EnumShaderProgram.Blur, ShaderPrograms.Blur = new ShaderProgramBlur());
			ShaderRegistry.RegisterShaderProgram(EnumShaderProgram.Celestialobject, ShaderPrograms.Celestialobject = new ShaderProgramCelestialobject());
			ShaderRegistry.RegisterShaderProgram(EnumShaderProgram.Chunkliquid, ShaderPrograms.Chunkliquid = new ShaderProgramChunkliquid());
			ShaderRegistry.RegisterShaderProgram(EnumShaderProgram.Chunkliquiddepth, ShaderPrograms.Chunkliquiddepth = new ShaderProgramChunkliquiddepth());
			ShaderRegistry.RegisterShaderProgram(EnumShaderProgram.Chunkopaque, ShaderPrograms.Chunkopaque = new ShaderProgramChunkopaque());
			ShaderRegistry.RegisterShaderProgram(EnumShaderProgram.Chunktopsoil, ShaderPrograms.Chunktopsoil = new ShaderProgramChunktopsoil());
			ShaderRegistry.RegisterShaderProgram(EnumShaderProgram.Chunktransparent, ShaderPrograms.Chunktransparent = new ShaderProgramChunktransparent());
			ShaderRegistry.RegisterShaderProgram(EnumShaderProgram.Colorgrade, ShaderPrograms.Colorgrade = new ShaderProgramColorgrade());
			ShaderRegistry.RegisterShaderProgram(EnumShaderProgram.Debugdepthbuffer, ShaderPrograms.Debugdepthbuffer = new ShaderProgramDebugdepthbuffer());
			ShaderRegistry.RegisterShaderProgram(EnumShaderProgram.Decals, ShaderPrograms.Decals = new ShaderProgramDecals());
			ShaderRegistry.RegisterShaderProgram(EnumShaderProgram.Entityanimated, ShaderPrograms.Entityanimated = new ShaderProgramEntityanimated());
			ShaderRegistry.RegisterShaderProgram(EnumShaderProgram.Final, ShaderPrograms.Final = new ShaderProgramFinal());
			ShaderRegistry.RegisterShaderProgram(EnumShaderProgram.Findbright, ShaderPrograms.Findbright = new ShaderProgramFindbright());
			ShaderRegistry.RegisterShaderProgram(EnumShaderProgram.Godrays, ShaderPrograms.Godrays = new ShaderProgramGodrays());
			ShaderRegistry.RegisterShaderProgram(EnumShaderProgram.Gui, ShaderPrograms.Gui = new ShaderProgramGui());
			ShaderRegistry.RegisterShaderProgram(EnumShaderProgram.Guigear, ShaderPrograms.Guigear = new ShaderProgramGuigear());
			ShaderRegistry.RegisterShaderProgram(EnumShaderProgram.Guitopsoil, ShaderPrograms.Guitopsoil = new ShaderProgramGuitopsoil());
			ShaderRegistry.RegisterShaderProgram(EnumShaderProgram.Helditem, ShaderPrograms.Helditem = new ShaderProgramHelditem());
			ShaderRegistry.RegisterShaderProgram(EnumShaderProgram.Luma, ShaderPrograms.Luma = new ShaderProgramLuma());
			ShaderRegistry.RegisterShaderProgram(EnumShaderProgram.Nightsky, ShaderPrograms.Nightsky = new ShaderProgramNightsky());
			ShaderRegistry.RegisterShaderProgram(EnumShaderProgram.Particlescube, ShaderPrograms.Particlescube = new ShaderProgramParticlescube());
			ShaderRegistry.RegisterShaderProgram(EnumShaderProgram.Particlesquad, ShaderPrograms.Particlesquad = new ShaderProgramParticlesquad());
			ShaderRegistry.RegisterShaderProgram(EnumShaderProgram.Particlesquad2d, ShaderPrograms.Particlesquad2d = new ShaderProgramParticlesquad2d());
			ShaderRegistry.RegisterShaderProgram(EnumShaderProgram.Shadowmapentityanimated, ShaderPrograms.Shadowmapentityanimated = new ShaderProgramShadowmapentityanimated());
			ShaderRegistry.RegisterShaderProgram(EnumShaderProgram.Chunkshadowmap, ShaderPrograms.Chunkshadowmap = new ShaderProgramShadowmapgeneric());
			ShaderRegistry.RegisterShaderProgram(EnumShaderProgram.Sky, ShaderPrograms.Sky = new ShaderProgramSky());
			ShaderRegistry.RegisterShaderProgram(EnumShaderProgram.Ssao, ShaderPrograms.Ssao = new ShaderProgramSsao());
			ShaderRegistry.RegisterShaderProgram(EnumShaderProgram.Standard, ShaderPrograms.Standard = new ShaderProgramStandard());
			ShaderRegistry.RegisterShaderProgram(EnumShaderProgram.Texture2texture, ShaderPrograms.Texture2texture = new ShaderProgramTexture2texture());
			ShaderRegistry.RegisterShaderProgram(EnumShaderProgram.Transparentcompose, ShaderPrograms.Transparentcompose = new ShaderProgramTransparentcompose());
			ShaderRegistry.RegisterShaderProgram(EnumShaderProgram.Wireframe, ShaderPrograms.Wireframe = new ShaderProgramWireframe());
			ShaderRegistry.RegisterShaderProgram(EnumShaderProgram.Woittest, ShaderPrograms.Woittest = new ShaderProgramWoittest());
		}

		static ShaderRegistry()
		{
			ShaderRegistry.registerDefaultShaderPrograms();
			ShaderRegistry.nextPassId++;
		}

		public static int RegisterShaderProgram(string name, ShaderProgram program)
		{
			int passid = ShaderRegistry.nextPassId;
			if (ShaderRegistry.shaderIdsByName.ContainsKey(name))
			{
				passid = ShaderRegistry.shaderIdsByName[name];
			}
			else
			{
				ShaderRegistry.nextPassId++;
			}
			program.PassId = passid;
			program.PassName = name;
			ShaderRegistry.shaderNames[passid] = name;
			ShaderRegistry.shaderPrograms[passid] = program;
			ShaderRegistry.shaderIdsByName[name] = passid;
			ShaderRegistry.LoadShaderProgram(program, ScreenManager.Platform.UseSSBOs);
			return program.PassId;
		}

		public static void RegisterShaderProgram(EnumShaderProgram defaultProgram, ShaderProgram program)
		{
			program.PassId = (int)defaultProgram;
			string filename = defaultProgram.ToString().ToLowerInvariant();
			ShaderRegistry.shaderNames[(int)defaultProgram] = filename;
			ShaderRegistry.shaderPrograms[(int)defaultProgram] = program;
			ShaderRegistry.nextPassId = Math.Max((int)(defaultProgram + 1), ShaderRegistry.nextPassId);
			int i = filename.IndexOf('_');
			if (i > 0)
			{
				filename = filename.Substring(0, i);
			}
			program.PassName = filename;
		}

		public static ShaderProgram getProgram(EnumShaderProgram renderPass)
		{
			return ShaderRegistry.shaderPrograms[(int)renderPass];
		}

		public static ShaderProgram getProgram(int renderPass)
		{
			return ShaderRegistry.shaderPrograms[renderPass];
		}

		public static ShaderProgram getProgramByName(string shadername)
		{
			int id;
			if (ShaderRegistry.shaderIdsByName.TryGetValue(shadername, out id))
			{
				return ShaderRegistry.shaderPrograms[id];
			}
			return null;
		}

		public static void Load()
		{
			ShaderRegistry.loadRegisteredShaderPrograms();
		}

		public static bool ReloadShaders()
		{
			ScreenManager.Platform.AssetManager.Reload(AssetCategory.shaders);
			ScreenManager.Platform.AssetManager.Reload(AssetCategory.shaderincludes);
			for (int i = 0; i < ShaderRegistry.shaderPrograms.Length; i++)
			{
				if (ShaderRegistry.shaderPrograms[i] != null)
				{
					ShaderRegistry.shaderPrograms[i].Dispose();
					ShaderRegistry.shaderPrograms[i] = null;
				}
			}
			ShaderRegistry.registerDefaultShaderPrograms();
			bool flag = ShaderRegistry.loadRegisteredShaderPrograms();
			if (ScreenManager.Platform.UseSSBOs)
			{
				ShaderRegistry.RegisterShaderProgram(EnumShaderProgram.Chunkshadowmap_NoSSBOs, new ShaderProgramShadowmapgeneric());
				ShaderProgram program = ShaderRegistry.shaderPrograms[42];
				if (program != null)
				{
					ShaderRegistry.LoadShaderProgram(program, false);
					program.Compile();
				}
			}
			return flag;
		}

		private static bool loadRegisteredShaderPrograms()
		{
			ScreenManager.Platform.Logger.Notification("Loading shaders...");
			bool ok = true;
			AssetManager assetManager = ScreenManager.Platform.AssetManager;
			List<IAsset> many = ScreenManager.Platform.AssetManager.GetMany(AssetCategory.shaderincludes, true);
			many.AddRange(ScreenManager.Platform.AssetManager.GetMany(AssetCategory.shaders, true));
			foreach (IAsset asset in many)
			{
				ShaderRegistry.includes[asset.Name] = asset.ToText();
			}
			for (int i = 0; i < ShaderRegistry.nextPassId; i++)
			{
				ShaderProgram program = ShaderRegistry.shaderPrograms[i];
				if (program != null)
				{
					ShaderRegistry.LoadShaderProgram(program, ScreenManager.Platform.UseSSBOs);
					ok = program.Compile() && ok;
				}
			}
			ShaderPrograms.Chunkopaque.SetCustomSampler("terrainTex", false);
			ShaderPrograms.Chunkopaque.SetCustomSampler("terrainTexLinear", true);
			ShaderPrograms.Chunktopsoil.SetCustomSampler("terrainTex", false);
			ShaderPrograms.Chunktopsoil.SetCustomSampler("terrainTexLinear", true);
			return ok;
		}

		private static void LoadShaderProgram(ShaderProgram program, bool useSSBOs)
		{
			if (program.LoadFromFile)
			{
				ShaderRegistry.LoadShader(program, EnumShaderType.VertexShader);
				ShaderRegistry.LoadShader(program, EnumShaderType.FragmentShader);
				ShaderRegistry.LoadShader(program, EnumShaderType.GeometryShader);
			}
			if (program.VertexShader == null)
			{
				ScreenManager.Platform.Logger.Error("Vertex shader missing for shader {0}. Will probably crash.", new object[] { program.PassName });
			}
			if (program.FragmentShader == null)
			{
				ScreenManager.Platform.Logger.Error("Fragment shader missing for shader {0}. Will probably crash.", new object[] { program.PassName });
			}
			ShaderRegistry.registerDefaultShaderCodePrefixes(program, useSSBOs);
		}

		private static void LoadShader(ShaderProgram program, EnumShaderType shaderType)
		{
			AssetManager amgr = ScreenManager.Platform.AssetManager;
			string ext = ".unknown";
			if (shaderType != EnumShaderType.FragmentShader)
			{
				if (shaderType != EnumShaderType.VertexShader)
				{
					if (shaderType == EnumShaderType.GeometryShader)
					{
						ext = ".gsh";
					}
				}
				else
				{
					ext = ".vsh";
				}
			}
			else
			{
				ext = ".fsh";
			}
			string filename = program.PassName;
			AssetLocation loc = new AssetLocation(program.AssetDomain, "shaders/" + filename + ext);
			IAsset asset = amgr.TryGet_BaseAssets(loc, true);
			if (asset == null)
			{
				if (shaderType != EnumShaderType.GeometryShader)
				{
					ScreenManager.Platform.Logger.Error("Shader file {0} not found. Stack trace:\n{1}", new object[]
					{
						loc,
						Environment.StackTrace
					});
					program.LoadError = true;
				}
				return;
			}
			string code = ShaderRegistry.HandleIncludes(program, asset.ToText(), null);
			if (shaderType != EnumShaderType.FragmentShader)
			{
				if (shaderType != EnumShaderType.VertexShader)
				{
					if (shaderType != EnumShaderType.GeometryShader)
					{
						return;
					}
					if (program.GeometryShader == null)
					{
						program.GeometryShader = new Shader(shaderType, code, filename + ext);
						return;
					}
					program.GeometryShader.Code = code;
					program.GeometryShader.Type = shaderType;
					program.GeometryShader.Filename = filename + ext;
					return;
				}
				else
				{
					if (program.VertexShader == null)
					{
						program.VertexShader = new Shader(shaderType, code, filename + ext);
						return;
					}
					program.VertexShader.Code = code;
					program.VertexShader.Type = shaderType;
					program.VertexShader.Filename = filename + ext;
					return;
				}
			}
			else
			{
				if (program.FragmentShader == null)
				{
					program.FragmentShader = new Shader(shaderType, code, filename + ext);
					return;
				}
				program.FragmentShader.Code = code;
				program.FragmentShader.Type = shaderType;
				program.FragmentShader.Filename = filename + ext;
				return;
			}
		}

		private static void registerDefaultShaderCodePrefixes(ShaderProgram program, bool useSSBOs)
		{
			Shader fragmentShader = program.FragmentShader;
			fragmentShader.PrefixCode = fragmentShader.PrefixCode + "#define FXAA " + ((ClientSettings.FXAA > false) ? 1 : 0).ToString() + "\r\n";
			Shader fragmentShader2 = program.FragmentShader;
			fragmentShader2.PrefixCode = fragmentShader2.PrefixCode + "#define SSAOLEVEL " + ClientSettings.SSAOQuality.ToString() + "\r\n";
			Shader fragmentShader3 = program.FragmentShader;
			fragmentShader3.PrefixCode = fragmentShader3.PrefixCode + "#define NORMALVIEW " + ((ShaderRegistry.NormalView > false) ? 1 : 0).ToString() + "\r\n";
			Shader fragmentShader4 = program.FragmentShader;
			fragmentShader4.PrefixCode = fragmentShader4.PrefixCode + "#define BLOOM " + ((ClientSettings.Bloom > false) ? 1 : 0).ToString() + "\r\n";
			Shader fragmentShader5 = program.FragmentShader;
			fragmentShader5.PrefixCode = fragmentShader5.PrefixCode + "#define GODRAYS " + ClientSettings.GodRayQuality.ToString() + "\r\n";
			Shader fragmentShader6 = program.FragmentShader;
			fragmentShader6.PrefixCode = fragmentShader6.PrefixCode + "#define FOAMEFFECT " + ((ClientSettings.LiquidFoamAndShinyEffect > false) ? 1 : 0).ToString() + "\r\n";
			Shader fragmentShader7 = program.FragmentShader;
			fragmentShader7.PrefixCode = fragmentShader7.PrefixCode + "#define SHINYEFFECT " + ((ClientSettings.LiquidFoamAndShinyEffect > false) ? 1 : 0).ToString() + "\r\n";
			Shader shader = program.FragmentShader;
			shader.PrefixCode = string.Concat(new string[]
			{
				shader.PrefixCode,
				"#define SHADOWQUALITY ",
				ClientSettings.ShadowMapQuality.ToString(),
				"\r\n#define DYNLIGHTS ",
				ClientSettings.MaxDynamicLights.ToString(),
				"\r\n"
			});
			Shader vertexShader = program.VertexShader;
			vertexShader.PrefixCode = vertexShader.PrefixCode + "#define USESSBO " + ((useSSBOs > false) ? 1 : 0).ToString() + "\r\n";
			Shader vertexShader2 = program.VertexShader;
			vertexShader2.PrefixCode = vertexShader2.PrefixCode + "#define WAVINGSTUFF " + ((ClientSettings.WavingFoliage > false) ? 1 : 0).ToString() + "\r\n";
			Shader vertexShader3 = program.VertexShader;
			vertexShader3.PrefixCode = vertexShader3.PrefixCode + "#define FOAMEFFECT " + ((ClientSettings.LiquidFoamAndShinyEffect > false) ? 1 : 0).ToString() + "\r\n";
			Shader vertexShader4 = program.VertexShader;
			vertexShader4.PrefixCode = vertexShader4.PrefixCode + "#define SSAOLEVEL " + ClientSettings.SSAOQuality.ToString() + "\r\n";
			Shader vertexShader5 = program.VertexShader;
			vertexShader5.PrefixCode = vertexShader5.PrefixCode + "#define NORMALVIEW " + ((ShaderRegistry.NormalView > false) ? 1 : 0).ToString() + "\r\n";
			Shader vertexShader6 = program.VertexShader;
			vertexShader6.PrefixCode = vertexShader6.PrefixCode + "#define SHINYEFFECT " + ((ClientSettings.LiquidFoamAndShinyEffect > false) ? 1 : 0).ToString() + "\r\n";
			Shader vertexShader7 = program.VertexShader;
			vertexShader7.PrefixCode = vertexShader7.PrefixCode + "#define GODRAYS " + ClientSettings.GodRayQuality.ToString() + "\r\n";
			Shader vertexShader8 = program.VertexShader;
			vertexShader8.PrefixCode = vertexShader8.PrefixCode + "#define MINBRIGHT " + ClientSettings.Minbrightness.ToString() + "\r\n";
			shader = program.VertexShader;
			shader.PrefixCode = string.Concat(new string[]
			{
				shader.PrefixCode,
				"#define SHADOWQUALITY ",
				ClientSettings.ShadowMapQuality.ToString(),
				"\r\n#define DYNLIGHTS ",
				ClientSettings.MaxDynamicLights.ToString(),
				"\r\n"
			});
			Shader vertexShader9 = program.VertexShader;
			vertexShader9.PrefixCode = vertexShader9.PrefixCode + "#define MAXANIMATEDELEMENTS " + GlobalConstants.MaxAnimatedElements.ToString() + "\r\n";
		}

		private static string HandleIncludes(ShaderProgram program, string shaderCode, HashSet<string> filenames = null)
		{
			if (filenames == null)
			{
				filenames = new HashSet<string>();
			}
			return Regex.Replace(shaderCode, "^#include\\s+(.*)", delegate(Match m)
			{
				string filename = m.Groups[1].Value.Trim().ToLowerInvariant();
				if (filenames.Contains(filename))
				{
					return "";
				}
				filenames.Add(filename);
				return ShaderRegistry.InsertIncludedFile(program, filename, filenames);
			}, RegexOptions.Multiline);
		}

		private static string InsertIncludedFile(ShaderProgram program, string filename, HashSet<string> filenames = null)
		{
			if (!ShaderRegistry.includes.ContainsKey(filename))
			{
				ScreenManager.Platform.Logger.Warning("Error when loading shaders: Include file {0} not found. Ignoring.", new object[] { filename });
				return "";
			}
			program.includes.Add(filename);
			string includedCode = ShaderRegistry.includes[filename];
			return ShaderRegistry.HandleIncludes(program, includedCode, filenames);
		}

		public static bool IsGLSLVersionSupported(string minVersion)
		{
			int versionSupportedInt;
			int.TryParse(Regex.Match(ScreenManager.Platform.GetGLShaderVersionString(), "(\\d\\.\\d+)").Groups[1].Value.Replace(".", ""), out versionSupportedInt);
			int versionUsedInt;
			int.TryParse(minVersion, out versionUsedInt);
			return versionUsedInt <= versionSupportedInt;
		}

		private static string[] shaderNames = new string[100];

		private static ShaderProgram[] shaderPrograms = new ShaderProgram[100];

		private static Dictionary<string, string> includes = new Dictionary<string, string>();

		private static Dictionary<string, int> shaderIdsByName = new Dictionary<string, int>();

		private static int nextPassId = 0;

		public static bool NormalView;
	}
}
