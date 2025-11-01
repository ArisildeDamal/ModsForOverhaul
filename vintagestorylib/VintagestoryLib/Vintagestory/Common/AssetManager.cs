using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.Common
{
	public class AssetManager : IAssetManager
	{
		public Dictionary<AssetLocation, IAsset> AllAssets
		{
			get
			{
				return this.Assets;
			}
		}

		List<IAssetOrigin> IAssetManager.Origins
		{
			get
			{
				return this.Origins;
			}
		}

		public AssetManager(string assetsPath, EnumAppSide side)
		{
			this.assetsPath = assetsPath;
			this.side = side;
		}

		public void Add(AssetLocation path, IAsset asset)
		{
			this.Assets[path] = asset;
			this.assetsByCategory[path.Category.Code].Add(asset);
			if (!this.RuntimeAssets.ContainsKey(path))
			{
				this.RuntimeAssets[path] = asset;
			}
		}

		public int InitAndLoadBaseAssets(ILogger Logger)
		{
			return this.InitAndLoadBaseAssets(Logger, null);
		}

		public int InitAndLoadBaseAssets(ILogger Logger, string pathForReservedCharsCheck)
		{
			this.allAssetsLoaded = false;
			this.Origins = new List<IAssetOrigin>();
			this.Origins.Add(new GameOrigin(this.assetsPath, pathForReservedCharsCheck));
			this.Assets = new Dictionary<AssetLocation, IAsset>();
			this.assetsByCategory = new FastSmallDictionary<string, List<IAsset>>(AssetCategory.categories.Values.Count + 1);
			int count = 0;
			foreach (AssetCategory category in AssetCategory.categories.Values)
			{
				if ((category.SideType & this.side) > (EnumAppSide)0)
				{
					Dictionary<AssetLocation, IAsset> categoryassets = this.GetAssetsDontLoad(category, this.Origins);
					foreach (IAsset asset in categoryassets.Values)
					{
						this.Assets[asset.Location] = asset;
					}
					count += categoryassets.Count;
					this.assetsByCategory[category.Code] = categoryassets.Values.ToList<IAsset>();
					if (Logger != null)
					{
						Logger.Notification("Found {1} base assets in category {0}", new object[] { category, categoryassets.Count });
					}
				}
			}
			return count;
		}

		public int AddExternalAssets(ILogger Logger, ModLoader modloader = null)
		{
			List<string> assetOriginsForLog = new List<string>();
			List<IAssetOrigin> externalOrigins = new List<IAssetOrigin>();
			foreach (IAssetOrigin origin in this.CustomAppOrigins)
			{
				this.Origins.Add(origin);
				externalOrigins.Add(origin);
				assetOriginsForLog.Add("arg@" + StringUtil.SanitizePath(origin.OriginPath));
			}
			foreach (IAssetOrigin origin2 in this.CustomModOrigins)
			{
				this.Origins.Add(origin2);
				externalOrigins.Add(origin2);
				assetOriginsForLog.Add("modorigin@" + StringUtil.SanitizePath(origin2.OriginPath));
			}
			if (modloader != null)
			{
				foreach (KeyValuePair<string, IAssetOrigin> val in modloader.GetContentArchives())
				{
					externalOrigins.Add(val.Value);
					this.Origins.Add(val.Value);
					assetOriginsForLog.Add("mod@" + StringUtil.SanitizePath(val.Key));
				}
				foreach (KeyValuePair<string, IAssetOrigin> val2 in modloader.GetThemeArchives())
				{
					externalOrigins.Add(val2.Value);
					this.Origins.Add(val2.Value);
					assetOriginsForLog.Add("themepack@" + StringUtil.SanitizePath(val2.Key));
				}
			}
			if (assetOriginsForLog.Count > 0)
			{
				Logger.Notification("External Origins in load order: {0}", new object[] { string.Join(", ", assetOriginsForLog) });
			}
			int categoryIndex = 0;
			int count = 0;
			foreach (AssetCategory category in AssetCategory.categories.Values)
			{
				if ((category.SideType & this.side) > (EnumAppSide)0)
				{
					Dictionary<AssetLocation, IAsset> categoryassets = this.GetAssetsDontLoad(category, externalOrigins);
					foreach (IAsset asset in categoryassets.Values)
					{
						this.Assets[asset.Location] = asset;
					}
					count += categoryassets.Count;
					List<IAsset> list;
					if (!this.assetsByCategory.TryGetValue(category.Code, out list))
					{
						list = (this.assetsByCategory[category.Code] = new List<IAsset>());
					}
					list.AddRange(categoryassets.Values);
					Logger.Notification("Found {1} external assets in category {0}", new object[] { category, categoryassets.Count });
				}
				categoryIndex++;
			}
			this.allAssetsLoaded = true;
			return count;
		}

		public void UnloadExternalAssets(ILogger logger)
		{
			this.allAssetsLoaded = false;
			this.InitAndLoadBaseAssets(null);
		}

		public void UnloadAssets(AssetCategory category)
		{
			foreach (KeyValuePair<AssetLocation, IAsset> val in this.Assets)
			{
				if (val.Key.Category == category)
				{
					val.Value.Data = null;
				}
			}
		}

		public void UnloadAssets()
		{
			foreach (KeyValuePair<AssetLocation, IAsset> val in this.Assets)
			{
				val.Value.Data = null;
			}
		}

		public void UnloadUnpatchedAssets()
		{
			foreach (KeyValuePair<AssetLocation, IAsset> val in this.Assets)
			{
				if (!val.Value.IsPatched)
				{
					val.Value.Data = null;
				}
			}
		}

		public List<AssetLocation> GetLocations(string fullPathBeginsWith, string domain = null)
		{
			List<AssetLocation> locations = new List<AssetLocation>();
			foreach (IAsset asset in this.Assets.Values)
			{
				if (asset.Location.BeginsWith(domain, fullPathBeginsWith))
				{
					locations.Add(asset.Location);
				}
			}
			return locations;
		}

		public bool Exists(AssetLocation location)
		{
			return this.Assets.ContainsKey(location);
		}

		public IAsset TryGet(string Path, bool loadAsset = true)
		{
			return this.TryGet(new AssetLocation(Path), loadAsset);
		}

		public IAsset TryGet(AssetLocation Location, bool loadAsset = true)
		{
			if (!this.allAssetsLoaded)
			{
				throw new Exception("Coding error: Mods must not get assets before AssetsLoaded stage - do not load assets in a Start() method!");
			}
			return this.TryGet_BaseAssets(Location, loadAsset);
		}

		public IAsset TryGet_BaseAssets(string Path, bool loadAsset = true)
		{
			return this.TryGet_BaseAssets(new AssetLocation(Path), loadAsset);
		}

		public IAsset TryGet_BaseAssets(AssetLocation Location, bool loadAsset = true)
		{
			IAsset asset;
			if (!this.Assets.TryGetValue(Location, out asset))
			{
				return null;
			}
			if (!asset.IsLoaded() && loadAsset)
			{
				asset.Origin.TryLoadAsset(asset);
			}
			return asset;
		}

		public IAsset Get(string Path)
		{
			return this.Get(new AssetLocation(Path));
		}

		public IAsset Get(AssetLocation Location)
		{
			IAsset asset = this.TryGet_BaseAssets(Location, true);
			if (asset == null)
			{
				throw new Exception("Asset " + Location + " could not be found");
			}
			return asset;
		}

		public T Get<T>(AssetLocation Location)
		{
			return this.Get(Location).ToObject<T>(null);
		}

		public List<IAsset> GetMany(AssetCategory category, bool loadAsset = true)
		{
			List<IAsset> foundassets = new List<IAsset>();
			List<IAsset> categoryAssets;
			if (this.assetsByCategory.TryGetValue(category.Code, out categoryAssets))
			{
				foreach (IAsset asset in categoryAssets)
				{
					if (asset.Location.Category == category)
					{
						if (!asset.IsLoaded() && loadAsset)
						{
							asset.Origin.LoadAsset(asset);
						}
						foundassets.Add(asset);
					}
				}
			}
			return foundassets;
		}

		public List<IAsset> GetManyInCategory(string categoryCode, string pathBegins, string domain = null, bool loadAsset = true)
		{
			List<IAsset> foundassets = new List<IAsset>();
			List<IAsset> categoryAssets;
			if (this.assetsByCategory.TryGetValue(categoryCode, out categoryAssets))
			{
				int offset = categoryCode.Length + 1;
				foreach (IAsset asset in categoryAssets)
				{
					if (asset.Location.BeginsWith(domain, pathBegins, offset))
					{
						if (loadAsset && !asset.IsLoaded())
						{
							asset.Origin.LoadAsset(asset);
						}
						foundassets.Add(asset);
					}
				}
			}
			return foundassets;
		}

		public List<IAsset> GetMany(string partialPath, string domain = null, bool loadAsset = true)
		{
			List<IAsset> foundassets = new List<IAsset>();
			foreach (KeyValuePair<AssetLocation, IAsset> val in this.Assets)
			{
				IAsset asset = val.Value;
				if (val.Key.BeginsWith(domain, partialPath))
				{
					if (loadAsset && !asset.IsLoaded())
					{
						asset.Origin.LoadAsset(asset);
					}
					foundassets.Add(asset);
				}
			}
			return foundassets;
		}

		public Dictionary<AssetLocation, T> GetMany<T>(ILogger logger, string fullPath, string domain = null)
		{
			Dictionary<AssetLocation, T> result = new Dictionary<AssetLocation, T>();
			foreach (IAsset asset2 in this.GetMany(fullPath, domain, true))
			{
				Asset asset = (Asset)asset2;
				try
				{
					result.Add(asset.Location, asset.ToObject<T>(null));
				}
				catch (JsonReaderException e)
				{
					logger.Error("Syntax error in json file '{0}': {1}", new object[] { asset, e.Message });
				}
			}
			return result;
		}

		internal Dictionary<AssetLocation, IAsset> GetAssetsDontLoad(AssetCategory category, List<IAssetOrigin> fromOrigins)
		{
			Dictionary<AssetLocation, IAsset> assets = new Dictionary<AssetLocation, IAsset>();
			foreach (IAssetOrigin Origin in fromOrigins)
			{
				if (Origin.IsAllowedToAffectGameplay() || !category.AffectsGameplay)
				{
					foreach (IAsset asset in Origin.GetAssets(category, false))
					{
						assets[asset.Location] = asset;
					}
				}
			}
			return assets;
		}

		public int Reload(AssetLocation location)
		{
			this.Assets.RemoveAllByKey((AssetLocation x) => location == null || location.IsChild(x));
			int count = 0;
			List<IAsset> list = null;
			if (location != null)
			{
				int pathSep = location.Path.IndexOf('/');
				if (pathSep > 0)
				{
					string categoryCode = location.Path.Substring(0, pathSep);
					if (this.assetsByCategory.TryGetValue(categoryCode, out list))
					{
						list.RemoveAll((IAsset a) => location.IsChild(a.Location));
					}
				}
			}
			foreach (IAssetOrigin assetOrigin in this.Origins)
			{
				List<IAsset> locationAssets = assetOrigin.GetAssets(location, true);
				foreach (IAsset asset in locationAssets)
				{
					this.Assets[asset.Location] = asset;
					count++;
				}
				if (list != null)
				{
					list.AddRange(locationAssets);
				}
			}
			return count;
		}

		public int Reload(AssetCategory category)
		{
			this.Assets.RemoveAllByKey((AssetLocation x) => category == null || x.Category == category);
			int count = 0;
			List<IAsset> list;
			if (!this.assetsByCategory.TryGetValue(category.Code, out list))
			{
				list = (this.assetsByCategory[category.Code] = new List<IAsset>());
			}
			else
			{
				list.Clear();
			}
			foreach (IAssetOrigin assetOrigin in this.Origins)
			{
				List<IAsset> categoryAssets = assetOrigin.GetAssets(category, true);
				foreach (IAsset asset in categoryAssets)
				{
					this.Assets[asset.Location] = asset;
					count++;
				}
				list.AddRange(categoryAssets);
			}
			foreach (KeyValuePair<AssetLocation, IAsset> val in this.RuntimeAssets)
			{
				if (val.Key.Category == category)
				{
					this.Add(val.Key, val.Value);
				}
			}
			return count;
		}

		public AssetCategory GetCategoryFromFullPath(string fullpath)
		{
			return AssetCategory.FromCode(fullpath.Split('/', StringSplitOptions.None)[0]);
		}

		public void AddPathOrigin(string domain, string fullPath)
		{
			this.AddModOrigin(domain, fullPath, null);
		}

		public void AddModOrigin(string domain, string fullPath)
		{
			this.AddModOrigin(domain, fullPath, null);
		}

		public void AddModOrigin(string domain, string fullPath, string pathForReservedCharsCheck)
		{
			for (int i = 0; i < this.CustomModOrigins.Count; i++)
			{
				IAssetOrigin orig = this.CustomModOrigins[i];
				PathOrigin pathOrigin = orig as PathOrigin;
				if (((pathOrigin != null) ? pathOrigin.OriginPath : null) == fullPath)
				{
					PathOrigin pathOrigin2 = orig as PathOrigin;
					if (((pathOrigin2 != null) ? pathOrigin2.Domain : null) == domain)
					{
						return;
					}
				}
			}
			this.CustomModOrigins.Add(new PathOrigin(domain, fullPath, pathForReservedCharsCheck));
		}

		private EnumAppSide side;

		public bool allAssetsLoaded;

		public Dictionary<AssetLocation, IAsset> Assets;

		public Dictionary<AssetLocation, IAsset> RuntimeAssets = new Dictionary<AssetLocation, IAsset>();

		private IDictionary<string, List<IAsset>> assetsByCategory;

		public List<IAssetOrigin> Origins;

		public List<IAssetOrigin> CustomAppOrigins = new List<IAssetOrigin>();

		public List<IAssetOrigin> CustomModOrigins = new List<IAssetOrigin>();

		private string assetsPath;
	}
}
