using System;

public class Wire
{
	public static bool IsValid(int v)
	{
		return v <= 2 || v == 5;
	}

	public const int Varint = 0;

	public const int Fixed64 = 1;

	public const int LengthDelimited = 2;

	public const int Fixed32 = 5;
}
