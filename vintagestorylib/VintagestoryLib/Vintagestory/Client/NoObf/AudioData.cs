using System;

namespace Vintagestory.Client.NoObf
{
	public abstract class AudioData
	{
		public abstract bool Load();

		public abstract int Load_Async(MainThreadAction onCompleted);

		public int Loaded;
	}
}
