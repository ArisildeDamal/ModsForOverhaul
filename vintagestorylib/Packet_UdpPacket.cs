using System;

public class Packet_UdpPacket : IPacket
{
	public void SerializeTo(CitoStream stream)
	{
		Packet_UdpPacketSerializer.Serialize(stream, this);
	}

	public void SetId(int value)
	{
		this.Id = value;
	}

	public void SetEntityPosition(Packet_EntityPosition value)
	{
		this.EntityPosition = value;
	}

	public void SetBulkPositions(Packet_BulkEntityPosition value)
	{
		this.BulkPositions = value;
	}

	public void SetChannelPacket(Packet_CustomPacket value)
	{
		this.ChannelPacket = value;
	}

	public void SetConnectionPacket(Packet_ConnectionPacket value)
	{
		this.ConnectionPacket = value;
	}

	public void SetLength(int value)
	{
		this.Length = value;
	}

	internal void InitializeValues()
	{
	}

	public int Id;

	public Packet_EntityPosition EntityPosition;

	public Packet_BulkEntityPosition BulkPositions;

	public Packet_CustomPacket ChannelPacket;

	public Packet_ConnectionPacket ConnectionPacket;

	public int Length;

	public const int IdFieldID = 1;

	public const int EntityPositionFieldID = 2;

	public const int BulkPositionsFieldID = 3;

	public const int ChannelPacketFieldID = 4;

	public const int ConnectionPacketFieldID = 5;

	public const int LengthFieldID = 6;

	public int size;
}
