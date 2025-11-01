using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;
using Vintagestory.ModDb;

namespace Vintagestory.Client
{
	public class GuiScreenMods : GuiScreen
	{
		public GuiScreenMods(ScreenManager screenManager, GuiScreen parentScreen)
			: base(screenManager, parentScreen)
		{
			this.screenManager = screenManager;
			this.ShowMainMenu = true;
			this.InitGui();
			screenManager.GamePlatform.WindowResized += delegate(int w, int h)
			{
				this.invalidate();
			};
			ClientSettings.Inst.AddWatcher<float>("guiScale", delegate(float s)
			{
				this.invalidate();
			});
			this.warningIcon = this.ScreenManager.api.Assets.Get(new AssetLocation("textures/icons/warning.svg"));
		}

		private void invalidate()
		{
			if (base.IsOpened)
			{
				this.InitGui();
				return;
			}
			ScreenManager.GuiComposers.Dispose("mainmenu-mods");
		}

		public override void OnScreenLoaded()
		{
			if (this.ingoreLoadOnce)
			{
				this.ingoreLoadOnce = false;
				return;
			}
			this.InitGui();
			this.ElementComposer.GetCellList("modstable").ReloadCells(this.LoadModCells());
			this.listBounds.CalcWorldBounds();
			this.clippingBounds.CalcWorldBounds();
			this.ElementComposer.GetScrollbar("scrollbar").SetHeights((float)this.clippingBounds.fixedHeight, (float)this.listBounds.fixedHeight);
		}

		private List<ModCellEntry> LoadModCells()
		{
			List<string> disabledMods = ClientSettings.DisabledMods;
			List<ModCellEntry> cells = new List<ModCellEntry>();
			List<ModContainer> mods = this.ScreenManager.allMods;
			if (!ClientSettings.DisableModSafetyCheck)
			{
				while (ModDbUtil.ModBlockList == null)
				{
					Thread.Sleep(20);
				}
			}
			foreach (ModContainer mod in mods)
			{
				ModInfo modinfo = mod.Info;
				CairoFont font = CairoFont.WhiteDetailText();
				font.WithFontSize((float)GuiStyle.SmallFontSize);
				string reason = string.Empty;
				if (mod.Error == null && ModDbUtil.IsModBlocked(mod.Info.ModID, mod.Info.Version, out reason))
				{
					mod.SetError(ModError.Blocked);
				}
				if (mod.Error != null)
				{
					ModError? error = mod.Error;
					if (error != null)
					{
						string errorText;
						switch (error.GetValueOrDefault())
						{
						case ModError.Loading:
							errorText = Lang.Get("Unable to load mod. Check log files.", Array.Empty<object>());
							break;
						case ModError.Dependency:
							if (mod.MissingDependencies == null)
							{
								errorText = Lang.Get("Unable to load mod. A dependency has an error. Make sure they all load correctly.", Array.Empty<object>());
							}
							else if (mod.MissingDependencies.Count == 1)
							{
								string text = "Unable to load mod. Requires dependency {0}";
								object[] array = new object[1];
								array[0] = string.Join(", ", mod.MissingDependencies.Select((string str) => str.Replace("@", " v")));
								errorText = Lang.Get(text, array);
							}
							else
							{
								string text2 = "Unable to load mod. Requires dependencies {0}";
								object[] array2 = new object[1];
								array2[0] = string.Join(", ", mod.MissingDependencies.Select((string str) => str.Replace("@", " v")));
								errorText = Lang.Get(text2, array2);
							}
							break;
						case ModError.ChangedVersion:
							goto IL_01E8;
						case ModError.Blocked:
							errorText = Lang.Get("modloader-blockedmod", new object[] { reason });
							break;
						default:
							goto IL_01E8;
						}
						List<ModCellEntry> list = cells;
						ModCellEntry modCellEntry = new ModCellEntry();
						modCellEntry.Title = mod.FileName;
						modCellEntry.DetailText = errorText;
						List<string> list2 = disabledMods;
						ModInfo info = mod.Info;
						string text3 = ((info != null) ? info.ModID : null);
						string text4 = "@";
						ModInfo info2 = mod.Info;
						modCellEntry.Enabled = !list2.Contains(text3 + text4 + ((info2 != null) ? info2.Version : null)) && mod.Error.GetValueOrDefault() != ModError.Blocked;
						modCellEntry.Mod = mod;
						modCellEntry.DetailTextFont = font;
						list.Add(modCellEntry);
						continue;
					}
					IL_01E8:
					throw new InvalidOperationException();
				}
				StringBuilder descriptionBuilder = new StringBuilder();
				if (modinfo.Authors.Count > 0)
				{
					descriptionBuilder.AppendLine(string.Join(", ", modinfo.Authors));
				}
				if (!string.IsNullOrEmpty(modinfo.Description))
				{
					descriptionBuilder.AppendLine(modinfo.Description);
				}
				cells.Add(new ModCellEntry
				{
					Title = modinfo.Name + " (" + modinfo.Type.ToString() + ")",
					RightTopText = ((!string.IsNullOrEmpty(modinfo.Version)) ? modinfo.Version : "--"),
					RightTopOffY = 3f,
					DetailText = descriptionBuilder.ToString().Trim(),
					Enabled = !disabledMods.Contains(mod.Info.ModID + "@" + mod.Info.Version),
					Mod = mod,
					DetailTextFont = font
				});
			}
			return cells;
		}

		private void InitGui()
		{
			int windowHeight = this.ScreenManager.GamePlatform.WindowSize.Height;
			int windowWidth = this.ScreenManager.GamePlatform.WindowSize.Width;
			ElementBounds buttonBounds = ElementBounds.FixedSize(60.0, 30.0).WithFixedPadding(10.0, 2.0).WithAlignment(EnumDialogArea.RightFixed);
			ElementBounds titleBounds = ElementBounds.Fixed(EnumDialogArea.LeftTop, 0.0, 0.0, 690.0, 35.0);
			double num = Math.Max(400.0, (double)windowWidth * 0.5) / (double)ClientSettings.GUIScale;
			float height = (float)Math.Max(300, windowHeight) / ClientSettings.GUIScale;
			GuiComposer elementComposer = this.ElementComposer;
			if (elementComposer != null)
			{
				elementComposer.Dispose();
			}
			ElementBounds insetBounds;
			this.ElementComposer = base.dialogBase("mainmenu-mods", -1.0, -1.0).AddStaticText(Lang.Get("Installed mods", Array.Empty<object>()), CairoFont.WhiteSmallishText(), ElementBounds.Fixed(EnumDialogArea.LeftTop, 0.0, 0.0, 690.0, 40.0), null).AddInset(insetBounds = titleBounds.BelowCopy(0.0, 3.0, 0.0, 0.0).WithFixedSize(Math.Max(400.0, (double)windowWidth * 0.5) / (double)ClientSettings.GUIScale, (double)(height - 190f)), 4, 0.85f)
				.AddVerticalScrollbar(new Action<float>(this.OnNewScrollbarvalue), ElementStdBounds.VerticalScrollbar(insetBounds), "scrollbar")
				.BeginClip(this.clippingBounds = insetBounds.ForkContainingChild(3.0, 3.0, 3.0, 3.0))
				.AddCellList(this.listBounds = this.clippingBounds.ForkContainingChild(0.0, 0.0, 0.0, -3.0).WithFixedPadding(5.0), new OnRequireCell<ModCellEntry>(this.createCellElem), this.LoadModCells(), "modstable")
				.EndClip()
				.AddSmallButton(Lang.Get("Reload Mods", Array.Empty<object>()), new ActionConsumable(this.OnReloadMods), buttonBounds.FlatCopy().FixedUnder(insetBounds, 10.0).WithFixedAlignmentOffset(-13.0, 0.0), EnumButtonStyle.Normal, null)
				.AddSmallButton(Lang.Get("Open Mods Folder", Array.Empty<object>()), new ActionConsumable(this.OnOpenModsFolder), buttonBounds.FlatCopy().FixedUnder(insetBounds, 10.0).WithFixedAlignmentOffset(-150.0, 0.0), EnumButtonStyle.Normal, null)
				.EndChildElements()
				.Compose(true);
			this.listBounds.CalcWorldBounds();
			this.clippingBounds.CalcWorldBounds();
			this.ElementComposer.GetScrollbar("scrollbar").SetHeights((float)this.clippingBounds.fixedHeight, (float)this.listBounds.fixedHeight);
		}

		private bool OnBrowseOnlineMods()
		{
			this.ScreenManager.LoadScreen(new GuiScreenOnlineMods(this.ScreenManager, this));
			return true;
		}

		private IGuiElementCell createCellElem(ModCellEntry cell, ElementBounds bounds)
		{
			GuiElementModCell cellElem = new GuiElementModCell(this.ScreenManager.api, cell, bounds, this.warningIcon)
			{
				On = cell.Enabled
			};
			if (cell.Mod.Error.GetValueOrDefault() != ModError.Blocked)
			{
				cellElem.OnMouseDownOnCellRight = new Action<int>(this.OnClickCellRight);
			}
			cellElem.OnMouseDownOnCellLeft = new Action<int>(this.OnClickCellLeft);
			return cellElem;
		}

		private bool OnReloadMods()
		{
			this.ScreenManager.loadMods();
			this.OnScreenLoaded();
			return true;
		}

		private bool OnOpenModsFolder()
		{
			NetUtil.OpenUrlInBrowser(GamePaths.DataPathMods);
			return true;
		}

		private void OnClickCellRight(int cellIndex)
		{
			GuiElementModCell guicell = (GuiElementModCell)this.ElementComposer.GetCellList("modstable").elementCells[cellIndex];
			ModContainer mod = guicell.cell.Mod;
			if (mod.Info != null && mod.Info.CoreMod && mod.Status == ModStatus.Enabled)
			{
				this.ShowConfirmationDialog(guicell, mod);
				return;
			}
			this.SwitchModStatus(guicell, mod);
		}

		private void SwitchModStatus(GuiElementModCell guicell, ModContainer mod)
		{
			guicell.On = !guicell.On;
			if (mod.Status == ModStatus.Enabled || mod.Status == ModStatus.Disabled)
			{
				mod.Status = (guicell.On ? ModStatus.Enabled : ModStatus.Disabled);
			}
			List<string> disabledMods = ClientSettings.DisabledMods;
			if (mod.Info == null)
			{
				return;
			}
			disabledMods.Remove(mod.Info.ModID + "@" + mod.Info.Version);
			if (!guicell.On)
			{
				disabledMods.Add(mod.Info.ModID + "@" + mod.Info.Version);
			}
			ClientSettings.DisabledMods = disabledMods;
			ClientSettings.Inst.Save(true);
		}

		private void ShowConfirmationDialog(GuiElementModCell guicell, ModContainer mod)
		{
			this.screenManager.LoadScreen(new GuiScreenConfirmAction("coremod-warningtitle", Lang.Get("coremod-warning", new object[] { mod.Info.Name }), "general-back", "Confirm", delegate(bool val)
			{
				if (val)
				{
					this.SwitchModStatus(guicell, mod);
				}
				this.screenManager.LoadScreen(this);
			}, this.screenManager, this, "coremod-confirmation", false));
		}

		private void OnClickCellLeft(int cellIndex)
		{
		}

		private void OnNewScrollbarvalue(float value)
		{
			ElementBounds bounds = this.ElementComposer.GetCellList("modstable").Bounds;
			bounds.fixedY = (double)(0f - value);
			bounds.CalcWorldBounds();
		}

		private bool ingoreLoadOnce = true;

		private ElementBounds listBounds;

		private ElementBounds clippingBounds;

		private IAsset warningIcon;

		private ScreenManager screenManager;
	}
}
