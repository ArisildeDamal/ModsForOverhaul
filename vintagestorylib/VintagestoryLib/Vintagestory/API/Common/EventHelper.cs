using System;

namespace Vintagestory.API.Common
{
	public static class EventHelper
	{
		public static bool InvokeSafeCancellable<T>(this Delegate ev, ILogger exceptionLogger, string eventName, T arg)
		{
			return ev.InvokeSafeCancellable(exceptionLogger, eventName, (Delegate handler) => ((Func<T, EnumHandling>)handler)(arg));
		}

		public static bool InvokeSafeCancellable<T0, T1>(this Delegate ev, ILogger exceptionLogger, string eventName, T0 arg0, T1 arg1)
		{
			return ev.InvokeSafeCancellable(exceptionLogger, eventName, (Delegate handler) => ((Func<T0, T1, EnumHandling>)handler)(arg0, arg1));
		}

		public static bool InvokeSafe<T>(this Delegate ev, ILogger exceptionLogger, string eventName, T arg)
		{
			return ev.InvokeSafe(exceptionLogger, eventName, delegate(Delegate handler)
			{
				((Action<T>)handler)(arg);
			});
		}

		public static bool InvokeSafe<T0, T1>(this Delegate ev, ILogger exceptionLogger, string eventName, T0 arg0, T1 arg1)
		{
			return ev.InvokeSafe(exceptionLogger, eventName, delegate(Delegate handler)
			{
				((Action<T0, T1>)handler)(arg0, arg1);
			});
		}

		private static bool InvokeSafeCancellable(this Delegate ev, ILogger exceptionLogger, string eventName, Func<Delegate, EnumHandling> mapper)
		{
			if (ev == null)
			{
				throw new ArgumentNullException("ev");
			}
			if (exceptionLogger == null)
			{
				throw new ArgumentNullException("exceptionLogger");
			}
			bool preventDefault = false;
			foreach (Delegate handler in ev.GetInvocationList())
			{
				try
				{
					EnumHandling enumHandling = mapper(handler);
					if (enumHandling != EnumHandling.PreventDefault)
					{
						if (enumHandling == EnumHandling.PreventSubsequent)
						{
							return false;
						}
					}
					else
					{
						preventDefault = true;
					}
				}
				catch (Exception ex)
				{
					exceptionLogger.Error("Exception when invoking '" + eventName + "':");
					exceptionLogger.Error(ex);
				}
			}
			return !preventDefault;
		}

		private static bool InvokeSafe(this Delegate ev, ILogger exceptionLogger, string eventName, Action<Delegate> mapper)
		{
			if (ev == null)
			{
				throw new ArgumentNullException("ev");
			}
			if (exceptionLogger == null)
			{
				throw new ArgumentNullException("exceptionLogger");
			}
			bool preventDefault = false;
			foreach (Delegate handler in ev.GetInvocationList())
			{
				try
				{
					mapper(handler);
				}
				catch (Exception ex)
				{
					exceptionLogger.Error("Exception when invoking '" + eventName + "':");
					exceptionLogger.Error(ex);
				}
			}
			return !preventDefault;
		}
	}
}
