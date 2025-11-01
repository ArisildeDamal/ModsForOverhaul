using System;

public class Packet_PartialAttribute
{
	public void SetPath(string value)
	{
		this.Path = value;
	}

	public void SetData(byte[] value)
	{
		this.Data = value;
	}

	internal void InitializeValues()
	{
	}

	public string Path;

	public byte[] Data;

	public const int PathFieldID = 1;

	public const int DataFieldID = 2;

	public int size;
}
