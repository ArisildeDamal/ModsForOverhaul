using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;
using Vintagestory.ModDb;

namespace Vintagestory.Client
{
	public class GuiScreenOnlineMods : GuiScreen
	{
		public GuiScreenOnlineMods(ScreenManager screenManager, GuiScreen parentScreen)
			: base(screenManager, parentScreen)
		{
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
			this.modDbUtil = new ModDbUtil(screenManager.api, ClientSettings.ModDbUrl, GamePaths.DataPathMods);
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
			this.Search();
			this.listBounds.CalcWorldBounds();
			this.clippingBounds.CalcWorldBounds();
			this.ElementComposer.GetScrollbar("scrollbar").SetHeights((float)this.clippingBounds.fixedHeight, (float)this.listBounds.fixedHeight);
		}

		private void Search()
		{
			this.modDbUtil.Search(this.searchText, new Action<ModSearchResult>(this.onDone), Array.Empty<int>(), null, "trendingpoints", 25);
		}

		private List<ModCellEntry> LoadModCells(ModDbEntrySearchResponse[] mods)
		{
			List<ModCellEntry> cells = new List<ModCellEntry>();
			foreach (ModDbEntrySearchResponse mod in mods)
			{
				CairoFont font = CairoFont.WhiteDetailText();
				font.WithFontSize((float)GuiStyle.SmallFontSize);
				if (!(mod.Type != "mod"))
				{
					cells.Add(new ModCellEntry
					{
						Title = mod.Name,
						RightTopText = mod.Downloads.ToString() + " downloads",
						RightTopOffY = 3f,
						DetailText = mod.Author,
						Enabled = false,
						DetailTextFont = font
					});
				}
			}
			return cells;
		}

		private void onDone(ModSearchResult searchResult)
		{
			if (searchResult.Mods == null)
			{
				this.modCells = new List<ModCellEntry>
				{
					new ModCellEntry
					{
						Title = searchResult.StatusMessage
					}
				};
			}
			else
			{
				this.modCells = this.LoadModCells(searchResult.Mods);
			}
			this.ElementComposer.GetCellList("modstable").ReloadCells(this.modCells);
		}

		private void InitGui()
		{
			int windowHeight = this.ScreenManager.GamePlatform.WindowSize.Height;
			int windowWidth = this.ScreenManager.GamePlatform.WindowSize.Width;
			ElementBounds titleBounds = ElementBounds.Fixed(EnumDialogArea.LeftTop, 0.0, 0.0, 690.0, 35.0);
			ElementBounds searchFieldBounds = ElementBounds.Fixed(0.0, 0.0, 200.0, 30.0).FixedUnder(titleBounds, 10.0);
			ElementBounds buttonBounds = ElementBounds.FixedSize(60.0, 30.0).WithFixedPadding(10.0, 2.0).WithAlignment(EnumDialogArea.RightFixed);
			ElementBounds insetBounds = searchFieldBounds.BelowCopy(0.0, 10.0, 0.0, 0.0).WithFixedSize(Math.Max(400.0, (double)windowWidth * 0.5) / (double)ClientSettings.GUIScale, (double)((float)Math.Max(300, windowHeight) / ClientSettings.GUIScale - 145f));
			double num = Math.Max(400.0, (double)windowWidth * 0.5) / (double)ClientSettings.GUIScale;
			GuiComposer elementComposer = this.ElementComposer;
			if (elementComposer != null)
			{
				elementComposer.Dispose();
			}
			this.ElementComposer = base.dialogBase("mainmenu-onlinemods", -1.0, -1.0).AddStaticText(Lang.Get("All mods from the VS Mod DB (work in progress)", Array.Empty<object>()), CairoFont.WhiteSmallishText(), titleBounds, null).AddTextInput(searchFieldBounds, null, null, "search")
				.AddSmallButton(Lang.Get("Search", Array.Empty<object>()), new ActionConsumable(this.OnSearch), buttonBounds.FlatCopy().FixedUnder(titleBounds, 10.0).FixedRightOf(searchFieldBounds, 10.0)
					.WithAlignment(EnumDialogArea.LeftFixed), EnumButtonStyle.Normal, null)
				.AddInset(insetBounds, 4, 0.85f)
				.AddVerticalScrollbar(new Action<float>(this.OnNewScrollbarvalue), ElementStdBounds.VerticalScrollbar(insetBounds), "scrollbar")
				.BeginClip(this.clippingBounds = insetBounds.ForkContainingChild(3.0, 3.0, 3.0, 3.0))
				.AddCellList(this.listBounds = this.clippingBounds.ForkContainingChild(0.0, 0.0, 0.0, -3.0).WithFixedPadding(10.0), new OnRequireCell<ModCellEntry>(this.createCellElem), this.modCells, "modstable")
				.EndClip()
				.AddSmallButton(Lang.Get("Back", Array.Empty<object>()), new ActionConsumable(this.OnBack), buttonBounds.FlatCopy().FixedUnder(insetBounds, 10.0).WithAlignment(EnumDialogArea.LeftFixed), EnumButtonStyle.Normal, null)
				.AddSmallButton(Lang.Get("Install", Array.Empty<object>()), new ActionConsumable(this.OnInstall), buttonBounds.FlatCopy().FixedUnder(insetBounds, 10.0).WithFixedAlignmentOffset(-13.0, 0.0), EnumButtonStyle.Normal, null)
				.EndChildElements()
				.Compose(true);
			this.listBounds.CalcWorldBounds();
			this.clippingBounds.CalcWorldBounds();
			this.ElementComposer.GetScrollbar("scrollbar").SetHeights((float)this.clippingBounds.fixedHeight, (float)this.listBounds.fixedHeight);
		}

		private bool OnInstall()
		{
			return true;
		}

		private bool OnBack()
		{
			this.ScreenManager.LoadScreen(this.ParentScreen);
			return true;
		}

		private bool OnSearch()
		{
			this.searchText = this.ElementComposer.GetTextInput("search").GetText();
			this.Search();
			return true;
		}

		private IGuiElementCell createCellElem(ModCellEntry cell, ElementBounds bounds)
		{
			return new GuiElementModCell(this.ScreenManager.api, cell, bounds, null)
			{
				On = cell.Enabled
			};
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

		private ModDbUtil modDbUtil;

		private List<ModCellEntry> modCells;

		private string searchText = "";
	}
}
