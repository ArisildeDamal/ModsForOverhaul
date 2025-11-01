using System;

public class Packet_BlockEntityPacket
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

	public int X;

	public int Y;

	public int Z;

	public int Packetid;

	public byte[] Data;

	public const int XFieldID = 1;

	public const int YFieldID = 2;

	public const int ZFieldID = 3;

	public const int PacketidFieldID = 4;

	public const int DataFieldID = 5;

	public int size;
}
