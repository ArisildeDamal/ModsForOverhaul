using System;
using System.Collections.Concurrent;
using System.Threading;
using Vintagestory.Common;
using Vintagestory.Server.Network;

namespace Vintagestory.Server
{
	public class TcpNetServer : NetServer
	{
		public override string LocalEndpoint
		{
			get
			{
				return this.Ip;
			}
		}

		public TcpNetServer()
		{
			this.messages = new ConcurrentQueue<NetIncomingMessage>();
			this.server = new ServerNetManager(this.cts.Token);
		}

		public override void Start()
		{
			this.server.StartServer(this.Port, this.Ip);
			this.server.Connected += this.ServerConnected;
			this.server.ReceivedMessage += this.ServerReceivedMessage;
			this.server.Disconnected += this.ServerDisconnected;
		}

		private void ServerConnected(TcpNetConnection tcpConnection)
		{
			NetIncomingMessage msg = new NetIncomingMessage();
			msg.Type = NetworkMessageType.Connect;
			msg.SenderConnection = tcpConnection;
			this.messages.Enqueue(msg);
		}

		private void ServerDisconnected(TcpNetConnection tcpConnection)
		{
			NetIncomingMessage msg = new NetIncomingMessage();
			msg.Type = NetworkMessageType.Disconnect;
			msg.SenderConnection = tcpConnection;
			this.messages.Enqueue(msg);
		}

		private void ServerReceivedMessage(byte[] data, TcpNetConnection tcpConnection)
		{
			NetIncomingMessage msg = new NetIncomingMessage();
			msg.Type = NetworkMessageType.Data;
			msg.message = data;
			msg.messageLength = data.Length;
			msg.SenderConnection = tcpConnection;
			this.messages.Enqueue(msg);
		}

		public override NetIncomingMessage ReadMessage()
		{
			NetIncomingMessage msg;
			if (this.messages.TryDequeue(out msg))
			{
				return msg;
			}
			return null;
		}

		public override void SetIpAndPort(string ip, int port)
		{
			this.Ip = ip;
			this.Port = port;
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!this.disposed)
			{
				if (disposing)
				{
					this.cts.Cancel();
					this.cts.Dispose();
					this.server.Dispose();
					this.messages.Clear();
				}
				this.disposed = true;
			}
		}

		public override void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected ServerNetManager server;

		private ConcurrentQueue<NetIncomingMessage> messages;

		private int Port;

		private string Ip;

		public CancellationTokenSource cts = new CancellationTokenSource();

		private bool disposed;
	}
}
