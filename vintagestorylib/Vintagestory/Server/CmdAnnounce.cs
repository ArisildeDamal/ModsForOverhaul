using System;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Vintagestory.Server
{
	internal class CmdAnnounce
	{
		public CmdAnnounce(ServerMain server)
		{
			IChatCommandApi chatCommands = server.api.ChatCommands;
			CommandArgumentParsers parsers = server.api.ChatCommands.Parsers;
			chatCommands.Create("announce").RequiresPrivilege(Privilege.announce).WithDescription("Announce a server wide message in all groups")
				.WithArgs(new ICommandArgumentParser[] { parsers.All("announcement message") })
				.HandleWith(delegate(TextCommandCallingArgs args)
				{
					ServerMain.Logger.Event(string.Format("{0} announced: {1}.", args.Caller.GetName(), (string)args[0]));
					server.BroadcastMessageToAllGroups("<strong><font color=\"orange\">" + (string)args[0] + "</font></strong>", EnumChatType.AllGroups, null);
					return TextCommandResult.Success("", null);
				});
		}
	}
}
