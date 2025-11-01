using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.Server
{
	public class EntitySpawnerResult
	{
		public EntitySpawnerResult(List<SpawnOppurtunity> spawnPositions, SpawnState spawnState)
		{
			this.spawnPositions = spawnPositions;
			this.spawnState = spawnState;
			this.quantityToSpawn = spawnState.NextGroupSize;
		}

		public void Spawn(ServerMain server, ServerSystemEntitySpawner entitySpawner)
		{
			long herdid = server.GetNextHerdId();
			if (server.SpawnDebug)
			{
				SpawnOppurtunity firstPos = this.spawnPositions[0];
				ServerMain.Logger.Notification("Spawn {0}x {1} @{2}/{3}/{4}", new object[]
				{
					this.spawnPositions.Count,
					firstPos.ForType.Code,
					(int)firstPos.Pos.X,
					(int)firstPos.Pos.Y,
					(int)firstPos.Pos.Z
				});
			}
			BlockPos tmpPos = new BlockPos();
			RuntimeSpawnConditions sc = this.spawnState.ForType.Server.SpawnConditions.Runtime;
			int totalSpawned = 0;
			foreach (SpawnOppurtunity so in this.spawnPositions)
			{
				int num = this.quantityToSpawn;
				this.quantityToSpawn = num - 1;
				if (num <= 0)
				{
					break;
				}
				EntityProperties props = so.ForType;
				if (entitySpawner.CheckCanSpawnAt(props, sc, tmpPos.Set(so.Pos)))
				{
					AssetLocation originalType = props.Code;
					if (server.EventManager.TriggerTrySpawnEntity(server.blockAccessor, ref props, so.Pos, herdid))
					{
						EntitySpawnerResult.DoSpawn(server, props, so.Pos, herdid, originalType);
						totalSpawned++;
					}
				}
			}
			ServerMain.FrameProfiler.Mark(this.spawnState.profilerName);
			if (totalSpawned < this.quantityToSpawn)
			{
				this.spawnState.SpawnableAmountGlobal += this.quantityToSpawn - totalSpawned;
			}
		}

		private static void DoSpawn(ServerMain server, EntityProperties entityType, Vec3d spawnPosition, long herdid, AssetLocation originalType)
		{
			Entity entity = server.Api.ClassRegistry.CreateEntity(entityType);
			EntityAgent agent = entity as EntityAgent;
			if (agent != null)
			{
				agent.HerdId = herdid;
			}
			EntityPos serverPos = entity.ServerPos;
			serverPos.SetPosWithDimension(spawnPosition);
			serverPos.SetYaw((float)((IWorldAccessor)server).Rand.NextDouble() * 6.2831855f);
			entity.Pos.SetFrom(serverPos);
			entity.PositionBeforeFalling.Set(serverPos.X, serverPos.Y, serverPos.Z);
			if (entityType.Code != originalType)
			{
				entity.Attributes.SetString("originaltype", originalType.ToString());
			}
			entity.Attributes.SetString("origin", "entityspawner");
			server.DelayedSpawnQueue.Enqueue(entity);
		}

		private List<SpawnOppurtunity> spawnPositions;

		private SpawnState spawnState;

		private int quantityToSpawn;
	}
}
