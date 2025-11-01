using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Vintagestory.Common
{
	public class FolderOrigin : IAssetOrigin
	{
		public string OriginPath { get; protected set; }

		public FolderOrigin(string fullPath)
			: this(fullPath, null)
		{
		}

		public FolderOrigin(string fullPath, string pathForReservedCharsCheck)
		{
			this.OriginPath = Path.Combine(fullPath, "assets");
			string ignoreFilename = Path.Combine(fullPath, ".ignore");
			IgnoreFile ignoreFile = (File.Exists(ignoreFilename) ? new IgnoreFile(ignoreFilename, fullPath) : null);
			DirectoryInfo dir = new DirectoryInfo(this.OriginPath);
			int dirPathLength = dir.FullName.Length;
			if (Directory.Exists(this.OriginPath))
			{
				foreach (FileInfo file in dir.EnumerateFiles("*", SearchOption.AllDirectories))
				{
					if (!file.Name.Equals("thumbs.db", StringComparison.InvariantCultureIgnoreCase) && !(file.Extension == ".psd") && file.Name[0] != '.' && (ignoreFile == null || ignoreFile.Available(file.FullName)))
					{
						string path = file.FullName.Substring(dirPathLength + 1);
						if (Path.DirectorySeparatorChar == '\\')
						{
							path = path.Replace('\\', '/');
						}
						int firstSlashIndex = path.IndexOf('/');
						if (firstSlashIndex >= 0)
						{
							string domain = path.Substring(0, firstSlashIndex);
							path = path.Substring(firstSlashIndex + 1);
							if (pathForReservedCharsCheck != null && path.StartsWith(pathForReservedCharsCheck))
							{
								FolderOrigin.CheckForReservedCharacters(domain, path);
							}
							AssetLocation location = new AssetLocation(domain, path);
							this._fileLookup.Add(location, file.FullName);
						}
					}
				}
			}
		}

		public void LoadAsset(IAsset asset)
		{
			string filePath;
			if (!this._fileLookup.TryGetValue(asset.Location, out filePath))
			{
				throw new Exception("Requested asset [" + ((asset != null) ? asset.ToString() : null) + "] could not be found");
			}
			asset.Data = File.ReadAllBytes(filePath);
		}

		public bool TryLoadAsset(IAsset asset)
		{
			string filePath;
			if (!this._fileLookup.TryGetValue(asset.Location, out filePath))
			{
				return false;
			}
			asset.Data = File.ReadAllBytes(filePath);
			return true;
		}

		public List<IAsset> GetAssets(AssetCategory Category, bool shouldLoad = true)
		{
			List<IAsset> assets = new List<IAsset>();
			if (!Directory.Exists(this.OriginPath))
			{
				return assets;
			}
			foreach (string fullPath in Directory.GetDirectories(this.OriginPath))
			{
				string text = fullPath.Substring(this.OriginPath.Length + 1).ToLowerInvariant();
				ReadOnlySpan<char> readOnlySpan = fullPath;
				char directorySeparatorChar = Path.DirectorySeparatorChar;
				this.ScanAssetFolderRecursive(text, readOnlySpan + new ReadOnlySpan<char>(ref directorySeparatorChar) + Category.Code, assets, shouldLoad);
			}
			return assets;
		}

		public List<IAsset> GetAssets(AssetLocation baseLocation, bool shouldLoad = true)
		{
			List<IAsset> assets = new List<IAsset>();
			this.ScanAssetFolderRecursive(baseLocation.Domain, string.Concat(new string[]
			{
				this.OriginPath,
				Path.DirectorySeparatorChar.ToString(),
				baseLocation.Domain,
				Path.DirectorySeparatorChar.ToString(),
				baseLocation.Path.Replace('/', Path.DirectorySeparatorChar)
			}), assets, shouldLoad);
			return assets;
		}

		private void ScanAssetFolderRecursive(string domain, string currentPath, List<IAsset> list, bool shouldLoad)
		{
			if (!Directory.Exists(currentPath))
			{
				return;
			}
			foreach (string fullPath in Directory.GetDirectories(currentPath))
			{
				this.ScanAssetFolderRecursive(domain, fullPath, list, shouldLoad);
			}
			foreach (string fullPath2 in Directory.GetFiles(currentPath))
			{
				FileInfo f = new FileInfo(fullPath2);
				if (!f.Name.Equals("thumbs.db", StringComparison.InvariantCultureIgnoreCase) && !f.Name.EndsWithOrdinal(".psd") && !f.Name.StartsWith('.'))
				{
					AssetLocation location = new AssetLocation(domain, fullPath2.Substring(this.OriginPath.Length + domain.Length + 2).Replace(Path.DirectorySeparatorChar, '/'));
					list.Add(new Asset(shouldLoad ? File.ReadAllBytes(fullPath2) : null, location, this));
				}
			}
		}

		public virtual bool IsAllowedToAffectGameplay()
		{
			return true;
		}

		public static void CheckForReservedCharacters(string domain, string filepath)
		{
			foreach (string reserved in GlobalConstants.ReservedCharacterSequences)
			{
				if (filepath.Contains(reserved))
				{
					throw new FormatException(string.Concat(new string[] { "Reserved characters ", reserved, " not allowed in filename:- ", domain, ":", filepath }));
				}
			}
		}

		protected readonly Dictionary<AssetLocation, string> _fileLookup = new Dictionary<AssetLocation, string>();
	}
}
