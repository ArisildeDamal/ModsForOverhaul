using System;

public class Packet_FlipItemstacks
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

	public void SetSourceLastChanged(long value)
	{
		this.SourceLastChanged = value;
	}

	public void SetTargetLastChanged(long value)
	{
		this.TargetLastChanged = value;
	}

	public void SetSourceTabIndex(int value)
	{
		this.SourceTabIndex = value;
	}

	public void SetTargetTabIndex(int value)
	{
		this.TargetTabIndex = value;
	}

	internal void InitializeValues()
	{
	}

	public string SourceInventoryId;

	public string TargetInventoryId;

	public int SourceSlot;

	public int TargetSlot;

	public long SourceLastChanged;

	public long TargetLastChanged;

	public int SourceTabIndex;

	public int TargetTabIndex;

	public const int SourceInventoryIdFieldID = 1;

	public const int TargetInventoryIdFieldID = 2;

	public const int SourceSlotFieldID = 3;

	public const int TargetSlotFieldID = 4;

	public const int SourceLastChangedFieldID = 5;

	public const int TargetLastChangedFieldID = 6;

	public const int SourceTabIndexFieldID = 7;

	public const int TargetTabIndexFieldID = 8;

	public int size;
}
