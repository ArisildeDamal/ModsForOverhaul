using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.Server
{
	public class ServerSystemEntitySpawner : ServerSystem
	{
		public ServerSystemEntitySpawner(ServerMain server)
			: base(server)
		{
			this.multithreaded = !server.ReducedServerThreads;
		}

		public override void OnBeginModsAndConfigReady()
		{
			List<EntityProperties> entityTypes = this.server.EntityTypes;
			for (int i = 0; i < entityTypes.Count; i++)
			{
				EntityProperties entityType = entityTypes[i];
				this.entityTypesByCode[entityType.Code] = entityType;
			}
		}

		public override void OnBeginGameReady(SaveGame savegame)
		{
			base.OnBeginGameReady(savegame);
			this.cachingBlockAccessor = this.server.GetCachingBlockAccessor(false, false);
			if (!savegame.EntitySpawning)
			{
				return;
			}
			this.server.RegisterGameTickListener(new Action<float>(this.SpawnReadyMobs), 250, 0);
			if (this.multithreaded)
			{
				Thread thread = TyronThreadPool.CreateDedicatedThread(new ThreadStart(new SpawnerOffthread(this).Start), "physicsManagerHelper");
				thread.IsBackground = true;
				thread.Priority = Thread.CurrentThread.Priority;
				this.server.Serverthreads.Add(thread);
			}
			if (savegame.IsNewWorld || !savegame.ModData.ContainsKey("graceTimeUntilTotalDays"))
			{
				string @string = savegame.WorldConfiguration.GetString("graceTimer", "5");
				this.GraceTimeUntilTotalDays = 5.0;
				double.TryParse(@string, out this.GraceTimeUntilTotalDays);
				if (!savegame.IsNewWorld && !savegame.ModData.ContainsKey("graceTimeUntilTotalDays"))
				{
					int dayspm = Math.Max(1, savegame.WorldConfiguration.GetAsInt("daysPerMonth", 12));
					double daysStart = (double)(0.33333334f + (float)(dayspm * 4));
					this.GraceTimeUntilTotalDays += daysStart;
				}
				else
				{
					this.GraceTimeUntilTotalDays += this.server.Calendar.TotalDays;
				}
				savegame.ModData["graceTimeUntilTotalDays"] = SerializerUtil.Serialize<double>(this.GraceTimeUntilTotalDays);
			}
			else
			{
				this.GraceTimeUntilTotalDays = SerializerUtil.Deserialize<double>(savegame.ModData["graceTimeUntilTotalDays"]);
			}
			Dictionary<AssetLocation, Block[]> searchCache = new Dictionary<AssetLocation, Block[]>();
			foreach (EntityProperties type in this.server.EntityTypes)
			{
				SpawnConditions spawnConditions = type.Server.SpawnConditions;
				RuntimeSpawnConditions runtimeSpawnConditions = ((spawnConditions != null) ? spawnConditions.Runtime : null);
				if (runtimeSpawnConditions != null)
				{
					runtimeSpawnConditions.Initialise(this.server, type.Code.ToShortString(), searchCache);
				}
			}
		}

		public override void Dispose()
		{
			ICachingBlockAccessor cachingBlockAccessor = this.cachingBlockAccessor;
			if (cachingBlockAccessor != null)
			{
				cachingBlockAccessor.Dispose();
			}
			this.cachingBlockAccessor = null;
			this.readyToSpawn.Clear();
		}

		public override void OnServerPause()
		{
			this.paused = true;
		}

		public override void OnServerResume()
		{
			this.paused = false;
		}

		private void SpawnReadyMobs(float dt)
		{
			EntitySpawnerResult spawngroup;
			while (!this.readyToSpawn.IsEmpty && this.readyToSpawn.TryDequeue(out spawngroup))
			{
				spawngroup.Spawn(this.server, this);
			}
			this.PrepareForSpawning(dt);
		}

		internal void PrepareForSpawning(float dt)
		{
			this.slowaccum += dt;
			int day = (int)this.server.Calendar.TotalDays;
			double daysLeft = this.GraceTimeUntilTotalDays - this.server.Calendar.TotalDays + 0.25;
			if (this.GraceTimerDayNotify != day && daysLeft >= 0.0)
			{
				this.GraceTimerDayNotify = day;
				if ((int)daysLeft > 1)
				{
					this.server.SendMessageToGeneral(Lang.Get("server-xdaysleft", new object[] { (int)daysLeft }), EnumChatType.Notification, null, null);
				}
				if ((int)daysLeft == 1)
				{
					this.server.SendMessageToGeneral(Lang.Get("server-1dayleft", Array.Empty<object>()), EnumChatType.Notification, null, null);
				}
				if ((int)daysLeft == 0)
				{
					this.server.SendMessageToGeneral(Lang.Get("server-monsterbegins", Array.Empty<object>()), EnumChatType.Notification, null, null);
				}
			}
			if (this.first || this.slowaccum > 10f)
			{
				this.LoadViableSpawnAreas();
				this.slowaccum = 0f;
				this.first = false;
			}
			if (!this.multithreaded)
			{
				this.FindMobSpawnPositions_offthread(dt);
			}
		}

		internal void FindMobSpawnPositions_offthread(float dt)
		{
			if (this.server.Clients.IsEmpty)
			{
				return;
			}
			this.ReloadSpawnStates_offthread();
			this.cachingBlockAccessor.Begin();
			this.SeaLevel = this.server.SeaLevel;
			this.SkyHeight = this.server.WorldMap.MapSizeY - this.SeaLevel;
			this.errorLoggedThisTick = false;
			List<SpawnState> spawnStates = this.spawnStates;
			List<SpawnArea> spawnAreas = this.spawnAreas;
			for (int i = 0; i < spawnAreas.Count; i++)
			{
				SpawnArea spawnArea = spawnAreas[i];
				for (int j = 0; j < spawnArea.ChunkColumnCoords.Length; j++)
				{
					long num = spawnArea.ChunkColumnCoords[j];
					int x = (int)num;
					int z = (int)(num >> 32);
					this.TrySpawnSomethingAt_offthread(x, spawnArea.chunkY, z, spawnArea.spawnCounts, spawnStates);
				}
			}
		}

		private void TrySpawnSomethingAt_offthread(int baseX, int baseY, int baseZ, Dictionary<AssetLocation, int> spawnCounts, List<SpawnState> spawnStates)
		{
			ServerWorldMap WorldMap = this.server.WorldMap;
			IMapChunk mapchunk = WorldMap.GetMapChunk(baseX, baseZ);
			this.heightMap = ((mapchunk != null) ? mapchunk.WorldGenTerrainHeightMap : null);
			if (this.heightMap == null)
			{
				return;
			}
			IWorldChunk[] chunkCol = new IWorldChunk[WorldMap.ChunkMapSizeY];
			for (int cy = 0; cy < chunkCol.Length; cy++)
			{
				IWorldChunk chunk = WorldMap.GetChunk(baseX, cy, baseZ);
				if (chunk == null)
				{
					return;
				}
				chunkCol[cy] = chunk;
				chunk.Unpack_ReadOnly();
				chunk.AcquireBlockReadLock();
			}
			try
			{
				this.TrySpawnSomethingAt_offthrad(baseX, baseY, baseZ, spawnCounts, chunkCol, spawnStates);
			}
			catch (Exception e)
			{
				if (!this.errorLoggedThisTick)
				{
					this.errorLoggedThisTick = true;
					this.server.World.Logger.Warning(string.Concat(new string[]
					{
						"Error when testing to spawn entities at ",
						(baseX * 32).ToString(),
						",",
						(baseY * 32).ToString(),
						",",
						(baseZ * 32).ToString()
					}));
					this.server.World.Logger.Error(e);
				}
			}
			finally
			{
				for (int cy2 = 0; cy2 < chunkCol.Length; cy2++)
				{
					chunkCol[cy2].ReleaseBlockReadLock();
				}
			}
		}

		private void TrySpawnSomethingAt_offthrad(int baseX, int baseY, int baseZ, Dictionary<AssetLocation, int> spawnCounts, IWorldChunk[] chunkCol, List<SpawnState> spawnStates)
		{
			Vec3i spawnPosition = this.spawnPosition;
			int mapsizey = this.server.WorldMap.MapSizeY;
			List<SpawnOppurtunity> spawnPositions = new List<SpawnOppurtunity>();
			int num = this.surfaceMapTryID;
			this.surfaceMapTryID = num + 1;
			int surfaceMapTryID = num;
			int SeaLevel = this.SeaLevel;
			int SkyHeight = this.SkyHeight;
			ushort[] heightMap = this.heightMap;
			Random rand = this.rand;
			int[] shuffledY = this.shuffledY;
			shuffledY.Shuffle(rand);
			for (int yIndex = 0; yIndex < shuffledY.Length; yIndex++)
			{
				int startY = (baseY + shuffledY[yIndex]) * 32 + rand.Next(32);
				if (startY > -3 && startY < mapsizey + 3)
				{
					int startX;
					int startZ;
					spawnPosition.Set(startX = baseX * 32 + rand.Next(32), startY, startZ = baseZ * 32 + rand.Next(32));
					foreach (SpawnState spawnState in spawnStates)
					{
						RuntimeSpawnConditions sc = spawnState.ForType.Server.SpawnConditions.Runtime;
						if (spawnState.SpawnableAmountGlobal > 0)
						{
							int areaSpawned;
							spawnCounts.TryGetValue(spawnState.ForType.Code, out areaSpawned);
							if (areaSpawned <= spawnState.SpawnCapScaledPerPlayer)
							{
								if (!sc.TryOnlySurface)
								{
									double y = (double)(startY + 3);
									double yRel = ((y > (double)SeaLevel) ? (1.0 + (y - (double)SeaLevel) / (double)SkyHeight) : (y / (double)SeaLevel));
									if ((double)sc.MinY > yRel)
									{
										continue;
									}
									y -= 6.0;
									yRel = ((y > (double)SeaLevel) ? (1.0 + (y - (double)SeaLevel) / (double)SkyHeight) : (y / (double)SeaLevel));
									if ((double)sc.MaxY < yRel)
									{
										continue;
									}
								}
								int tries = 10;
								while (spawnState.NextGroupSize <= 0 && tries-- > 0)
								{
									float val = sc.HerdSize.nextFloat();
									spawnState.NextGroupSize = (int)val + (((double)(val - (float)((int)val)) > rand.NextDouble()) ? 1 : 0);
								}
								if (spawnState.NextGroupSize <= 0)
								{
									spawnState.NextGroupSize = -1;
								}
								else
								{
									spawnPositions.Clear();
									int qSelfAndCompanions = spawnState.SelfAndCompanionProps.Length;
									EntityProperties typeToSpawn = spawnState.SelfAndCompanionProps[0];
									tries = spawnState.NextGroupSize * 4 + 5;
									int i = 0;
									while (i < tries && spawnPositions.Count < spawnState.NextGroupSize)
									{
										spawnPosition.X = this.randomWithinSameChunk(startX);
										spawnPosition.Z = this.randomWithinSameChunk(startZ);
										if (!sc.TryOnlySurface)
										{
											spawnPosition.Y = Math.Max(1, startY + rand.Next(7) - 3);
											goto IL_02E4;
										}
										int mapIndex = spawnPosition.Z % 32 * 32 + spawnPosition.X % 32;
										if (spawnState.surfaceMap == null)
										{
											spawnState.surfaceMap = new int[1024];
										}
										if (spawnState.surfaceMap[mapIndex] != surfaceMapTryID)
										{
											spawnState.surfaceMap[mapIndex] = surfaceMapTryID;
											spawnPosition.Y = (int)(heightMap[mapIndex] + 1);
											goto IL_02E4;
										}
										IL_03C1:
										i++;
										continue;
										IL_02E4:
										if (spawnPosition.Y < 1 || spawnPosition.Y >= mapsizey)
										{
											i++;
											goto IL_03C1;
										}
										double yRel2 = ((spawnPosition.Y > SeaLevel) ? (1.0 + (double)(spawnPosition.Y - SeaLevel) / (double)SkyHeight) : ((double)spawnPosition.Y / (double)SeaLevel));
										if ((double)sc.MinY > yRel2 || (double)sc.MaxY < yRel2)
										{
											i++;
											goto IL_03C1;
										}
										if (spawnPositions.Count > 0 && qSelfAndCompanions > 1)
										{
											int rnd = 1 + rand.Next(qSelfAndCompanions - 1);
											typeToSpawn = spawnState.SelfAndCompanionProps[rnd];
										}
										Vec3d canSpawnPos = this.CanSpawnAt_offthread(typeToSpawn, spawnPosition, sc, chunkCol);
										if (canSpawnPos != null)
										{
											spawnPositions.Add(new SpawnOppurtunity
											{
												ForType = typeToSpawn,
												Pos = canSpawnPos
											});
										}
										if (spawnPositions.Count == 0)
										{
											i++;
											goto IL_03C1;
										}
										goto IL_03C1;
									}
									if (spawnPositions.Count >= spawnState.NextGroupSize)
									{
										EntitySpawnerResult toSpawn = new EntitySpawnerResult(new List<SpawnOppurtunity>(spawnPositions), spawnState);
										this.readyToSpawn.Enqueue(toSpawn);
										spawnState.SpawnableAmountGlobal -= spawnState.NextGroupSize;
										spawnState.NextGroupSize = -1;
									}
								}
							}
						}
					}
				}
			}
		}

		private int randomWithinSameChunk(int x)
		{
			return (x & -32) + (x + this.rand.Next(11) - 5 + 32) % 32;
		}

		public Vec3d CanSpawnAt_offthread(EntityProperties type, Vec3i spawnPosition, RuntimeSpawnConditions sc, IWorldChunk[] chunkCol)
		{
			ServerWorldMap WorldMap = this.server.WorldMap;
			if (spawnPosition.Y <= 0 || spawnPosition.Y >= WorldMap.MapSizeY)
			{
				return null;
			}
			BlockPos tmpPos = this.tmpPos;
			IWorldAccessor worldAccessor = WorldMap.World;
			Vec3d vec3d;
			try
			{
				tmpPos.Set(spawnPosition);
				ClimateCondition climate;
				Block block;
				if (sc.TryOnlySurface)
				{
					climate = this.GetSuitableClimateTemperatureRainfall(WorldMap, sc);
					if (climate == null)
					{
						return null;
					}
					block = chunkCol[tmpPos.Y / 32].GetLocalBlockAtBlockPos_LockFree(worldAccessor, tmpPos, 0);
					if (!sc.CanSpawnInside(block))
					{
						return null;
					}
					tmpPos.Y--;
					if (!chunkCol[tmpPos.Y / 32].GetLocalBlockAtBlockPos_LockFree(worldAccessor, tmpPos, 1).CanCreatureSpawnOn(this.cachingBlockAccessor, tmpPos, type, sc))
					{
						return null;
					}
				}
				else
				{
					block = chunkCol[tmpPos.Y / 32].GetLocalBlockAtBlockPos_LockFree(worldAccessor, tmpPos, 0);
					bool canspawn = false;
					for (int i = 0; i < 5; i++)
					{
						BlockPos blockPos = tmpPos;
						int num = blockPos.Y - 1;
						blockPos.Y = num;
						if (num < 0)
						{
							break;
						}
						Block belowBlock = chunkCol[tmpPos.Y / 32].GetLocalBlockAtBlockPos_LockFree(worldAccessor, tmpPos, 0);
						if (sc.CanSpawnInside(block) && belowBlock.CanCreatureSpawnOn(this.cachingBlockAccessor, tmpPos, type, sc))
						{
							canspawn = true;
							break;
						}
						spawnPosition.Y--;
						block = belowBlock;
					}
					if (!canspawn)
					{
						return null;
					}
					climate = this.GetSuitableClimateTemperatureRainfall(WorldMap, sc);
					if (climate == null)
					{
						return null;
					}
				}
				tmpPos.Y++;
				IMapRegion mapregion = WorldMap.GetMapRegion(tmpPos);
				WorldMap.AddWorldGenForestShrub(climate, mapregion, tmpPos);
				if (sc.MinForest > climate.ForestDensity || sc.MaxForest < climate.ForestDensity)
				{
					vec3d = null;
				}
				else if (sc.MinShrubs > climate.ShrubDensity || sc.MaxShrubs < climate.ShrubDensity)
				{
					vec3d = null;
				}
				else if (sc.MinForestOrShrubs > Math.Max(climate.ForestDensity, climate.ShrubDensity))
				{
					vec3d = null;
				}
				else
				{
					double yOffset = 1E-07;
					Cuboidf[] insideBlockBoxes = block.GetCollisionBoxes(this.server.BlockAccessor, tmpPos);
					if (insideBlockBoxes != null && insideBlockBoxes.Length != 0)
					{
						yOffset += (double)(insideBlockBoxes[0].MaxY % 1f);
					}
					Vec3d spawnPosition3d = new Vec3d((double)spawnPosition.X + 0.5, (double)spawnPosition.Y + yOffset, (double)spawnPosition.Z + 0.5);
					Cuboidf collisionBox = type.SpawnCollisionBox.OmniNotDownGrowBy(0.1f);
					if (this.collisionTester.IsColliding(this.server.BlockAccessor, collisionBox, spawnPosition3d, false))
					{
						vec3d = null;
					}
					else
					{
						IPlayer plr = this.server.NearestPlayer(spawnPosition3d.X, spawnPosition3d.Y, spawnPosition3d.Z);
						if (((plr != null) ? plr.Entity : null) != null && !plr.Entity.CanSpawnNearby(type, spawnPosition3d, sc))
						{
							vec3d = null;
						}
						else
						{
							vec3d = spawnPosition3d;
						}
					}
				}
			}
			catch (Exception e)
			{
				if (!this.errorLoggedThisTick)
				{
					this.errorLoggedThisTick = true;
					this.server.World.Logger.Warning("Error when testing to spawn entity {0} at position {1}, can report to dev team but otherwise should do no harm.", new object[]
					{
						type.Code.ToShortString(),
						spawnPosition
					});
					this.server.World.Logger.Error(e);
				}
				vec3d = null;
			}
			return vec3d;
		}

		public bool CheckCanSpawnAt(EntityProperties type, RuntimeSpawnConditions sc, BlockPos spawnPosition)
		{
			ServerWorldMap WorldMap = this.server.WorldMap;
			if (spawnPosition.Y <= 0 || spawnPosition.Y >= WorldMap.MapSizeY)
			{
				return false;
			}
			IBlockAccessor blockAccessor = WorldMap.World.BlockAccessor;
			bool flag;
			try
			{
				Block block = blockAccessor.GetBlock(spawnPosition);
				if (!sc.CanSpawnInside(block))
				{
					flag = false;
				}
				else
				{
					spawnPosition.Y--;
					flag = blockAccessor.GetBlock(spawnPosition, 1).CanCreatureSpawnOn(blockAccessor, spawnPosition, type, sc);
				}
			}
			catch (Exception e)
			{
				this.server.World.Logger.Warning("Error when re-testing to spawn entity {0} at position {1}, can report to dev team but otherwise should do no harm.", new object[]
				{
					type.Code.ToShortString(),
					spawnPosition
				});
				this.server.World.Logger.Error(e);
				flag = false;
			}
			return flag;
		}

		private ClimateCondition GetSuitableClimateTemperatureRainfall(ServerWorldMap worldMap, RuntimeSpawnConditions sc)
		{
			this.tmpPos.Y = (int)((double)this.server.seaLevel * 1.09);
			ClimateCondition climate = worldMap.getWorldGenClimateAt(this.tmpPos, true);
			if (climate == null)
			{
				return null;
			}
			if (sc.ClimateValueMode != EnumGetClimateMode.WorldGenValues)
			{
				worldMap.GetClimateAt(this.tmpPos, climate, sc.ClimateValueMode, this.server.Calendar.TotalDays);
			}
			if (sc.MinTemp > climate.Temperature || sc.MaxTemp < climate.Temperature)
			{
				return null;
			}
			if (sc.MinRain > climate.Rainfall || sc.MaxRain < climate.Rainfall)
			{
				return null;
			}
			return climate;
		}

		private void LoadViableSpawnAreas()
		{
			List<SpawnArea> spawnAreas = new List<SpawnArea>();
			HashSet<long> chunkColumnCoordsTmp = this.chunkColumnCoordsTmp;
			ServerWorldMap worldMap = this.server.WorldMap;
			foreach (ConnectedClient client in this.server.Clients.Values)
			{
				if (client.IsPlayingClient && client.Entityplayer != null)
				{
					EntityPos cpos = client.Entityplayer.Pos;
					if (cpos.Dimension == 0)
					{
						int chunkX = (int)cpos.X / 32;
						int chunkY = (int)cpos.Y / 32;
						int chunkZ = (int)cpos.Z / 32;
						SpawnArea area = new SpawnArea();
						area.chunkY = chunkY;
						chunkColumnCoordsTmp.Clear();
						for (int dx = -3; dx <= 3; dx++)
						{
							int x = chunkX + dx;
							for (int dz = -3; dz <= 3; dz++)
							{
								int z = chunkZ + dz;
								bool columnLoaded = false;
								for (int dy = Math.Max(-3, -chunkY); dy <= 3; dy++)
								{
									IWorldChunk chunk = worldMap.GetChunk(x, chunkY + dy, z);
									if (chunk != null)
									{
										columnLoaded = true;
										Entity[] chunkEntities = chunk.Entities;
										if (chunkEntities != null)
										{
											for (int i = 0; i < chunkEntities.Length; i++)
											{
												Entity e = chunkEntities[i];
												int cnt = 0;
												if (e != null)
												{
													area.spawnCounts.TryGetValue(e.Code, out cnt);
												}
												else if (i >= chunk.EntitiesCount)
												{
													break;
												}
												area.spawnCounts[e.Code] = cnt + 1;
											}
										}
									}
								}
								if (columnLoaded)
								{
									chunkColumnCoordsTmp.Add(((long)z << 32) + (long)x);
								}
							}
						}
						if (chunkColumnCoordsTmp.Count > 0)
						{
							area.ChunkColumnCoords = chunkColumnCoordsTmp.ToArray<long>();
							area.ChunkColumnCoords.Shuffle(this.rand);
							spawnAreas.Add(area);
						}
					}
				}
			}
			this.spawnAreas = spawnAreas;
		}

		internal void ReloadSpawnStates_offthread()
		{
			double daysLeft = this.GraceTimeUntilTotalDays - this.server.Calendar.TotalDays;
			Dictionary<AssetLocation, int> quantityLoaded = new Dictionary<AssetLocation, int>();
			foreach (Entity entity in this.server.LoadedEntities.Values)
			{
				if (!(entity.Code == null))
				{
					string es = entity.Attributes.GetString("originaltype", null);
					AssetLocation ecode = ((es == null) ? entity.Code : new AssetLocation(es));
					int beforeQuantity;
					quantityLoaded.TryGetValue(ecode, out beforeQuantity);
					quantityLoaded[ecode] = beforeQuantity + 1;
					SpawnConditions spawnConditions = entity.Properties.Server.SpawnConditions;
					QuantityByGroup quantityByGroup;
					if (spawnConditions == null)
					{
						quantityByGroup = null;
					}
					else
					{
						RuntimeSpawnConditions runtime = spawnConditions.Runtime;
						quantityByGroup = ((runtime != null) ? runtime.MaxQuantityByGroup : null);
					}
					QuantityByGroup maxqgrp = quantityByGroup;
					if (maxqgrp != null && WildcardUtil.Match(maxqgrp.Code, ecode))
					{
						quantityLoaded.TryGetValue(maxqgrp.Code, out beforeQuantity);
						quantityLoaded[maxqgrp.Code] = beforeQuantity + 1;
					}
				}
			}
			List<SpawnState> newSpawnStates = new List<SpawnState>();
			Random rand = this.rand;
			int onlinePlayersCount = this.server.AllOnlinePlayers.Length;
			foreach (EntityProperties type in this.server.EntityTypes)
			{
				SpawnConditions spawnConditions2 = type.Server.SpawnConditions;
				RuntimeSpawnConditions conds = ((spawnConditions2 != null) ? spawnConditions2.Runtime : null);
				if (conds != null && conds.MaxQuantity != 0 && (daysLeft <= 0.0 || !(conds.Group == "hostile")) && rand.NextDouble() < 1.0 * conds.Chance)
				{
					int qNow;
					quantityLoaded.TryGetValue(type.Code, out qNow);
					float spawnCapMul = 1f + Math.Max(0f, (float)(onlinePlayersCount - 1) * this.server.Config.SpawnCapPlayerScaling * conds.SpawnCapPlayerScaling);
					int spawnableAmount = (int)((float)conds.MaxQuantity * spawnCapMul - (float)qNow);
					if (conds.MaxQuantityByGroup != null)
					{
						int qNowGroup;
						quantityLoaded.TryGetValue(conds.MaxQuantityByGroup.Code, out qNowGroup);
						spawnableAmount = Math.Min(spawnableAmount, (int)((float)conds.MaxQuantityByGroup.MaxQuantity * spawnCapMul) - qNowGroup);
					}
					if (spawnableAmount > 0)
					{
						bool flag = conds.Companions != null && conds.Companions.Length != 0;
						List<EntityProperties> selfAndCompanionsProps = new List<EntityProperties> { type };
						if (flag)
						{
							AssetLocation[] companions = conds.Companions;
							for (int i = 0; i < companions.Length; i++)
							{
								EntityProperties companionType;
								if (this.entityTypesByCode.TryGetValue(companions[i], out companionType))
								{
									selfAndCompanionsProps.Add(companionType);
								}
								else if (!conds.doneInitialLoad)
								{
									ServerMain.Logger.Warning("Entity with code {0} has defined a companion spawn {1}, but no such entity type found.", new object[]
									{
										type.Code,
										conds.Companions[i]
									});
								}
							}
						}
						conds.doneInitialLoad = true;
						newSpawnStates.Add(new SpawnState
						{
							ForType = type,
							profilerName = "testspawn " + type.Code,
							SpawnableAmountGlobal = spawnableAmount,
							SpawnCapScaledPerPlayer = (int)((float)conds.MaxQuantity * spawnCapMul / (float)onlinePlayersCount),
							SelfAndCompanionProps = selfAndCompanionsProps.ToArray()
						});
					}
				}
			}
			this.spawnStates = newSpawnStates;
		}

		public bool ShouldExit()
		{
			return this.server.stopped || this.server.exit.exit;
		}

		private bool multithreaded = true;

		private const int yWiggle = 3;

		private const int xzWiggle = 5;

		private const int chunksize = 32;

		private const int chunkRange = 3;

		private const float globalMultiplier = 1f;

		private int GraceTimerDayNotify = -1;

		private double GraceTimeUntilTotalDays = 5.0;

		private List<SpawnArea> spawnAreas = new List<SpawnArea>();

		private List<SpawnState> spawnStates = new List<SpawnState>();

		private HashSet<long> chunkColumnCoordsTmp = new HashSet<long>();

		private CollisionTester collisionTester = new CollisionTester();

		private Dictionary<AssetLocation, EntityProperties> entityTypesByCode = new Dictionary<AssetLocation, EntityProperties>();

		private Random rand = new Random();

		private ushort[] heightMap;

		private ICachingBlockAccessor cachingBlockAccessor;

		private float slowaccum;

		private bool first = true;

		private int SeaLevel;

		private int SkyHeight;

		private Vec3i spawnPosition = new Vec3i();

		private int[] shuffledY = new int[] { -3, -2, -1, 0, 1, 2, 3 };

		private int surfaceMapTryID;

		private bool errorLoggedThisTick;

		public volatile bool paused;

		private ConcurrentQueue<EntitySpawnerResult> readyToSpawn = new ConcurrentQueue<EntitySpawnerResult>();

		private readonly BlockPos tmpPos = new BlockPos();
	}
}
