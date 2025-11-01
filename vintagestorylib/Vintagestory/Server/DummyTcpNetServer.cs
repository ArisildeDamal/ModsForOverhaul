using System;
using System.Threading;
using Vintagestory.Common;

namespace Vintagestory.Server
{
	public class DummyTcpNetServer : NetServer
	{
		public override string LocalEndpoint
		{
			get
			{
				return "127.0.0.1";
			}
		}

		public DummyTcpNetServer()
		{
			this.connectedClient = new DummyNetConnection();
		}

		public override void Start()
		{
		}

		public override NetIncomingMessage ReadMessage()
		{
			NetIncomingMessage msg = null;
			Monitor.Enter(this.network.ServerReceiveBufferLock);
			if (this.network.ServerReceiveBuffer.Count > 0)
			{
				if (!this.receivedAnyMessage)
				{
					this.receivedAnyMessage = true;
					msg = new NetIncomingMessage();
					msg.Type = NetworkMessageType.Connect;
					msg.SenderConnection = this.connectedClient;
				}
				else
				{
					msg = new NetIncomingMessage();
					DummyNetworkPacket b = this.network.ServerReceiveBuffer.Dequeue() as DummyNetworkPacket;
					msg.message = b.Data;
					msg.messageLength = b.Length;
					msg.SenderConnection = this.connectedClient;
				}
			}
			Monitor.Exit(this.network.ServerReceiveBufferLock);
			return msg;
		}

		public void SetNetwork(DummyNetwork dummyNetwork)
		{
			this.network = dummyNetwork;
			this.connectedClient.network = this.network;
		}

		public override void SetIpAndPort(string ip, int port)
		{
		}

		public override void Dispose()
		{
		}

		internal DummyNetwork network;

		private DummyNetConnection connectedClient;

		private bool receivedAnyMessage;
	}
}
