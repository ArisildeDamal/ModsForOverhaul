using System;

public class Packet_IntString
{
	public void SetKey_(int value)
	{
		this.Key_ = value;
	}

	public void SetValue_(string value)
	{
		this.Value_ = value;
	}

	internal void InitializeValues()
	{
	}

	public int Key_;

	public string Value_;

	public const int Key_FieldID = 1;

	public const int Value_FieldID = 2;

	public int size;
}
