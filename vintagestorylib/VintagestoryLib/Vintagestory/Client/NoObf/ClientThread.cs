using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Vintagestory.Client.NoObf
{
	internal class ClientThread
	{
		public ClientThread(ClientMain game, string threadName, ClientSystem[] clientsystems, CancellationToken cancellationToken)
		{
			this.game = game;
			this.threadName = threadName;
			this.clientsystems = clientsystems;
			this._token = cancellationToken;
		}

		public void Process()
		{
			this.totalPassedTime.Start();
			try
			{
				while (!this._token.IsCancellationRequested)
				{
					if (!this.Update())
					{
						Thread.Sleep(5);
					}
					if (this.game.threadsShouldExit)
					{
						this.ShutDown();
						break;
					}
				}
			}
			catch (TaskCanceledException)
			{
			}
			catch (Exception e)
			{
				if (this.game.threadsShouldExit)
				{
					this.game.Logger.Notification("Client thread {0} threw an exception during exit. Likely unclean exit, which should not be a problem in most instance. Exception: '{1}'", new object[] { this.threadName, e });
				}
				else
				{
					this.game.Logger.Fatal("Caught unhandled exception in thread '{0}'. Exiting game.", new object[] { this.threadName });
					this.game.Logger.Fatal(e);
					this.game.Platform.XPlatInterface.ShowMessageBox("Client Thread Crash", "Whoops, a client game thread crashed, please check the client-main.log for more Information. I will now exit the game (and stop the server if in singleplayer). Sorry about that :(");
					this.game.KillNextFrame = true;
				}
			}
		}

		public void ShutDown()
		{
		}

		public bool Update()
		{
			float dt = (float)this.lastFramePassedTime.ElapsedTicks / (float)Stopwatch.Frequency;
			long elapsedMS = this.totalPassedTime.ElapsedMilliseconds;
			this.lastFramePassedTime.Restart();
			bool skipSleep = false;
			for (int i = 0; i < this.clientsystems.Length; i++)
			{
				int intervalMs = this.clientsystems[i].SeperateThreadTickIntervalMs();
				skipSleep |= intervalMs < 0;
				if (elapsedMS - this.clientsystems[i].threadMillisecondsSinceStart > (long)intervalMs)
				{
					this.clientsystems[i].threadMillisecondsSinceStart = elapsedMS;
					this.clientsystems[i].OnSeperateThreadGameTick(dt);
				}
			}
			return skipSleep;
		}

		private string threadName;

		internal bool paused;

		private ClientSystem[] clientsystems;

		private Stopwatch lastFramePassedTime = new Stopwatch();

		private Stopwatch totalPassedTime = new Stopwatch();

		private ClientMain game;

		private readonly CancellationToken _token;
	}
}
