using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf
{
	public class TesselatedChunkPart
	{
		internal void AddToPools(ChunkRenderer cr, List<ModelDataPoolLocation> locations, Vec3i chunkOrigin, int dimension, Sphere boundingSphere, Bools cullVisible)
		{
			bool isTransparentDimension = dimension == 1 && BlockAccessorMovable.IsTransparent(chunkOrigin);
			MeshDataPoolManager pools = cr.poolsByRenderPass[isTransparentDimension ? Math.Max(3, (int)this.pass) : ((int)this.pass)][this.atlasNumber];
			if (this.modelDataLod0 != null)
			{
				cr.SetInterleaveStrides(this.modelDataLod0, this.pass);
				this.AddModelAndStoreLocation(pools, locations, this.modelDataLod0, chunkOrigin, dimension, boundingSphere, cullVisible, 0);
			}
			if (this.modelDataLod1 != null)
			{
				cr.SetInterleaveStrides(this.modelDataLod1, this.pass);
				this.AddModelAndStoreLocation(pools, locations, this.modelDataLod1, chunkOrigin, dimension, boundingSphere, cullVisible, 1);
			}
			if (this.modelDataNotLod2Far != null)
			{
				cr.SetInterleaveStrides(this.modelDataNotLod2Far, this.pass);
				this.AddModelAndStoreLocation(pools, locations, this.modelDataNotLod2Far, chunkOrigin, dimension, boundingSphere, cullVisible, 2);
			}
			if (this.modelDataLod2Far != null)
			{
				cr.SetInterleaveStrides(this.modelDataLod2Far, this.pass);
				this.AddModelAndStoreLocation(pools, locations, this.modelDataLod2Far, chunkOrigin, dimension, boundingSphere, cullVisible, 3);
			}
			this.Dispose();
		}

		internal void AddModelAndStoreLocation(MeshDataPoolManager pools, List<ModelDataPoolLocation> locations, MeshData modeldata, Vec3i modelOrigin, int dimension, Sphere frustumCullSphere, Bools cullVisible, int lodLevel)
		{
			ModelDataPoolLocation location = pools.AddModel(modeldata, modelOrigin, dimension, frustumCullSphere);
			if (location != null)
			{
				location.CullVisible = cullVisible;
				location.LodLevel = lodLevel;
				locations.Add(location);
			}
		}

		internal void Dispose()
		{
			MeshData meshData = this.modelDataLod0;
			if (meshData != null)
			{
				meshData.Dispose();
			}
			MeshData meshData2 = this.modelDataLod1;
			if (meshData2 != null)
			{
				meshData2.Dispose();
			}
			MeshData meshData3 = this.modelDataNotLod2Far;
			if (meshData3 != null)
			{
				meshData3.Dispose();
			}
			MeshData meshData4 = this.modelDataLod2Far;
			if (meshData4 == null)
			{
				return;
			}
			meshData4.Dispose();
		}

		internal int atlasNumber;

		internal MeshData modelDataLod0;

		internal MeshData modelDataLod1;

		internal MeshData modelDataNotLod2Far;

		internal MeshData modelDataLod2Far;

		internal EnumChunkRenderPass pass;
	}
}
