using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Vintagestory.Server
{
	public class ServerSystemEntityCodeRemapper : ServerSystem
	{
		public ServerSystemEntityCodeRemapper(ServerMain server)
			: base(server)
		{
			CommandArgumentParsers parsers = server.api.ChatCommands.Parsers;
			server.api.ChatCommands.Create("eir").RequiresPrivilege(Privilege.controlserver).WithDescription("Entity code remapper info and fixing tool")
				.BeginSubCommand("list")
				.WithDescription("list")
				.HandleWith(new OnCommandDelegate(this.OnCmdList))
				.EndSubCommand()
				.BeginSubCommand("map")
				.WithDescription("map")
				.RequiresPlayer()
				.WithArgs(new ICommandArgumentParser[]
				{
					parsers.Word("new_entity"),
					parsers.Word("old_entity"),
					parsers.OptionalWord("force")
				})
				.HandleWith(new OnCommandDelegate(this.OnCmdMap))
				.EndSubCommand()
				.BeginSubCommand("remap")
				.WithAlias(new string[] { "remapq" })
				.WithDescription("map")
				.RequiresPlayer()
				.WithArgs(new ICommandArgumentParser[]
				{
					parsers.Word("new_entity"),
					parsers.Word("old_entity"),
					parsers.OptionalWord("force")
				})
				.HandleWith(new OnCommandDelegate(this.OnCmdReMap))
				.EndSubCommand();
		}

		private TextCommandResult OnCmdReMap(TextCommandCallingArgs args)
		{
			bool quiet = args.SubCmdCode == "remapq";
			string newEntityCode = args[0] as string;
			string oldEntityCode = args[1] as string;
			bool force = !args.Parsers[2].IsMissing && args[2] as string == "force";
			IServerPlayer player = args.Caller.Player as IServerPlayer;
			this.Addmapping(this.server.EntityCodeRemappings, newEntityCode, oldEntityCode, player, args.Caller.FromChatGroupId, true, force, quiet);
			return TextCommandResult.Success("", null);
		}

		private TextCommandResult OnCmdMap(TextCommandCallingArgs args)
		{
			string newEntityCode = args[0] as string;
			string oldEntityCode = args[1] as string;
			bool force = !args.Parsers[2].IsMissing && args[2] as string == "force";
			IServerPlayer player = args.Caller.Player as IServerPlayer;
			this.Addmapping(this.server.EntityCodeRemappings, newEntityCode, oldEntityCode, player, args.Caller.FromChatGroupId, false, force, false);
			return TextCommandResult.Success("", null);
		}

		private TextCommandResult OnCmdList(TextCommandCallingArgs args)
		{
			Dictionary<string, string> entityCodeRemappings = this.server.EntityCodeRemappings;
			ServerMain.Logger.Notification("Current entity code remapping (issued by /eir list command)");
			foreach (KeyValuePair<string, string> val in entityCodeRemappings)
			{
				ServerMain.Logger.Notification("  " + val.Key + ": " + val.Value);
			}
			return TextCommandResult.Success("Full mapping printed to console and main log file", null);
		}

		private void Addmapping(Dictionary<string, string> entityRemaps, string newCode, string oldCode, IServerPlayer player, int groupId, bool remap, bool force, bool quiet)
		{
			string prevCode;
			if (!force && entityRemaps.TryGetValue(oldCode, out prevCode))
			{
				player.SendMessage(groupId, string.Concat(new string[]
				{
					"new entity code ",
					oldCode,
					" is already mapped to ",
					prevCode,
					", type '/eir ",
					remap ? "remap" : "map",
					" ",
					newCode,
					" ",
					oldCode,
					" force' to overwrite"
				}), EnumChatType.CommandError, null);
				return;
			}
			entityRemaps[oldCode] = newCode;
			if (!quiet)
			{
				string type = (remap ? "remapped" : "mapped");
				player.SendMessage(groupId, string.Concat(new string[] { newCode, " is now ", type, " from entity code ", oldCode }), EnumChatType.CommandSuccess, null);
			}
		}
	}
}
