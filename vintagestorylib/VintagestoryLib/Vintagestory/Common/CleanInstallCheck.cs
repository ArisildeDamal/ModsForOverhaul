using System;
using System.IO;
using Vintagestory.API.Config;

namespace Vintagestory.Common
{
	public class CleanInstallCheck
	{
		public static bool IsCleanInstall()
		{
			if (RuntimeEnv.IsDevEnvironment)
			{
				return true;
			}
			string basePath = AppDomain.CurrentDomain.BaseDirectory;
			if (!File.Exists(Path.Combine(new string[] { basePath, "assets", "survival", "itemtypes", "bag", "backpack.json" })))
			{
				bool flag = File.Exists(Path.Combine(basePath, "assets", "version-1.21.5.txt"));
				string[] strings = Directory.GetFiles(Path.Combine(basePath, "assets"), "version-*.txt", SearchOption.TopDirectoryOnly);
				return flag && strings.Length == 1;
			}
			return false;
		}
	}
}
