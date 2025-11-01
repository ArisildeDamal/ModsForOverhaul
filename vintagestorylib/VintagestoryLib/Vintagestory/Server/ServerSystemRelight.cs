using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace Vintagestory.Server
{
	public class ServerSystemRelight : ServerSystem
	{
		public ServerSystemRelight(ServerMain server)
			: base(server)
		{
		}

		public override void OnBeginGameReady(SaveGame savegame)
		{
			this.chunkIlluminator = new ChunkIlluminator(this.server.WorldMap, new BlockAccessorRelaxed(this.server.WorldMap, this.server, false, false), MagicNum.ServerChunkSize);
			this.chunkIlluminator.InitForWorld(this.server.Blocks, (ushort)this.server.sunBrightness, this.server.WorldMap.MapSizeX, this.server.WorldMap.MapSizeY, this.server.WorldMap.MapSizeZ);
		}

		public override void OnSeparateThreadTick()
		{
			this.ProcessLightingQueue();
		}

		public override int GetUpdateInterval()
		{
			return 10;
		}

		public void ProcessLightingQueue()
		{
			while (this.server.WorldMap.LightingTasks.Count > 0)
			{
				UpdateLightingTask task = null;
				object lightingTasksLock = this.server.WorldMap.LightingTasksLock;
				lock (lightingTasksLock)
				{
					task = this.server.WorldMap.LightingTasks.Dequeue();
				}
				if (task == null)
				{
					return;
				}
				if (this.server.WorldMap.IsValidPos(task.pos))
				{
					this.ProcessLightingTask(task, task.pos);
					if (this.server.Suspended)
					{
						break;
					}
				}
			}
		}

		public void ProcessLightingTask(UpdateLightingTask task, BlockPos pos)
		{
			int oldLightAbsorb = 0;
			int newLightAbsorb = 0;
			bool changedLightSource = false;
			int posX = task.pos.X;
			int posY = task.pos.InternalY;
			int posZ = task.pos.Z;
			HashSet<long> chunksDirty = new HashSet<long>();
			if (task.absorbUpdate)
			{
				oldLightAbsorb = (int)task.oldAbsorb;
				newLightAbsorb = (int)task.newAbsorb;
			}
			else if (task.removeLightHsv != null)
			{
				changedLightSource = true;
				chunksDirty.AddRange(this.chunkIlluminator.RemoveBlockLight(task.removeLightHsv, posX, posY, posZ));
			}
			else
			{
				int oldblockid = task.oldBlockId;
				int newblockid = task.newBlockId;
				Block block = this.server.Blocks[oldblockid];
				Block newBlock = this.server.Blocks[newblockid];
				byte[] oldLightHsv = block.GetLightHsv(this.server.BlockAccessor, pos, null);
				byte[] newLightHsv = newBlock.GetLightHsv(this.server.BlockAccessor, pos, null);
				if (oldLightHsv[2] > 0)
				{
					changedLightSource = true;
					chunksDirty.AddRange(this.chunkIlluminator.RemoveBlockLight(oldLightHsv, pos.X, pos.InternalY, pos.Z));
				}
				if (newLightHsv[2] > 0)
				{
					changedLightSource = true;
					chunksDirty.AddRange(this.chunkIlluminator.PlaceBlockLight(newLightHsv, pos.X, pos.InternalY, pos.Z));
				}
				oldLightAbsorb = block.GetLightAbsorption(this.server.BlockAccessor, pos);
				newLightAbsorb = newBlock.GetLightAbsorption(this.server.BlockAccessor, pos);
				if (oldLightHsv[2] == 0 && newLightHsv[2] == 0 && oldLightAbsorb != newLightAbsorb)
				{
					chunksDirty.AddRange(this.chunkIlluminator.UpdateBlockLight(oldLightAbsorb, newLightAbsorb, pos.X, pos.InternalY, pos.Z));
				}
				this.server.WorldMap.MarkChunksDirty(pos, GameMath.Max(new int[]
				{
					1,
					(int)newLightHsv[2],
					(int)oldLightHsv[2]
				}));
			}
			bool requireRelight = newLightAbsorb != oldLightAbsorb;
			if (requireRelight || changedLightSource)
			{
				for (int i = 0; i < 6; i++)
				{
					Vec3i vec = BlockFacing.ALLNORMALI[i];
					long neibindex3d = this.server.WorldMap.ChunkIndex3D((pos.X + vec.X) / 32, (pos.InternalY + vec.Y) / 32, (pos.Z + vec.Z) / 32);
					chunksDirty.Add(neibindex3d);
				}
			}
			if (requireRelight)
			{
				chunksDirty.AddRange(this.chunkIlluminator.UpdateSunLight(pos.X, pos.InternalY, pos.Z, oldLightAbsorb, newLightAbsorb));
			}
			foreach (long index3d in chunksDirty)
			{
				ServerChunk serverChunk = this.server.WorldMap.GetServerChunk(index3d);
				if (serverChunk != null)
				{
					serverChunk.MarkModified();
				}
			}
		}

		public ChunkIlluminator chunkIlluminator;
	}
}
