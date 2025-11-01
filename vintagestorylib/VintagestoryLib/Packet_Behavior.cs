using System;

public class Packet_Behavior
{
	public void SetCode(string value)
	{
		this.Code = value;
	}

	public void SetAttributes(string value)
	{
		this.Attributes = value;
	}

	public void SetClientSideOptional(int value)
	{
		this.ClientSideOptional = value;
	}

	internal void InitializeValues()
	{
	}

	public string Code;

	public string Attributes;

	public int ClientSideOptional;

	public const int CodeFieldID = 1;

	public const int AttributesFieldID = 2;

	public const int ClientSideOptionalFieldID = 3;

	public int size;
}
