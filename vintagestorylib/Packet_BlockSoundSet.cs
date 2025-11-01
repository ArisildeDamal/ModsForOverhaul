using System;

public class Packet_BlockSoundSet
{
	public void SetWalk(string value)
	{
		this.Walk = value;
	}

	public void SetBreak(string value)
	{
		this.Break = value;
	}

	public void SetPlace(string value)
	{
		this.Place = value;
	}

	public void SetHit(string value)
	{
		this.Hit = value;
	}

	public void SetInside(string value)
	{
		this.Inside = value;
	}

	public void SetAmbient(string value)
	{
		this.Ambient = value;
	}

	public void SetAmbientBlockCount(int value)
	{
		this.AmbientBlockCount = value;
	}

	public void SetAmbientSoundType(int value)
	{
		this.AmbientSoundType = value;
	}

	public void SetAmbientMaxDistanceMerge(int value)
	{
		this.AmbientMaxDistanceMerge = value;
	}

	public int[] GetByToolTool()
	{
		return this.ByToolTool;
	}

	public void SetByToolTool(int[] value, int count, int length)
	{
		this.ByToolTool = value;
		this.ByToolToolCount = count;
		this.ByToolToolLength = length;
	}

	public void SetByToolTool(int[] value)
	{
		this.ByToolTool = value;
		this.ByToolToolCount = value.Length;
		this.ByToolToolLength = value.Length;
	}

	public int GetByToolToolCount()
	{
		return this.ByToolToolCount;
	}

	public void ByToolToolAdd(int value)
	{
		if (this.ByToolToolCount >= this.ByToolToolLength)
		{
			if ((this.ByToolToolLength *= 2) == 0)
			{
				this.ByToolToolLength = 1;
			}
			int[] newArray = new int[this.ByToolToolLength];
			for (int i = 0; i < this.ByToolToolCount; i++)
			{
				newArray[i] = this.ByToolTool[i];
			}
			this.ByToolTool = newArray;
		}
		int[] byToolTool = this.ByToolTool;
		int byToolToolCount = this.ByToolToolCount;
		this.ByToolToolCount = byToolToolCount + 1;
		byToolTool[byToolToolCount] = value;
	}

	public Packet_BlockSoundSet[] GetByToolSound()
	{
		return this.ByToolSound;
	}

	public void SetByToolSound(Packet_BlockSoundSet[] value, int count, int length)
	{
		this.ByToolSound = value;
		this.ByToolSoundCount = count;
		this.ByToolSoundLength = length;
	}

	public void SetByToolSound(Packet_BlockSoundSet[] value)
	{
		this.ByToolSound = value;
		this.ByToolSoundCount = value.Length;
		this.ByToolSoundLength = value.Length;
	}

	public int GetByToolSoundCount()
	{
		return this.ByToolSoundCount;
	}

	public void ByToolSoundAdd(Packet_BlockSoundSet value)
	{
		if (this.ByToolSoundCount >= this.ByToolSoundLength)
		{
			if ((this.ByToolSoundLength *= 2) == 0)
			{
				this.ByToolSoundLength = 1;
			}
			Packet_BlockSoundSet[] newArray = new Packet_BlockSoundSet[this.ByToolSoundLength];
			for (int i = 0; i < this.ByToolSoundCount; i++)
			{
				newArray[i] = this.ByToolSound[i];
			}
			this.ByToolSound = newArray;
		}
		Packet_BlockSoundSet[] byToolSound = this.ByToolSound;
		int byToolSoundCount = this.ByToolSoundCount;
		this.ByToolSoundCount = byToolSoundCount + 1;
		byToolSound[byToolSoundCount] = value;
	}

	internal void InitializeValues()
	{
	}

	public string Walk;

	public string Break;

	public string Place;

	public string Hit;

	public string Inside;

	public string Ambient;

	public int AmbientBlockCount;

	public int AmbientSoundType;

	public int AmbientMaxDistanceMerge;

	public int[] ByToolTool;

	public int ByToolToolCount;

	public int ByToolToolLength;

	public Packet_BlockSoundSet[] ByToolSound;

	public int ByToolSoundCount;

	public int ByToolSoundLength;

	public const int WalkFieldID = 1;

	public const int BreakFieldID = 2;

	public const int PlaceFieldID = 3;

	public const int HitFieldID = 4;

	public const int InsideFieldID = 5;

	public const int AmbientFieldID = 6;

	public const int AmbientBlockCountFieldID = 9;

	public const int AmbientSoundTypeFieldID = 10;

	public const int AmbientMaxDistanceMergeFieldID = 11;

	public const int ByToolToolFieldID = 7;

	public const int ByToolSoundFieldID = 8;

	public int size;
}
