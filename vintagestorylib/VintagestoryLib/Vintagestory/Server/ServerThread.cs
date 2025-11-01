using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace Vintagestory.Server
{
	public class ServerThread
	{
		public bool Alive
		{
			get
			{
				return this.alive;
			}
		}

		public ServerThread(ServerMain server, string threadName, CancellationToken cancellationToken)
		{
			this.server = server;
			this.threadName = threadName;
			this.alive = true;
			this._token = cancellationToken;
		}

		public void Process()
		{
			ServerMain.FrameProfiler = new FrameProfilerUtil("[Thread " + this.threadName + "] ");
			this.totalPassedTime.Start();
			try
			{
				while (!this._token.IsCancellationRequested)
				{
					bool paused = this.server.Suspended || this.ShouldPause;
					bool skipSleep = false;
					if (!paused)
					{
						ServerMain.FrameProfiler.Begin(null, Array.Empty<object>());
						skipSleep = this.Update();
						ServerMain.FrameProfiler.Mark("update");
					}
					this.UpdatePausedStatus(paused);
					if (!skipSleep)
					{
						Thread.Sleep(ServerThread.SleepMs);
						ServerMain.FrameProfiler.Mark("sleep");
					}
					if (ServerThread.shouldExit)
					{
						this.ShutDown();
						break;
					}
					if (!paused)
					{
						ServerMain.FrameProfiler.OffThreadEnd();
					}
				}
			}
			catch (TaskCanceledException)
			{
			}
			catch (Exception e)
			{
				ServerMain.Logger.Fatal("Caught unhandled exception in thread '{0}'. Shutting down server.", new object[] { this.threadName });
				ServerMain.Logger.Fatal(e);
				this.server.EnqueueMainThreadTask(delegate
				{
					this.server.Stop("Exception during Process", null, EnumLogType.Notification);
				});
			}
			this.alive = false;
		}

		protected virtual void UpdatePausedStatus(bool newpaused)
		{
			this.paused = newpaused;
		}

		public void ShutDown()
		{
			for (int i = 0; i < this.serversystems.Length; i++)
			{
				this.serversystems[i].OnSeperateThreadShutDown();
			}
			this.alive = false;
		}

		public bool Update()
		{
			long elapsedMS = this.totalPassedTime.ElapsedMilliseconds;
			bool skipSleep = false;
			for (int i = 0; i < this.serversystems.Length; i++)
			{
				ServerSystem serversystem = this.serversystems[i];
				int updateInterval = serversystem.GetUpdateInterval();
				skipSleep |= updateInterval < 0;
				if (elapsedMS - serversystem.millisecondsSinceStartSeperateThread > (long)updateInterval)
				{
					serversystem.millisecondsSinceStartSeperateThread = elapsedMS;
					serversystem.OnSeparateThreadTick();
					ServerMain.FrameProfiler.Mark(serversystem.FrameprofilerName);
				}
			}
			return skipSleep;
		}

		public virtual void OnBeginInitialization()
		{
			for (int i = 0; i < this.serversystems.Length; i++)
			{
				this.serversystems[i].OnBeginInitialization();
			}
		}

		public virtual void OnBeginConfiguration()
		{
			for (int i = 0; i < this.serversystems.Length; i++)
			{
				this.serversystems[i].OnBeginConfiguration();
			}
		}

		public virtual void OnPrepareAssets()
		{
			for (int i = 0; i < this.serversystems.Length; i++)
			{
				this.serversystems[i].OnLoadAssets();
			}
		}

		public virtual void OnBeginLoadGamePre()
		{
			for (int i = 0; i < this.serversystems.Length; i++)
			{
				this.serversystems[i].OnBeginModsAndConfigReady();
			}
		}

		public virtual void OnBeginLoadGame(SaveGame savegame)
		{
			for (int i = 0; i < this.serversystems.Length; i++)
			{
				this.serversystems[i].OnBeginGameReady(savegame);
			}
		}

		public virtual void OnBeginRunGame()
		{
			for (int i = 0; i < this.serversystems.Length; i++)
			{
				this.serversystems[i].OnBeginRunGame();
			}
		}

		public virtual void OnBeginShutdown()
		{
			for (int i = 0; i < this.serversystems.Length; i++)
			{
				this.serversystems[i].OnBeginShutdown();
			}
		}

		internal static bool shouldExit = false;

		public volatile bool ShouldPause;

		internal string threadName;

		internal bool paused;

		private bool alive;

		public ServerSystem[] serversystems;

		internal ServerMain server;

		private Stopwatch totalPassedTime = new Stopwatch();

		public static int SleepMs = 1;

		private readonly CancellationToken _token;
	}
}
