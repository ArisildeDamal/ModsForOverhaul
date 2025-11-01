using System;
using Vintagestory.API.MathTools;

namespace Vintagestory.Server
{
	public struct MapRegionAndPos
	{
		public MapRegionAndPos(Vec3i pos, ServerMapRegion reg)
		{
			this.pos = pos;
			this.region = reg;
		}

		public Vec3i pos;

		public ServerMapRegion region;
	}
}
