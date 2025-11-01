using System;

public class BitTools
{
	public static bool IsPowerOfTwo(int x)
	{
		return x > 0 && (x & (x - 1)) == 0;
	}

	public static int NextPowerOfTwo(int x)
	{
		x--;
		x |= x >> 1;
		x |= x >> 2;
		x |= x >> 4;
		x |= x >> 8;
		x |= x >> 16;
		x++;
		return x;
	}
}
