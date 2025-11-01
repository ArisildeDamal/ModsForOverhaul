using System;

namespace Vintagestory.Client.NoObf
{
	public class TouchEventArgs
	{
		public int GetX()
		{
			return this.x;
		}

		public void SetX(int value)
		{
			this.x = value;
		}

		public int GetY()
		{
			return this.y;
		}

		public void SetY(int value)
		{
			this.y = value;
		}

		public int GetId()
		{
			return this.id;
		}

		public void SetId(int value)
		{
			this.id = value;
		}

		public bool GetHandled()
		{
			return this.handled;
		}

		public void SetHandled(bool value)
		{
			this.handled = value;
		}

		private int x;

		private int y;

		private int id;

		private bool handled;
	}
}
