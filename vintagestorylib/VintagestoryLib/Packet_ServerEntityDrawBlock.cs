using System;

public class Packet_ServerEntityDrawBlock
{
	public void SetBlockType(int value)
	{
		this.BlockType = value;
	}

	internal void InitializeValues()
	{
	}

	public int BlockType;

	public const int BlockTypeFieldID = 1;

	public int size;
}
