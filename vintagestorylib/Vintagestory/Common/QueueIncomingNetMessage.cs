using System;

namespace Vintagestory.Common
{
	public class QueueIncomingNetMessage
	{
		public QueueIncomingNetMessage()
		{
			this.items = new NetIncomingMessage[1];
			this.itemsSize = 1;
			this.count = 0;
		}

		internal int Count()
		{
			return this.count;
		}

		internal NetIncomingMessage Dequeue()
		{
			NetIncomingMessage ret = this.items[0];
			for (int i = 0; i < this.count - 1; i++)
			{
				this.items[i] = this.items[i + 1];
			}
			this.count--;
			return ret;
		}

		internal void Enqueue(NetIncomingMessage p)
		{
			if (this.count == this.itemsSize)
			{
				NetIncomingMessage[] items2 = new NetIncomingMessage[this.itemsSize * 2];
				for (int i = 0; i < this.itemsSize; i++)
				{
					items2[i] = this.items[i];
				}
				this.itemsSize *= 2;
				this.items = items2;
			}
			NetIncomingMessage[] array = this.items;
			int num = this.count;
			this.count = num + 1;
			array[num] = p;
		}

		private NetIncomingMessage[] items;

		private int count;

		private int itemsSize;
	}
}
