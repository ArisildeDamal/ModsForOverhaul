using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class TesselatedChunk : IComparable<TesselatedChunk>, IMergeable<TesselatedChunk>
	{
		public int CompareTo(TesselatedChunk obj)
		{
			return this.priority - obj.priority;
		}

		public bool MergeIfEqual(TesselatedChunk otc)
		{
			if (this.positionX == otc.positionX && this.positionYAndDimension == otc.positionYAndDimension && this.positionZ == otc.positionZ)
			{
				if (otc.centerParts != null)
				{
					this.Dispose(this.centerParts);
					this.centerParts = otc.centerParts;
				}
				if (otc.edgeParts != null)
				{
					this.Dispose(this.edgeParts);
					this.edgeParts = otc.edgeParts;
				}
				return true;
			}
			return false;
		}

		internal bool AddCenterToPools(ChunkRenderer chunkRenderer, Vec3i chunkOrigin, int dimension, Sphere boundingSphere, ClientChunk hostChunk)
		{
			if (this.centerParts != null)
			{
				bool prevHidden = hostChunk.GetHiddenState(ref hostChunk.centerModelPoolLocations);
				this.chunk.RemoveCenterDataPoolLocations(chunkRenderer);
				List<ModelDataPoolLocation> locations = new List<ModelDataPoolLocation>(this.centerParts.Length);
				for (int i = 0; i < this.centerParts.Length; i++)
				{
					this.centerParts[i].AddToPools(chunkRenderer, locations, chunkOrigin, dimension, boundingSphere, this.CullVisible);
				}
				hostChunk.SetPoolLocations(ref hostChunk.centerModelPoolLocations, locations.ToArray(), prevHidden);
				return true;
			}
			return false;
		}

		internal bool AddEdgeToPools(ChunkRenderer chunkRenderer, Vec3i chunkOrigin, int dimension, Sphere boundingSphere, ClientChunk hostChunk)
		{
			if (this.edgeParts != null)
			{
				bool prevHidden = hostChunk.GetHiddenState(ref hostChunk.edgeModelPoolLocations);
				chunkRenderer.QuantityRenderingChunks -= this.chunk.RemoveEdgeDataPoolLocations(chunkRenderer);
				List<ModelDataPoolLocation> locations = new List<ModelDataPoolLocation>(this.edgeParts.Length);
				for (int i = 0; i < this.edgeParts.Length; i++)
				{
					this.edgeParts[i].AddToPools(chunkRenderer, locations, chunkOrigin, dimension, boundingSphere, this.CullVisible);
				}
				hostChunk.SetPoolLocations(ref hostChunk.edgeModelPoolLocations, locations.ToArray(), prevHidden);
				chunkRenderer.QuantityRenderingChunks++;
				return true;
			}
			return false;
		}

		internal void RecalcPriority(ClientPlayer player)
		{
			int dx = this.positionX - player.Entity.Pos.XInt;
			if (dx < 0)
			{
				if (dx < -32)
				{
					dx += 32;
				}
				else
				{
					dx += 16;
				}
			}
			int dz = this.positionZ - player.Entity.Pos.ZInt;
			if (dz < 0)
			{
				if (dz < -32)
				{
					dz += 32;
				}
				else
				{
					dz += 16;
				}
			}
			float angleDiff = GameMath.AngleRadDistance((float)Math.Atan2((double)(-(double)dz), (double)dx), player.CameraYaw);
			this.priority = (int)(1000.0 * Math.Sqrt(Math.Sqrt((double)(dx * dx + dz * dz))) * (double)Math.Abs(angleDiff)) - 1000 * this.positionYAndDimension / 32;
		}

		internal void SetBounds(float xMin, float xMax, float yMin, float yMax, float zMin, float zMax)
		{
			this.boundingSphere = new Sphere((float)this.positionX + (xMax + xMin) / 2f, (float)this.positionYAndDimension + (yMax + yMin) / 2f, (float)this.positionZ + (zMax + zMin) / 2f, Math.Max(0f, xMax - xMin), Math.Max(0f, yMax - yMin), Math.Max(0f, zMax - zMin));
		}

		internal void UnusedDispose()
		{
			this.Dispose(this.centerParts);
			this.Dispose(this.edgeParts);
		}

		private void Dispose(TesselatedChunkPart[] parts)
		{
			if (parts != null)
			{
				for (int i = 0; i < parts.Length; i++)
				{
					parts[i].Dispose();
				}
			}
		}

		internal int positionX;

		internal int positionYAndDimension;

		internal int positionZ;

		internal int priority;

		internal Bools CullVisible;

		internal ClientChunk chunk;

		internal TesselatedChunkPart[] centerParts;

		internal TesselatedChunkPart[] edgeParts;

		internal int VerticesCount;

		internal Sphere boundingSphere;
	}
}
