using System;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.Server
{
	internal class CmdWorldConfigCreate
	{
		public CmdWorldConfigCreate(ServerMain server)
		{
			this.server = server;
			CommandArgumentParsers parsers = server.api.ChatCommands.Parsers;
			server.api.ChatCommands.Create("worldconfigcreate").RequiresPrivilege(Privilege.controlserver).WithDescription("Add a new world config value")
				.WithArgs(new ICommandArgumentParser[]
				{
					parsers.WordRange("type", new string[] { "bool", "double", "float", "int", "string" }),
					parsers.Word("key"),
					parsers.All("value")
				})
				.HandleWith(new OnCommandDelegate(this.handle));
		}

		private TextCommandResult handle(TextCommandCallingArgs args)
		{
			string type = (string)args[0];
			string configname = (string)args[1];
			string newvalue = (string)args[2];
			string result;
			if (!(type == "bool"))
			{
				if (!(type == "double"))
				{
					if (!(type == "float"))
					{
						if (!(type == "string"))
						{
							if (!(type == "int"))
							{
								return TextCommandResult.Error("Invalid or missing datatype", "");
							}
							int val = newvalue.ToInt(0);
							this.server.SaveGameData.WorldConfiguration.SetInt(configname, val);
							result = string.Format("Ok, value {0} set", val);
						}
						else
						{
							this.server.SaveGameData.WorldConfiguration.SetString(configname, newvalue);
							result = string.Format("Ok, value {0} set", newvalue);
						}
					}
					else
					{
						float val2 = newvalue.ToFloat(0f);
						this.server.SaveGameData.WorldConfiguration.SetFloat(configname, val2);
						result = string.Format("Ok, value {0} set", val2);
					}
				}
				else
				{
					double val3 = newvalue.ToDouble(0.0);
					this.server.SaveGameData.WorldConfiguration.SetDouble(configname, val3);
					result = string.Format("Ok, value {0} set", val3);
				}
			}
			else
			{
				bool val4 = newvalue.ToBool(false);
				this.server.SaveGameData.WorldConfiguration.SetBool(configname, val4);
				result = string.Format("Ok, value {0} set", val4);
			}
			return TextCommandResult.Success(result, null);
		}

		private ServerMain server;
	}
}
