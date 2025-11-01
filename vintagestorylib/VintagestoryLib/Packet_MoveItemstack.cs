using System;

public class Packet_MoveItemstack
{
	public void SetSourceInventoryId(string value)
	{
		this.SourceInventoryId = value;
	}

	public void SetTargetInventoryId(string value)
	{
		this.TargetInventoryId = value;
	}

	public void SetSourceSlot(int value)
	{
		this.SourceSlot = value;
	}

	public void SetTargetSlot(int value)
	{
		this.TargetSlot = value;
	}

	public void SetQuantity(int value)
	{
		this.Quantity = value;
	}

	public void SetSourceLastChanged(long value)
	{
		this.SourceLastChanged = value;
	}

	public void SetTargetLastChanged(long value)
	{
		this.TargetLastChanged = value;
	}

	public void SetMouseButton(int value)
	{
		this.MouseButton = value;
	}

	public void SetModifiers(int value)
	{
		this.Modifiers = value;
	}

	public void SetPriority(int value)
	{
		this.Priority = value;
	}

	public void SetTabIndex(int value)
	{
		this.TabIndex = value;
	}

	internal void InitializeValues()
	{
	}

	public string SourceInventoryId;

	public string TargetInventoryId;

	public int SourceSlot;

	public int TargetSlot;

	public int Quantity;

	public long SourceLastChanged;

	public long TargetLastChanged;

	public int MouseButton;

	public int Modifiers;

	public int Priority;

	public int TabIndex;

	public const int SourceInventoryIdFieldID = 1;

	public const int TargetInventoryIdFieldID = 2;

	public const int SourceSlotFieldID = 3;

	public const int TargetSlotFieldID = 4;

	public const int QuantityFieldID = 5;

	public const int SourceLastChangedFieldID = 6;

	public const int TargetLastChangedFieldID = 7;

	public const int MouseButtonFieldID = 8;

	public const int ModifiersFieldID = 9;

	public const int PriorityFieldID = 10;

	public const int TabIndexFieldID = 11;

	public int size;
}
