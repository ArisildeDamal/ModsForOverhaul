using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common.Database;

namespace Vintagestory.Server
{
	internal class ServerSystemSendChunks : ServerSystem
	{
		public override void Dispose()
		{
			this.chunkPackets = null;
			this.chunksToSend = null;
			this.mapChunksToSend = null;
			this.toRemove = null;
		}

		public override int GetUpdateInterval()
		{
			if (!this.server.IsDedicatedServer)
			{
				return 0;
			}
			return MagicNum.ChunkRequestTickTime;
		}

		public ServerSystemSendChunks(ServerMain server)
			: base(server)
		{
			server.clientAwarenessEvents[EnumClientAwarenessEvent.ChunkTransition].Add(new Action<ClientStatistics>(this.OnClientLeaveChunk));
		}

		public override void OnServerTick(float dt)
		{
			if (this.server.RunPhase != EnumServerRunPhase.RunGame)
			{
				return;
			}
			this.packetsWithEntities.Clear();
			this.packetsWithoutEntities.Clear();
			IPlayer[] onlinePlayers = this.server.AllOnlinePlayers;
			foreach (IMiniDimension miniDimension in this.server.LoadedMiniDimensions.Values)
			{
				miniDimension.CollectChunksForSending(onlinePlayers);
			}
			foreach (ConnectedClient client in this.server.Clients.Values)
			{
				if (client.State == EnumClientState.Connected || client.State == EnumClientState.Playing)
				{
					this.sendAndEnqueueChunks(client);
				}
			}
			this.packetsWithEntities.Clear();
			this.packetsWithoutEntities.Clear();
		}

		private void OnClientLeaveChunk(ClientStatistics clientstats)
		{
			clientstats.client.CurrentChunkSentRadius = 0;
		}

		private void sendAndEnqueueChunks(ConnectedClient client)
		{
			int desiredRadius = (int)Math.Ceiling((double)((float)client.WorldData.Viewdistance / (float)MagicNum.ServerChunkSize));
			int finalChunkRadius = Math.Min(this.server.Config.MaxChunkRadius, desiredRadius);
			if (client.CurrentChunkSentRadius > finalChunkRadius && client.forceSendChunks.Count == 0 && client.forceSendMapChunks.Count == 0)
			{
				return;
			}
			this.chunksToSend.Clear();
			this.mapChunksToSend.Clear();
			this.toRemove.Clear();
			int countChunks = MagicNum.ChunksToSendPerTick * (client.IsLocalConnection ? 8 : 1);
			List<long> unsentMapChunks = new List<long>(1);
			foreach (long index2d in client.forceSendMapChunks)
			{
				Vec2i pos = this.server.WorldMap.MapChunkPosFromChunkIndex2D(index2d);
				ServerMapChunk mpc;
				this.server.loadedMapChunks.TryGetValue(index2d, out mpc);
				if (mpc != null)
				{
					this.server.SendPacketFast(client.Id, mpc.ToPacket(pos.X, pos.Y));
				}
				else
				{
					unsentMapChunks.Add(index2d);
				}
			}
			client.forceSendMapChunks.Clear();
			foreach (long index2d2 in unsentMapChunks)
			{
				client.forceSendMapChunks.Add(index2d2);
			}
			foreach (long index3d in client.forceSendChunks)
			{
				ServerChunk chunk = this.server.GetLoadedChunk(index3d);
				if (chunk != null)
				{
					if (countChunks <= 0)
					{
						break;
					}
					ChunkPos pos2 = this.server.WorldMap.ChunkPosFromChunkIndex3D(index3d);
					this.chunksToSend.Add(new ServerChunkWithCoord
					{
						chunk = chunk,
						pos = pos2,
						withEntities = true
					});
					countChunks--;
					this.toRemove.Add(index3d);
				}
			}
			foreach (object obj in this.toRemove)
			{
				long sentChunk = (long)obj;
				client.forceSendChunks.Remove(sentChunk);
			}
			if (countChunks > 0 && this.server.SendChunks && client.CurrentChunkSentRadius < finalChunkRadius && this.loadSendableChunksAtCurrentRadius(client, countChunks, client.Player.Entity.Pos.Dimension) == 0)
			{
				int num = client.CurrentChunkSentRadius + 1;
				client.CurrentChunkSentRadius = num;
				if (num <= finalChunkRadius && this.loadSendableChunksAtCurrentRadius(client, countChunks, client.Player.Entity.Pos.Dimension) == 0)
				{
					client.CurrentChunkSentRadius++;
				}
			}
			foreach (object obj2 in this.mapChunksToSend)
			{
				ServerMapChunkWithCoord req = (ServerMapChunkWithCoord)obj2;
				int chunkX = req.chunkX;
				int chunkZ = req.chunkZ;
				int regionX = chunkX / MagicNum.ChunkRegionSizeInChunks;
				int regionZ = chunkZ / MagicNum.ChunkRegionSizeInChunks;
				if (!client.DidSendMapRegion(this.server.WorldMap.MapRegionIndex2D(regionX, regionZ)))
				{
					this.server.SendPacketFast(client.Id, req.mapchunk.MapRegion.ToPacket(regionX, regionZ));
					client.SetMapRegionSent(this.server.WorldMap.MapRegionIndex2D(regionX, regionZ));
				}
				for (int dx = -1; dx <= 1; dx++)
				{
					for (int dz = -1; dz <= 1; dz++)
					{
						int nRegionX = regionX + dx;
						int nRegionZ = regionZ + dz;
						long nindex2d = this.server.WorldMap.MapRegionIndex2D(nRegionX, nRegionZ);
						ServerMapRegion region;
						if (!client.DidSendMapRegion(nindex2d) && this.server.loadedMapRegions.TryGetValue(nindex2d, out region))
						{
							this.server.SendPacketFast(client.Id, region.ToPacket(nRegionX, nRegionZ));
							client.SetMapRegionSent(nindex2d);
						}
					}
				}
				this.server.SendPacketFast(client.Id, req.mapchunk.ToPacket(chunkX, chunkZ));
				client.SetMapChunkSent(this.server.WorldMap.MapChunkIndex2D(chunkX, chunkZ));
			}
			int cnt = 0;
			foreach (object obj3 in this.chunksToSend)
			{
				ServerChunkWithCoord req2 = (ServerChunkWithCoord)obj3;
				this.chunkPackets[cnt++] = this.collectChunk(req2.chunk, req2.pos.X, req2.pos.Y + req2.pos.Dimension * 1024, req2.pos.Z, client, req2.withEntities);
				if (cnt >= 2048)
				{
					Packet_ServerChunks packet = new Packet_ServerChunks();
					packet.SetChunks(this.chunkPackets, cnt, cnt);
					this.server.SendPacketFast(client.Id, new Packet_Server
					{
						Id = 10,
						Chunks = packet
					});
					cnt = 0;
				}
			}
			if (cnt > 0)
			{
				Packet_ServerChunks packet2 = new Packet_ServerChunks();
				packet2.SetChunks(this.chunkPackets, cnt, cnt);
				this.server.SendPacketFast(client.Id, new Packet_Server
				{
					Id = 10,
					Chunks = packet2
				});
			}
		}

		private int loadSendableChunksAtCurrentRadius(ConnectedClient client, int countChunks, int dimension)
		{
			int requestChunkColumns = MagicNum.ChunksColumnsToRequestPerTick * (client.IsLocalConnection ? 4 : 1);
			Vec2i[] points = ShapeUtil.GetOctagonPoints((int)client.Position.X / MagicNum.ServerChunkSize, (int)client.Position.Z / MagicNum.ServerChunkSize, client.CurrentChunkSentRadius);
			int sentOrRequested = 0;
			int offsetY = dimension * 1024;
			for (int i = 0; i < points.Length; i++)
			{
				int chunkX = points[i].X;
				int chunkZ = points[i].Y;
				bool mapChunkAdded = false;
				long index2d = this.server.WorldMap.MapChunkIndex2D(chunkX, chunkZ);
				if (this.server.WorldMap.IsValidChunkPos(chunkX, offsetY, chunkZ))
				{
					for (int chunkY = 0; chunkY < this.server.WorldMap.ChunkMapSizeY; chunkY++)
					{
						long index3d = this.server.WorldMap.ChunkIndex3D(chunkX, chunkY + offsetY, chunkZ);
						if (!client.DidSendChunk(index3d) && !this.toRemove.Contains(index3d))
						{
							ServerChunk chunk = this.server.GetLoadedChunk(index3d);
							if (chunk != null)
							{
								if (countChunks > 0)
								{
									this.chunksToSend.Add(new ServerChunkWithCoord
									{
										chunk = chunk,
										pos = new ChunkPos(chunkX, chunkY, chunkZ, dimension)
									});
									countChunks--;
									if (!mapChunkAdded)
									{
										if (!client.DidSendMapChunk(index2d))
										{
											this.mapChunksToSend.Add(new ServerMapChunkWithCoord
											{
												chunkX = chunkX,
												chunkZ = chunkZ,
												mapchunk = (chunk.MapChunk as ServerMapChunk),
												index2d = index2d
											});
										}
										mapChunkAdded = true;
									}
								}
								sentOrRequested++;
							}
							else if (requestChunkColumns > 0)
							{
								if (!this.server.ChunkColumnRequested.ContainsKey(index2d) && this.server.AutoGenerateChunks)
								{
									this.server.ChunkColumnRequested[index2d] = 1;
									object requestedChunkColumnsLock = this.server.requestedChunkColumnsLock;
									lock (requestedChunkColumnsLock)
									{
										this.server.requestedChunkColumns.Enqueue(index2d);
									}
									requestChunkColumns--;
								}
								sentOrRequested++;
							}
						}
					}
				}
			}
			return sentOrRequested;
		}

		private Packet_ServerChunk collectChunk(ServerChunk serverChunk, int chunkX, int chunkY, int chunkZ, ConnectedClient client, bool withEntities)
		{
			long index3d = this.server.WorldMap.ChunkIndex3D(chunkX, chunkY, chunkZ);
			client.SetChunkSent(index3d);
			this.chunksSent++;
			Dictionary<long, Packet_ServerChunk> dict = (withEntities ? this.packetsWithEntities : this.packetsWithoutEntities);
			Packet_ServerChunk packet;
			if (dict.TryGetValue(index3d, out packet))
			{
				return packet;
			}
			packet = serverChunk.ToPacket(chunkX, chunkY, chunkZ, withEntities);
			dict[index3d] = packet;
			return packet;
		}

		public static string performanceTest(ServerMain server)
		{
			int iterations = 5;
			int affinityMask = 1023;
			int cx = 15650;
			int cz = 15640;
			int cy = 3;
			Process proc = Process.GetCurrentProcess();
			if (RuntimeEnv.OS == OS.Mac)
			{
				ServerMain.Logger.Warning("Cannot set a processor to run the performance test on Mac, performance test may not show max capable");
			}
			else
			{
				affinityMask = proc.ProcessorAffinity.ToInt32();
				proc.ProcessorAffinity = new IntPtr(2);
			}
			proc.PriorityClass = ProcessPriorityClass.High;
			Thread.CurrentThread.Priority = ThreadPriority.Highest;
			Stopwatch stopwatch = Stopwatch.StartNew();
			ServerChunk chunk = server.GetLoadedChunk(server.WorldMap.ChunkIndex3D(cx, cy, cz));
			if (chunk != null)
			{
				while (--iterations >= 0)
				{
					Packet_ServerChunk psc = chunk.ToPacket(cx, cy, cz, true);
					Packet_ServerChunks packet = new Packet_ServerChunks();
					packet.SetChunks(new Packet_ServerChunk[] { psc }, 1, 1);
					server.Serialize_(new Packet_Server
					{
						Id = 10,
						Chunks = packet
					});
				}
			}
			stopwatch.Stop();
			if (RuntimeEnv.OS != OS.Mac)
			{
				Process.GetCurrentProcess().ProcessorAffinity = new IntPtr(affinityMask);
			}
			Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
			Thread.CurrentThread.Priority = ThreadPriority.Normal;
			return "-ServerPacketSending: " + stopwatch.ElapsedTicks.ToString();
		}

		private Packet_ServerChunk[] chunkPackets = new Packet_ServerChunk[2048];

		private int chunksSent;

		private FastList<ServerChunkWithCoord> chunksToSend = new FastList<ServerChunkWithCoord>();

		private FastList<ServerMapChunkWithCoord> mapChunksToSend = new FastList<ServerMapChunkWithCoord>();

		private FastList<long> toRemove = new FastList<long>();

		private Dictionary<long, Packet_ServerChunk> packetsWithEntities = new Dictionary<long, Packet_ServerChunk>();

		private Dictionary<long, Packet_ServerChunk> packetsWithoutEntities = new Dictionary<long, Packet_ServerChunk>();
	}
}
