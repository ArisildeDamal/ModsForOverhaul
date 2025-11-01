using System;
using System.Threading;
using Vintagestory.API.Common;
using Vintagestory.Client.NoObf;

namespace Vintagestory.Client
{
	internal class LoadSoundsThread
	{
		public LoadSoundsThread(ILogger logger, ClientMain game, Action onCompleted)
		{
			this.logger = logger;
			this.game = game;
			this.onCompleted = onCompleted;
		}

		public void Process()
		{
			try
			{
				ScreenManager.LoadSoundsInitial();
				this.logger.Notification("Reloaded sounds, now with mod assets");
			}
			catch (ThreadAbortException)
			{
			}
			catch (Exception e)
			{
				this.logger.Fatal("Exception in async LoadSounds thread:");
				this.logger.Fatal(e);
			}
			finally
			{
				Action action = this.onCompleted;
				if (action != null)
				{
					action();
				}
			}
		}

		public void ProcessSlow()
		{
			try
			{
				ScreenManager.LoadSoundsSlow(this.game);
				this.logger.Notification("Finished fully loading sounds (async)");
			}
			catch (ThreadAbortException)
			{
			}
			catch (Exception e)
			{
				this.logger.Fatal("Exception in async LoadSounds thread:");
				this.logger.Fatal(e);
			}
			finally
			{
				Action action = this.onCompleted;
				if (action != null)
				{
					action();
				}
			}
		}

		private readonly ILogger logger;

		private readonly Action onCompleted;

		private ClientMain game;
	}
}
