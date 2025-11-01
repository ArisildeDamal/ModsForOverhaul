using System;

public class Packet_NatFloatSerializer
{
	public static Packet_NatFloat DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_NatFloat instance = new Packet_NatFloat();
		Packet_NatFloatSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_NatFloat DeserializeBuffer(byte[] buffer, int length, Packet_NatFloat instance)
	{
		Packet_NatFloatSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_NatFloat Deserialize(CitoMemoryStream stream, Packet_NatFloat instance)
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
					instance.Avg = ProtocolParser.ReadUInt32(stream);
					continue;
				}
			}
			else
			{
				if (keyInt == 16)
				{
					instance.Var = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 24)
				{
					instance.Dist = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_NatFloat DeserializeLengthDelimited(CitoMemoryStream stream, Packet_NatFloat instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_NatFloat packet_NatFloat = Packet_NatFloatSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_NatFloat;
	}

	public static void Serialize(CitoStream stream, Packet_NatFloat instance)
	{
		if (instance.Avg != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.Avg);
		}
		if (instance.Var != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.Var);
		}
		if (instance.Dist != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.Dist);
		}
	}

	public static int GetSize(Packet_NatFloat instance)
	{
		int size = 0;
		if (instance.Avg != 0)
		{
			size += ProtocolParser.GetSize(instance.Avg) + 1;
		}
		if (instance.Var != 0)
		{
			size += ProtocolParser.GetSize(instance.Var) + 1;
		}
		if (instance.Dist != 0)
		{
			size += ProtocolParser.GetSize(instance.Dist) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_NatFloat instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_NatFloatSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_NatFloat instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_NatFloatSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_NatFloat instance)
	{
		byte[] data = Packet_NatFloatSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
