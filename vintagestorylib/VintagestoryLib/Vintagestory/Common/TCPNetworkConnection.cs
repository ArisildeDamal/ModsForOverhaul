using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.Client.NoObf;

namespace Vintagestory.Common
{
	public class TCPNetworkConnection : INetworkConnection
	{
		public bool Connected
		{
			get
			{
				return this.connected;
			}
		}

		public bool Disconnected
		{
			get
			{
				return this.disconnected;
			}
		}

		public TCPNetworkConnection(string ip, int port, Action<ConnectionResult> onConnectResult, Action<Exception> onDisconnected)
		{
			TCPNetworkConnection.<>c__DisplayClass13_0 CS$<>8__locals1 = new TCPNetworkConnection.<>c__DisplayClass13_0();
			CS$<>8__locals1.ip = ip;
			CS$<>8__locals1.port = port;
			CS$<>8__locals1.onConnectResult = onConnectResult;
			base..ctor();
			CS$<>8__locals1.<>4__this = this;
			this.onDisconnected = onDisconnected;
			IPAddress addr;
			if (IPAddress.TryParse(CS$<>8__locals1.ip, out addr) && addr.AddressFamily == AddressFamily.InterNetworkV6)
			{
				this.tcpSocket = new VintageStorySocket(addr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			}
			else
			{
				try
				{
					this.tcpSocket = new VintageStorySocket(SocketType.Stream, ProtocolType.Tcp);
					this.tcpSocket.DualMode = true;
				}
				catch (NotSupportedException)
				{
					Console.Error.WriteLine("NotSupportedException thrown when trying to init a dual mode socket. Will attempt init ipv4 only socket.");
					this.tcpSocket = new VintageStorySocket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				}
			}
			this.tcpSocket.NoDelay = true;
			Task.Run(delegate
			{
				TCPNetworkConnection.<>c__DisplayClass13_0.<<-ctor>b__0>d <<-ctor>b__0>d;
				<<-ctor>b__0>d.<>t__builder = AsyncTaskMethodBuilder.Create();
				<<-ctor>b__0>d.<>4__this = CS$<>8__locals1;
				<<-ctor>b__0>d.<>1__state = -1;
				<<-ctor>b__0>d.<>t__builder.Start<TCPNetworkConnection.<>c__DisplayClass13_0.<<-ctor>b__0>d>(ref <<-ctor>b__0>d);
				return <<-ctor>b__0>d.<>t__builder.Task;
			}, this.cts.Token);
		}

		public void Disconnect()
		{
			if (this.tcpSocket != null)
			{
				try
				{
					this.tcpSocket.Shutdown(SocketShutdown.Send);
				}
				catch
				{
				}
				TyronThreadPool.QueueLongDurationTask(delegate
				{
					try
					{
						Thread.Sleep(1000);
						VintageStorySocket vintageStorySocket = this.tcpSocket;
						if (vintageStorySocket != null && !vintageStorySocket.Disposed)
						{
							this.tcpSocket.Shutdown(SocketShutdown.Receive);
							this.tcpSocket.Close();
							this.cts.Cancel();
							this.tcpSocket = null;
						}
					}
					catch
					{
					}
				}, "disconnect");
			}
			this.onDisconnected = null;
			this.disconnected = true;
			this.connected = false;
		}

		protected unsafe async void OnBytesReceived(object state)
		{
			try
			{
				while (this.tcpSocket.Connected && !this.cts.Token.IsCancellationRequested)
				{
					int nBytesRec = await this.tcpSocket.ReceiveAsync(this.dataRcvBuf, this.cts.Token);
					if (nBytesRec <= 0)
					{
						this.disconnected = true;
						if (this.onDisconnected == null)
						{
							try
							{
								this.tcpSocket.Close();
								this.cts.Cancel();
								break;
							}
							catch
							{
								break;
							}
						}
						this.onDisconnected(new Exception("The server closed down the socket without response. The server may be password protected or whitelisted"));
						break;
					}
					Queue<byte> queue = this.received;
					lock (queue)
					{
						for (int i = 0; i < nBytesRec; i++)
						{
							this.received.Enqueue(*this.dataRcvBuf.Span[i]);
						}
					}
				}
			}
			catch (Exception e)
			{
				try
				{
					VintageStorySocket vintageStorySocket = this.tcpSocket;
					if (vintageStorySocket != null)
					{
						vintageStorySocket.Close();
					}
					this.cts.Cancel();
				}
				catch
				{
				}
				this.disconnected = true;
				Action<Exception> action = this.onDisconnected;
				if (action != null)
				{
					action(e);
				}
			}
		}

		public void Receive(byte[] data, int dataLength, out int total)
		{
			total = 0;
			Queue<byte> queue = this.received;
			lock (queue)
			{
				for (int i = 0; i < dataLength; i++)
				{
					if (this.received.Count == 0)
					{
						break;
					}
					data[i] = this.received.Dequeue();
					total++;
				}
			}
		}

		public void Send(byte[] data, int length)
		{
			if (!this.connected)
			{
				if (!this.disconnected)
				{
					for (int i = 0; i < length; i++)
					{
						this.tosendBeforeConnected.Enqueue(data[i]);
					}
				}
				return;
			}
			try
			{
				this.tcpSocket.SendAsync(data, SocketFlags.None, this.cts.Token);
			}
			catch (Exception e)
			{
				this.disconnected = true;
				Action<Exception> action = this.onDisconnected;
				if (action != null)
				{
					action(e);
				}
			}
		}

		public override string ToString()
		{
			return this.address ?? base.ToString();
		}

		private bool connected;

		private bool disconnected;

		public VintageStorySocket tcpSocket;

		public string address;

		private Memory<byte> dataRcvBuf = new byte[8192];

		private Action<Exception> onDisconnected;

		private Queue<byte> received = new Queue<byte>();

		private Queue<byte> tosendBeforeConnected = new Queue<byte>();

		private CancellationTokenSource cts = new CancellationTokenSource();
	}
}
