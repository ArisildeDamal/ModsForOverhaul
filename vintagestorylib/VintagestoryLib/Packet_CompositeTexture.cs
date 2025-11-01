using System;

public class Packet_CompositeTexture
{
	public void SetBase(string value)
	{
		this.Base = value;
	}

	public Packet_BlendedOverlayTexture[] GetOverlays()
	{
		return this.Overlays;
	}

	public void SetOverlays(Packet_BlendedOverlayTexture[] value, int count, int length)
	{
		this.Overlays = value;
		this.OverlaysCount = count;
		this.OverlaysLength = length;
	}

	public void SetOverlays(Packet_BlendedOverlayTexture[] value)
	{
		this.Overlays = value;
		this.OverlaysCount = value.Length;
		this.OverlaysLength = value.Length;
	}

	public int GetOverlaysCount()
	{
		return this.OverlaysCount;
	}

	public void OverlaysAdd(Packet_BlendedOverlayTexture value)
	{
		if (this.OverlaysCount >= this.OverlaysLength)
		{
			if ((this.OverlaysLength *= 2) == 0)
			{
				this.OverlaysLength = 1;
			}
			Packet_BlendedOverlayTexture[] newArray = new Packet_BlendedOverlayTexture[this.OverlaysLength];
			for (int i = 0; i < this.OverlaysCount; i++)
			{
				newArray[i] = this.Overlays[i];
			}
			this.Overlays = newArray;
		}
		Packet_BlendedOverlayTexture[] overlays = this.Overlays;
		int overlaysCount = this.OverlaysCount;
		this.OverlaysCount = overlaysCount + 1;
		overlays[overlaysCount] = value;
	}

	public Packet_CompositeTexture[] GetAlternates()
	{
		return this.Alternates;
	}

	public void SetAlternates(Packet_CompositeTexture[] value, int count, int length)
	{
		this.Alternates = value;
		this.AlternatesCount = count;
		this.AlternatesLength = length;
	}

	public void SetAlternates(Packet_CompositeTexture[] value)
	{
		this.Alternates = value;
		this.AlternatesCount = value.Length;
		this.AlternatesLength = value.Length;
	}

	public int GetAlternatesCount()
	{
		return this.AlternatesCount;
	}

	public void AlternatesAdd(Packet_CompositeTexture value)
	{
		if (this.AlternatesCount >= this.AlternatesLength)
		{
			if ((this.AlternatesLength *= 2) == 0)
			{
				this.AlternatesLength = 1;
			}
			Packet_CompositeTexture[] newArray = new Packet_CompositeTexture[this.AlternatesLength];
			for (int i = 0; i < this.AlternatesCount; i++)
			{
				newArray[i] = this.Alternates[i];
			}
			this.Alternates = newArray;
		}
		Packet_CompositeTexture[] alternates = this.Alternates;
		int alternatesCount = this.AlternatesCount;
		this.AlternatesCount = alternatesCount + 1;
		alternates[alternatesCount] = value;
	}

	public void SetRotation(int value)
	{
		this.Rotation = value;
	}

	public void SetAlpha(int value)
	{
		this.Alpha = value;
	}

	public Packet_CompositeTexture[] GetTiles()
	{
		return this.Tiles;
	}

	public void SetTiles(Packet_CompositeTexture[] value, int count, int length)
	{
		this.Tiles = value;
		this.TilesCount = count;
		this.TilesLength = length;
	}

	public void SetTiles(Packet_CompositeTexture[] value)
	{
		this.Tiles = value;
		this.TilesCount = value.Length;
		this.TilesLength = value.Length;
	}

	public int GetTilesCount()
	{
		return this.TilesCount;
	}

	public void TilesAdd(Packet_CompositeTexture value)
	{
		if (this.TilesCount >= this.TilesLength)
		{
			if ((this.TilesLength *= 2) == 0)
			{
				this.TilesLength = 1;
			}
			Packet_CompositeTexture[] newArray = new Packet_CompositeTexture[this.TilesLength];
			for (int i = 0; i < this.TilesCount; i++)
			{
				newArray[i] = this.Tiles[i];
			}
			this.Tiles = newArray;
		}
		Packet_CompositeTexture[] tiles = this.Tiles;
		int tilesCount = this.TilesCount;
		this.TilesCount = tilesCount + 1;
		tiles[tilesCount] = value;
	}

	public void SetTilesWidth(int value)
	{
		this.TilesWidth = value;
	}

	internal void InitializeValues()
	{
	}

	public string Base;

	public Packet_BlendedOverlayTexture[] Overlays;

	public int OverlaysCount;

	public int OverlaysLength;

	public Packet_CompositeTexture[] Alternates;

	public int AlternatesCount;

	public int AlternatesLength;

	public int Rotation;

	public int Alpha;

	public Packet_CompositeTexture[] Tiles;

	public int TilesCount;

	public int TilesLength;

	public int TilesWidth;

	public const int BaseFieldID = 1;

	public const int OverlaysFieldID = 2;

	public const int AlternatesFieldID = 3;

	public const int RotationFieldID = 4;

	public const int AlphaFieldID = 5;

	public const int TilesFieldID = 6;

	public const int TilesWidthFieldID = 7;

	public int size;
}
