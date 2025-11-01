using System;
using System.Text.RegularExpressions;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace Vintagestory.Client.NoObf
{
	public class Shader : IShader
	{
		public EnumShaderType Type
		{
			get
			{
				return this.shaderType;
			}
			set
			{
				this.shaderType = value;
			}
		}

		public string PrefixCode
		{
			get
			{
				return this.prefixCode;
			}
			set
			{
				this.prefixCode = value;
			}
		}

		public string Code
		{
			get
			{
				return this.code;
			}
			set
			{
				this.code = value;
			}
		}

		public Shader()
		{
		}

		public Shader(EnumShaderType shaderType, string code, string filename)
		{
			this.shaderType = shaderType;
			this.code = code;
			this.Filename = filename;
		}

		public bool Compile()
		{
			return ScreenManager.Platform.CompileShader(this);
		}

		public void EnsureVersionSupported()
		{
			Match match = Regex.Match(this.code, Shader.shaderVersionPattern);
			if (match.Groups.Count > 1)
			{
				string versionRequested = match.Groups[1].Value;
				if (ScreenManager.Platform.UseSSBOs && this.UsesSSBOs())
				{
					versionRequested = "430";
				}
				Shader.EnsureVersionSupported(versionRequested, this.Filename);
			}
		}

		public Shader Clone()
		{
			return new Shader(this.shaderType, this.code, this.Filename)
			{
				prefixCode = this.prefixCode
			};
		}

		public static void EnsureVersionSupported(string versionUsed, string ownFilename)
		{
			string versionSupported = ScreenManager.Platform.GetGLShaderVersionString();
			string versionSupportedfiltered = Regex.Match(versionSupported, "(\\d\\.\\d+)").Groups[1].Value.Replace(".", "");
			int versionSupportedInt;
			int.TryParse(versionSupportedfiltered, out versionSupportedInt);
			int versionUsedInt;
			int.TryParse(versionUsed, out versionUsedInt);
			if (versionUsedInt > versionSupportedInt)
			{
				string errorMessage = string.Format("Your graphics card supports only OpenGL version {0} ({1}), but OpenGL version {2} is required.\n", versionSupportedfiltered, versionSupported, versionUsed);
				if (versionSupportedInt == 330 && ClientSettings.GlContextVersion == "3.3" && RuntimeEnv.OS != OS.Mac)
				{
					errorMessage += "===>  In your clientsettings.json file try searching and setting this string setting:  \"glContextVersion\": \"4.3\",  then start the game again <===";
					if (ScreenManager.Platform.UseSSBOs)
					{
						errorMessage += "\n(You can also try setting bool setting \"allowSSBOs\" to false, but try out \"glContextVersion\" first)";
					}
				}
				else if (ScreenManager.Platform.UseSSBOs && versionUsed == "430")
				{
					if (versionSupportedInt < 430 && RuntimeEnv.OS != OS.Mac)
					{
						errorMessage += "***In your clientsettings.json file please either set bool setting \"allowSSBOs\" to false, or set \"glContextVersion\" to 4.3, and try again.***\n(AllowSSBOs true should only be used if your hardware supports OpenGL 4.3 or later. Ask for support if necessary!)\n";
					}
					else
					{
						errorMessage += "***In your clientsettings.json file please set bool setting \"allowSSBOs\" to false, and try again.***\n";
					}
					errorMessage += "Please then check if you have installed the latest version of your graphics card driver. If so, your graphics card may be to old to play Vintage Story.(Note: In case of modded gameplay with modded shaders, the mod author may be able to lower the OpenGL version requirements)";
				}
				else
				{
					errorMessage += "*** First check clientsettings.json setting \"glContextVersion\", this may have modified the version reported by the hardware, try increasing it.***\nPlease then check if you have installed the latest version of your graphics card driver. If so, your graphics card may be to old to play Vintage Story.(Note: In case of modded gameplay with modded shaders, the mod author may be able to lower the OpenGL version requirements)";
				}
				throw new NotSupportedException(errorMessage);
			}
		}

		public bool UsesSSBOs()
		{
			return this.Type == EnumShaderType.VertexShader && (this.Filename.StartsWith("chunk") || this.Filename.Equals("decals.vsh"));
		}

		private static string shaderVersionPattern = "\\#version (\\d+)";

		public int ShaderId;

		private string prefixCode = "";

		private string code = "";

		public EnumShaderType shaderType;

		internal string Filename = "";
	}
}
