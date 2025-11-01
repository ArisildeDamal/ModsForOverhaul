using System;
using System.Collections.Generic;
using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.Server
{
	[ProtoContract]
	public class ServerMapRegion : IMapRegion
	{
		[ProtoMember(13)]
		public IntDataMap2D OreMapVerticalDistortTop { get; set; }

		[ProtoMember(14)]
		public IntDataMap2D OreMapVerticalDistortBottom { get; set; }

		[ProtoMember(16)]
		public Dictionary<string, IntDataMap2D> BlockPatchMaps { get; set; }

		IntDataMap2D IMapRegion.ClimateMap
		{
			get
			{
				return this.ClimateMap;
			}
			set
			{
				this.ClimateMap = value;
			}
		}

		IntDataMap2D IMapRegion.LandformMap
		{
			get
			{
				return this.LandformMap;
			}
			set
			{
				this.LandformMap = value;
			}
		}

		IntDataMap2D IMapRegion.ForestMap
		{
			get
			{
				return this.ForestMap;
			}
			set
			{
				this.ForestMap = value;
			}
		}

		IntDataMap2D IMapRegion.BeachMap
		{
			get
			{
				return this.BeachMap;
			}
			set
			{
				this.BeachMap = value;
			}
		}

		IntDataMap2D IMapRegion.UpheavelMap
		{
			get
			{
				return this.UpheavelMap;
			}
			set
			{
				this.UpheavelMap = value;
			}
		}

		IntDataMap2D IMapRegion.OceanMap
		{
			get
			{
				return this.OceanMap;
			}
			set
			{
				this.OceanMap = value;
			}
		}

		IntDataMap2D IMapRegion.ShrubMap
		{
			get
			{
				return this.BushMap;
			}
			set
			{
				this.BushMap = value;
			}
		}

		IntDataMap2D IMapRegion.FlowerMap
		{
			get
			{
				return this.FlowerMap;
			}
			set
			{
				this.FlowerMap = value;
			}
		}

		IntDataMap2D IMapRegion.GeologicProvinceMap
		{
			get
			{
				return this.GeologicProvinceMap;
			}
			set
			{
				this.GeologicProvinceMap = value;
			}
		}

		IntDataMap2D[] IMapRegion.RockStrata
		{
			get
			{
				return this.RockStrata;
			}
			set
			{
				this.RockStrata = value;
			}
		}

		bool IMapRegion.DirtyForSaving
		{
			get
			{
				return this.DirtyForSaving;
			}
			set
			{
				this.DirtyForSaving = value;
			}
		}

		Dictionary<string, byte[]> IMapRegion.ModData
		{
			get
			{
				return this.ModData;
			}
		}

		Dictionary<string, IntDataMap2D> IMapRegion.ModMaps
		{
			get
			{
				return this.ModMaps;
			}
		}

		Dictionary<string, IntDataMap2D> IMapRegion.OreMaps
		{
			get
			{
				return this.OreMaps;
			}
		}

		List<GeneratedStructure> IMapRegion.GeneratedStructures
		{
			get
			{
				return this.GeneratedStructures;
			}
		}

		public static ServerMapRegion CreateNew()
		{
			return new ServerMapRegion
			{
				LandformMap = IntDataMap2D.CreateEmpty(),
				UpheavelMap = IntDataMap2D.CreateEmpty(),
				ForestMap = IntDataMap2D.CreateEmpty(),
				BushMap = IntDataMap2D.CreateEmpty(),
				FlowerMap = IntDataMap2D.CreateEmpty(),
				ClimateMap = IntDataMap2D.CreateEmpty(),
				BeachMap = IntDataMap2D.CreateEmpty(),
				OreMapVerticalDistortTop = IntDataMap2D.CreateEmpty(),
				OreMapVerticalDistortBottom = IntDataMap2D.CreateEmpty(),
				GeologicProvinceMap = IntDataMap2D.CreateEmpty(),
				OreMaps = new Dictionary<string, IntDataMap2D>(),
				ModMaps = new Dictionary<string, IntDataMap2D>(),
				ModData = new Dictionary<string, byte[]>(),
				GeneratedStructures = new List<GeneratedStructure>(),
				BlockPatchMaps = new Dictionary<string, IntDataMap2D>(),
				OceanMap = IntDataMap2D.CreateEmpty(),
				worldgenVersion = 3,
				createdGameVersion = "1.21.5",
				DirtyForSaving = true
			};
		}

		public void AddGeneratedStructure(GeneratedStructure newStructure)
		{
			List<GeneratedStructure> newlist = new List<GeneratedStructure>(this.GeneratedStructures.Count + 1);
			foreach (GeneratedStructure oldstruct in this.GeneratedStructures)
			{
				newlist.Add(oldstruct);
			}
			newlist.Add(newStructure);
			this.GeneratedStructures = newlist;
			this.DirtyForSaving = true;
		}

		public static ServerMapRegion FromBytes(byte[] serializedMapRegion)
		{
			ServerMapRegion mapreg = SerializerUtil.Deserialize<ServerMapRegion>(serializedMapRegion);
			if (mapreg.OreMaps == null)
			{
				mapreg.OreMaps = new Dictionary<string, IntDataMap2D>();
			}
			if (mapreg.ModMaps == null)
			{
				mapreg.ModMaps = new Dictionary<string, IntDataMap2D>();
			}
			if (mapreg.ModData == null)
			{
				mapreg.ModData = new Dictionary<string, byte[]>();
			}
			if (mapreg.GeneratedStructures == null)
			{
				mapreg.GeneratedStructures = new List<GeneratedStructure>();
			}
			if (mapreg.BeachMap == null)
			{
				mapreg.BeachMap = IntDataMap2D.CreateEmpty();
			}
			if (mapreg.BlockPatchMaps == null)
			{
				mapreg.BlockPatchMaps = new Dictionary<string, IntDataMap2D>();
			}
			if (mapreg.OceanMap == null)
			{
				mapreg.OceanMap = new IntDataMap2D();
			}
			return mapreg;
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
			return SerializerUtil.Serialize<ServerMapRegion>(this, ms);
		}

		public Packet_Server ToPacket(int regionX, int regionZ)
		{
			Packet_MapRegion p = new Packet_MapRegion
			{
				ClimateMap = ServerMapRegion.ToPacket(this.ClimateMap),
				ForestMap = ServerMapRegion.ToPacket(this.ForestMap),
				GeologicProvinceMap = ServerMapRegion.ToPacket(this.GeologicProvinceMap),
				OceanMap = ServerMapRegion.ToPacket(this.OceanMap),
				LandformMap = ServerMapRegion.ToPacket(this.LandformMap),
				RegionX = regionX,
				RegionZ = regionZ
			};
			p.SetGeneratedStructures(ServerMapRegion.ToPacket(this.GeneratedStructures));
			p.SetModdata(SerializerUtil.Serialize<Dictionary<string, byte[]>>(this.ModData));
			return new Packet_Server
			{
				Id = 42,
				MapRegion = p
			};
		}

		private static Packet_GeneratedStructure[] ToPacket(List<GeneratedStructure> generatedStructures)
		{
			Packet_GeneratedStructure[] packets = new Packet_GeneratedStructure[generatedStructures.Count];
			for (int i = 0; i < packets.Length; i++)
			{
				GeneratedStructure struc = generatedStructures[i];
				Packet_GeneratedStructure packet_GeneratedStructure = (packets[i] = new Packet_GeneratedStructure());
				packet_GeneratedStructure.X1 = struc.Location.X1;
				packet_GeneratedStructure.Y1 = struc.Location.Y1;
				packet_GeneratedStructure.Z1 = struc.Location.Z1;
				packet_GeneratedStructure.X2 = struc.Location.X2;
				packet_GeneratedStructure.Y2 = struc.Location.Y2;
				packet_GeneratedStructure.Z2 = struc.Location.Z2;
				packet_GeneratedStructure.Code = struc.Code;
				packet_GeneratedStructure.Group = struc.Group;
			}
			return packets;
		}

		public static Packet_IntMap ToPacket(IntDataMap2D map)
		{
			if (((map != null) ? map.Data : null) == null)
			{
				return new Packet_IntMap
				{
					Data = Array.Empty<int>(),
					DataCount = 0,
					DataLength = 0,
					Size = 0
				};
			}
			return new Packet_IntMap
			{
				Data = map.Data,
				DataCount = map.Data.Length,
				DataLength = map.Data.Length,
				Size = map.Size,
				BottomRightPadding = map.BottomRightPadding,
				TopLeftPadding = map.TopLeftPadding
			};
		}

		public void SetModdata(string key, byte[] data)
		{
			this.ModData[key] = data;
			this.DirtyForSaving = true;
		}

		public void RemoveModdata(string key)
		{
			if (this.ModData.Remove(key))
			{
				this.DirtyForSaving = true;
			}
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

		public T GetModdata<T>(string key)
		{
			byte[] data = this.GetModdata(key);
			if (data != null)
			{
				return SerializerUtil.Deserialize<T>(data);
			}
			return default(T);
		}

		[ProtoMember(1)]
		public IntDataMap2D LandformMap;

		[ProtoMember(2)]
		public IntDataMap2D ForestMap;

		[ProtoMember(3)]
		public IntDataMap2D ClimateMap;

		[ProtoMember(4)]
		public IntDataMap2D GeologicProvinceMap;

		[ProtoMember(5)]
		public IntDataMap2D BushMap;

		[ProtoMember(6)]
		public IntDataMap2D FlowerMap;

		[ProtoMember(8)]
		public Dictionary<string, IntDataMap2D> OreMaps;

		[ProtoMember(9)]
		public Dictionary<string, byte[]> ModData;

		[ProtoMember(10)]
		public Dictionary<string, IntDataMap2D> ModMaps;

		[ProtoMember(11)]
		public List<GeneratedStructure> GeneratedStructures;

		[ProtoMember(12)]
		public IntDataMap2D[] RockStrata;

		[ProtoMember(15)]
		public IntDataMap2D BeachMap;

		[ProtoMember(17)]
		public IntDataMap2D UpheavelMap;

		[ProtoMember(18)]
		public IntDataMap2D OceanMap;

		[ProtoMember(19)]
		public int worldgenVersion;

		[ProtoMember(20)]
		public string createdGameVersion;

		public bool DirtyForSaving;

		public bool NeighbourRegionsChecked;

		public long loadedTotalMs;
	}
}
