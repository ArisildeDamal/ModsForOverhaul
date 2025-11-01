using System;

public class Packet_EntityBoundingBoxSerializer
{
	public static Packet_EntityBoundingBox DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_EntityBoundingBox instance = new Packet_EntityBoundingBox();
		Packet_EntityBoundingBoxSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_EntityBoundingBox DeserializeBuffer(byte[] buffer, int length, Packet_EntityBoundingBox instance)
	{
		Packet_EntityBoundingBoxSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_EntityBoundingBox Deserialize(CitoMemoryStream stream, Packet_EntityBoundingBox instance)
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
			if (keyInt <= 8)
			{
				if (keyInt == 0)
				{
					goto IL_0046;
				}
				if (keyInt == 8)
				{
					instance.SizeX = ProtocolParser.ReadUInt32(stream);
					continue;
				}
			}
			else
			{
				if (keyInt == 16)
				{
					instance.SizeY = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 24)
				{
					instance.SizeZ = ProtocolParser.ReadUInt32(stream);
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
		IL_0046:
		return null;
	}

	public static Packet_EntityBoundingBox DeserializeLengthDelimited(CitoMemoryStream stream, Packet_EntityBoundingBox instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_EntityBoundingBox packet_EntityBoundingBox = Packet_EntityBoundingBoxSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_EntityBoundingBox;
	}

	public static void Serialize(CitoStream stream, Packet_EntityBoundingBox instance)
	{
		if (instance.SizeX != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.SizeX);
		}
		if (instance.SizeY != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.SizeY);
		}
		if (instance.SizeZ != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.SizeZ);
		}
	}

	public static int GetSize(Packet_EntityBoundingBox instance)
	{
		int size = 0;
		if (instance.SizeX != 0)
		{
			size += ProtocolParser.GetSize(instance.SizeX) + 1;
		}
		if (instance.SizeY != 0)
		{
			size += ProtocolParser.GetSize(instance.SizeY) + 1;
		}
		if (instance.SizeZ != 0)
		{
			size += ProtocolParser.GetSize(instance.SizeZ) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_EntityBoundingBox instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_EntityBoundingBoxSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_EntityBoundingBox instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_EntityBoundingBoxSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_EntityBoundingBox instance)
	{
		byte[] data = Packet_EntityBoundingBoxSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
