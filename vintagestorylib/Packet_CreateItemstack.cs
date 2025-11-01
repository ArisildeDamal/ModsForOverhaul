using System;

public class Packet_CreateItemstack
{
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

	public void SetItemstack(Packet_ItemStack value)
	{
		this.Itemstack = value;
	}

	internal void InitializeValues()
	{
	}

	public string TargetInventoryId;

	public int TargetSlot;

	public long TargetLastChanged;

	public Packet_ItemStack Itemstack;

	public const int TargetInventoryIdFieldID = 1;

	public const int TargetSlotFieldID = 2;

	public const int TargetLastChangedFieldID = 3;

	public const int ItemstackFieldID = 4;

	public int size;
}
