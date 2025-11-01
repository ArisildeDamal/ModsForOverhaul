using System;
using System.Collections.Generic;

namespace Vintagestory.Common
{
	public abstract class UNetClient
	{
		public abstract void Connect(string ip, int port);

		public abstract IEnumerable<Packet_UdpPacket> ReadMessage();

		public abstract void Send(Packet_UdpPacket udpPacket);

		public virtual void Dispose()
		{
		}

		public abstract void EnqueuePacket(Packet_UdpPacket udpPacket);

		public abstract event UNetClient.UdpConnectionRequest DidReceiveUdpConnectionRequest;

		public delegate void UdpConnectionRequest();
	}
}
