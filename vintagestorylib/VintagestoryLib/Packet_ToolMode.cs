using System;

public class Packet_ToolMode
{
	public void SetMode(int value)
	{
		this.Mode = value;
	}

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

	public void SetFace(int value)
	{
		this.Face = value;
	}

	public void SetSelectionBoxIndex(int value)
	{
		this.SelectionBoxIndex = value;
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

	internal void InitializeValues()
	{
	}

	public int Mode;

	public int X;

	public int Y;

	public int Z;

	public int Face;

	public int SelectionBoxIndex;

	public long HitX;

	public long HitY;

	public long HitZ;

	public const int ModeFieldID = 1;

	public const int XFieldID = 2;

	public const int YFieldID = 3;

	public const int ZFieldID = 4;

	public const int FaceFieldID = 5;

	public const int SelectionBoxIndexFieldID = 6;

	public const int HitXFieldID = 7;

	public const int HitYFieldID = 8;

	public const int HitZFieldID = 9;

	public int size;
}
