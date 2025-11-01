using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace Vintagestory.Server
{
	[JsonObject(MemberSerialization.OptIn)]
	public class ServerPlayerData : IComparable<ServerPlayerData>, IServerPlayerData
	{
		[JsonProperty]
		public string FirstJoinDate { get; set; }

		[JsonProperty]
		public string LastJoinDate { get; set; }

		[JsonProperty]
		public string LastCharacterSelectionDate { get; set; }

		public Dictionary<int, PlayerGroupMembership> PlayerGroupMemberships
		{
			get
			{
				return this.PlayerGroupMemberShips;
			}
		}

		Dictionary<string, string> IServerPlayerData.CustomPlayerData
		{
			get
			{
				return this.CustomPlayerData;
			}
		}

		int IServerPlayerData.ExtraLandClaimAllowance
		{
			get
			{
				return this.ExtraLandClaimAllowance;
			}
			set
			{
				this.ExtraLandClaimAllowance = value;
			}
		}

		int IServerPlayerData.ExtraLandClaimAreas
		{
			get
			{
				return this.ExtraLandClaimAreas;
			}
			set
			{
				this.ExtraLandClaimAreas = value;
			}
		}

		string IServerPlayerData.PlayerUID
		{
			get
			{
				return this.PlayerUID;
			}
		}

		string IServerPlayerData.RoleCode
		{
			get
			{
				return this.RoleCode;
			}
		}

		HashSet<string> IServerPlayerData.PermaPrivileges
		{
			get
			{
				return this.PermaPrivileges;
			}
		}

		HashSet<string> IServerPlayerData.DeniedPrivileges
		{
			get
			{
				return this.DeniedPrivileges;
			}
		}

		Dictionary<int, PlayerGroupMembership> IServerPlayerData.PlayerGroupMemberships
		{
			get
			{
				return this.PlayerGroupMemberShips;
			}
		}

		bool IServerPlayerData.AllowInvite
		{
			get
			{
				return this.AllowInvite;
			}
		}

		string IServerPlayerData.LastKnownPlayername
		{
			get
			{
				return this.LastKnownPlayername;
			}
		}

		public ServerPlayerData()
		{
			this.PlayerUID = "";
			this.RoleCode = "";
			this.PermaPrivileges = new HashSet<string>();
			this.DeniedPrivileges = new HashSet<string>();
			this.RuntimePrivileges = new HashSet<string>();
			this.PlayerGroupMemberShips = new Dictionary<int, PlayerGroupMembership>();
			this.CustomPlayerData = new Dictionary<string, string>();
		}

		public override string ToString()
		{
			return string.Format("{0}:{1}", this.PlayerUID, this.RoleCode);
		}

		public int CompareTo(ServerPlayerData other)
		{
			return this.RoleCode.CompareOrdinal(other.RoleCode);
		}

		public void SetRole(PlayerRole newGroup)
		{
			this.RoleCode = newGroup.Code;
		}

		public void GrantPrivilege(string privilege)
		{
			this.PermaPrivileges.Add(privilege);
			this.DeniedPrivileges.Remove(privilege);
		}

		public void DenyPrivilege(string privilege)
		{
			this.PermaPrivileges.Remove(privilege);
			this.DeniedPrivileges.Add(privilege);
		}

		public void RevokePrivilege(string privilege)
		{
			this.PermaPrivileges.Remove(privilege);
			this.DeniedPrivileges.Add(privilege);
		}

		internal void RemovePrivilegeDenial(string code)
		{
			this.DeniedPrivileges.Remove(code);
		}

		public PlayerGroupMembership JoinGroup(PlayerGroup group, EnumPlayerGroupMemberShip level)
		{
			Dictionary<int, PlayerGroupMembership> playerGroupMemberShips = this.PlayerGroupMemberShips;
			int uid = group.Uid;
			PlayerGroupMembership playerGroupMembership = new PlayerGroupMembership();
			playerGroupMembership.GroupName = group.Name;
			playerGroupMembership.GroupUid = group.Uid;
			playerGroupMembership.Level = level;
			PlayerGroupMembership playerGroupMembership2 = playerGroupMembership;
			playerGroupMemberShips[uid] = playerGroupMembership;
			return playerGroupMembership2;
		}

		public HashSet<string> GetAllPrivilegeCodes(ServerConfig serverConfig)
		{
			HashSet<string> codes = new HashSet<string>();
			codes.AddRange(this.PermaPrivileges);
			PlayerRole role;
			if (serverConfig.RolesByCode.TryGetValue(this.RoleCode, out role))
			{
				codes.AddRange(role.Privileges);
				codes.AddRange(role.RuntimePrivileges);
			}
			foreach (string val in this.DeniedPrivileges)
			{
				codes.Remove(val);
			}
			codes.AddRange(this.RuntimePrivileges);
			return codes;
		}

		public void LeaveGroup(PlayerGroup group)
		{
			this.PlayerGroupMemberShips.Remove(group.Uid);
		}

		public void LeaveGroup(int groupid)
		{
			this.PlayerGroupMemberShips.Remove(groupid);
		}

		public bool HasPrivilege(string privilege, Dictionary<string, PlayerRole> rolesByCode)
		{
			if (privilege == null)
			{
				return true;
			}
			if (this.RuntimePrivileges.Contains(privilege))
			{
				return true;
			}
			if (this.DeniedPrivileges.Contains(privilege))
			{
				return false;
			}
			if (this.PermaPrivileges.Contains(privilege))
			{
				return true;
			}
			PlayerRole role;
			rolesByCode.TryGetValue(this.RoleCode, out role);
			return role != null && (role.Privileges.Contains(privilege) || role.RuntimePrivileges.Contains(privilege));
		}

		public PlayerRole GetPlayerRole(ServerMain server)
		{
			PlayerRole role;
			server.Config.RolesByCode.TryGetValue(this.RoleCode, out role);
			if (role == null)
			{
				ServerMain.Logger.Warning(string.Concat(new string[] { "Player ", this.LastKnownPlayername, " has role ", this.RoleCode, " but no such role exists! Assigning to default group" }));
				this.RoleCode = server.Config.DefaultRoleCode;
				return server.Config.DefaultRole;
			}
			return role;
		}

		[JsonProperty]
		public string PlayerUID;

		[JsonProperty]
		public string RoleCode;

		[JsonProperty]
		internal HashSet<string> PermaPrivileges;

		[JsonProperty]
		internal HashSet<string> DeniedPrivileges;

		[JsonProperty]
		public Dictionary<int, PlayerGroupMembership> PlayerGroupMemberShips;

		[JsonProperty]
		public bool AllowInvite;

		[JsonProperty]
		public string LastKnownPlayername;

		[JsonProperty]
		public Dictionary<string, string> CustomPlayerData;

		[JsonProperty]
		public int ExtraLandClaimAllowance;

		[JsonProperty]
		public int ExtraLandClaimAreas;

		internal HashSet<string> RuntimePrivileges;
	}
}
