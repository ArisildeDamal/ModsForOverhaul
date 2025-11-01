using System;

public class Packet_ModId
{
	public void SetModid(string value)
	{
		this.Modid = value;
	}

	public void SetName(string value)
	{
		this.Name = value;
	}

	public void SetVersion(string value)
	{
		this.Version = value;
	}

	public void SetNetworkversion(string value)
	{
		this.Networkversion = value;
	}

	public void SetRequiredOnClient(bool value)
	{
		this.RequiredOnClient = value;
	}

	internal void InitializeValues()
	{
	}

	public string Modid;

	public string Name;

	public string Version;

	public string Networkversion;

	public bool RequiredOnClient;

	public const int ModidFieldID = 1;

	public const int NameFieldID = 2;

	public const int VersionFieldID = 3;

	public const int NetworkversionFieldID = 4;

	public const int RequiredOnClientFieldID = 5;

	public int size;
}
