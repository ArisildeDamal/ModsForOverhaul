using System;

public class Packet_RuntimeSetting
{
	public void SetImmersiveFpMode(int value)
	{
		this.ImmersiveFpMode = value;
	}

	public void SetItemCollectMode(int value)
	{
		this.ItemCollectMode = value;
	}

	internal void InitializeValues()
	{
	}

	public int ImmersiveFpMode;

	public int ItemCollectMode;

	public const int ImmersiveFpModeFieldID = 1;

	public const int ItemCollectModeFieldID = 2;

	public int size;
}
