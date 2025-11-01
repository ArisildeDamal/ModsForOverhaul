using System;

public class Packet_TransitionableProperties
{
	public void SetFreshHours(Packet_NatFloat value)
	{
		this.FreshHours = value;
	}

	public void SetTransitionHours(Packet_NatFloat value)
	{
		this.TransitionHours = value;
	}

	public void SetTransitionedStack(byte[] value)
	{
		this.TransitionedStack = value;
	}

	public void SetTransitionRatio(int value)
	{
		this.TransitionRatio = value;
	}

	public void SetType(int value)
	{
		this.Type = value;
	}

	internal void InitializeValues()
	{
	}

	public Packet_NatFloat FreshHours;

	public Packet_NatFloat TransitionHours;

	public byte[] TransitionedStack;

	public int TransitionRatio;

	public int Type;

	public const int FreshHoursFieldID = 1;

	public const int TransitionHoursFieldID = 2;

	public const int TransitionedStackFieldID = 3;

	public const int TransitionRatioFieldID = 4;

	public const int TypeFieldID = 5;

	public int size;
}
