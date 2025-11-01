using System;

public class Packet_ServerRedirect
{
	public void SetName(string value)
	{
		this.Name = value;
	}

	public void SetHost(string value)
	{
		this.Host = value;
	}

	internal void InitializeValues()
	{
	}

	public string Name;

	public string Host;

	public const int NameFieldID = 1;

	public const int HostFieldID = 2;

	public int size;
}
