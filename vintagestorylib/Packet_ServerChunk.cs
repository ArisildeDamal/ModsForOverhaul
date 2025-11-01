using System;

public class Packet_ServerChunk
{
	public void SetBlocks(byte[] value)
	{
		this.Blocks = value;
	}

	public void SetLight(byte[] value)
	{
		this.Light = value;
	}

	public void SetLightSat(byte[] value)
	{
		this.LightSat = value;
	}

	public void SetLiquids(byte[] value)
	{
		this.Liquids = value;
	}

	public int[] GetLightPositions()
	{
		return this.LightPositions;
	}

	public void SetLightPositions(int[] value, int count, int length)
	{
		this.LightPositions = value;
		this.LightPositionsCount = count;
		this.LightPositionsLength = length;
	}

	public void SetLightPositions(int[] value)
	{
		this.LightPositions = value;
		this.LightPositionsCount = value.Length;
		this.LightPositionsLength = value.Length;
	}

	public int GetLightPositionsCount()
	{
		return this.LightPositionsCount;
	}

	public void LightPositionsAdd(int value)
	{
		if (this.LightPositionsCount >= this.LightPositionsLength)
		{
			if ((this.LightPositionsLength *= 2) == 0)
			{
				this.LightPositionsLength = 1;
			}
			int[] newArray = new int[this.LightPositionsLength];
			for (int i = 0; i < this.LightPositionsCount; i++)
			{
				newArray[i] = this.LightPositions[i];
			}
			this.LightPositions = newArray;
		}
		int[] lightPositions = this.LightPositions;
		int lightPositionsCount = this.LightPositionsCount;
		this.LightPositionsCount = lightPositionsCount + 1;
		lightPositions[lightPositionsCount] = value;
	}

	public void SetX(int value)
	{
		this.X = value;
	}

	public void SetY(int value)
	{
		this.Y = value;
	}

	public void SetZ(int value)
	{
		this.Z = value;
	}

	public Packet_Entity[] GetEntities()
	{
		return this.Entities;
	}

	public void SetEntities(Packet_Entity[] value, int count, int length)
	{
		this.Entities = value;
		this.EntitiesCount = count;
		this.EntitiesLength = length;
	}

	public void SetEntities(Packet_Entity[] value)
	{
		this.Entities = value;
		this.EntitiesCount = value.Length;
		this.EntitiesLength = value.Length;
	}

	public int GetEntitiesCount()
	{
		return this.EntitiesCount;
	}

	public void EntitiesAdd(Packet_Entity value)
	{
		if (this.EntitiesCount >= this.EntitiesLength)
		{
			if ((this.EntitiesLength *= 2) == 0)
			{
				this.EntitiesLength = 1;
			}
			Packet_Entity[] newArray = new Packet_Entity[this.EntitiesLength];
			for (int i = 0; i < this.EntitiesCount; i++)
			{
				newArray[i] = this.Entities[i];
			}
			this.Entities = newArray;
		}
		Packet_Entity[] entities = this.Entities;
		int entitiesCount = this.EntitiesCount;
		this.EntitiesCount = entitiesCount + 1;
		entities[entitiesCount] = value;
	}

	public Packet_BlockEntity[] GetBlockEntities()
	{
		return this.BlockEntities;
	}

	public void SetBlockEntities(Packet_BlockEntity[] value, int count, int length)
	{
		this.BlockEntities = value;
		this.BlockEntitiesCount = count;
		this.BlockEntitiesLength = length;
	}

	public void SetBlockEntities(Packet_BlockEntity[] value)
	{
		this.BlockEntities = value;
		this.BlockEntitiesCount = value.Length;
		this.BlockEntitiesLength = value.Length;
	}

	public int GetBlockEntitiesCount()
	{
		return this.BlockEntitiesCount;
	}

	public void BlockEntitiesAdd(Packet_BlockEntity value)
	{
		if (this.BlockEntitiesCount >= this.BlockEntitiesLength)
		{
			if ((this.BlockEntitiesLength *= 2) == 0)
			{
				this.BlockEntitiesLength = 1;
			}
			Packet_BlockEntity[] newArray = new Packet_BlockEntity[this.BlockEntitiesLength];
			for (int i = 0; i < this.BlockEntitiesCount; i++)
			{
				newArray[i] = this.BlockEntities[i];
			}
			this.BlockEntities = newArray;
		}
		Packet_BlockEntity[] blockEntities = this.BlockEntities;
		int blockEntitiesCount = this.BlockEntitiesCount;
		this.BlockEntitiesCount = blockEntitiesCount + 1;
		blockEntities[blockEntitiesCount] = value;
	}

	public void SetModdata(byte[] value)
	{
		this.Moddata = value;
	}

	public void SetEmpty(int value)
	{
		this.Empty = value;
	}

	public int[] GetDecorsPos()
	{
		return this.DecorsPos;
	}

	public void SetDecorsPos(int[] value, int count, int length)
	{
		this.DecorsPos = value;
		this.DecorsPosCount = count;
		this.DecorsPosLength = length;
	}

	public void SetDecorsPos(int[] value)
	{
		this.DecorsPos = value;
		this.DecorsPosCount = value.Length;
		this.DecorsPosLength = value.Length;
	}

	public int GetDecorsPosCount()
	{
		return this.DecorsPosCount;
	}

	public void DecorsPosAdd(int value)
	{
		if (this.DecorsPosCount >= this.DecorsPosLength)
		{
			if ((this.DecorsPosLength *= 2) == 0)
			{
				this.DecorsPosLength = 1;
			}
			int[] newArray = new int[this.DecorsPosLength];
			for (int i = 0; i < this.DecorsPosCount; i++)
			{
				newArray[i] = this.DecorsPos[i];
			}
			this.DecorsPos = newArray;
		}
		int[] decorsPos = this.DecorsPos;
		int decorsPosCount = this.DecorsPosCount;
		this.DecorsPosCount = decorsPosCount + 1;
		decorsPos[decorsPosCount] = value;
	}

	public int[] GetDecorsIds()
	{
		return this.DecorsIds;
	}

	public void SetDecorsIds(int[] value, int count, int length)
	{
		this.DecorsIds = value;
		this.DecorsIdsCount = count;
		this.DecorsIdsLength = length;
	}

	public void SetDecorsIds(int[] value)
	{
		this.DecorsIds = value;
		this.DecorsIdsCount = value.Length;
		this.DecorsIdsLength = value.Length;
	}

	public int GetDecorsIdsCount()
	{
		return this.DecorsIdsCount;
	}

	public void DecorsIdsAdd(int value)
	{
		if (this.DecorsIdsCount >= this.DecorsIdsLength)
		{
			if ((this.DecorsIdsLength *= 2) == 0)
			{
				this.DecorsIdsLength = 1;
			}
			int[] newArray = new int[this.DecorsIdsLength];
			for (int i = 0; i < this.DecorsIdsCount; i++)
			{
				newArray[i] = this.DecorsIds[i];
			}
			this.DecorsIds = newArray;
		}
		int[] decorsIds = this.DecorsIds;
		int decorsIdsCount = this.DecorsIdsCount;
		this.DecorsIdsCount = decorsIdsCount + 1;
		decorsIds[decorsIdsCount] = value;
	}

	public void SetCompver(int value)
	{
		this.Compver = value;
	}

	internal void InitializeValues()
	{
	}

	public byte[] Blocks;

	public byte[] Light;

	public byte[] LightSat;

	public byte[] Liquids;

	public int[] LightPositions;

	public int LightPositionsCount;

	public int LightPositionsLength;

	public int X;

	public int Y;

	public int Z;

	public Packet_Entity[] Entities;

	public int EntitiesCount;

	public int EntitiesLength;

	public Packet_BlockEntity[] BlockEntities;

	public int BlockEntitiesCount;

	public int BlockEntitiesLength;

	public byte[] Moddata;

	public int Empty;

	public int[] DecorsPos;

	public int DecorsPosCount;

	public int DecorsPosLength;

	public int[] DecorsIds;

	public int DecorsIdsCount;

	public int DecorsIdsLength;

	public int Compver;

	public const int BlocksFieldID = 1;

	public const int LightFieldID = 2;

	public const int LightSatFieldID = 3;

	public const int LiquidsFieldID = 15;

	public const int LightPositionsFieldID = 9;

	public const int XFieldID = 4;

	public const int YFieldID = 5;

	public const int ZFieldID = 6;

	public const int EntitiesFieldID = 7;

	public const int BlockEntitiesFieldID = 8;

	public const int ModdataFieldID = 10;

	public const int EmptyFieldID = 11;

	public const int DecorsPosFieldID = 12;

	public const int DecorsIdsFieldID = 13;

	public const int CompverFieldID = 14;

	public int size;
}
