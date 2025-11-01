using System;

public class Packet_ServerSetDecors
{
	public void SetSetDecors(byte[] value)
	{
		this.SetDecors = value;
	}

	internal void InitializeValues()
	{
	}

	public byte[] SetDecors;

	public const int SetDecorsFieldID = 1;

	public int size;
}
