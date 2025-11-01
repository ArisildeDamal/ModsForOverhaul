using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory
{
	public abstract class Logger : LoggerBase, IDisposable
	{
		public Logger(string program, bool clearOldFiles, int archiveLogFileCount, int archiveLogFileMaxSize)
		{
			this.program = program;
			if (clearOldFiles && !Logger._logsRotated)
			{
				Logger.ArchiveLogFiles(archiveLogFileCount, archiveLogFileMaxSize);
			}
			foreach (object obj in Enum.GetValues(typeof(EnumLogType)))
			{
				EnumLogType logType = (EnumLogType)obj;
				string logFileName = this.getLogFile(logType);
				if (logFileName != null && !this.fileWriters.ContainsKey(logFileName))
				{
					try
					{
						this.fileWriters.Add(logFileName, new DisposableWriter(logFileName, logType != EnumLogType.Worldgen && clearOldFiles));
						this.LinesWritten.Add(logFileName, 0U);
						this.LogfileSplitNumbers.Add(logFileName, 2);
					}
					catch (Exception e)
					{
						base.Error("Cannot open logfile {0} for writing ", new object[] { logFileName });
						base.Error(e);
					}
				}
			}
			base.Notification("{0} logger started.", new object[] { program });
			base.Notification("Game Version: {0}", new object[] { GameVersion.LongGameVersion });
		}

		private static void ArchiveLogFiles(int archiveLogFileCount, int archiveLogFileMaxSize)
		{
			Logger._logsRotated = true;
			List<FileInfo> logsToMove = (from f in Directory.GetFiles(GamePaths.Logs)
				where !Logger.logTypeToExcludeFromArchive.Any(new Func<string, bool>(f.Contains))
				select f into folder
				select new FileInfo(folder) into file
				orderby file.LastWriteTime descending
				select file).ToList<FileInfo>();
			if (logsToMove.Count > 0)
			{
				string sessionTimeStamp = Logger.GetSessionTimeStamp(logsToMove.First<FileInfo>().FullName);
				string archiveFolder = Path.Combine(GamePaths.Logs, "Archive");
				Directory.CreateDirectory(archiveFolder);
				string sessionArchiveFolder = Path.Combine(GamePaths.Logs, "Archive", sessionTimeStamp);
				Directory.CreateDirectory(sessionArchiveFolder);
				bool delete = logsToMove.Sum((FileInfo file) => file.Length / 1024L / 1024L) > (long)archiveLogFileMaxSize;
				foreach (FileInfo file2 in logsToMove)
				{
					try
					{
						if (delete)
						{
							File.Delete(file2.FullName);
						}
						else
						{
							File.Move(file2.FullName, Path.Combine(sessionArchiveFolder, file2.Name));
						}
					}
					catch (Exception)
					{
					}
				}
				List<DirectoryInfo> directories = (from folder in Directory.GetDirectories(archiveFolder)
					select new DirectoryInfo(folder)).ToList<DirectoryInfo>();
				if (directories.Count >= archiveLogFileCount)
				{
					foreach (DirectoryInfo dir in directories.OrderBy((DirectoryInfo folder) => folder.CreationTime).Take(directories.Count - archiveLogFileCount))
					{
						string[] files = Directory.GetFiles(dir.FullName);
						for (int i = 0; i < files.Length; i++)
						{
							File.Delete(files[i]);
						}
						Directory.Delete(dir.FullName);
					}
				}
			}
		}

		private static string GetSessionTimeStamp(string logFile)
		{
			IEnumerator<string> logFileLines = Logger.ReadLines(logFile).GetEnumerator();
			logFileLines.MoveNext();
			char c = ' ';
			string text = logFileLines.Current;
			string timeStamp = string.Join<string>(c, ((text != null) ? text.Split(' ', StringSplitOptions.None).Take(2) : null) ?? Array.Empty<string>());
			logFileLines.Dispose();
			string sessionTimeStamp;
			DateTime sessionTimeStampParsed;
			if (string.IsNullOrWhiteSpace(timeStamp))
			{
				sessionTimeStamp = DateTime.Now.ToString("yyyy-MM-dd_HH_mm_ss");
			}
			else if (DateTime.TryParseExact(timeStamp, "d.M.yyyy HH:mm:ss", null, DateTimeStyles.None, out sessionTimeStampParsed))
			{
				sessionTimeStamp = sessionTimeStampParsed.ToString("yyyy-MM-dd_HH_mm_ss", CultureInfo.InvariantCulture);
			}
			else
			{
				sessionTimeStamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HH_mm_ss");
			}
			return sessionTimeStamp;
		}

		public static IEnumerable<string> ReadLines(string path)
		{
			Logger.<ReadLines>d__16 <ReadLines>d__ = new Logger.<ReadLines>d__16(-2);
			<ReadLines>d__.<>3__path = path;
			return <ReadLines>d__;
		}

		public void Dispose()
		{
			foreach (DisposableWriter disposableWriter in this.fileWriters.Values)
			{
				disposableWriter.Dispose();
			}
			this.fileWriters.Clear();
			this.disposed = true;
		}

		public abstract string getLogFile(EnumLogType logType);

		public abstract bool printToConsole(EnumLogType logType);

		public abstract bool printToDebugWindow(EnumLogType logType);

		protected override void LogImpl(EnumLogType logType, string message, params object[] args)
		{
			if (this.disposed)
			{
				return;
			}
			try
			{
				string logFileName = this.getLogFile(logType);
				if (logFileName != null)
				{
					try
					{
						this.LogToFile(logFileName, logType, message, args);
					}
					catch (NotSupportedException)
					{
						Console.WriteLine("Unable to write to log file " + logFileName);
					}
					catch (ObjectDisposedException)
					{
						Console.WriteLine("Unable to write to log file " + logFileName);
					}
				}
				if (base.TraceLog && this.printToDebugWindow(logType))
				{
					Trace.WriteLine(this.FormatLogEntry(logType, message, args));
				}
				if (this.printToConsole(logType))
				{
					this.SetColorForLogType(logType);
					Console.WriteLine(this.FormatLogEntry(logType, message, args));
					if (this.canUseColor)
					{
						Console.ResetColor();
					}
				}
			}
			catch (Exception)
			{
			}
		}

		public string FormatLogEntry(EnumLogType logType, string message, params object[] args)
		{
			return string.Format(string.Concat(new string[]
			{
				DateTime.Now.ToString((logType == EnumLogType.VerboseDebug) ? "d.M.yyyy HH:mm:ss.fff" : "d.M.yyyy HH:mm:ss"),
				" [",
				this.program,
				" ",
				logType.ToString(),
				"] ",
				message
			}), args);
		}

		public virtual void LogToFile(string logFileName, EnumLogType logType, string message, params object[] args)
		{
			if (!this.fileWriters.ContainsKey(logFileName) || this.disposed)
			{
				return;
			}
			try
			{
				Dictionary<string, uint> linesWritten = this.LinesWritten;
				uint num = linesWritten[logFileName];
				linesWritten[logFileName] = num + 1U;
				if (this.LinesWritten[logFileName] > Logger.LogFileSplitAfterLine)
				{
					this.LinesWritten[logFileName] = 0U;
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(5, 2);
					defaultInterpolatedStringHandler.AppendFormatted(logFileName.Replace(".log", ""));
					defaultInterpolatedStringHandler.AppendLiteral("-");
					defaultInterpolatedStringHandler.AppendFormatted<int>(this.LogfileSplitNumbers[logFileName]);
					defaultInterpolatedStringHandler.AppendLiteral(".log");
					string filename = defaultInterpolatedStringHandler.ToStringAndClear();
					this.fileWriters[logFileName].Dispose();
					this.fileWriters[logFileName] = new DisposableWriter(filename, true);
					Dictionary<string, int> logfileSplitNumbers = this.LogfileSplitNumbers;
					int num2 = logfileSplitNumbers[logFileName];
					logfileSplitNumbers[logFileName] = num2 + 1;
				}
				string type = logType.ToString() ?? "";
				if (logType == EnumLogType.StoryEvent)
				{
					type = "Event";
				}
				this.fileWriters[logFileName].writer.WriteLine(string.Concat(new string[]
				{
					DateTime.Now.ToString((logType == EnumLogType.VerboseDebug) ? "d.M.yyyy HH:mm:ss.fff" : "d.M.yyyy HH:mm:ss"),
					" [",
					type,
					"] ",
					message
				}), args);
				this.fileWriters[logFileName].writer.Flush();
			}
			catch (FormatException)
			{
				if (!this.exceptionPrinted)
				{
					this.exceptionPrinted = true;
					base.Error("Couldn't write to log file, failed formatting {0} (FormatException)", new object[] { message });
				}
			}
			catch (Exception e)
			{
				if (!this.exceptionPrinted)
				{
					this.exceptionPrinted = true;
					base.Error("Couldn't write to log file {0}!", new object[] { logFileName });
					base.Error(e);
				}
			}
		}

		private void SetColorForLogType(EnumLogType logType)
		{
			if (!this.canUseColor)
			{
				return;
			}
			try
			{
				if (logType != EnumLogType.Warning)
				{
					if (logType - EnumLogType.Error <= 1)
					{
						Console.ForegroundColor = ConsoleColor.Red;
					}
				}
				else
				{
					Console.ForegroundColor = ConsoleColor.DarkYellow;
				}
			}
			catch (Exception)
			{
				this.canUseColor = false;
			}
		}

		private string program = "";

		protected Dictionary<string, DisposableWriter> fileWriters = new Dictionary<string, DisposableWriter>();

		protected Dictionary<string, uint> LinesWritten = new Dictionary<string, uint>();

		protected Dictionary<string, int> LogfileSplitNumbers = new Dictionary<string, int>();

		protected bool exceptionPrinted;

		protected bool disposed;

		protected bool canUseColor = true;

		private static string[] logTypeToExcludeFromArchive = new string[] { "crash", "worldgen" };

		public const string ArchiveFolderDateFormat = "yyyy-MM-dd_HH_mm_ss";

		public const string LogDateFormat = "d.M.yyyy HH:mm:ss";

		public const string LogDateFormatVerbose = "d.M.yyyy HH:mm:ss.fff";

		private static bool _logsRotated;

		public static uint LogFileSplitAfterLine = 500000U;
	}
}
