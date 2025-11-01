using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Vintagestory.Common;

namespace Vintagestory.Client.Network
{
	public class DummyUdpNetClient : UNetClient
	{
		public override event UNetClient.UdpConnectionRequest DidReceiveUdpConnectionRequest;

		public override void Connect(string ip, int port)
		{
		}

		public override IEnumerable<Packet_UdpPacket> ReadMessage()
		{
			Monitor.Enter(this.network.ClientReceiveBufferLock);
			Packet_UdpPacket[] udpPacket = null;
			if (this.network.ClientReceiveBuffer.Count > 0)
			{
				udpPacket = this.network.ClientReceiveBuffer.Select((object p) => (Packet_UdpPacket)p).ToArray<Packet_UdpPacket>();
				this.network.ClientReceiveBuffer.Clear();
			}
			Monitor.Exit(this.network.ClientReceiveBufferLock);
			return udpPacket;
		}

		public override void Send(Packet_UdpPacket packet)
		{
			Monitor.Enter(this.network.ServerReceiveBufferLock);
			this.network.ServerReceiveBuffer.Enqueue(packet);
			Monitor.Exit(this.network.ServerReceiveBufferLock);
		}

		public override void EnqueuePacket(Packet_UdpPacket udpPacket)
		{
			throw new NotImplementedException();
		}

		public void SetNetwork(DummyNetwork network_)
		{
			this.network = network_;
		}

		private DummyNetwork network;
	}
}
