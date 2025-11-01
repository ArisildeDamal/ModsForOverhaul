using System;

public class Packet_GeneratedStructureSerializer
{
	public static Packet_GeneratedStructure DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_GeneratedStructure instance = new Packet_GeneratedStructure();
		Packet_GeneratedStructureSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_GeneratedStructure DeserializeBuffer(byte[] buffer, int length, Packet_GeneratedStructure instance)
	{
		Packet_GeneratedStructureSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_GeneratedStructure Deserialize(CitoMemoryStream stream, Packet_GeneratedStructure instance)
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
						goto IL_007C;
					}
					if (keyInt == 8)
					{
						instance.X1 = ProtocolParser.ReadUInt32(stream);
						continue;
					}
				}
				else
				{
					if (keyInt == 16)
					{
						instance.Y1 = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 24)
					{
						instance.Z1 = ProtocolParser.ReadUInt32(stream);
						continue;
					}
				}
			}
			else if (keyInt <= 40)
			{
				if (keyInt == 32)
				{
					instance.X2 = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 40)
				{
					instance.Y2 = ProtocolParser.ReadUInt32(stream);
					continue;
				}
			}
			else
			{
				if (keyInt == 48)
				{
					instance.Z2 = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 58)
				{
					instance.Code = ProtocolParser.ReadString(stream);
					continue;
				}
				if (keyInt == 66)
				{
					instance.Group = ProtocolParser.ReadString(stream);
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
		IL_007C:
		return null;
	}

	public static Packet_GeneratedStructure DeserializeLengthDelimited(CitoMemoryStream stream, Packet_GeneratedStructure instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_GeneratedStructure packet_GeneratedStructure = Packet_GeneratedStructureSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_GeneratedStructure;
	}

	public static void Serialize(CitoStream stream, Packet_GeneratedStructure instance)
	{
		if (instance.X1 != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.X1);
		}
		if (instance.Y1 != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.Y1);
		}
		if (instance.Z1 != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.Z1);
		}
		if (instance.X2 != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.X2);
		}
		if (instance.Y2 != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.Y2);
		}
		if (instance.Z2 != 0)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt32(stream, instance.Z2);
		}
		if (instance.Code != null)
		{
			stream.WriteByte(58);
			ProtocolParser.WriteString(stream, instance.Code);
		}
		if (instance.Group != null)
		{
			stream.WriteByte(66);
			ProtocolParser.WriteString(stream, instance.Group);
		}
	}

	public static int GetSize(Packet_GeneratedStructure instance)
	{
		int size = 0;
		if (instance.X1 != 0)
		{
			size += ProtocolParser.GetSize(instance.X1) + 1;
		}
		if (instance.Y1 != 0)
		{
			size += ProtocolParser.GetSize(instance.Y1) + 1;
		}
		if (instance.Z1 != 0)
		{
			size += ProtocolParser.GetSize(instance.Z1) + 1;
		}
		if (instance.X2 != 0)
		{
			size += ProtocolParser.GetSize(instance.X2) + 1;
		}
		if (instance.Y2 != 0)
		{
			size += ProtocolParser.GetSize(instance.Y2) + 1;
		}
		if (instance.Z2 != 0)
		{
			size += ProtocolParser.GetSize(instance.Z2) + 1;
		}
		if (instance.Code != null)
		{
			size += ProtocolParser.GetSize(instance.Code) + 1;
		}
		if (instance.Group != null)
		{
			size += ProtocolParser.GetSize(instance.Group) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_GeneratedStructure instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_GeneratedStructureSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_GeneratedStructure instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_GeneratedStructureSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_GeneratedStructure instance)
	{
		byte[] data = Packet_GeneratedStructureSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
