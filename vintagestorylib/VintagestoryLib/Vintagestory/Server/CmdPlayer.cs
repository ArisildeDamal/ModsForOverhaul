using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.Server.Network;

namespace Vintagestory.Server
{
	public class CmdPlayer : ServerSystem
	{
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

		private ServerConfig Config
		{
			get
			{
				return this.server.Config;
			}
		}

		public CmdPlayer(ServerMain server)
			: base(server)
		{
			CmdPlayer <>4__this = this;
			IChatCommandApi cmdapi = server.api.ChatCommands;
			CommandArgumentParsers parsers = server.api.ChatCommands.Parsers;
			string[] gameModes = new string[] { "0", "1", "2", "3", "4", "creative", "survival", "spectator", "guest", "abbreviated game mode names are valid as well" };
			cmdapi.GetOrCreate("mystats").WithDescription("shows players stats").RequiresPrivilege(Privilege.chat)
				.RequiresPlayer()
				.HandleWith(new OnCommandDelegate(this.OnCmdMyStats))
				.Validate();
			CmdPlayer.PlayerEachDelegate <>9__26;
			CmdPlayer.PlayerEachDelegate <>9__28;
			cmdapi.GetOrCreate("whitelist").WithDesc("Whitelist control").RequiresPrivilege(Privilege.whitelist)
				.BeginSub("add")
				.WithDesc("Add a player to the whitelist")
				.WithArgs(new ICommandArgumentParser[]
				{
					parsers.PlayerUids("player"),
					parsers.OptionalAll("optional reason")
				})
				.HandleWith(delegate(TextCommandCallingArgs args)
				{
					CmdPlayer.PlayerEachDelegate playerEachDelegate;
					if ((playerEachDelegate = <>9__26) == null)
					{
						playerEachDelegate = (<>9__26 = delegate(PlayerUidName targetPlayer, TextCommandCallingArgs args)
						{
							if (server.PlayerDataManager.WhitelistedPlayers.Any((PlayerEntry item) => item.PlayerUID == targetPlayer.Uid))
							{
								return TextCommandResult.Error("Player is already whitelisted", "");
							}
							string reason = (string)args[1];
							server.PlayerDataManager.GetOrCreateServerPlayerData(targetPlayer.Uid, targetPlayer.Name);
							string issuer = ((args.Caller.Player != null) ? args.Caller.Player.PlayerName : args.Caller.Type.ToString());
							DateTime untildate = DateTime.Now.AddYears(50);
							server.PlayerDataManager.WhitelistPlayer(targetPlayer.Name, targetPlayer.Uid, issuer, reason, new DateTime?(untildate));
							return TextCommandResult.Success(Lang.Get("Player is now whitelisted until {0}", new object[] { untildate }), null);
						});
					}
					return CmdPlayer.Each(args, playerEachDelegate);
				})
				.EndSub()
				.BeginSub("remove")
				.WithDesc("Remove a player from the whitelist")
				.WithArgs(new ICommandArgumentParser[] { parsers.PlayerUids("player") })
				.HandleWith(delegate(TextCommandCallingArgs args)
				{
					CmdPlayer.PlayerEachDelegate playerEachDelegate2;
					if ((playerEachDelegate2 = <>9__28) == null)
					{
						playerEachDelegate2 = (<>9__28 = delegate(PlayerUidName targetPlayer, TextCommandCallingArgs args)
						{
							if (!server.PlayerDataManager.WhitelistedPlayers.Any((PlayerEntry item) => item.PlayerUID == targetPlayer.Uid))
							{
								return TextCommandResult.Error("Player is not whitelisted", "");
							}
							server.PlayerDataManager.GetOrCreateServerPlayerData(targetPlayer.Uid, targetPlayer.Name);
							server.PlayerDataManager.UnWhitelistPlayer(targetPlayer.Name, targetPlayer.Uid);
							return TextCommandResult.Success(Lang.Get("Player is now removed from the whitelist", Array.Empty<object>()), null);
						});
					}
					return CmdPlayer.Each(args, playerEachDelegate2);
				})
				.EndSub()
				.BeginSub("on")
				.WithDesc("Enable whitelist system. Only whitelisted players can join")
				.HandleWith(delegate(TextCommandCallingArgs args)
				{
					if (server.Config.WhitelistMode == EnumWhitelistMode.On)
					{
						return TextCommandResult.Error(Lang.Get("Whitelist was already enabled", Array.Empty<object>()), "");
					}
					server.Config.WhitelistMode = EnumWhitelistMode.On;
					server.ConfigNeedsSaving = true;
					return TextCommandResult.Success(Lang.Get("Whitelist now enabled", Array.Empty<object>()), null);
				})
				.EndSub()
				.BeginSub("off")
				.HandleWith(delegate(TextCommandCallingArgs args)
				{
					if (server.Config.WhitelistMode == EnumWhitelistMode.Off)
					{
						return TextCommandResult.Error(Lang.Get("Whitelist was already disabled", Array.Empty<object>()), "");
					}
					server.Config.WhitelistMode = EnumWhitelistMode.Off;
					server.ConfigNeedsSaving = true;
					return TextCommandResult.Success(Lang.Get("Whitelist now disabled", Array.Empty<object>()), null);
				})
				.WithDesc("Disable whitelist system. All players can join")
				.EndSub()
				.Validate();
			cmdapi.GetOrCreate("player").WithDesc("Player control").WithArgs(new ICommandArgumentParser[] { parsers.PlayerUids("player") })
				.RequiresPrivilege(Privilege.chat)
				.BeginSub("movespeed")
				.RequiresPrivilege(Privilege.grantrevoke)
				.WithDesc("Set a player's move speed")
				.WithArgs(new ICommandArgumentParser[] { parsers.Float("movespeed") })
				.HandleWith((TextCommandCallingArgs args) => CmdPlayer.Each(args, new CmdPlayer.PlayerEachDelegate(<>4__this.setMovespeed)))
				.EndSub()
				.BeginSub("whitelist")
				.RequiresPrivilege(Privilege.whitelist)
				.WithDesc("Add/remove player to/from the whitelist")
				.WithArgs(new ICommandArgumentParser[]
				{
					parsers.OptionalBool("add/remove", "add"),
					parsers.OptionalAll("optional reason")
				})
				.HandleWith((TextCommandCallingArgs args) => CmdPlayer.Each(args, new CmdPlayer.PlayerEachDelegate(<>4__this.addRemoveWhitelist)))
				.EndSub()
				.BeginSub("privilege")
				.RequiresPrivilege(Privilege.grantrevoke)
				.WithDesc("Player privilege control")
				.HandleWith((TextCommandCallingArgs args) => CmdPlayer.Each(args, new CmdPlayer.PlayerEachDelegate(<>4__this.listPrivilege)))
				.BeginSub("grant")
				.WithDesc("Grant a privilege to a player")
				.WithArgs(new ICommandArgumentParser[] { parsers.Word("privilege_name", Privilege.AllCodes().Append("or custom defined privileges")) })
				.HandleWith((TextCommandCallingArgs args) => CmdPlayer.Each(args, new CmdPlayer.PlayerEachDelegate(<>4__this.grantPrivilege)))
				.EndSub()
				.BeginSub("revoke")
				.WithArgs(new ICommandArgumentParser[] { parsers.Word("privilege_name", Privilege.AllCodes().Append("or custom defined privileges")) })
				.WithDesc("Revoke a privilege from a player")
				.HandleWith((TextCommandCallingArgs args) => CmdPlayer.Each(args, new CmdPlayer.PlayerEachDelegate(<>4__this.revokePrivilege)))
				.EndSub()
				.BeginSub("deny")
				.WithArgs(new ICommandArgumentParser[] { parsers.Privilege("privilege_name") })
				.WithDesc("Deny a privilege to a player that was ordinarily granted from a role")
				.HandleWith((TextCommandCallingArgs args) => CmdPlayer.Each(args, new CmdPlayer.PlayerEachDelegate(<>4__this.denyPrivilege)))
				.EndSub()
				.BeginSub("removedeny")
				.WithArgs(new ICommandArgumentParser[] { parsers.Privilege("privilege_name") })
				.WithDesc("Remove a previous privilege denial from a player")
				.HandleWith((TextCommandCallingArgs args) => CmdPlayer.Each(args, new CmdPlayer.PlayerEachDelegate(<>4__this.removeDenyPrivilege)))
				.EndSub()
				.EndSub()
				.BeginSub("role")
				.RequiresPrivilege(Privilege.grantrevoke)
				.WithDesc("Set or get a player role")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalPlayerRole("role") })
				.HandleWith((TextCommandCallingArgs args) => CmdPlayer.Each(args, new CmdPlayer.PlayerEachDelegate(<>4__this.GetSetRole)))
				.EndSub()
				.BeginSub("stats")
				.RequiresPrivilege(Privilege.grantrevoke)
				.WithDesc("Display player parameters")
				.HandleWith((TextCommandCallingArgs args) => CmdPlayer.Each(args, new CmdPlayer.PlayerEachDelegate(<>4__this.getStats)))
				.EndSub()
				.BeginSub("entity")
				.RequiresPrivilege(Privilege.grantrevoke)
				.WithDesc("Get/Set an attribute value on the player entity")
				.WithArgs(new ICommandArgumentParser[]
				{
					parsers.OptionalWord("attribute_name"),
					parsers.OptionalFloat("attribute value", 0f)
				})
				.HandleWith((TextCommandCallingArgs args) => CmdPlayer.Each(args, new CmdPlayer.PlayerEachDelegate(<>4__this.handleEntity)))
				.EndSub()
				.BeginSub("wipedata")
				.RequiresPrivilege(Privilege.controlserver)
				.WithDesc("Wipe the player data, such as the entire inventory, skin/class, etc.")
				.HandleWith((TextCommandCallingArgs args) => CmdPlayer.Each(args, new CmdPlayer.PlayerEachDelegate(<>4__this.WipePlayerData)))
				.EndSub()
				.BeginSub("clearinv")
				.RequiresPrivilege(Privilege.controlserver)
				.WithDesc("Clear the player's entire inventory")
				.HandleWith((TextCommandCallingArgs args) => CmdPlayer.Each(args, new CmdPlayer.PlayerEachDelegate(<>4__this.WipePlayerInventory)))
				.EndSub()
				.BeginSub("gamemode")
				.WithAlias(new string[] { "gm" })
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalWordRange("mode", gameModes) })
				.RequiresPrivilege(Privilege.gamemode)
				.WithDesc("Set (or discover) the player(s) game mode")
				.HandleWith((TextCommandCallingArgs args) => CmdPlayer.Each(args, new CmdPlayer.PlayerEachDelegate(<>4__this.getSetGameMode)))
				.EndSub()
				.BeginSub("allowcharselonce")
				.WithAlias(new string[] { "acso" })
				.RequiresPrivilege(Privilege.grantrevoke)
				.WithDesc("Allow changing character class and skin one more time")
				.WithAdditionalInformation("Allows the player to run the <code>.charsel</code> command client-side")
				.HandleWith((TextCommandCallingArgs args) => CmdPlayer.Each(args, new CmdPlayer.PlayerEachDelegate(<>4__this.handleCharSel)))
				.EndSub()
				.BeginSub("landclaimallowance")
				.WithAlias(new string[] { "lca" })
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalInt("amount", 0) })
				.WithDesc("Get/Set land claim allowance")
				.WithAdditionalInformation("Specifies the amount of land a player can claim, in m³")
				.RequiresPrivilege(Privilege.grantrevoke)
				.HandleWith((TextCommandCallingArgs args) => CmdPlayer.Each(args, new CmdPlayer.PlayerEachDelegate(<>4__this.handleLandClaimAllowance)))
				.EndSub()
				.BeginSub("landclaimmaxareas")
				.WithAlias(new string[] { "lcma" })
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalInt("number", 0) })
				.WithDesc("Get/Set land claim max areas")
				.WithAdditionalInformation("Specifies the maximum number of separate land areas a player can claim")
				.RequiresPrivilege(Privilege.grantrevoke)
				.HandleWith((TextCommandCallingArgs args) => CmdPlayer.Each(args, new CmdPlayer.PlayerEachDelegate(<>4__this.handleLandClaimMaxAreas)))
				.EndSub()
				.Validate();
			cmdapi.Create("op").WithDesc("Give a player admin status. Shorthand for /player &lt;playername&gt; role admin").WithArgs(new ICommandArgumentParser[] { parsers.PlayerUids("playername") })
				.RequiresPrivilege(Privilege.grantrevoke)
				.HandleWith((TextCommandCallingArgs args) => CmdPlayer.Each(args, new CmdPlayer.PlayerEachDelegate(<>4__this.opPlayer)))
				.Validate();
			cmdapi.Create("self").WithDesc("Information about your player").RequiresPrivilege(Privilege.chat)
				.BeginSub("stats")
				.WithDesc("Full stats")
				.HandleWith(delegate(TextCommandCallingArgs args)
				{
					CmdPlayer <>4__this4 = <>4__this;
					IPlayer player = args.Caller.Player;
					string text = ((player != null) ? player.PlayerUID : null);
					IPlayer player2 = args.Caller.Player;
					return <>4__this4.getStats(new PlayerUidName(text, (player2 != null) ? player2.PlayerName : null), args);
				})
				.EndSub()
				.BeginSub("privileges")
				.WithDesc("Your current privileges")
				.HandleWith(delegate(TextCommandCallingArgs args)
				{
					CmdPlayer <>4__this2 = <>4__this;
					IPlayer player3 = args.Caller.Player;
					string text2 = ((player3 != null) ? player3.PlayerUID : null);
					IPlayer player4 = args.Caller.Player;
					return <>4__this2.listPrivilege(new PlayerUidName(text2, (player4 != null) ? player4.PlayerName : null), args);
				})
				.EndSub()
				.BeginSub("role")
				.WithDesc("Your current role")
				.HandleWith(delegate(TextCommandCallingArgs args)
				{
					CmdPlayer <>4__this3 = <>4__this;
					IPlayer player5 = args.Caller.Player;
					string text3 = ((player5 != null) ? player5.PlayerUID : null);
					IPlayer player6 = args.Caller.Player;
					return <>4__this3.GetSetRole(new PlayerUidName(text3, (player6 != null) ? player6.PlayerName : null), args);
				})
				.EndSub()
				.BeginSub("gamemode")
				.WithDesc("Your current game mode")
				.HandleWith(new OnCommandDelegate(this.handleGameMode))
				.EndSub()
				.BeginSub("clearinv")
				.RequiresPrivilege(Privilege.gamemode)
				.WithRootAlias("clearinv")
				.WithDesc("Empties your inventory")
				.HandleWith(delegate(TextCommandCallingArgs args)
				{
					IPlayer player7 = args.Caller.Player;
					if (player7 != null)
					{
						player7.InventoryManager.DiscardAll();
					}
					return TextCommandResult.Success("", null);
				})
				.EndSub()
				.BeginSub("kill")
				.RequiresPrivilege(Privilege.selfkill)
				.WithRootAlias("kill")
				.WithDesc("Kill yourself")
				.HandleWith(delegate(TextCommandCallingArgs args)
				{
					args.Caller.Entity.Die(EnumDespawnReason.Death, new DamageSource
					{
						Source = EnumDamageSource.Suicide
					});
					return TextCommandResult.Success("", null);
				})
				.EndSub()
				.Validate();
			cmdapi.Create("gamemode").WithAlias(new string[] { "gm" }).WithDesc("Get/Set one players game mode. Omit playername arg to get/set your own game mode")
				.RequiresPrivilege(Privilege.chat)
				.WithArgs(new ICommandArgumentParser[]
				{
					parsers.Unparsed("playername", Array.Empty<string>()),
					parsers.Unparsed("mode", gameModes)
				})
				.HandleWith(new OnCommandDelegate(this.handleGameMode))
				.Validate();
			cmdapi.Create("role").RequiresPrivilege(Privilege.controlserver).WithDescription("Modify/See player role related data")
				.WithArgs(new ICommandArgumentParser[] { parsers.PlayerRole("rolename") })
				.BeginSub("landclaimallowance")
				.WithAlias(new string[] { "lca" })
				.WithDescription("Get/Set land claim allowance m³")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalInt("landClaimAllowance", -1) })
				.HandleWith(new OnCommandDelegate(this.OnLandclaimallowanceCmd))
				.EndSub()
				.BeginSub("landclaimminsize")
				.WithAlias(new string[] { "lcms" })
				.WithDescription("Get/Set land claim minimum size")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalVec3i("minSize") })
				.HandleWith(new OnCommandDelegate(this.OnLandclaimminsizeCmd))
				.EndSub()
				.BeginSub("landclaimmaxareas")
				.WithAlias(new string[] { "lcma" })
				.WithDescription("Get/Set land claim maximum areas")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalInt("area", -1) })
				.HandleWith(new OnCommandDelegate(this.OnLandclaimmaxareasCmd))
				.EndSub()
				.BeginSub("privilege")
				.WithDescription("Show privileges for role")
				.HandleWith(new OnCommandDelegate(this.OnPrivilegeCmd))
				.BeginSub("grant")
				.WithDescription("Grant a privilege")
				.WithArgs(new ICommandArgumentParser[] { parsers.Word("privilege_name", Privilege.AllCodes().Append("or custom defined privileges")) })
				.HandleWith(new OnCommandDelegate(this.OnGrantCmd))
				.EndSub()
				.BeginSub("revoke")
				.WithDescription("Revoke  a privilege")
				.WithArgs(new ICommandArgumentParser[] { parsers.Privilege("privilege_name") })
				.HandleWith(new OnCommandDelegate(this.OnRevokeCmd))
				.EndSub()
				.EndSub()
				.BeginSub("spawnpoint")
				.WithDescription("Get/Set/Unset the default spawnpoint")
				.HandleWith(new OnCommandDelegate(this.OnSpawnpointCmd))
				.BeginSub("set")
				.WithDescription("Set the default spawnpoint")
				.WithArgs(new ICommandArgumentParser[] { parsers.WorldPosition("pos") })
				.HandleWith(new OnCommandDelegate(this.OnSpawnpointSetCmd))
				.EndSub()
				.BeginSub("unset")
				.WithDesc("Unset the default spawnpoint")
				.HandleWith(new OnCommandDelegate(this.OnSpawnpointUnsetCmd))
				.EndSub()
				.EndSub()
				.Validate();
		}

		private TextCommandResult OnSpawnpointUnsetCmd(TextCommandCallingArgs args)
		{
			PlayerRole role = (PlayerRole)args.Parsers[0].GetValue();
			role.DefaultSpawn = null;
			this.ConfigNeedsSaving = true;
			return TextCommandResult.Success(Lang.Get("Spawnpoint for role {0} now unset.", new object[] { role.Name, role.DefaultSpawn }), null);
		}

		private TextCommandResult OnSpawnpointSetCmd(TextCommandCallingArgs args)
		{
			PlayerRole role = (PlayerRole)args.Parsers[0].GetValue();
			Vec3d pos = (Vec3d)args.Parsers[1].GetValue();
			role.DefaultSpawn = new PlayerSpawnPos
			{
				x = (int)pos.X,
				y = new int?((int)pos.Y),
				z = (int)pos.Z
			};
			this.ConfigNeedsSaving = true;
			return TextCommandResult.Success(Lang.Get("Spawnpoint for role {0} now set to {1}", new object[] { role.Name, role.DefaultSpawn }), null);
		}

		private TextCommandResult OnSpawnpointCmd(TextCommandCallingArgs args)
		{
			PlayerRole role = (PlayerRole)args.Parsers[0].GetValue();
			if (role.DefaultSpawn == null)
			{
				return TextCommandResult.Success(Lang.Get("Spawnpoint for role {0} is not set.", new object[] { role.Name }), null);
			}
			return TextCommandResult.Success(Lang.Get("Spawnpoint for role {0} is at {1}", new object[] { role.Name, role.DefaultSpawn }), null);
		}

		private TextCommandResult OnRevokeCmd(TextCommandCallingArgs args)
		{
			IPlayerRole role = (IPlayerRole)args.Parsers[0].GetValue();
			string privname = (string)args.Parsers[1].GetValue();
			if (!role.Privileges.Contains(privname))
			{
				return TextCommandResult.Error(Lang.Get("Role does not have this privilege", Array.Empty<object>()), "");
			}
			role.RevokePrivilege(privname);
			this.server.ConfigNeedsSaving = true;
			return TextCommandResult.Success(Lang.Get("Ok, privilege '{0}' now revoked", new object[] { privname }), null);
		}

		private TextCommandResult OnGrantCmd(TextCommandCallingArgs args)
		{
			IPlayerRole role = (IPlayerRole)args.Parsers[0].GetValue();
			string privname = (string)args.Parsers[1].GetValue();
			if (role.Privileges.Contains(privname))
			{
				return TextCommandResult.Error(Lang.Get("Role already has this privilege", Array.Empty<object>()), "");
			}
			role.GrantPrivilege(new string[] { privname });
			this.server.ConfigNeedsSaving = true;
			return TextCommandResult.Success(Lang.Get("Ok, privilege '{0}' now granted", new object[] { privname }), null);
		}

		private TextCommandResult OnPrivilegeCmd(TextCommandCallingArgs args)
		{
			IPlayerRole role = (IPlayerRole)args.Parsers[0].GetValue();
			return TextCommandResult.Success(Lang.Get("This role has following privileges: {0}", new object[] { string.Join(", ", role.Privileges) }), null);
		}

		private TextCommandResult OnLandclaimmaxareasCmd(TextCommandCallingArgs args)
		{
			IPlayerRole role = (IPlayerRole)args.Parsers[0].GetValue();
			int? area = (int?)args.Parsers[1].GetValue();
			if (area != null)
			{
				int? num = area;
				int num2 = 0;
				if (!((num.GetValueOrDefault() < num2) & (num != null)))
				{
					role.LandClaimMaxAreas = area.Value;
					this.server.ConfigNeedsSaving = true;
					return TextCommandResult.Success(Lang.Get("Land claim max areas now set to {0}", new object[] { role.LandClaimMaxAreas }), null);
				}
			}
			return TextCommandResult.Success(Lang.Get("This role has a land claim max areas {0}", new object[] { role.LandClaimMaxAreas }), null);
		}

		private TextCommandResult OnLandclaimminsizeCmd(TextCommandCallingArgs args)
		{
			IPlayerRole role = (IPlayerRole)args.Parsers[0].GetValue();
			Vec3i minSize = (Vec3i)args.Parsers[1].GetValue();
			if (minSize == null)
			{
				return TextCommandResult.Success(Lang.Get("This role has a land claim min size of {0} blocks", new object[] { role.LandClaimMinSize }), null);
			}
			role.LandClaimMinSize = minSize;
			this.server.ConfigNeedsSaving = true;
			return TextCommandResult.Success(Lang.Get("Land claim min size now set to {0} blocks", new object[] { role.LandClaimMinSize }), null);
		}

		private TextCommandResult OnLandclaimallowanceCmd(TextCommandCallingArgs args)
		{
			IPlayerRole role = (IPlayerRole)args.Parsers[0].GetValue();
			int? landClaimAllowance = (int?)args.Parsers[1].GetValue();
			if (landClaimAllowance != null)
			{
				int? num = landClaimAllowance;
				int num2 = 0;
				if (!((num.GetValueOrDefault() < num2) & (num != null)))
				{
					role.LandClaimAllowance = landClaimAllowance.Value;
					this.server.ConfigNeedsSaving = true;
					return TextCommandResult.Success(Lang.Get("Land claim allowance now set to {0}m³", new object[] { role.LandClaimAllowance }), null);
				}
			}
			return TextCommandResult.Success(Lang.Get("This role has a land claim allowance of {0}m³", new object[] { role.LandClaimAllowance }), null);
		}

		private TextCommandResult OnCmdMyStats(TextCommandCallingArgs args)
		{
			return this.getStats(new PlayerUidName(args.Caller.Player.PlayerUID, args.Caller.Player.PlayerName), args);
		}

		private TextCommandResult WipePlayerInventory(PlayerUidName targetPlayer, TextCommandCallingArgs args)
		{
			ConnectedClient client = this.server.GetClientByUID(targetPlayer.Uid);
			if (client != null)
			{
				foreach (KeyValuePair<string, InventoryBase> val in client.WorldData.inventories)
				{
					val.Value.Clear();
				}
				client.Player.BroadcastPlayerData(true);
				return TextCommandResult.Success("Inventory cleared.", null);
			}
			this.server.ClearPlayerInvs.Add(targetPlayer.Uid);
			return TextCommandResult.Success("Clear command queued. Inventory will be cleared next time the player connects, which must happen before the server restarts", null);
		}

		private TextCommandResult WipePlayerData(PlayerUidName targetPlayer, TextCommandCallingArgs args)
		{
			if (this.server.chunkThread.gameDatabase.GetPlayerData(targetPlayer.Uid) == null)
			{
				return TextCommandResult.Error("No data for this player found in savegame", "");
			}
			this.server.chunkThread.gameDatabase.SetPlayerData(targetPlayer.Uid, null);
			this.server.PlayerDataManager.PlayerDataByUid.Remove(targetPlayer.Uid);
			this.server.PlayerDataManager.WorldDataByUID.Remove(targetPlayer.Uid);
			this.server.PlayerDataManager.playerDataDirty = true;
			return TextCommandResult.Success("Ok, player data deleted", null);
		}

		private TextCommandResult handleGameMode(TextCommandCallingArgs args)
		{
			IPlayer player = args.Caller.Player;
			string targetPlayername = ((player != null) ? player.PlayerName : null);
			if (args.RawArgs.Length > 0 && (this.server.GetClientByPlayername(args.RawArgs.PeekWord(null)) != null || args.RawArgs.Length > 1))
			{
				targetPlayername = args.RawArgs.PopWord(null);
			}
			string gamemodestr = args.RawArgs.PopWord(null);
			ConnectedClient targetPlayer = this.server.GetClientByPlayername(targetPlayername);
			if (targetPlayer == null)
			{
				return TextCommandResult.Error(Lang.Get("No player with name '{0}' online", new object[] { targetPlayername }), "");
			}
			IPlayer player2 = args.Caller.Player;
			bool isSelf = ((player2 != null) ? player2.PlayerUID : null) == targetPlayer.Player.PlayerUID;
			if (gamemodestr == null)
			{
				if (isSelf)
				{
					return TextCommandResult.Success(Lang.Get("Your Current gamemode is {0}", new object[] { targetPlayer.WorldData.GameMode }), null);
				}
				return TextCommandResult.Success(Lang.Get("Current gamemode for {0} is {1}", new object[]
				{
					targetPlayername,
					targetPlayer.WorldData.GameMode
				}), null);
			}
			else
			{
				if (!isSelf && !args.Caller.HasPrivilege(Privilege.commandplayer))
				{
					return TextCommandResult.Error(Lang.Get("Insufficient Privileges to set another players game mode", Array.Empty<object>()), "");
				}
				if (isSelf && !args.Caller.HasPrivilege(Privilege.gamemode))
				{
					return TextCommandResult.Error(Lang.Get("Insufficient Privileges to set your game mode", Array.Empty<object>()), "");
				}
				return this.SetGameMode(args.Caller, new PlayerUidName(targetPlayer.SentPlayerUid, targetPlayer.PlayerName), gamemodestr);
			}
		}

		private TextCommandResult handleEntity(PlayerUidName targetPlayer, TextCommandCallingArgs args)
		{
			string type = (string)args[1];
			float value = (float)args[2];
			IServerPlayer player = this.server.PlayerByUid(targetPlayer.Uid) as IServerPlayer;
			if (player == null)
			{
				return TextCommandResult.Error(Lang.Get("Player must be online to set attributes", Array.Empty<object>()), "");
			}
			EntityPlayer eplr = player.Entity;
			ITreeAttribute hungerTree = eplr.WatchedAttributes.GetTreeAttribute("hunger");
			ITreeAttribute healthTree = eplr.WatchedAttributes.GetTreeAttribute("health");
			ITreeAttribute oxyTree = eplr.WatchedAttributes.GetTreeAttribute("oxygen");
			if (args.Parsers[1].IsMissing)
			{
				return TextCommandResult.Error(Lang.Get("Position: {0}, Satiety: {1}/{2}, Health: {3}/{4}", new object[]
				{
					eplr.ServerPos.XYZ,
					hungerTree.GetFloat("currentsaturation", 0f),
					hungerTree.TryGetFloat("maxsaturation"),
					healthTree.GetFloat("currenthealth", 0f),
					healthTree.TryGetFloat("maxhealth")
				}), "");
			}
			float? maxSaturation = hungerTree.TryGetFloat("maxsaturation");
			if (type != null)
			{
				switch (type.Length)
				{
				case 4:
					if (!(type == "temp"))
					{
						goto IL_05E1;
					}
					eplr.WatchedAttributes.GetTreeAttribute("bodyTemp").SetFloat("bodytemp", value);
					return TextCommandResult.Success("Body temp " + value.ToString() + " set.", null);
				case 5:
					switch (type[0])
					{
					case 'd':
						if (!(type == "dairy"))
						{
							goto IL_05E1;
						}
						break;
					case 'e':
					case 'h':
						goto IL_05E1;
					case 'f':
						if (!(type == "fruit"))
						{
							goto IL_05E1;
						}
						break;
					case 'g':
						if (!(type == "grain"))
						{
							goto IL_05E1;
						}
						break;
					case 'i':
						if (!(type == "intox"))
						{
							goto IL_05E1;
						}
						eplr.WatchedAttributes.SetFloat("intoxication", value);
						return TextCommandResult.Success("Intoxication value " + value.ToString() + " set.", null);
					default:
						goto IL_05E1;
					}
					break;
				case 6:
				{
					char c = type[0];
					if (c != 'h')
					{
						if (c != 'm')
						{
							goto IL_05E1;
						}
						if (!(type == "maxoxy"))
						{
							goto IL_05E1;
						}
						goto IL_0575;
					}
					else
					{
						if (!(type == "health"))
						{
							goto IL_05E1;
						}
						value = GameMath.Clamp(value, 0f, 1f);
						if (healthTree != null)
						{
							float newval = value * healthTree.TryGetFloat("maxhealth").Value;
							healthTree.SetFloat("currenthealth", newval);
							eplr.WatchedAttributes.MarkPathDirty("health");
							return TextCommandResult.Success("Health " + newval.ToString() + " set.", null);
						}
						return TextCommandResult.Error("health attribute tree not found.", "");
					}
					break;
				}
				case 7:
				{
					char c = type[0];
					if (c != 'p')
					{
						if (c != 's')
						{
							goto IL_05E1;
						}
						if (!(type == "satiety"))
						{
							goto IL_05E1;
						}
						value = GameMath.Clamp(value, 0f, 1f);
						if (hungerTree != null)
						{
							float newval2 = value * maxSaturation.Value;
							hungerTree.SetFloat("currentsaturation", newval2);
							eplr.WatchedAttributes.MarkPathDirty("hunger");
							return TextCommandResult.Success("Satiety " + newval2.ToString() + " set.", null);
						}
						return TextCommandResult.Error("hunger attribute tree not found.", "");
					}
					else if (!(type == "protein"))
					{
						goto IL_05E1;
					}
					break;
				}
				case 8:
					if (!(type == "tempstab"))
					{
						goto IL_05E1;
					}
					value = GameMath.Clamp(value, 0f, 1f);
					eplr.WatchedAttributes.SetDouble("temporalStability", (double)value);
					return TextCommandResult.Success("Stability " + value.ToString() + " set.", null);
				case 9:
				{
					char c = type[3];
					if (c != 'e')
					{
						if (c != 'h')
						{
							if (c != 'o')
							{
								goto IL_05E1;
							}
							if (!(type == "maxoxygen"))
							{
								goto IL_05E1;
							}
							goto IL_0575;
						}
						else
						{
							if (!(type == "maxhealth"))
							{
								goto IL_05E1;
							}
							value = GameMath.Clamp(value, 0f, 9999f);
							if (healthTree != null)
							{
								healthTree.SetFloat("basemaxhealth", value);
								healthTree.SetFloat("maxhealth", value);
								healthTree.SetFloat("currenthealth", value);
								eplr.WatchedAttributes.MarkPathDirty("health");
								return TextCommandResult.Success("Max Health " + value.ToString() + " set.", null);
							}
							return TextCommandResult.Error("health attribute tree not found.", "");
						}
					}
					else if (!(type == "vegetable"))
					{
						goto IL_05E1;
					}
					break;
				}
				default:
					goto IL_05E1;
				}
				value = GameMath.Clamp(value, 0f, 1f);
				if (hungerTree != null)
				{
					float newval3 = value * maxSaturation.Value;
					hungerTree.SetFloat(type + "Level", newval3);
					return TextCommandResult.Success(type + " level " + newval3.ToString() + " set.", null);
				}
				return TextCommandResult.Error("hunger attribute tree not found.", "");
				IL_0575:
				value = GameMath.Clamp(value, 0f, 100000000f);
				if (oxyTree != null)
				{
					oxyTree.SetFloat("maxoxygen", value);
					oxyTree.SetFloat("currentoxygen", value);
					eplr.WatchedAttributes.MarkPathDirty("oxygen");
					return TextCommandResult.Success("Max Oxygen " + value.ToString() + " set.", null);
				}
				return TextCommandResult.Error("Oxygen attribute tree not found.", "");
			}
			IL_05E1:
			return TextCommandResult.Success("Incorrect attribute name", null);
		}

		private TextCommandResult handleLandClaimMaxAreas(PlayerUidName targetPlayer, TextCommandCallingArgs args)
		{
			ServerPlayerData plrdata = this.server.GetServerPlayerData(targetPlayer.Uid);
			if (plrdata == null)
			{
				return TextCommandResult.Error(Lang.Get("Only works for players that have connected to your server at least once", Array.Empty<object>()), "");
			}
			if (args.Parsers[1].IsMissing)
			{
				return TextCommandResult.Success(Lang.Get("This player has a land claim extra max areas setting of {0}", new object[] { plrdata.ExtraLandClaimAreas }), null);
			}
			plrdata.ExtraLandClaimAreas = (int)args[1];
			return TextCommandResult.Success(Lang.Get("Land claim extra max areas now set to {0}", new object[] { plrdata.ExtraLandClaimAreas }), null);
		}

		private TextCommandResult handleLandClaimAllowance(PlayerUidName targetPlayer, TextCommandCallingArgs args)
		{
			ServerPlayerData plrdata = this.server.GetServerPlayerData(targetPlayer.Uid);
			if (plrdata == null)
			{
				return TextCommandResult.Error(Lang.Get("Only works for players that have connected to your server at least once", Array.Empty<object>()), "");
			}
			if (args.Parsers[1].IsMissing)
			{
				return TextCommandResult.Success(Lang.Get("This player has a land claim extra allowance of {0}m³", new object[] { plrdata.ExtraLandClaimAllowance }), null);
			}
			plrdata.ExtraLandClaimAllowance = (int)args[1];
			return TextCommandResult.Success(Lang.Get("Land claim extra allowance now set to {0}m³", new object[] { plrdata.ExtraLandClaimAllowance }), null);
		}

		private TextCommandResult handleCharSel(PlayerUidName targetPlayer, TextCommandCallingArgs args)
		{
			IWorldPlayerData plrdata = this.server.GetWorldPlayerData(targetPlayer.Uid);
			if (plrdata == null)
			{
				return TextCommandResult.Error(Lang.Get("Only works for players that have connected to your server at least once", Array.Empty<object>()), "");
			}
			if (!plrdata.EntityPlayer.WatchedAttributes.GetBool("allowcharselonce", false))
			{
				plrdata.EntityPlayer.WatchedAttributes.SetBool("allowcharselonce", true);
				return TextCommandResult.Success(Lang.Get("Ok, player can now run .charsel to change skin and character class once", Array.Empty<object>()), null);
			}
			return TextCommandResult.Error(Lang.Get("Player can already run .charsel to change skin and character class", Array.Empty<object>()), "");
		}

		private TextCommandResult getSetGameMode(PlayerUidName targetPlayer, TextCommandCallingArgs args)
		{
			if (!args.Parsers[1].IsMissing)
			{
				return this.SetGameMode(args.Caller, targetPlayer, (string)args[1]);
			}
			ServerWorldPlayerData targetPlayerWorldData;
			if (!this.server.PlayerDataManager.WorldDataByUID.TryGetValue(targetPlayer.Uid, out targetPlayerWorldData))
			{
				return TextCommandResult.Error(Lang.Get("Player never connected to this server. Must at least connect once to set game mode", Array.Empty<object>()), "");
			}
			return TextCommandResult.Success(Lang.Get("Player has game mode {0}", new object[] { targetPlayerWorldData.GameMode }), null);
		}

		private TextCommandResult SetGameMode(Caller caller, PlayerUidName parsedTargetPlayer, string modestring)
		{
			EnumGameMode? mode = null;
			int modeint;
			if (int.TryParse(modestring, out modeint))
			{
				if (Enum.IsDefined(typeof(EnumGameMode), modeint))
				{
					mode = new EnumGameMode?((EnumGameMode)modeint);
				}
			}
			else if (modestring.ToLowerInvariant().StartsWith('c'))
			{
				mode = new EnumGameMode?(EnumGameMode.Creative);
			}
			else if (modestring.ToLowerInvariant().StartsWithOrdinal("sp"))
			{
				mode = new EnumGameMode?(EnumGameMode.Spectator);
			}
			else if (modestring.ToLowerInvariant().StartsWith('s'))
			{
				mode = new EnumGameMode?(EnumGameMode.Survival);
			}
			else if (modestring.ToLowerInvariant().StartsWith('g'))
			{
				mode = new EnumGameMode?(EnumGameMode.Guest);
			}
			if (mode == null)
			{
				return TextCommandResult.Error(Lang.Get("Invalid game mode '{0}'", new object[] { modestring }), "");
			}
			ServerWorldPlayerData targetPlayerWorldData;
			if (!this.server.PlayerDataManager.WorldDataByUID.TryGetValue(parsedTargetPlayer.Uid, out targetPlayerWorldData))
			{
				return TextCommandResult.Error(Lang.Get("Player never connected to this server. Must at least connect once to set game mode.", new object[] { modestring }), "");
			}
			EnumGameMode modeBefore = targetPlayerWorldData.GameMode;
			targetPlayerWorldData.GameMode = mode.Value;
			bool canFreeMove = mode.GetValueOrDefault() == EnumGameMode.Creative || mode.GetValueOrDefault() == EnumGameMode.Spectator;
			targetPlayerWorldData.FreeMove = (targetPlayerWorldData.FreeMove && canFreeMove) || mode.GetValueOrDefault() == EnumGameMode.Spectator;
			targetPlayerWorldData.NoClip = (targetPlayerWorldData.NoClip && canFreeMove) || mode.GetValueOrDefault() == EnumGameMode.Spectator;
			EnumGameMode? enumGameMode;
			EnumGameMode enumGameMode2;
			if (mode.GetValueOrDefault() != EnumGameMode.Survival)
			{
				enumGameMode = mode;
				enumGameMode2 = EnumGameMode.Guest;
				if (!((enumGameMode.GetValueOrDefault() == enumGameMode2) & (enumGameMode != null)))
				{
					goto IL_01A7;
				}
			}
			if (modeBefore == EnumGameMode.Creative)
			{
				targetPlayerWorldData.PreviousPickingRange = targetPlayerWorldData.PickingRange;
			}
			targetPlayerWorldData.PickingRange = GlobalConstants.DefaultPickingRange;
			IL_01A7:
			if (mode.GetValueOrDefault() == EnumGameMode.Creative && (modeBefore == EnumGameMode.Survival || modeBefore == EnumGameMode.Guest))
			{
				targetPlayerWorldData.PickingRange = targetPlayerWorldData.PreviousPickingRange;
			}
			ConnectedClient connectedClient = this.server.GetConnectedClient(parsedTargetPlayer.Uid);
			ServerPlayer targetPlayer = ((connectedClient != null) ? connectedClient.Player : null);
			enumGameMode = mode;
			enumGameMode2 = modeBefore;
			if (!((enumGameMode.GetValueOrDefault() == enumGameMode2) & (enumGameMode != null)))
			{
				if (targetPlayer != null)
				{
					for (int i = 0; i < this.server.Systems.Length; i++)
					{
						this.server.Systems[i].OnPlayerSwitchGameMode(targetPlayer);
					}
				}
				enumGameMode = mode;
				enumGameMode2 = EnumGameMode.Guest;
				if (((enumGameMode.GetValueOrDefault() == enumGameMode2) & (enumGameMode != null)) || mode.GetValueOrDefault() == EnumGameMode.Survival)
				{
					targetPlayerWorldData.MoveSpeedMultiplier = 1f;
				}
				if (targetPlayer != null)
				{
					this.server.EventManager.TriggerPlayerChangeGamemode(targetPlayer);
				}
			}
			if (targetPlayer != null)
			{
				this.server.BroadcastPlayerData(targetPlayer, false, false);
				targetPlayer.Entity.UpdatePartitioning();
				TcpNetConnection tcpSocket = targetPlayer.client.Socket as TcpNetConnection;
				if (tcpSocket != null)
				{
					tcpSocket.SetLengthLimit(mode.GetValueOrDefault() == EnumGameMode.Creative);
				}
			}
			string text2;
			if (mode != null)
			{
				string text = "gamemode-";
				enumGameMode = mode;
				text2 = Lang.Get(text + enumGameMode.ToString(), Array.Empty<object>());
			}
			else
			{
				text2 = "-";
			}
			string modeLocalized = text2;
			if (targetPlayer == caller.Player)
			{
				ServerMain.Logger.Audit("{0} put himself into game mode {1}", new object[]
				{
					caller.GetName(),
					modeLocalized
				});
				return TextCommandResult.Success(Lang.Get("Game mode {0} set.", new object[] { modeLocalized }), null);
			}
			if (targetPlayer != null)
			{
				targetPlayer.SendMessage(GlobalConstants.CurrentChatGroup, Lang.Get("{0} has set your gamemode to {1}", new object[]
				{
					caller.GetName(),
					modeLocalized
				}), EnumChatType.Notification, null);
			}
			ServerMain.Logger.Audit("{0} put {1} into game mode {2}", new object[]
			{
				caller.GetName(),
				parsedTargetPlayer.Name,
				modeLocalized
			});
			return TextCommandResult.Success(Lang.Get("Game mode {0} set for player {1}.", new object[] { modeLocalized, parsedTargetPlayer.Name }), null);
		}

		private TextCommandResult getStats(PlayerUidName targetPlayer, TextCommandCallingArgs args)
		{
			ServerPlayerData plrData = this.server.GetServerPlayerData(targetPlayer.Uid);
			HashSet<string> privcodes = plrData.GetAllPrivilegeCodes(this.server.Config);
			StringBuilder stats = new StringBuilder();
			ConnectedClient client = this.server.GetClientByUID(targetPlayer.Uid);
			PlayerRole role = plrData.GetPlayerRole(this.server);
			stats.AppendLine(Lang.Get("{0} is currently {1}", new object[]
			{
				plrData.LastKnownPlayername,
				(client == null) ? "offline" : "online"
			}));
			stats.AppendLine(Lang.Get("Role: {0}", new object[] { plrData.RoleCode }));
			stats.AppendLine(Lang.Get("All Privilege codes: {0}", new object[] { string.Join(", ", privcodes.ToArray<string>()) }));
			stats.AppendLine(Lang.Get("Land claim allowance: {0}m³ + {1}m³", new object[] { role.LandClaimAllowance, plrData.ExtraLandClaimAllowance }));
			stats.AppendLine(Lang.Get("Land claim max areas: {0} + {1}", new object[] { role.LandClaimMaxAreas, plrData.ExtraLandClaimAreas }));
			List<LandClaim> claims = CmdLand.GetPlayerClaims(this.server, targetPlayer.Uid);
			int totalSize = 0;
			foreach (LandClaim claim in claims)
			{
				totalSize += claim.SizeXYZ;
			}
			stats.AppendLine(Lang.Get("Land claimed: {0}m³", new object[] { totalSize }));
			stats.AppendLine(Lang.Get("Amount of areas claimed: {0}", new object[] { claims.Count }));
			if (args.Caller.HasPrivilege(Privilege.grantrevoke) && client != null)
			{
				stats.AppendLine(string.Format("Fly suspicion count: {0}", client.AuditFlySuspicion));
				stats.AppendLine(string.Format("Tele/Speed suspicion count: {0}", client.TotalTeleSupicions));
			}
			return TextCommandResult.Success(stats.ToString(), null);
		}

		private TextCommandResult GetSetRole(PlayerUidName targetPlayer, TextCommandCallingArgs args)
		{
			if (args.Parsers.Count == 0 || args.Parsers[1].IsMissing)
			{
				ServerPlayerData targetPlayerData = this.server.PlayerDataManager.GetOrCreateServerPlayerData(targetPlayer.Uid, targetPlayer.Name);
				return TextCommandResult.Success("Player has role " + targetPlayerData.RoleCode, null);
			}
			PlayerRole role = (PlayerRole)args[1];
			return this.ChangeRole(args.Caller, targetPlayer, role.Code);
		}

		private TextCommandResult opPlayer(PlayerUidName targetPlayer, TextCommandCallingArgs args)
		{
			return this.ChangeRole(args.Caller, targetPlayer, "admin");
		}

		public TextCommandResult ChangeRole(Caller caller, PlayerUidName targetPlayer, string newRoleCode)
		{
			ServerPlayerData targetPlayerData = this.server.PlayerDataManager.GetOrCreateServerPlayerData(targetPlayer.Uid, targetPlayer.Name);
			if (targetPlayerData == null)
			{
				return TextCommandResult.Error(Lang.Get("No player with this playername found", Array.Empty<object>()), "");
			}
			IPlayer player = caller.Player;
			if (((player != null) ? player.PlayerUID : null) == targetPlayerData.PlayerUID)
			{
				return TextCommandResult.Error(Lang.Get("Can't change your own group", Array.Empty<object>()), "");
			}
			PlayerRole newRole = null;
			foreach (KeyValuePair<string, PlayerRole> val in this.Config.RolesByCode)
			{
				if (val.Key.ToLowerInvariant() == newRoleCode.ToLowerInvariant())
				{
					newRole = val.Value;
					break;
				}
			}
			if (newRole == null)
			{
				return TextCommandResult.Error(Lang.Get("No group '{0}' found", new object[] { newRoleCode }), "");
			}
			string callerRole = caller.CallerRole;
			if (caller.Player != null)
			{
				callerRole = this.server.PlayerDataManager.GetPlayerDataByUid(caller.Player.PlayerUID).RoleCode;
			}
			PlayerRole issuingRole;
			this.Config.RolesByCode.TryGetValue(callerRole, out issuingRole);
			if (newRole.IsSuperior(issuingRole) || (newRole.EqualLevel(issuingRole) && !caller.HasPrivilege(Privilege.root)))
			{
				return TextCommandResult.Error(Lang.Get("Can only set lower role level than your own", Array.Empty<object>()), "");
			}
			PlayerRole oldTargetRole = this.Config.RolesByCode[targetPlayerData.RoleCode];
			if (oldTargetRole.Code == newRole.Code)
			{
				return TextCommandResult.Error(Lang.Get("Player is already in group {0}", new object[] { oldTargetRole.Code }), "");
			}
			if (oldTargetRole.IsSuperior(issuingRole) || (oldTargetRole.EqualLevel(issuingRole) && !caller.HasPrivilege(Privilege.root)))
			{
				return TextCommandResult.Error(Lang.Get("Can't modify a players role with a superior role. Players current role is {0}", new object[] { oldTargetRole.Code }), "");
			}
			targetPlayerData.SetRole(newRole);
			this.server.PlayerDataManager.playerDataDirty = true;
			ServerMain.Logger.Audit(string.Format("{0} assigned {1} the role {2}.", caller.GetName(), newRole.Name, targetPlayer.Name));
			ConnectedClient client = this.server.GetClientByPlayername(targetPlayer.Name);
			if (client != null)
			{
				this.server.SendOwnPlayerData(client.Player, false, true);
				string msg = ((newRole.PrivilegeLevel > oldTargetRole.PrivilegeLevel) ? Lang.Get("You've been promoted to role {0}", new object[] { newRole.Name }) : Lang.Get("You've been demoted to role {0}", new object[] { newRole.Name }));
				this.server.SendMessage(client.Player, GlobalConstants.CurrentChatGroup, msg, EnumChatType.Notification, null);
				this.server.SendRoles(client.Player);
			}
			return TextCommandResult.Success(Lang.Get("Ok, role {0} assigned to {1}", new object[] { newRole.Name, targetPlayer.Name }), null);
		}

		private TextCommandResult removeDenyPrivilege(PlayerUidName targetPlayer, TextCommandCallingArgs args)
		{
			ServerPlayerData plrdata = this.server.PlayerDataManager.GetOrCreateServerPlayerData(targetPlayer.Uid, targetPlayer.Name);
			string privilege = (string)args[1];
			string targetPlayerName = targetPlayer.Name;
			if (!plrdata.DeniedPrivileges.Contains(privilege))
			{
				return TextCommandResult.Error(Lang.Get("Player {0} did not have this privilege denied.", new object[] { targetPlayerName }), "");
			}
			plrdata.RemovePrivilegeDenial(privilege);
			string hisMsg = Lang.Get("{0} removed your Privilege denial for {1}", new object[]
			{
				args.Caller.GetName(),
				privilege
			});
			ConnectedClient targetClient = this.server.GetConnectedClient(targetPlayer.Uid);
			if (targetClient != null)
			{
				this.server.SendMessage(targetClient.Player, GlobalConstants.CurrentChatGroup, hisMsg, EnumChatType.Notification, null);
				this.server.SendOwnPlayerData(targetClient.Player, false, true);
			}
			ServerMain.Logger.Audit(string.Format("{0} no longer denied {1} the privilege {2}.", args.Caller.GetName(), targetPlayer.Name, privilege));
			ServerMain.Logger.Event(string.Format("{0} no longer denied {1} the privilege {2}.", args.Caller.GetName(), targetPlayer.Name, privilege));
			return TextCommandResult.Success(Lang.Get("Privilege {0} is no longer denied from {1}", new object[] { privilege, targetPlayerName }), null);
		}

		private TextCommandResult denyPrivilege(PlayerUidName targetPlayer, TextCommandCallingArgs args)
		{
			ServerPlayerData plrdata = this.server.PlayerDataManager.GetOrCreateServerPlayerData(targetPlayer.Uid, targetPlayer.Name);
			string privilege = (string)args[1];
			string targetPlayerName = targetPlayer.Name;
			if (plrdata.DeniedPrivileges.Contains(privilege))
			{
				return TextCommandResult.Error(Lang.Get("Player {0} already has this privilege denied.", new object[] { targetPlayerName }), "");
			}
			plrdata.DenyPrivilege(privilege);
			string hisMsg = Lang.Get("{0} has denied Privilege {1}", new object[]
			{
				args.Caller.GetName(),
				privilege
			});
			ConnectedClient targetClient = this.server.GetConnectedClient(targetPlayer.Uid);
			if (targetClient != null)
			{
				this.server.SendMessage(targetClient.Player, GlobalConstants.CurrentChatGroup, hisMsg, EnumChatType.Notification, null);
				this.server.SendOwnPlayerData(targetClient.Player, false, true);
			}
			ServerMain.Logger.Audit(string.Format("{0} denied {1} the privilege {2}.", args.Caller.GetName(), targetPlayer.Name, privilege));
			ServerMain.Logger.Event(string.Format("{0} denied {1} the privilege {2}.", args.Caller.GetName(), targetPlayer.Name, privilege));
			return TextCommandResult.Success(Lang.Get("Privilege {0} has been denied from {1}", new object[] { privilege, targetPlayerName }), null);
		}

		private TextCommandResult revokePrivilege(PlayerUidName targetPlayer, TextCommandCallingArgs args)
		{
			ServerPlayerData plrdata = this.server.PlayerDataManager.GetOrCreateServerPlayerData(targetPlayer.Uid, targetPlayer.Name);
			string privilege = (string)args[1];
			string targetPlayerName = targetPlayer.Name;
			if (!plrdata.PermaPrivileges.Contains(privilege) && !plrdata.HasPrivilege(privilege, this.server.Config.RolesByCode))
			{
				return TextCommandResult.Error(Lang.Get("Player {0} does not have this privilege neither directly or by role", new object[] { targetPlayerName }), "");
			}
			plrdata.RevokePrivilege(privilege);
			string hisMsg = Lang.Get("{0} has revoked your Privilege {1}", new object[]
			{
				args.Caller.GetName(),
				privilege
			});
			ConnectedClient targetClient = this.server.GetConnectedClient(targetPlayer.Uid);
			if (targetClient != null)
			{
				this.server.SendMessage(targetClient.Player, GlobalConstants.CurrentChatGroup, hisMsg, EnumChatType.Notification, null);
				this.server.SendOwnPlayerData(targetClient.Player, false, true);
			}
			ServerMain.Logger.Audit(string.Format("{0} revoked {1} privilege {2}.", args.Caller.GetName(), targetPlayer.Name, privilege));
			ServerMain.Logger.Event(string.Format("{0} revoked {1} privilege {2}.", args.Caller.GetName(), targetPlayer.Name, privilege));
			return TextCommandResult.Success(Lang.Get("Privilege {0} has been revoked from {1}", new object[] { privilege, targetPlayerName }), null);
		}

		private TextCommandResult grantPrivilege(PlayerUidName targetPlayer, TextCommandCallingArgs args)
		{
			ServerPlayerData plrdata = this.server.PlayerDataManager.GetOrCreateServerPlayerData(targetPlayer.Uid, targetPlayer.Name);
			string privilege = (string)args[1];
			string targetPlayerName = targetPlayer.Name;
			string denyRemoveMsg = "";
			if (plrdata.DeniedPrivileges.Contains(privilege))
			{
				denyRemoveMsg = Lang.Get("Privilege deny for '{0}' removed from player {1}", new object[] { privilege, targetPlayerName });
				ServerMain.Logger.Audit("{0} removed the privilege deny for '{1}' from player {2}", new object[]
				{
					args.Caller.GetName(),
					privilege,
					targetPlayerName
				});
			}
			if (!plrdata.HasPrivilege(privilege, this.server.Config.RolesByCode))
			{
				plrdata.GrantPrivilege(privilege);
				ConnectedClient targetClient = this.server.GetConnectedClient(targetPlayer.Uid);
				if (targetClient != null)
				{
					string hisMsg = Lang.Get("{0} granted you the privilege {1}", new object[]
					{
						args.Caller.GetName(),
						privilege
					});
					this.server.SendMessage(targetClient.Player, GlobalConstants.CurrentChatGroup, hisMsg, EnumChatType.Notification, null);
					this.server.SendOwnPlayerData(targetClient.Player, false, true);
				}
				ServerMain.Logger.Audit("Player {0} granted {1} the privilege {2}", new object[]
				{
					args.Caller.GetName(),
					targetPlayerName,
					privilege
				});
				ServerMain.Logger.Event(string.Format("{0} grants {1} the privilege {2}.", args.Caller.GetName(), targetPlayerName, privilege));
				return TextCommandResult.Success(Lang.Get("Privilege {0} granted to {1}", new object[] { privilege, targetPlayerName }), null);
			}
			if (denyRemoveMsg.Length == 0)
			{
				return TextCommandResult.Error(Lang.Get("Player {0} has this privilege already", new object[] { targetPlayerName }), "");
			}
			return TextCommandResult.Success(denyRemoveMsg, null);
		}

		private TextCommandResult listPrivilege(PlayerUidName targetPlayer, TextCommandCallingArgs args)
		{
			string uid = targetPlayer.Uid;
			IPlayer player = args.Caller.Player;
			bool self = uid == ((player != null) ? player.PlayerUID : null);
			if (!this.server.PlayerDataManager.PlayerDataByUid.ContainsKey(targetPlayer.Uid))
			{
				return TextCommandResult.Error(Lang.Get("This player is has never joined your server. He will have the privileges of the default role '{0}'.", new object[] { this.server.Config.DefaultRoleCode }), "");
			}
			ServerPlayerData serverPlayerData = this.server.PlayerDataManager.PlayerDataByUid[targetPlayer.Uid];
			HashSet<string> privcodes = serverPlayerData.GetAllPrivilegeCodes(this.server.Config);
			foreach (string priv in serverPlayerData.DeniedPrivileges)
			{
				privcodes.Remove(priv);
			}
			return TextCommandResult.Success(self ? Lang.Get("You have {0} privileges: {1}", new object[]
			{
				privcodes.Count,
				privcodes.Implode(", ")
			}) : Lang.Get("{0} has {1} privileges: {2}", new object[]
			{
				targetPlayer.Name,
				privcodes.Count,
				privcodes.Implode(", ")
			}), null);
		}

		private TextCommandResult addRemoveWhitelist(PlayerUidName targetPlayer, TextCommandCallingArgs args)
		{
			if (args.Parsers[1].IsMissing)
			{
				bool islisted = this.server.PlayerDataManager.WhitelistedPlayers.Any((PlayerEntry item) => item.PlayerUID == targetPlayer.Uid);
				return TextCommandResult.Success(Lang.Get("Player is currently {0}", new object[] { islisted ? "whitelisted" : "not whitelisted" }), null);
			}
			bool flag = (bool)args[1];
			string reason = (string)args[2];
			this.server.PlayerDataManager.GetOrCreateServerPlayerData(targetPlayer.Uid, targetPlayer.Name);
			string issuer = ((args.Caller.Player != null) ? args.Caller.Player.PlayerName : args.Caller.Type.ToString());
			if (flag)
			{
				DateTime untildate = DateTime.Now.AddYears(50);
				this.server.PlayerDataManager.WhitelistPlayer(targetPlayer.Name, targetPlayer.Uid, issuer, reason, new DateTime?(untildate));
				return TextCommandResult.Success(Lang.Get("Player is now whitelisted until {0}", new object[] { untildate }), null);
			}
			if (this.server.PlayerDataManager.UnWhitelistPlayer(targetPlayer.Name, targetPlayer.Uid))
			{
				return TextCommandResult.Success(Lang.Get("Player is now removed from the whitelist", Array.Empty<object>()), null);
			}
			return TextCommandResult.Error(Lang.Get("Player is not whitelisted", Array.Empty<object>()), "");
		}

		private TextCommandResult setMovespeed(PlayerUidName targetPlayer, TextCommandCallingArgs args)
		{
			IWorldPlayerData plrdata = this.server.GetWorldPlayerData(targetPlayer.Uid);
			plrdata.MoveSpeedMultiplier = (float)args[1];
			IServerPlayer plr = this.server.PlayerByUid(plrdata.PlayerUID) as IServerPlayer;
			if (plr != null)
			{
				plr.Entity.Controls.MovespeedMultiplier = plrdata.MoveSpeedMultiplier;
				this.server.broadCastModeChange(plr);
			}
			return TextCommandResult.Success("Ok, movespeed set to " + plrdata.MoveSpeedMultiplier.ToString(), null);
		}

		public static TextCommandResult Each(TextCommandCallingArgs args, CmdPlayer.PlayerEachDelegate onPlayer)
		{
			PlayerUidName[] players = (PlayerUidName[])args.Parsers[0].GetValue();
			int successCnt = 0;
			LimitedList<TextCommandResult> results = new LimitedList<TextCommandResult>(10);
			if (players.Length == 0)
			{
				return TextCommandResult.Error(Lang.Get("No players found that match your selector", Array.Empty<object>()), "");
			}
			foreach (PlayerUidName parsedplayer in players)
			{
				TextCommandResult result = onPlayer(parsedplayer, args);
				if (result.Status == EnumCommandStatus.Success)
				{
					successCnt++;
				}
				results.Add(result);
			}
			if (players.Length <= 10)
			{
				return TextCommandResult.Success(string.Join(", ", results.Select((TextCommandResult el) => el.StatusMessage)), null);
			}
			return TextCommandResult.Success(Lang.Get("Successfully executed commands on {0}/{1} players", new object[] { successCnt, players.Length }), null);
		}

		public delegate TextCommandResult PlayerEachDelegate(PlayerUidName targetPlayer, TextCommandCallingArgs args);
	}
}
