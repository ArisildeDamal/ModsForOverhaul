using System;
using System.Collections.Generic;
using System.IO;
using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf
{
	public class NetworkChannel : NetworkChannelBase, IClientNetworkChannel, INetworkChannel
	{
		public bool Connected { get; set; }

		bool IClientNetworkChannel.Connected
		{
			get
			{
				return this.Connected;
			}
		}

		public NetworkChannel(NetworkAPI api, int channelId, string channelName)
			: base(channelId, channelName)
		{
			this.api = api;
		}

		public void OnPacket(Packet_CustomPacket p)
		{
			if (p.MessageId >= this.handlers.Length)
			{
				return;
			}
			Action<Packet_CustomPacket> action = this.handlers[p.MessageId];
			if (action == null)
			{
				return;
			}
			action(p);
		}

		public new IClientNetworkChannel RegisterMessageType(Type type)
		{
			Dictionary<Type, int> messageTypes = this.messageTypes;
			int nextHandlerId = this.nextHandlerId;
			this.nextHandlerId = nextHandlerId + 1;
			messageTypes[type] = nextHandlerId;
			return this;
		}

		public new IClientNetworkChannel RegisterMessageType<T>()
		{
			Dictionary<Type, int> messageTypes = this.messageTypes;
			Type typeFromHandle = typeof(T);
			int nextHandlerId = this.nextHandlerId;
			this.nextHandlerId = nextHandlerId + 1;
			messageTypes[typeFromHandle] = nextHandlerId;
			return this;
		}

		public virtual IClientNetworkChannel SetMessageHandler<T>(NetworkServerMessageHandler<T> handler)
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
			this.handlers[messageId] = delegate(Packet_CustomPacket p)
			{
				T message = default(T);
				if (p.Data != null)
				{
					using (MemoryStream ms = new MemoryStream(p.Data))
					{
						message = Serializer.Deserialize<T>(ms);
					}
				}
				handler(message);
			};
			return this;
		}

		public virtual void SendPacket<T>(T message)
		{
			if (!this.Connected)
			{
				throw new Exception("Attempting to send data to a not connected channel. For optionally dependent network channels test if your channel is Connected before sending data.");
			}
			int messageId;
			if (!this.messageTypes.TryGetValue(typeof(T), out messageId))
			{
				string text = "No such message type ";
				Type typeFromHandle = typeof(T);
				throw new Exception(text + ((typeFromHandle != null) ? typeFromHandle.ToString() : null) + " registered. Did you forgot to call RegisterMessageType?");
			}
			byte[] data;
			using (MemoryStream ms = new MemoryStream())
			{
				Serializer.Serialize<T>(ms, message);
				data = ms.ToArray();
			}
			Packet_CustomPacket p = new Packet_CustomPacket
			{
				ChannelId = this.channelId,
				MessageId = messageId
			};
			p.SetData(data);
			this.api.game.SendPacketClient(new Packet_Client
			{
				Id = 23,
				CustomPacket = p
			});
		}

		protected NetworkAPI api;

		internal Action<Packet_CustomPacket>[] handlers = new Action<Packet_CustomPacket>[128];
	}
}
