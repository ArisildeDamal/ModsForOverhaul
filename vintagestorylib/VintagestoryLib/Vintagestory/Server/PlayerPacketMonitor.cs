using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace Vintagestory.Server
{
	public class PlayerPacketMonitor : ServerSystem
	{
		public PlayerPacketMonitor(ServerMain server)
			: base(server)
		{
			this.LoadConfig();
			this.monitorClients = new Dictionary<int, PlayerPacketMonitor.MonitorClient>();
		}

		public override int GetUpdateInterval()
		{
			return 3000;
		}

		public bool RemoveMonitorClient(int clientid)
		{
			return this.monitorClients.Remove(clientid);
		}

		public override void OnServerTick(float dt)
		{
			foreach (KeyValuePair<int, PlayerPacketMonitor.MonitorClient> i in this.monitorClients)
			{
				i.Value.BlocksSet = 0;
				i.Value.MessagesSent = 0;
				i.Value.PacketsReceived = 0;
				i.Value.PacketsReceivedById = new int[100];
			}
		}

		private bool HaveOverflow(PlayerPacketMonitor.MonitorClient monitor, int packetid)
		{
			return (this.config.PacketLimits.ContainsKey(packetid) && monitor.PacketsReceivedById[packetid] > this.config.PacketLimits[packetid]) || monitor.PacketsReceived > this.config.MaxPackets;
		}

		private string OverflowReason(PlayerPacketMonitor.MonitorClient monitor, int lastpacketid, int mostsendpacketId)
		{
			if (this.config.PacketLimits.ContainsKey(lastpacketid) && monitor.PacketsReceivedById[lastpacketid] > this.config.PacketLimits[lastpacketid])
			{
				return "Packet with id " + lastpacketid.ToString() + " was sent more often than max allowed of " + monitor.PacketsReceivedById[lastpacketid].ToString();
			}
			if (monitor.PacketsReceived > this.config.MaxPackets)
			{
				return string.Concat(new string[]
				{
					"Total sum of packet exceeded max allowed of ",
					this.config.MaxPackets.ToString(),
					", mostly packet id ",
					mostsendpacketId.ToString(),
					" (",
					monitor.PacketsReceivedById[mostsendpacketId].ToString(),
					" times)"
				});
			}
			return "unknown";
		}

		public bool CheckPacket(int clientId, Packet_Client packet)
		{
			if (!this.monitorClients.ContainsKey(clientId))
			{
				this.monitorClients.Add(clientId, new PlayerPacketMonitor.MonitorClient
				{
					Id = clientId
				});
			}
			PlayerPacketMonitor.MonitorClient monitorClient = this.monitorClients[clientId];
			monitorClient.PacketsReceived++;
			monitorClient.PacketsReceivedById[packet.Id]++;
			ConnectedClient client = this.server.Clients[clientId];
			if (this.HaveOverflow(monitorClient, packet.Id))
			{
				string playerName = client.PlayerName;
				string message = Lang.Get("Automatically kicked by packet monitor, reason: {0}", new object[] { "Packet overflow" });
				this.server.DisconnectPlayer(client, message, null);
				int packetId = monitorClient.PacketsReceivedById.ToList<int>().IndexOf(monitorClient.PacketsReceivedById.Max());
				ServerMain.Logger.Notification(this.OverflowReason(monitorClient, packet.Id, packetId));
				return false;
			}
			int id = packet.Id;
			if (id == 3)
			{
				return true;
			}
			if (id != 4)
			{
				return true;
			}
			if (this.monitorClients[clientId].MessagePunished())
			{
				this.server.SendMessage(client.Player, packet.Chatline.Groupid, Lang.Get("Spam protection in place, message not sent", Array.Empty<object>()), EnumChatType.Notification, null);
				return false;
			}
			if (this.monitorClients[clientId].MessagesSent < this.config.MaxMessages)
			{
				this.monitorClients[clientId].MessagesSent++;
				return true;
			}
			return this.ActionMessage(client.Player, packet.Chatline.Groupid);
		}

		private bool ActionMessage(IServerPlayer player, int groupid)
		{
			this.monitorClients[player.ClientId].MessagePunishment = new PlayerPacketMonitor.Punishment(new TimeSpan(0, 0, this.config.MessageBanTime));
			string msg = Lang.Get("You've sent too many message at once, you've been muted for {0} seconds", new object[] { this.config.MessageBanTime });
			player.SendMessage(groupid, msg, EnumChatType.Notification, null);
			return false;
		}

		private void LoadConfig()
		{
			if (!File.Exists(this.filename))
			{
				ServerMain.Logger.Notification("servermonitor.json not found, creating new one");
				this.SaveConfig();
			}
			else
			{
				using (TextReader textReader = new StreamReader(this.filename))
				{
					this.config = JsonConvert.DeserializeObject<PlayerPacketMonitor.ServerMonitorConfig>(textReader.ReadToEnd());
					textReader.Close();
					this.SaveConfig();
				}
			}
			ServerMain.Logger.Notification("servermonitor.json now loaded");
		}

		public void SaveConfig()
		{
			using (TextWriter textWriter = new StreamWriter(this.filename))
			{
				textWriter.Write(JsonConvert.SerializeObject(this.config));
				textWriter.Close();
			}
		}

		private PlayerPacketMonitor.ServerMonitorConfig config = new PlayerPacketMonitor.ServerMonitorConfig();

		private Dictionary<int, PlayerPacketMonitor.MonitorClient> monitorClients;

		private string filename = "servermonitor.json";

		private class MonitorClient
		{
			public bool SetBlockPunished()
			{
				return this.SetBlockPunishment != null && this.SetBlockPunishment.Active();
			}

			public bool MessagePunished()
			{
				return this.MessagePunishment != null && this.MessagePunishment.Active();
			}

			public int Id = -1;

			public int[] PacketsReceivedById = new int[100];

			public int PacketsReceived;

			public int BlocksSet;

			public int MessagesSent;

			public PlayerPacketMonitor.Punishment SetBlockPunishment;

			public PlayerPacketMonitor.Punishment MessagePunishment;
		}

		private class Punishment
		{
			public Punishment(TimeSpan duration)
			{
				this.punishmentStartDate = DateTime.UtcNow;
				this.duration = duration;
				this.permanent = false;
			}

			public Punishment()
			{
				this.punishmentStartDate = DateTime.UtcNow;
				this.duration = TimeSpan.MinValue;
				this.permanent = true;
			}

			public bool Active()
			{
				return this.permanent || DateTime.UtcNow.Subtract(this.punishmentStartDate).CompareTo(this.duration) == -1;
			}

			private DateTime punishmentStartDate;

			private bool permanent;

			private TimeSpan duration;
		}

		public class ServerMonitorConfig
		{
			public ServerMonitorConfig()
			{
				this.MaxPackets = 1000;
				this.MaxBlocks = 100;
				this.MaxMessages = 10;
				this.MessageBanTime = 60;
				this.TimeIntervall = 3;
			}

			public Dictionary<int, int> PacketLimits = new Dictionary<int, int>();

			public int MaxPackets;

			public int MaxBlocks;

			public int MaxMessages;

			public int MessageBanTime;

			public int TimeIntervall;
		}
	}
}
