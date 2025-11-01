using System;

public class Packet_EntityInteraction
{
	public void SetMouseButton(int value)
	{
		this.MouseButton = value;
	}

	public void SetEntityId(long value)
	{
		this.EntityId = value;
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

	internal void InitializeValues()
	{
	}

	public int MouseButton;

	public long EntityId;

	public int OnBlockFace;

	public long HitX;

	public long HitY;

	public long HitZ;

	public int SelectionBoxIndex;

	public const int MouseButtonFieldID = 1;

	public const int EntityIdFieldID = 2;

	public const int OnBlockFaceFieldID = 3;

	public const int HitXFieldID = 4;

	public const int HitYFieldID = 5;

	public const int HitZFieldID = 6;

	public const int SelectionBoxIndexFieldID = 7;

	public int size;
}
