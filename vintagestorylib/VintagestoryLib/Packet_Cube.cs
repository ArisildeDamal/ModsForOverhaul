using System;

public class Packet_Cube
{
	public void SetMinx(int value)
	{
		this.Minx = value;
	}

	public void SetMiny(int value)
	{
		this.Miny = value;
	}

	public void SetMinz(int value)
	{
		this.Minz = value;
	}

	public void SetMaxx(int value)
	{
		this.Maxx = value;
	}

	public void SetMaxy(int value)
	{
		this.Maxy = value;
	}

	public void SetMaxz(int value)
	{
		this.Maxz = value;
	}

	internal void InitializeValues()
	{
	}

	public int Minx;

	public int Miny;

	public int Minz;

	public int Maxx;

	public int Maxy;

	public int Maxz;

	public const int MinxFieldID = 1;

	public const int MinyFieldID = 2;

	public const int MinzFieldID = 3;

	public const int MaxxFieldID = 4;

	public const int MaxyFieldID = 5;

	public const int MaxzFieldID = 6;

	public int size;
}
