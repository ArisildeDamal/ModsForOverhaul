using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
using Vintagestory.Common.Database;
using Vintagestory.Server;

namespace Vintagestory.Common
{
	public class BlockAccessorMovable : BlockAccessorBase, IMiniDimension, IBlockAccessor
	{
		public EntityPos CurrentPos { get; set; }

		public bool Dirty { get; set; }

		public Vec3d CenterOfMass { get; set; }

		public bool TrackSelection { get; set; }

		public int BlocksPreviewSubDimension_Server { get; set; }

		public BlockPos selectionTrackingOriginalPos { get; set; }

		public int subDimensionId { get; set; }

		public BlockAccessorMovable(BlockAccessorBase parent, Vec3d pos)
			: base(parent.worldmap, parent.worldAccessor)
		{
			this.CurrentPos = new EntityPos(pos.X, pos.Y, pos.Z, 0f, 0f, 0f);
			this.parent = parent;
			this.BlocksPreviewSubDimension_Server = -1;
		}

		public virtual void SetSubDimensionId(int subId)
		{
			this.subDimensionId = subId;
		}

		public void SetSelectionTrackingSubId_Server(int subId)
		{
			this.BlocksPreviewSubDimension_Server = subId;
		}

		public virtual void ClearChunks()
		{
			IServerWorldAccessor worldAccessor = this.parent.worldAccessor as IServerWorldAccessor;
			if (worldAccessor != null)
			{
				using (Dictionary<long, IWorldChunk>.Enumerator enumerator = this.chunks.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						KeyValuePair<long, IWorldChunk> val = enumerator.Current;
						((ServerChunk)val.Value).ClearAll(worldAccessor);
					}
					goto IL_00AD;
				}
			}
			ChunkRenderer cr = ((ClientMain)this.parent.worldAccessor).chunkRenderer;
			foreach (KeyValuePair<long, IWorldChunk> val2 in this.chunks)
			{
				((ClientChunk)val2.Value).RemoveDataPoolLocations(cr);
			}
			IL_00AD:
			this.dirtychunks.Clear();
			if (this.CenterOfMass == null)
			{
				this.CenterOfMass = new Vec3d(0.0, 0.0, 0.0);
			}
			else
			{
				this.CenterOfMass.Set(0.0, 0.0, 0.0);
			}
			this.totalMass = 0.0;
		}

		public virtual void UnloadUnusedServerChunks()
		{
			List<long> toRemove = new List<long>();
			foreach (KeyValuePair<long, IWorldChunk> val in this.chunks)
			{
				if (val.Value.Empty)
				{
					toRemove.Add(val.Key);
					ChunkPos cpos = this.parent.worldmap.ChunkPosFromChunkIndex3D(val.Key);
					ServerSystemUnloadChunks.TryUnloadChunk(val.Key, cpos, (ServerChunk)val.Value, new List<ServerChunkWithCoord>(), (ServerMain)this.parent.worldAccessor);
				}
			}
			foreach (long key in toRemove)
			{
				this.chunks.Remove(key);
			}
		}

		public static bool ChunkCoordsInSameDimension(int cyA, int cyB)
		{
			return cyA / 1024 == cyB / 1024;
		}

		protected virtual IWorldChunk GetChunkAt(int posX, int posY, int posZ)
		{
			IWorldChunk chunk;
			this.chunks.TryGetValue(this.ChunkIndex(posX, posY, posZ), out chunk);
			return chunk;
		}

		protected virtual long ChunkIndex(int posX, int posY, int posZ)
		{
			int cx = posX / 32 % 512 + this.subDimensionId % 4096 * 512;
			long num = (long)(posY / 32 + 1024);
			int cz = posZ / 32 % 512 + this.subDimensionId / 4096 * 512;
			return (num * (long)this.worldmap.index3dMulZ + (long)cz) * (long)this.worldmap.index3dMulX + (long)cx;
		}

		public virtual void AdjustPosForSubDimension(BlockPos pos)
		{
			pos.X += this.subDimensionId % 4096 * 16384 + 8192;
			pos.Y += 8192;
			pos.Z += this.subDimensionId / 4096 * 16384 + 8192;
		}

		protected virtual IWorldChunk CreateChunkAt(int posX, int posY, int posZ)
		{
			long cindex = this.ChunkIndex(posX, posY, posZ);
			IWorldChunk chunk;
			if (this.worldAccessor.Side == EnumAppSide.Server)
			{
				ServerMain server = (ServerMain)this.worldAccessor;
				chunk = ServerChunk.CreateNew(server.serverChunkDataPool);
				chunk.Lighting.FloodWithSunlight(18);
				server.loadedChunksLock.AcquireWriteLock();
				try
				{
					ServerChunk oldchunk;
					if (server.loadedChunks.TryGetValue(cindex, out oldchunk))
					{
						oldchunk.Dispose();
					}
					server.loadedChunks[cindex] = (ServerChunk)chunk;
					goto IL_0094;
				}
				finally
				{
					server.loadedChunksLock.ReleaseWriteLock();
				}
			}
			chunk = ClientChunk.CreateNew(((ClientWorldMap)this.worldmap).chunkDataPool);
			IL_0094:
			this.chunks[cindex] = chunk;
			return chunk;
		}

		public virtual void MarkChunkDirty(int x, int y, int z)
		{
			this.dirtychunks.Add(this.ChunkIndex(x, y, z));
			this.Dirty = true;
		}

		public virtual void CollectChunksForSending(IPlayer[] players)
		{
			foreach (long index in this.dirtychunks)
			{
				IWorldChunk chunk;
				if (this.chunks.TryGetValue(index, out chunk))
				{
					((ServerChunk)chunk).MarkToPack();
					foreach (IPlayer player in players)
					{
						this.MarkChunkForSendingToPlayersInRange(chunk, index, player);
					}
				}
			}
			this.dirtychunks.Clear();
		}

		public virtual void MarkChunkForSendingToPlayersInRange(IWorldChunk chunk, long index, IPlayer player)
		{
			ServerPlayer plr = player as ServerPlayer;
			if (((plr != null) ? plr.Entity : null) == null || ((plr != null) ? plr.client : null) == null)
			{
				return;
			}
			ConnectedClient client = plr.client;
			float viewDist = (float)(client.WorldData.Viewdistance + 16);
			if (client.Entityplayer.ServerPos.InHorizontalRangeOf((int)this.CurrentPos.X, (int)this.CurrentPos.Z, viewDist * viewDist) || this.subDimensionId == this.BlocksPreviewSubDimension_Server)
			{
				client.forceSendChunks.Add(index);
			}
		}

		protected virtual int Index3d(int posX, int posY, int posZ)
		{
			return this.worldmap.ChunkSizedIndex3D(posX & 31, posY & 31, posZ & 31);
		}

		protected virtual bool SetBlock(int blockId, BlockPos pos, int layer, ItemStack byItemstack)
		{
			pos.SetDimension(1);
			IWorldChunk chunk = this.GetChunkAt(pos.X, pos.Y, pos.Z);
			if (chunk == null)
			{
				if (blockId == 0)
				{
					return false;
				}
				chunk = this.CreateChunkAt(pos.X, pos.Y, pos.Z);
			}
			else
			{
				chunk.Unpack();
				if (chunk.Empty)
				{
					chunk.Lighting.FloodWithSunlight(18);
				}
			}
			Block newBlock = this.worldmap.Blocks[blockId];
			if (layer == 2 || (layer == 0 && newBlock.ForFluidsLayer))
			{
				if (layer == 0)
				{
					this.SetSolidBlock(0, pos, chunk, byItemstack);
				}
				this.SetFluidBlock(blockId, pos, chunk);
				return true;
			}
			if (layer != 0 && layer != 1)
			{
				throw new ArgumentException("Layer must be solid or fluid");
			}
			return this.SetSolidBlock(blockId, pos, chunk, byItemstack);
		}

		protected virtual bool SetSolidBlock(int blockId, BlockPos pos, IWorldChunk chunk, ItemStack byItemstack)
		{
			int index3d = this.Index3d(pos.X, pos.Y, pos.Z);
			int oldblockid = (chunk.Data as ChunkData).GetSolidBlock(index3d);
			if (blockId == oldblockid)
			{
				return false;
			}
			Block newBlock = this.worldmap.Blocks[blockId];
			Block oldBlock = this.worldmap.Blocks[oldblockid];
			if (oldblockid > 0)
			{
				this.AddToCenterOfMass(oldBlock, pos, -1);
			}
			if (blockId > 0)
			{
				this.AddToCenterOfMass(newBlock, pos, 1);
			}
			chunk.Data[index3d] = blockId;
			if (blockId != 0)
			{
				chunk.Empty = false;
			}
			chunk.BreakAllDecorFast(this.worldAccessor, pos, index3d, true);
			oldBlock.OnBlockRemoved(this.worldmap.World, pos);
			newBlock.OnBlockPlaced(this.worldmap.World, pos, byItemstack);
			if (newBlock.DisplacesLiquids(this, pos))
			{
				chunk.Data.SetFluid(index3d, 0);
			}
			else
			{
				int liqId = chunk.Data.GetFluid(index3d);
				if (liqId != 0)
				{
					this.worldAccessor.GetBlock(liqId);
				}
			}
			return true;
		}

		protected virtual bool SetFluidBlock(int fluidBlockid, BlockPos pos, IWorldChunk chunk)
		{
			int index3d = this.Index3d(pos.X, pos.Y, pos.Z);
			int oldblockid = chunk.Data.GetFluid(index3d);
			if (fluidBlockid == oldblockid)
			{
				return false;
			}
			chunk.Data.SetFluid(index3d, fluidBlockid);
			if (fluidBlockid != 0)
			{
				chunk.Empty = false;
			}
			return true;
		}

		protected virtual void AddToCenterOfMass(Block block, BlockPos pos, int sign)
		{
			double mass = (double)Math.Max(block.MaterialDensity, 1) / 1000.0;
			double px = (double)pos.X + 0.5 - 8192.0;
			double py = (double)pos.Y + 0.5 - 8192.0;
			double pz = (double)pos.Z + 0.5 - 8192.0;
			if (this.CenterOfMass == null)
			{
				this.CenterOfMass = new Vec3d(px, py, pz);
			}
			else
			{
				this.CenterOfMass.X = (this.CenterOfMass.X * this.totalMass + px * mass * (double)sign) / (this.totalMass + mass * (double)sign);
				this.CenterOfMass.Y = (this.CenterOfMass.Y * this.totalMass + py * mass * (double)sign) / (this.totalMass + mass * (double)sign);
				this.CenterOfMass.Z = (this.CenterOfMass.Z * this.totalMass + pz * mass * (double)sign) / (this.totalMass + mass * (double)sign);
			}
			this.totalMass += mass * (double)sign;
		}

		public virtual FastVec3d GetRenderOffset(float dt)
		{
			FastVec3d result = new FastVec3d((double)(-(double)(this.subDimensionId % 4096) * 16384), 0.0, (double)(-(double)(this.subDimensionId / 4096) * 16384));
			result = result.Add(-8192.0);
			if (this.TrackSelection)
			{
				BlockSelection selection = ((ClientMain)this.parent.worldAccessor).BlockSelection;
				if (selection != null && selection.Position != null)
				{
					return result.Add(selection.Position).Add(selection.Face.Normali);
				}
			}
			return result.Add((double)this.selectionTrackingOriginalPos.X, (double)this.selectionTrackingOriginalPos.InternalY, (double)this.selectionTrackingOriginalPos.Z);
		}

		public virtual void SetRenderOffsetY(int offset)
		{
			this.selectionTrackingOriginalPos.Y = offset;
		}

		public virtual float[] GetRenderTransformMatrix(float[] currentModelViewMatrix, Vec3d playerPos)
		{
			if (this.CurrentPos.Yaw == 0f && this.CurrentPos.Pitch == 0f && this.CurrentPos.Roll == 0f)
			{
				return currentModelViewMatrix;
			}
			float[] result = new float[currentModelViewMatrix.Length];
			float dx = (float)(this.CurrentPos.X + this.CenterOfMass.X - playerPos.X);
			float dy = (float)(this.CurrentPos.Y + this.CenterOfMass.Y - playerPos.Y);
			float dz = (float)(this.CurrentPos.Z + this.CenterOfMass.Z - playerPos.Z);
			Mat4f.Translate(result, currentModelViewMatrix, dx, dy, dz);
			this.ApplyCurrentRotation(result);
			return Mat4f.Translate(result, result, -dx, -dy, -dz);
		}

		public virtual void ApplyCurrentRotation(float[] result)
		{
			Mat4f.RotateY(result, result, this.CurrentPos.Yaw);
			Mat4f.RotateZ(result, result, this.CurrentPos.Pitch);
			Mat4f.RotateX(result, result, this.CurrentPos.Roll);
		}

		public override int GetBlockId(int posX, int posY, int posZ, int layer)
		{
			if ((posX | posY | posZ) < 0)
			{
				return 0;
			}
			if (posY >= 32768)
			{
				posX %= 16384;
				posY %= 32768;
				posZ %= 16384;
			}
			IWorldChunk chunk = this.GetChunkAt(posX, posY, posZ);
			if (chunk == null)
			{
				return 0;
			}
			return chunk.UnpackAndReadBlock(this.Index3d(posX, posY, posZ), layer);
		}

		public override Block GetBlockOrNull(int posX, int posY, int posZ, int layer = 4)
		{
			if ((posX | posY | posZ) < 0)
			{
				return null;
			}
			if (posY >= 32768)
			{
				posX %= 16384;
				posY %= 32768;
				posZ %= 16384;
			}
			IWorldChunk chunk = this.GetChunkAt(posX, posY, posZ);
			if (chunk == null)
			{
				return null;
			}
			return this.worldmap.Blocks[chunk.UnpackAndReadBlock(this.Index3d(posX, posY, posZ), layer)];
		}

		public override void SetBlock(int blockId, BlockPos pos, ItemStack byItemstack)
		{
			if (this.SetBlock(blockId, pos, 0, byItemstack))
			{
				this.MarkChunkDirty(pos.X, pos.Y, pos.Z);
			}
		}

		public override void SetBlock(int blockId, BlockPos pos, int layer)
		{
			if (this.SetBlock(blockId, pos, 0, null))
			{
				this.MarkChunkDirty(pos.X, pos.Y, pos.Z);
			}
		}

		public override void ExchangeBlock(int blockId, BlockPos pos)
		{
		}

		public virtual void ReceiveClientChunk(long cindex, IWorldChunk chunk, IWorldAccessor world)
		{
			this.chunks[cindex] = chunk;
			this.RecalculateCenterOfMass(world);
		}

		public virtual void RecalculateCenterOfMass(IWorldAccessor world)
		{
			this.CenterOfMass = new Vec3d(0.0, 0.0, 0.0);
			this.totalMass = 0.0;
			BlockPos tmp = new BlockPos();
			foreach (KeyValuePair<long, IWorldChunk> entry in this.chunks)
			{
				ChunkPos chunkPos = this.worldmap.ChunkPosFromChunkIndex3D(entry.Key);
				int cx = chunkPos.X * 32 % 16384;
				int cy = chunkPos.Y * 32 % 16384;
				int cz = chunkPos.Z * 32 % 16384;
				IWorldChunk value = entry.Value;
				value.Unpack_ReadOnly();
				IChunkBlocks blocks = value.Data;
				for (int i = 0; i < 32768; i++)
				{
					int blockId = blocks.GetBlockId(i, 1);
					if (blockId > 0)
					{
						tmp.X = cx + i % 32;
						tmp.Y = cy + i / 1024;
						tmp.Z = cz + i / 32 % 32;
						this.AddToCenterOfMass(world.GetBlock(blockId), tmp, 1);
					}
				}
			}
		}

		internal static int CalcSubDimensionId(int cx, int cz)
		{
			return cx / 512 + cz / 512 * 4096;
		}

		internal static int CalcSubDimensionId(Vec3i vec)
		{
			return BlockAccessorMovable.CalcSubDimensionId(vec.X / 32, vec.Z / 32);
		}

		internal static bool IsTransparent(Vec3i chunkOrigin)
		{
			return BlockAccessorMovable.CalcSubDimensionId(chunkOrigin) == Dimensions.BlocksPreviewSubDimension_Client;
		}

		protected double totalMass;

		private BlockAccessorBase parent;

		private Dictionary<long, IWorldChunk> chunks = new Dictionary<long, IWorldChunk>();

		private FastSetOfLongs dirtychunks = new FastSetOfLongs();

		public const int subDimensionSize = 16384;

		public const int subDimensionIndexZMultiplier = 4096;

		public const int originOffset = 8192;

		public const int MaxMiniDimensions = 16777216;

		public const int subDimensionSizeInChunks = 512;

		public const int dimensionSizeY = 32768;

		public const int dimensionId = 1;

		public const int DefaultLightLevel = 18;
	}
}
