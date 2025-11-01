using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.Common
{
	public class ChatCommandImpl : IChatCommand
	{
		private event CommandPreconditionDelegate _precond;

		public bool Incomplete
		{
			get
			{
				return this.name == null || this.GetPrivilege() == null;
			}
		}

		public List<string> Aliases
		{
			get
			{
				return this.aliases;
			}
		}

		public List<string> RootAliases
		{
			get
			{
				return this.rootAliases;
			}
		}

		public string CommandPrefix
		{
			get
			{
				return this._cmdapi.CommandPrefix;
			}
		}

		public string Name
		{
			get
			{
				return this.name;
			}
		}

		public string Description
		{
			get
			{
				return this.description;
			}
		}

		public string AdditionalInformation
		{
			get
			{
				return this.additionalInformation;
			}
		}

		public string[] Examples
		{
			get
			{
				return this.examples;
			}
		}

		public string GetPrivilege()
		{
			string text;
			if ((text = this.privilege) == null)
			{
				ChatCommandImpl parent = this._parent;
				if (parent == null)
				{
					return null;
				}
				text = parent.GetPrivilege();
			}
			return text;
		}

		public string FullName
		{
			get
			{
				if (this._parent != null)
				{
					return this._parent.name + " " + this.name;
				}
				return this._cmdapi.CommandPrefix + this.name;
			}
		}

		public string GetFullName(string alias, bool isRootAlias = false)
		{
			if (this._parent == null || isRootAlias)
			{
				return this._cmdapi.CommandPrefix + alias;
			}
			if (alias != this.name)
			{
				return this._cmdapi.CommandPrefix + alias;
			}
			return this._parent.name + " " + alias;
		}

		public IChatCommand this[string name]
		{
			get
			{
				return this.subCommands[name];
			}
		}

		public ChatCommandImpl(ChatCommandApi cmdapi, string name = null, ChatCommandImpl parent = null)
		{
			this._cmdapi = cmdapi;
			this.name = name;
			this._parent = parent;
		}

		public IChatCommand EndSubCommand()
		{
			if (this._parent == null)
			{
				throw new InvalidOperationException("Not inside a subcommand");
			}
			return this._parent;
		}

		public IChatCommand HandleWith(OnCommandDelegate handler)
		{
			this.handler = handler;
			return this;
		}

		public IChatCommand RequiresPrivilege(string privilege)
		{
			this.privilege = privilege;
			return this;
		}

		public IChatCommand WithDescription(string description)
		{
			this.description = description;
			return this;
		}

		public IChatCommand WithAdditionalInformation(string text)
		{
			this.additionalInformation = text;
			return this;
		}

		public IChatCommand WithName(string commandName)
		{
			if (this._parent != null)
			{
				throw new InvalidOperationException("This method is not available for subcommands");
			}
			if (this._cmdapi.ichatCommands.ContainsKey(commandName))
			{
				throw new InvalidOperationException("Command with such name already exists");
			}
			this.name = commandName;
			this._cmdapi.ichatCommands[commandName] = this;
			return this;
		}

		public IChatCommand WithRootAlias(string commandName)
		{
			string lowerInvariant = commandName.ToLowerInvariant();
			if (this.rootAliases == null)
			{
				this.rootAliases = new List<string>();
			}
			this.rootAliases.Add(lowerInvariant);
			this._cmdapi.ichatCommands[lowerInvariant] = this;
			return this;
		}

		public IChatCommand BeginSub(string name)
		{
			return this.BeginSubCommand(name);
		}

		public IChatCommand EndSub()
		{
			return this.EndSubCommand();
		}

		public IChatCommand BeginSubCommand(string name)
		{
			name = name.ToLowerInvariant();
			IChatCommand command;
			if (this.subCommands.TryGetValue(name, out command))
			{
				return command;
			}
			return this.subCommands[name] = new ChatCommandImpl(this._cmdapi, name, this);
		}

		public IChatCommand BeginSubCommands(params string[] names)
		{
			names[0] = names[0].ToLowerInvariant();
			IChatCommand chatCommand2;
			if (!this.subCommands.ContainsKey(names[0]))
			{
				IChatCommand chatCommand = new ChatCommandImpl(this._cmdapi, names[0], this);
				chatCommand2 = chatCommand;
			}
			else
			{
				chatCommand2 = this.subCommands[names[0]];
			}
			ChatCommandImpl cmd = chatCommand2 as ChatCommandImpl;
			ChatCommandImpl chatCommandImpl = cmd;
			if (chatCommandImpl.aliases == null)
			{
				chatCommandImpl.aliases = new List<string>();
			}
			foreach (string name in names)
			{
				this.subCommands[name.ToLowerInvariant()] = cmd;
			}
			foreach (string name2 in RuntimeHelpers.GetSubArray<string>(names, Range.StartAt(1)))
			{
				cmd.Aliases.Add(name2.ToLowerInvariant());
			}
			return cmd;
		}

		public IChatCommand WithSubCommand(string name, string desc, OnCommandDelegate handler, params ICommandArgumentParser[] parsers)
		{
			this.BeginSubCommand(name).WithName(name).WithDescription(desc)
				.WithArgs(parsers)
				.HandleWith(handler)
				.EndSubCommand();
			return this;
		}

		public IChatCommand WithArgs(params ICommandArgumentParser[] parsers)
		{
			this._parsers = parsers;
			return this;
		}

		public void Execute(TextCommandCallingArgs callargs, Action<TextCommandResult> onCommandComplete = null)
		{
			callargs.Command = this;
			if (this._precond != null)
			{
				Delegate[] invocationList = this._precond.GetInvocationList();
				for (int j = 0; j < invocationList.Length; j++)
				{
					TextCommandResult res = ((CommandPreconditionDelegate)invocationList[j])(callargs);
					if (res.Status == EnumCommandStatus.Error)
					{
						if (onCommandComplete != null)
						{
							onCommandComplete(res);
						}
						return;
					}
				}
			}
			Dictionary<int, AsyncParseResults> asyncParseResults = null;
			int deferredCount = 0;
			bool allParsed = false;
			ICommandArgumentParser parserResultDependedOnSubsequent = null;
			for (int i = 0; i < this._parsers.Length; i++)
			{
				int index = i;
				ICommandArgumentParser parser = this._parsers[i];
				parser.PreProcess(callargs);
				if (parser.IsMissing)
				{
					if (parser.IsMandatoryArg)
					{
						Action<TextCommandResult> onCommandComplete2 = onCommandComplete;
						if (onCommandComplete2 == null)
						{
							return;
						}
						onCommandComplete2(TextCommandResult.Error(Lang.Get("command-missingarg", new object[]
						{
							i + 1,
							Lang.Get(parser.ArgumentName, Array.Empty<object>())
						}), "missingarg"));
						return;
					}
				}
				else
				{
					EnumParseResult status = parser.TryProcess(callargs, delegate(AsyncParseResults data)
					{
						int deferredCount2 = deferredCount;
						deferredCount = deferredCount2 - 1;
						if (asyncParseResults == null)
						{
							asyncParseResults = new Dictionary<int, AsyncParseResults>();
						}
						asyncParseResults[index] = data;
						if ((deferredCount == 0) & allParsed)
						{
							this.CallHandler(callargs, onCommandComplete, asyncParseResults);
						}
					});
					if (status != EnumParseResult.Good)
					{
						if (status == EnumParseResult.Deferred)
						{
							int j = deferredCount;
							deferredCount = j + 1;
						}
						if (parserResultDependedOnSubsequent != null)
						{
							Action<TextCommandResult> onCommandComplete3 = onCommandComplete;
							if (onCommandComplete3 != null)
							{
								onCommandComplete3(TextCommandResult.Error(Lang.Get("command-argumenterror1", new object[]
								{
									parserResultDependedOnSubsequent.ArgumentName,
									Lang.Get(parserResultDependedOnSubsequent.LastErrorMessage ?? "unknown error", Array.Empty<object>())
								}), "wrongarg"));
							}
						}
						if (status == EnumParseResult.Bad)
						{
							Action<TextCommandResult> onCommandComplete4 = onCommandComplete;
							if (onCommandComplete4 == null)
							{
								return;
							}
							onCommandComplete4(TextCommandResult.Error(Lang.Get("command-argumenterror2", new object[]
							{
								i + 1,
								parser.ArgumentName,
								Lang.Get(parser.LastErrorMessage ?? "unknown error", Array.Empty<object>())
							}), "wrongarg"));
							return;
						}
						else if (status == EnumParseResult.DependsOnSubsequent)
						{
							if (parserResultDependedOnSubsequent != null)
							{
								return;
							}
							parserResultDependedOnSubsequent = parser;
						}
					}
				}
			}
			callargs.Parsers.AddRange(this._parsers);
			allParsed = true;
			if (deferredCount == 0)
			{
				this.CallHandler(callargs, onCommandComplete, asyncParseResults);
				return;
			}
			Action<TextCommandResult> onCommandComplete5 = onCommandComplete;
			if (onCommandComplete5 == null)
			{
				return;
			}
			onCommandComplete5(TextCommandResult.Deferred);
		}

		private void CallHandler(TextCommandCallingArgs callargs, Action<TextCommandResult> onCommandComplete = null, Dictionary<int, AsyncParseResults> asyncParseResults = null)
		{
			if (asyncParseResults != null)
			{
				foreach (KeyValuePair<int, AsyncParseResults> val in asyncParseResults)
				{
					int index = val.Key;
					if (val.Value.Status == EnumParseResultStatus.Error)
					{
						if (onCommandComplete == null)
						{
							return;
						}
						onCommandComplete(TextCommandResult.Error(Lang.Get("Error in argument {0} ({1}): {2}", new object[]
						{
							index + 1,
							Lang.Get(this._parsers[index].ArgumentName, Array.Empty<object>()),
							Lang.Get(this._parsers[index].LastErrorMessage, Array.Empty<object>())
						}), "wrongarg"));
						return;
					}
					else
					{
						callargs.Parsers[index].SetValue(val.Value.Data);
					}
				}
			}
			string text = callargs.RawArgs.PeekWord(null);
			string subcmd = ((text != null) ? text.ToLowerInvariant() : null);
			if (subcmd != null && this.subCommands.ContainsKey(subcmd))
			{
				callargs.SubCmdCode = callargs.RawArgs.PopWord(null);
				this.subCommands[subcmd].Execute(callargs, onCommandComplete);
				return;
			}
			if (!callargs.Caller.HasPrivilege(this.GetPrivilege()))
			{
				if (onCommandComplete != null)
				{
					onCommandComplete(new TextCommandResult
					{
						Status = EnumCommandStatus.Error,
						ErrorCode = "noprivilege",
						StatusMessage = Lang.Get("Sorry, you don't have the privilege to use this command", Array.Empty<object>())
					});
				}
				return;
			}
			if (this.handler == null)
			{
				if (this.subCommands.Count > 0)
				{
					List<string> subcchat = new List<string>();
					foreach (string val2 in this.subCommands.Keys)
					{
						subcchat.Add(string.Format("<a href=\"chattype://{0}\">{1}</a>", callargs.Command.FullName + " " + val2, val2));
					}
					if (onCommandComplete != null)
					{
						onCommandComplete(TextCommandResult.Error("Choose a subcommand: " + string.Join(", ", subcchat), "selectsubcommand"));
					}
					return;
				}
				if (onCommandComplete != null)
				{
					onCommandComplete(TextCommandResult.Error("Insufficently set up command - no handlers or subcommands set up", "incompletecommandsetup"));
				}
				return;
			}
			else
			{
				if (callargs.RawArgs.Length > 0 && callargs.ArgCount >= 0 && !this.ignoreAdditonalArguments)
				{
					if (this._parent == null)
					{
						if (this.subCommands.Count > 0)
						{
							if (onCommandComplete != null)
							{
								onCommandComplete(TextCommandResult.Error(Lang.Get("Command {0}, unrecognised subcommand: {1}", new object[]
								{
									this._cmdapi.CommandPrefix + this.name,
									subcmd
								}), "wrongargcount"));
								return;
							}
						}
						else if (onCommandComplete != null)
						{
							onCommandComplete(TextCommandResult.Error(Lang.Get("Command {0}, too many arguments", new object[] { this._cmdapi.CommandPrefix + this.name }), "wrongargcount"));
							return;
						}
					}
					else if (onCommandComplete != null)
					{
						onCommandComplete(TextCommandResult.Error(Lang.Get("Subcommand {0}, too many arguments", new object[] { this.name }), "wrongargcount"));
					}
					return;
				}
				TextCommandResult results = this.handler(callargs);
				if (onCommandComplete != null)
				{
					onCommandComplete(results);
				}
			}
		}

		public IChatCommand WithPreCondition(CommandPreconditionDelegate precond)
		{
			this._precond += precond;
			return this;
		}

		public IChatCommand WithAlias(params string[] names)
		{
			if (this.aliases == null)
			{
				this.aliases = new List<string>();
			}
			for (int i = 0; i < names.Length; i++)
			{
				string lowerInvariant = names[i].ToLowerInvariant();
				if (this._parent == null)
				{
					this._cmdapi.ichatCommands[lowerInvariant] = this;
				}
				else
				{
					this._parent.subCommands[lowerInvariant] = this;
				}
				this.aliases.Add(lowerInvariant);
			}
			return this;
		}

		public IChatCommand GroupWith(params string[] name)
		{
			this.WithAlias(name);
			return this;
		}

		public IChatCommand WithExamples(params string[] examples)
		{
			this.examples = examples;
			return this;
		}

		public IChatCommand RequiresPlayer()
		{
			this._precond += delegate(TextCommandCallingArgs args)
			{
				if (args.Caller.Player == null)
				{
					return TextCommandResult.Error("Caller must be player", "");
				}
				return TextCommandResult.Success("", null);
			};
			return this;
		}

		public void Validate()
		{
			if (this._parent != null)
			{
				throw new Exception("Validate not called from the root command, likely missing EndSub()");
			}
			this.ValidateRecursive();
		}

		private void ValidateRecursive()
		{
			if (string.IsNullOrEmpty(this.description))
			{
				throw new Exception("Command " + this.CallSyntax + ": Description not set");
			}
			if (string.IsNullOrEmpty(this.name))
			{
				throw new Exception("Command " + this.CallSyntax + ": Name not set");
			}
			if (!this.AnyPrivilegeSet)
			{
				throw new Exception("Command " + this.CallSyntax + ": Privilege not set for subcommand or any parent command");
			}
			if (this.subCommands.Count == 0 && this.handler == null)
			{
				throw new Exception("Command " + this.CallSyntax + ": No handler or subcommands defined");
			}
			foreach (KeyValuePair<string, IChatCommand> en in this.subCommands)
			{
				(en.Value as ChatCommandImpl).ValidateRecursive();
			}
		}

		public bool AnyPrivilegeSet
		{
			get
			{
				return !string.IsNullOrEmpty(this.GetPrivilege());
			}
		}

		public bool IsAvailableTo(Caller caller)
		{
			return caller.HasPrivilege(this.GetPrivilege());
		}

		public IChatCommand IgnoreAdditionalArgs()
		{
			this.ignoreAdditonalArguments = true;
			return this;
		}

		public string GetFullSyntaxHandbook(Caller caller, string indent = "", bool isRootAlias = false)
		{
			StringBuilder text = new StringBuilder();
			Dictionary<string, IChatCommand> subcommands = IChatCommandApi.GetOrdered(this.AllSubcommands);
			if (this.handler != null && (isRootAlias || this._parent == null))
			{
				if (this.RootAliases != null)
				{
					foreach (string alias in this.RootAliases)
					{
						text.AppendLine(indent + string.Format("<a href=\"chattype://{0}\">{1}</a>", this.GetCallSyntaxUnformatted(alias, true), this.GetCallSyntax(alias, true)));
					}
				}
				text.AppendLine(indent + string.Format("<a href=\"chattype://{0}\">{1}</a>", this.CallSyntaxUnformatted, this.CallSyntax));
			}
			if (this.Description != null)
			{
				this.AddVerticalSpace(text);
				text.AppendLine(indent + this.Description);
			}
			if (this.AdditionalInformation != null)
			{
				this.AddVerticalSpace(text);
				text.AppendLine(indent + this.AdditionalInformation);
			}
			this.AddSyntaxExplanation(text, indent);
			if (this.Examples != null && this.Examples.Length != 0)
			{
				this.AddVerticalSpace(text);
				text.AppendLine(indent + ((this.Examples.Length > 1) ? "Examples:" : "Example:"));
				foreach (string ex in this.Examples)
				{
					text.AppendLine(indent + ex);
				}
			}
			if (subcommands.Count > 0 && !isRootAlias)
			{
				this.AddVerticalSpace(text);
				ChatCommandImpl.WriteCommandsListHandbook(text, subcommands, caller, indent);
			}
			this.AddVerticalSpace(text);
			return text.ToString();
		}

		public string GetFullSyntaxConsole(Caller caller)
		{
			StringBuilder text = new StringBuilder();
			Dictionary<string, IChatCommand> subcommands = this.AllSubcommands;
			if (subcommands.Count > 0)
			{
				text.AppendLine("Available subcommands:");
				ChatCommandImpl.WriteCommandsList(text, subcommands, caller, true);
				text.AppendLine();
				text.AppendLine("Type <code>/help " + this.CallSyntax.Substring(1) + " &lt;<i>subcommand_name</i>&gt;</code> for help on a specific subcommand");
			}
			else
			{
				text.AppendLine();
				if (this.Description != null)
				{
					text.AppendLine(this.Description);
				}
				if (this.AdditionalInformation != null)
				{
					text.AppendLine(this.AdditionalInformation);
				}
				text.AppendLine();
				text.AppendLine("Usage: <code>");
				text.Append(this.CallSyntax);
				text.Append("</code>");
				this.AddSyntaxExplanation(text, "");
				if (this.Examples != null && this.Examples.Length != 0)
				{
					text.AppendLine((this.Examples.Length > 1) ? "Examples:" : "Example:");
					foreach (string ex in this.Examples)
					{
						text.AppendLine(ex);
					}
				}
			}
			return text.ToString();
		}

		public static void WriteCommandsListHandbook(StringBuilder text, Dictionary<string, IChatCommand> commands, Caller caller, string indent = "")
		{
			text.AppendLine();
			foreach (ChatCommandImpl cm in commands.Values.Distinct(ChatCommandComparer.Comparer).Cast<ChatCommandImpl>())
			{
				if (caller == null || cm.IsAvailableTo(caller))
				{
					if (cm.AllSubcommands.Count > 0 && cm.handler == null)
					{
						if (cm.RootAliases != null)
						{
							foreach (string alias in cm.RootAliases)
							{
								text.AppendLine(indent + "<strong>" + cm.GetCallSyntax(alias, true) + "</strong>");
							}
						}
						if (cm.Aliases != null)
						{
							foreach (string alias2 in cm.Aliases)
							{
								text.AppendLine(indent + "<strong>" + cm.GetCallSyntax(alias2, false) + "</strong>");
							}
						}
						text.AppendLine(indent + "<strong>" + cm.CallSyntax + "</strong> ");
					}
					else
					{
						if (cm.RootAliases != null)
						{
							foreach (string alias3 in cm.RootAliases)
							{
								text.AppendLine(indent + string.Format("<a href=\"chattype://{0}\">{1}</a>", cm.GetCallSyntaxUnformatted(alias3, true), cm.GetCallSyntax(alias3, true).TrimEnd()));
							}
						}
						if (cm.Aliases != null)
						{
							foreach (string alias4 in cm.Aliases)
							{
								text.AppendLine(indent + string.Format("<a href=\"chattype://{0}\">{1}</a>", cm.GetCallSyntaxUnformatted(alias4, false), cm.GetCallSyntax(alias4, false).TrimEnd()));
							}
						}
						text.AppendLine(indent + string.Format("<a href=\"chattype://{0}\">{1}</a>", cm.CallSyntaxUnformatted, cm.CallSyntax));
					}
					text.Append(cm.GetFullSyntaxHandbook(caller, indent + "   ", false));
				}
			}
		}

		public static void WriteCommandsList(StringBuilder text, Dictionary<string, IChatCommand> commands, Caller caller, bool isSubCommand = false)
		{
			foreach (KeyValuePair<string, IChatCommand> val in commands)
			{
				IChatCommand cm = val.Value;
				if (caller == null || cm.IsAvailableTo(caller))
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
					text.AppendLine("<code>" + cm.GetCallSyntax(val.Key, !isSubCommand).TrimEnd() + "</code> :  " + desc);
				}
			}
		}

		public string GetCallSyntax(string name, bool isRootAlias = false)
		{
			StringBuilder sb = new StringBuilder();
			if (isRootAlias)
			{
				sb.Append(this._cmdapi.CommandPrefix);
			}
			else
			{
				sb.Append((this._parent == null) ? this._cmdapi.CommandPrefix : this._parent.CallSyntax);
			}
			sb.Append(name);
			sb.Append(" ");
			this.AddParameterSyntax(sb, "");
			return sb.ToString();
		}

		public string GetCallSyntaxUnformatted(string name, bool isRootAlias = false)
		{
			StringBuilder sb = new StringBuilder();
			if (isRootAlias)
			{
				sb.Append(this._cmdapi.CommandPrefix);
			}
			else
			{
				sb.Append((this._parent == null) ? this._cmdapi.CommandPrefix : this._parent.CallSyntaxUnformatted);
			}
			sb.Append(name);
			sb.Append(" ");
			this.AddParameterSyntaxUnformatted(sb, "");
			return sb.ToString();
		}

		public string CallSyntax
		{
			get
			{
				StringBuilder sb = new StringBuilder();
				if (this._parent == null)
				{
					sb.Append(this._cmdapi.CommandPrefix);
				}
				else
				{
					sb.Append(this._parent.CallSyntax);
				}
				sb.Append(this.Name);
				sb.Append(" ");
				this.AddParameterSyntax(sb, "");
				return sb.ToString();
			}
		}

		public string CallSyntaxUnformatted
		{
			get
			{
				StringBuilder sb = new StringBuilder();
				if (this._parent == null)
				{
					sb.Append(this._cmdapi.CommandPrefix);
				}
				else
				{
					sb.Append(this._parent.CallSyntaxUnformatted);
				}
				sb.Append(this.Name);
				sb.Append(" ");
				this.AddParameterSyntaxUnformatted(sb, "");
				return sb.ToString();
			}
		}

		public void AddParameterSyntax(StringBuilder sb, string indent)
		{
			foreach (ArgumentParserBase p in this._parsers)
			{
				sb.Append(p.GetSyntax());
				sb.Append(" ");
			}
		}

		public void AddParameterSyntaxUnformatted(StringBuilder sb, string indent)
		{
			foreach (ArgumentParserBase p in this._parsers)
			{
				sb.Append(p.GetSyntaxUnformatted());
				sb.Append(" ");
			}
		}

		public void AddSyntaxExplanation(StringBuilder sb, string indent)
		{
			if (this._parsers.Length == 0)
			{
				return;
			}
			bool first = true;
			sb.Append("<font scale=\"80%\">");
			ICommandArgumentParser[] parsers = this._parsers;
			for (int i = 0; i < parsers.Length; i++)
			{
				string explanation = ((ArgumentParserBase)parsers[i]).GetSyntaxExplanation(indent);
				if (explanation != null)
				{
					if (first)
					{
						sb.AppendLine();
						first = false;
					}
					sb.AppendLine(explanation);
				}
			}
			sb.Append("</font>");
		}

		private void AddVerticalSpace(StringBuilder text)
		{
			if (text.Length == 0)
			{
				return;
			}
			text.Append("\n");
		}

		public IEnumerable<IChatCommand> Subcommands
		{
			get
			{
				return this.subCommands.Values;
			}
		}

		public Dictionary<string, IChatCommand> AllSubcommands
		{
			get
			{
				return this.subCommands;
			}
		}

		private ChatCommandApi _cmdapi;

		private ChatCommandImpl _parent;

		protected bool ignoreAdditonalArguments;

		protected string name;

		protected string[] examples;

		protected List<string> aliases;

		protected List<string> rootAliases;

		protected string privilege;

		protected string description;

		protected string additionalInformation;

		protected OnCommandDelegate handler;

		protected Dictionary<string, IChatCommand> subCommands = new Dictionary<string, IChatCommand>(StringComparer.OrdinalIgnoreCase);

		private ICommandArgumentParser[] _parsers = Array.Empty<ICommandArgumentParser>();
	}
}
