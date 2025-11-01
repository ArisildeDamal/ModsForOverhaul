using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Vintagestory.Server.Systems
{
	[NullableContext(1)]
	[Nullable(0)]
	public class ServerUdpQueue
	{
		public ServerUdpQueue(ServerMain server, ServerUdpNetwork network)
		{
			this.server = server;
			this.network = network;
			network.ImmediateUdpQueue = this;
		}

		internal void QueuePacket(ConnectedClient client, Packet_UdpPacket packet)
		{
			this.queue.Enqueue(new QueuedUDPPacket(client, packet));
			if (this.idle)
			{
				lock (this)
				{
					Monitor.Pulse(this);
				}
			}
		}

		internal void DedicatedThreadLoop()
		{
			while (!this.server.stopped && !this.server.exit.exit)
			{
				if (this.queue.IsEmpty)
				{
					try
					{
						this.idle = true;
						lock (this)
						{
							Monitor.Wait(this, 10);
						}
						this.idle = false;
					}
					catch (ThreadInterruptedException)
					{
					}
				}
				long oldestPermittedTime = (long)(Environment.TickCount - 750);
				QueuedUDPPacket p;
				while (this.queue.TryDequeue(out p))
				{
					if (p.creationTime > oldestPermittedTime)
					{
						try
						{
							this.server.SendPacketBlocking(p.client, p.packet);
							continue;
						}
						catch (Exception e)
						{
							ServerMain.Logger.Error(e);
							continue;
						}
						break;
					}
				}
			}
		}

		private const int PacketTTL = 750;

		private readonly ServerMain server;

		private readonly ServerUdpNetwork network;

		private ConcurrentQueue<QueuedUDPPacket> queue = new ConcurrentQueue<QueuedUDPPacket>();

		private bool idle;
	}
}
