using System;

public class Packet_BulkEntityDebugAttributes
{
	public Packet_EntityAttributes[] GetFullUpdates()
	{
		return this.FullUpdates;
	}

	public void SetFullUpdates(Packet_EntityAttributes[] value, int count, int length)
	{
		this.FullUpdates = value;
		this.FullUpdatesCount = count;
		this.FullUpdatesLength = length;
	}

	public void SetFullUpdates(Packet_EntityAttributes[] value)
	{
		this.FullUpdates = value;
		this.FullUpdatesCount = value.Length;
		this.FullUpdatesLength = value.Length;
	}

	public int GetFullUpdatesCount()
	{
		return this.FullUpdatesCount;
	}

	public void FullUpdatesAdd(Packet_EntityAttributes value)
	{
		if (this.FullUpdatesCount >= this.FullUpdatesLength)
		{
			if ((this.FullUpdatesLength *= 2) == 0)
			{
				this.FullUpdatesLength = 1;
			}
			Packet_EntityAttributes[] newArray = new Packet_EntityAttributes[this.FullUpdatesLength];
			for (int i = 0; i < this.FullUpdatesCount; i++)
			{
				newArray[i] = this.FullUpdates[i];
			}
			this.FullUpdates = newArray;
		}
		Packet_EntityAttributes[] fullUpdates = this.FullUpdates;
		int fullUpdatesCount = this.FullUpdatesCount;
		this.FullUpdatesCount = fullUpdatesCount + 1;
		fullUpdates[fullUpdatesCount] = value;
	}

	internal void InitializeValues()
	{
	}

	public Packet_EntityAttributes[] FullUpdates;

	public int FullUpdatesCount;

	public int FullUpdatesLength;

	public const int FullUpdatesFieldID = 1;

	public int size;
}
