using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;

namespace Vintagestory.ClientNative
{
	public class CrashReporter
	{
		public static List<ModContainer> LoadedMods { get; set; } = new List<ModContainer>();

		public static void SetLogger(Logger logger)
		{
			CrashReporter.logger = logger;
		}

		public CrashReporter(EnumAppSide side)
		{
			this.crashLogFileName = ((side == EnumAppSide.Client) ? "client-crash.log" : "server-crash.log");
			this.launchCrashReporterGui = side == EnumAppSide.Client;
		}

		public static void EnableGlobalExceptionHandling(bool blnIsConsole)
		{
			CrashReporter.s_blnIsConsole = blnIsConsole;
			AppDomain currentDomain = AppDomain.CurrentDomain;
			UnhandledExceptionEventHandler unhandledExceptionEventHandler;
			if ((unhandledExceptionEventHandler = CrashReporter.<>O.<0>__CurrentDomain_UnhandledException) == null)
			{
				unhandledExceptionEventHandler = (CrashReporter.<>O.<0>__CurrentDomain_UnhandledException = new UnhandledExceptionEventHandler(CrashReporter.CurrentDomain_UnhandledException));
			}
			currentDomain.UnhandledException += unhandledExceptionEventHandler;
		}

		private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			if (CrashReporter.s_blnIsConsole)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.Out.WriteLine("Unhandled Exception occurred");
			}
			Exception ex = e.ExceptionObject as Exception;
			new CrashReporter(Process.GetCurrentProcess().MainModule.FileName.ToLowerInvariant().Contains("server") ? EnumAppSide.Server : EnumAppSide.Client).Crash(ex);
		}

		public void Start(ThreadStart start)
		{
			if (!Debugger.IsAttached)
			{
				try
				{
					start();
					return;
				}
				catch (Exception e)
				{
					this.Crash(e);
					return;
				}
			}
			start();
		}

		public void Crash(Exception exCrash)
		{
			this.isCrashing = true;
			StringBuilder fullCrashMsg = new StringBuilder();
			try
			{
				if (!Directory.Exists(GamePaths.Logs))
				{
					Directory.CreateDirectory(GamePaths.Logs);
				}
				string crashfile = Path.Combine(GamePaths.Logs, this.crashLogFileName);
				fullCrashMsg.AppendLine("Game Version: " + GameVersion.LongGameVersion);
				IEnumerable<ModContainer> codeMods = CrashReporter.LoadedMods.Where(delegate(ModContainer mod)
				{
					if (mod.Assembly != null)
					{
						AssemblyCopyrightAttribute customAttribute = mod.Assembly.GetCustomAttribute<AssemblyCopyrightAttribute>();
						return ((customAttribute != null) ? customAttribute.Copyright : null) != "Copyright © 2016-2024 Anego Studios";
					}
					return false;
				});
				StringBuilder stackTraceMsg = new StringBuilder();
				HashSet<ModContainer> culprits = new HashSet<ModContainer>();
				HashSet<string> culpritHarmonyIds = new HashSet<string>();
				for (Exception exToLog = exCrash; exToLog != null; exToLog = exToLog.InnerException)
				{
					stackTraceMsg.AppendLine(LoggerBase.CleanStackTrace(exToLog.ToString()));
					StackFrame[] frames = new StackTrace(exToLog, true).GetFrames();
					int i = 0;
					while (i < frames.Length)
					{
						StackFrame frame = frames[i];
						MethodBase method;
						try
						{
							method = Harmony.GetMethodFromStackframe(frame);
						}
						catch (Exception)
						{
							goto IL_0165;
						}
						goto IL_00D3;
						IL_0165:
						i++;
						continue;
						IL_00D3:
						if (!(method != null))
						{
							goto IL_0165;
						}
						CrashReporter.<>c__DisplayClass15_0 CS$<>8__locals1 = new CrashReporter.<>c__DisplayClass15_0();
						CrashReporter.<>c__DisplayClass15_0 CS$<>8__locals2 = CS$<>8__locals1;
						Type declaringType = method.DeclaringType;
						CS$<>8__locals2.assembly = ((declaringType != null) ? declaringType.Assembly : null);
						if (CS$<>8__locals1.assembly != null)
						{
							culprits.UnionWith(codeMods.Where((ModContainer mod) => mod.Assembly == CS$<>8__locals1.assembly));
						}
						MethodInfo methodInfo = method as MethodInfo;
						if (methodInfo == null)
						{
							goto IL_0165;
						}
						MethodBase original = Harmony.GetOriginalMethod(methodInfo);
						if (!(original != null))
						{
							goto IL_0165;
						}
						Patches patchInfo = Harmony.GetPatchInfo(original);
						if (patchInfo != null)
						{
							culpritHarmonyIds.UnionWith(patchInfo.Owners);
							goto IL_0165;
						}
						goto IL_0165;
					}
				}
				fullCrashMsg.Append(DateTime.Now.ToString() + ": Critical error occurred");
				if (culprits.Count == 0)
				{
					fullCrashMsg.Append('\n');
				}
				else
				{
					fullCrashMsg.AppendFormat(" in the following mod{0}: {1}\n", (culprits.Count > 1) ? "s" : "", string.Join(", ", culprits.Select(delegate(ModContainer mod)
					{
						ModInfo info = mod.Info;
						string text = ((info != null) ? info.ModID : null);
						string text2 = "@";
						ModInfo info2 = mod.Info;
						return text + text2 + ((info2 != null) ? info2.Version : null);
					})));
				}
				fullCrashMsg.AppendLine("Loaded Mods: " + string.Join(", ", CrashReporter.LoadedMods.Select(delegate(ModContainer mod)
				{
					ModInfo info3 = mod.Info;
					string text3 = ((info3 != null) ? info3.ModID : null);
					string text4 = "@";
					ModInfo info4 = mod.Info;
					return text3 + text4 + ((info4 != null) ? info4.Version : null);
				})));
				if (culpritHarmonyIds.Count > 0)
				{
					fullCrashMsg.Append("Involved Harmony IDs: ");
					fullCrashMsg.AppendLine(string.Join(", ", culpritHarmonyIds));
				}
				fullCrashMsg.Append(stackTraceMsg);
				Process process = null;
				if (this.launchCrashReporterGui)
				{
					try
					{
						File.WriteAllText(Path.Combine(Path.GetTempPath(), "VSLastCrash.log"), fullCrashMsg.ToString());
						switch (RuntimeEnv.OS)
						{
						case OS.Windows:
							process = Process.Start(Path.Combine(GamePaths.Binaries, "VSCrashReporter.exe"), new string[] { GamePaths.Logs });
							break;
						case OS.Mac:
							process = Process.Start("open", new string[] { Path.Combine(GamePaths.Binaries, "VSCrashReporterMac.app", "--args", GamePaths.Logs) });
							break;
						case OS.Linux:
							process = Process.Start(Path.Combine(GamePaths.Binaries, "VSCrashReporter"), new string[] { GamePaths.Logs });
							break;
						}
					}
					catch (Exception e)
					{
						fullCrashMsg.Append("Failed to open crash reporter because: " + e.ToString());
					}
				}
				using (FileStream fs = File.Open(crashfile, FileMode.Append))
				{
					using (StreamWriter crashLogger = new StreamWriter(fs))
					{
						crashLogger.Write(fullCrashMsg.ToString());
					}
				}
				fullCrashMsg.AppendLine("Crash written to file at \"" + crashfile + "\"");
				if (CrashReporter.logger != null)
				{
					CrashReporter.logger.Fatal("{0}", new object[] { fullCrashMsg.ToString() });
				}
				this.CallOnCrash();
				Console.WriteLine("{0}", fullCrashMsg);
				if (process != null)
				{
					process.WaitForExit();
				}
			}
			catch (Exception ex)
			{
				StringBuilder stringBuilder = fullCrashMsg;
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder.AppendInterpolatedStringHandler appendInterpolatedStringHandler = new StringBuilder.AppendInterpolatedStringHandler(20, 1, stringBuilder);
				appendInterpolatedStringHandler.AppendLiteral("Crashreport failed: ");
				appendInterpolatedStringHandler.AppendFormatted(LoggerBase.CleanStackTrace(ex.ToString()));
				stringBuilder2.AppendLine(ref appendInterpolatedStringHandler);
				Logger logger = CrashReporter.logger;
				if (logger != null)
				{
					logger.Fatal(fullCrashMsg.ToString());
				}
			}
			finally
			{
				ClientPlatformAbstract platform = ScreenManager.Platform;
				if (platform != null)
				{
					platform.WindowExit("Game crashed");
				}
			}
		}

		private void CallOnCrash()
		{
			if (this.OnCrash != null)
			{
				try
				{
					this.OnCrash();
				}
				catch (Exception)
				{
				}
			}
		}

		private string crashLogFileName = "";

		private bool launchCrashReporterGui;

		private static Logger logger;

		public Action OnCrash;

		public bool isCrashing;

		private static bool s_blnIsConsole = false;

		[CompilerGenerated]
		private static class <>O
		{
			public static UnhandledExceptionEventHandler <0>__CurrentDomain_UnhandledException;
		}
	}
}
