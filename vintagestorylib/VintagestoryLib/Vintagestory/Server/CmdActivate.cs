using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.Server
{
	internal class CmdActivate
	{
		public CmdActivate(ServerMain server)
		{
			this.sapi = server.api;
			CommandArgumentParsers parsers = this.sapi.ChatCommands.Parsers;
			this.sapi.ChatCommands.Create("activate").RequiresPrivilege(Privilege.controlserver).WithDesc("Runs activate on targeted block with supplied arguments.")
				.WithExamples(new string[] { "<code>/activate ~ ~ ~1 { opened: true }</code> - Open door 1 block adjacent in x direction, given there is a placed door" })
				.WithArgs(new ICommandArgumentParser[]
				{
					parsers.WorldPosition("position"),
					parsers.OptionalAll("argument object")
				})
				.HandleWith(new OnCommandDelegate(this.activate));
		}

		private TextCommandResult activate(TextCommandCallingArgs args)
		{
			TreeAttribute tree = null;
			if (!args.Parsers[1].IsMissing)
			{
				tree = (TreeAttribute)JsonObject.FromJson(args[1] as string).ToAttribute();
			}
			Vec3d pos = args[0] as Vec3d;
			Block block = this.sapi.World.BlockAccessor.GetBlock(pos.AsBlockPos);
			block.Activate(this.sapi.World, args.Caller, new BlockSelection
			{
				Position = pos.AsBlockPos,
				Face = BlockFacing.NORTH
			}, tree);
			return TextCommandResult.Success("Called activate on block code" + block.Code, null);
		}

		private ICoreServerAPI sapi;
	}
}
