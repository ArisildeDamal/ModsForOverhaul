using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.Server
{
	internal class CmdStats
	{
		public CmdStats(ServerMain server)
		{
			this.server = server;
			server.api.commandapi.Create("stats").RequiresPrivilege(Privilege.controlserver).WithArgs(new ICommandArgumentParser[] { server.api.commandapi.Parsers.OptionalWord("compact") })
				.HandleWith(new OnCommandDelegate(this.handleStats));
		}

		private TextCommandResult handleStats(TextCommandCallingArgs args)
		{
			string ending = (((string)args[0] == "compact") ? ";" : "\n");
			return TextCommandResult.Success(CmdStats.genStats(this.server, ending), null);
		}

		public static string genStats(ServerMain server, string ending)
		{
			StringBuilder sb = new StringBuilder();
			long totalsecondsup = server.totalUpTime.ElapsedMilliseconds / 1000L;
			long secondsup = server.totalUpTime.ElapsedMilliseconds / 1000L;
			int minutesup = 0;
			int hoursup = 0;
			int daysup = 0;
			if (secondsup > 60L)
			{
				minutesup = (int)(secondsup / 60L);
				secondsup -= (long)(60 * minutesup);
			}
			if (minutesup > 60)
			{
				hoursup = minutesup / 60;
				minutesup -= 60 * hoursup;
			}
			if (hoursup > 24)
			{
				daysup = hoursup / 24;
				hoursup -= 24 * daysup;
			}
			ICollection<ConnectedClient> clientList = server.Clients.Values;
			int clientCount = clientList.Count((ConnectedClient x) => x.State != EnumClientState.Queued);
			if (clientCount > 0)
			{
				server.lastDisconnectTotalMs = server.totalUpTime.ElapsedMilliseconds;
			}
			int lastonlinesec = Math.Max(0, (int)(totalsecondsup - server.lastDisconnectTotalMs / 1000L));
			sb.Append("Version: 1.21.5");
			sb.Append(ending);
			sb.Append(string.Format("Uptime: {0} days, {1} hours, {2} minutes, {3} seconds", new object[] { daysup, hoursup, minutesup, secondsup }));
			sb.Append(ending);
			sb.Append(string.Format("Players last online: {0} seconds ago", lastonlinesec));
			sb.Append(ending);
			sb.Append("Players online: " + clientCount.ToString() + " / " + server.Config.MaxClients.ToString());
			if (clientCount > 0 && clientCount < 20)
			{
				sb.Append(" (");
				int i = 0;
				foreach (ConnectedClient client in clientList)
				{
					if (client.State != EnumClientState.Connecting && client.State != EnumClientState.Queued)
					{
						if (i++ > 0)
						{
							sb.Append(", ");
						}
						sb.Append(client.PlayerName);
					}
				}
				sb.Append(")");
			}
			sb.Append(ending);
			if (server.Config.MaxClientsInQueue > 0)
			{
				sb.Append("Players in queue: " + server.ConnectionQueue.Count.ToString() + " / " + server.Config.MaxClientsInQueue.ToString());
			}
			int activeCount = 0;
			using (IEnumerator<Entity> enumerator2 = server.LoadedEntities.Values.GetEnumerator())
			{
				while (enumerator2.MoveNext())
				{
					if (enumerator2.Current.State != EnumEntityState.Inactive)
					{
						activeCount++;
					}
				}
			}
			sb.Append(ending);
			string managed = decimal.Round((decimal)((float)GC.GetTotalMemory(false) / 1024f / 1024f), 2).ToString("#.#", GlobalConstants.DefaultCultureInfo);
			string total = decimal.Round((decimal)((float)Process.GetCurrentProcess().WorkingSet64 / 1024f / 1024f), 2).ToString("#.#", GlobalConstants.DefaultCultureInfo);
			sb.Append(string.Concat(new string[] { "Memory usage Managed/Total: ", managed, "Mb / ", total, " Mb" }));
			sb.Append(ending);
			StatsCollection prevColl = server.StatsCollector[GameMath.Mod(server.StatsCollectorIndex - 1, server.StatsCollector.Length)];
			double seconds = 2.0;
			if (prevColl.ticksTotal > 0L)
			{
				sb.Append("Last 2s Average Tick Time: " + decimal.Round(prevColl.tickTimeTotal / prevColl.ticksTotal, 2).ToString() + " ms");
				sb.Append(ending);
				sb.Append("Last 2s Ticks/s: " + decimal.Round((decimal)((double)prevColl.ticksTotal / seconds), 2).ToString());
				sb.Append(ending);
				sb.Append("Last 10 ticks (ms): " + string.Join<long>(", ", prevColl.tickTimes));
			}
			sb.Append(ending);
			sb.Append("Loaded chunks: " + server.loadedChunks.Count.ToString());
			sb.Append(ending);
			sb.Append(string.Concat(new string[]
			{
				"Loaded entities: ",
				server.LoadedEntities.Count.ToString(),
				" (",
				activeCount.ToString(),
				" active)"
			}));
			sb.Append(ending);
			sb.Append(string.Concat(new string[]
			{
				"Network TCP: ",
				decimal.Round((decimal)((double)prevColl.statTotalPackets / seconds), 2).ToString(),
				" Packets/s or ",
				decimal.Round((decimal)((double)prevColl.statTotalPacketsLength / seconds / 1024.0), 2, MidpointRounding.AwayFromZero).ToString(),
				" Kb/s"
			}));
			sb.Append(ending);
			sb.Append(string.Concat(new string[]
			{
				"Network UDP: ",
				decimal.Round((decimal)((double)prevColl.statTotalUdpPackets / seconds), 2).ToString(),
				" Packets/s or ",
				decimal.Round((decimal)((double)prevColl.statTotalUdpPacketsLength / seconds / 1024.0), 2, MidpointRounding.AwayFromZero).ToString(),
				" Kb/s"
			}));
			return sb.ToString();
		}

		private ServerMain server;
	}
}
