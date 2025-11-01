using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Vintagestory.API.Util;

namespace Vintagestory.Common
{
	internal class ConcurrentIndexedFifoQueue<T> : IEnumerable<T>, IEnumerable where T : ILongIndex
	{
		public ConcurrentIndexedFifoQueue(int capacity, int stages)
		{
			capacity = Math.Min(ArrayConvert.GetRoundedUpSize(capacity), 65536);
			this.elements = new T[capacity];
			this.length = capacity;
			this.elementsByIndex = new ConcurrentDictionary<long, T>(stages, capacity);
		}

		public int Count
		{
			get
			{
				return (int)(this.end - this.start);
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
			return this.end - this.start >= (uint)this.length;
		}

		public bool IsEmpty()
		{
			return this.start == this.end;
		}

		public T GetByIndex(long index)
		{
			T val;
			this.elementsByIndex.TryGetValue(index, out val);
			return val;
		}

		public void Enqueue(T elem)
		{
			this.elementsByIndex[elem.Index] = elem;
			this.EnqueueWithoutAddingToIndex(elem);
		}

		internal void EnqueueWithoutAddingToIndex(T elem)
		{
			uint localcopy = this.endBleedingEdge;
			if (localcopy - this.start > (uint)(this.length - 1))
			{
				throw new Exception("Indexed Fifo Queue overflow. Try increasing servermagicnumbers RequestChunkColumnsQueueSize?");
			}
			uint newEnd;
			while ((newEnd = Interlocked.CompareExchange(ref this.endBleedingEdge, localcopy + 1U, localcopy)) != localcopy)
			{
				localcopy = newEnd;
				if (newEnd - this.start > (uint)(this.length - 1))
				{
					throw new Exception("Indexed Fifo Queue overflow. Try increasing servermagicnumbers RequestChunkColumnsQueueSize?");
				}
			}
			this.elements[(int)((ushort)localcopy) % this.length] = elem;
			Interlocked.Increment(ref this.end);
			if (!this.elementsByIndex.ContainsKey(elem.Index))
			{
				throw new Exception("In queue but missed from index!");
			}
		}

		public T DequeueWithoutRemovingFromIndex()
		{
			uint localcopy = this.startBleedingEdge;
			if (localcopy - this.end >= 0U)
			{
				return default(T);
			}
			uint newStart;
			while ((newStart = Interlocked.CompareExchange(ref this.startBleedingEdge, localcopy + 1U, localcopy)) != localcopy)
			{
				localcopy = newStart;
				if (newStart - this.end >= 0U)
				{
					return default(T);
				}
			}
			T t = this.elements[(int)((ushort)localcopy) % this.length];
			Interlocked.Increment(ref this.start);
			this.elements[(int)((ushort)localcopy) % this.length] = default(T);
			return t;
		}

		public T Dequeue()
		{
			T elem = this.DequeueWithoutRemovingFromIndex();
			if (elem != null)
			{
				this.elementsByIndex.Remove(elem.Index);
			}
			return elem;
		}

		internal void Requeue()
		{
			if (this.IsFull())
			{
				Interlocked.Increment(ref this.endBleedingEdge);
				Interlocked.Increment(ref this.end);
				Interlocked.Increment(ref this.startBleedingEdge);
				Interlocked.Increment(ref this.start);
				return;
			}
			T elem = this.DequeueWithoutRemovingFromIndex();
			if (elem != null)
			{
				this.EnqueueWithoutAddingToIndex(elem);
			}
		}

		public T Peek()
		{
			return this.elements[(int)((ushort)this.startBleedingEdge) % this.length];
		}

		public T PeekAtPosition(int position)
		{
			return this.elements[(int)((ushort)(this.startBleedingEdge + (uint)position)) % this.length];
		}

		public bool Remove(long index)
		{
			T elem;
			if (this.elementsByIndex.TryRemove(index, out elem))
			{
				elem.FlagToDispose();
				return true;
			}
			return false;
		}

		public IEnumerator<T> GetEnumerator()
		{
			ConcurrentIndexedFifoQueue<T>.<GetEnumerator>d__23 <GetEnumerator>d__ = new ConcurrentIndexedFifoQueue<T>.<GetEnumerator>d__23(0);
			<GetEnumerator>d__.<>4__this = this;
			return <GetEnumerator>d__;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		public ICollection<T> Snapshot()
		{
			return this.elementsByIndex.Values;
		}

		public void Clear()
		{
			this.elementsByIndex.Clear();
			this.start = 0U;
			this.startBleedingEdge = 0U;
			this.end = 0U;
			this.endBleedingEdge = 0U;
			for (int i = 0; i < this.elements.Length; i++)
			{
				this.elements[i] = default(T);
			}
		}

		internal readonly ConcurrentDictionary<long, T> elementsByIndex;

		private readonly T[] elements;

		private readonly int length;

		private volatile uint start;

		private volatile uint end;

		private volatile uint startBleedingEdge;

		private volatile uint endBleedingEdge;
	}
}
