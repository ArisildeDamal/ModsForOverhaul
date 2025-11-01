using System;

public class Packet_ServerAssets
{
	public Packet_BlockType[] GetBlocks()
	{
		return this.Blocks;
	}

	public void SetBlocks(Packet_BlockType[] value, int count, int length)
	{
		this.Blocks = value;
		this.BlocksCount = count;
		this.BlocksLength = length;
	}

	public void SetBlocks(Packet_BlockType[] value)
	{
		this.Blocks = value;
		this.BlocksCount = value.Length;
		this.BlocksLength = value.Length;
	}

	public int GetBlocksCount()
	{
		return this.BlocksCount;
	}

	public void BlocksAdd(Packet_BlockType value)
	{
		if (this.BlocksCount >= this.BlocksLength)
		{
			if ((this.BlocksLength *= 2) == 0)
			{
				this.BlocksLength = 1;
			}
			Packet_BlockType[] newArray = new Packet_BlockType[this.BlocksLength];
			for (int i = 0; i < this.BlocksCount; i++)
			{
				newArray[i] = this.Blocks[i];
			}
			this.Blocks = newArray;
		}
		Packet_BlockType[] blocks = this.Blocks;
		int blocksCount = this.BlocksCount;
		this.BlocksCount = blocksCount + 1;
		blocks[blocksCount] = value;
	}

	public Packet_ItemType[] GetItems()
	{
		return this.Items;
	}

	public void SetItems(Packet_ItemType[] value, int count, int length)
	{
		this.Items = value;
		this.ItemsCount = count;
		this.ItemsLength = length;
	}

	public void SetItems(Packet_ItemType[] value)
	{
		this.Items = value;
		this.ItemsCount = value.Length;
		this.ItemsLength = value.Length;
	}

	public int GetItemsCount()
	{
		return this.ItemsCount;
	}

	public void ItemsAdd(Packet_ItemType value)
	{
		if (this.ItemsCount >= this.ItemsLength)
		{
			if ((this.ItemsLength *= 2) == 0)
			{
				this.ItemsLength = 1;
			}
			Packet_ItemType[] newArray = new Packet_ItemType[this.ItemsLength];
			for (int i = 0; i < this.ItemsCount; i++)
			{
				newArray[i] = this.Items[i];
			}
			this.Items = newArray;
		}
		Packet_ItemType[] items = this.Items;
		int itemsCount = this.ItemsCount;
		this.ItemsCount = itemsCount + 1;
		items[itemsCount] = value;
	}

	public Packet_EntityType[] GetEntities()
	{
		return this.Entities;
	}

	public void SetEntities(Packet_EntityType[] value, int count, int length)
	{
		this.Entities = value;
		this.EntitiesCount = count;
		this.EntitiesLength = length;
	}

	public void SetEntities(Packet_EntityType[] value)
	{
		this.Entities = value;
		this.EntitiesCount = value.Length;
		this.EntitiesLength = value.Length;
	}

	public int GetEntitiesCount()
	{
		return this.EntitiesCount;
	}

	public void EntitiesAdd(Packet_EntityType value)
	{
		if (this.EntitiesCount >= this.EntitiesLength)
		{
			if ((this.EntitiesLength *= 2) == 0)
			{
				this.EntitiesLength = 1;
			}
			Packet_EntityType[] newArray = new Packet_EntityType[this.EntitiesLength];
			for (int i = 0; i < this.EntitiesCount; i++)
			{
				newArray[i] = this.Entities[i];
			}
			this.Entities = newArray;
		}
		Packet_EntityType[] entities = this.Entities;
		int entitiesCount = this.EntitiesCount;
		this.EntitiesCount = entitiesCount + 1;
		entities[entitiesCount] = value;
	}

	public Packet_Recipes[] GetRecipes()
	{
		return this.Recipes;
	}

	public void SetRecipes(Packet_Recipes[] value, int count, int length)
	{
		this.Recipes = value;
		this.RecipesCount = count;
		this.RecipesLength = length;
	}

	public void SetRecipes(Packet_Recipes[] value)
	{
		this.Recipes = value;
		this.RecipesCount = value.Length;
		this.RecipesLength = value.Length;
	}

	public int GetRecipesCount()
	{
		return this.RecipesCount;
	}

	public void RecipesAdd(Packet_Recipes value)
	{
		if (this.RecipesCount >= this.RecipesLength)
		{
			if ((this.RecipesLength *= 2) == 0)
			{
				this.RecipesLength = 1;
			}
			Packet_Recipes[] newArray = new Packet_Recipes[this.RecipesLength];
			for (int i = 0; i < this.RecipesCount; i++)
			{
				newArray[i] = this.Recipes[i];
			}
			this.Recipes = newArray;
		}
		Packet_Recipes[] recipes = this.Recipes;
		int recipesCount = this.RecipesCount;
		this.RecipesCount = recipesCount + 1;
		recipes[recipesCount] = value;
	}

	public void SetTags(Packet_Tags value)
	{
		this.Tags = value;
	}

	internal void InitializeValues()
	{
	}

	public Packet_BlockType[] Blocks;

	public int BlocksCount;

	public int BlocksLength;

	public Packet_ItemType[] Items;

	public int ItemsCount;

	public int ItemsLength;

	public Packet_EntityType[] Entities;

	public int EntitiesCount;

	public int EntitiesLength;

	public Packet_Recipes[] Recipes;

	public int RecipesCount;

	public int RecipesLength;

	public Packet_Tags Tags;

	public const int BlocksFieldID = 1;

	public const int ItemsFieldID = 2;

	public const int EntitiesFieldID = 3;

	public const int RecipesFieldID = 4;

	public const int TagsFieldID = 5;

	public int size;
}
