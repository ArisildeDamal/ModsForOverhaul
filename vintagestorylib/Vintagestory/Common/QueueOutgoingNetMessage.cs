using System;

namespace Vintagestory.Common
{
	public class QueueOutgoingNetMessage
	{
		public QueueOutgoingNetMessage()
		{
			this.packets = new byte[1][];
			this.itemsSize = 1;
			this.count = 0;
		}

		internal int Count()
		{
			return this.count;
		}

		internal byte[] Dequeue()
		{
			byte[] ret = this.packets[0];
			for (int i = 0; i < this.count - 1; i++)
			{
				this.packets[i] = this.packets[i + 1];
			}
			this.count--;
			return ret;
		}

		internal void Enqueue(byte[] data)
		{
			if (this.count == this.itemsSize)
			{
				byte[][] grownMessageQueue = new byte[this.itemsSize * 2][];
				for (int i = 0; i < this.itemsSize; i++)
				{
					grownMessageQueue[i] = this.packets[i];
				}
				this.itemsSize *= 2;
				this.packets = grownMessageQueue;
			}
			byte[][] array = this.packets;
			int num = this.count;
			this.count = num + 1;
			array[num] = data;
		}

		private byte[][] packets;

		private int count;

		private int itemsSize;
	}
}
