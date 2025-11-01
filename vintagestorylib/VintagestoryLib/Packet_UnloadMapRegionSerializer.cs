using System;

public class Packet_UnloadMapRegionSerializer
{
	public static Packet_UnloadMapRegion DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_UnloadMapRegion instance = new Packet_UnloadMapRegion();
		Packet_UnloadMapRegionSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_UnloadMapRegion DeserializeBuffer(byte[] buffer, int length, Packet_UnloadMapRegion instance)
	{
		Packet_UnloadMapRegionSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_UnloadMapRegion Deserialize(CitoMemoryStream stream, Packet_UnloadMapRegion instance)
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
			if (keyInt == 0)
			{
				goto IL_003B;
			}
			if (keyInt != 8)
			{
				if (keyInt != 16)
				{
					ProtocolParser.SkipKey(stream, Key.Create(keyInt));
				}
				else
				{
					instance.RegionZ = ProtocolParser.ReadUInt32(stream);
				}
			}
			else
			{
				instance.RegionX = ProtocolParser.ReadUInt32(stream);
			}
		}
		if (keyInt >= 0)
		{
			return null;
		}
		return instance;
		IL_003B:
		return null;
	}

	public static Packet_UnloadMapRegion DeserializeLengthDelimited(CitoMemoryStream stream, Packet_UnloadMapRegion instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_UnloadMapRegion packet_UnloadMapRegion = Packet_UnloadMapRegionSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_UnloadMapRegion;
	}

	public static void Serialize(CitoStream stream, Packet_UnloadMapRegion instance)
	{
		if (instance.RegionX != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.RegionX);
		}
		if (instance.RegionZ != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.RegionZ);
		}
	}

	public static int GetSize(Packet_UnloadMapRegion instance)
	{
		int size = 0;
		if (instance.RegionX != 0)
		{
			size += ProtocolParser.GetSize(instance.RegionX) + 1;
		}
		if (instance.RegionZ != 0)
		{
			size += ProtocolParser.GetSize(instance.RegionZ) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_UnloadMapRegion instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_UnloadMapRegionSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_UnloadMapRegion instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_UnloadMapRegionSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_UnloadMapRegion instance)
	{
		byte[] data = Packet_UnloadMapRegionSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
