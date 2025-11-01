using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using CommandLine;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.ClientNative;
using Vintagestory.Common;
using Vintagestory.Common.Convert;
using VSPlatform;

namespace Vintagestory.Server
{
	public class ServerProgram
	{
		public static void Main(string[] args)
		{
			AppDomain currentDomain = AppDomain.CurrentDomain;
			ResolveEventHandler resolveEventHandler;
			if ((resolveEventHandler = ServerProgram.<>O.<0>__AssemblyResolve) == null)
			{
				resolveEventHandler = (ServerProgram.<>O.<0>__AssemblyResolve = new ResolveEventHandler(AssemblyResolver.AssemblyResolve));
			}
			currentDomain.AssemblyResolve += resolveEventHandler;
			if (RuntimeEnv.OS == OS.Windows)
			{
				ConsoleWindowUtil.QuickEditMode(false);
			}
			ServerProgram.args = args;
			new ServerProgram();
		}

		private static void OnExit(PosixSignalContext ctx)
		{
			ctx.Cancel = true;
			ServerMain.Logger.Notification("Server termination event received. Shutting down server");
			if (ServerProgram.server != null && ServerProgram.server.RunPhase != EnumServerRunPhase.Standby)
			{
				ServerProgram.server.Stop("External close event (CTRL+C/kill/etc)", null, EnumLogType.Notification);
			}
			ServerMain.Logger.Notification("Server: Exit() called");
		}

		public ServerProgram()
		{
			ServerProgram.progArgs = new ServerProgramArgs();
			ParserResult<ServerProgramArgs> progArgsRaw = new Parser(delegate(ParserSettings config)
			{
				config.HelpWriter = null;
				config.AutoHelp = false;
				config.AutoVersion = false;
			}).ParseArguments<ServerProgramArgs>(ServerProgram.args);
			ServerProgram.progArgs = progArgsRaw.Value;
			if (ServerProgram.progArgs.DataPath != null && ServerProgram.progArgs.DataPath.Length > 0)
			{
				GamePaths.DataPath = ServerProgram.progArgs.DataPath;
			}
			if (ServerProgram.progArgs.LogPath != null && ServerProgram.progArgs.LogPath.Length > 0)
			{
				GamePaths.CustomLogPath = ServerProgram.progArgs.LogPath;
			}
			if (ServerProgram.progArgs.PrintVersion)
			{
				Console.WriteLine();
				Console.Write("1.21.5");
				return;
			}
			if (ServerProgram.progArgs.PrintHelp)
			{
				Console.WriteLine();
				Console.Write(ServerProgram.progArgs.GetUsage(progArgsRaw));
				return;
			}
			GamePaths.EnsurePathsExist();
			CrashReporter.EnableGlobalExceptionHandling(true);
			new CrashReporter(EnumAppSide.Server).Start(new ThreadStart(this.Main));
		}

		private void Main()
		{
			ServerMain.xPlatInterface = XPlatformInterfaces.GetInterface();
			PosixSignalRegistration[] signals = ServerProgram.Signals;
			int num = 0;
			PosixSignal posixSignal = PosixSignal.SIGTERM;
			Action<PosixSignalContext> action;
			if ((action = ServerProgram.<>O.<1>__OnExit) == null)
			{
				action = (ServerProgram.<>O.<1>__OnExit = new Action<PosixSignalContext>(ServerProgram.OnExit));
			}
			signals[num] = PosixSignalRegistration.Create(posixSignal, action);
			PosixSignalRegistration[] signals2 = ServerProgram.Signals;
			int num2 = 1;
			PosixSignal posixSignal2 = PosixSignal.SIGINT;
			Action<PosixSignalContext> action2;
			if ((action2 = ServerProgram.<>O.<1>__OnExit) == null)
			{
				action2 = (ServerProgram.<>O.<1>__OnExit = new Action<PosixSignalContext>(ServerProgram.OnExit));
			}
			signals2[num2] = PosixSignalRegistration.Create(posixSignal2, action2);
			ServerMain.Logger = new ServerLogger(ServerProgram.progArgs);
			Lang.PreLoad(ServerMain.Logger, GamePaths.AssetsPath, ServerSettings.Language);
			if (!CleanInstallCheck.IsCleanInstall())
			{
				ServerMain.Logger.Error("Your Server installation still contains old files from a previous game version, which may break things. Please fully delete the /assets folder and then do a full reinstallation. Shutting down server.");
				Environment.Exit(0);
				return;
			}
			ServerProgram.server = new ServerMain(null, ServerProgram.args, ServerProgram.progArgs, true);
			if (ServerProgram.progArgs.GenConfigAndExit || ServerProgram.progArgs.SetConfigAndExit != null)
			{
				Environment.Exit(ServerProgram.server.ExitCode);
				return;
			}
			ServerMain.Logger.Notification("C# Framework: " + ServerProgram.FrameworkInfos());
			LoggerBase logger = ServerMain.Logger;
			string text = "Zstd Version: ";
			Version version = ZstdNative.Version;
			logger.Notification(text + ((version != null) ? version.ToString() : null));
			ServerMain.Logger.Notification("Operating System: " + RuntimeEnv.GetOsString());
			ServerMain.Logger.Notification("CPU Cores: {0}", new object[] { Environment.ProcessorCount });
			ServerMain.Logger.Notification("CPU: {0}", new object[] { ServerMain.xPlatInterface.GetCpuInfo() });
			ServerMain.Logger.Notification("Available RAM: {0} MB", new object[] { ServerMain.xPlatInterface.GetRamCapacity() / 1024L });
			ServerProgram.server.exit = new GameExit();
			ServerProgram.server.Standalone = true;
			ServerProgram.server.PreLaunch();
			if (ServerProgram.progArgs.Standby)
			{
				ServerProgram.server.StandbyLaunch();
			}
			else
			{
				ServerProgram.server.Launch();
			}
			do
			{
				ServerProgram.server.Process();
			}
			while (ServerProgram.server.exit == null || !ServerProgram.server.exit.GetExit());
			ServerProgram.server.Stop("Stop through standalone server exit request", null, EnumLogType.Notification);
			ServerProgram.server.Dispose();
			Environment.Exit(ServerProgram.server.ExitCode);
		}

		public static string FrameworkInfos()
		{
			string text = ".net ";
			Version version = Environment.Version;
			return text + ((version != null) ? version.ToString() : null);
		}

		private static ServerMain server;

		private static string[] args;

		private static ServerProgramArgs progArgs;

		private static readonly PosixSignalRegistration[] Signals = new PosixSignalRegistration[2];

		[CompilerGenerated]
		private static class <>O
		{
			public static ResolveEventHandler <0>__AssemblyResolve;

			[Nullable(new byte[] { 0, 1 })]
			public static Action<PosixSignalContext> <1>__OnExit;
		}
	}
}
