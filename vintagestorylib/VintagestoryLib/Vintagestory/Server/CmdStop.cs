using System;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace Vintagestory.Server
{
	internal class CmdStop
	{
		public CmdStop(ServerMain server)
		{
			this.server = server;
			server.api.commandapi.Create("stop").WithArgs(new ICommandArgumentParser[] { server.api.commandapi.Parsers.OptionalInt("exit code", 0) }).RequiresPrivilege(Privilege.controlserver)
				.HandleWith(new OnCommandDelegate(this.handleStop));
		}

		private TextCommandResult handleStop(TextCommandCallingArgs args)
		{
			this.server.BroadcastMessageToAllGroups(Lang.Get("{0} commenced a server shutdown", new object[] { args.Caller.GetName() }), EnumChatType.AllGroups, null);
			ServerMain.Logger.Event(string.Format("{0} shuts down server.", args.Caller.GetName()));
			this.server.ExitCode = (int)args[0];
			this.server.Stop("Shutdown via server command", null, EnumLogType.Notification);
			return TextCommandResult.Success("Shut down command executed", null);
		}

		private ServerMain server;
	}
}
