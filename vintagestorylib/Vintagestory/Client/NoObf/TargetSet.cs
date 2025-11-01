using System;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Vintagestory.Client.NoObf
{
	public class TargetSet
	{
		public void Add(CompositeShape shape, string message, AssetLocation sourceLoc, int alternateNo = -1)
		{
			HashSet<AssetLocationAndSource> hashSet = ((shape.Format == EnumShapeFormat.Obj) ? this.objlocations : ((shape.Format == EnumShapeFormat.GltfEmbedded) ? this.gltflocations : this.shapelocations));
			AssetLocationAndSource loc = new AssetLocationAndSource(shape.Base, message, sourceLoc, -1);
			hashSet.Add(loc);
		}

		internal HashSet<AssetLocationAndSource> shapelocations = new HashSet<AssetLocationAndSource>();

		internal HashSet<AssetLocationAndSource> objlocations = new HashSet<AssetLocationAndSource>();

		internal HashSet<AssetLocationAndSource> gltflocations = new HashSet<AssetLocationAndSource>();

		internal volatile bool finished;
	}
}
