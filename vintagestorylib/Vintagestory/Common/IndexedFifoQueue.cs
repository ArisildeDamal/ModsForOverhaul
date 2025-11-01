using System;
using System.Collections;
using System.Collections.Generic;

namespace Vintagestory.Common
{
	internal class IndexedFifoQueue<T> : IEnumerable<T>, IEnumerable where T : ILongIndex
	{
		public IndexedFifoQueue(int capacity)
		{
			this.elements = new T[capacity];
			this.elementsByIndex = new Dictionary<long, T>(capacity);
		}

		public int Count
		{
			get
			{
				if (this.end >= this.start && (!this.isfull || this.end != this.start))
				{
					return this.end - this.start;
				}
				return this.elements.Length - this.start + this.end;
			}
		}

		public int Capacity
		{
			get
			{
				return this.elements.Length;
			}
		}

		public bool IsFull()
		{
			return this.isfull;
		}

		public T GetByIndex(long index)
		{
			T val;
			this.elementsByIndex.TryGetValue(index, out val);
			return val;
		}

		public T GetAtPosition(int position)
		{
			return this.elements[(this.start + position) % this.elements.Length];
		}

		public void Enqueue(T elem)
		{
			if (this.Count >= this.elements.Length - 1)
			{
				throw new Exception("Indexed Fifo Queue overflow");
			}
			this.elements[this.end] = elem;
			this.elementsByIndex[elem.Index] = elem;
			this.end++;
			if (this.end >= this.elements.Length)
			{
				this.end = 0;
			}
			this.isfull = this.start == this.end;
		}

		public T Dequeue()
		{
			T elem = this.elements[this.start];
			this.elements[this.start] = default(T);
			this.elementsByIndex.Remove(elem.Index);
			if (this.start != this.end)
			{
				this.start++;
			}
			if (this.start >= this.elements.Length)
			{
				this.start = 0;
			}
			this.isfull = false;
			return elem;
		}

		internal void Requeue()
		{
			T elem = this.Dequeue();
			if (elem != null)
			{
				this.Enqueue(elem);
			}
		}

		public T Peek()
		{
			return this.elements[this.start];
		}

		public bool Remove(long index)
		{
			bool found = false;
			this.elementsByIndex.Remove(index);
			for (int i = 0; i < this.Count; i++)
			{
				int pos = (i + this.start) % this.elements.Length;
				if (this.elements[pos].Index == index)
				{
					found = true;
				}
				if (found)
				{
					int nextPos = (pos + 1) % this.elements.Length;
					if (this.elements[nextPos] == null)
					{
						break;
					}
					this.elements[pos] = this.elements[nextPos];
				}
			}
			if (found)
			{
				this.end--;
				if (this.end < 0)
				{
					this.end += this.elements.Length;
				}
				this.elements[this.end] = default(T);
				this.isfull = false;
			}
			return found;
		}

		public IEnumerator<T> GetEnumerator()
		{
			IndexedFifoQueue<T>.<GetEnumerator>d__18 <GetEnumerator>d__ = new IndexedFifoQueue<T>.<GetEnumerator>d__18(0);
			<GetEnumerator>d__.<>4__this = this;
			return <GetEnumerator>d__;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		public void Clear()
		{
			for (int i = 0; i < this.elements.Length; i++)
			{
				this.elements[i] = default(T);
			}
			this.elementsByIndex.Clear();
			this.start = 0;
			this.end = 0;
			this.isfull = false;
		}

		private readonly Dictionary<long, T> elementsByIndex;

		private readonly T[] elements;

		private int start;

		private int end;

		private bool isfull;
	}
}
