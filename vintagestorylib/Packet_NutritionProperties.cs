using System;

public class Packet_NutritionProperties
{
	public void SetFoodCategory(int value)
	{
		this.FoodCategory = value;
	}

	public void SetSaturation(int value)
	{
		this.Saturation = value;
	}

	public void SetHealth(int value)
	{
		this.Health = value;
	}

	public void SetEatenStack(byte[] value)
	{
		this.EatenStack = value;
	}

	internal void InitializeValues()
	{
	}

	public int FoodCategory;

	public int Saturation;

	public int Health;

	public byte[] EatenStack;

	public const int FoodCategoryFieldID = 1;

	public const int SaturationFieldID = 2;

	public const int HealthFieldID = 3;

	public const int EatenStackFieldID = 4;

	public int size;
}
