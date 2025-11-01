using System;
using System.IO;

namespace Vintagestory
{
	public class DisposableWriter
	{
		public DisposableWriter(string filename, bool clearOldFiles)
		{
			this.writer = new StreamWriter(this.stream = new FileStream(filename, clearOldFiles ? FileMode.Create : FileMode.Append, FileAccess.Write, FileShare.Read));
		}

		public void Dispose()
		{
			this.writer.Dispose();
			this.stream.Dispose();
		}

		private FileStream stream;

		public StreamWriter writer;
	}
}
