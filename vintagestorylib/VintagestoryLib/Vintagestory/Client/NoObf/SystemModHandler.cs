using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using ProperVersion;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.ClientNative;
using Vintagestory.Common;
using Vintagestory.ModDb;

namespace Vintagestory.Client.NoObf
{
	public class SystemModHandler : ClientSystem
	{
		public override string Name
		{
			get
			{
				return "modhandler";
			}
		}

		public SystemModHandler(ClientMain game)
			: base(game)
		{
		}

		public override void OnServerIdentificationReceived()
		{
			if (!this.game.IsSingleplayer)
			{
				List<string> modSearchPaths = new List<string>(ClientSettings.ModPaths);
				if (ScreenManager.ParsedArgs.AddModPath != null)
				{
					modSearchPaths.AddRange(ScreenManager.ParsedArgs.AddModPath);
				}
				if (this.game.Connectdata.Host != null)
				{
					string path = Path.Combine(GamePaths.DataPathServerMods, GamePaths.ReplaceInvalidChars(this.game.Connectdata.Host + "-" + this.game.Connectdata.Port.ToString()));
					if (Directory.Exists(path))
					{
						modSearchPaths.Add(path);
					}
				}
				this.game.Logger.Notification("Loading and pre-starting client side mods...");
				this.loader = new ModLoader(this.game.api, modSearchPaths, ScreenManager.ParsedArgs.TraceLog);
				this.game.api.modLoader = this.loader;
				List<ModContainer> allMods = this.game.api.modLoader.LoadModInfos();
				List<string> disableMods = new List<string>();
				Dictionary<string, ModId> serverModsById = this.game.ServerMods.ToDictionary((ModId t) => t.Id, (ModId t) => t);
				foreach (ModContainer cMod in allMods)
				{
					ModId mod3;
					if (cMod.Info != null && serverModsById.TryGetValue(cMod.Info.ModID, out mod3) && mod3.Version != cMod.Info.Version && mod3.Id != "game" && mod3.Id != "creative" && mod3.Id != "survival")
					{
						disableMods.Add(mod3.Id + "@" + cMod.Info.Version);
					}
				}
				List<ModContainer> mods = this.game.api.modLoader.DisableAndVerify(allMods, disableMods);
				disableMods.AddRange(ClientSettings.DisabledMods);
				List<string> availableModsOnClient = (from mod in mods
					where mod.Info.Side == EnumAppSide.Universal && mod.Error == null
					select mod.Info.ModID + "@" + mod.Info.NetworkVersion).ToList<string>();
				List<string> availableUniversalMods = (from mod in mods
					where mod.Info.Side == EnumAppSide.Universal && mod.Info.RequiredOnServer && mod.Error == null
					select mod.Info.ModID + "@" + mod.Info.NetworkVersion).ToList<string>();
				List<string> missingModsOnClient = (from modid in (from mod in this.game.ServerMods
						where mod.RequiredOnClient
						select mod.Id + "@" + mod.NetworkVersion).ToList<string>().Except(availableModsOnClient)
					where !modid.StartsWithOrdinal("game@") && !modid.StartsWithOrdinal("creative@") && !modid.StartsWithOrdinal("survival@")
					select modid into modidver
					select this.game.ServerMods.FirstOrDefault((ModId mod) => mod.Id + "@" + mod.NetworkVersion == modidver) into mod
					select mod.Id + "@" + mod.Version).ToList<string>();
				List<string> requiredCoreModVersions = (from modid in (from modidver in missingModsOnClient
						select mods.FirstOrDefault((ModContainer mod) => mod.Info.ModID + "@" + mod.Info.Version == modidver && mod.Error.GetValueOrDefault() == ModError.Dependency) into mod
						where ((mod != null) ? mod.MissingDependencies : null) != null
						select mod).SelectMany((ModContainer mod) => mod.MissingDependencies)
					where modid.StartsWithOrdinal("game@") || modid.StartsWithOrdinal("creative@") || modid.StartsWithOrdinal("survival@")
					select modid.Split("@", StringSplitOptions.None)[1]).ToList<string>();
				if (requiredCoreModVersions.Count > 0)
				{
					requiredCoreModVersions.Sort((string x, string y) => SemVer.Compare(SemVer.Parse(x), SemVer.Parse(y)));
					this.game.disconnectReason = Lang.Get("disconnect-modrequiresnewerclient", new object[] { requiredCoreModVersions[0] });
					this.game.exitReason = "client<=>server game version mismatch";
					this.game.DestroyGameSession(true);
					return;
				}
				if (missingModsOnClient.Count > 0)
				{
					List<string> erroringMods = new List<string>();
					using (List<string>.Enumerator enumerator2 = missingModsOnClient.GetEnumerator())
					{
						while (enumerator2.MoveNext())
						{
							string modid = enumerator2.Current;
							ModContainer erroringMod = mods.FirstOrDefault((ModContainer mod) => modid == mod.Info.ModID + "@" + mod.Info.NetworkVersion && mod.Error != null);
							if (erroringMod != null)
							{
								erroringMods.Add(erroringMod.Info.ModID + "@" + erroringMod.Info.Version);
							}
						}
					}
					foreach (string val in erroringMods)
					{
						missingModsOnClient.Remove(val);
					}
					this.game.Logger.Notification("Disconnected, modded server with lacking mods on the client side. Mods in question: {0}, our available mods: {1}", new object[]
					{
						string.Join(", ", missingModsOnClient),
						string.Join(", ", availableModsOnClient)
					});
					if (erroringMods.Count > 0)
					{
						this.game.disconnectReason = Lang.Get("joinerror-modsmissing-modserroring", new object[]
						{
							string.Join(", ", missingModsOnClient).Replace("@", " v"),
							string.Join(", ", erroringMods).Replace("@", " v")
						});
					}
					else
					{
						this.game.disconnectReason = Lang.Get("joinerror-modsmissing", new object[] { string.Join(", ", missingModsOnClient).Replace("@", " v") });
					}
					this.game.disconnectAction = "trydownloadmods";
					this.game.disconnectMissingMods = missingModsOnClient;
					this.game.DestroyGameSession(true);
					return;
				}
				foreach (ModId mod2 in this.game.ServerMods)
				{
					disableMods.Remove(mod2.Id + "@" + mod2.Version);
				}
				List<string> serverMods = this.game.ServerMods.Select((ModId mod) => mod.Id + "@" + mod.NetworkVersion).ToList<string>();
				List<string> missingModsOnServer = availableUniversalMods.Except(serverMods).ToList<string>();
				disableMods.AddRange(missingModsOnServer);
				disableMods.AddRange(this.game.ServerModIdBlacklist);
				if (this.game.ServerModIdWhitelist.Count > 0)
				{
					List<string> modWhitelist = this.game.ServerModIdWhitelist.ToList<string>();
					if (this.game.ServerModIdWhitelist.Count == 1 && this.game.ServerModIdWhitelist[0].Contains("game"))
					{
						modWhitelist = new List<string>();
					}
					IEnumerable<string> notAllowedMods = from mod in mods
						where mod.Info.Side == EnumAppSide.Client
						where modWhitelist.All((string serverModId) => !(mod.Info.ModID + "@" + mod.Info.Version).Contains(serverModId))
						select mod.Info.ModID + "@" + mod.Info.Version;
					disableMods.AddRange(notAllowedMods);
				}
				this.loader.LoadMods(mods, disableMods);
				CrashReporter.LoadedMods = mods.Where((ModContainer mod) => mod.Enabled).ToList<ModContainer>();
				this.game.textureSize = this.loader.TextureSize;
				this.PreStartMods();
				this.StartMods();
				this.ReloadExternalAssets();
			}
		}

		internal void SinglePlayerStart()
		{
			List<string> modSearchPaths = new List<string>(ClientSettings.ModPaths);
			if (ScreenManager.ParsedArgs.AddModPath != null)
			{
				modSearchPaths.AddRange(ScreenManager.ParsedArgs.AddModPath);
			}
			this.game.Logger.Notification("Loading and pre-starting client side mods...");
			this.loader = new ModLoader(this.game.api, modSearchPaths, ScreenManager.ParsedArgs.TraceLog);
			this.game.api.modLoader = this.loader;
			List<ModContainer> allMods = this.loader.LoadModInfos();
			List<string> disableMods = new List<string>();
			disableMods.AddRange(ClientSettings.DisabledMods);
			List<ModContainer> mods = this.loader.DisableAndVerify(allMods, disableMods);
			if (this.loader.MissingDependencies.Count <= 0)
			{
				if (!ClientSettings.DisableModSafetyCheck)
				{
					while (ModDbUtil.ModBlockList == null)
					{
						Thread.Sleep(20);
					}
				}
				this.loader.LoadMods(mods, disableMods);
				CrashReporter.LoadedMods = mods.Where((ModContainer mod) => mod.Enabled).ToList<ModContainer>();
				this.game.textureSize = this.loader.TextureSize;
				return;
			}
			List<string> requiredCoreModVersions = (from modid in this.loader.MissingDependencies
				where modid.StartsWithOrdinal("game@") || modid.StartsWithOrdinal("creative@") || modid.StartsWithOrdinal("survival@")
				select modid.Split("@", StringSplitOptions.None)[1]).ToList<string>();
			if (requiredCoreModVersions.Count > 0)
			{
				requiredCoreModVersions.Sort((string x, string y) => SemVer.Compare(SemVer.Parse(x), SemVer.Parse(y)));
				StringBuilder sb = new StringBuilder();
				sb.AppendLine(Lang.Get("disconnect-modrequiresnewerclient", new object[] { requiredCoreModVersions[0] }));
				sb.AppendLine();
				sb.AppendLine(Lang.Get("disconnect-modrequiresnewerclient-sp", Array.Empty<object>()));
				this.game.disconnectReason = sb.ToString();
				this.game.disconnectAction = "disconnectSP";
				this.game.exitReason = "mod requiers newer game version";
				this.game.DestroyGameSession(true);
				return;
			}
			this.game.disconnectReason = Lang.Get("joinerror-modsmissing", new object[] { string.Join(", ", this.loader.MissingDependencies).Replace("@", " v") });
			this.game.disconnectAction = "trydownloadmods";
			this.game.disconnectMissingMods = this.loader.MissingDependencies;
			this.game.DestroyGameSession(true);
		}

		internal void PreStartMods()
		{
			this.loader.RunModPhase(ModRunPhase.Pre);
			this.game.Logger.Notification("Done loading and pre-starting client side mods.");
		}

		internal void ReloadExternalAssets()
		{
			this.game.Logger.VerboseDebug("Searching file system (including mods) for asset files");
			this.game.Platform.AssetManager.AddExternalAssets(this.game.Logger, this.loader);
			this.game.Logger.VerboseDebug("Finished the search for asset files");
			foreach (KeyValuePair<string, ITranslationService> locale in Lang.AvailableLanguages)
			{
				locale.Value.Invalidate();
			}
			Lang.Load(this.game.Logger, this.game.AssetManager, ClientSettings.Language);
			this.game.Logger.Notification("Reloaded lang file now with mod assets");
			this.game.Logger.VerboseDebug("Loaded lang file: " + ClientSettings.Language);
		}

		internal void OnAssetsLoaded()
		{
			this.loader.RunModPhase(ModRunPhase.AssetsLoaded);
		}

		internal override void OnLevelFinalize()
		{
			this.loader.RunModPhase(ModRunPhase.AssetsFinalize);
		}

		internal void StartMods()
		{
			this.loader.RunModPhase(ModRunPhase.Start);
		}

		internal void StartModsFully()
		{
			this.loader.RunModPhase(ModRunPhase.Normal);
		}

		private void onReloadMods(int groupId, CmdArgs args)
		{
		}

		public override void OnBlockTexturesLoaded()
		{
			this.game.api.Logger.VerboseDebug("Trigger mod event OnBlockTexturesLoaded");
			this.game.api.eventapi.TriggerBlockTexturesLoaded();
		}

		public override void Dispose(ClientMain game)
		{
			base.Dispose(game);
			ModLoader modLoader = this.loader;
			if (modLoader != null)
			{
				modLoader.Dispose();
			}
			CrashReporter.LoadedMods.Clear();
		}

		public override EnumClientSystemType GetSystemType()
		{
			return EnumClientSystemType.Misc;
		}

		private ModLoader loader;
	}
}
