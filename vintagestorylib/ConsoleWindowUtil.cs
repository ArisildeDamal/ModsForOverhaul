using System;
using System.Runtime.InteropServices;

public static class ConsoleWindowUtil
{
	public static void QuickEditMode(bool Enable)
	{
		IntPtr stdHandle = ConsoleWindowUtil.NativeFunctions.GetStdHandle(-10);
		uint consoleMode;
		ConsoleWindowUtil.NativeFunctions.GetConsoleMode(stdHandle, out consoleMode);
		if (Enable)
		{
			consoleMode |= 64U;
		}
		else
		{
			consoleMode &= 4294967231U;
		}
		consoleMode |= 128U;
		ConsoleWindowUtil.NativeFunctions.SetConsoleMode(stdHandle, consoleMode);
	}

	private static class NativeFunctions
	{
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr GetStdHandle(int nStdHandle);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

		public enum StdHandle
		{
			STD_INPUT_HANDLE = -10,
			STD_OUTPUT_HANDLE = -11,
			STD_ERROR_HANDLE = -12
		}

		public enum ConsoleMode : uint
		{
			ENABLE_ECHO_INPUT = 4U,
			ENABLE_EXTENDED_FLAGS = 128U,
			ENABLE_INSERT_MODE = 32U,
			ENABLE_LINE_INPUT = 2U,
			ENABLE_MOUSE_INPUT = 16U,
			ENABLE_PROCESSED_INPUT = 1U,
			ENABLE_QUICK_EDIT_MODE = 64U,
			ENABLE_WINDOW_INPUT = 8U,
			ENABLE_VIRTUAL_TERMINAL_INPUT = 512U,
			ENABLE_PROCESSED_OUTPUT = 1U,
			ENABLE_WRAP_AT_EOL_OUTPUT,
			ENABLE_VIRTUAL_TERMINAL_PROCESSING = 4U,
			DISABLE_NEWLINE_AUTO_RETURN = 8U,
			ENABLE_LVB_GRID_WORLDWIDE = 16U
		}
	}
}
