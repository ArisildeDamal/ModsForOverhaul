using System;

namespace Vintagestory.Common
{
	public class GameTickListener : GameTickListenerBase
	{
		public void OnTriggered(long ellapsedMilliseconds)
		{
			try
			{
				this.Handler((float)(ellapsedMilliseconds - this.LastUpdateMilliseconds) / 1000f);
			}
			catch (Exception e)
			{
				if (this.ErrorHandler == null)
				{
					throw;
				}
				this.ErrorHandler(e);
			}
			this.LastUpdateMilliseconds = ellapsedMilliseconds;
		}

		public object Origin()
		{
			return this.Handler.Target;
		}

		public Action<float> Handler;

		public Action<Exception> ErrorHandler;
	}
}
