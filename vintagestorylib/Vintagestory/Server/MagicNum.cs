using System;
using System.IO;
using Newtonsoft.Json;
using Vintagestory.API.Config;

namespace Vintagestory.Server
{
	public class MagicNum
	{
		static MagicNum()
		{
			MagicNum.Load();
		}

		public static void Save()
		{
			MagicNum.ServerMagicNumVersion = "1.3";
			using (TextWriter textWriter = new StreamWriter(MagicNum.FileName))
			{
				textWriter.Write(JsonConvert.SerializeObject(new MagicNum(), Formatting.Indented));
				textWriter.Close();
			}
		}

		public static void Load()
		{
			bool shouldSave = false;
			if (File.Exists(MagicNum.FileName))
			{
				using (TextReader textReader = new StreamReader(MagicNum.FileName))
				{
					JsonConvert.DeserializeObject<MagicNum>(textReader.ReadToEnd());
					textReader.Close();
					goto IL_0039;
				}
			}
			shouldSave = true;
			IL_0039:
			if (MagicNum.ServerAutoSave > 0L && MagicNum.ServerAutoSave < 15L)
			{
				MagicNum.ServerAutoSave = 15L;
			}
			if (MagicNum.ServerMagicNumVersion == null || GameVersion.IsLowerVersionThan(MagicNum.ServerMagicNumVersion, "1.1"))
			{
				if (MagicNum.ChunkColumnsToGeneratePerThreadTick == 7)
				{
					MagicNum.ChunkColumnsToGeneratePerThreadTick = 30;
				}
				if (MagicNum.ChunksColumnsToRequestPerTick == 1)
				{
					MagicNum.ChunksColumnsToRequestPerTick = 4;
				}
				if (MagicNum.ChunksToSendPerTick == 32)
				{
					MagicNum.ChunksToSendPerTick = 192;
				}
				shouldSave = true;
			}
			if (MagicNum.ServerMagicNumVersion == null || GameVersion.IsLowerVersionThan(MagicNum.ServerMagicNumVersion, "1.2"))
			{
				if (MagicNum.ChunkColumnsToGeneratePerThreadTick == 30 && RuntimeEnv.OS == OS.Linux)
				{
					MagicNum.ChunkColumnsToGeneratePerThreadTick = 15;
				}
				if (MagicNum.ChunkRequestTickTime == 40)
				{
					MagicNum.ChunkRequestTickTime = 10;
				}
				if (MagicNum.ChunkThreadTickTime == 10)
				{
					MagicNum.ChunkThreadTickTime = 5;
				}
				shouldSave = true;
			}
			if (MagicNum.ServerMagicNumVersion == null || GameVersion.IsLowerVersionThan(MagicNum.ServerMagicNumVersion, "1.3"))
			{
				shouldSave = true;
			}
			if (shouldSave)
			{
				MagicNum.Save();
			}
			GlobalConstants.DefaultSimulationRange = MagicNum.DefaultSimulationRange;
		}

		public static string FileName = Path.Combine(GamePaths.Config, "servermagicnumbers.json");

		[JsonProperty(Order = 1)]
		public const string Comment = "You can use this config to increase/lower the cpu and network load of the server. A Warning though: Changing these numbers might make your server run unstable or slow. Use at your own risk! Until there is an official documentation, feel free to ask in the forums what the numbers do.";

		[JsonProperty(Order = 2)]
		public static int DefaultEntityTrackingRange = 4;

		[JsonProperty(Order = 3)]
		public static int DefaultSimulationRange = 128;

		public static int ServerChunkSize = 32;

		public static int ServerChunkSizeMask = 31;

		[JsonProperty(Order = 5)]
		public static int RequestChunkColumnsQueueSize = 2000;

		[JsonProperty(Order = 6)]
		public static int ReadyChunksQueueSize = 200;

		[JsonProperty(Order = 7)]
		public static int ChunksColumnsToRequestPerTick = 4;

		[JsonProperty(Order = 8)]
		public static int ChunksToSendPerTick = 192;

		[JsonProperty(Order = 9)]
		public static int ChunkRequestTickTime = 20;

		[JsonProperty(Order = 10)]
		public static int ChunkColumnsToGeneratePerThreadTick = 30;

		[JsonProperty(Order = 11)]
		public static long ServerAutoSave = 300L;

		[JsonProperty(Order = 12)]
		public static int SpawnChunksWidth = 7;

		[JsonProperty(Order = 15)]
		public static int TrackedEntitiesPerClient = 3000;

		public static int ChunkRegionSizeInChunks = 16;

		[JsonProperty(Order = 17)]
		public static int CalendarPacketSecondInterval = 60;

		[JsonProperty(Order = 18)]
		public static int ChunkUnloadInterval = 4000;

		[JsonProperty(Order = 19)]
		public static int UncompressedChunkTTL = 15000;

		[JsonProperty(Order = 20)]
		public static long CompressedChunkTTL = 45000L;

		public static int MapRegionSize = MagicNum.ChunkRegionSizeInChunks * MagicNum.ServerChunkSize;

		[JsonProperty(Order = 21)]
		public static double PlayerDesyncTolerance = 0.02;

		[JsonProperty(Order = 22)]
		public static double PlayerDesyncMaxIntervalls = 20.0;

		[JsonProperty(Order = 23)]
		public static int ChunkThreadTickTime = 5;

		[JsonProperty(Order = 24)]
		public static int AntiAbuseMaxWalkBlocksPer200ms = 3;

		[JsonProperty(Order = 25)]
		public static int AntiAbuseMaxFlySuspicions = 3;

		[JsonProperty(Order = 26)]
		public static int AntiAbuseMaxTeleSuspicions = 8;

		[JsonProperty(Order = 27)]
		public static int MaxPhysicsThreads = 1;

		[JsonProperty(Order = 28)]
		public static int MaxWorldgenThreads = 1;

		[JsonProperty(Order = 29)]
		public static int MaxEntitySpawnsPerTick = 8;

		[JsonProperty(Order = 30)]
		public static string ServerMagicNumVersion = null;
	}
}
