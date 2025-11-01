using System;
using Newtonsoft.Json;

namespace Vintagestory.Server
{
	[JsonObject(MemberSerialization.OptIn)]
	public class ServerSettings
	{
		public static bool WatchModFolder
		{
			get
			{
				return ServerSettings.instance.watchModFolder;
			}
		}

		public static void Save()
		{
		}

		public static void Load()
		{
			ServerSettings.instance = new ServerSettings();
		}

		public static ServerSettings instance = new ServerSettings();

		[JsonProperty]
		private bool watchModFolder = true;

		[JsonProperty]
		public static string Language = "en";
	}
}
