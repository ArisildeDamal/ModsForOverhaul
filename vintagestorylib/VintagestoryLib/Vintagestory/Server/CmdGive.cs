using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace Vintagestory.Server
{
	internal class CmdGive
	{
		public CmdGive(ServerMain server)
		{
			IChatCommandApi cmdapi = server.api.ChatCommands;
			CommandArgumentParsers parsers = server.api.ChatCommands.Parsers;
			ServerCoreAPI api = server.api;
			cmdapi.Create("giveitem").RequiresPrivilege(Privilege.gamemode).WithDescription("Give items to target")
				.WithArgs(new ICommandArgumentParser[]
				{
					parsers.Item("item code"),
					parsers.OptionalInt("quantity", 1),
					parsers.OptionalEntities("target"),
					parsers.OptionalAll("attributes")
				})
				.HandleWith(delegate(TextCommandCallingArgs args)
				{
					if (args.Parsers[2].IsMissing)
					{
						return this.give(args.Caller.Entity, args);
					}
					return CmdUtil.EntityEach(args, (Entity e) => this.give(e, args), 2);
				});
			cmdapi.Create("giveblock").RequiresPrivilege(Privilege.gamemode).WithDescription("Give blocks to target")
				.WithArgs(new ICommandArgumentParser[]
				{
					parsers.Block("block code"),
					parsers.OptionalInt("quantity", 1),
					parsers.OptionalEntities("target"),
					parsers.OptionalAll("attributes")
				})
				.HandleWith(delegate(TextCommandCallingArgs args)
				{
					if (args.Parsers[2].IsMissing)
					{
						return this.give(args.Caller.Entity, args);
					}
					return CmdUtil.EntityEach(args, (Entity e) => this.give(e, args), 2);
				});
		}

		private TextCommandResult give(Entity target, TextCommandCallingArgs args)
		{
			ItemStack stack = args[0] as ItemStack;
			int quantity = (int)args[1];
			stack.StackSize = quantity;
			string jsonattributes = (string)args.LastArg;
			if (jsonattributes != null)
			{
				stack.Attributes.MergeTree(TreeAttribute.FromJson(jsonattributes) as TreeAttribute);
			}
			if (target.TryGiveItemStack(stack.Clone()))
			{
				return TextCommandResult.Success("Ok, gave " + quantity.ToString() + "x " + stack.GetName(), null);
			}
			return TextCommandResult.Error("Failed, target players inventory is likely full or cant accept this item for other reasons", "");
		}
	}
}
