using System;
using System.Collections.Generic;
using System.Linq;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;

namespace Vintagestory.Client
{
	public class GuiScreenWorldCustomize : GuiScreen
	{
		public GuiScreenWorldCustomize(Action<bool> didApply, ScreenManager screenManager, GuiScreen parentScreen, WorldConfig wcu, List<PlaystyleListEntry> playstyles)
			: base(screenManager, parentScreen)
		{
			this.wcu = wcu;
			if (playstyles == null)
			{
				this.loadPlaystyleCells();
			}
			List<ModContainer> list = this.ScreenManager.verifiedMods.ToList<ModContainer>();
			list.Sort(delegate(ModContainer x, ModContainer y)
			{
				bool? flag;
				if (x == null)
				{
					flag = null;
				}
				else
				{
					ModInfo info = x.Info;
					flag = ((info != null) ? new bool?(info.CoreMod) : null);
				}
				bool? flag2 = flag;
				if (flag2.GetValueOrDefault())
				{
					bool? flag3;
					if (y == null)
					{
						flag3 = null;
					}
					else
					{
						ModInfo info2 = y.Info;
						flag3 = ((info2 != null) ? new bool?(info2.CoreMod) : null);
					}
					flag2 = flag3;
					if (flag2.GetValueOrDefault())
					{
						return 0;
					}
				}
				bool? flag4;
				if (x == null)
				{
					flag4 = null;
				}
				else
				{
					ModInfo info3 = x.Info;
					flag4 = ((info3 != null) ? new bool?(info3.CoreMod) : null);
				}
				flag2 = flag4;
				if (flag2.GetValueOrDefault())
				{
					return -1;
				}
				bool? flag5;
				if (y == null)
				{
					flag5 = null;
				}
				else
				{
					ModInfo info4 = y.Info;
					flag5 = ((info4 != null) ? new bool?(info4.CoreMod) : null);
				}
				flag2 = flag5;
				if (flag2.GetValueOrDefault())
				{
					return 1;
				}
				int? num;
				if (x == null)
				{
					num = null;
				}
				else
				{
					ModInfo info5 = x.Info;
					if (info5 == null)
					{
						num = null;
					}
					else
					{
						string name = info5.Name;
						string text;
						if (y == null)
						{
							text = null;
						}
						else
						{
							ModInfo info6 = y.Info;
							text = ((info6 != null) ? info6.Name : null);
						}
						num = new int?(name.CompareTo(text));
					}
				}
				int? num2 = num;
				return num2.GetValueOrDefault();
			});
			foreach (ModContainer mod in list)
			{
				ModWorldConfiguration config = mod.WorldConfig;
				if (config != null)
				{
					WorldConfigurationAttribute[] worldConfigAttributes = config.WorldConfigAttributes;
					for (int i = 0; i < worldConfigAttributes.Length; i++)
					{
						WorldConfigurationAttribute attribute = worldConfigAttributes[i];
						if (attribute.OnCustomizeScreen && this.sortedAttributes.Find((WorldConfigurationAttribute sAttr) => sAttr.Code == attribute.Code) == null)
						{
							attribute.ModInfo = mod.Info;
							int index = this.sortedAttributes.FindLastIndex((WorldConfigurationAttribute nAttribute) => nAttribute.Category == attribute.Category);
							if (this.sortedAttributes.Count == 0 || index == -1 || index + 1 >= this.sortedAttributes.Count)
							{
								this.sortedAttributes.Add(attribute);
							}
							else
							{
								this.sortedAttributes.Insert(index + 1, attribute);
							}
						}
					}
				}
			}
			this.didApply = didApply;
			this.ShowMainMenu = true;
			screenManager.GamePlatform.WindowResized += delegate(int w, int h)
			{
				this.invalidate();
			};
			ClientSettings.Inst.AddWatcher<float>("guiScale", delegate(float s)
			{
				this.invalidate();
			});
		}

		public override void OnScreenLoaded()
		{
			base.OnScreenLoaded();
			this.InitGui();
		}

		private void invalidate()
		{
			if (base.IsOpened)
			{
				this.InitGui();
				return;
			}
			ScreenManager.GuiComposers.Dispose("mainmenu-singleplayercustomize");
		}

		private void InitGui()
		{
			this.cells = this.loadPlaystyleCells();
			this.tabs = this.loadTabs();
			double windowWidth = (double)((float)this.ScreenManager.GamePlatform.WindowSize.Width / RuntimeEnv.GUIScale);
			double windowHeight = (double)((float)this.ScreenManager.GamePlatform.WindowSize.Height / RuntimeEnv.GUIScale);
			double insetWidth = Math.Max(400.0, windowWidth * 0.5);
			double insetHeight = Math.Max(300.0, windowHeight - 175.0);
			double elementWidth = insetWidth - 20.0;
			ElementBounds buttonBounds = ElementBounds.FixedSize(60.0, 25.0).WithFixedPadding(10.0, 0.0);
			ElementBounds titleBounds = ElementBounds.Fixed(EnumDialogArea.LeftTop, 0.0, 0.0, 690.0, 35.0);
			ElementBounds leftColumn = ElementBounds.Fixed(0.0, 0.0, (double)((int)GameMath.Clamp(elementWidth / 2.0, 300.0, elementWidth)), 30.0);
			ElementBounds rightColumn = ElementBounds.Fixed(-52.0, 0.0, (double)((int)GameMath.Clamp(elementWidth / 2.0, 125.0, elementWidth - 300.0)), 30.0).WithAlignment(EnumDialogArea.RightFixed);
			string[] pvalues = this.cells.Select((PlaystyleListEntry c) => c.PlayStyle.Code).ToArray<string>();
			string[] pnames = this.cells.Select((PlaystyleListEntry c) => c.Title).ToArray<string>();
			int selectedIndex = this.cells.FindIndex((PlaystyleListEntry c) => c.PlayStyle.Code == this.wcu.CurrentPlayStyle.Code);
			ElementBounds tabBounds = ElementBounds.Fixed(0.0, 185.0, insetWidth, 20.0);
			ElementBounds insetBounds;
			this.ElementComposer = base.dialogBase("mainmenu-singleplayercustomize", -1.0, -1.0).AddStaticText(Lang.Get("singleplayer-customize", Array.Empty<object>()), CairoFont.WhiteSmallishText(), titleBounds, null).AddStaticText(Lang.Get("singleplayer-playstyle", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftColumn = leftColumn.BelowCopy(0.0, 12.0, 0.0, 0.0), "playstyleText")
				.AddDropDown(pvalues, pnames, selectedIndex, new SelectionChangedDelegate(this.onPlayStyleChanged), rightColumn = rightColumn.BelowCopy(-31.0, 12.0, 0.0, 0.0).WithFixedWidth(rightColumn.fixedWidth - 30.0), "playstyleDropDown")
				.AddIconButton("paste", new Action<bool>(this.OnPasteWorldConfig), rightColumn.CopyOffsetedSibling(32.0, 0.0, 0.0, 0.0).WithFixedSize(30.0, 31.0), null)
				.AddHoverText(Lang.Get("playstyle-pastefromclipboard", Array.Empty<object>()), CairoFont.WhiteDetailText(), 200, rightColumn.CopyOffsetedSibling(32.0, 0.0, 0.0, 0.0).WithFixedSize(30.0, 31.0).WithFixedPadding(5.0), null)
				.AddStaticText(Lang.Get("singleplayer-worldheight", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftColumn = leftColumn.BelowCopy(0.0, 12.0, 0.0, 0.0), "worldHeightText")
				.AddSlider(new ActionConsumable<int>(this.onNewWorldHeightValue), rightColumn = rightColumn.BelowCopy(31.0, 15.0, 0.0, 0.0).WithFixedSize(rightColumn.fixedWidth + 30.0, 20.0), "worldHeight")
				.AddStaticText(Lang.Get("singleplayer-seed", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftColumn = leftColumn.BelowCopy(0.0, 11.0, 0.0, 0.0), "worldseedText")
				.AddTextInput(rightColumn = rightColumn.BelowCopy(0.0, 18.0, 0.0, 0.0).WithFixedHeight(30.0), null, null, "worldseed")
				.AddIf(!this.wcu.IsNewWorld)
				.AddRichtext("<font opacity=\"0.6\">" + Lang.Get("singleplayer-disabledcustomizations", Array.Empty<object>()) + "</font>", CairoFont.WhiteDetailText(), leftColumn = leftColumn.BelowCopy(0.0, 8.0, 0.0, 0.0).WithFixedWidth(600.0), null)
				.Execute(delegate
				{
					rightColumn = rightColumn.BelowCopy(0.0, 18.0, 0.0, 0.0);
				})
				.EndIf()
				.AddHorizontalTabs(this.tabs, tabBounds, new Action<int>(this.onTabClicked), CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold), CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold).WithColor(GuiStyle.ActiveButtonTextColor), "tabs")
				.AddInset(insetBounds = leftColumn.BelowCopy(0.0, 12.0, 0.0, 0.0).WithFixedSize(insetWidth, insetHeight - leftColumn.fixedY - leftColumn.fixedHeight), 4, 0.85f)
				.BeginClip(this.clippingBounds = insetBounds.ForkContainingChild(3.0, 3.0, 3.0, 3.0));
			if (!this.wcu.IsNewWorld)
			{
				this.ElementComposer.GetStaticText("playstyleText").Font.Color[3] = 0.5;
				this.ElementComposer.GetStaticText("worldHeightText").Font.Color[3] = 0.5;
				this.ElementComposer.GetStaticText("worldseedText").Font.Color[3] = 0.5;
				this.ElementComposer.GetDropDown("playstyleDropDown").Enabled = false;
				this.ElementComposer.GetSlider("worldHeight").Enabled = false;
				this.ElementComposer.GetTextInput("worldseed").Enabled = false;
			}
			this.container = new GuiElementContainer(this.ElementComposer.Api, this.listBounds = this.clippingBounds.ForkContainingChild(0.0, 0.0, 0.0, -3.0).WithFixedPadding(10.0));
			this.container.Tabbable = true;
			int i = 0;
			double size = 26.0;
			ElementBounds leftColumni = ElementBounds.Fixed(0.0, 2.0, (double)((int)GameMath.Clamp(elementWidth / 2.0, 300.0, elementWidth)), size);
			ElementBounds rightColumni = ElementBounds.Fixed(-20.0, 0.0, (double)((int)GameMath.Clamp(elementWidth / 2.0, 125.0, elementWidth - 300.0)), size).WithAlignment(EnumDialogArea.RightFixed);
			leftColumni = leftColumni.FlatCopy();
			rightColumni = rightColumni.FlatCopy();
			this.elementsByCategory.Clear();
			this.allInputElements.Clear();
			Dictionary<string, List<GuiElement>> hoverElementsByCat = new Dictionary<string, List<GuiElement>>();
			using (List<WorldConfigurationAttribute>.Enumerator enumerator = this.sortedAttributes.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					WorldConfigurationAttribute attribute = enumerator.Current;
					if (attribute.OnCustomizeScreen)
					{
						List<GuiElement> elements;
						if (!this.elementsByCategory.TryGetValue(attribute.Category, out elements))
						{
							elements = (this.elementsByCategory[attribute.Category] = new List<GuiElement>());
							leftColumni = ElementBounds.Fixed(0.0, 2.0, (double)((int)GameMath.Clamp(elementWidth / 2.0, 300.0, elementWidth)), size);
							rightColumni = ElementBounds.Fixed(-20.0, 0.0, (double)((int)GameMath.Clamp(elementWidth / 2.0, 125.0, elementWidth - 300.0)), size).WithAlignment(EnumDialogArea.RightFixed);
							leftColumni = leftColumni.FlatCopy();
							rightColumni = rightColumni.FlatCopy();
						}
						List<GuiElement> hoverElements;
						if (!hoverElementsByCat.TryGetValue(attribute.Category, out hoverElements))
						{
							hoverElements = (hoverElementsByCat[attribute.Category] = new List<GuiElement>());
						}
						bool enabled = this.wcu.IsNewWorld || !attribute.OnlyDuringWorldCreate;
						WorldConfigurationValue value2 = this.wcu[attribute.Code];
						object defaultValue = attribute.TypedDefault;
						GuiElementControl elem = null;
						switch (attribute.DataType)
						{
						case EnumDataType.Bool:
						{
							bool on = (bool)defaultValue;
							if (value2 != null)
							{
								on = (bool)value2.Value;
							}
							GuiElementSwitch guiElementSwitch = new GuiElementSwitch(this.ScreenManager.api, null, rightColumni.FlatCopy().WithFixedAlignmentOffset(-rightColumni.fixedWidth + size, 0.0), size, 4.0);
							guiElementSwitch.SetValue(on);
							elem = guiElementSwitch;
							break;
						}
						case EnumDataType.IntInput:
						{
							double val = (double)((int)defaultValue);
							if (value2 != null)
							{
								val = (double)((int)value2.Value);
							}
							GuiElementNumberInput guiElementNumberInput = new GuiElementNumberInput(this.ScreenManager.api, (rightColumni != null) ? rightColumni.FlatCopy() : null, null, CairoFont.WhiteSmallText());
							guiElementNumberInput.IntMode = true;
							guiElementNumberInput.SetValue(val);
							elem = guiElementNumberInput;
							break;
						}
						case EnumDataType.DoubleInput:
						{
							double val2 = (double)defaultValue;
							if (value2 != null)
							{
								val2 = (double)value2.Value;
							}
							GuiElementNumberInput guiElementNumberInput2 = new GuiElementNumberInput(this.ScreenManager.api, (rightColumni != null) ? rightColumni.FlatCopy() : null, null, CairoFont.WhiteSmallText());
							guiElementNumberInput2.SetValue(val2);
							elem = guiElementNumberInput2;
							break;
						}
						case EnumDataType.IntRange:
						{
							int val3 = (int)defaultValue;
							if (value2 != null)
							{
								val3 = (int)value2.Value;
							}
							GuiElementSlider sliderElem = new GuiElementSlider(this.ScreenManager.api, null, rightColumni.FlatCopy());
							sliderElem.TooltipExceedClipBounds = true;
							sliderElem.ShowTextWhenResting = true;
							if (attribute.SkipValues != null)
							{
								string[] array = attribute.SkipValues;
								for (int num = 0; num < array.Length; num++)
								{
									string[] split = array[num].Split("...", 2, StringSplitOptions.RemoveEmptyEntries);
									if (split.Length == 1)
									{
										sliderElem.AddSkipValue(split[0].ToInt(0));
									}
									else
									{
										int max = split[1].ToInt(0);
										int intStep = (int)attribute.Step;
										for (int j = split[0].ToInt(0); j <= max; j += intStep)
										{
											sliderElem.AddSkipValue(j);
										}
									}
								}
							}
							string unit = "worldattribute-" + attribute.Code + "-unit";
							int alarm2 = (int)Math.Clamp(attribute.Alarm, attribute.Min, attribute.Max);
							if (attribute.Values != null && attribute.Names != null)
							{
								string[] values4 = attribute.Values;
								string[] names3 = new string[attribute.Names.Length];
								for (int k = 0; k < values4.Length; k++)
								{
									string langkey = "worldconfig-" + attribute.Code + "-" + attribute.Names[k];
									if (ClientSettings.DeveloperMode && !Lang.HasTranslation(langkey, true, true))
									{
										this.ScreenManager.api.Logger.Debug("\"{0}\": \"{1}\",", new object[]
										{
											langkey,
											attribute.Names[k]
										});
									}
									names3[k] = Lang.Get(langkey, Array.Empty<object>());
								}
								sliderElem.OnSliderTooltip = delegate(int value)
								{
									if (values4.Contains(value.ToString()))
									{
										return names3[values4.IndexOf(value.ToString())];
									}
									return Lang.Get(unit, new object[] { value / attribute.DisplayUnit }) + ((value > alarm2) ? Lang.GetWithFallback("worldattribute-" + attribute.Code + "-warning", "", Array.Empty<object>()) : "");
								};
								sliderElem.OnSliderRestingText = delegate(int value)
								{
									if (values4.Contains(value.ToString()))
									{
										return names3[values4.IndexOf(value.ToString())];
									}
									return Lang.Get(unit, new object[] { value / attribute.DisplayUnit });
								};
							}
							else
							{
								sliderElem.OnSliderTooltip = (int value) => Lang.GetWithFallback(unit, "{0}", new object[] { value / attribute.DisplayUnit }) + ((value > alarm2) ? Lang.GetWithFallback("worldattribute-" + attribute.Code + "-warning", "", Array.Empty<object>()) : "");
								sliderElem.OnSliderRestingText = (int value) => Lang.GetWithFallback(unit, "{0}", new object[] { value / attribute.DisplayUnit });
							}
							sliderElem.SetValues(val3, (int)attribute.Min, (int)attribute.Max, (int)attribute.Step, "");
							elem = sliderElem;
							break;
						}
						case EnumDataType.String:
						{
							string val4 = (string)defaultValue;
							if (value2 != null)
							{
								val4 = (string)value2.Value;
							}
							GuiElementTextInput guiElementTextInput = new GuiElementTextInput(this.ScreenManager.api, rightColumni.FlatCopy(), null, CairoFont.WhiteSmallText());
							guiElementTextInput.SetValue(val4, true);
							elem = guiElementTextInput;
							break;
						}
						case EnumDataType.DropDown:
						{
							string val5 = (string)defaultValue;
							if (value2 != null)
							{
								val5 = (string)value2.Value;
							}
							int selindex = attribute.Values.IndexOf(val5);
							string[] values3 = attribute.Values;
							string[] names2 = new string[attribute.Names.Length];
							for (int l = 0; l < values3.Length; l++)
							{
								string langkey2 = "worldconfig-" + attribute.Code + "-" + attribute.Names[l];
								if (ClientSettings.DeveloperMode && !Lang.HasTranslation(langkey2, true, true))
								{
									this.ScreenManager.api.Logger.Debug("\"{0}\": \"{1}\",", new object[]
									{
										langkey2,
										attribute.Names[l]
									});
								}
								names2[l] = Lang.Get(langkey2, Array.Empty<object>());
							}
							if (selindex < 0)
							{
								values3 = values3.Append(val5);
								names2 = names2.Append(val5);
								selindex = names2.Length - 1;
							}
							elem = new GuiElementDropDown(this.ScreenManager.api, values3, names2, selindex, null, rightColumni.FlatCopy(), CairoFont.WhiteSmallText(), false);
							break;
						}
						case EnumDataType.DoubleRange:
						{
							double val6 = (double)defaultValue;
							if (value2 != null)
							{
								val6 = (double)value2.Value;
							}
							GuiElementSlider sliderElem2 = new GuiElementSlider(this.ScreenManager.api, null, rightColumni.FlatCopy());
							sliderElem2.TooltipExceedClipBounds = true;
							sliderElem2.ShowTextWhenResting = true;
							if (attribute.SkipValues != null)
							{
								string[] array = attribute.SkipValues;
								for (int num = 0; num < array.Length; num++)
								{
									string[] split2 = array[num].Split("...", 2, StringSplitOptions.RemoveEmptyEntries);
									if (split2.Length == 1)
									{
										sliderElem2.AddSkipValue((int)((decimal)split2[0].ToDouble(0.0) * attribute.Multiplier));
									}
									else
									{
										int m = (int)((decimal)split2[0].ToDouble(0.0) * attribute.Multiplier);
										int max2 = (int)((decimal)split2[1].ToDouble(0.0) * attribute.Multiplier);
										int intStep2 = (int)((decimal)attribute.Step * attribute.Multiplier);
										while (m <= max2)
										{
											sliderElem2.AddSkipValue(m);
											m += intStep2;
										}
									}
								}
							}
							string unitCode = "worldattribute-" + attribute.Code + "-unit";
							int alarm = (int)((decimal)Math.Clamp(attribute.Alarm, attribute.Min, attribute.Max) * attribute.Multiplier);
							if (attribute.Values != null && attribute.Names != null)
							{
								string[] values = attribute.Values;
								string[] names4 = new string[attribute.Names.Length];
								for (int n = 0; n < values.Length; n++)
								{
									string langkey3 = "worldconfig-" + attribute.Code + "-" + attribute.Names[n];
									if (ClientSettings.DeveloperMode && !Lang.HasTranslation(langkey3, true, true))
									{
										this.ScreenManager.api.Logger.Debug("\"{0}\": \"{1}\",", new object[]
										{
											langkey3,
											attribute.Names[n]
										});
									}
									names4[n] = Lang.Get(langkey3, Array.Empty<object>());
								}
								sliderElem2.OnSliderTooltip = delegate(int value)
								{
									if (values.Contains((value / attribute.Multiplier).ToString()))
									{
										return names4[values.IndexOf((value / attribute.Multiplier).ToString())];
									}
									return Lang.GetWithFallback(unitCode, "{0}", new object[] { value / attribute.Multiplier / attribute.DisplayUnit }) + ((value > alarm) ? Lang.GetWithFallback("worldattribute-" + attribute.Code + "-warning", "", Array.Empty<object>()) : "");
								};
								sliderElem2.OnSliderRestingText = delegate(int value)
								{
									if (values.Contains((value / attribute.Multiplier).ToString()))
									{
										return names4[values.IndexOf((value / attribute.Multiplier).ToString())];
									}
									return Lang.GetWithFallback(unitCode, "{0}", new object[] { value / attribute.Multiplier / attribute.DisplayUnit });
								};
							}
							else
							{
								sliderElem2.OnSliderTooltip = (int value) => Lang.GetWithFallback(unitCode, "{0}", new object[] { value / attribute.Multiplier / attribute.DisplayUnit }) + ((value > alarm) ? Lang.GetWithFallback("worldattribute-" + attribute.Code + "-warning", "", Array.Empty<object>()) : "");
								sliderElem2.OnSliderRestingText = (int value) => Lang.GetWithFallback(unitCode, "{0}", new object[] { value / attribute.Multiplier / attribute.DisplayUnit });
							}
							sliderElem2.SetValues((int)((decimal)val6 * attribute.Multiplier), (int)((decimal)attribute.Min * attribute.Multiplier), (int)((decimal)attribute.Max * attribute.Multiplier), (int)((decimal)attribute.Step * attribute.Multiplier), "");
							elem = sliderElem2;
							break;
						}
						case EnumDataType.StringRange:
						{
							string[] values2 = attribute.Values;
							string[] names = new string[attribute.Names.Length];
							int val7 = values2.IndexOf((string)defaultValue);
							if (value2 != null)
							{
								val7 = values2.IndexOf((string)value2.Value);
							}
							for (int k2 = 0; k2 < values2.Length; k2++)
							{
								string langkey4 = "worldconfig-" + attribute.Code + "-" + attribute.Names[k2];
								if (ClientSettings.DeveloperMode && !Lang.HasTranslation(langkey4, true, true))
								{
									this.ScreenManager.api.Logger.Debug("\"{0}\": \"{1}\",", new object[]
									{
										langkey4,
										attribute.Names[k2]
									});
								}
								names[k2] = Lang.Get(langkey4, Array.Empty<object>());
							}
							GuiElementSlider guiElementSlider = new GuiElementSlider(this.ScreenManager.api, null, rightColumni.FlatCopy());
							guiElementSlider.TooltipExceedClipBounds = true;
							guiElementSlider.ShowTextWhenResting = true;
							guiElementSlider.OnSliderTooltip = (int value) => names[value];
							guiElementSlider.SetValues(val7, 0, values2.Length - 1, 1, "");
							elem = guiElementSlider;
							break;
						}
						}
						elem.Enabled = enabled;
						elements.Add(elem);
						this.allInputElements.Add(elem);
						CairoFont font = (attribute.ModInfo.CoreMod ? CairoFont.WhiteSmallText() : CairoFont.WhiteSmallText().WithColor(GuiStyle.WarningTextColor));
						if (!enabled)
						{
							font.Color[3] = 0.5;
						}
						elements.Add(new GuiElementStaticText(this.ScreenManager.api, Lang.Get("worldattribute-" + attribute.Code, Array.Empty<object>()), EnumTextOrientation.Left, leftColumni, font));
						string tooltip = Lang.GetIfExists("worldattribute-" + attribute.Code + "-desc", Array.Empty<object>());
						if (tooltip != null)
						{
							ElementBounds hbounds = leftColumni.FlatCopy();
							hbounds.fixedWidth -= 50.0;
							if (!attribute.ModInfo.CoreMod)
							{
								tooltip = tooltip + "\n\n<font color=\"#F2C983\">" + Lang.Get("createworld-worldattribute-notcoremodhover", new object[] { attribute.ModInfo.Name }) + "</font>";
							}
							GuiElementHoverText hoverelem = new GuiElementHoverText(this.ScreenManager.api, tooltip, CairoFont.WhiteSmallText(), 320, hbounds, null);
							hoverElements.Add(hoverelem);
						}
						leftColumni = leftColumni.BelowCopy(0.0, 9.9, 0.0, 0.0);
						rightColumni = rightColumni.BelowCopy(0.0, 10.0, 0.0, 0.0);
						i++;
					}
				}
			}
			foreach (KeyValuePair<string, List<GuiElement>> val8 in this.elementsByCategory)
			{
				List<GuiElement> hoverEles;
				if (hoverElementsByCat.TryGetValue(val8.Key, out hoverEles))
				{
					foreach (GuiElement hoverEle in hoverEles)
					{
						val8.Value.Add(hoverEle);
					}
				}
				val8.Value.Add(new GuiElementStaticText(this.ScreenManager.api, " ", EnumTextOrientation.Left, leftColumni = leftColumni.BelowCopy(0.0, 0.0, 0.0, 0.0), CairoFont.WhiteDetailText()));
			}
			this.updateWorldHeightSlider();
			this.ElementComposer.AddInteractiveElement(this.container, "configlist").EndClip().AddVerticalScrollbar(new Action<float>(this.OnNewScrollbarvalue), ElementStdBounds.VerticalScrollbar(insetBounds), "scrollbar")
				.AddButton(Lang.Get("general-back", Array.Empty<object>()), new ActionConsumable(this.OnBack), buttonBounds.FlatCopy().FixedUnder(insetBounds, 10.0), EnumButtonStyle.Normal, null)
				.AddButton(Lang.Get("general-apply", Array.Empty<object>()), new ActionConsumable(this.OnApply), buttonBounds.FlatCopy().FixedUnder(insetBounds, 10.0).WithFixedWidth(200.0)
					.WithAlignment(EnumDialogArea.RightFixed)
					.WithFixedAlignmentOffset(-13.0, 0.0), EnumButtonStyle.Normal, null)
				.EndChildElements()
				.Compose(true);
			this.ElementComposer.GetScrollbar("scrollbar").SetHeights((float)this.clippingBounds.fixedHeight, (float)this.listBounds.fixedHeight);
			this.selectTab(0);
			this.ElementComposer.GetTextInput("worldseed").SetValue(this.wcu.Seed, true);
			this.setConfigSliderAlarmValues();
		}

		private GuiTab[] loadTabs()
		{
			List<GuiTab> tabs = new List<GuiTab>();
			this.categories.Clear();
			int i = 0;
			foreach (WorldConfigurationAttribute attribute in this.sortedAttributes)
			{
				if (attribute.OnCustomizeScreen && !this.categories.Contains(attribute.Category))
				{
					this.categories.Add(attribute.Category);
					tabs.Add(new GuiTab
					{
						Name = Lang.Get("worldconfig-category-" + attribute.Category, Array.Empty<object>()),
						DataInt = i++
					});
				}
			}
			return tabs.ToArray();
		}

		private void onTabClicked(int dataint)
		{
			this.selectTab(dataint);
		}

		private void selectTab(int tabIndex)
		{
			string cat = this.categories[tabIndex];
			this.container.Clear();
			foreach (GuiElement ele in this.elementsByCategory[cat])
			{
				this.container.Add(ele, -1);
			}
			this.ElementComposer.ReCompose();
			this.listBounds.CalcWorldBounds();
			this.clippingBounds.CalcWorldBounds();
			this.ElementComposer.GetScrollbar("scrollbar").SetHeights((float)this.clippingBounds.fixedHeight, (float)this.listBounds.fixedHeight);
			this.updateWorldHeightSlider();
		}

		private void setConfigSliderAlarmValues()
		{
			int i = 0;
			foreach (WorldConfigurationAttribute attribute in this.sortedAttributes)
			{
				if (attribute.OnCustomizeScreen)
				{
					if (!this.wcu.IsNewWorld && attribute.OnlyDuringWorldCreate)
					{
						i++;
					}
					else
					{
						WorldConfigurationValue worldConfigurationValue = this.wcu[attribute.Code];
						object typedDefault = attribute.TypedDefault;
						GuiElement elem = this.allInputElements[i];
						EnumDataType dataType = attribute.DataType;
						if (dataType != EnumDataType.IntRange)
						{
							if (dataType == EnumDataType.DoubleRange)
							{
								(elem as GuiElementSlider).SetAlarmValue((int)((decimal)Math.Clamp(attribute.Alarm, attribute.Min, attribute.Max) * attribute.Multiplier));
							}
						}
						else
						{
							(elem as GuiElementSlider).SetAlarmValue((int)Math.Clamp(attribute.Alarm, attribute.Min, attribute.Max));
						}
						i++;
					}
				}
			}
		}

		private void setFieldValues()
		{
			int i = 0;
			foreach (WorldConfigurationAttribute attribute in this.sortedAttributes)
			{
				if (attribute.OnCustomizeScreen)
				{
					if (!this.wcu.IsNewWorld && attribute.OnlyDuringWorldCreate)
					{
						i++;
					}
					else
					{
						WorldConfigurationValue value = this.wcu[attribute.Code];
						object defaultValue = attribute.TypedDefault;
						GuiElement elem = this.allInputElements[i];
						switch (attribute.DataType)
						{
						case EnumDataType.Bool:
						{
							GuiElementSwitch guiElementSwitch = elem as GuiElementSwitch;
							bool on = (bool)defaultValue;
							if (value != null)
							{
								on = (bool)value.Value;
							}
							guiElementSwitch.SetValue(on);
							break;
						}
						case EnumDataType.IntInput:
						{
							GuiElementEditableTextBase guiElementEditableTextBase = elem as GuiElementNumberInput;
							int val = (int)defaultValue;
							if (value != null)
							{
								val = (int)value.Value;
							}
							guiElementEditableTextBase.SetValue((float)val);
							break;
						}
						case EnumDataType.DoubleInput:
						{
							GuiElementEditableTextBase guiElementEditableTextBase2 = elem as GuiElementNumberInput;
							double val2 = (double)defaultValue;
							if (value != null)
							{
								val2 = (double)value.Value;
							}
							guiElementEditableTextBase2.SetValue(val2);
							break;
						}
						case EnumDataType.IntRange:
						{
							GuiElementSlider guiElementSlider = elem as GuiElementSlider;
							int val3 = (int)defaultValue;
							if (value != null)
							{
								val3 = (int)value.Value;
							}
							guiElementSlider.SetValues(val3, (int)attribute.Min, (int)attribute.Max, (int)attribute.Step, "");
							break;
						}
						case EnumDataType.String:
						{
							string val4 = (string)defaultValue;
							if (value != null)
							{
								val4 = (string)value.Value;
							}
							(elem as GuiElementTextInput).SetValue(val4, true);
							break;
						}
						case EnumDataType.DropDown:
						{
							string val5 = (string)defaultValue;
							if (value != null)
							{
								val5 = (string)value.Value;
							}
							int selindex = attribute.Values.IndexOf(val5);
							(elem as GuiElementDropDown).SetSelectedIndex(selindex);
							break;
						}
						case EnumDataType.DoubleRange:
						{
							GuiElementSlider guiElementSlider2 = elem as GuiElementSlider;
							double val6 = (double)defaultValue;
							if (value != null)
							{
								val6 = (double)value.Value;
							}
							guiElementSlider2.SetValues((int)((decimal)val6 * attribute.Multiplier), (int)((decimal)attribute.Min * attribute.Multiplier), (int)((decimal)attribute.Max * attribute.Multiplier), (int)((decimal)attribute.Step * attribute.Multiplier), "");
							break;
						}
						case EnumDataType.StringRange:
						{
							GuiElementSlider guiElementSlider3 = elem as GuiElementSlider;
							int val7 = attribute.Values.IndexOf((string)defaultValue);
							if (value != null)
							{
								val7 = attribute.Values.IndexOf((string)value.Value);
							}
							guiElementSlider3.SetValues(val7, 0, attribute.Values.Length - 1, 1, "");
							break;
						}
						}
						i++;
					}
				}
			}
		}

		private void onPlayStyleChanged(string code, bool selected)
		{
			this.wcu.selectPlayStyle(code);
			this.updateWorldHeightSlider();
			this.setFieldValues();
		}

		private void updateWorldHeightSlider()
		{
			if (this.wcu.CurrentPlayStyle.Code != "creativebuilding")
			{
				this.ElementComposer.GetSlider("worldHeight").SetValues(this.wcu.MapsizeY, 128, 512, 64, " blocks");
				this.ElementComposer.GetSlider("worldHeight").SetAlarmValue(384);
				this.ElementComposer.GetSlider("worldHeight").OnSliderTooltip = (int value) => Lang.Get("createworld-worldheight", new object[] { value }) + ((value > 384) ? ("\n" + Lang.Get("createworld-worldheight-warning", Array.Empty<object>())) : "");
				return;
			}
			this.ElementComposer.GetSlider("worldHeight").SetValues(this.wcu.MapsizeY, 128, 2048, 64, " blocks");
			this.ElementComposer.GetSlider("worldHeight").SetAlarmValue(1024);
			this.ElementComposer.GetSlider("worldHeight").OnSliderTooltip = (int value) => Lang.Get("createworld-worldheight", new object[] { value }) + ((value > 1024) ? ("\n" + Lang.Get("createworld-worldheight-warning", Array.Empty<object>())) : "");
		}

		private List<PlaystyleListEntry> loadPlaystyleCells()
		{
			this.cells = new List<PlaystyleListEntry>();
			this.wcu.LoadPlayStyles();
			foreach (PlayStyle ps in this.wcu.PlayStyles)
			{
				this.cells.Add(new PlaystyleListEntry
				{
					Title = Lang.Get("playstyle-" + ps.LangCode, Array.Empty<object>()),
					PlayStyle = ps
				});
			}
			if (this.cells.Count == 0)
			{
				this.cells.Add(new PlaystyleListEntry
				{
					Title = Lang.Get("noplaystyles-title", Array.Empty<object>()),
					DetailText = Lang.Get("noplaystyles-desc", Array.Empty<object>()),
					PlayStyle = null,
					Enabled = false
				});
			}
			return this.cells;
		}

		private bool onNewWorldHeightValue(int value)
		{
			this.wcu.MapsizeY = value;
			return true;
		}

		public override void OnKeyDown(KeyEvent e)
		{
			base.OnKeyDown(e);
			if (e.CtrlPressed && e.KeyCode == 104)
			{
				this.OnPasteWorldConfig(false);
			}
		}

		private void OnPasteWorldConfig(bool ok = false)
		{
			try
			{
				string json = ScreenManager.Platform.XPlatInterface.GetClipboardText();
				if (json.StartsWith("{"))
				{
					this.wcu.FromJson(json);
					this.ScreenManager.GamePlatform.Logger.Notification("Pasted world config loaded!");
					this.updateWorldHeightSlider();
					this.setFieldValues();
				}
			}
			catch (Exception ex)
			{
				this.ScreenManager.GamePlatform.Logger.Warning("Unable to load pasted world config:");
				this.ScreenManager.GamePlatform.Logger.Warning(ex);
			}
		}

		private bool OnApply()
		{
			this.wcu.Seed = this.ElementComposer.GetTextInput("worldseed").GetText();
			this.wcu.MapsizeY = this.ElementComposer.GetSlider("worldHeight").GetValue();
			int i = 0;
			this.wcu.WorldConfigsCustom.Clear();
			foreach (WorldConfigurationAttribute attribute in this.sortedAttributes)
			{
				if (attribute.OnCustomizeScreen)
				{
					GuiElement elem = this.allInputElements[i];
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
					this.wcu.WorldConfigsCustom.Add(value.Code, value);
					i++;
				}
			}
			this.wcu.updateJWorldConfig();
			this.didApply(true);
			return true;
		}

		private bool OnBack()
		{
			this.didApply(false);
			return true;
		}

		private void OnNewScrollbarvalue(float value)
		{
			ElementBounds bounds = this.container.Bounds;
			bounds.fixedY = (double)(0f - value);
			bounds.CalcWorldBounds();
			foreach (GuiElement guiElement in this.container.Elements)
			{
				GuiElementDropDown gelemd = guiElement as GuiElementDropDown;
				if (gelemd != null && gelemd.listMenu.IsOpened)
				{
					gelemd.listMenu.Close();
				}
			}
		}

		private ElementBounds listBounds;

		private ElementBounds clippingBounds;

		private Dictionary<string, List<GuiElement>> elementsByCategory = new Dictionary<string, List<GuiElement>>();

		private List<GuiElement> allInputElements = new List<GuiElement>();

		private GuiTab[] tabs;

		private List<string> categories = new List<string>();

		private List<WorldConfigurationAttribute> sortedAttributes = new List<WorldConfigurationAttribute>();

		private Action<bool> didApply;

		private GuiElementContainer container;

		public WorldConfig wcu;

		private List<PlaystyleListEntry> cells;
	}
}
