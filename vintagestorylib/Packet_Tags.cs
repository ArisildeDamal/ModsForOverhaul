using System;

public class Packet_Tags
{
	public string[] GetEntityTags()
	{
		return this.EntityTags;
	}

	public void SetEntityTags(string[] value, int count, int length)
	{
		this.EntityTags = value;
		this.EntityTagsCount = count;
		this.EntityTagsLength = length;
	}

	public void SetEntityTags(string[] value)
	{
		this.EntityTags = value;
		this.EntityTagsCount = value.Length;
		this.EntityTagsLength = value.Length;
	}

	public int GetEntityTagsCount()
	{
		return this.EntityTagsCount;
	}

	public void EntityTagsAdd(string value)
	{
		if (this.EntityTagsCount >= this.EntityTagsLength)
		{
			if ((this.EntityTagsLength *= 2) == 0)
			{
				this.EntityTagsLength = 1;
			}
			string[] newArray = new string[this.EntityTagsLength];
			for (int i = 0; i < this.EntityTagsCount; i++)
			{
				newArray[i] = this.EntityTags[i];
			}
			this.EntityTags = newArray;
		}
		string[] entityTags = this.EntityTags;
		int entityTagsCount = this.EntityTagsCount;
		this.EntityTagsCount = entityTagsCount + 1;
		entityTags[entityTagsCount] = value;
	}

	public string[] GetBlockTags()
	{
		return this.BlockTags;
	}

	public void SetBlockTags(string[] value, int count, int length)
	{
		this.BlockTags = value;
		this.BlockTagsCount = count;
		this.BlockTagsLength = length;
	}

	public void SetBlockTags(string[] value)
	{
		this.BlockTags = value;
		this.BlockTagsCount = value.Length;
		this.BlockTagsLength = value.Length;
	}

	public int GetBlockTagsCount()
	{
		return this.BlockTagsCount;
	}

	public void BlockTagsAdd(string value)
	{
		if (this.BlockTagsCount >= this.BlockTagsLength)
		{
			if ((this.BlockTagsLength *= 2) == 0)
			{
				this.BlockTagsLength = 1;
			}
			string[] newArray = new string[this.BlockTagsLength];
			for (int i = 0; i < this.BlockTagsCount; i++)
			{
				newArray[i] = this.BlockTags[i];
			}
			this.BlockTags = newArray;
		}
		string[] blockTags = this.BlockTags;
		int blockTagsCount = this.BlockTagsCount;
		this.BlockTagsCount = blockTagsCount + 1;
		blockTags[blockTagsCount] = value;
	}

	public string[] GetItemTags()
	{
		return this.ItemTags;
	}

	public void SetItemTags(string[] value, int count, int length)
	{
		this.ItemTags = value;
		this.ItemTagsCount = count;
		this.ItemTagsLength = length;
	}

	public void SetItemTags(string[] value)
	{
		this.ItemTags = value;
		this.ItemTagsCount = value.Length;
		this.ItemTagsLength = value.Length;
	}

	public int GetItemTagsCount()
	{
		return this.ItemTagsCount;
	}

	public void ItemTagsAdd(string value)
	{
		if (this.ItemTagsCount >= this.ItemTagsLength)
		{
			if ((this.ItemTagsLength *= 2) == 0)
			{
				this.ItemTagsLength = 1;
			}
			string[] newArray = new string[this.ItemTagsLength];
			for (int i = 0; i < this.ItemTagsCount; i++)
			{
				newArray[i] = this.ItemTags[i];
			}
			this.ItemTags = newArray;
		}
		string[] itemTags = this.ItemTags;
		int itemTagsCount = this.ItemTagsCount;
		this.ItemTagsCount = itemTagsCount + 1;
		itemTags[itemTagsCount] = value;
	}

	internal void InitializeValues()
	{
	}

	public string[] EntityTags;

	public int EntityTagsCount;

	public int EntityTagsLength;

	public string[] BlockTags;

	public int BlockTagsCount;

	public int BlockTagsLength;

	public string[] ItemTags;

	public int ItemTagsCount;

	public int ItemTagsLength;

	public const int EntityTagsFieldID = 1;

	public const int BlockTagsFieldID = 2;

	public const int ItemTagsFieldID = 3;

	public int size;
}
