using System;

public class Packet_CubeSerializer
{
	public static Packet_Cube DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_Cube instance = new Packet_Cube();
		Packet_CubeSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_Cube DeserializeBuffer(byte[] buffer, int length, Packet_Cube instance)
	{
		Packet_CubeSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_Cube Deserialize(CitoMemoryStream stream, Packet_Cube instance)
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
			if (keyInt <= 16)
			{
				if (keyInt == 0)
				{
					goto IL_0060;
				}
				if (keyInt == 8)
				{
					instance.Minx = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 16)
				{
					instance.Miny = ProtocolParser.ReadUInt32(stream);
					continue;
				}
			}
			else if (keyInt <= 32)
			{
				if (keyInt == 24)
				{
					instance.Minz = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 32)
				{
					instance.Maxx = ProtocolParser.ReadUInt32(stream);
					continue;
				}
			}
			else
			{
				if (keyInt == 40)
				{
					instance.Maxy = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 48)
				{
					instance.Maxz = ProtocolParser.ReadUInt32(stream);
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
		IL_0060:
		return null;
	}

	public static Packet_Cube DeserializeLengthDelimited(CitoMemoryStream stream, Packet_Cube instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_Cube packet_Cube = Packet_CubeSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_Cube;
	}

	public static void Serialize(CitoStream stream, Packet_Cube instance)
	{
		if (instance.Minx != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.Minx);
		}
		if (instance.Miny != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.Miny);
		}
		if (instance.Minz != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.Minz);
		}
		if (instance.Maxx != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.Maxx);
		}
		if (instance.Maxy != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.Maxy);
		}
		if (instance.Maxz != 0)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt32(stream, instance.Maxz);
		}
	}

	public static int GetSize(Packet_Cube instance)
	{
		int size = 0;
		if (instance.Minx != 0)
		{
			size += ProtocolParser.GetSize(instance.Minx) + 1;
		}
		if (instance.Miny != 0)
		{
			size += ProtocolParser.GetSize(instance.Miny) + 1;
		}
		if (instance.Minz != 0)
		{
			size += ProtocolParser.GetSize(instance.Minz) + 1;
		}
		if (instance.Maxx != 0)
		{
			size += ProtocolParser.GetSize(instance.Maxx) + 1;
		}
		if (instance.Maxy != 0)
		{
			size += ProtocolParser.GetSize(instance.Maxy) + 1;
		}
		if (instance.Maxz != 0)
		{
			size += ProtocolParser.GetSize(instance.Maxz) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_Cube instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_CubeSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_Cube instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_CubeSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_Cube instance)
	{
		byte[] data = Packet_CubeSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
