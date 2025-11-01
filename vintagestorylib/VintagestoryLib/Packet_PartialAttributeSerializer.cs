using System;

public class Packet_PartialAttributeSerializer
{
	public static Packet_PartialAttribute DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_PartialAttribute instance = new Packet_PartialAttribute();
		Packet_PartialAttributeSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_PartialAttribute DeserializeBuffer(byte[] buffer, int length, Packet_PartialAttribute instance)
	{
		Packet_PartialAttributeSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_PartialAttribute Deserialize(CitoMemoryStream stream, Packet_PartialAttribute instance)
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
				goto IL_003C;
			}
			if (keyInt != 10)
			{
				if (keyInt != 18)
				{
					ProtocolParser.SkipKey(stream, Key.Create(keyInt));
				}
				else
				{
					instance.Data = ProtocolParser.ReadBytes(stream);
				}
			}
			else
			{
				instance.Path = ProtocolParser.ReadString(stream);
			}
		}
		if (keyInt >= 0)
		{
			return null;
		}
		return instance;
		IL_003C:
		return null;
	}

	public static Packet_PartialAttribute DeserializeLengthDelimited(CitoMemoryStream stream, Packet_PartialAttribute instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_PartialAttribute packet_PartialAttribute = Packet_PartialAttributeSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_PartialAttribute;
	}

	public static void Serialize(CitoStream stream, Packet_PartialAttribute instance)
	{
		if (instance.Path != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.Path);
		}
		if (instance.Data != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteBytes(stream, instance.Data);
		}
	}

	public static int GetSize(Packet_PartialAttribute instance)
	{
		int size = 0;
		if (instance.Path != null)
		{
			size += ProtocolParser.GetSize(instance.Path) + 1;
		}
		if (instance.Data != null)
		{
			size += ProtocolParser.GetSize(instance.Data) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_PartialAttribute instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_PartialAttributeSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_PartialAttribute instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_PartialAttributeSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_PartialAttribute instance)
	{
		byte[] data = Packet_PartialAttributeSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
