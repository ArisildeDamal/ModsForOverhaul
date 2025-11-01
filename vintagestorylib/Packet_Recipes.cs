using System;

public class Packet_Recipes
{
	public void SetCode(string value)
	{
		this.Code = value;
	}

	public void SetQuantity(int value)
	{
		this.Quantity = value;
	}

	public void SetData(byte[] value)
	{
		this.Data = value;
	}

	internal void InitializeValues()
	{
	}

	public string Code;

	public int Quantity;

	public byte[] Data;

	public const int CodeFieldID = 1;

	public const int QuantityFieldID = 2;

	public const int DataFieldID = 3;

	public int size;
}
