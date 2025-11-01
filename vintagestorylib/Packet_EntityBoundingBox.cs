using System;

public class Packet_EntityBoundingBox
{
	public void SetSizeX(int value)
	{
		this.SizeX = value;
	}

	public void SetSizeY(int value)
	{
		this.SizeY = value;
	}

	public void SetSizeZ(int value)
	{
		this.SizeZ = value;
	}

	internal void InitializeValues()
	{
	}

	public int SizeX;

	public int SizeY;

	public int SizeZ;

	public const int SizeXFieldID = 1;

	public const int SizeYFieldID = 2;

	public const int SizeZFieldID = 3;

	public int size;
}
