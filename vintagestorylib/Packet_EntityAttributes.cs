using System;

public class Packet_EntityAttributes
{
	public void SetEntityId(long value)
	{
		this.EntityId = value;
	}

	public void SetData(byte[] value)
	{
		this.Data = value;
	}

	internal void InitializeValues()
	{
	}

	public long EntityId;

	public byte[] Data;

	public const int EntityIdFieldID = 1;

	public const int DataFieldID = 2;

	public int size;
}
