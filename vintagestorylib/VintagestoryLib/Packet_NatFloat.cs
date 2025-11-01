using System;

public class Packet_NatFloat
{
	public void SetAvg(int value)
	{
		this.Avg = value;
	}

	public void SetVar(int value)
	{
		this.Var = value;
	}

	public void SetDist(int value)
	{
		this.Dist = value;
	}

	internal void InitializeValues()
	{
	}

	public int Avg;

	public int Var;

	public int Dist;

	public const int AvgFieldID = 1;

	public const int VarFieldID = 2;

	public const int DistFieldID = 3;

	public int size;
}
