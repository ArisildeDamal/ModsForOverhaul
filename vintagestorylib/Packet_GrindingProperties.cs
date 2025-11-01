using System;

public class Packet_GrindingProperties
{
	public void SetGroundStack(byte[] value)
	{
		this.GroundStack = value;
	}

	internal void InitializeValues()
	{
	}

	public byte[] GroundStack;

	public const int GroundStackFieldID = 1;

	public int size;
}
