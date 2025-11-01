using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace Vintagestory.Server
{
	public class PlayerDataManager : IPermissionManager, IGroupManager, IPlayerDataManager
	{
		Dictionary<int, PlayerGroup> IGroupManager.PlayerGroupsById
		{
			get
			{
				return this.PlayerGroupsById;
			}
		}

		Dictionary<string, IServerPlayerData> IPlayerDataManager.PlayerDataByUid
		{
			get
			{
				Dictionary<string, IServerPlayerData> dict = new Dictionary<string, IServerPlayerData>();
				foreach (KeyValuePair<string, ServerPlayerData> val in this.PlayerDataByUid)
				{
					dict[val.Key] = val.Value;
				}
				return dict;
			}
		}

		public PlayerDataManager(ServerMain server)
		{
			this.server = server;
			server.RegisterGameTickListener(new Action<float>(this.OnCheckRequireSave), 1000, 0);
			server.EventManager.OnGameWorldBeingSaved += this.OnGameWorldBeingSaved;
			server.EventManager.OnPlayerJoin += this.EventManager_OnPlayerJoin;
		}

		private void EventManager_OnPlayerJoin(IServerPlayer byPlayer)
		{
			ServerPlayerData plrdata = this.GetOrCreateServerPlayerData(byPlayer.PlayerUID, byPlayer.PlayerName);
			plrdata.LastJoinDate = DateTime.UtcNow.ToShortDateString() + " " + DateTime.UtcNow.ToShortTimeString();
			if (plrdata.FirstJoinDate == null)
			{
				plrdata.FirstJoinDate = DateTime.UtcNow.ToShortDateString() + " " + DateTime.UtcNow.ToShortTimeString();
			}
		}

		private void OnGameWorldBeingSaved()
		{
			this.playerDataDirty = true;
			this.playerGroupsDirty = true;
			this.bannedListDirty = true;
			this.whiteListDirty = true;
			this.OnCheckRequireSave(0f);
		}

		private void OnCheckRequireSave(float dt)
		{
			if (this.playerDataDirty)
			{
				try
				{
					using (TextWriter textWriter = new StreamWriter(Path.Combine(GamePaths.PlayerData, "playerdata.json")))
					{
						textWriter.Write(JsonConvert.SerializeObject(this.PlayerDataByUid.Values.ToList<ServerPlayerData>(), Formatting.Indented));
						textWriter.Close();
					}
					this.playerDataDirty = false;
				}
				catch (Exception e)
				{
					ServerMain.Logger.Warning("Failed saving player data, will try again. {0}", new object[] { e.Message });
				}
			}
			if (this.playerGroupsDirty)
			{
				try
				{
					using (TextWriter textWriter2 = new StreamWriter(Path.Combine(GamePaths.PlayerData, "playergroups.json")))
					{
						textWriter2.Write(JsonConvert.SerializeObject(this.PlayerGroupsById.Values.ToList<PlayerGroup>(), Formatting.Indented));
						textWriter2.Close();
					}
					this.playerGroupsDirty = false;
				}
				catch (Exception e2)
				{
					ServerMain.Logger.Warning("Failed saving player group data, will try again. {0}", new object[] { e2.Message });
				}
			}
			if (this.bannedListDirty)
			{
				try
				{
					using (TextWriter textWriter3 = new StreamWriter(Path.Combine(GamePaths.PlayerData, "playersbanned.json")))
					{
						textWriter3.Write(JsonConvert.SerializeObject(this.BannedPlayers, Formatting.Indented));
						textWriter3.Close();
					}
					this.bannedListDirty = false;
				}
				catch (Exception e3)
				{
					ServerMain.Logger.Warning("Failed saving player banned list, will try again. {0}", new object[] { e3.Message });
				}
			}
			if (this.whiteListDirty)
			{
				try
				{
					using (TextWriter textWriter4 = new StreamWriter(Path.Combine(GamePaths.PlayerData, "playerswhitelisted.json")))
					{
						textWriter4.Write(JsonConvert.SerializeObject(this.WhitelistedPlayers, Formatting.Indented));
						textWriter4.Close();
					}
					this.whiteListDirty = false;
				}
				catch (Exception e4)
				{
					ServerMain.Logger.Warning("Failed saving player whitelist, will try again. {0}", new object[] { e4.Message });
				}
			}
		}

		private List<T> LoadList<T>(string name)
		{
			List<T> elems = null;
			try
			{
				string filepath = Path.Combine(GamePaths.PlayerData, name);
				if (File.Exists(filepath))
				{
					using (TextReader textReader = new StreamReader(filepath))
					{
						elems = JsonConvert.DeserializeObject<List<T>>(textReader.ReadToEnd());
						textReader.Close();
					}
				}
				if (elems == null)
				{
					elems = new List<T>();
				}
			}
			catch (Exception e)
			{
				ServerMain.Logger.Error("Failed reading file " + name + ". Will stop server now.");
				ServerMain.Logger.Error(e);
				this.server.Stop("Failed reading playerdata", null, EnumLogType.Notification);
			}
			return elems;
		}

		public void Load()
		{
			this.PlayerGroupsById = new Dictionary<int, PlayerGroup>();
			this.PlayerDataByUid = new Dictionary<string, ServerPlayerData>();
			this.BannedPlayers = new List<PlayerEntry>();
			this.WhitelistedPlayers = new List<PlayerEntry>();
			List<ServerPlayerData> list = this.LoadList<ServerPlayerData>("playerdata.json");
			List<PlayerGroup> PlayerGroups = this.LoadList<PlayerGroup>("playergroups.json");
			List<PlayerEntry> PlayerBans = this.LoadList<PlayerEntry>("playersbanned.json");
			List<PlayerEntry> PlayerWhitelist = this.LoadList<PlayerEntry>("playerswhitelisted.json");
			foreach (ServerPlayerData plrdata in list)
			{
				this.PlayerDataByUid[plrdata.PlayerUID] = plrdata;
			}
			foreach (PlayerGroup group in PlayerGroups)
			{
				this.PlayerGroupsById[group.Uid] = group;
			}
			foreach (PlayerEntry ban in PlayerBans)
			{
				if (ban.UntilDate >= DateTime.Now)
				{
					this.BannedPlayers.Add(ban);
				}
				else
				{
					this.bannedListDirty = true;
				}
			}
			foreach (PlayerEntry whitelist in PlayerWhitelist)
			{
				this.WhitelistedPlayers.Add(whitelist);
			}
		}

		public PlayerGroup PlayerGroupForPrivateMessage(ConnectedClient sender, ConnectedClient receiver)
		{
			string md5 = GameMath.Md5Hash(sender.ServerData.PlayerUID + "-" + receiver.ServerData.PlayerUID);
			foreach (PlayerGroup group2 in this.PlayerGroupsById.Values)
			{
				if (group2.Md5Identifier == md5)
				{
					return group2;
				}
			}
			PlayerGroup group3 = new PlayerGroup
			{
				OwnerUID = receiver.ServerData.PlayerUID,
				CreatedDate = DateTime.Today.ToLongDateString(),
				Md5Identifier = md5,
				Name = "PM from " + sender.PlayerName + " to " + receiver.PlayerName,
				CreatedByPrivateMessage = true
			};
			this.AddPlayerGroup(group3);
			return group3;
		}

		public bool CanCreatePlayerGroup(string playerUid)
		{
			ServerPlayerData plrdata = this.GetOrCreateServerPlayerData(playerUid, null);
			if (plrdata == null)
			{
				return false;
			}
			if (!plrdata.HasPrivilege(Privilege.manageplayergroups, this.server.Config.RolesByCode))
			{
				return false;
			}
			int channels = 0;
			using (Dictionary<int, PlayerGroupMembership>.ValueCollection.Enumerator enumerator = plrdata.PlayerGroupMemberShips.Values.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current.Level == EnumPlayerGroupMemberShip.Owner)
					{
						channels++;
					}
				}
			}
			return channels < this.server.Config.MaxOwnedGroupChannelsPerUser;
		}

		public PlayerGroup GetPlayerGroupByName(string name)
		{
			foreach (PlayerGroup group in this.PlayerGroupsById.Values)
			{
				if (group.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase))
				{
					return group;
				}
			}
			return null;
		}

		public void AddPlayerGroup(PlayerGroup group)
		{
			int maxgid = 0;
			foreach (int num in this.PlayerGroupsById.Keys)
			{
				maxgid = Math.Max(num, maxgid);
			}
			if (maxgid >= this.server.Config.NextPlayerGroupUid)
			{
				this.server.Config.NextPlayerGroupUid = maxgid + 1;
				this.server.ConfigNeedsSaving = true;
			}
			ServerConfig config = this.server.Config;
			int nextPlayerGroupUid = config.NextPlayerGroupUid;
			config.NextPlayerGroupUid = nextPlayerGroupUid + 1;
			group.Uid = nextPlayerGroupUid;
			this.server.ConfigNeedsSaving = true;
			this.PlayerGroupsById[group.Uid] = group;
		}

		public void RemovePlayerGroup(PlayerGroup group)
		{
			this.PlayerGroupsById.Remove(group.Uid);
		}

		public ServerPlayerData GetOrCreateServerPlayerData(string playerUID, string playerName = null)
		{
			ServerPlayerData plrdata;
			this.PlayerDataByUid.TryGetValue(playerUID, out plrdata);
			string defaultRoleCode = this.server.Config.DefaultRole.Code;
			if (plrdata == null)
			{
				plrdata = new ServerPlayerData
				{
					AllowInvite = true,
					PlayerUID = playerUID,
					RoleCode = defaultRoleCode,
					LastKnownPlayername = playerName
				};
				this.PlayerDataByUid[playerUID] = plrdata;
				this.playerDataDirty = true;
			}
			ConnectedClient clientByUID = this.server.GetClientByUID(playerUID);
			if (clientByUID != null && clientByUID.IsSinglePlayerClient)
			{
				plrdata.RoleCode = this.server.Config.Roles.MaxBy((PlayerRole v) => v.PrivilegeLevel).Code;
			}
			return plrdata;
		}

		public ServerPlayerData GetServerPlayerDataByLastKnownPlayername(string playername)
		{
			foreach (ServerPlayerData plrdata in this.PlayerDataByUid.Values)
			{
				if (plrdata.LastKnownPlayername != null && plrdata.LastKnownPlayername.Equals(playername, StringComparison.InvariantCultureIgnoreCase))
				{
					return plrdata;
				}
			}
			return null;
		}

		internal void BanPlayer(string playername, string playeruid, string byPlayerName, string reason = "", DateTime? untildate = null)
		{
			PlayerEntry entry = this.GetPlayerBan(playername, playeruid);
			if (entry == null)
			{
				this.BannedPlayers.Add(new PlayerEntry
				{
					PlayerName = playername,
					IssuedByPlayerName = byPlayerName,
					PlayerUID = playeruid,
					Reason = reason,
					UntilDate = untildate
				});
				ServerMain.Logger.Audit("{0} was banned by {1} until {2}. Reason: {3}", new object[] { playername, byPlayerName, untildate, reason });
			}
			else
			{
				entry.Reason = reason;
				entry.UntilDate = untildate;
				ServerMain.Logger.Audit("Existing player ban of {0} updated by {1}. Now until {2}, Reason: {3}", new object[] { playername, byPlayerName, untildate, reason });
			}
			this.bannedListDirty = true;
		}

		internal bool UnbanPlayer(string playername, string playeruid, string issuingPlayerName)
		{
			PlayerEntry entry = this.GetPlayerBan(playername, playeruid);
			if (entry != null)
			{
				this.BannedPlayers.Remove(entry);
				this.bannedListDirty = true;
				ServerMain.Logger.Audit("{0} was unbanned by {1}.", new object[] { playername, issuingPlayerName });
				return true;
			}
			return false;
		}

		public bool UnWhitelistPlayer(string playername, string playeruid)
		{
			PlayerEntry entry = this.GetPlayerWhitelist(playername, playeruid);
			if (entry != null)
			{
				this.WhitelistedPlayers.Remove(entry);
				this.whiteListDirty = true;
				return true;
			}
			return false;
		}

		public void WhitelistPlayer(string playername, string playeruid, string byPlayername, string reason = "", DateTime? untildate = null)
		{
			PlayerEntry entry = this.GetPlayerWhitelist(playername, playeruid);
			if (entry == null)
			{
				this.WhitelistedPlayers.Add(new PlayerEntry
				{
					PlayerName = playername,
					IssuedByPlayerName = byPlayername,
					Reason = reason,
					UntilDate = untildate,
					PlayerUID = playeruid
				});
			}
			else
			{
				entry.Reason = reason;
				entry.UntilDate = untildate;
			}
			this.whiteListDirty = true;
		}

		public PlayerEntry GetPlayerBan(string playername, string playeruid)
		{
			PlayerEntry entry = this.GetPlayerEntry(this.BannedPlayers, playeruid, playername);
			if (entry == null)
			{
				return null;
			}
			if (playeruid != null && playeruid != entry.PlayerUID)
			{
				entry.PlayerUID = playeruid;
				this.bannedListDirty = true;
			}
			return entry;
		}

		public PlayerEntry GetPlayerWhitelist(string playername, string playeruid)
		{
			PlayerEntry entry = this.GetPlayerEntry(this.WhitelistedPlayers, playeruid, playername);
			if (entry == null)
			{
				return null;
			}
			if (playeruid != null && playeruid != entry.PlayerUID)
			{
				entry.PlayerUID = playeruid;
				this.whiteListDirty = true;
			}
			return entry;
		}

		private PlayerEntry GetPlayerEntry(List<PlayerEntry> list, string playeruid, string playername)
		{
			foreach (PlayerEntry entry in list)
			{
				if (entry.PlayerUID == null || playeruid == null)
				{
					string playerName = entry.PlayerName;
					if (((playerName != null) ? playerName.ToLowerInvariant() : null) == ((playername != null) ? playername.ToLowerInvariant() : null))
					{
						return entry;
					}
				}
				else if (entry.PlayerUID == playeruid)
				{
					return entry;
				}
			}
			return null;
		}

		public void SetRole(IServerPlayer player, IPlayerRole role)
		{
			if (!this.server.Config.RolesByCode.ContainsKey(role.Code))
			{
				throw new ArgumentException("No such role configured '" + role.Code + "'");
			}
			this.GetOrCreateServerPlayerData(player.PlayerUID, null).SetRole(role as PlayerRole);
		}

		public void SetRole(IServerPlayer player, string roleCode)
		{
			if (!this.server.Config.RolesByCode.ContainsKey(roleCode))
			{
				throw new ArgumentException("No such role configured '" + roleCode + "'");
			}
			this.GetOrCreateServerPlayerData(player.PlayerUID, null).SetRole(this.server.Config.RolesByCode[roleCode]);
		}

		public IPlayerRole GetRole(string code)
		{
			return this.server.Config.RolesByCode[code];
		}

		public void RegisterPrivilege(string code, string shortdescription, bool adminAutoGrant = true)
		{
			this.server.AllPrivileges.Add(code);
			this.server.PrivilegeDescriptions[code] = shortdescription;
			if (adminAutoGrant)
			{
				foreach (PlayerRole role in this.server.Config.RolesByCode.Values)
				{
					if (role.AutoGrant)
					{
						role.GrantPrivilege(new string[] { code });
					}
				}
			}
		}

		public void GrantTemporaryPrivilege(string code)
		{
			this.server.Config.RuntimePrivileveCodes.Add(code);
		}

		public void DropTemporaryPrivilege(string code)
		{
			this.server.Config.RuntimePrivileveCodes.Remove(code);
		}

		public bool GrantPrivilege(string playerUID, string code, bool permanent = false)
		{
			ServerPlayerData plrdata = this.GetOrCreateServerPlayerData(playerUID, null);
			if (plrdata == null)
			{
				return false;
			}
			if (permanent)
			{
				plrdata.GrantPrivilege(code);
			}
			else
			{
				plrdata.RuntimePrivileges.Add(code);
			}
			return true;
		}

		public bool DenyPrivilege(string playerUID, string code)
		{
			ServerPlayerData plrdata = this.GetOrCreateServerPlayerData(playerUID, null);
			if (plrdata == null)
			{
				return false;
			}
			plrdata.DenyPrivilege(code);
			return true;
		}

		public bool RemovePrivilegeDenial(string playerUID, string code)
		{
			ServerPlayerData plrdata = this.GetOrCreateServerPlayerData(playerUID, null);
			if (plrdata == null)
			{
				return false;
			}
			plrdata.RemovePrivilegeDenial(code);
			return true;
		}

		public bool RevokePrivilege(string playerUID, string code, bool permanent = false)
		{
			ServerPlayerData plrdata = this.GetOrCreateServerPlayerData(playerUID, null);
			if (plrdata == null)
			{
				return false;
			}
			if (permanent)
			{
				plrdata.RevokePrivilege(code);
			}
			else
			{
				plrdata.RuntimePrivileges.Remove(code);
			}
			return true;
		}

		public bool AddPrivilegeToGroup(string groupCode, string privilegeCode)
		{
			PlayerRole group;
			this.server.Config.RolesByCode.TryGetValue(groupCode, out group);
			if (group == null)
			{
				return false;
			}
			group.RuntimePrivileges.Add(privilegeCode);
			return true;
		}

		public bool RemovePrivilegeFromGroup(string groupCode, string privilegeCode)
		{
			PlayerRole group;
			this.server.Config.RolesByCode.TryGetValue(groupCode, out group);
			if (group == null)
			{
				return false;
			}
			group.RuntimePrivileges.Remove(privilegeCode);
			return true;
		}

		public int GetPlayerPermissionLevel(int player)
		{
			return this.server.Clients[player].ServerData.GetPlayerRole(this.server).PrivilegeLevel;
		}

		public IServerPlayerData GetPlayerDataByUid(string playerUid)
		{
			ServerPlayerData plrdata;
			this.PlayerDataByUid.TryGetValue(playerUid, out plrdata);
			return plrdata;
		}

		public IServerPlayerData GetPlayerDataByLastKnownName(string name)
		{
			return this.GetServerPlayerDataByLastKnownPlayername(name);
		}

		public void ResolvePlayerName(string playername, Action<EnumServerResponse, string> onPlayerReceived)
		{
			this.server.GetOnlineOrOfflinePlayer(playername, onPlayerReceived);
		}

		public void ResolvePlayerUid(string playeruid, Action<EnumServerResponse, string> onPlayerReceived)
		{
			this.server.GetOnlineOrOfflinePlayerByUid(playeruid, onPlayerReceived);
		}

		private ServerMain server;

		public Dictionary<string, ServerWorldPlayerData> WorldDataByUID = new Dictionary<string, ServerWorldPlayerData>();

		public Dictionary<int, PlayerGroup> PlayerGroupsById;

		public Dictionary<string, ServerPlayerData> PlayerDataByUid;

		public List<PlayerEntry> BannedPlayers;

		public List<PlayerEntry> WhitelistedPlayers;

		public bool playerDataDirty;

		public bool playerGroupsDirty;

		public bool bannedListDirty;

		public bool whiteListDirty;
	}
}
