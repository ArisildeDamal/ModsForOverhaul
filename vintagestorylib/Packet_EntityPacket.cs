using System;

public class Packet_EntityPacket
{
	public void SetEntityId(long value)
	{
		this.EntityId = value;
	}

	public void SetPacketid(int value)
	{
		this.Packetid = value;
	}

	public void SetData(byte[] value)
	{
		this.Data = value;
	}

	internal void InitializeValues()
	{
	}

	public long EntityId;

	public int Packetid;

	public byte[] Data;

	public const int EntityIdFieldID = 1;

	public const int PacketidFieldID = 2;

	public const int DataFieldID = 3;

	public int size;
}
