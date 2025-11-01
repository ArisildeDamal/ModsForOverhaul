using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.Common.Database;

namespace Vintagestory.Client.NoObf
{
	public class TerrainIlluminator : IChunkProvider
	{
		public ILogger Logger
		{
			get
			{
				return this.game.Logger;
			}
		}

		public TerrainIlluminator(ClientMain game)
		{
			this.game = game;
			this.chunkIlluminator = new ChunkIlluminator(this, new BlockAccessorRelaxed(game.WorldMap, game, false, false), game.WorldMap.ClientChunkSize);
		}

		public void OnBlockTexturesLoaded()
		{
			this.chunkIlluminator.InitForWorld(this.game.Blocks, (ushort)this.game.WorldMap.SunBrightness, this.game.WorldMap.MapSizeX, this.game.WorldMap.MapSizeY, this.game.WorldMap.MapSizeZ);
		}

		internal void SunRelightChunk(ClientChunk chunk, long index3d)
		{
			ChunkPos pos = this.game.WorldMap.ChunkPosFromChunkIndex3D(index3d);
			this.SunRelightChunk(chunk, pos.X, pos.Y, pos.Z);
		}

		public void SunRelightChunk(ClientChunk chunk, int chunkX, int chunkY, int chunkZ)
		{
			ClientChunk[] chunks = new ClientChunk[this.game.WorldMap.ChunkMapSizeY];
			for (int y = 0; y < this.game.WorldMap.ChunkMapSizeY; y++)
			{
				chunks[y] = this.game.WorldMap.GetClientChunk(chunkX, y, chunkZ);
				chunks[y].shouldSunRelight = false;
				chunks[y].quantityRelit++;
				chunks[y].Unpack();
			}
			chunk.Lighting.ClearAllSunlight();
			ChunkIlluminator chunkIlluminator = this.chunkIlluminator;
			IWorldChunk[] array = chunks;
			chunkIlluminator.Sunlight(array, chunkX, chunkY, chunkZ, 0);
			ChunkIlluminator chunkIlluminator2 = this.chunkIlluminator;
			array = chunks;
			chunkIlluminator2.SunlightFlood(array, chunkX, chunkY, chunkZ);
			ChunkIlluminator chunkIlluminator3 = this.chunkIlluminator;
			array = chunks;
			byte spreadFaces = chunkIlluminator3.SunLightFloodNeighbourChunks(array, chunkX, chunkY, chunkZ, 0);
			foreach (BlockFacing face in BlockFacing.ALLFACES)
			{
				if ((face.Flag & spreadFaces) > 0)
				{
					int neibCx = chunkX + face.Normali.X;
					int neibCy = chunkY + face.Normali.Y;
					int neibCz = chunkZ + face.Normali.Z;
					this.game.WorldMap.MarkChunkDirty(neibCx, neibCy, neibCz, true, false, null, true, false);
				}
			}
		}

		public IWorldChunk GetChunk(int chunkX, int chunkY, int chunkZ)
		{
			ClientChunk chunk = this.game.WorldMap.GetClientChunk(chunkX, chunkY, chunkZ);
			if (chunk != null)
			{
				chunk.Unpack();
			}
			return chunk;
		}

		public IWorldChunk GetUnpackedChunkFast(int chunkX, int chunkY, int chunkZ, bool notRecentlyAccessed = false)
		{
			return ((IChunkProvider)this.game.WorldMap).GetUnpackedChunkFast(chunkX, chunkY, chunkZ, notRecentlyAccessed);
		}

		public long ChunkIndex3D(int chunkX, int chunkY, int chunkZ)
		{
			return ((long)chunkY * (long)this.game.WorldMap.index3dMulZ + (long)chunkZ) * (long)this.game.WorldMap.index3dMulX + (long)chunkX;
		}

		public long ChunkIndex3D(EntityPos pos)
		{
			return this.game.WorldMap.ChunkIndex3D(pos);
		}

		private ChunkIlluminator chunkIlluminator;

		private ClientMain game;
	}
}
