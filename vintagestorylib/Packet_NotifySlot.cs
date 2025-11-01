using System;

public class Packet_NotifySlot
{
	public void SetInventoryId(string value)
	{
		this.InventoryId = value;
	}

	public void SetSlotId(int value)
	{
		this.SlotId = value;
	}

	internal void InitializeValues()
	{
	}

	public string InventoryId;

	public int SlotId;

	public const int InventoryIdFieldID = 1;

	public const int SlotIdFieldID = 2;

	public int size;
}
