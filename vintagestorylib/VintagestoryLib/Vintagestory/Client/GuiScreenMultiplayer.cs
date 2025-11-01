using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;

namespace Vintagestory.Client
{
	public class GuiScreenMultiplayer : GuiScreen
	{
		public GuiScreenMultiplayer(ScreenManager screenManager, GuiScreen parent)
			: base(screenManager, parent)
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
		}

		private void invalidate()
		{
			if (base.IsOpened)
			{
				this.InitGui();
				return;
			}
			ScreenManager.GuiComposers.Dispose("mainmenu-multiplayer");
		}

		private void InitGui()
		{
			List<SavegameCellEntry> cells = this.LoadServerEntries();
			int windowHeight = this.ScreenManager.GamePlatform.WindowSize.Height;
			int windowWidth = this.ScreenManager.GamePlatform.WindowSize.Width;
			ElementBounds buttonBounds = ElementBounds.FixedSize(60.0, 30.0).WithFixedPadding(10.0, 2.0);
			ElementBounds titleBounds = ElementBounds.Fixed(EnumDialogArea.LeftTop, 0.0, (double)(this.ScreenManager.ClientIsOffline ? 30 : 0), 690.0, 35.0);
			float height = (float)Math.Max(300, windowHeight) / ClientSettings.GUIScale;
			ElementBounds insetBounds;
			this.ElementComposer = base.dialogBase("mainmenu-multiplayer", -1.0, -1.0).AddStaticText(Lang.Get("multiplayer-yourservers", Array.Empty<object>()), CairoFont.WhiteSmallishText(), ElementBounds.Fixed(EnumDialogArea.LeftTop, 0.0, 0.0, 690.0, 240.0), null).AddIf(this.ScreenManager.ClientIsOffline)
				.AddRichtext(Lang.Get("offlinemultiplayerwarning", Array.Empty<object>()), CairoFont.WhiteSmallishText(), ElementBounds.Fixed(0.0, 25.0, 690.0, 30.0), null)
				.EndIf()
				.AddInset(insetBounds = titleBounds.BelowCopy(0.0, 3.0, 0.0, 0.0).WithFixedSize(Math.Max(400.0, (double)windowWidth * 0.5) / (double)ClientSettings.GUIScale, (double)(height - 250f)), 4, 0.85f)
				.AddVerticalScrollbar(new Action<float>(this.OnNewScrollbarvalue), ElementStdBounds.VerticalScrollbar(insetBounds), "scrollbar")
				.BeginClip(this.clippingBounds = insetBounds.ForkContainingChild(3.0, 3.0, 3.0, 3.0))
				.AddCellList(this.tableBounds = this.clippingBounds.ForkContainingChild(0.0, 0.0, 0.0, -3.0).WithFixedPadding(5.0), new OnRequireCell<SavegameCellEntry>(this.createCellElem), cells, "serverstable")
				.EndClip()
				.AddButton(Lang.Get("multiplayer-addserver", Array.Empty<object>()), new ActionConsumable(this.OnAddServer), buttonBounds.FlatCopy().FixedUnder(insetBounds, 10.0).WithAlignment(EnumDialogArea.RightFixed)
					.WithFixedAlignmentOffset(-13.0, 0.0), EnumButtonStyle.Normal, null)
				.AddButton(Lang.Get("multiplayer-browsepublicservers", Array.Empty<object>()), new ActionConsumable(this.OnPublicListing), buttonBounds.FlatCopy().FixedUnder(insetBounds, 10.0), EnumButtonStyle.Normal, null)
				.AddSmallButton(Lang.Get("multiplayer-selfhosting", Array.Empty<object>()), new ActionConsumable(this.OnSelfHosting), buttonBounds.FlatCopy().FixedUnder(insetBounds, 60.0), EnumButtonStyle.Normal, null)
				.EndChildElements()
				.Compose(true);
			this.tableBounds.CalcWorldBounds();
			this.clippingBounds.CalcWorldBounds();
			this.ElementComposer.GetScrollbar("scrollbar").SetHeights((float)this.clippingBounds.fixedHeight, (float)this.tableBounds.fixedHeight);
		}

		private IGuiElementCell createCellElem(SavegameCellEntry cell, ElementBounds bounds)
		{
			GuiElementMainMenuCell guiElementMainMenuCell = new GuiElementMainMenuCell(this.ScreenManager.api, cell, bounds);
			cell.LeftOffY = -2f;
			guiElementMainMenuCell.OnMouseDownOnCellLeft = new Action<int>(this.OnClickCellLeft);
			guiElementMainMenuCell.OnMouseDownOnCellRight = new Action<int>(this.OnClickCellRight);
			return guiElementMainMenuCell;
		}

		private bool OnSelfHosting()
		{
			this.ScreenManager.api.Gui.OpenLink("https://www.vintagestory.at/multiplayer");
			return true;
		}

		private bool OnPublicListing()
		{
			this.ScreenManager.LoadAndCacheScreen(typeof(GuiScreenPublicServers));
			return true;
		}

		public override void OnScreenLoaded()
		{
			this.InitGui();
			this.ElementComposer.GetCellList("serverstable").ReloadCells(this.LoadServerEntries());
			this.tableBounds.CalcWorldBounds();
			this.clippingBounds.CalcWorldBounds();
			this.ElementComposer.GetScrollbar("scrollbar").SetHeights((float)this.clippingBounds.fixedHeight, (float)this.tableBounds.fixedHeight);
		}

		private List<SavegameCellEntry> LoadServerEntries()
		{
			this.serverentries = new List<MultiplayerServerEntry>();
			List<string> entries = ClientSettings.Inst.GetStringListSetting("multiplayerservers", new List<string>());
			List<SavegameCellEntry> cells = new List<SavegameCellEntry>();
			for (int i = 0; i < entries.Count; i++)
			{
				string[] elems = entries[i].Split(',', StringSplitOptions.None);
				MultiplayerServerEntry serverentry = new MultiplayerServerEntry
				{
					index = i,
					name = elems[0],
					host = elems[1],
					password = ((elems.Length > 2) ? elems[2] : "")
				};
				this.serverentries.Add(serverentry);
				SavegameCellEntry cell = new SavegameCellEntry
				{
					Title = serverentry.name
				};
				cells.Add(cell);
			}
			return cells;
		}

		private void OnNewScrollbarvalue(float value)
		{
			ElementBounds bounds = this.ElementComposer.GetCellList("serverstable").Bounds;
			bounds.fixedY = (double)(0f - value);
			bounds.CalcWorldBounds();
		}

		private void OnClickCellLeft(int index)
		{
			MultiplayerServerEntry entry = this.serverentries[index];
			this.ScreenManager.ConnectToMultiplayer(entry.host, entry.password);
		}

		private void OnClickCellRight(int cellIndex)
		{
			this.ScreenManager.LoadScreen(new GuiScreenMultiplayerModify(this.serverentries[cellIndex], this.ScreenManager, this));
		}

		private bool OnAddServer()
		{
			this.ScreenManager.LoadAndCacheScreen(typeof(GuiScreenMultiplayerNewServer));
			return true;
		}

		private List<MultiplayerServerEntry> serverentries;

		private ElementBounds tableBounds;

		private ElementBounds clippingBounds;
	}
}
