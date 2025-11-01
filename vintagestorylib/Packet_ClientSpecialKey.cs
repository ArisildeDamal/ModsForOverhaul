using System;

public class Packet_ClientSpecialKey
{
	public void SetKey_(int value)
	{
		this.Key_ = value;
	}

	internal void InitializeValues()
	{
		this.Key_ = 0;
	}

	public int Key_;

	public const int Key_FieldID = 1;

	public int size;
}
