using System;
using System.Threading;

namespace Vintagestory.Client.NoObf
{
	public class ThreadMusicInitialise
	{
		public ThreadMusicInitialise(SystemMusicEngine engine, ClientMain game)
		{
			this.engine = engine;
			this.game = game;
		}

		public void Process()
		{
			try
			{
				this.engine.EarlyInitialise();
			}
			catch (ThreadAbortException)
			{
			}
			catch (Exception e)
			{
				this.game.Logger.Fatal("Caught unhandled exception in Music Engine initialisation. Exiting game.");
				this.game.Logger.Fatal(e);
				this.game.Platform.XPlatInterface.ShowMessageBox("Client Thread Crash", "Whoops, a client game thread crashed, please check the client-main.log for more Information. I will now exit the game (and stop the server if in singleplayer). Sorry about that :(");
				this.game.KillNextFrame = true;
			}
		}

		private SystemMusicEngine engine;

		private ClientMain game;
	}
}
