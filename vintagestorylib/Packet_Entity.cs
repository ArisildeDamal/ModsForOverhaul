using System;

public class Packet_Entity
{
	public void SetEntityType(string value)
	{
		this.EntityType = value;
	}

	public void SetEntityId(long value)
	{
		this.EntityId = value;
	}

	public void SetSimulationRange(int value)
	{
		this.SimulationRange = value;
	}

	public void SetData(byte[] value)
	{
		this.Data = value;
	}

	internal void InitializeValues()
	{
	}

	public string EntityType;

	public long EntityId;

	public int SimulationRange;

	public byte[] Data;

	public const int EntityTypeFieldID = 1;

	public const int EntityIdFieldID = 2;

	public const int SimulationRangeFieldID = 3;

	public const int DataFieldID = 4;

	public int size;
}
