using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;

namespace Vintagestory.Client
{
	public class GuiScreenSingleplayerModify : GuiScreen
	{
		public GuiScreenSingleplayerModify(string worldfilename, ScreenManager screenManager, GuiScreen parentScreen)
			: base(screenManager, parentScreen)
		{
			this.ShowMainMenu = true;
			this.worldfilename = worldfilename;
		}

		private SaveGame getSaveGame(out int version, out bool isreadonly, bool keepOpen = false)
		{
			if (this.gamedb != null)
			{
				this.gamedb.Dispose();
			}
			this.gamedb = new GameDatabase(this.ScreenManager.GamePlatform.Logger);
			string errorMessage;
			SaveGame saveGame = this.gamedb.ProbeOpenConnection(this.worldfilename, false, out version, out errorMessage, out isreadonly, true);
			if (saveGame != null)
			{
				saveGame.LoadWorldConfig();
			}
			if (!keepOpen)
			{
				this.gamedb.CloseConnection();
			}
			return saveGame;
		}

		public void initGui(SaveGame savegame)
		{
			if (!this.valuesChanged)
			{
				this.wcu = new WorldConfig(this.ScreenManager.verifiedMods);
				this.wcu.loadFromSavegame(savegame);
			}
			this.wcu.updateJWorldConfig();
			if (savegame != null)
			{
				this.worldSeed = savegame.Seed;
				this.playstylelangcode = savegame.PlayStyleLangCode;
				ElementBounds titleElement = ElementBounds.Fixed(0.0, 0.0, 330.0, 80.0);
				ElementBounds leftElement = titleElement.BelowCopy(0.0, 0.0, 0.0, 0.0).WithFixedHeight(35.0);
				double saveWidth = CairoFont.ButtonText().GetTextExtents(Lang.Get("Save", Array.Empty<object>())).Width / (double)RuntimeEnv.GUIScale + 40.0;
				double customizeWidth = CairoFont.ButtonText().GetTextExtents(Lang.Get("Customize", Array.Empty<object>())).Width / (double)RuntimeEnv.GUIScale + 40.0;
				string rectext = Lang.Get("Create a new world with this world seed", Array.Empty<object>());
				double recseedWidth = CairoFont.WhiteSmallText().GetTextExtents(rectext).Width / (double)RuntimeEnv.GUIScale + 40.0;
				string playstyle = ((savegame.PlayStyleLangCode == null) ? Lang.Get("playstyle-" + savegame.PlayStyle, Array.Empty<object>()) : Lang.Get("playstyle-" + savegame.PlayStyleLangCode, Array.Empty<object>()));
				this.ElementComposer = base.dialogBase("mainmenu-singleplayermodifyworld", -1.0, 550.0).AddStaticText(Lang.Get("Modify World", Array.Empty<object>()), CairoFont.WhiteSmallishText(), titleElement, null).AddStaticText(Lang.Get("World name", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftElement, null)
					.AddTextInput(leftElement.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedOffset(0.0, -3.0)
						.WithFixedSize(470.0, 30.0), null, null, "worldname")
					.AddStaticText(Lang.Get("Filename on disk", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftElement = leftElement.BelowCopy(0.0, 0.0, 0.0, 0.0), null)
					.AddTextInput(leftElement.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedOffset(0.0, -3.0)
						.WithFixedSize(470.0, 30.0), null, null, "filename")
					.AddStaticText(Lang.Get("Seed", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftElement = leftElement.BelowCopy(0.0, 4.0, 0.0, 0.0), null)
					.AddStaticText(this.worldSeed.ToString() ?? "", CairoFont.WhiteSmallishText(), EnumTextOrientation.Left, leftElement.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedSize(470.0, 30.0), null)
					.AddIconButton("copy", new Action<bool>(this.OnCopySeed), leftElement.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedSize(30.0, 30.0), null)
					.AddHoverText(Lang.Get("Copies the seed to your clipboard", Array.Empty<object>()), CairoFont.WhiteDetailText(), 200, leftElement.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedSize(30.0, 30.0)
						.WithFixedPadding(5.0), null)
					.AddStaticText(Lang.Get("Total Time Played", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftElement = leftElement.BelowCopy(0.0, 0.0, 0.0, 0.0), null)
					.AddStaticText(GuiScreenSingleplayer.PrettyTime(savegame.TotalSecondsPlayed) ?? "", CairoFont.WhiteSmallishText(), EnumTextOrientation.Left, leftElement.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedSize(470.0, 30.0), null)
					.AddStaticText(Lang.Get("Created with", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftElement = leftElement.BelowCopy(0.0, 0.0, 0.0, 0.0), null)
					.AddStaticText(Lang.Get("versionnumber", new object[] { savegame.CreatedGameVersion }), CairoFont.WhiteSmallishText(), EnumTextOrientation.Left, leftElement.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedSize(470.0, 30.0), null)
					.AddStaticText(Lang.GetWithFallback("singleplayer-world-creator", "Created by", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftElement = leftElement.BelowCopy(0.0, 0.0, 0.0, 0.0), null)
					.AddStaticText(savegame.CreatedByPlayerName, CairoFont.WhiteSmallishText(), EnumTextOrientation.Left, leftElement.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedSize(470.0, 30.0), null)
					.AddStaticText(Lang.Get("Playstyle", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftElement = leftElement.BelowCopy(0.0, 0.0, 0.0, 0.0), null)
					.AddStaticText(playstyle, CairoFont.WhiteSmallishText(), EnumTextOrientation.Left, leftElement.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedSize(470.0, 30.0), null)
					.AddIconButton("copy", new Action<bool>(this.OnCopyPlaystyle), leftElement.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedSize(30.0, 30.0), null)
					.AddHoverText(Lang.Get("Copies the playstyle to your clipboard", Array.Empty<object>()), CairoFont.WhiteDetailText(), 200, leftElement.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedSize(30.0, 30.0)
						.WithFixedPadding(5.0), null)
					.AddStaticText(Lang.Get("World Size", Array.Empty<object>()), CairoFont.WhiteSmallishText(), leftElement = leftElement.BelowCopy(0.0, 0.0, 0.0, 0.0), null)
					.AddStaticText(string.Concat(new string[]
					{
						savegame.MapSizeX.ToString(),
						"x",
						savegame.MapSizeY.ToString(),
						"x",
						savegame.MapSizeZ.ToString()
					}), CairoFont.WhiteSmallishText(), EnumTextOrientation.Left, leftElement.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedSize(470.0, 30.0), null)
					.AddButton(Lang.Get("Back", Array.Empty<object>()), new ActionConsumable(this.OnCancel), ElementStdBounds.Rowed(5.8f, 0.0, EnumDialogArea.LeftFixed).WithFixedAlignmentOffset(0.0, 0.0).WithFixedPadding(10.0, 2.0), EnumButtonStyle.Normal, null)
					.AddButton(Lang.Get("Delete", Array.Empty<object>()), new ActionConsumable(this.OnDelete), ElementStdBounds.Rowed(5.8f, 0.0, EnumDialogArea.RightFixed).WithFixedAlignmentOffset(-saveWidth - customizeWidth, 0.0).WithFixedPadding(10.0, 2.0), EnumButtonStyle.Normal, null)
					.AddButton(Lang.Get("Customize", Array.Empty<object>()), new ActionConsumable(this.OnCustomize), ElementStdBounds.Rowed(5.8f, 0.0, EnumDialogArea.RightFixed).WithFixedAlignmentOffset(-saveWidth, 0.0).WithFixedPadding(10.0, 2.0), EnumButtonStyle.Normal, null)
					.AddButton(Lang.Get("Save", Array.Empty<object>()), new ActionConsumable(this.OnSave), ElementStdBounds.Rowed(5.8f, 0.0, EnumDialogArea.RightFixed).WithFixedPadding(10.0, 2.0), EnumButtonStyle.Normal, null)
					.AddSmallButton(Lang.Get("Create a backup", Array.Empty<object>()), new ActionConsumable(this.OnCreateBackup), ElementStdBounds.Rowed(6.8f, 0.0, EnumDialogArea.RightFixed).WithFixedAlignmentOffset(-recseedWidth - 20.0, 0.0).WithFixedPadding(10.0, 3.0), EnumButtonStyle.Normal, null)
					.AddSmallButton(rectext, new ActionConsumable(this.OnNewWorldWithSeed), ElementStdBounds.Rowed(6.8f, 0.0, EnumDialogArea.RightFixed).WithFixedPadding(10.0, 3.0), EnumButtonStyle.Normal, null)
					.AddSmallButton(Lang.Get("Run in Repair mode", Array.Empty<object>()), new ActionConsumable(this.OnRunInRepairMode), ElementStdBounds.Rowed(7.5f, 0.0, EnumDialogArea.RightFixed).WithFixedPadding(10.0, 3.0), EnumButtonStyle.Normal, null)
					.AddDynamicText("", CairoFont.WhiteSmallishText(), ElementStdBounds.Rowed(8.5f, 0.0, EnumDialogArea.None).WithFixedSize(400.0, 30.0), "dyntextbottom")
					.EndChildElements()
					.Compose(true);
				this.ElementComposer.GetTextInput("worldname").SetValue(savegame.WorldName, true);
				FileInfo file = new FileInfo(this.worldfilename);
				this.ElementComposer.GetTextInput("filename").SetValue(file.Name, true);
				return;
			}
			this.ElementComposer = base.dialogBase("mainmenu-singleplayermodifyworld", -1.0, 550.0).AddStaticText(Lang.Get("singleplayer-modify", Array.Empty<object>()), CairoFont.WhiteSmallishText(), ElementStdBounds.Rowed(0f, 0.0, EnumDialogArea.LeftFixed).WithFixedWidth(300.0), null).AddStaticText(Lang.Get("singleplayer-corrupt", new object[] { this.worldfilename }), CairoFont.WhiteSmallishText(), ElementStdBounds.Rowed(1f, 0.0, EnumDialogArea.LeftFixed).WithFixedWidth(300.0), null)
				.AddButton(Lang.Get("general-cancel", Array.Empty<object>()), new ActionConsumable(this.OnCancel), ElementStdBounds.Rowed(5.2f, 0.0, EnumDialogArea.LeftFixed).WithFixedPadding(10.0, 2.0).WithFixedAlignmentOffset(-10.0, 0.0), EnumButtonStyle.Normal, null)
				.AddButton(Lang.Get("general-delete", Array.Empty<object>()), new ActionConsumable(this.OnDelete), ElementStdBounds.Rowed(5.2f, 0.0, EnumDialogArea.RightFixed).WithFixedPadding(10.0, 2.0), EnumButtonStyle.Normal, null)
				.Compose(true)
				.EndChildElements();
		}

		private void OnCopyPlaystyle(bool ok)
		{
			ScreenManager.Platform.XPlatInterface.SetClipboardText(this.wcu.ToJson());
		}

		public bool OnCreateBackup()
		{
			this.ScreenManager.LoadScreen(new GuiScreenConfirmAction(Lang.Get("Create back up now?", Array.Empty<object>()), new Action<bool>(this.OnDidBackup), this.ScreenManager, this, false));
			return true;
		}

		private void OnDidBackup(bool ok)
		{
			if (ok)
			{
				FileSystemInfo fileSystemInfo = new FileInfo(this.worldfilename);
				string time = string.Format("{0:yyyy-MM-dd_HH-mm-ss}", DateTime.Now);
				string filename = fileSystemInfo.Name.Replace(".vcdbs", "") + "-bkp-" + time + ".vcdbs";
				File.Copy(this.worldfilename, Path.Combine(GamePaths.BackupSaves, filename));
				this.ScreenManager.LoadScreen(this);
				this.ElementComposer.GetDynamicText("dyntextbottom").SetNewText(Lang.Get("Ok, backup created", Array.Empty<object>()), false, false, false);
				return;
			}
			this.ScreenManager.LoadScreen(this);
		}

		public override void OnScreenLoaded()
		{
			base.OnScreenLoaded();
			int num;
			bool flag;
			this.initGui(this.getSaveGame(out num, out flag, false));
		}

		private bool OnCustomize()
		{
			this.ScreenManager.LoadScreen(this.customizeScreen = new GuiScreenWorldCustomize(new Action<bool>(this.OnReturnFromCustomizer), this.ScreenManager, this, this.wcu.Clone(), null));
			return true;
		}

		private void OnReturnFromCustomizer(bool didApply)
		{
			if (didApply)
			{
				this.wcu = this.customizeScreen.wcu;
				this.valuesChanged = true;
			}
			this.ScreenManager.LoadScreen(this);
		}

		private void OnCopySeed(bool copy)
		{
			ScreenManager.Platform.XPlatInterface.SetClipboardText(this.worldSeed.ToString() ?? "");
		}

		private bool OnNewWorldWithSeed()
		{
			this.ScreenManager.LoadAndCacheScreen(typeof(GuiScreenSingleplayerNewWorld));
			GuiScreenSingleplayerNewWorld screen = this.ScreenManager.CurrentScreen as GuiScreenSingleplayerNewWorld;
			int i = 0;
			using (List<PlaystyleListEntry>.Enumerator enumerator = screen.cells.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current.PlayStyle.LangCode == this.playstylelangcode)
					{
						screen.OnClickCellLeft(i);
						break;
					}
					i++;
				}
			}
			screen.OnCustomize();
			GuiScreenWorldCustomize screen2 = this.ScreenManager.CurrentScreen as GuiScreenWorldCustomize;
			if (screen2 == null)
			{
				return false;
			}
			screen2.ElementComposer.GetTextInput("worldseed").SetValue(this.worldSeed.ToString() ?? "", true);
			return true;
		}

		private bool OnRunInRepairMode()
		{
			this.ScreenManager.LoadScreen(new GuiScreenConfirmAction(Lang.Get("confirm-repairmode", Array.Empty<object>()), delegate(bool val)
			{
				if (val)
				{
					this.repairGame();
					return;
				}
				this.ScreenManager.LoadScreen(this);
			}, this.ScreenManager, this, false));
			return true;
		}

		private void repairGame()
		{
			int version;
			bool isreadonly;
			this.getSaveGame(out version, out isreadonly, false);
			if (version != GameVersion.DatabaseVersion)
			{
				this.ScreenManager.LoadScreen(new GuiScreenConfirmAction(Lang.Get("This world uses an old file format that needs upgrading. This might take a while. It is also suggested to first back up your savegame in case the upgrade fails. Proceed?", Array.Empty<object>()), new Action<bool>(this.OnDidConfirmUpgrade), this.ScreenManager, this, false));
				return;
			}
			if (isreadonly)
			{
				this.ScreenManager.LoadScreen(new GuiScreenConfirmAction(Lang.Get("Have no write access to this file, it seems in use. Make sure no other client or server is currently using this savegame.", Array.Empty<object>()), delegate(bool val)
				{
					this.ScreenManager.LoadScreen(this);
				}, this.ScreenManager, this, true));
				return;
			}
			this.ScreenManager.ConnectToSingleplayer(new StartServerArgs
			{
				SaveFileLocation = this.worldfilename,
				DisabledMods = ClientSettings.DisabledMods,
				Language = ClientSettings.Language,
				RepairMode = true
			});
		}

		private void OnDidConfirmUpgrade(bool confirm)
		{
			if (confirm)
			{
				this.ScreenManager.ConnectToSingleplayer(new StartServerArgs
				{
					SaveFileLocation = this.worldfilename,
					DisabledMods = ClientSettings.DisabledMods,
					Language = ClientSettings.Language,
					RepairMode = true
				});
				return;
			}
			this.ScreenManager.LoadScreen(this);
		}

		private bool OnSave()
		{
			FileInfo file = new FileInfo(this.worldfilename);
			string nowFileName = this.ElementComposer.GetTextInput("filename").GetText();
			if (file.Name != nowFileName)
			{
				try
				{
					file.MoveTo(this.worldfilename = Path.Combine(file.DirectoryName, nowFileName));
				}
				catch (Exception)
				{
				}
			}
			string errorMessage;
			if (!this.gamedb.OpenConnection(file.FullName, out errorMessage, false, false))
			{
				this.ScreenManager.LoadScreen(new GuiScreenMessage(Lang.Get("singleplayer-failedchanges", Array.Empty<object>()), Lang.Get("singleplayer-maybecorrupt", new object[] { errorMessage }), new Action(this.OnMessageConfirmed), this.ScreenManager, this));
				return true;
			}
			int num;
			bool flag;
			SaveGame savegame = this.getSaveGame(out num, out flag, true);
			savegame.WorldName = this.ElementComposer.GetTextInput("worldname").GetText();
			if (this.valuesChanged)
			{
				foreach (string key in this.wcu.WorldConfigsPlaystyle.Keys.ToList<string>().Concat(this.wcu.WorldConfigsCustom.Keys))
				{
					WorldConfigurationValue configValue = this.wcu[key];
					if (configValue != null)
					{
						switch (configValue.Attribute.DataType)
						{
						case EnumDataType.Bool:
							savegame.WorldConfiguration.SetBool(key, (bool)configValue.Value);
							break;
						case EnumDataType.IntInput:
						case EnumDataType.IntRange:
							savegame.WorldConfiguration.SetInt(key, (int)configValue.Value);
							break;
						case EnumDataType.DoubleInput:
						case EnumDataType.DoubleRange:
							savegame.WorldConfiguration.SetDouble(key, (double)configValue.Value);
							break;
						case EnumDataType.String:
						case EnumDataType.DropDown:
						case EnumDataType.StringRange:
							savegame.WorldConfiguration.SetString(key, (string)configValue.Value);
							break;
						}
					}
				}
				savegame.WillSave(new FastMemoryStream());
				this.valuesChanged = false;
			}
			this.gamedb.StoreSaveGame(savegame);
			this.gamedb.CloseConnection();
			this.gamedb.Dispose();
			this.ScreenManager.LoadAndCacheScreen(typeof(GuiScreenSingleplayer));
			return true;
		}

		private void OnMessageConfirmed()
		{
			this.ScreenManager.LoadAndCacheScreen(typeof(GuiScreenSingleplayer));
		}

		private bool OnDelete()
		{
			this.ScreenManager.LoadScreen(new GuiScreenConfirmAction(Lang.Get("Really delete world '{0}'?", new object[] { this.worldfilename }), new Action<bool>(this.OnDidConfirmDelete), this.ScreenManager, this, false));
			return true;
		}

		private void OnDidConfirmDelete(bool confirm)
		{
			if (confirm)
			{
				this.ScreenManager.GamePlatform.XPlatInterface.MoveFileToRecyclebin(this.worldfilename);
				this.ScreenManager.LoadAndCacheScreen(typeof(GuiScreenSingleplayer));
				return;
			}
			this.ScreenManager.LoadScreen(this);
		}

		private bool OnCancel()
		{
			this.ScreenManager.LoadAndCacheScreen(typeof(GuiScreenSingleplayer));
			return true;
		}

		private string worldfilename;

		private int worldSeed;

		private string playstylelangcode;

		private GameDatabase gamedb;

		private bool valuesChanged;

		private WorldConfig wcu;

		private GuiScreenWorldCustomize customizeScreen;
	}
}
