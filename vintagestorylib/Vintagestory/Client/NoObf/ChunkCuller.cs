using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class ChunkCuller
	{
		public ChunkCuller(ClientMain game)
		{
			this.game = game;
			ClientSettings.Inst.AddWatcher<int>("viewDistance", new OnSettingsChanged<int>(this.genShellVectors));
			this.genShellVectors(ClientSettings.ViewDistance);
		}

		private void genShellVectors(int viewDistance)
		{
			Vec2i[] points = ShapeUtil.GetOctagonPoints(0, 0, viewDistance / 32 + 1);
			int cmapheight = this.game.WorldMap.ChunkMapSizeY;
			HashSet<Vec3i> shellPositions = new HashSet<Vec3i>();
			foreach (Vec2i point in points)
			{
				for (int cy = -cmapheight; cy <= cmapheight; cy++)
				{
					shellPositions.Add(new Vec3i(point.X, cy, point.Y));
				}
			}
			for (int r = 0; r < viewDistance / 32 + 1; r++)
			{
				foreach (Vec2i point2 in ShapeUtil.GetOctagonPoints(0, 0, r))
				{
					shellPositions.Add(new Vec3i(point2.X, -cmapheight, point2.Y));
					shellPositions.Add(new Vec3i(point2.X, cmapheight, point2.Y));
				}
			}
			this.cubicShellPositions = shellPositions.ToArray<Vec3i>();
		}

		public void CullInvisibleChunks()
		{
			object obj;
			if (!ClientSettings.Occlusionculling || this.game.WorldMap.chunks.Count < 100)
			{
				if (!this.nowOff)
				{
					ClientChunk.bufIndex = 1;
					obj = this.game.WorldMap.chunksLock;
					lock (obj)
					{
						foreach (KeyValuePair<long, ClientChunk> val in this.game.WorldMap.chunks)
						{
							if (val.Key / 4503599627370496L != 1L)
							{
								val.Value.SetVisible(true);
							}
						}
					}
					ClientChunk.bufIndex = 0;
					obj = this.game.WorldMap.chunksLock;
					lock (obj)
					{
						foreach (KeyValuePair<long, ClientChunk> val2 in this.game.WorldMap.chunks)
						{
							if (val2.Key / 4503599627370496L != 1L)
							{
								val2.Value.SetVisible(true);
							}
						}
					}
					this.nowOff = true;
				}
				return;
			}
			this.nowOff = false;
			Vec3d camPos = this.game.player.Entity.CameraPos;
			if (this.centerpos.Equals((int)camPos.X / 32, (int)camPos.Y / 32, (int)camPos.Z / 32) && Math.Abs(this.game.chunkPositionsForRegenTrav.Count - this.qCount) < 10)
			{
				return;
			}
			this.qCount = this.game.chunkPositionsForRegenTrav.Count;
			this.centerpos.Set((int)(camPos.X / 32.0), (int)(camPos.Y / 32.0), (int)(camPos.Z / 32.0));
			this.isAboveHeightLimit = this.centerpos.Y >= this.game.WorldMap.ChunkMapSizeY;
			obj = this.game.WorldMap.chunksLock;
			lock (obj)
			{
				foreach (KeyValuePair<long, ClientChunk> val3 in this.game.WorldMap.chunks)
				{
					val3.Value.SetVisible(false);
				}
				for (int dx = -1; dx <= 1; dx++)
				{
					for (int dy = -1; dy <= 1; dy++)
					{
						for (int dz = -1; dz <= 1; dz++)
						{
							long index3d = this.game.WorldMap.ChunkIndex3D(dx + this.centerpos.X, dy + this.centerpos.Y, dz + this.centerpos.Z);
							ClientChunk chunk;
							if (this.game.WorldMap.chunks.TryGetValue(index3d, out chunk))
							{
								chunk.SetVisible(true);
							}
						}
					}
				}
			}
			for (int i = 0; i < this.cubicShellPositions.Length; i++)
			{
				Vec3i vec = this.cubicShellPositions[i];
				this.traverseRayAndMarkVisible(this.centerpos, vec, 0.25, 0.5);
				this.traverseRayAndMarkVisible(this.centerpos, vec, 0.75, 0.5);
				this.traverseRayAndMarkVisible(this.centerpos, vec, 0.75, 0.0);
			}
			this.game.chunkRenderer.SwapVisibleBuffers();
		}

		private void traverseRayAndMarkVisible(Vec3i fromPos, Vec3i toPosRel, double yoffset = 0.5, double xoffset = 0.5)
		{
			this.ray.origin.Set((double)fromPos.X + xoffset, (double)fromPos.Y + yoffset, (double)fromPos.Z + 0.5);
			this.ray.dir.Set((double)toPosRel.X + xoffset, (double)toPosRel.Y + yoffset, (double)toPosRel.Z + 0.5);
			this.toPos.Set(fromPos.X + toPosRel.X, fromPos.Y + toPosRel.Y, fromPos.Z + toPosRel.Z);
			this.curpos.Set(fromPos);
			BlockFacing fromFace = null;
			int manhattenLength = fromPos.ManhattenDistanceTo(this.toPos);
			int curMhDist;
			while ((curMhDist = this.curpos.ManhattenDistanceTo(fromPos)) <= manhattenLength + 2)
			{
				BlockFacing toFace = this.getExitingFace(this.curpos);
				if (toFace == null)
				{
					return;
				}
				long index3d = ((long)this.curpos.Y * (long)this.game.WorldMap.index3dMulZ + (long)this.curpos.Z) * (long)this.game.WorldMap.index3dMulX + (long)this.curpos.X;
				ClientChunk chunk;
				this.game.WorldMap.chunks.TryGetValue(index3d, out chunk);
				if (chunk != null)
				{
					chunk.SetVisible(true);
					if (curMhDist > 1 && !chunk.IsTraversable(fromFace, toFace))
					{
						break;
					}
				}
				this.curpos.Offset(toFace);
				fromFace = toFace.Opposite;
				if (!this.game.WorldMap.IsValidChunkPosFast(this.curpos.X, this.curpos.Y, this.curpos.Z) && (!this.isAboveHeightLimit || this.curpos.Y <= 0))
				{
					break;
				}
			}
		}

		private BlockFacing getExitingFace(Vec3i pos)
		{
			for (int i = 0; i < 6; i++)
			{
				BlockFacing blockSideFacing = BlockFacing.ALLFACES[i];
				Vec3i planeNormal = blockSideFacing.Normali;
				double demon = (double)planeNormal.X * this.ray.dir.X + (double)planeNormal.Y * this.ray.dir.Y + (double)planeNormal.Z * this.ray.dir.Z;
				if (demon > 1E-05)
				{
					this.planePosition.Set(pos).Add(blockSideFacing.PlaneCenter);
					double num = this.planePosition.X - this.ray.origin.X;
					double pty = this.planePosition.Y - this.ray.origin.Y;
					double ptz = this.planePosition.Z - this.ray.origin.Z;
					double t = (num * (double)planeNormal.X + pty * (double)planeNormal.Y + ptz * (double)planeNormal.Z) / demon;
					if (t >= 0.0 && Math.Abs(this.ray.origin.X + this.ray.dir.X * t - this.planePosition.X) <= 0.5 && Math.Abs(this.ray.origin.Y + this.ray.dir.Y * t - this.planePosition.Y) <= 0.5 && Math.Abs(this.ray.origin.Z + this.ray.dir.Z * t - this.planePosition.Z) <= 0.5)
					{
						return blockSideFacing;
					}
				}
			}
			return null;
		}

		private ClientMain game;

		private const int chunksize = 32;

		public Ray ray = new Ray();

		private Vec3d planePosition = new Vec3d();

		private Vec3i[] cubicShellPositions;

		private Vec3i centerpos = new Vec3i();

		private bool isAboveHeightLimit;

		public Vec3i curpos = new Vec3i();

		private Vec3i toPos = new Vec3i();

		private int qCount;

		private bool nowOff;

		private const long ExtraDimensionsStart = 4503599627370496L;
	}
}
