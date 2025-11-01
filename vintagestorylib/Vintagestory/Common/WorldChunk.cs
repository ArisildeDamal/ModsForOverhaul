using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.Common
{
	public abstract class WorldChunk : IWorldChunk
	{
		public byte[] blocksCompressed { get; set; }

		public byte[] lightCompressed { get; set; }

		public byte[] lightSatCompressed { get; set; }

		public byte[] fluidsCompressed { get; set; }

		public int EntitiesCount { get; set; }

		[Obsolete("Use Data field")]
		public IChunkBlocks Blocks
		{
			get
			{
				return this.chunkdata;
			}
		}

		public IChunkBlocks Data
		{
			get
			{
				return this.chunkdata;
			}
		}

		public IChunkLight Lighting
		{
			get
			{
				return this.chunkdata;
			}
		}

		public IChunkBlocks MaybeBlocks { get; set; }

		public bool Empty { get; set; }

		public abstract IMapChunk MapChunk { get; }

		Entity[] IWorldChunk.Entities
		{
			get
			{
				return this.Entities;
			}
		}

		Dictionary<BlockPos, BlockEntity> IWorldChunk.BlockEntities
		{
			get
			{
				return this.BlockEntities;
			}
			set
			{
				this.BlockEntities = value;
			}
		}

		public virtual void MarkModified()
		{
			this.WasModified = true;
			this.lastReadOrWrite = (long)Environment.TickCount;
		}

		public virtual bool IsPacked()
		{
			return this.chunkdata == null;
		}

		public virtual void TryPackAndCommit(int chunkTTL = 8000)
		{
			if ((long)Environment.TickCount - this.lastReadOrWrite < (long)chunkTTL)
			{
				return;
			}
			this.Pack();
			this.TryCommitPackAndFree(chunkTTL);
		}

		public virtual void Pack()
		{
			if (this.Disposed)
			{
				return;
			}
			object obj = this.packUnpackLock;
			lock (obj)
			{
				if (this.chunkdata != null)
				{
					if (this.PotentialBlockOrLightingChanges)
					{
						this.chunkdata.CompressInto(ref this.blocksCompressedTmp, ref this.lightCompressedTmp, ref this.lightSatCompressedTmp, ref this.fluidsCompressedTmp, 2);
					}
					else
					{
						this.blocksCompressedTmp = this.blocksCompressed;
						this.lightCompressedTmp = this.lightCompressed;
						this.lightSatCompressedTmp = this.lightSatCompressed;
						this.fluidsCompressedTmp = this.fluidsCompressed;
					}
				}
			}
		}

		public virtual bool TryCommitPackAndFree(int chunkTTL = 8000)
		{
			if (this.Disposed)
			{
				return false;
			}
			object obj = this.packUnpackLock;
			lock (obj)
			{
				if (this.blocksCompressedTmp == null)
				{
					return false;
				}
				if ((long)Environment.TickCount - this.lastReadOrWrite < (long)chunkTTL)
				{
					this.blocksCompressedTmp = null;
					this.lightCompressedTmp = null;
					this.lightSatCompressedTmp = null;
					this.fluidsCompressedTmp = null;
					return false;
				}
				this.blocksCompressed = this.blocksCompressedTmp;
				this.blocksCompressedTmp = null;
				this.lightCompressed = this.lightCompressedTmp;
				this.lightCompressedTmp = null;
				this.lightSatCompressed = this.lightSatCompressedTmp;
				this.lightSatCompressedTmp = null;
				this.fluidsCompressed = this.fluidsCompressedTmp;
				this.fluidsCompressedTmp = null;
				if (this.chunkdata != null && this.blocksCompressed != null)
				{
					if (this.WasModified)
					{
						this.UpdateEmptyFlag();
					}
					this.datapool.Free(this.chunkdata);
					this.MaybeBlocks = this.datapool.OnlyAirBlocksData;
					this.chunkdata = null;
				}
				this.chunkdataVersion = 2;
				this.WasModified = false;
				this.PotentialBlockOrLightingChanges = false;
			}
			return true;
		}

		public virtual void Unpack()
		{
			if (this.Disposed)
			{
				return;
			}
			object obj = this.packUnpackLock;
			lock (obj)
			{
				bool flag2 = this.chunkdata == null;
				this.unpackNoLock();
				if (flag2)
				{
					this.blocksCompressed = null;
					this.lightCompressed = null;
					this.lightSatCompressed = null;
					this.fluidsCompressed = null;
				}
				this.PotentialBlockOrLightingChanges = true;
			}
		}

		protected virtual void UpdateForVersion()
		{
			this.PotentialBlockOrLightingChanges = true;
		}

		public virtual bool Unpack_ReadOnly()
		{
			if (this.Disposed)
			{
				return false;
			}
			object obj = this.packUnpackLock;
			bool flag3;
			lock (obj)
			{
				bool flag2 = this.chunkdata == null;
				this.unpackNoLock();
				flag3 = flag2;
			}
			return flag3;
		}

		public virtual int UnpackAndReadBlock(int index, int layer)
		{
			if (this.Disposed)
			{
				return 0;
			}
			object obj = this.packUnpackLock;
			int blockId;
			lock (obj)
			{
				this.unpackNoLock();
				blockId = this.chunkdata.GetBlockId(index, layer);
			}
			return blockId;
		}

		public virtual ushort Unpack_AndReadLight(int index)
		{
			if (this.Disposed)
			{
				return 0;
			}
			object obj = this.packUnpackLock;
			ushort num;
			lock (obj)
			{
				this.unpackNoLock();
				num = this.chunkdata.ReadLight(index);
			}
			return num;
		}

		public virtual ushort Unpack_AndReadLight(int index, out int lightSat)
		{
			if (this.Disposed)
			{
				lightSat = 0;
				return 0;
			}
			object obj = this.packUnpackLock;
			ushort num;
			lock (obj)
			{
				this.unpackNoLock();
				num = this.chunkdata.ReadLight(index, out lightSat);
			}
			return num;
		}

		public virtual void Unpack_MaybeNullData()
		{
			object obj = this.packUnpackLock;
			lock (obj)
			{
				this.lastReadOrWrite = (long)Environment.TickCount;
				bool flag2 = this.chunkdata == null;
				this.unpackNoLock();
				if (flag2)
				{
					this.blocksCompressed = null;
					this.lightCompressed = null;
					this.lightSatCompressed = null;
					this.fluidsCompressed = null;
				}
			}
		}

		private void unpackNoLock()
		{
			this.lastReadOrWrite = (long)Environment.TickCount;
			if (this.chunkdata == null)
			{
				this.chunkdata = this.datapool.Request();
				this.chunkdata.DecompressFrom(this.blocksCompressed, this.lightCompressed, this.lightSatCompressed, this.fluidsCompressed, this.chunkdataVersion);
				this.MaybeBlocks = this.chunkdata;
				if (this.chunkdataVersion < 2)
				{
					this.UpdateForVersion();
				}
			}
		}

		public void AcquireBlockReadLock()
		{
			this.Unpack_ReadOnly();
			this.Data.TakeBulkReadLock();
		}

		public void ReleaseBlockReadLock()
		{
			this.Data.ReleaseBulkReadLock();
		}

		public virtual void UpdateEmptyFlag()
		{
			this.Empty = this.chunkdata.IsEmpty();
		}

		public virtual void MarkFresh()
		{
			this.lastReadOrWrite = (long)Environment.TickCount;
		}

		public abstract HashSet<int> LightPositions { get; set; }

		public abstract Dictionary<string, byte[]> ModData { get; set; }

		public bool Disposed
		{
			get
			{
				return this._disposed != 0;
			}
			set
			{
				this._disposed = ((value > false) ? 1 : 0);
			}
		}

		public Dictionary<string, object> LiveModData { get; set; }

		internal virtual void AddBlockEntity(BlockEntity blockEntity)
		{
			object obj = this.packUnpackLock;
			lock (obj)
			{
				this.BlockEntities[blockEntity.Pos] = blockEntity;
			}
		}

		public virtual bool RemoveBlockEntity(BlockPos pos)
		{
			object obj = this.packUnpackLock;
			bool flag2;
			lock (obj)
			{
				flag2 = this.BlockEntities.Remove(pos);
			}
			return flag2;
		}

		public virtual void AddEntity(Entity entity)
		{
			object obj = this.packUnpackLock;
			lock (obj)
			{
				Entity[] Entities = this.Entities;
				if (Entities == null)
				{
					Entities = (this.Entities = new Entity[32]);
					this.EntitiesCount = 0;
				}
				else
				{
					for (int i = 0; i < Entities.Length; i++)
					{
						Entity e = Entities[i];
						if (e == null)
						{
							if (i >= this.EntitiesCount)
							{
								break;
							}
						}
						else if (e.EntityId == entity.EntityId)
						{
							Entities[i] = entity;
							return;
						}
					}
					if (this.EntitiesCount >= Entities.Length)
					{
						Array.Resize<Entity>(ref this.Entities, this.EntitiesCount + 32);
						Entities = this.Entities;
					}
				}
				Entity[] array = Entities;
				int entitiesCount = this.EntitiesCount;
				this.EntitiesCount = entitiesCount + 1;
				array[entitiesCount] = entity;
			}
		}

		public virtual bool RemoveEntity(long entityId)
		{
			object obj = this.packUnpackLock;
			lock (obj)
			{
				Entity[] Entities;
				if ((Entities = this.Entities) == null)
				{
					return false;
				}
				int EntitiesCount = this.EntitiesCount;
				for (int i = 0; i < Entities.Length; i++)
				{
					Entity e = Entities[i];
					if (e == null)
					{
						if (i >= EntitiesCount)
						{
							break;
						}
					}
					else if (e.EntityId == entityId)
					{
						int j = i + 1;
						while (j < Entities.Length && j < EntitiesCount)
						{
							Entities[j - 1] = Entities[j];
							j++;
						}
						Entities[EntitiesCount - 1] = null;
						int entitiesCount = this.EntitiesCount;
						this.EntitiesCount = entitiesCount - 1;
						return true;
					}
				}
			}
			return false;
		}

		public void SetModdata(string key, byte[] data)
		{
			this.ModData[key] = data;
			this.MarkModified();
		}

		public void RemoveModdata(string key)
		{
			this.ModData.Remove(key);
			this.MarkModified();
		}

		public byte[] GetModdata(string key)
		{
			byte[] data;
			this.ModData.TryGetValue(key, out data);
			return data;
		}

		public void SetModdata<T>(string key, T data)
		{
			this.SetModdata(key, SerializerUtil.Serialize<T>(data));
		}

		public T GetModdata<T>(string key, T defaultValue = default(T))
		{
			byte[] data = this.GetModdata(key);
			if (data == null)
			{
				return defaultValue;
			}
			return SerializerUtil.Deserialize<T>(data);
		}

		public void Dispose()
		{
			if (Interlocked.CompareExchange(ref this._disposed, 1, 0) != 0)
			{
				return;
			}
			object obj = this.packUnpackLock;
			lock (obj)
			{
				ChunkData oldData = this.chunkdata;
				this.chunkdata = this.datapool.BlackHoleData;
				this.MaybeBlocks = this.datapool.OnlyAirBlocksData;
				this.Empty = true;
				if (oldData != null)
				{
					this.datapool.Free(oldData);
				}
			}
		}

		public Block GetLocalBlockAtBlockPos(IWorldAccessor world, BlockPos position)
		{
			return this.GetLocalBlockAtBlockPos(world, position.X, position.Y, position.Z, 0);
		}

		public Block GetLocalBlockAtBlockPos(IWorldAccessor world, int posX, int posY, int posZ, int layer = 0)
		{
			int lx = posX % 32;
			int ly = posY % 32;
			int lz = posZ % 32;
			return world.Blocks[this.UnpackAndReadBlock((ly * 32 + lz) * 32 + lx, layer)];
		}

		public Block GetLocalBlockAtBlockPos_LockFree(IWorldAccessor world, BlockPos pos, int layer = 0)
		{
			int lx = pos.X % 32;
			int ly = pos.Y % 32;
			int lz = pos.Z % 32;
			int blockId = this.chunkdata.GetBlockIdUnsafe((ly * 32 + lz) * 32 + lx, layer);
			return world.Blocks[blockId];
		}

		public BlockEntity GetLocalBlockEntityAtBlockPos(BlockPos position)
		{
			BlockEntity blockEntity;
			this.BlockEntities.TryGetValue(position, out blockEntity);
			return blockEntity;
		}

		public virtual void FinishLightDoubleBuffering()
		{
		}

		public int GetLightAbsorptionAt(int index3d, BlockPos blockPos, IList<Block> blockTypes)
		{
			int solidBlockId = this.chunkdata.GetSolidBlock(index3d);
			int fluidBlockId = this.chunkdata.GetFluid(index3d);
			if (solidBlockId == 0)
			{
				return blockTypes[fluidBlockId].LightAbsorption;
			}
			int absSolid = blockTypes[solidBlockId].GetLightAbsorption(this, blockPos);
			if (fluidBlockId == 0)
			{
				return absSolid;
			}
			int absFluid = blockTypes[fluidBlockId].LightAbsorption;
			return Math.Max(absSolid, absFluid);
		}

		public bool SetDecor(Block block, int index3d, BlockFacing onFace)
		{
			if (block == null)
			{
				return false;
			}
			index3d += DecorBits.FaceToIndex(onFace);
			this.SetDecorInternal(index3d, block);
			return true;
		}

		public bool SetDecor(Block block, int index3d, int faceAndSubposition)
		{
			if (block == null)
			{
				return false;
			}
			int packedIndex = index3d + DecorBits.FaceAndSubpositionToIndex(faceAndSubposition);
			this.SetDecorInternal(packedIndex, block);
			return true;
		}

		private void SetDecorInternal(int packedIndex, Block block)
		{
			if (this.Decors == null)
			{
				this.Decors = new Dictionary<int, Block>();
			}
			Dictionary<int, Block> decors = this.Decors;
			lock (decors)
			{
				if (block.Id == 0)
				{
					this.Decors.Remove(packedIndex);
				}
				else
				{
					this.Decors[packedIndex] = block;
				}
			}
		}

		public Dictionary<int, Block> GetSubDecors(IBlockAccessor blockAccessor, BlockPos position)
		{
			if (this.Decors == null || this.Decors.Count == 0)
			{
				return null;
			}
			int index3d = WorldChunk.ToIndex3d(position);
			Dictionary<int, Block> decors = new Dictionary<int, Block>();
			foreach (KeyValuePair<int, Block> val in this.Decors)
			{
				int packedIndex = val.Key;
				if (DecorBits.Index3dFromIndex(packedIndex) == index3d)
				{
					decors[DecorBits.FaceAndSubpositionFromIndex(packedIndex)] = val.Value;
				}
			}
			return decors;
		}

		public Block[] GetDecors(IBlockAccessor blockAccessor, BlockPos position)
		{
			if (this.Decors == null || this.Decors.Count == 0)
			{
				return null;
			}
			int index3d = WorldChunk.ToIndex3d(position);
			Block[] decors = new Block[6];
			foreach (KeyValuePair<int, Block> val in this.Decors)
			{
				int packedIndex = val.Key;
				if (DecorBits.Index3dFromIndex(packedIndex) == index3d)
				{
					decors[DecorBits.FaceFromIndex(packedIndex)] = val.Value;
				}
			}
			return decors;
		}

		public Block GetDecor(IBlockAccessor blockAccessor, BlockPos position, int faceAndSubposition)
		{
			if (this.Decors == null || this.Decors.Count == 0)
			{
				return null;
			}
			int packedIndex = WorldChunk.ToIndex3d(position) + DecorBits.FaceAndSubpositionToIndex(faceAndSubposition);
			return this.TryGetDecor(ref packedIndex, BlockFacing.NORTH);
		}

		public bool BreakDecor(IWorldAccessor world, BlockPos pos, BlockFacing side = null, int? faceAndSubposition = null)
		{
			if (this.Decors == null || this.Decors.Count == 0)
			{
				return false;
			}
			int index3d = WorldChunk.ToIndex3d(pos);
			Dictionary<int, Block> dictionary;
			if (side == null && faceAndSubposition == null)
			{
				List<int> toRemove = new List<int>();
				foreach (KeyValuePair<int, Block> val in this.Decors)
				{
					int packedIndex = val.Key;
					if (DecorBits.Index3dFromIndex(packedIndex) == index3d)
					{
						Block value = val.Value;
						toRemove.Add(packedIndex);
						value.OnBrokenAsDecor(world, pos, DecorBits.FacingFromIndex(packedIndex));
					}
				}
				dictionary = this.Decors;
				lock (dictionary)
				{
					foreach (int ix in toRemove)
					{
						this.Decors.Remove(ix);
					}
				}
				return true;
			}
			index3d += ((faceAndSubposition != null) ? DecorBits.FaceAndSubpositionToIndex(faceAndSubposition.Value) : DecorBits.FaceToIndex(side));
			Block decor = this.TryGetDecor(ref index3d, BlockFacing.NORTH);
			if (decor == null)
			{
				return false;
			}
			decor.OnBrokenAsDecor(world, pos, side);
			dictionary = this.Decors;
			lock (dictionary)
			{
				this.Decors.Remove(index3d);
			}
			return true;
		}

		public bool BreakDecorPart(IWorldAccessor world, BlockPos pos, BlockFacing side, int faceAndSubposition)
		{
			return this.setDecorPart(world, pos, side, faceAndSubposition, true);
		}

		public bool RemoveDecorPart(IWorldAccessor world, BlockPos pos, BlockFacing side, int faceAndSubposition)
		{
			return this.setDecorPart(world, pos, side, faceAndSubposition, false);
		}

		private bool setDecorPart(IWorldAccessor world, BlockPos pos, BlockFacing side, int faceAndSubposition, bool callBlockBroken)
		{
			if (this.Decors == null || this.Decors.Count == 0)
			{
				return false;
			}
			int packedIndex = WorldChunk.ToIndex3d(pos) + DecorBits.FaceAndSubpositionToIndex(faceAndSubposition);
			Block decor = this.TryGetDecor(ref packedIndex, BlockFacing.NORTH);
			if (decor == null)
			{
				return false;
			}
			if (callBlockBroken)
			{
				decor.OnBrokenAsDecor(world, pos, side);
			}
			Dictionary<int, Block> decors = this.Decors;
			lock (decors)
			{
				this.Decors.Remove(packedIndex);
			}
			return true;
		}

		public void BreakAllDecorFast(IWorldAccessor world, BlockPos pos, int index3d, bool callOnBrokenAsDecor = true)
		{
			if (this.Decors == null)
			{
				return;
			}
			List<int> toRemove = new List<int>();
			foreach (KeyValuePair<int, Block> val in this.Decors)
			{
				int packedIndex = val.Key;
				if (DecorBits.Index3dFromIndex(packedIndex) == index3d)
				{
					toRemove.Add(packedIndex);
					if (callOnBrokenAsDecor)
					{
						val.Value.OnBrokenAsDecor(world, pos, DecorBits.FacingFromIndex(packedIndex));
					}
				}
			}
			Dictionary<int, Block> decors = this.Decors;
			lock (decors)
			{
				foreach (int ix in toRemove)
				{
					this.Decors.Remove(ix);
				}
			}
		}

		public Cuboidf[] AdjustSelectionBoxForDecor(IBlockAccessor blockAccessor, BlockPos position, Cuboidf[] orig)
		{
			if (this.Decors == null || this.Decors.Count == 0)
			{
				return orig;
			}
			Cuboidf box = orig[0];
			int index3d = WorldChunk.ToIndex3d(position);
			bool changed = false;
			foreach (KeyValuePair<int, Block> val in this.Decors)
			{
				int packedIndex = val.Key;
				if (DecorBits.Index3dFromIndex(packedIndex) == index3d)
				{
					float thickness = val.Value.DecorThickness;
					if (thickness > 0f)
					{
						if (!changed)
						{
							changed = true;
							box = box.Clone();
						}
						box.Expand(DecorBits.FacingFromIndex(packedIndex), thickness);
					}
				}
			}
			if (!changed)
			{
				return orig;
			}
			return new Cuboidf[] { box };
		}

		public List<Cuboidf> GetDecorSelectionBoxes(IBlockAccessor blockAccessor, BlockPos position)
		{
			int chunkEdge = 31;
			int lx = position.X % 32;
			int num = position.Y % 32;
			int lz = position.Z % 32;
			List<Cuboidf> result = new List<Cuboidf>();
			int index = (num * 32 + lz) * 32 + lx;
			if (lz == 0)
			{
				WorldChunk worldChunk = (WorldChunk)blockAccessor.GetChunk(position.X / 32, position.InternalY / 32, (position.Z - 1) / 32);
				if (worldChunk != null)
				{
					worldChunk.AddDecorSelectionBox(result, index + chunkEdge * 32, BlockFacing.NORTH);
				}
			}
			else
			{
				this.AddDecorSelectionBox(result, index - 32, BlockFacing.NORTH);
			}
			if (lz == chunkEdge)
			{
				WorldChunk worldChunk2 = (WorldChunk)blockAccessor.GetChunk(position.X / 32, position.InternalY / 32, (position.Z + 1) / 32);
				if (worldChunk2 != null)
				{
					worldChunk2.AddDecorSelectionBox(result, index - chunkEdge * 32, BlockFacing.SOUTH);
				}
			}
			else
			{
				this.AddDecorSelectionBox(result, index + 32, BlockFacing.SOUTH);
			}
			if (lx == 0)
			{
				WorldChunk worldChunk3 = (WorldChunk)blockAccessor.GetChunk((position.X - 1) / 32, position.InternalY / 32, position.Z / 32);
				if (worldChunk3 != null)
				{
					worldChunk3.AddDecorSelectionBox(result, index + chunkEdge, BlockFacing.WEST);
				}
			}
			else
			{
				this.AddDecorSelectionBox(result, index - 1, BlockFacing.WEST);
			}
			if (lx == chunkEdge)
			{
				WorldChunk worldChunk4 = (WorldChunk)blockAccessor.GetChunk((position.X + 1) / 32, position.InternalY / 32, position.Z / 32);
				if (worldChunk4 != null)
				{
					worldChunk4.AddDecorSelectionBox(result, index - chunkEdge, BlockFacing.EAST);
				}
			}
			else
			{
				this.AddDecorSelectionBox(result, index + 1, BlockFacing.EAST);
			}
			if (num == 0)
			{
				WorldChunk worldChunk5 = (WorldChunk)blockAccessor.GetChunk(position.X / 32, (position.InternalY - 1) / 32, position.Z / 32);
				if (worldChunk5 != null)
				{
					worldChunk5.AddDecorSelectionBox(result, index + chunkEdge * 32 * 32, BlockFacing.DOWN);
				}
			}
			else
			{
				this.AddDecorSelectionBox(result, index - 1024, BlockFacing.DOWN);
			}
			if (num == chunkEdge)
			{
				WorldChunk worldChunk6 = (WorldChunk)blockAccessor.GetChunk(position.X / 32, (position.InternalY + 1) / 32, position.Z / 32);
				if (worldChunk6 != null)
				{
					worldChunk6.AddDecorSelectionBox(result, lz * 32 + lx, BlockFacing.UP);
				}
			}
			else
			{
				this.AddDecorSelectionBox(result, index + 1024, BlockFacing.UP);
			}
			return result;
		}

		private void AddDecorSelectionBox(List<Cuboidf> result, int index, BlockFacing face)
		{
			if (this.Decors == null)
			{
				return;
			}
			Block block = this.TryGetDecor(ref index, face.Opposite);
			if (block == null)
			{
				return;
			}
			float thickness = block.DecorThickness;
			if (thickness == 0f)
			{
				return;
			}
			DecorSelectionBox box;
			switch (face.Index)
			{
			case 0:
				box = new DecorSelectionBox(0f, 0f, 0f, 1f, 1f, thickness);
				break;
			case 1:
				box = new DecorSelectionBox(1f - thickness, 0f, 0f, 1f, 1f, 1f);
				break;
			case 2:
				box = new DecorSelectionBox(0f, 0f, 1f - thickness, 1f, 1f, 1f);
				break;
			case 3:
				box = new DecorSelectionBox(0f, 0f, 0f, thickness, 1f, 1f);
				break;
			case 4:
				box = new DecorSelectionBox(0f, 1f - thickness, 0f, 1f, 1f, 1f);
				break;
			case 5:
				box = new DecorSelectionBox(0f, 0f, 0f, 1f, thickness, 1f);
				break;
			default:
				box = null;
				break;
			}
			box.PosAdjust = face.Normali;
			result.Add(box);
		}

		public Block TryGetDecor(ref int index, BlockFacing face)
		{
			int packedIndexBase = (index & -458753) + DecorBits.FaceToIndex(face);
			for (int rotationData = 0; rotationData <= 7; rotationData++)
			{
				Block block;
				if (this.Decors.TryGetValue(packedIndexBase + (rotationData << 16), out block) && block != null)
				{
					index = packedIndexBase + (rotationData << 16);
					return block;
				}
			}
			return null;
		}

		public void SetDecors(Dictionary<int, Block> newDecors)
		{
			this.Decors = newDecors;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int ToIndex3d(BlockPos pos)
		{
			int lx = pos.X % 32;
			int num = pos.Y % 32;
			int lz = pos.Z % 32;
			return (num * 32 + lz) * 32 + lx;
		}

		public bool WasModified;

		protected ChunkDataPool datapool;

		protected ChunkData chunkdata;

		protected int chunkdataVersion;

		public long lastReadOrWrite;

		protected bool PotentialBlockOrLightingChanges;

		public byte[] blocksCompressedTmp;

		public byte[] lightCompressedTmp;

		public byte[] lightSatCompressedTmp;

		public byte[] fluidsCompressedTmp;

		public Entity[] Entities;

		public Dictionary<BlockPos, BlockEntity> BlockEntities = new Dictionary<BlockPos, BlockEntity>();

		public Dictionary<int, Block> Decors;

		internal object packUnpackLock = new object();

		private int _disposed;
	}
}
