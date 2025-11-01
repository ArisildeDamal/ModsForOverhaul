using System;

public class Packet_InvOpenClose
{
	public void SetInventoryId(string value)
	{
		this.InventoryId = value;
	}

	public void SetOpened(int value)
	{
		this.Opened = value;
	}

	internal void InitializeValues()
	{
	}

	public string InventoryId;

	public int Opened;

	public const int InventoryIdFieldID = 1;

	public const int OpenedFieldID = 2;

	public int size;
}
