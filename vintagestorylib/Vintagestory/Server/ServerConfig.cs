using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.Server.Network;

namespace Vintagestory.Server
{
	[JsonObject(MemberSerialization.OptIn)]
	public class ServerConfig : IServerConfig
	{
		public event Action onUpnpChanged;

		public event Action onAdvertiseChanged;

		[JsonProperty]
		public string FileEditWarning { get; set; }

		[JsonProperty]
		public string ConfigVersion { get; set; }

		[JsonProperty]
		public string ServerName { get; set; }

		[JsonProperty]
		public string ServerUrl { get; set; }

		[JsonProperty]
		public string ServerDescription { get; set; }

		[JsonProperty]
		public string WelcomeMessage { get; set; }

		[JsonProperty]
		public string Ip { get; set; }

		[JsonProperty]
		public int Port { get; set; }

		[JsonProperty]
		public bool Upnp
		{
			get
			{
				return this.upnp;
			}
			set
			{
				this.upnp = value;
				Action action = this.onUpnpChanged;
				if (action == null)
				{
					return;
				}
				action();
			}
		}

		[JsonProperty]
		public bool CompressPackets { get; set; }

		[JsonProperty]
		public bool AdvertiseServer
		{
			get
			{
				return this.advertiseServer;
			}
			set
			{
				this.advertiseServer = value;
				Action action = this.onAdvertiseChanged;
				if (action == null)
				{
					return;
				}
				action();
			}
		}

		[JsonIgnore]
		public int MaxClientsProgArgs { get; set; } = -1;

		[JsonProperty]
		public int MaxClients
		{
			get
			{
				if (this.MaxClientsProgArgs == -1)
				{
					return this.maxClients;
				}
				return this.MaxClientsProgArgs;
			}
			set
			{
				this.maxClients = value;
				if (this.MaxClientsProgArgs != -1)
				{
					this.MaxClientsProgArgs = value;
				}
			}
		}

		[JsonProperty]
		public int MaxClientsInQueue { get; set; }

		[JsonProperty]
		public bool PassTimeWhenEmpty { get; set; }

		[JsonProperty]
		public string MasterserverUrl { get; set; }

		[JsonProperty]
		public string ModDbUrl { get; set; }

		[JsonProperty]
		public int ClientConnectionTimeout { get; set; }

		[JsonProperty]
		public bool EntityDebugMode { get; set; }

		[JsonProperty]
		public string Password { get; set; }

		[JsonProperty]
		public int MapSizeX { get; set; }

		[JsonProperty]
		public int MapSizeY { get; set; }

		[JsonProperty]
		public int MapSizeZ { get; set; }

		[JsonProperty]
		public string ServerLanguage { get; set; }

		[JsonProperty]
		public int MaxChunkRadius { get; set; }

		[JsonProperty]
		public float TickTime { get; set; }

		[JsonProperty]
		public float SpawnCapPlayerScaling { get; set; }

		[JsonProperty]
		public int BlockTickChunkRange { get; set; }

		[JsonProperty]
		public int MaxMainThreadBlockTicks { get; set; }

		[JsonProperty]
		public int RandomBlockTicksPerChunk { get; set; }

		[JsonProperty]
		public int BlockTickInterval { get; set; }

		[JsonProperty]
		public int SkipEveryChunkRow { get; set; }

		[JsonProperty]
		public int SkipEveryChunkRowWidth { get; set; }

		[JsonProperty]
		public List<PlayerRole> Roles { get; set; }

		[JsonProperty]
		public string DefaultRoleCode { get; set; }

		[JsonProperty]
		public string[] ModPaths { get; set; }

		[JsonProperty]
		public EnumProtectionLevel AntiAbuse { get; set; }

		[JsonProperty]
		public StartServerArgs WorldConfig { get; set; }

		[JsonProperty]
		public int NextPlayerGroupUid { get; set; }

		[JsonProperty]
		public int GroupChatHistorySize { get; set; }

		[JsonProperty]
		public int MaxOwnedGroupChannelsPerUser { get; set; }

		[JsonProperty]
		[Obsolete("No longer used. Use WhitelistMode instead")]
		private bool OnlyWhitelisted { get; set; }

		[JsonProperty]
		public EnumWhitelistMode WhitelistMode { get; set; }

		[JsonProperty]
		public bool VerifyPlayerAuth { get; set; }

		[JsonProperty]
		[Obsolete("No longer used. Retrieve value from the savegame instead")]
		public PlayerSpawnPos DefaultSpawn { get; set; }

		[JsonProperty]
		public bool AllowPvP { get; set; }

		[JsonProperty]
		public bool AllowFireSpread { get; set; }

		[JsonProperty]
		public bool AllowFallingBlocks { get; set; }

		[JsonProperty]
		public bool HostedMode { get; set; }

		[JsonProperty]
		public bool HostedModeAllowMods { get; set; }

		[JsonProperty]
		public string VhIdentifier { get; set; }

		[JsonProperty]
		public string StartupCommands { get; set; }

		[JsonProperty]
		public bool RepairMode { get; set; }

		[JsonProperty]
		public bool AnalyzeMode { get; set; }

		[JsonProperty]
		public bool CorruptionProtection { get; set; }

		[JsonProperty]
		public bool RegenerateCorruptChunks { get; set; }

		List<IPlayerRole> IServerConfig.Roles
		{
			get
			{
				return this.Roles.Select((PlayerRole e) => e).ToList<IPlayerRole>();
			}
		}

		[JsonProperty]
		public int ChatRateLimitMs { get; set; }

		[JsonProperty]
		public int DieBelowDiskSpaceMb { get; set; }

		[JsonProperty]
		public string[] ModIdBlackList { get; set; }

		[JsonProperty]
		public string[] ModIdWhiteList { get; set; }

		[JsonProperty]
		public string ServerIdentifier { get; set; }

		[JsonProperty]
		public bool LogBlockBreakPlace { get; set; }

		[JsonProperty]
		public uint LogFileSplitAfterLine { get; set; }

		[JsonProperty]
		public int DieAboveErrorCount { get; set; }

		[JsonProperty]
		public bool LoginFloodProtection { get; set; }

		[JsonProperty]
		public bool TemporaryIpBlockList
		{
			get
			{
				return this.temporaryIpBlockList;
			}
			set
			{
				this.temporaryIpBlockList = value;
				TcpNetConnection.TemporaryIpBlockList = value;
			}
		}

		[JsonProperty]
		public bool DisableModSafetyCheck { get; set; }

		[JsonProperty]
		public int DieAboveMemoryUsageMb { get; set; }

		public bool IsPasswordProtected()
		{
			return !string.IsNullOrEmpty(this.Password);
		}

		public int GetMaxClients(ServerMain server)
		{
			return this.MaxClients;
		}

		public ServerConfig()
		{
			this.ConfigVersion = "1.7";
			this.ServerName = "Vintage Story Server";
			this.WelcomeMessage = Lang.GetUnformatted("survive-and-star-trek");
			this.DefaultRoleCode = "suplayer";
			this.MasterserverUrl = "http://masterserver.vintagestory.at/api/v1/servers/";
			this.ModDbUrl = "https://mods.vintagestory.at/";
			this.VerifyPlayerAuth = true;
			this.AdvertiseServer = false;
			this.CorruptionProtection = true;
			this.Port = 42420;
			this.MaxClients = 16;
			this.ClientConnectionTimeout = 150;
			this.MapSizeX = 1024000;
			this.MapSizeY = 256;
			this.MapSizeZ = 1024000;
			this.Seed = null;
			this.SpawnCapPlayerScaling = 0.5f;
			this.ServerLanguage = "en";
			this.MaxChunkRadius = 12;
			this.SkipEveryChunkRow = 0;
			this.SkipEveryChunkRowWidth = 0;
			this.ModPaths = new string[]
			{
				"Mods",
				GamePaths.DataPathMods
			};
			this.AntiAbuse = EnumProtectionLevel.Off;
			this.NextPlayerGroupUid = 10;
			this.GroupChatHistorySize = 20;
			this.MaxOwnedGroupChannelsPerUser = 10;
			this.WhitelistMode = EnumWhitelistMode.Default;
			this.CompressPackets = true;
			this.TickTime = 33.333332f;
			this.AllowPvP = true;
			this.AllowFireSpread = true;
			this.AllowFallingBlocks = true;
			this.HostedMode = false;
			this.HostedModeAllowMods = false;
			this.Upnp = false;
			this.BlockTickChunkRange = 5;
			this.RandomBlockTicksPerChunk = 16;
			this.BlockTickInterval = 300;
			this.MaxMainThreadBlockTicks = 10000;
			this.ChatRateLimitMs = 1000;
			this.DieBelowDiskSpaceMb = 400;
			this.PassTimeWhenEmpty = false;
			this.LogBlockBreakPlace = false;
			this.DieAboveErrorCount = 100000;
			this.DieAboveMemoryUsageMb = 50000;
			this.LogFileSplitAfterLine = 500000U;
			this.LoginFloodProtection = false;
			this.TemporaryIpBlockList = false;
			this.DisableModSafetyCheck = false;
			this.WorldConfig = new StartServerArgs
			{
				SaveFileLocation = Path.Combine(GamePaths.Saves, GamePaths.DefaultSaveFilenameWithoutExtension + ".vcdbs"),
				WorldName = "A new world",
				AllowCreativeMode = true,
				PlayStyle = "surviveandbuild",
				PlayStyleLangCode = "surviveandbuild-bands",
				WorldType = "standard"
			};
		}

		public object Get(string propertyName)
		{
			foreach (PropertyInfo prop in typeof(ServerConfig).GetProperties())
			{
				if (prop.Name == propertyName)
				{
					return prop.GetValue(this, null);
				}
			}
			throw new ArgumentException("No such property exists");
		}

		public void Set(string propertyName, object value)
		{
			foreach (PropertyInfo prop in typeof(ServerConfig).GetProperties())
			{
				if (prop.Name == propertyName)
				{
					prop.SetValue(this, value);
					return;
				}
			}
			throw new ArgumentException("No such property exists");
		}

		[OnDeserialized]
		internal void OnDeserializedMethod(StreamingContext context)
		{
			if (this.Roles != null && this.Roles.Count > 0)
			{
				this.RolesByCode.Clear();
				foreach (PlayerRole group in this.Roles)
				{
					this.RolesByCode[group.Code] = group;
					if (group.AutoGrant)
					{
						if (group.Privileges != null)
						{
							group.Privileges = group.Privileges.Union(Privilege.AllCodes()).ToList<string>();
						}
						else
						{
							group.Privileges = Privilege.AllCodes().ToList<string>();
						}
					}
				}
			}
			if (this.ConfigVersion == "1.0" && this.RolesByCode.Count == 0)
			{
				this.InitializeRoles();
			}
			if (GameVersion.IsLowerVersionThan(this.ConfigVersion, "1.1.0"))
			{
				this.WelcomeMessage = Lang.GetUnformatted("survive-and-star-trek");
			}
			if (GameVersion.IsLowerVersionThan(this.ConfigVersion, "1.3"))
			{
				foreach (KeyValuePair<string, PlayerRole> val in this.RolesByCode)
				{
					if (!val.Value.AutoGrant)
					{
						val.Value.Privileges.Add(Privilege.attackcreatures);
						val.Value.Privileges.Add(Privilege.attackplayers);
					}
				}
			}
			if (GameVersion.IsLowerVersionThan(this.ConfigVersion, "1.4"))
			{
				this.CorruptionProtection = true;
			}
			if (GameVersion.IsLowerVersionThan(this.ConfigVersion, "1.5"))
			{
				this.RolesByCode["limitedsuplayer"].GrantPrivilege(new string[] { Privilege.selfkill });
				this.RolesByCode["limitedcrplayer"].GrantPrivilege(new string[] { Privilege.selfkill });
				this.RolesByCode["suplayer"].GrantPrivilege(new string[] { Privilege.selfkill });
				this.RolesByCode["crplayer"].GrantPrivilege(new string[] { Privilege.selfkill });
				this.RolesByCode["sumod"].GrantPrivilege(new string[] { Privilege.selfkill });
				this.RolesByCode["crmod"].GrantPrivilege(new string[] { Privilege.selfkill });
			}
			if (GameVersion.IsLowerVersionThan(this.ConfigVersion, "1.6"))
			{
				this.WhitelistMode = (this.OnlyWhitelisted ? EnumWhitelistMode.On : EnumWhitelistMode.Default);
			}
			if (GameVersion.IsLowerVersionThan(this.ConfigVersion, "1.7"))
			{
				ServerSystemLoadAndSaveGame.SetDefaultSpawnOnce = this.DefaultSpawn;
			}
			if (GameVersion.IsLowerVersionThan(this.ConfigVersion, "1.8") && RuntimeEnv.OS == OS.Mac)
			{
				string oldPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.DoNotVerify), ".config", "VintagestoryData");
				for (int i = 0; i < this.ModPaths.Length; i++)
				{
					if (this.ModPaths[i].Contains(oldPath))
					{
						this.ModPaths[i] = this.ModPaths[i].Replace(oldPath, GamePaths.Config);
					}
				}
				StartServerArgs worldConfig = this.WorldConfig;
				if (((worldConfig != null) ? worldConfig.SaveFileLocation : null) != null)
				{
					this.WorldConfig.SaveFileLocation = this.WorldConfig.SaveFileLocation.Replace(oldPath, GamePaths.Config);
				}
			}
			if (!this.RolesByCode.ContainsKey(this.DefaultRoleCode))
			{
				ServerMain.Logger.Fatal("You have configured a default group code " + this.DefaultRoleCode + " but no such group exists! Killing server");
				Environment.Exit(0);
				return;
			}
			if (this.ServerIdentifier == null)
			{
				this.ServerIdentifier = Guid.NewGuid().ToString();
			}
			this.DefaultRole = this.RolesByCode[this.DefaultRoleCode];
			this.LoadedConfigVersion = this.ConfigVersion;
			this.ConfigVersion = "1.8";
			if (this.LoadedConfigVersion != this.ConfigVersion)
			{
				this.Save();
			}
		}

		public void ApplyStartServerArgs(StartServerArgs serverargs)
		{
			if (serverargs == null)
			{
				return;
			}
			this.WorldConfig = serverargs.Clone();
			if (serverargs.Language != null)
			{
				this.ServerLanguage = serverargs.Language;
			}
			if (this.WorldConfig.Seed != null && this.WorldConfig.Seed.Length > 0)
			{
				int seed;
				if (int.TryParse(this.WorldConfig.Seed, out seed))
				{
					this.Seed = new int?(seed);
				}
				else
				{
					this.Seed = new int?(GameMath.DotNetStringHash(this.WorldConfig.Seed));
				}
				ServerMain.Logger.Notification("Using world seed: {0}", new object[] { this.Seed });
			}
			if (serverargs.MapSizeY != null)
			{
				this.MapSizeY = serverargs.MapSizeY.Value;
			}
			this.RepairMode = serverargs.RepairMode;
			if (this.RepairMode)
			{
				this.AnalyzeMode = true;
			}
		}

		public void InitializeRoles()
		{
			this.Roles = new List<PlayerRole>();
			this.Roles.Add(new PlayerRole
			{
				Code = "suvisitor",
				Name = "Survival Visitor",
				Description = "Can only visit this world and chat but not use/place/break anything",
				PrivilegeLevel = -1,
				Color = Color.Green,
				DefaultGameMode = EnumGameMode.Survival,
				Privileges = new List<string>(new string[] { Privilege.chat })
			});
			this.Roles.Add(new PlayerRole
			{
				Code = "crvisitor",
				Name = "Creative Visitor",
				Description = "Can only visit this world, chat and fly but not use/place/break anything",
				PrivilegeLevel = -1,
				Color = Color.DarkGray,
				DefaultGameMode = EnumGameMode.Creative,
				Privileges = new List<string>(new string[] { Privilege.chat })
			});
			this.Roles.Add(new PlayerRole
			{
				Code = "limitedsuplayer",
				Name = "Limited Survival Player",
				Description = "Can use/place/break blocks only in permitted areas (priv level -1), create/manage player groups and chat",
				PrivilegeLevel = -1,
				Color = Color.White,
				DefaultGameMode = EnumGameMode.Survival,
				Privileges = new List<string>(new string[]
				{
					Privilege.controlplayergroups,
					Privilege.manageplayergroups,
					Privilege.chat,
					Privilege.buildblocks,
					Privilege.useblock,
					Privilege.attackcreatures,
					Privilege.attackplayers,
					Privilege.selfkill
				})
			});
			this.Roles.Add(new PlayerRole
			{
				Code = "limitedcrplayer",
				Name = "Limited Creative Player",
				Description = "Can use/place/break blocks in only in permitted areas (priv level -1), create/manage player groups, chat, fly and set his own game mode (= allows fly and change of move speed)",
				PrivilegeLevel = -1,
				Color = Color.LightGreen,
				DefaultGameMode = EnumGameMode.Creative,
				Privileges = new List<string>(new string[]
				{
					Privilege.controlplayergroups,
					Privilege.manageplayergroups,
					Privilege.chat,
					Privilege.buildblocks,
					Privilege.useblock,
					Privilege.gamemode,
					Privilege.freemove,
					Privilege.attackcreatures,
					Privilege.attackplayers,
					Privilege.selfkill
				})
			});
			this.Roles.Add(new PlayerRole
			{
				Code = "suplayer",
				Name = "Survival Player",
				Description = "Can use/place/break blocks in unprotected areas (priv level 0), create/manage player groups and chat. Can claim an area of up to 8 chunks.",
				PrivilegeLevel = 0,
				LandClaimAllowance = 262144,
				LandClaimMaxAreas = 3,
				Color = Color.White,
				DefaultGameMode = EnumGameMode.Survival,
				Privileges = new List<string>(new string[]
				{
					Privilege.controlplayergroups,
					Privilege.manageplayergroups,
					Privilege.chat,
					Privilege.claimland,
					Privilege.buildblocks,
					Privilege.useblock,
					Privilege.attackcreatures,
					Privilege.attackplayers,
					Privilege.selfkill
				})
			});
			this.Roles.Add(new PlayerRole
			{
				Code = "crplayer",
				Name = "Creative Player",
				Description = "Can use/place/break blocks in all areas (priv level 100), create/manage player groups, chat, fly and set his own game mode (= allows fly and change of move speed). Can claim an area of up to 40 chunks.",
				PrivilegeLevel = 100,
				LandClaimAllowance = 1310720,
				LandClaimMaxAreas = 6,
				Color = Color.LightGreen,
				DefaultGameMode = EnumGameMode.Creative,
				Privileges = new List<string>(new string[]
				{
					Privilege.controlplayergroups,
					Privilege.manageplayergroups,
					Privilege.chat,
					Privilege.claimland,
					Privilege.buildblocks,
					Privilege.useblock,
					Privilege.gamemode,
					Privilege.freemove,
					Privilege.attackcreatures,
					Privilege.attackplayers,
					Privilege.selfkill
				})
			});
			this.Roles.Add(new PlayerRole
			{
				Code = "sumod",
				Name = "Survival Moderator",
				Description = "Can use/place/break blocks everywhere (priv level 200), create/manage player groups, chat, kick/ban players and do serverwide announcements. Can claim an area of up to 4 chunks.",
				PrivilegeLevel = 200,
				LandClaimAllowance = 1310720,
				LandClaimMaxAreas = 60,
				Color = Color.Cyan,
				DefaultGameMode = EnumGameMode.Survival,
				Privileges = new List<string>(new string[]
				{
					Privilege.controlplayergroups,
					Privilege.manageplayergroups,
					Privilege.chat,
					Privilege.claimland,
					Privilege.buildblocks,
					Privilege.useblock,
					Privilege.buildblockseverywhere,
					Privilege.useblockseverywhere,
					Privilege.kick,
					Privilege.ban,
					Privilege.announce,
					Privilege.readlists,
					Privilege.attackcreatures,
					Privilege.attackplayers,
					Privilege.selfkill
				})
			});
			this.Roles.Add(new PlayerRole
			{
				Code = "crmod",
				Name = "Creative Moderator",
				Description = "Can use/place/break blocks everywhere (priv level 500), create/manage player groups, chat, kick/ban players, fly and set his own or other players game modes (= allows fly and change of move speed). Can claim an area of up to 40 chunks.",
				LandClaimAllowance = 1310720,
				LandClaimMaxAreas = 60,
				PrivilegeLevel = 500,
				Color = Color.Cyan,
				DefaultGameMode = EnumGameMode.Creative,
				Privileges = new List<string>(new string[]
				{
					Privilege.controlplayergroups,
					Privilege.manageplayergroups,
					Privilege.chat,
					Privilege.claimland,
					Privilege.buildblocks,
					Privilege.useblock,
					Privilege.buildblockseverywhere,
					Privilege.useblockseverywhere,
					Privilege.kick,
					Privilege.ban,
					Privilege.gamemode,
					Privilege.freemove,
					Privilege.commandplayer,
					Privilege.announce,
					Privilege.readlists,
					Privilege.attackcreatures,
					Privilege.attackplayers,
					Privilege.selfkill
				})
			});
			this.Roles.Add(new PlayerRole
			{
				Code = "admin",
				Name = "Admin",
				Description = "Has all privileges, including giving other players admin status.",
				LandClaimAllowance = int.MaxValue,
				LandClaimMaxAreas = 99999,
				PrivilegeLevel = 99999,
				Color = Color.LightBlue,
				DefaultGameMode = EnumGameMode.Survival,
				AutoGrant = true
			});
			this.Roles.Sort();
			foreach (PlayerRole group in this.Roles)
			{
				if (group.AutoGrant)
				{
					if (group.Privileges != null)
					{
						group.Privileges = group.Privileges.Union(Privilege.AllCodes()).ToList<string>();
					}
					else
					{
						group.Privileges = Privilege.AllCodes().ToList<string>();
					}
				}
			}
			foreach (PlayerRole group2 in this.Roles)
			{
				this.RolesByCode[group2.Code] = group2;
			}
			this.DefaultRole = this.RolesByCode[this.DefaultRoleCode];
		}

		public void Save()
		{
			string json = JsonConvert.SerializeObject(this, Formatting.Indented);
			File.WriteAllText(Path.Combine(GamePaths.Config, "serverconfig.json"), json);
		}

		public string LoadedConfigVersion;

		private bool upnp;

		private bool advertiseServer;

		public bool RuntimeUpnp;

		[JsonIgnore]
		private int maxClients;

		private bool temporaryIpBlockList;

		public int? Seed;

		internal PlayerRole DefaultRole;

		public Dictionary<string, PlayerRole> RolesByCode = new Dictionary<string, PlayerRole>();

		internal HashSet<string> RuntimePrivileveCodes = new HashSet<string>();
	}
}
