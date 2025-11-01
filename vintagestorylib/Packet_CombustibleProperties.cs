using System;

public class Packet_CombustibleProperties
{
	public void SetBurnTemperature(int value)
	{
		this.BurnTemperature = value;
	}

	public void SetBurnDuration(int value)
	{
		this.BurnDuration = value;
	}

	public void SetHeatResistance(int value)
	{
		this.HeatResistance = value;
	}

	public void SetMeltingPoint(int value)
	{
		this.MeltingPoint = value;
	}

	public void SetMeltingDuration(int value)
	{
		this.MeltingDuration = value;
	}

	public void SetSmeltedStack(byte[] value)
	{
		this.SmeltedStack = value;
	}

	public void SetSmeltedRatio(int value)
	{
		this.SmeltedRatio = value;
	}

	public void SetRequiresContainer(int value)
	{
		this.RequiresContainer = value;
	}

	public void SetMeltingType(int value)
	{
		this.MeltingType = value;
	}

	public void SetMaxTemperature(int value)
	{
		this.MaxTemperature = value;
	}

	internal void InitializeValues()
	{
	}

	public int BurnTemperature;

	public int BurnDuration;

	public int HeatResistance;

	public int MeltingPoint;

	public int MeltingDuration;

	public byte[] SmeltedStack;

	public int SmeltedRatio;

	public int RequiresContainer;

	public int MeltingType;

	public int MaxTemperature;

	public const int BurnTemperatureFieldID = 1;

	public const int BurnDurationFieldID = 2;

	public const int HeatResistanceFieldID = 3;

	public const int MeltingPointFieldID = 4;

	public const int MeltingDurationFieldID = 5;

	public const int SmeltedStackFieldID = 6;

	public const int SmeltedRatioFieldID = 7;

	public const int RequiresContainerFieldID = 8;

	public const int MeltingTypeFieldID = 9;

	public const int MaxTemperatureFieldID = 10;

	public int size;
}
