using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Vintagestory.API.Common;

namespace Vintagestory.Server
{
	public class ServerSystemNotifyPing : ServerSystem
	{
		public ServerSystemNotifyPing(ServerMain server)
			: base(server)
		{
			server.RegisterGameTickListener(new Action<float>(this.OnEveryFewSeconds), 5000, 0);
			server.PacketHandlers[2] = new ClientPacketHandler<Packet_Client, ConnectedClient>(this.HandlePingReply);
			server.PacketHandlingOnConnectingAllowed[2] = true;
		}

		public override void OnServerTick(float dt)
		{
			this.pingtimer.Update(new Timer.Tick(this.PingTimerTick));
		}

		private void OnEveryFewSeconds(float t1)
		{
			this.server.BroadcastPlayerPings();
		}

		private void HandlePingReply(Packet_Client packet, ConnectedClient client)
		{
			client.Ping.OnReceive(this.server.totalUnpausedTime.ElapsedMilliseconds);
			client.LastPing = (float)client.Ping.RoundtripTimeTotalMilliseconds() / 1000f;
		}

		private void PingTimerTick()
		{
			if (this.server.exit.GetExit())
			{
				return;
			}
			long currentMs = this.server.totalUnpausedTime.ElapsedMilliseconds;
			List<int> clientsToKick = new List<int>();
			foreach (KeyValuePair<int, ConnectedClient> keyValuePair in this.server.Clients)
			{
				int num;
				ConnectedClient connectedClient2;
				keyValuePair.Deconstruct(out num, out connectedClient2);
				int clientId = num;
				ConnectedClient client = connectedClient2;
				if (!client.Ping.DidReplyOnLastPing)
				{
					if (client.Ping.DidTimeout(currentMs) && !client.IsSinglePlayerClient)
					{
						long seconds = (currentMs - client.Ping.TimeSendMilliSeconds) / 1000L;
						ServerMain.Logger.Notification(seconds.ToString() + "s ping timeout for " + client.PlayerName + ". Kicking player...");
						clientsToKick.Add(clientId);
					}
				}
				else
				{
					this.server.SendPacket(clientId, ServerPackets.Ping());
					client.Ping.OnSend(currentMs);
				}
				if (!client.FallBackToTcp && !client.IsSinglePlayerClient && client.Ping.DidUdpTimeout(currentMs))
				{
					client.FallBackToTcp = true;
					Packet_Server packetTcp = new Packet_Server
					{
						Id = 78
					};
					this.server.SendPacket(clientId, packetTcp);
					float seconds2 = (float)(currentMs - client.Ping.TimeReceivedUdp) / 1000f;
					LoggerBase logger = ServerMain.Logger;
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(138, 2);
					defaultInterpolatedStringHandler.AppendLiteral("UDP: Server did not receive any UDP packets from Client ");
					defaultInterpolatedStringHandler.AppendFormatted<int>(client.Id);
					defaultInterpolatedStringHandler.AppendLiteral(" for ");
					defaultInterpolatedStringHandler.AppendFormatted<float>(seconds2);
					defaultInterpolatedStringHandler.AppendLiteral("s, telling the client to send positions over TCP, server switches to TCP too.");
					logger.Debug(defaultInterpolatedStringHandler.ToStringAndClear());
				}
			}
			foreach (int key in clientsToKick)
			{
				ConnectedClient connectedClient;
				if (this.server.Clients.TryGetValue(key, out connectedClient))
				{
					this.server.DisconnectPlayer(connectedClient, null, null);
				}
			}
		}

		private Timer pingtimer = new Timer
		{
			Interval = 1.0,
			MaxDeltaTime = 5.0
		};
	}
}
