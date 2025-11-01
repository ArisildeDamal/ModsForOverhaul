using System;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.Server
{
	internal class CmdWorldConfig
	{
		public CmdWorldConfig(ServerMain server)
		{
			this.server = server;
			CommandArgumentParsers parsers = server.api.ChatCommands.Parsers;
			server.api.ChatCommands.Create("worldconfig").WithAlias(new string[] { "wc" }).RequiresPrivilege(Privilege.controlserver)
				.WithDescription("Modify the world config")
				.WithArgs(new ICommandArgumentParser[]
				{
					parsers.OptionalWord("key"),
					parsers.OptionalAll("value")
				})
				.HandleWith(new OnCommandDelegate(this.handle));
		}

		private TextCommandResult handle(TextCommandCallingArgs args)
		{
			if (args.Parsers[0].IsMissing)
			{
				return TextCommandResult.Success(string.Format("Specify one of the following world configuration settings: {0}", this.ListConfigs()), null);
			}
			string configname = (string)args[0];
			if (configname == "worldWidth" || configname == "worldLength")
			{
				return TextCommandResult.Error(string.Format("Changing world size is not supported", Array.Empty<object>()), "");
			}
			string currentValue = "";
			bool exists = false;
			WorldConfigurationAttribute attr = null;
			foreach (Mod mod in this.server.api.modLoader.Mods)
			{
				ModWorldConfiguration config = mod.WorldConfig;
				if (config != null)
				{
					if (exists)
					{
						break;
					}
					foreach (WorldConfigurationAttribute attribute in config.WorldConfigAttributes)
					{
						if (attribute.Code.Equals(configname, StringComparison.InvariantCultureIgnoreCase))
						{
							configname = attribute.Code;
							attr = attribute;
							currentValue = "(default:) " + attribute.TypedDefault.ToString();
							if (this.server.SaveGameData.WorldConfiguration.HasAttribute(configname))
							{
								switch (attr.DataType)
								{
								case EnumDataType.Bool:
									currentValue = this.server.SaveGameData.WorldConfiguration.GetBool(configname, false).ToString() ?? "";
									break;
								case EnumDataType.IntInput:
								case EnumDataType.IntRange:
									currentValue = this.server.SaveGameData.WorldConfiguration.GetInt(configname, 0).ToString() ?? "";
									break;
								case EnumDataType.DoubleInput:
								case EnumDataType.DoubleRange:
								{
									double @decimal = this.server.SaveGameData.WorldConfiguration.GetDecimal(configname, 0.0);
									currentValue = @decimal.ToString() ?? "";
									break;
								}
								case EnumDataType.String:
								case EnumDataType.DropDown:
								case EnumDataType.StringRange:
									currentValue = this.server.SaveGameData.WorldConfiguration.GetAsString(configname, null) ?? "";
									break;
								}
							}
							exists = true;
							break;
						}
					}
				}
			}
			if (!exists)
			{
				if (args.Parsers[1].IsMissing && this.server.SaveGameData.WorldConfiguration.HasAttribute(configname))
				{
					return TextCommandResult.Success(string.Format("{0} currently has value: {1}", configname, this.server.SaveGameData.WorldConfiguration[configname]), null);
				}
				return TextCommandResult.Error(string.Format("No such config found: {0}", configname), "");
			}
			else
			{
				if (args.Parsers[1].IsMissing)
				{
					return TextCommandResult.Success(string.Format("{0} currently has value: {1}", configname, currentValue), null);
				}
				string newvalue = (string)args[1];
				string result;
				switch (attr.DataType)
				{
				case EnumDataType.Bool:
				{
					bool val = newvalue.ToBool(false);
					this.server.SaveGameData.WorldConfiguration.SetBool(configname, val);
					result = string.Format("Ok, value {0} set. Restart game world or server to apply changes.", val);
					break;
				}
				case EnumDataType.IntInput:
				case EnumDataType.IntRange:
				{
					int val2 = newvalue.ToInt(0);
					this.server.SaveGameData.WorldConfiguration.SetInt(configname, val2);
					result = string.Format("Ok, value {0} set. Restart game world or server to apply changes.", val2);
					break;
				}
				case EnumDataType.DoubleInput:
				case EnumDataType.DoubleRange:
				{
					double val3 = newvalue.ToDouble(0.0);
					this.server.SaveGameData.WorldConfiguration.SetDouble(configname, val3);
					result = string.Format("Ok, value {0} set. Restart game world or server to apply changes.", val3);
					break;
				}
				case EnumDataType.String:
				case EnumDataType.DropDown:
				case EnumDataType.StringRange:
					this.server.SaveGameData.WorldConfiguration.SetString(configname, newvalue);
					result = string.Format("Ok, value {0} set. Restart game world or server to apply changes.", newvalue);
					break;
				default:
					return TextCommandResult.Error(string.Format("Unknown attr datatype.", Array.Empty<object>()), "");
				}
				if (attr.Values != null)
				{
					double @decimal;
					if (!attr.Values.Any(delegate(string value)
					{
						double num;
						return !double.TryParse(value, out num);
					}) && !double.TryParse(newvalue, out @decimal))
					{
						result = result + "\n" + string.Format("Values for this config are usually decimals, {0} is not a decimal. Config might not apply correctly.", newvalue);
					}
				}
				return TextCommandResult.Success(result, null);
			}
		}

		private string ListConfigs()
		{
			StringBuilder sb = new StringBuilder();
			foreach (Mod mod in this.server.api.modLoader.Mods)
			{
				ModWorldConfiguration config = mod.WorldConfig;
				if (config != null)
				{
					foreach (WorldConfigurationAttribute attribute in config.WorldConfigAttributes)
					{
						if (sb.Length != 0)
						{
							sb.Append(", ");
						}
						sb.Append(attribute.Code);
					}
				}
			}
			return sb.ToString();
		}

		private ServerMain server;
	}
}
