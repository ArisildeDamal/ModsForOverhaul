using System;

public class Packet_ServerLevelProgress
{
	public void SetPercentComplete(int value)
	{
		this.PercentComplete = value;
	}

	public void SetStatus(string value)
	{
		this.Status = value;
	}

	public void SetPercentCompleteSubitem(int value)
	{
		this.PercentCompleteSubitem = value;
	}

	internal void InitializeValues()
	{
	}

	public int PercentComplete;

	public string Status;

	public int PercentCompleteSubitem;

	public const int PercentCompleteFieldID = 2;

	public const int StatusFieldID = 3;

	public const int PercentCompleteSubitemFieldID = 4;

	public int size;
}
