using System;

public class Packet_ServerIdentification
{
	public void SetNetworkVersion(string value)
	{
		this.NetworkVersion = value;
	}

	public void SetGameVersion(string value)
	{
		this.GameVersion = value;
	}

	public void SetServerName(string value)
	{
		this.ServerName = value;
	}

	public void SetMapSizeX(int value)
	{
		this.MapSizeX = value;
	}

	public void SetMapSizeY(int value)
	{
		this.MapSizeY = value;
	}

	public void SetMapSizeZ(int value)
	{
		this.MapSizeZ = value;
	}

	public void SetRegionMapSizeX(int value)
	{
		this.RegionMapSizeX = value;
	}

	public void SetRegionMapSizeY(int value)
	{
		this.RegionMapSizeY = value;
	}

	public void SetRegionMapSizeZ(int value)
	{
		this.RegionMapSizeZ = value;
	}

	public void SetDisableShadows(int value)
	{
		this.DisableShadows = value;
	}

	public void SetPlayerAreaSize(int value)
	{
		this.PlayerAreaSize = value;
	}

	public void SetSeed(int value)
	{
		this.Seed = value;
	}

	public void SetPlayStyle(string value)
	{
		this.PlayStyle = value;
	}

	public void SetRequireRemapping(int value)
	{
		this.RequireRemapping = value;
	}

	public Packet_ModId[] GetMods()
	{
		return this.Mods;
	}

	public void SetMods(Packet_ModId[] value, int count, int length)
	{
		this.Mods = value;
		this.ModsCount = count;
		this.ModsLength = length;
	}

	public void SetMods(Packet_ModId[] value)
	{
		this.Mods = value;
		this.ModsCount = value.Length;
		this.ModsLength = value.Length;
	}

	public int GetModsCount()
	{
		return this.ModsCount;
	}

	public void ModsAdd(Packet_ModId value)
	{
		if (this.ModsCount >= this.ModsLength)
		{
			if ((this.ModsLength *= 2) == 0)
			{
				this.ModsLength = 1;
			}
			Packet_ModId[] newArray = new Packet_ModId[this.ModsLength];
			for (int i = 0; i < this.ModsCount; i++)
			{
				newArray[i] = this.Mods[i];
			}
			this.Mods = newArray;
		}
		Packet_ModId[] mods = this.Mods;
		int modsCount = this.ModsCount;
		this.ModsCount = modsCount + 1;
		mods[modsCount] = value;
	}

	public void SetWorldConfiguration(byte[] value)
	{
		this.WorldConfiguration = value;
	}

	public void SetSavegameIdentifier(string value)
	{
		this.SavegameIdentifier = value;
	}

	public void SetPlayListCode(string value)
	{
		this.PlayListCode = value;
	}

	public string[] GetServerModIdBlackList()
	{
		return this.ServerModIdBlackList;
	}

	public void SetServerModIdBlackList(string[] value, int count, int length)
	{
		this.ServerModIdBlackList = value;
		this.ServerModIdBlackListCount = count;
		this.ServerModIdBlackListLength = length;
	}

	public void SetServerModIdBlackList(string[] value)
	{
		this.ServerModIdBlackList = value;
		this.ServerModIdBlackListCount = value.Length;
		this.ServerModIdBlackListLength = value.Length;
	}

	public int GetServerModIdBlackListCount()
	{
		return this.ServerModIdBlackListCount;
	}

	public void ServerModIdBlackListAdd(string value)
	{
		if (this.ServerModIdBlackListCount >= this.ServerModIdBlackListLength)
		{
			if ((this.ServerModIdBlackListLength *= 2) == 0)
			{
				this.ServerModIdBlackListLength = 1;
			}
			string[] newArray = new string[this.ServerModIdBlackListLength];
			for (int i = 0; i < this.ServerModIdBlackListCount; i++)
			{
				newArray[i] = this.ServerModIdBlackList[i];
			}
			this.ServerModIdBlackList = newArray;
		}
		string[] serverModIdBlackList = this.ServerModIdBlackList;
		int serverModIdBlackListCount = this.ServerModIdBlackListCount;
		this.ServerModIdBlackListCount = serverModIdBlackListCount + 1;
		serverModIdBlackList[serverModIdBlackListCount] = value;
	}

	public string[] GetServerModIdWhiteList()
	{
		return this.ServerModIdWhiteList;
	}

	public void SetServerModIdWhiteList(string[] value, int count, int length)
	{
		this.ServerModIdWhiteList = value;
		this.ServerModIdWhiteListCount = count;
		this.ServerModIdWhiteListLength = length;
	}

	public void SetServerModIdWhiteList(string[] value)
	{
		this.ServerModIdWhiteList = value;
		this.ServerModIdWhiteListCount = value.Length;
		this.ServerModIdWhiteListLength = value.Length;
	}

	public int GetServerModIdWhiteListCount()
	{
		return this.ServerModIdWhiteListCount;
	}

	public void ServerModIdWhiteListAdd(string value)
	{
		if (this.ServerModIdWhiteListCount >= this.ServerModIdWhiteListLength)
		{
			if ((this.ServerModIdWhiteListLength *= 2) == 0)
			{
				this.ServerModIdWhiteListLength = 1;
			}
			string[] newArray = new string[this.ServerModIdWhiteListLength];
			for (int i = 0; i < this.ServerModIdWhiteListCount; i++)
			{
				newArray[i] = this.ServerModIdWhiteList[i];
			}
			this.ServerModIdWhiteList = newArray;
		}
		string[] serverModIdWhiteList = this.ServerModIdWhiteList;
		int serverModIdWhiteListCount = this.ServerModIdWhiteListCount;
		this.ServerModIdWhiteListCount = serverModIdWhiteListCount + 1;
		serverModIdWhiteList[serverModIdWhiteListCount] = value;
	}

	internal void InitializeValues()
	{
	}

	public string NetworkVersion;

	public string GameVersion;

	public string ServerName;

	public int MapSizeX;

	public int MapSizeY;

	public int MapSizeZ;

	public int RegionMapSizeX;

	public int RegionMapSizeY;

	public int RegionMapSizeZ;

	public int DisableShadows;

	public int PlayerAreaSize;

	public int Seed;

	public string PlayStyle;

	public int RequireRemapping;

	public Packet_ModId[] Mods;

	public int ModsCount;

	public int ModsLength;

	public byte[] WorldConfiguration;

	public string SavegameIdentifier;

	public string PlayListCode;

	public string[] ServerModIdBlackList;

	public int ServerModIdBlackListCount;

	public int ServerModIdBlackListLength;

	public string[] ServerModIdWhiteList;

	public int ServerModIdWhiteListCount;

	public int ServerModIdWhiteListLength;

	public const int NetworkVersionFieldID = 1;

	public const int GameVersionFieldID = 17;

	public const int ServerNameFieldID = 3;

	public const int MapSizeXFieldID = 7;

	public const int MapSizeYFieldID = 8;

	public const int MapSizeZFieldID = 9;

	public const int RegionMapSizeXFieldID = 21;

	public const int RegionMapSizeYFieldID = 22;

	public const int RegionMapSizeZFieldID = 23;

	public const int DisableShadowsFieldID = 11;

	public const int PlayerAreaSizeFieldID = 12;

	public const int SeedFieldID = 13;

	public const int PlayStyleFieldID = 16;

	public const int RequireRemappingFieldID = 18;

	public const int ModsFieldID = 19;

	public const int WorldConfigurationFieldID = 20;

	public const int SavegameIdentifierFieldID = 24;

	public const int PlayListCodeFieldID = 25;

	public const int ServerModIdBlackListFieldID = 26;

	public const int ServerModIdWhiteListFieldID = 27;

	public int size;
}
