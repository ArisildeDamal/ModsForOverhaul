using System;
using System.Diagnostics;
using System.Threading;

namespace Vintagestory.Server
{
	internal class SpawnerOffthread
	{
		public SpawnerOffthread(ServerSystemEntitySpawner serverSystem)
		{
			this.entitySpawner = serverSystem;
		}

		internal void Start()
		{
			long lastTickTime = 0L;
			Stopwatch sw = new Stopwatch();
			while (!this.entitySpawner.ShouldExit())
			{
				Thread.Sleep(Math.Max(0, 500 - (int)lastTickTime));
				if (this.entitySpawner.ShouldExit())
				{
					break;
				}
				if (this.entitySpawner.paused)
				{
					lastTickTime = 0L;
				}
				else
				{
					sw.Reset();
					sw.Start();
					this.entitySpawner.FindMobSpawnPositions_offthread(0.5f);
					sw.Stop();
					lastTickTime = sw.ElapsedMilliseconds;
				}
			}
		}

		private readonly ServerSystemEntitySpawner entitySpawner;
	}
}
