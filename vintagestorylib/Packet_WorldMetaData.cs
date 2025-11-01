using System;

public class Packet_WorldMetaData
{
	public void SetSunBrightness(int value)
	{
		this.SunBrightness = value;
	}

	public int[] GetBlockLightlevels()
	{
		return this.BlockLightlevels;
	}

	public void SetBlockLightlevels(int[] value, int count, int length)
	{
		this.BlockLightlevels = value;
		this.BlockLightlevelsCount = count;
		this.BlockLightlevelsLength = length;
	}

	public void SetBlockLightlevels(int[] value)
	{
		this.BlockLightlevels = value;
		this.BlockLightlevelsCount = value.Length;
		this.BlockLightlevelsLength = value.Length;
	}

	public int GetBlockLightlevelsCount()
	{
		return this.BlockLightlevelsCount;
	}

	public void BlockLightlevelsAdd(int value)
	{
		if (this.BlockLightlevelsCount >= this.BlockLightlevelsLength)
		{
			if ((this.BlockLightlevelsLength *= 2) == 0)
			{
				this.BlockLightlevelsLength = 1;
			}
			int[] newArray = new int[this.BlockLightlevelsLength];
			for (int i = 0; i < this.BlockLightlevelsCount; i++)
			{
				newArray[i] = this.BlockLightlevels[i];
			}
			this.BlockLightlevels = newArray;
		}
		int[] blockLightlevels = this.BlockLightlevels;
		int blockLightlevelsCount = this.BlockLightlevelsCount;
		this.BlockLightlevelsCount = blockLightlevelsCount + 1;
		blockLightlevels[blockLightlevelsCount] = value;
	}

	public int[] GetSunLightlevels()
	{
		return this.SunLightlevels;
	}

	public void SetSunLightlevels(int[] value, int count, int length)
	{
		this.SunLightlevels = value;
		this.SunLightlevelsCount = count;
		this.SunLightlevelsLength = length;
	}

	public void SetSunLightlevels(int[] value)
	{
		this.SunLightlevels = value;
		this.SunLightlevelsCount = value.Length;
		this.SunLightlevelsLength = value.Length;
	}

	public int GetSunLightlevelsCount()
	{
		return this.SunLightlevelsCount;
	}

	public void SunLightlevelsAdd(int value)
	{
		if (this.SunLightlevelsCount >= this.SunLightlevelsLength)
		{
			if ((this.SunLightlevelsLength *= 2) == 0)
			{
				this.SunLightlevelsLength = 1;
			}
			int[] newArray = new int[this.SunLightlevelsLength];
			for (int i = 0; i < this.SunLightlevelsCount; i++)
			{
				newArray[i] = this.SunLightlevels[i];
			}
			this.SunLightlevels = newArray;
		}
		int[] sunLightlevels = this.SunLightlevels;
		int sunLightlevelsCount = this.SunLightlevelsCount;
		this.SunLightlevelsCount = sunLightlevelsCount + 1;
		sunLightlevels[sunLightlevelsCount] = value;
	}

	public void SetWorldConfiguration(byte[] value)
	{
		this.WorldConfiguration = value;
	}

	public void SetSeaLevel(int value)
	{
		this.SeaLevel = value;
	}

	internal void InitializeValues()
	{
	}

	public int SunBrightness;

	public int[] BlockLightlevels;

	public int BlockLightlevelsCount;

	public int BlockLightlevelsLength;

	public int[] SunLightlevels;

	public int SunLightlevelsCount;

	public int SunLightlevelsLength;

	public byte[] WorldConfiguration;

	public int SeaLevel;

	public const int SunBrightnessFieldID = 1;

	public const int BlockLightlevelsFieldID = 2;

	public const int SunLightlevelsFieldID = 3;

	public const int WorldConfigurationFieldID = 4;

	public const int SeaLevelFieldID = 5;

	public int size;
}
