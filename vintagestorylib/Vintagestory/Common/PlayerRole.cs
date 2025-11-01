using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.Serialization;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common
{
	public class PlayerRole : IPlayerRole, IComparable<IPlayerRole>
	{
		public string Code { get; set; }

		public int PrivilegeLevel { get; set; }

		public string Name { get; set; }

		public string Description { get; set; }

		public PlayerSpawnPos DefaultSpawn { get; set; }

		public PlayerSpawnPos ForcedSpawn { get; set; }

		public List<string> Privileges { get; set; }

		public HashSet<string> RuntimePrivileges { get; set; }

		public EnumGameMode DefaultGameMode { get; set; }

		public Color Color { get; set; }

		public int LandClaimAllowance { get; set; }

		public Vec3i LandClaimMinSize { get; set; } = new Vec3i(5, 5, 5);

		public int LandClaimMaxAreas { get; set; } = 3;

		public bool AutoGrant { get; set; }

		public PlayerRole()
		{
			this.PrivilegeLevel = 0;
			this.Privileges = new List<string>();
			this.RuntimePrivileges = new HashSet<string>();
			this.Color = Color.White;
		}

		[OnDeserialized]
		internal void OnDeserializedMethod(StreamingContext context)
		{
			if (this.Privileges == null)
			{
				this.Privileges = new List<string>();
			}
			if (this.RuntimePrivileges == null)
			{
				this.RuntimePrivileges = new HashSet<string>();
			}
		}

		public override string ToString()
		{
			return string.Format("{0}:{1}:{2}:{3}:{4}", new object[]
			{
				this.Code,
				this.Name,
				this.PrivilegeLevel,
				PlayerRole.PrivilegesString(this.Privileges),
				this.Color.ToString()
			});
		}

		public int CompareTo(IPlayerRole other)
		{
			return this.PrivilegeLevel.CompareTo(other.PrivilegeLevel);
		}

		public bool IsSuperior(IPlayerRole clientGroup)
		{
			return clientGroup == null || this.PrivilegeLevel > clientGroup.PrivilegeLevel;
		}

		public bool EqualLevel(IPlayerRole clientGroup)
		{
			return this.PrivilegeLevel == clientGroup.PrivilegeLevel;
		}

		public void GrantPrivilege(params string[] privileges)
		{
			foreach (string priv in privileges)
			{
				if (!this.Privileges.Contains(priv))
				{
					this.Privileges.Add(priv);
				}
			}
		}

		public void RevokePrivilege(string privilege)
		{
			this.Privileges.Remove(privilege);
		}

		public static string PrivilegesString(List<string> privileges)
		{
			string privilegesString = "";
			if (privileges.Count > 0)
			{
				privilegesString = privileges[0].ToString();
				for (int i = 1; i < privileges.Count; i++)
				{
					privilegesString = privilegesString + "," + privileges[i].ToString();
				}
			}
			return privilegesString;
		}
	}
}
