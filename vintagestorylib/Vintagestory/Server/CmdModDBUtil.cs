using System;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.ModDb;

namespace Vintagestory.Server
{
	internal class CmdModDBUtil
	{
		public CmdModDBUtil(ServerMain server)
		{
			this.server = server;
			this.modDbUtil = new ModDbUtil(server.api, server.Config.ModDbUrl, GamePaths.DataPathMods);
			CommandArgumentParsers parsers = server.api.ChatCommands.Parsers;
			server.api.commandapi.Create("moddb").RequiresPrivilege(Privilege.controlserver).WithDescription("ModDB utility. To install and remove mods.")
				.WithPreCondition(new CommandPreconditionDelegate(this.OnPrecondition))
				.BeginSubCommand("install")
				.WithDescription("Install the specified mod")
				.WithArgs(new ICommandArgumentParser[]
				{
					parsers.Word("modid"),
					parsers.OptionalWord("forGameVersion")
				})
				.HandleWith((TextCommandCallingArgs args) => this.handleTwoArgs(args, new Action<string, string, Action<string>>(this.modDbUtil.onInstallCommand)))
				.EndSubCommand()
				.BeginSubCommand("remove")
				.WithDescription("Uninstall the specified mod")
				.WithArgs(new ICommandArgumentParser[] { parsers.Word("modid") })
				.HandleWith((TextCommandCallingArgs args) => this.handleOneArg(args, new Action<string, Action<string>>(this.modDbUtil.onRemoveCommand)))
				.EndSubCommand()
				.BeginSubCommand("list")
				.WithDescription("List all installed mods")
				.HandleWith(new OnCommandDelegate(this.handleList))
				.EndSubCommand()
				.BeginSubCommand("search")
				.WithDescription("Search for a mod, filtered for the current game version only")
				.WithArgs(new ICommandArgumentParser[] { parsers.Word("modid") })
				.HandleWith((TextCommandCallingArgs args) => this.handleOneArg(args, new Action<string, Action<string>>(this.modDbUtil.onSearchCommand)))
				.EndSubCommand()
				.BeginSubCommand("searchcompatible")
				.WithAlias(new string[] { "searchc" })
				.WithDescription("Search for a mod, filtered for game versions compatible with the current version")
				.WithArgs(new ICommandArgumentParser[] { parsers.Word("modid") })
				.HandleWith((TextCommandCallingArgs args) => this.handleOneArg(args, new Action<string, Action<string>>(this.modDbUtil.onSearchCompatibleCommand)))
				.EndSubCommand()
				.BeginSubCommand("searchfor")
				.WithDescription("Search for a mod, filtered for the specified game version only")
				.WithArgs(new ICommandArgumentParser[]
				{
					parsers.Word("version"),
					parsers.Word("modid")
				})
				.HandleWith((TextCommandCallingArgs args) => this.handleTwoArgs(args, new Action<string, string, Action<string>>(this.modDbUtil.onSearchforCommand)))
				.EndSubCommand()
				.BeginSubCommand("searchforc")
				.WithDescription("Search for a mod, filtered for game versions compatible with the specified version")
				.WithArgs(new ICommandArgumentParser[]
				{
					parsers.Word("version"),
					parsers.Word("modid")
				})
				.HandleWith((TextCommandCallingArgs args) => this.handleTwoArgs(args, new Action<string, string, Action<string>>(this.modDbUtil.onSearchforAndCompatibleCommand)))
				.EndSubCommand()
				.Validate();
		}

		private TextCommandResult OnPrecondition(TextCommandCallingArgs args)
		{
			if (!this.server.Config.HostedMode || (this.server.Config.HostedMode && this.server.Config.HostedModeAllowMods))
			{
				return TextCommandResult.Success("", null);
			}
			return TextCommandResult.Error("Command not available. Disabled probably by the host.", "");
		}

		private TextCommandResult handleOneArg(TextCommandCallingArgs args, Action<string, Action<string>> modDbCommand)
		{
			string result = this.modDbUtil.preConsoleCommand();
			if (result != null)
			{
				return TextCommandResult.Error(result, "");
			}
			modDbCommand((string)args[0], delegate(string response)
			{
				this.server.SendMessage(args.Caller, response, EnumChatType.CommandSuccess, null);
			});
			return TextCommandResult.Deferred;
		}

		private TextCommandResult handleTwoArgs(TextCommandCallingArgs args, Action<string, string, Action<string>> modDbCommand)
		{
			string result = this.modDbUtil.preConsoleCommand();
			if (result != null)
			{
				return TextCommandResult.Error(result, "");
			}
			modDbCommand((string)args[0], (string)args[1], delegate(string response)
			{
				this.server.SendMessage(args.Caller, response, EnumChatType.CommandSuccess, null);
			});
			return TextCommandResult.Deferred;
		}

		private TextCommandResult handleList(TextCommandCallingArgs args)
		{
			string result = this.modDbUtil.preConsoleCommand();
			if (result != null)
			{
				return TextCommandResult.Error(result, "");
			}
			this.modDbUtil.onListCommand(delegate(string response)
			{
				this.server.SendMessage(args.Caller, response, EnumChatType.CommandSuccess, null);
			});
			return TextCommandResult.Deferred;
		}

		private ServerMain server;

		private ModDbUtil modDbUtil;
	}
}
