using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace Vintagestory.Server
{
	[ProtoContract]
	public class ServerMapChunk : IServerMapChunk, IMapChunk, IWithFastSerialize
	{
		public EnumWorldGenPass CurrentIncompletePass
		{
			get
			{
				return (EnumWorldGenPass)this.currentpass;
			}
			set
			{
				this.DirtyForSaving = true;
				this.currentpass = (int)value;
			}
		}

		[ProtoMember(11)]
		public byte[] CaveHeightDistort { get; set; }

		[ProtoMember(12)]
		public ushort[] SedimentaryThicknessMap { get; set; }

		[ProtoMember(15)]
		public ConcurrentDictionary<Vec2i, float> SnowAccum { get; set; }

		EnumWorldGenPass IMapChunk.CurrentPass
		{
			get
			{
				return this.CurrentIncompletePass;
			}
			set
			{
				this.CurrentIncompletePass = value;
			}
		}

		ushort IMapChunk.YMax
		{
			get
			{
				return this.YMax;
			}
			set
			{
				this.YMax = value;
			}
		}

		public bool SelfLoaded
		{
			get
			{
				return this.NeighboursLoaded[8];
			}
			set
			{
				this.NeighboursLoaded[8] = value;
			}
		}

		public static ServerMapChunk CreateNew(ServerMapRegion mapRegion)
		{
			return new ServerMapChunk
			{
				MapRegion = mapRegion,
				Moddata = new Dictionary<string, byte[]>(),
				RainHeightMap = new ushort[MagicNum.ServerChunkSize * MagicNum.ServerChunkSize],
				WorldGenTerrainHeightMap = new ushort[MagicNum.ServerChunkSize * MagicNum.ServerChunkSize],
				TopRockIdMap = new int[MagicNum.ServerChunkSize * MagicNum.ServerChunkSize],
				TopRockIdMapOld = null,
				SedimentaryThicknessMap = new ushort[MagicNum.ServerChunkSize * MagicNum.ServerChunkSize],
				CaveHeightDistort = new byte[MagicNum.ServerChunkSize * MagicNum.ServerChunkSize],
				DirtyForSaving = true,
				CurrentIncompletePass = EnumWorldGenPass.None,
				SnowAccum = new ConcurrentDictionary<Vec2i, float>()
			};
		}

		public static ServerMapChunk FromBytes(byte[] serializedChunk)
		{
			ServerMapChunk mapchunk = Serializer.Deserialize<ServerMapChunk>(new MemoryStream(serializedChunk));
			if (mapchunk.WorldGenTerrainHeightMap == null)
			{
				mapchunk.WorldGenTerrainHeightMap = (ushort[])mapchunk.RainHeightMap.Clone();
			}
			if (mapchunk.TopRockIdMapOld != null)
			{
				if (mapchunk.TopRockIdMap == null)
				{
					mapchunk.TopRockIdMap = new int[mapchunk.TopRockIdMapOld.Length];
				}
				for (int i = 0; i < mapchunk.TopRockIdMapOld.Length; i++)
				{
					mapchunk.TopRockIdMap[i] = (int)mapchunk.TopRockIdMapOld[i];
				}
			}
			if (mapchunk.Moddata == null)
			{
				mapchunk.Moddata = new Dictionary<string, byte[]>();
			}
			if (mapchunk.SnowAccum == null)
			{
				mapchunk.SnowAccum = new ConcurrentDictionary<Vec2i, float>();
			}
			return mapchunk;
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
			return ((IWithFastSerialize)this).FastSerialize(ms);
		}

		public void SetData(string key, byte[] data)
		{
			this.SetModdata(key, data);
		}

		public byte[] GetData(string key)
		{
			return this.GetModdata(key);
		}

		public void SetModdata(string key, byte[] data)
		{
			object obj = this.modDataLock;
			lock (obj)
			{
				this.Moddata[key] = data;
				this.MarkDirty();
			}
		}

		public byte[] GetModdata(string key)
		{
			object obj = this.modDataLock;
			byte[] array;
			lock (obj)
			{
				byte[] data;
				this.Moddata.TryGetValue(key, out data);
				array = data;
			}
			return array;
		}

		public void SetModdata<T>(string key, T data)
		{
			this.SetModdata(key, SerializerUtil.Serialize<T>(data));
		}

		public T GetModdata<T>(string key, T defaultValue = default(T))
		{
			byte[] moddata = this.GetModdata(key);
			if (moddata == null)
			{
				return defaultValue;
			}
			return SerializerUtil.Deserialize<T>(moddata);
		}

		public void RemoveModdata(string key)
		{
			this.Moddata.Remove(key);
			this.MarkDirty();
		}

		ushort[] IMapChunk.RainHeightMap
		{
			get
			{
				return this.RainHeightMap;
			}
		}

		ushort[] IMapChunk.WorldGenTerrainHeightMap
		{
			get
			{
				return this.WorldGenTerrainHeightMap;
			}
		}

		IMapRegion IMapChunk.MapRegion
		{
			get
			{
				return this.MapRegion;
			}
		}

		int[] IMapChunk.TopRockIdMap
		{
			get
			{
				return this.TopRockIdMap;
			}
		}

		public void MarkFresh()
		{
			this.UnloadGeneration = 5;
		}

		public void DoAge()
		{
			this.UnloadGeneration -= 1;
		}

		public bool IsOld()
		{
			return this.UnloadGeneration <= 1;
		}

		public Packet_Server ToPacket(int chunkX, int chunkZ)
		{
			Packet_ServerMapChunk p = new Packet_ServerMapChunk
			{
				ChunkX = chunkX,
				ChunkZ = chunkZ,
				Ymax = (int)this.YMax,
				RainHeightMap = ArrayConvert.UshortToByte(this.RainHeightMap),
				TerrainHeightMap = ArrayConvert.UshortToByte(this.WorldGenTerrainHeightMap),
				Structures = null
			};
			return new Packet_Server
			{
				Id = 17,
				MapChunk = p
			};
		}

		public void MarkDirty()
		{
			this.DirtyForSaving = true;
		}

		public byte[] FastSerialize(FastMemoryStream ms)
		{
			ms.Reset();
			FastSerializer.Write(ms, 1, this.Moddata);
			FastSerializer.Write(ms, 3, this.RainHeightMap);
			FastSerializer.Write(ms, 4, this.currentpass);
			FastSerializer.Write(ms, 6, this.TopRockIdMapOld);
			FastSerializer.Write(ms, 7, this.WorldGenTerrainHeightMap);
			FastSerializer.Write(ms, 8, this.ScheduledBlockUpdates);
			FastSerializer.Write(ms, 9, this.NewBlockEntities);
			FastSerializer.Write(ms, 10, (int)this.YMax);
			FastSerializer.Write(ms, 11, this.CaveHeightDistort);
			FastSerializer.Write(ms, 12, this.SedimentaryThicknessMap);
			FastSerializer.Write(ms, 13, this.TopRockIdMap);
			FastSerializer.Write(ms, 15, this.SnowAccum);
			FastSerializer.Write(ms, 16, this.WorldGenVersion);
			FastSerializer.Write(ms, 17, this.ScheduledBlockLightUpdates);
			return ms.ToArray();
		}

		[ProtoMember(1)]
		public Dictionary<string, byte[]> Moddata;

		[ProtoMember(3)]
		public ushort[] RainHeightMap;

		[ProtoMember(4)]
		internal int currentpass;

		[ProtoMember(6)]
		public ushort[] TopRockIdMapOld;

		[ProtoMember(13)]
		public int[] TopRockIdMap;

		[ProtoMember(7)]
		public ushort[] WorldGenTerrainHeightMap;

		[ProtoMember(8)]
		public List<BlockPos> ScheduledBlockUpdates = new List<BlockPos>();

		[ProtoMember(9)]
		public HashSet<BlockPos> NewBlockEntities = new HashSet<BlockPos>();

		[ProtoMember(10)]
		public ushort YMax;

		[ProtoMember(16)]
		public int WorldGenVersion;

		[ProtoMember(17)]
		public List<Vec4i> ScheduledBlockLightUpdates;

		public int QuantityNeighboursLoaded;

		public ServerMapRegion MapRegion;

		public bool DirtyForSaving;

		private object modDataLock = new object();

		public SmallBoolArray NeighboursLoaded;

		public byte UnloadGeneration = 3;
	}
}
