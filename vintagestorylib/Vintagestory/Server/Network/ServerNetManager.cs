using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Vintagestory.Server.Network
{
	public class ServerNetManager : IDisposable
	{
		public ServerNetManager(CancellationToken cts)
		{
			this.cancellationToken = cts;
		}

		public event TcpConnectionDelegate Connected;

		public event OnReceivedMessageDelegate ReceivedMessage;

		public event TcpConnectionDelegate Disconnected;

		public void StartServer(int port, string ipAddress = null)
		{
			bool ossupportsIPv = Socket.OSSupportsIPv6;
			IPAddress addr = (ossupportsIPv ? IPAddress.IPv6Any : IPAddress.Any);
			bool dualMode = ossupportsIPv;
			if (ipAddress != null)
			{
				addr = IPAddress.Parse(ipAddress);
				dualMode = false;
			}
			try
			{
				this.Socket = new Socket(addr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
				if (dualMode)
				{
					this.Socket.DualMode = true;
				}
			}
			catch (NotSupportedException)
			{
				Console.Error.WriteLine("NotSupportedException thrown when trying to init a dual mode socket, maybe due to ipv6 being disabled on this system. Will attempt init ipv4 only socket.");
				this.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			}
			this.Socket.NoDelay = true;
			this.Socket.Bind(new IPEndPoint(addr, port));
			this.Socket.Listen(10);
			this.Socket.ReceiveTimeout = 5000;
			Task.Run(new Func<Task>(this.OnConnectRequest), this.cancellationToken);
		}

		private async Task OnConnectRequest()
		{
			while (!this.cancellationToken.IsCancellationRequested)
			{
				try
				{
					Socket clientSocket = await this.Socket.AcceptAsync(this.cancellationToken);
					string addr = ((IPEndPoint)clientSocket.RemoteEndPoint).Address.ToString();
					if (TcpNetConnection.TemporaryIpBlockList && TcpNetConnection.blockedIps.Contains(addr))
					{
						ServerMain.Logger.Notification("Client " + addr + " disconnected, blacklisted");
						clientSocket.Disconnect(false);
					}
					else
					{
						clientSocket.ReceiveBufferSize = 4096;
						TcpNetConnection tcpNetConnection = new TcpNetConnection(clientSocket);
						tcpNetConnection.ReceivedMessage += this.NewConnReceivedMessage;
						tcpNetConnection.Disconnected += this.NewConnDisconnected;
						tcpNetConnection.StartReceiving();
					}
				}
				catch
				{
				}
			}
		}

		private void NewConnDisconnected(TcpNetConnection tcpConnection)
		{
			try
			{
				this.Disconnected(tcpConnection);
			}
			catch
			{
			}
		}

		private void NewConnReceivedMessage(byte[] data, TcpNetConnection tcpConnection)
		{
			try
			{
				if (!tcpConnection.Connected && this.Connected != null)
				{
					tcpConnection.Connected = true;
					this.Connected(tcpConnection);
				}
				this.ReceivedMessage(data, tcpConnection);
			}
			catch
			{
			}
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!this.disposed)
			{
				if (disposing && this.Socket != null)
				{
					this.Socket.Dispose();
					this.Socket = null;
				}
				this.disposed = true;
			}
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		public Socket Socket;

		private readonly CancellationToken cancellationToken;

		private bool disposed;
	}
}
