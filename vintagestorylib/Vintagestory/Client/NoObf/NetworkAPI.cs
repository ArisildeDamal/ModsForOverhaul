using System;
using System.Collections.Generic;
using System.Linq;
using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.Common.Network.Packets;
using Vintagestory.Server;

namespace Vintagestory.Client.NoObf
{
	public class NetworkAPI : ClientSystem, IClientNetworkAPI, INetworkAPI
	{
		public NetworkAPI(ClientMain game)
			: base(game)
		{
			game.PacketHandlers[55] = new ServerPacketHandler<Packet_Server>(this.HandleCustomPacket);
			game.PacketHandlers[56] = new ServerPacketHandler<Packet_Server>(this.HandleChannelsPacket);
			game.HandleCustomUdpPackets = new HandleServerCustomUdpPacket(this.HandleCustomUdpPacket);
		}

		public void SendPlayerNowReady()
		{
			if (!this.game.clientPlayingFired)
			{
				this.game.SendPacketClient(new Packet_Client
				{
					Id = 29
				});
				this.game.clientPlayingFired = true;
			}
		}

		public EnumChannelState GetChannelState(string channelName)
		{
			if (this.clientchannels.Values.FirstOrDefault((NetworkChannel c) => c.channelName == channelName) == null)
			{
				return EnumChannelState.NotFound;
			}
			if (!this.serverChannelsReceived)
			{
				return EnumChannelState.Registered;
			}
			if (this.clientchannels.Values.FirstOrDefault((NetworkChannel c) => c.channelName == channelName) != null)
			{
				return EnumChannelState.Connected;
			}
			return EnumChannelState.NotConnected;
		}

		private void HandleChannelsPacket(Packet_Server packet)
		{
			NetworkAPI.<>c__DisplayClass13_0 CS$<>8__locals1 = new NetworkAPI.<>c__DisplayClass13_0();
			Dictionary<int, NetworkChannel> matchedchannels = new Dictionary<int, NetworkChannel>();
			Dictionary<int, UdpNetworkChannel> matchedchannelsUdp = new Dictionary<int, UdpNetworkChannel>();
			CS$<>8__locals1.serverPacket = packet.NetworkChannels;
			List<NetworkChannel> clientChannels = new List<NetworkChannel>(this.clientchannels.Values);
			List<UdpNetworkChannel> clientUdpChannels = new List<UdpNetworkChannel>(this.clientchannelsUdp.Values);
			int j;
			int num;
			for (j = 0; j < CS$<>8__locals1.serverPacket.ChannelNamesCount; j = num + 1)
			{
				NetworkChannel channel = clientChannels.FirstOrDefault((NetworkChannel ch) => ch.ChannelName == CS$<>8__locals1.serverPacket.ChannelNames[j]);
				if (channel != null)
				{
					clientChannels.Remove(channel);
					channel.channelId = CS$<>8__locals1.serverPacket.ChannelIds[j];
					channel.Connected = true;
					matchedchannels[CS$<>8__locals1.serverPacket.ChannelIds[j]] = channel;
				}
				else
				{
					this.game.Logger.Warning("Improperly configured mod. Server sends me channel name {0}, but no client side mod registered it.", new object[] { CS$<>8__locals1.serverPacket.ChannelNames[j] });
				}
				num = j;
			}
			if (clientChannels.Count > 0)
			{
				ILogger logger = this.game.Logger;
				string text = "Client registered {0} network channels ({1}) the server does not know about, may cause issues.";
				object[] array = new object[2];
				array[0] = clientChannels.Count;
				array[1] = string.Join(", ", clientChannels.Select((NetworkChannel ch) => ch.channelName));
				logger.Warning(text, array);
				foreach (NetworkChannel networkChannel in clientChannels)
				{
					networkChannel.Connected = false;
					networkChannel.channelId = 0;
				}
			}
			int i;
			for (i = 0; i < CS$<>8__locals1.serverPacket.ChannelUdpNamesCount; i = num + 1)
			{
				UdpNetworkChannel channel2 = clientUdpChannels.FirstOrDefault((UdpNetworkChannel ch) => ch.ChannelName == CS$<>8__locals1.serverPacket.ChannelUdpNames[i]);
				if (channel2 != null)
				{
					clientUdpChannels.Remove(channel2);
					channel2.channelId = CS$<>8__locals1.serverPacket.ChannelUdpIds[i];
					channel2.Connected = true;
					matchedchannelsUdp[CS$<>8__locals1.serverPacket.ChannelUdpIds[i]] = channel2;
				}
				else
				{
					this.game.Logger.Warning("Improperly configured mod. Server sends me udp channel name {0}, but no client side mod registered it.", new object[] { CS$<>8__locals1.serverPacket.ChannelUdpNames[i] });
				}
				num = i;
			}
			if (clientUdpChannels.Count > 0)
			{
				ILogger logger2 = this.game.Logger;
				string text2 = "Client registered {0} network udp channels ({1}) the server does not know about, may cause issues.";
				object[] array2 = new object[2];
				array2[0] = clientUdpChannels.Count;
				array2[1] = string.Join(", ", clientUdpChannels.Select((UdpNetworkChannel ch) => ch.channelName));
				logger2.Warning(text2, array2);
				foreach (UdpNetworkChannel udpNetworkChannel in clientUdpChannels)
				{
					udpNetworkChannel.Connected = false;
					udpNetworkChannel.channelId = 0;
				}
			}
			this.channels = matchedchannels;
			this.channelsUdp = matchedchannelsUdp;
			this.serverChannelsReceived = true;
			while (this.earlyPackets.Count > 0)
			{
				this.HandleCustomPacket(this.earlyPackets.Dequeue());
			}
			while (this.earlyUdpPackets.Count > 0)
			{
				this.HandleCustomUdpPacket(this.earlyUdpPackets.Dequeue());
			}
		}

		private void HandleCustomPacket(Packet_Server packet)
		{
			if (!this.serverChannelsReceived)
			{
				this.earlyPackets.Enqueue(packet);
				return;
			}
			Packet_CustomPacket p = packet.CustomPacket;
			NetworkChannel channel;
			if (this.channels.TryGetValue(p.ChannelId, out channel))
			{
				channel.OnPacket(p);
			}
		}

		private void HandleCustomUdpPacket(Packet_CustomPacket packet)
		{
			if (!this.serverChannelsReceived)
			{
				this.earlyUdpPackets.Enqueue(packet);
				return;
			}
			UdpNetworkChannel channel;
			if (this.channelsUdp.TryGetValue(packet.ChannelId, out channel))
			{
				channel.OnPacket(packet);
			}
		}

		public IClientNetworkChannel RegisterChannel(string channelName)
		{
			if (this.serverChannelsReceived)
			{
				throw new Exception("Cannot register network channels at this point. Server already sent his channel list. Make sure to register your network channel early enough, i.e. in StartClientSide().");
			}
			this.nextFreeChannelId++;
			this.clientchannels[this.nextFreeChannelId] = new NetworkChannel(this, this.nextFreeChannelId, channelName);
			return this.clientchannels[this.nextFreeChannelId];
		}

		public IClientNetworkChannel RegisterUdpChannel(string channelName)
		{
			if (this.serverChannelsReceived)
			{
				throw new Exception("Cannot register network udp channels at this point. Server already sent his udp channel list. Make sure to register your network channel early enough, i.e. in StartClientSide().");
			}
			this.nextFreeUdpChannelId++;
			this.clientchannelsUdp[this.nextFreeUdpChannelId] = new UdpNetworkChannel(this, this.nextFreeUdpChannelId, channelName);
			return this.clientchannelsUdp[this.nextFreeUdpChannelId];
		}

		public void SendArbitraryPacket(byte[] data)
		{
			this.game.SendArbitraryPacket(data);
		}

		public void SendBlockEntityPacket(int x, int y, int z, int packetId, byte[] data = null)
		{
			this.game.SendBlockEntityPacket(x, y, z, packetId, data);
		}

		public void SendBlockEntityPacket(BlockPos pos, int packetId, byte[] data = null)
		{
			this.game.SendBlockEntityPacket(pos.X, pos.InternalY, pos.Z, packetId, data);
		}

		public void SendBlockEntityPacket(int x, int y, int z, object packetClient)
		{
			Packet_Client packet = packetClient as Packet_Client;
			byte[] data = this.game.Serialize(packet);
			this.game.SendBlockEntityPacket(x, y, z, packet.Id, data);
		}

		public void SendPacketClient(object packetClient)
		{
			this.game.SendPacketClient(packetClient as Packet_Client);
		}

		public override string Name
		{
			get
			{
				return "networkapi";
			}
		}

		public override EnumClientSystemType GetSystemType()
		{
			return EnumClientSystemType.Misc;
		}

		public void SendHandInteraction(int mouseButton, BlockSelection blockSelection, EntitySelection entitySelection, EnumHandInteract beforeUseType, int handInteract, bool firstEvent, EnumItemUseCancelReason cancelReason)
		{
			this.game.SendHandInteraction(mouseButton, blockSelection, entitySelection, beforeUseType, (EnumHandInteractNw)handInteract, firstEvent, cancelReason);
		}

		public void SendEntityPacket(long entityid, int packetId, byte[] data = null)
		{
			this.game.SendEntityPacket(entityid, packetId, data);
		}

		public void SendPlayerPositionPacket()
		{
			if (double.IsNaN(this.game.EntityPlayer.Pos.X))
			{
				throw new ArgumentException("Position is not a number");
			}
			if (double.IsNaN(this.game.EntityPlayer.Pos.Motion.X))
			{
				throw new ArgumentException("Motion is not a number");
			}
			Packet_EntityPosition packet = ServerPackets.getEntityPositionPacket(this.game.EntityPlayer.Pos, this.game.EntityPlayer, 0);
			Packet_UdpPacket packetUdpClient = new Packet_UdpPacket
			{
				Id = 2,
				EntityPosition = packet
			};
			if (this.game.FallBackToTcp)
			{
				Packet_Client packetClient = new Packet_Client
				{
					Id = 35,
					UdpPacket = packetUdpClient
				};
				this.game.SendPacketClient(packetClient);
				return;
			}
			this.game.UdpNetClient.Send(packetUdpClient);
		}

		public void SendPlayerMountPositionPacket(Entity mount)
		{
			if (double.IsNaN(mount.Pos.X))
			{
				throw new ArgumentException("Mount Position is not a number");
			}
			if (double.IsNaN(mount.Pos.Motion.X))
			{
				throw new ArgumentException("Mount Motion is not a number");
			}
			Packet_EntityPosition packet = ServerPackets.getEntityPositionPacket(mount.Pos, mount, 0);
			Packet_UdpPacket packetUdpClient = new Packet_UdpPacket
			{
				Id = 3,
				EntityPosition = packet
			};
			if (mount.AnimManager != null)
			{
				Packet_CustomPacket packet_CustomPacket = (packetUdpClient.ChannelPacket = new Packet_CustomPacket());
				string gaitCode = mount.WatchedAttributes.GetString("currentgait", null);
				MountAnimationPacket combiPacket = new MountAnimationPacket
				{
					gaitCode = gaitCode,
					animPacket = new AnimationPacket(mount)
				};
				if (this.mountStream == null)
				{
					this.mountStream = new FastMemoryStream(64);
				}
				this.mountStream.Reset();
				Serializer.Serialize<MountAnimationPacket>(this.mountStream, combiPacket);
				packet_CustomPacket.SetData(this.mountStream.ToArray());
			}
			if (this.game.FallBackToTcp)
			{
				Packet_Client packetClient = new Packet_Client
				{
					Id = 35,
					UdpPacket = packetUdpClient
				};
				this.game.SendPacketClient(packetClient);
				return;
			}
			this.game.UdpNetClient.Send(packetUdpClient);
		}

		public void SendEntityPacket(long entityid, object packetClient)
		{
			Packet_Client packet = packetClient as Packet_Client;
			byte[] data = this.game.Serialize(packet);
			this.game.SendEntityPacket(entityid, packet.Id, data);
		}

		public void SendEntityPacketWithOffset(long entityid, int packetIdOffset, object packetClient)
		{
			Packet_Client packet = packetClient as Packet_Client;
			byte[] data = this.game.Serialize(packet);
			this.game.SendEntityPacket(entityid, packet.Id + packetIdOffset, data);
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
			return (this.serverChannelsReceived ? this.channels : this.clientchannels).FirstOrDefault((KeyValuePair<int, NetworkChannel> pair) => pair.Value.ChannelName == channelName).Value;
		}

		IClientNetworkChannel IClientNetworkAPI.GetChannel(string channelName)
		{
			return (this.serverChannelsReceived ? this.channels : this.clientchannels).FirstOrDefault((KeyValuePair<int, NetworkChannel> pair) => pair.Value.ChannelName == channelName).Value;
		}

		public INetworkChannel GetUdpChannel(string channelName)
		{
			return (this.serverChannelsReceived ? this.channelsUdp : this.clientchannelsUdp).FirstOrDefault((KeyValuePair<int, UdpNetworkChannel> pair) => pair.Value.ChannelName == channelName).Value;
		}

		IClientNetworkChannel IClientNetworkAPI.GetUdpChannel(string channelName)
		{
			return (this.serverChannelsReceived ? this.channelsUdp : this.clientchannelsUdp).FirstOrDefault((KeyValuePair<int, UdpNetworkChannel> pair) => pair.Value.ChannelName == channelName).Value;
		}

		public void SendBlockEntityPacket<T>(BlockPos pos, int packetId, T data = default(T))
		{
			this.game.SendBlockEntityPacket(pos.X, pos.InternalY, pos.Z, packetId, SerializerUtil.Serialize<T>(data));
		}

		private Dictionary<int, NetworkChannel> clientchannels = new Dictionary<int, NetworkChannel>();

		private Dictionary<int, UdpNetworkChannel> clientchannelsUdp = new Dictionary<int, UdpNetworkChannel>();

		private Dictionary<int, NetworkChannel> channels = new Dictionary<int, NetworkChannel>();

		private Dictionary<int, UdpNetworkChannel> channelsUdp = new Dictionary<int, UdpNetworkChannel>();

		private int nextFreeChannelId;

		private int nextFreeUdpChannelId;

		private bool serverChannelsReceived;

		private Queue<Packet_Server> earlyPackets = new Queue<Packet_Server>();

		private Queue<Packet_CustomPacket> earlyUdpPackets = new Queue<Packet_CustomPacket>();

		private FastMemoryStream mountStream;
	}
}
