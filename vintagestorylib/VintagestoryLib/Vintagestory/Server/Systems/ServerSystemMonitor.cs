using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Server.Network;

namespace Vintagestory.Server.Systems
{
	[NullableContext(1)]
	[Nullable(0)]
	public class ServerSystemMonitor : ServerSystem
	{
		public ServerSystemMonitor(ServerMain server)
			: base(server)
		{
		}

		public override void OnBeginConfiguration()
		{
			this.server.api.ChatCommands.GetOrCreate("ipblock").WithDescription("Manage the ip block list. This list will be cleared automatically every 10 minutes.").RequiresPrivilege(Privilege.controlserver)
				.BeginSubCommand("clear")
				.WithDescription("Clear the current ip block list.")
				.HandleWith(new OnCommandDelegate(this.OnClearList))
				.EndSubCommand()
				.BeginSubCommand("list")
				.WithDescription("Print the current ip block list.")
				.HandleWith(new OnCommandDelegate(this.OnList))
				.EndSubCommand();
		}

		private TextCommandResult OnList(TextCommandCallingArgs args)
		{
			StringBuilder sb = new StringBuilder();
			foreach (string ip in TcpNetConnection.blockedIps.ToArray<string>())
			{
				sb.AppendLine(ip);
			}
			return TextCommandResult.Success(sb.ToString(), null);
		}

		private TextCommandResult OnClearList(TextCommandCallingArgs args)
		{
			int count = TcpNetConnection.blockedIps.Count;
			TcpNetConnection.blockedIps.Clear();
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(33, 1);
			defaultInterpolatedStringHandler.AppendLiteral("Cleared ");
			defaultInterpolatedStringHandler.AppendFormatted<int>(count);
			defaultInterpolatedStringHandler.AppendLiteral(" IPs from the block list.");
			return TextCommandResult.Success(defaultInterpolatedStringHandler.ToStringAndClear(), null);
		}

		private void OnEvery60sec(float obj)
		{
			this.currentProcess.Refresh();
			float num = (float)this.currentProcess.WorkingSet64 / 1024f / 1024f;
			if ((double)num > (double)this.server.Config.DieAboveMemoryUsageMb * 0.9)
			{
				ServerMain.Logger.Warning("The server is currently using more than 90% of its maximum allowed memory. If usage reaches 100% (" + this.server.Config.DieAboveMemoryUsageMb.ToString() + " MB), the server will shut down automatically.");
				this.server.BroadcastMessageToAllGroups("<strong><font color=\"orange\">The server is currently using more than 90% of its maximum allowed memory. If usage reaches 100%, the server will shut down automatically.</font></strong>", EnumChatType.AllGroups, null);
			}
			if (num > (float)this.server.Config.DieAboveMemoryUsageMb)
			{
				ServerMain.Logger.Notification(TcpNetConnection.blockedIps.Count.ToString() + " ips were blocked.");
				this.server.Stop("Server is consuming too much RAM", "Server is consuming more then " + this.server.Config.DieAboveMemoryUsageMb.ToString() + " MB of RAM", EnumLogType.Error);
			}
			this.accumTick++;
			if (this.accumTick % 15 == 0)
			{
				if (TcpNetConnection.blockedIps.Count > 0)
				{
					ServerMain.Logger.Notification(TcpNetConnection.blockedIps.Count.ToString() + " IP's were blocked. Clearing the temporary block list now.");
					TcpNetConnection.blockedIps.Clear();
				}
				if (!this.server.RecentClientLogins.IsEmpty)
				{
					ServerMain.Logger.Notification(this.server.RecentClientLogins.Count.ToString() + " IP's send Connection Attempts too fast. Clearing the list now.");
					this.server.RecentClientLogins.Clear();
				}
				this.accumTick = 0;
			}
		}

		public override void OnLoadAssets()
		{
			this.server.api.Logger.EntryAdded += this.OnEntryAdded;
			if (this.server.IsDedicatedServer)
			{
				this.currentProcess = Process.GetCurrentProcess();
				this.listener = this.server.RegisterGameTickListener(new Action<float>(this.OnEvery60sec), 60000, 0);
			}
		}

		public override void Dispose()
		{
			this.server.api.Logger.EntryAdded -= this.OnEntryAdded;
			this.server.UnregisterGameTickListener(this.listener);
			Process process = this.currentProcess;
			if (process != null)
			{
				process.Dispose();
			}
			this.currentProcess = null;
		}

		private void OnEntryAdded(EnumLogType logtype, string message, object[] args)
		{
			if (logtype == EnumLogType.Error || logtype == EnumLogType.Fatal)
			{
				this.errors++;
				if (this.errors <= this.server.Config.DieAboveErrorCount)
				{
					return;
				}
				string msg = string.Format("More then {0} errors detected. Shutting down now. Threshold can be changed in serverconfig.json \"DieAboveErrorCount\"", this.server.Config.DieAboveErrorCount);
				this.server.Stop("Too many errors detected. See server-main.log file", msg, EnumLogType.Error);
			}
		}

		private int errors;

		private long listener;

		[Nullable(2)]
		private Process currentProcess;

		private int accumTick;
	}
}
