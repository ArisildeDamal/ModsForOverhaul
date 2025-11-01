using System;
using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;

namespace Vintagestory.Client.NoObf
{
	public class UdpNetworkChannel : NetworkChannel
	{
		public UdpNetworkChannel(NetworkAPI api, int channelId, string channelName)
			: base(api, channelId, channelName)
		{
		}

		public override IClientNetworkChannel SetMessageHandler<T>(NetworkServerMessageHandler<T> handler)
		{
			int messageId;
			if (!this.messageTypes.TryGetValue(typeof(T), out messageId))
			{
				string text = "No such message type ";
				Type typeFromHandle = typeof(T);
				throw new Exception(text + ((typeFromHandle != null) ? typeFromHandle.ToString() : null) + " registered. Did you forgot to call RegisterMessageType?");
			}
			if (typeof(T).IsArray)
			{
				throw new ArgumentException("Please do not use array messages, they seem to cause serialization problems in rare cases. Pack that array into its own class.");
			}
			Serializer.PrepareSerializer<T>();
			this.handlersUdp[messageId] = delegate(Packet_CustomPacket p)
			{
				T message = default(T);
				if (p.Data != null)
				{
					using (FastMemoryStream ms = new FastMemoryStream(p.Data, p.Data.Length))
					{
						message = Serializer.Deserialize<T>(ms);
					}
				}
				handler(message);
			};
			return this;
		}

		public override void SendPacket<T>(T message)
		{
			if (!base.Connected)
			{
				throw new Exception("Attempting to send data to a not connected udp channel. For optionally dependent network channels test if your channel is Connected before sending data.");
			}
			int messageId;
			if (!this.messageTypes.TryGetValue(typeof(T), out messageId))
			{
				string text = "No such message type ";
				Type typeFromHandle = typeof(T);
				throw new Exception(text + ((typeFromHandle != null) ? typeFromHandle.ToString() : null) + " registered. Did you forgot to call RegisterMessageType?");
			}
			FastMemoryStream fastMemoryStream;
			if ((fastMemoryStream = UdpNetworkChannel.reusableStream) == null)
			{
				fastMemoryStream = (UdpNetworkChannel.reusableStream = new FastMemoryStream());
			}
			using (FastMemoryStream ms = fastMemoryStream)
			{
				ms.Reset();
				Serializer.Serialize<T>(ms, message);
				Packet_CustomPacket udpChannelPacket = new Packet_CustomPacket
				{
					ChannelId = this.channelId,
					MessageId = messageId,
					Data = ms.ToArray()
				};
				Packet_UdpPacket udpPacket = new Packet_UdpPacket
				{
					Id = 6,
					ChannelPacket = udpChannelPacket
				};
				if (this.api.game.FallBackToTcp)
				{
					Packet_Client packet = new Packet_Client
					{
						Id = 35,
						UdpPacket = udpPacket
					};
					this.api.game.SendPacketClient(packet);
				}
				else
				{
					this.api.game.UdpNetClient.Send(udpPacket);
				}
			}
		}

		public new void OnPacket(Packet_CustomPacket packet)
		{
			if (packet.MessageId >= this.handlersUdp.Length)
			{
				return;
			}
			Action<Packet_CustomPacket> action = this.handlersUdp[packet.MessageId];
			if (action == null)
			{
				return;
			}
			action(packet);
		}

		[ThreadStatic]
		private static FastMemoryStream reusableStream;

		internal Action<Packet_CustomPacket>[] handlersUdp = new Action<Packet_CustomPacket>[128];
	}
}
