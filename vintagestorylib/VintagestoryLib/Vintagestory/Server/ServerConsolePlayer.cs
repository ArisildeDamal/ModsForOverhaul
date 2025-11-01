using System;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace Vintagestory.Server
{
	public class ServerConsolePlayer : ServerPlayer
	{
		public ServerConsolePlayer(ServerMain server, ServerWorldPlayerData worlddata)
			: base(server, worlddata)
		{
			this.client = server.ServerConsoleClient;
			this.clientGroup = server.Config.Roles.MaxBy((PlayerRole v) => v.PrivilegeLevel);
		}

		protected override void Init()
		{
		}

		public override void BroadcastPlayerData(bool sendInventory = false)
		{
		}

		public override EnumClientState ConnectionState
		{
			get
			{
				return EnumClientState.Offline;
			}
		}

		public override string PlayerUID
		{
			get
			{
				return "console";
			}
		}

		public override bool HasPrivilege(string privilegeCode)
		{
			return true;
		}

		public override void Disconnect()
		{
		}

		public override void Disconnect(string message)
		{
		}

		public override IPlayerRole Role
		{
			get
			{
				return this.clientGroup;
			}
		}

		private PlayerRole clientGroup;
	}
}
