using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf
{
	public class GuiDialogInventory : GuiDialog
	{
		public override double DrawOrder
		{
			get
			{
				return 0.2;
			}
		}

		public override string ToggleKeyCombinationCode
		{
			get
			{
				return "inventorydialog";
			}
		}

		public GuiDialogInventory(ICoreClientAPI capi)
			: base(capi)
		{
			(capi.World as ClientMain).eventManager.OnPlayerModeChange.Add(new Action(this.OnPlayerModeChanged));
			capi.Input.RegisterHotKey("creativesearch", Lang.Get("Search Creative inventory", Array.Empty<object>()), GlKeys.F, HotkeyType.CreativeTool, false, true, false);
			capi.Input.SetHotKeyHandler("creativesearch", new ActionConsumable<KeyCombination>(this.onSearchCreative));
		}

		private bool onSearchCreative(KeyCombination t1)
		{
			if (this.capi.World.Player.WorldData.CurrentGameMode != EnumGameMode.Creative)
			{
				return false;
			}
			if (this.TryOpen())
			{
				this.creativeInvDialog.FocusElement(this.creativeInvDialog.GetTextInput("searchbox").TabIndex);
			}
			return true;
		}

		public override void OnOwnPlayerDataReceived()
		{
			this.capi.Logger.VerboseDebug("GuiDialogInventory: starting composeGUI");
			this.ComposeGui(true);
			this.capi.Logger.VerboseDebug("GuiDialogInventory: done composeGUI");
			TyronThreadPool.QueueTask(delegate
			{
				this.creativeInv.CreativeTabs.CreateSearchCache(this.capi.World);
			});
			this.prevGameMode = this.capi.World.Player.WorldData.CurrentGameMode;
		}

		public void ComposeGui(bool firstBuild)
		{
			IPlayerInventoryManager invm = this.capi.World.Player.InventoryManager;
			this.creativeInv = (ITabbedInventory)invm.GetOwnInventory("creative");
			this.craftingInv = invm.GetOwnInventory("craftinggrid");
			this.backPackInv = invm.GetOwnInventory("backpack");
			if (firstBuild)
			{
				this.backPackInv.SlotModified += this.BackPackInv_SlotModified;
			}
			if (this.capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Creative)
			{
				this.ComposeCreativeInvDialog();
				this.Composers["maininventory"] = this.creativeInvDialog;
			}
			if (this.capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Guest || this.capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Survival)
			{
				this.ComposeSurvivalInvDialog();
				this.Composers["maininventory"] = this.survivalInvDialog;
			}
			if (firstBuild)
			{
				this.OnPlayerModeChanged();
			}
		}

		private void ComposeCreativeInvDialog()
		{
			GuiDialogInventory.<>c__DisplayClass16_0 CS$<>8__locals1 = new GuiDialogInventory.<>c__DisplayClass16_0();
			CS$<>8__locals1.<>4__this = this;
			if (this.creativeInv == null)
			{
				ScreenManager.Platform.Logger.Notification("Server did not send a creative inventory, so I won't display one");
				return;
			}
			double elemToDlgPad = GuiStyle.ElementToDialogPadding;
			double pad = GuiElementItemSlotGridBase.unscaledSlotPadding;
			int rows = (int)Math.Ceiling((double)((float)this.creativeInv.Count / (float)this.cols));
			ElementBounds slotGridBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, pad, pad, this.cols, 9).FixedGrow(2.0 * pad, 2.0 * pad);
			ElementBounds fullGridBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, 0.0, this.cols, rows);
			this.creativeClippingBounds = slotGridBounds.ForkBoundingParent(0.0, 0.0, 0.0, 0.0);
			this.creativeClippingBounds.Name = "clip";
			ElementBounds insetBounds = this.creativeClippingBounds.ForkBoundingParent(6.0, 3.0, 0.0, 3.0);
			insetBounds.Name = "inset";
			ElementBounds dialogBounds = insetBounds.ForkBoundingParent(elemToDlgPad, elemToDlgPad + 70.0, elemToDlgPad + 31.0, elemToDlgPad).WithFixedAlignmentOffset(-3.0, -100.0).WithAlignment(EnumDialogArea.CenterBottom);
			ElementBounds scrollbarBounds = ElementStdBounds.VerticalScrollbar(insetBounds).WithParent(dialogBounds);
			ElementBounds textInputBounds = ElementBounds.Fixed(elemToDlgPad, 45.0, 250.0, 30.0);
			ElementBounds tabBoundsL = ElementBounds.Fixed(-130.0, 35.0, 130.0, 545.0);
			ElementBounds tabBoundsR = ElementBounds.Fixed(0.0, 35.0, 130.0, 545.0).FixedRightOf(dialogBounds, 0.0).WithFixedAlignmentOffset(-4.0, 0.0);
			ElementBounds rightTextBounds = ElementBounds.Fixed(elemToDlgPad, 45.0, 250.0, 30.0).WithAlignment(EnumDialogArea.RightFixed).WithFixedAlignmentOffset(-28.0 - elemToDlgPad, 7.0);
			CreativeTabsConfig creativeTabsConfig = this.capi.Assets.TryGet("config/creativetabs.json", true).ToObject<CreativeTabsConfig>(null);
			IEnumerable<CreativeTab> unorderedTabs = this.creativeInv.CreativeTabs.Tabs;
			CS$<>8__locals1.orderedTabs = new List<TabConfig>();
			using (IEnumerator<CreativeTab> enumerator = unorderedTabs.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					CreativeTab tab2 = enumerator.Current;
					TabConfig tabcfg = creativeTabsConfig.TabConfigs.FirstOrDefault((TabConfig cfg) => cfg.Code == tab2.Code);
					if (tabcfg == null)
					{
						tabcfg = new TabConfig
						{
							Code = tab2.Code,
							ListOrder = 1.0
						};
					}
					int pos = 0;
					int j = 0;
					while (j < CS$<>8__locals1.orderedTabs.Count && CS$<>8__locals1.orderedTabs[j].ListOrder < tabcfg.ListOrder)
					{
						pos++;
						j++;
					}
					CS$<>8__locals1.orderedTabs.Insert(pos, tabcfg);
				}
			}
			int currentGuiTabIndex = 0;
			CS$<>8__locals1.tabs = new GuiTab[CS$<>8__locals1.orderedTabs.Count];
			double maxWidth = 0.0;
			double padding = GuiElement.scaled(3.0);
			CairoFont font = CairoFont.WhiteDetailText().WithFontSize(17f);
			int i;
			int i2;
			for (i = 0; i < CS$<>8__locals1.orderedTabs.Count; i = i2 + 1)
			{
				int tabIndex = unorderedTabs.FirstOrDefault((CreativeTab tab) => tab.Code == CS$<>8__locals1.orderedTabs[i].Code).Index;
				if (tabIndex == this.currentTabIndex)
				{
					currentGuiTabIndex = i;
				}
				CS$<>8__locals1.tabs[i] = new GuiTab
				{
					DataInt = tabIndex,
					Name = Lang.Get("tabname-" + CS$<>8__locals1.orderedTabs[i].Code, Array.Empty<object>()),
					PaddingTop = CS$<>8__locals1.orderedTabs[i].PaddingTop
				};
				maxWidth = Math.Max(font.GetTextExtents(CS$<>8__locals1.tabs[i].Name).Width + 1.0 + 2.0 * padding, maxWidth);
				i2 = i;
			}
			tabBoundsL.fixedWidth = Math.Max(tabBoundsL.fixedWidth, maxWidth);
			tabBoundsL.fixedX = -tabBoundsL.fixedWidth;
			if (this.creativeInvDialog != null)
			{
				this.creativeInvDialog.Dispose();
			}
			GuiTab[] tabsL = CS$<>8__locals1.tabs;
			GuiTab[] tabsR = null;
			if (CS$<>8__locals1.tabs.Length > 16)
			{
				tabsL = CS$<>8__locals1.tabs.Take(16).ToArray<GuiTab>();
				tabsR = CS$<>8__locals1.tabs.Skip(16).ToArray<GuiTab>();
			}
			this.creativeInvDialog = this.capi.Gui.CreateCompo("inventory-creative", dialogBounds).AddShadedDialogBG(ElementBounds.Fill, true, 5.0, 0.75f).AddDialogTitleBar(Lang.Get("Creative Inventory", Array.Empty<object>()), new Action(this.CloseIconPressed), null, null, null)
				.AddVerticalTabs(tabsL, tabBoundsL, new Action<int, GuiTab>(this.OnTabClicked), "verticalTabs");
			if (tabsR != null)
			{
				this.creativeInvDialog.AddVerticalTabs(tabsR, tabBoundsR, delegate(int index, GuiTab tab)
				{
					CS$<>8__locals1.<>4__this.OnTabClicked(index + 16, CS$<>8__locals1.tabs[index + 16]);
				}, "verticalTabsR");
			}
			this.creativeInvDialog.AddInset(insetBounds, 3, 0.85f).BeginClip(this.creativeClippingBounds).AddItemSlotGrid(this.creativeInv, new Action<object>(this.SendInvPacket), this.cols, fullGridBounds, "slotgrid")
				.EndClip()
				.AddVerticalScrollbar(new Action<float>(this.OnNewScrollbarvalue), scrollbarBounds, "scrollbar")
				.AddTextInput(textInputBounds, new Action<string>(this.OnTextChanged), null, "searchbox")
				.AddDynamicText("", CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Right), rightTextBounds, "searchResults");
			if (tabsR != null)
			{
				this.creativeInvDialog.GetVerticalTab("verticalTabsR").Right = true;
			}
			this.creativeInvDialog.Compose(true);
			this.creativeInvDialog.UnfocusOwnElements();
			this.creativeInvDialog.GetScrollbar("scrollbar").SetHeights((float)slotGridBounds.fixedHeight, (float)(fullGridBounds.fixedHeight + pad));
			this.creativeInvDialog.GetTextInput("searchbox").DeleteOnRefocusBackSpace = true;
			this.creativeInvDialog.GetTextInput("searchbox").SetPlaceHolderText(Lang.Get("Search...", Array.Empty<object>()));
			this.creativeInvDialog.GetVerticalTab((currentGuiTabIndex < 16) ? "verticalTabs" : "verticalTabsR").SetValue((currentGuiTabIndex < 16) ? currentGuiTabIndex : (currentGuiTabIndex - 16), false);
			this.creativeInv.SetTab(this.currentTabIndex);
			this.update();
		}

		private void ComposeSurvivalInvDialog()
		{
			double elemToDlgPad = GuiStyle.ElementToDialogPadding;
			double pad = GuiElementItemSlotGridBase.unscaledSlotPadding;
			int rows = (int)Math.Ceiling((double)((float)this.backPackInv.Count / 6f));
			this.prevRows = rows;
			ElementBounds slotGridBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, pad, pad, 6, 7).FixedGrow(2.0 * pad, 2.0 * pad);
			ElementBounds fullGridBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, 0.0, 6, rows);
			ElementBounds insetBounds = slotGridBounds.ForkBoundingParent(3.0, 3.0, 3.0, 3.0);
			ElementBounds clippingBounds = slotGridBounds.CopyOffsetedSibling(0.0, 0.0, 0.0, 0.0);
			clippingBounds.fixedHeight -= 3.0;
			ElementBounds gridBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, 0.0, 3, 3).FixedRightOf(insetBounds, 45.0);
			gridBounds.fixedY += 50.0;
			ElementBounds outputBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, 0.0, 1, 1).FixedRightOf(insetBounds, 45.0).FixedUnder(gridBounds, 20.0);
			outputBounds.fixedX += pad + GuiElementPassiveItemSlot.unscaledSlotSize;
			ElementBounds dialogBounds = insetBounds.ForkBoundingParent(elemToDlgPad, elemToDlgPad + 30.0, elemToDlgPad + gridBounds.fixedWidth + 20.0, elemToDlgPad);
			if (this.capi.Settings.Bool["immersiveMouseMode"])
			{
				dialogBounds.WithAlignment(EnumDialogArea.RightMiddle).WithFixedAlignmentOffset(-12.0, 0.0);
			}
			else
			{
				dialogBounds.WithAlignment(EnumDialogArea.CenterMiddle).WithFixedAlignmentOffset(20.0, 0.0);
			}
			ElementBounds scrollBarBounds = ElementStdBounds.VerticalScrollbar(insetBounds).WithParent(dialogBounds);
			scrollBarBounds.fixedOffsetX -= 2.0;
			scrollBarBounds.fixedWidth = 15.0;
			this.survivalInvDialog = this.capi.Gui.CreateCompo("inventory-backpack", dialogBounds).AddShadedDialogBG(ElementBounds.Fill, true, 5.0, 0.75f).AddDialogTitleBar(Lang.Get("Inventory and Crafting", Array.Empty<object>()), new Action(this.CloseIconPressed), null, null, null)
				.AddVerticalScrollbar(new Action<float>(this.OnNewScrollbarvalue), scrollBarBounds, "scrollbar")
				.AddInset(insetBounds, 3, 0.85f)
				.BeginClip(clippingBounds)
				.AddItemSlotGridExcl(this.backPackInv, new Action<object>(this.SendInvPacket), 6, new int[] { 0, 1, 2, 3 }, fullGridBounds, "slotgrid")
				.EndClip()
				.AddItemSlotGrid(this.craftingInv, new Action<object>(this.SendInvPacket), 3, new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 }, gridBounds, "craftinggrid")
				.AddItemSlotGrid(this.craftingInv, new Action<object>(this.SendInvPacket), 1, new int[] { 9 }, outputBounds, "outputslot")
				.Compose(true);
			this.survivalInvDialog.GetScrollbar("scrollbar").SetHeights((float)slotGridBounds.fixedHeight, (float)(fullGridBounds.fixedHeight + pad));
		}

		private void BackPackInv_SlotModified(int t1)
		{
			if ((int)Math.Ceiling((double)((float)this.backPackInv.Count / 6f)) != this.prevRows)
			{
				this.ComposeSurvivalInvDialog();
				this.Composers.Remove("maininventory");
				if (this.capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Creative)
				{
					if (this.creativeInvDialog == null)
					{
						this.ComposeCreativeInvDialog();
					}
					this.Composers["maininventory"] = this.creativeInvDialog ?? this.survivalInvDialog;
				}
				if (this.capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Guest || this.capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Survival)
				{
					this.Composers["maininventory"] = this.survivalInvDialog;
				}
			}
		}

		private void update()
		{
			this.OnTextChanged(this.creativeInvDialog.GetTextInput("searchbox").GetText());
		}

		private void OnTabClicked(int index, GuiTab tab)
		{
			this.currentTabIndex = tab.DataInt;
			this.creativeInv.SetTab(tab.DataInt);
			this.creativeInvDialog.GetSlotGrid("slotgrid").DetermineAvailableSlots(null);
			GuiElementItemSlotGrid slotGrid = this.creativeInvDialog.GetSlotGrid("slotgrid");
			int rows = (int)Math.Ceiling((double)((float)slotGrid.renderedSlots.Count / (float)this.cols));
			ElementBounds bounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, 0.0, this.cols, rows);
			slotGrid.Bounds.fixedHeight = bounds.fixedHeight;
			this.update();
		}

		private void SendInvPacket(object packet)
		{
			this.capi.Network.SendPacketClient(packet);
		}

		private void CloseIconPressed()
		{
			this.TryClose();
		}

		private void OnNewScrollbarvalue(float value)
		{
			if (!this.IsOpened())
			{
				return;
			}
			if (this.capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Creative)
			{
				ElementBounds bounds = this.creativeInvDialog.GetSlotGrid("slotgrid").Bounds;
				bounds.fixedY = 10.0 - GuiElementItemSlotGridBase.unscaledSlotPadding - (double)value;
				bounds.CalcWorldBounds();
				return;
			}
			if (this.capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Survival && this.survivalInvDialog != null)
			{
				ElementBounds bounds2 = this.survivalInvDialog.GetSlotGridExcl("slotgrid").Bounds;
				bounds2.fixedY = 10.0 - GuiElementItemSlotGridBase.unscaledSlotPadding - (double)value;
				bounds2.CalcWorldBounds();
			}
		}

		private void OnTextChanged(string text)
		{
			if (this.capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Creative)
			{
				GuiElementItemSlotGrid slotgrid = this.creativeInvDialog.GetSlotGrid("slotgrid");
				slotgrid.FilterItemsBySearchText(text, this.creativeInv.CurrentTab.SearchCache, this.creativeInv.CurrentTab.SearchCacheNames);
				int rows = (int)Math.Ceiling((double)((float)slotgrid.renderedSlots.Count / (float)this.cols));
				ElementBounds fullGridBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, 0.0, this.cols, rows);
				this.creativeInvDialog.GetScrollbar("scrollbar").SetNewTotalHeight((float)(fullGridBounds.fixedHeight + 3.0));
				this.creativeInvDialog.GetScrollbar("scrollbar").SetScrollbarPosition(0);
				this.creativeInvDialog.GetDynamicText("searchResults").SetNewText(Lang.Get("creative-searchresults", new object[] { slotgrid.renderedSlots.Count }), false, false, false);
			}
		}

		public override bool TryOpen()
		{
			return this.capi.World.Player.WorldData.CurrentGameMode != EnumGameMode.Spectator && base.TryOpen();
		}

		public override void OnGuiOpened()
		{
			base.OnGuiOpened();
			this.ComposeGui(false);
			this.capi.World.Player.Entity.TryStopHandAction(true, EnumItemUseCancelReason.OpenedGui);
			if (this.capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Guest || this.capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Survival)
			{
				if (this.craftingInv != null)
				{
					this.capi.Network.SendPacketClient((Packet_Client)this.craftingInv.Open(this.capi.World.Player));
				}
				if (this.backPackInv != null)
				{
					this.capi.Network.SendPacketClient((Packet_Client)this.backPackInv.Open(this.capi.World.Player));
				}
			}
			if (this.capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Creative && this.creativeInv != null)
			{
				this.capi.Network.SendPacketClient((Packet_Client)this.creativeInv.Open(this.capi.World.Player));
			}
		}

		public override void OnGuiClosed()
		{
			if (this.capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Creative)
			{
				GuiComposer guiComposer = this.creativeInvDialog;
				if (guiComposer != null)
				{
					GuiElementTextInput textInput = guiComposer.GetTextInput("searchbox");
					if (textInput != null)
					{
						textInput.SetValue("", true);
					}
				}
				GuiComposer guiComposer2 = this.creativeInvDialog;
				if (guiComposer2 != null)
				{
					GuiElementItemSlotGrid slotGrid = guiComposer2.GetSlotGrid("slotgrid");
					if (slotGrid != null)
					{
						slotGrid.OnGuiClosed(this.capi);
					}
				}
				this.capi.Network.SendPacketClient((Packet_Client)this.creativeInv.Close(this.capi.World.Player));
				return;
			}
			if (this.craftingInv != null)
			{
				foreach (ItemSlot slot in this.craftingInv)
				{
					if (!slot.Empty)
					{
						ItemStackMoveOperation moveop = new ItemStackMoveOperation(this.capi.World, EnumMouseButton.Left, (EnumModifierKey)0, EnumMergePriority.AutoMerge, slot.StackSize);
						moveop.ActingPlayer = this.capi.World.Player;
						object[] packets = this.capi.World.Player.InventoryManager.TryTransferAway(slot, ref moveop, true, false);
						int i = 0;
						while (packets != null && i < packets.Length)
						{
							this.capi.Network.SendPacketClient((Packet_Client)packets[i]);
							i++;
						}
					}
				}
				this.capi.World.Player.InventoryManager.DropAllInventoryItems(this.craftingInv);
				this.capi.Network.SendPacketClient((Packet_Client)this.craftingInv.Close(this.capi.World.Player));
				this.survivalInvDialog.GetSlotGrid("craftinggrid").OnGuiClosed(this.capi);
				this.survivalInvDialog.GetSlotGrid("outputslot").OnGuiClosed(this.capi);
			}
			if (this.survivalInvDialog != null)
			{
				this.capi.Network.SendPacketClient((Packet_Client)this.backPackInv.Close(this.capi.World.Player));
				this.survivalInvDialog.GetSlotGridExcl("slotgrid").OnGuiClosed(this.capi);
			}
		}

		private void OnPlayerModeChanged()
		{
			if (!this.IsOpened())
			{
				return;
			}
			if (this.prevGameMode != this.capi.World.Player.WorldData.CurrentGameMode)
			{
				this.Composers.Remove("maininventory");
				this.ComposeGui(false);
				this.prevGameMode = this.capi.World.Player.WorldData.CurrentGameMode;
				if (this.capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Creative)
				{
					this.capi.Network.SendPacketClient((Packet_Client)this.creativeInv.Open(this.capi.World.Player));
					this.capi.Network.SendPacketClient((Packet_Client)this.backPackInv.Close(this.capi.World.Player));
				}
				if (this.capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Guest || this.capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Survival)
				{
					this.capi.Network.SendPacketClient((Packet_Client)this.backPackInv.Open(this.capi.World.Player));
					this.capi.Network.SendPacketClient((Packet_Client)this.creativeInv.Close(this.capi.World.Player));
				}
			}
		}

		internal override bool OnKeyCombinationToggle(KeyCombination viaKeyComb)
		{
			if (this.IsOpened() && this.creativeInv != null && this.creativeInvDialog != null)
			{
				GuiElementTextInput textInput = this.creativeInvDialog.GetTextInput("searchbox");
				if (textInput != null && textInput.HasFocus)
				{
					return false;
				}
			}
			return base.OnKeyCombinationToggle(viaKeyComb);
		}

		public override void OnMouseDown(MouseEvent args)
		{
			if (args.Handled)
			{
				return;
			}
			foreach (GuiComposer guiComposer in this.Composers.Values)
			{
				guiComposer.OnMouseDown(args);
				if (args.Handled)
				{
					return;
				}
			}
			if (!args.Handled && this.creativeInv != null && this.creativeClippingBounds != null && this.creativeClippingBounds.PointInside(args.X, args.Y) && this.capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Creative)
			{
				ItemSlot mouseCursorSlot = this.capi.World.Player.InventoryManager.GetOwnInventory("mouse")[0];
				if (!mouseCursorSlot.Empty)
				{
					ItemStackMoveOperation op = new ItemStackMoveOperation(this.capi.World, EnumMouseButton.Left, (EnumModifierKey)0, EnumMergePriority.AutoMerge, 0);
					op.ActingPlayer = this.capi.World.Player;
					op.CurrentPriority = EnumMergePriority.DirectMerge;
					int slotid = ((mouseCursorSlot.Itemstack.Equals(this.capi.World, this.creativeInv[0].Itemstack, GlobalConstants.IgnoredStackAttributes) > false) ? 1 : 0);
					object packet = this.creativeInv.ActivateSlot(slotid, mouseCursorSlot, ref op);
					if (packet != null)
					{
						this.SendInvPacket(packet);
					}
				}
			}
			if (!args.Handled)
			{
				using (IEnumerator<GuiComposer> enumerator = this.Composers.Values.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						if (enumerator.Current.Bounds.PointInside(args.X, args.Y))
						{
							args.Handled = true;
						}
					}
				}
			}
		}

		public override bool CaptureAllInputs()
		{
			if (this.IsOpened())
			{
				GuiComposer guiComposer = this.creativeInvDialog;
				return guiComposer != null && guiComposer.GetTextInput("searchbox").HasFocus;
			}
			return false;
		}

		public override bool PrefersUngrabbedMouse
		{
			get
			{
				return this.Composers["maininventory"] == this.creativeInvDialog;
			}
		}

		public override void Dispose()
		{
			base.Dispose();
			GuiComposer guiComposer = this.creativeInvDialog;
			if (guiComposer != null)
			{
				guiComposer.Dispose();
			}
			GuiComposer guiComposer2 = this.survivalInvDialog;
			if (guiComposer2 == null)
			{
				return;
			}
			guiComposer2.Dispose();
		}

		public override float ZSize
		{
			get
			{
				return 250f;
			}
		}

		private ITabbedInventory creativeInv;

		private IInventory backPackInv;

		private IInventory craftingInv;

		private GuiComposer creativeInvDialog;

		private GuiComposer survivalInvDialog;

		private int currentTabIndex;

		private int cols = 15;

		private ElementBounds creativeClippingBounds;

		private int prevRows;

		private EnumGameMode prevGameMode;
	}
}
