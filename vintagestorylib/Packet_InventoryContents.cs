using System;

public class Packet_InventoryContents
{
	public void SetClientId(int value)
	{
		this.ClientId = value;
	}

	public void SetInventoryClass(string value)
	{
		this.InventoryClass = value;
	}

	public void SetInventoryId(string value)
	{
		this.InventoryId = value;
	}

	public Packet_ItemStack[] GetItemstacks()
	{
		return this.Itemstacks;
	}

	public void SetItemstacks(Packet_ItemStack[] value, int count, int length)
	{
		this.Itemstacks = value;
		this.ItemstacksCount = count;
		this.ItemstacksLength = length;
	}

	public void SetItemstacks(Packet_ItemStack[] value)
	{
		this.Itemstacks = value;
		this.ItemstacksCount = value.Length;
		this.ItemstacksLength = value.Length;
	}

	public int GetItemstacksCount()
	{
		return this.ItemstacksCount;
	}

	public void ItemstacksAdd(Packet_ItemStack value)
	{
		if (this.ItemstacksCount >= this.ItemstacksLength)
		{
			if ((this.ItemstacksLength *= 2) == 0)
			{
				this.ItemstacksLength = 1;
			}
			Packet_ItemStack[] newArray = new Packet_ItemStack[this.ItemstacksLength];
			for (int i = 0; i < this.ItemstacksCount; i++)
			{
				newArray[i] = this.Itemstacks[i];
			}
			this.Itemstacks = newArray;
		}
		Packet_ItemStack[] itemstacks = this.Itemstacks;
		int itemstacksCount = this.ItemstacksCount;
		this.ItemstacksCount = itemstacksCount + 1;
		itemstacks[itemstacksCount] = value;
	}

	internal void InitializeValues()
	{
	}

	public int ClientId;

	public string InventoryClass;

	public string InventoryId;

	public Packet_ItemStack[] Itemstacks;

	public int ItemstacksCount;

	public int ItemstacksLength;

	public const int ClientIdFieldID = 1;

	public const int InventoryClassFieldID = 2;

	public const int InventoryIdFieldID = 3;

	public const int ItemstacksFieldID = 4;

	public int size;
}
