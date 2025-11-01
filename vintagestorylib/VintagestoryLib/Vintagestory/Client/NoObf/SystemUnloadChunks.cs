using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class SystemUnloadChunks : ClientSystem
	{
		public SystemUnloadChunks(ClientMain game)
			: base(game)
		{
			game.PacketHandlers[11] = new ServerPacketHandler<Packet_Server>(this.HandleChunkUnload);
		}

		public override string Name
		{
			get
			{
				return "uc";
			}
		}

		private void HandleChunkUnload(Packet_Server packet)
		{
			int chunkMapSizeX = this.game.WorldMap.index3dMulX;
			int chunkMapSizeZ = this.game.WorldMap.index3dMulZ;
			HashSet<Vec2i> horCoods = new HashSet<Vec2i>();
			int count = packet.UnloadChunk.GetXCount();
			object obj;
			for (int i = 0; i < count; i++)
			{
				int cx = packet.UnloadChunk.X[i];
				int cy = packet.UnloadChunk.Y[i];
				int cz = packet.UnloadChunk.Z[i];
				if (cy < 1024)
				{
					horCoods.Add(new Vec2i(cx, cz));
				}
				long posIndex = MapUtil.Index3dL(cx, cy, cz, (long)chunkMapSizeX, (long)chunkMapSizeZ);
				ClientChunk clientchunk = null;
				obj = this.game.WorldMap.chunksLock;
				lock (obj)
				{
					this.game.WorldMap.chunks.TryGetValue(posIndex, out clientchunk);
				}
				if (clientchunk != null)
				{
					this.UnloadChunk(clientchunk);
					RuntimeStats.chunksUnloaded++;
				}
			}
			this.game.Logger.VerboseDebug("Entities and pool locations removed. Removing from chunk dict");
			obj = this.game.WorldMap.chunksLock;
			lock (obj)
			{
				for (int j = 0; j < count; j++)
				{
					long posIndex2 = MapUtil.Index3dL(packet.UnloadChunk.X[j], packet.UnloadChunk.Y[j], packet.UnloadChunk.Z[j], (long)chunkMapSizeX, (long)chunkMapSizeZ);
					ClientChunk clientchunk2;
					this.game.WorldMap.chunks.TryGetValue(posIndex2, out clientchunk2);
					if (clientchunk2 != null)
					{
						clientchunk2.Dispose();
					}
					this.game.WorldMap.chunks.Remove(posIndex2);
				}
			}
			foreach (Vec2i vec2i in horCoods)
			{
				int cx2 = vec2i.X;
				int cz2 = vec2i.Y;
				bool anyfound = false;
				int cy2 = 0;
				while (!anyfound && cy2 < this.game.WorldMap.ChunkMapSizeY)
				{
					anyfound |= this.game.WorldMap.GetChunk(cx2, cy2, cz2) != null;
					cy2++;
				}
				if (!anyfound)
				{
					this.game.WorldMap.MapChunks.Remove(this.game.WorldMap.MapChunkIndex2D(cx2, cz2));
				}
			}
			ScreenManager.FrameProfiler.Mark("doneUnlCh");
		}

		private void UnloadChunk(ClientChunk clientchunk)
		{
			if (clientchunk == null)
			{
				return;
			}
			clientchunk.RemoveDataPoolLocations(this.game.chunkRenderer);
			for (int i = 0; i < clientchunk.EntitiesCount; i++)
			{
				Entity entity = clientchunk.Entities[i];
				if (entity != null && (this.game.EntityPlayer == null || entity.EntityId != this.game.EntityPlayer.EntityId))
				{
					EntityDespawnData reason = new EntityDespawnData
					{
						Reason = EnumDespawnReason.Unload
					};
					ClientEventManager eventManager = this.game.eventManager;
					if (eventManager != null)
					{
						eventManager.TriggerEntityDespawn(entity, reason);
					}
					entity.OnEntityDespawn(reason);
					this.game.RemoveEntityRenderer(entity);
					this.game.LoadedEntities.Remove(entity.EntityId);
				}
			}
			foreach (KeyValuePair<BlockPos, BlockEntity> val in clientchunk.BlockEntities)
			{
				val.Value.OnBlockUnloaded();
			}
		}

		public override void Dispose(ClientMain game)
		{
			foreach (KeyValuePair<long, ClientChunk> val in game.WorldMap.chunks)
			{
				this.UnloadChunk(val.Value);
			}
			EntityPlayer entityPlayer = game.EntityPlayer;
			if (entityPlayer == null)
			{
				return;
			}
			EntityClientProperties client = entityPlayer.Properties.Client;
			if (client == null)
			{
				return;
			}
			EntityRenderer renderer = client.Renderer;
			if (renderer == null)
			{
				return;
			}
			renderer.Dispose();
		}

		public override EnumClientSystemType GetSystemType()
		{
			return EnumClientSystemType.Misc;
		}
	}
}
