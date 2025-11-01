using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;

namespace Vintagestory.Client
{
	public class GuiScreenSingleplayer : GuiScreen
	{
		public GuiScreenSingleplayer(ScreenManager screenManager, GuiScreen parent)
			: base(screenManager, parent)
		{
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

		private void invalidate()
		{
			if (base.IsOpened)
			{
				this.InitGui();
				return;
			}
			ScreenManager.GuiComposers.Dispose("mainmenu-singleplayer");
		}

		private void InitGui()
		{
			List<SavegameCellEntry> cells = this.LoadSaveGameCells();
			int windowWidth = this.ScreenManager.GamePlatform.WindowSize.Width;
			int windowHeight = this.ScreenManager.GamePlatform.WindowSize.Height;
			ElementBounds buttonBounds = ElementBounds.FixedSize(60.0, 30.0).WithFixedPadding(10.0, 2.0).WithAlignment(EnumDialogArea.RightFixed)
				.WithFixedAlignmentOffset(-13.0, 0.0);
			ElementBounds titleBounds = ElementBounds.Fixed(EnumDialogArea.LeftTop, 0.0, 0.0, 690.0, 35.0);
			double num = Math.Max(400.0, (double)windowWidth * 0.5) / (double)ClientSettings.GUIScale;
			ElementBounds insetBounds;
			this.ElementComposer = base.dialogBase("mainmenu-singleplayer", -1.0, -1.0).AddStaticText(Lang.Get("singleplayer-worlds", Array.Empty<object>()), CairoFont.WhiteSmallishText(), titleBounds, null).AddInset(insetBounds = titleBounds.BelowCopy(0.0, 3.0, 0.0, 0.0).WithFixedSize(Math.Max(400.0, (double)windowWidth * 0.5) / (double)ClientSettings.GUIScale, (double)((float)Math.Max(300, windowHeight) / ClientSettings.GUIScale - 205f)), 4, 0.85f)
				.AddVerticalScrollbar(new Action<float>(this.OnNewScrollbarvalue), ElementStdBounds.VerticalScrollbar(insetBounds), "scrollbar")
				.BeginClip(this.clippingBounds = insetBounds.ForkContainingChild(3.0, 3.0, 3.0, 3.0))
				.AddCellList(this.listBounds = this.clippingBounds.ForkContainingChild(0.0, 0.0, 0.0, -3.0).WithFixedPadding(5.0), new OnRequireCell<SavegameCellEntry>(this.createCellElem), cells, "worldstable")
				.EndClip();
			GuiElementCellList<SavegameCellEntry> cellListElem = this.ElementComposer.GetCellList("worldstable");
			cellListElem.BeforeCalcBounds();
			for (int i = 0; i < cells.Count; i++)
			{
				ElementBounds bounds = cellListElem.elementCells[i].Bounds.ForkChild();
				cellListElem.elementCells[i].Bounds.ChildBounds.Add(bounds);
				bounds.fixedWidth -= 56.0;
				bounds.fixedY = -3.0;
				bounds.fixedX -= 6.0;
				bounds.fixedHeight -= 2.0;
				this.ElementComposer.AddHoverText(cells[i].Title + "\r\n" + cells[i].HoverText, CairoFont.WhiteDetailText(), 320, bounds, "hover-" + i.ToString());
				this.ElementComposer.GetHoverText("hover-" + i.ToString()).InsideClipBounds = this.clippingBounds;
			}
			this.ElementComposer.AddIf(cells.Count == 0).AddStaticText(Lang.Get("singleplayer-noworldsfound", Array.Empty<object>()), CairoFont.WhiteMediumText().WithOrientation(EnumTextOrientation.Center), titleBounds.FlatCopy().WithAlignment(EnumDialogArea.CenterMiddle).WithFixedAlignmentOffset(buttonBounds.fixedOffsetX, -30.0), null).AddButton(Lang.Get("singleplayer-newworld", Array.Empty<object>()), new ActionConsumable(this.OnNewWorld), buttonBounds.FlatCopy().WithAlignment(EnumDialogArea.CenterMiddle).WithFixedAlignmentOffset(buttonBounds.fixedOffsetX, 30.0), EnumButtonStyle.Normal, null)
				.EndIf()
				.AddButton(Lang.Get("Open Saves Folder", Array.Empty<object>()), new ActionConsumable(this.OnOpenSavesFolder), buttonBounds.FlatCopy().FixedUnder(insetBounds, 10.0).WithAlignment(EnumDialogArea.LeftFixed)
					.WithFixedAlignmentOffset(0.0, 0.0), EnumButtonStyle.Normal, null)
				.AddButton(Lang.Get("singleplayer-newworld", Array.Empty<object>()), new ActionConsumable(this.OnNewWorld), buttonBounds.FixedUnder(insetBounds, 10.0), EnumButtonStyle.Normal, null)
				.EndChildElements()
				.Compose(true);
			this.listBounds.CalcWorldBounds();
			this.clippingBounds.CalcWorldBounds();
			this.ElementComposer.GetScrollbar("scrollbar").SetHeights((float)this.clippingBounds.fixedHeight, (float)this.listBounds.fixedHeight);
		}

		private IGuiElementCell createCellElem(SavegameCellEntry cell, ElementBounds bounds)
		{
			return new GuiElementMainMenuCell(this.ScreenManager.api, cell, bounds)
			{
				cellEntry = 
				{
					DetailTextOffY = 0.0,
					LeftOffY = -2f
				},
				OnMouseDownOnCellLeft = new Action<int>(this.OnClickCellLeft),
				OnMouseDownOnCellRight = new Action<int>(this.OnClickCellRight)
			};
		}

		public override void OnScreenLoaded()
		{
			this.InitGui();
			this.listBounds.CalcWorldBounds();
			this.clippingBounds.CalcWorldBounds();
			this.ElementComposer.GetScrollbar("scrollbar").SetHeights((float)this.clippingBounds.fixedHeight, (float)this.listBounds.fixedHeight);
		}

		private bool OnOpenSavesFolder()
		{
			NetUtil.OpenUrlInBrowser(GamePaths.Saves);
			return true;
		}

		private void OnNewScrollbarvalue(float value)
		{
			ElementBounds bounds = this.ElementComposer.GetCellList("worldstable").Bounds;
			bounds.fixedY = (double)(0f - value);
			bounds.CalcWorldBounds();
		}

		private void OnClickCellRight(int cellIndex)
		{
			this.lastClickedCellIndex = cellIndex;
			if (this.entries[cellIndex].IsReadOnly)
			{
				this.ScreenManager.LoadScreen(new GuiScreenConfirmAction(Lang.Get("Have no write access to this file, it seems in use. Make sure no other client or server is currently using this savegame.", Array.Empty<object>()), delegate(bool val)
				{
					this.ScreenManager.LoadScreen(this);
				}, this.ScreenManager, this, true));
				return;
			}
			this.ScreenManager.LoadScreen(new GuiScreenSingleplayerModify(this.entries[cellIndex].Filename, this.ScreenManager, this));
		}

		private void OnClickCellLeft(int cellIndex)
		{
			this.lastClickedCellIndex = cellIndex;
			if (this.entries[cellIndex].Savegame == null)
			{
				this.ScreenManager.LoadScreen(new GuiScreenConfirmAction(Lang.Get("savegame-corrupt-confirmrepair", Array.Empty<object>()), new Action<bool>(this.OnConfirmRepairMode), this.ScreenManager, this, false));
				return;
			}
			if (this.entries[cellIndex].Savegame.HighestChunkdataVersion > 2)
			{
				return;
			}
			if (this.entries[cellIndex].DatabaseVersion != GameVersion.DatabaseVersion)
			{
				this.ScreenManager.LoadScreen(new GuiScreenConfirmAction(Lang.Get("This world uses an old file format that needs upgrading. This might take a while. It is also suggested to first back up your savegame in case the upgrade fails. Proceed?", Array.Empty<object>()), new Action<bool>(this.OnDidConfirmUpgrade), this.ScreenManager, this, false));
				return;
			}
			if (this.entries[cellIndex].IsReadOnly)
			{
				this.ScreenManager.LoadScreen(new GuiScreenConfirmAction(Lang.Get("Have no write access to this file, it seems in use. Make sure no other client or server is currently using this savegame.", Array.Empty<object>()), delegate(bool val)
				{
					this.ScreenManager.LoadScreen(this);
				}, this.ScreenManager, this, true));
				return;
			}
			this.ScreenManager.ConnectToSingleplayer(new StartServerArgs
			{
				SaveFileLocation = this.entries[cellIndex].Filename,
				DisabledMods = ClientSettings.DisabledMods,
				Language = ClientSettings.Language
			});
		}

		private void OnConfirmRepairMode(bool confirm)
		{
			if (confirm)
			{
				this.ScreenManager.ConnectToSingleplayer(new StartServerArgs
				{
					SaveFileLocation = this.entries[this.lastClickedCellIndex].Filename,
					DisabledMods = ClientSettings.DisabledMods,
					Language = ClientSettings.Language,
					RepairMode = true
				});
				return;
			}
			this.ScreenManager.LoadScreen(this);
		}

		private void OnDidConfirmUpgrade(bool confirm)
		{
			if (confirm)
			{
				this.ScreenManager.ConnectToSingleplayer(new StartServerArgs
				{
					SaveFileLocation = this.entries[this.lastClickedCellIndex].Filename,
					DisabledMods = ClientSettings.DisabledMods,
					Language = ClientSettings.Language
				});
				return;
			}
			this.ScreenManager.LoadScreen(this);
		}

		private bool OnNewWorld()
		{
			this.ScreenManager.LoadAndCacheScreen(typeof(GuiScreenSingleplayerNewWorld));
			return true;
		}

		public override bool OnBackPressed()
		{
			this.ScreenManager.StartMainMenu();
			return true;
		}

		public bool OnCancel()
		{
			this.OnBackPressed();
			return true;
		}

		private List<SavegameCellEntry> LoadSaveGameCells()
		{
			List<SavegameCellEntry> cells = new List<SavegameCellEntry>();
			this.LoadSaveGames();
			for (int i = 0; i < this.entries.Length; i++)
			{
				SaveGameEntry entry = this.entries[i];
				SavegameCellEntry cell;
				if (entry.Savegame == null)
				{
					cell = new SavegameCellEntry
					{
						Title = new FileInfo(entry.Filename).Name,
						DetailText = (entry.IsReadOnly ? Lang.Get("Unable to load savegame and no write access, likely already opened elsewhere.", Array.Empty<object>()) : Lang.Get("Invalid or corrupted savegame", Array.Empty<object>())),
						TitleFont = CairoFont.WhiteSmallishText().WithColor(GuiStyle.ErrorTextColor),
						DetailTextFont = CairoFont.WhiteSmallText().WithColor(GuiStyle.ErrorTextColor)
					};
				}
				else if (entry.Savegame.HighestChunkdataVersion > 2)
				{
					cell = new SavegameCellEntry
					{
						Title = new FileInfo(entry.Filename).Name,
						DetailText = Lang.Get("versionmismatch-chunk", Array.Empty<object>()),
						TitleFont = CairoFont.WhiteSmallishText().WithColor(GuiStyle.ErrorTextColor),
						DetailTextFont = CairoFont.WhiteSmallText().WithColor(GuiStyle.ErrorTextColor)
					};
				}
				else
				{
					bool isNewerVersion = GameVersion.IsNewerVersionThan(entry.Savegame.LastSavedGameVersion, "1.21.5");
					cell = new SavegameCellEntry
					{
						Title = entry.Savegame.WorldName,
						DetailText = string.Format("{0}, {1}{2}{3}", new object[]
						{
							(entry.Savegame.PlayStyleLangCode == null) ? Lang.Get("playstyle-" + entry.Savegame.PlayStyle, Array.Empty<object>()) : Lang.Get("playstyle-" + entry.Savegame.PlayStyleLangCode, Array.Empty<object>()),
							Lang.Get("Time played: {0}", new object[] { GuiScreenSingleplayer.PrettyTime(entry.Savegame.TotalSecondsPlayed) }),
							(entry.DatabaseVersion != GameVersion.DatabaseVersion) ? ("\nRequires file format upgrade (DB v" + entry.DatabaseVersion.ToString() + ")") : "",
							isNewerVersion ? ("\n" + Lang.Get("versionmismatch-savegame", Array.Empty<object>())) : ""
						}),
						HoverText = this.getHoverText(entry.Savegame)
					};
				}
				cells.Add(cell);
			}
			return cells;
		}

		private string getHoverText(SaveGame savegame)
		{
			ITreeAttribute pworldConfig = savegame.WorldConfiguration;
			StringBuilder sb = new StringBuilder();
			foreach (ModContainer mod in this.ScreenManager.verifiedMods)
			{
				ModWorldConfiguration config = mod.WorldConfig;
				if (config != null)
				{
					foreach (WorldConfigurationAttribute attribute in config.WorldConfigAttributes)
					{
						WorldConfigurationValue value = new WorldConfigurationValue();
						value.Attribute = attribute;
						value.Code = attribute.Code;
						PlayStyle playstyle = null;
						foreach (PlayStyle ps in mod.WorldConfig.PlayStyles)
						{
							if (ps.Code == savegame.PlayStyle)
							{
								playstyle = ps;
								break;
							}
						}
						string defaultValue = attribute.Default.ToLowerInvariant();
						if (playstyle != null && playstyle.WorldConfig[value.Code].Exists)
						{
							defaultValue = playstyle.WorldConfig[value.Code].ToString();
						}
						IAttribute attr = pworldConfig[value.Code];
						if (attr != null && attr.ToString().ToLowerInvariant() != defaultValue)
						{
							sb.AppendLine("<font opacity=\"0.6\">" + Lang.Get("worldattribute-" + attribute.Code, Array.Empty<object>()) + ":</font> " + attribute.valueToHumanReadable(attr.ToString()));
						}
					}
				}
			}
			if (savegame.MapSizeY != 256)
			{
				sb.Append(Lang.Get("worldconfig-worldheight", new object[] { savegame.MapSizeY }));
			}
			if (sb.Length == 0)
			{
				sb.Append("<font opacity=\"0.6\"><i>" + Lang.Get("No custom configurations", Array.Empty<object>()) + "</i></font>");
			}
			else
			{
				sb.AppendLine();
				sb.Append("<font opacity=\"0.6\"><i>" + Lang.Get("All other configurations are default values", Array.Empty<object>()) + "</i></font>");
			}
			return sb.ToString();
		}

		public static string PrettyTime(int seconds)
		{
			if (seconds < 60)
			{
				return Lang.Get("{0} seconds", new object[] { seconds });
			}
			if (seconds < 3600)
			{
				return Lang.Get("{0} minutes, {1} seconds", new object[]
				{
					seconds / 60,
					seconds - seconds / 60 * 60
				});
			}
			int hours = seconds / 3600;
			int minutes = seconds / 60 - hours * 60;
			return Lang.Get("{0} hours, {1} minutes", new object[] { hours, minutes });
		}

		internal string[] GetFilenames()
		{
			string[] files = Directory.GetFiles(GamePaths.Saves);
			List<string> savegames = new List<string>();
			for (int i = 0; i < files.Length; i++)
			{
				if (files[i].EndsWithOrdinal(".vcdbs"))
				{
					savegames.Add(files[i]);
				}
			}
			return savegames.ToArray();
		}

		private void LoadSaveGames()
		{
			string[] filenames = this.GetFilenames();
			List<SaveGameEntry> savegames = new List<SaveGameEntry>();
			GameDatabase db = new GameDatabase(this.ScreenManager.GamePlatform.Logger);
			for (int i = 0; i < filenames.Length; i++)
			{
				int version = 0;
				bool isreadonly = true;
				SaveGame savegame = null;
				try
				{
					savegame = db.ProbeOpenConnection(filenames[i], false, out version, out isreadonly, true);
					if (savegame != null)
					{
						savegame.LoadWorldConfig();
					}
					db.CloseConnection();
				}
				catch (Exception)
				{
				}
				SaveGameEntry entry = new SaveGameEntry
				{
					DatabaseVersion = version,
					Savegame = savegame,
					Filename = filenames[i],
					IsReadOnly = isreadonly,
					Modificationdate = File.GetLastWriteTime(filenames[i])
				};
				savegames.Add(entry);
			}
			savegames.Sort((SaveGameEntry entry1, SaveGameEntry entry2) => entry2.Modificationdate.CompareTo(entry1.Modificationdate));
			this.entries = savegames.ToArray();
		}

		private SaveGameEntry[] entries;

		private int lastClickedCellIndex;

		private ElementBounds listBounds;

		private ElementBounds clippingBounds;
	}
}
