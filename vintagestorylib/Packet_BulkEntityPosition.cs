using System;

public class Packet_BulkEntityPosition
{
	public Packet_EntityPosition[] GetEntityPositions()
	{
		return this.EntityPositions;
	}

	public void SetEntityPositions(Packet_EntityPosition[] value, int count, int length)
	{
		this.EntityPositions = value;
		this.EntityPositionsCount = count;
		this.EntityPositionsLength = length;
	}

	public void SetEntityPositions(Packet_EntityPosition[] value)
	{
		this.EntityPositions = value;
		this.EntityPositionsCount = value.Length;
		this.EntityPositionsLength = value.Length;
	}

	public int GetEntityPositionsCount()
	{
		return this.EntityPositionsCount;
	}

	public void EntityPositionsAdd(Packet_EntityPosition value)
	{
		if (this.EntityPositionsCount >= this.EntityPositionsLength)
		{
			if ((this.EntityPositionsLength *= 2) == 0)
			{
				this.EntityPositionsLength = 1;
			}
			Packet_EntityPosition[] newArray = new Packet_EntityPosition[this.EntityPositionsLength];
			for (int i = 0; i < this.EntityPositionsCount; i++)
			{
				newArray[i] = this.EntityPositions[i];
			}
			this.EntityPositions = newArray;
		}
		Packet_EntityPosition[] entityPositions = this.EntityPositions;
		int entityPositionsCount = this.EntityPositionsCount;
		this.EntityPositionsCount = entityPositionsCount + 1;
		entityPositions[entityPositionsCount] = value;
	}

	internal void InitializeValues()
	{
	}

	public Packet_EntityPosition[] EntityPositions;

	public int EntityPositionsCount;

	public int EntityPositionsLength;

	public const int EntityPositionsFieldID = 1;

	public int size;
}
