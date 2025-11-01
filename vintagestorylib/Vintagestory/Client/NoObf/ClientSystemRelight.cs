using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf
{
	public class ClientSystemRelight : ClientSystem
	{
		public ClientSystemRelight(ClientMain game)
			: base(game)
		{
			this.chunkIlluminator = new ChunkIlluminator(game.WorldMap, new BlockAccessorRelaxed(game.WorldMap, game, false, false), game.WorldMap.ClientChunkSize);
		}

		public override void OnBlockTexturesLoaded()
		{
			this.chunkIlluminator.InitForWorld(this.game.Blocks, (ushort)this.game.WorldMap.SunBrightness, this.game.WorldMap.MapSizeX, this.game.WorldMap.MapSizeY, this.game.WorldMap.MapSizeZ);
		}

		public override int SeperateThreadTickIntervalMs()
		{
			return 10;
		}

		public override void OnSeperateThreadGameTick(float dt)
		{
			this.ProcessLightingQueue();
		}

		public void ProcessLightingQueue()
		{
			ClientPlayer player = this.game.player;
			EntityPos entityPos;
			if (player == null)
			{
				entityPos = null;
			}
			else
			{
				EntityPlayer entity = player.Entity;
				entityPos = ((entity != null) ? entity.Pos : null);
			}
			EntityPos playerPos = entityPos;
			while (this.game.WorldMap.LightingTasks.Count > 0)
			{
				UpdateLightingTask task = null;
				object lightingTasksLock = this.game.WorldMap.LightingTasksLock;
				lock (lightingTasksLock)
				{
					task = this.game.WorldMap.LightingTasks.Dequeue();
				}
				if (task == null)
				{
					return;
				}
				this.ProcessLightingTask(playerPos, task);
			}
		}

		private void ProcessLightingTask(EntityPos playerPos, UpdateLightingTask task)
		{
			int chunksize = 32;
			int posX = task.pos.X;
			int posY = task.pos.InternalY;
			int posZ = task.pos.Z;
			bool isPriorityRelight = playerPos != null && playerPos.SquareDistanceTo((float)posX, (float)posY, (float)posZ) < 2304f;
			int oldLightAbsorb = 0;
			int newLightAbsorb = 0;
			bool changedLightSource = false;
			HashSet<long> chunksToRedraw = new HashSet<long>();
			chunksToRedraw.Add(this.chunkIlluminator.GetChunkIndexForPos(posX, posY, posZ));
			if (task.absorbUpdate)
			{
				oldLightAbsorb = (int)task.oldAbsorb;
				newLightAbsorb = (int)task.newAbsorb;
			}
			else if (task.removeLightHsv != null)
			{
				changedLightSource = true;
				chunksToRedraw.AddRange(this.chunkIlluminator.RemoveBlockLight(task.removeLightHsv, posX, posY, posZ));
			}
			else
			{
				Block block = this.game.Blocks[task.oldBlockId];
				Block newblock = this.game.Blocks[task.newBlockId];
				byte[] oldLightHsv = block.GetLightHsv(this.game.BlockAccessor, task.pos, null);
				byte[] newLightHsv = newblock.GetLightHsv(this.game.BlockAccessor, task.pos, null);
				if (oldLightHsv[2] > 0)
				{
					changedLightSource = true;
					chunksToRedraw.AddRange(this.chunkIlluminator.RemoveBlockLight(oldLightHsv, posX, posY, posZ));
				}
				if (newLightHsv[2] > 0)
				{
					changedLightSource = true;
					chunksToRedraw.AddRange(this.chunkIlluminator.PlaceBlockLight(newLightHsv, posX, posY, posZ));
				}
				oldLightAbsorb = block.GetLightAbsorption(this.game.BlockAccessor, task.pos);
				newLightAbsorb = newblock.GetLightAbsorption(this.game.BlockAccessor, task.pos);
				if (oldLightHsv[2] == 0 && newLightHsv[2] == 0 && oldLightAbsorb != newLightAbsorb)
				{
					chunksToRedraw.AddRange(this.chunkIlluminator.UpdateBlockLight(oldLightAbsorb, newLightAbsorb, posX, posY, posZ));
				}
			}
			bool requireRelight = oldLightAbsorb != newLightAbsorb;
			if (requireRelight)
			{
				chunksToRedraw.AddRange(this.chunkIlluminator.UpdateSunLight(posX, posY, posZ, oldLightAbsorb, newLightAbsorb));
			}
			foreach (long neibindex3d in chunksToRedraw)
			{
				this.game.WorldMap.SetChunkDirty(neibindex3d, isPriorityRelight, false, false);
			}
			if (requireRelight || changedLightSource)
			{
				long baseindex3d = this.game.WorldMap.ChunkIndex3D(posX / chunksize, posY / chunksize, posZ / chunksize);
				if (!chunksToRedraw.Contains(baseindex3d))
				{
					this.game.WorldMap.SetChunkDirty(baseindex3d, isPriorityRelight, false, false);
				}
				for (int x = -1; x < 2; x++)
				{
					for (int y = -1; y < 2; y++)
					{
						for (int z = -1; z < 2; z++)
						{
							if (z != 0 || y != 0 || x != 0)
							{
								long neibindex3d2 = this.game.WorldMap.ChunkIndex3D((posX + x) / chunksize, (posY + y) / chunksize, (posZ + z) / chunksize);
								if (neibindex3d2 != baseindex3d && !chunksToRedraw.Contains(neibindex3d2))
								{
									this.game.WorldMap.SetChunkDirty(neibindex3d2, isPriorityRelight, false, true);
								}
							}
						}
					}
				}
			}
		}

		public override string Name
		{
			get
			{
				return "relight";
			}
		}

		public override EnumClientSystemType GetSystemType()
		{
			return EnumClientSystemType.Misc;
		}

		internal ChunkIlluminator chunkIlluminator;
	}
}
