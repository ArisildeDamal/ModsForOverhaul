using System;

public class Packet_Entities
{
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

	internal void InitializeValues()
	{
	}

	public Packet_Entity[] Entities;

	public int EntitiesCount;

	public int EntitiesLength;

	public const int EntitiesFieldID = 1;

	public int size;
}
