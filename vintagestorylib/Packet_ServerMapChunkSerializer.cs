using System;

public class Packet_ServerMapChunkSerializer
{
	public static Packet_ServerMapChunk DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ServerMapChunk instance = new Packet_ServerMapChunk();
		Packet_ServerMapChunkSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ServerMapChunk DeserializeBuffer(byte[] buffer, int length, Packet_ServerMapChunk instance)
	{
		Packet_ServerMapChunkSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ServerMapChunk Deserialize(CitoMemoryStream stream, Packet_ServerMapChunk instance)
	{
		instance.InitializeValues();
		int keyInt;
		for (;;)
		{
			keyInt = stream.ReadByte();
			if ((keyInt & 128) != 0)
			{
				keyInt = ProtocolParser.ReadKeyAsInt(keyInt, stream);
				if ((keyInt & 16384) != 0)
				{
					break;
				}
			}
			if (keyInt <= 24)
			{
				if (keyInt <= 8)
				{
					if (keyInt == 0)
					{
						goto IL_0074;
					}
					if (keyInt == 8)
					{
						instance.ChunkX = ProtocolParser.ReadUInt32(stream);
						continue;
					}
				}
				else
				{
					if (keyInt == 16)
					{
						instance.ChunkZ = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 24)
					{
						instance.Ymax = ProtocolParser.ReadUInt32(stream);
						continue;
					}
				}
			}
			else if (keyInt <= 50)
			{
				if (keyInt == 42)
				{
					instance.RainHeightMap = ProtocolParser.ReadBytes(stream);
					continue;
				}
				if (keyInt == 50)
				{
					instance.Structures = ProtocolParser.ReadBytes(stream);
					continue;
				}
			}
			else
			{
				if (keyInt == 58)
				{
					instance.TerrainHeightMap = ProtocolParser.ReadBytes(stream);
					continue;
				}
				if (keyInt == 66)
				{
					instance.Moddata = ProtocolParser.ReadBytes(stream);
					continue;
				}
			}
			ProtocolParser.SkipKey(stream, Key.Create(keyInt));
		}
		if (keyInt >= 0)
		{
			return null;
		}
		return instance;
		IL_0074:
		return null;
	}

	public static Packet_ServerMapChunk DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ServerMapChunk instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ServerMapChunk packet_ServerMapChunk = Packet_ServerMapChunkSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_ServerMapChunk;
	}

	public static void Serialize(CitoStream stream, Packet_ServerMapChunk instance)
	{
		if (instance.ChunkX != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.ChunkX);
		}
		if (instance.ChunkZ != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.ChunkZ);
		}
		if (instance.Ymax != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.Ymax);
		}
		if (instance.RainHeightMap != null)
		{
			stream.WriteByte(42);
			ProtocolParser.WriteBytes(stream, instance.RainHeightMap);
		}
		if (instance.TerrainHeightMap != null)
		{
			stream.WriteByte(58);
			ProtocolParser.WriteBytes(stream, instance.TerrainHeightMap);
		}
		if (instance.Structures != null)
		{
			stream.WriteByte(50);
			ProtocolParser.WriteBytes(stream, instance.Structures);
		}
		if (instance.Moddata != null)
		{
			stream.WriteByte(66);
			ProtocolParser.WriteBytes(stream, instance.Moddata);
		}
	}

	public static int GetSize(Packet_ServerMapChunk instance)
	{
		int size = 0;
		if (instance.ChunkX != 0)
		{
			size += ProtocolParser.GetSize(instance.ChunkX) + 1;
		}
		if (instance.ChunkZ != 0)
		{
			size += ProtocolParser.GetSize(instance.ChunkZ) + 1;
		}
		if (instance.Ymax != 0)
		{
			size += ProtocolParser.GetSize(instance.Ymax) + 1;
		}
		if (instance.RainHeightMap != null)
		{
			size += ProtocolParser.GetSize(instance.RainHeightMap) + 1;
		}
		if (instance.TerrainHeightMap != null)
		{
			size += ProtocolParser.GetSize(instance.TerrainHeightMap) + 1;
		}
		if (instance.Structures != null)
		{
			size += ProtocolParser.GetSize(instance.Structures) + 1;
		}
		if (instance.Moddata != null)
		{
			size += ProtocolParser.GetSize(instance.Moddata) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ServerMapChunk instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_ServerMapChunkSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_ServerMapChunk instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_ServerMapChunkSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ServerMapChunk instance)
	{
		byte[] data = Packet_ServerMapChunkSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
