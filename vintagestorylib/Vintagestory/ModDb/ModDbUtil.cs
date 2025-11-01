using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;

namespace Vintagestory.ModDb
{
	public class ModDbUtil
	{
		public bool IsLoading { get; private set; }

		public ModDbUtil(ICoreAPI api, string modDbUrl, string installPath)
		{
			this.api = api;
			this.modDbApiUrl = modDbUrl + "api/";
			this.modDbDownloadUrl = modDbUrl;
			this.installPath = installPath;
			this.cmdLetter = ((api.Side == EnumAppSide.Client) ? "." : "/");
		}

		private void ensureModsLoaded()
		{
			if (this.mods == null)
			{
				ModLoader modloader = this.api.ModLoader as ModLoader;
				this.mods = modloader.LoadModInfos();
			}
		}

		public string preConsoleCommand()
		{
			if (this.gameversions == null)
			{
				string result = null;
				this.modDbRequest("gameversions", delegate(EnumModDbResponse state, string text)
				{
					if (state == EnumModDbResponse.Good)
					{
						string error;
						this.gameversions = this.parseResponse<GameVersionResponse>(text, out error);
						if (error != null)
						{
							result = error;
							return;
						}
						if (this.gameversions != null)
						{
							this.loadVersionIds();
							result = null;
							return;
						}
						result = "Bad moddb response - no game versions";
						return;
					}
					else
					{
						if (state == EnumModDbResponse.Offline)
						{
							result = "Mod hub offline";
							return;
						}
						result = "Bad moddb response - " + text;
						return;
					}
				}, null);
				return result;
			}
			return null;
		}

		public void onInstallCommand(string modid, string forGameVersion, Action<string> onProgressUpdate)
		{
			this.ensureModsLoaded();
			this.SearchAndInstall(modid, forGameVersion ?? "1.21.5", delegate(string msg, EnumModInstallState state)
			{
				onProgressUpdate(msg);
			}, true);
		}

		public void onRemoveCommand(string modid, Action<string> onProgressUpdate)
		{
			this.ensureModsLoaded();
			foreach (ModContainer val in this.mods)
			{
				if (val.Status != ModStatus.Errored && val.Info.ModID == modid)
				{
					File.Delete(val.SourcePath);
					onProgressUpdate("modutil-modremoved");
					return;
				}
			}
			onProgressUpdate("No such mod found.");
		}

		public void onListCommand(Action<string> onProgressUpdate)
		{
			this.ensureModsLoaded();
			List<string> modids = new List<string>();
			foreach (ModContainer val in this.mods)
			{
				if (val.Status != ModStatus.Errored && val.Info.ModID != "game" && val.Info.ModID != "creative" && val.Info.ModID != "survival")
				{
					modids.Add(val.Info.ModID);
				}
			}
			if (modids.Count == 0)
			{
				onProgressUpdate(Lang.Get("modutil-list-none", Array.Empty<object>()));
				return;
			}
			onProgressUpdate(Lang.Get("modutil-list", new object[]
			{
				modids.Count,
				string.Join(", ", modids)
			}));
		}

		public void onSearchforCommand(string version, string modid, Action<string> onProgressUpdate)
		{
			this.ensureModsLoaded();
			int verid = -1;
			foreach (GameVersionEntry val in this.gameversions.GameVersions)
			{
				if (val.Name == "v" + version)
				{
					verid = val.TagId;
				}
			}
			if (verid <= 0)
			{
				onProgressUpdate("No such version is listed on the moddb");
				return;
			}
			int[] verids = new int[] { verid };
			this.search(modid, onProgressUpdate, verids);
		}

		public void onSearchforAndCompatibleCommand(string version, string modid, Action<string> onProgressUpdate)
		{
			this.ensureModsLoaded();
			string major = version.Substring(0, 1);
			string minor = version.Substring(2, 3);
			List<int> sameminvids = new List<int>();
			foreach (GameVersionEntry val in this.gameversions.GameVersions)
			{
				if (val.Name.StartsWithOrdinal("v" + major + "." + minor))
				{
					sameminvids.Add(val.TagId);
				}
			}
			int[] verids = sameminvids.ToArray();
			this.search(modid, onProgressUpdate, verids);
		}

		public void onSearchCommand(string modid, Action<string> onProgressUpdate)
		{
			this.ensureModsLoaded();
			this.search(modid, onProgressUpdate, new int[] { this.selfGameVersionId });
		}

		public void onSearchCompatibleCommand(string modid, Action<string> onProgressUpdate)
		{
			this.ensureModsLoaded();
			this.search(modid, onProgressUpdate, this.sameMinorVersionIds);
		}

		public void SearchAndInstall(string modid, string forGameVersion, ModInstallProgressUpdate onDone, bool deletedOutdated)
		{
			this.ensureModsLoaded();
			string[] modidparts = modid.Split('@', StringSplitOptions.None);
			this.api.Logger.Debug("ModDbUtil.SearchAndInstall(): Request to mod/" + modidparts[0]);
			this.modDbRequest("mod/" + modidparts[0], delegate(EnumModDbResponse state, string text)
			{
				this.api.Logger.Debug("ModDbUtil.SearchAndInstall(): Response: {0}", new object[] { text });
				if (state == EnumModDbResponse.Good)
				{
					string error;
					ModDbEntryResponse modentry = this.parseResponse<ModDbEntryResponse>(text, out error);
					if (error != null)
					{
						if (modentry != null && modentry.StatusCode == 404)
						{
							onDone(Lang.Get("modinstall-notfound", new object[] { modid }), EnumModInstallState.NotFound);
							return;
						}
						onDone(error, EnumModInstallState.Error);
						return;
					}
					else
					{
						ICoreServerAPI sapi = this.api as ICoreServerAPI;
						if (sapi != null && sapi.Server.Config.HostedMode)
						{
							if (sapi.Server.Config.HostedModeAllowMods)
							{
								if (modentry.Mod.Releases.Any((ModEntryRelease r) => r.HostedModeAllow))
								{
									modentry.Mod.Releases = modentry.Mod.Releases.Where((ModEntryRelease r) => r.HostedModeAllow).ToArray<ModEntryRelease>();
									this.installMod(modentry, onDone, forGameVersion, deletedOutdated, modid);
									return;
								}
							}
							onDone(Lang.Get("modinstall-notallowed", new object[] { modid }), EnumModInstallState.Error);
							return;
						}
						this.installMod(modentry, onDone, forGameVersion, deletedOutdated, modid);
						return;
					}
				}
				else
				{
					if (state == EnumModDbResponse.Offline)
					{
						onDone(Lang.Get("modinstall-offline", new object[] { modid }), EnumModInstallState.Offline);
						return;
					}
					onDone(Lang.Get("modinstall-badresponse", new object[] { modid, text }), EnumModInstallState.Error);
					return;
				}
			}, null);
		}

		private void loadVersionIds()
		{
			List<int> sameMinorVersionIds = new List<int>();
			string major = "1.21.5".Substring(0, 1);
			string minor = "1.21.5".Substring(2, 3);
			string shortVersion = "v1.21.5";
			string longVersion = "v" + major + "." + minor;
			foreach (GameVersionEntry val in this.gameversions.GameVersions)
			{
				if (val.Name == shortVersion)
				{
					this.selfGameVersionId = val.TagId;
				}
				if (val.Name.StartsWithOrdinal(longVersion))
				{
					sameMinorVersionIds.Add(val.TagId);
				}
			}
			this.sameMinorVersionIds = sameMinorVersionIds.ToArray();
		}

		private void search(string stext, Action<string> onDone, int[] gameversionIds)
		{
			if (stext == null)
			{
				onDone("Syntax: " + this.cmdLetter + "moddb search [text]");
				return;
			}
			this.Search(stext, delegate(ModSearchResult searchResult)
			{
				if (searchResult.Mods == null)
				{
					onDone(searchResult.StatusMessage);
					return;
				}
				StringBuilder sbr = new StringBuilder();
				if (searchResult.Mods.Length == 0)
				{
					sbr.AppendLine(Lang.Get("Found no mods compatible for your game version", Array.Empty<object>()));
				}
				else
				{
					sbr.AppendLine(Lang.Get("Found {0} compatible mods. Name and mod id:", new object[] { searchResult.Mods.Length }));
				}
				int i = 0;
				foreach (ModDbEntrySearchResponse val in searchResult.Mods)
				{
					sbr.AppendLine(Lang.Get("{0}: <strong>{1}</strong>", new object[]
					{
						val.Name,
						val.ModIdStrs[0]
					}));
					i++;
					if (i > 10)
					{
						sbr.AppendLine("and more...");
						break;
					}
				}
				onDone(sbr.ToString());
			}, gameversionIds, null, null, 100);
		}

		public void Search(string stext, Action<ModSearchResult> onDone, int[] gameversionIds, string mv = null, string sortBy = null, int limit = 100)
		{
			List<string> getParams = new List<string>();
			foreach (int tagid in gameversionIds)
			{
				if (tagid != -1)
				{
					getParams.Add("gv[]=" + tagid.ToString());
				}
			}
			getParams.Add("text=" + stext);
			if (mv != null)
			{
				getParams.Add("mv=" + mv);
			}
			if (sortBy != null)
			{
				getParams.Add("sortby=" + sortBy);
			}
			getParams.Add("limit=" + limit.ToString());
			this.modDbRequest("mods?" + string.Join("&", getParams), delegate(EnumModDbResponse state, string text)
			{
				if (state == EnumModDbResponse.Good)
				{
					string error;
					ModSearchResult searchResult = this.parseResponse<ModSearchResult>(text, out error);
					if (error != null)
					{
						onDone(new ModSearchResult
						{
							StatusCode = 500,
							StatusMessage = error
						});
						return;
					}
					searchResult.Mods = searchResult.Mods.Where((ModDbEntrySearchResponse m) => m.Type.Equals("mod")).ToArray<ModDbEntrySearchResponse>();
					onDone(searchResult);
					return;
				}
				else
				{
					if (state == EnumModDbResponse.Offline)
					{
						onDone(new ModSearchResult
						{
							StatusCode = 500,
							StatusMessage = Lang.Get("Mod hub offline", Array.Empty<object>())
						});
						return;
					}
					onDone(new ModSearchResult
					{
						StatusCode = 500,
						StatusMessage = Lang.Get("Bad moddb response - {0}", new object[] { text })
					});
					return;
				}
			}, null);
		}

		private void installMod(ModDbEntryResponse modentry, ModInstallProgressUpdate onProgressUpdate, string forGameVersion, bool deleteOutdated, string installExactVer = null)
		{
			ModEntryRelease selectedRelease = null;
			string installExactModVersion = null;
			if (installExactVer != null)
			{
				string[] modidparts = ((installExactVer != null) ? installExactVer.Split('@', StringSplitOptions.None) : null);
				installExactModVersion = ((modidparts.Length > 1) ? modidparts[1] : null);
				onProgressUpdate(Lang.Get("Checking {0}...", new object[] { installExactVer }) + " ", EnumModInstallState.InProgress);
			}
			else
			{
				onProgressUpdate(Lang.Get("Checking {0}...", new object[] { modentry.Mod.Name }) + " ", EnumModInstallState.InProgress);
			}
			if (installExactModVersion != null && installExactModVersion != "*")
			{
				foreach (ModEntryRelease release in modentry.Mod.Releases)
				{
					if (release.ModVersion == installExactModVersion)
					{
						selectedRelease = release;
					}
				}
				if (selectedRelease == null)
				{
					onProgressUpdate(Lang.Get("modinstall-versionnotfound", new object[]
					{
						modentry.Mod.Name,
						installExactModVersion
					}), EnumModInstallState.NotFound);
					return;
				}
			}
			else
			{
				List<ModEntryRelease> compaReleases = new List<ModEntryRelease>();
				HashSet<string> gameVersions = new HashSet<string>();
				foreach (ModEntryRelease release2 in modentry.Mod.Releases)
				{
					if (release2.Tags.Contains(forGameVersion) || release2.Tags.Contains("v" + forGameVersion))
					{
						compaReleases.Add(release2);
					}
					foreach (string tag in release2.Tags)
					{
						gameVersions.Add(tag.Substring(1));
					}
				}
				if (compaReleases.Count == 0)
				{
					onProgressUpdate(Lang.Get("mod-outdated-notavailable", new object[]
					{
						string.Join(", ", gameVersions),
						this.cmdLetter,
						modentry.Mod.Releases[0].ModIdStr
					}), EnumModInstallState.TooOld);
					return;
				}
				compaReleases.Sort(delegate(ModEntryRelease mod1, ModEntryRelease mod2)
				{
					if (mod1.ModVersion == mod2.ModVersion)
					{
						return 0;
					}
					if (!GameVersion.IsNewerVersionThan(mod1.ModVersion, mod2.ModVersion))
					{
						return 1;
					}
					return -1;
				});
				selectedRelease = compaReleases[0];
			}
			foreach (ModContainer mod in this.mods)
			{
				if (mod.Enabled && mod.Info.ModID == selectedRelease.ModIdStr)
				{
					if (mod.Info.Version == selectedRelease.ModVersion)
					{
						onProgressUpdate(Lang.Get("mod-installed-willenable", Array.Empty<object>()), EnumModInstallState.InstalledOrReady);
						List<string> disabledMods = ClientSettings.DisabledMods;
						disabledMods.Remove(mod.Info.ModID + "@" + mod.Info.Version);
						ClientSettings.DisabledMods = disabledMods;
						ClientSettings.Inst.Save(true);
						return;
					}
					if (deleteOutdated)
					{
						onProgressUpdate(Lang.Get("{0} v{1} is already installed, which is outdated. Will delete it.", new object[]
						{
							modentry.Mod.Name,
							mod.Info.Version
						}), EnumModInstallState.InstalledOrReady);
						File.Delete(mod.SourcePath);
					}
				}
			}
			onProgressUpdate(Lang.GetWithFallback("mod-found-downloading", "found! Downloading...", Array.Empty<object>()), EnumModInstallState.InProgress);
			Console.WriteLine(Lang.Get("Downloading {0}...", new object[] { selectedRelease.Filename }) + " ");
			string filepath = Path.Combine(this.installPath, selectedRelease.Filename);
			GamePaths.EnsurePathExists(this.installPath);
			try
			{
				using (Stream streamAsync = VSWebClient.Inst.GetStreamAsync(new Uri(this.modDbDownloadUrl + "download?fileid=" + selectedRelease.Fileid)).Result)
				{
					using (FileStream fileStream = new FileStream(filepath, FileMode.Create))
					{
						streamAsync.CopyTo(fileStream);
						onProgressUpdate(Lang.Get("mod-successfully-downloaded", new object[] { new FileInfo(filepath).Length / 1024L }), EnumModInstallState.InstalledOrReady);
					}
				}
			}
			catch (Exception e)
			{
				onProgressUpdate("Failed to download mod " + selectedRelease.Filename + " | " + e.Message, EnumModInstallState.Error);
			}
		}

		private void modDbRequest(string action, ModDbResponseDelegate onComplete, FormUrlEncodedContent postData = null)
		{
			this.IsLoading = true;
			Uri uri = new Uri(this.modDbApiUrl + action);
			this.api.Logger.Notification("Send request: {0}", new object[] { action });
			VSWebClient.Inst.PostAsync(uri, postData, delegate(CompletedArgs args)
			{
				this.api.Event.EnqueueMainThreadTask(delegate
				{
					if (args.State != CompletionState.Good)
					{
						onComplete(EnumModDbResponse.Offline, null);
						return;
					}
					if (args.Response == null)
					{
						onComplete(EnumModDbResponse.Bad, null);
						return;
					}
					this.IsLoading = false;
					onComplete(EnumModDbResponse.Good, args.Response);
				}, "moddbrequest");
			});
		}

		public T parseResponse<T>(string text, out string errorText) where T : ModDbResponse
		{
			errorText = null;
			T response;
			try
			{
				response = JsonConvert.DeserializeObject<T>(text);
			}
			catch (Exception e)
			{
				this.api.Logger.Notification("{0}", new object[] { e });
				errorText = LoggerBase.CleanStackTrace(e.ToString());
				return default(T);
			}
			if (response.StatusCode != 200)
			{
				errorText = "Invalid request - " + response.StatusCode.ToString();
			}
			return response;
		}

		public static async Task GetBlockedModsAsync(ILogger logger)
		{
			if (ModDbUtil.ModBlockList == null)
			{
				int num = 0;
				try
				{
					ModDbUtil.blockModDownloadTries += 1;
					ModDbUtil.ModBlockList = JsonConvert.DeserializeObject<ModBlock[]>(await VSWebClient.Inst.GetStringAsync("https://cdn.vintagestory.at/api/blockedmods.json")).ToDictionary((ModBlock b) => b.Id, (ModBlock b) => b.reason);
				}
				catch (Exception obj)
				{
					num = 1;
				}
				object obj;
				if (num == 1)
				{
					Exception e = (Exception)obj;
					logger.Warning("Could not get blocked mods from api");
					logger.Warning(e);
					if (ModDbUtil.blockModDownloadTries < 2)
					{
						logger.Notification("Trying again to get blocked mods list ...");
						Thread.Sleep(100);
						await ModDbUtil.GetBlockedModsAsync(logger);
						return;
					}
				}
				obj = null;
				if (ModDbUtil.ModBlockList == null)
				{
					ModDbUtil.ModBlockList = new Dictionary<string, string>();
				}
			}
		}

		public static bool IsModBlocked(string modId, string version, out string reason)
		{
			return ModDbUtil.ModBlockList.TryGetValue(modId + "@" + version, out reason) || ModDbUtil.ModBlockList.TryGetValue(modId ?? "", out reason);
		}

		private string installPath;

		private string modDbApiUrl;

		private string modDbDownloadUrl;

		private ICoreAPI api;

		private string cmdLetter;

		private static short blockModDownloadTries;

		public static Dictionary<string, string> ModBlockList;

		private GameVersionResponse gameversions;

		public int selfGameVersionId = -1;

		public int[] sameMinorVersionIds = Array.Empty<int>();

		private List<ModContainer> mods;
	}
}
