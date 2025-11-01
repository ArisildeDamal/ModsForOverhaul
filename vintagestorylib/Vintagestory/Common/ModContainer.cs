using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using Mono.Cecil;
using Newtonsoft.Json;
using ProperVersion;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Vintagestory.Common
{
	public class ModContainer : Mod
	{
		public bool Enabled
		{
			get
			{
				return this.Status == ModStatus.Enabled;
			}
		}

		public ModStatus Status { get; set; } = ModStatus.Enabled;

		public ModError? Error { get; set; }

		public string FolderPath { get; private set; }

		public List<string> SourceFiles { get; } = new List<string>();

		public List<string> AssemblyFiles { get; } = new List<string>();

		public bool RequiresCompilation
		{
			get
			{
				return this.SourceFiles.Count > 0;
			}
		}

		public Assembly Assembly { get; private set; }

		public ModContainer(FileSystemInfo fsInfo, ILogger parentLogger, bool logDebug)
		{
			base.SourceType = ModContainer.GetSourceType(fsInfo).Value;
			base.FileName = fsInfo.Name;
			base.SourcePath = fsInfo.FullName;
			base.Logger = new ModLogger(parentLogger, this);
			base.Logger.TraceLog = logDebug;
			switch (base.SourceType)
			{
			case EnumModSourceType.CS:
				this.SourceFiles.Add(base.SourcePath);
				return;
			case EnumModSourceType.DLL:
				this.AssemblyFiles.Add(base.SourcePath);
				return;
			case EnumModSourceType.ZIP:
				break;
			case EnumModSourceType.Folder:
				this.FolderPath = base.SourcePath;
				break;
			default:
				return;
			}
		}

		public static EnumModSourceType? GetSourceType(FileSystemInfo fsInfo)
		{
			if (fsInfo is DirectoryInfo)
			{
				return new EnumModSourceType?(EnumModSourceType.Folder);
			}
			return ModContainer.GetSourceTypeFromExtension(fsInfo.Name);
		}

		private static EnumModSourceType? GetSourceTypeFromExtension(string fileName)
		{
			string ext = Path.GetExtension(fileName);
			if (string.IsNullOrEmpty(ext))
			{
				return null;
			}
			ext = ext.Substring(1).ToUpperInvariant();
			EnumModSourceType type;
			if (!Enum.TryParse<EnumModSourceType>(ext, out type))
			{
				return null;
			}
			return new EnumModSourceType?(type);
		}

		public void SetError(ModError error)
		{
			this.Status = ModStatus.Errored;
			this.Error = new ModError?(error);
		}

		public static IEnumerable<string> EnumerateModFiles(string path)
		{
			IEnumerable<string> modfiles = Directory.EnumerateFiles(path);
			foreach (string folder in Directory.EnumerateDirectories(path))
			{
				if (!(Path.GetFileName(folder) == ".git"))
				{
					modfiles = modfiles.Concat(ModContainer.EnumerateModFiles(folder));
				}
			}
			return modfiles;
		}

		public void Unpack(string unpackPath)
		{
			if (!this.Enabled || this.SourceFiles.Count > 0 || this.AssemblyFiles.Count > 0)
			{
				return;
			}
			if (base.SourceType == EnumModSourceType.ZIP)
			{
				using (FileStream stream = File.OpenRead(base.SourcePath))
				{
					IEnumerable<byte> enumerable = ModContainer.fileHasher.ComputeHash(stream);
					StringBuilder sb = new StringBuilder(12);
					foreach (byte b in enumerable.Take(6))
					{
						sb.Append(b.ToString("x2"));
					}
					this.FolderPath = Path.Combine(unpackPath, base.FileName + "_" + sb.ToString());
				}
				if (!Directory.Exists(this.FolderPath))
				{
					try
					{
						Directory.CreateDirectory(this.FolderPath);
						using (ZipFile zipFile = new ZipFile(base.SourcePath, null))
						{
							foreach (object obj in zipFile)
							{
								ZipEntry entry = (ZipEntry)obj;
								string outputPath = Path.Combine(this.FolderPath, entry.Name);
								if (entry.IsDirectory)
								{
									Directory.CreateDirectory(outputPath);
								}
								else
								{
									string outputDirectory = Path.GetDirectoryName(outputPath);
									if (!Directory.Exists(outputDirectory))
									{
										Directory.CreateDirectory(outputDirectory);
									}
									using (Stream inputStream = zipFile.GetInputStream(entry))
									{
										using (FileStream outputStream = new FileStream(outputPath, FileMode.Create))
										{
											inputStream.CopyTo(outputStream);
										}
									}
								}
							}
						}
					}
					catch (Exception ex)
					{
						base.Logger.Error("An exception was thrown when trying to extract the mod archive to '{0}':", new object[] { this.FolderPath });
						base.Logger.Error(ex);
						this.SetError(ModError.Loading);
						try
						{
							Directory.Delete(this.FolderPath, true);
						}
						catch (Exception)
						{
							base.Logger.Error("Additionally, there was an exception when deleting cached mod folder path '{0}':", new object[] { this.FolderPath });
							base.Logger.Error(ex);
						}
						return;
					}
				}
			}
			string ignoreFilename = Path.Combine(this.FolderPath, ".ignore");
			IgnoreFile ignoreFile = (File.Exists(ignoreFilename) ? new IgnoreFile(ignoreFilename, this.FolderPath) : null);
			foreach (string path in ModContainer.EnumerateModFiles(this.FolderPath))
			{
				if (ignoreFile == null || ignoreFile.Available(path))
				{
					EnumModSourceType? type = ModContainer.GetSourceTypeFromExtension(path);
					string relativePath = path.Substring(this.FolderPath.Length + 1);
					int slashIndex = relativePath.IndexOfAny(new char[] { '/', '\\' });
					string topFolderName = ((slashIndex >= 0) ? relativePath.Substring(0, slashIndex) : null);
					if (type != null)
					{
						EnumModSourceType valueOrDefault = type.GetValueOrDefault();
						if (valueOrDefault != EnumModSourceType.CS)
						{
							if (valueOrDefault == EnumModSourceType.DLL)
							{
								if (!(topFolderName == "native"))
								{
									if (topFolderName != null)
									{
										base.Logger.Error("File '{0}' is not in the mod's root folder. Won't load this mod. If you need to ship unmanaged dlls, put them in the native/ folder.", new object[] { Path.GetFileName(path) });
										if (base.SourceType != EnumModSourceType.Folder)
										{
											this.SetError(ModError.Loading);
											break;
										}
									}
									else
									{
										this.AssemblyFiles.Add(path);
									}
								}
							}
						}
						else if (topFolderName != "src")
						{
							base.Logger.Error("File '{0}' is not in the 'src/' subfolder.", new object[] { Path.GetFileName(path) });
							if (base.SourceType != EnumModSourceType.Folder)
							{
								this.SetError(ModError.Loading);
								break;
							}
						}
						else
						{
							this.SourceFiles.Add(path);
						}
					}
				}
			}
		}

		public void LoadModInfo(ModCompilationContext compilationContext, ModAssemblyLoader loader)
		{
			if (!this.Enabled || base.Info != null)
			{
				return;
			}
			try
			{
				if (base.SourceType == EnumModSourceType.ZIP || base.SourceType == EnumModSourceType.Folder)
				{
					if (this.FolderPath != null)
					{
						string modInfoPath = Path.Combine(this.FolderPath, "modinfo.json");
						if (File.Exists(modInfoPath))
						{
							string content = File.ReadAllText(modInfoPath);
							base.Info = JsonConvert.DeserializeObject<ModInfo>(content);
							ModInfo info = base.Info;
							if (info != null)
							{
								info.Init();
							}
						}
						string worldConfigPath = Path.Combine(this.FolderPath, "worldconfig.json");
						if (File.Exists(worldConfigPath))
						{
							string content2 = File.ReadAllText(worldConfigPath);
							base.WorldConfig = JsonConvert.DeserializeObject<ModWorldConfiguration>(content2);
						}
						string iconPath = null;
						ModInfo info2 = base.Info;
						if (!string.IsNullOrWhiteSpace((info2 != null) ? info2.IconPath : null))
						{
							try
							{
								iconPath = Path.GetFullPath(Path.Combine(this.FolderPath, base.Info.IconPath));
							}
							catch (Exception ex)
							{
								base.Logger.Warning("Failed create path from the IconPath '{0}' specified in the ModInfo, did you use characters that are not valid in a path?: {1}.", new object[]
								{
									base.Info.IconPath,
									ex
								});
								iconPath = null;
							}
							if (!iconPath.StartsWithOrdinal(this.FolderPath))
							{
								base.Logger.Warning("The IconPath '{0}' specified in the ModInfo tried to escape the mod root. This is not allowed.", new object[] { base.Info.IconPath });
								iconPath = null;
							}
						}
						else
						{
							iconPath = Path.Combine(this.FolderPath, "modicon.png");
							if (!File.Exists(iconPath))
							{
								iconPath = ModContainer.<LoadModInfo>g__GetFallbackIconPath|35_0();
							}
						}
						if (File.Exists(iconPath))
						{
							base.Icon = new BitmapExternal(iconPath, base.Logger);
						}
					}
					else
					{
						using (ZipFile zip = new ZipFile(base.SourcePath, null))
						{
							ZipEntry modInfoEntry = zip.GetEntry("modinfo.json");
							if (modInfoEntry != null)
							{
								using (StreamReader reader = new StreamReader(zip.GetInputStream(modInfoEntry)))
								{
									string content3 = reader.ReadToEnd();
									base.Info = JsonConvert.DeserializeObject<ModInfo>(content3);
									ModInfo info3 = base.Info;
									if (info3 != null)
									{
										info3.Init();
									}
								}
							}
							ZipEntry worldConfigEntry = zip.GetEntry("worldconfig.json");
							if (worldConfigEntry != null)
							{
								using (StreamReader reader2 = new StreamReader(zip.GetInputStream(worldConfigEntry)))
								{
									string content4 = reader2.ReadToEnd();
									base.WorldConfig = JsonConvert.DeserializeObject<ModWorldConfiguration>(content4);
								}
							}
							ModInfo info4 = base.Info;
							if (!string.IsNullOrWhiteSpace((info4 != null) ? info4.IconPath : null))
							{
								ZipEntry iconEntry = zip.GetEntry(base.Info.IconPath);
								if (iconEntry != null)
								{
									using (MemoryStream memoryStream = new MemoryStream(1048576))
									{
										using (Stream entryStream = zip.GetInputStream(iconEntry))
										{
											entryStream.CopyTo(memoryStream);
											base.Icon = new BitmapExternal(memoryStream, base.Logger, null);
										}
										goto IL_035F;
									}
								}
								base.Logger.Warning("Failed find the IconPath '{0}' specified in the ModInfo within the mod archive.", new object[] { base.Info.IconPath });
							}
							else
							{
								ZipEntry iconEntry2 = zip.GetEntry("modicon.png");
								if (iconEntry2 != null)
								{
									using (MemoryStream memoryStream2 = new MemoryStream(1048576))
									{
										using (Stream entryStream2 = zip.GetInputStream(iconEntry2))
										{
											entryStream2.CopyTo(memoryStream2);
											base.Icon = new BitmapExternal(memoryStream2, base.Logger, null);
										}
										goto IL_035F;
									}
								}
								string iconPath2 = ModContainer.<LoadModInfo>g__GetFallbackIconPath|35_0();
								if (File.Exists(iconPath2))
								{
									base.Icon = new BitmapExternal(iconPath2, base.Logger);
								}
							}
							IL_035F:;
						}
						if (base.WorldConfig != null)
						{
							this.Unpack(Path.Combine(GamePaths.Cache, "unpack"));
						}
					}
					if (base.Info == null)
					{
						base.Logger.Error("Missing modinfo.json");
						this.SetError(ModError.Loading);
					}
					if (this.SourceFiles.Count > 0 || this.AssemblyFiles.Count > 0)
					{
						base.Logger.Warning("Is a {0} mod, but .cs or .dll files were found. These will be ignored.", new object[] { base.SourceType });
					}
					if (base.WorldConfig != null)
					{
						string folderPath = this.FolderPath;
						ModInfo info5 = base.Info;
						Lang.PreLoadModWorldConfig(folderPath, (info5 != null) ? info5.ModID : null, Lang.CurrentLocale);
					}
				}
				else
				{
					ModWorldConfiguration worldConfig;
					base.Info = this.LoadModInfoFromCode(compilationContext, loader, out worldConfig);
					ModInfo info6 = base.Info;
					if (info6 != null)
					{
						info6.Init();
					}
					base.WorldConfig = worldConfig;
					if (base.Info == null)
					{
						base.Logger.Error("Missing ModInfoAttribute");
						this.SetError(ModError.Loading);
					}
					string iconPath3 = null;
					ModInfo info7 = base.Info;
					if (!string.IsNullOrWhiteSpace((info7 != null) ? info7.IconPath : null))
					{
						try
						{
							iconPath3 = Path.GetFullPath(Path.Combine(GamePaths.AssetsPath, base.Info.IconPath));
						}
						catch (Exception ex2)
						{
							base.Logger.Warning("Failed create path from the IconPath '{0}' specified in the ModInfo, did you use characters that are not valid in a path?: {0}.", new object[]
							{
								base.Info.IconPath,
								ex2
							});
							iconPath3 = null;
						}
						if (!iconPath3.StartsWithOrdinal(GamePaths.AssetsPath))
						{
							base.Logger.Warning("The IconPath '{0}' specified in the ModInfo tried to escape the AssetPath. This is not allowed.", new object[] { base.Info.IconPath });
							iconPath3 = null;
						}
					}
					else
					{
						iconPath3 = ModContainer.<LoadModInfo>g__GetFallbackIconPath|35_0();
					}
					if (File.Exists(iconPath3))
					{
						base.Icon = new BitmapExternal(iconPath3, base.Logger);
					}
				}
				if (base.Info != null)
				{
					this.CheckProperVersions();
				}
			}
			catch (Exception ex3)
			{
				base.Logger.Error("An exception was thrown trying to to load the ModInfo:");
				base.Logger.Error(ex3);
				this.SetError(ModError.Loading);
			}
		}

		private ModInfo LoadModInfoFromCode(ModCompilationContext compilationContext, ModAssemblyLoader loader, out ModWorldConfiguration modWorldConfig)
		{
			ModInfo modInfo;
			if (this.RequiresCompilation)
			{
				if (this.AssemblyFiles.Count > 0)
				{
					throw new Exception("Found both .cs and .dll files, this is not supported");
				}
				this.Assembly = compilationContext.CompileFromFiles(this);
				base.Logger.Notification("Successfully compiled {0} source files", new object[] { this.SourceFiles.Count });
				base.Logger.VerboseDebug("Successfully compiled {0} source files", new object[] { this.SourceFiles.Count });
				modInfo = this.LoadModInfoFromAssembly(this.Assembly, out modWorldConfig);
			}
			else
			{
				List<string> sanitizedPaths = new List<string>(this.AssemblyFiles.Count);
				foreach (string path in this.AssemblyFiles)
				{
					sanitizedPaths.Add(StringUtil.SanitizePath(path));
				}
				base.Logger.VerboseDebug("Check for mod systems in mod {0}", new object[] { string.Join(", ", sanitizedPaths) });
				List<string> assemblyCandidates = this.AssemblyFiles.Where((string file) => base.<LoadModInfoFromCode>g__isEligible|1(file)).ToList<string>();
				if (assemblyCandidates.Count == 0)
				{
					throw new Exception(string.Format("{0} declared as code mod, but there are no .dll files that contain at least one ModSystem or has a ModInfo attribute", string.Join(", ", this.AssemblyFiles)));
				}
				if (assemblyCandidates.Count >= 2)
				{
					throw new Exception("Found multiple .dll files with ModSystems and/or ModInfo attributes");
				}
				this.selectedAssemblyFile = assemblyCandidates[0];
				base.Logger.VerboseDebug("Selected assembly {0}", new object[] { StringUtil.SanitizePath(this.selectedAssemblyFile) });
				modInfo = this.LoadModInfoFromAssemblyDefinition(loader.LoadAssemblyDefinition(this.selectedAssemblyFile), out modWorldConfig);
			}
			return modInfo;
		}

		public void LoadAssembly(ModCompilationContext compilationContext, ModAssemblyLoader loader)
		{
			ModInfo info = base.Info;
			EnumModType modType = ((info != null) ? info.Type : EnumModType.Code);
			if (!this.Enabled || this.Assembly != null)
			{
				return;
			}
			if (modType != EnumModType.Code)
			{
				if (this.SourceFiles.Count > 0 || this.AssemblyFiles.Count > 0)
				{
					base.Logger.Warning("Is a {0} mod, but .cs or .dll files were found. These will be ignored.", new object[] { modType });
				}
				return;
			}
			try
			{
				if (this.RequiresCompilation)
				{
					this.Assembly = compilationContext.CompileFromFiles(this);
					base.Logger.Notification("Successfully compiled {0} source files", new object[] { this.SourceFiles.Count });
					base.Logger.VerboseDebug("Successfully compiled {0} source files", new object[] { this.SourceFiles.Count });
				}
				else if (this.selectedAssemblyFile != null)
				{
					this.Assembly = loader.LoadFrom(this.selectedAssemblyFile);
				}
				else
				{
					base.Logger.VerboseDebug("Check for mod systems in mod {0}", new object[] { string.Join(", ", this.AssemblyFiles) });
					List<Assembly> assemblyCandidates = (from path in this.AssemblyFiles
						select loader.LoadFrom(path) into ass
						where ass.GetCustomAttribute<ModInfoAttribute>() != null || this.GetModSystems(ass).Any<Type>()
						select ass).ToList<Assembly>();
					if (assemblyCandidates.Count == 0)
					{
						throw new Exception(string.Format("{0} declared as code mod, but there are no .dll files that contain at least one ModSystem or has a ModInfo attribute", string.Join(", ", this.AssemblyFiles)));
					}
					if (assemblyCandidates.Count >= 2)
					{
						throw new Exception("Found multiple .dll files with ModSystems and/or ModInfo attributes");
					}
					this.Assembly = assemblyCandidates[0];
					base.Logger.VerboseDebug("Loaded assembly {0}", new object[] { this.Assembly.Location });
				}
			}
			catch (Exception ex)
			{
				if (ex.Message == "Assembly with same name is already loaded")
				{
					string msg = "Please restart the game. The mod's .dll was already loaded and cannot be reloaded. Most likely cause is switching mod versions after already playing one world. Other rare causes include two mods with .dlls with the same name";
					base.Logger.Error(msg);
					this.SetError(ModError.ChangedVersion);
					throw new Exception(msg, ex);
				}
				base.Logger.Error("An exception was thrown when trying to load assembly:");
				base.Logger.Error(ex);
				this.SetError(ModError.Loading);
			}
		}

		public void InstantiateModSystems(EnumAppSide side)
		{
			if (!this.Enabled || this.Assembly == null || base.Systems.Count > 0)
			{
				return;
			}
			if (base.Info == null)
			{
				throw new InvalidOperationException("LoadModInfo was not called before InstantiateModSystems");
			}
			if (!base.Info.Side.Is(side))
			{
				this.Status = ModStatus.Unused;
				return;
			}
			List<ModSystem> systems = new List<ModSystem>();
			foreach (Type systemType in this.GetModSystems(this.Assembly))
			{
				try
				{
					ModSystem system = (ModSystem)Activator.CreateInstance(systemType);
					system.Mod = this;
					systems.Add(system);
				}
				catch (Exception ex)
				{
					base.Logger.Error("Exception thrown when trying to create an instance of ModSystem {0}:", new object[] { systemType });
					base.Logger.Error(ex);
				}
			}
			base.Systems = systems.AsReadOnly();
			if (base.Systems.Count == 0 && this.FolderPath == null)
			{
				base.Logger.Warning("Is a Code mod, but no ModSystems found");
			}
		}

		private IEnumerable<Type> GetModSystems(Assembly assembly)
		{
			IEnumerable<Type> enumerable;
			try
			{
				enumerable = from type in assembly.GetTypes()
					where typeof(ModSystem).IsAssignableFrom(type) && !type.IsAbstract
					select type;
			}
			catch (Exception ex)
			{
				if (ex is ReflectionTypeLoadException)
				{
					Exception[] es = (ex as ReflectionTypeLoadException).LoaderExceptions;
					base.Logger.Error("Exception thrown when attempting to retrieve all types of the assembly {0}. Will ignore asssembly. Loader exceptions:", new object[] { assembly.FullName });
					base.Logger.Error(ex);
					if (ex.InnerException != null)
					{
						base.Logger.Error("InnerException:");
						base.Logger.Error(ex.InnerException);
					}
					for (int i = 0; i < es.Length; i++)
					{
						base.Logger.Error(es[i]);
					}
				}
				else
				{
					base.Logger.Error("Exception thrown when attempting to retrieve all types of the assembly {0}: {1}, InnerException: {2}. Will ignore asssembly.", new object[] { assembly.FullName, ex, ex.InnerException });
				}
				enumerable = Enumerable.Empty<Type>();
			}
			return enumerable;
		}

		private ModInfo LoadModInfoFromAssembly(Assembly assembly, out ModWorldConfiguration modWorldConfig)
		{
			ModInfoAttribute modInfoAttr = assembly.GetCustomAttribute<ModInfoAttribute>();
			if (modInfoAttr == null)
			{
				modWorldConfig = null;
				return null;
			}
			List<ModDependency> dependencies = (from attr in assembly.GetCustomAttributes<ModDependencyAttribute>()
				select new ModDependency(attr.ModID, attr.Version)).ToList<ModDependency>();
			return this.LoadModInfoFromModInfoAttribute(modInfoAttr, dependencies, out modWorldConfig);
		}

		private ModInfo LoadModInfoFromAssemblyDefinition(AssemblyDefinition assemblyDefinition, out ModWorldConfiguration modWorldConfig)
		{
			CustomAttribute modInfoAttribute = assemblyDefinition.CustomAttributes.SingleOrDefault((CustomAttribute attribute) => attribute.AttributeType.Name == "ModInfoAttribute");
			if (modInfoAttribute == null)
			{
				modWorldConfig = null;
				return null;
			}
			string name = modInfoAttribute.ConstructorArguments[0].Value as string;
			string modID = modInfoAttribute.ConstructorArguments[1].Value as string;
			ModInfoAttribute modInfo = new ModInfoAttribute(name, modID);
			foreach (CustomAttributeNamedArgument property in modInfoAttribute.Properties.Where((CustomAttributeNamedArgument p) => p.Name != "Name" && p.Name != "ModID"))
			{
				PropertyInfo propertySetter = modInfo.GetType().GetProperty(property.Name);
				CustomAttributeArgument[] array = property.Argument.Value as CustomAttributeArgument[];
				if (array != null)
				{
					propertySetter.SetValue(modInfo, array.Select((CustomAttributeArgument item) => item.Value as string).ToArray<string>());
				}
				else
				{
					propertySetter.SetValue(modInfo, property.Argument.Value);
				}
			}
			List<ModDependency> dependencies = (from attribute in assemblyDefinition.CustomAttributes
				where attribute.AttributeType.Name == "ModDependencyAttribute"
				select new ModDependency((string)attribute.ConstructorArguments[0].Value, attribute.ConstructorArguments[1].Value as string)).ToList<ModDependency>();
			return this.LoadModInfoFromModInfoAttribute(modInfo, dependencies, out modWorldConfig);
		}

		private ModInfo LoadModInfoFromModInfoAttribute(ModInfoAttribute modInfoAttr, List<ModDependency> dependencies, out ModWorldConfiguration modWorldConfig)
		{
			EnumAppSide side;
			if (!Enum.TryParse<EnumAppSide>(modInfoAttr.Side, true, out side))
			{
				base.Logger.Warning("Cannot parse '{0}', must be either 'Client', 'Server' or 'Universal'. Defaulting to 'Universal'.", new object[] { modInfoAttr.Side });
				side = EnumAppSide.Universal;
			}
			if (modInfoAttr.WorldConfig != null)
			{
				modWorldConfig = JsonConvert.DeserializeObject<ModWorldConfiguration>(modInfoAttr.WorldConfig);
			}
			else
			{
				modWorldConfig = null;
			}
			return new ModInfo(EnumModType.Code, modInfoAttr.Name, modInfoAttr.ModID, modInfoAttr.Version, modInfoAttr.Description, modInfoAttr.Authors, modInfoAttr.Contributors, modInfoAttr.Website, side, modInfoAttr.RequiredOnClient, modInfoAttr.RequiredOnServer, dependencies)
			{
				NetworkVersion = modInfoAttr.NetworkVersion,
				CoreMod = modInfoAttr.CoreMod,
				IconPath = modInfoAttr.IconPath
			};
		}

		private void CheckProperVersions()
		{
			SemVer guess;
			string error;
			if (!string.IsNullOrEmpty(base.Info.Version) && !SemVer.TryParse(base.Info.Version, out guess, out error))
			{
				base.Logger.Warning("{0} (best guess: {1})", new object[] { error, guess });
			}
			foreach (ModDependency dep in base.Info.Dependencies)
			{
				if (!(dep.Version == "*") && !string.IsNullOrEmpty(dep.Version) && !SemVer.TryParse(dep.Version, out guess, out error))
				{
					base.Logger.Warning("Dependency '{0}': {1} (best guess: {2})", new object[] { dep.ModID, error, guess });
				}
			}
		}

		[CompilerGenerated]
		internal static string <LoadModInfo>g__GetFallbackIconPath|35_0()
		{
			return Path.Combine(GamePaths.AssetsPath, "game/textures/gui/3rdpartymodicon.png");
		}

		[CompilerGenerated]
		internal static bool <LoadModInfoFromCode>g__isModSystem|36_0(TypeDefinition typeDefinition)
		{
			TypeDefinition typeDefinition2;
			for (TypeReference baseType = typeDefinition.BaseType; baseType != null; baseType = ((typeDefinition2 != null) ? typeDefinition2.BaseType : null))
			{
				if (baseType.FullName == typeof(ModSystem).FullName)
				{
					return true;
				}
				typeDefinition2 = baseType.Resolve();
			}
			return false;
		}

		public List<string> MissingDependencies;

		private string selectedAssemblyFile;

		private static HashAlgorithm fileHasher = SHA1.Create();
	}
}
