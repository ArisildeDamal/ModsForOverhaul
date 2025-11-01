using System;

public class Packet_RemoveBlockLight
{
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

	public void SetLightH(int value)
	{
		this.LightH = value;
	}

	public void SetLightS(int value)
	{
		this.LightS = value;
	}

	public void SetLightV(int value)
	{
		this.LightV = value;
	}

	internal void InitializeValues()
	{
	}

	public int PosX;

	public int PosY;

	public int PosZ;

	public int LightH;

	public int LightS;

	public int LightV;

	public const int PosXFieldID = 1;

	public const int PosYFieldID = 2;

	public const int PosZFieldID = 3;

	public const int LightHFieldID = 4;

	public const int LightSFieldID = 5;

	public const int LightVFieldID = 6;

	public int size;
}
