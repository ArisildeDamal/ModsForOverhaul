using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace Vintagestory.Server
{
	internal class CmdGlobalList
	{
		public CmdGlobalList(ServerMain server)
		{
			this.server = server;
			server.api.commandapi.Create("list").RequiresPrivilege(Privilege.readlists).WithDesc("Show global lists (clients, banned, roles or privileges)")
				.BeginSub("clients")
				.WithAlias(new string[] { "c" })
				.WithDesc("Players who are currently online")
				.HandleWith(new OnCommandDelegate(this.listClients))
				.EndSub()
				.BeginSub("banned")
				.WithAlias(new string[] { "b" })
				.WithDesc("Users who are banned from this server")
				.HandleWith(new OnCommandDelegate(this.listBanned))
				.EndSub()
				.BeginSub("roles")
				.WithAlias(new string[] { "r" })
				.WithDesc("Available roles")
				.HandleWith(new OnCommandDelegate(this.listRoles))
				.EndSub()
				.BeginSub("privileges")
				.WithAlias(new string[] { "p" })
				.WithDesc("Available privileges")
				.HandleWith(new OnCommandDelegate(this.listPrivileges))
				.EndSub()
				.HandleWith(new OnCommandDelegate(this.handleList));
		}

		private TextCommandResult listClients(TextCommandCallingArgs args)
		{
			StringBuilder text = new StringBuilder();
			text.AppendLine(Lang.Get("List of online Players", Array.Empty<object>()));
			using (IEnumerator<ConnectedClient> enumerator = this.server.Clients.Values.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					ConnectedClient client = enumerator.Current;
					if (client.State == EnumClientState.Connected || client.State == EnumClientState.Playing || client.State == EnumClientState.Queued)
					{
						if (client.State == EnumClientState.Queued)
						{
							int q = this.server.ConnectionQueue.FindIndex((QueuedClient c) => c.Client.Id == client.Id);
							if (q >= 0)
							{
								QueuedClient queueClient = this.server.ConnectionQueue[q];
								text.AppendLine(string.Format("[{0}] {1} {2} | Queue position: ({3})", new object[]
								{
									client.Id,
									queueClient.Identification.Playername,
									client.Socket.RemoteEndPoint(),
									q + 1
								}));
							}
							else
							{
								ServerMain.Logger.Warning("Client {0} not found in connection queue", new object[] { client.Id });
							}
						}
						else
						{
							text.AppendLine(string.Format("[{0}] {1} {2}", client.Id, client.PlayerName, client.Socket.RemoteEndPoint()));
						}
					}
				}
			}
			return TextCommandResult.Success(text.ToString(), null);
		}

		private TextCommandResult listBanned(TextCommandCallingArgs args)
		{
			StringBuilder text = new StringBuilder();
			text.AppendLine(Lang.Get("List of Banned Users:", Array.Empty<object>()));
			foreach (PlayerEntry entry in this.server.PlayerDataManager.BannedPlayers)
			{
				string reason = entry.Reason;
				if (string.IsNullOrEmpty(reason))
				{
					reason = "";
				}
				if (entry.UntilDate >= DateTime.Now)
				{
					text.AppendLine(string.Format("{0} until {1}. Reason: {2}", entry.PlayerName, entry.UntilDate, reason));
				}
			}
			return TextCommandResult.Success(text.ToString(), null);
		}

		private TextCommandResult listRoles(TextCommandCallingArgs args)
		{
			StringBuilder text = new StringBuilder();
			text.AppendLine(Lang.Get("List of roles:", Array.Empty<object>()));
			foreach (PlayerRole group in this.server.Config.Roles)
			{
				text.AppendLine(group.ToString());
			}
			return TextCommandResult.Success(text.ToString(), null);
		}

		private TextCommandResult listPrivileges(TextCommandCallingArgs args)
		{
			StringBuilder text = new StringBuilder();
			text.AppendLine(Lang.Get("Available privileges:", Array.Empty<object>()));
			foreach (string privilege in this.server.AllPrivileges)
			{
				text.AppendLine(privilege.ToString());
			}
			return TextCommandResult.Success(text.ToString(), null);
		}

		private TextCommandResult handleList(TextCommandCallingArgs args)
		{
			return TextCommandResult.Error("Syntax error, requires argument clients|banned|roles|privileges or c|b|r|p", "");
		}

		private ServerMain server;
	}
}
