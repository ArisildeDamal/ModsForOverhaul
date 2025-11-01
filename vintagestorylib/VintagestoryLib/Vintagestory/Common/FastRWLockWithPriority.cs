using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Vintagestory.Common
{
	public struct FastRWLockWithPriority
	{
		public FastRWLockWithPriority(IShutDownMonitor monitor)
		{
			this.readLockAttempt = 0;
			this.currentCount = 0;
			this.monitor = monitor;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int AcquireWriteLock(int bit)
		{
			while (this.readLockAttempt != 0 && !this.monitor.ShuttingDown)
			{
			}
			int current = Interlocked.Or(ref this.currentCount, bit);
			int attempts = 1;
			while (current < 0)
			{
				Thread.SpinWait(1);
				attempts++;
				Thread.MemoryBarrier();
				current = this.currentCount;
				if (this.monitor.ShuttingDown)
				{
					return 0;
				}
			}
			return attempts;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ReleaseWriteLock(int bit)
		{
			Interlocked.And(ref this.currentCount, ~bit);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void AcquireReadLock()
		{
			while (Interlocked.CompareExchange(ref this.currentCount, -2147483648, 0) != 0)
			{
				this.readLockAttempt = 1;
				if (this.monitor.ShuttingDown)
				{
					this.readLockAttempt = 0;
					return;
				}
			}
			this.readLockAttempt = 0;
		}

		public void ReleaseReadLock()
		{
			Interlocked.And(ref this.currentCount, int.MaxValue);
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
			this.readLockAttempt = 0;
		}

		private volatile int currentCount;

		private volatile int readLockAttempt;

		public IShutDownMonitor monitor;
	}
}
