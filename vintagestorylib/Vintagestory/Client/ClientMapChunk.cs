using System;
using System.Collections.Concurrent;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace Vintagestory.Client
{
	public class ClientMapChunk : IMapChunk
	{
		EnumWorldGenPass IMapChunk.CurrentPass
		{
			get
			{
				return EnumWorldGenPass.Done;
			}
			set
			{
			}
		}

		ushort[] IMapChunk.RainHeightMap
		{
			get
			{
				return this.rainheightmap;
			}
		}

		ushort[] IMapChunk.WorldGenTerrainHeightMap
		{
			get
			{
				return this.terrainheightmap;
			}
		}

		IMapRegion IMapChunk.MapRegion
		{
			get
			{
				return null;
			}
		}

		public int[] TopRockIdMap
		{
			get
			{
				return null;
			}
		}

		public ushort YMax { get; set; }

		public byte[] CaveHeightDistort { get; set; }

		public ushort[] SedimentaryThicknessMap
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public ConcurrentDictionary<Vec2i, float> SnowAccum
		{
			get
			{
				return null;
			}
		}

		public void SetData(string key, byte[] data)
		{
		}

		public byte[] GetData(string key)
		{
			return null;
		}

		internal void UpdateFromPacket(Packet_ServerMapChunk mapChunk)
		{
			this.rainheightmap = ArrayConvert.ByteToUshort(mapChunk.RainHeightMap);
			this.terrainheightmap = ArrayConvert.ByteToUshort(mapChunk.TerrainHeightMap);
			this.YMax = (ushort)mapChunk.Ymax;
		}

		public void MarkFresh()
		{
		}

		public void MarkDirty()
		{
		}

		public void SetModdata(string key, byte[] data)
		{
			throw new NotImplementedException();
		}

		public void RemoveModdata(string key)
		{
			throw new NotImplementedException();
		}

		public byte[] GetModdata(string key)
		{
			throw new NotImplementedException();
		}

		public void SetModdata<T>(string key, T data)
		{
			throw new NotImplementedException();
		}

		public T GetModdata<T>(string key, T defaultValue = default(T))
		{
			throw new NotImplementedException();
		}

		private ushort[] rainheightmap;

		private ushort[] terrainheightmap;
	}
}
