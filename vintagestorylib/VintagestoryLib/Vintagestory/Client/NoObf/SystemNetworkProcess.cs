using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.Common;
using Vintagestory.Common.Network.Packets;
using Vintagestory.Server.Systems;

namespace Vintagestory.Client.NoObf
{
	public class SystemNetworkProcess : ClientSystem
	{
		public override string Name
		{
			get
			{
				return "nwp";
			}
		}

		public int TotalBytesReceivedAndReceiving
		{
			get
			{
				return this.totalByteCount + this.game.MainNetClient.CurrentlyReceivingBytes;
			}
		}

		static SystemNetworkProcess()
		{
			FieldInfo[] infos = typeof(Packet_ServerIdEnum).GetFields();
			for (int i = 0; i < infos.Length; i++)
			{
				if ((infos[i].Attributes & FieldAttributes.Literal) != FieldAttributes.PrivateScope && !(infos[i].FieldType != typeof(int)))
				{
					SystemNetworkProcess.ServerPacketNames[(int)infos[i].GetValue(null)] = infos[i].Name;
				}
			}
		}

		public SystemNetworkProcess(ClientMain game)
			: base(game)
		{
			this.totalByteCount = 0;
			game.RegisterGameTickListener(new Action<float>(this.UpdatePacketCount), 1000, 0);
			game.RegisterGameTickListener(new Action<float>(this.ClientUdpTick), 15, 0);
			game.PacketHandlers[78] = new ServerPacketHandler<Packet_Server>(this.HandleRequestPositionTcp);
			game.PacketHandlers[79] = new ServerPacketHandler<Packet_Server>(this.EnqueueUdpPacket);
			game.PacketHandlers[80] = new ServerPacketHandler<Packet_Server>(this.HandleEntitySpawnPosition);
			game.PacketHandlers[81] = new ServerPacketHandler<Packet_Server>(this.HandleDidReceiveUdp);
			game.api.ChatCommands.Create("netbenchmark").WithDescription("Toggles network benchmarking").HandleWith(new OnCommandDelegate(this.CmdBenchmark));
			this.clientNetworkChannel = game.api.Network.RegisterChannel("EntityAnims");
			this.clientNetworkChannel.RegisterMessageType<AnimationPacket>().RegisterMessageType<BulkAnimationPacket>().RegisterMessageType<EntityTagPacket>()
				.SetMessageHandler<AnimationPacket>(new NetworkServerMessageHandler<AnimationPacket>(this.HandleAnimationPacket))
				.SetMessageHandler<BulkAnimationPacket>(new NetworkServerMessageHandler<BulkAnimationPacket>(this.HandleBulkAnimationPacket))
				.SetMessageHandler<EntityTagPacket>(new NetworkServerMessageHandler<EntityTagPacket>(this.HandleTagPacket));
			this.packetDebug = ClientSettings.Inst.Bool["packetDebug"];
		}

		private void UdpConnectionRequestFromServer()
		{
			if (!this.DidReceiveUdp)
			{
				this.game.Logger.Notification("UDP: Server send UDP connect");
				this.DidReceiveUdp = true;
			}
		}

		private void HandleDidReceiveUdp(Packet_Server packet)
		{
			this.game.UdpTryConnect = false;
			this.game.Logger.Notification("UDP: Server send DidReceiveUdp");
			Task.Run(async delegate
			{
				for (int i = 0; i < 20; i++)
				{
					await Task.Delay(500);
					if (this.game.disposed)
					{
						return;
					}
					if (this.DidReceiveUdp || this.game.FallBackToTcp)
					{
						break;
					}
				}
				if (!this.DidReceiveUdp)
				{
					Packet_Client packet_Client = new Packet_Client();
					packet_Client.Id = 34;
					this.game.Logger.Notification("UDP: Server did not receive any UDP packets and requests position updates over TCP");
					this.game.SendPacketClient(packet_Client);
				}
				else
				{
					this.game.Logger.Notification("UDP: Client can receive UDP packets");
				}
			});
		}

		private void HandleEntitySpawnPosition(Packet_Server packet)
		{
			this.HandleSinglePacket(packet.EntityPosition);
		}

		private void EnqueueUdpPacket(Packet_Server packet)
		{
			this.game.UdpNetClient.EnqueuePacket(packet.UdpPacket);
		}

		private void HandleRequestPositionTcp(Packet_Server packet)
		{
			this.game.Logger.Notification("UDP: Server requested to fallback to use only TCP");
			this.game.FallBackToTcp = true;
			this.game.UdpTryConnect = false;
		}

		public void StartUdpConnectRequest(string token)
		{
			SystemNetworkProcess.<>c__DisplayClass25_0 CS$<>8__locals1 = new SystemNetworkProcess.<>c__DisplayClass25_0();
			CS$<>8__locals1.token = token;
			CS$<>8__locals1.<>4__this = this;
			if (ClientSettings.ForceUdpOverTcp && !this.game.IsSingleplayer)
			{
				this.game.Logger.Notification("UDP: is disabled in clientsettings: forceUdpOverTcp , using only TCP now");
				Packet_Client packetClient = new Packet_Client
				{
					Id = 34
				};
				this.game.SendPacketClient(packetClient);
				return;
			}
			this.game.UdpTryConnect = true;
			this.game.UdpNetClient.DidReceiveUdpConnectionRequest += this.UdpConnectionRequestFromServer;
			Task.Run(delegate
			{
				SystemNetworkProcess.<>c__DisplayClass25_0.<<StartUdpConnectRequest>b__0>d <<StartUdpConnectRequest>b__0>d;
				<<StartUdpConnectRequest>b__0>d.<>t__builder = AsyncTaskMethodBuilder.Create();
				<<StartUdpConnectRequest>b__0>d.<>4__this = CS$<>8__locals1;
				<<StartUdpConnectRequest>b__0>d.<>1__state = -1;
				<<StartUdpConnectRequest>b__0>d.<>t__builder.Start<SystemNetworkProcess.<>c__DisplayClass25_0.<<StartUdpConnectRequest>b__0>d>(ref <<StartUdpConnectRequest>b__0>d);
				return <<StartUdpConnectRequest>b__0>d.<>t__builder.Task;
			});
			if (!this.game.IsSingleplayer)
			{
				this.game.Logger.Notification("UDP: set up 10 s keep alive to server");
				Task.Run(delegate
				{
					SystemNetworkProcess.<>c__DisplayClass25_0.<<StartUdpConnectRequest>b__1>d <<StartUdpConnectRequest>b__1>d;
					<<StartUdpConnectRequest>b__1>d.<>t__builder = AsyncTaskMethodBuilder.Create();
					<<StartUdpConnectRequest>b__1>d.<>4__this = CS$<>8__locals1;
					<<StartUdpConnectRequest>b__1>d.<>1__state = -1;
					<<StartUdpConnectRequest>b__1>d.<>t__builder.Start<SystemNetworkProcess.<>c__DisplayClass25_0.<<StartUdpConnectRequest>b__1>d>(ref <<StartUdpConnectRequest>b__1>d);
					return <<StartUdpConnectRequest>b__1>d.<>t__builder.Task;
				});
			}
		}

		public void HandleTagPacket(EntityTagPacket packet)
		{
			Entity entity = this.game.GetEntityById(packet.EntityId);
			if (entity == null)
			{
				return;
			}
			entity.Tags = new EntityTagArray((ulong)packet.TagsBitmask1, (ulong)packet.TagsBitmask2);
		}

		public void HandleAnimationPacket(AnimationPacket packet)
		{
			Entity entity = this.game.GetEntityById(packet.entityId);
			if (entity == null)
			{
				return;
			}
			EntityProperties properties = entity.Properties;
			bool flag;
			if (properties == null)
			{
				flag = null != null;
			}
			else
			{
				EntityClientProperties client = properties.Client;
				if (client == null)
				{
					flag = null != null;
				}
				else
				{
					Shape loadedShapeForEntity = client.LoadedShapeForEntity;
					flag = ((loadedShapeForEntity != null) ? loadedShapeForEntity.Animations : null) != null;
				}
			}
			if (flag)
			{
				float[] speeds = new float[packet.activeAnimationSpeedsCount];
				for (int x = 0; x < speeds.Length; x++)
				{
					speeds[x] = CollectibleNet.DeserializeFloatPrecise(packet.activeAnimationSpeeds[x]);
				}
				entity.OnReceivedServerAnimations(packet.activeAnimations, packet.activeAnimationsCount, speeds);
			}
		}

		public void HandleBulkAnimationPacket(BulkAnimationPacket bulkPacket)
		{
			if (bulkPacket.Packets == null)
			{
				return;
			}
			for (int i = 0; i < bulkPacket.Packets.Length; i++)
			{
				AnimationPacket packet = bulkPacket.Packets[i];
				Entity entity = this.game.GetEntityById(packet.entityId);
				if (entity != null)
				{
					EntityProperties properties = entity.Properties;
					bool flag;
					if (properties == null)
					{
						flag = null != null;
					}
					else
					{
						EntityClientProperties client = properties.Client;
						if (client == null)
						{
							flag = null != null;
						}
						else
						{
							Shape loadedShapeForEntity = client.LoadedShapeForEntity;
							flag = ((loadedShapeForEntity != null) ? loadedShapeForEntity.Animations : null) != null;
						}
					}
					if (flag)
					{
						float[] speeds = new float[packet.activeAnimationSpeedsCount];
						for (int x = 0; x < speeds.Length; x++)
						{
							speeds[x] = CollectibleNet.DeserializeFloatPrecise(packet.activeAnimationSpeeds[x]);
						}
						entity.OnReceivedServerAnimations(packet.activeAnimations, packet.activeAnimationsCount, speeds);
					}
				}
			}
		}

		private void ClientUdpTick(float obj)
		{
			if (this.game.UdpNetClient == null)
			{
				return;
			}
			IEnumerable<Packet_UdpPacket> packets = this.game.UdpNetClient.ReadMessage();
			if (packets == null)
			{
				return;
			}
			using (IEnumerator<Packet_UdpPacket> enumerator = packets.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					Packet_UdpPacket packet = enumerator.Current;
					int udpByteCount = packet.Length;
					if (this.packetDebug)
					{
						ScreenManager.EnqueueMainThreadTask(delegate
						{
							this.game.Logger.VerboseDebug("Received UDP packet id {0}, dataLength {1}", new object[] { packet.Id, udpByteCount });
						});
					}
					this.UpdateUdpStatsAndBenchmark(packet, udpByteCount);
					switch (packet.Id)
					{
					case 4:
						this.HandleBulkPacket(packet.BulkPositions);
						break;
					case 5:
						this.HandleSinglePacket(packet.EntityPosition);
						break;
					case 6:
						this.game.HandleCustomUdpPackets(packet.ChannelPacket);
						break;
					}
				}
			}
		}

		private void UpdateUdpStatsAndBenchmark(Packet_UdpPacket packet, int udpByteCount)
		{
			if (this.doBenchmark)
			{
				int benchmark;
				if (this.udpPacketBenchmark.TryGetValue(packet.Id, out benchmark))
				{
					this.udpPacketBenchmark[packet.Id] = benchmark + udpByteCount;
				}
				else
				{
					this.udpPacketBenchmark[packet.Id] = udpByteCount;
				}
			}
			this.totalUdpByteCount += udpByteCount;
			this.deltaUdpByteCount += udpByteCount;
		}

		private TextCommandResult CmdBenchmark(TextCommandCallingArgs textCommandCallingArgs)
		{
			this.doBenchmark = !this.doBenchmark;
			if (!this.doBenchmark)
			{
				StringBuilder str = new StringBuilder();
				foreach (KeyValuePair<int, int> val in this.packetBenchmark)
				{
					string packetName;
					SystemNetworkProcess.ServerPacketNames.TryGetValue(val.Key, out packetName);
					str.AppendLine(packetName + ": " + ((val.Value > 9999) ? (((float)val.Value / 1024f).ToString("#.#") + "kb") : (val.Value.ToString() + "b")));
				}
				foreach (KeyValuePair<int, int> val2 in this.udpPacketBenchmark)
				{
					string packetName2 = val2.Key.ToString();
					str.AppendLine(packetName2 + ": " + ((val2.Value > 9999) ? (((float)val2.Value / 1024f).ToString("#.#") + "kb") : (val2.Value.ToString() + "b")));
				}
				return TextCommandResult.Success(str.ToString(), null);
			}
			this.packetBenchmark.Clear();
			return TextCommandResult.Success("Benchmarking started. Stop it after a while to get results.", null);
		}

		private void UpdatePacketCount(float dt)
		{
			if (this.game.extendedDebugInfo)
			{
				string deltaByte = ((this.deltaByteCount > 1024) ? (((float)this.deltaByteCount / 1024f).ToString("0.0") + "kb/s") : (this.deltaByteCount.ToString() + "b/s"));
				string deltaUdpByte = ((this.deltaUdpByteCount > 1024) ? (((float)this.deltaUdpByteCount / 1024f).ToString("0.0") + "kb/s") : (this.deltaUdpByteCount.ToString() + "b/s"));
				this.game.DebugScreenInfo["incomingbytes"] = string.Concat(new string[]
				{
					"Network TCP/UDP: ",
					((float)this.totalByteCount / 1024f).ToString("#.#", GlobalConstants.DefaultCultureInfo),
					" kb, ",
					deltaByte,
					" / ",
					((float)this.totalUdpByteCount / 1024f).ToString("#.#", GlobalConstants.DefaultCultureInfo),
					" kb, ",
					deltaUdpByte
				});
			}
			else
			{
				this.game.DebugScreenInfo["incomingbytes"] = "";
			}
			this.deltaByteCount = 0;
			this.deltaUdpByteCount = 0;
		}

		public override void OnSeperateThreadGameTick(float dt)
		{
			if (this.game.MainNetClient == null)
			{
				return;
			}
			for (;;)
			{
				NetIncomingMessage msg = this.game.MainNetClient.ReadMessage();
				if (msg == null)
				{
					break;
				}
				this.totalByteCount += msg.originalMessageLength;
				this.deltaByteCount += msg.originalMessageLength;
				this.TryReadPacket(msg.message, msg.messageLength);
			}
		}

		public void TryReadPacket(byte[] data, int dataLength)
		{
			Packet_Server packet = new Packet_Server();
			Packet_ServerSerializer.DeserializeBuffer(data, dataLength, packet);
			if (this.game.disposed)
			{
				return;
			}
			if (this.packetDebug)
			{
				ScreenManager.EnqueueMainThreadTask(delegate
				{
					this.game.Logger.VerboseDebug("Received packet id {0}, dataLength {1}", new object[] { packet.Id, dataLength });
				});
			}
			if (this.doBenchmark)
			{
				int benchmark;
				if (this.packetBenchmark.TryGetValue(packet.Id, out benchmark))
				{
					this.packetBenchmark[packet.Id] = benchmark + data.Length;
				}
				else
				{
					this.packetBenchmark[packet.Id] = data.Length;
				}
			}
			if (this.ProcessInBackground(packet))
			{
				return;
			}
			ProcessPacketTask task = new ProcessPacketTask
			{
				game = this.game,
				packet = packet
			};
			if (packet.Id == 73)
			{
				this.game.ServerReady = true;
				if (this.game.IsSingleplayer && this.game.GameLaunchTasks.Count > 0)
				{
					this.game.Logger.VerboseDebug("ServerIdentification packet received; will wait until block tesselation is complete to handle it");
				}
			}
			if (false)
			{
				string taskId = "readpacket" + packet.Id.ToString();
				Action <>9__4;
				Action <>9__3;
				Action <>9__2;
				this.game.EnqueueMainThreadTask(delegate
				{
					ClientMain game = this.game;
					Action action;
					if ((action = <>9__2) == null)
					{
						action = (<>9__2 = delegate
						{
							ClientMain game2 = this.game;
							Action action2;
							if ((action2 = <>9__3) == null)
							{
								action2 = (<>9__3 = delegate
								{
									ClientMain game3 = this.game;
									Action action3;
									if ((action3 = <>9__4) == null)
									{
										action3 = (<>9__4 = delegate
										{
											this.game.EnqueueMainThreadTask(new Action(task.Run), taskId);
										});
									}
									game3.EnqueueMainThreadTask(action3, taskId);
								});
							}
							game2.EnqueueMainThreadTask(action2, taskId);
						});
					}
					game.EnqueueMainThreadTask(action, taskId);
				}, taskId);
			}
			else
			{
				this.game.EnqueueMainThreadTask(new Action(task.Run), "readpacket" + packet.Id.ToString());
			}
			this.game.LastReceivedMilliseconds = this.game.Platform.EllapsedMs;
		}

		public override int SeperateThreadTickIntervalMs()
		{
			return 1;
		}

		private bool ProcessInBackground(Packet_Server packet)
		{
			int id = packet.Id;
			if (id <= 21)
			{
				if (id == 4)
				{
					this.game.WorldMap.ServerChunkSize = packet.LevelInitialize.ServerChunkSize;
					this.game.WorldMap.MapChunkSize = packet.LevelInitialize.ServerMapChunkSize;
					this.game.WorldMap.regionSize = packet.LevelInitialize.ServerMapRegionSize;
					this.game.WorldMap.MaxViewDistance = packet.LevelInitialize.MaxViewDistance;
					return false;
				}
				if (id != 10)
				{
					switch (id)
					{
					case 17:
					{
						long index2d = this.game.WorldMap.MapChunkIndex2D(packet.MapChunk.ChunkX, packet.MapChunk.ChunkZ);
						ClientMapChunk mapchunk;
						this.game.WorldMap.MapChunks.TryGetValue(index2d, out mapchunk);
						if (mapchunk == null)
						{
							mapchunk = new ClientMapChunk();
						}
						mapchunk.UpdateFromPacket(packet.MapChunk);
						this.game.WorldMap.MapChunks[index2d] = mapchunk;
						return true;
					}
					case 19:
					{
						ServerPacketHandler<Packet_Server> serverPacketHandler = this.game.PacketHandlers[packet.Id];
						if (serverPacketHandler != null)
						{
							serverPacketHandler(packet);
						}
						return true;
					}
					case 21:
						this.game.EnqueueGameLaunchTask(delegate
						{
							ServerPacketHandler<Packet_Server> serverPacketHandler2 = this.game.PacketHandlers[packet.Id];
							if (serverPacketHandler2 == null)
							{
								return;
							}
							serverPacketHandler2(packet);
						}, "worldmetadatareceived");
						return true;
					}
				}
				else
				{
					if (!this.game.BlocksReceivedAndLoaded)
					{
						for (int i = 0; i < packet.Chunks.ChunksCount; i++)
						{
							this.cheapFixChunkQueue.Push(packet.Chunks.Chunks[i]);
						}
						return true;
					}
					while (this.cheapFixChunkQueue.Count > 0)
					{
						Packet_ServerChunk p = this.cheapFixChunkQueue.Pop();
						this.game.WorldMap.LoadChunkFromPacket(p);
						RuntimeStats.chunksReceived++;
					}
					for (int j = 0; j < packet.Chunks.ChunksCount; j++)
					{
						Packet_ServerChunk p2 = packet.Chunks.Chunks[j];
						this.game.WorldMap.LoadChunkFromPacket(p2);
						RuntimeStats.chunksReceived++;
					}
					return true;
				}
			}
			else if (id <= 47)
			{
				if (id == 42)
				{
					long index2d2 = this.game.WorldMap.MapRegionIndex2D(packet.MapRegion.RegionX, packet.MapRegion.RegionZ);
					ClientMapRegion region;
					this.game.WorldMap.MapRegions.TryGetValue(index2d2, out region);
					if (region == null)
					{
						region = new ClientMapRegion();
					}
					region.UpdateFromPacket(packet);
					this.game.WorldMap.MapRegions[index2d2] = region;
					this.game.EnqueueMainThreadTask(delegate
					{
						this.game.api.eventapi.TriggerMapregionLoaded(new Vec2i(packet.MapRegion.RegionX, packet.MapRegion.RegionZ), region);
					}, "mapregionloadedevent");
					return true;
				}
				if (id == 47)
				{
					if (!this.game.Spawned)
					{
						return true;
					}
					int[] liquidLayer3;
					KeyValuePair<BlockPos[], int[]> pair3 = BlockTypeNet.UnpackSetBlocks(packet.SetBlocks.SetBlocks, out liquidLayer3);
					this.game.EnqueueMainThreadTask(delegate
					{
						BlockPos[] positions2 = pair3.Key;
						int[] blockids2 = pair3.Value;
						for (int m = 0; m < positions2.Length; m++)
						{
							this.game.WorldMap.BulkBlockAccess.SetBlock(blockids2[m], positions2[m]);
							ClientEventManager eventManager = this.game.eventManager;
							if (eventManager != null)
							{
								eventManager.TriggerBlockChanged(this.game, positions2[m], null);
							}
						}
						if (liquidLayer3 != null)
						{
							for (int n = 0; n < positions2.Length; n++)
							{
								this.game.WorldMap.BulkBlockAccess.SetBlock(liquidLayer3[n], positions2[n], 2);
							}
							this.game.WorldMap.BulkBlockAccess.Commit();
						}
					}, "setblocks");
					return true;
				}
			}
			else
			{
				if (id == 63)
				{
					int[] liquidLayer;
					KeyValuePair<BlockPos[], int[]> pair = BlockTypeNet.UnpackSetBlocks(packet.SetBlocks.SetBlocks, out liquidLayer);
					this.game.EnqueueMainThreadTask(delegate
					{
						BlockPos[] positions3 = pair.Key;
						int[] blockids3 = pair.Value;
						if (this.game.BlocksReceivedAndLoaded)
						{
							for (int i2 = 0; i2 < positions3.Length; i2++)
							{
								this.game.WorldMap.NoRelightBulkBlockAccess.SetBlock(blockids3[i2], positions3[i2]);
								ClientEventManager eventManager2 = this.game.eventManager;
								if (eventManager2 != null)
								{
									eventManager2.TriggerBlockChanged(this.game, positions3[i2], null);
								}
							}
						}
						else
						{
							for (int i3 = 0; i3 < positions3.Length; i3++)
							{
								this.game.WorldMap.NoRelightBulkBlockAccess.SetBlock(blockids3[i3], positions3[i3]);
							}
						}
						this.game.WorldMap.NoRelightBulkBlockAccess.Commit();
						if (liquidLayer != null)
						{
							for (int i4 = 0; i4 < positions3.Length; i4++)
							{
								this.game.WorldMap.NoRelightBulkBlockAccess.SetBlock(liquidLayer[i4], positions3[i4], 2);
							}
							this.game.WorldMap.NoRelightBulkBlockAccess.Commit();
						}
					}, "setblocksnorelight");
					return true;
				}
				switch (id)
				{
				case 70:
				{
					while (this.commitingMinimalUpdate)
					{
						Thread.Sleep(5);
					}
					int[] liquidLayer2;
					KeyValuePair<BlockPos[], int[]> pair2 = BlockTypeNet.UnpackSetBlocks(packet.SetBlocks.SetBlocks, out liquidLayer2);
					BlockPos[] positions = pair2.Key;
					int[] blockids = pair2.Value;
					for (int k = 0; k < positions.Length; k++)
					{
						BlockPos pos = positions[k];
						if (this.game.WorldMap.IsPosLoaded(pos))
						{
							this.game.WorldMap.BulkMinimalBlockAccess.SetBlock(blockids[k], pos);
						}
					}
					if (liquidLayer2 != null)
					{
						for (int l = 0; l < positions.Length; l++)
						{
							this.game.WorldMap.BulkMinimalBlockAccess.SetBlock(liquidLayer2[l], positions[l], 2);
						}
					}
					this.commitingMinimalUpdate = true;
					this.game.EnqueueMainThreadTask(delegate
					{
						this.game.WorldMap.BulkMinimalBlockAccess.Commit();
						this.commitingMinimalUpdate = false;
					}, "setblocksminimal");
					return true;
				}
				case 71:
				{
					if (!this.game.Spawned)
					{
						return true;
					}
					long chunkIndex;
					Dictionary<int, Block> newDecors = BlockTypeNet.UnpackSetDecors(packet.SetDecors.SetDecors, this.game.WorldMap.World, out chunkIndex);
					this.game.EnqueueMainThreadTask(delegate
					{
						this.game.WorldMap.BulkBlockAccess.SetDecorsBulk(chunkIndex, newDecors);
					}, "setdecors");
					return true;
				}
				case 74:
					if (!this.game.Spawned)
					{
						return true;
					}
					this.game.EnqueueMainThreadTask(delegate
					{
						this.game.WorldMap.UnloadMapRegion(packet.UnloadMapRegion.RegionX, packet.UnloadMapRegion.RegionZ);
					}, "unloadmapregion");
					return true;
				}
			}
			return false;
		}

		public override EnumClientSystemType GetSystemType()
		{
			return EnumClientSystemType.Misc;
		}

		public void HandleSinglePacket(Packet_EntityPosition packet)
		{
			if (packet == null)
			{
				return;
			}
			Entity entity = this.game.GetEntityById(packet.EntityId);
			if (entity == null)
			{
				return;
			}
			int currentTick = entity.Attributes.GetInt("tick", 0);
			if (currentTick == 0 && this.bulkPositions)
			{
				entity.Attributes.SetInt("tick", packet.Tick);
				return;
			}
			if (packet.Tick <= currentTick)
			{
				return;
			}
			entity.Attributes.SetInt("tickDiff", Math.Min(packet.Tick - currentTick, 5));
			entity.Attributes.SetInt("tick", packet.Tick);
			entity.ServerPos.SetFromPacket(packet, entity);
			EntityAgent agent = entity as EntityAgent;
			if (agent != null)
			{
				agent.Controls.FromInt(packet.Controls & 528);
				if (agent.EntityId != this.game.EntityPlayer.EntityId)
				{
					agent.ServerControls.FromInt(packet.Controls);
				}
			}
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
			entity.OnReceivedServerPos(packet.Teleport);
			entity.Tags = new EntityTagArray((ulong)packet.TagsBitmask1, (ulong)packet.TagsBitmask2);
		}

		public void HandleBulkPacket(Packet_BulkEntityPosition bulkPacket)
		{
			if (bulkPacket.EntityPositions != null)
			{
				this.bulkPositions = true;
				foreach (Packet_EntityPosition packet in bulkPacket.EntityPositions)
				{
					this.HandleSinglePacket(packet);
				}
				this.bulkPositions = false;
			}
		}

		private Stack<Packet_ServerChunk> cheapFixChunkQueue = new Stack<Packet_ServerChunk>();

		private int totalByteCount;

		private int deltaByteCount;

		private int totalUdpByteCount;

		private int deltaUdpByteCount;

		private readonly bool packetDebug;

		private bool doBenchmark;

		private readonly SortedDictionary<int, int> packetBenchmark = new SortedDictionary<int, int>();

		private readonly SortedDictionary<int, int> udpPacketBenchmark = new SortedDictionary<int, int>();

		private bool commitingMinimalUpdate;

		private readonly IClientNetworkChannel clientNetworkChannel;

		public static readonly Dictionary<int, string> ServerPacketNames = new Dictionary<int, string>();

		public bool DidReceiveUdp;

		private bool bulkPositions;
	}
}
