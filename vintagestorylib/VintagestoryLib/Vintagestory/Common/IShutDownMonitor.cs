using System;

namespace Vintagestory.Common
{
	public interface IShutDownMonitor
	{
		bool ShuttingDown { get; }
	}
}
