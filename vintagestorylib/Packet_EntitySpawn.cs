using System;

public class Packet_EntitySpawn
{
	public Packet_Entity[] GetEntity()
	{
		return this.Entity;
	}

	public void SetEntity(Packet_Entity[] value, int count, int length)
	{
		this.Entity = value;
		this.EntityCount = count;
		this.EntityLength = length;
	}

	public void SetEntity(Packet_Entity[] value)
	{
		this.Entity = value;
		this.EntityCount = value.Length;
		this.EntityLength = value.Length;
	}

	public int GetEntityCount()
	{
		return this.EntityCount;
	}

	public void EntityAdd(Packet_Entity value)
	{
		if (this.EntityCount >= this.EntityLength)
		{
			if ((this.EntityLength *= 2) == 0)
			{
				this.EntityLength = 1;
			}
			Packet_Entity[] newArray = new Packet_Entity[this.EntityLength];
			for (int i = 0; i < this.EntityCount; i++)
			{
				newArray[i] = this.Entity[i];
			}
			this.Entity = newArray;
		}
		Packet_Entity[] entity = this.Entity;
		int entityCount = this.EntityCount;
		this.EntityCount = entityCount + 1;
		entity[entityCount] = value;
	}

	internal void InitializeValues()
	{
	}

	public Packet_Entity[] Entity;

	public int EntityCount;

	public int EntityLength;

	public const int EntityFieldID = 1;

	public int size;
}
