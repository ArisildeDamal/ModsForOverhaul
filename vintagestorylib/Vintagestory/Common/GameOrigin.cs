using System;
using System.IO;

namespace Vintagestory.Common
{
	public class GameOrigin : PathOrigin
	{
		public GameOrigin(string assetsPath)
			: this(assetsPath, null)
		{
		}

		public GameOrigin(string assetsPath, string pathForReservedCharsCheck)
			: base("game", assetsPath, pathForReservedCharsCheck)
		{
			this.domain = "game";
			ReadOnlySpan<char> readOnlySpan = Path.Combine(Path.GetFullPath(assetsPath), "game");
			char directorySeparatorChar = Path.DirectorySeparatorChar;
			this.fullPath = readOnlySpan + new ReadOnlySpan<char>(ref directorySeparatorChar);
		}
	}
}
