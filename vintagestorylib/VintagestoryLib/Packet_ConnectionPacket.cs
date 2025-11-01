using System;

public class Packet_ConnectionPacket
{
	public void SetLoginToken(string value)
	{
		this.LoginToken = value;
	}

	internal void InitializeValues()
	{
	}

	public string LoginToken;

	public const int LoginTokenFieldID = 1;

	public int size;
}
