using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;

namespace Vintagestory.Client
{
	public class GuiScreenSingleplayerNewWorld : GuiScreen
	{
		public GuiScreenSingleplayerNewWorld(ScreenManager screenManager, GuiScreen parentScreen)
			: base(screenManager, parentScreen)
		{
			this.ShowMainMenu = true;
			this.wcu = new WorldConfig(screenManager.verifiedMods);
			this.wcu.IsNewWorld = true;
			this.InitGui();
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
			ScreenManager.GuiComposers.Dispose("mainmenu-singleplayernewworld");
		}

		private void InitGui()
		{
			this.wcu.mods = this.ScreenManager.verifiedMods;
			this.wcu.LoadPlayStyles();
			this.cells.Clear();
			this.cells = this.loadPlaystyleCells();
			if (this.wcu.PlayStyles.Count > 0)
			{
				this.cells[0].Selected = true;
				this.wcu.selectPlayStyle(this.selectedPlaystyleIndex);
			}
			int windowWidth = this.ScreenManager.GamePlatform.WindowSize.Width;
			int windowHeight = this.ScreenManager.GamePlatform.WindowSize.Height;
			ElementBounds leftColumn = ElementBounds.Fixed(0.0, 0.0, 300.0, 30.0);
			ElementBounds rightColumn = ElementBounds.Fixed(0.0, 0.0, 300.0, 30.0).FixedRightOf(leftColumn, 0.0);
			double width = Math.Max(400.0, (double)windowWidth * 0.5) / (double)ClientSettings.GUIScale + 40.0;
			ElementBounds insetBounds;
			this.ElementComposer = base.dialogBase("mainmenu-singleplayernewworld", -1.0, -1.0).AddStaticText(Lang.Get("singleplayer-newworld", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftColumn.FlatCopy(), null).AddStaticText(Lang.Get("singleplayer-newworldname", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftColumn = leftColumn.BelowCopy(0.0, 13.0, 0.0, 0.0), null)
				.AddTextInput(rightColumn = rightColumn.BelowCopy(0.0, 10.0, 0.0, 0.0).WithFixedWidth(270.0), null, null, "worldname")
				.AddIconButton("dice", new Action<bool>(this.OnPressDice), rightColumn.FlatCopy().FixedRightOf(rightColumn, 0.0).WithFixedSize(30.0, 30.0), null)
				.AddStaticText(Lang.Get("singleplayer-selectplaystyle", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftColumn = leftColumn.BelowCopy(0.0, 13.0, 0.0, 0.0), null)
				.AddInset(insetBounds = leftColumn.BelowCopy(0.0, 3.0, 0.0, 0.0).WithFixedSize(width - (double)GuiElementScrollbar.DefaultScrollbarWidth - (double)GuiElementScrollbar.DeafultScrollbarPadding - 3.0, (double)((float)Math.Max(300, windowHeight) / ClientSettings.GUIScale - 170f) - leftColumn.fixedY - leftColumn.fixedHeight), 4, 0.85f)
				.AddVerticalScrollbar(new Action<float>(this.OnNewScrollbarvalue), ElementStdBounds.VerticalScrollbar(insetBounds), "scrollbar")
				.BeginClip(this.clippingBounds = insetBounds.ForkContainingChild(3.0, 3.0, 3.0, 3.0))
				.AddCellList(this.listBounds = this.clippingBounds.ForkContainingChild(0.0, 0.0, 0.0, -3.0).WithFixedPadding(10.0), new OnRequireCell<PlaystyleListEntry>(this.createCellElem), this.cells, "playstylelist")
				.EndClip();
			GuiElementCellList<PlaystyleListEntry> cellListElem = this.ElementComposer.GetCellList("playstylelist");
			cellListElem.BeforeCalcBounds();
			for (int i = 0; i < this.cells.Count; i++)
			{
				ElementBounds bounds = cellListElem.elementCells[i].Bounds;
				this.ElementComposer.AddHoverText(this.cells[i].HoverText, CairoFont.WhiteDetailText(), 320, bounds, "hovertext-" + i.ToString());
				this.ElementComposer.GetHoverText("hovertext-" + i.ToString()).InsideClipBounds = this.clippingBounds;
			}
			this.ElementComposer.AddButton(Lang.Get("general-back", Array.Empty<object>()), new ActionConsumable(this.OnBack), leftColumn = insetBounds.BelowCopy(0.0, 10.0, 0.0, 0.0).WithFixedSize(100.0, 30.0).WithFixedPadding(5.0, 0.0), EnumButtonStyle.Normal, null).AddButton(Lang.Get("general-customize", Array.Empty<object>()), new ActionConsumable(this.OnCustomize), leftColumn = leftColumn.FlatCopy().WithFixedWidth(200.0).WithAlignment(EnumDialogArea.RightFixed)
				.WithFixedAlignmentOffset(-220.0, 0.0), EnumButtonStyle.Normal, null).AddButton(Lang.Get("general-createworld", Array.Empty<object>()), new ActionConsumable(this.OnCreate), leftColumn.FlatCopy().WithFixedWidth(200.0).WithAlignment(EnumDialogArea.RightFixed)
				.WithFixedAlignmentOffset(0.0, 0.0), EnumButtonStyle.Normal, null)
				.EndChildElements()
				.Compose(true);
			this.ElementComposer.GetTextInput("worldname").OnKeyPressed = delegate
			{
				this.isCustomWorldName = true;
			};
			this.listBounds.CalcWorldBounds();
			this.clippingBounds.CalcWorldBounds();
			this.ElementComposer.GetScrollbar("scrollbar").SetHeights((float)this.clippingBounds.fixedHeight, (float)this.listBounds.fixedHeight);
			this.updatePlaysStyleSpecificFields();
			if (this.selectedPlaystyleIndex >= 0)
			{
				for (int j = 0; j < this.cells.Count; j++)
				{
					this.cells[j].Selected = false;
				}
				this.cells[this.selectedPlaystyleIndex].Selected = true;
			}
			for (int k = 0; k < this.cells.Count; k++)
			{
				string hoverText;
				if (this.selectedPlaystyleIndex == k)
				{
					hoverText = this.wcu.ToRichText(true);
				}
				else
				{
					hoverText = this.wcu.ToRichText(this.wcu.PlayStyles[k], false);
				}
				this.ElementComposer.GetHoverText("hovertext-" + k.ToString()).SetNewText(hoverText);
			}
		}

		private IGuiElementCell createCellElem(SavegameCellEntry cell, ElementBounds bounds)
		{
			return new GuiElementMainMenuCell(this.ScreenManager.api, cell, bounds)
			{
				ShowModifyIcons = false,
				cellEntry = 
				{
					DetailTextOffY = 4.0
				},
				OnMouseDownOnCellLeft = new Action<int>(this.OnClickCellLeft)
			};
		}

		private void updatePlaysStyleSpecificFields()
		{
			if (!this.isCustomWorldName)
			{
				GuiElementEditableTextBase textInput = this.ElementComposer.GetTextInput("worldname");
				PlayStyle currentPlayStyle = this.wcu.CurrentPlayStyle;
				textInput.SetValue((((currentPlayStyle != null) ? currentPlayStyle.Code : null) == "creativebuilding") ? this.GenRandomCreativeName() : this.GenRandomSurvivalName(), true);
			}
			if (!this.isCustomWorldName)
			{
				GuiElementEditableTextBase textInput2 = this.ElementComposer.GetTextInput("worldname");
				PlayStyle currentPlayStyle2 = this.wcu.CurrentPlayStyle;
				textInput2.SetValue((((currentPlayStyle2 != null) ? currentPlayStyle2.Code : null) == "creativebuilding") ? this.GenRandomCreativeName() : this.GenRandomSurvivalName(), true);
			}
		}

		private List<PlaystyleListEntry> loadPlaystyleCells()
		{
			CairoFont font = CairoFont.WhiteDetailText();
			font.WithFontSize((float)GuiStyle.SmallFontSize);
			foreach (PlayStyle ps in this.wcu.PlayStyles)
			{
				this.cells.Add(new PlaystyleListEntry
				{
					Title = Lang.Get("playstyle-" + ps.LangCode, Array.Empty<object>()),
					DetailText = Lang.Get("playstyle-desc-" + ps.LangCode, Array.Empty<object>()),
					PlayStyle = ps,
					DetailTextFont = font,
					HoverText = ""
				});
			}
			if (this.cells.Count == 0)
			{
				PlayStyle ps2 = new PlayStyle
				{
					Code = "default",
					LangCode = "default",
					WorldConfig = new JsonObject(JToken.Parse("{}")),
					WorldType = "none"
				};
				this.wcu.PlayStyles.Add(ps2);
				this.wcu.selectPlayStyle(0);
				this.cells.Add(new PlaystyleListEntry
				{
					Title = Lang.Get("noplaystyles-title", Array.Empty<object>()),
					DetailText = Lang.Get("noplaystyles-desc", Array.Empty<object>()),
					PlayStyle = ps2,
					DetailTextFont = font,
					Enabled = true
				});
			}
			return this.cells;
		}

		private void OnNewScrollbarvalue(float value)
		{
			ElementBounds bounds = this.ElementComposer.GetCellList("playstylelist").Bounds;
			bounds.fixedY = (double)(0f - value);
			bounds.CalcWorldBounds();
		}

		private void OnPressDice(bool on)
		{
			this.ElementComposer.GetTextInput("worldname").SetValue((this.wcu.CurrentPlayStyle.Code == "creative") ? this.GenRandomCreativeName() : this.GenRandomSurvivalName(), true);
			this.isCustomWorldName = false;
		}

		internal void OnClickCellLeft(int cellIndex)
		{
			this.wcu.selectPlayStyle(cellIndex);
			foreach (PlaystyleListEntry playstyleListEntry in this.cells)
			{
				playstyleListEntry.Selected = false;
			}
			this.cells[cellIndex].Selected = !this.cells[cellIndex].Selected;
			this.updatePlaysStyleSpecificFields();
			this.selectedPlaystyleIndex = cellIndex;
			for (int i = 0; i < this.cells.Count; i++)
			{
				string hoverText;
				if (this.selectedPlaystyleIndex == i)
				{
					hoverText = this.wcu.ToRichText(true);
				}
				else
				{
					hoverText = this.wcu.ToRichText(this.wcu.PlayStyles[i], false);
				}
				this.ElementComposer.GetHoverText("hovertext-" + i.ToString()).SetNewText(hoverText);
			}
		}

		public bool OnCustomize()
		{
			if (this.wcu.CurrentPlayStyle == null)
			{
				return false;
			}
			this.customizeScreen = new GuiScreenWorldCustomize(new Action<bool>(this.OnReturnFromCustomizer), this.ScreenManager, this, this.wcu.Clone(), this.cells);
			this.ScreenManager.LoadScreen(this.customizeScreen);
			return true;
		}

		private void OnReturnFromCustomizer(bool didApply)
		{
			if (didApply)
			{
				this.wcu = this.customizeScreen.wcu;
			}
			string worldName = this.ElementComposer.GetTextInput("worldname").GetText();
			this.ScreenManager.LoadScreen(this);
			this.ElementComposer.GetTextInput("worldname").SetValue(worldName, true);
		}

		private bool OnCreate()
		{
			if (this.wcu.CurrentPlayStyle.Code == "creativebuilding")
			{
				if (this.wcu.MapsizeY > 1024)
				{
					string text = Lang.Get("createworld-creativebuilding-warning-largeworldheight", new object[] { this.wcu.MapsizeY });
					this.ScreenManager.LoadScreen(new GuiScreenConfirmAction(text, new Action<bool>(this.OnDidConfirmCreate), this.ScreenManager, this, false));
					return true;
				}
			}
			else
			{
				if (this.wcu.MapsizeY > 384)
				{
					string text2 = Lang.Get("createworld-surviveandbuild-warning-largeworldheight", new object[] { this.wcu.MapsizeY });
					this.ScreenManager.LoadScreen(new GuiScreenConfirmAction(text2, new Action<bool>(this.OnDidConfirmCreate), this.ScreenManager, this, false));
					return true;
				}
				if (this.wcu.MapsizeY < 256)
				{
					string text2 = Lang.Get("createworld-surviveandbuild-warning-smallworldheight", new object[] { this.wcu.MapsizeY });
					this.ScreenManager.LoadScreen(new GuiScreenConfirmAction(text2, new Action<bool>(this.OnDidConfirmCreate), this.ScreenManager, this, false));
					return true;
				}
			}
			this.CreateWorld();
			return true;
		}

		private void OnDidConfirmCreate(bool confirm)
		{
			if (confirm)
			{
				this.CreateWorld();
				return;
			}
			this.ScreenManager.LoadScreen(this);
		}

		private void CreateWorld()
		{
			string worldname = this.ElementComposer.GetTextInput("worldname").GetText();
			if (string.IsNullOrWhiteSpace(worldname))
			{
				PlayStyle currentPlayStyle = this.wcu.CurrentPlayStyle;
				worldname = ((((currentPlayStyle != null) ? currentPlayStyle.Code : null) == "creativebuilding") ? this.GenRandomCreativeName() : this.GenRandomSurvivalName());
			}
			string basefilename = Regex.Replace(worldname.ToLowerInvariant(), "[^\\w\\d0-9_\\- ]+", "");
			string filename = basefilename;
			int i = 2;
			while (File.Exists(Path.Combine(GamePaths.Saves, filename) + ".vcdbs"))
			{
				filename = basefilename + "-" + i.ToString();
				i++;
			}
			PlayStyle playstyle = this.wcu.CurrentPlayStyle;
			StartServerArgs args = new StartServerArgs
			{
				AllowCreativeMode = GuiScreenSingleplayerNewWorld.allowCheats,
				PlayStyle = playstyle.Code,
				PlayStyleLangCode = playstyle.LangCode,
				WorldType = playstyle.WorldType,
				WorldName = worldname,
				WorldConfiguration = this.wcu.Jworldconfig,
				SaveFileLocation = Path.Combine(GamePaths.Saves, filename) + ".vcdbs",
				Seed = this.wcu.Seed,
				MapSizeY = new int?(this.wcu.MapsizeY),
				CreatedByPlayerName = ClientSettings.PlayerName,
				DisabledMods = ClientSettings.DisabledMods,
				Language = ClientSettings.Language
			};
			this.ScreenManager.ConnectToSingleplayer(args);
		}

		private bool OnBack()
		{
			this.ScreenManager.LoadAndCacheScreen(typeof(GuiScreenSingleplayer));
			return true;
		}

		public string GenRandomSurvivalName()
		{
			string playername = ClientSettings.PlayerName;
			if (playername == null)
			{
				playername = "Tyron";
			}
			if (!playername.EndsWith('s'))
			{
				playername += "s";
			}
			else
			{
				playername += "'";
			}
			string[] firstPart = new string[]
			{
				playername, playername, "Vintage", "Awesome", "Dark", "Serene", "Creepy", "Gloomy", "Peaceful", "Foggy",
				"Sunny"
			};
			string[] secondPart = new string[] { "Adventure", "Cave", "Kingdom", "Village", "Hermit" };
			string[] thirdPart = new string[] { "Tales", "Valley", "Lands", "Story", "World" };
			return string.Concat(new string[]
			{
				firstPart[this.rand.Next(firstPart.Length)],
				" ",
				secondPart[this.rand.Next(secondPart.Length)],
				" ",
				thirdPart[this.rand.Next(thirdPart.Length)]
			});
		}

		public string GenRandomCreativeName()
		{
			string playername = ClientSettings.PlayerName;
			if (playername == null)
			{
				playername = "Tyron";
			}
			if (!playername.EndsWith('s'))
			{
				playername += "s";
			}
			else
			{
				playername += "'";
			}
			string[] firstPart = new string[]
			{
				playername, playername, "Vintage", "Massive", "Dark", "Serene", "Epic", "Gloomy", "Peaceful", "Foggy",
				"Sunny"
			};
			string[] secondPart = new string[] { "Test", "Superflat", "Creative", "Freestyle", "Doodle" };
			string[] thirdPart = new string[] { "Place", "Lands", "Story", "World" };
			return string.Concat(new string[]
			{
				firstPart[this.rand.Next(firstPart.Length)],
				" ",
				secondPart[this.rand.Next(secondPart.Length)],
				" ",
				thirdPart[this.rand.Next(thirdPart.Length)]
			});
		}

		private int selectedPlaystyleIndex;

		protected static bool allowCheats = true;

		protected ElementBounds listBounds;

		protected ElementBounds clippingBounds;

		internal List<PlaystyleListEntry> cells = new List<PlaystyleListEntry>();

		private bool isCustomWorldName;

		private WorldConfig wcu;

		private GuiScreenWorldCustomize customizeScreen;

		private Random rand = new Random();
	}
}
