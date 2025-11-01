using System;
using Vintagestory.API.Client;
using Vintagestory.Client;
using Vintagestory.Client.NoObf;

public class GuiScreen
{
	public virtual bool ShouldDisposePreviousScreen { get; } = true;

	public bool IsOpened
	{
		get
		{
			return this.ScreenManager.CurrentScreen == this;
		}
	}

	public bool RenderBg { get; set; } = true;

	public GuiScreen(ScreenManager screenManager, GuiScreen parentScreen)
	{
		this.ScreenManager = screenManager;
		this.ParentScreen = parentScreen;
	}

	protected GuiComposer dialogBase(string name, double unScWidth = -1.0, double unScHeight = -1.0)
	{
		int windowHeight = this.ScreenManager.GamePlatform.WindowSize.Height;
		int windowWidth = this.ScreenManager.GamePlatform.WindowSize.Width;
		if (unScWidth < 0.0)
		{
			unScWidth = Math.Max(400.0, (double)windowWidth * 0.5) / (double)ClientSettings.GUIScale + 40.0;
		}
		if (unScHeight < 0.0)
		{
			unScHeight = (double)((float)Math.Max(300, windowHeight) / ClientSettings.GUIScale - 120f);
		}
		double p = GuiStyle.ElementToDialogPadding;
		ElementBounds innerbounds = ElementBounds.Fixed(0.0, 0.0, unScWidth, unScHeight);
		this.dlgBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.FixedMiddle);
		ElementBounds bgBounds = innerbounds.ForkBoundingParent(p, p / 2.0, p, p);
		GuiComposer guiComposer = ScreenManager.GuiComposers.Create(name, this.dlgBounds).AddShadedDialogBG(bgBounds, false, 5.0, 0.75f).BeginChildElements(innerbounds);
		guiComposer.OnComposed += this.Cmp_OnRecomposed;
		return guiComposer;
	}

	private void Cmp_OnRecomposed()
	{
		double windowWidth = (double)this.ScreenManager.GamePlatform.WindowSize.Width;
		double menuWidth = this.ScreenManager.guiMainmenuLeft.Width;
		this.dlgBounds.absOffsetX = menuWidth + (windowWidth - menuWidth - this.dlgBounds.OuterWidth) / 2.0;
	}

	public void BubbleUpEvent(string eventCode)
	{
		this.BubbleUpEvent(eventCode, null);
	}

	public virtual void Refresh()
	{
	}

	public void BubbleUpEvent(string eventCode, object arg)
	{
		GuiScreen cur = this;
		while (!cur.OnEvent(eventCode, arg))
		{
			cur = cur.ParentScreen;
			if (cur == null)
			{
				break;
			}
		}
	}

	public virtual bool OnEvent(string eventCode, object arg)
	{
		return false;
	}

	public virtual void RenderToPrimary(float dt)
	{
		this.FocusedMouseCursor = null;
	}

	public virtual void RenderAfterPostProcessing(float dt)
	{
	}

	public virtual void RenderAfterFinalComposition(float dt)
	{
	}

	public virtual void RenderAfterBlit(float dt)
	{
	}

	public virtual void RenderToDefaultFramebuffer(float dt)
	{
		this.ElementComposer.Render(dt);
		if (this.ElementComposer.MouseOverCursor != null)
		{
			this.FocusedMouseCursor = this.ElementComposer.MouseOverCursor;
		}
		this.ScreenManager.RenderMainMenuParts(dt, this.ElementComposer.Bounds, this.ShowMainMenu, true);
		if (this.ScreenManager.mainMenuComposer.MouseOverCursor != null)
		{
			this.FocusedMouseCursor = this.ScreenManager.mainMenuComposer.MouseOverCursor;
		}
		this.ElementComposer.PostRender(dt);
		this.ScreenManager.GamePlatform.UseMouseCursor((this.FocusedMouseCursor != null) ? this.FocusedMouseCursor : this.UnfocusedMouseCursor, false);
	}

	public virtual bool OnFileDrop(string filename)
	{
		return false;
	}

	public virtual void OnKeyDown(KeyEvent e)
	{
		if (this.ElementComposer != null)
		{
			this.ElementComposer.OnKeyDown(e, true);
		}
		if (!e.Handled && e.KeyCode == 52)
		{
			GuiComposer elementComposer = this.ElementComposer;
			int num = this.tabIndex + 1;
			this.tabIndex = num;
			elementComposer.FocusElement(num);
			if (this.tabIndex > this.ElementComposer.MaxTabIndex)
			{
				this.ElementComposer.FocusElement(0);
				this.tabIndex = 0;
			}
			e.Handled = true;
		}
	}

	public virtual void OnKeyPress(KeyEvent e)
	{
		if (this.ElementComposer != null)
		{
			this.ElementComposer.OnKeyPress(e);
		}
	}

	public virtual void OnKeyUp(KeyEvent e)
	{
	}

	public virtual void OnMouseDown(MouseEvent e)
	{
		if (this.ShowMainMenu)
		{
			this.ScreenManager.guiMainmenuLeft.OnMouseDown(e);
			if (e.Handled)
			{
				return;
			}
		}
		if (this.ElementComposer != null)
		{
			this.ElementComposer.OnMouseDown(e);
			bool handled = e.Handled;
			return;
		}
	}

	public virtual void OnMouseUp(MouseEvent e)
	{
		if (this.ShowMainMenu)
		{
			this.ScreenManager.guiMainmenuLeft.OnMouseUp(e);
			if (e.Handled)
			{
				return;
			}
		}
		if (this.ElementComposer != null)
		{
			this.ElementComposer.OnMouseUp(e);
			bool handled = e.Handled;
			return;
		}
	}

	public virtual void OnMouseMove(MouseEvent e)
	{
		if (this.ShowMainMenu)
		{
			this.ScreenManager.guiMainmenuLeft.OnMouseMove(e);
			if (e.Handled)
			{
				return;
			}
		}
		if (this.ElementComposer != null)
		{
			this.ElementComposer.OnMouseMove(e);
			bool handled = e.Handled;
			return;
		}
	}

	public virtual void OnMouseWheel(MouseWheelEventArgs e)
	{
		if (this.ElementComposer != null)
		{
			this.ElementComposer.OnMouseWheel(e);
			bool isHandled = e.IsHandled;
			return;
		}
	}

	public virtual bool OnBackPressed()
	{
		return true;
	}

	public virtual void OnWindowClosed()
	{
	}

	public virtual void OnFocusChanged(bool focus)
	{
	}

	public virtual void OnScreenUnload()
	{
	}

	public virtual void OnScreenLoaded()
	{
		if (!this.ScreenManager.sessionManager.IsCachedSessionKeyValid())
		{
			ScreenManager.Platform.Logger.Notification("Cached session key is invalid, require login");
			ScreenManager.Platform.ToggleOffscreenBuffer(true);
			this.ScreenManager.LoadAndCacheScreen(typeof(GuiScreenLogin));
			return;
		}
	}

	public virtual void ReloadWorld(string reason)
	{
	}

	public virtual void Dispose()
	{
		GuiComposer elementComposer = this.ElementComposer;
		if (elementComposer == null)
		{
			return;
		}
		elementComposer.Dispose();
	}

	public GuiComposer ElementComposer;

	public ScreenManager ScreenManager;

	public GuiScreen ParentScreen;

	public bool ShowMainMenu;

	public string UnfocusedMouseCursor = "normal";

	public string FocusedMouseCursor;

	protected int tabIndex;

	protected ElementBounds dlgBounds;
}
