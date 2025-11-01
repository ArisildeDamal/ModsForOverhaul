using System;

public class Packet_StringList
{
	public string[] GetItems()
	{
		return this.Items;
	}

	public void SetItems(string[] value, int count, int length)
	{
		this.Items = value;
		this.ItemsCount = count;
		this.ItemsLength = length;
	}

	public void SetItems(string[] value)
	{
		this.Items = value;
		this.ItemsCount = value.Length;
		this.ItemsLength = value.Length;
	}

	public int GetItemsCount()
	{
		return this.ItemsCount;
	}

	public void ItemsAdd(string value)
	{
		if (this.ItemsCount >= this.ItemsLength)
		{
			if ((this.ItemsLength *= 2) == 0)
			{
				this.ItemsLength = 1;
			}
			string[] newArray = new string[this.ItemsLength];
			for (int i = 0; i < this.ItemsCount; i++)
			{
				newArray[i] = this.Items[i];
			}
			this.Items = newArray;
		}
		string[] items = this.Items;
		int itemsCount = this.ItemsCount;
		this.ItemsCount = itemsCount + 1;
		items[itemsCount] = value;
	}

	internal void InitializeValues()
	{
	}

	public string[] Items;

	public int ItemsCount;

	public int ItemsLength;

	public const int ItemsFieldID = 1;

	public int size;
}
