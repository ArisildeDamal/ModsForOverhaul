using System;

namespace Vintagestory.Client.NoObf
{
	public class ProcessPacketTask
	{
		public void Run()
		{
			this.ProcessPacket(this.packet);
		}

		internal void ProcessPacket(Packet_Server packet)
		{
			if (!this.game.disposed)
			{
				ServerPacketHandler<Packet_Server> serverPacketHandler = this.game.PacketHandlers[packet.Id];
				if (serverPacketHandler == null)
				{
					return;
				}
				serverPacketHandler(packet);
			}
		}

		internal ClientMain game;

		internal Packet_Server packet;
	}
}
