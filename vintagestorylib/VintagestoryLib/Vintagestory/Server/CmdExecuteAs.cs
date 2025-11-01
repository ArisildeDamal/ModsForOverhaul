using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace Vintagestory.Server
{
	internal class CmdExecuteAs
	{
		public CmdExecuteAs(ServerMain server)
		{
			this.sapi = server.api;
			CommandArgumentParsers parsers = this.sapi.ChatCommands.Parsers;
			this.sapi.ChatCommands.Create("executeas").RequiresPrivilege(Privilege.controlserver).WithDesc("Execute command with selected player/entity as the caller, but runs under the caller privileges.")
				.WithExamples(new string[] { "<code>/executeas e[type=wolf*] /setblock rock-granite ~ ~1 ~</code> - Place granite above all wolves" })
				.WithArgs(new ICommandArgumentParser[]
				{
					parsers.Entities("caller"),
					parsers.All("command without /")
				})
				.HandleWith((TextCommandCallingArgs args) => CmdUtil.EntityEach(args, (Entity e) => this.executeAs(e, args), 0));
		}

		private TextCommandResult executeAs(Entity entity, TextCommandCallingArgs args)
		{
			string command = args[1] as string;
			TextCommandResult result = TextCommandResult.Deferred;
			string[] array;
			if ((array = args.Caller.CallerPrivileges) == null)
			{
				IPlayer player = args.Caller.Player;
				array = ((player != null) ? player.Privileges : null);
			}
			string[] callerPrivs = array;
			Caller caller = new Caller
			{
				Entity = entity,
				Type = EnumCallerType.Entity,
				CallerPrivileges = callerPrivs
			};
			EntityPlayer eplr = entity as EntityPlayer;
			if (eplr != null)
			{
				caller.Player = eplr.Player;
			}
			this.sapi.ChatCommands.ExecuteUnparsed(command, new TextCommandCallingArgs
			{
				Caller = caller
			}, delegate(TextCommandResult res)
			{
				result = res;
			});
			return result;
		}

		private ICoreServerAPI sapi;
	}
}
