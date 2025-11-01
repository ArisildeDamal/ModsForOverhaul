using System;

public class Packet_ServerSound
{
	public void SetName(string value)
	{
		this.Name = value;
	}

	public void SetX(int value)
	{
		this.X = value;
	}

	public void SetY(int value)
	{
		this.Y = value;
	}

	public void SetZ(int value)
	{
		this.Z = value;
	}

	public void SetPitch(int value)
	{
		this.Pitch = value;
	}

	public void SetRange(int value)
	{
		this.Range = value;
	}

	public void SetVolume(int value)
	{
		this.Volume = value;
	}

	public void SetSoundType(int value)
	{
		this.SoundType = value;
	}

	internal void InitializeValues()
	{
	}

	public string Name;

	public int X;

	public int Y;

	public int Z;

	public int Pitch;

	public int Range;

	public int Volume;

	public int SoundType;

	public const int NameFieldID = 1;

	public const int XFieldID = 2;

	public const int YFieldID = 3;

	public const int ZFieldID = 4;

	public const int PitchFieldID = 5;

	public const int RangeFieldID = 6;

	public const int VolumeFieldID = 7;

	public const int SoundTypeFieldID = 8;

	public int size;
}
