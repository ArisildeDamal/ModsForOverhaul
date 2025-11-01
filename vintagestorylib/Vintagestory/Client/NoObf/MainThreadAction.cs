using System;

namespace Vintagestory.Client.NoObf
{
	public class MainThreadAction
	{
		public MainThreadAction(ClientMain game, Func<int> action, string label)
		{
			this.game = game;
			this.action = action;
			this.label = label;
		}

		public void Enqueue()
		{
			this.game.EnqueueMainThreadTask(delegate
			{
				this.action();
			}, this.label);
		}

		public void Enqueue(Action otherAction)
		{
			this.game.EnqueueMainThreadTask(otherAction, this.label);
		}

		public int Invoke()
		{
			return this.action();
		}

		private ClientMain game;

		private Func<int> action;

		private string label;
	}
}
