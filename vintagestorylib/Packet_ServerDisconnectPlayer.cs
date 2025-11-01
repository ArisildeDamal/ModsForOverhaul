using System;

public class Packet_ServerDisconnectPlayer
{
	public void SetDisconnectReason(string value)
	{
		this.DisconnectReason = value;
	}

	internal void InitializeValues()
	{
	}

	public string DisconnectReason;

	public const int DisconnectReasonFieldID = 1;

	public int size;
}
