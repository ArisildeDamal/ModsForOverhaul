using System;

public class Packet_InventoryUpdate
{
	public void SetClientId(int value)
	{
		this.ClientId = value;
	}

	public void SetInventoryId(string value)
	{
		this.InventoryId = value;
	}

	public void SetSlotId(int value)
	{
		this.SlotId = value;
	}

	public void SetItemStack(Packet_ItemStack value)
	{
		this.ItemStack = value;
	}

	internal void InitializeValues()
	{
	}

	public int ClientId;

	public string InventoryId;

	public int SlotId;

	public Packet_ItemStack ItemStack;

	public const int ClientIdFieldID = 1;

	public const int InventoryIdFieldID = 2;

	public const int SlotIdFieldID = 3;

	public const int ItemStackFieldID = 4;

	public int size;
}
