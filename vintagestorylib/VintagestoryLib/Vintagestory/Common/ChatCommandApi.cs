using System;
using System.Collections;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace Vintagestory.Common
{
	public class ChatCommandApi : IChatCommandApi, IEnumerable<KeyValuePair<string, IChatCommand>>, IEnumerable
	{
		public string CommandPrefix
		{
			get
			{
				if (this.api.Side != EnumAppSide.Client)
				{
					return ChatCommandApi.ServerCommandPrefix;
				}
				return ChatCommandApi.ClientCommandPrefix;
			}
		}

		public CommandArgumentParsers Parsers
		{
			get
			{
				return this.parsers;
			}
		}

		public int Count
		{
			get
			{
				return this.ichatCommands.Count;
			}
		}

		public IEnumerator<IChatCommand> GetEnumerator()
		{
			ChatCommandApi.<GetEnumerator>d__9 <GetEnumerator>d__ = new ChatCommandApi.<GetEnumerator>d__9(0);
			<GetEnumerator>d__.<>4__this = this;
			return <GetEnumerator>d__;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.ichatCommands.GetEnumerator();
		}

		public IChatCommand this[string name]
		{
			get
			{
				return this.ichatCommands[name];
			}
		}

		public ChatCommandApi(ICoreAPI api)
		{
			this.api = api;
			this.parsers = new CommandArgumentParsers(api);
		}

		public IChatCommand Get(string name)
		{
			IChatCommand cmd;
			this.ichatCommands.TryGetValue(name, out cmd);
			return cmd;
		}

		public IChatCommand Create()
		{
			return new ChatCommandImpl(this, null, null);
		}

		public IChatCommand Create(string commandName)
		{
			return new ChatCommandImpl(this, null, null).WithName(commandName.ToLowerInvariant());
		}

		public IChatCommand GetOrCreate(string commandName)
		{
			commandName = commandName.ToLowerInvariant();
			return this.Get(commandName) ?? this.Create().WithName(commandName);
		}

		public IEnumerable<IChatCommand> ListAll()
		{
			return this.ichatCommands.Values;
		}

		public Dictionary<string, IChatCommand> AllSubcommands()
		{
			return this.ichatCommands;
		}

		public void Execute(string commandName, TextCommandCallingArgs args, Action<TextCommandResult> onCommandComplete = null)
		{
			commandName = commandName.ToLowerInvariant();
			IChatCommand cmd;
			if (!this.ichatCommands.TryGetValue(commandName, out cmd))
			{
				onCommandComplete(new TextCommandResult
				{
					Status = EnumCommandStatus.NoSuchCommand,
					ErrorCode = "nosuchcommand"
				});
				return;
			}
			if (this.api.Side == EnumAppSide.Server && cmd.Incomplete)
			{
				throw new InvalidOperationException("Programming error: Incomplete command - no name or required privilege has been set");
			}
			if (this.api.Side == EnumAppSide.Client && (cmd as ChatCommandImpl).AnyPrivilegeSet)
			{
				args.Caller.CallerPrivileges = null;
			}
			IServerPlayer serverPlayer = args.Caller.Player as IServerPlayer;
			args.LanguageCode = ((serverPlayer != null) ? serverPlayer.LanguageCode : null) ?? Lang.CurrentLocale;
			cmd.Execute(args, onCommandComplete);
		}

		public void ExecuteUnparsed(string message, TextCommandCallingArgs args, Action<TextCommandResult> onCommandComplete = null)
		{
			message = message.Substring(1);
			int argsStart = message.IndexOf(' ');
			string strargs;
			string command;
			if (argsStart > 0)
			{
				strargs = message.Substring(argsStart + 1);
				command = message.Substring(0, argsStart);
			}
			else
			{
				strargs = "";
				command = message;
			}
			args.RawArgs = new CmdArgs(strargs);
			this.Execute(command, args, onCommandComplete);
		}

		public void Execute(string commandName, IServerPlayer player, int groupId, string args, Action<TextCommandResult> onCommandComplete = null)
		{
			this.api.Logger.Audit("Handling command for {0} /{1} {2}", new object[] { player.PlayerName, commandName, args });
			string langCode = player.LanguageCode;
			try
			{
				string commandName2 = commandName;
				TextCommandCallingArgs textCommandCallingArgs = new TextCommandCallingArgs();
				Caller caller = new Caller();
				caller.Player = player;
				EntityPlayer entity = player.Entity;
				caller.Pos = ((entity != null) ? entity.Pos.XYZ : null);
				caller.FromChatGroupId = groupId;
				textCommandCallingArgs.Caller = caller;
				textCommandCallingArgs.RawArgs = new CmdArgs(args);
				this.Execute(commandName2, textCommandCallingArgs, delegate(TextCommandResult results)
				{
					if (results.StatusMessage != null && results.StatusMessage.Length > 0)
					{
						string message = results.StatusMessage;
						if (results.StatusMessage.IndexOf('\n') == -1)
						{
							message = ((results.MessageParams == null) ? Lang.GetL(langCode, results.StatusMessage, Array.Empty<object>()) : Lang.GetL(langCode, results.StatusMessage, results.MessageParams));
						}
						player.SendMessage(groupId, message, (results.Status == EnumCommandStatus.Success) ? EnumChatType.CommandSuccess : EnumChatType.CommandError, null);
					}
					if (results.Status == EnumCommandStatus.NoSuchCommand)
					{
						player.SendMessage(groupId, Lang.GetL(langCode, "No such command exists", Array.Empty<object>()), EnumChatType.CommandError, null);
						this.SuggestCommands(player, groupId, commandName);
					}
					if (results.Status == EnumCommandStatus.Error)
					{
						player.SendMessage(groupId, Lang.GetL(langCode, "For help, type <code>/help {0}</code>", new object[] { commandName }), EnumChatType.CommandError, null);
					}
					Action<TextCommandResult> onCommandComplete3 = onCommandComplete;
					if (onCommandComplete3 == null)
					{
						return;
					}
					onCommandComplete3(results);
				});
			}
			catch (Exception ex)
			{
				this.api.Logger.Error("Player {0}/{1} caused an exception through a command.", new object[] { player.PlayerName, player.PlayerUID });
				this.api.Logger.Error("Command: /{0} {1}", new object[] { commandName, args });
				this.api.Logger.Error(ex);
				string err = "An Exception was thrown while executing Command: {0}. Check error log for more detail.";
				player.SendMessage(groupId, Lang.GetL(langCode, err, new object[] { ex.Message }), EnumChatType.CommandError, null);
				Action<TextCommandResult> onCommandComplete2 = onCommandComplete;
				if (onCommandComplete2 != null)
				{
					onCommandComplete2(TextCommandResult.Error(err, "exception"));
				}
			}
		}

		public void Execute(string commandName, IClientPlayer player, int groupId, string args, Action<TextCommandResult> onCommandComplete = null)
		{
			this.Execute(commandName, new TextCommandCallingArgs
			{
				Caller = new Caller
				{
					Player = player,
					FromChatGroupId = groupId,
					CallerPrivileges = new string[] { "*" }
				},
				RawArgs = new CmdArgs(args)
			}, delegate(TextCommandResult results)
			{
				if (results.StatusMessage != null)
				{
					player.ShowChatNotification(Lang.Get(results.StatusMessage, Array.Empty<object>()));
				}
				if (results.Status == EnumCommandStatus.NoSuchCommand)
				{
					player.ShowChatNotification(Lang.Get("No such command exists", Array.Empty<object>()));
				}
				Action<TextCommandResult> onCommandComplete2 = onCommandComplete;
				if (onCommandComplete2 == null)
				{
					return;
				}
				onCommandComplete2(results);
			});
		}

		private void SuggestCommands(IServerPlayer player, int groupId, string commandName)
		{
			string similarCommand = null;
			int minDist = 99;
			foreach (KeyValuePair<string, IChatCommand> val in this.ichatCommands)
			{
				int distance = ChatCommandApi.LevenshteinDistance(val.Key, commandName);
				if (distance < 4 && distance < commandName.Length / 2 && minDist > distance)
				{
					similarCommand = val.Key;
					minDist = distance;
				}
			}
			if (similarCommand != null)
			{
				player.SendMessage(groupId, Lang.Get("command-suggestion", new object[] { similarCommand }), EnumChatType.CommandError, null);
			}
		}

		public static int LevenshteinDistance(string source1, string source2)
		{
			int source1Length = source1.Length;
			int source2Length = source2.Length;
			int[,] matrix = new int[source1Length + 1, source2Length + 1];
			if (source1Length == 0)
			{
				return source2Length;
			}
			if (source2Length == 0)
			{
				return source1Length;
			}
			int i = 0;
			while (i <= source1Length)
			{
				matrix[i, 0] = i++;
			}
			int j = 0;
			while (j <= source2Length)
			{
				matrix[0, j] = j++;
			}
			for (int k = 1; k <= source1Length; k++)
			{
				for (int l = 1; l <= source2Length; l++)
				{
					int cost = ((source2[l - 1] != source1[k - 1]) ? 1 : 0);
					matrix[k, l] = Math.Min(Math.Min(matrix[k - 1, l] + 1, matrix[k, l - 1] + 1), matrix[k - 1, l - 1] + cost);
				}
			}
			return matrix[source1Length, source2Length];
		}

		internal void UnregisterCommand(string name)
		{
			this.ichatCommands.Remove(name);
		}

		internal virtual bool RegisterCommand(string command, string descriptionMsg, string syntaxMsg, ClientChatCommandDelegate handler, string requiredPrivilege = null)
		{
			try
			{
				this.Create(command).WithDesc(descriptionMsg + "\nSyntax:" + syntaxMsg).RequiresPrivilege(requiredPrivilege)
					.WithArgs(new ICommandArgumentParser[] { this.parsers.Unparsed("legacy args", Array.Empty<string>()) })
					.HandleWith(delegate(TextCommandCallingArgs args)
					{
						handler(args.Caller.FromChatGroupId, args.RawArgs);
						return new TextCommandResult
						{
							Status = EnumCommandStatus.UnknownLegacy
						};
					});
			}
			catch (InvalidOperationException e)
			{
				this.api.Logger.Warning("Command {0}{1} already registered:", new object[]
				{
					ChatCommandApi.ClientCommandPrefix,
					command
				});
				this.api.Logger.Warning(e);
				return false;
			}
			return true;
		}

		internal virtual bool RegisterCommand(ChatCommand chatCommand)
		{
			try
			{
				this.Create(chatCommand.Command).WithDesc(chatCommand.Description + "\nSyntax:" + chatCommand.Syntax).RequiresPrivilege(chatCommand.RequiredPrivilege)
					.WithArgs(new ICommandArgumentParser[] { this.parsers.Unparsed("legacy args", Array.Empty<string>()) })
					.HandleWith(delegate(TextCommandCallingArgs args)
					{
						chatCommand.CallHandler(args.Caller.Player, args.Caller.FromChatGroupId, args.RawArgs);
						return new TextCommandResult
						{
							Status = EnumCommandStatus.UnknownLegacy
						};
					});
			}
			catch (InvalidOperationException e)
			{
				this.api.Logger.Warning("Command {0}{1} already registered:", new object[]
				{
					(chatCommand is ServerChatCommand) ? ChatCommandApi.ServerCommandPrefix : ChatCommandApi.ClientCommandPrefix,
					chatCommand.Command
				});
				this.api.Logger.Warning(e);
				return false;
			}
			return true;
		}

		[Obsolete("Better to directly use new ChatCommands api instead")]
		public bool RegisterCommand(string command, string descriptionMsg, string syntaxMsg, ServerChatCommandDelegate handler, string requiredPrivilege = null)
		{
			try
			{
				this.Create(command).WithDesc(descriptionMsg + "\nSyntax:" + syntaxMsg).RequiresPrivilege(requiredPrivilege)
					.WithArgs(new ICommandArgumentParser[] { this.parsers.Unparsed("legacy args", Array.Empty<string>()) })
					.HandleWith(delegate(TextCommandCallingArgs args)
					{
						handler(args.Caller.Player as IServerPlayer, args.Caller.FromChatGroupId, args.RawArgs);
						return new TextCommandResult
						{
							Status = EnumCommandStatus.UnknownLegacy
						};
					});
			}
			catch (InvalidOperationException e)
			{
				this.api.Logger.Warning("Command {0}{1} already registered:", new object[]
				{
					ChatCommandApi.ClientCommandPrefix,
					command
				});
				this.api.Logger.Warning(e);
				return false;
			}
			return true;
		}

		public bool RegisterCommand(string command, string descriptionMsg, string syntaxMsg, ClientChatCommandDelegate handler)
		{
			return this.RegisterCommand(command, descriptionMsg, syntaxMsg, handler, null);
		}

		IEnumerator<KeyValuePair<string, IChatCommand>> IEnumerable<KeyValuePair<string, IChatCommand>>.GetEnumerator()
		{
			return this.ichatCommands.GetEnumerator();
		}

		public static string ClientCommandPrefix = ".";

		public static string ServerCommandPrefix = "/";

		internal Dictionary<string, IChatCommand> ichatCommands = new Dictionary<string, IChatCommand>(StringComparer.OrdinalIgnoreCase);

		private ICoreAPI api;

		private CommandArgumentParsers parsers;
	}
}
