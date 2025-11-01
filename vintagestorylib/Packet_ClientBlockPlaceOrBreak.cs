using System;

public class Packet_ClientBlockPlaceOrBreak
{
	public void SetX(int value)
	{
		this.X = value;
	}

	public void SetY(int value)
	{
		this.Y = value;
	}

	public void SetZ(int value)
	{
		this.Z = value;
	}

	public void SetMode(int value)
	{
		this.Mode = value;
	}

	public void SetBlockType(int value)
	{
		this.BlockType = value;
	}

	public void SetOnBlockFace(int value)
	{
		this.OnBlockFace = value;
	}

	public void SetHitX(long value)
	{
		this.HitX = value;
	}

	public void SetHitY(long value)
	{
		this.HitY = value;
	}

	public void SetHitZ(long value)
	{
		this.HitZ = value;
	}

	public void SetSelectionBoxIndex(int value)
	{
		this.SelectionBoxIndex = value;
	}

	public void SetDidOffset(int value)
	{
		this.DidOffset = value;
	}

	internal void InitializeValues()
	{
		this.Mode = 0;
	}

	public int X;

	public int Y;

	public int Z;

	public int Mode;

	public int BlockType;

	public int OnBlockFace;

	public long HitX;

	public long HitY;

	public long HitZ;

	public int SelectionBoxIndex;

	public int DidOffset;

	public const int XFieldID = 1;

	public const int YFieldID = 2;

	public const int ZFieldID = 3;

	public const int ModeFieldID = 4;

	public const int BlockTypeFieldID = 5;

	public const int OnBlockFaceFieldID = 7;

	public const int HitXFieldID = 8;

	public const int HitYFieldID = 9;

	public const int HitZFieldID = 10;

	public const int SelectionBoxIndexFieldID = 11;

	public const int DidOffsetFieldID = 12;

	public int size;
}
