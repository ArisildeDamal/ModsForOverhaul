using System;

public class Packet_ServerPlayerPing
{
	public int[] GetClientIds()
	{
		return this.ClientIds;
	}

	public void SetClientIds(int[] value, int count, int length)
	{
		this.ClientIds = value;
		this.ClientIdsCount = count;
		this.ClientIdsLength = length;
	}

	public void SetClientIds(int[] value)
	{
		this.ClientIds = value;
		this.ClientIdsCount = value.Length;
		this.ClientIdsLength = value.Length;
	}

	public int GetClientIdsCount()
	{
		return this.ClientIdsCount;
	}

	public void ClientIdsAdd(int value)
	{
		if (this.ClientIdsCount >= this.ClientIdsLength)
		{
			if ((this.ClientIdsLength *= 2) == 0)
			{
				this.ClientIdsLength = 1;
			}
			int[] newArray = new int[this.ClientIdsLength];
			for (int i = 0; i < this.ClientIdsCount; i++)
			{
				newArray[i] = this.ClientIds[i];
			}
			this.ClientIds = newArray;
		}
		int[] clientIds = this.ClientIds;
		int clientIdsCount = this.ClientIdsCount;
		this.ClientIdsCount = clientIdsCount + 1;
		clientIds[clientIdsCount] = value;
	}

	public int[] GetPings()
	{
		return this.Pings;
	}

	public void SetPings(int[] value, int count, int length)
	{
		this.Pings = value;
		this.PingsCount = count;
		this.PingsLength = length;
	}

	public void SetPings(int[] value)
	{
		this.Pings = value;
		this.PingsCount = value.Length;
		this.PingsLength = value.Length;
	}

	public int GetPingsCount()
	{
		return this.PingsCount;
	}

	public void PingsAdd(int value)
	{
		if (this.PingsCount >= this.PingsLength)
		{
			if ((this.PingsLength *= 2) == 0)
			{
				this.PingsLength = 1;
			}
			int[] newArray = new int[this.PingsLength];
			for (int i = 0; i < this.PingsCount; i++)
			{
				newArray[i] = this.Pings[i];
			}
			this.Pings = newArray;
		}
		int[] pings = this.Pings;
		int pingsCount = this.PingsCount;
		this.PingsCount = pingsCount + 1;
		pings[pingsCount] = value;
	}

	internal void InitializeValues()
	{
	}

	public int[] ClientIds;

	public int ClientIdsCount;

	public int ClientIdsLength;

	public int[] Pings;

	public int PingsCount;

	public int PingsLength;

	public const int ClientIdsFieldID = 1;

	public const int PingsFieldID = 2;

	public int size;
}
