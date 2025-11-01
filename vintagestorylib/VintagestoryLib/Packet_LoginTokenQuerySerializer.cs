using System;

public class Packet_LoginTokenQuerySerializer
{
	public static Packet_LoginTokenQuery DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_LoginTokenQuery instance = new Packet_LoginTokenQuery();
		Packet_LoginTokenQuerySerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_LoginTokenQuery DeserializeBuffer(byte[] buffer, int length, Packet_LoginTokenQuery instance)
	{
		Packet_LoginTokenQuerySerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_LoginTokenQuery Deserialize(CitoMemoryStream stream, Packet_LoginTokenQuery instance)
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
				goto Block_4;
			}
			ProtocolParser.SkipKey(stream, Key.Create(keyInt));
		}
		if (keyInt >= 0)
		{
			return null;
		}
		return instance;
		Block_4:
		return null;
	}

	public static Packet_LoginTokenQuery DeserializeLengthDelimited(CitoMemoryStream stream, Packet_LoginTokenQuery instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_LoginTokenQuery packet_LoginTokenQuery = Packet_LoginTokenQuerySerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_LoginTokenQuery;
	}

	public static void Serialize(CitoStream stream, Packet_LoginTokenQuery instance)
	{
	}

	public static int GetSize(Packet_LoginTokenQuery instance)
	{
		int size = 0;
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_LoginTokenQuery instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_LoginTokenQuerySerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_LoginTokenQuery instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_LoginTokenQuerySerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_LoginTokenQuery instance)
	{
		byte[] data = Packet_LoginTokenQuerySerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
