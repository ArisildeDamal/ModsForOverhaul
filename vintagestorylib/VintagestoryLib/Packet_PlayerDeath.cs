using System;

public class Packet_PlayerDeath
{
	public void SetClientId(int value)
	{
		this.ClientId = value;
	}

	public void SetLivesLeft(int value)
	{
		this.LivesLeft = value;
	}

	internal void InitializeValues()
	{
	}

	public int ClientId;

	public int LivesLeft;

	public const int ClientIdFieldID = 1;

	public const int LivesLeftFieldID = 2;

	public int size;
}
