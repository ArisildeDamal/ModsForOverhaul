using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Vintagestory.Common
{
	public class PathOrigin : IAssetOrigin
	{
		public string OriginPath
		{
			get
			{
				return this.fullPath;
			}
		}

		public string Domain
		{
			get
			{
				return this.domain;
			}
		}

		public PathOrigin(string domain, string fullPath)
			: this(domain, fullPath, null)
		{
		}

		public PathOrigin(string domain, string fullPath, string pathForReservedCharsCheck)
		{
			this.domain = domain.ToLowerInvariant();
			this.fullPath = fullPath;
			if (!this.fullPath.EndsWith(Path.DirectorySeparatorChar))
			{
				ReadOnlySpan<char> readOnlySpan = this.fullPath;
				char directorySeparatorChar = Path.DirectorySeparatorChar;
				this.fullPath = readOnlySpan + new ReadOnlySpan<char>(ref directorySeparatorChar);
			}
			if (pathForReservedCharsCheck != null)
			{
				this.CheckForReservedCharacters(domain, pathForReservedCharsCheck);
			}
		}

		public void LoadAsset(IAsset asset)
		{
			if (asset.Location.Domain != this.domain)
			{
				throw new Exception(string.Concat(new string[]
				{
					"Invalid LoadAsset call or invalid asset instance. Trying to load [",
					(asset != null) ? asset.ToString() : null,
					"] from domain ",
					this.domain,
					" is bound to fail."
				}));
			}
			string path = this.fullPath + asset.Location.Path.Replace('/', Path.DirectorySeparatorChar);
			if (!File.Exists(path))
			{
				throw new Exception("Requested asset [" + asset.Location + "] could not be found");
			}
			asset.Data = File.ReadAllBytes(path);
		}

		public bool TryLoadAsset(IAsset asset)
		{
			if (asset.Location.Domain != this.domain)
			{
				return false;
			}
			string path = this.fullPath + (asset as Asset).FilePath.Replace('/', Path.DirectorySeparatorChar);
			if (!File.Exists(path))
			{
				return false;
			}
			asset.Data = File.ReadAllBytes(path);
			return true;
		}

		public List<IAsset> GetAssets(AssetCategory Category, bool shouldLoad = true)
		{
			List<IAsset> assets = new List<IAsset>();
			this.ScanAssetFolderRecursive(this.fullPath + Category.Code, assets, shouldLoad);
			return assets;
		}

		public List<IAsset> GetAssets(AssetLocation baseLocation, bool shouldLoad = true)
		{
			List<IAsset> assets = new List<IAsset>();
			this.ScanAssetFolderRecursive(this.fullPath + baseLocation.Path, assets, shouldLoad);
			return assets;
		}

		private void ScanAssetFolderRecursive(string currentPath, List<IAsset> list, bool shouldLoad)
		{
			if (!Directory.Exists(currentPath))
			{
				return;
			}
			foreach (string fullPath in Directory.GetDirectories(currentPath))
			{
				this.ScanAssetFolderRecursive(fullPath, list, shouldLoad);
			}
			foreach (string fullPath2 in Directory.GetFiles(currentPath))
			{
				FileInfo f = new FileInfo(fullPath2);
				if (!f.Name.Equals("thumbs.db", StringComparison.InvariantCultureIgnoreCase) && !f.Name.EndsWithOrdinal(".psd") && !f.Name.StartsWith('.'))
				{
					string path = fullPath2.Substring(this.fullPath.Length).Replace(Path.DirectorySeparatorChar, '/');
					AssetLocation location = new AssetLocation(this.domain, path.ToLowerInvariant());
					list.Add(new Asset(shouldLoad ? File.ReadAllBytes(fullPath2) : null, location, this)
					{
						FilePath = path
					});
				}
			}
		}

		public bool IsAllowedToAffectGameplay()
		{
			return true;
		}

		public string GetDefaultDomain()
		{
			return this.domain;
		}

		public virtual void CheckForReservedCharacters(string domain, string path)
		{
			path = ((path == null) ? this.OriginPath : Path.Combine(this.OriginPath, path));
			if (Directory.Exists(path))
			{
				DirectoryInfo directoryInfo = new DirectoryInfo(path);
				int dirPathLength = directoryInfo.FullName.Length - 9;
				foreach (FileInfo fileInfo in directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories))
				{
					string filepath = fileInfo.FullName.Substring(dirPathLength + 1);
					FolderOrigin.CheckForReservedCharacters(domain, filepath);
				}
			}
		}

		protected string fullPath;

		protected string domain;
	}
}
