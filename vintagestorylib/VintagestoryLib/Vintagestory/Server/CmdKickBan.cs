using System;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace Vintagestory.Server
{
	internal class CmdKickBan
	{
		public CmdKickBan(ServerMain server)
		{
			this.server = server;
			IChatCommandApi cmdapi = server.api.ChatCommands;
			CommandArgumentParsers parsers = server.api.ChatCommands.Parsers;
			cmdapi.Create("kick").RequiresPrivilege(Privilege.kick).WithDescription("Kicks a player from the server")
				.WithArgs(new ICommandArgumentParser[]
				{
					parsers.PlayerUids("player name"),
					parsers.OptionalAll("kick reason")
				})
				.HandleWith((TextCommandCallingArgs args) => CmdPlayer.Each(args, (PlayerUidName plr, TextCommandCallingArgs args) => this.Kick(args.Caller, plr, (string)args[1])))
				.Validate();
			cmdapi.Create("ban").RequiresPrivilege(Privilege.ban).WithDescription("Ban a player from the server")
				.WithArgs(new ICommandArgumentParser[]
				{
					parsers.PlayerUids("player name"),
					parsers.DateTime("duration"),
					parsers.All("reason")
				})
				.HandleWith((TextCommandCallingArgs cargs) => CmdPlayer.Each(cargs, (PlayerUidName plr, TextCommandCallingArgs args) => this.Ban(args.Caller, plr, (DateTime)args[1], (string)args[2])))
				.Validate();
			cmdapi.Create("unban").RequiresPrivilege(Privilege.ban).WithDescription("Remove a player ban")
				.WithArgs(new ICommandArgumentParser[] { parsers.PlayerUids("player name") })
				.HandleWith((TextCommandCallingArgs cargs) => CmdPlayer.Each(cargs, (PlayerUidName plr, TextCommandCallingArgs args) => this.UnBan(args.Caller, plr)))
				.Validate();
			cmdapi.Create("hardban").RequiresPrivilege(Privilege.ban).WithDescription("Ban a player forever without reason")
				.WithArgs(new ICommandArgumentParser[] { parsers.PlayerUids("player name") })
				.HandleWith((TextCommandCallingArgs cargs) => CmdPlayer.Each(cargs, (PlayerUidName plr, TextCommandCallingArgs args) => this.Ban(args.Caller, plr, DateTime.Now.AddYears(1000), "hard ban")))
				.Validate();
		}

		private TextCommandResult UnBan(Caller caller, PlayerUidName plr)
		{
			if (this.server.PlayerDataManager.UnbanPlayer(plr.Name, plr.Uid, caller.GetName()))
			{
				return TextCommandResult.Success(Lang.Get("Player is now unbanned", Array.Empty<object>()), null);
			}
			return TextCommandResult.Error(Lang.Get("Player was not banned", Array.Empty<object>()), "");
		}

		private TextCommandResult Ban(Caller caller, PlayerUidName targetPlayer, DateTime untilDate, string reason)
		{
			TextCommandResult result = this.CanKickOrBanTarget(caller, targetPlayer.Name);
			if (result.Status == EnumCommandStatus.Error)
			{
				return result;
			}
			this.server.PlayerDataManager.BanPlayer(targetPlayer.Name, targetPlayer.Uid, caller.GetName(), reason, new DateTime?(untilDate));
			ConnectedClient targetClient = this.server.GetClientByUID(targetPlayer.Uid);
			if (targetClient != null)
			{
				this.server.DisconnectPlayer(targetClient, Lang.Get("cmdban-playerwasbanned", new object[]
				{
					targetPlayer.Name,
					caller.GetName(),
					(reason.Length > 0) ? (", reason: " + reason) : ""
				}), Lang.Get("cmdban-youvebeenbanned", new object[]
				{
					caller.GetName(),
					(reason.Length > 0) ? (", reason: " + reason) : ""
				}));
			}
			return TextCommandResult.Success(Lang.Get("cmdban-playerisnowbanned", new object[] { untilDate }), null);
		}

		private TextCommandResult Kick(Caller caller, PlayerUidName puidn, string reason = "")
		{
			IPlayer targetPlayer = this.server.AllOnlinePlayers.FirstOrDefault((IPlayer plr) => plr.PlayerUID == puidn.Uid);
			if (targetPlayer == null)
			{
				return TextCommandResult.Error("No such user online", "");
			}
			ConnectedClient targetClient;
			if (!this.server.Clients.TryGetValue(targetPlayer.ClientId, out targetClient))
			{
				return TextCommandResult.Error(Lang.Get("No player with connectionid '{0}' exists", new object[] { targetPlayer.ClientId }), "");
			}
			TextCommandResult result = this.CanKickOrBanTarget(caller, targetPlayer.PlayerName);
			if (result.Status == EnumCommandStatus.Error)
			{
				return result;
			}
			string targetName = targetClient.PlayerName;
			string sourceName = caller.GetName();
			if (reason == null)
			{
				reason = "";
			}
			string hisMsg = ((reason.Length == 0) ? Lang.Get("You've been kicked by {0}", new object[] { sourceName }) : Lang.Get("You've been kicked by {0}, reason: {1}", new object[] { sourceName, reason }));
			string othersMsg = ((reason.Length == 0) ? Lang.Get("{0} has been kicked by {1}", new object[] { targetName, sourceName }) : Lang.Get("{0} has been kicked by {1}, reason: {2}", new object[] { targetName, sourceName, reason }));
			this.server.DisconnectPlayer(targetClient, othersMsg, hisMsg);
			ServerMain.Logger.Audit(string.Format("{0} kicks {1}. Reason: {2}", sourceName, targetName, (reason.Length == 0) ? "none given" : reason));
			return TextCommandResult.Success(othersMsg, null);
		}

		protected TextCommandResult CanKickOrBanTarget(Caller caller, string targetPlayerName)
		{
			ServerPlayerData plrdata = this.server.PlayerDataManager.GetServerPlayerDataByLastKnownPlayername(targetPlayerName);
			if (plrdata == null)
			{
				return TextCommandResult.Success("", null);
			}
			PlayerRole targetRole = plrdata.GetPlayerRole(this.server);
			if (targetRole == null)
			{
				return TextCommandResult.Success("", null);
			}
			IPlayerRole callerRole = caller.GetRole(this.server.api);
			if (targetRole.IsSuperior(callerRole) || (targetRole.EqualLevel(callerRole) && !caller.HasPrivilege(Privilege.root)))
			{
				return TextCommandResult.Error(Lang.Get("Can't kick or ban a player with a superior or equal group level", Array.Empty<object>()), "");
			}
			return TextCommandResult.Success("", null);
		}

		private ServerMain server;
	}
}
