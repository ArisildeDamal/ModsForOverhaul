using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf
{
	public class ClientChunk : WorldChunk, IClientChunk, IWorldChunk
	{
		internal void RemoveDataPoolLocations(ChunkRenderer chunkRenderer)
		{
			this.RemoveCenterDataPoolLocations(chunkRenderer);
			this.RemoveEdgeDataPoolLocations(chunkRenderer);
		}

		internal int RemoveEdgeDataPoolLocations(ChunkRenderer chunkRenderer)
		{
			if (this.edgeModelPoolLocations != null)
			{
				chunkRenderer.RemoveDataPoolLocations(this.edgeModelPoolLocations);
				this.edgeModelPoolLocations = null;
				return 1;
			}
			return 0;
		}

		internal int RemoveCenterDataPoolLocations(ChunkRenderer chunkRenderer)
		{
			if (this.centerModelPoolLocations != null)
			{
				chunkRenderer.RemoveDataPoolLocations(this.centerModelPoolLocations);
				this.centerModelPoolLocations = null;
				return 1;
			}
			return 0;
		}

		public bool IsTraversable(BlockFacing from, BlockFacing to)
		{
			int bitIndex = ClientChunk.traversabilityMapping[from.Index, to.Index];
			return !this.traversabilityFresh || ((this.traversability >> bitIndex) & 1) > 0;
		}

		public void SetTraversable(int from, int to)
		{
			int bitIndex = ClientChunk.traversabilityMapping[from, to];
			this.traversability |= (ushort)(1 << bitIndex);
		}

		public void ClearTraversable()
		{
			this.traversability = 0;
		}

		public override Dictionary<string, byte[]> ModData
		{
			get
			{
				return this.moddata;
			}
			set
			{
				if (value == null)
				{
					throw new NullReferenceException("ModData must not be set to null");
				}
				this.moddata = value;
			}
		}

		public override HashSet<int> LightPositions
		{
			get
			{
				return this.lightPositions;
			}
			set
			{
				this.lightPositions = value;
			}
		}

		public override IMapChunk MapChunk
		{
			get
			{
				return this.clientmapchunk;
			}
		}

		public bool LoadedFromServer
		{
			get
			{
				return this.loadedFromServer;
			}
		}

		static ClientChunk()
		{
			ClientChunk.ReadWriteStopWatch.Start();
			ClientChunk.traversabilityMapping[BlockFacing.NORTH.Index, BlockFacing.EAST.Index] = 0;
			ClientChunk.traversabilityMapping[BlockFacing.EAST.Index, BlockFacing.NORTH.Index] = 0;
			ClientChunk.traversabilityMapping[BlockFacing.NORTH.Index, BlockFacing.WEST.Index] = 1;
			ClientChunk.traversabilityMapping[BlockFacing.WEST.Index, BlockFacing.NORTH.Index] = 1;
			ClientChunk.traversabilityMapping[BlockFacing.NORTH.Index, BlockFacing.SOUTH.Index] = 2;
			ClientChunk.traversabilityMapping[BlockFacing.SOUTH.Index, BlockFacing.NORTH.Index] = 2;
			ClientChunk.traversabilityMapping[BlockFacing.NORTH.Index, BlockFacing.UP.Index] = 3;
			ClientChunk.traversabilityMapping[BlockFacing.UP.Index, BlockFacing.NORTH.Index] = 3;
			ClientChunk.traversabilityMapping[BlockFacing.NORTH.Index, BlockFacing.DOWN.Index] = 4;
			ClientChunk.traversabilityMapping[BlockFacing.DOWN.Index, BlockFacing.NORTH.Index] = 4;
			ClientChunk.traversabilityMapping[BlockFacing.EAST.Index, BlockFacing.SOUTH.Index] = 5;
			ClientChunk.traversabilityMapping[BlockFacing.SOUTH.Index, BlockFacing.EAST.Index] = 5;
			ClientChunk.traversabilityMapping[BlockFacing.EAST.Index, BlockFacing.WEST.Index] = 6;
			ClientChunk.traversabilityMapping[BlockFacing.WEST.Index, BlockFacing.EAST.Index] = 6;
			ClientChunk.traversabilityMapping[BlockFacing.EAST.Index, BlockFacing.UP.Index] = 7;
			ClientChunk.traversabilityMapping[BlockFacing.UP.Index, BlockFacing.EAST.Index] = 7;
			ClientChunk.traversabilityMapping[BlockFacing.EAST.Index, BlockFacing.DOWN.Index] = 8;
			ClientChunk.traversabilityMapping[BlockFacing.DOWN.Index, BlockFacing.EAST.Index] = 8;
			ClientChunk.traversabilityMapping[BlockFacing.SOUTH.Index, BlockFacing.WEST.Index] = 9;
			ClientChunk.traversabilityMapping[BlockFacing.WEST.Index, BlockFacing.SOUTH.Index] = 9;
			ClientChunk.traversabilityMapping[BlockFacing.SOUTH.Index, BlockFacing.UP.Index] = 10;
			ClientChunk.traversabilityMapping[BlockFacing.UP.Index, BlockFacing.SOUTH.Index] = 10;
			ClientChunk.traversabilityMapping[BlockFacing.SOUTH.Index, BlockFacing.DOWN.Index] = 11;
			ClientChunk.traversabilityMapping[BlockFacing.DOWN.Index, BlockFacing.SOUTH.Index] = 11;
			ClientChunk.traversabilityMapping[BlockFacing.WEST.Index, BlockFacing.UP.Index] = 12;
			ClientChunk.traversabilityMapping[BlockFacing.UP.Index, BlockFacing.WEST.Index] = 12;
			ClientChunk.traversabilityMapping[BlockFacing.WEST.Index, BlockFacing.DOWN.Index] = 13;
			ClientChunk.traversabilityMapping[BlockFacing.DOWN.Index, BlockFacing.WEST.Index] = 13;
			ClientChunk.traversabilityMapping[BlockFacing.UP.Index, BlockFacing.DOWN.Index] = 14;
			ClientChunk.traversabilityMapping[BlockFacing.DOWN.Index, BlockFacing.UP.Index] = 14;
		}

		public static ClientChunk CreateNew(ClientChunkDataPool datapool)
		{
			return new ClientChunk
			{
				chunkdataVersion = 2,
				PotentialBlockOrLightingChanges = true,
				chunkdata = datapool.Request(),
				datapool = datapool,
				MaybeBlocks = datapool.OnlyAirBlocksData
			};
		}

		public static ClientChunk CreateNewCompressed(ChunkDataPool datapool, byte[] blocksCompressed, byte[] lightCompressed, byte[] lightSatCompressed, byte[] fluidsCompressed, byte[] moddata, int compver)
		{
			ClientChunk chunk = new ClientChunk();
			chunk.datapool = datapool;
			chunk.chunkdataVersion = compver;
			chunk.lightCompressed = lightCompressed;
			chunk.lightSatCompressed = lightSatCompressed;
			chunk.fluidsCompressed = fluidsCompressed;
			chunk.lastReadOrWrite = (long)Environment.TickCount;
			chunk.moddata = SerializerUtil.Deserialize<Dictionary<string, byte[]>>(moddata);
			chunk.LiveModData = new Dictionary<string, object>();
			chunk.MaybeBlocks = datapool.OnlyAirBlocksData;
			chunk.blocksCompressed = blocksCompressed;
			if (blocksCompressed == null || lightCompressed == null || lightSatCompressed == null)
			{
				chunk.Unpack_MaybeNullData();
			}
			return chunk;
		}

		private ClientChunk()
		{
		}

		public bool ChunkHasData()
		{
			ChunkData chunkdata = this.chunkdata;
			return (chunkdata != null && chunkdata.HasData()) || base.blocksCompressed != null;
		}

		internal void SetVisible(bool visible)
		{
			this.CullVisible[(ClientChunk.bufIndex + 1) % 2] = visible;
		}

		internal bool IsFrustumVisible()
		{
			if (this.edgeModelPoolLocations == null || this.centerModelPoolLocations == null)
			{
				return false;
			}
			if (this.edgeModelPoolLocations.Length == 0 && this.centerModelPoolLocations.Length == 0)
			{
				return false;
			}
			if (this.centerModelPoolLocations.Length == 0)
			{
				return this.edgeModelPoolLocations[0].FrustumVisible;
			}
			return this.centerModelPoolLocations[0].FrustumVisible;
		}

		public virtual bool TemporaryUnpack(int[] blocks)
		{
			object packUnpackLock = this.packUnpackLock;
			lock (packUnpackLock)
			{
				if (this.chunkdata != null)
				{
					this.chunkdata.CopyBlocksTo(blocks);
				}
				else
				{
					ChunkData.UnpackBlocksTo(blocks, base.blocksCompressed, base.lightSatCompressed, this.chunkdataVersion);
				}
			}
			return true;
		}

		internal void PreLoadBlockEntitiesFromPacket(Packet_BlockEntity[] blockEntities, int blockEntitiesCount, ClientMain game)
		{
			this.BlockEntities.Clear();
			for (int i = 0; i < blockEntitiesCount; i++)
			{
				Packet_BlockEntity packet = blockEntities[i];
				BlockEntity blockEntity = ClientMain.ClassRegistry.CreateBlockEntity(packet.Classname);
				using (MemoryStream ms = new MemoryStream(packet.Data))
				{
					using (BinaryReader reader = new BinaryReader(ms))
					{
						TreeAttribute tree = new TreeAttribute();
						tree.FromBytes(reader);
						Block block = base.GetLocalBlockAtBlockPos(game, tree.GetInt("posx", 0), tree.GetInt("posy", 0), tree.GetInt("posz", 0), 0);
						try
						{
							blockEntity.CreateBehaviors(block, game);
							blockEntity.FromTreeAttributes(tree, game);
						}
						catch (Exception e)
						{
							BlockPos pos = new BlockPos(packet.PosX, packet.PosY, packet.PosZ);
							ILogger logger = game.Logger;
							string[] array = new string[5];
							array[0] = "At position ";
							int num = 1;
							BlockPos blockPos = pos;
							array[num] = ((blockPos != null) ? blockPos.ToString() : null);
							array[2] = " with block ";
							array[3] = block.Code.ToShortString();
							array[4] = ", {0} threw an error when being created:";
							logger.Error(string.Concat(array), new object[] { packet.Classname });
							game.Logger.Error(e);
						}
					}
				}
				this.BlockEntities[blockEntity.Pos] = blockEntity;
			}
			this.BlockEntitiesCount = blockEntitiesCount;
		}

		internal void InitBlockEntitiesFromPacket(ClientMain game)
		{
			foreach (BlockEntity be in this.BlockEntities.Values)
			{
				try
				{
					be.Initialize(game.api);
				}
				catch (Exception e)
				{
					if (be != null)
					{
						if (game.ClassRegistryInt != null)
						{
							string classname = game.ClassRegistryInt.blockEntityTypeToClassnameMapping[be.GetType()];
							game.Logger.Error("Exception thrown when initializing a block entity with classname {0}:", new object[] { classname });
						}
						else
						{
							game.Logger.Error("Exception thrown when initializing a block entity {0}:", new object[] { be.GetType() });
						}
						game.Logger.Error(e);
					}
					else
					{
						game.Logger.Error("Exception thrown when initializing a block entity, because it's null. Seems to be a corrupt chunk.");
					}
				}
				if (ScreenManager.FrameProfiler.Enabled)
				{
					ScreenManager.FrameProfiler.Mark("initbe-", game.ClassRegistryInt.blockEntityTypeToClassnameMapping[be.GetType()]);
				}
			}
		}

		internal void AddOrUpdateBlockEntityFromPacket(Packet_BlockEntity p, ClientMain game)
		{
			BlockPos pos = new BlockPos(p.PosX, p.PosY, p.PosZ);
			if (p.Data == null && p.Classname == null)
			{
				this.RemoveBlockEntity(game, pos);
				return;
			}
			BlockEntity blockentity;
			if (this.BlockEntities.TryGetValue(pos, out blockentity))
			{
				Type type = ClientMain.ClassRegistry.GetBlockEntityType(p.Classname);
				if (blockentity.GetType() == type)
				{
					BinaryReader reader = new BinaryReader(new MemoryStream(p.Data));
					ITreeAttribute tree = new TreeAttribute();
					tree.FromBytes(reader);
					try
					{
						blockentity.FromTreeAttributes(tree, game);
					}
					catch (Exception e)
					{
						ILogger logger = game.Logger;
						string text = "At position ";
						BlockPos blockPos = pos;
						logger.Error(text + ((blockPos != null) ? blockPos.ToString() : null) + ", BlockEntity {0} threw an error when being updated:", new object[] { p.Classname });
						game.Logger.Error(e);
					}
					return;
				}
				this.RemoveBlockEntity(game, pos);
			}
			BlockEntity be = ClientSystemEntities.createBlockEntityFromPacket(p, game);
			try
			{
				be.Initialize(game.api);
			}
			catch (Exception e2)
			{
				ILogger logger2 = game.Logger;
				string text2 = "Exception thrown at ";
				BlockPos blockPos2 = pos;
				logger2.Error(text2 + ((blockPos2 != null) ? blockPos2.ToString() : null) + " when initializing a block entity with classname {0}:", new object[] { p.Classname });
				game.Logger.Error(e2);
			}
			this.AddBlockEntity(be);
		}

		private void RemoveBlockEntity(ClientMain game, BlockPos pos)
		{
			BlockEntity blockEntity = game.WorldMap.GetBlockEntity(pos);
			if (blockEntity != null)
			{
				blockEntity.OnBlockRemoved();
			}
			this.RemoveBlockEntity(pos);
		}

		public override void FinishLightDoubleBuffering()
		{
			if (this.chunkdata == null)
			{
				this.Unpack();
			}
			ClientChunkData clientChunkData = (ClientChunkData)this.chunkdata;
			if (clientChunkData == null)
			{
				return;
			}
			clientChunkData.FinishLightDoubleBuffering();
		}

		public void SetVisibility(bool visible)
		{
			bool hidden = !visible;
			if (this.centerModelPoolLocations != null)
			{
				ModelDataPoolLocation[] array = this.centerModelPoolLocations;
				for (int i = 0; i < array.Length; i++)
				{
					array[i].Hide = hidden;
				}
			}
			if (this.edgeModelPoolLocations != null)
			{
				ModelDataPoolLocation[] array = this.edgeModelPoolLocations;
				for (int i = 0; i < array.Length; i++)
				{
					array[i].Hide = hidden;
				}
			}
		}

		public void SetPoolLocations(ref ModelDataPoolLocation[] target, ModelDataPoolLocation[] modelDataPoolLocations, bool hidden)
		{
			if (hidden && modelDataPoolLocations != null)
			{
				for (int i = 0; i < modelDataPoolLocations.Length; i++)
				{
					modelDataPoolLocations[i].Hide = hidden;
				}
			}
			target = modelDataPoolLocations;
		}

		public bool GetHiddenState(ref ModelDataPoolLocation[] target)
		{
			bool hidden = false;
			if (target != null && target.Length != 0)
			{
				hidden = target[0].Hide;
			}
			return hidden;
		}

		public static Stopwatch ReadWriteStopWatch = new Stopwatch();

		internal int BlockEntitiesCount;

		private HashSet<int> lightPositions;

		public ClientMapChunk clientmapchunk;

		private Dictionary<string, byte[]> moddata;

		internal ModelDataPoolLocation[] centerModelPoolLocations;

		internal ModelDataPoolLocation[] edgeModelPoolLocations;

		internal bool queuedForUpload;

		internal long lastTesselationMs;

		internal bool loadedFromServer;

		internal int quantityDrawn;

		internal int quantityRelit;

		internal int quantityOverloads;

		internal bool enquedForRedraw;

		internal bool shouldSunRelight;

		public ushort traversability;

		public bool traversabilityFresh;

		public static int[,] traversabilityMapping = new int[6, 6];

		public Bools CullVisible = new Bools(true, true);

		public static int bufIndex;
	}
}
