using System;

public class Packet_SelectedHotbarSlot
{
	public void SetSlotNumber(int value)
	{
		this.SlotNumber = value;
	}

	public void SetClientId(int value)
	{
		this.ClientId = value;
	}

	public void SetItemstack(Packet_ItemStack value)
	{
		this.Itemstack = value;
	}

	public void SetOffhandStack(Packet_ItemStack value)
	{
		this.OffhandStack = value;
	}

	internal void InitializeValues()
	{
	}

	public int SlotNumber;

	public int ClientId;

	public Packet_ItemStack Itemstack;

	public Packet_ItemStack OffhandStack;

	public const int SlotNumberFieldID = 1;

	public const int ClientIdFieldID = 2;

	public const int ItemstackFieldID = 3;

	public const int OffhandStackFieldID = 4;

	public int size;
}
