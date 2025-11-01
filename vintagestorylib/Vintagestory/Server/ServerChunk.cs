using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace Vintagestory.Server
{
	[ProtoContract]
	public class ServerChunk : WorldChunk, IServerChunk, IWorldChunk, IWithFastSerialize
	{
		static ServerChunk()
		{
			ServerChunk.ReadWriteStopWatch.Start();
		}

		[ProtoMember(1)]
		internal new byte[] blocksCompressed
		{
			get
			{
				return base.blocksCompressed;
			}
			set
			{
				base.blocksCompressed = value;
			}
		}

		[ProtoMember(2)]
		internal new byte[] lightCompressed
		{
			get
			{
				return base.lightCompressed;
			}
			set
			{
				base.lightCompressed = value;
			}
		}

		[ProtoMember(3)]
		internal new byte[] lightSatCompressed
		{
			get
			{
				return base.lightSatCompressed;
			}
			set
			{
				base.lightSatCompressed = value;
			}
		}

		[ProtoMember(16)]
		internal byte[] liquidsCompressed
		{
			get
			{
				return base.fluidsCompressed;
			}
			set
			{
				base.fluidsCompressed = value;
			}
		}

		[ProtoMember(5)]
		[CustomFastSerializer]
		public new int EntitiesCount
		{
			get
			{
				return base.EntitiesCount;
			}
			set
			{
				base.EntitiesCount = value;
			}
		}

		public override IMapChunk MapChunk
		{
			get
			{
				return this.serverMapChunk;
			}
		}

		public bool NotAtEdge
		{
			get
			{
				return this.serverMapChunk != null && this.serverMapChunk.NeighboursLoaded.Value() == 511;
			}
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

		string IServerChunk.GameVersionCreated
		{
			get
			{
				return this.GameVersionCreated;
			}
		}

		int IServerChunk.BlocksPlaced
		{
			get
			{
				return this.BlocksPlaced;
			}
		}

		int IServerChunk.BlocksRemoved
		{
			get
			{
				return this.BlocksRemoved;
			}
		}

		private ServerChunk()
		{
		}

		public static ServerChunk CreateNew(ChunkDataPool datapool)
		{
			ServerChunk serverChunk = new ServerChunk();
			serverChunk.datapool = datapool;
			serverChunk.PotentialBlockOrLightingChanges = true;
			serverChunk.chunkdataVersion = 2;
			serverChunk.chunkdata = datapool.Request();
			serverChunk.GameVersionCreated = "1.21.5";
			serverChunk.lightPositions = new HashSet<int>();
			serverChunk.moddata = new Dictionary<string, byte[]>();
			serverChunk.ServerSideModdata = new Dictionary<string, byte[]>();
			serverChunk.LiveModData = new Dictionary<string, object>();
			serverChunk.MaybeBlocks = datapool.OnlyAirBlocksData;
			serverChunk.MarkModified();
			return serverChunk;
		}

		public void RemoveEntitiesAndBlockEntities(IServerWorldAccessor server)
		{
			Entity[] Entities = this.Entities;
			if (Entities != null)
			{
				EntityDespawnData reason = new EntityDespawnData
				{
					Reason = EnumDespawnReason.Unload
				};
				for (int i = 0; i < Entities.Length; i++)
				{
					Entity entity = Entities[i];
					if (entity == null)
					{
						if (i >= this.EntitiesCount)
						{
							break;
						}
					}
					else if (!(entity is EntityPlayer))
					{
						server.DespawnEntity(entity, reason);
					}
				}
			}
			foreach (KeyValuePair<BlockPos, BlockEntity> val in this.BlockEntities)
			{
				val.Value.OnBlockUnloaded();
			}
		}

		public void ClearData()
		{
			ChunkData oldData = this.chunkdata;
			this.PotentialBlockOrLightingChanges = true;
			this.chunkdataVersion = 2;
			this.chunkdata = this.datapool.Request();
			this.GameVersionCreated = "1.21.5";
			this.lightPositions = new HashSet<int>();
			this.moddata = new Dictionary<string, byte[]>();
			this.ServerSideModdata = new Dictionary<string, byte[]>();
			base.MaybeBlocks = this.datapool.OnlyAirBlocksData;
			this.MarkModified();
			base.Empty = true;
			if (oldData != null)
			{
				this.datapool.Free(oldData);
			}
		}

		public static ServerChunk FromBytes(byte[] serializedChunk, ChunkDataPool datapool, IWorldAccessor worldForResolve)
		{
			if (datapool == null)
			{
				throw new MissingFieldException("datapool cannot be null");
			}
			ServerChunk chunk;
			using (MemoryStream ms = new MemoryStream(serializedChunk))
			{
				chunk = Serializer.Deserialize<ServerChunk>(ms);
			}
			chunk.chunkdataVersion = chunk.savedCompressionVersion;
			chunk.datapool = datapool;
			if (chunk.blocksCompressed == null || chunk.lightCompressed == null || chunk.lightSatCompressed == null)
			{
				chunk.Unpack_MaybeNullData();
			}
			chunk.AfterDeserialization(worldForResolve);
			if (chunk.lightPositions == null)
			{
				chunk.lightPositions = new HashSet<int>();
			}
			if (chunk.moddata == null)
			{
				chunk.moddata = new Dictionary<string, byte[]>();
			}
			if (chunk.ServerSideModdata == null)
			{
				chunk.ServerSideModdata = new Dictionary<string, byte[]>();
			}
			if (chunk.LiveModData == null)
			{
				chunk.LiveModData = new Dictionary<string, object>();
			}
			chunk.MaybeBlocks = datapool.OnlyAirBlocksData;
			return chunk;
		}

		public byte[] ToBytes()
		{
			byte[] array;
			using (FastMemoryStream ms = new FastMemoryStream())
			{
				array = this.ToBytes(ms);
			}
			return array;
		}

		public byte[] ToBytes(FastMemoryStream ms)
		{
			object packUnpackLock = this.packUnpackLock;
			byte[] array;
			lock (packUnpackLock)
			{
				if (!this.IsPacked())
				{
					this.Pack();
					this.blocksCompressed = this.blocksCompressedTmp;
					this.lightCompressed = this.lightCompressedTmp;
					this.lightSatCompressed = this.lightSatCompressedTmp;
					this.liquidsCompressed = this.fluidsCompressedTmp;
					this.chunkdataVersion = 2;
				}
				this.savedCompressionVersion = this.chunkdataVersion;
				this.BeforeSerializationCommon();
				if (ServerChunk.reusableSerializationStream == null)
				{
					ServerChunk.reusableSerializationStream = new FastMemoryStream();
				}
				array = ((IWithFastSerialize)this).FastSerialize(ms);
			}
			return array;
		}

		protected override void UpdateForVersion()
		{
			this.chunkdata.UpdateFluids();
			this.chunkdataVersion = 2;
			this.DirtyForSaving = true;
			this.PotentialBlockOrLightingChanges = true;
		}

		public void MarkToPack()
		{
			this.PotentialBlockOrLightingChanges = true;
		}

		private void BeforeSerializationCommon()
		{
			foreach (KeyValuePair<string, object> var in base.LiveModData)
			{
				base.SetModdata<object>(var.Key, var.Value);
			}
			this.EmptyBeforeSave = base.Empty;
		}

		private int GatherEntitiesToSerialize(List<Entity> list)
		{
			int cnt = 0;
			Entity[] Entities = this.Entities;
			for (int i = 0; i < Entities.Length; i++)
			{
				Entity entity = Entities[i];
				if (entity == null)
				{
					if (i >= this.EntitiesCount)
					{
						break;
					}
				}
				else if (!entity.StoreWithChunk)
				{
					cnt++;
				}
				else
				{
					list.Add(entity);
					cnt++;
				}
			}
			return cnt;
		}

		private void AfterDeserialization(IWorldAccessor worldAccessorForResolve)
		{
			object packUnpackLock = this.packUnpackLock;
			lock (packUnpackLock)
			{
				base.Empty = this.EmptyBeforeSave;
				if (this.EntitiesSerialized == null)
				{
					this.Entities = Array.Empty<Entity>();
					this.EntitiesCount = 0;
				}
				else
				{
					Entity[] entities = new Entity[this.EntitiesSerialized.Count];
					Dictionary<string, string> entityRemappings = ((IServerWorldAccessor)worldAccessorForResolve).RemappedEntities;
					int cnt = 0;
					for (int i = 0; i < entities.Length; i++)
					{
						string className = "unknown";
						try
						{
							using (MemoryStream ms = new MemoryStream(this.EntitiesSerialized[i]))
							{
								BinaryReader reader = new BinaryReader(ms);
								className = reader.ReadString();
								Entity entity = ServerMain.ClassRegistry.CreateEntity(className);
								entity.FromBytes(reader, false, entityRemappings);
								entities[cnt++] = entity;
							}
						}
						catch (Exception e)
						{
							ServerMain.Logger.Error("Failed loading an entity (type " + className + ") in a chunk. Will discard, sorry. Exception logged to verbose debug.");
							ServerMain.Logger.VerboseDebug("Failed loading an entity in a chunk. Will discard, sorry. Exception: {0}", new object[] { LoggerBase.CleanStackTrace(e.ToString()) });
						}
					}
					this.Entities = entities;
					this.EntitiesCount = cnt;
					this.EntitiesSerialized = null;
				}
				if (this.BlockEntitiesSerialized != null)
				{
					foreach (byte[] array in this.BlockEntitiesSerialized)
					{
						using (MemoryStream ms2 = new MemoryStream(array))
						{
							using (BinaryReader reader2 = new BinaryReader(ms2))
							{
								string className2;
								try
								{
									className2 = reader2.ReadString();
								}
								catch (Exception)
								{
									ServerMain.Logger.Error("Badly corrupted BlockEntity data in a chunk. Will discard it. Sorry.");
									continue;
								}
								string blockCode = null;
								try
								{
									TreeAttribute tree = new TreeAttribute();
									tree.FromBytes(reader2);
									BlockEntity entity2 = ServerMain.ClassRegistry.CreateBlockEntity(className2);
									Block block = null;
									blockCode = tree.GetString("blockCode", null);
									if (blockCode != null)
									{
										block = worldAccessorForResolve.GetBlock(new AssetLocation(blockCode));
									}
									if (block == null)
									{
										block = base.GetLocalBlockAtBlockPos(worldAccessorForResolve, tree.GetInt("posx", 0), tree.GetInt("posy", 0), tree.GetInt("posz", 0), 0);
										if (((block != null) ? block.Code : null) != null)
										{
											tree.SetString("blockCode", block.Code.ToShortString());
										}
									}
									if (((block != null) ? block.Code : null) == null)
									{
										int posx = tree.GetInt("posx", 0);
										int posy = tree.GetInt("posy", 0);
										int posz = tree.GetInt("posz", 0);
										worldAccessorForResolve.Logger.Notification("Block entity with classname {3} at {0}, {1}, {2} has a block that is null or whose code is null o.O? Won't load this block entity!", new object[] { posx, posy, posz, className2 });
									}
									else
									{
										entity2.CreateBehaviors(block, worldAccessorForResolve);
										entity2.FromTreeAttributes(tree, worldAccessorForResolve);
										this.BlockEntities[entity2.Pos] = entity2;
									}
								}
								catch (Exception e2)
								{
									ServerMain.Logger.Error("Failed loading blockentity {0} for block {1} in a chunk. Will discard it. Sorry. Exception logged to verbose debug.", new object[] { className2, blockCode });
									ServerMain.Logger.VerboseDebug("Failed loading a blockentity in a chunk. Will discard it. Sorry. Exception: {0}", new object[] { LoggerBase.CleanStackTrace(e2.ToString()) });
								}
							}
						}
					}
					this.BlockEntitiesCount = this.BlockEntities.Count;
					this.BlockEntitiesSerialized = null;
				}
				if (this.DecorsSerialized != null && this.DecorsSerialized.Length != 0)
				{
					this.Decors = new Dictionary<int, Block>();
					using (MemoryStream ms3 = new MemoryStream(this.DecorsSerialized))
					{
						BinaryReader reader3 = new BinaryReader(ms3);
						while (reader3.BaseStream.Position < reader3.BaseStream.Length)
						{
							int index3d = reader3.ReadInt32();
							int blockId = reader3.ReadInt32();
							Block dec = worldAccessorForResolve.GetBlock(blockId);
							this.Decors.Add(index3d, dec);
						}
					}
					this.DecorsSerialized = null;
				}
				if (this.LightPositions == null)
				{
					this.LightPositions = new HashSet<int>(0);
				}
				if (this.moddata == null)
				{
					this.moddata = new Dictionary<string, byte[]>(0);
				}
			}
		}

		public override void AddEntity(Entity entity)
		{
			base.AddEntity(entity);
			this.MarkModified();
		}

		public override bool RemoveEntity(long entityId)
		{
			bool flag = base.RemoveEntity(entityId);
			if (flag)
			{
				this.MarkModified();
			}
			return flag;
		}

		internal Packet_ServerChunk ToPacket(int posX, int posY, int posZ, bool withEntities = false)
		{
			Packet_ServerChunk packet = new Packet_ServerChunk
			{
				X = posX,
				Y = posY,
				Z = posZ
			};
			object packUnpackLock = this.packUnpackLock;
			lock (packUnpackLock)
			{
				foreach (KeyValuePair<string, object> var in base.LiveModData)
				{
					base.SetModdata<object>(var.Key, var.Value);
				}
				if (this.chunkdata == null && this.chunkdataVersion < 2)
				{
					this.Unpack();
				}
				if (this.chunkdata != null)
				{
					this.UpdateEmptyFlag();
					if (this.PotentialBlockOrLightingChanges)
					{
						this.chunkdataVersion = 2;
						byte[] bcTmp = null;
						byte[] lcTmp = null;
						byte[] lscTmp = null;
						byte[] lqTmp = null;
						this.chunkdata.CompressInto(ref bcTmp, ref lcTmp, ref lscTmp, ref lqTmp, this.chunkdataVersion);
						base.blocksCompressed = bcTmp;
						base.lightCompressed = lcTmp;
						base.lightSatCompressed = lscTmp;
						base.fluidsCompressed = lqTmp;
						this.PotentialBlockOrLightingChanges = false;
					}
					if ((long)Environment.TickCount - this.lastReadOrWrite > (long)MagicNum.UncompressedChunkTTL)
					{
						this.datapool.Free(this.chunkdata);
						base.MaybeBlocks = this.datapool.OnlyAirBlocksData;
						this.chunkdata = null;
					}
				}
				packet.Empty = ((base.Empty > false) ? 1 : 0);
				packet.SetBlocks(this.blocksCompressed);
				packet.SetLight(this.lightCompressed);
				packet.SetLightSat(this.lightSatCompressed);
				packet.SetLiquids(this.liquidsCompressed);
				packet.SetCompver(this.chunkdataVersion);
			}
			Packet_ServerChunk packet_ServerChunk = packet;
			Dictionary<string, byte[]> dictionary = this.moddata;
			FastMemoryStream fastMemoryStream;
			if ((fastMemoryStream = ServerChunk.reusableSerializationStream) == null)
			{
				fastMemoryStream = (ServerChunk.reusableSerializationStream = new FastMemoryStream());
			}
			packet_ServerChunk.SetModdata(SerializerUtil.Serialize<Dictionary<string, byte[]>>(dictionary, fastMemoryStream));
			if (this.BlockEntities.Count > 0)
			{
				packet.SetBlockEntities(this.GetBlockEntitiesPackets());
			}
			if (this.LightPositions != null && this.LightPositions.Count > 0)
			{
				packet.SetLightPositions(this.LightPositions.ToArray<int>());
			}
			if (this.Decors != null && this.Decors.Count > 0)
			{
				int[] decorsPos = new int[this.Decors.Count];
				int[] decorsIds = new int[this.Decors.Count];
				int i = 0;
				foreach (KeyValuePair<int, Block> val in this.Decors)
				{
					if (i >= decorsPos.Length)
					{
						break;
					}
					decorsPos[i] = val.Key;
					decorsIds[i] = val.Value.BlockId;
					i++;
				}
				packet.SetDecorsPos(decorsPos);
				packet.SetDecorsIds(decorsIds);
			}
			return packet;
		}

		internal Packet_Entity[] GetEntitiesPackets()
		{
			Packet_Entity[] packets = new Packet_Entity[(this.Entities == null) ? 0 : this.EntitiesCount];
			if (packets.Length != 0)
			{
				FastMemoryStream fastMemoryStream;
				if ((fastMemoryStream = ServerChunk.reusableSerializationStream) == null)
				{
					fastMemoryStream = (ServerChunk.reusableSerializationStream = new FastMemoryStream());
				}
				using (FastMemoryStream ms = fastMemoryStream)
				{
					ms.Reset();
					BinaryWriter writer = new BinaryWriter(ms);
					int i = 0;
					Entity[] Entities = this.Entities;
					for (int j = 0; j < Entities.Length; j++)
					{
						Entity entity = Entities[j];
						if (entity == null)
						{
							if (j >= this.EntitiesCount)
							{
								break;
							}
						}
						else
						{
							packets[i++] = ServerPackets.GetEntityPacket(entity, ms, writer);
						}
					}
					if (i != packets.Length)
					{
						Array.Resize<Packet_Entity>(ref packets, i);
					}
				}
			}
			return packets;
		}

		internal Packet_BlockEntity[] GetBlockEntitiesPackets()
		{
			Packet_BlockEntity[] packets = new Packet_BlockEntity[this.BlockEntities.Count];
			FastMemoryStream fastMemoryStream;
			if ((fastMemoryStream = ServerChunk.reusableSerializationStream) == null)
			{
				fastMemoryStream = (ServerChunk.reusableSerializationStream = new FastMemoryStream());
			}
			Packet_BlockEntity[] array;
			using (FastMemoryStream ms = fastMemoryStream)
			{
				ms.Reset();
				BinaryWriter writer = new BinaryWriter(ms);
				int i = 0;
				Dictionary<Type, string> blockEntityRegistry = ServerMain.ClassRegistry.blockEntityTypeToClassnameMapping;
				foreach (BlockEntity be in this.BlockEntities.Values)
				{
					string beClass;
					if (be != null && blockEntityRegistry.TryGetValue(be.GetType(), out beClass))
					{
						packets[i++] = ServerPackets.getBlockEntityPacket(be, beClass, ms, writer);
					}
				}
				array = packets;
			}
			return array;
		}

		public override void MarkModified()
		{
			base.MarkModified();
			this.DirtyForSaving = true;
		}

		public void SetServerModdata(string key, byte[] data)
		{
			this.ServerSideModdata[key] = data;
		}

		public byte[] GetServerModdata(string key)
		{
			byte[] data;
			this.ServerSideModdata.TryGetValue(key, out data);
			return data;
		}

		public void ClearAll(IServerWorldAccessor worldAccessor)
		{
			this.RemoveEntitiesAndBlockEntities(worldAccessor);
			this.ClearData();
			Dictionary<BlockPos, BlockEntity> blockEntities = this.BlockEntities;
			if (blockEntities != null)
			{
				blockEntities.Clear();
			}
			Dictionary<int, Block> decors = this.Decors;
			if (decors != null)
			{
				decors.Clear();
			}
			this.Entities = null;
		}

		private void FastSerializeEntitiesCount(FastMemoryStream ms, int idCount, ref int count, ref int savedPosition)
		{
			if (this.Entities == null || this.Entities.Length == 0)
			{
				return;
			}
			savedPosition = (int)ms.Position + 1;
			List<Entity> list;
			if ((list = ServerChunk.reusableSerializationList) == null)
			{
				list = (ServerChunk.reusableSerializationList = new List<Entity>(this.EntitiesCount));
			}
			List<Entity> EntitiesToSend = list;
			EntitiesToSend.Clear();
			count = this.GatherEntitiesToSerialize(EntitiesToSend);
			FastSerializer.Write(ms, idCount, count);
		}

		private void FastSerializeEntities(FastMemoryStream ms, int idEntitySerialized, ref int count, ref int savedPosition)
		{
			if (this.Entities == null || this.Entities.Length == 0)
			{
				return;
			}
			List<Entity> EntitiesToSend = ServerChunk.reusableSerializationList;
			FastMemoryStream secondBuffer = ServerChunk.reusableSerializationStream;
			if (EntitiesToSend.Count > 0)
			{
				BinaryWriter writer = new BinaryWriter(secondBuffer);
				int failedCount = 0;
				foreach (Entity entity in EntitiesToSend)
				{
					try
					{
						secondBuffer.Reset();
						writer.Write(ServerMain.ClassRegistry.GetEntityClassName(entity.GetType()));
						entity.ToBytes(writer, false);
						FastSerializer.Write(ms, idEntitySerialized, secondBuffer);
					}
					catch (Exception e)
					{
						ServerMain.Logger.Error("Error thrown trying to serialize entity with code {0}, will not save, sorry!", new object[] { (entity != null) ? entity.Code : null });
						ServerMain.Logger.Error(e);
						failedCount++;
					}
				}
				if (failedCount > 0 && failedCount <= count)
				{
					ms.WriteAt(savedPosition, count - failedCount, FastSerializer.GetSize(count));
				}
			}
			EntitiesToSend.Clear();
		}

		private void FastSerializeBlockEntitiesCount(FastMemoryStream ms, int idCount, ref int count, ref int savedPosition)
		{
			count = this.BlockEntities.Count;
			if (count == 0)
			{
				return;
			}
			savedPosition = (int)ms.Position + 1;
			FastSerializer.Write(ms, idCount, count);
		}

		private void FastSerializeBlockEntities(FastMemoryStream ms, int idEntitySerialized, ref int count, ref int savedPosition)
		{
			FastMemoryStream secondBuffer = ServerChunk.reusableSerializationStream;
			BinaryWriter writer = new BinaryWriter(secondBuffer);
			int failedCount = 0;
			foreach (BlockEntity be in this.BlockEntities.Values)
			{
				try
				{
					secondBuffer.Reset();
					string classsName = ServerMain.ClassRegistry.blockEntityTypeToClassnameMapping[be.GetType()];
					writer.Write(classsName);
					TreeAttribute tree = new TreeAttribute();
					be.ToTreeAttributes(tree);
					tree.ToBytes(writer);
					FastSerializer.Write(ms, idEntitySerialized, secondBuffer);
				}
				catch (Exception e)
				{
					ServerMain.Logger.Error("Error thrown trying to serialize block entity {0} at {1}, will not save, sorry!", new object[]
					{
						(be != null) ? be.GetType() : null,
						(be != null) ? be.Pos : null
					});
					ServerMain.Logger.Error(e);
					failedCount++;
				}
			}
			if (failedCount > 0 && failedCount <= count)
			{
				ms.WriteAt(savedPosition, count - failedCount, FastSerializer.GetSize(count));
			}
			this.BlockEntitiesCount = count - failedCount;
		}

		private void FastSerializeDecors(FastMemoryStream ms, int id, ref int count, ref int savedPosition)
		{
			if (this.Decors != null && this.Decors.Count != 0)
			{
				FastSerializer.WriteTagLengthDelim(ms, id, this.Decors.Count * 8);
				foreach (KeyValuePair<int, Block> val in this.Decors)
				{
					Block de = val.Value;
					ms.WriteInt32(val.Key);
					ms.WriteInt32(de.BlockId);
				}
			}
		}

		public byte[] FastSerialize(FastMemoryStream ms)
		{
			ms.Reset();
			int count = 0;
			int position = 0;
			FastSerializer.Write(ms, 1, this.blocksCompressed);
			FastSerializer.Write(ms, 2, this.lightCompressed);
			FastSerializer.Write(ms, 3, this.lightSatCompressed);
			this.FastSerializeEntitiesCount(ms, 5, ref count, ref position);
			this.FastSerializeEntities(ms, 6, ref count, ref position);
			this.FastSerializeBlockEntitiesCount(ms, 7, ref count, ref position);
			this.FastSerializeBlockEntities(ms, 8, ref count, ref position);
			FastSerializer.Write(ms, 9, this.moddata);
			FastSerializer.Write(ms, 10, this.lightPositions);
			FastSerializer.Write(ms, 11, this.ServerSideModdata);
			FastSerializer.Write(ms, 12, this.GameVersionCreated);
			FastSerializer.Write(ms, 13, this.EmptyBeforeSave);
			this.FastSerializeDecors(ms, 14, ref count, ref position);
			FastSerializer.Write(ms, 15, this.savedCompressionVersion);
			FastSerializer.Write(ms, 16, this.liquidsCompressed);
			FastSerializer.Write(ms, 17, this.BlocksPlaced);
			FastSerializer.Write(ms, 18, this.BlocksRemoved);
			return ms.ToArray();
		}

		public static Stopwatch ReadWriteStopWatch = new Stopwatch();

		[ThreadStatic]
		private static FastMemoryStream reusableSerializationStream;

		[ThreadStatic]
		private static List<Entity> reusableSerializationList;

		[ProtoMember(6)]
		[CustomFastSerializer]
		private List<byte[]> EntitiesSerialized;

		[ProtoMember(7)]
		[CustomFastSerializer]
		public int BlockEntitiesCount;

		[ProtoMember(8)]
		[CustomFastSerializer]
		private List<byte[]> BlockEntitiesSerialized;

		[ProtoMember(9)]
		private Dictionary<string, byte[]> moddata;

		[ProtoMember(10)]
		protected HashSet<int> lightPositions;

		[ProtoMember(11)]
		public Dictionary<string, byte[]> ServerSideModdata;

		[ProtoMember(12)]
		public string GameVersionCreated;

		[ProtoMember(13)]
		public bool EmptyBeforeSave;

		[ProtoMember(14)]
		[CustomFastSerializer]
		public byte[] DecorsSerialized;

		[ProtoMember(15)]
		public int savedCompressionVersion;

		[ProtoMember(17)]
		public int BlocksPlaced;

		[ProtoMember(18)]
		public int BlocksRemoved;

		public ServerMapChunk serverMapChunk;

		public bool DirtyForSaving;
	}
}
