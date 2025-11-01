using System;

public class Packet_ClientRequestJoin
{
	public void SetLanguage(string value)
	{
		this.Language = value;
	}

	internal void InitializeValues()
	{
	}

	public string Language;

	public const int LanguageFieldID = 1;

	public int size;
}
