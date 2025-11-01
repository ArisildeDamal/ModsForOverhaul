using System;

public class Packet_UnloadServerChunk
{
	public int[] GetX()
	{
		return this.X;
	}

	public void SetX(int[] value, int count, int length)
	{
		this.X = value;
		this.XCount = count;
		this.XLength = length;
	}

	public void SetX(int[] value)
	{
		this.X = value;
		this.XCount = value.Length;
		this.XLength = value.Length;
	}

	public int GetXCount()
	{
		return this.XCount;
	}

	public void XAdd(int value)
	{
		if (this.XCount >= this.XLength)
		{
			if ((this.XLength *= 2) == 0)
			{
				this.XLength = 1;
			}
			int[] newArray = new int[this.XLength];
			for (int i = 0; i < this.XCount; i++)
			{
				newArray[i] = this.X[i];
			}
			this.X = newArray;
		}
		int[] x = this.X;
		int xcount = this.XCount;
		this.XCount = xcount + 1;
		x[xcount] = value;
	}

	public int[] GetY()
	{
		return this.Y;
	}

	public void SetY(int[] value, int count, int length)
	{
		this.Y = value;
		this.YCount = count;
		this.YLength = length;
	}

	public void SetY(int[] value)
	{
		this.Y = value;
		this.YCount = value.Length;
		this.YLength = value.Length;
	}

	public int GetYCount()
	{
		return this.YCount;
	}

	public void YAdd(int value)
	{
		if (this.YCount >= this.YLength)
		{
			if ((this.YLength *= 2) == 0)
			{
				this.YLength = 1;
			}
			int[] newArray = new int[this.YLength];
			for (int i = 0; i < this.YCount; i++)
			{
				newArray[i] = this.Y[i];
			}
			this.Y = newArray;
		}
		int[] y = this.Y;
		int ycount = this.YCount;
		this.YCount = ycount + 1;
		y[ycount] = value;
	}

	public int[] GetZ()
	{
		return this.Z;
	}

	public void SetZ(int[] value, int count, int length)
	{
		this.Z = value;
		this.ZCount = count;
		this.ZLength = length;
	}

	public void SetZ(int[] value)
	{
		this.Z = value;
		this.ZCount = value.Length;
		this.ZLength = value.Length;
	}

	public int GetZCount()
	{
		return this.ZCount;
	}

	public void ZAdd(int value)
	{
		if (this.ZCount >= this.ZLength)
		{
			if ((this.ZLength *= 2) == 0)
			{
				this.ZLength = 1;
			}
			int[] newArray = new int[this.ZLength];
			for (int i = 0; i < this.ZCount; i++)
			{
				newArray[i] = this.Z[i];
			}
			this.Z = newArray;
		}
		int[] z = this.Z;
		int zcount = this.ZCount;
		this.ZCount = zcount + 1;
		z[zcount] = value;
	}

	internal void InitializeValues()
	{
	}

	public int[] X;

	public int XCount;

	public int XLength;

	public int[] Y;

	public int YCount;

	public int YLength;

	public int[] Z;

	public int ZCount;

	public int ZLength;

	public const int XFieldID = 1;

	public const int YFieldID = 2;

	public const int ZFieldID = 3;

	public int size;
}
