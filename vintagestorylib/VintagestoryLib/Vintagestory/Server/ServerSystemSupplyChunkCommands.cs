using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ProtoBuf;
using SkiaSharp;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.Common.Database;

namespace Vintagestory.Server
{
	internal class ServerSystemSupplyChunkCommands : ServerSystem
	{
		public ServerSystemSupplyChunkCommands(ServerMain server, ChunkServerThread chunkthread)
			: base(server)
		{
			this.chunkthread = chunkthread;
			server.api.ChatCommands.GetOrCreate("chunk").BeginSub("cit").WithDescription("Chunk information from the supply chunks thread")
				.WithArgs(new ICommandArgumentParser[] { server.api.ChatCommands.Parsers.OptionalWord("perf") })
				.HandleWith(new OnCommandDelegate(this.OnChunkInfoCmd))
				.EndSub()
				.BeginSub("printmap")
				.WithDescription("Export a png file of a map of loaded chunks. Marks call location with a yellow pixel")
				.HandleWith(new OnCommandDelegate(this.OnChunkMap))
				.EndSub();
		}

		private TextCommandResult OnChunkMap(TextCommandCallingArgs args)
		{
			string filename = this.PrintServerChunkMap(new Vec2i(args.Caller.Pos.XInt / 32, args.Caller.Pos.ZInt / 32));
			return TextCommandResult.Success("map " + filename + " generated", null);
		}

		public override void OnBeginModsAndConfigReady()
		{
			base.OnBeginModsAndConfigReady();
			CommandArgumentParsers parsers = this.server.api.ChatCommands.Parsers;
			IChatCommand dbcmd = this.server.api.ChatCommands.GetOrCreate("db").RequiresPrivilege(Privilege.controlserver).WithDesc("Save-game related commands");
			if (!this.server.Config.HostedMode)
			{
				dbcmd.BeginSub("backup").WithDesc("Creates a copy of the current save game in the Backups folder").WithArgs(new ICommandArgumentParser[] { parsers.OptionalWord("filename") })
					.HandleWith(new OnCommandDelegate(this.onCmdGenBackup))
					.WithRootAlias("genbackup")
					.EndSub()
					.BeginSub("vacuum")
					.WithDesc("Repack save game to minimize its file size")
					.HandleWith(new OnCommandDelegate(this.onCmdVacuum))
					.EndSub();
			}
			else
			{
				dbcmd.WithAdditionalInformation("(/db backup and /db vacuum sub-commands are not available for hosted servers, sorry)");
			}
			dbcmd.BeginSub("prune").WithDesc("Delete all unchanged or hardly changed chunks, with changes below a specified threshold. Chunks with claims can be protected.").WithAdditionalInformation("'Changes' refers to edits by players, counted separately in each 32x32 chunk in the world. The number of edits is the count of blocks of any kind placed or broken by any player in either Survival or Creative modes. Breaking grass or leaves is counted, harvesting berries or collecting sticks is not counted. Only player actions since game version 1.18.0 (April 2023) are counted. Chunks with land claims of any size, even a single block, can be protected using the 'keep' option. The 'keep' option will preserve all trader caravans and the Resonance Archives.\n\nPruned chunks are fully deleted and destroyed and, when next visited, will be regenerated with up-to-date worldgen from the current game version, including new vegetation and ruins. Bodies of water, general terrain shape and climate conditions will be unchanged or almost unchanged. Ore presence in each chunk will be similar as before, may be in slightly different positions.\n\nWithout the 'confirm' arg, does a dry-run only! If mods or worldconfig have changed since the world was first created, or if the map was first created in game version 1.17 or earlier, results of a prune may be unpredictable or chunk borders may become visible, a backup first is advisable. This command is irreversible, use with care!")
				.WithArgs(new ICommandArgumentParser[]
				{
					parsers.Int("threshold"),
					parsers.WordRange("choice whether to protect (keep) all chunks which have land claims", new string[] { "keep", "drop" }),
					parsers.OptionalWordRange("confirm flag", new string[] { "confirm", "dryrun" }),
					parsers.OptionalWord("(optionally) the minimum version to re-do. For older worldgen first made before this minimum version, ALL chunks will be preserved.")
				})
				.HandleWith(new OnCommandDelegate(this.onCmdPrune))
				.EndSub()
				.Validate();
		}

		private TextCommandResult onCmdVacuum(TextCommandCallingArgs args)
		{
			IServerPlayer logToPlayer = args.Caller.Player as IServerPlayer;
			this.processInBackground(new Action(this.chunkthread.gameDatabase.Vacuum), delegate
			{
				this.notifyIndirect(logToPlayer, Lang.Get("Vacuum complete!", Array.Empty<object>()));
			});
			return TextCommandResult.Success(Lang.Get("Vacuum started, this may take some time", Array.Empty<object>()), null);
		}

		private TextCommandResult onCmdPrune(TextCommandCallingArgs args)
		{
			IServerPlayer logToPlayer = args.Caller.Player as IServerPlayer;
			int threshold = (int)args[0];
			bool keepClaims = (string)args[1] == "keep";
			bool dryRun = (string)args[2] != "confirm";
			string aboveVersion = (args.Parsers[3].IsMissing ? null : ((string)args[3]));
			return this.prune(logToPlayer, threshold, dryRun, keepClaims, aboveVersion);
		}

		private TextCommandResult prune(IServerPlayer logToPlayer, int threshold, bool dryRun, bool keepClaims, string aboveVersion)
		{
			int qBelowThreshold = 0;
			HashSet<FastVec2i> toDelete = new HashSet<FastVec2i>();
			HashSet<FastVec2i> toKeep = new HashSet<FastVec2i>();
			List<LandClaim> claims = this.server.WorldMap.All;
			HorRectanglei rect = new HorRectanglei();
			int chunksize = this.server.api.worldapi.ChunkSize;
			Action<DbChunk> <>9__1;
			Action <>9__2;
			this.processInBackground(delegate
			{
				GameDatabase gameDatabase = this.chunkthread.gameDatabase;
				Action<DbChunk> action;
				if ((action = <>9__1) == null)
				{
					action = (<>9__1 = delegate(DbChunk schunk)
					{
						try
						{
							if (keepClaims)
							{
								bool keep = false;
								rect.X1 = schunk.Position.X * chunksize;
								rect.Z1 = schunk.Position.Z * chunksize;
								rect.X2 = schunk.Position.X * chunksize + chunksize;
								rect.Z2 = schunk.Position.Z * chunksize + chunksize;
								foreach (LandClaim claim in claims)
								{
									if (claim != null && claim.Intersects2d(rect))
									{
										keep = true;
										break;
									}
								}
								if (keep)
								{
									toKeep.Add(new FastVec2i(schunk.Position.X, schunk.Position.Z));
									return;
								}
							}
							bool keepChunk = false;
							if (threshold > 0 || aboveVersion != null)
							{
								ServerChunk serverchunk;
								using (MemoryStream ms = new MemoryStream(schunk.Data))
								{
									serverchunk = Serializer.Deserialize<ServerChunk>(ms);
								}
								keepChunk = serverchunk.BlocksRemoved + serverchunk.BlocksPlaced >= threshold && threshold > 0;
								if (aboveVersion != null && GameVersion.IsLowerVersionThan(serverchunk.GameVersionCreated, aboveVersion))
								{
									keepChunk = true;
								}
							}
							if (keepChunk)
							{
								toKeep.Add(new FastVec2i(schunk.Position.X, schunk.Position.Z));
							}
							else
							{
								int qBelowThreshold2 = qBelowThreshold;
								qBelowThreshold = qBelowThreshold2 + 1;
								toDelete.Add(new FastVec2i(schunk.Position.X, schunk.Position.Z));
							}
						}
						catch (Exception)
						{
							throw;
						}
					});
				}
				gameDatabase.ForAllChunks(action);
				foreach (FastVec2i pos2d in toKeep)
				{
					toDelete.Remove(pos2d);
				}
				ServerMain server = this.server;
				Action action2;
				if ((action2 = <>9__2) == null)
				{
					action2 = (<>9__2 = delegate
					{
						if (dryRun)
						{
							this.notifyIndirect(logToPlayer, Lang.Get("Dry run prune complete. With a {0} block edits threshold, {1} chunk columns can be removed, {2} chunk columns would be kept.", new object[] { threshold, toDelete.Count, toKeep.Count }));
							return;
						}
						this.server.api.Server.PauseThread("chunkdbthread", 5000);
						int regionSize = this.server.api.worldapi.RegionSize / chunksize;
						Cuboidi chunkRect = new Cuboidi();
						Dictionary<long, ServerMapRegion> regions = new Dictionary<long, ServerMapRegion>(10);
						Queue<long> regionsQueue = new Queue<long>(10);
						FastMemoryStream ms = new FastMemoryStream();
						foreach (FastVec2i pos2d2 in toDelete)
						{
							int regionX = pos2d2.X / regionSize;
							int regionZ = pos2d2.Y / regionSize;
							ServerMapRegion mapRegion = this.server.WorldMap.GetMapRegion(regionX, regionZ) as ServerMapRegion;
							long mapRegionIndex2d = this.server.WorldMap.MapRegionIndex2D(regionX, regionZ);
							if (mapRegion == null && !regions.TryGetValue(mapRegionIndex2d, out mapRegion))
							{
								byte[] mapRegionBytes = this.chunkthread.gameDatabase.GetMapRegion(regionX, regionZ);
								if (mapRegionBytes != null)
								{
									if (regionsQueue.Count >= 9)
									{
										long oldestIndex = regionsQueue.Dequeue();
										ServerMapRegion oldest = regions[oldestIndex];
										DbChunk dbchunk = new DbChunk
										{
											Position = this.server.WorldMap.MapRegionPosFromIndex2D(oldestIndex),
											Data = oldest.ToBytes(ms)
										};
										this.chunkthread.gameDatabase.SetMapRegions(new List<DbChunk> { dbchunk });
										regions.Remove(oldestIndex);
									}
									mapRegion = (regions[mapRegionIndex2d] = ServerMapRegion.FromBytes(mapRegionBytes));
									regionsQueue.Enqueue(mapRegionIndex2d);
								}
							}
							List<GeneratedStructure> structuresToDelete = new List<GeneratedStructure>();
							chunkRect.X1 = pos2d2.X * chunksize;
							chunkRect.Z1 = pos2d2.Y * chunksize;
							chunkRect.X2 = pos2d2.X * chunksize + chunksize;
							chunkRect.Z2 = pos2d2.Y * chunksize + chunksize;
							if (((mapRegion != null) ? mapRegion.GeneratedStructures : null) != null)
							{
								foreach (GeneratedStructure structure in mapRegion.GeneratedStructures)
								{
									if (chunkRect.Contains(structure.Location.Start.X, structure.Location.Start.Z))
									{
										structuresToDelete.Add(structure);
									}
								}
								foreach (GeneratedStructure structure2 in structuresToDelete)
								{
									mapRegion.GeneratedStructures.Remove(structure2);
								}
							}
							this.server.api.WorldManager.DeleteChunkColumn(pos2d2.X, pos2d2.Y);
						}
						this.chunkthread.gameDatabase.SetMapRegions(regions.Select((KeyValuePair<long, ServerMapRegion> r) => new DbChunk
						{
							Position = this.server.WorldMap.MapRegionPosFromIndex2D(r.Key),
							Data = r.Value.ToBytes(ms)
						}));
						this.server.api.Server.ResumeThread("chunkdbthread");
						this.notifyIndirect(logToPlayer, Lang.Get("Prune complete, {1} chunk columns were removed, {2} chunk columns were kept.", new object[] { threshold, toDelete.Count, toKeep.Count }));
					});
				}
				server.EnqueueMainThreadTask(action2);
			}, null);
			return TextCommandResult.Success(dryRun ? Lang.Get("Dry run prune started, this may take some time.", Array.Empty<object>()) : Lang.Get("Prune started, this may take some time.", Array.Empty<object>()), null);
		}

		private TextCommandResult onCmdGenBackup(TextCommandCallingArgs args)
		{
			if (this.server.Config.HostedMode)
			{
				return TextCommandResult.Error(Lang.Get("Can't access this feature, server is in hosted mode", Array.Empty<object>()), "");
			}
			this.backupFileName = (args.Parsers[0].IsMissing ? null : Path.GetFileName(args[0] as string));
			this.GenBackup(args.Caller.Player as IServerPlayer);
			return TextCommandResult.Success(Lang.Get("Ok, generating backup, this might take a while", Array.Empty<object>()), null);
		}

		private void GenBackup(IServerPlayer logToPlayer = null)
		{
			if (this.chunkthread.BackupInProgress)
			{
				this.notifyIndirect(logToPlayer, Lang.Get("Can't run backup. A backup is already in progress", Array.Empty<object>()));
				return;
			}
			this.chunkthread.BackupInProgress = true;
			if (this.backupFileName == null || this.backupFileName.Length == 0 || this.backupFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
			{
				string filename = Path.GetFileName(this.server.Config.WorldConfig.SaveFileLocation).Replace(".vcdbs", "");
				if (filename.Length == 0)
				{
					filename = "world";
				}
				this.backupFileName = filename + "-" + string.Format("{0:yyyy-MM-dd_HH-mm-ss}", DateTime.Now) + ".vcdbs";
			}
			this.processInBackground(delegate
			{
				this.chunkthread.gameDatabase.CreateBackup(this.backupFileName);
			}, delegate
			{
				this.chunkthread.BackupInProgress = false;
				string msg = Lang.Get("Backup complete!", Array.Empty<object>());
				this.notifyIndirect(logToPlayer, msg);
			});
		}

		private void processInBackground(Action backgroundProc, Action onDoneOnMainthread)
		{
			Action <>9__1;
			TyronThreadPool.QueueLongDurationTask(delegate
			{
				backgroundProc();
				ServerMain server = this.server;
				Action action;
				if ((action = <>9__1) == null)
				{
					action = (<>9__1 = delegate
					{
						Action onDoneOnMainthread2 = onDoneOnMainthread;
						if (onDoneOnMainthread2 == null)
						{
							return;
						}
						onDoneOnMainthread2();
					});
				}
				server.EnqueueMainThreadTask(action);
			}, "supplychunkcommand");
		}

		private void notifyIndirect(IServerPlayer logToPlayer, string msg)
		{
			if (logToPlayer != null)
			{
				logToPlayer.SendMessage(this.server.IsDedicatedServer ? GlobalConstants.ServerInfoChatGroup : GlobalConstants.GeneralChatGroup, msg, EnumChatType.CommandSuccess, "backupdone");
				return;
			}
			ServerMain.Logger.Notification(msg);
		}

		private TextCommandResult OnChunkInfoCmd(TextCommandCallingArgs args)
		{
			if ((string)args[0] == "perf")
			{
				StringBuilder sb = new StringBuilder();
				for (int i = 0; i < 20; i++)
				{
					sb.AppendLine(ServerSystemSendChunks.performanceTest(this.server));
				}
				return TextCommandResult.Success(sb.ToString(), null);
			}
			BlockPos asBlockPos = args.Caller.Pos.AsBlockPos;
			int chunkX = asBlockPos.X / 32;
			int chunkY = asBlockPos.Y / 32;
			int chunkZ = asBlockPos.Z / 32;
			long index2d = this.server.WorldMap.MapChunkIndex2D(chunkX, chunkZ);
			ServerChunk chunk = this.chunkthread.GetGeneratingChunk(chunkX, chunkY, chunkZ);
			ChunkColumnLoadRequest chunkReq = this.chunkthread.requestedChunkColumns.GetByIndex(index2d);
			if (chunkReq != null)
			{
				return TextCommandResult.Success(string.Format("Chunk in genQ: {0}, chunkReq in Q: {1}, currentPass: {2}, untilPass: {3}", new object[]
				{
					chunk != null,
					chunkReq != null,
					chunkReq.CurrentIncompletePass,
					chunkReq.GenerateUntilPass
				}), null);
			}
			return TextCommandResult.Success(string.Format("Chunk in genQ: {0}, chunkReq in Q: {1}", chunk != null, chunkReq != null), null);
		}

		public string PrintServerChunkMap(Vec2i markChunkPos = null)
		{
			ChunkPos minPos = new ChunkPos(int.MaxValue, 0, int.MaxValue, 0);
			ChunkPos maxPos = new ChunkPos(0, 0, 0, 0);
			this.server.loadedChunksLock.AcquireReadLock();
			try
			{
				foreach (long index3d in this.server.loadedChunks.Keys)
				{
					ChunkPos vec = this.server.WorldMap.ChunkPosFromChunkIndex3D(index3d);
					if (vec.Dimension <= 0)
					{
						minPos.X = Math.Min(minPos.X, vec.X);
						minPos.Z = Math.Min(minPos.Z, vec.Z);
						maxPos.X = Math.Max(maxPos.X, vec.X);
						maxPos.Z = Math.Max(maxPos.Z, vec.Z);
					}
				}
			}
			finally
			{
				this.server.loadedChunksLock.ReleaseReadLock();
			}
			if (minPos.X == 2147483647)
			{
				return "";
			}
			int num = maxPos.X - minPos.X;
			int sizeZ = maxPos.Z - minPos.Z;
			SKBitmap bmp = new SKBitmap(num + 1, sizeZ + 1, false);
			this.server.loadedChunksLock.AcquireReadLock();
			try
			{
				foreach (long index3d2 in this.server.loadedChunks.Keys)
				{
					ChunkPos vec2 = this.server.WorldMap.ChunkPosFromChunkIndex3D(index3d2);
					if (vec2.Dimension <= 0)
					{
						bmp.SetPixel(vec2.X - minPos.X, vec2.Z - minPos.Z, new SKColor(0, byte.MaxValue, 0, byte.MaxValue));
					}
				}
			}
			finally
			{
				this.server.loadedChunksLock.ReleaseReadLock();
			}
			foreach (ChunkColumnLoadRequest req in this.chunkthread.requestedChunkColumns.Snapshot())
			{
				if (req != null && !req.Disposed)
				{
					if (req.Chunks == null)
					{
						bmp.SetPixel(req.chunkX, req.chunkZ, new SKColor(20, 20, 20, byte.MaxValue));
					}
					else
					{
						int currentpass = req.CurrentIncompletePass_AsInt;
						SKColor c = new SKColor(5 + bmp.GetPixel(req.chunkX, req.chunkZ).Red, (byte)(currentpass * 30), (byte)(currentpass * 30), byte.MaxValue);
						bmp.SetPixel(req.chunkX - minPos.X, req.chunkZ - minPos.Z, c);
					}
				}
			}
			int i = 0;
			while (File.Exists("serverchunks" + i.ToString() + ".png"))
			{
				i++;
			}
			if (markChunkPos != null)
			{
				bmp.SetPixel(markChunkPos.X - minPos.X, markChunkPos.Y - minPos.Z, new SKColor(byte.MaxValue, 20, byte.MaxValue, byte.MaxValue));
			}
			bmp.Save("serverchunks" + i.ToString() + ".png");
			return "serverchunks" + i.ToString() + ".png";
		}

		private string backupFileName;

		private ChunkServerThread chunkthread;
	}
}
