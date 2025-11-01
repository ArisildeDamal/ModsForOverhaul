using System;

public class Packet_BlendedOverlayTexture
{
	public void SetBase(string value)
	{
		this.Base = value;
	}

	public void SetMode(int value)
	{
		this.Mode = value;
	}

	internal void InitializeValues()
	{
	}

	public string Base;

	public int Mode;

	public const int BaseFieldID = 1;

	public const int ModeFieldID = 2;

	public int size;
}
