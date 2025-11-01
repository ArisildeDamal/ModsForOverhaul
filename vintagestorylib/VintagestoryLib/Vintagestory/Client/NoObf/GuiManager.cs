using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Vintagestory.Client.NoObf
{
	public class GuiManager : ClientSystem
	{
		public override string Name
		{
			get
			{
				return "gdm";
			}
		}

		public IWorldAccessor World
		{
			get
			{
				return this.game;
			}
		}

		public GuiManager(ClientMain game)
			: base(game)
		{
			this.inventoryItemRenderer = new InventoryItemRenderer(game);
			game.eventManager.OnGameWindowFocus.Add(new Action<bool>(this.FocusChanged));
			game.eventManager.OnDialogOpened.Add(new Action<GuiDialog>(this.OnGuiOpened));
			game.eventManager.OnDialogClosed.Add(new Action<GuiDialog>(this.OnGuiClosed));
			this.RegisterDefaultDialogs();
			game.eventManager.RegisterRenderer(new Action<float>(this.OnBeforeRenderFrame3D), EnumRenderStage.Before, this.Name, 0.1);
			game.eventManager.RegisterRenderer(new Action<float>(this.OnFinalizeFrame), EnumRenderStage.Done, this.Name, 0.1);
			game.Logger.Notification("Initialized GUI Manager");
		}

		public override void OnServerIdentificationReceived()
		{
			ClientEventManager eventManager = this.game.eventManager;
			if (eventManager == null)
			{
				return;
			}
			eventManager.RegisterRenderer(new Action<float>(this.OnRenderFrameGUI), EnumRenderStage.Ortho, this.Name, 1.0);
		}

		private void FocusChanged(bool focus)
		{
		}

		public void RegisterDefaultDialogs()
		{
			this.game.RegisterDialog(new GuiDialog[]
			{
				new HudEntityNameTags(this.game.api),
				new GuiDialogEscapeMenu(this.game.api),
				new HudIngameError(this.game.api),
				new HudIngameDiscovery(this.game.api),
				new HudDialogChat(this.game.api),
				new HudElementInteractionHelp(this.game.api),
				new HudHotbar(this.game.api),
				new HudStatbar(this.game.api),
				new GuiDialogInventory(this.game.api),
				new GuiDialogCharacter(this.game.api),
				new GuiDialogConfirmRemapping(this.game.api),
				new GuiDialogMacroEditor(this.game.api),
				new HudDebugScreen(this.game.api),
				new HudElementCoordinates(this.game.api),
				new HudElementBlockAndEntityInfo(this.game.api),
				new GuiDialogTickProfiler(this.game.api),
				new HudDisconnected(this.game.api),
				new HudNotMinecraft(this.game.api),
				new GuiDialogTransformEditor(this.game.api),
				new GuiDialogSelboxEditor(this.game.api),
				new GuiDialogToolMode(this.game.api),
				new GuiDialogDead(this.game.api),
				new GuiDialogFirstlaunchInfo(this.game.api),
				new HudMouseTools(this.game.api),
				new HudDropItem(this.game.api)
			});
		}

		public override EnumClientSystemType GetSystemType()
		{
			return EnumClientSystemType.GuiDialog;
		}

		internal void OnEscapePressed()
		{
			bool allClosed = true;
			for (int i = 0; i < this.game.OpenedGuis.Count; i++)
			{
				bool didClose = this.game.OpenedGuis[i].OnEscapePressed();
				allClosed = allClosed && didClose;
				if (didClose)
				{
					i--;
				}
			}
		}

		internal void OnGuiClosed(GuiDialog dialog)
		{
			this.game.OpenedGuis.Remove(dialog);
			if (dialog.UnregisterOnClose)
			{
				this.game.LoadedGuis.Remove(dialog);
			}
			bool anyDialogOpened = this.game.DialogsOpened > 0;
			if (this.game.player == null)
			{
				return;
			}
			ClientPlayerInventoryManager plrInv = this.game.player.inventoryMgr;
			if (plrInv.currentHoveredSlot != null)
			{
				InventoryBase inventory = plrInv.currentHoveredSlot.Inventory;
				if ((inventory != null && !inventory.HasOpened(this.game.player)) || !anyDialogOpened)
				{
					plrInv.currentHoveredSlot = null;
				}
			}
			if (this.game.OpenedGuis.FirstOrDefault((GuiDialog dlg) => dlg.Focused) == null)
			{
				GuiDialog fdlg = this.game.OpenedGuis.FirstOrDefault((GuiDialog dlg) => dlg.Focusable);
				this.RequestFocus(fdlg);
			}
			if (!anyDialogOpened)
			{
				this.game.UpdateFreeMouse();
			}
		}

		internal void OnGuiOpened(GuiDialog dialog)
		{
			if (this.game.OpenedGuis.Contains(dialog))
			{
				this.game.OpenedGuis.Remove(dialog);
			}
			int index = this.game.OpenedGuis.FindIndex((GuiDialog d) => dialog.DrawOrder >= d.DrawOrder);
			if (index >= 0)
			{
				this.game.OpenedGuis.Insert(index, dialog);
				return;
			}
			this.game.OpenedGuis.Add(dialog);
		}

		internal void RequestFocus(GuiDialog dialog)
		{
			if (!this.game.LoadedGuis.Contains(dialog))
			{
				this.game.Logger.Error("The dialog {0} requested focus, but was not added yet. Missing call to api.Gui.RegisterDialog()", new object[] { dialog.DebugName });
				return;
			}
			if (this.ignoreFocusEvents || !dialog.IsOpened())
			{
				return;
			}
			this.Move<GuiDialog>(this.game.LoadedGuis, dialog, this.game.LoadedGuis.FindIndex((GuiDialog d) => d.InputOrder == dialog.InputOrder && d.DrawOrder == dialog.DrawOrder));
			this.Move<GuiDialog>(this.game.OpenedGuis, dialog, this.game.OpenedGuis.FindIndex((GuiDialog d) => d.DrawOrder == dialog.DrawOrder));
			this.ignoreFocusEvents = true;
			foreach (GuiDialog guiDialog in this.game.LoadedGuis.Where((GuiDialog d) => d != dialog).ToList<GuiDialog>())
			{
				guiDialog.UnFocus();
			}
			dialog.Focus();
			this.ignoreFocusEvents = false;
		}

		private void Move<T>(List<T> list, T element, int to)
		{
			int from = list.FindIndex((T e) => e.Equals(element));
			if (from == -1)
			{
				return;
			}
			if (from > to)
			{
				for (int i = from; i > to; i--)
				{
					list[i] = list[i - 1];
				}
			}
			else if (from < to)
			{
				for (int j = from; j < to; j++)
				{
					list[j] = list[j + 1];
				}
			}
			list[to] = element;
		}

		public override void OnBlockTexturesLoaded()
		{
			foreach (GuiDialog guiDialog in this.game.LoadedGuis.ToList<GuiDialog>())
			{
				guiDialog.OnBlockTexturesLoaded();
			}
		}

		internal override void OnLevelFinalize()
		{
			foreach (GuiDialog guiDialog in this.game.LoadedGuis.ToList<GuiDialog>())
			{
				guiDialog.OnLevelFinalize();
			}
		}

		public override void OnOwnPlayerDataReceived()
		{
			foreach (GuiDialog guiDialog in this.game.LoadedGuis.ToList<GuiDialog>())
			{
				guiDialog.OnOwnPlayerDataReceived();
			}
		}

		public void OnBeforeRenderFrame3D(float deltaTime)
		{
			foreach (GuiDialog dialog in this.game.OpenedGuis.Reverse<GuiDialog>())
			{
				if (dialog.ShouldReceiveRenderEvents())
				{
					dialog.OnBeforeRenderFrame3D(deltaTime);
				}
			}
		}

		public void OnRenderFrameGUI(float deltaTime)
		{
			this.game.GlPushMatrix();
			string focusedMouseCursor = null;
			foreach (GuiDialog dialog in this.game.OpenedGuis.Reverse<GuiDialog>())
			{
				if (dialog.ShouldReceiveRenderEvents())
				{
					dialog.OnRenderGUI(deltaTime);
					this.game.Platform.CheckGlError(dialog.DebugName);
					this.game.GlTranslate(0.0, 0.0, (double)dialog.ZSize);
					if (dialog.MouseOverCursor != null)
					{
						focusedMouseCursor = dialog.MouseOverCursor;
					}
					ScreenManager.FrameProfiler.Mark("rendGui", dialog.DebugName);
				}
			}
			this.game.Platform.UseMouseCursor((focusedMouseCursor != null) ? focusedMouseCursor : "normal", false);
			this.game.GlPopMatrix();
			ScreenManager.FrameProfiler.Mark("rendGuiDone");
		}

		public void OnFinalizeFrame(float dt)
		{
			foreach (GuiDialog dialog in this.game.LoadedGuis.ToList<GuiDialog>())
			{
				dialog.OnFinalizeFrame(dt);
				ScreenManager.FrameProfiler.Mark("gdm-finFr-", dialog.DebugName);
			}
		}

		public override void OnKeyDown(KeyEvent args)
		{
			int eKey = args.KeyCode;
			List<GuiDialog> dialogs = this.game.OpenedGuis.ToList<GuiDialog>();
			foreach (GuiDialog dialog in dialogs)
			{
				if (dialog.CaptureAllInputs())
				{
					dialog.OnKeyDown(args);
					if (args.Handled)
					{
						return;
					}
				}
			}
			if (eKey == 50 && this.game.DialogsOpened > 0)
			{
				this.OnEscapePressed();
				args.Handled = true;
				return;
			}
			foreach (GuiDialog dialog2 in dialogs)
			{
				if (dialog2.ShouldReceiveKeyboardEvents())
				{
					dialog2.OnKeyDown(args);
					if (args.Handled)
					{
						break;
					}
				}
			}
		}

		public override void OnKeyUp(KeyEvent args)
		{
			foreach (GuiDialog dialog in this.game.LoadedGuis.ToList<GuiDialog>())
			{
				if (dialog.ShouldReceiveKeyboardEvents())
				{
					dialog.OnKeyUp(args);
					if (args.Handled)
					{
						break;
					}
				}
			}
		}

		public override void OnKeyPress(KeyEvent args)
		{
			foreach (GuiDialog dialog in this.game.LoadedGuis.ToList<GuiDialog>())
			{
				if (dialog.ShouldReceiveKeyboardEvents())
				{
					dialog.OnKeyPress(args);
					if (args.Handled)
					{
						break;
					}
				}
			}
		}

		public override void OnMouseDown(MouseEvent args)
		{
			foreach (GuiDialog dialog in this.game.LoadedGuis.ToList<GuiDialog>())
			{
				if (dialog.ShouldReceiveMouseEvents())
				{
					dialog.OnMouseDown(args);
					if (args.Handled)
					{
						if (GuiManager.DEBUG_PRINT_INTERACTIONS)
						{
							this.game.Logger.Debug("[GuiManager] OnMouseDown handled by {0}", new object[] { dialog.GetType().Name });
						}
						this.RequestFocus(dialog);
						break;
					}
				}
			}
		}

		public override void OnMouseUp(MouseEvent args)
		{
			foreach (GuiDialog dialog in this.game.LoadedGuis.ToList<GuiDialog>())
			{
				if (dialog.ShouldReceiveMouseEvents())
				{
					dialog.OnMouseUp(args);
					if (args.Handled)
					{
						if (GuiManager.DEBUG_PRINT_INTERACTIONS)
						{
							this.game.Logger.Debug("[GuiManager] OnMouseUp handled by {0}", new object[] { dialog.GetType().Name });
						}
						break;
					}
				}
			}
		}

		public override void OnMouseMove(MouseEvent args)
		{
			this.didHoverSlotEventTrigger = false;
			foreach (GuiDialog dialog in this.game.LoadedGuis.ToList<GuiDialog>())
			{
				if (dialog.ShouldReceiveMouseEvents())
				{
					dialog.OnMouseMove(args);
					if (args.Handled)
					{
						this.OnMouseMoveOver(dialog);
						return;
					}
				}
			}
			this.OnMouseMoveOver(null);
		}

		private void OnMouseMoveOver(GuiDialog nowMouseOverDialog)
		{
			if ((nowMouseOverDialog != this.prevMousedOverDialog || nowMouseOverDialog == null) && !this.didHoverSlotEventTrigger && this.prevHoverSlot != null)
			{
				this.game.api.Input.TriggerOnMouseLeaveSlot(this.prevHoverSlot);
			}
			this.prevMousedOverDialog = nowMouseOverDialog;
		}

		public override bool OnMouseEnterSlot(ItemSlot slot)
		{
			this.prevHoverSlot = slot;
			this.didHoverSlotEventTrigger = true;
			return false;
		}

		public override bool OnMouseLeaveSlot(ItemSlot itemSlot)
		{
			this.didHoverSlotEventTrigger = true;
			foreach (GuiDialog dialog in this.game.LoadedGuis)
			{
				if (dialog.ShouldReceiveMouseEvents() && dialog.OnMouseLeaveSlot(itemSlot))
				{
					return true;
				}
			}
			return false;
		}

		public override void OnMouseWheel(MouseWheelEventArgs args)
		{
			foreach (GuiDialog dialog in this.game.OpenedGuis)
			{
				if (dialog.CaptureAllInputs())
				{
					dialog.OnMouseWheel(args);
					if (args.IsHandled)
					{
						return;
					}
				}
			}
			foreach (GuiDialog dialog2 in this.game.LoadedGuis)
			{
				if (dialog2.IsOpened())
				{
					bool inside = false;
					foreach (GuiComposer composer in dialog2.Composers.Values)
					{
						inside |= composer.Bounds.PointInside(this.game.MouseCurrentX, this.game.MouseCurrentY);
					}
					if (inside && dialog2.ShouldReceiveMouseEvents())
					{
						dialog2.OnMouseWheel(args);
						if (args.IsHandled)
						{
							return;
						}
					}
				}
			}
			foreach (GuiDialog dialog3 in this.game.LoadedGuis)
			{
				if (dialog3.ShouldReceiveMouseEvents())
				{
					dialog3.OnMouseWheel(args);
					if (args.IsHandled)
					{
						break;
					}
				}
			}
		}

		public override bool CaptureAllInputs()
		{
			using (List<GuiDialog>.Enumerator enumerator = this.game.OpenedGuis.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current.CaptureAllInputs())
					{
						return true;
					}
				}
			}
			return false;
		}

		public override bool CaptureRawMouse()
		{
			using (List<GuiDialog>.Enumerator enumerator = this.game.OpenedGuis.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current.CaptureRawMouse())
					{
						return true;
					}
				}
			}
			return false;
		}

		public void SendPacketClient(Packet_Client packetClient)
		{
			this.game.SendPacketClient(packetClient);
		}

		public override void Dispose(ClientMain game)
		{
			InventoryItemRenderer inventoryItemRenderer = this.inventoryItemRenderer;
			if (inventoryItemRenderer != null)
			{
				inventoryItemRenderer.Dispose();
			}
			foreach (GuiDialog guiDialog in game.LoadedGuis)
			{
				if (guiDialog != null)
				{
					guiDialog.Dispose();
				}
			}
		}

		public static bool DEBUG_PRINT_INTERACTIONS;

		internal InventoryItemRenderer inventoryItemRenderer;

		private bool ignoreFocusEvents;

		private GuiDialog prevMousedOverDialog;

		private bool didHoverSlotEventTrigger;

		private ItemSlot prevHoverSlot;
	}
}
