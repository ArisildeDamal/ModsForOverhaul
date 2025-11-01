using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace Vintagestory.Server
{
	internal class CmdHelp
	{
		public CmdHelp(ServerMain server)
		{
			this.server = server;
			CommandArgumentParsers parsers = server.api.ChatCommands.Parsers;
			server.api.commandapi.GetOrCreate("help").RequiresPrivilege(Privilege.chat).WithArgs(new ICommandArgumentParser[]
			{
				parsers.OptionalWord("commandname"),
				parsers.OptionalWord("subcommand"),
				parsers.OptionalWord("subsubcommand")
			})
				.WithDescription("Display list of available server commands")
				.HandleWith(new OnCommandDelegate(this.handleHelp));
		}

		private TextCommandResult handleHelp(TextCommandCallingArgs args)
		{
			StringBuilder text = new StringBuilder();
			Dictionary<string, IChatCommand> commands = IChatCommandApi.GetOrdered(this.server.api.commandapi.AllSubcommands());
			if (args.Parsers[0].IsMissing)
			{
				text.AppendLine("Available commands:");
				this.WriteCommandsList(text, commands, args.Caller, false);
				text.Append("\n" + Lang.Get("Type /help [commandname] to see more info about a command", Array.Empty<object>()));
				return TextCommandResult.Success(text.ToString(), null);
			}
			string arg = (string)args[0];
			if (!args.Parsers[1].IsMissing)
			{
				bool found = false;
				foreach (KeyValuePair<string, IChatCommand> entry in commands)
				{
					if (entry.Key == arg)
					{
						commands = IChatCommandApi.GetOrdered(entry.Value.AllSubcommands);
						found = true;
						break;
					}
				}
				if (!found)
				{
					return TextCommandResult.Error(string.Concat(new string[]
					{
						Lang.Get("No such sub-command found", Array.Empty<object>()),
						": ",
						arg,
						" ",
						(string)args[1]
					}), "");
				}
				arg = (string)args[1];
				if (!args.Parsers[2].IsMissing)
				{
					found = false;
					foreach (KeyValuePair<string, IChatCommand> entry2 in commands)
					{
						if (entry2.Key == arg)
						{
							commands = IChatCommandApi.GetOrdered(entry2.Value.AllSubcommands);
							found = true;
							break;
						}
					}
					if (!found)
					{
						return TextCommandResult.Error(string.Concat(new string[]
						{
							Lang.Get("No such sub-command found", Array.Empty<object>()),
							": ",
							(string)args[0],
							arg,
							" ",
							(string)args[2]
						}), "");
					}
					arg = (string)args[2];
				}
			}
			foreach (KeyValuePair<string, IChatCommand> entry3 in commands)
			{
				if (entry3.Key == arg)
				{
					ChatCommandImpl cm = entry3.Value as ChatCommandImpl;
					if (cm.IsAvailableTo(args.Caller))
					{
						Dictionary<string, IChatCommand> subcommands = cm.AllSubcommands;
						if (subcommands.Count > 0)
						{
							text.AppendLine("Available subcommands:");
							this.WriteCommandsList(text, subcommands, args.Caller, true);
							text.AppendLine();
							text.AppendLine("Type <code>/help " + cm.CallSyntax.Substring(1) + " &lt;<i>subcommand_name</i>&gt;</code> for help on a specific subcommand");
						}
						else
						{
							text.AppendLine();
							if (cm.Description != null)
							{
								text.AppendLine(cm.Description);
							}
							if (cm.AdditionalInformation != null)
							{
								text.AppendLine(cm.AdditionalInformation);
							}
							text.AppendLine();
							text.AppendLine("Usage: <code>");
							text.Append(cm.GetCallSyntax(entry3.Key, false));
							text.Append("</code>");
							cm.AddSyntaxExplanation(text, "");
							if (cm.Examples != null && cm.Examples.Length != 0)
							{
								text.AppendLine();
								text.AppendLine((cm.Examples.Length > 1) ? "Examples:" : "Example:");
								foreach (string ex in cm.Examples)
								{
									text.AppendLine(ex);
								}
							}
						}
						return TextCommandResult.Success(text.ToString(), null);
					}
					return TextCommandResult.Error("Insufficient privilege to use this command", "");
				}
			}
			return TextCommandResult.Error(Lang.Get("No such command found", Array.Empty<object>()) + ": " + arg, "");
		}

		private void WriteCommandsList(StringBuilder text, Dictionary<string, IChatCommand> commands, Caller caller, bool isSubCommand = false)
		{
			text.AppendLine();
			foreach (KeyValuePair<string, IChatCommand> val in commands)
			{
				IChatCommand cm = val.Value;
				if (cm.IsAvailableTo(caller))
				{
					string desc = cm.Description;
					if (desc == null)
					{
						desc = " ";
					}
					else
					{
						int i = desc.IndexOf('\n');
						if (i >= 0)
						{
							desc = desc.Substring(0, i);
						}
						desc = Lang.Get(desc, Array.Empty<object>());
					}
					text.AppendLine("<code>" + cm.GetCallSyntax(val.Key, !isSubCommand) + "</code> :  " + desc);
				}
			}
		}

		private ServerMain server;
	}
}
