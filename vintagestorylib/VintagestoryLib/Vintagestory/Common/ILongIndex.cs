using System;

namespace Vintagestory.Common
{
	public interface ILongIndex
	{
		long Index { get; }

		void FlagToDispose();
	}
}
