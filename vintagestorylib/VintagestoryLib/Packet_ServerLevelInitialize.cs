using System;

public class Packet_ServerLevelInitialize
{
	public void SetServerChunkSize(int value)
	{
		this.ServerChunkSize = value;
	}

	public void SetServerMapChunkSize(int value)
	{
		this.ServerMapChunkSize = value;
	}

	public void SetServerMapRegionSize(int value)
	{
		this.ServerMapRegionSize = value;
	}

	public void SetMaxViewDistance(int value)
	{
		this.MaxViewDistance = value;
	}

	internal void InitializeValues()
	{
	}

	public int ServerChunkSize;

	public int ServerMapChunkSize;

	public int ServerMapRegionSize;

	public int MaxViewDistance;

	public const int ServerChunkSizeFieldID = 1;

	public const int ServerMapChunkSizeFieldID = 2;

	public const int ServerMapRegionSizeFieldID = 3;

	public const int MaxViewDistanceFieldID = 4;

	public int size;
}
