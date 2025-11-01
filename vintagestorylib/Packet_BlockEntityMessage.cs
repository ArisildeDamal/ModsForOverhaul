using System;

public class Packet_BlockEntityMessage
{
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

	public void SetPacketId(int value)
	{
		this.PacketId = value;
	}

	public void SetData(byte[] value)
	{
		this.Data = value;
	}

	internal void InitializeValues()
	{
	}

	public int X;

	public int Y;

	public int Z;

	public int PacketId;

	public byte[] Data;

	public const int XFieldID = 1;

	public const int YFieldID = 2;

	public const int ZFieldID = 3;

	public const int PacketIdFieldID = 4;

	public const int DataFieldID = 5;

	public int size;
}
