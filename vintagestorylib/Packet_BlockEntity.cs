using System;

public class Packet_BlockEntity
{
	public void SetClassname(string value)
	{
		this.Classname = value;
	}

	public void SetPosX(int value)
	{
		this.PosX = value;
	}

	public void SetPosY(int value)
	{
		this.PosY = value;
	}

	public void SetPosZ(int value)
	{
		this.PosZ = value;
	}

	public void SetData(byte[] value)
	{
		this.Data = value;
	}

	internal void InitializeValues()
	{
	}

	public string Classname;

	public int PosX;

	public int PosY;

	public int PosZ;

	public byte[] Data;

	public const int ClassnameFieldID = 1;

	public const int PosXFieldID = 2;

	public const int PosYFieldID = 3;

	public const int PosZFieldID = 4;

	public const int DataFieldID = 5;

	public int size;
}
