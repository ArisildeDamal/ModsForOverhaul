using System;

public class Packet_ServerSetBlocks
{
	public void SetSetBlocks(byte[] value)
	{
		this.SetBlocks = value;
	}

	internal void InitializeValues()
	{
	}

	public byte[] SetBlocks;

	public const int SetBlocksFieldID = 1;

	public int size;
}
