using System;
using System.Threading;
using Vintagestory.Common;

namespace Vintagestory.Client
{
	public class DummyTcpNetClient : NetClient
	{
		public override int CurrentlyReceivingBytes
		{
			get
			{
				return 0;
			}
		}

		public override void Connect(string ip, int port, Action<ConnectionResult> OnConnectionResult, Action<Exception> OnDisconnected)
		{
		}

		public override NetIncomingMessage ReadMessage()
		{
			NetIncomingMessage msg = null;
			Monitor.Enter(this.network.ClientReceiveBufferLock);
			if (this.network.ClientReceiveBuffer.Count > 0)
			{
				msg = new NetIncomingMessage();
				DummyNetworkPacket packet = this.network.ClientReceiveBuffer.Dequeue() as DummyNetworkPacket;
				msg.message = packet.Data;
				msg.messageLength = packet.Length;
			}
			Monitor.Exit(this.network.ClientReceiveBufferLock);
			return msg;
		}

		public override void Send(byte[] data)
		{
			Monitor.Enter(this.network.ServerReceiveBufferLock);
			DummyNetworkPacket b = new DummyNetworkPacket();
			b.Data = data;
			b.Length = data.Length;
			this.network.ServerReceiveBuffer.Enqueue(b);
			Monitor.Exit(this.network.ServerReceiveBufferLock);
		}

		public void SetNetwork(DummyNetwork network_)
		{
			this.network = network_;
		}

		internal DummyNetwork network;
	}
}
