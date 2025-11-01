using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.Common.Network.Packets;

namespace Vintagestory.Server.Network
{
	public class UdpNetServer : UNetServer
	{
		private int port { get; set; }

		private string ip { get; set; }

		public override Dictionary<IPEndPoint, int> EndPoints { get; } = new Dictionary<IPEndPoint, int>();

		public UdpNetServer(CachingConcurrentDictionary<int, ConnectedClient> clients)
		{
			this.clients = clients;
		}

		public override void SetIpAndPort(string ip, int port)
		{
			this.ip = ip;
			this.port = port;
		}

		public override void Start()
		{
			IPAddress ipAddress = (Socket.OSSupportsIPv6 ? IPAddress.IPv6Any : IPAddress.Any);
			bool dualMode = Socket.OSSupportsIPv6;
			if (this.ip != null)
			{
				ipAddress = IPAddress.Parse(this.ip);
				dualMode = false;
			}
			this.udpServer = new UdpClient(ipAddress.AddressFamily);
			if (dualMode)
			{
				this.udpServer.Client.DualMode = true;
			}
			this.udpServer.Client.Bind(new IPEndPoint(ipAddress, this.port));
			this.udpListenTask = new Task(new Action<object>(this.ListenServer), null, this.cts.Token, TaskCreationOptions.LongRunning);
			this.udpListenTask.Start();
		}

		private async void ListenServer(object state)
		{
			while (!this.cts.IsCancellationRequested)
			{
				try
				{
					UdpReceiveResult result = await this.udpServer.ReceiveAsync(this.cts.Token);
					if (result.Buffer.Length <= UdpNetServer.MaxUdpPacketSize)
					{
						Packet_UdpPacket packet = new Packet_UdpPacket();
						Packet_UdpPacketSerializer.DeserializeBuffer(result.Buffer, result.Buffer.Length, packet);
						int id = packet.Id;
						if (id >= 1 && id <= 7)
						{
							UdpPacket udpPacket = default(UdpPacket);
							udpPacket.Packet = packet;
							packet.Length = result.Buffer.Length;
							if (packet.Id == 1)
							{
								udpPacket.EndPoint = result.RemoteEndPoint;
								this.serverPacketQueue.Enqueue(udpPacket);
							}
							else
							{
								int clientId = this.EndPoints.Get(result.RemoteEndPoint, 0);
								ConnectedClient client;
								if (this.clients.TryGetValue(clientId, out client))
								{
									udpPacket.Client = client;
									this.serverPacketQueue.Enqueue(udpPacket);
								}
							}
						}
					}
				}
				catch
				{
				}
			}
		}

		public override UdpPacket[] ReadMessage()
		{
			UdpPacket[] packets = null;
			if (!this.serverPacketQueue.IsEmpty)
			{
				packets = this.serverPacketQueue.ToArray();
				this.serverPacketQueue.Clear();
			}
			return packets;
		}

		public override void Dispose()
		{
			this.cts.Cancel();
			this.cts.Dispose();
			this.udpServer.Dispose();
			this.udpListenTask.Dispose();
		}

		public override int SendToClient(int clientId, Packet_UdpPacket packet)
		{
			try
			{
				IPEndPoint ipEndPoint;
				if (this.endPointsReverse.TryGetValue(clientId, out ipEndPoint))
				{
					byte[] data = Packet_UdpPacketSerializer.SerializeToBytes(packet);
					this.udpServer.Send(data, ipEndPoint);
					return data.Length;
				}
			}
			catch
			{
			}
			return 0;
		}

		public override void Remove(IServerPlayer player)
		{
			IPEndPoint ipEndPoint;
			if (this.endPointsReverse.TryGetValue(player.ClientId, out ipEndPoint))
			{
				this.endPointsReverse.Remove(player.ClientId);
				this.EndPoints.Remove(ipEndPoint);
			}
		}

		public override void EnqueuePacket(UdpPacket udpPacket)
		{
			this.serverPacketQueue.Enqueue(udpPacket);
		}

		public override void Add(IPEndPoint endPoint, int clientId)
		{
			this.EndPoints.Add(endPoint, clientId);
			this.endPointsReverse.Add(clientId, endPoint);
		}

		private UdpClient udpServer;

		private readonly CancellationTokenSource cts = new CancellationTokenSource();

		public static int MaxUdpPacketSize = 5000;

		private readonly Dictionary<int, IPEndPoint> endPointsReverse = new Dictionary<int, IPEndPoint>();

		private readonly CachingConcurrentDictionary<int, ConnectedClient> clients;

		private readonly ConcurrentQueue<UdpPacket> serverPacketQueue = new ConcurrentQueue<UdpPacket>();

		private Task udpListenTask;
	}
}
