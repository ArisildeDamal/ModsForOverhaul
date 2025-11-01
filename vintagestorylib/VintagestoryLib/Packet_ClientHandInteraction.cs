using System;

public class Packet_ClientHandInteraction
{
	public void SetUseType(int value)
	{
		this.UseType = value;
	}

	public void SetMouseButton(int value)
	{
		this.MouseButton = value;
	}

	public void SetInventoryId(string value)
	{
		this.InventoryId = value;
	}

	public void SetSlotId(int value)
	{
		this.SlotId = value;
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

	public void SetOnEntityId(long value)
	{
		this.OnEntityId = value;
	}

	public void SetEnumHandInteract(int value)
	{
		this.EnumHandInteract = value;
	}

	public void SetUsingCount(int value)
	{
		this.UsingCount = value;
	}

	public void SetSelectionBoxIndex(int value)
	{
		this.SelectionBoxIndex = value;
	}

	public void SetCancelReason(int value)
	{
		this.CancelReason = value;
	}

	public void SetFirstEvent(int value)
	{
		this.FirstEvent = value;
	}

	internal void InitializeValues()
	{
	}

	public int UseType;

	public int MouseButton;

	public string InventoryId;

	public int SlotId;

	public int X;

	public int Y;

	public int Z;

	public int OnBlockFace;

	public long HitX;

	public long HitY;

	public long HitZ;

	public long OnEntityId;

	public int EnumHandInteract;

	public int UsingCount;

	public int SelectionBoxIndex;

	public int CancelReason;

	public int FirstEvent;

	public const int UseTypeFieldID = 15;

	public const int MouseButtonFieldID = 1;

	public const int InventoryIdFieldID = 2;

	public const int SlotIdFieldID = 3;

	public const int XFieldID = 4;

	public const int YFieldID = 5;

	public const int ZFieldID = 6;

	public const int OnBlockFaceFieldID = 7;

	public const int HitXFieldID = 8;

	public const int HitYFieldID = 9;

	public const int HitZFieldID = 10;

	public const int OnEntityIdFieldID = 14;

	public const int EnumHandInteractFieldID = 11;

	public const int UsingCountFieldID = 12;

	public const int SelectionBoxIndexFieldID = 13;

	public const int CancelReasonFieldID = 16;

	public const int FirstEventFieldID = 17;

	public int size;
}
