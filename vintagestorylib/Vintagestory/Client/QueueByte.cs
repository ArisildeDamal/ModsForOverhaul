using System;

namespace Vintagestory.Client
{
	public class QueueByte
	{
		public QueueByte()
		{
			this.max = QueueByte.bufferPortionSize;
			this.items = new byte[this.max];
		}

		public int GetCount()
		{
			return this.count;
		}

		public void Enqueue(byte value)
		{
			if (this.count + 1 >= this.max)
			{
				byte[] moreitems = new byte[this.max + QueueByte.bufferPortionSize];
				int origcount = this.GetCount();
				for (int i = 0; i < origcount; i++)
				{
					moreitems[i] = this.items[(this.start + i) % this.max];
				}
				this.items = moreitems;
				this.start = 0;
				this.count = origcount;
				this.max += QueueByte.bufferPortionSize;
			}
			int pos = this.start + this.count;
			pos %= this.max;
			this.count++;
			this.items[pos] = value;
		}

		public byte Dequeue()
		{
			byte b = this.items[this.start];
			this.start++;
			this.start %= this.max;
			this.count--;
			return b;
		}

		public void DequeueRange(byte[] data, int length)
		{
			for (int i = 0; i < length; i++)
			{
				data[i] = this.Dequeue();
			}
		}

		internal void PeekRange(byte[] data, int length)
		{
			for (int i = 0; i < length; i++)
			{
				data[i] = this.items[(this.start + i) % this.max];
			}
		}

		private static int bufferPortionSize = 5242880;

		private byte[] items;

		public int start;

		public int count;

		public int max;
	}
}
