using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.Server
{
	internal class CmdSetBlock
	{
		public CmdSetBlock(ServerMain server)
		{
			this.sapi = server.api;
			CommandArgumentParsers parsers = this.sapi.ChatCommands.Parsers;
			this.sapi.ChatCommands.Create("setblock").RequiresPrivilege(Privilege.gamemode).WithDesc("Set a block at a given location")
				.WithArgs(new ICommandArgumentParser[]
				{
					parsers.Block("block code"),
					parsers.WorldPosition("target")
				})
				.HandleWith(new OnCommandDelegate(this.handleSetBlock));
		}

		private TextCommandResult handleSetBlock(TextCommandCallingArgs args)
		{
			ItemStack stack = args[0] as ItemStack;
			Vec3d target = args[1] as Vec3d;
			if (target == null)
			{
				return TextCommandResult.Error("Missing/Invalid target", "");
			}
			this.sapi.World.BlockAccessor.SetBlock(stack.Block.Id, target.AsBlockPos, stack);
			return TextCommandResult.Error(stack.Block.Code + " + placed.", "");
		}

		private ICoreServerAPI sapi;
	}
}
