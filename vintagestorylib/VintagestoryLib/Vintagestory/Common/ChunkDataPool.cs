using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Server;

namespace Vintagestory.Common
{
	public class ChunkDataPool : IShutDownMonitor
	{
		public virtual bool ShuttingDown
		{
			get
			{
				return this.server.RunPhase >= EnumServerRunPhase.Shutdown;
			}
		}

		public virtual GameMain Game
		{
			get
			{
				return this.server;
			}
		}

		public virtual ILogger Logger
		{
			get
			{
				return ServerMain.Logger;
			}
		}

		protected ChunkDataPool()
		{
		}

		public ChunkDataPool(int chunksize, ServerMain serverMain)
		{
			this.chunksize = chunksize;
			this.BlackHoleData = ChunkData.CreateNew(chunksize, this);
			this.OnlyAirBlocksData = NoChunkData.CreateNew(chunksize);
			this.server = serverMain;
		}

		public void FreeAll()
		{
			List<int[]> list = this.datas;
			lock (list)
			{
				this.datas.Clear();
			}
		}

		public virtual ChunkData Request()
		{
			this.quantityRequestsSinceLastSlowDispose++;
			return ChunkData.CreateNew(this.chunksize, this);
		}

		public void Free(ChunkData cdata)
		{
			this.FreeArraysAndReset(cdata);
		}

		public void FreeArrays(ChunkDataLayer layer)
		{
			List<int[]> list = this.datas;
			lock (list)
			{
				layer.Clear(this.datas);
			}
		}

		public void FreeArraysAndReset(ChunkData cdata)
		{
			List<int[]> list = this.datas;
			lock (list)
			{
				if (this.datas.Count < this.CacheSize * 2)
				{
					cdata.EmptyAndReuseArrays(this.datas);
				}
				else
				{
					cdata.EmptyAndReuseArrays(null);
				}
			}
		}

		internal void Return(int[] released)
		{
			if (released == null)
			{
				throw new Exception("attempting to return null to pool");
			}
			List<int[]> list = this.datas;
			lock (list)
			{
				if (this.datas.Count < this.CacheSize * 2)
				{
					this.datas.Add(released);
				}
			}
		}

		public void SlowDispose()
		{
			if (this.quantityRequestsSinceLastSlowDispose > 50)
			{
				this.quantityRequestsSinceLastSlowDispose = 0;
				return;
			}
			this.quantityRequestsSinceLastSlowDispose = 0;
			List<int[]> list = this.datas;
			lock (list)
			{
				if (this.datas.Count > this.SlowDisposeThreshold * 4)
				{
					for (int i = 0; i < this.SlowDisposeThreshold * 2; i++)
					{
						this.datas.RemoveAt(this.datas.Count - 1);
					}
				}
			}
		}

		public int CountFree()
		{
			return this.datas.Count;
		}

		internal int[] NewData()
		{
			List<int[]> list = this.datas;
			int[] result;
			lock (list)
			{
				if (this.datas.Count == 0)
				{
					result = new int[1024];
				}
				else
				{
					result = this.datas[this.datas.Count - 1];
					this.datas.RemoveAt(this.datas.Count - 1);
					for (int i = 0; i < result.Length; i += 4)
					{
						result[i] = 0;
						result[i + 1] = 0;
						result[i + 2] = 0;
						result[i + 3] = 0;
					}
				}
			}
			return result;
		}

		internal int[] NewData_NoClear()
		{
			List<int[]> list = this.datas;
			int[] result;
			lock (list)
			{
				if (this.datas.Count == 0)
				{
					result = new int[1024];
				}
				else
				{
					result = this.datas[this.datas.Count - 1];
					this.datas.RemoveAt(this.datas.Count - 1);
				}
			}
			return result;
		}

		protected List<int[]> datas = new List<int[]>();

		protected int chunksize;

		protected int quantityRequestsSinceLastSlowDispose;

		public int SlowDisposeThreshold = 1000;

		public int CacheSize = 1500;

		public ChunkData BlackHoleData;

		public IChunkBlocks OnlyAirBlocksData;

		public ServerMain server;
	}
}
