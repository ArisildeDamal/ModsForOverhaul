using System;

public class Packet_QueuePacket
{
	public void SetPosition(int value)
	{
		this.Position = value;
	}

	internal void InitializeValues()
	{
	}

	public int Position;

	public const int PositionFieldID = 1;

	public int size;
}
