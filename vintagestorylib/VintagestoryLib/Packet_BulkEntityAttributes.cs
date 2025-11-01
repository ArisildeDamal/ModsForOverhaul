using System;

public class Packet_BulkEntityAttributes
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

	public Packet_EntityAttributeUpdate[] GetPartialUpdates()
	{
		return this.PartialUpdates;
	}

	public void SetPartialUpdates(Packet_EntityAttributeUpdate[] value, int count, int length)
	{
		this.PartialUpdates = value;
		this.PartialUpdatesCount = count;
		this.PartialUpdatesLength = length;
	}

	public void SetPartialUpdates(Packet_EntityAttributeUpdate[] value)
	{
		this.PartialUpdates = value;
		this.PartialUpdatesCount = value.Length;
		this.PartialUpdatesLength = value.Length;
	}

	public int GetPartialUpdatesCount()
	{
		return this.PartialUpdatesCount;
	}

	public void PartialUpdatesAdd(Packet_EntityAttributeUpdate value)
	{
		if (this.PartialUpdatesCount >= this.PartialUpdatesLength)
		{
			if ((this.PartialUpdatesLength *= 2) == 0)
			{
				this.PartialUpdatesLength = 1;
			}
			Packet_EntityAttributeUpdate[] newArray = new Packet_EntityAttributeUpdate[this.PartialUpdatesLength];
			for (int i = 0; i < this.PartialUpdatesCount; i++)
			{
				newArray[i] = this.PartialUpdates[i];
			}
			this.PartialUpdates = newArray;
		}
		Packet_EntityAttributeUpdate[] partialUpdates = this.PartialUpdates;
		int partialUpdatesCount = this.PartialUpdatesCount;
		this.PartialUpdatesCount = partialUpdatesCount + 1;
		partialUpdates[partialUpdatesCount] = value;
	}

	internal void InitializeValues()
	{
	}

	public Packet_EntityAttributes[] FullUpdates;

	public int FullUpdatesCount;

	public int FullUpdatesLength;

	public Packet_EntityAttributeUpdate[] PartialUpdates;

	public int PartialUpdatesCount;

	public int PartialUpdatesLength;

	public const int FullUpdatesFieldID = 1;

	public const int PartialUpdatesFieldID = 2;

	public int size;
}
