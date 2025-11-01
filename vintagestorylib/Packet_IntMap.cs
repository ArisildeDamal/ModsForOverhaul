using System;

public class Packet_IntMap
{
	public int[] GetData()
	{
		return this.Data;
	}

	public void SetData(int[] value, int count, int length)
	{
		this.Data = value;
		this.DataCount = count;
		this.DataLength = length;
	}

	public void SetData(int[] value)
	{
		this.Data = value;
		this.DataCount = value.Length;
		this.DataLength = value.Length;
	}

	public int GetDataCount()
	{
		return this.DataCount;
	}

	public void DataAdd(int value)
	{
		if (this.DataCount >= this.DataLength)
		{
			if ((this.DataLength *= 2) == 0)
			{
				this.DataLength = 1;
			}
			int[] newArray = new int[this.DataLength];
			for (int i = 0; i < this.DataCount; i++)
			{
				newArray[i] = this.Data[i];
			}
			this.Data = newArray;
		}
		int[] data = this.Data;
		int dataCount = this.DataCount;
		this.DataCount = dataCount + 1;
		data[dataCount] = value;
	}

	public void SetSize(int value)
	{
		this.Size = value;
	}

	public void SetTopLeftPadding(int value)
	{
		this.TopLeftPadding = value;
	}

	public void SetBottomRightPadding(int value)
	{
		this.BottomRightPadding = value;
	}

	internal void InitializeValues()
	{
	}

	public int[] Data;

	public int DataCount;

	public int DataLength;

	public int Size;

	public int TopLeftPadding;

	public int BottomRightPadding;

	public const int DataFieldID = 1;

	public const int SizeFieldID = 2;

	public const int TopLeftPaddingFieldID = 3;

	public const int BottomRightPaddingFieldID = 4;

	public int size;
}
