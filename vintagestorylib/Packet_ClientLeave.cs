using System;

public class Packet_ClientLeave
{
	public void SetReason(int value)
	{
		this.Reason = value;
	}

	internal void InitializeValues()
	{
		this.Reason = 0;
	}

	public int Reason;

	public const int ReasonFieldID = 1;

	public int size;
}
