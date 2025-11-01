using System;

public class Packet_UnloadMapRegion
{
	public void SetRegionX(int value)
	{
		this.RegionX = value;
	}

	public void SetRegionZ(int value)
	{
		this.RegionZ = value;
	}

	internal void InitializeValues()
	{
	}

	public int RegionX;

	public int RegionZ;

	public const int RegionXFieldID = 1;

	public const int RegionZFieldID = 2;

	public int size;
}
