using System;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace Vintagestory.Server
{
	public abstract class BaseServerChatCommandDelegateProvider : LegacyServerChatCommand
	{
		protected void ServerEventLog(string p)
		{
			ServerMain.Logger.Event(p);
		}

		protected void ErrorSyntax(string command, IServerPlayer player, int groupId)
		{
			player.SendMessage(groupId, "Syntax: " + this.syntax, EnumChatType.CommandError, null);
		}

		protected bool PlayerHasPrivilege(int player, string privilege)
		{
			return this.server.PlayerHasPrivilege(player, privilege);
		}

		protected bool ConfigNeedsSaving
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

		protected ServerConfig Config
		{
			get
			{
				return this.server.Config;
			}
		}

		protected ServerWorldMap Servermap
		{
			get
			{
				return this.server.WorldMap;
			}
		}

		public string syntax { get; set; }

		public BaseServerChatCommandDelegateProvider(ServerMain server)
		{
			this.server = server;
		}

		public ServerChatCommandDelegate GetDelegate()
		{
			return new ServerChatCommandDelegate(this.Handle);
		}

		public ConnectedClient GetClient(IServerPlayer player)
		{
			if (player is ServerConsolePlayer)
			{
				return this.server.ServerConsoleClient;
			}
			return this.server.Clients[player.ClientId];
		}

		protected void Success(IServerPlayer player, int groupId, string message)
		{
			player.SendMessage(groupId, message, EnumChatType.CommandSuccess, null);
		}

		protected void Error(IServerPlayer player, int groupId, string message)
		{
			player.SendMessage(groupId, message, EnumChatType.CommandError, null);
		}

		protected bool CanKickOrBanTarget(int groupId, IServerPlayer issuingPlayer, string targetPlayerName)
		{
			ServerPlayerData plrdata = this.server.PlayerDataManager.GetServerPlayerDataByLastKnownPlayername(targetPlayerName);
			if (plrdata == null)
			{
				return true;
			}
			PlayerRole hisGroup = plrdata.GetPlayerRole(this.server);
			if (hisGroup == null)
			{
				return true;
			}
			PlayerRole ownGroup = this.Config.RolesByCode[this.GetClient(issuingPlayer).ServerData.RoleCode];
			if (hisGroup.IsSuperior(ownGroup) || (hisGroup.EqualLevel(ownGroup) && !issuingPlayer.HasPrivilege(Privilege.root)))
			{
				this.Error(issuingPlayer, groupId, Lang.Get("Can't kick or ban a player with a superior or equal group level", Array.Empty<object>()));
				return false;
			}
			return true;
		}

		public abstract void Handle(IServerPlayer player, int groupId, CmdArgs args);

		protected ServerMain server;
	}
}
