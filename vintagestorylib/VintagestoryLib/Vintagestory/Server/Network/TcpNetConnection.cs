using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.Common;

namespace Vintagestory.Server.Network
{
	public class TcpNetConnection : NetConnection
	{
		public event OnReceivedMessageDelegate ReceivedMessage;

		public event TcpConnectionDelegate Disconnected;

		public void SetLengthLimit(bool isCreative)
		{
			this.MaxPacketSize = (isCreative ? int.MaxValue : 1000000);
		}

		public void StartReceiving()
		{
			this.cts = new CancellationTokenSource();
			Task.Run(new Func<Task>(this.ReceiveData));
		}

		private unsafe async Task ReceiveData()
		{
			try
			{
				FastMemoryStream receivedBytes = null;
				while (this.TcpSocket.Connected && !this.cts.Token.IsCancellationRequested)
				{
					int nBytesRec;
					try
					{
						nBytesRec = await this.TcpSocket.ReceiveAsync(this.dataRcvBuf, this.cts.Token);
					}
					catch
					{
						this.InvokeDisconnected();
						return;
					}
					if (nBytesRec <= 0)
					{
						this.InvokeDisconnected();
						return;
					}
					if ((base.client == null || base.client.IsNewClient) && nBytesRec > 4 && (receivedBytes == null || receivedBytes.Position == 0L))
					{
						int peekPacketId = (int)(*this.dataRcvBuf.Span[4]);
						if (peekPacketId != 8 && peekPacketId != 18)
						{
							this.DisconnectForBadPacket("Client " + this.Address + " disconnected, invalid packet received");
							return;
						}
					}
					if (receivedBytes == null)
					{
						receivedBytes = new FastMemoryStream(512);
					}
					receivedBytes.Write(this.dataRcvBuf.Span.Slice(0, nBytesRec));
					while (receivedBytes.Position >= 4L)
					{
						byte[] receivedBytesArray = receivedBytes.GetBuffer();
						int packetLength = NetIncomingMessage.ReadInt(receivedBytesArray);
						bool compressed = packetLength < 0;
						packetLength &= int.MaxValue;
						if (packetLength > this.MaxPacketSize)
						{
							DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(57, 2);
							defaultInterpolatedStringHandler.AppendLiteral("Client ");
							defaultInterpolatedStringHandler.AppendFormatted(this.Address);
							defaultInterpolatedStringHandler.AppendLiteral(" disconnected, too large packet of ");
							defaultInterpolatedStringHandler.AppendFormatted<int>(packetLength);
							defaultInterpolatedStringHandler.AppendLiteral(" bytes received");
							this.DisconnectForBadPacket(defaultInterpolatedStringHandler.ToStringAndClear());
							return;
						}
						if (packetLength == 0)
						{
							receivedBytes.RemoveFromStart(4);
						}
						else
						{
							if (receivedBytes.Position < (long)(4 + packetLength))
							{
								break;
							}
							byte[] packet;
							if (compressed)
							{
								packet = Compression.Decompress(receivedBytesArray, 4, packetLength);
							}
							else
							{
								packet = new byte[packetLength];
								for (int i = 0; i < packetLength; i++)
								{
									packet[i] = receivedBytesArray[4 + i];
								}
							}
							receivedBytes.RemoveFromStart(4 + packetLength);
							int packetId = ProtocolParser.PeekPacketId(packet);
							if (packetId <= 0 || packetId >= (TcpNetConnection.MaxPacketClientId + 1) * 8)
							{
								this.DisconnectForBadPacket("Client " + this.Address + " disconnected, send packet with invalid client packet id: " + packetId.ToString());
								return;
							}
							this.ReceivedMessage(packet, this);
						}
					}
				}
				receivedBytes = null;
			}
			catch
			{
				this.InvokeDisconnected();
			}
		}

		private void DisconnectForBadPacket(string msg)
		{
			if (TcpNetConnection.TemporaryIpBlockList)
			{
				TcpNetConnection.blockedIps.Add(((IPEndPoint)this.TcpSocket.RemoteEndPoint).Address.ToString());
			}
			this.InvokeDisconnected();
			ServerMain.Logger.Notification(msg);
		}

		public override EnumSendResult Send(byte[] data, bool compressedFlag)
		{
			EnumSendResult enumSendResult;
			try
			{
				int length = data.Length;
				byte[] dataWithLength = new byte[length + 4];
				NetIncomingMessage.WriteInt(dataWithLength, length | ((((compressedFlag > false) ? 1 : 0) << 31) ? 1 : 0));
				for (int i = 0; i < length; i++)
				{
					dataWithLength[4 + i] = data[i];
				}
				this.TcpSocket.SendAsync(dataWithLength, SocketFlags.None, this.cts.Token);
				enumSendResult = EnumSendResult.Ok;
			}
			catch
			{
				this.InvokeDisconnected();
				enumSendResult = EnumSendResult.Disconnected;
			}
			return enumSendResult;
		}

		public EnumSendResult SendPreparedBytes(byte[] dataWithLength, int length, bool compressedFlag)
		{
			if (this.cts == null)
			{
				return EnumSendResult.Disconnected;
			}
			EnumSendResult enumSendResult;
			try
			{
				NetIncomingMessage.WriteInt(dataWithLength, length | ((((compressedFlag > false) ? 1 : 0) << 31) ? 1 : 0));
				this.TcpSocket.SendAsync(dataWithLength, SocketFlags.None, this.cts.Token);
				enumSendResult = EnumSendResult.Ok;
			}
			catch
			{
				this.InvokeDisconnected();
				enumSendResult = EnumSendResult.Disconnected;
			}
			return enumSendResult;
		}

		public override string ToString()
		{
			if (this.Address != null)
			{
				return this.Address;
			}
			return base.ToString();
		}

		public TcpNetConnection(Socket tcpSocket)
		{
			this.TcpSocket = tcpSocket;
			IPEndPoint enpoint = tcpSocket.RemoteEndPoint as IPEndPoint;
			if (enpoint != null)
			{
				this.IpEndpoint = enpoint;
				this.Address = enpoint.Address.ToString();
				return;
			}
			this.IpEndpoint = new IPEndPoint(0L, 0);
			this.Address = "0.0.0.0";
		}

		public override IPEndPoint RemoteEndPoint()
		{
			return this.IpEndpoint;
		}

		public override EnumSendResult HiPerformanceSend(BoxedPacket box, ILogger Logger, bool compressionAllowed)
		{
			bool compressed;
			byte[] packetBytes = this.PreparePacketForSending(box, compressionAllowed, out compressed);
			return this.SendPreparedPacket(packetBytes, compressed, Logger);
		}

		public override byte[] PreparePacketForSending(BoxedPacket box, bool compressionAllowed, out bool compressed)
		{
			int len = box.Length;
			compressed = false;
			byte[] packet;
			if (len > 1460 && compressionAllowed)
			{
				packet = Compression.CompressOffset4(box.buffer, len);
				len = packet.Length - 4;
				compressed = true;
			}
			else
			{
				packet = box.Clone(4);
			}
			box.LengthSent = len;
			return packet;
		}

		public override EnumSendResult SendPreparedPacket(byte[] packet, bool compressed, ILogger Logger)
		{
			EnumSendResult result;
			try
			{
				result = this.SendPreparedBytes(packet, packet.Length - 4, compressed);
			}
			catch (Exception e)
			{
				Logger.Error("Network exception:");
				Logger.Error(e);
				return EnumSendResult.Error;
			}
			return result;
		}

		public override bool EqualsConnection(NetConnection connection)
		{
			return this.TcpSocket == ((TcpNetConnection)connection).TcpSocket;
		}

		public override void Shutdown()
		{
			if (this.TcpSocket == null)
			{
				return;
			}
			try
			{
				this.TcpSocket.Shutdown(SocketShutdown.Both);
			}
			catch
			{
			}
		}

		public override void Close()
		{
			try
			{
				CancellationTokenSource cancellationTokenSource = this.cts;
				if (cancellationTokenSource != null)
				{
					cancellationTokenSource.Cancel();
				}
			}
			catch
			{
			}
			try
			{
				Socket tcpSocket = this.TcpSocket;
				if (tcpSocket != null)
				{
					tcpSocket.Close();
				}
			}
			catch
			{
			}
			this.Dispose();
		}

		internal void InvokeDisconnected()
		{
			if (this._disposed)
			{
				return;
			}
			try
			{
				this.cts.Cancel();
			}
			catch
			{
			}
			try
			{
				this.TcpSocket.Close();
			}
			catch
			{
			}
			if (this.Disconnected != null && this.TcpSocket != null && this.Connected)
			{
				this.Disconnected(this);
				this.Connected = false;
			}
			this.Dispose();
		}

		public void Dispose()
		{
			if (!this._disposed)
			{
				this._disposed = true;
				this.TcpSocket.Dispose();
				this.cts.Dispose();
				this.TcpSocket = null;
				this.cts = null;
			}
		}

		public static int DetermineMaxPacketId()
		{
			MemberInfo[] members = typeof(Packet_ClientIdEnum).GetMembers();
			int maxid = 0;
			foreach (MemberInfo member in members)
			{
				if (member.MemberType == MemberTypes.Field)
				{
					FieldInfo f = member as FieldInfo;
					if (f != null && f.FieldType.Name == "Int32")
					{
						object value = f.GetValue(f);
						if (value is int)
						{
							int id = (int)value;
							if (id > maxid)
							{
								maxid = id;
							}
						}
					}
				}
			}
			return maxid;
		}

		public static HashSet<string> blockedIps = new HashSet<string>();

		public static bool TemporaryIpBlockList = false;

		public const int ClientSocketBufferSize = 4096;

		public static int MaxPacketClientId = TcpNetConnection.DetermineMaxPacketId();

		public const int clientIdentificationPacketId = 8;

		public const int pingPacketId = 18;

		public Socket TcpSocket;

		public string Address;

		public IPEndPoint IpEndpoint;

		public bool Connected;

		private bool _disposed;

		private Memory<byte> dataRcvBuf = new byte[4096];

		private CancellationTokenSource cts;

		public int MaxPacketSize = 5000;
	}
}
