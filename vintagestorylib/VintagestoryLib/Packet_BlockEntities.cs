using System;

public class Packet_BlockEntities
{
	public Packet_BlockEntity[] GetBlockEntitites()
	{
		return this.BlockEntitites;
	}

	public void SetBlockEntitites(Packet_BlockEntity[] value, int count, int length)
	{
		this.BlockEntitites = value;
		this.BlockEntititesCount = count;
		this.BlockEntititesLength = length;
	}

	public void SetBlockEntitites(Packet_BlockEntity[] value)
	{
		this.BlockEntitites = value;
		this.BlockEntititesCount = value.Length;
		this.BlockEntititesLength = value.Length;
	}

	public int GetBlockEntititesCount()
	{
		return this.BlockEntititesCount;
	}

	public void BlockEntititesAdd(Packet_BlockEntity value)
	{
		if (this.BlockEntititesCount >= this.BlockEntititesLength)
		{
			if ((this.BlockEntititesLength *= 2) == 0)
			{
				this.BlockEntititesLength = 1;
			}
			Packet_BlockEntity[] newArray = new Packet_BlockEntity[this.BlockEntititesLength];
			for (int i = 0; i < this.BlockEntititesCount; i++)
			{
				newArray[i] = this.BlockEntitites[i];
			}
			this.BlockEntitites = newArray;
		}
		Packet_BlockEntity[] blockEntitites = this.BlockEntitites;
		int blockEntititesCount = this.BlockEntititesCount;
		this.BlockEntititesCount = blockEntititesCount + 1;
		blockEntitites[blockEntititesCount] = value;
	}

	internal void InitializeValues()
	{
	}

	public Packet_BlockEntity[] BlockEntitites;

	public int BlockEntititesCount;

	public int BlockEntititesLength;

	public const int BlockEntititesFieldID = 1;

	public int size;
}
