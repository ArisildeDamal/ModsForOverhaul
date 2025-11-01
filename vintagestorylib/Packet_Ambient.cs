using System;

public class Packet_Ambient
{
	public void SetData(byte[] value)
	{
		this.Data = value;
	}

	internal void InitializeValues()
	{
	}

	public byte[] Data;

	public const int DataFieldID = 1;

	public int size;
}
