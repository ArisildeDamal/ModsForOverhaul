using System;
using System.Threading;

namespace Vintagestory.Server
{
	internal class ClientPacketParserOffthread
	{
		public ClientPacketParserOffthread(ServerMain server)
		{
			this.server = server;
		}

		internal void Start()
		{
			try
			{
				for (;;)
				{
					try
					{
						if (!this.server.stopped && !this.server.exit.exit)
						{
							Thread.Sleep(10);
							this.server.PacketParsingLoop();
							continue;
						}
					}
					catch (ThreadInterruptedException)
					{
						continue;
					}
					break;
				}
			}
			catch (ThreadAbortException)
			{
			}
			this.server.ClientPackets.Clear();
		}

		private ServerMain server;
	}
}
