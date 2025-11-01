using System;
using Vintagestory.API.Common;

namespace Vintagestory.Common
{
	public class ModLogger : LoggerBase
	{
		public ILogger Parent { get; }

		public ModContainer Mod { get; }

		public ModLogger(ILogger parent, ModContainer mod)
		{
			this.Parent = parent;
			this.Mod = mod;
		}

		protected override void LogImpl(EnumLogType logType, string message, params object[] args)
		{
			ILogger parent = this.Parent;
			string text = "[";
			ModInfo info = this.Mod.Info;
			parent.Log(logType, text + (((info != null) ? info.ModID : null) ?? this.Mod.FileName) + "] " + message, args);
		}
	}
}
