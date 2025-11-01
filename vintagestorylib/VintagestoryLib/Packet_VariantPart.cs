using System;

public class Packet_VariantPart
{
	public void SetCode(string value)
	{
		this.Code = value;
	}

	public void SetValue(string value)
	{
		this.Value = value;
	}

	internal void InitializeValues()
	{
	}

	public string Code;

	public string Value;

	public const int CodeFieldID = 1;

	public const int ValueFieldID = 2;

	public int size;
}
