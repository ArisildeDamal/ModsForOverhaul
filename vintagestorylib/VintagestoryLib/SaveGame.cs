using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.Server;

[ProtoContract]
public class SaveGame : ISaveGame
{
	[ProtoMember(40, IsRequired = false)]
	public PlayerSpawnPos DefaultSpawn { get; set; }

	string ISaveGame.PlayStyle
	{
		get
		{
			return this.PlayStyle;
		}
		set
		{
			this.PlayStyle = value;
		}
	}

	string ISaveGame.WorldType
	{
		get
		{
			return this.WorldType;
		}
		set
		{
			this.WorldType = value;
		}
	}

	bool ISaveGame.IsNew
	{
		get
		{
			return this.IsNewWorld;
		}
	}

	int ISaveGame.Seed
	{
		get
		{
			return this.Seed;
		}
		set
		{
			this.Seed = value;
		}
	}

	long ISaveGame.TotalGameSeconds
	{
		get
		{
			return this.TotalGameSeconds;
		}
		set
		{
			this.TotalGameSeconds = value;
		}
	}

	string ISaveGame.WorldName
	{
		get
		{
			return this.WorldName;
		}
		set
		{
			this.WorldName = value;
		}
	}

	bool ISaveGame.EntitySpawning
	{
		get
		{
			return this.EntitySpawning;
		}
		set
		{
			this.EntitySpawning = value;
		}
	}

	List<LandClaim> ISaveGame.LandClaims
	{
		get
		{
			return this.LandClaims;
		}
	}

	ITreeAttribute ISaveGame.WorldConfiguration
	{
		get
		{
			return this.WorldConfiguration;
		}
	}

	string ISaveGame.CreatedGameVersion
	{
		get
		{
			return this.CreatedGameVersion;
		}
	}

	string ISaveGame.LastSavedGameVersion
	{
		get
		{
			return this.LastSavedGameVersionWhenLoaded ?? this.LastSavedGameVersion;
		}
	}

	string ISaveGame.SavegameIdentifier
	{
		get
		{
			return this.SavegameIdentifier;
		}
	}

	public static SaveGame CreateNew(ServerConfig config)
	{
		SaveGame saveGame = new SaveGame();
		StartServerArgs startserverargs = config.WorldConfig;
		saveGame.ModData = new ConcurrentDictionary<string, byte[]>();
		saveGame.Seed = ((config.Seed == null) ? new Random(Guid.NewGuid().GetHashCode()).Next() : config.Seed.Value);
		saveGame.IsNewWorld = true;
		saveGame.SavegameIdentifier = Guid.NewGuid().ToString();
		saveGame.MapSizeX = Math.Min(config.MapSizeX, 67108864);
		saveGame.MapSizeY = Math.Min(config.MapSizeY, 16384);
		saveGame.MapSizeZ = Math.Min(config.MapSizeZ, 67108864);
		saveGame.EntitySpawning = true;
		saveGame.CalendarSpeedMul = 0.5f;
		saveGame.LastBlockItemMappingVersion = GameVersion.BlockItemMappingVersion;
		saveGame.TimeSpeedModifiers = new Dictionary<string, float> { { "baseline", 60f } };
		saveGame.WorldName = startserverargs.WorldName;
		saveGame.WorldType = startserverargs.WorldType;
		saveGame.PlayStyle = startserverargs.PlayStyle;
		saveGame.PlayStyleLangCode = startserverargs.PlayStyleLangCode;
		saveGame.CreatedByPlayerName = startserverargs.CreatedByPlayerName;
		saveGame.LastHerdId = 1L;
		saveGame.CreatedWorldGenVersion = 3;
		saveGame.LandClaims = new List<LandClaim>();
		saveGame.LastPlayed = DateTime.Now.ToString("O");
		saveGame.CreatedGameVersion = "1.21.5";
		saveGame.LastSavedGameVersion = "1.21.5";
		return saveGame;
	}

	public static void SetNewWorldConfig(SaveGame savegame, ServerMain server)
	{
		ServerConfig config = server.Config;
		WorldConfig wcu = new WorldConfig(server.Api.ModLoader.Mods.Select((Mod m) => (ModContainer)m).ToList<ModContainer>());
		wcu.selectPlayStyle(server.Config.WorldConfig.PlayStyle);
		if (config.WorldConfig.WorldConfiguration.Token.HasValues)
		{
			Dictionary<string, WorldConfigurationValue> customConfig = new Dictionary<string, WorldConfigurationValue>();
			wcu.loadWorldConfigValues(config.WorldConfig.WorldConfiguration, customConfig);
			wcu.updateJWorldConfigFrom(customConfig);
		}
		savegame.WorldConfiguration = wcu.Jworldconfig.ToAttribute() as TreeAttribute;
		savegame.TotalGameSeconds = (savegame.TotalGameSecondsStart = (long)savegame.GetTotalGameSecondsStart());
		if (savegame.WorldConfiguration != null && savegame.WorldConfiguration.HasAttribute("worldWidth"))
		{
			savegame.MapSizeX = Math.Min(savegame.WorldConfiguration.GetString("worldWidth", null).ToInt(config.MapSizeX), 67108864);
		}
		if (savegame.WorldConfiguration != null && savegame.WorldConfiguration.HasAttribute("worldLength"))
		{
			savegame.MapSizeZ = Math.Min(savegame.WorldConfiguration.GetString("worldLength", null).ToInt(config.MapSizeZ), 67108864);
		}
	}

	public int GetTotalGameSecondsStart()
	{
		int days = Math.Max(1, this.WorldConfiguration.GetAsInt("daysPerMonth", 12));
		return 28800 + 86400 * days * 4;
	}

	public DateTime GetLastPlayed()
	{
		return DateTime.ParseExact(this.LastPlayed, "O", GlobalConstants.DefaultCultureInfo);
	}

	internal void UpdateChunkdataVersion()
	{
		if (2 > this.HighestChunkdataVersion)
		{
			this.HighestChunkdataVersion = 2;
		}
	}

	public void Init(ServerMain server)
	{
		if (this.LastSavedGameVersion == null)
		{
			this.LastSavedGameVersion = this.CreatedGameVersion;
		}
		if (this.TimeSpeedModifiers == null)
		{
			this.TimeSpeedModifiers = new Dictionary<string, float> { { "baseline", 60f } };
		}
		server.PlayersByUid.Clear();
		this.LoadWorldConfig();
		this.UpdateWorldConfig(server);
	}

	private void UpdateWorldConfig(ServerMain server)
	{
		if (GameVersion.IsLowerVersionThan(this.LastSavedGameVersion, "1.21.0-dev.1"))
		{
			this.WorldConfiguration.SetBool("allowFallingBlocks", server.Config.AllowFallingBlocks);
			this.WorldConfiguration.SetBool("allowFireSpread", server.Config.AllowFireSpread);
		}
	}

	public void LoadWorldConfig()
	{
		if (this.WorldConfigBytes == null)
		{
			this.WorldConfiguration = new TreeAttribute();
		}
		else
		{
			using (MemoryStream ms = new MemoryStream(this.WorldConfigBytes))
			{
				using (BinaryReader reader = new BinaryReader(ms))
				{
					this.WorldConfiguration = new TreeAttribute();
					this.WorldConfiguration.FromBytes(reader);
				}
			}
		}
		if (GameVersion.IsLowerVersionThan(this.LastSavedGameVersion, "1.19.0-pre.6"))
		{
			if (!this.WorldConfiguration.HasAttribute("upheavelCommonness"))
			{
				this.WorldConfiguration.SetString("upheavelCommonness", "0.4");
			}
			if (!this.WorldConfiguration.HasAttribute("landformScale"))
			{
				this.WorldConfiguration.SetString("landformScale", "1.2");
			}
		}
	}

	[ProtoAfterDeserialization]
	private void afterDeserialization()
	{
		if (this.PlayStyle == null)
		{
			this.PlayStyle = this.WorldPlayStyle.ToString().ToLowerInvariant();
			this.WorldType = ((this.WorldPlayStyle == SaveGame.EnumPlayStyle.CreativeBuilding) ? "superflat" : "standard");
		}
		if (this.LastBlockItemMappingVersion >= 1)
		{
			this.RemappingsAppliedByCode["game:v1.12clayplanters"] = true;
		}
		if (this.WorldType == null)
		{
			this.WorldType = "standard";
		}
		if (this.PlayStyle == null)
		{
			this.PlayStyle = "surviveandbuild";
		}
		if (this.WorldConfiguration == null)
		{
			this.WorldConfiguration = new TreeAttribute();
		}
		if (this.SavegameIdentifier == null)
		{
			this.SavegameIdentifier = Guid.NewGuid().ToString();
		}
		if (GameVersion.IsLowerVersionThan(this.LastSavedGameVersion, "1.13-pre.1"))
		{
			this.CalendarSpeedMul = 0.5f;
		}
		if (GameVersion.IsLowerVersionThan(this.LastSavedGameVersion, "1.17-pre.5"))
		{
			int days = Math.Max(1, this.WorldConfiguration.GetAsInt("daysPerMonth", 12));
			this.TotalGameSecondsStart = (long)(28800 + 86400 * days * 4);
		}
		this.LastSavedGameVersionWhenLoaded = this.LastSavedGameVersion;
	}

	public byte[] GetData(string name)
	{
		if (this.ModData == null)
		{
			return null;
		}
		byte[] data;
		if (this.ModData.TryGetValue(name, out data))
		{
			return data;
		}
		return null;
	}

	public void StoreData(string name, byte[] value)
	{
		if (this.ModData == null)
		{
			this.ModData = new ConcurrentDictionary<string, byte[]>();
		}
		this.ModData[name] = value;
	}

	public T GetData<T>(string name, T defaultValue = default(T))
	{
		if (this.ModData == null)
		{
			return defaultValue;
		}
		byte[] bytes;
		if (!this.ModData.TryGetValue(name, out bytes))
		{
			return defaultValue;
		}
		if (bytes == null)
		{
			return defaultValue;
		}
		return SerializerUtil.Deserialize<T>(bytes);
	}

	public void StoreData<T>(string name, T data)
	{
		if (this.ModData == null)
		{
			this.ModData = new ConcurrentDictionary<string, byte[]>();
		}
		this.ModData[name] = SerializerUtil.Serialize<T>(data);
	}

	public SaveGame GetSaveGameForSaving(ServerConfig config)
	{
		return this;
	}

	internal void WillSave(FastMemoryStream ms)
	{
		this.LastPlayed = DateTime.Now.ToString("O");
		this.LastSavedGameVersion = "1.21.5";
		ms.Reset();
		using (BinaryWriter writer = new BinaryWriter(ms))
		{
			if (this.WorldConfiguration == null)
			{
				new TreeAttribute().ToBytes(writer);
			}
			else
			{
				this.WorldConfiguration.ToBytes(writer);
			}
		}
		this.WorldConfigBytes = ms.ToArray();
	}

	internal void UpdateLandClaims(List<LandClaim> toSave)
	{
		List<LandClaim> landclaims = new List<LandClaim>();
		for (int i = 0; i < toSave.Count; i++)
		{
			landclaims.Add(toSave[i]);
		}
		this.LandClaims = landclaims;
	}

	[ProtoMember(1, IsRequired = false)]
	public int MapSizeX;

	[ProtoMember(2, IsRequired = false)]
	public int MapSizeY;

	[ProtoMember(3, IsRequired = false)]
	public int MapSizeZ;

	[ProtoMember(4, IsRequired = false)]
	[Obsolete("Now stored in gamedatabase")]
	public Dictionary<string, ServerWorldPlayerData> PlayerDataByUID;

	[ProtoMember(7, IsRequired = false)]
	public int Seed;

	[ProtoMember(8, IsRequired = false)]
	public long SimulationCurrentFrame;

	[ProtoMember(10, IsRequired = false)]
	public long LastEntityId;

	[ProtoMember(11, IsRequired = false)]
	public ConcurrentDictionary<string, byte[]> ModData;

	[ProtoMember(12, IsRequired = false)]
	public long TotalGameSeconds;

	[Obsolete("Replaced with TimeSpeedModifiers")]
	[ProtoMember(19, IsRequired = false)]
	public int GameTimeSpeed;

	[ProtoMember(13, IsRequired = false)]
	public string WorldName;

	[ProtoMember(14, IsRequired = false)]
	public int TotalSecondsPlayed;

	[Obsolete("Replaced with string playstyle")]
	[ProtoMember(16, IsRequired = false)]
	private SaveGame.EnumPlayStyle WorldPlayStyle;

	[ProtoMember(20, IsRequired = false)]
	public int MiniDimensionsCreated;

	[ProtoMember(17, IsRequired = false)]
	public string LastPlayed;

	[ProtoMember(18, IsRequired = false)]
	public string CreatedGameVersion;

	[ProtoMember(33, IsRequired = false)]
	[Obsolete]
	public int LastBlockItemMappingVersion;

	[ProtoMember(21, IsRequired = false)]
	public string LastSavedGameVersion;

	[ProtoMember(22, IsRequired = false)]
	public string CreatedByPlayerName;

	[ProtoMember(23, IsRequired = false)]
	public bool EntitySpawning;

	[ProtoMember(25, IsRequired = false)]
	public float HoursPerDay = 24f;

	[ProtoMember(26, IsRequired = false)]
	public long LastHerdId;

	[ProtoMember(27, IsRequired = false)]
	public List<LandClaim> LandClaims = new List<LandClaim>();

	[ProtoMember(28, IsRequired = false)]
	public Dictionary<string, float> TimeSpeedModifiers;

	[ProtoMember(29, IsRequired = false)]
	public string PlayStyle;

	[ProtoMember(32, IsRequired = false)]
	public string PlayStyleLangCode;

	[ProtoMember(30, IsRequired = false)]
	public string WorldType;

	[ProtoMember(31, IsRequired = false)]
	private byte[] WorldConfigBytes;

	[ProtoMember(34, IsRequired = false)]
	public string SavegameIdentifier;

	[ProtoMember(35, IsRequired = false)]
	public float CalendarSpeedMul;

	[ProtoMember(36, IsRequired = false)]
	public Dictionary<string, bool> RemappingsAppliedByCode = new Dictionary<string, bool>();

	[ProtoMember(37, IsRequired = false)]
	public int HighestChunkdataVersion;

	[ProtoMember(38, IsRequired = false)]
	public long TotalGameSecondsStart;

	[ProtoMember(39, IsRequired = false)]
	public int CreatedWorldGenVersion;

	public ITreeAttribute WorldConfiguration = new TreeAttribute();

	public bool IsNewWorld;

	private string LastSavedGameVersionWhenLoaded;

	private enum EnumPlayStyle
	{
		WildernessSurvival,
		SurviveAndBuild,
		SurviveAndAutomate,
		CreativeBuilding
	}
}
