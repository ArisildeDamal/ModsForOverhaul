using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.Server
{
	public class ServerySystemPlayerGroups : ServerSystem
	{
		public Dictionary<int, PlayerGroup> PlayerGroupsByUid
		{
			get
			{
				return this.server.PlayerDataManager.PlayerGroupsById;
			}
		}

		public ServerySystemPlayerGroups(ServerMain server)
			: base(server)
		{
			CommandArgumentParsers parsers = server.api.ChatCommands.Parsers;
			server.api.ChatCommands.Create("group").WithDescription("Manage a player group").RequiresPrivilege(Privilege.controlplayergroups)
				.BeginSubCommand("create")
				.WithDescription("Creates a new group.")
				.WithExamples(new string[] { "Syntax: /group create [groupname]" })
				.RequiresPlayer()
				.WithArgs(new ICommandArgumentParser[] { parsers.Word("groupName") })
				.HandleWith(new OnCommandDelegate(this.CmdCreategroup))
				.EndSubCommand()
				.BeginSubCommand("disband")
				.WithDescription("Disband a group. Only the owner has the privilege to disband.")
				.WithExamples(new string[] { "Syntax: /group disband [groupname]" })
				.RequiresPlayer()
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalWord("groupName") })
				.HandleWith(new OnCommandDelegate(this.CmdDisbandgroup))
				.EndSubCommand()
				.BeginSubCommand("confirmdisband")
				.WithDescription("Confirm disband a group. Only the owner has the privilege to disband.")
				.WithExamples(new string[] { "Syntax: /group confirmdisband [groupname]" })
				.RequiresPlayer()
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalWord("groupName") })
				.HandleWith(new OnCommandDelegate(this.CmdConfirmDisbandgroup))
				.EndSubCommand()
				.BeginSubCommand("joinpolicy")
				.WithDescription("Define how users can join your group")
				.RequiresPlayer()
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalWordRange("policy", new string[] { "inviteonly", "everyone" }) })
				.HandleWith(new OnCommandDelegate(this.CmdJoinPolicy))
				.EndSubCommand()
				.BeginSubCommand("join")
				.WithDescription("Join a group thats open for everyone")
				.RequiresPlayer()
				.WithArgs(new ICommandArgumentParser[] { parsers.Word("group name") })
				.HandleWith(new OnCommandDelegate(this.CmdJoin))
				.EndSubCommand()
				.BeginSubCommand("rename")
				.WithDescription("Rename a group.")
				.WithExamples(new string[] { "Syntax: /group rename [oldname] [newname]", " Syntax in group chat: /group rename [newname]" })
				.RequiresPlayer()
				.WithArgs(new ICommandArgumentParser[]
				{
					parsers.Word("groupName"),
					parsers.OptionalWord("newName")
				})
				.HandleWith(new OnCommandDelegate(this.CmdRenamegroup))
				.EndSubCommand()
				.BeginSubCommand("invite")
				.WithDescription("Invite a player.")
				.WithExamples(new string[] { "Syntax: /group invite [groupname] [playername]" })
				.RequiresPlayer()
				.WithArgs(new ICommandArgumentParser[]
				{
					parsers.Word("groupName"),
					parsers.OptionalWord("playerName")
				})
				.HandleWith(new OnCommandDelegate(this.CmdInvitePlayer))
				.EndSubCommand()
				.BeginSubCommand("acceptinvite")
				.WithAlias(new string[] { "ai" })
				.WithDescription("Accept an invitation to a group.")
				.WithExamples(new string[] { "Syntax: /group acceptinvite [groupname/groupid]" })
				.WithArgs(new ICommandArgumentParser[] { parsers.Word("groupName/groupId") })
				.RequiresPlayer()
				.HandleWith(new OnCommandDelegate(this.CmdAcceptInvite))
				.EndSubCommand()
				.BeginSubCommand("leave")
				.WithDescription("Leave a group.")
				.WithExamples(new string[] { "Syntax: /group leave [groupname]", "/group leave while in the groups chat room" })
				.RequiresPlayer()
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalWord("groupName") })
				.HandleWith(new OnCommandDelegate(this.CmdLeavegroup))
				.EndSubCommand()
				.BeginSubCommand("list")
				.WithDescription("Lists the group you are in")
				.RequiresPlayer()
				.HandleWith(new OnCommandDelegate(this.CmdListgroups))
				.EndSubCommand()
				.BeginSubCommand("info")
				.WithDescription("Show some info on a group.")
				.WithExamples(new string[] { "Syntax: /group info [groupname]" })
				.RequiresPlayer()
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalWord("groupName") })
				.HandleWith(new OnCommandDelegate(this.CmdgroupInfo))
				.EndSubCommand()
				.BeginSubCommand("kick")
				.WithDescription("Kick a player from a group.")
				.WithExamples(new string[] { "Syntax: /group kick [groupname] (playername)", "/group kick (playername) while in the groups chat room" })
				.RequiresPlayer()
				.WithArgs(new ICommandArgumentParser[]
				{
					parsers.Word("groupName"),
					parsers.OptionalWord("playerName")
				})
				.HandleWith(new OnCommandDelegate(this.CmdKickFromgroup))
				.EndSubCommand()
				.BeginSubCommand("op")
				.WithDescription("Grant operator status to a player. Gives that player the ability to kick and invite players.")
				.WithExamples(new string[] { "Syntax: /group op [groupname] (playername)", "/group op (playername) while in the groups chat room" })
				.RequiresPlayer()
				.WithArgs(new ICommandArgumentParser[]
				{
					parsers.Word("groupName"),
					parsers.OptionalWord("playerName")
				})
				.HandleWith((TextCommandCallingArgs args) => this.CmdOpPlayer(args, false))
				.EndSubCommand()
				.BeginSubCommand("deop")
				.WithDescription("Revoke operator status from a player.")
				.WithExamples(new string[] { "Syntax: /group deop [groupname] (playername)", "/group deop (playername) while in the groups chat room" })
				.RequiresPlayer()
				.WithArgs(new ICommandArgumentParser[]
				{
					parsers.Word("groupName"),
					parsers.OptionalWord("playerName")
				})
				.HandleWith((TextCommandCallingArgs args) => this.CmdOpPlayer(args, true))
				.EndSubCommand();
			server.api.ChatCommands.Create("groupinvite").WithDescription("Enables or disables group invites to be sent to you").RequiresPrivilege(Privilege.chat)
				.RequiresPlayer()
				.WithArgs(new ICommandArgumentParser[] { parsers.Bool("enable", "on") })
				.HandleWith(new OnCommandDelegate(this.CmdNoInvite));
		}

		public override void OnPlayerJoinPost(ServerPlayer player)
		{
			List<int> removedPlayergroups = new List<int>();
			foreach (KeyValuePair<int, PlayerGroupMembership> val in player.serverdata.PlayerGroupMemberShips)
			{
				if (val.Value.Level != EnumPlayerGroupMemberShip.None)
				{
					PlayerGroup plrGroup;
					this.server.PlayerDataManager.PlayerGroupsById.TryGetValue(val.Key, out plrGroup);
					if (plrGroup == null)
					{
						removedPlayergroups.Add(val.Key);
						this.server.SendMessage(player, GlobalConstants.ServerInfoChatGroup, "The player group " + val.Value.GroupName + " you were a member of no longer exists. It probably has been disbanded", EnumChatType.Notification, null);
					}
					else
					{
						this.server.PlayerDataManager.PlayerGroupsById[val.Key].OnlinePlayers.Add(player);
						if (plrGroup.Name != val.Value.GroupName)
						{
							this.server.SendMessage(player, GlobalConstants.ServerInfoChatGroup, "The player group " + val.Value.GroupName + " you were a member of has been renamed to " + plrGroup.Name, EnumChatType.Notification, null);
							val.Value.GroupName = plrGroup.Name;
						}
					}
				}
			}
			foreach (int groupid in removedPlayergroups)
			{
				player.serverdata.PlayerGroupMemberShips.Remove(groupid);
				this.server.PlayerDataManager.playerDataDirty = true;
			}
			this.SendPlayerGroups(player);
		}

		public override void OnPlayerDisconnect(ServerPlayer player)
		{
			foreach (KeyValuePair<int, PlayerGroupMembership> val in player.serverdata.PlayerGroupMemberShips)
			{
				PlayerGroup group;
				if (val.Value.Level != EnumPlayerGroupMemberShip.None && this.server.PlayerDataManager.PlayerGroupsById.TryGetValue(val.Key, out group))
				{
					group.OnlinePlayers.Remove(player);
				}
			}
		}

		private TextCommandResult Success(TextCommandCallingArgs args, string message, params string[] msgargs)
		{
			return TextCommandResult.Success(Lang.GetL(args.LanguageCode, message, msgargs), null);
		}

		private TextCommandResult Error(TextCommandCallingArgs args, string message, params string[] msgargs)
		{
			return TextCommandResult.Error(Lang.GetL(args.LanguageCode, message, msgargs), "");
		}

		private TextCommandResult CmdCreategroup(TextCommandCallingArgs args)
		{
			string playerUid = args.Caller.Player.PlayerUID;
			IServerPlayer player = args.Caller.Player as IServerPlayer;
			string groupName = args[0] as string;
			if (!this.server.PlayerDataManager.CanCreatePlayerGroup(playerUid))
			{
				return this.Error(args, "No privilege to create groups.", Array.Empty<string>());
			}
			if (this.server.PlayerDataManager.GetPlayerGroupByName(groupName) != null)
			{
				return this.Error(args, "This group name already exists, please choose another name", Array.Empty<string>());
			}
			if (Regex.IsMatch(groupName, "[^" + GlobalConstants.AllowedChatGroupChars + "]+"))
			{
				return this.Error(args, "Invalid group name, may only use letters and numbers", Array.Empty<string>());
			}
			PlayerGroup group = new PlayerGroup
			{
				Name = groupName,
				OwnerUID = playerUid
			};
			this.server.PlayerDataManager.AddPlayerGroup(group);
			group.Md5Identifier = GameMath.Md5Hash(group.Uid.ToString() + playerUid);
			this.server.PlayerDataManager.PlayerDataByUid[playerUid].JoinGroup(group, EnumPlayerGroupMemberShip.Owner);
			group.OnlinePlayers.Add(player);
			this.SendPlayerGroup(player, group);
			this.GotoGroup(player, group.Uid);
			this.server.PlayerDataManager.playerDataDirty = true;
			this.server.PlayerDataManager.playerGroupsDirty = true;
			player.SendMessage(group.Uid, Lang.GetL(player.LanguageCode, "Group {0} created by {1}", new object[]
			{
				args[0],
				player.PlayerName
			}), EnumChatType.CommandSuccess, null);
			return this.Success(args, "Group {0} created.", new string[] { args[0] as string });
		}

		private int GetgroupId(string groupName)
		{
			foreach (PlayerGroup group in this.server.PlayerDataManager.PlayerGroupsById.Values)
			{
				if (group.Name.Equals(groupName, StringComparison.CurrentCultureIgnoreCase))
				{
					return group.Uid;
				}
			}
			return 0;
		}

		private TextCommandResult CmdDisbandgroup(TextCommandCallingArgs args)
		{
			IServerPlayer player = args.Caller.Player as IServerPlayer;
			string playerUid = player.PlayerUID;
			int targetgroupid = args.Caller.FromChatGroupId;
			if (!args.Parsers[0].IsMissing)
			{
				targetgroupid = this.GetgroupId(args[0] as string);
			}
			if (targetgroupid <= 0)
			{
				return this.Error(args, "Invalid group name", Array.Empty<string>());
			}
			if (!this.HasPlayerPrivilege(player, targetgroupid, EnumPlayerGroupPrivilege.Disband, null))
			{
				return this.Error(args, "You must be the owner of the group to disband it.", Array.Empty<string>());
			}
			if (this.DisbandRequests.ContainsKey(targetgroupid))
			{
				return this.Error(args, "Disband already requested, type /group confirmdisband [groupname] to confirm.", Array.Empty<string>());
			}
			this.DisbandRequests.Add(targetgroupid, playerUid);
			return this.Success(args, "Really disband group {0}? Type /group confirmdisband [groupname] to confirm.", new string[] { this.PlayerGroupsByUid[targetgroupid].Name });
		}

		private TextCommandResult CmdConfirmDisbandgroup(TextCommandCallingArgs args)
		{
			IServerPlayer player = args.Caller.Player as IServerPlayer;
			string playerUid = player.PlayerUID;
			int targetgroupid = args.Caller.FromChatGroupId;
			if (!args.Parsers[0].IsMissing)
			{
				targetgroupid = this.GetgroupId(args[0] as string);
			}
			if (targetgroupid <= 0)
			{
				return this.Error(args, "Invalid group name", Array.Empty<string>());
			}
			if (!this.HasPlayerPrivilege(player, targetgroupid, EnumPlayerGroupPrivilege.Disband, null))
			{
				return this.Error(args, "You must be the owner of the group to disband it.", Array.Empty<string>());
			}
			if (this.DisbandRequests.ContainsKey(targetgroupid) && this.DisbandRequests[targetgroupid] == playerUid)
			{
				PlayerGroup plrgroup = this.PlayerGroupsByUid[targetgroupid];
				this.server.PlayerDataManager.RemovePlayerGroup(this.PlayerGroupsByUid[targetgroupid]);
				this.server.PlayerDataManager.playerGroupsDirty = true;
				this.server.PlayerDataManager.playerDataDirty = true;
				using (List<IPlayer>.Enumerator enumerator = plrgroup.OnlinePlayers.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						IPlayer player2 = enumerator.Current;
						IServerPlayer memberPlayer = (IServerPlayer)player2;
						((ServerPlayer)memberPlayer).serverdata.LeaveGroup(plrgroup);
						this.SendPlayerGroups(memberPlayer);
						string msg = Lang.GetL(memberPlayer.LanguageCode, "Player group {0} has been disbanded by {1}", new object[] { plrgroup.Name, player.PlayerName });
						memberPlayer.SendMessage((memberPlayer.ClientId == player.ClientId && args.Caller.FromChatGroupId != targetgroupid) ? args.Caller.FromChatGroupId : GlobalConstants.ServerInfoChatGroup, msg, EnumChatType.Notification, null);
					}
					goto IL_01BE;
				}
				goto IL_01AC;
				IL_01BE:
				return TextCommandResult.Success("", null);
			}
			IL_01AC:
			return this.Error(args, "Found no disband request to confirm, please use /group disband [groupname] first.", Array.Empty<string>());
		}

		private TextCommandResult CmdRenamegroup(TextCommandCallingArgs args)
		{
			IServerPlayer player = args.Caller.Player as IServerPlayer;
			int targetGroupid = args.Caller.FromChatGroupId;
			string newName;
			if (!args.Parsers[1].IsMissing)
			{
				targetGroupid = this.GetgroupId(args[0] as string);
				newName = args[1] as string;
			}
			else
			{
				newName = args[0] as string;
			}
			if (targetGroupid <= 0)
			{
				return this.Error(args, "Invalid group name", Array.Empty<string>());
			}
			if (!this.HasPlayerPrivilege(player, targetGroupid, EnumPlayerGroupPrivilege.Rename, null))
			{
				return this.Error(args, "You must be the owner of the group to rename it.", Array.Empty<string>());
			}
			if (this.server.PlayerDataManager.GetPlayerGroupByName(newName) != null)
			{
				return this.Error(args, "This group name already exists, please choose another name", Array.Empty<string>());
			}
			if (Regex.IsMatch(newName, "[^" + GlobalConstants.AllowedChatGroupChars + "]+"))
			{
				return this.Error(args, "Invalid group name, may only use letters and numbers", Array.Empty<string>());
			}
			PlayerGroup plrgroup = this.PlayerGroupsByUid[targetGroupid];
			string oldname = plrgroup.Name;
			plrgroup.Name = newName;
			this.server.PlayerDataManager.playerGroupsDirty = true;
			foreach (IPlayer player2 in plrgroup.OnlinePlayers)
			{
				IServerPlayer memberPlayer = (IServerPlayer)player2;
				this.SendPlayerGroup(memberPlayer, plrgroup);
				this.server.Clients[player.ClientId].ServerData.PlayerGroupMemberShips[plrgroup.Uid].GroupName = plrgroup.Name;
				memberPlayer.SendMessage(targetGroupid, Lang.GetL(memberPlayer.LanguageCode, "Player group has been renamed from {0} to {1}", new object[] { oldname, plrgroup.Name }), EnumChatType.Notification, null);
			}
			return this.Success(args, "Player group renamed", Array.Empty<string>());
		}

		private TextCommandResult CmdJoin(TextCommandCallingArgs args)
		{
			IServerPlayer player = args.Caller.Player as IServerPlayer;
			string groupname = args[0] as string;
			PlayerGroup targetGroup = this.PlayerGroupsByUid.FirstOrDefault((KeyValuePair<int, PlayerGroup> g) => g.Value.JoinPolicy == "everyone" && g.Value.Name == groupname).Value;
			if (targetGroup == null)
			{
				this.PlayerGroupsByUid.TryGetValue(groupname.ToInt(0), out targetGroup);
			}
			if (targetGroup == null || targetGroup.JoinPolicy != "everyone")
			{
				return this.Error(args, "No such group found or the invite policy is invite only", Array.Empty<string>());
			}
			int targetGroupid = targetGroup.Uid;
			PlayerGroupMembership membership = ((ServerPlayer)player).serverdata.JoinGroup(targetGroup, EnumPlayerGroupMemberShip.Member);
			this.server.PlayerDataManager.playerDataDirty = true;
			this.PlayerGroupsByUid[targetGroupid].OnlinePlayers.Add(player);
			this.SendPlayerGroup(player, this.PlayerGroupsByUid[targetGroupid], membership);
			this.GotoGroup(player, targetGroupid);
			foreach (IPlayer player2 in this.PlayerGroupsByUid[targetGroupid].OnlinePlayers)
			{
				((IServerPlayer)player2).SendMessage(targetGroupid, Lang.Get("Player {0} has joined the group.", new object[] { player.PlayerName }), EnumChatType.Notification, null);
			}
			return TextCommandResult.Success("", null);
		}

		private TextCommandResult CmdJoinPolicy(TextCommandCallingArgs args)
		{
			IServerPlayer player = args.Caller.Player as IServerPlayer;
			int targetGroupid = args.Caller.FromChatGroupId;
			if (targetGroupid <= 0)
			{
				return this.Error(args, "Must write the command inside the chat group you wish to modify", Array.Empty<string>());
			}
			if (args.Parsers[0].IsMissing)
			{
				return this.Success(args, "Join policy of this group is: {0}.", new string[] { Lang.Get("plrgroup-invitepolicy-" + (this.PlayerGroupsByUid[targetGroupid].JoinPolicy ?? "inviteonly"), Array.Empty<object>()) });
			}
			string policy = args[0] as string;
			if (!this.HasPlayerPrivilege(player, targetGroupid, EnumPlayerGroupPrivilege.Rename, null))
			{
				return this.Error(args, "You must be the owner of the group to rename it.", Array.Empty<string>());
			}
			this.PlayerGroupsByUid[targetGroupid].JoinPolicy = policy;
			return this.Success(args, "Join policy {0} set.", new string[] { policy });
		}

		private TextCommandResult CmdInvitePlayer(TextCommandCallingArgs args)
		{
			IServerPlayer player = args.Caller.Player as IServerPlayer;
			int targetGroupid = args.Caller.FromChatGroupId;
			string playerName;
			if (!args.Parsers[1].IsMissing)
			{
				targetGroupid = this.GetgroupId(args[0] as string);
				playerName = args[1] as string;
			}
			else
			{
				playerName = args[0] as string;
			}
			if (targetGroupid <= 0)
			{
				return this.Error(args, "Invalid group name", Array.Empty<string>());
			}
			if (!this.HasPlayerPrivilege(player, targetGroupid, EnumPlayerGroupPrivilege.Invite, null))
			{
				return this.Error(args, "You must be the op or owner of the group to invite players.", Array.Empty<string>());
			}
			ConnectedClient invitedClient = this.server.GetClientByPlayername(playerName);
			if (invitedClient == null || invitedClient.Player == null)
			{
				return this.Error(args, "Can't invite. Player name {0} does not exist or is not online", new string[] { playerName });
			}
			if (!this.server.PlayerDataManager.PlayerDataByUid[invitedClient.ServerData.PlayerUID].AllowInvite)
			{
				return this.Error(args, "Can't invite. Player name {0} has disabled group invites", new string[] { playerName });
			}
			if (invitedClient.ServerData.PlayerGroupMemberShips.ContainsKey(targetGroupid))
			{
				return this.Error(args, "Can't invite. Player name {0} already in this player group!", new string[] { playerName });
			}
			this.InviteRequests[targetGroupid.ToString() + "-" + invitedClient.ServerData.PlayerUID] = invitedClient.ServerData.PlayerUID;
			string cmd = "/group ai " + this.PlayerGroupsByUid[targetGroupid].Uid.ToString();
			string msg = Lang.GetL(invitedClient.Player.LanguageCode, "playergroup-invitemsg", new object[]
			{
				player.PlayerName,
				this.PlayerGroupsByUid[targetGroupid].Name,
				cmd,
				cmd
			});
			invitedClient.Player.SendMessage(GlobalConstants.GeneralChatGroup, msg, EnumChatType.GroupInvite, null);
			return this.Success(args, "Player name {0} invited.", new string[] { playerName });
		}

		private TextCommandResult CmdAcceptInvite(TextCommandCallingArgs args)
		{
			IServerPlayer player = args.Caller.Player as IServerPlayer;
			string playerUid = player.PlayerUID;
			string groupName = args[0] as string;
			int targetGroupid;
			if (!int.TryParse(groupName, out targetGroupid))
			{
				PlayerGroup existingGroup = this.server.PlayerDataManager.GetPlayerGroupByName(groupName);
				if (existingGroup == null)
				{
					return this.Error(args, "Invalid param (not a number and no such group name exists), use /group help ai to see available params.", Array.Empty<string>());
				}
				targetGroupid = existingGroup.Uid;
			}
			if (this.InviteRequests.ContainsKey(targetGroupid.ToString() + "-" + playerUid))
			{
				PlayerGroup targetGroup;
				this.server.PlayerDataManager.PlayerGroupsById.TryGetValue(targetGroupid, out targetGroup);
				if (targetGroup == null)
				{
					return this.Error(args, "Player group no longer exists.", Array.Empty<string>());
				}
				ServerPlayerData plrData = ((ServerPlayer)player).serverdata;
				if (plrData.PlayerGroupMemberShips.ContainsKey(targetGroupid))
				{
					player.SendMessage(args.Caller.FromChatGroupId, Lang.GetL(player.LanguageCode, "Can't accept invite, you are already joined this player group", Array.Empty<object>()), EnumChatType.CommandError, null);
					goto IL_01DE;
				}
				PlayerGroupMembership membership = plrData.JoinGroup(targetGroup, EnumPlayerGroupMemberShip.Member);
				this.server.PlayerDataManager.playerDataDirty = true;
				this.PlayerGroupsByUid[targetGroupid].OnlinePlayers.Add(player);
				this.SendPlayerGroup(player, this.PlayerGroupsByUid[targetGroupid], membership);
				this.GotoGroup(player, targetGroupid);
				using (List<IPlayer>.Enumerator enumerator = this.PlayerGroupsByUid[targetGroupid].OnlinePlayers.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						IPlayer player2 = enumerator.Current;
						IServerPlayer memberPlayer = (IServerPlayer)player2;
						memberPlayer.SendMessage(targetGroupid, Lang.GetL(memberPlayer.LanguageCode, "Player {0} has joined the group.", new object[] { player.PlayerName }), EnumChatType.Notification, null);
					}
					goto IL_01DE;
				}
			}
			player.SendMessage(args.Caller.FromChatGroupId, Lang.GetL(player.LanguageCode, "No invite for this player group found.", Array.Empty<object>()), EnumChatType.CommandError, null);
			IL_01DE:
			return TextCommandResult.Success("", null);
		}

		private void GotoGroup(IServerPlayer player, int groupId)
		{
			this.server.SendPacket(player, new Packet_Server
			{
				Id = 57,
				GotoGroup = new Packet_GotoGroup
				{
					GroupId = groupId
				}
			});
		}

		private TextCommandResult CmdLeavegroup(TextCommandCallingArgs args)
		{
			int targetgroupid = args.Caller.FromChatGroupId;
			IServerPlayer player = args.Caller.Player as IServerPlayer;
			string groupName = args[0] as string;
			if (!args.Parsers[0].IsMissing)
			{
				targetgroupid = this.GetgroupId(groupName);
			}
			if (targetgroupid <= 0)
			{
				return this.Error(args, "Invalid group name", Array.Empty<string>());
			}
			ServerPlayerData plrData = ((ServerPlayer)player).serverdata;
			if (!plrData.PlayerGroupMemberShips.ContainsKey(targetgroupid))
			{
				return this.Error(args, "No such group membership found, perhaps you already left this group.", Array.Empty<string>());
			}
			if (this.PlayerGroupsByUid.ContainsKey(targetgroupid))
			{
				PlayerGroup targetGroup = this.PlayerGroupsByUid[targetgroupid];
				player.SendMessage((args.Caller.FromChatGroupId == targetgroupid) ? GlobalConstants.ServerInfoChatGroup : args.Caller.FromChatGroupId, Lang.GetL(player.LanguageCode, "You have left the group {0}", new object[] { targetGroup.Name }), EnumChatType.CommandSuccess, null);
				plrData.LeaveGroup(targetGroup);
				this.server.PlayerDataManager.playerDataDirty = true;
				this.SendPlayerGroups(player);
				targetGroup.OnlinePlayers.Remove(player);
				this.server.SendMessageToGroup(args.Caller.FromChatGroupId, Lang.Get("Player {0} has left the group.", new object[] { player.PlayerName }), EnumChatType.Notification, null, null);
			}
			else
			{
				plrData.LeaveGroup(targetgroupid);
				player.SendMessage((args.Caller.FromChatGroupId == targetgroupid) ? GlobalConstants.ServerInfoChatGroup : args.Caller.FromChatGroupId, Lang.GetL(player.LanguageCode, "You have left the group.", Array.Empty<object>()), EnumChatType.CommandSuccess, null);
			}
			return TextCommandResult.Success("", null);
		}

		private TextCommandResult CmdListgroups(TextCommandCallingArgs args)
		{
			IServerPlayer player = args.Caller.Player as IServerPlayer;
			Dictionary<int, PlayerGroupMembership> playerGroupMemberShips = ((ServerPlayer)player).serverdata.PlayerGroupMemberShips;
			player.SendMessage(args.Caller.FromChatGroupId, Lang.GetL(player.LanguageCode, "You are in the following groups: ", Array.Empty<object>()), EnumChatType.Notification, null);
			foreach (KeyValuePair<int, PlayerGroupMembership> val in playerGroupMemberShips)
			{
				string name = Lang.GetL(player.LanguageCode, "Disbanded group name {0}", new object[] { val.Value.GroupName });
				if (this.PlayerGroupsByUid.ContainsKey(val.Key))
				{
					name = this.PlayerGroupsByUid[val.Key].Name;
				}
				player.SendMessage(args.Caller.FromChatGroupId, name, EnumChatType.Notification, null);
			}
			return TextCommandResult.Success("", null);
		}

		private TextCommandResult CmdgroupInfo(TextCommandCallingArgs args)
		{
			PlayerGroup group;
			if (!args.Parsers[0].IsMissing)
			{
				group = this.server.PlayerDataManager.GetPlayerGroupByName(args[0] as string);
				if (group == null)
				{
					return this.Error(args, "No such group exists.", Array.Empty<string>());
				}
			}
			else
			{
				if (GlobalConstants.DefaultChatGroups.Contains(args.Caller.FromChatGroupId))
				{
					return this.Error(args, "This is a default group.", Array.Empty<string>());
				}
				if (!this.server.PlayerDataManager.PlayerGroupsById.TryGetValue(args.Caller.FromChatGroupId, out group))
				{
					return this.Error(args, "No such group exists.", Array.Empty<string>());
				}
			}
			StringBuilder sb = new StringBuilder();
			sb.AppendLine(Lang.GetL(args.LanguageCode, "Created: {0}", new object[] { group.CreatedDate }));
			sb.AppendLine(Lang.GetL(args.LanguageCode, "Created by: {0}", new object[] { this.server.PlayerDataManager.PlayerDataByUid[group.OwnerUID].LastKnownPlayername }));
			sb.Append(Lang.GetL(args.LanguageCode, "Members: ", Array.Empty<object>()));
			int i = 0;
			foreach (ServerPlayerData plrdata in this.server.PlayerDataManager.PlayerDataByUid.Values)
			{
				if (plrdata.PlayerGroupMemberships.ContainsKey(group.Uid))
				{
					if (i > 0)
					{
						sb.Append(", ");
					}
					i++;
					sb.Append(plrdata.LastKnownPlayername);
				}
			}
			sb.AppendLine();
			return TextCommandResult.Success(sb.ToString(), null);
		}

		private TextCommandResult CmdKickFromgroup(TextCommandCallingArgs args)
		{
			IServerPlayer player = args.Caller.Player as IServerPlayer;
			int targetgroupid = args.Caller.FromChatGroupId;
			string playerName;
			if (!args.Parsers[1].IsMissing)
			{
				targetgroupid = this.GetgroupId(args[0] as string);
				playerName = args[1] as string;
			}
			else
			{
				playerName = args[0] as string;
			}
			if (targetgroupid <= 0)
			{
				return this.Error(args, "Invalid group name", Array.Empty<string>());
			}
			PlayerGroup plrgroup = this.PlayerGroupsByUid[targetgroupid];
			foreach (ServerPlayerData plrdata in this.server.PlayerDataManager.PlayerDataByUid.Values)
			{
				if (string.Equals(playerName, plrdata.LastKnownPlayername, StringComparison.OrdinalIgnoreCase))
				{
					foreach (KeyValuePair<int, PlayerGroupMembership> val in plrdata.PlayerGroupMemberShips)
					{
						if (val.Key == targetgroupid)
						{
							if (!this.HasPlayerPrivilege(player, targetgroupid, EnumPlayerGroupPrivilege.Kick, plrdata.PlayerUID))
							{
								return this.Error(args, "You must be the op or owner to kick this player (and ops can only be kicked by owner).", Array.Empty<string>());
							}
							plrdata.LeaveGroup(plrgroup);
							this.server.PlayerDataManager.playerDataDirty = true;
							ServerPlayer targetPlr;
							if (this.server.PlayersByUid.TryGetValue(plrdata.PlayerUID, out targetPlr))
							{
								this.PlayerGroupsByUid[targetgroupid].OnlinePlayers.Remove(targetPlr);
							}
							this.server.SendMessageToGroup(args.Caller.FromChatGroupId, Lang.GetL(args.LanguageCode, "Player {0} has been removed from the player group.", new object[] { plrdata.LastKnownPlayername }), EnumChatType.CommandSuccess, null, null);
							foreach (ConnectedClient client in this.server.Clients.Values)
							{
								if (client.WorldData.PlayerUID == plrdata.PlayerUID && client.Player != null)
								{
									this.SendPlayerGroups(client.Player);
									client.Player.SendMessage(GlobalConstants.InfoLogChatGroup, Lang.GetL(client.Player.LanguageCode, "You've been kicked from player group {0}.", new object[] { plrgroup.Name }), EnumChatType.Notification, null);
									break;
								}
							}
							return TextCommandResult.Success("", null);
						}
					}
					return this.Error(args, "This player is not in this group.", Array.Empty<string>());
				}
			}
			return this.Success(args, "No such player name found", Array.Empty<string>());
		}

		private TextCommandResult CmdOpPlayer(TextCommandCallingArgs args, bool deop)
		{
			IServerPlayer player = args.Caller.Player as IServerPlayer;
			int targetgroupid = args.Caller.FromChatGroupId;
			string playerName;
			if (!args.Parsers[1].IsMissing)
			{
				targetgroupid = this.GetgroupId(args[0] as string);
				playerName = args[1] as string;
			}
			else
			{
				playerName = args[0] as string;
			}
			if (targetgroupid <= 0)
			{
				return this.Error(args, "Invalid group name", Array.Empty<string>());
			}
			if (!this.HasPlayerPrivilege(player, targetgroupid, EnumPlayerGroupPrivilege.Op, null))
			{
				return this.Error(args, "You must be the owner to op/deop players", Array.Empty<string>());
			}
			PlayerGroup plrgroup = this.PlayerGroupsByUid[targetgroupid];
			foreach (ServerPlayerData plrdata in this.server.PlayerDataManager.PlayerDataByUid.Values)
			{
				if (string.Equals(playerName, plrdata.LastKnownPlayername, StringComparison.OrdinalIgnoreCase))
				{
					EnumPlayerGroupMemberShip membership = this.GetGroupMemberShip(plrdata.PlayerUID, targetgroupid).Level;
					if (membership == EnumPlayerGroupMemberShip.None)
					{
						return this.Error(args, "This player is not in this group, invite him first.", Array.Empty<string>());
					}
					if (!deop && (membership == EnumPlayerGroupMemberShip.Op || membership == EnumPlayerGroupMemberShip.Owner))
					{
						return this.Error(args, "This player is already op in this channel.", Array.Empty<string>());
					}
					if (deop && membership != EnumPlayerGroupMemberShip.Op)
					{
						return this.Error(args, "This player is no op in this channel.", Array.Empty<string>());
					}
					plrdata.PlayerGroupMemberShips[targetgroupid].Level = (deop ? EnumPlayerGroupMemberShip.Member : EnumPlayerGroupMemberShip.Op);
					this.server.PlayerDataManager.playerDataDirty = true;
					using (List<IPlayer>.Enumerator enumerator2 = plrgroup.OnlinePlayers.GetEnumerator())
					{
						while (enumerator2.MoveNext())
						{
							IPlayer player2 = enumerator2.Current;
							ServerPlayer memberPlayer = (ServerPlayer)player2;
							if (memberPlayer.WorldData.PlayerUID == plrdata.PlayerUID)
							{
								string msg = Lang.GetL(memberPlayer.LanguageCode, "{0} has given you op status. You can now invite and kick group members.", new object[] { player.PlayerName });
								if (deop)
								{
									msg = Lang.GetL(memberPlayer.LanguageCode, "{0} has removed your op status. You can no longer invite or kick members", new object[] { player.PlayerName });
								}
								memberPlayer.SendMessage(targetgroupid, msg, EnumChatType.Notification, null);
							}
							else
							{
								string msg2 = Lang.GetL(memberPlayer.LanguageCode, deop ? "Player {0} has been deopped." : "Player {0} has been opped.", new object[] { plrdata.LastKnownPlayername });
								memberPlayer.SendMessage((memberPlayer.ClientId == player.ClientId && args.Caller.FromChatGroupId != targetgroupid) ? args.Caller.FromChatGroupId : targetgroupid, msg2, EnumChatType.CommandSuccess, null);
							}
						}
						break;
					}
				}
			}
			return TextCommandResult.Success("", null);
		}

		private TextCommandResult CmdNoInvite(TextCommandCallingArgs args)
		{
			string playerUid = args.Caller.Player.PlayerUID;
			this.server.PlayerDataManager.PlayerDataByUid[playerUid].AllowInvite = (bool)args[0];
			return this.Success(args, this.server.PlayerDataManager.PlayerDataByUid[playerUid].AllowInvite ? "Ok, Group invites are now enabled" : "Ok, Group invites are now disabled", Array.Empty<string>());
		}

		public void SendPlayerGroups(IServerPlayer player)
		{
			List<Packet_PlayerGroup> packets = new List<Packet_PlayerGroup>();
			foreach (KeyValuePair<int, PlayerGroupMembership> val in ((ServerPlayer)player).serverdata.PlayerGroupMemberShips)
			{
				if (val.Value.Level != EnumPlayerGroupMemberShip.None)
				{
					PlayerGroup plrgroup;
					this.server.PlayerDataManager.PlayerGroupsById.TryGetValue(val.Key, out plrgroup);
					if (plrgroup != null)
					{
						packets.Add(this.GetPlayerGroupPacket(plrgroup, val.Value));
					}
				}
			}
			Packet_PlayerGroups packet = new Packet_PlayerGroups();
			packet.SetGroups(packets.ToArray());
			this.server.SendPacket(player, new Packet_Server
			{
				Id = 49,
				PlayerGroups = packet
			});
		}

		public void SendPlayerGroup(IServerPlayer player, PlayerGroup playergroup)
		{
			PlayerGroupMembership membership;
			((ServerPlayer)player).serverdata.PlayerGroupMemberShips.TryGetValue(playergroup.Uid, out membership);
			if (membership == null)
			{
				return;
			}
			if (membership.Level == EnumPlayerGroupMemberShip.None)
			{
				return;
			}
			this.server.SendPacket(player, new Packet_Server
			{
				Id = 50,
				PlayerGroup = this.GetPlayerGroupPacket(playergroup, membership)
			});
		}

		public void SendPlayerGroup(IServerPlayer player, PlayerGroup playergroup, PlayerGroupMembership membership)
		{
			if (membership.Level == EnumPlayerGroupMemberShip.None)
			{
				return;
			}
			this.server.SendPacket(player, new Packet_Server
			{
				Id = 50,
				PlayerGroup = this.GetPlayerGroupPacket(playergroup, membership)
			});
		}

		private Packet_PlayerGroup GetPlayerGroupPacket(PlayerGroup plrgroup, PlayerGroupMembership membership)
		{
			Packet_PlayerGroup pg = new Packet_PlayerGroup
			{
				Membership = (int)membership.Level,
				Name = plrgroup.Name,
				Owneruid = plrgroup.OwnerUID,
				Uid = plrgroup.Uid
			};
			List<Packet_ChatLine> chatlines = new List<Packet_ChatLine>();
			foreach (ChatLine chatline in plrgroup.ChatHistory)
			{
				chatlines.Add(new Packet_ChatLine
				{
					ChatType = (int)chatline.ChatType,
					Groupid = plrgroup.Uid,
					Message = chatline.Message
				});
			}
			pg.SetChathistory(chatlines.ToArray());
			return pg;
		}

		public bool HasPlayerPrivilege(IServerPlayer player, int targetGroupid, EnumPlayerGroupPrivilege priv, string targetPlayerUid = null)
		{
			EnumPlayerGroupMemberShip level = this.GetGroupMemberShip(player, targetGroupid).Level;
			switch (priv)
			{
			case EnumPlayerGroupPrivilege.Invite:
				return level == EnumPlayerGroupMemberShip.Op || level == EnumPlayerGroupMemberShip.Owner;
			case EnumPlayerGroupPrivilege.Kick:
				return (level == EnumPlayerGroupMemberShip.Op && this.GetGroupMemberShip(targetPlayerUid, targetGroupid).Level == EnumPlayerGroupMemberShip.Member) || level == EnumPlayerGroupMemberShip.Owner;
			case EnumPlayerGroupPrivilege.Disband:
				return level == EnumPlayerGroupMemberShip.Owner;
			case EnumPlayerGroupPrivilege.Op:
				return level == EnumPlayerGroupMemberShip.Owner;
			case EnumPlayerGroupPrivilege.Rename:
				return level == EnumPlayerGroupMemberShip.Owner;
			default:
				return false;
			}
		}

		public PlayerGroupMembership GetGroupMemberShip(IServerPlayer player, int targetGroupid)
		{
			return this.GetGroupMemberShip(player.PlayerUID, targetGroupid);
		}

		public PlayerGroupMembership GetGroupMemberShip(string playerUID, int targetGroupid)
		{
			ServerPlayerData plrData = this.server.PlayerDataManager.PlayerDataByUid[playerUID];
			if (!plrData.PlayerGroupMemberShips.ContainsKey(targetGroupid))
			{
				return new PlayerGroupMembership
				{
					Level = EnumPlayerGroupMemberShip.None
				};
			}
			return plrData.PlayerGroupMemberShips[targetGroupid];
		}

		public Dictionary<int, string> DisbandRequests = new Dictionary<int, string>();

		public Dictionary<string, string> InviteRequests = new Dictionary<string, string>();
	}
}
