using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.Server
{
	internal class CmdServerConfig
	{
		private ServerConfig Config
		{
			get
			{
				return this.server.Config;
			}
		}

		private bool ConfigNeedsSaving
		{
			get
			{
				return this.server.ConfigNeedsSaving;
			}
			set
			{
				this.server.ConfigNeedsSaving = value;
			}
		}

		public CmdServerConfig(ServerMain server)
		{
			CmdServerConfig <>4__this = this;
			this.server = server;
			IChatCommandApi chatCommands = server.api.ChatCommands;
			CommandArgumentParsers parsers = server.api.ChatCommands.Parsers;
			chatCommands.Create("serverconfig").WithAlias(new string[] { "sc" }).WithDesc("Read or Set server configuration")
				.RequiresPrivilege(Privilege.controlserver)
				.BeginSub("nopassword")
				.WithDesc("Remove password protection, if set")
				.HandleWith(delegate(TextCommandCallingArgs args)
				{
					if (server.Config.Password == null || server.Config.Password == "")
					{
						return TextCommandResult.Error("There is already no password protection in place", "");
					}
					server.Config.Password = "";
					server.ConfigNeedsSaving = true;
					return TextCommandResult.Success("Password protection now removed", null);
				})
				.EndSub()
				.BeginSub("simrange")
				.WithDesc("Get or temporarily set entity simulation range. Value is not saved. Default is 128")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalFloat("range", 0f) })
				.HandleWith((TextCommandCallingArgs args) => <>4__this.getOrSet("simrange", args))
				.EndSub()
				.BeginSub("spawncapplayerscaling")
				.WithAlias(new string[] { "scps" })
				.WithDesc("Get or set spawn cap player scaling. The lower the value, the less additional mobs are spawned for each additional online player")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalFloat("Scaling value", 0f) })
				.HandleWith((TextCommandCallingArgs args) => <>4__this.getOrSet("SpawnCapPlayerScaling", args))
				.EndSub()
				.BeginSub("setspawnhere")
				.WithDesc("Set the default spawn point to the callers location")
				.HandleWith(new OnCommandDelegate(this.handleSetSpawnhere))
				.EndSub()
				.BeginSub("setspawn")
				.WithDesc("Get or Set the default spawn point to given xz or xyz coordinates")
				.WithArgs(new ICommandArgumentParser[]
				{
					parsers.OptionalInt("x position", 0),
					parsers.OptionalInt("z position (optional)", 0),
					parsers.OptionalInt("z position", 0)
				})
				.HandleWith(new OnCommandDelegate(this.handleSetSpawn))
				.EndSub()
				.BeginSub("welcome")
				.WithDesc("Get or set welcome message")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalAll("Welcome message") })
				.HandleWith((TextCommandCallingArgs args) => <>4__this.getOrSet("WelcomeMessage", args))
				.EndSub()
				.BeginSub("name")
				.WithDesc("Get or set server name")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalAll("Server name") })
				.HandleWith((TextCommandCallingArgs args) => <>4__this.getOrSet("ServerName", args))
				.EndSub()
				.BeginSub("description")
				.WithDesc("Get or set server description")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalAll("Server description") })
				.HandleWith((TextCommandCallingArgs args) => <>4__this.getOrSet("ServerDescription", args))
				.EndSub()
				.BeginSub("url")
				.WithDesc("Get or set server url")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalAll("Server url") })
				.HandleWith((TextCommandCallingArgs args) => <>4__this.getOrSet("ServerUrl", args))
				.EndSub()
				.BeginSub("maxchunkradius")
				.WithDesc("Get or set the maximum view distance in chunks the server will load for players")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalInt("Radius in chunks", 0) })
				.HandleWith((TextCommandCallingArgs args) => <>4__this.getOrSet("MaxChunkRadius", args))
				.EndSub()
				.BeginSub("maxclients")
				.WithDesc("Get or set the maximum amount players that can join the server")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalInt("Max amount of clients", 0) })
				.HandleWith((TextCommandCallingArgs args) => <>4__this.getOrSet("MaxClients", args))
				.EndSub()
				.BeginSub("maxclientsinqueue")
				.WithDesc("Get or set the maximum amount players that can wait in the server join queue")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalInt("Max amount of clients in queue", 0) })
				.HandleWith((TextCommandCallingArgs args) => <>4__this.getOrSet("MaxClientsInQueue", args))
				.EndSub()
				.BeginSub("passtimewhenempty")
				.WithDesc("Get or toggle the passing of time when the server empty")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalBool("Empty server time passing mode", "on") })
				.HandleWith((TextCommandCallingArgs args) => <>4__this.getOrSet("PassTimeWhenEmpty", args))
				.EndSub()
				.BeginSub("upnp")
				.WithDesc("Enable/Disable Upnp discovery")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalBool("Upnp Mode", "on") })
				.HandleWith((TextCommandCallingArgs args) => <>4__this.getOrSet("Upnp", args))
				.EndSub()
				.BeginSub("advertise")
				.WithDesc("Whether to list your server on the public server listing")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalBool("Advertise mode", "on") })
				.HandleWith((TextCommandCallingArgs args) => <>4__this.getOrSet("AdvertiseServer", args))
				.EndSub()
				.BeginSub("allowpvp")
				.WithDesc("Whether to allow Player versus Player combat")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalBool("PvP Mode", "on") })
				.HandleWith((TextCommandCallingArgs args) => <>4__this.getOrSet("AllowPvP", args))
				.EndSub()
				.BeginSub("allowfirespread")
				.WithDesc("Whether to allow the spreading of fire")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalAll("all") })
				.HandleWith((TextCommandCallingArgs _) => TextCommandResult.Success("Please use /worldconfig allowFireSpread true/false to change the value", null))
				.EndSub()
				.BeginSub("allowfallingblocks")
				.WithAlias(new string[] { "fallingblocks" })
				.WithDesc("Whether to allow falling block physics")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalAll("all") })
				.HandleWith((TextCommandCallingArgs _) => TextCommandResult.Success("Please use /worldconfig allowFallingBlocks true/false to change the value", null))
				.EndSub()
				.BeginSub("whitelistmode")
				.WithDesc("Whether to only allow whitelisted players to join your server")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalWordRange("Whitelist mode", new string[] { "on", "off", "default" }) })
				.HandleWith((TextCommandCallingArgs args) => <>4__this.getOrSet("WhitelistMode", args))
				.EndSub()
				.BeginSub("entityspawning")
				.WithDesc("Whether to spawn new creatures and monsters over time")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalBool("Entity spawning mode", "on") })
				.HandleWith((TextCommandCallingArgs args) => <>4__this.getOrSet("EntitySpawning", args))
				.EndSub()
				.BeginSub("entitydebugmode")
				.WithDesc("Whether to enable entity debug mode")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalBool("Entity debug mode", "on") })
				.HandleWith((TextCommandCallingArgs args) => <>4__this.getOrSet("EntityDebugMode", args))
				.EndSub()
				.BeginSub("password")
				.WithDesc("Sets a password when connecting to the server. Cannot use spaces. Use /serverconfig nopassword to clear.")
				.WithArgs(new ICommandArgumentParser[] { parsers.Word("password") })
				.HandleWith((TextCommandCallingArgs args) => <>4__this.getOrSet("Password", args))
				.EndSub()
				.BeginSub("tickrate")
				.WithDesc("How often the server should tick per second.")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalFloat("tick interval in ms", 0f) })
				.HandleWith(delegate(TextCommandCallingArgs args)
				{
					if (server.Config.HostedMode && args.Caller.Type != EnumCallerType.Console)
					{
						return TextCommandResult.Error(Lang.Get("Can't access this feature, server is in hosted mode", Array.Empty<object>()), "");
					}
					if (args.Parsers[0].IsMissing)
					{
						return TextCommandResult.Success("The current tick rate is at " + (1000f / server.Config.TickTime).ToString() + " tp/s", null);
					}
					float tickspeed = (float)args[0];
					server.Config.TickTime = 1000f / tickspeed;
					server.ConfigNeedsSaving = true;
					return TextCommandResult.Success(Lang.Get("Ok, tick rate now at {0} tp/s", new object[] { tickspeed }), EnumChatType.CommandSuccess);
				})
				.EndSub()
				.BeginSub("autosaveintervall")
				.WithDesc("How often the server save to disk while it is running.")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalIntRange("save interval in seconds", 30, 3600, 0) })
				.HandleWith(delegate(TextCommandCallingArgs args)
				{
					if (server.Config.HostedMode && args.Caller.Type != EnumCallerType.Console)
					{
						return TextCommandResult.Error(Lang.Get("Can't access this feature, server is in hosted mode", Array.Empty<object>()), "");
					}
					if (args.Parsers[0].IsMissing)
					{
						return TextCommandResult.Success("Autosave interval is at " + MagicNum.ServerAutoSave.ToString() + " seconds", null);
					}
					int newseci = GameMath.Max((int)args[0], 30);
					MagicNum.ServerAutoSave = (long)newseci;
					MagicNum.Save();
					return TextCommandResult.Success(Lang.Get("Ok, autosave interval now at {0} s", new object[] { newseci }), null);
				})
				.EndSub()
				.BeginSub("blockTicksPerChunk")
				.WithAlias(new string[] { "btpc" })
				.WithAlias(new string[] { "blockticks" })
				.WithDesc("How often blocks around the player should randomly tick to update their state")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalInt("block tick rate", 0) })
				.HandleWith((TextCommandCallingArgs args) => <>4__this.getOrSet("RandomBlockTicksPerChunk", args))
				.EndSub()
				.BeginSub("welcomemessage")
				.WithAlias(new string[] { "motd" })
				.WithDesc("Set a message to be shown in chat when a player joins.")
				.WithArgs(new ICommandArgumentParser[] { parsers.Word("message") })
				.HandleWith((TextCommandCallingArgs args) => <>4__this.getOrSet("WelcomeMessage", args))
				.EndSub()
				.BeginSub("loginfloodprotection")
				.WithDesc("Enable or disable the ip based login flood protection")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalBool("LoginFloodProtection mode", "on") })
				.HandleWith((TextCommandCallingArgs args) => <>4__this.getOrSet("LoginFloodProtection", args))
				.EndSub()
				.BeginSub("temporaryipblocklist")
				.WithDesc("Enable or disable the ip based block list")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalBool("TemporaryIpBlockList mode", "on") })
				.HandleWith((TextCommandCallingArgs args) => <>4__this.getOrSet("TemporaryIpBlockList", args))
				.EndSub()
				.BeginSub("modwhitelist")
				.WithDesc("Modify the mod whitelist")
				.BeginSub("add")
				.WithDesc("Add a modId to the whitelist")
				.WithArgs(new ICommandArgumentParser[] { parsers.Word("modId") })
				.HandleWith(delegate(TextCommandCallingArgs args)
				{
					string modId = args[0] as string;
					ServerConfig config = server.Config;
					string[] array2;
					if (server.Config.ModIdWhiteList != null)
					{
						string[] modIdWhiteList = server.Config.ModIdWhiteList;
						int num = 0;
						string[] array = new string[1 + modIdWhiteList.Length];
						ReadOnlySpan<string> readOnlySpan = new ReadOnlySpan<string>(modIdWhiteList);
						readOnlySpan.CopyTo(new Span<string>(array).Slice(num, readOnlySpan.Length));
						num += readOnlySpan.Length;
						array[num] = modId;
						array2 = array;
					}
					else
					{
						(array2 = new string[1])[0] = modId;
					}
					config.ModIdWhiteList = array2;
					<>4__this.ConfigNeedsSaving = true;
					return TextCommandResult.Success("Mod " + modId + " added to whitelist", null);
				})
				.EndSub()
				.BeginSub("remove")
				.WithDesc("Remove a modId from the whitelist")
				.WithArgs(new ICommandArgumentParser[] { parsers.Word("modId") })
				.HandleWith(delegate(TextCommandCallingArgs args)
				{
					string modId2 = args[0] as string;
					string[] modIdWhiteList2 = server.Config.ModIdWhiteList;
					if (modIdWhiteList2 == null || !modIdWhiteList2.Contains(modId2))
					{
						return TextCommandResult.Success("Mod " + modId2 + " is not part of the whitelist", null);
					}
					List<string> tempList = new List<string>(server.Config.ModIdWhiteList);
					tempList.Remove(modId2);
					server.Config.ModIdWhiteList = ((tempList.Count == 0) ? null : tempList.ToArray());
					<>4__this.ConfigNeedsSaving = true;
					return TextCommandResult.Success("Mod " + modId2 + " was removed from the whitelist", null);
				})
				.EndSub()
				.BeginSub("clear")
				.WithDesc("Clear the whitelist")
				.HandleWith(delegate(TextCommandCallingArgs _)
				{
					server.Config.ModIdWhiteList = null;
					<>4__this.ConfigNeedsSaving = true;
					return TextCommandResult.Success("Mod whitelist was cleared", null);
				})
				.EndSub()
				.BeginSub("list")
				.WithDesc("Show all whitelisted mods")
				.HandleWith(delegate(TextCommandCallingArgs _)
				{
					string list = ((server.Config.ModIdWhiteList != null) ? string.Join(",", server.Config.ModIdWhiteList) : "List is empty");
					return TextCommandResult.Success("Mod whitelist: " + list, null);
				})
				.EndSub()
				.EndSub()
				.BeginSub("modblacklist")
				.WithDesc("Modify the mod blacklist")
				.BeginSub("add")
				.WithDesc("Add a modId to the blacklist")
				.WithArgs(new ICommandArgumentParser[] { parsers.Word("modId") })
				.HandleWith(delegate(TextCommandCallingArgs args)
				{
					string modId3 = args[0] as string;
					ServerConfig config2 = server.Config;
					string[] array4;
					if (server.Config.ModIdBlackList != null)
					{
						string[] modIdBlackList = server.Config.ModIdBlackList;
						int num2 = 0;
						string[] array3 = new string[1 + modIdBlackList.Length];
						ReadOnlySpan<string> readOnlySpan2 = new ReadOnlySpan<string>(modIdBlackList);
						readOnlySpan2.CopyTo(new Span<string>(array3).Slice(num2, readOnlySpan2.Length));
						num2 += readOnlySpan2.Length;
						array3[num2] = modId3;
						array4 = array3;
					}
					else
					{
						(array4 = new string[1])[0] = modId3;
					}
					config2.ModIdBlackList = array4;
					<>4__this.ConfigNeedsSaving = true;
					return TextCommandResult.Success("Mod " + modId3 + " added to blacklist", null);
				})
				.EndSub()
				.BeginSub("remove")
				.WithDesc("Remove a modId from the blacklist")
				.WithArgs(new ICommandArgumentParser[] { parsers.Word("modId") })
				.HandleWith(delegate(TextCommandCallingArgs args)
				{
					string modId4 = args[0] as string;
					string[] modIdBlackList2 = server.Config.ModIdBlackList;
					if (modIdBlackList2 == null || !modIdBlackList2.Contains(modId4))
					{
						return TextCommandResult.Success("Mod " + modId4 + " is not part of the blacklist", null);
					}
					List<string> tempList2 = new List<string>(server.Config.ModIdBlackList);
					tempList2.Remove(modId4);
					server.Config.ModIdBlackList = ((tempList2.Count == 0) ? null : tempList2.ToArray());
					<>4__this.ConfigNeedsSaving = true;
					return TextCommandResult.Success("Mod " + modId4 + " was removed from the blacklist", null);
				})
				.EndSub()
				.BeginSub("clear")
				.WithDesc("Clear the blacklist")
				.HandleWith(delegate(TextCommandCallingArgs _)
				{
					server.Config.ModIdBlackList = null;
					<>4__this.ConfigNeedsSaving = true;
					return TextCommandResult.Success("Mod blacklist was cleared", null);
				})
				.EndSub()
				.BeginSub("list")
				.WithDesc("Show all blacklist mods")
				.HandleWith(delegate(TextCommandCallingArgs _)
				{
					string list2 = ((server.Config.ModIdBlackList != null) ? string.Join(",", server.Config.ModIdBlackList) : "List is empty");
					return TextCommandResult.Success("Mod blacklist: " + list2, null);
				})
				.EndSub()
				.EndSub();
		}

		private TextCommandResult getOrSet(string name, TextCommandCallingArgs args)
		{
			if (name == "simrange")
			{
				if (args.Parsers[0].IsMissing)
				{
					return TextCommandResult.Success(Lang.Get("Current DefaultSimulationRange is {0}", new object[] { GlobalConstants.DefaultSimulationRange }), null);
				}
				GlobalConstants.DefaultSimulationRange = (int)args[0];
				return TextCommandResult.Success(Lang.Get("DefaultSimulationRange set to {0}", new object[] { GlobalConstants.DefaultSimulationRange }), null);
			}
			else
			{
				if (name == "MaxClients" && this.server.progArgs.MaxClients != null)
				{
					return TextCommandResult.Success("Current Max Clients is overridden by command line supplied max clients with a value of  " + this.server.progArgs.MaxClients, null);
				}
				if (name == "WhitelistMode")
				{
					if (args.Parsers[0].IsMissing)
					{
						return TextCommandResult.Success(Lang.Get("Current WhitelistMode is {0}", new object[] { this.Config.WhitelistMode }), null);
					}
					string text = ((string)args[0]).ToLowerInvariant();
					if (!(text == "off"))
					{
						if (!(text == "on"))
						{
							if (text == "default")
							{
								this.Config.WhitelistMode = EnumWhitelistMode.Default;
							}
						}
						else
						{
							this.Config.WhitelistMode = EnumWhitelistMode.On;
						}
					}
					else
					{
						this.Config.WhitelistMode = EnumWhitelistMode.Off;
					}
					this.ConfigNeedsSaving = true;
					return TextCommandResult.Success(Lang.Get("WhitelistMode set to {0}", new object[] { this.Config.WhitelistMode }), null);
				}
				else
				{
					if (this.server.Config.HostedMode && args.Caller.Type != EnumCallerType.Console && this.unavailableInHostedMode.Contains(name))
					{
						return TextCommandResult.Error(Lang.Get("Can't access this feature, this server is in hosted mode", Array.Empty<object>()), "");
					}
					if (args.Parsers[0].IsMissing)
					{
						object value;
						if (name == "EntitySpawning")
						{
							value = this.server.SaveGameData.EntitySpawning;
						}
						else
						{
							value = this.server.Config.Get(name);
						}
						if (value is bool)
						{
							value = (((bool)value) ? "on" : "off");
						}
						return TextCommandResult.Success(Lang.Get("Current {0} is {1}", new object[]
						{
							args.Parsers[0].ArgumentName,
							value
						}), null);
					}
					object nowvalue = args[0];
					if (nowvalue is bool)
					{
						nowvalue = (((bool)nowvalue) ? "on" : "off");
					}
					if (name == "EntitySpawning")
					{
						this.server.SaveGameData.EntitySpawning = (bool)args[0];
					}
					else
					{
						this.Config.Set(name, args[0]);
					}
					this.ConfigNeedsSaving = true;
					ServerMain.Logger.Audit(Lang.Get("{0} changes server config {1} to {2}.", new object[]
					{
						args.Caller.GetName(),
						name,
						nowvalue
					}));
					return TextCommandResult.Success(Lang.Get("{0} set to {1}", new object[]
					{
						args.Parsers[0].ArgumentName,
						nowvalue
					}), null);
				}
			}
		}

		private TextCommandResult handleSetSpawn(TextCommandCallingArgs args)
		{
			if (args.Parsers[0].IsMissing)
			{
				return TextCommandResult.Success((this.server.SaveGameData.DefaultSpawn == null) ? Lang.Get("Default Spawnpoint is not set.", Array.Empty<object>()) : Lang.Get("Default spawnpoint is at ={0} ={1} ={2}", new object[]
				{
					this.server.SaveGameData.DefaultSpawn.x,
					this.server.SaveGameData.DefaultSpawn.y,
					this.server.SaveGameData.DefaultSpawn.z
				}), null);
			}
			int x = (int)args[0];
			int z;
			int y;
			if (args.Parsers[2].IsMissing)
			{
				z = (int)args[1];
				y = this.server.WorldMap.GetTerrainGenSurfacePosY(x, z);
			}
			else
			{
				y = (int)args[1];
				z = (int)args[2];
			}
			if (!this.server.WorldMap.IsValidPos(x, y, z))
			{
				return TextCommandResult.Error(Lang.Get("Invalid coordinates - beyond world bounds", Array.Empty<object>()), "");
			}
			this.server.SaveGameData.DefaultSpawn = new PlayerSpawnPos
			{
				x = x,
				y = new int?(y),
				z = z
			};
			this.ConfigNeedsSaving = true;
			return TextCommandResult.Success(Lang.Get("Default spawnpoint now set to ={0} ={1} ={2}", new object[] { x, y, z }), null);
		}

		private TextCommandResult handleSetSpawnhere(TextCommandCallingArgs args)
		{
			BlockPos plrPos = args.Caller.Pos.AsBlockPos;
			if (!this.server.WorldMap.IsValidPos(plrPos.X, plrPos.Y, plrPos.Z))
			{
				return TextCommandResult.Error(Lang.Get("Invalid coordinates (probably beyond world bounds)", Array.Empty<object>()), "");
			}
			this.server.SaveGameData.DefaultSpawn = new PlayerSpawnPos
			{
				x = plrPos.X,
				y = new int?(plrPos.Y),
				z = plrPos.Z
			};
			return TextCommandResult.Success(Lang.Get("Default spawnpoint now set to ={0} ={1} ={2}", new object[]
			{
				this.server.SaveGameData.DefaultSpawn.x,
				this.server.SaveGameData.DefaultSpawn.y,
				this.server.SaveGameData.DefaultSpawn.z
			}), null);
		}

		private ServerMain server;

		private HashSet<string> unavailableInHostedMode = new HashSet<string>(new string[] { "MaxChunkRadius", "MaxClients", "Upnp", "EntityDebugMode", "TickTime", "RandomBlockTicksPerChunk" });
	}
}
