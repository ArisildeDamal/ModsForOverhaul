using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.Common.Database;

namespace Vintagestory.Server
{
	internal class ServerSystemSupplyChunks : ServerSystem
	{
		public ServerSystemSupplyChunks(ServerMain server, ChunkServerThread chunkthread)
			: base(server)
		{
			this.chunkthread = chunkthread;
			chunkthread.loadsavechunks = this;
			server.RegisterGameTickListener(delegate(float dt)
			{
				server.serverChunkDataPool.SlowDispose();
			}, 1000, 0);
		}

		public override int GetUpdateInterval()
		{
			if (this.chunkthread.requestedChunkColumns.Count == 0 || !this.is8Core)
			{
				return MagicNum.ChunkThreadTickTime;
			}
			return this.chunkthread.additionalWorldGenThreadsCount - 1;
		}

		public override void OnBeginGameReady(SaveGame savegame)
		{
			this.requiresChunkBorderSmoothing = savegame.CreatedWorldGenVersion != 3;
			this.requiresSetEmptyFlag = GameVersion.IsLowerVersionThan(this.server.SaveGameData.LastSavedGameVersion, "1.12-dev.1");
			if (this.chunkthread.additionalWorldGenThreadsCount > 0)
			{
				int stageStart = 1;
				for (int i = 0; i < this.chunkthread.additionalWorldGenThreadsCount; i++)
				{
					this.CreateAdditionalWorldGenThread(stageStart, stageStart + 1, i + 1);
					stageStart++;
				}
			}
		}

		public override void OnSeparateThreadTick()
		{
			if (this.server.RunPhase != EnumServerRunPhase.RunGame)
			{
				return;
			}
			this.deleteChunkColumns();
			this.moveRequestsToGeneratingQueue();
			KeyValuePair<HorRectanglei, ChunkLoadOptions> val3;
			while (!this.server.fastChunkQueue.IsEmpty && this.server.fastChunkQueue.TryDequeue(out val3))
			{
				this.loadChunkAreaBlocking(val3.Key.X1, val3.Key.Z1, val3.Key.X2, val3.Key.Z2, false, val3.Value.ChunkGenParams);
				if (val3.Value.OnLoaded != null)
				{
					this.server.EnqueueMainThreadTask(val3.Value.OnLoaded);
				}
			}
			if (!this.server.simpleLoadRequests.IsEmpty && this.server.mapMiddleSpawnPos != null)
			{
				ChunkColumnLoadRequest loadRequest;
				while (this.server.simpleLoadRequests.TryDequeue(out loadRequest))
				{
					this.simplyLoadChunkColumn(loadRequest);
				}
			}
			KeyValuePair<Vec2i, ChunkPeekOptions> val2;
			if (!this.server.peekChunkColumnQueue.IsEmpty && this.server.peekChunkColumnQueue.TryDequeue(out val2))
			{
				if (this.PauseAllWorldgenThreads(3600))
				{
					this.PeekChunkAreaLocking(val2.Key, val2.Value.UntilPass, val2.Value.OnGenerated, val2.Value.ChunkGenParams);
				}
				this.ResumeAllWorldgenThreads();
			}
			while (!this.server.testChunkExistsQueue.IsEmpty)
			{
				ChunkLookupRequest val;
				if (!this.server.testChunkExistsQueue.TryDequeue(out val))
				{
					break;
				}
				bool exists = false;
				switch (val.Type)
				{
				case EnumChunkType.Chunk:
					exists = this.chunkthread.gameDatabase.ChunkExists(val.chunkX, val.chunkY, val.chunkZ);
					break;
				case EnumChunkType.MapChunk:
					exists = this.chunkthread.gameDatabase.MapChunkExists(val.chunkX, val.chunkZ);
					break;
				case EnumChunkType.MapRegion:
					exists = this.chunkthread.gameDatabase.MapRegionExists(val.chunkX, val.chunkZ);
					break;
				}
				this.server.EnqueueMainThreadTask(delegate
				{
					val.onTested(exists);
					ServerMain.FrameProfiler.Mark("MTT-TestExists");
				});
			}
			int i = 0;
			while (i < MagicNum.ChunkColumnsToGeneratePerThreadTick && this.tryLoadOrGenerateChunkColumnsInQueue() && !this.server.Suspended)
			{
				i++;
			}
		}

		private void deleteChunkColumns()
		{
			List<ChunkPos> chunkCoords = null;
			List<ChunkPos> mapChunkCoords = null;
			int mapSizeY = this.server.WorldMap.ChunkMapSizeY;
			long mapchunkindex2d;
			while (!this.server.deleteChunkColumns.IsEmpty && this.server.deleteChunkColumns.TryDequeue(out mapchunkindex2d))
			{
				if (chunkCoords == null)
				{
					chunkCoords = new List<ChunkPos>();
				}
				if (mapChunkCoords == null)
				{
					mapChunkCoords = new List<ChunkPos>();
				}
				if (this.chunkthread.requestedChunkColumns.Remove(mapchunkindex2d))
				{
					this.server.ChunkColumnRequested.Remove(mapchunkindex2d);
				}
				ChunkPos pos = this.server.WorldMap.ChunkPosFromChunkIndex2D(mapchunkindex2d);
				int cx = pos.X;
				int cz = pos.Z;
				if (cx < 0 || cz < 0)
				{
					ServerMain.Logger.Error("Delete chunks: mapchunkindex outside the map: " + cx.ToString() + "," + cz.ToString());
				}
				else
				{
					ServerSystemSupplyChunks.UpdateLoadedNeighboursFlags(this.server.WorldMap, cx, cz);
					for (int cy = 0; cy < mapSizeY; cy++)
					{
						ServerChunk chunk = (ServerChunk)this.server.WorldMap.GetChunk(cx, cy, cz);
						if (chunk != null)
						{
							Entity[] entities = chunk.Entities;
							for (int i = 0; i < entities.Length; i++)
							{
								Entity entity = entities[i];
								if (!(entity is EntityPlayer))
								{
									if (entity == null)
									{
										if (i >= chunk.EntitiesCount)
										{
											break;
										}
									}
									else
									{
										this.server.DespawnEntity(entity, new EntityDespawnData
										{
											Reason = EnumDespawnReason.Death
										});
									}
								}
							}
						}
						chunkCoords.Add(new ChunkPos
						{
							X = cx,
							Y = cy,
							Z = cz
						});
					}
					mapChunkCoords.Add(pos);
					this.server.loadedMapChunks.Remove(mapchunkindex2d);
				}
			}
			if (chunkCoords != null && chunkCoords.Count > 0)
			{
				this.chunkthread.gameDatabase.DeleteChunks(chunkCoords);
			}
			if (mapChunkCoords != null && mapChunkCoords.Count > 0)
			{
				this.chunkthread.gameDatabase.DeleteMapChunks(mapChunkCoords);
			}
			HashSet<ChunkPos> mapRegionCoords = null;
			long mapregionIndex2d;
			while (!this.server.deleteMapRegions.IsEmpty && this.server.deleteMapRegions.TryDequeue(out mapregionIndex2d))
			{
				ChunkPos regpos = this.server.WorldMap.MapRegionPosFromIndex2D(mapregionIndex2d);
				if (mapRegionCoords == null)
				{
					mapRegionCoords = new HashSet<ChunkPos>();
				}
				mapRegionCoords.Add(regpos);
				this.server.loadedMapRegions.Remove(mapregionIndex2d);
			}
			if (mapRegionCoords != null && mapRegionCoords.Count > 0)
			{
				this.chunkthread.gameDatabase.DeleteMapRegions(mapRegionCoords);
			}
		}

		private void moveRequestsToGeneratingQueue()
		{
			List<long> elems = new List<long>();
			object requestedChunkColumnsLock = this.server.requestedChunkColumnsLock;
			lock (requestedChunkColumnsLock)
			{
				while (this.server.requestedChunkColumns.Count > 0)
				{
					if (this.chunkthread.requestedChunkColumns.Capacity - this.chunkthread.requestedChunkColumns.Count < elems.Count + 200)
					{
						break;
					}
					elems.Add(this.server.requestedChunkColumns.Dequeue());
				}
			}
			for (int i = 0; i < elems.Count; i++)
			{
				long index2d = elems[i];
				Vec2i pos = this.server.WorldMap.MapChunkPosFromChunkIndex2D(index2d);
				this.chunkthread.addChunkColumnRequest(index2d, pos.X, pos.Y, -1, EnumWorldGenPass.Done, null);
			}
		}

		private bool simplyLoadChunkColumn(ChunkColumnLoadRequest request)
		{
			ServerMapChunk mapChunk = this.chunkthread.loadsavechunks.GetOrCreateMapChunk(request, false);
			if (mapChunk == null)
			{
				return false;
			}
			ServerChunk[] chunks = this.chunkthread.loadsavechunks.TryLoadChunkColumn(request);
			if (chunks == null)
			{
				return false;
			}
			foreach (ServerChunk serverChunk in chunks)
			{
				serverChunk.serverMapChunk = mapChunk;
				serverChunk.MarkFresh();
			}
			request.MapChunk = mapChunk;
			request.Chunks = chunks;
			this.server.EnqueueMainThreadTask(delegate
			{
				this.chunkthread.loadsavechunks.mainThreadLoadChunkColumn(request);
			});
			return true;
		}

		private bool tryLoadOrGenerateChunkColumnsInQueue()
		{
			if (this.chunkthread.requestedChunkColumns.Count == 0)
			{
				return false;
			}
			this.PauseWorldgenThreadIfRequired(true);
			this.CleanupRequestsQueue();
			int highestWorldgenPassOnOtherThreads = this.chunkthread.additionalWorldGenThreadsCount;
			foreach (ChunkColumnLoadRequest chunkRequest in this.chunkthread.requestedChunkColumns)
			{
				if (chunkRequest != null && !chunkRequest.Disposed)
				{
					int curPass = chunkRequest.CurrentIncompletePass_AsInt;
					if (curPass == 0 || curPass > highestWorldgenPassOnOtherThreads)
					{
						if (this.server.Suspended)
						{
							return false;
						}
						if (this.loadOrGenerateChunkColumn_OnChunkThread(chunkRequest, curPass) && this.chunkthread.additionalWorldGenThreadsCount == 0)
						{
							return true;
						}
					}
				}
			}
			return false;
		}

		private int CleanupRequestsQueue()
		{
			int countRemoved = 0;
			ConcurrentIndexedFifoQueue<ChunkColumnLoadRequest> requestedChunkColumns = this.chunkthread.requestedChunkColumns;
			while (requestedChunkColumns.Count > 0)
			{
				ChunkColumnLoadRequest chunkRequest = requestedChunkColumns.Peek();
				if (chunkRequest == null || chunkRequest.disposeOrRequeueFlags == 0)
				{
					break;
				}
				if (chunkRequest.Disposed)
				{
					requestedChunkColumns.DequeueWithoutRemovingFromIndex();
					countRemoved++;
				}
				else
				{
					chunkRequest.disposeOrRequeueFlags = 0;
					requestedChunkColumns.Requeue();
				}
			}
			return countRemoved;
		}

		public bool loadOrGenerateChunkColumn_OnChunkThread(ChunkColumnLoadRequest chunkRequest, int stage)
		{
			ServerMapChunk mapChunk = this.GetOrCreateMapChunk(chunkRequest, false);
			bool recreateColumn = false;
			if (mapChunk.WorldGenVersion != 3 && mapChunk.WorldGenVersion != 0 && mapChunk.CurrentIncompletePass < EnumWorldGenPass.Done)
			{
				mapChunk = this.GetOrCreateMapChunk(chunkRequest, true);
				recreateColumn = true;
				chunkRequest.Chunks = null;
			}
			if (chunkRequest.Chunks == null)
			{
				if (!recreateColumn)
				{
					chunkRequest.Chunks = this.TryLoadChunkColumn(chunkRequest);
					if (chunkRequest.Chunks != null)
					{
						for (int y = 0; y < chunkRequest.Chunks.Length; y++)
						{
							ServerChunk serverChunk = chunkRequest.Chunks[y];
							serverChunk.serverMapChunk = mapChunk;
							serverChunk.MarkFresh();
						}
					}
				}
				int regionX = chunkRequest.chunkX / (this.server.api.WorldManager.RegionSize / this.server.api.WorldManager.ChunkSize);
				int regionZ = chunkRequest.chunkZ / (this.server.api.WorldManager.RegionSize / this.server.api.WorldManager.ChunkSize);
				long regionIndex = this.server.api.WorldManager.MapRegionIndex2D(regionX, regionZ);
				IMapRegion mapRegion = this.server.api.WorldManager.GetMapRegion(regionIndex);
				if (mapRegion != null)
				{
					this.TryRestoreGeneratedStructures(regionX, regionZ, chunkRequest.chunkGenParams, mapRegion);
				}
				if (chunkRequest.Chunks == null)
				{
					this.GenerateNewChunkColumn(mapChunk, chunkRequest);
				}
			}
			chunkRequest.MapChunk = mapChunk;
			if (chunkRequest.CurrentIncompletePass == EnumWorldGenPass.Done)
			{
				if (chunkRequest.blockingRequest)
				{
					int num = this.blockingRequestsRemaining - 1;
					this.blockingRequestsRemaining = num;
					if (num == 0)
					{
						ServerMain.Logger.VerboseDebug("Completed area loading/generation");
					}
				}
				this.runScheduledBlockUpdatesWithNeighbours(chunkRequest);
				try
				{
					ServerEventAPI eventapi = this.server.api.eventapi;
					IServerMapChunk serverMapChunk = mapChunk;
					int chunkX = chunkRequest.chunkX;
					int chunkZ = chunkRequest.chunkZ;
					IWorldChunk[] chunks = chunkRequest.Chunks;
					eventapi.TriggerBeginChunkColumnLoadChunkThread(serverMapChunk, chunkX, chunkZ, chunks);
				}
				catch (Exception e)
				{
					ServerMain.Logger.Error("Exception throwing during chunk Unpack() at chunkpos xz {0}/{1}. Likely corrupted. Exception: {2}", new object[] { chunkRequest.chunkX, chunkRequest.chunkZ, e });
					if (this.server.Config.RepairMode)
					{
						ServerMain.Logger.Error("Repair mode is enabled so will delete the entire chunk column.");
						this.GenerateNewChunkColumn(mapChunk, chunkRequest);
						return false;
					}
				}
				this.server.EnqueueMainThreadTask(delegate
				{
					this.mainThreadLoadChunkColumn(chunkRequest);
					this.chunkthread.requestedChunkColumns.elementsByIndex.Remove(chunkRequest.Index);
				});
				chunkRequest.FlagToDispose();
				return true;
			}
			if (mapChunk.currentpass != stage && mapChunk.currentpass <= this.chunkthread.additionalWorldGenThreadsCount)
			{
				return stage == 0;
			}
			bool flag = this.CanGenerateChunkColumn(chunkRequest);
			if (flag)
			{
				this.PopulateChunk(chunkRequest);
			}
			if (!flag || this.chunkthread.additionalWorldGenThreadsCount == 0)
			{
				chunkRequest.FlagToRequeue();
			}
			return flag;
		}

		public int GenerateChunkColumns_OnSeparateThread(int stageStart, int stageEnd)
		{
			ChunkColumnLoadRequest newestRequiringWork = null;
			long newestTime = long.MinValue;
			foreach (ChunkColumnLoadRequest chunkRequest in this.chunkthread.requestedChunkColumns)
			{
				if (chunkRequest != null && !chunkRequest.Disposed)
				{
					int stage = chunkRequest.CurrentIncompletePass_AsInt;
					if (stage >= stageStart && stage < stageEnd)
					{
						if (stage >= chunkRequest.untilPass)
						{
							chunkRequest.FlagToRequeue();
						}
						else if (chunkRequest.creationTime > newestTime)
						{
							if (this.ensurePrettyNeighbourhood(chunkRequest))
							{
								newestTime = chunkRequest.creationTime;
								newestRequiringWork = chunkRequest;
							}
							else
							{
								chunkRequest.FlagToRequeue();
							}
						}
					}
				}
			}
			if (newestRequiringWork != null)
			{
				this.PopulateChunk(newestRequiringWork);
				return 1;
			}
			return 0;
		}

		public bool CanGenerateChunkColumn(ChunkColumnLoadRequest chunkRequest)
		{
			return chunkRequest.CurrentIncompletePass_AsInt < chunkRequest.untilPass && this.ensurePrettyNeighbourhood(chunkRequest);
		}

		internal void mainThreadLoadChunkColumn(ChunkColumnLoadRequest chunkRequest)
		{
			if (this.server.RunPhase == EnumServerRunPhase.Shutdown)
			{
				return;
			}
			ServerMain.FrameProfiler.Enter("MTT-ChunkLoaded-Begin");
			ServerEventAPI eventapi = this.server.api.eventapi;
			Vec2i vec2i = new Vec2i(chunkRequest.chunkX, chunkRequest.chunkZ);
			IWorldChunk[] chunks = chunkRequest.Chunks;
			eventapi.TriggerChunkColumnLoaded(vec2i, chunks);
			ServerMain.FrameProfiler.Mark("MTT-ChunkLoaded-LoadedEvent");
			int yIndex = 0;
			while (yIndex < chunkRequest.Chunks.Length)
			{
				ServerMain.FrameProfiler.Mark("MTT-ChunkLoaded-LoadedEvent");
				ServerChunk chunk = chunkRequest.Chunks[yIndex];
				int chunkY = yIndex + chunkRequest.dimension * 1024;
				long index3d = this.server.WorldMap.ChunkIndex3D(chunkRequest.chunkX, chunkY, chunkRequest.chunkZ);
				this.server.loadedChunksLock.AcquireWriteLock();
				try
				{
					if (this.server.loadedChunks.ContainsKey(index3d))
					{
						goto IL_0464;
					}
					this.server.loadedChunks[index3d] = chunk;
					chunk.MarkToPack();
				}
				finally
				{
					this.server.loadedChunksLock.ReleaseWriteLock();
				}
				goto IL_00FF;
				IL_0464:
				yIndex++;
				continue;
				IL_00FF:
				if (this.server.Config.AnalyzeMode)
				{
					try
					{
						chunk.Unpack();
					}
					catch (Exception e)
					{
						ServerMain.Logger.Error("Exception throwing during chunk Unpack() at chunkpos {0}/{1}/{2}, dimension {4}. Likely corrupted. Exception: {3}", new object[] { chunkRequest.chunkX, yIndex, chunkRequest.chunkZ, e, chunkRequest.dimension });
						if (this.server.Config.RepairMode && chunkRequest.dimension == 0)
						{
							ServerMain.Logger.Error("Repair mode is enabled so will delete the entire chunk column.");
							this.server.api.worldapi.DeleteChunkColumn(chunkRequest.chunkX, chunkRequest.chunkZ);
							ServerMain.FrameProfiler.Leave();
							return;
						}
					}
				}
				this.entitiesToRemove.Clear();
				if (chunk.Entities != null)
				{
					for (int i = 0; i < chunk.Entities.Length; i++)
					{
						Entity e2 = chunk.Entities[i];
						if (e2 == null)
						{
							if (i >= chunk.EntitiesCount)
							{
								break;
							}
						}
						else if (!this.server.LoadEntity(e2, index3d))
						{
							this.entitiesToRemove.Add(e2.EntityId);
							if (e2.Code.Path.StartsWith("villager"))
							{
								string vname = "-";
								try
								{
									ITreeAttribute treeAttribute = e2.WatchedAttributes.GetTreeAttribute("nametag");
									vname = ((treeAttribute != null) ? treeAttribute.GetString("name", null) : null) ?? "-";
								}
								catch (Exception)
								{
								}
								LoggerBase logger = ServerMain.Logger;
								EnumLogType enumLogType = EnumLogType.Worldgen;
								string[] array = new string[7];
								array[0] = "In ";
								array[1] = this.server.WorldName;
								array[2] = ", villager ";
								array[3] = vname;
								array[4] = " removed at ";
								int num = 5;
								BlockPos asBlockPos = e2.ServerPos.AsBlockPos;
								array[num] = ((asBlockPos != null) ? asBlockPos.ToString() : null);
								array[6] = " due to an exception on loading";
								logger.Log(enumLogType, string.Concat(array));
							}
						}
					}
				}
				foreach (long entityId in this.entitiesToRemove)
				{
					chunk.RemoveEntity(entityId);
				}
				ServerMain.FrameProfiler.Enter("MTT-ChunkLoaded-LoadBlockEntities");
				foreach (KeyValuePair<BlockPos, BlockEntity> val in chunk.BlockEntities)
				{
					BlockEntity be = val.Value;
					if (be != null)
					{
						try
						{
							ServerMain.FrameProfiler.Enter(be.Block.Code.Path);
							be.Initialize(this.server.api);
							if (chunk.serverMapChunk.NewBlockEntities.Contains(be.Pos))
							{
								chunk.serverMapChunk.NewBlockEntities.Remove(be.Pos);
								be.OnBlockPlaced(null);
							}
							ServerMain.FrameProfiler.Leave();
						}
						catch (Exception e3)
						{
							ServerMain.Logger.Notification("Exception thrown when trying to initialize a block entity @{0}: {1}", new object[] { be.Pos, e3 });
							be.UnregisterAllTickListeners();
						}
					}
				}
				ServerMain.FrameProfiler.Leave();
				this.server.api.eventapi.TriggerChunkDirty(new Vec3i(chunkRequest.chunkX, chunkY, chunkRequest.chunkZ), chunk, EnumChunkDirtyReason.NewlyLoaded);
				goto IL_0464;
			}
			ServerMain.FrameProfiler.Mark("MTT-ChunkLoaded-MarkDirtyEvent");
			this.updateNeighboursLoadedFlags(chunkRequest.MapChunk, chunkRequest.chunkX, chunkRequest.chunkZ);
			ServerMain.FrameProfiler.Mark("MTT-ChunkLoaded-UpdateNeighboursFlags");
			for (int chunkY2 = 0; chunkY2 < chunkRequest.Chunks.Length; chunkY2++)
			{
				chunkRequest.Chunks[chunkY2].TryPackAndCommit(8000);
			}
			ServerMain.FrameProfiler.Mark("MTT-ChunkLoaded-Pack");
			ServerMain.FrameProfiler.Leave();
		}

		private void updateNeighboursLoadedFlags(ServerMapChunk mapChunk, int chunkX, int chunkZ)
		{
			mapChunk.NeighboursLoaded = default(SmallBoolArray);
			mapChunk.SelfLoaded = true;
			for (int i = 0; i < Cardinal.ALL.Length; i++)
			{
				Cardinal cd = Cardinal.ALL[i];
				ServerMapChunk mc = (ServerMapChunk)this.server.WorldMap.GetMapChunk(chunkX + cd.Normali.X, chunkZ + cd.Normali.Z);
				if (mc != null)
				{
					mapChunk.NeighboursLoaded[i] = mc.SelfLoaded;
					mc.NeighboursLoaded[cd.Opposite.Index] = true;
				}
			}
		}

		public static void UpdateLoadedNeighboursFlags(ServerWorldMap WorldMap, int chunkX, int chunkZ)
		{
			ServerMapChunk mcNorth = (ServerMapChunk)WorldMap.GetMapChunk(chunkX, chunkZ - 1);
			ServerMapChunk mcNorthEast = (ServerMapChunk)WorldMap.GetMapChunk(chunkX + 1, chunkZ - 1);
			ServerMapChunk mcEast = (ServerMapChunk)WorldMap.GetMapChunk(chunkX + 1, chunkZ);
			ServerMapChunk mcSouthEast = (ServerMapChunk)WorldMap.GetMapChunk(chunkX + 1, chunkZ + 1);
			ServerMapChunk mcSouth = (ServerMapChunk)WorldMap.GetMapChunk(chunkX, chunkZ + 1);
			ServerMapChunk mcSouthWest = (ServerMapChunk)WorldMap.GetMapChunk(chunkX - 1, chunkZ + 1);
			ServerMapChunk mcWest = (ServerMapChunk)WorldMap.GetMapChunk(chunkX - 1, chunkZ);
			ServerMapChunk mcNorthWest = (ServerMapChunk)WorldMap.GetMapChunk(chunkX - 1, chunkZ - 1);
			if (mcNorth != null)
			{
				mcNorth.NeighboursLoaded[4] = false;
			}
			if (mcNorthEast != null)
			{
				mcNorthEast.NeighboursLoaded[5] = false;
			}
			if (mcEast != null)
			{
				mcEast.NeighboursLoaded[6] = false;
			}
			if (mcSouthEast != null)
			{
				mcSouthEast.NeighboursLoaded[7] = false;
			}
			if (mcSouth != null)
			{
				mcSouth.NeighboursLoaded[0] = false;
			}
			if (mcSouthWest != null)
			{
				mcSouthWest.NeighboursLoaded[1] = false;
			}
			if (mcWest != null)
			{
				mcWest.NeighboursLoaded[2] = false;
			}
			if (mcNorthWest != null)
			{
				mcNorthWest.NeighboursLoaded[3] = false;
			}
		}

		private void runScheduledBlockUpdatesWithNeighbours(ChunkColumnLoadRequest chunkRequest)
		{
			for (int z = -1; z <= 1; z++)
			{
				for (int x = -1; x <= 1; x++)
				{
					bool doScheduledBlockUpdates = false;
					ServerMapChunk mpc;
					if (this.server.loadedMapChunks.TryGetValue(this.server.WorldMap.MapChunkIndex2D(chunkRequest.chunkX + x, chunkRequest.chunkZ + z), out mpc) && mpc.CurrentIncompletePass == EnumWorldGenPass.Done && mpc.ScheduledBlockUpdates.Count > 0 && this.areAllChunkNeighboursLoaded(mpc, chunkRequest.chunkX + x, chunkRequest.chunkZ + z))
					{
						doScheduledBlockUpdates = true;
					}
					if (doScheduledBlockUpdates)
					{
						foreach (BlockPos pos in mpc.ScheduledBlockUpdates)
						{
							this.server.WorldMap.MarkBlockModified(pos, true);
						}
						mpc.ScheduledBlockUpdates.Clear();
					}
				}
			}
		}

		private bool areAllChunkNeighboursLoaded(ServerMapChunk mpc, int chunkX, int chunkZ)
		{
			int neibsloaded = 0;
			for (int dz = -1; dz <= 1; dz++)
			{
				for (int dx = -1; dx <= 1; dx++)
				{
					ServerMapChunk neibmpc;
					if ((dx != 0 || dz != 0) && this.server.loadedMapChunks.TryGetValue(this.server.WorldMap.MapChunkIndex2D(chunkX + dx, chunkZ + dz), out neibmpc) && neibmpc.CurrentIncompletePass == EnumWorldGenPass.Done)
					{
						neibsloaded++;
					}
				}
			}
			return neibsloaded == 8;
		}

		private ServerMapRegion GetOrCreateMapRegionEnsureNeighbours(int chunkX, int chunkZ, ITreeAttribute chunkGenParams, bool updateEarlierVersion)
		{
			ServerMapRegion mapRegion = this.GetOrCreateMapRegion(chunkX, chunkZ, chunkGenParams, updateEarlierVersion);
			if (!mapRegion.NeighbourRegionsChecked)
			{
				int regionX = chunkX / MagicNum.ChunkRegionSizeInChunks;
				int regionZ = chunkZ / MagicNum.ChunkRegionSizeInChunks;
				for (int dx = -1; dx <= 1; dx++)
				{
					for (int dz = -1; dz <= 1; dz++)
					{
						if (regionX + dx >= 0 && regionZ + dz >= 0 && (dx != 0 || dz != 0))
						{
							this.GetOrCreateMapRegion((regionX + dx) * MagicNum.ChunkRegionSizeInChunks, (regionZ + dz) * MagicNum.ChunkRegionSizeInChunks, chunkGenParams, false);
						}
					}
				}
				mapRegion.NeighbourRegionsChecked = true;
			}
			return mapRegion;
		}

		private ServerMapRegion GetOrCreateMapRegion(int chunkX, int chunkZ, ITreeAttribute chunkGenParams, bool updateEarlierVersion)
		{
			int regionX = chunkX / MagicNum.ChunkRegionSizeInChunks;
			int regionZ = chunkZ / MagicNum.ChunkRegionSizeInChunks;
			long mapRegionIndex2d = this.server.WorldMap.MapRegionIndex2D(regionX, regionZ);
			ServerMapRegion mapRegion;
			this.server.loadedMapRegions.TryGetValue(mapRegionIndex2d, out mapRegion);
			if (mapRegion != null)
			{
				if (!updateEarlierVersion || mapRegion.worldgenVersion == 3)
				{
					return mapRegion;
				}
			}
			else
			{
				mapRegion = this.TryLoadMapRegion(regionX, regionZ);
			}
			List<GeneratedStructure> savedMapRegionStructures = null;
			Dictionary<string, byte[]> savedModData = null;
			Dictionary<string, IntDataMap2D> savedOreMaps = null;
			IntDataMap2D savedGeologicProvinceMap = null;
			IntDataMap2D[] savedRockStrata = null;
			bool createNewRegion = true;
			if (mapRegion != null)
			{
				if (mapRegion.worldgenVersion != 3 && updateEarlierVersion)
				{
					ServerMain.Logger.Worldgen(string.Concat(new string[]
					{
						"Updating existing mapregion with worldgenVersion ",
						mapRegion.worldgenVersion.ToString(),
						" at ",
						(regionX * MagicNum.ChunkRegionSizeInChunks).ToString(),
						",",
						(regionZ * MagicNum.ChunkRegionSizeInChunks).ToString(),
						" in world: ",
						this.server.WorldName
					}));
					savedMapRegionStructures = mapRegion.GeneratedStructures;
					savedModData = mapRegion.ModData;
					savedOreMaps = mapRegion.OreMaps;
					savedGeologicProvinceMap = mapRegion.GeologicProvinceMap;
					savedRockStrata = mapRegion.RockStrata;
				}
				else
				{
					createNewRegion = false;
					mapRegion.loadedTotalMs = this.server.ElapsedMilliseconds;
				}
			}
			if (createNewRegion)
			{
				mapRegion = this.CreateMapRegion(regionX, regionZ, chunkGenParams);
				mapRegion.loadedTotalMs = this.server.ElapsedMilliseconds;
				if (savedMapRegionStructures != null)
				{
					mapRegion.GeneratedStructures = savedMapRegionStructures;
				}
				if (savedModData != null)
				{
					mapRegion.ModData = savedModData;
				}
				if (savedOreMaps != null)
				{
					mapRegion.OreMaps = savedOreMaps;
				}
				if (savedGeologicProvinceMap != null)
				{
					mapRegion.GeologicProvinceMap = savedGeologicProvinceMap;
				}
				if (savedRockStrata != null)
				{
					mapRegion.RockStrata = savedRockStrata;
				}
			}
			this.server.loadedMapRegions[mapRegionIndex2d] = mapRegion;
			this.server.EnqueueMainThreadTask(delegate
			{
				this.server.api.eventapi.TriggerMapRegionLoaded(new Vec2i(regionX, regionZ), mapRegion);
				ServerMain.FrameProfiler.Mark("trigger-mapregionloaded");
			});
			return mapRegion;
		}

		private ServerMapRegion CreateMapRegion(int regionX, int regionZ, ITreeAttribute chunkGenParams)
		{
			ServerMapRegion mapRegion = ServerMapRegion.CreateNew();
			for (int i = 0; i < this.worldgenHandler.OnMapRegionGen.Count; i++)
			{
				this.worldgenHandler.OnMapRegionGen[i](mapRegion, regionX, regionZ, chunkGenParams);
			}
			return mapRegion;
		}

		private void TryRestoreGeneratedStructures(int regionX, int regionZ, ITreeAttribute chunkGenParams, IMapRegion mapRegion)
		{
			byte[] structureData = ((chunkGenParams != null) ? chunkGenParams.GetBytes("GeneratedStructures", null) : null);
			if (structureData != null)
			{
				Dictionary<long, List<GeneratedStructure>> dictionary = SerializerUtil.Deserialize<Dictionary<long, List<GeneratedStructure>>>(structureData);
				long regionIndex = this.server.api.WorldManager.MapRegionIndex2D(regionX, regionZ);
				List<GeneratedStructure> generatedStructures;
				if (dictionary.TryGetValue(regionIndex, out generatedStructures))
				{
					if (generatedStructures == null)
					{
						return;
					}
					mapRegion.GeneratedStructures.AddRange(generatedStructures.Where((GeneratedStructure structure) => !mapRegion.GeneratedStructures.Any((GeneratedStructure s) => s.Location.Start.Equals(structure.Location.Start))));
				}
			}
		}

		internal ServerMapChunk GetOrCreateMapChunk(ChunkColumnLoadRequest chunkRequest, bool forceCreate = false)
		{
			int chunkX = chunkRequest.chunkX;
			int chunkZ = chunkRequest.chunkZ;
			long mapChunkIndex2d = this.server.WorldMap.MapChunkIndex2D(chunkX, chunkZ);
			ServerMapChunk mapChunk;
			ServerMapRegion mapRegion;
			if (!forceCreate)
			{
				this.server.loadedMapChunks.TryGetValue(mapChunkIndex2d, out mapChunk);
				if (mapChunk != null)
				{
					return mapChunk;
				}
				mapChunk = this.TryLoadMapChunk(chunkX, chunkZ);
				if (mapChunk != null)
				{
					mapRegion = this.GetOrCreateMapRegionEnsureNeighbours(chunkX, chunkZ, chunkRequest.chunkGenParams, false);
					mapChunk.MapRegion = mapRegion;
					this.server.loadedMapChunks[mapChunkIndex2d] = mapChunk;
					return mapChunk;
				}
			}
			bool maybeEarlierVersion = GameVersion.IsLowerVersionThan(this.server.SaveGameData.CreatedGameVersion, "1.21.5");
			mapRegion = this.GetOrCreateMapRegionEnsureNeighbours(chunkX, chunkZ, chunkRequest.chunkGenParams, maybeEarlierVersion);
			mapChunk = this.CreateMapChunk(chunkX, chunkZ, mapRegion);
			this.server.loadedMapChunks[mapChunkIndex2d] = mapChunk;
			return mapChunk;
		}

		private ServerMapChunk PeekMapChunk(int chunkX, int chunkZ)
		{
			long mapChunkIndex2d = this.server.WorldMap.MapChunkIndex2D(chunkX, chunkZ);
			ServerMapChunk mapChunk;
			this.server.loadedMapChunks.TryGetValue(mapChunkIndex2d, out mapChunk);
			if (mapChunk != null)
			{
				return mapChunk;
			}
			return this.TryLoadMapChunk(chunkX, chunkZ);
		}

		private ServerMapChunk CreateMapChunk(int chunkX, int chunkZ, ServerMapRegion mapRegion)
		{
			ServerMapChunk mapChunk = ServerMapChunk.CreateNew(mapRegion);
			for (int i = 0; i < this.worldgenHandler.OnMapChunkGen.Count; i++)
			{
				this.worldgenHandler.OnMapChunkGen[i](mapChunk, chunkX, chunkZ);
			}
			return mapChunk;
		}

		private void GenerateNewChunkColumn(ServerMapChunk mapChunk, ChunkColumnLoadRequest chunkRequest)
		{
			int quantity = this.server.WorldMap.ChunkMapSizeY;
			chunkRequest.Chunks = new ServerChunk[quantity];
			for (int y = 0; y < quantity; y++)
			{
				ServerChunk chunk = ServerChunk.CreateNew(this.server.serverChunkDataPool);
				chunk.serverMapChunk = mapChunk;
				chunkRequest.Chunks[y] = chunk;
			}
			chunkRequest.MapChunk = mapChunk;
			if (this.requiresChunkBorderSmoothing)
			{
				for (int i = 0; i < Cardinal.ALL.Length; i++)
				{
					Cardinal cd = Cardinal.ALL[i];
					ServerMapChunk mc = this.PeekMapChunk(chunkRequest.chunkX + cd.Normali.X, chunkRequest.chunkZ + cd.Normali.Z);
					if (mc != null && mc.CurrentIncompletePass >= EnumWorldGenPass.Done && mc.WorldGenVersion != 3)
					{
						if (chunkRequest.NeighbourTerrainHeight == null)
						{
							chunkRequest.NeighbourTerrainHeight = new ushort[8][];
						}
						chunkRequest.NeighbourTerrainHeight[i] = mc.WorldGenTerrainHeightMap;
					}
				}
				chunkRequest.RequiresChunkBorderSmoothing = chunkRequest.NeighbourTerrainHeight != null;
				if (mapChunk.CurrentIncompletePass < EnumWorldGenPass.NeighbourSunLightFlood && mapChunk.WorldGenVersion != 3)
				{
					mapChunk.WorldGenVersion = 3;
				}
			}
			chunkRequest.CurrentIncompletePass = EnumWorldGenPass.Terrain;
		}

		internal ServerChunk[] TryLoadChunkColumn(ChunkColumnLoadRequest chunkRequest)
		{
			int quantity = this.server.WorldMap.ChunkMapSizeY;
			ServerChunk[] chunks = new ServerChunk[quantity];
			int loaded = 0;
			for (int y = 0; y < quantity; y++)
			{
				byte[] serializedChunk = this.chunkthread.gameDatabase.GetChunk(chunkRequest.chunkX, y, chunkRequest.chunkZ, chunkRequest.dimension);
				if (serializedChunk != null)
				{
					try
					{
						loaded++;
						chunks[y] = ServerChunk.FromBytes(serializedChunk, this.server.serverChunkDataPool, this.server);
					}
					catch (Exception e)
					{
						if (!this.server.Config.RegenerateCorruptChunks && !this.server.Config.RepairMode)
						{
							ServerMain.Logger.Error("Failed deserializing a chunk. Not in repair mode, will exit.");
							throw;
						}
						chunks[y] = ServerChunk.CreateNew(this.server.serverChunkDataPool);
						ServerMain.Logger.Error("Failed deserializing a chunk, we are in repair mode, so will initilize empty one. Exception: {0}", new object[] { e });
					}
				}
			}
			if (this.requiresSetEmptyFlag)
			{
				foreach (ServerChunk chunk in chunks)
				{
					if (chunk != null)
					{
						chunk.Unpack();
						chunk.MarkModified();
						chunk.TryPackAndCommit(8000);
					}
				}
			}
			if (loaded != 0 && loaded != quantity)
			{
				ServerMain.Logger.Error("Loaded some but not all chunks of a column? Discarding whole column.");
				return null;
			}
			if (loaded != quantity)
			{
				return null;
			}
			return chunks;
		}

		private ServerMapChunk TryLoadMapChunk(int chunkX, int chunkZ)
		{
			byte[] serializedMapChunk = this.chunkthread.gameDatabase.GetMapChunk(chunkX, chunkZ);
			if (serializedMapChunk != null)
			{
				try
				{
					ServerMapChunk mapchunk = ServerMapChunk.FromBytes(serializedMapChunk);
					if (GameVersion.IsLowerVersionThan(this.server.SaveGameData.CreatedGameVersion, "1.7"))
					{
						mapchunk.YMax = (ushort)(this.server.SaveGameData.MapSizeY - 1);
					}
					return mapchunk;
				}
				catch (Exception e)
				{
					if (chunkX == 0 && chunkZ == 0)
					{
						return null;
					}
					if (this.server.Config.RegenerateCorruptChunks || this.server.Config.RepairMode)
					{
						ServerMain.Logger.Error("Failed deserializing a map chunk, we are in repair mode, so will initialize empty one. Exception: {0}", new object[] { e });
						return null;
					}
					ServerMain.Logger.Error("Failed deserializing a map chunk. Not in repair mode, will exit.");
					throw;
				}
			}
			return null;
		}

		private ServerMapRegion TryLoadMapRegion(int regionX, int regionZ)
		{
			byte[] serializedMapRegion = this.chunkthread.gameDatabase.GetMapRegion(regionX, regionZ);
			try
			{
				if (serializedMapRegion != null)
				{
					return ServerMapRegion.FromBytes(serializedMapRegion);
				}
			}
			catch (Exception e)
			{
				if (this.server.Config.RepairMode)
				{
					ServerMain.Logger.Error("Failed deserializing a map region, we are in repair mode, so will initialize empty one.");
					ServerMain.Logger.Error(e);
					return null;
				}
				ServerMain.Logger.Error("Failed deserializing a map region. Not in repair mode, will exit.");
				throw;
			}
			return null;
		}

		public void InitWorldgenAndSpawnChunks()
		{
			this.worldgenHandler = (WorldGenHandler)this.server.api.Event.TriggerInitWorldGen();
			ServerMain.Logger.Event("Loading {0}x{1}x{2} spawn chunks...", new object[]
			{
				MagicNum.SpawnChunksWidth,
				MagicNum.SpawnChunksWidth,
				this.server.WorldMap.ChunkMapSizeY
			});
			if (this.storyChunkSpawnEvents == null)
			{
				this.storyChunkSpawnEvents = new string[]
				{
					Lang.Get("...the carved mountains", Array.Empty<object>()),
					Lang.Get("...the rolling hills", Array.Empty<object>()),
					Lang.Get("...the vertical cliffs", Array.Empty<object>()),
					Lang.Get("...the endless plains", Array.Empty<object>()),
					Lang.Get("...the winter lands", Array.Empty<object>()),
					Lang.Get("...and scorching deserts", Array.Empty<object>()),
					Lang.Get("...spring waters", Array.Empty<object>()),
					Lang.Get("...tunnels deep below", Array.Empty<object>()),
					Lang.Get("...the luscious trees", Array.Empty<object>()),
					Lang.Get("...the fragrant flowers", Array.Empty<object>()),
					Lang.Get("...the roaming creatures", Array.Empty<object>()),
					Lang.Get("with their offspring...", Array.Empty<object>()),
					Lang.Get("...a misty sunrise", Array.Empty<object>()),
					Lang.Get("...dew drops on a blade of grass", Array.Empty<object>()),
					Lang.Get("...a soft breeze", Array.Empty<object>())
				};
			}
			BlockPos pos = new BlockPos(this.server.WorldMap.MapSizeX / 2, 0, this.server.WorldMap.MapSizeZ / 2, 0);
			if (GameVersion.IsLowerVersionThan(this.server.SaveGameData.CreatedGameVersion, "1.20.0-pre.14"))
			{
				int dcx = 0;
				int dcz = 0;
				bool found = false;
				Random rand = new Random(this.server.Seed);
				int maxTries = 5;
				int maxRadiusToTry = 20;
				for (int tries = 0; tries < maxTries; tries++)
				{
					if (tries > 0)
					{
						double maxRadiusThisAttempt = (double)GameMath.Sqrt((double)tries * (1.0 / (double)maxTries)) * (double)maxRadiusToTry;
						double num = (1.0 - Math.Abs(rand.NextDouble() - rand.NextDouble())) * maxRadiusThisAttempt;
						double rndAngle = rand.NextDouble() * 6.2831854820251465;
						double offsetX = num * GameMath.Sin(rndAngle);
						int num2 = (int)(num * GameMath.Cos(rndAngle));
						dcx = (int)offsetX;
						dcz = num2;
						pos = new BlockPos(dcx * 32 + this.server.WorldMap.MapSizeX / 2, 0, dcz * 32 + this.server.WorldMap.MapSizeZ / 2, 0);
					}
					this.loadChunkAreaBlocking(dcx + this.server.WorldMap.ChunkMapSizeX / 2 - MagicNum.SpawnChunksWidth / 2, dcz + this.server.WorldMap.ChunkMapSizeZ / 2 - MagicNum.SpawnChunksWidth / 2, dcx + this.server.WorldMap.ChunkMapSizeX / 2 + MagicNum.SpawnChunksWidth / 2, dcz + this.server.WorldMap.ChunkMapSizeZ / 2 + MagicNum.SpawnChunksWidth / 2, true, null);
					this.server.ProcessMainThreadTasks();
					if (ServerSystemSupplyChunks.AdjustForSaveSpawnSpot(this.server, pos, null, rand))
					{
						found = true;
						break;
					}
					if (tries + 1 < maxTries)
					{
						this.server.api.Logger.Notification("Trying another spawn location ({0}/{1})...", new object[]
						{
							tries + 2,
							maxTries
						});
					}
				}
				if (!found)
				{
					pos = new BlockPos(this.server.WorldMap.MapSizeX / 2, 0, this.server.WorldMap.MapSizeZ / 2, 0);
					pos.Y = this.server.blockAccessor.GetRainMapHeightAt(pos);
					if (!this.server.blockAccessor.GetBlock(pos).SideSolid[BlockFacing.UP.Index])
					{
						this.server.blockAccessor.SetBlock(this.server.blockAccessor.GetBlock(new AssetLocation("planks-oak-we")).Id, pos);
					}
					pos.Y++;
				}
			}
			else
			{
				this.loadChunkAreaBlocking(this.server.WorldMap.ChunkMapSizeX / 2 - MagicNum.SpawnChunksWidth / 2, this.server.WorldMap.ChunkMapSizeZ / 2 - MagicNum.SpawnChunksWidth / 2, this.server.WorldMap.ChunkMapSizeX / 2 + MagicNum.SpawnChunksWidth / 2, this.server.WorldMap.ChunkMapSizeZ / 2 + MagicNum.SpawnChunksWidth / 2, true, null);
				this.server.ProcessMainThreadTasks();
			}
			this.server.mapMiddleSpawnPos = new PlayerSpawnPos
			{
				x = pos.X,
				y = new int?(pos.Y),
				z = pos.Z
			};
			if (pos.Y < 0)
			{
				this.server.mapMiddleSpawnPos.y = null;
			}
			this.server.api.Logger.VerboseDebug("Done spawn chunk");
		}

		public static bool AdjustForSaveSpawnSpot(ServerMain server, BlockPos pos, IServerPlayer forPlayer, Random rand)
		{
			int tries = 60;
			int dx = 0;
			int dz = 0;
			while (tries-- > 0)
			{
				int posx = GameMath.Clamp(dx + pos.X, 0, server.WorldMap.MapSizeX - 1);
				int posz = GameMath.Clamp(dz + pos.Z, 0, server.WorldMap.MapSizeZ - 1);
				int posy = server.WorldMap.GetTerrainGenSurfacePosY(posx, posz);
				pos.Set(posx, posy, posz);
				dx = rand.Next(64) - 32;
				dz = rand.Next(64) - 32;
				if (posy != 0 && (double)posy <= 0.75 * (double)server.WorldMap.MapSizeY)
				{
					if (server.WorldMap.GetBlockingLandClaimant(forPlayer, pos, EnumBlockAccessFlags.Use) != null)
					{
						server.api.Logger.Notification("Spawn pos blocked at " + ((pos != null) ? pos.ToString() : null));
					}
					else if (!server.BlockAccessor.GetBlockRaw(posx, posy + 1, posz, 2).IsLiquid() && !server.BlockAccessor.GetBlockRaw(posx, posy, posz, 2).IsLiquid() && !server.BlockAccessor.IsSideSolid(posx, posy + 1, posz, BlockFacing.UP))
					{
						return true;
					}
				}
			}
			return false;
		}

		private void PeekChunkAreaLocking(Vec2i coords, EnumWorldGenPass untilPass, OnChunkPeekedDelegate onGenerated, ITreeAttribute chunkGenParams)
		{
			this.chunkthread.peekMode = true;
			int centerCx = coords.X;
			int centerCz = coords.Y;
			Dictionary<Vec2i, ServerMapRegion> regions = new Dictionary<Vec2i, ServerMapRegion>();
			int nowPass = 1;
			int endPass = Math.Min((int)untilPass, 5);
			int startRadius = endPass - nowPass;
			ChunkColumnLoadRequest[,] reqs = new ChunkColumnLoadRequest[startRadius * 2 + 1, startRadius * 2 + 1];
			IndexedFifoQueue<ChunkColumnLoadRequest> indexedFifoQueue;
			for (int cx = -startRadius; cx <= startRadius; cx++)
			{
				for (int cz = -startRadius; cz <= startRadius; cz++)
				{
					long num = this.server.WorldMap.MapChunkIndex2D(centerCx + cx, centerCz + cz);
					int regionX = (centerCx + cx) / MagicNum.ChunkRegionSizeInChunks;
					int regionZ = (centerCz + cz) / MagicNum.ChunkRegionSizeInChunks;
					Vec2i regionCoord = new Vec2i(regionX, regionZ);
					bool createNewRegion = false;
					List<GeneratedStructure> savedMapRegionStructures = null;
					Dictionary<string, byte[]> savedModData = null;
					Dictionary<string, IntDataMap2D> savedOreMaps = null;
					IntDataMap2D savedGeologicProvinceMap = null;
					IntDataMap2D[] savedRockStrata = null;
					ServerMapRegion mapregion;
					if (!regions.TryGetValue(regionCoord, out mapregion))
					{
						createNewRegion = true;
					}
					else if (mapregion.worldgenVersion != 3 && GameVersion.IsLowerVersionThan(this.server.SaveGameData.CreatedGameVersion, "1.21.5"))
					{
						createNewRegion = true;
						savedMapRegionStructures = mapregion.GeneratedStructures;
						savedModData = mapregion.ModData;
						savedOreMaps = mapregion.OreMaps;
						savedGeologicProvinceMap = mapregion.GeologicProvinceMap;
						savedRockStrata = mapregion.RockStrata;
					}
					if (createNewRegion)
					{
						mapregion = (regions[regionCoord] = this.CreateMapRegion(regionX, regionZ, chunkGenParams));
						if (savedMapRegionStructures != null)
						{
							mapregion.GeneratedStructures = savedMapRegionStructures;
						}
						if (savedModData != null)
						{
							mapregion.ModData = savedModData;
						}
						if (savedOreMaps != null)
						{
							mapregion.OreMaps = savedOreMaps;
						}
						if (savedGeologicProvinceMap != null)
						{
							mapregion.GeologicProvinceMap = savedGeologicProvinceMap;
						}
						if (savedRockStrata != null)
						{
							mapregion.RockStrata = savedRockStrata;
						}
					}
					ServerMapChunk mapchunk = this.CreateMapChunk(centerCx, centerCz, mapregion);
					ChunkColumnLoadRequest chunkRequest = new ChunkColumnLoadRequest(num, centerCx + cx, centerCz + cz, 0, (int)untilPass, this.server)
					{
						chunkGenParams = ((cx == 0 && cz == 0) ? chunkGenParams : null)
					};
					chunkRequest.MapChunk = mapchunk;
					this.GenerateNewChunkColumn(mapchunk, chunkRequest);
					chunkRequest.Unpack();
					reqs[cx + startRadius, cz + startRadius] = chunkRequest;
					indexedFifoQueue = this.chunkthread.peekingChunkColumns;
					lock (indexedFifoQueue)
					{
						this.chunkthread.peekingChunkColumns.Enqueue(chunkRequest);
					}
				}
			}
			int radius = endPass - nowPass;
			while (nowPass <= endPass)
			{
				radius = endPass - nowPass;
				for (int cx2 = -radius; cx2 <= radius; cx2++)
				{
					for (int cz2 = -radius; cz2 <= radius; cz2++)
					{
						ChunkColumnLoadRequest chunkRequest2 = reqs[cx2 + startRadius, cz2 + startRadius];
						this.runGenerators(chunkRequest2, nowPass);
					}
				}
				nowPass++;
			}
			Dictionary<Vec2i, IServerChunk[]> columnsByChunkCoordinate = new Dictionary<Vec2i, IServerChunk[]>();
			indexedFifoQueue = this.chunkthread.peekingChunkColumns;
			lock (indexedFifoQueue)
			{
				for (int cx3 = -startRadius; cx3 <= startRadius; cx3++)
				{
					for (int cz3 = -startRadius; cz3 <= startRadius; cz3++)
					{
						long index2d = this.server.WorldMap.MapChunkIndex2D(centerCx + cx3, centerCz + cz3);
						this.chunkthread.peekingChunkColumns.Remove(index2d);
						ChunkColumnLoadRequest chunkRequest3 = reqs[cx3 + startRadius, cz3 + startRadius];
						Dictionary<Vec2i, IServerChunk[]> columnsByChunkCoordinate2 = columnsByChunkCoordinate;
						Vec2i vec2i = new Vec2i(centerCx + cx3, centerCz + cz3);
						IServerChunk[] chunks = chunkRequest3.Chunks;
						columnsByChunkCoordinate2[vec2i] = chunks;
					}
				}
			}
			this.chunkthread.peekMode = false;
			this.server.EnqueueMainThreadTask(delegate
			{
				onGenerated(columnsByChunkCoordinate);
				ServerMain.FrameProfiler.Mark("MTT-PeekChunk");
			});
		}

		private void loadChunkAreaBlocking(int chunkX1, int chunkZ1, int chunkX2, int chunkZ2, bool isStartupLoad = false, ITreeAttribute chunkGenParams = null)
		{
			this.ResumeAllWorldgenThreads();
			int startQuantity = this.server.loadedChunks.Count / this.server.WorldMap.ChunkMapSizeY;
			int toGenerate = (chunkX2 - chunkX1 + 1) * (chunkZ2 - chunkZ1 + 1);
			this.CleanupRequestsQueue();
			this.moveRequestsToGeneratingQueue();
			this.blockingRequestsRemaining = 0;
			foreach (ChunkColumnLoadRequest chunkColumnLoadRequest in this.chunkthread.requestedChunkColumns)
			{
				chunkColumnLoadRequest.blockingRequest = false;
			}
			for (int chunkX3 = chunkX1; chunkX3 <= chunkX2; chunkX3++)
			{
				for (int chunkZ3 = chunkZ1; chunkZ3 <= chunkZ2; chunkZ3++)
				{
					if (this.server.WorldMap.IsValidChunkPos(chunkX3, 0, chunkZ3) && !this.server.IsChunkColumnFullyLoaded(chunkX3, chunkZ3))
					{
						ChunkColumnLoadRequest req = new ChunkColumnLoadRequest(this.server.WorldMap.MapChunkIndex2D(chunkX3, chunkZ3), chunkX3, chunkZ3, this.server.serverConsoleId, 6, this.server)
						{
							chunkGenParams = chunkGenParams
						};
						req.blockingRequest = true;
						if (this.chunkthread.addChunkColumnRequest(req))
						{
							this.blockingRequestsRemaining++;
						}
					}
				}
			}
			long timeout = (long)(Environment.TickCount + 12000);
			ServerMain.Logger.VerboseDebug("Starting area loading/generation: columns " + this.blockingRequestsRemaining.ToString() + ", total queue length " + this.chunkthread.requestedChunkColumns.Count.ToString());
			while (!this.server.stopped && !this.server.exit.exit)
			{
				this.CleanupRequestsQueue();
				if (this.blockingRequestsRemaining <= 0 || this.chunkthread.requestedChunkColumns.Count == 0)
				{
					break;
				}
				if (isStartupLoad && this.server.totalUnpausedTime.ElapsedMilliseconds - this.millisecondsSinceStart > 1500L)
				{
					this.millisecondsSinceStart = this.server.totalUnpausedTime.ElapsedMilliseconds;
					float completion = 100f * ((float)this.server.loadedChunks.Count / (float)this.server.WorldMap.ChunkMapSizeY - (float)startQuantity) / (float)toGenerate;
					ServerMain.Logger.Event(completion.ToString("0.#") + "% ({0} in queue)", new object[] { this.chunkthread.requestedChunkColumns.Count });
					if (this.storyEventPrints < this.storyChunkSpawnEvents.Length)
					{
						ServerMain.Logger.StoryEvent(this.storyChunkSpawnEvents[this.storyEventPrints]);
					}
					else
					{
						ServerMain.Logger.StoryEvent("...");
					}
					this.storyEventPrints++;
				}
				bool doneAny = false;
				foreach (ChunkColumnLoadRequest request in this.chunkthread.requestedChunkColumns)
				{
					if (request != null && !request.Disposed)
					{
						int curPass = request.CurrentIncompletePass_AsInt;
						if (curPass == 0 || curPass > this.chunkthread.additionalWorldGenThreadsCount)
						{
							if (this.server.exit.exit || this.server.stopped)
							{
								return;
							}
							doneAny |= this.loadOrGenerateChunkColumn_OnChunkThread(request, curPass);
						}
					}
				}
				if (doneAny)
				{
					timeout = (long)(Environment.TickCount + 12000);
				}
				else if ((long)Environment.TickCount > timeout)
				{
					ServerMain.Logger.Error(string.Concat(new string[]
					{
						"Attempting to force generate chunk columns from ",
						chunkX1.ToString(),
						",",
						chunkZ1.ToString(),
						" to ",
						chunkX2.ToString(),
						",",
						chunkZ2.ToString()
					}));
					ServerMain.Logger.Error(this.chunkthread.additionalWorldGenThreadsCount.ToString() + " additional worldgen threads active, number of 'undone' chunks is " + this.blockingRequestsRemaining.ToString());
					foreach (ChunkColumnLoadRequest request2 in this.chunkthread.requestedChunkColumns)
					{
						if (request2 != null)
						{
							string inset = ((request2.chunkX >= chunkX1 && request2.chunkX <= chunkX2 && request2.chunkZ >= chunkZ1 && request2.chunkZ <= chunkZ2) ? " (in original req)" : "");
							ServerMain.Logger.Error(string.Concat(new string[]
							{
								"Column ",
								request2.ChunkX.ToString(),
								",",
								request2.ChunkZ.ToString(),
								" has reached pass ",
								request2.CurrentIncompletePass_AsInt.ToString(),
								inset
							}));
						}
					}
					throw new Exception("Somehow worldgen has become stuck in an endless loop, please report this as a bug!  Additional data in the server-main log");
				}
			}
		}

		private void PopulateChunk(ChunkColumnLoadRequest chunkRequest)
		{
			chunkRequest.Unpack();
			chunkRequest.generatingLock.AcquireWriteLock();
			try
			{
				if (this.server.Config.SkipEveryChunkRow > 0 && chunkRequest.chunkX % (this.server.Config.SkipEveryChunkRow + this.server.Config.SkipEveryChunkRowWidth) < this.server.Config.SkipEveryChunkRowWidth)
				{
					if (chunkRequest.CurrentIncompletePass == EnumWorldGenPass.Terrain)
					{
						ushort defaultSunlight = (ushort)this.server.sunBrightness;
						for (int y = 0; y < chunkRequest.Chunks.Length; y++)
						{
							chunkRequest.Chunks[y].Lighting.ClearWithSunlight(defaultSunlight);
						}
					}
				}
				else
				{
					this.runGenerators(chunkRequest, chunkRequest.MapChunk.currentpass);
				}
				if (chunkRequest.CurrentIncompletePass == EnumWorldGenPass.Terrain)
				{
					chunkRequest.MapChunk.WorldGenVersion = 3;
				}
				for (int i = 0; i < chunkRequest.Chunks.Length; i++)
				{
					chunkRequest.Chunks[i].MarkModified();
				}
				chunkRequest.MapChunk.currentpass++;
				chunkRequest.MapChunk.DirtyForSaving = true;
			}
			finally
			{
				chunkRequest.generatingLock.ReleaseWriteLock();
			}
		}

		private void runGenerators(ChunkColumnLoadRequest chunkRequest, int forPass)
		{
			List<ChunkColumnGenerationDelegate> handlers = this.worldgenHandler.OnChunkColumnGen[forPass];
			if (handlers != null)
			{
				for (int i = 0; i < handlers.Count; i++)
				{
					try
					{
						handlers[i](chunkRequest);
					}
					catch (Exception e)
					{
						ServerMain.Logger.Worldgen("An error was thrown in pass {5} when generating chunk column X={0},Z={1} in world '{3}' with seed {4}\nException {2}\n\n", new object[]
						{
							chunkRequest.chunkX,
							chunkRequest.chunkZ,
							e,
							this.server.SaveGameData.WorldName,
							this.server.SaveGameData.Seed,
							chunkRequest.CurrentIncompletePass.ToString()
						});
						if (chunkRequest.CurrentIncompletePass <= EnumWorldGenPass.Terrain)
						{
							break;
						}
					}
				}
			}
		}

		private bool ensurePrettyNeighbourhood(ChunkColumnLoadRequest chunkRequest)
		{
			if (chunkRequest.CurrentIncompletePass <= EnumWorldGenPass.Terrain)
			{
				return true;
			}
			bool pretty = true;
			int minPass = chunkRequest.CurrentIncompletePass_AsInt;
			int minx = Math.Max(chunkRequest.chunkX - 1, 0);
			int maxx = Math.Min(chunkRequest.chunkX + 1, this.server.WorldMap.ChunkMapSizeX - 1);
			int minz = Math.Max(chunkRequest.chunkZ - 1, 0);
			int maxz = Math.Min(chunkRequest.chunkZ + 1, this.server.WorldMap.ChunkMapSizeZ - 1);
			if (!chunkRequest.prettified && !this.EnsureQueueSpace(chunkRequest))
			{
				return false;
			}
			for (int chunkX = minx; chunkX <= maxx; chunkX++)
			{
				for (int chunkZ = minz; chunkZ <= maxz; chunkZ++)
				{
					if (chunkX != chunkRequest.chunkX || chunkZ != chunkRequest.chunkZ)
					{
						long index2d = this.server.WorldMap.MapChunkIndex2D(chunkX, chunkZ);
						if (!this.chunkthread.EnsureMinimumWorldgenPassAt(index2d, chunkX, chunkZ, minPass, chunkRequest.creationTime))
						{
							if (chunkRequest.prettified)
							{
								return false;
							}
							pretty = false;
						}
					}
				}
			}
			chunkRequest.prettified = true;
			return pretty;
		}

		private EnumWorldGenPass getLoadedOrQueuedChunkPass(long index2d)
		{
			ServerMapChunk mapchunk;
			this.server.loadedMapChunks.TryGetValue(index2d, out mapchunk);
			if (mapchunk != null && mapchunk.CurrentIncompletePass == EnumWorldGenPass.Done)
			{
				return EnumWorldGenPass.Done;
			}
			ChunkColumnLoadRequest request = this.chunkthread.requestedChunkColumns.GetByIndex(index2d);
			if (request != null)
			{
				return request.CurrentIncompletePass;
			}
			return EnumWorldGenPass.None;
		}

		private bool EnsureQueueSpace(ChunkColumnLoadRequest curRequest)
		{
			int requestToDrop = this.chunkthread.requestedChunkColumns.Count + 30 - this.chunkthread.requestedChunkColumns.Capacity;
			if (requestToDrop <= 0)
			{
				return true;
			}
			if (!this.PauseAllWorldgenThreads(5000))
			{
				return false;
			}
			bool flag;
			try
			{
				ServerMain.Logger.Warning("Requested chunks buffer is too small! Taking measures to attempt to free enough space. Try increasing servermagicnumbers RequestChunkColumnsQueueSize?");
				((ServerSystemUnloadChunks)this.chunkthread.serversystems[2]).UnloadGeneratingChunkColumns((long)(MagicNum.UncompressedChunkTTL / 10));
				foreach (ChunkColumnLoadRequest chunkColumnLoadRequest in this.chunkthread.requestedChunkColumns.Snapshot())
				{
					chunkColumnLoadRequest.FlagToRequeue();
				}
				requestToDrop -= this.CleanupRequestsQueue();
				if (requestToDrop <= 0)
				{
					flag = true;
				}
				else
				{
					ServerMain.Logger.Error("Requested chunks buffer is too small! Can't free enough space to completely generate chunks, clearing whole buffer. This may cause issues. Try increasing servermagicnumbers RequestChunkColumnsQueueSize and/or reducing UncompressedChunkTTL.");
					this.FullyClearGeneratingQueue();
					this.chunkthread.requestedChunkColumns.Enqueue(curRequest);
					flag = true;
				}
			}
			finally
			{
				this.ResumeAllWorldgenThreads();
				if (this.chunkthread.additionalWorldGenThreadsCount > 0)
				{
					ServerMain.Logger.VerboseDebug("Un-pausing all worldgen threads.");
				}
			}
			return flag;
		}

		internal void FullyClearGeneratingQueue()
		{
			ServerSystemLoadAndSaveGame loadsavegame = this.chunkthread.loadsavegame;
			FastMemoryStream fastMemoryStream;
			if ((fastMemoryStream = ServerSystemSupplyChunks.saveChunksStream) == null)
			{
				fastMemoryStream = (ServerSystemSupplyChunks.saveChunksStream = new FastMemoryStream());
			}
			loadsavegame.SaveAllDirtyGeneratingChunks(fastMemoryStream);
			foreach (ChunkColumnLoadRequest req in this.chunkthread.requestedChunkColumns)
			{
				if (req != null && !req.Disposed)
				{
					this.server.loadedMapChunks.Remove(req.mapIndex2d);
					this.server.ChunkColumnRequested.Remove(req.mapIndex2d);
				}
			}
			this.chunkthread.requestedChunkColumns.Clear();
			ServerMain.Logger.VerboseDebug("Incomplete chunks stored and wiped.");
		}

		private Thread CreateAdditionalWorldGenThread(int stageStart, int stageEnd, int threadnum)
		{
			Thread thread = TyronThreadPool.CreateDedicatedThread(delegate
			{
				this.GeneratorThreadLoop(stageStart, stageEnd);
			}, "worldgen" + threadnum.ToString());
			thread.Start();
			return thread;
		}

		public void GeneratorThreadLoop(int stageStart, int stageEnd)
		{
			Thread.Sleep(5);
			int tries = Math.Min(3, MagicNum.ChunkColumnsToGeneratePerThreadTick / this.chunkthread.additionalWorldGenThreadsCount);
			int columnsDone = 0;
			while (!this.server.stopped)
			{
				if (this.chunkthread.requestedChunkColumns.Count > 0 && (!this.server.Suspended || this.server.RunPhase == EnumServerRunPhase.WorldReady))
				{
					try
					{
						for (int i = 0; i < tries; i++)
						{
							int done = this.GenerateChunkColumns_OnSeparateThread(stageStart, stageEnd);
							columnsDone += done;
							if (done == 0)
							{
								break;
							}
						}
					}
					catch (Exception e)
					{
						ServerMain.Logger.Error(e);
					}
				}
				this.PauseWorldgenThreadIfRequired(false);
				Thread.Sleep(1);
			}
			BlockAccessorWorldGen.ThreadDispose();
		}

		public override void OnSeperateThreadShutDown()
		{
			BlockAccessorWorldGen.ThreadDispose();
		}

		public override void Dispose()
		{
			BlockAccessorWorldGen.ThreadDispose();
		}

		public bool PauseAllWorldgenThreads(int timeoutms)
		{
			if (Interlocked.CompareExchange(ref this.pauseAllWorldgenThreads, 1, 0) != 0)
			{
				return false;
			}
			if (this.chunkthread.additionalWorldGenThreadsCount > 0)
			{
				ServerMain.Logger.VerboseDebug("Pausing all worldgen threads.");
			}
			long maxTime = (long)(Environment.TickCount + timeoutms);
			while (this.pauseAllWorldgenThreads < this.chunkthread.additionalWorldGenThreadsCount + 1 && !this.server.stopped)
			{
				if ((long)Environment.TickCount > maxTime)
				{
					ServerMain.Logger.VerboseDebug("Pausing all worldgen threads - exceeded timeout!");
					return false;
				}
				Thread.Sleep(1);
			}
			return true;
		}

		public void ResumeAllWorldgenThreads()
		{
			this.pauseAllWorldgenThreads = 0;
		}

		public void PauseWorldgenThreadIfRequired(bool onChunkthread)
		{
			if (this.pauseAllWorldgenThreads > 0)
			{
				Interlocked.Increment(ref this.pauseAllWorldgenThreads);
				while (this.pauseAllWorldgenThreads != 0 && !this.server.stopped)
				{
					if (onChunkthread)
					{
						this.chunkthread.paused = true;
					}
					Thread.Sleep(15);
				}
				if (onChunkthread)
				{
					this.chunkthread.paused = this.server.Suspended || this.chunkthread.ShouldPause;
				}
			}
		}

		private WorldGenHandler worldgenHandler;

		private ChunkServerThread chunkthread;

		private bool is8Core = Environment.ProcessorCount >= 8;

		private bool requiresChunkBorderSmoothing;

		private bool requiresSetEmptyFlag;

		private volatile int pauseAllWorldgenThreads;

		[ThreadStatic]
		private static FastMemoryStream saveChunksStream;

		private List<long> entitiesToRemove = new List<long>();

		private int storyEventPrints;

		private string[] storyChunkSpawnEvents;

		private int blockingRequestsRemaining;
	}
}
