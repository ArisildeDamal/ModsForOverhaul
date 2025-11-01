using System;

public class Packet_BlockDrop
{
	public void SetQuantityAvg(int value)
	{
		this.QuantityAvg = value;
	}

	public void SetQuantityVar(int value)
	{
		this.QuantityVar = value;
	}

	public void SetQuantityDist(int value)
	{
		this.QuantityDist = value;
	}

	public void SetDroppedStack(byte[] value)
	{
		this.DroppedStack = value;
	}

	public void SetTool(int value)
	{
		this.Tool = value;
	}

	internal void InitializeValues()
	{
	}

	public int QuantityAvg;

	public int QuantityVar;

	public int QuantityDist;

	public byte[] DroppedStack;

	public int Tool;

	public const int QuantityAvgFieldID = 1;

	public const int QuantityVarFieldID = 2;

	public const int QuantityDistFieldID = 3;

	public const int DroppedStackFieldID = 4;

	public const int ToolFieldID = 5;

	public int size;
}
