using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using ProperVersion;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Client;
using Vintagestory.ModDb;

namespace Vintagestory.Common
{
	public class ModLoader : IModLoader
	{
		public int TextureSize { get; set; } = 32;

		public IReadOnlyCollection<string> ModSearchPaths { get; }

		public string UnpackPath { get; } = Path.Combine(GamePaths.Cache, "unpack");

		public IEnumerable<Mod> Mods
		{
			get
			{
				return this.loadedMods.Values.Where((ModContainer mod) => mod.Enabled);
			}
		}

		public IEnumerable<ModSystem> Systems
		{
			get
			{
				return this.enabledSystems.Select((ModSystem x) => x);
			}
		}

		public Mod GetMod(string modID)
		{
			ModContainer mod;
			if (!this.loadedMods.TryGetValue(modID, out mod))
			{
				return null;
			}
			if (!mod.Enabled)
			{
				return null;
			}
			return mod;
		}

		public bool IsModEnabled(string modID)
		{
			ModContainer modContainer = this.GetMod(modID) as ModContainer;
			return modContainer != null && modContainer.Enabled;
		}

		public ModSystem GetModSystem(string fullName)
		{
			return this.Systems.FirstOrDefault((ModSystem mod) => string.Equals(mod.GetType().FullName, fullName, StringComparison.InvariantCultureIgnoreCase));
		}

		public T GetModSystem<T>(bool withInheritance = true) where T : ModSystem
		{
			if (withInheritance)
			{
				return this.Systems.OfType<T>().FirstOrDefault<T>();
			}
			return this.Systems.FirstOrDefault((ModSystem mod) => mod.GetType() == typeof(T)) as T;
		}

		public bool IsModSystemEnabled(string fullName)
		{
			return this.GetModSystem(fullName) != null;
		}

		public ModLoader(ILogger logger, EnumAppSide side, IEnumerable<string> modSearchPaths, bool traceLog)
			: this(null, side, logger, modSearchPaths, traceLog)
		{
		}

		public ModLoader(ICoreAPI api, IEnumerable<string> modSearchPaths, bool traceLog)
			: this(api, api.Side, api.World.Logger, modSearchPaths, traceLog)
		{
		}

		private ModLoader(ICoreAPI api, EnumAppSide side, ILogger logger, IEnumerable<string> modSearchPaths, bool traceLog)
		{
			this.api = api;
			this.side = side;
			this.logger = logger;
			this.traceLog = traceLog;
			this.ModSearchPaths = modSearchPaths.Select(delegate(string path)
			{
				if (Path.IsPathRooted(path))
				{
					return path;
				}
				return Path.Combine(GamePaths.Binaries, path);
			}).ToList<string>().AsReadOnly();
		}

		public OrderedDictionary<string, IAssetOrigin> GetContentArchives()
		{
			return this.contentAssetOrigins;
		}

		public OrderedDictionary<string, IAssetOrigin> GetThemeArchives()
		{
			return this.themeAssetOrigins;
		}

		public List<ModContainer> LoadModInfos()
		{
			List<ModContainer> mods = this.CollectMods();
			using (ModAssemblyLoader loader = new ModAssemblyLoader(this.ModSearchPaths, mods))
			{
				foreach (ModContainer modContainer in mods)
				{
					modContainer.LoadModInfo(this.compilationContext, loader);
				}
			}
			return mods;
		}

		public List<ModContainer> LoadModInfosAndVerify(IEnumerable<string> disabledModsByIdAndVersion = null)
		{
			List<ModContainer> mods = this.LoadModInfos();
			return this.DisableAndVerify(mods, disabledModsByIdAndVersion);
		}

		public List<ModContainer> DisableAndVerify(List<ModContainer> mods, IEnumerable<string> disabledModsByIdAndVersion = null)
		{
			if (disabledModsByIdAndVersion != null && disabledModsByIdAndVersion.Count<string>() > 0)
			{
				this.DisableMods(mods, disabledModsByIdAndVersion);
			}
			return this.verifyMods(mods);
		}

		public void LoadMods(IEnumerable<string> disabledModsByIdAndVersion = null)
		{
			List<ModContainer> mods = this.LoadModInfos();
			this.LoadMods(mods, disabledModsByIdAndVersion);
		}

		public void LoadMods(List<ModContainer> mods, IEnumerable<string> disabledModsByIdAndVersion = null)
		{
			Dictionary<string, string> modBlockList = ModDbUtil.ModBlockList;
			if (modBlockList != null && modBlockList.Count > 0)
			{
				List<string> nowBlockedMods = new List<string>();
				foreach (ModContainer mod in mods)
				{
					string reason;
					if (mod.Error == null && ModDbUtil.IsModBlocked(mod.Info.ModID, mod.Info.Version, out reason))
					{
						List<string> list = nowBlockedMods;
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(3, 3);
						defaultInterpolatedStringHandler.AppendFormatted(mod.Info.ModID);
						defaultInterpolatedStringHandler.AppendLiteral("@");
						defaultInterpolatedStringHandler.AppendFormatted(mod.Info.Version);
						defaultInterpolatedStringHandler.AppendLiteral(": ");
						defaultInterpolatedStringHandler.AppendFormatted(reason);
						list.Add(defaultInterpolatedStringHandler.ToStringAndClear());
					}
				}
				if (nowBlockedMods.Count > 0)
				{
					this.logger.Warning("The following mods where blocked from loading:");
					foreach (string bm in nowBlockedMods)
					{
						this.logger.Warning("  " + bm);
					}
				}
				if (disabledModsByIdAndVersion != null)
				{
					disabledModsByIdAndVersion = new List<string>(disabledModsByIdAndVersion);
				}
				else
				{
					disabledModsByIdAndVersion = new List<string>();
				}
				((List<string>)disabledModsByIdAndVersion).AddRange(ModDbUtil.ModBlockList.Keys);
			}
			if (disabledModsByIdAndVersion != null && disabledModsByIdAndVersion.Count<string>() > 0)
			{
				using (ModAssemblyLoader loader = new ModAssemblyLoader(this.ModSearchPaths, mods))
				{
					foreach (ModContainer modContainer in mods)
					{
						modContainer.LoadModInfo(this.compilationContext, loader);
					}
				}
				int disabledModCount = this.DisableMods(mods, disabledModsByIdAndVersion);
				this.logger.Notification("Found {0} mods ({1} disabled)", new object[] { mods.Count, disabledModCount });
			}
			else
			{
				this.logger.Notification("Found {0} mods (0 disabled)", new object[] { mods.Count });
			}
			mods = this.verifyMods(mods);
			ILogger logger = this.logger;
			string text = "Mods, sorted by dependency: {0}";
			object[] array = new object[1];
			array[0] = string.Join(", ", mods.Select((ModContainer m) => m.Info.ModID));
			logger.Notification(text, array);
			foreach (ModContainer mod2 in mods)
			{
				if (mod2.Enabled)
				{
					mod2.Unpack(this.UnpackPath);
				}
			}
			this.ClearCacheFolder(mods);
			this.enabledSystems = this.instantiateMods(mods);
		}

		private List<ModContainer> verifyMods(List<ModContainer> mods)
		{
			this.CheckDuplicateModIDMods(mods);
			return this.CheckAndSortDependencies(mods);
		}

		private List<ModSystem> instantiateMods(List<ModContainer> mods)
		{
			List<ModSystem> enabledSystems = new List<ModSystem>();
			mods = mods.OrderBy((ModContainer mod) => mod.RequiresCompilation).ToList<ModContainer>();
			using (ModAssemblyLoader loader = new ModAssemblyLoader(this.ModSearchPaths, mods))
			{
				foreach (ModContainer mod4 in mods)
				{
					if (mod4.Enabled)
					{
						mod4.LoadAssembly(this.compilationContext, loader);
						if (mod4.Status == ModStatus.Errored && mod4.Error.GetValueOrDefault() == ModError.ChangedVersion)
						{
							ICoreServerAPI sapi = this.api as ICoreServerAPI;
							if (sapi != null && !sapi.Server.IsDedicated)
							{
								throw new RestartGameException(Lang.Get("modwarning-assemblyloaded", new object[] { mod4.Info.ModID }));
							}
						}
					}
				}
			}
			this.logger.VerboseDebug("{0} assemblies loaded", new object[] { mods.Count });
			if (mods.Any((ModContainer mod) => mod.Error != null && mod.RequiresCompilation))
			{
				this.logger.Warning("One or more source code mods failed to compile. Info to modders: In case you cannot find the problem, be aware that the game engine currently can only compile C# code until version 5.0. Any language features from C#6.0 or above will result in compile errors.");
			}
			foreach (ModContainer mod2 in mods)
			{
				if (mod2.Enabled)
				{
					this.logger.VerboseDebug("Instantiate mod systems for {0}", new object[] { mod2.Info.ModID });
					mod2.InstantiateModSystems(this.side);
				}
			}
			this.contentAssetOrigins = new OrderedDictionary<string, IAssetOrigin>();
			this.themeAssetOrigins = new OrderedDictionary<string, IAssetOrigin>();
			OrderedDictionary<string, int> textureSizes = new OrderedDictionary<string, int>();
			foreach (ModContainer mod3 in mods.Where((ModContainer mod) => mod.Enabled))
			{
				this.loadedMods.Add(mod3.Info.ModID, mod3);
				enabledSystems.AddRange(mod3.Systems);
				if (mod3.FolderPath != null && Directory.Exists(Path.Combine(mod3.FolderPath, "assets")))
				{
					bool flag = mod3.Info.Type == EnumModType.Theme;
					OrderedDictionary<string, IAssetOrigin> origins = (flag ? this.themeAssetOrigins : this.contentAssetOrigins);
					FolderOrigin origin = (flag ? new ThemeFolderOrigin(mod3.FolderPath, (this.api.Side == EnumAppSide.Client) ? "textures/" : null) : new FolderOrigin(mod3.FolderPath, (this.api.Side == EnumAppSide.Client) ? "textures/" : null));
					origins.Add(mod3.FileName, origin);
					textureSizes.Add(mod3.FileName, mod3.Info.TextureSize);
				}
			}
			if (textureSizes.Count > 0)
			{
				this.TextureSize = textureSizes.Values.Last<int>();
			}
			enabledSystems = enabledSystems.OrderBy((ModSystem system) => system.ExecuteOrder()).ToList<ModSystem>();
			this.logger.Notification("Instantiated {0} mod systems from {1} enabled mods", new object[]
			{
				enabledSystems.Count,
				this.Mods.Count<Mod>()
			});
			return enabledSystems;
		}

		private void ClearCacheFolder(IEnumerable<ModContainer> mods)
		{
			if (!Directory.Exists(this.UnpackPath))
			{
				return;
			}
			foreach (string folder in Directory.GetDirectories(this.UnpackPath).Except(from mod in mods
				where mod.Error == null
				select mod.FolderPath, StringComparer.InvariantCultureIgnoreCase))
			{
				try
				{
					string[] files = Directory.GetFiles(folder, "*.dll");
					for (int i = 0; i < files.Length; i++)
					{
						File.Delete(files[i]);
					}
				}
				catch
				{
					break;
				}
				try
				{
					Directory.Delete(folder, true);
				}
				catch (Exception ex)
				{
					this.logger.Error("There was an exception deleting the cached mod folder '{0}':");
					this.logger.Error(ex);
				}
			}
		}

		private List<ModContainer> CollectMods()
		{
			List<DirectoryInfo> dirInfos = (from path in this.ModSearchPaths
				select new DirectoryInfo(path) into dirInfo
				group dirInfo by dirInfo.FullName.ToLowerInvariant() into @group
				select @group.First<DirectoryInfo>()).ToList<DirectoryInfo>();
			this.logger.Notification("Will search the following paths for mods:");
			foreach (DirectoryInfo dirInfo2 in dirInfos)
			{
				if (dirInfo2.Exists)
				{
					this.logger.Notification("    {0}", new object[] { StringUtil.SanitizePath(dirInfo2.FullName) });
				}
				else
				{
					this.logger.Notification("    {0} (Not found?)", new object[] { StringUtil.SanitizePath(dirInfo2.FullName) });
				}
			}
			return (from fsInfo in dirInfos.Where((DirectoryInfo dirInfo) => dirInfo.Exists).SelectMany((DirectoryInfo dirInfo) => dirInfo.GetFileSystemInfos())
				where ModContainer.GetSourceType(fsInfo) != null
				select new ModContainer(fsInfo, this.logger, this.traceLog)).OrderBy((ModContainer mod) => mod.FileName, StringComparer.OrdinalIgnoreCase).ToList<ModContainer>();
		}

		private int DisableMods(IEnumerable<ModContainer> mods, IEnumerable<string> disabledModsByIdAndVersion)
		{
			if (disabledModsByIdAndVersion == null)
			{
				return 0;
			}
			HashSet<string> disabledSet = new HashSet<string>(disabledModsByIdAndVersion);
			List<ModContainer> disabledMods = mods.Where((ModContainer mod) => ((mod != null) ? mod.Info : null) == null || disabledSet.Contains(mod.Info.ModID + "@" + mod.Info.Version) || disabledSet.Contains(mod.Info.ModID ?? "")).ToList<ModContainer>();
			foreach (ModContainer modContainer in disabledMods)
			{
				modContainer.Status = ModStatus.Disabled;
			}
			return disabledMods.Count<ModContainer>();
		}

		private void CheckDuplicateModIDMods(IEnumerable<ModContainer> mods)
		{
			foreach (IGrouping<string, ModContainer> duplicateMods in from mod in mods.Where(delegate(ModContainer mod)
				{
					ModInfo info = mod.Info;
					return ((info != null) ? info.ModID : null) != null && mod.Enabled;
				})
				group mod by mod.Info.ModID into @group
				where @group.Skip(1).Any<ModContainer>()
				select @group)
			{
				IOrderedEnumerable<ModContainer> sortedMods = duplicateMods.OrderBy((ModContainer mod) => mod.Info);
				ILogger logger = this.logger;
				string text = "Multiple mods share the mod ID '{0}' ({1}). Will only load the highest version one - v{2}.";
				object[] array = new object[3];
				array[0] = duplicateMods.Key;
				array[1] = string.Join(", ", duplicateMods.Select((ModContainer m) => "'" + m.FileName + "'"));
				array[2] = sortedMods.First<ModContainer>().Info.Version;
				logger.Warning(text, array);
				foreach (ModContainer modContainer in sortedMods.Skip(1))
				{
					modContainer.SetError(ModError.Loading);
				}
			}
		}

		private List<ModContainer> CheckAndSortDependencies(IEnumerable<ModContainer> mods)
		{
			mods = mods.Where((ModContainer mod) => mod.Error == null && mod.Enabled).ToList<ModContainer>();
			List<ModContainer> sorted = new List<ModContainer>();
			HashSet<ModContainer> toCheck = new HashSet<ModContainer>(mods);
			List<ModContainer> toRemove = new List<ModContainer>();
			Dictionary<string, ModContainer> lookup = mods.Where(delegate(ModContainer mod)
			{
				ModInfo info = mod.Info;
				return ((info != null) ? info.ModID : null) != null;
			}).ToDictionary((ModContainer mod) => mod.Info.ModID);
			do
			{
				toRemove.Clear();
				foreach (ModContainer mod4 in toCheck)
				{
					bool dependenciesMet = true;
					if (mod4.Info != null)
					{
						foreach (ModDependency dependency in mod4.Info.Dependencies)
						{
							ModContainer dependingMod;
							if (!lookup.TryGetValue(dependency.ModID, out dependingMod) || !this.SatisfiesVersion(dependency.Version, dependingMod.Info.Version) || !dependingMod.Enabled)
							{
								mod4.SetError(ModError.Dependency);
							}
							else if (toCheck.Contains(dependingMod))
							{
								dependenciesMet = false;
							}
						}
					}
					if (dependenciesMet)
					{
						toRemove.Add(mod4);
					}
				}
				foreach (ModContainer mod2 in toRemove)
				{
					toCheck.Remove(mod2);
					sorted.Add(mod2);
				}
			}
			while (toRemove.Count > 0);
			foreach (ModContainer mod3 in mods)
			{
				if (!mod3.Enabled && mod3.Status != ModStatus.Disabled)
				{
					mod3.Logger.Error("Could not resolve some dependencies:");
					foreach (ModDependency dependency2 in mod3.Info.Dependencies)
					{
						ModContainer dependingMod2;
						if (!lookup.TryGetValue(dependency2.ModID, out dependingMod2))
						{
							mod3.Logger.Error("    {0} - Missing", new object[] { dependency2 });
							this.MissingDependencies.Add(dependency2.ModID + "@" + dependency2.Version);
							if (mod3.MissingDependencies == null)
							{
								mod3.MissingDependencies = new List<string>();
							}
							mod3.MissingDependencies.Add(dependency2.ModID + "@" + dependency2.Version);
						}
						else if (!this.SatisfiesVersion(dependency2.Version, dependingMod2.Info.Version))
						{
							mod3.Logger.Error("    {0} - Version mismatch (has {1})", new object[]
							{
								dependency2,
								dependingMod2.Info.Version
							});
							this.MissingDependencies.Add(dependency2.ModID + "@" + dependency2.Version);
							if (mod3.MissingDependencies == null)
							{
								mod3.MissingDependencies = new List<string>();
							}
							mod3.MissingDependencies.Add(dependency2.ModID + "@" + dependency2.Version);
						}
						else
						{
							ModError? error = dependingMod2.Error;
							ModError modError = ModError.Loading;
							if ((error.GetValueOrDefault() == modError) & (error != null))
							{
								mod3.Logger.Error("    {0} - Dependency {1} failed loading", new object[] { dependency2, dependingMod2 });
							}
							else if (dependingMod2.Error.GetValueOrDefault() == ModError.Dependency)
							{
								mod3.Logger.Error("    {0} - Dependency {1} has dependency errors itself", new object[] { dependency2, dependingMod2 });
							}
							else if (!dependingMod2.Enabled)
							{
								mod3.Logger.Error("    {0} - Dependency {1} is not enabled", new object[] { dependency2, dependingMod2 });
							}
						}
					}
				}
			}
			if (toCheck.Count > 0)
			{
				this.logger.Warning("Possible cyclic dependencies between mods: " + string.Join<ModContainer>(", ", toCheck));
				sorted.AddRange(toCheck);
			}
			return sorted;
		}

		private bool SatisfiesVersion(string requested, string provided)
		{
			if (string.IsNullOrEmpty(requested) || string.IsNullOrEmpty(provided) || requested == "*")
			{
				return true;
			}
			SemVer reqVersion;
			SemVer.TryParse(requested, out reqVersion);
			SemVer provVersion;
			SemVer.TryParse(provided, out provVersion);
			return provVersion >= reqVersion;
		}

		public void RunModPhase(ModRunPhase phase)
		{
			this.RunModPhase(ref this.enabledSystems, phase);
		}

		public void RunModPhase(ref List<ModSystem> enabledSystems, ModRunPhase phase)
		{
			if (phase != ModRunPhase.Normal)
			{
				foreach (ModSystem system4 in enabledSystems)
				{
					if (system4 != null && system4.ShouldLoad(this.api) && !this.TryRunModPhase(system4.Mod, system4, this.api, phase))
					{
						this.logger.Error("Failed to run mod phase {0} for mod {1}", new object[] { phase, system4 });
					}
				}
				return;
			}
			List<ModSystem> startedSystems = new List<ModSystem>();
			foreach (ModSystem system2 in enabledSystems)
			{
				if (system2.ShouldLoad(this.api))
				{
					this.logger.VerboseDebug("Starting system: " + system2.GetType().Name);
					if (this.TryRunModPhase(system2.Mod, system2, this.api, ModRunPhase.Normal))
					{
						startedSystems.Add(system2);
					}
					else
					{
						this.logger.Error("Failed to start system {0}", new object[] { system2 });
					}
				}
			}
			this.logger.Notification("Started {0} systems on {1}:", new object[]
			{
				startedSystems.Count,
				this.api.Side
			});
			foreach (IGrouping<Mod, ModSystem> group in from system in startedSystems
				group system by system.Mod)
			{
				this.logger.Notification("    Mod {0}:", new object[] { group.Key });
				foreach (ModSystem system3 in group)
				{
					this.logger.Notification("        {0}", new object[] { system3 });
				}
			}
			enabledSystems = startedSystems;
		}

		private bool TryRunModPhase(Mod mod, ModSystem system, ICoreAPI api, ModRunPhase phase)
		{
			try
			{
				switch (phase)
				{
				case ModRunPhase.Pre:
					system.StartPre(api);
					break;
				case ModRunPhase.Start:
					system.Start(api);
					break;
				case ModRunPhase.AssetsLoaded:
					system.AssetsLoaded(api);
					break;
				case ModRunPhase.AssetsFinalize:
					system.AssetsFinalize(api);
					break;
				case ModRunPhase.Normal:
					if (api.Side == EnumAppSide.Client)
					{
						system.StartClientSide(api as ICoreClientAPI);
					}
					else
					{
						system.StartServerSide(api as ICoreServerAPI);
					}
					break;
				case ModRunPhase.Dispose:
					system.Dispose();
					break;
				}
				return true;
			}
			catch (FormatException ex2)
			{
				throw ex2;
			}
			catch (Exception ex)
			{
				mod.Logger.Error("An exception was thrown when trying to start the mod:");
				mod.Logger.Error(ex);
			}
			return false;
		}

		public void Dispose()
		{
			this.RunModPhase(ModRunPhase.Dispose);
		}

		private readonly ICoreAPI api;

		private readonly EnumAppSide side;

		private readonly ILogger logger;

		private bool traceLog;

		private readonly ModCompilationContext compilationContext = new ModCompilationContext();

		private Dictionary<string, ModContainer> loadedMods = new Dictionary<string, ModContainer>();

		private List<ModSystem> enabledSystems = new List<ModSystem>();

		public List<string> MissingDependencies = new List<string>();

		internal OrderedDictionary<string, IAssetOrigin> contentAssetOrigins;

		internal OrderedDictionary<string, IAssetOrigin> themeAssetOrigins;
	}
}
