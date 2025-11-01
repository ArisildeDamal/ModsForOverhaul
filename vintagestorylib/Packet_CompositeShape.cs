using System;

public class Packet_CompositeShape
{
	public void SetBase(string value)
	{
		this.Base = value;
	}

	public void SetRotatex(int value)
	{
		this.Rotatex = value;
	}

	public void SetRotatey(int value)
	{
		this.Rotatey = value;
	}

	public void SetRotatez(int value)
	{
		this.Rotatez = value;
	}

	public Packet_CompositeShape[] GetAlternates()
	{
		return this.Alternates;
	}

	public void SetAlternates(Packet_CompositeShape[] value, int count, int length)
	{
		this.Alternates = value;
		this.AlternatesCount = count;
		this.AlternatesLength = length;
	}

	public void SetAlternates(Packet_CompositeShape[] value)
	{
		this.Alternates = value;
		this.AlternatesCount = value.Length;
		this.AlternatesLength = value.Length;
	}

	public int GetAlternatesCount()
	{
		return this.AlternatesCount;
	}

	public void AlternatesAdd(Packet_CompositeShape value)
	{
		if (this.AlternatesCount >= this.AlternatesLength)
		{
			if ((this.AlternatesLength *= 2) == 0)
			{
				this.AlternatesLength = 1;
			}
			Packet_CompositeShape[] newArray = new Packet_CompositeShape[this.AlternatesLength];
			for (int i = 0; i < this.AlternatesCount; i++)
			{
				newArray[i] = this.Alternates[i];
			}
			this.Alternates = newArray;
		}
		Packet_CompositeShape[] alternates = this.Alternates;
		int alternatesCount = this.AlternatesCount;
		this.AlternatesCount = alternatesCount + 1;
		alternates[alternatesCount] = value;
	}

	public Packet_CompositeShape[] GetOverlays()
	{
		return this.Overlays;
	}

	public void SetOverlays(Packet_CompositeShape[] value, int count, int length)
	{
		this.Overlays = value;
		this.OverlaysCount = count;
		this.OverlaysLength = length;
	}

	public void SetOverlays(Packet_CompositeShape[] value)
	{
		this.Overlays = value;
		this.OverlaysCount = value.Length;
		this.OverlaysLength = value.Length;
	}

	public int GetOverlaysCount()
	{
		return this.OverlaysCount;
	}

	public void OverlaysAdd(Packet_CompositeShape value)
	{
		if (this.OverlaysCount >= this.OverlaysLength)
		{
			if ((this.OverlaysLength *= 2) == 0)
			{
				this.OverlaysLength = 1;
			}
			Packet_CompositeShape[] newArray = new Packet_CompositeShape[this.OverlaysLength];
			for (int i = 0; i < this.OverlaysCount; i++)
			{
				newArray[i] = this.Overlays[i];
			}
			this.Overlays = newArray;
		}
		Packet_CompositeShape[] overlays = this.Overlays;
		int overlaysCount = this.OverlaysCount;
		this.OverlaysCount = overlaysCount + 1;
		overlays[overlaysCount] = value;
	}

	public void SetVoxelizeShape(int value)
	{
		this.VoxelizeShape = value;
	}

	public string[] GetSelectiveElements()
	{
		return this.SelectiveElements;
	}

	public void SetSelectiveElements(string[] value, int count, int length)
	{
		this.SelectiveElements = value;
		this.SelectiveElementsCount = count;
		this.SelectiveElementsLength = length;
	}

	public void SetSelectiveElements(string[] value)
	{
		this.SelectiveElements = value;
		this.SelectiveElementsCount = value.Length;
		this.SelectiveElementsLength = value.Length;
	}

	public int GetSelectiveElementsCount()
	{
		return this.SelectiveElementsCount;
	}

	public void SelectiveElementsAdd(string value)
	{
		if (this.SelectiveElementsCount >= this.SelectiveElementsLength)
		{
			if ((this.SelectiveElementsLength *= 2) == 0)
			{
				this.SelectiveElementsLength = 1;
			}
			string[] newArray = new string[this.SelectiveElementsLength];
			for (int i = 0; i < this.SelectiveElementsCount; i++)
			{
				newArray[i] = this.SelectiveElements[i];
			}
			this.SelectiveElements = newArray;
		}
		string[] selectiveElements = this.SelectiveElements;
		int selectiveElementsCount = this.SelectiveElementsCount;
		this.SelectiveElementsCount = selectiveElementsCount + 1;
		selectiveElements[selectiveElementsCount] = value;
	}

	public string[] GetIgnoreElements()
	{
		return this.IgnoreElements;
	}

	public void SetIgnoreElements(string[] value, int count, int length)
	{
		this.IgnoreElements = value;
		this.IgnoreElementsCount = count;
		this.IgnoreElementsLength = length;
	}

	public void SetIgnoreElements(string[] value)
	{
		this.IgnoreElements = value;
		this.IgnoreElementsCount = value.Length;
		this.IgnoreElementsLength = value.Length;
	}

	public int GetIgnoreElementsCount()
	{
		return this.IgnoreElementsCount;
	}

	public void IgnoreElementsAdd(string value)
	{
		if (this.IgnoreElementsCount >= this.IgnoreElementsLength)
		{
			if ((this.IgnoreElementsLength *= 2) == 0)
			{
				this.IgnoreElementsLength = 1;
			}
			string[] newArray = new string[this.IgnoreElementsLength];
			for (int i = 0; i < this.IgnoreElementsCount; i++)
			{
				newArray[i] = this.IgnoreElements[i];
			}
			this.IgnoreElements = newArray;
		}
		string[] ignoreElements = this.IgnoreElements;
		int ignoreElementsCount = this.IgnoreElementsCount;
		this.IgnoreElementsCount = ignoreElementsCount + 1;
		ignoreElements[ignoreElementsCount] = value;
	}

	public void SetQuantityElements(int value)
	{
		this.QuantityElements = value;
	}

	public void SetQuantityElementsSet(int value)
	{
		this.QuantityElementsSet = value;
	}

	public void SetFormat(int value)
	{
		this.Format = value;
	}

	public void SetOffsetx(int value)
	{
		this.Offsetx = value;
	}

	public void SetOffsety(int value)
	{
		this.Offsety = value;
	}

	public void SetOffsetz(int value)
	{
		this.Offsetz = value;
	}

	public void SetInsertBakedTextures(bool value)
	{
		this.InsertBakedTextures = value;
	}

	public void SetScaleAdjust(int value)
	{
		this.ScaleAdjust = value;
	}

	internal void InitializeValues()
	{
	}

	public string Base;

	public int Rotatex;

	public int Rotatey;

	public int Rotatez;

	public Packet_CompositeShape[] Alternates;

	public int AlternatesCount;

	public int AlternatesLength;

	public Packet_CompositeShape[] Overlays;

	public int OverlaysCount;

	public int OverlaysLength;

	public int VoxelizeShape;

	public string[] SelectiveElements;

	public int SelectiveElementsCount;

	public int SelectiveElementsLength;

	public string[] IgnoreElements;

	public int IgnoreElementsCount;

	public int IgnoreElementsLength;

	public int QuantityElements;

	public int QuantityElementsSet;

	public int Format;

	public int Offsetx;

	public int Offsety;

	public int Offsetz;

	public bool InsertBakedTextures;

	public int ScaleAdjust;

	public const int BaseFieldID = 1;

	public const int RotatexFieldID = 2;

	public const int RotateyFieldID = 3;

	public const int RotatezFieldID = 4;

	public const int AlternatesFieldID = 5;

	public const int OverlaysFieldID = 11;

	public const int VoxelizeShapeFieldID = 6;

	public const int SelectiveElementsFieldID = 7;

	public const int IgnoreElementsFieldID = 17;

	public const int QuantityElementsFieldID = 8;

	public const int QuantityElementsSetFieldID = 9;

	public const int FormatFieldID = 10;

	public const int OffsetxFieldID = 12;

	public const int OffsetyFieldID = 13;

	public const int OffsetzFieldID = 14;

	public const int InsertBakedTexturesFieldID = 15;

	public const int ScaleAdjustFieldID = 16;

	public int size;
}
