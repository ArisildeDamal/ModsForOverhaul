using System;
using System.Diagnostics;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.Server
{
	public class BlockAccessorWorldGenUpdateHeightmap : BlockAccessorWorldGen
	{
		public BlockAccessorWorldGenUpdateHeightmap(ServerMain server, ChunkServerThread chunkdbthread)
			: base(server, chunkdbthread)
		{
		}

		public override void SetBlock(int blockId, BlockPos pos, ItemStack byItemstack = null)
		{
			ServerChunk chunk = this.chunkdbthread.GetGeneratingChunkAtPos(pos);
			if (chunk == null)
			{
				chunk = this.server.WorldMap.GetChunkAtPos(pos.X, pos.Y, pos.Z) as ServerChunk;
			}
			if (chunk != null)
			{
				Block newBlock = this.worldAccessor.GetBlock(blockId);
				int oldblockid = base.SetSolidBlock(chunk, pos, newBlock, blockId);
				if (newBlock.LightHsv[2] > 0)
				{
					(this.chunkdbthread.worldgenBlockAccessor as IWorldGenBlockAccessor).ScheduleBlockLightUpdate(pos, oldblockid, blockId);
				}
				bool rainPermeable = this.worldmap.Blocks[oldblockid].RainPermeable;
				bool newRainPermeable = newBlock.RainPermeable;
				int ymax = (int)chunk.MapChunk.YMax;
				if (blockId != 0)
				{
					ymax = Math.Max(pos.Y, ymax);
				}
				int index2d = (pos.X & MagicNum.ServerChunkSizeMask) + 32 * (pos.Z & MagicNum.ServerChunkSizeMask);
				if (rainPermeable && !newRainPermeable)
				{
					chunk.MapChunk.RainHeightMap[index2d] = Math.Max(chunk.MapChunk.RainHeightMap[index2d], (ushort)pos.Y);
				}
				if (!rainPermeable && newRainPermeable && (int)chunk.MapChunk.RainHeightMap[index2d] == pos.Y)
				{
					BlockPos horizon = pos.DownCopy(1);
					while (this.worldmap.Blocks[this.GetBlockId(horizon, 3)].RainPermeable && horizon.Y > 0)
					{
						horizon.Down(1);
					}
					chunk.MapChunk.RainHeightMap[index2d] = (ushort)horizon.Y;
				}
				if (chunk.serverMapChunk.CurrentIncompletePass < EnumWorldGenPass.Vegetation)
				{
					bool flag = this.worldmap.Blocks[oldblockid].SideSolid[BlockFacing.UP.Index];
					bool newSolid = newBlock.SideSolid[BlockFacing.UP.Index];
					if (!flag && newSolid)
					{
						chunk.MapChunk.WorldGenTerrainHeightMap[index2d] = Math.Max(chunk.MapChunk.WorldGenTerrainHeightMap[index2d], (ushort)pos.Y);
					}
					if (flag && !newSolid && (int)chunk.MapChunk.WorldGenTerrainHeightMap[index2d] == pos.Y)
					{
						BlockPos horizon2 = pos.DownCopy(1);
						while (this.worldmap.Blocks[this.GetBlockId(horizon2, 3)].RainPermeable && horizon2.Y > 0)
						{
							horizon2.Down(1);
						}
						chunk.MapChunk.WorldGenTerrainHeightMap[index2d] = (ushort)horizon2.Y;
					}
				}
				chunk.MapChunk.YMax = (ushort)ymax;
				return;
			}
			if (RuntimeEnv.DebugOutOfRangeBlockAccess)
			{
				ServerMain.Logger.Notification("Tried to set block outside generating chunks! (at pos {0}, {1}, {2} = chunk {3}, {4}, {5})", new object[]
				{
					pos.X,
					pos.Y,
					pos.Z,
					pos.X / MagicNum.ServerChunkSize,
					pos.Y / MagicNum.ServerChunkSize,
					pos.Z / MagicNum.ServerChunkSize
				});
				LoggerBase logger = ServerMain.Logger;
				StackTrace stackTrace = new StackTrace();
				logger.VerboseDebug(((stackTrace != null) ? stackTrace.ToString() : null) ?? "");
			}
		}
	}
}
