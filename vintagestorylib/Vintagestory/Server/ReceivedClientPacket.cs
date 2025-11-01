using System;

namespace Vintagestory.Server
{
	public class ReceivedClientPacket
	{
		public ReceivedClientPacket(ConnectedClient client)
		{
			this.type = ReceivedClientPacketType.NewConnection;
			this.client = client;
		}

		public ReceivedClientPacket(ConnectedClient client, Packet_Client packet)
		{
			this.type = ReceivedClientPacketType.PacketReceived;
			this.client = client;
			this.packet = packet;
		}

		public ReceivedClientPacket(ConnectedClient client, string reason)
		{
			this.type = ReceivedClientPacketType.Disconnect;
			this.client = client;
			this.disconnectReason = reason;
		}

		internal readonly ConnectedClient client;

		internal readonly Packet_Client packet;

		internal readonly string disconnectReason;

		internal readonly ReceivedClientPacketType type;
	}
}
