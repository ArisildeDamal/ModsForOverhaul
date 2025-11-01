using System;

public class Packet_ServerChunks
{
	public Packet_ServerChunk[] GetChunks()
	{
		return this.Chunks;
	}

	public void SetChunks(Packet_ServerChunk[] value, int count, int length)
	{
		this.Chunks = value;
		this.ChunksCount = count;
		this.ChunksLength = length;
	}

	public void SetChunks(Packet_ServerChunk[] value)
	{
		this.Chunks = value;
		this.ChunksCount = value.Length;
		this.ChunksLength = value.Length;
	}

	public int GetChunksCount()
	{
		return this.ChunksCount;
	}

	public void ChunksAdd(Packet_ServerChunk value)
	{
		if (this.ChunksCount >= this.ChunksLength)
		{
			if ((this.ChunksLength *= 2) == 0)
			{
				this.ChunksLength = 1;
			}
			Packet_ServerChunk[] newArray = new Packet_ServerChunk[this.ChunksLength];
			for (int i = 0; i < this.ChunksCount; i++)
			{
				newArray[i] = this.Chunks[i];
			}
			this.Chunks = newArray;
		}
		Packet_ServerChunk[] chunks = this.Chunks;
		int chunksCount = this.ChunksCount;
		this.ChunksCount = chunksCount + 1;
		chunks[chunksCount] = value;
	}

	internal void InitializeValues()
	{
	}

	public Packet_ServerChunk[] Chunks;

	public int ChunksCount;

	public int ChunksLength;

	public const int ChunksFieldID = 1;

	public int size;
}
