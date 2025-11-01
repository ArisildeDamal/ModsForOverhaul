using System;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;

namespace Vintagestory.Client
{
	public class TcpNetClient : NetClient
	{
		public override int CurrentlyReceivingBytes
		{
			get
			{
				return this.received + this.incoming.count;
			}
		}

		public TcpNetClient()
		{
			this.incoming = new QueueByte();
			this.data = new byte[16384];
		}

		public override void Dispose()
		{
			if (this.tcpConnection != null)
			{
				this.tcpConnection.Disconnect();
			}
		}

		public override void Connect(string host, int port, Action<ConnectionResult> OnConnectionResult, Action<Exception> OnDisconnected)
		{
			this.tcpConnection = new TCPNetworkConnection(host, port, OnConnectionResult, OnDisconnected);
		}

		public override NetIncomingMessage ReadMessage()
		{
			if (this.tcpConnection == null)
			{
				return null;
			}
			NetIncomingMessage message = this.TryGetMessageFromIncoming();
			if (message != null)
			{
				return message;
			}
			for (int i = 0; i < 1; i++)
			{
				this.received = 0;
				this.tcpConnection.Receive(this.data, 16384, out this.received);
				if (this.received <= 0)
				{
					break;
				}
				for (int j = 0; j < this.received; j++)
				{
					this.incoming.Enqueue(this.data[j]);
				}
			}
			return this.TryGetMessageFromIncoming();
		}

		private NetIncomingMessage TryGetMessageFromIncoming()
		{
			if (this.incoming.count >= 4)
			{
				byte[] length = new byte[4];
				this.incoming.PeekRange(length, 4);
				int num = NetIncomingMessage.ReadInt(length);
				bool compressed = ((ulong)num & 18446744071562067968UL) > 0UL;
				int messageLength = num & int.MaxValue;
				if (this.incoming.count >= 4 + messageLength)
				{
					this.incoming.DequeueRange(new byte[4], 4);
					NetIncomingMessage msg = new NetIncomingMessage();
					msg.message = new byte[messageLength];
					msg.messageLength = messageLength;
					msg.originalMessageLength = messageLength;
					this.incoming.DequeueRange(msg.message, msg.messageLength);
					if (compressed)
					{
						msg.message = Compression.Decompress(msg.message);
						msg.messageLength = msg.message.Length;
					}
					return msg;
				}
			}
			return null;
		}

		public override void Send(byte[] data)
		{
			byte[] packet = new byte[data.Length + 4];
			NetIncomingMessage.WriteInt(packet, data.Length);
			for (int i = 0; i < data.Length; i++)
			{
				packet[i + 4] = data[i];
			}
			this.tcpConnection.Send(packet, data.Length + 4);
		}

		private INetworkConnection tcpConnection;

		private QueueByte incoming;

		private byte[] data;

		private const int dataLength = 16384;

		private int received;
	}
}
