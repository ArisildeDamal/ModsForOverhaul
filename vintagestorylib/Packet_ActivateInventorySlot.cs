using System;

public class Packet_ActivateInventorySlot
{
	public void SetMouseButton(int value)
	{
		this.MouseButton = value;
	}

	public void SetModifiers(int value)
	{
		this.Modifiers = value;
	}

	public void SetTargetInventoryId(string value)
	{
		this.TargetInventoryId = value;
	}

	public void SetTargetSlot(int value)
	{
		this.TargetSlot = value;
	}

	public void SetTargetLastChanged(long value)
	{
		this.TargetLastChanged = value;
	}

	public void SetTabIndex(int value)
	{
		this.TabIndex = value;
	}

	public void SetPriority(int value)
	{
		this.Priority = value;
	}

	public void SetDir(int value)
	{
		this.Dir = value;
	}

	internal void InitializeValues()
	{
	}

	public int MouseButton;

	public int Modifiers;

	public string TargetInventoryId;

	public int TargetSlot;

	public long TargetLastChanged;

	public int TabIndex;

	public int Priority;

	public int Dir;

	public const int MouseButtonFieldID = 1;

	public const int ModifiersFieldID = 4;

	public const int TargetInventoryIdFieldID = 2;

	public const int TargetSlotFieldID = 3;

	public const int TargetLastChangedFieldID = 5;

	public const int TabIndexFieldID = 6;

	public const int PriorityFieldID = 7;

	public const int DirFieldID = 8;

	public int size;
}
