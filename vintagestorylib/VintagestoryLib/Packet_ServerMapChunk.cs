using System;

public class Packet_ServerMapChunk
{
	public void SetChunkX(int value)
	{
		this.ChunkX = value;
	}

	public void SetChunkZ(int value)
	{
		this.ChunkZ = value;
	}

	public void SetYmax(int value)
	{
		this.Ymax = value;
	}

	public void SetRainHeightMap(byte[] value)
	{
		this.RainHeightMap = value;
	}

	public void SetTerrainHeightMap(byte[] value)
	{
		this.TerrainHeightMap = value;
	}

	public void SetStructures(byte[] value)
	{
		this.Structures = value;
	}

	public void SetModdata(byte[] value)
	{
		this.Moddata = value;
	}

	internal void InitializeValues()
	{
	}

	public int ChunkX;

	public int ChunkZ;

	public int Ymax;

	public byte[] RainHeightMap;

	public byte[] TerrainHeightMap;

	public byte[] Structures;

	public byte[] Moddata;

	public const int ChunkXFieldID = 1;

	public const int ChunkZFieldID = 2;

	public const int YmaxFieldID = 3;

	public const int RainHeightMapFieldID = 5;

	public const int TerrainHeightMapFieldID = 7;

	public const int StructuresFieldID = 6;

	public const int ModdataFieldID = 8;

	public int size;
}
