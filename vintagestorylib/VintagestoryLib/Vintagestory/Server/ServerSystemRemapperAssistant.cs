using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace Vintagestory.Server
{
	public class ServerSystemRemapperAssistant : ServerSystem
	{
		public ServerSystemRemapperAssistant(ServerMain server)
			: base(server)
		{
			server.api.ChatCommands.Create("fixmapping").RequiresPrivilege(Privilege.controlserver).BeginSubCommand("doremap")
				.WithDescription("Do remap")
				.WithArgs(new ICommandArgumentParser[] { server.api.ChatCommands.Parsers.Word("code") })
				.HandleWith(new OnCommandDelegate(this.OnCmdDoremap))
				.EndSubCommand()
				.BeginSubCommand("ignoreall")
				.WithDescription("Ignore all remappings")
				.HandleWith(new OnCommandDelegate(this.OnCmdIgnoreall))
				.EndSubCommand()
				.BeginSubCommand("applyall")
				.WithDescription("Apply all remappings")
				.WithArgs(new ICommandArgumentParser[] { server.api.ChatCommands.Parsers.OptionalWord("force") })
				.HandleWith(new OnCommandDelegate(this.OnCmdApplyall))
				.EndSubCommand();
		}

		private TextCommandResult OnCmdApplyall(TextCommandCallingArgs args)
		{
			int commandsExecuted = 0;
			int setsprocessed = 0;
			bool force = args[0] as string == "force";
			IServerPlayer player = args.Caller.Player as IServerPlayer;
			if (player == null && args.Caller.Type == EnumCallerType.Console)
			{
				player = this.server.ServerConsoleClient.Player;
			}
			foreach (KeyValuePair<string, string[]> val in this.remaps)
			{
				string code = val.Key;
				if (!this.server.SaveGameData.RemappingsAppliedByCode.ContainsKey(code) || force)
				{
					setsprocessed++;
					string[] value = val.Value;
					for (int i = 0; i < value.Length; i++)
					{
						string command = value[i].Trim();
						if (command.Length != 0)
						{
							this.server.HandleChatMessage(player, args.Caller.FromChatGroupId, command);
							commandsExecuted++;
						}
					}
					this.server.SaveGameData.RemappingsAppliedByCode[code] = true;
				}
			}
			if (commandsExecuted == 0)
			{
				return TextCommandResult.Success("No applicable remappings found, seems all good for now!", null);
			}
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(115, 2);
			defaultInterpolatedStringHandler.AppendLiteral("Okay, ");
			defaultInterpolatedStringHandler.AppendFormatted<int>(setsprocessed);
			defaultInterpolatedStringHandler.AppendLiteral(" remapping sets with a total of ");
			defaultInterpolatedStringHandler.AppendFormatted<int>(commandsExecuted);
			defaultInterpolatedStringHandler.AppendLiteral(" remappings commands have been executed. You can now restart your game/server");
			return TextCommandResult.Success(defaultInterpolatedStringHandler.ToStringAndClear(), null);
		}

		private TextCommandResult OnCmdIgnoreall(TextCommandCallingArgs args)
		{
			foreach (KeyValuePair<string, string[]> val in this.remaps)
			{
				string code = val.Key;
				if (!this.server.SaveGameData.RemappingsAppliedByCode.ContainsKey(code))
				{
					this.server.SaveGameData.RemappingsAppliedByCode[code] = false;
				}
			}
			return TextCommandResult.Success(Lang.Get("Okay, ignoring all new remappings. You can still manually remap them using /fixmapping doremap [code]", Array.Empty<object>()), null);
		}

		private TextCommandResult OnCmdDoremap(TextCommandCallingArgs args)
		{
			string code = args[0] as string;
			if (!this.remaps.ContainsKey(code))
			{
				return TextCommandResult.Success(Lang.Get("No remapping group found under this code", Array.Empty<object>()), null);
			}
			string[] array = this.remaps[code];
			int commandsExecuted = 0;
			string[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				string command = array2[i].Trim();
				if (command.Length != 0)
				{
					this.server.HandleChatMessage(args.Caller.Player as IServerPlayer, args.Caller.FromChatGroupId, command);
					commandsExecuted++;
				}
			}
			this.server.SaveGameData.RemappingsAppliedByCode[code] = true;
			return TextCommandResult.Success(Lang.Get("Ok, {0} commands executed.", new object[] { commandsExecuted }), null);
		}

		public override void Dispose()
		{
			BlockSchematic.BlockRemaps = null;
			BlockSchematic.ItemRemaps = null;
		}

		public override void OnFinalizeAssets()
		{
			this.remaps = this.server.AssetManager.Get("config/remaps.json").ToObject<Dictionary<string, string[]>>(null);
			this.extractRemapsForSchematicImports();
			HashSet<string> remapcodes = new HashSet<string>(this.remaps.Keys);
			if (this.server.SaveGameData.IsNewWorld)
			{
				using (HashSet<string>.Enumerator enumerator = remapcodes.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						string code = enumerator.Current;
						this.server.SaveGameData.RemappingsAppliedByCode[code] = true;
					}
					goto IL_00E7;
				}
			}
			foreach (string code2 in this.server.SaveGameData.RemappingsAppliedByCode.Keys)
			{
				remapcodes.Remove(code2);
			}
			this.server.requiresRemaps = remapcodes.Count > 0;
			IL_00E7:
			string[] array = this.server.AssetManager.Get("config/remapentities.json").ToObject<string[]>(null);
			for (int i = 0; i < array.Length; i++)
			{
				string[] cmdSplit = array[i].Split(" ", StringSplitOptions.None);
				if (cmdSplit[0].Equals("/eir"))
				{
					this.server.EntityCodeRemappings.TryAdd(cmdSplit[3], cmdSplit[2]);
				}
			}
		}

		private void extractRemapsForSchematicImports()
		{
			BlockSchematic.BlockRemaps = new Dictionary<string, Dictionary<string, string>>();
			BlockSchematic.ItemRemaps = new Dictionary<string, Dictionary<string, string>>();
			foreach (KeyValuePair<string, string[]> remapping in this.remaps)
			{
				Dictionary<string, string> blockMapping = new Dictionary<string, string>();
				Dictionary<string, string> itemMapping = new Dictionary<string, string>();
				string[] value = remapping.Value;
				for (int i = 0; i < value.Length; i++)
				{
					string[] cmdSplit = value[i].Split(" ", StringSplitOptions.None);
					string command = cmdSplit[0];
					if (command.Equals("/bir"))
					{
						blockMapping.TryAdd(cmdSplit[3], cmdSplit[2]);
					}
					else if (command.Equals("/iir"))
					{
						itemMapping.TryAdd(cmdSplit[3], cmdSplit[2]);
					}
				}
				BlockSchematic.BlockRemaps.TryAdd(remapping.Key, blockMapping);
				BlockSchematic.ItemRemaps.TryAdd(remapping.Key, itemMapping);
			}
		}

		private Dictionary<string, string[]> remaps = new Dictionary<string, string[]>();
	}
}
