using System;

public class Packet_GotoGroup
{
	public void SetGroupId(int value)
	{
		this.GroupId = value;
	}

	internal void InitializeValues()
	{
	}

	public int GroupId;

	public const int GroupIdFieldID = 1;

	public int size;
}
