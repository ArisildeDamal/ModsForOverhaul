using System;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.Server
{
	public class ServerLogger : Logger
	{
		public ServerLogger(ServerProgramArgs args)
			: base("Server", !args.Append, args.ArchiveLogFileCount, args.ArchiveLogFileMaxSizeMb)
		{
		}

		public override string getLogFile(EnumLogType logType)
		{
			switch (logType)
			{
			case EnumLogType.Chat:
				return Path.Combine(GamePaths.Logs, "server-chat.log");
			case EnumLogType.Build:
				return Path.Combine(GamePaths.Logs, "server-build.log");
			case EnumLogType.VerboseDebug:
			case EnumLogType.Debug:
				return Path.Combine(GamePaths.Logs, "server-debug.log");
			case EnumLogType.Audit:
				return Path.Combine(GamePaths.Logs, "server-audit.log");
			case EnumLogType.Worldgen:
				return Path.Combine(GamePaths.Logs, "server-worldgen.log");
			}
			return Path.Combine(GamePaths.Logs, "server-main.log");
		}

		public override bool printToConsole(EnumLogType logType)
		{
			return logType != EnumLogType.VerboseDebug && logType != EnumLogType.StoryEvent && logType != EnumLogType.Build && logType != EnumLogType.Audit;
		}

		public override bool printToDebugWindow(EnumLogType logType)
		{
			return logType != EnumLogType.VerboseDebug && logType != EnumLogType.StoryEvent && logType != EnumLogType.Build;
		}
	}
}
