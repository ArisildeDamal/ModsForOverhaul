using System;
using System.Collections.Generic;
using System.IO;
using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace Vintagestory.Server
{
	public class NetworkChannel : NetworkChannelBase, IServerNetworkChannel, INetworkChannel
	{
		private FastMemoryStream reusableStream
		{
			get
			{
				FastMemoryStream fastMemoryStream;
				if ((fastMemoryStream = NetworkChannel.reusableStreamPerThread) == null)
				{
					fastMemoryStream = (NetworkChannel.reusableStreamPerThread = new FastMemoryStream());
				}
				return fastMemoryStream;
			}
		}

		public NetworkChannel(NetworkAPI api, int channelId, string channelName)
			: base(channelId, channelName)
		{
			this.api = api;
		}

		public void OnPacket(Packet_CustomPacket p, IServerPlayer player)
		{
			if (p.MessageId >= this.handlers.Length)
			{
				return;
			}
			Action<Packet_CustomPacket, IServerPlayer> action = this.handlers[p.MessageId];
			if (action != null)
			{
				action(p, player);
			}
			ServerMain.FrameProfiler.Mark("handlecustom", p.MessageId);
		}

		public new IServerNetworkChannel RegisterMessageType(Type type)
		{
			Dictionary<Type, int> messageTypes = this.messageTypes;
			int nextHandlerId = this.nextHandlerId;
			this.nextHandlerId = nextHandlerId + 1;
			messageTypes[type] = nextHandlerId;
			return this;
		}

		public new IServerNetworkChannel RegisterMessageType<T>()
		{
			Dictionary<Type, int> messageTypes = this.messageTypes;
			Type typeFromHandle = typeof(T);
			int nextHandlerId = this.nextHandlerId;
			this.nextHandlerId = nextHandlerId + 1;
			messageTypes[typeFromHandle] = nextHandlerId;
			return this;
		}

		public virtual IServerNetworkChannel SetMessageHandler<T>(NetworkClientMessageHandler<T> handler)
		{
			int messageId;
			if (!this.messageTypes.TryGetValue(typeof(T), out messageId))
			{
				string text = "No such message type ";
				Type typeFromHandle = typeof(T);
				throw new Exception(text + ((typeFromHandle != null) ? typeFromHandle.ToString() : null) + " registered. Did you forgot to call RegisterMessageType?");
			}
			this.handlers[messageId] = delegate(Packet_CustomPacket p, IServerPlayer player)
			{
				T message;
				using (MemoryStream ms = new MemoryStream(p.Data))
				{
					message = Serializer.Deserialize<T>(ms);
				}
				handler(player, message);
			};
			return this;
		}

		public virtual void SendPacket<T>(T message, params IServerPlayer[] players)
		{
			if (players == null || players.Length == 0)
			{
				throw new ArgumentNullException("No players supplied to send the packet to");
			}
			this.api.server.SendArbitraryPacket(this.GenPacket<T>(message), players);
		}

		public void SendPacket<T>(T message, byte[] data, params IServerPlayer[] players)
		{
			if (players == null || players.Length == 0)
			{
				throw new ArgumentNullException("No players supplied to send the packet to");
			}
			this.api.server.SendArbitraryPacket(this.GenPacket<T>(message, data), players);
		}

		public virtual void BroadcastPacket<T>(T message, params IServerPlayer[] exceptPlayers)
		{
			this.api.server.BroadcastArbitraryPacket(this.GenPacket<T>(message), exceptPlayers);
		}

		private Packet_Server GenPacket<T>(T message)
		{
			this.reusableStream.Reset();
			Serializer.Serialize<T>(this.reusableStream, message);
			return this.GenPacket<T>(message, this.reusableStream.ToArray());
		}

		private Packet_Server GenPacket<T>(T message, byte[] data)
		{
			int messageId;
			if (!this.messageTypes.TryGetValue(typeof(T), out messageId))
			{
				string text = "No such message type ";
				Type typeFromHandle = typeof(T);
				throw new Exception(text + ((typeFromHandle != null) ? typeFromHandle.ToString() : null) + " registered. Did you forgot to call RegisterMessageType?");
			}
			Packet_CustomPacket p = new Packet_CustomPacket
			{
				ChannelId = this.channelId,
				MessageId = messageId
			};
			p.SetData(data);
			return new Packet_Server
			{
				Id = 55,
				CustomPacket = p
			};
		}

		protected NetworkAPI api;

		internal Action<Packet_CustomPacket, IServerPlayer>[] handlers = new Action<Packet_CustomPacket, IServerPlayer>[256];

		[ThreadStatic]
		private static FastMemoryStream reusableStreamPerThread = new FastMemoryStream();
	}
}
