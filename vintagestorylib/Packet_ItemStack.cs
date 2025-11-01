using System;

public class Packet_ItemStack
{
	public void SetItemClass(int value)
	{
		this.ItemClass = value;
	}

	public void SetItemId(int value)
	{
		this.ItemId = value;
	}

	public void SetStackSize(int value)
	{
		this.StackSize = value;
	}

	public void SetAttributes(byte[] value)
	{
		this.Attributes = value;
	}

	internal void InitializeValues()
	{
		this.ItemClass = 0;
	}

	public int ItemClass;

	public int ItemId;

	public int StackSize;

	public byte[] Attributes;

	public const int ItemClassFieldID = 1;

	public const int ItemIdFieldID = 2;

	public const int StackSizeFieldID = 3;

	public const int AttributesFieldID = 4;

	public int size;
}
