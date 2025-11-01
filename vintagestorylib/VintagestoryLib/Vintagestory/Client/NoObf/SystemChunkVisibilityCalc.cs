using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.Common.Database;

namespace Vintagestory.Client.NoObf
{
	public class SystemChunkVisibilityCalc : ClientSystem
	{
		public override string Name
		{
			get
			{
				return "chunkculler";
			}
		}

		public SystemChunkVisibilityCalc(ClientMain game)
			: base(game)
		{
			SystemChunkVisibilityCalc <>4__this = this;
			game.eventManager.OnUpdateLighting += this.OnUpdateLighting;
			game.eventManager.OnChunkLoaded += this.OnChunkLoaded;
			game.eventManager.AddGameTickListener(new Action<float>(this.onEvery500ms), 500, 0);
			this.doOcclCulling = ClientSettings.Occlusionculling;
			ClientSettings.Inst.AddWatcher<bool>("occlusionculling", delegate(bool nowon)
			{
				int num = (<>4__this.doOcclCulling ? 1 : 0);
				<>4__this.doOcclCulling = nowon;
				if (num == 0 && nowon)
				{
					object chunkPositionsLock = game.chunkPositionsLock;
					lock (chunkPositionsLock)
					{
						foreach (KeyValuePair<long, ClientChunk> val in game.WorldMap.chunks)
						{
							val.Value.traversabilityFresh = false;
							ChunkPos pos = game.WorldMap.ChunkPosFromChunkIndex3D(val.Key);
							if (pos.Dimension == 0)
							{
								game.chunkPositionsForRegenTrav.Add(pos);
							}
						}
					}
				}
			});
		}

		private void onEvery500ms(float dt)
		{
			if (this.game.extendedDebugInfo)
			{
				this.game.DebugScreenInfo["traversethread"] = "traverseQ: " + this.game.chunkPositionsForRegenTrav.Count.ToString();
			}
		}

		public override void OnBlockTexturesLoaded()
		{
			base.OnBlockTexturesLoaded();
			this.visitedBlock = new uint[32768];
			this.blocksFast = (this.game.Blocks as BlockList).BlocksFast;
			this.Blocks = new int[32768];
		}

		private void OnChunkLoaded(Vec3i chunkpos)
		{
			if (!this.doOcclCulling)
			{
				return;
			}
			object chunkPositionsLock = this.game.chunkPositionsLock;
			lock (chunkPositionsLock)
			{
				this.game.chunkPositionsForRegenTrav.Add(new ChunkPos(chunkpos));
			}
		}

		private void OnUpdateLighting(int oldBlockId, int newBlockId, BlockPos pos, Dictionary<BlockPos, BlockUpdate> blockUpdatesBulk)
		{
			if (!this.doOcclCulling)
			{
				return;
			}
			object chunkPositionsLock = this.game.chunkPositionsLock;
			lock (chunkPositionsLock)
			{
				if (blockUpdatesBulk != null)
				{
					using (Dictionary<BlockPos, BlockUpdate>.Enumerator enumerator = blockUpdatesBulk.GetEnumerator())
					{
						while (enumerator.MoveNext())
						{
							KeyValuePair<BlockPos, BlockUpdate> val = enumerator.Current;
							if (val.Value.NewSolidBlockId >= 0 && this.RequiresRecalc(val.Value.OldBlockId, val.Value.NewSolidBlockId))
							{
								ChunkPos chunkPos = ChunkPos.FromPosition(val.Key.X, val.Key.Y, val.Key.Z, 0);
								if (!this.game.chunkPositionsForRegenTrav.Contains(chunkPos))
								{
									this.game.chunkPositionsForRegenTrav.Add(chunkPos);
									ClientChunk chunk = this.game.WorldMap.GetClientChunk(chunkPos.X, chunkPos.Y, chunkPos.Z);
									if (chunk != null)
									{
										chunk.traversabilityFresh = false;
									}
								}
							}
						}
						return;
					}
				}
				if (this.RequiresRecalc(oldBlockId, newBlockId))
				{
					ChunkPos chunkPos2 = ChunkPos.FromPosition(pos.X, pos.Y, pos.Z);
					if (!this.game.chunkPositionsForRegenTrav.Contains(chunkPos2))
					{
						this.game.chunkPositionsForRegenTrav.Add(chunkPos2);
						ClientChunk chunk2 = this.game.WorldMap.GetClientChunk(chunkPos2.X, chunkPos2.Y, chunkPos2.Z);
						if (chunk2 != null)
						{
							chunk2.traversabilityFresh = false;
						}
					}
				}
			}
		}

		private bool RequiresRecalc(int oldblockid, int newblockid)
		{
			Block oldblock = this.game.Blocks[oldblockid];
			Block newblock = this.game.Blocks[newblockid];
			return oldblock.SideOpaque[0] != newblock.SideOpaque[0] || oldblock.SideOpaque[1] != newblock.SideOpaque[1] || oldblock.SideOpaque[2] != newblock.SideOpaque[2] || oldblock.SideOpaque[3] != newblock.SideOpaque[3] || oldblock.SideOpaque[4] != newblock.SideOpaque[4] || oldblock.SideOpaque[5] != newblock.SideOpaque[5];
		}

		public override int SeperateThreadTickIntervalMs()
		{
			return 10;
		}

		public override void OnSeperateThreadGameTick(float dt)
		{
			if (this.game.chunkPositionsForRegenTrav.Count == 0)
			{
				return;
			}
			object chunkPositionsLock = this.game.chunkPositionsLock;
			ChunkPos chunkpos;
			lock (chunkPositionsLock)
			{
				chunkpos = this.game.chunkPositionsForRegenTrav.PopOne<ChunkPos>();
			}
			this.RegenTraversabilityGraph(chunkpos);
		}

		private void RegenTraversabilityGraph(ChunkPos chunkpos)
		{
			ClientChunk chunk = this.game.WorldMap.GetClientChunk(chunkpos.X, chunkpos.Y, chunkpos.Z);
			if (chunk == null || !chunk.ChunkHasData())
			{
				return;
			}
			if (chunk.Empty)
			{
				this.setFullyTraversable(chunk);
				return;
			}
			chunk.ClearTraversable();
			this.bfsQueue.Clear();
			uint num = this.iteration + 1U;
			this.iteration = num;
			uint iter = num;
			chunk.TemporaryUnpack(this.Blocks);
			Vec3i curpos = new Vec3i();
			int validBlocks = this.blocksFast.Length;
			for (int i = 0; i < this.Blocks.Length; i++)
			{
				if (this.visitedBlock[i] != iter)
				{
					int exitedFaces = 0;
					this.bfsQueue.Enqueue(i);
					while (this.bfsQueue.Count > 0)
					{
						int index = this.bfsQueue.Dequeue();
						int blockId = this.Blocks[index];
						if (blockId < validBlocks)
						{
							Block block = this.blocksFast[blockId];
							if (!this.AllSidesOpaque(block))
							{
								curpos.Set(index % 32, index / 32 / 32, index / 32 % 32);
								for (int f = 0; f < 6; f++)
								{
									if (!block.SideOpaque[f])
									{
										Vec3i Normali = BlockFacing.ALLNORMALI[f];
										int nx = curpos.X + Normali.X;
										int ny = curpos.Y + Normali.Y;
										int nz = curpos.Z + Normali.Z;
										if (this.DidWeExitChunk(nx, ny, nz))
										{
											exitedFaces |= 1 << f;
										}
										else
										{
											int nindex = (ny * 32 + nz) * 32 + nx;
											if (this.visitedBlock[nindex] != iter)
											{
												this.visitedBlock[nindex] = iter;
												this.bfsQueue.Enqueue(nindex);
											}
										}
									}
								}
							}
						}
					}
					this.connectFacesAndSetTraversable(exitedFaces, chunk);
				}
			}
			chunk.traversabilityFresh = true;
		}

		private void connectFacesAndSetTraversable(int exitedFaces, ClientChunk chunk)
		{
			for (int i = 0; i < 6; i++)
			{
				if ((exitedFaces & (1 << i)) != 0)
				{
					for (int j = i + 1; j < 6; j++)
					{
						if ((exitedFaces & (1 << j)) != 0)
						{
							chunk.SetTraversable(i, j);
						}
					}
				}
			}
		}

		private void setFullyTraversable(ClientChunk chunk)
		{
			for (int i = 0; i < 6; i++)
			{
				for (int j = i + 1; j < 6; j++)
				{
					chunk.SetTraversable(i, j);
				}
			}
		}

		public bool AllSidesOpaque(Block block)
		{
			return block.SideOpaque[0] && block.SideOpaque[1] && block.SideOpaque[2] && block.SideOpaque[3] && block.SideOpaque[4] && block.SideOpaque[5];
		}

		public bool DidWeExitChunk(int posX, int posY, int posZ)
		{
			return posX < 0 || posX >= 32 || posY < 0 || posY >= 32 || posZ < 0 || posZ >= 32;
		}

		public override EnumClientSystemType GetSystemType()
		{
			return EnumClientSystemType.Misc;
		}

		private bool doOcclCulling;

		private uint[] visitedBlock;

		private uint iteration;

		private readonly QueueOfInt bfsQueue = new QueueOfInt();

		private const int chunksize = 32;

		private Block[] blocksFast;

		private int[] Blocks;
	}
}
