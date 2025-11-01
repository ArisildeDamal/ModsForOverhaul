using System;

public class Packet_CrushingProperties
{
	public void SetCrushedStack(byte[] value)
	{
		this.CrushedStack = value;
	}

	public void SetHardnessTier(int value)
	{
		this.HardnessTier = value;
	}

	public void SetQuantity(Packet_NatFloat value)
	{
		this.Quantity = value;
	}

	internal void InitializeValues()
	{
	}

	public byte[] CrushedStack;

	public int HardnessTier;

	public Packet_NatFloat Quantity;

	public const int CrushedStackFieldID = 1;

	public const int HardnessTierFieldID = 2;

	public const int QuantityFieldID = 3;

	public int size;
}
