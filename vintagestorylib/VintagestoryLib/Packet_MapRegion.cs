using System;

public class Packet_MapRegion
{
	public void SetRegionX(int value)
	{
		this.RegionX = value;
	}

	public void SetRegionZ(int value)
	{
		this.RegionZ = value;
	}

	public void SetLandformMap(Packet_IntMap value)
	{
		this.LandformMap = value;
	}

	public void SetForestMap(Packet_IntMap value)
	{
		this.ForestMap = value;
	}

	public void SetClimateMap(Packet_IntMap value)
	{
		this.ClimateMap = value;
	}

	public void SetGeologicProvinceMap(Packet_IntMap value)
	{
		this.GeologicProvinceMap = value;
	}

	public Packet_GeneratedStructure[] GetGeneratedStructures()
	{
		return this.GeneratedStructures;
	}

	public void SetGeneratedStructures(Packet_GeneratedStructure[] value, int count, int length)
	{
		this.GeneratedStructures = value;
		this.GeneratedStructuresCount = count;
		this.GeneratedStructuresLength = length;
	}

	public void SetGeneratedStructures(Packet_GeneratedStructure[] value)
	{
		this.GeneratedStructures = value;
		this.GeneratedStructuresCount = value.Length;
		this.GeneratedStructuresLength = value.Length;
	}

	public int GetGeneratedStructuresCount()
	{
		return this.GeneratedStructuresCount;
	}

	public void GeneratedStructuresAdd(Packet_GeneratedStructure value)
	{
		if (this.GeneratedStructuresCount >= this.GeneratedStructuresLength)
		{
			if ((this.GeneratedStructuresLength *= 2) == 0)
			{
				this.GeneratedStructuresLength = 1;
			}
			Packet_GeneratedStructure[] newArray = new Packet_GeneratedStructure[this.GeneratedStructuresLength];
			for (int i = 0; i < this.GeneratedStructuresCount; i++)
			{
				newArray[i] = this.GeneratedStructures[i];
			}
			this.GeneratedStructures = newArray;
		}
		Packet_GeneratedStructure[] generatedStructures = this.GeneratedStructures;
		int generatedStructuresCount = this.GeneratedStructuresCount;
		this.GeneratedStructuresCount = generatedStructuresCount + 1;
		generatedStructures[generatedStructuresCount] = value;
	}

	public void SetModdata(byte[] value)
	{
		this.Moddata = value;
	}

	public void SetOceanMap(Packet_IntMap value)
	{
		this.OceanMap = value;
	}

	internal void InitializeValues()
	{
	}

	public int RegionX;

	public int RegionZ;

	public Packet_IntMap LandformMap;

	public Packet_IntMap ForestMap;

	public Packet_IntMap ClimateMap;

	public Packet_IntMap GeologicProvinceMap;

	public Packet_GeneratedStructure[] GeneratedStructures;

	public int GeneratedStructuresCount;

	public int GeneratedStructuresLength;

	public byte[] Moddata;

	public Packet_IntMap OceanMap;

	public const int RegionXFieldID = 1;

	public const int RegionZFieldID = 2;

	public const int LandformMapFieldID = 3;

	public const int ForestMapFieldID = 4;

	public const int ClimateMapFieldID = 5;

	public const int GeologicProvinceMapFieldID = 6;

	public const int GeneratedStructuresFieldID = 7;

	public const int ModdataFieldID = 8;

	public const int OceanMapFieldID = 9;

	public int size;
}
