using System;

public class Packet_MoveKeyChange
{
	public void SetKey(int value)
	{
		this.Key = value;
	}

	public void SetDown(int value)
	{
		this.Down = value;
	}

	internal void InitializeValues()
	{
		this.Key = 0;
	}

	public int Key;

	public int Down;

	public const int KeyFieldID = 1;

	public const int DownFieldID = 2;

	public int size;
}
