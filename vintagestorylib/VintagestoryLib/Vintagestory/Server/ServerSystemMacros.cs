using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.Server
{
	public class ServerSystemMacros : ServerSystem
	{
		public override void OnPlayerDisconnect(ServerPlayer player)
		{
			this.wipMacroByPlayer.Remove(player.PlayerUID);
		}

		public override void OnBeginConfiguration()
		{
			this.LoadMacros();
		}

		public ServerSystemMacros(ServerMain server)
			: base(server)
		{
			IChatCommandApi chatCommands = server.api.ChatCommands;
			CommandArgumentParsers parsers = server.api.ChatCommands.Parsers;
			chatCommands.Create("macro").WithDesc("Manage server side macros").RequiresPrivilege(Privilege.controlserver)
				.BeginSubCommand("addcmd")
				.WithDesc("Append a command")
				.WithArgs(new ICommandArgumentParser[] { parsers.All("Command to add. {{param0}}, {{param1}}, etc. can be used as placeholders for command arguments.") })
				.HandleWith((TextCommandCallingArgs args) => this.addCmd(args, false))
				.EndSubCommand()
				.BeginSubCommand("setcmd")
				.WithDesc("Set command (clears any previously set commands). {{param0}}, {{param1}}, etc. can be used as placeholders for command arguments.")
				.WithArgs(new ICommandArgumentParser[] { parsers.All("Command to set") })
				.HandleWith((TextCommandCallingArgs args) => this.addCmd(args, true))
				.EndSubCommand()
				.BeginSubCommand("desc")
				.WithDesc("Set command description")
				.WithArgs(new ICommandArgumentParser[] { parsers.All("Description to set") })
				.HandleWith(delegate(TextCommandCallingArgs args)
				{
					this.getWipMacro(args.Caller, true).Description = (string)args[0];
					return TextCommandResult.Success(Lang.Get("Ok, description set", Array.Empty<object>()), null);
				})
				.EndSubCommand()
				.BeginSubCommand("priv")
				.WithDesc("Set command privilege")
				.WithArgs(new ICommandArgumentParser[] { parsers.Word("Required privilege to run command", Privilege.AllCodes().Append("or custom privelges")) })
				.HandleWith(delegate(TextCommandCallingArgs args)
				{
					this.getWipMacro(args.Caller, true).Privilege = (string)args[0];
					return TextCommandResult.Success(Lang.Get("Ok, privilege set", Array.Empty<object>()), null);
				})
				.EndSubCommand()
				.BeginSubCommand("discard")
				.WithDesc("Discard wip macro")
				.HandleWith(delegate(TextCommandCallingArgs args)
				{
					Dictionary<string, ServerCommandMacro> dictionary = this.wipMacroByPlayer;
					IPlayer player = args.Caller.Player;
					dictionary.Remove(((player != null) ? player.PlayerUID : null) ?? "_console");
					return TextCommandResult.Success("wip macro discarded", null);
				})
				.EndSubCommand()
				.BeginSubCommand("save")
				.WithDesc("Save wip macro")
				.WithArgs(new ICommandArgumentParser[] { parsers.Word("name of the macro") })
				.HandleWith(new OnCommandDelegate(this.saveMacro))
				.EndSubCommand()
				.BeginSubCommand("delete")
				.WithDesc("Delete a macro")
				.WithArgs(new ICommandArgumentParser[] { parsers.Word("macro name") })
				.HandleWith(new OnCommandDelegate(this.deleteMacro))
				.EndSubCommand()
				.BeginSubCommand("list")
				.WithDesc("List current macros")
				.HandleWith(new OnCommandDelegate(this.listMacros))
				.EndSubCommand()
				.BeginSubCommand("show")
				.WithDesc("Show given info on macro")
				.WithArgs(new ICommandArgumentParser[] { parsers.Word("macro name") })
				.HandleWith(new OnCommandDelegate(this.showMacro))
				.EndSubCommand()
				.BeginSubCommand("showwip")
				.WithDesc("Show info on current wip macro")
				.HandleWith(new OnCommandDelegate(this.showWipMacro))
				.EndSubCommand();
		}

		private TextCommandResult saveMacro(TextCommandCallingArgs args)
		{
			string macroname = (string)args[0];
			if (this.server.api.commandapi.Get(macroname) != null)
			{
				return TextCommandResult.Error(Lang.Get("Command /{0} is already taken, please choose another name", new object[] { macroname }), "commandnameused");
			}
			ServerCommandMacro macro = this.getWipMacro(args.Caller, false);
			if (macro == null || macro.Commands.Length == 0)
			{
				return TextCommandResult.Error(Lang.Get("No commands defined for this macro. Add at least 1 command first.", Array.Empty<object>()), "nocommandsdefined");
			}
			if (macro.Privilege == null)
			{
				return TextCommandResult.Error(Lang.Get("No privilege defined for this macro. Set privilege with /macro priv.", Array.Empty<object>()), "noprivdefined");
			}
			ServerCommandMacro serverCommandMacro = macro;
			IPlayer player = args.Caller.Player;
			serverCommandMacro.CreatedByPlayerUid = ((player != null) ? player.PlayerUID : null) ?? "console";
			this.ServerCommmandMacros[macroname] = macro;
			this.RegisterMacro(macro);
			this.SaveMacros();
			Dictionary<string, ServerCommandMacro> dictionary = this.wipMacroByPlayer;
			IPlayer player2 = args.Caller.Player;
			dictionary.Remove(((player2 != null) ? player2.PlayerUID : null) ?? "_console");
			return TextCommandResult.Success(Lang.Get("Ok, command created. You can use it now.", Array.Empty<object>()), null);
		}

		private TextCommandResult showWipMacro(TextCommandCallingArgs args)
		{
			ServerCommandMacro macro = this.getWipMacro(args.Caller, false);
			if (macro != null)
			{
				return TextCommandResult.Success(Lang.Get("Name: {0}\nDescription: {1}\nRequired privilege: {2}\nCommands: {3}", new object[] { macro.Name, macro.Syntax, macro.Description, macro.Privilege, macro.Commands }), null);
			}
			return TextCommandResult.Error(Lang.Get("No macro in wip", Array.Empty<object>()), "nomacroinwip");
		}

		private TextCommandResult showMacro(TextCommandCallingArgs args)
		{
			string name = (string)args[0];
			ServerCommandMacro macro;
			if (this.ServerCommmandMacros.TryGetValue(name, out macro))
			{
				return TextCommandResult.Success(Lang.Get("Name: {0}\nDescription: {1}\nRequired privilege: {2}\nCommands: {3}", new object[] { macro.Name, macro.Syntax, macro.Description, macro.Privilege, macro.Commands }), null);
			}
			return TextCommandResult.Error(Lang.Get("No such macro found", Array.Empty<object>()), "notfound");
		}

		private TextCommandResult listMacros(TextCommandCallingArgs args)
		{
			if (this.ServerCommmandMacros.Count > 0)
			{
				StringBuilder macrosList = new StringBuilder();
				foreach (ServerCommandMacro macro in this.ServerCommmandMacros.Values)
				{
					macrosList.AppendLine(string.Concat(new string[] { "  /", macro.Name, " ", macro.Syntax, " - ", macro.Description }));
				}
				return TextCommandResult.Success(Lang.Get("{0}Type /macro show [name] to see more info about a particular macro", new object[] { macrosList.ToString() }), null);
			}
			return TextCommandResult.Error("No macros defined on this server", "nomacros");
		}

		private TextCommandResult deleteMacro(TextCommandCallingArgs args)
		{
			string name = (string)args[0];
			ServerCommandMacro macro;
			if (this.ServerCommmandMacros.TryGetValue(name, out macro))
			{
				this.ServerCommmandMacros.Remove(macro.Name);
				this.server.api.commandapi.UnregisterCommand(macro.Name);
				this.SaveMacros();
				return TextCommandResult.Success("Ok, macro deleted", null);
			}
			return TextCommandResult.Error("No such macro found", "nosuchmacro");
		}

		private TextCommandResult addCmd(TextCommandCallingArgs args, bool clear)
		{
			ServerCommandMacro macro = this.getWipMacro(args.Caller, true);
			if (clear)
			{
				macro.Commands = "";
			}
			ServerCommandMacro serverCommandMacro = macro;
			serverCommandMacro.Commands += (string)args[0];
			ServerCommandMacro serverCommandMacro2 = macro;
			serverCommandMacro2.Commands += "\n";
			return TextCommandResult.Success(Lang.Get("Ok, command added.", Array.Empty<object>()), null);
		}

		private ServerCommandMacro getWipMacro(Caller caller, bool createIfNotExists)
		{
			IPlayer player = caller.Player;
			string key = ((player != null) ? player.PlayerUID : null) ?? "_console";
			ServerCommandMacro macro;
			if (this.wipMacroByPlayer.TryGetValue(key, out macro))
			{
				return macro;
			}
			if (createIfNotExists)
			{
				macro = new ServerCommandMacro();
				return this.wipMacroByPlayer[key] = macro;
			}
			return null;
		}

		private void OnMacro(string name, TextCommandCallingArgs args, Action<TextCommandResult> onCommandComplete = null)
		{
			if (!this.ServerCommmandMacros.ContainsKey(name))
			{
				onCommandComplete(TextCommandResult.Error("No such macro found", "nosuchmacro"));
			}
			ServerCommandMacro macro = this.ServerCommmandMacros[name];
			string[] commands = macro.Commands.Split('\n', StringSplitOptions.None);
			int success = 0;
			for (int i = 0; i < commands.Length; i++)
			{
				int index = i;
				string message = commands[i];
				for (int j = 0; j < args.RawArgs.Length; j++)
				{
					message = message.Replace("{param" + (j + 1).ToString() + "}", args.RawArgs[j]);
				}
				message = Regex.Replace(message, "{param\\d+}", "");
				if (message.Length != 0)
				{
					string[] ss = message.Split(new char[] { ' ' });
					string command = ss[0].Replace("/", "");
					string argument = ((message.IndexOf(' ') < 0) ? "" : message.Substring(message.IndexOf(' ') + 1));
					this.server.api.ChatCommands.Execute(command, new TextCommandCallingArgs
					{
						Caller = args.Caller,
						RawArgs = new CmdArgs(argument)
					}, delegate(TextCommandResult result)
					{
						if (result.Status == EnumCommandStatus.Success)
						{
							int success2 = success;
							success = success2 + 1;
						}
						if (index == command.Length - 1)
						{
							onCommandComplete(TextCommandResult.Success(Lang.Get("Macro executed. {0}/{1} commands successful.", new object[] { success, commands.Length }), null));
						}
					});
				}
			}
		}

		public void LoadMacros()
		{
			string filename = "servermacros.json";
			if (!File.Exists(Path.Combine(GamePaths.Config, filename)))
			{
				return;
			}
			try
			{
				List<ServerCommandMacro> macros = null;
				using (TextReader textReader = new StreamReader(Path.Combine(GamePaths.Config, filename)))
				{
					macros = JsonConvert.DeserializeObject<List<ServerCommandMacro>>(textReader.ReadToEnd());
					textReader.Close();
				}
				foreach (ServerCommandMacro macro in macros)
				{
					this.ServerCommmandMacros[macro.Name] = macro;
					this.RegisterMacro(macro);
				}
				ServerMain.Logger.Notification("{0} Macros loaded", new object[] { macros.Count });
			}
			catch (Exception e)
			{
				ServerMain.Logger.Error("Failed loading {0}:", new object[] { filename });
				ServerMain.Logger.Error(e);
			}
		}

		private void RegisterMacro(ServerCommandMacro macro)
		{
			this.server.api.ChatCommands.Create(macro.Name).WithDesc(macro.Description).HandleWith(delegate(TextCommandCallingArgs args)
			{
				this.OnMacro(macro.Name, args, null);
				return TextCommandResult.Deferred;
			})
				.RequiresPrivilege(macro.Privilege);
		}

		public void SaveMacros()
		{
			StreamWriter streamWriter = new StreamWriter(Path.Combine(GamePaths.Config, "servermacros.json"));
			streamWriter.Write(JsonConvert.SerializeObject(this.ServerCommmandMacros.Values, Formatting.Indented));
			streamWriter.Close();
		}

		private Dictionary<string, ServerCommandMacro> wipMacroByPlayer = new Dictionary<string, ServerCommandMacro>();

		private Dictionary<string, ServerCommandMacro> ServerCommmandMacros = new Dictionary<string, ServerCommandMacro>();
	}
}
