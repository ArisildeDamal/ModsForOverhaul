using System;
using System.Collections.Generic;
using Vintagestory.API.Client;

namespace Vintagestory.Common
{
	public abstract class NetworkChannelBase : INetworkChannel
	{
		public NetworkChannelBase(int channelId, string channelName)
		{
			this.channelId = channelId;
			this.channelName = channelName;
		}

		public string ChannelName
		{
			get
			{
				return this.channelName;
			}
		}

		public INetworkChannel RegisterMessageType(Type type)
		{
			Dictionary<Type, int> dictionary = this.messageTypes;
			int num = this.nextHandlerId;
			this.nextHandlerId = num + 1;
			dictionary[type] = num;
			return this;
		}

		public INetworkChannel RegisterMessageType<T>()
		{
			Dictionary<Type, int> dictionary = this.messageTypes;
			Type typeFromHandle = typeof(T);
			int num = this.nextHandlerId;
			this.nextHandlerId = num + 1;
			dictionary[typeFromHandle] = num;
			return this;
		}

		internal int channelId;

		internal string channelName;

		internal int nextHandlerId;

		internal Dictionary<Type, int> messageTypes = new Dictionary<Type, int>();
	}
}
