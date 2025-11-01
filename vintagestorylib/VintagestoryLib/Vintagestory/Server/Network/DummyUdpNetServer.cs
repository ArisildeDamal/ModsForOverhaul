using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.Common.Network.Packets;

namespace Vintagestory.Server.Network
{
	public class DummyUdpNetServer : UNetServer
	{
		public override Dictionary<IPEndPoint, int> EndPoints { get; } = new Dictionary<IPEndPoint, int>();

		public override void SetIpAndPort(string ip, int port)
		{
		}

		public override void Start()
		{
		}

		public override UdpPacket[] ReadMessage()
		{
			Monitor.Enter(this.network.ServerReceiveBufferLock);
			UdpPacket[] udpPacket = null;
			if (this.network.ServerReceiveBuffer.Count > 0)
			{
				this.udpPacketList.Clear();
				foreach (object obj in this.network.ServerReceiveBuffer)
				{
					Packet_UdpPacket udp = (Packet_UdpPacket)obj;
					if (udp.Id == 1 || this.Client.Player != null)
					{
						UdpPacket pack = default(UdpPacket);
						pack.Packet = udp;
						pack.Client = this.Client;
						pack.EndPoint = this.localEndpoint;
						this.udpPacketList.Add(pack);
					}
				}
				udpPacket = this.udpPacketList.ToArray();
				this.network.ServerReceiveBuffer.Clear();
			}
			Monitor.Exit(this.network.ServerReceiveBufferLock);
			return udpPacket;
		}

		public override void Dispose()
		{
		}

		public override int SendToClient(int clientId, Packet_UdpPacket packet)
		{
			Monitor.Enter(this.network.ClientReceiveBufferLock);
			this.network.ClientReceiveBuffer.Enqueue(packet);
			Monitor.Exit(this.network.ClientReceiveBufferLock);
			return 0;
		}

		public override void Remove(IServerPlayer player)
		{
			IPEndPoint ipEndPoint = this.endPointsReverse[player.ClientId];
			this.endPointsReverse.Remove(player.ClientId);
			this.EndPoints.Remove(ipEndPoint);
		}

		public override void EnqueuePacket(UdpPacket udpPacket)
		{
			throw new NotImplementedException();
		}

		public override void Add(IPEndPoint endPoint, int clientId)
		{
			this.EndPoints.Add(endPoint, clientId);
			this.endPointsReverse.Add(clientId, endPoint);
		}

		public void SetNetwork(DummyNetwork dummyNetwork)
		{
			this.network = dummyNetwork;
		}

		private readonly IPEndPoint localEndpoint = new IPEndPoint(0L, 0);

		public ConnectedClient Client = new ConnectedClient(-1);

		private readonly Dictionary<int, IPEndPoint> endPointsReverse = new Dictionary<int, IPEndPoint>();

		private DummyNetwork network;

		private readonly List<UdpPacket> udpPacketList = new List<UdpPacket>();
	}
}
