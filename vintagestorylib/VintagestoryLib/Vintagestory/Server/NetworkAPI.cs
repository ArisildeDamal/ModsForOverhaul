using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace Vintagestory.Server
{
	public class NetworkAPI : IServerNetworkAPI, INetworkAPI
	{
		public NetworkAPI(ServerMain server)
		{
			this.server = server;
			server.PacketHandlers[23] = new ClientPacketHandler<Packet_Client, ConnectedClient>(this.HandleCustomPacket);
			server.HandleCustomUdpPackets = new HandleClientCustomUdpPacket(this.HandleCustomUdpPacket);
		}

		private void HandleCustomUdpPacket(Packet_CustomPacket packet, IServerPlayer player)
		{
			UdpNetworkChannel channel;
			if (this.channelsUdp.TryGetValue(packet.ChannelId, out channel))
			{
				channel.OnPacket(packet, player);
			}
		}

		public void SendChannelsPacket(IServerPlayer player)
		{
			Packet_NetworkChannels p = new Packet_NetworkChannels();
			p.SetChannelIds(this.channels.Keys.ToArray<int>());
			p.SetChannelNames(this.channels.Values.Select((NetworkChannel ch) => ch.ChannelName).ToArray<string>());
			p.SetChannelUdpIds(this.channelsUdp.Keys.ToArray<int>());
			p.SetChannelUdpNames(this.channelsUdp.Values.Select((UdpNetworkChannel ch) => ch.ChannelName).ToArray<string>());
			this.server.SendPacket(player.ClientId, new Packet_Server
			{
				Id = 56,
				NetworkChannels = p
			});
		}

		private void HandleCustomPacket(Packet_Client packet, ConnectedClient client)
		{
			Packet_CustomPacket p = packet.CustomPacket;
			NetworkChannel channel;
			if (this.channels.TryGetValue(p.ChannelId, out channel))
			{
				channel.OnPacket(p, client.Player);
			}
		}

		public IServerNetworkChannel RegisterChannel(string channelName)
		{
			this.nextFreeChannelId++;
			this.channels[this.nextFreeChannelId] = new NetworkChannel(this, this.nextFreeChannelId, channelName);
			return this.channels[this.nextFreeChannelId];
		}

		public IServerNetworkChannel RegisterUdpChannel(string channelName)
		{
			this.nextFreeUdpChannelId++;
			this.channelsUdp[this.nextFreeUdpChannelId] = new UdpNetworkChannel(this, this.nextFreeUdpChannelId, channelName);
			return this.channelsUdp[this.nextFreeUdpChannelId];
		}

		public void BroadcastArbitraryPacket(byte[] data, params IServerPlayer[] exceptPlayers)
		{
			this.server.BroadcastArbitraryPacket(data, exceptPlayers);
		}

		public void BroadcastArbitraryPacket(object packet, params IServerPlayer[] exceptPlayers)
		{
			this.server.BroadcastArbitraryPacket(packet as Packet_Server, exceptPlayers);
		}

		public void BroadcastBlockEntityPacket(BlockPos pos, int packetId, byte[] data = null)
		{
			this.server.BroadcastBlockEntityPacket(pos.X, pos.InternalY, pos.Z, packetId, data, Array.Empty<IServerPlayer>());
		}

		public void BroadcastBlockEntityPacket(BlockPos pos, int packetId, byte[] data = null, params IServerPlayer[] skipPlayers)
		{
			this.server.BroadcastBlockEntityPacket(pos.X, pos.InternalY, pos.Z, packetId, data, skipPlayers);
		}

		public void SendArbitraryPacket(byte[] data, params IServerPlayer[] players)
		{
			this.server.SendArbitraryPacket(data, players);
		}

		public void SendArbitraryPacket(object packet, params IServerPlayer[] players)
		{
			this.server.SendArbitraryPacket(packet as Packet_Server, players);
		}

		public void SendBlockEntityPacket(IServerPlayer player, BlockPos pos, int packetId, byte[] data = null)
		{
			this.server.SendBlockEntityMessagePacket(player, pos.X, pos.InternalY, pos.Z, packetId, data);
		}

		public void SendEntityPacket(IServerPlayer player, long entityid, int packetId, byte[] data = null)
		{
			this.server.SendEntityPacket(player, entityid, packetId, data);
		}

		public void BroadcastEntityPacket(long entityid, int packetId, byte[] data = null)
		{
			this.server.BroadcastEntityPacket(entityid, packetId, data);
		}

		INetworkChannel INetworkAPI.RegisterChannel(string channelName)
		{
			return this.RegisterChannel(channelName);
		}

		INetworkChannel INetworkAPI.RegisterUdpChannel(string channelName)
		{
			return this.RegisterUdpChannel(channelName);
		}

		public INetworkChannel GetChannel(string channelName)
		{
			return this.channels.FirstOrDefault((KeyValuePair<int, NetworkChannel> pair) => pair.Value.ChannelName == channelName).Value;
		}

		IServerNetworkChannel IServerNetworkAPI.GetChannel(string channelName)
		{
			return this.channels.FirstOrDefault((KeyValuePair<int, NetworkChannel> pair) => pair.Value.ChannelName == channelName).Value;
		}

		public INetworkChannel GetUdpChannel(string channelName)
		{
			return this.channelsUdp.FirstOrDefault((KeyValuePair<int, UdpNetworkChannel> pair) => pair.Value.ChannelName == channelName).Value;
		}

		IServerNetworkChannel IServerNetworkAPI.GetUdpChannel(string channelName)
		{
			return this.channelsUdp.FirstOrDefault((KeyValuePair<int, UdpNetworkChannel> pair) => pair.Value.ChannelName == channelName).Value;
		}

		public void SendBlockEntityPacket<T>(IServerPlayer player, BlockPos pos, int packetId, T data = default(T))
		{
			this.server.SendBlockEntityMessagePacket(player, pos.X, pos.InternalY, pos.Z, packetId, SerializerUtil.Serialize<T>(data));
		}

		public void BroadcastBlockEntityPacket<T>(BlockPos pos, int packetId, T data = default(T))
		{
			this.server.BroadcastBlockEntityPacket(pos.X, pos.InternalY, pos.Z, packetId, SerializerUtil.Serialize<T>(data), Array.Empty<IServerPlayer>());
		}

		internal ServerMain server;

		private OrderedDictionary<int, NetworkChannel> channels = new OrderedDictionary<int, NetworkChannel>();

		private OrderedDictionary<int, UdpNetworkChannel> channelsUdp = new OrderedDictionary<int, UdpNetworkChannel>();

		private int nextFreeChannelId;

		private int nextFreeUdpChannelId;
	}
}
