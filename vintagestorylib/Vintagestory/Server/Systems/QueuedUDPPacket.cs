using System;
using System.Runtime.CompilerServices;

namespace Vintagestory.Server.Systems
{
	[NullableContext(1)]
	[Nullable(0)]
	internal class QueuedUDPPacket
	{
		public QueuedUDPPacket(ConnectedClient client, Packet_UdpPacket udpPacket)
		{
			this.packet = udpPacket;
			this.client = client;
			this.creationTime = (long)Environment.TickCount;
		}

		internal Packet_UdpPacket packet;

		internal ConnectedClient client;

		internal long creationTime;
	}
}
