using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Vintagestory.Common;

namespace Vintagestory.Client
{
	public class UdpNetClient : UNetClient
	{
		public override event UNetClient.UdpConnectionRequest DidReceiveUdpConnectionRequest;

		public override void Connect(string ip, int port)
		{
			this.udpClient = new UdpClient(ip, port);
			this.udpListenTask = new Task(new Action<object>(this.ListenClient), null, this.cts.Token, TaskCreationOptions.LongRunning);
			this.udpClient.Connect(ip, port);
			this.udpListenTask.Start();
		}

		private async void ListenClient(object state)
		{
			while (!this.cts.IsCancellationRequested)
			{
				try
				{
					UdpReceiveResult result = await this.udpClient.ReceiveAsync(this.cts.Token);
					Packet_UdpPacket packet = new Packet_UdpPacket();
					Packet_UdpPacketSerializer.Deserialize(new CitoMemoryStream(result.Buffer, result.Buffer.Length), packet);
					if (packet.Id > 0)
					{
						if (packet.Id == 1)
						{
							UNetClient.UdpConnectionRequest didReceiveUdpConnectionRequest = this.DidReceiveUdpConnectionRequest;
							if (didReceiveUdpConnectionRequest != null)
							{
								didReceiveUdpConnectionRequest();
							}
						}
						else
						{
							packet.Length = result.Buffer.Length;
							this.clientPacketQueue.Enqueue(packet);
						}
					}
				}
				catch
				{
				}
			}
		}

		public override IEnumerable<Packet_UdpPacket> ReadMessage()
		{
			Packet_UdpPacket[] packets = null;
			if (!this.clientPacketQueue.IsEmpty)
			{
				packets = this.clientPacketQueue.ToArray();
				this.clientPacketQueue.Clear();
			}
			return packets;
		}

		public override void EnqueuePacket(Packet_UdpPacket udpPacket)
		{
			this.clientPacketQueue.Enqueue(udpPacket);
		}

		public override void Send(Packet_UdpPacket packet)
		{
			if (this.disposed)
			{
				return;
			}
			try
			{
				byte[] data = Packet_UdpPacketSerializer.SerializeToBytes(packet);
				this.udpClient.Send(data);
			}
			catch
			{
			}
		}

		public override void Dispose()
		{
			if (!this.disposed)
			{
				this.disposed = true;
				UdpClient udpClient = this.udpClient;
				if (udpClient != null)
				{
					udpClient.Dispose();
				}
				CancellationTokenSource cancellationTokenSource = this.cts;
				if (cancellationTokenSource != null)
				{
					cancellationTokenSource.Cancel();
				}
				CancellationTokenSource cancellationTokenSource2 = this.cts;
				if (cancellationTokenSource2 != null)
				{
					cancellationTokenSource2.Dispose();
				}
				Task task = this.udpListenTask;
				if (task == null)
				{
					return;
				}
				task.Dispose();
			}
		}

		protected internal UdpClient udpClient;

		private readonly CancellationTokenSource cts = new CancellationTokenSource();

		private ConcurrentQueue<Packet_UdpPacket> clientPacketQueue = new ConcurrentQueue<Packet_UdpPacket>();

		private Task udpListenTask;

		private bool disposed;
	}
}
