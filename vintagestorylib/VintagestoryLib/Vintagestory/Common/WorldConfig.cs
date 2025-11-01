using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.Common
{
	public class WorldConfig
	{
		public List<PlayStyle> PlayStyles
		{
			get
			{
				return this.playstyles;
			}
		}

		public PlayStyle CurrentPlayStyle
		{
			get
			{
				return this.playstyles.FirstOrDefault((PlayStyle p) => p.Code == this.playstylecode);
			}
		}

		public int CurrentPlayStyleIndex
		{
			get
			{
				return this.playstyles.IndexOf(this.CurrentPlayStyle);
			}
		}

		public Dictionary<string, WorldConfigurationValue> WorldConfigsPlaystyle
		{
			get
			{
				return this.worldConfigsPlaystyle;
			}
		}

		public Dictionary<string, WorldConfigurationValue> WorldConfigsCustom
		{
			get
			{
				return this.worldConfigsCustom;
			}
		}

		public JsonObject Jworldconfig
		{
			get
			{
				return this.jworldconfig;
			}
		}

		public WorldConfigurationValue this[string code]
		{
			get
			{
				WorldConfigurationValue val;
				if (this.worldConfigsCustom.TryGetValue(code, out val))
				{
					return val;
				}
				this.worldConfigsPlaystyle.TryGetValue(code, out val);
				return val;
			}
		}

		internal void loadFromSavegame(SaveGame savegame)
		{
			if (savegame != null)
			{
				this.Seed = savegame.Seed.ToString();
				this.MapsizeY = savegame.MapSizeY;
				this.selectPlayStyle(savegame.PlayStyle);
				this.loadWorldConfigValues(new JsonObject(JToken.Parse(savegame.WorldConfiguration.ToJsonToken())), this.WorldConfigsCustom);
				this.updateJWorldConfig();
			}
		}

		public WorldConfig(List<ModContainer> mods)
		{
			this.mods = mods;
			this.LoadPlayStyles();
		}

		public void LoadPlayStyles()
		{
			this.playstyles = new List<PlayStyle>();
			foreach (ModContainer mod in this.mods)
			{
				if (mod.Error == null && mod.Enabled)
				{
					ModWorldConfiguration worldConfig = mod.WorldConfig;
					if (((worldConfig != null) ? worldConfig.PlayStyles : null) != null)
					{
						PlayStyle[] playStyles = mod.WorldConfig.PlayStyles;
						for (int i = 0; i < playStyles.Length; i++)
						{
							PlayStyle playstyle = playStyles[i];
							if (this.playstyles.Find((PlayStyle sAttr) => sAttr.Code == playstyle.Code) == null)
							{
								this.playstyles.Add(playstyle);
							}
						}
					}
				}
			}
			this.playstyles = this.playstyles.OrderBy((PlayStyle p) => p.ListOrder).ToList<PlayStyle>();
			if (this.playstyles.Count == 0)
			{
				this.playstyles.Add(new PlayStyle
				{
					Code = "default",
					LangCode = "default",
					WorldConfig = new JsonObject(JObject.Parse("{}"))
				});
			}
		}

		public void selectPlayStyle(int index)
		{
			this.playstylecode = this.playstyles[index].Code;
			this.loadWorldConfigValuesFromPlaystyle();
		}

		public void selectPlayStyle(string playstylecode)
		{
			this.playstylecode = playstylecode;
			this.loadWorldConfigValuesFromPlaystyle();
		}

		private void loadWorldConfigValuesFromPlaystyle()
		{
			if (this.playstylecode == null)
			{
				return;
			}
			PlayStyle playstyle = this.CurrentPlayStyle;
			this.jworldconfig = playstyle.WorldConfig.Clone();
			this.loadWorldConfigValues(this.jworldconfig, this.worldConfigsPlaystyle);
			this.updateJWorldConfig();
		}

		public void loadWorldConfigValues(JsonObject jworldconfig, Dictionary<string, WorldConfigurationValue> intoDict)
		{
			intoDict.Clear();
			foreach (ModContainer modContainer in this.mods)
			{
				ModWorldConfiguration config = modContainer.WorldConfig;
				if (config != null)
				{
					foreach (WorldConfigurationAttribute attribute in config.WorldConfigAttributes)
					{
						WorldConfigurationValue value = new WorldConfigurationValue();
						value.Attribute = attribute;
						value.Code = attribute.Code;
						JsonObject valueObject = jworldconfig[value.Code];
						if (valueObject.Exists)
						{
							switch (value.Attribute.DataType)
							{
							case EnumDataType.Bool:
								value.Value = valueObject.AsBool((bool)value.Attribute.TypedDefault);
								break;
							case EnumDataType.IntInput:
							case EnumDataType.IntRange:
								value.Value = valueObject.AsInt((int)value.Attribute.TypedDefault);
								break;
							case EnumDataType.DoubleInput:
							case EnumDataType.DoubleRange:
								value.Value = valueObject.AsDouble((double)value.Attribute.TypedDefault);
								break;
							case EnumDataType.String:
							case EnumDataType.DropDown:
							case EnumDataType.StringRange:
								value.Value = valueObject.AsString((string)value.Attribute.TypedDefault);
								break;
							}
							intoDict[value.Code] = value;
						}
					}
				}
			}
		}

		public void updateJWorldConfig()
		{
			if (this.CurrentPlayStyle == null)
			{
				return;
			}
			this.jworldconfig = WorldConfig.allDefaultValues(this.mods);
			this.updateJWorldConfigFrom(this.worldConfigsPlaystyle);
			this.updateJWorldConfigFrom(this.worldConfigsCustom);
		}

		public static JsonObject allDefaultValues(List<ModContainer> mods)
		{
			JToken token = JToken.Parse("{}");
			JObject obj = token as JObject;
			foreach (ModContainer modContainer in mods)
			{
				ModWorldConfiguration config = modContainer.WorldConfig;
				if (config != null)
				{
					foreach (WorldConfigurationAttribute attribute in config.WorldConfigAttributes)
					{
						switch (attribute.DataType)
						{
						case EnumDataType.Bool:
							obj[attribute.Code] = (bool)attribute.TypedDefault;
							break;
						case EnumDataType.IntInput:
						case EnumDataType.IntRange:
							obj[attribute.Code] = (int)attribute.TypedDefault;
							break;
						case EnumDataType.DoubleInput:
						case EnumDataType.DoubleRange:
							obj[attribute.Code] = (double)attribute.TypedDefault;
							break;
						case EnumDataType.String:
						case EnumDataType.DropDown:
						case EnumDataType.StringRange:
							obj[attribute.Code] = (string)attribute.TypedDefault;
							break;
						}
					}
				}
			}
			return new JsonObject(token);
		}

		public void updateJWorldConfigFrom(Dictionary<string, WorldConfigurationValue> dict)
		{
			JObject obj = this.jworldconfig.Token as JObject;
			foreach (KeyValuePair<string, WorldConfigurationValue> pair in dict)
			{
				object value = pair.Value.Value;
				switch (pair.Value.Attribute.DataType)
				{
				case EnumDataType.Bool:
					obj[pair.Key] = (bool)value;
					break;
				case EnumDataType.IntInput:
				case EnumDataType.IntRange:
					obj[pair.Key] = (int)value;
					break;
				case EnumDataType.DoubleInput:
				case EnumDataType.DoubleRange:
					obj[pair.Key] = (double)value;
					break;
				case EnumDataType.String:
				case EnumDataType.DropDown:
				case EnumDataType.StringRange:
					obj[pair.Key] = (string)value;
					break;
				}
			}
		}

		public void ApplyConfigs(List<GuiElement> inputElements)
		{
			int i = 0;
			this.worldConfigsCustom = new Dictionary<string, WorldConfigurationValue>();
			foreach (ModContainer modContainer in this.mods)
			{
				ModWorldConfiguration config = modContainer.WorldConfig;
				if (config != null)
				{
					foreach (WorldConfigurationAttribute attribute in config.WorldConfigAttributes)
					{
						if (attribute.OnCustomizeScreen)
						{
							GuiElement elem = inputElements[i];
							WorldConfigurationValue value = new WorldConfigurationValue();
							value.Attribute = attribute;
							value.Code = attribute.Code;
							switch (attribute.DataType)
							{
							case EnumDataType.Bool:
							{
								GuiElementSwitch switchElem = elem as GuiElementSwitch;
								value.Value = switchElem.On;
								break;
							}
							case EnumDataType.IntInput:
							{
								GuiElementNumberInput numInput = elem as GuiElementNumberInput;
								value.Value = numInput.GetText().ToInt(0);
								break;
							}
							case EnumDataType.DoubleInput:
							{
								GuiElementNumberInput numInput2 = elem as GuiElementNumberInput;
								value.Value = numInput2.GetText().ToDouble(0.0);
								break;
							}
							case EnumDataType.IntRange:
							{
								GuiElementSlider slider = elem as GuiElementSlider;
								value.Value = slider.GetValue();
								break;
							}
							case EnumDataType.String:
							{
								GuiElementTextInput textInput = elem as GuiElementTextInput;
								value.Value = textInput.GetText();
								break;
							}
							case EnumDataType.DropDown:
							{
								GuiElementDropDown dropDown = elem as GuiElementDropDown;
								value.Value = dropDown.SelectedValue;
								break;
							}
							case EnumDataType.DoubleRange:
							{
								GuiElementSlider slider2 = elem as GuiElementSlider;
								value.Value = (double)(slider2.GetValue() / attribute.Multiplier);
								break;
							}
							case EnumDataType.StringRange:
							{
								GuiElementSlider slider3 = elem as GuiElementSlider;
								value.Value = attribute.Values[slider3.GetValue()];
								break;
							}
							}
							this.worldConfigsCustom.Add(value.Code, value);
							i++;
						}
					}
				}
			}
		}

		public string ToRichText(bool withCustomConfigs)
		{
			return this.ToRichText(this.CurrentPlayStyle, withCustomConfigs);
		}

		public string ToRichText(PlayStyle playstyle, bool withCustomConfigs)
		{
			if (this.CurrentPlayStyle == null)
			{
				return "";
			}
			JsonObject pworldconfig = playstyle.WorldConfig.Clone();
			if (withCustomConfigs)
			{
				JObject obj = pworldconfig.Token as JObject;
				foreach (KeyValuePair<string, WorldConfigurationValue> pair in this.worldConfigsCustom)
				{
					object value = pair.Value.Value;
					switch (pair.Value.Attribute.DataType)
					{
					case EnumDataType.Bool:
						obj[pair.Key] = (bool)value;
						break;
					case EnumDataType.IntInput:
					case EnumDataType.IntRange:
						obj[pair.Key] = (int)value;
						break;
					case EnumDataType.DoubleInput:
					case EnumDataType.DoubleRange:
						obj[pair.Key] = (double)value;
						break;
					case EnumDataType.String:
					case EnumDataType.DropDown:
					case EnumDataType.StringRange:
						obj[pair.Key] = (string)value;
						break;
					}
				}
			}
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("<font opacity=\"0.6\">" + Lang.Get("World height:", Array.Empty<object>()) + "</font> " + this.MapsizeY.ToString());
			if (this.Seed == null || this.Seed.Length == 0)
			{
				sb.AppendLine("<font opacity=\"0.6\">" + Lang.Get("Random seed", Array.Empty<object>()) + "</font> ");
			}
			else
			{
				sb.AppendLine("<font opacity=\"0.6\">" + Lang.Get("Seed: ", new object[] { this.Seed }) + "</font> " + this.Seed);
			}
			foreach (ModContainer modContainer in this.mods)
			{
				ModWorldConfiguration config = modContainer.WorldConfig;
				if (config != null)
				{
					foreach (WorldConfigurationAttribute attribute in config.WorldConfigAttributes)
					{
						JsonObject valueObject = pworldconfig[new WorldConfigurationValue
						{
							Attribute = attribute,
							Code = attribute.Code
						}.Code];
						if (valueObject.Exists && valueObject.Token.ToString() != attribute.Default)
						{
							sb.AppendLine("<font opacity=\"0.6\">" + Lang.Get("worldattribute-" + attribute.Code, Array.Empty<object>()) + ":</font> " + attribute.valueToHumanReadable(valueObject.Token.ToString()));
						}
					}
				}
			}
			return sb.ToString();
		}

		public string ToJson()
		{
			this.jworldconfig.Token["playstyle"] = this.playstylecode;
			this.jworldconfig.Token["worldHeight"] = this.MapsizeY;
			return this.jworldconfig.ToString();
		}

		public void FromJson(string json)
		{
			JsonObject jworldconfig = new JsonObject(JToken.Parse(json));
			try
			{
				string text;
				if (jworldconfig == null)
				{
					text = null;
				}
				else
				{
					JToken jtoken = jworldconfig.Token["playstyle"];
					text = ((jtoken != null) ? jtoken.ToString() : null);
				}
				this.playstylecode = text;
				int? num;
				if (jworldconfig == null)
				{
					num = null;
				}
				else
				{
					JToken jtoken2 = jworldconfig.Token["worldHeight"];
					if (jtoken2 == null)
					{
						num = null;
					}
					else
					{
						string text2 = jtoken2.ToString();
						num = ((text2 != null) ? new int?(text2.ToInt(0)) : null);
					}
				}
				this.MapsizeY = num ?? this.MapsizeY;
			}
			catch (Exception)
			{
				return;
			}
			this.selectPlayStyle(this.playstylecode);
			this.loadWorldConfigValues(jworldconfig, this.WorldConfigsCustom);
			this.updateJWorldConfig();
		}

		public WorldConfig Clone()
		{
			return new WorldConfig(this.mods)
			{
				playstylecode = this.playstylecode,
				jworldconfig = this.jworldconfig.Clone(),
				worldConfigsPlaystyle = new Dictionary<string, WorldConfigurationValue>(this.worldConfigsPlaystyle),
				worldConfigsCustom = new Dictionary<string, WorldConfigurationValue>(this.worldConfigsCustom),
				MapsizeY = this.MapsizeY,
				Seed = this.Seed,
				IsNewWorld = this.IsNewWorld
			};
		}

		public List<ModContainer> mods;

		protected List<PlayStyle> playstyles;

		protected string playstylecode;

		protected JsonObject jworldconfig;

		protected Dictionary<string, WorldConfigurationValue> worldConfigsPlaystyle = new Dictionary<string, WorldConfigurationValue>();

		protected Dictionary<string, WorldConfigurationValue> worldConfigsCustom = new Dictionary<string, WorldConfigurationValue>();

		public int MapsizeY = 256;

		public string Seed;

		public bool IsNewWorld;
	}
}
