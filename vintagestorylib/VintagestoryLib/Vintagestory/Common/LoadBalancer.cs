using System;
using System.Collections.Generic;
using System.Threading;
using Vintagestory.API.Common;

namespace Vintagestory.Common
{
	internal class LoadBalancer
	{
		internal LoadBalancer(LoadBalancedTask caller, Logger logger)
		{
			this.caller = caller;
			this.logger = logger;
		}

		internal void CreateDedicatedThreads(int threadCount, string name, List<Thread> threadlist)
		{
			for (int i = 2; i <= threadCount; i++)
			{
				Thread newThread = this.CreateDedicatedWorkerThread(i, name, threadlist);
				if (threadlist != null)
				{
					threadlist.Add(newThread);
				}
			}
		}

		private Thread CreateDedicatedWorkerThread(int threadnum, string name, List<Thread> threadlist = null)
		{
			Thread thread = TyronThreadPool.CreateDedicatedThread(delegate
			{
				this.caller.StartWorkerThread(threadnum);
			}, name + threadnum.ToString());
			thread.IsBackground = false;
			thread.Priority = Thread.CurrentThread.Priority;
			return thread;
		}

		internal void SynchroniseWorkToMainThread(object source)
		{
			this.threadCompletionCounter = 1;
			try
			{
				if (!this.caller.ShouldExit())
				{
					lock (source)
					{
						Monitor.PulseAll(source);
					}
					this.caller.DoWork(1);
				}
			}
			catch (Exception e)
			{
				this.caller.HandleException(e);
			}
		}

		internal void SynchroniseWorkOnWorkerThread(object source, int workernum)
		{
			bool shouldStart = false;
			try
			{
				lock (source)
				{
					shouldStart = Monitor.Wait(source, 1600);
				}
			}
			catch (ThreadInterruptedException)
			{
				return;
			}
			try
			{
				if (shouldStart)
				{
					this.caller.DoWork(workernum);
					Interlocked.Increment(ref this.threadCompletionCounter);
				}
			}
			catch (ThreadAbortException)
			{
				throw;
			}
			catch (Exception e)
			{
				this.caller.HandleException(e);
			}
		}

		internal void WorkerThreadLoop(object source, int workernum, int msToSleep = 1)
		{
			try
			{
				while (!this.caller.ShouldExit())
				{
					this.SynchroniseWorkOnWorkerThread(source, workernum);
				}
			}
			catch (ThreadAbortException)
			{
			}
			catch (Exception e)
			{
				this.logger.Fatal("Error thrown in worker thread management (this worker thread will now stop as a precaution)\n{0}", new object[] { e.Message });
				this.logger.Fatal(e);
			}
		}

		internal void AwaitCompletionOnAllThreads(int threadsCount)
		{
			long timeout = (long)(Environment.TickCount + 1000);
			SpinWait spinner = default(SpinWait);
			while (Interlocked.CompareExchange(ref this.threadCompletionCounter, 0, threadsCount) != threadsCount)
			{
				spinner.SpinOnce();
				if ((long)Environment.TickCount > timeout)
				{
					break;
				}
			}
			this.threadCompletionCounter = 0;
		}

		private LoadBalancedTask caller;

		private Logger logger;

		private volatile int threadCompletionCounter;
	}
}
