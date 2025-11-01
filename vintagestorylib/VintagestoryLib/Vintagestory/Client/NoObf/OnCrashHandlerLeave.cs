using System;

namespace Vintagestory.Client.NoObf
{
	public class OnCrashHandlerLeave : OnCrashHandler
	{
		public static OnCrashHandlerLeave Create(ClientMain game)
		{
			return new OnCrashHandlerLeave
			{
				g = game
			};
		}

		public override void OnCrash()
		{
			ClientMain clientMain = this.g;
			if (clientMain == null)
			{
				return;
			}
			clientMain.SendLeave(1);
		}

		private ClientMain g;
	}
}
