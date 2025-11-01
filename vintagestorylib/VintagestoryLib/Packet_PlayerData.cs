using System;

public class Packet_PlayerData
{
	public void SetClientId(int value)
	{
		this.ClientId = value;
	}

	public void SetEntityId(long value)
	{
		this.EntityId = value;
	}

	public void SetGameMode(int value)
	{
		this.GameMode = value;
	}

	public void SetMoveSpeed(int value)
	{
		this.MoveSpeed = value;
	}

	public void SetFreeMove(int value)
	{
		this.FreeMove = value;
	}

	public void SetNoClip(int value)
	{
		this.NoClip = value;
	}

	public Packet_InventoryContents[] GetInventoryContents()
	{
		return this.InventoryContents;
	}

	public void SetInventoryContents(Packet_InventoryContents[] value, int count, int length)
	{
		this.InventoryContents = value;
		this.InventoryContentsCount = count;
		this.InventoryContentsLength = length;
	}

	public void SetInventoryContents(Packet_InventoryContents[] value)
	{
		this.InventoryContents = value;
		this.InventoryContentsCount = value.Length;
		this.InventoryContentsLength = value.Length;
	}

	public int GetInventoryContentsCount()
	{
		return this.InventoryContentsCount;
	}

	public void InventoryContentsAdd(Packet_InventoryContents value)
	{
		if (this.InventoryContentsCount >= this.InventoryContentsLength)
		{
			if ((this.InventoryContentsLength *= 2) == 0)
			{
				this.InventoryContentsLength = 1;
			}
			Packet_InventoryContents[] newArray = new Packet_InventoryContents[this.InventoryContentsLength];
			for (int i = 0; i < this.InventoryContentsCount; i++)
			{
				newArray[i] = this.InventoryContents[i];
			}
			this.InventoryContents = newArray;
		}
		Packet_InventoryContents[] inventoryContents = this.InventoryContents;
		int inventoryContentsCount = this.InventoryContentsCount;
		this.InventoryContentsCount = inventoryContentsCount + 1;
		inventoryContents[inventoryContentsCount] = value;
	}

	public void SetPlayerUID(string value)
	{
		this.PlayerUID = value;
	}

	public void SetPickingRange(int value)
	{
		this.PickingRange = value;
	}

	public void SetFreeMovePlaneLock(int value)
	{
		this.FreeMovePlaneLock = value;
	}

	public void SetAreaSelectionMode(int value)
	{
		this.AreaSelectionMode = value;
	}

	public string[] GetPrivileges()
	{
		return this.Privileges;
	}

	public void SetPrivileges(string[] value, int count, int length)
	{
		this.Privileges = value;
		this.PrivilegesCount = count;
		this.PrivilegesLength = length;
	}

	public void SetPrivileges(string[] value)
	{
		this.Privileges = value;
		this.PrivilegesCount = value.Length;
		this.PrivilegesLength = value.Length;
	}

	public int GetPrivilegesCount()
	{
		return this.PrivilegesCount;
	}

	public void PrivilegesAdd(string value)
	{
		if (this.PrivilegesCount >= this.PrivilegesLength)
		{
			if ((this.PrivilegesLength *= 2) == 0)
			{
				this.PrivilegesLength = 1;
			}
			string[] newArray = new string[this.PrivilegesLength];
			for (int i = 0; i < this.PrivilegesCount; i++)
			{
				newArray[i] = this.Privileges[i];
			}
			this.Privileges = newArray;
		}
		string[] privileges = this.Privileges;
		int privilegesCount = this.PrivilegesCount;
		this.PrivilegesCount = privilegesCount + 1;
		privileges[privilegesCount] = value;
	}

	public void SetPlayerName(string value)
	{
		this.PlayerName = value;
	}

	public void SetEntitlements(string value)
	{
		this.Entitlements = value;
	}

	public void SetHotbarSlotId(int value)
	{
		this.HotbarSlotId = value;
	}

	public void SetDeaths(int value)
	{
		this.Deaths = value;
	}

	public void SetSpawnx(int value)
	{
		this.Spawnx = value;
	}

	public void SetSpawny(int value)
	{
		this.Spawny = value;
	}

	public void SetSpawnz(int value)
	{
		this.Spawnz = value;
	}

	public void SetRoleCode(string value)
	{
		this.RoleCode = value;
	}

	internal void InitializeValues()
	{
	}

	public int ClientId;

	public long EntityId;

	public int GameMode;

	public int MoveSpeed;

	public int FreeMove;

	public int NoClip;

	public Packet_InventoryContents[] InventoryContents;

	public int InventoryContentsCount;

	public int InventoryContentsLength;

	public string PlayerUID;

	public int PickingRange;

	public int FreeMovePlaneLock;

	public int AreaSelectionMode;

	public string[] Privileges;

	public int PrivilegesCount;

	public int PrivilegesLength;

	public string PlayerName;

	public string Entitlements;

	public int HotbarSlotId;

	public int Deaths;

	public int Spawnx;

	public int Spawny;

	public int Spawnz;

	public string RoleCode;

	public const int ClientIdFieldID = 1;

	public const int EntityIdFieldID = 2;

	public const int GameModeFieldID = 3;

	public const int MoveSpeedFieldID = 4;

	public const int FreeMoveFieldID = 5;

	public const int NoClipFieldID = 6;

	public const int InventoryContentsFieldID = 7;

	public const int PlayerUIDFieldID = 8;

	public const int PickingRangeFieldID = 9;

	public const int FreeMovePlaneLockFieldID = 10;

	public const int AreaSelectionModeFieldID = 11;

	public const int PrivilegesFieldID = 12;

	public const int PlayerNameFieldID = 13;

	public const int EntitlementsFieldID = 14;

	public const int HotbarSlotIdFieldID = 15;

	public const int DeathsFieldID = 16;

	public const int SpawnxFieldID = 17;

	public const int SpawnyFieldID = 18;

	public const int SpawnzFieldID = 19;

	public const int RoleCodeFieldID = 20;

	public int size;
}
