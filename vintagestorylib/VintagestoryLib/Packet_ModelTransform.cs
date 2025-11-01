using System;

public class Packet_ModelTransform
{
	public void SetTranslateX(int value)
	{
		this.TranslateX = value;
	}

	public void SetTranslateY(int value)
	{
		this.TranslateY = value;
	}

	public void SetTranslateZ(int value)
	{
		this.TranslateZ = value;
	}

	public void SetRotateX(int value)
	{
		this.RotateX = value;
	}

	public void SetRotateY(int value)
	{
		this.RotateY = value;
	}

	public void SetRotateZ(int value)
	{
		this.RotateZ = value;
	}

	public void SetRotate(int value)
	{
		this.Rotate = value;
	}

	public void SetOriginX(int value)
	{
		this.OriginX = value;
	}

	public void SetOriginY(int value)
	{
		this.OriginY = value;
	}

	public void SetOriginZ(int value)
	{
		this.OriginZ = value;
	}

	public void SetScaleX(int value)
	{
		this.ScaleX = value;
	}

	public void SetScaleY(int value)
	{
		this.ScaleY = value;
	}

	public void SetScaleZ(int value)
	{
		this.ScaleZ = value;
	}

	internal void InitializeValues()
	{
	}

	public int TranslateX;

	public int TranslateY;

	public int TranslateZ;

	public int RotateX;

	public int RotateY;

	public int RotateZ;

	public int Rotate;

	public int OriginX;

	public int OriginY;

	public int OriginZ;

	public int ScaleX;

	public int ScaleY;

	public int ScaleZ;

	public const int TranslateXFieldID = 1;

	public const int TranslateYFieldID = 2;

	public const int TranslateZFieldID = 3;

	public const int RotateXFieldID = 4;

	public const int RotateYFieldID = 5;

	public const int RotateZFieldID = 6;

	public const int RotateFieldID = 8;

	public const int OriginXFieldID = 9;

	public const int OriginYFieldID = 10;

	public const int OriginZFieldID = 11;

	public const int ScaleXFieldID = 12;

	public const int ScaleYFieldID = 13;

	public const int ScaleZFieldID = 14;

	public int size;
}
