using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf
{
	public class UnloadableShape : Shape
	{
		public void Unload()
		{
			this.Loaded = false;
			this.Textures = null;
			this.Elements = null;
			this.Animations = null;
			this.AnimationsByCrc32 = null;
			this.JointsById = null;
		}

		public bool Load(ClientMain game, AssetLocationAndSource srcandLoc)
		{
			this.Loaded = true;
			AssetLocation newLocation = srcandLoc.CopyWithPathPrefixAndAppendixOnce("shapes/", ".json");
			IAsset asset = ScreenManager.Platform.AssetManager.TryGet(newLocation, true);
			if (asset == null)
			{
				game.Platform.Logger.Warning("Did not find required shape {0} anywhere. (defined in {1})", new object[] { newLocation, srcandLoc.Source });
				return false;
			}
			bool flag;
			try
			{
				ShapeElement.locationForLogging = newLocation;
				JsonUtil.PopulateObject(this, Asset.BytesToString(asset.Data), asset.Location.Domain, null);
				flag = true;
			}
			catch (Exception e)
			{
				game.Platform.Logger.Warning("Failed parsing shape model {0}\n{1}", new object[] { newLocation, e.Message });
				flag = false;
			}
			return flag;
		}

		public void ResolveTextures(Dictionary<AssetLocation, CompositeTexture> shapeTexturesCache)
		{
			FastSmallDictionary<string, CompositeTexture> dict = new FastSmallDictionary<string, CompositeTexture>(this.Textures.Count);
			foreach (KeyValuePair<string, AssetLocation> val in this.Textures)
			{
				AssetLocation textureLoc = val.Value;
				CompositeTexture ct;
				if (!shapeTexturesCache.TryGetValue(textureLoc, out ct))
				{
					ct = new CompositeTexture
					{
						Base = textureLoc
					};
					shapeTexturesCache[textureLoc] = ct;
				}
				dict.Add(val.Key, ct);
			}
			this.TexturesResolved = dict;
		}

		public bool Loaded;

		public IDictionary<string, CompositeTexture> TexturesResolved;
	}
}
