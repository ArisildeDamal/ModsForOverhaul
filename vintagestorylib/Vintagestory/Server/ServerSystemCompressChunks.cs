using System;
using System.Collections.Generic;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common.Database;

namespace Vintagestory.Server
{
	internal class ServerSystemCompressChunks : ServerSystem
	{
		public ServerSystemCompressChunks(ServerMain server)
			: base(server)
		{
		}

		public override void OnPlayerJoin(ServerPlayer player)
		{
			object obj = this.clientIdsLock;
			lock (obj)
			{
				this.clientIds.Add(player.ClientId);
			}
		}

		public override void OnPlayerDisconnect(ServerPlayer player)
		{
			object obj = this.clientIdsLock;
			lock (obj)
			{
				this.clientIds.Remove(player.ClientId);
			}
		}

		public override int GetUpdateInterval()
		{
			return 10;
		}

		public override void OnServerTick(float dt)
		{
			if (this.compactableChunks.Count > 0)
			{
				return;
			}
			this.FreeMemory();
			long cur = this.server.totalUnpausedTime.ElapsedMilliseconds;
			if (cur - this.chunkCompressScanTimer < 4000L)
			{
				return;
			}
			this.chunkCompressScanTimer = cur;
			this.FindFreeableMemory();
		}

		private void FindFreeableMemory()
		{
			List<BlockPos> plrChunkPos = new List<BlockPos>();
			object obj = this.clientIdsLock;
			lock (obj)
			{
				foreach (int clientid in this.clientIds)
				{
					ConnectedClient client;
					if (this.server.Clients.TryGetValue(clientid, out client) && client.State == EnumClientState.Playing)
					{
						plrChunkPos.Add(client.ChunkPos);
					}
				}
			}
			int compressed = 0;
			obj = this.compactableChunksLock;
			lock (obj)
			{
				this.server.loadedChunksLock.AcquireReadLock();
				try
				{
					foreach (KeyValuePair<long, ServerChunk> val in this.server.loadedChunks)
					{
						if (val.Value.IsPacked())
						{
							compressed++;
						}
						else if ((long)Environment.TickCount - val.Value.lastReadOrWrite > (long)MagicNum.UncompressedChunkTTL)
						{
							bool skip = false;
							ChunkPos chunkpos = this.server.WorldMap.ChunkPosFromChunkIndex3D(val.Key);
							if (!val.Value.Empty && chunkpos.Dimension == 0)
							{
								foreach (BlockPos cpos in plrChunkPos)
								{
									if (Math.Abs(cpos.X - chunkpos.X) < 2 || Math.Abs(cpos.Z - chunkpos.Z) < 2)
									{
										skip = true;
										break;
									}
								}
							}
							if (!skip)
							{
								this.compactableChunks.Enqueue(val.Key);
							}
						}
					}
				}
				finally
				{
					this.server.loadedChunksLock.ReleaseReadLock();
				}
			}
		}

		private void FreeMemory()
		{
			while (this.compactedChunks.Count > 0)
			{
				long index3d = 0L;
				object obj = this.compactedChunksLock;
				lock (obj)
				{
					index3d = this.compactedChunks.Dequeue();
				}
				ServerChunk chunk = this.server.GetLoadedChunk(index3d);
				if (chunk != null)
				{
					chunk.TryCommitPackAndFree(MagicNum.UncompressedChunkTTL);
				}
			}
		}

		public override void OnSeparateThreadTick()
		{
			long index3d = 0L;
			object obj = this.compactableChunksLock;
			lock (obj)
			{
				if (this.compactableChunks.Count > 0)
				{
					index3d = this.compactableChunks.Dequeue();
				}
			}
			if (index3d == 0L)
			{
				return;
			}
			ServerChunk chunk = this.server.GetLoadedChunk(index3d);
			if (chunk == null)
			{
				return;
			}
			chunk.Pack();
			obj = this.compactedChunksLock;
			lock (obj)
			{
				this.compactedChunks.Enqueue(index3d);
			}
		}

		private long chunkCompressScanTimer;

		private object compactableChunksLock = new object();

		private Queue<long> compactableChunks = new Queue<long>();

		private object compactedChunksLock = new object();

		private Queue<long> compactedChunks = new Queue<long>();

		private object clientIdsLock = new object();

		private List<int> clientIds = new List<int>();
	}
}
