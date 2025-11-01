using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Vintagestory
{
	public class WindowsConsole
	{
		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool AttachConsole(int dwProcessId);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern IntPtr GetStdHandle(WindowsConsole.StandardHandle nStdHandle);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool SetStdHandle(WindowsConsole.StandardHandle nStdHandle, IntPtr handle);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern WindowsConsole.FileType GetFileType(IntPtr handle);

		private static bool IsRedirected(IntPtr handle)
		{
			WindowsConsole.FileType fileType = WindowsConsole.GetFileType(handle);
			return fileType == WindowsConsole.FileType.Disk || fileType == WindowsConsole.FileType.Pipe;
		}

		public static void Attach()
		{
			if (WindowsConsole.IsRedirected(WindowsConsole.GetStdHandle((WindowsConsole.StandardHandle)4294967285U)))
			{
				TextWriter @out = Console.Out;
			}
			bool flag = WindowsConsole.IsRedirected(WindowsConsole.GetStdHandle((WindowsConsole.StandardHandle)4294967284U));
			if (flag)
			{
				TextWriter error = Console.Error;
			}
			WindowsConsole.AttachConsole(-1);
			if (!flag)
			{
				WindowsConsole.SetStdHandle((WindowsConsole.StandardHandle)4294967284U, WindowsConsole.GetStdHandle((WindowsConsole.StandardHandle)4294967285U));
			}
		}

		private enum StandardHandle : uint
		{
			Input = 4294967286U,
			Output = 4294967285U,
			Error = 4294967284U
		}

		private enum FileType : uint
		{
			Unknown,
			Disk,
			Char,
			Pipe
		}
	}
}
