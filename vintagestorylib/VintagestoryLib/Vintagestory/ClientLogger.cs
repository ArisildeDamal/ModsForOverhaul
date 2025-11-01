using System;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

namespace Vintagestory
{
	public class ClientLogger : Logger
	{
		public ClientLogger()
			: base("Client", true, ClientSettings.ArchiveLogFileCount, ClientSettings.ArchiveLogFileMaxSizeMb)
		{
		}

		public override string getLogFile(EnumLogType logType)
		{
			if (logType == EnumLogType.Chat)
			{
				return Path.Combine(GamePaths.Logs, "client-chat.log");
			}
			if (logType - EnumLogType.VerboseDebug <= 1)
			{
				return Path.Combine(GamePaths.Logs, "client-debug.log");
			}
			if (logType == EnumLogType.Audit)
			{
				return Path.Combine(GamePaths.Logs, "client-audit.log");
			}
			return Path.Combine(GamePaths.Logs, "client-main.log");
		}

		public override bool printToConsole(EnumLogType logType)
		{
			return logType != EnumLogType.VerboseDebug && logType != EnumLogType.StoryEvent;
		}

		public override bool printToDebugWindow(EnumLogType logType)
		{
			return logType != EnumLogType.VerboseDebug && logType != EnumLogType.StoryEvent;
		}
	}
}
