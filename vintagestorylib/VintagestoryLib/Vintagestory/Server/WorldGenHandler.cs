using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Vintagestory.Server
{
	public class WorldGenHandler : IWorldGenHandler
	{
		List<MapRegionGeneratorDelegate> IWorldGenHandler.OnMapRegionGen
		{
			get
			{
				return this.OnMapRegionGen;
			}
		}

		List<MapChunkGeneratorDelegate> IWorldGenHandler.OnMapChunkGen
		{
			get
			{
				return this.OnMapChunkGen;
			}
		}

		List<ChunkColumnGenerationDelegate>[] IWorldGenHandler.OnChunkColumnGen
		{
			get
			{
				return this.OnChunkColumnGen;
			}
		}

		public WorldGenHandler()
		{
			this.OnChunkColumnGen[1] = new List<ChunkColumnGenerationDelegate>();
			this.OnChunkColumnGen[2] = new List<ChunkColumnGenerationDelegate>();
			this.OnChunkColumnGen[3] = new List<ChunkColumnGenerationDelegate>();
			this.OnChunkColumnGen[4] = new List<ChunkColumnGenerationDelegate>();
			this.OnChunkColumnGen[5] = new List<ChunkColumnGenerationDelegate>();
		}

		public void WipeAllHandlers()
		{
			this.OnMapRegionGen.Clear();
			this.OnMapChunkGen.Clear();
			for (int i = 0; i < this.OnChunkColumnGen.Length; i++)
			{
				if (this.OnChunkColumnGen[i] != null)
				{
					this.OnChunkColumnGen[i].Clear();
				}
			}
		}

		public List<Action> OnInitWorldGen = new List<Action>();

		public List<MapRegionGeneratorDelegate> OnMapRegionGen = new List<MapRegionGeneratorDelegate>();

		public List<MapChunkGeneratorDelegate> OnMapChunkGen = new List<MapChunkGeneratorDelegate>();

		public List<ChunkColumnGenerationDelegate>[] OnChunkColumnGen = new List<ChunkColumnGenerationDelegate>[6];

		public Dictionary<string, WorldGenHookDelegate> SpecialHooks = new Dictionary<string, WorldGenHookDelegate>();
	}
}
