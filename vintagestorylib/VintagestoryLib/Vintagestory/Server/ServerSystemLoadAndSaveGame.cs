using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.Common.Database;
using Vintagestory.Server.Database;

namespace Vintagestory.Server
{
	internal class ServerSystemLoadAndSaveGame : ServerSystem, IChunkProviderThread
	{
		private FastMemoryStream reusableStream
		{
			get
			{
				FastMemoryStream fastMemoryStream;
				if ((fastMemoryStream = ServerSystemLoadAndSaveGame.reusableMemoryStream) == null)
				{
					fastMemoryStream = (ServerSystemLoadAndSaveGame.reusableMemoryStream = new FastMemoryStream());
				}
				return fastMemoryStream;
			}
		}

		public ServerSystemLoadAndSaveGame(ServerMain server, ChunkServerThread chunkthread)
			: base(server)
		{
			this.chunkthread = chunkthread;
			chunkthread.loadsavegame = this;
		}

		public override void OnSeparateThreadTick()
		{
			if (this.chunkthread.runOffThreadSaveNow)
			{
				object obj = this.savingLock;
				lock (obj)
				{
					if (this.chunkthread.runOffThreadSaveNow)
					{
						int saved = this.SaveAllDirtyLoadedChunks(true, this.reusableStream);
						ServerMain.Logger.Event("Offthread save of {0} chunks done.", new object[] { saved });
						saved = this.SaveAllDirtyGeneratingChunks(this.reusableStream);
						ServerMain.Logger.Notification("Offthread save of {0} generating chunks done.", new object[] { saved });
						int dirtyMapChunks = this.SaveAllDirtyMapChunks(this.reusableStream);
						ServerMain.Logger.Event("Offthread save of {0} map chunks done.", new object[] { dirtyMapChunks });
						this.server.SaveGameData.UpdateLandClaims(this.server.WorldMap.All);
						this.chunkthread.gameDatabase.StoreSaveGame(this.server.SaveGameData, this.reusableStream);
						ServerMain.Logger.Event("Offthread save of savegame done.");
						this.chunkthread.runOffThreadSaveNow = false;
					}
				}
			}
		}

		public override void OnFinalizeAssets()
		{
			this.server.SaveGameData.WillSave(this.reusableStream);
			this.server.SaveGameData.UpdateLandClaims(this.server.WorldMap.All);
			this.chunkthread.gameDatabase.StoreSaveGame(this.server.SaveGameData, this.reusableStream);
		}

		public override void OnBeginConfiguration()
		{
			this.chunkthread.gameDatabase = new GameDatabase(ServerMain.Logger);
			string errorMessage = null;
			bool existed = File.Exists(this.server.GetSaveFilename());
			bool isreadonly = false;
			int version;
			try
			{
				this.server.SaveGameData = this.chunkthread.gameDatabase.ProbeOpenConnection(this.server.GetSaveFilename(), true, out version, out errorMessage, out isreadonly, true);
			}
			catch (Exception e)
			{
				ServerMain.Logger.Fatal("Unable to open or create savegame.");
				ServerMain.Logger.Fatal(e);
				this.server.Stop("Failed opening savegame", null, EnumLogType.Notification);
				return;
			}
			if (this.server.SaveGameData == null && existed && this.server.Config.RepairMode)
			{
				this.chunkthread.gameDatabase.CloseConnection();
				this.chunkthread.gameDatabase.OpenConnection(this.server.GetSaveFilename(), GameVersion.DatabaseVersion, out errorMessage, true, this.server.Config.CorruptionProtection, this.server.Config.RepairMode);
				ServerMain.Logger.Fatal("Failed opening savegame data, possibly corrupted. We are in repair mode, so initializing new savegame data structure.", new object[] { errorMessage });
				this.server.SaveGameData = SaveGame.CreateNew(this.server.Config);
				this.server.SaveGameData.WorldType = "standard";
				this.server.SaveGameData.PlayStyle = "surviveandbuild";
				this.server.SaveGameData.PlayStyleLangCode = "preset-surviveandbuild";
				version = GameVersion.DatabaseVersion;
			}
			else if (this.server.Config.RepairMode)
			{
				this.chunkthread.gameDatabase.IntegrityCheck();
			}
			if (this.server.SaveGameData == null && existed)
			{
				this.server.SaveGameData = null;
				ServerMain.Logger.Fatal("Failed opening savegame, possibly corrupted. Error Message: {0}. Will exit server now.", new object[] { errorMessage });
				this.server.Stop("Failed opening savegame", null, EnumLogType.Notification);
				return;
			}
			if (isreadonly)
			{
				this.server.SaveGameData = null;
				this.chunkthread.gameDatabase.CloseConnection();
				ServerMain.Logger.Fatal("Failed opening savegame, have no write access to it. Make sure no other server is accessing it. Will exit server now.");
				this.server.Stop("Failed opening savegame, it is readonly", null, EnumLogType.Notification);
				return;
			}
			try
			{
				FileInfo f = new FileInfo(this.chunkthread.gameDatabase.DatabaseFilename);
				long freeSpaceBytes = ServerMain.xPlatInterface.GetFreeDiskSpace(f.DirectoryName);
				if (freeSpaceBytes >= 0L && freeSpaceBytes < (long)(1048576 * this.server.Config.DieBelowDiskSpaceMb))
				{
					string messsage = string.Format("Disk space is below {0} megabytes ({1} mb left). A full harddisk can heavily corrupt a savegame. Please free up more disk space or adjust the threshold in the serverconfig.json (or set to -1 to disable this check). Will kill server now...", this.server.Config.DieBelowDiskSpaceMb, freeSpaceBytes / 1024L / 1024L);
					ServerMain.Logger.Fatal(messsage);
					throw new Exception(messsage);
				}
			}
			catch (ArgumentException)
			{
				ServerMain.Logger.Warning("Exception thrown when trying to check for available disk space. Please manually verify that your hard disk won't run full to avoid savegame corruption");
			}
			if (version != GameVersion.DatabaseVersion)
			{
				this.chunkthread.gameDatabase.CloseConnection();
				ServerMain.Logger.Event("Old savegame database version detected, will upgrade now...");
				DatabaseUpgrader upgrader = new DatabaseUpgrader(this.server, this.server.GetSaveFilename(), version, GameVersion.DatabaseVersion);
				try
				{
					upgrader.PerformUpgrade();
					this.chunkthread.gameDatabase.OpenConnection(this.server.GetSaveFilename(), true, true);
					this.server.SaveGameData = null;
				}
				catch (Exception e2)
				{
					ServerMain.Logger.Event("Failed upgrading old savegame, giving up, sorry.");
					throw new InvalidDataException("Failed upgrading savegame {0}", e2);
				}
			}
			this.chunkthread.gameDatabase.UpgradeToWriteAccess();
			this.server.ModEventManager.OnWorldgenStartup += this.OnWorldgenStartup;
			this.LoadSaveGame();
		}

		public override void OnBeginModsAndConfigReady()
		{
			if (this.server.SaveGameData.IsNewWorld)
			{
				this.server.ModEventManager.TriggerSaveGameCreated();
			}
			this.server.EventManager.TriggerSaveGameLoaded();
			this.server.WorldMap.chunkIlluminatorWorldGen.chunkProvider = this.chunkthread;
			this.chunkthread.worldgenBlockAccessor = this.GetBlockAccessor(false);
			foreach (WorldGenThreadDelegate worldGenThreadDelegate in this.server.ModEventManager.WorldgenBlockAccessor)
			{
				worldGenThreadDelegate(this);
			}
		}

		public void OnWorldgenStartup()
		{
			this.chunkthread.loadsavechunks.InitWorldgenAndSpawnChunks();
		}

		public override void OnBeginRunGame()
		{
			this.server.EventManager.OnGameWorldBeingSaved += this.OnWorldBeingSaved;
		}

		public override void OnBeginShutdown()
		{
			if (this.server.Saving)
			{
				ServerMain.Logger.Error("Server was saving and a shutdown has begun? Waiting 10 secs before doing save-on-shutdown");
				Thread.Sleep(10000);
			}
			this.server.Saving = true;
			if (this.server.SaveGameData != null)
			{
				this.server.SaveGameData.TotalSecondsPlayed += (int)(this.server.ElapsedMilliseconds / 1000L);
				this.server.EventManager.TriggerGameWorldBeingSaved();
			}
			this.server.Saving = false;
		}

		public override void OnSeperateThreadShutDown()
		{
			this.chunkthread.gameDatabase.Dispose();
		}

		public void OnWorldBeingSaved()
		{
			bool saveLater = this.server.RunPhase != EnumServerRunPhase.Shutdown;
			if (this.ignoreSave)
			{
				return;
			}
			try
			{
				FileInfo file = new FileInfo(this.chunkthread.gameDatabase.DatabaseFilename);
				long freeSpaceBytes = ServerMain.xPlatInterface.GetFreeDiskSpace(file.DirectoryName);
				long maxSpaceInBytes = (long)(1048576 * this.server.Config.DieBelowDiskSpaceMb);
				if (freeSpaceBytes >= 0L)
				{
					if (freeSpaceBytes >= maxSpaceInBytes && freeSpaceBytes < maxSpaceInBytes * 2L)
					{
						ServerMain.Logger.Warning("Disk space is getting close to configured server shutdown level. Please free up more disk space or adjust the threshold in the serverconfig.json.");
					}
					else if (freeSpaceBytes < maxSpaceInBytes)
					{
						string messsage = string.Format("Disk space is below {0} megabytes ({1} mb left). A full harddisk can heavily corrupt a savegame. Please free up more disk space or adjust the threshold in the serverconfig.json (or set to -1 to disable this check). Will kill server now...", this.server.Config.DieBelowDiskSpaceMb, freeSpaceBytes / 1024L / 1024L);
						ServerMain.Logger.Fatal(messsage);
						this.ignoreSave = true;
						this.server.Stop("Out of disk space", null, EnumLogType.Notification);
						return;
					}
				}
			}
			catch (ArgumentException)
			{
				ServerMain.Logger.Warning("Exception thrown when trying to check for available disk space. Please manually verify that your hard disk won't run full to avoid savegame corruption");
			}
			if (saveLater && this.chunkthread.runOffThreadSaveNow)
			{
				ServerMain.Logger.Fatal("Already saving, will ignore save this time");
				return;
			}
			object obj = this.savingLock;
			lock (obj)
			{
				this.SaveGameWorld(saveLater);
			}
		}

		private void LoadSaveGame()
		{
			string saveFileName = this.server.GetSaveFilename();
			ServerMain.Logger.Notification("Loading savegame");
			if (!File.Exists(saveFileName))
			{
				ServerMain.Logger.Notification("No savegame file found, creating new one");
			}
			if (this.server.SaveGameData == null)
			{
				this.server.SaveGameData = this.chunkthread.gameDatabase.GetSaveGame();
			}
			if (this.server.SaveGameData == null)
			{
				this.server.SaveGameData = SaveGame.CreateNew(this.server.Config);
				this.server.SaveGameData.WillSave(this.reusableStream);
				this.chunkthread.gameDatabase.StoreSaveGame(this.server.SaveGameData, this.reusableStream);
				this.server.EventManager.TriggerSaveGameCreated();
				ServerMain.Logger.Notification("Create new save game data. Playstyle: {0}", new object[] { this.server.SaveGameData.PlayStyle });
				if (!this.server.Standalone)
				{
					ServerMain.Logger.Notification("Default spawn was set in serverconfig, resetting for safety.");
					this.server.Config.DefaultSpawn = null;
					this.server.ConfigNeedsSaving = true;
				}
			}
			else
			{
				if (this.server.PlayerDataManager.WorldDataByUID == null)
				{
					this.server.PlayerDataManager.WorldDataByUID = new Dictionary<string, ServerWorldPlayerData>();
				}
				this.server.SaveGameData.Init(this.server);
				if (this.server.SaveGameData.PlayerDataByUID != null)
				{
					ServerMain.Logger.Notification("Transferring player data to new db table...");
					foreach (KeyValuePair<string, ServerWorldPlayerData> val in this.server.SaveGameData.PlayerDataByUID)
					{
						this.server.PlayerDataManager.WorldDataByUID[val.Key] = val.Value;
						val.Value.Init(this.server);
					}
					this.server.SaveGameData.PlayerDataByUID = null;
				}
				if (ServerSystemLoadAndSaveGame.SetDefaultSpawnOnce != null)
				{
					this.server.SaveGameData.DefaultSpawn = ServerSystemLoadAndSaveGame.SetDefaultSpawnOnce;
					ServerSystemLoadAndSaveGame.SetDefaultSpawnOnce = null;
				}
				this.server.SaveGameData.IsNewWorld = false;
				ServerMain.Logger.Notification("Loaded existing save game data. Playstyle: {0}, Playstyle Lang code: {1}, WorldType: {1}", new object[]
				{
					this.server.SaveGameData.PlayStyle,
					this.server.SaveGameData.PlayStyleLangCode,
					this.server.SaveGameData.WorldType
				});
			}
			this.server.WorldMap.Init(this.server.SaveGameData.MapSizeX, this.server.SaveGameData.MapSizeY, this.server.SaveGameData.MapSizeZ);
			int worldgenTotalThreads = Math.Max(1, Math.Min(6, MagicNum.MaxWorldgenThreads)) + 1;
			if (this.server.ReducedServerThreads)
			{
				worldgenTotalThreads = 1;
			}
			this.chunkthread.requestedChunkColumns = new ConcurrentIndexedFifoQueue<ChunkColumnLoadRequest>(MagicNum.RequestChunkColumnsQueueSize, worldgenTotalThreads);
			this.chunkthread.peekingChunkColumns = new IndexedFifoQueue<ChunkColumnLoadRequest>(MagicNum.RequestChunkColumnsQueueSize / 5);
			ServerMain.Logger.Notification("Savegame {0} loaded", new object[] { StringUtil.SanitizePath(saveFileName) });
			ServerMain.Logger.Notification("World size = {0} {1} {2}", new object[]
			{
				this.server.SaveGameData.MapSizeX,
				this.server.SaveGameData.MapSizeY,
				this.server.SaveGameData.MapSizeZ
			});
		}

		private void SaveGameWorld(bool saveLater = false)
		{
			if (!saveLater)
			{
				this.chunkthread.runOffThreadSaveNow = false;
			}
			if (ServerMain.FrameProfiler == null)
			{
				ServerMain.FrameProfiler = new FrameProfilerUtil(delegate(string text)
				{
					ServerMain.Logger.Notification(text);
				});
				ServerMain.FrameProfiler.Begin(null, Array.Empty<object>());
			}
			ServerMain.FrameProfiler.Mark("savegameworld-begin");
			ServerMain.Logger.Event("Mods and systems notified, now saving everything...");
			ServerMain.Logger.StoryEvent(Lang.Get("It pauses.", Array.Empty<object>()));
			this.server.SaveGameData.WillSave(this.reusableStream);
			if (saveLater)
			{
				ServerMain.Logger.Event("Will do offthread savegamedata saving...");
			}
			ServerMain.FrameProfiler.Mark("savegameworld-mid-1");
			ServerMain.Logger.StoryEvent(Lang.Get("One last gaze...", Array.Empty<object>()));
			foreach (ServerWorldPlayerData plrdata in this.server.PlayerDataManager.WorldDataByUID.Values)
			{
				try
				{
					plrdata.BeforeSerialization();
					this.chunkthread.gameDatabase.SetPlayerData(plrdata.PlayerUID, SerializerUtil.Serialize<ServerWorldPlayerData>(plrdata, this.reusableStream));
				}
				catch (Exception e)
				{
					ServerMain.Logger.Error("Failed to save player " + plrdata.PlayerUID);
					ServerMain.Logger.Error(e);
				}
			}
			ServerMain.FrameProfiler.Mark("savegameworld-mid-2");
			ServerMain.Logger.Event("Saved player world data...");
			int dirtyMapRegions = this.SaveAllDirtyMapRegions(this.reusableStream);
			ServerMain.FrameProfiler.Mark("savegameworld-mid-3");
			ServerMain.Logger.Event("Saved map regions...");
			ServerMain.Logger.StoryEvent(Lang.Get("...then all goes quiet", Array.Empty<object>()));
			int dirtyMapChunks = 0;
			if (!saveLater)
			{
				dirtyMapChunks = this.SaveAllDirtyMapChunks(this.reusableStream);
				ServerMain.FrameProfiler.Mark("savegameworld-mid-4");
			}
			ServerMain.Logger.Event("Saved map chunks...");
			ServerMain.Logger.StoryEvent(Lang.Get("The waters recede...", Array.Empty<object>()));
			ServerMain.FrameProfiler.Mark("savegameworld-mid-5");
			int dirtyChunks = 0;
			if (saveLater)
			{
				this.PopulateChunksCopy();
				this.chunkthread.runOffThreadSaveNow = true;
			}
			else
			{
				dirtyChunks = this.SaveAllDirtyLoadedChunks(false, this.reusableStream);
				ServerMain.Logger.Event("Saved loaded chunks...");
				ServerMain.Logger.StoryEvent(Lang.Get("The mountains fade...", Array.Empty<object>()));
				ServerMain.Logger.StoryEvent(Lang.Get("The dark settles in.", Array.Empty<object>()));
				dirtyChunks += this.SaveAllDirtyGeneratingChunks(this.reusableStream);
				ServerMain.Logger.Event("Saved generating chunks...");
				this.server.SaveGameData.UpdateLandClaims(this.server.WorldMap.All);
				this.chunkthread.gameDatabase.StoreSaveGame(this.server.SaveGameData, this.reusableStream);
				ServerMain.Logger.Event("Saved savegamedata..." + this.server.SaveGameData.HighestChunkdataVersion.ToString());
			}
			ServerMain.Logger.Event("World saved! Saved {0} chunks, {1} mapchunks, {2} mapregions.", new object[] { dirtyChunks, dirtyMapChunks, dirtyMapRegions });
			ServerMain.Logger.StoryEvent(Lang.Get("It sighs...", Array.Empty<object>()));
			ServerMain.FrameProfiler.Mark("savegameworld-end");
		}

		private int SaveAllDirtyMapRegions(FastMemoryStream ms)
		{
			int dirty = 0;
			List<DbChunk> dirtyMapRegions = new List<DbChunk>();
			foreach (KeyValuePair<long, ServerMapRegion> val in this.server.loadedMapRegions)
			{
				if (val.Value.DirtyForSaving)
				{
					val.Value.DirtyForSaving = false;
					dirty++;
					dirtyMapRegions.Add(new DbChunk
					{
						Position = this.server.WorldMap.MapRegionPosFromIndex2D(val.Key),
						Data = val.Value.ToBytes(ms)
					});
				}
			}
			this.chunkthread.gameDatabase.SetMapRegions(dirtyMapRegions);
			return dirty;
		}

		private int SaveAllDirtyMapChunks(FastMemoryStream ms)
		{
			int dirty = 0;
			List<DbChunk> dirtyMapChunks = new List<DbChunk>();
			foreach (KeyValuePair<long, ServerMapChunk> val in this.server.loadedMapChunks)
			{
				if (val.Value.DirtyForSaving)
				{
					val.Value.DirtyForSaving = false;
					ChunkPos pos = this.server.WorldMap.ChunkPosFromChunkIndex2D(val.Key);
					dirty++;
					dirtyMapChunks.Add(new DbChunk
					{
						Position = pos,
						Data = val.Value.ToBytes(ms)
					});
					if (dirtyMapChunks.Count > 200)
					{
						this.chunkthread.gameDatabase.SetMapChunks(dirtyMapChunks);
						dirtyMapChunks.Clear();
					}
				}
			}
			this.chunkthread.gameDatabase.SetMapChunks(dirtyMapChunks);
			return dirty;
		}

		internal int SaveAllDirtyLoadedChunks(bool isSaveLater, FastMemoryStream ms)
		{
			int dirty = 0;
			List<DbChunk> dirtyChunks = new List<DbChunk>();
			if (!isSaveLater)
			{
				this.PopulateChunksCopy();
			}
			foreach (KeyValuePair<long, ServerChunk> val in this.chunksCopy)
			{
				if (val.Value.DirtyForSaving)
				{
					val.Value.DirtyForSaving = false;
					ChunkPos vec = this.server.WorldMap.ChunkPosFromChunkIndex3D(val.Key);
					dirtyChunks.Add(new DbChunk
					{
						Position = vec,
						Data = val.Value.ToBytes(ms)
					});
					dirty++;
					if (dirtyChunks.Count > 300)
					{
						this.chunkthread.gameDatabase.SetChunks(dirtyChunks);
						dirtyChunks.Clear();
					}
					if (dirty > 0 && dirty % 300 == 0)
					{
						ServerMain.Logger.Event("Saved {0} chunks...", new object[] { dirty });
					}
				}
			}
			this.chunkthread.gameDatabase.SetChunks(dirtyChunks);
			if (dirty > 0)
			{
				this.server.SaveGameData.UpdateChunkdataVersion();
			}
			return dirty;
		}

		private void PopulateChunksCopy()
		{
			this.chunksCopy.Clear();
			this.server.loadedChunksLock.AcquireReadLock();
			try
			{
				foreach (KeyValuePair<long, ServerChunk> val in this.server.loadedChunks)
				{
					this.chunksCopy[val.Key] = val.Value;
				}
			}
			finally
			{
				this.server.loadedChunksLock.ReleaseReadLock();
			}
		}

		internal int SaveAllDirtyGeneratingChunks(FastMemoryStream ms)
		{
			int dirty = 0;
			List<DbChunk> dirtyChunks = new List<DbChunk>();
			if (this.chunkthread.requestedChunkColumns.Count > 0)
			{
				foreach (ChunkColumnLoadRequest request in this.chunkthread.requestedChunkColumns.Snapshot())
				{
					if (request.Chunks != null && !request.Disposed && request.CurrentIncompletePass > EnumWorldGenPass.Terrain)
					{
						request.generatingLock.AcquireReadLock();
						try
						{
							for (int y = 0; y < request.Chunks.Length; y++)
							{
								if (request.Chunks[y].DirtyForSaving)
								{
									request.Chunks[y].DirtyForSaving = false;
									dirtyChunks.Add(new DbChunk
									{
										Position = new ChunkPos(request.chunkX, y, request.chunkZ, 0),
										Data = request.Chunks[y].ToBytes(ms)
									});
									dirty++;
									if (dirty > 0 && dirty % 300 == 0)
									{
										ServerMain.Logger.Event("Saved {0} generating chunks...", new object[] { dirty });
										ServerMain.Logger.StoryEvent("...");
									}
								}
							}
						}
						finally
						{
							request.generatingLock.ReleaseReadLock();
						}
						if (dirtyChunks.Count > 300)
						{
							this.chunkthread.gameDatabase.SetChunks(dirtyChunks);
							dirtyChunks.Clear();
						}
					}
				}
				this.chunkthread.gameDatabase.SetChunks(dirtyChunks);
			}
			if (dirty > 0)
			{
				this.server.SaveGameData.UpdateChunkdataVersion();
			}
			return dirty;
		}

		public IWorldGenBlockAccessor GetBlockAccessor(bool updateHeightmap)
		{
			if (updateHeightmap)
			{
				if (this.blockAccessorWGUpdateHeightMap == null)
				{
					this.blockAccessorWGUpdateHeightMap = new BlockAccessorWorldGenUpdateHeightmap(this.server, this.chunkthread);
				}
				return this.blockAccessorWGUpdateHeightMap;
			}
			if (this.blockAccessorWG == null)
			{
				this.blockAccessorWG = new BlockAccessorWorldGen(this.server, this.chunkthread);
			}
			return this.blockAccessorWG;
		}

		private ChunkServerThread chunkthread;

		private object savingLock = new object();

		[ThreadStatic]
		private static FastMemoryStream reusableMemoryStream;

		private BlockAccessorWorldGen blockAccessorWG;

		private BlockAccessorWorldGenUpdateHeightmap blockAccessorWGUpdateHeightMap;

		private Dictionary<long, ServerChunk> chunksCopy = new Dictionary<long, ServerChunk>();

		private bool ignoreSave;

		internal static PlayerSpawnPos SetDefaultSpawnOnce;
	}
}
