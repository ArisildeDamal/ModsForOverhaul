using System;
using ProtoBuf;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace Vintagestory.Server
{
	public class UdpNetworkChannel : NetworkChannel
	{
		private FastMemoryStream reusableStream
		{
			get
			{
				FastMemoryStream fastMemoryStream;
				if ((fastMemoryStream = UdpNetworkChannel.reusableStreamPerThread) == null)
				{
					fastMemoryStream = (UdpNetworkChannel.reusableStreamPerThread = new FastMemoryStream());
				}
				return fastMemoryStream;
			}
		}

		public UdpNetworkChannel(NetworkAPI api, int channelId, string channelName)
			: base(api, channelId, channelName)
		{
		}

		public new void OnPacket(Packet_CustomPacket packet, IServerPlayer player)
		{
			if (packet.MessageId >= this.handlersUdp.Length)
			{
				return;
			}
			Action<Packet_CustomPacket, IServerPlayer> action = this.handlersUdp[packet.MessageId];
			if (action == null)
			{
				return;
			}
			action(packet, player);
		}

		public override IServerNetworkChannel SetMessageHandler<T>(NetworkClientMessageHandler<T> handler)
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
			this.handlersUdp[messageId] = delegate(Packet_CustomPacket p, IServerPlayer player)
			{
				T message;
				using (FastMemoryStream ms = new FastMemoryStream(p.Data, p.Data.Length))
				{
					message = Serializer.Deserialize<T>(ms);
				}
				handler(player, message);
			};
			return this;
		}

		public override void BroadcastPacket<T>(T message, params IServerPlayer[] exceptPlayers)
		{
			int messageId;
			if (!this.messageTypes.TryGetValue(typeof(T), out messageId))
			{
				string text = "No such message type ";
				Type typeFromHandle = typeof(T);
				throw new Exception(text + ((typeFromHandle != null) ? typeFromHandle.ToString() : null) + " registered. Did you forgot to call RegisterMessageType?");
			}
			this.reusableStream.Reset();
			Serializer.Serialize<T>(this.reusableStream, message);
			Packet_CustomPacket udpChannelPacket = new Packet_CustomPacket
			{
				ChannelId = this.channelId,
				MessageId = messageId,
				Data = this.reusableStream.ToArray()
			};
			Packet_UdpPacket udpPacket = new Packet_UdpPacket
			{
				Id = 6,
				ChannelPacket = udpChannelPacket
			};
			this.api.server.BroadcastArbitraryUdpPacket(udpPacket, exceptPlayers);
		}

		public override void SendPacket<T>(T message, params IServerPlayer[] players)
		{
			if (players == null || players.Length == 0)
			{
				throw new ArgumentNullException("No players supplied to send the packet to");
			}
			int messageId;
			if (!this.messageTypes.TryGetValue(typeof(T), out messageId))
			{
				string text = "No such message type ";
				Type typeFromHandle = typeof(T);
				throw new Exception(text + ((typeFromHandle != null) ? typeFromHandle.ToString() : null) + " registered. Did you forgot to call RegisterMessageType?");
			}
			this.reusableStream.Reset();
			Serializer.Serialize<T>(this.reusableStream, message);
			Packet_CustomPacket udpChannelPacket = new Packet_CustomPacket
			{
				ChannelId = this.channelId,
				MessageId = messageId,
				Data = this.reusableStream.ToArray()
			};
			Packet_UdpPacket udpPacket = new Packet_UdpPacket
			{
				Id = 6,
				ChannelPacket = udpChannelPacket
			};
			this.api.server.SendArbitraryUdpPacket(udpPacket, players);
		}

		internal Action<Packet_CustomPacket, IServerPlayer>[] handlersUdp = new Action<Packet_CustomPacket, IServerPlayer>[256];

		[ThreadStatic]
		private static FastMemoryStream reusableStreamPerThread = new FastMemoryStream();
	}
}
