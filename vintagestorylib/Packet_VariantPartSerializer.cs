using System;

public class Packet_VariantPartSerializer
{
	public static Packet_VariantPart DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_VariantPart instance = new Packet_VariantPart();
		Packet_VariantPartSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_VariantPart DeserializeBuffer(byte[] buffer, int length, Packet_VariantPart instance)
	{
		Packet_VariantPartSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_VariantPart Deserialize(CitoMemoryStream stream, Packet_VariantPart instance)
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
					instance.Value = ProtocolParser.ReadString(stream);
				}
			}
			else
			{
				instance.Code = ProtocolParser.ReadString(stream);
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

	public static Packet_VariantPart DeserializeLengthDelimited(CitoMemoryStream stream, Packet_VariantPart instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_VariantPart packet_VariantPart = Packet_VariantPartSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_VariantPart;
	}

	public static void Serialize(CitoStream stream, Packet_VariantPart instance)
	{
		if (instance.Code != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.Code);
		}
		if (instance.Value != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteString(stream, instance.Value);
		}
	}

	public static int GetSize(Packet_VariantPart instance)
	{
		int size = 0;
		if (instance.Code != null)
		{
			size += ProtocolParser.GetSize(instance.Code) + 1;
		}
		if (instance.Value != null)
		{
			size += ProtocolParser.GetSize(instance.Value) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_VariantPart instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_VariantPartSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_VariantPart instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_VariantPartSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_VariantPart instance)
	{
		byte[] data = Packet_VariantPartSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
