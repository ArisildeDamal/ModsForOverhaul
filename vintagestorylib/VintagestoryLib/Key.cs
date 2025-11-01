using System;

public struct Key
{
	public readonly int Field
	{
		get
		{
			return this.key >> 3;
		}
	}

	public readonly int WireType
	{
		get
		{
			return this.key % 8;
		}
	}

	public Key(byte field, byte wiretype)
	{
		this.key = ((int)field << 3) + (int)wiretype;
	}

	public static implicit operator int(Key a)
	{
		return a.key;
	}

	public static Key Create(int firstByte, int secondByte)
	{
		return new Key
		{
			key = ((secondByte << 7) | (firstByte & 127))
		};
	}

	public static Key Create(int n)
	{
		return new Key
		{
			key = n
		};
	}

	public const int Size1 = 1;

	public const int Size2 = 2;

	public int key;
}
