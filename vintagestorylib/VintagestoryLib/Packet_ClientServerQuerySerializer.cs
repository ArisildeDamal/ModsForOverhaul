using System;

public class Packet_ClientServerQuerySerializer
{
	public static Packet_ClientServerQuery DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ClientServerQuery instance = new Packet_ClientServerQuery();
		Packet_ClientServerQuerySerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ClientServerQuery DeserializeBuffer(byte[] buffer, int length, Packet_ClientServerQuery instance)
	{
		Packet_ClientServerQuerySerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ClientServerQuery Deserialize(CitoMemoryStream stream, Packet_ClientServerQuery instance)
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

	public static Packet_ClientServerQuery DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ClientServerQuery instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ClientServerQuery packet_ClientServerQuery = Packet_ClientServerQuerySerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_ClientServerQuery;
	}

	public static void Serialize(CitoStream stream, Packet_ClientServerQuery instance)
	{
	}

	public static int GetSize(Packet_ClientServerQuery instance)
	{
		int size = 0;
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ClientServerQuery instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_ClientServerQuerySerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_ClientServerQuery instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_ClientServerQuerySerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ClientServerQuery instance)
	{
		byte[] data = Packet_ClientServerQuerySerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
