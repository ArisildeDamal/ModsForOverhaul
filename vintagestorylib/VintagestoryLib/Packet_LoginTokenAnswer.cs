using System;

public class Packet_LoginTokenAnswer
{
	public void SetToken(string value)
	{
		this.Token = value;
	}

	internal void InitializeValues()
	{
	}

	public string Token;

	public const int TokenFieldID = 1;

	public int size;
}
