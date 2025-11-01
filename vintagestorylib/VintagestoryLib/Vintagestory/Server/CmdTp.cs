using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.Server
{
	internal class CmdTp
	{
		public CmdTp(ServerMain server)
		{
			ServerCoreAPI api = server.api;
			CommandArgumentParsers parsers = api.ChatCommands.Parsers;
			api.commandapi.Create("tp").RequiresPrivilege(Privilege.tp).WithDesc("Teleport a player or entity to a location")
				.WithExamples(new string[] { "<code>/tp Luke Hayden</code> - teleports Luke to Hayden, if both players are online", "<code>/tp p[] e[type=wolf*]</code> - teleport all players to the wolf nearest to you" })
				.WithArgs(new ICommandArgumentParser[]
				{
					parsers.Entities("source"),
					parsers.WorldPosition("target")
				})
				.HandleWith(delegate(TextCommandCallingArgs args)
				{
					if (args[1] as Vec3d == null)
					{
						return TextCommandResult.Error("No matching target location found", "");
					}
					return CmdUtil.EntityEach(args, (Entity e) => this.handleTp(e, args), 0);
				});
		}

		private TextCommandResult handleTp(Entity e, TextCommandCallingArgs args)
		{
			Vec3d target = args.Parsers[1].GetValue() as Vec3d;
			e.TeleportTo(target);
			return TextCommandResult.Success("Ok, teleported " + e.GetName(), null);
		}
	}
}
