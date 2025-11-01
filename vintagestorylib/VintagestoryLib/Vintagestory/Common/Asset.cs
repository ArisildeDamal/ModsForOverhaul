using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Vintagestory.Common
{
	public class Asset : IAsset
	{
		public IAssetOrigin Origin
		{
			get
			{
				return this.origin;
			}
			set
			{
				this.origin = value;
			}
		}

		public string Name
		{
			get
			{
				return this.Location.GetName();
			}
		}

		public Asset(byte[] bytes, AssetLocation Location, IAssetOrigin origin)
		{
			this.Data = bytes;
			this.origin = origin;
			this.Location = Location;
		}

		public Asset(AssetLocation Location)
		{
			this.Location = Location;
		}

		string IAsset.Name
		{
			get
			{
				return this.Name;
			}
		}

		byte[] IAsset.Data
		{
			get
			{
				return this.Data;
			}
			set
			{
				this.Data = value;
			}
		}

		AssetLocation IAsset.Location
		{
			get
			{
				return this.Location;
			}
		}

		public bool IsPatched { get; set; }

		public T ToObject<T>(JsonSerializerSettings settings = null)
		{
			T t;
			try
			{
				t = JsonUtil.ToObject<T>(Asset.BytesToString(this.Data), this.Location.Domain, settings);
			}
			catch (Exception e)
			{
				throw new JsonReaderException("Failed deserializing " + this.Name + ": " + e.Message);
			}
			return t;
		}

		public string ToText()
		{
			return Asset.BytesToString(this.Data);
		}

		public BitmapRef ToBitmap(ICoreClientAPI api)
		{
			return api.Render.BitmapCreateFromPng(this.Data);
		}

		public bool IsLoaded()
		{
			return this.Data != null;
		}

		public override string ToString()
		{
			return this.Location.ToString();
		}

		public override int GetHashCode()
		{
			return this.Location.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			return this.Location.Equals(obj);
		}

		public static string BytesToString(byte[] data)
		{
			if (data == null || data.Length == 0)
			{
				return "";
			}
			if (data[0] == 123)
			{
				return Encoding.UTF8.GetString(data);
			}
			string text;
			using (MemoryStream stream = new MemoryStream(data))
			{
				using (StreamReader sr = new StreamReader(stream, Encoding.UTF8, true))
				{
					text = sr.ReadToEnd();
				}
			}
			return text;
		}

		public byte[] Data;

		public AssetLocation Location;

		public string FilePath;

		private IAssetOrigin origin;
	}
}
