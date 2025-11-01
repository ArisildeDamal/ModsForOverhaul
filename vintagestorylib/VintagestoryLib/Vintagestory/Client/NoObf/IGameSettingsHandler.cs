using System;
using Vintagestory.API.Client;

namespace Vintagestory.Client.NoObf
{
	public interface IGameSettingsHandler : IGuiCompositeHandler
	{
		bool LeaveSettingsMenu();

		int? MaxViewDistanceAlarmValue { get; }

		void ReloadShaders();

		bool IsIngame { get; }

		GuiComposer dialogBase(string name, double width = -1.0, double height = -1.0);

		void OnMacroEditor();
	}
}
