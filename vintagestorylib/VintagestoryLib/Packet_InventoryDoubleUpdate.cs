using System;

public class Packet_InventoryDoubleUpdate
{
	public void SetClientId(int value)
	{
		this.ClientId = value;
	}

	public void SetInventoryId1(string value)
	{
		this.InventoryId1 = value;
	}

	public void SetInventoryId2(string value)
	{
		this.InventoryId2 = value;
	}

	public void SetSlotId1(int value)
	{
		this.SlotId1 = value;
	}

	public void SetSlotId2(int value)
	{
		this.SlotId2 = value;
	}

	public void SetItemStack1(Packet_ItemStack value)
	{
		this.ItemStack1 = value;
	}

	public void SetItemStack2(Packet_ItemStack value)
	{
		this.ItemStack2 = value;
	}

	internal void InitializeValues()
	{
	}

	public int ClientId;

	public string InventoryId1;

	public string InventoryId2;

	public int SlotId1;

	public int SlotId2;

	public Packet_ItemStack ItemStack1;

	public Packet_ItemStack ItemStack2;

	public const int ClientIdFieldID = 1;

	public const int InventoryId1FieldID = 2;

	public const int InventoryId2FieldID = 3;

	public const int SlotId1FieldID = 4;

	public const int SlotId2FieldID = 5;

	public const int ItemStack1FieldID = 6;

	public const int ItemStack2FieldID = 7;

	public int size;
}
