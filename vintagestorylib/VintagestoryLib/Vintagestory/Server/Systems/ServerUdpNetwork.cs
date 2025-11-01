using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.Common.Network.Packets;

namespace Vintagestory.Server.Systems
{
	public class ServerUdpNetwork : ServerSystem
	{
		public ServerUdpNetwork(ServerMain server)
			: base(server)
		{
			server.RegisterGameTickListener(new Action<float>(this.ServerTickUdp), 15, 0);
			this.physicsManager = new PhysicsManager(server.api, this);
			server.PacketHandlers[35] = new ClientPacketHandler<Packet_Client, ConnectedClient>(this.EnqueueUdpPacket);
		}

		private void EnqueueUdpPacket(Packet_Client packet, ConnectedClient player)
		{
			UdpPacket udpPacket = new UdpPacket
			{
				Packet = packet.UdpPacket,
				Client = player
			};
			this.server.UdpSockets[1].EnqueuePacket(udpPacket);
		}

		private void ServerTickUdp(float obj)
		{
			foreach (UNetServer udpSocket in this.server.UdpSockets)
			{
				UdpPacket[] packets = ((udpSocket != null) ? udpSocket.ReadMessage() : null);
				if (packets != null)
				{
					foreach (UdpPacket packet in packets)
					{
						this.server.TotalReceivedBytesUdp += (long)packet.Packet.Length;
						switch (packet.Packet.Id)
						{
						case 1:
							this.HandleConnectionRequest(packet, udpSocket);
							break;
						case 2:
							this.HandlePlayerPosition(packet.Packet.EntityPosition, packet.Client.Player);
							break;
						case 3:
							this.HandleMountPosition(packet.Packet.EntityPosition, packet.Client.Player, packet.Packet.ChannelPacket);
							break;
						case 6:
							this.server.HandleCustomUdpPackets(packet.Packet.ChannelPacket, packet.Client.Player);
							break;
						case 7:
							packet.Client.Ping.OnReceiveUdp(this.server.ElapsedMilliseconds);
							break;
						}
					}
				}
			}
		}

		public override void OnBeginInitialization()
		{
			this.physicsManager.Init();
		}

		public override void OnPlayerDisconnect(ServerPlayer player)
		{
			KeyValuePair<string, ConnectedClient> cc = this.connectingClients.FirstOrDefault(delegate(KeyValuePair<string, ConnectedClient> c)
			{
				ServerPlayer player2 = c.Value.Player;
				return player2 != null && player2.Equals(player);
			});
			if (cc.Key != null)
			{
				this.connectingClients.Remove(cc.Key);
			}
			if (player.client.IsSinglePlayerClient)
			{
				this.server.UdpSockets[0].Remove(player);
			}
			else
			{
				this.server.UdpSockets[1].Remove(player);
			}
			this.server.api.Logger.Notification("UDP: client disconnected " + player.PlayerName);
		}

		private void HandleConnectionRequest(UdpPacket udpPacket, UNetServer uNetServer)
		{
			ServerUdpNetwork.<>c__DisplayClass8_0 CS$<>8__locals1 = new ServerUdpNetwork.<>c__DisplayClass8_0();
			CS$<>8__locals1.<>4__this = this;
			try
			{
				Packet_ConnectionPacket packet = udpPacket.Packet.ConnectionPacket;
				if (packet != null)
				{
					this.connectingClients.TryGetValue((packet != null) ? packet.LoginToken : null, out CS$<>8__locals1.client);
					if (CS$<>8__locals1.client != null && !uNetServer.EndPoints.ContainsKey(udpPacket.EndPoint))
					{
						this.connectingClients.Remove(packet.LoginToken);
						uNetServer.Add(udpPacket.EndPoint, CS$<>8__locals1.client.Id);
						CS$<>8__locals1.client.ServerDidReceiveUdp = true;
						ILogger logger = this.server.api.Logger;
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(28, 2);
						defaultInterpolatedStringHandler.AppendLiteral("UDP: Client ");
						defaultInterpolatedStringHandler.AppendFormatted<int>(CS$<>8__locals1.client.Id);
						defaultInterpolatedStringHandler.AppendLiteral(" connected via: ");
						defaultInterpolatedStringHandler.AppendFormatted<IPEndPoint>(udpPacket.EndPoint);
						logger.Notification(defaultInterpolatedStringHandler.ToStringAndClear());
						Packet_Server didRecive = new Packet_Server
						{
							Id = 81
						};
						this.server.SendPacket(CS$<>8__locals1.client.Id, didRecive);
						CS$<>8__locals1.clientLoginToken = CS$<>8__locals1.client.LoginToken;
						if (!CS$<>8__locals1.client.IsSinglePlayerClient)
						{
							Task.Run(delegate
							{
								ServerUdpNetwork.<>c__DisplayClass8_0.<<HandleConnectionRequest>b__0>d <<HandleConnectionRequest>b__0>d;
								<<HandleConnectionRequest>b__0>d.<>t__builder = AsyncTaskMethodBuilder.Create();
								<<HandleConnectionRequest>b__0>d.<>4__this = CS$<>8__locals1;
								<<HandleConnectionRequest>b__0>d.<>1__state = -1;
								<<HandleConnectionRequest>b__0>d.<>t__builder.Start<ServerUdpNetwork.<>c__DisplayClass8_0.<<HandleConnectionRequest>b__0>d>(ref <<HandleConnectionRequest>b__0>d);
								return <<HandleConnectionRequest>b__0>d.<>t__builder.Task;
							});
						}
					}
				}
			}
			catch (Exception e)
			{
				ILogger logger2 = this.server.api.Logger;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(38, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Error when connecting UDP client from ");
				defaultInterpolatedStringHandler.AppendFormatted<IPEndPoint>(udpPacket.EndPoint);
				logger2.Warning(defaultInterpolatedStringHandler.ToStringAndClear());
				this.server.api.Logger.Warning(e);
			}
		}

		public void HandlePlayerPosition(Packet_EntityPosition packet, ServerPlayer player)
		{
			if (packet == null)
			{
				return;
			}
			EntityPlayer entity = player.Entity;
			int version = entity.WatchedAttributes.GetInt("positionVersionNumber", 0);
			if (packet.PositionVersion < version)
			{
				return;
			}
			player.LastReceivedClientPosition = this.server.ElapsedMilliseconds;
			int currentTick = entity.Attributes.GetInt("tick", 0);
			currentTick++;
			entity.Attributes.SetInt("tick", currentTick);
			entity.ServerPos.SetFromPacket(packet, entity);
			entity.Pos.SetFromPacket(packet, entity);
			foreach (EntityBehavior entityBehavior in entity.SidedProperties.Behaviors)
			{
				IRemotePhysics remote = entityBehavior as IRemotePhysics;
				if (remote != null)
				{
					remote.OnReceivedClientPos(version);
					break;
				}
			}
			Packet_EntityPosition entityPositionPacket = ServerPackets.getEntityPositionPacket(entity.ServerPos, entity, currentTick);
			entityPositionPacket.BodyYaw = CollectibleNet.SerializeFloatPrecise(entity.BodyYawServer);
			Packet_UdpPacket packetBytesUdp = new Packet_UdpPacket
			{
				Id = 5,
				EntityPosition = entityPositionPacket
			};
			entity.IsTeleport = false;
			AnimationPacket animationPacket = null;
			IAnimationManager animManager = entity.AnimManager;
			bool animationsDirty = animManager != null && animManager.AnimationsDirty;
			if (animationsDirty)
			{
				animationPacket = new AnimationPacket(entity);
				entity.AnimManager.AnimationsDirty = false;
			}
			foreach (ConnectedClient client in this.server.Clients.Values)
			{
				ServerPlayer sp = client.Player;
				if (sp != null && sp != player && client.TrackedEntities.Contains(entity.EntityId))
				{
					this.ImmediateUdpQueue.QueuePacket(client, packetBytesUdp);
					if (animationsDirty)
					{
						this.physicsManager.AnimationsAndTagsChannel.SendPacket<AnimationPacket>(animationPacket, new IServerPlayer[] { sp });
					}
				}
			}
		}

		public void HandleMountPosition(Packet_EntityPosition packet, ServerPlayer player, Packet_CustomPacket clientAnimationsAndGaitPacket)
		{
			if (packet == null)
			{
				return;
			}
			Entity entity = this.server.api.World.GetEntityById(packet.EntityId);
			IMountable mount = ((entity != null) ? entity.GetInterface<IMountable>() : null);
			if (mount == null || !mount.IsMountedBy(player.Entity))
			{
				return;
			}
			int version = entity.WatchedAttributes.GetInt("positionVersionNumber", 0);
			if (packet.PositionVersion < version)
			{
				return;
			}
			int currentTick = entity.Attributes.GetInt("tick", 0);
			currentTick++;
			entity.Attributes.SetInt("tick", currentTick);
			entity.ServerPos.SetFromPacket(packet, entity);
			entity.Pos.SetFromPacket(packet, entity);
			EntityControls entityControls;
			if (entity.SidedProperties == null)
			{
				entityControls = null;
			}
			else
			{
				IMountable @interface = entity.GetInterface<IMountable>();
				entityControls = ((@interface != null) ? @interface.ControllingControls : null);
			}
			EntityControls seatControls = entityControls;
			if (seatControls != null)
			{
				seatControls.FromInt(packet.MountControls);
			}
			foreach (EntityBehavior entityBehavior in entity.SidedProperties.Behaviors)
			{
				IRemotePhysics remote = entityBehavior as IRemotePhysics;
				if (remote != null)
				{
					remote.OnReceivedClientPos(version);
					break;
				}
			}
			Packet_EntityPosition entityPositionPacket = ServerPackets.getEntityPositionPacket(entity.ServerPos, entity, currentTick);
			Packet_UdpPacket packetBytesUdp = new Packet_UdpPacket
			{
				Id = 5,
				EntityPosition = entityPositionPacket
			};
			entity.IsTeleport = false;
			AnimationPacket animationPacket = null;
			if (clientAnimationsAndGaitPacket != null)
			{
				entity.AnimManager.AnimationsDirty = false;
				MountAnimationPacket combiPacket = null;
				using (MemoryStream ms = new MemoryStream(clientAnimationsAndGaitPacket.Data))
				{
					combiPacket = Serializer.Deserialize<MountAnimationPacket>(ms);
				}
				if (combiPacket != null)
				{
					string gaitCode = combiPacket.gaitCode;
					animationPacket = combiPacket.animPacket;
					this.server.api.Event.TriggerMountGaitReceived(entity, gaitCode);
				}
			}
			ICollection<ConnectedClient> values = this.server.Clients.Values;
			List<ServerPlayer> otherPlayersInRange = ((values.Count <= 1 || animationPacket == null) ? null : new List<ServerPlayer>());
			foreach (ConnectedClient client in values)
			{
				if (client.Player != null && client.TrackedEntities.Contains(entity.EntityId))
				{
					this.ImmediateUdpQueue.QueuePacket(client, packetBytesUdp);
					if (otherPlayersInRange != null && client.Player != player)
					{
						otherPlayersInRange.Add(client.Player);
					}
				}
			}
			if (otherPlayersInRange != null && otherPlayersInRange.Count > 0)
			{
				IServerNetworkChannel animationsAndTagsChannel = this.physicsManager.AnimationsAndTagsChannel;
				AnimationPacket animationPacket2 = animationPacket;
				IServerPlayer[] array = otherPlayersInRange.ToArray();
				animationsAndTagsChannel.SendPacket<AnimationPacket>(animationPacket2, array);
			}
		}

		public void SendPacket_Threadsafe(ConnectedClient client, Packet_BulkEntityPosition contentPacket)
		{
			Packet_UdpPacket packet = new Packet_UdpPacket
			{
				Id = 4,
				BulkPositions = contentPacket
			};
			this.ImmediateUdpQueue.QueuePacket(client, packet);
		}

		public void SendPacket_Threadsafe(ConnectedClient client, Packet_CustomPacket prePacked)
		{
			Packet_UdpPacket udpPacket = new Packet_UdpPacket
			{
				Id = 6,
				ChannelPacket = prePacked
			};
			this.ImmediateUdpQueue.QueuePacket(client, udpPacket);
		}

		public void SendPacket_Threadsafe(ConnectedClient client, Packet_UdpPacket prePacked)
		{
			this.ImmediateUdpQueue.QueuePacket(client, prePacked);
		}

		public readonly Dictionary<string, ConnectedClient> connectingClients = new Dictionary<string, ConnectedClient>();

		public PhysicsManager physicsManager;

		internal ServerUdpQueue ImmediateUdpQueue;
	}
}
