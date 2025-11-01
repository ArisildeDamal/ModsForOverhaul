using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common.Database;

namespace Vintagestory.Server
{
	internal class ServerSystemUnloadChunks : ServerSystem
	{
		public ServerSystemUnloadChunks(ServerMain server, ChunkServerThread chunkthread)
			: base(server)
		{
			this.chunkthread = chunkthread;
			server.api.ChatCommands.GetOrCreate("chunk").BeginSubCommand("unload").WithDescription("Toggle on / off whether the server(and thus in turn the client) should unload chunks")
				.WithAdditionalInformation("Default setting is on. This should normally be left on.")
				.WithArgs(new ICommandArgumentParser[] { server.api.ChatCommands.Parsers.Bool("setting", "on") })
				.HandleWith(new OnCommandDelegate(this.handleToggleUnload))
				.EndSubCommand();
		}

		public override void OnBeginModsAndConfigReady()
		{
			this.server.clientAwarenessEvents[EnumClientAwarenessEvent.ChunkTransition].Add(new Action<ClientStatistics>(this.OnPlayerLeaveChunk));
		}

		public override void OnBeginShutdown()
		{
			foreach (KeyValuePair<long, ServerChunk> val in this.server.loadedChunks)
			{
				ChunkPos pos = this.server.WorldMap.ChunkPosFromChunkIndex3D(val.Key);
				if (pos.Dimension <= 0)
				{
					this.server.api.eventapi.TriggerChunkColumnUnloaded(pos.ToVec3i());
				}
			}
			foreach (KeyValuePair<long, ServerMapRegion> val2 in this.server.loadedMapRegions)
			{
				ChunkPos pos2 = this.server.WorldMap.MapRegionPosFromIndex2D(val2.Key);
				this.server.api.eventapi.TriggerMapRegionUnloaded(new Vec2i(pos2.X, pos2.Z), val2.Value);
			}
		}

		private void OnPlayerLeaveChunk(ClientStatistics stats)
		{
			if (this.unloadingPaused)
			{
				return;
			}
			this.SendOutOfRangeChunkUnloads(stats.client);
		}

		public override void OnPlayerDisconnect(ServerPlayer player)
		{
			if (this.server.Clients.Count == 1)
			{
				ServerMain.Logger.Notification("Last player disconnected, compacting large object heap...");
				GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
				GC.Collect();
			}
		}

		private TextCommandResult handleToggleUnload(TextCommandCallingArgs args)
		{
			this.unloadingPaused = !(bool)args[0];
			return TextCommandResult.Success("Chunk unloading now " + (this.unloadingPaused ? "off" : "on"), null);
		}

		public override int GetUpdateInterval()
		{
			return 200;
		}

		public override void OnServerTick(float dt)
		{
			if (this.unloadingPaused)
			{
				return;
			}
			ServerMain.FrameProfiler.Enter("unloadchunks-all");
			int count = this.server.unloadedChunks.Count;
			this.SendUnloadedChunkUnloads();
			ServerMain.FrameProfiler.Mark("notified-clients:", count);
			this.accum3s += dt;
			if (this.accum3s >= 3f)
			{
				this.accum3s = 0f;
				this.FindUnloadableChunkColumnCandidates();
				ServerMain.FrameProfiler.Mark("find-chunkcolumns");
				if (this.mapChunkUnloadCandidates.Count > 0)
				{
					this.UnloadChunkColumns();
					if (this.server.Clients.IsEmpty)
					{
						this.server.serverChunkDataPool.FreeAll();
						GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
						GC.Collect();
						ServerMain.FrameProfiler.Mark("garbagecollector (no clients online)");
					}
				}
			}
			this.accum120s += dt;
			if (this.accum120s > 120f)
			{
				this.accum120s = 0f;
				this.FindUnusedMapRegions();
				ServerMain.FrameProfiler.Mark("find-mapregions");
			}
			ServerMain.FrameProfiler.Leave();
		}

		private void FindUnusedMapRegions()
		{
			List<long> regionsToClear = new List<long>();
			List<MapRegionAndPos> regionsToSave = null;
			foreach (KeyValuePair<long, ServerMapRegion> val in this.server.loadedMapRegions)
			{
				if (this.server.ElapsedMilliseconds - val.Value.loadedTotalMs >= 120000L)
				{
					ChunkPos pos = this.server.WorldMap.MapRegionPosFromIndex2D(val.Key);
					int blockx = pos.X * this.server.WorldMap.RegionSize;
					int num = pos.Z * this.server.WorldMap.RegionSize;
					int chunkx = blockx / 32;
					int chunkz = num / 32;
					if (!this.server.WorldMap.AnyLoadedChunkInMapRegion(chunkx, chunkz))
					{
						regionsToClear.Add(val.Key);
						this.server.api.eventapi.TriggerMapRegionUnloaded(new Vec2i(pos.X, pos.Z), val.Value);
						if (val.Value.DirtyForSaving)
						{
							if (regionsToSave == null)
							{
								regionsToSave = new List<MapRegionAndPos>();
							}
							regionsToSave.Add(new MapRegionAndPos(pos.ToVec3i(), val.Value));
						}
					}
				}
			}
			if (regionsToSave != null)
			{
				object obj = this.dirtyMapRegionsLock;
				lock (obj)
				{
					foreach (MapRegionAndPos toSave in regionsToSave)
					{
						this.dirtyMapRegions.Add(toSave);
						toSave.region.DirtyForSaving = false;
					}
				}
			}
			foreach (long val2 in regionsToClear)
			{
				this.server.loadedMapRegions.Remove(val2);
				this.server.BroadcastUnloadMapRegion(val2);
			}
		}

		public override void OnSeparateThreadTick()
		{
			if (this.server.RunPhase == EnumServerRunPhase.Shutdown)
			{
				return;
			}
			if (this.unloadingPaused)
			{
				return;
			}
			object obj = this.mapChunkIndicesLock;
			lock (obj)
			{
				this.mapChunkIndices.Clear();
				this.mapChunkIndices.AddRange(this.server.loadedMapChunks.Keys);
			}
			FastMemoryStream fastMemoryStream;
			if ((fastMemoryStream = ServerSystemUnloadChunks.reusableStream) == null)
			{
				fastMemoryStream = (ServerSystemUnloadChunks.reusableStream = new FastMemoryStream());
			}
			FastMemoryStream ms = fastMemoryStream;
			this.SaveDirtyUnloadedChunks(ms);
			this.SaveDirtyMapRegions(ms);
			this.UnloadGeneratingChunkColumns((long)MagicNum.UncompressedChunkTTL);
		}

		private void SaveDirtyMapRegions(FastMemoryStream ms)
		{
			if (this.dirtyMapRegions.Count > 0)
			{
				List<MapRegionAndPos> toSave = new List<MapRegionAndPos>();
				object obj = this.dirtyMapRegionsLock;
				lock (obj)
				{
					toSave.AddRange(this.dirtyMapRegions);
					this.dirtyMapRegions.Clear();
				}
				List<DbChunk> cp = new List<DbChunk>();
				foreach (MapRegionAndPos val in toSave)
				{
					cp.Add(new DbChunk(new ChunkPos(val.pos), val.region.ToBytes(ms)));
				}
				this.chunkthread.gameDatabase.SetMapRegions(cp);
			}
		}

		private void SaveDirtyUnloadedChunks(FastMemoryStream ms)
		{
			this.server.readyToAutoSave = false;
			List<ServerChunkWithCoord> dirtyChunksTmp = new List<ServerChunkWithCoord>();
			List<ServerMapChunkWithCoord> dirtyMapChunksTmp = new List<ServerMapChunkWithCoord>();
			object obj = this.dirtyChunksLock;
			lock (obj)
			{
				dirtyChunksTmp.AddRange(this.dirtyUnloadedChunks);
				dirtyMapChunksTmp.AddRange(this.dirtyUnloadedMapChunks);
				this.dirtyUnloadedChunks.Clear();
				this.dirtyUnloadedMapChunks.Clear();
			}
			List<DbChunk> dirtyDbChunks = new List<DbChunk>();
			List<DbChunk> dirtyDbMapChunks = new List<DbChunk>();
			foreach (ServerChunkWithCoord data in dirtyChunksTmp)
			{
				dirtyDbChunks.Add(new DbChunk
				{
					Position = data.pos,
					Data = data.chunk.ToBytes(ms)
				});
				data.chunk.Dispose();
			}
			foreach (ServerMapChunkWithCoord data2 in dirtyMapChunksTmp)
			{
				dirtyDbMapChunks.Add(new DbChunk
				{
					Position = new ChunkPos
					{
						X = data2.chunkX,
						Y = 0,
						Z = data2.chunkZ
					},
					Data = data2.mapchunk.ToBytes(ms)
				});
			}
			if (dirtyDbChunks.Count > 0)
			{
				this.chunkthread.gameDatabase.SetChunks(dirtyDbChunks);
			}
			if (dirtyDbMapChunks.Count > 0)
			{
				this.chunkthread.gameDatabase.SetMapChunks(dirtyDbMapChunks);
			}
			this.server.readyToAutoSave = true;
		}

		private void UnloadChunkColumns()
		{
			List<ServerChunkWithCoord> dirtyChunksTmp = new List<ServerChunkWithCoord>();
			List<ServerMapChunkWithCoord> dirtyMapChunksTmp = new List<ServerMapChunkWithCoord>();
			int cUnloaded = 0;
			foreach (long index2d in this.mapChunkUnloadCandidates)
			{
				if (!this.server.forceLoadedChunkColumns.Contains(index2d))
				{
					ChunkPos ret = this.server.WorldMap.ChunkPosFromChunkIndex2D(index2d);
					ServerSystemSupplyChunks.UpdateLoadedNeighboursFlags(this.server.WorldMap, ret.X, ret.Z);
					this.server.api.eventapi.TriggerChunkColumnUnloaded(ret.ToVec3i());
					for (int y = 0; y < this.server.WorldMap.ChunkMapSizeY; y++)
					{
						ret.Y = y;
						long posIndex3d = this.server.WorldMap.ChunkIndex3D(ret.X, y, ret.Z);
						ServerChunk chunk = this.server.GetLoadedChunk(posIndex3d);
						if (chunk != null && ServerSystemUnloadChunks.TryUnloadChunk(posIndex3d, ret, chunk, dirtyChunksTmp, this.server))
						{
							cUnloaded++;
						}
					}
					ServerMapChunk mapchunk;
					this.server.loadedMapChunks.TryGetValue(index2d, out mapchunk);
					if (mapchunk != null)
					{
						if (mapchunk.DirtyForSaving)
						{
							dirtyMapChunksTmp.Add(new ServerMapChunkWithCoord
							{
								chunkX = ret.X,
								chunkZ = ret.Z,
								index2d = index2d,
								mapchunk = mapchunk
							});
						}
						mapchunk.DirtyForSaving = false;
						this.server.loadedMapChunks.Remove(index2d);
					}
				}
			}
			object obj = this.dirtyChunksLock;
			lock (obj)
			{
				this.dirtyUnloadedChunks.AddRange(dirtyChunksTmp);
				this.dirtyUnloadedMapChunks.AddRange(dirtyMapChunksTmp);
			}
			ServerMain.FrameProfiler.Mark("unloaded-chunkcolumns:", this.mapChunkUnloadCandidates.Count);
			this.mapChunkUnloadCandidates.Clear();
		}

		public static bool TryUnloadChunk(long posIndex3d, ChunkPos ret, ServerChunk chunk, List<ServerChunkWithCoord> dirtyChunksTmp, ServerMain server)
		{
			bool mustSave = false;
			if (chunk.DirtyForSaving)
			{
				mustSave = true;
				dirtyChunksTmp.Add(new ServerChunkWithCoord
				{
					pos = ret,
					chunk = chunk
				});
			}
			chunk.DirtyForSaving = false;
			server.unloadedChunks.Enqueue(posIndex3d);
			long index2d = server.WorldMap.ChunkIndex3dToIndex2d(posIndex3d);
			server.loadedChunksLock.AcquireWriteLock();
			try
			{
				if (server.loadedChunks.Remove(posIndex3d))
				{
					server.ChunkColumnRequested.Remove(index2d);
				}
			}
			finally
			{
				server.loadedChunksLock.ReleaseWriteLock();
			}
			chunk.RemoveEntitiesAndBlockEntities(server);
			if (!mustSave)
			{
				chunk.Dispose();
			}
			return mustSave;
		}

		internal void UnloadGeneratingChunkColumns(long timeToLive)
		{
			List<ChunkColumnLoadRequest> toUnload = new List<ChunkColumnLoadRequest>();
			int cUnloaded = 0;
			foreach (ChunkColumnLoadRequest chunkreq in this.chunkthread.requestedChunkColumns.Snapshot())
			{
				if (chunkreq.Chunks != null && !chunkreq.Disposed)
				{
					EnumWorldGenPass curPass = chunkreq.CurrentIncompletePass;
					if (curPass >= chunkreq.GenerateUntilPass && curPass != EnumWorldGenPass.Done)
					{
						bool unload = true;
						if (!this.server.forceLoadedChunkColumns.Contains(chunkreq.mapIndex2d))
						{
							for (int y = 0; y < chunkreq.Chunks.Length; y++)
							{
								if ((long)Environment.TickCount - chunkreq.Chunks[y].lastReadOrWrite < timeToLive)
								{
									unload = false;
									break;
								}
							}
							if (unload)
							{
								toUnload.Add(chunkreq);
							}
						}
					}
				}
			}
			if (toUnload.Count == 0)
			{
				return;
			}
			List<DbChunk> dirtyChunks = new List<DbChunk>();
			List<DbChunk> dirtyMapChunks = new List<DbChunk>();
			using (FastMemoryStream reusableStream = new FastMemoryStream())
			{
				foreach (ChunkColumnLoadRequest req in toUnload)
				{
					req.generatingLock.AcquireReadLock();
					try
					{
						for (int y2 = 0; y2 < req.Chunks.Length; y2++)
						{
							if (req.Chunks[y2].DirtyForSaving)
							{
								req.Chunks[y2].DirtyForSaving = false;
								dirtyChunks.Add(new DbChunk
								{
									Position = new ChunkPos(req.chunkX, y2, req.chunkZ, 0),
									Data = req.Chunks[y2].ToBytes(reusableStream)
								});
							}
						}
						ServerMapChunk mapchunk = req.MapChunk;
						if (mapchunk != null)
						{
							if (mapchunk.DirtyForSaving)
							{
								dirtyMapChunks.Add(new DbChunk
								{
									Position = new ChunkPos(req.chunkX, 0, req.chunkZ, 0),
									Data = mapchunk.ToBytes(reusableStream)
								});
							}
							mapchunk.DirtyForSaving = false;
							this.server.loadedMapChunks.Remove(req.mapIndex2d);
						}
					}
					finally
					{
						req.generatingLock.ReleaseReadLock();
					}
					if (!this.chunkthread.requestedChunkColumns.Remove(req.mapIndex2d))
					{
						throw new Exception("Chunkrequest no longer in queue? Race condition?");
					}
					this.server.ChunkColumnRequested.Remove(req.mapIndex2d);
					cUnloaded++;
				}
				if (dirtyChunks.Count > 0)
				{
					this.chunkthread.gameDatabase.SetChunks(dirtyChunks);
				}
				if (dirtyMapChunks.Count > 0)
				{
					this.chunkthread.gameDatabase.SetMapChunks(dirtyMapChunks);
				}
			}
		}

		private void FindUnloadableChunkColumnCandidates()
		{
			List<long> index2ds = new List<long>();
			foreach (ConnectedClient client in this.server.Clients.Values)
			{
				int allowedChunkRadius = this.server.GetAllowedChunkRadius(client);
				int chunkX = ((client.Position == null) ? (this.server.WorldMap.MapSizeX / 2) : ((int)client.Position.X)) / MagicNum.ServerChunkSize;
				int chunkZ = ((client.Position == null) ? (this.server.WorldMap.MapSizeZ / 2) : ((int)client.Position.Z)) / MagicNum.ServerChunkSize;
				for (int r = 0; r <= allowedChunkRadius; r++)
				{
					ShapeUtil.LoadOctagonIndices(index2ds, chunkX, chunkZ, r, this.server.WorldMap.ChunkMapSizeX);
				}
			}
			Vec2i vec = new Vec2i();
			foreach (long num in index2ds)
			{
				MapUtil.PosInt2d(num, (long)this.server.WorldMap.ChunkMapSizeX, vec);
				long mapchunkindex2d = this.server.WorldMap.MapChunkIndex2D(vec.X, vec.Y);
				ServerMapChunk mapchunk;
				this.server.loadedMapChunks.TryGetValue(mapchunkindex2d, out mapchunk);
				if (mapchunk != null)
				{
					mapchunk.MarkFresh();
				}
			}
			object obj = this.mapChunkIndicesLock;
			lock (obj)
			{
				foreach (long index2d in this.mapChunkIndices)
				{
					ServerMapChunk mapchunk;
					if (!this.server.forceLoadedChunkColumns.Contains(index2d) && this.server.loadedMapChunks.TryGetValue(index2d, out mapchunk) && mapchunk.CurrentIncompletePass == EnumWorldGenPass.Done)
					{
						if (mapchunk.IsOld())
						{
							this.mapChunkUnloadCandidates.Add(index2d);
						}
						else
						{
							mapchunk.DoAge();
						}
					}
				}
			}
		}

		private void SendUnloadedChunkUnloads()
		{
			if (this.server.unloadedChunks.IsEmpty)
			{
				return;
			}
			List<long> unloadIndices = new List<long>();
			unloadIndices.AddRange(this.server.unloadedChunks);
			this.server.unloadedChunks = new ConcurrentQueue<long>();
			List<Vec3i> ulCoordForPlayer = new List<Vec3i>();
			foreach (ConnectedClient client in this.server.Clients.Values)
			{
				ulCoordForPlayer.Clear();
				foreach (long index3d in unloadIndices)
				{
					if (client.ChunkSent.Contains(index3d))
					{
						int cx = (int)(index3d % (long)this.server.WorldMap.index3dMulX);
						int cy = (int)(index3d / (long)this.server.WorldMap.index3dMulX / (long)this.server.WorldMap.index3dMulZ);
						int cz = (int)(index3d / (long)this.server.WorldMap.index3dMulX % (long)this.server.WorldMap.index3dMulZ);
						ulCoordForPlayer.Add(new Vec3i(cx, cy, cz));
						client.RemoveChunkSent(index3d);
						long index2d = this.server.WorldMap.ChunkIndex3dToIndex2d(index3d);
						client.RemoveMapChunkSent(index2d);
					}
				}
				if (ulCoordForPlayer.Count > 0)
				{
					int[] xr = new int[ulCoordForPlayer.Count];
					int[] yr = new int[ulCoordForPlayer.Count];
					int[] zr = new int[ulCoordForPlayer.Count];
					for (int i = 0; i < xr.Length; i++)
					{
						Vec3i coord = ulCoordForPlayer[i];
						xr[i] = coord.X;
						yr[i] = coord.Y;
						zr[i] = coord.Z;
					}
					Packet_UnloadServerChunk unloadPacket = new Packet_UnloadServerChunk();
					unloadPacket.SetX(xr);
					unloadPacket.SetY(yr);
					unloadPacket.SetZ(zr);
					Packet_Server packet = new Packet_Server
					{
						Id = 11,
						UnloadChunk = unloadPacket
					};
					this.server.SendPacket(client.Id, packet);
				}
			}
		}

		private void SendOutOfRangeChunkUnloads(ConnectedClient client)
		{
			List<long> unloadChunkIndices = new List<long>();
			HashSet<long> keepChunkColumns = new HashSet<long>();
			int allowedChunkRadius = this.server.GetAllowedChunkRadius(client);
			int chunkX = ((client.Position == null) ? (this.server.WorldMap.MapSizeX / 2) : ((int)client.Position.X)) / MagicNum.ServerChunkSize;
			int chunkZ = ((client.Position == null) ? (this.server.WorldMap.MapSizeZ / 2) : ((int)client.Position.Z)) / MagicNum.ServerChunkSize;
			int chunkMapSizeX = this.server.WorldMap.ChunkMapSizeX;
			for (int r = 0; r <= allowedChunkRadius; r++)
			{
				ShapeUtil.LoadOctagonIndices(keepChunkColumns, chunkX, chunkZ, r, chunkMapSizeX);
			}
			foreach (long index3d in client.ChunkSent)
			{
				if ((int)(index3d / ((long)this.server.WorldMap.index3dMulX * (long)this.server.WorldMap.index3dMulZ)) < 128)
				{
					long index2d = this.server.WorldMap.ChunkIndex3dToIndex2d(index3d);
					if (!keepChunkColumns.Contains(index2d))
					{
						unloadChunkIndices.Add(index3d);
						client.RemoveMapChunkSent(index2d);
					}
				}
			}
			if (unloadChunkIndices.Count > 0)
			{
				int[] xr = new int[unloadChunkIndices.Count];
				int[] yr = new int[unloadChunkIndices.Count];
				int[] zr = new int[unloadChunkIndices.Count];
				for (int i = 0; i < xr.Length; i++)
				{
					long index3d2 = unloadChunkIndices[i];
					client.RemoveChunkSent(index3d2);
					ServerChunk chunk = this.server.WorldMap.GetServerChunk(index3d2);
					if (chunk != null)
					{
						int count = chunk.EntitiesCount;
						for (int j = 0; j < count; j++)
						{
							client.TrackedEntities.Remove(chunk.Entities[j].EntityId);
						}
					}
					xr[i] = (int)(index3d2 % (long)this.server.WorldMap.index3dMulX);
					yr[i] = (int)(index3d2 / (long)this.server.WorldMap.index3dMulX / (long)this.server.WorldMap.index3dMulZ);
					zr[i] = (int)(index3d2 / (long)this.server.WorldMap.index3dMulX % (long)this.server.WorldMap.index3dMulZ);
				}
				Packet_UnloadServerChunk unloadPacket = new Packet_UnloadServerChunk();
				unloadPacket.SetX(xr);
				unloadPacket.SetY(yr);
				unloadPacket.SetZ(zr);
				Packet_Server packet = new Packet_Server
				{
					Id = 11,
					UnloadChunk = unloadPacket
				};
				this.server.SendPacket(client.Id, packet);
			}
		}

		private ChunkServerThread chunkthread;

		private bool unloadingPaused;

		private HashSet<long> mapChunkUnloadCandidates = new HashSet<long>();

		private object mapChunkIndicesLock = new object();

		private List<long> mapChunkIndices = new List<long>(800);

		private object dirtyChunksLock = new object();

		private List<ServerChunkWithCoord> dirtyUnloadedChunks = new List<ServerChunkWithCoord>();

		private List<ServerMapChunkWithCoord> dirtyUnloadedMapChunks = new List<ServerMapChunkWithCoord>();

		private object dirtyMapRegionsLock = new object();

		private List<MapRegionAndPos> dirtyMapRegions = new List<MapRegionAndPos>();

		[ThreadStatic]
		private static FastMemoryStream reusableStream;

		private float accum120s;

		private float accum3s;
	}
}
