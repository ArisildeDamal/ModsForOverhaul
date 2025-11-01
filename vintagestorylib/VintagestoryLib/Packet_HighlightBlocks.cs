using System;

public class Packet_HighlightBlocks
{
	public void SetMode(int value)
	{
		this.Mode = value;
	}

	public void SetShape(int value)
	{
		this.Shape = value;
	}

	public void SetBlocks(byte[] value)
	{
		this.Blocks = value;
	}

	public int[] GetColors()
	{
		return this.Colors;
	}

	public void SetColors(int[] value, int count, int length)
	{
		this.Colors = value;
		this.ColorsCount = count;
		this.ColorsLength = length;
	}

	public void SetColors(int[] value)
	{
		this.Colors = value;
		this.ColorsCount = value.Length;
		this.ColorsLength = value.Length;
	}

	public int GetColorsCount()
	{
		return this.ColorsCount;
	}

	public void ColorsAdd(int value)
	{
		if (this.ColorsCount >= this.ColorsLength)
		{
			if ((this.ColorsLength *= 2) == 0)
			{
				this.ColorsLength = 1;
			}
			int[] newArray = new int[this.ColorsLength];
			for (int i = 0; i < this.ColorsCount; i++)
			{
				newArray[i] = this.Colors[i];
			}
			this.Colors = newArray;
		}
		int[] colors = this.Colors;
		int colorsCount = this.ColorsCount;
		this.ColorsCount = colorsCount + 1;
		colors[colorsCount] = value;
	}

	public void SetSlotid(int value)
	{
		this.Slotid = value;
	}

	public void SetScale(int value)
	{
		this.Scale = value;
	}

	internal void InitializeValues()
	{
	}

	public int Mode;

	public int Shape;

	public byte[] Blocks;

	public int[] Colors;

	public int ColorsCount;

	public int ColorsLength;

	public int Slotid;

	public int Scale;

	public const int ModeFieldID = 1;

	public const int ShapeFieldID = 2;

	public const int BlocksFieldID = 3;

	public const int ColorsFieldID = 4;

	public const int SlotidFieldID = 5;

	public const int ScaleFieldID = 6;

	public int size;
}
