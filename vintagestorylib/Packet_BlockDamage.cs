using System;

public class Packet_BlockDamage
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

	public void SetFacing(int value)
	{
		this.Facing = value;
	}

	public void SetDamage(int value)
	{
		this.Damage = value;
	}

	internal void InitializeValues()
	{
	}

	public int PosX;

	public int PosY;

	public int PosZ;

	public int Facing;

	public int Damage;

	public const int PosXFieldID = 1;

	public const int PosYFieldID = 2;

	public const int PosZFieldID = 3;

	public const int FacingFieldID = 4;

	public const int DamageFieldID = 5;

	public int size;
}
