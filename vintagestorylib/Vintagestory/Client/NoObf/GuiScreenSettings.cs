using System;
using Vintagestory.API.Client;

namespace Vintagestory.Client.NoObf
{
	public class GuiScreenSettings : GuiScreen, IGameSettingsHandler, IGuiCompositeHandler
	{
		public bool IsIngame
		{
			get
			{
				return false;
			}
		}

		public GuiScreenSettings(ScreenManager screenManager, GuiScreen parentScreen)
			: base(screenManager, parentScreen)
		{
			this.ShowMainMenu = true;
			this.gameSettingsMenu = new GuiCompositeSettings(this, true);
			this.gameSettingsMenu.OpenSettingsMenu();
		}

		public int? MaxViewDistanceAlarmValue
		{
			get
			{
				return null;
			}
		}

		public override bool OnBackPressed()
		{
			return true;
		}

		public GuiComposerManager GuiComposers
		{
			get
			{
				return ScreenManager.GuiComposers;
			}
		}

		public ICoreClientAPI Api
		{
			get
			{
				return this.ScreenManager.api;
			}
		}

		public void LoadComposer(GuiComposer composer)
		{
			this.ElementComposer = composer;
		}

		public bool LeaveSettingsMenu()
		{
			this.ScreenManager.StartMainMenu();
			return true;
		}

		public override void OnKeyDown(KeyEvent e)
		{
			this.gameSettingsMenu.OnKeyDown(e);
			base.OnKeyDown(e);
		}

		public override void OnKeyUp(KeyEvent e)
		{
			this.gameSettingsMenu.OnKeyUp(e);
			base.OnKeyUp(e);
		}

		public override void OnMouseDown(MouseEvent e)
		{
			this.gameSettingsMenu.OnMouseDown(e);
			base.OnMouseDown(e);
		}

		public override void OnMouseUp(MouseEvent e)
		{
			this.gameSettingsMenu.OnMouseUp(e);
			base.OnMouseUp(e);
		}

		public void ReloadShaders()
		{
			ShaderRegistry.ReloadShaders();
		}

		public override void OnScreenLoaded()
		{
			this.gameSettingsMenu.OpenSettingsMenu();
			base.OnScreenLoaded();
		}

		GuiComposer IGameSettingsHandler.dialogBase(string name, double width, double height)
		{
			return base.dialogBase(name, width, height);
		}

		public void OnMacroEditor()
		{
			throw new NotImplementedException();
		}

		private GuiCompositeSettings gameSettingsMenu;
	}
}
