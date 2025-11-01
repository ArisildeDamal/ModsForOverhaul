using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Vintagestory.Common
{
	public struct FastRWLock
	{
		public FastRWLock(IShutDownMonitor monitor)
		{
			this.currentCount = 0;
			this.monitor = monitor;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void AcquireReadLock()
		{
			if (Interlocked.CompareExchange(ref this.currentCount, 1, 0) == 0)
			{
				return;
			}
			int readerIndex = 1;
			for (;;)
			{
				int current = Interlocked.CompareExchange(ref this.currentCount, readerIndex + 1, readerIndex);
				if (current == readerIndex)
				{
					break;
				}
				if (current > readerIndex)
				{
					readerIndex++;
				}
				else
				{
					readerIndex = 0;
				}
				if (this.monitor.ShuttingDown)
				{
					return;
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ReleaseReadLock()
		{
			Interlocked.Decrement(ref this.currentCount);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void AcquireWriteLock()
		{
			while (Interlocked.CompareExchange(ref this.currentCount, -1, 0) != 0)
			{
				if (this.monitor.ShuttingDown)
				{
					return;
				}
			}
		}

		public void ReleaseWriteLock()
		{
			this.currentCount = 0;
		}

		internal void WaitUntilFree()
		{
			while (this.currentCount != 0)
			{
			}
		}

		internal void Reset()
		{
			this.currentCount = 0;
		}

		private volatile int currentCount;

		public IShutDownMonitor monitor;
	}
}
