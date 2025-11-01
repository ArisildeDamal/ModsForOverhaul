using System;

public class Packet_ServerReadySerializer
{
	public static Packet_ServerReady DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ServerReady instance = new Packet_ServerReady();
		Packet_ServerReadySerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ServerReady DeserializeBuffer(byte[] buffer, int length, Packet_ServerReady instance)
	{
		Packet_ServerReadySerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ServerReady Deserialize(CitoMemoryStream stream, Packet_ServerReady instance)
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

	public static Packet_ServerReady DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ServerReady instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ServerReady packet_ServerReady = Packet_ServerReadySerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_ServerReady;
	}

	public static void Serialize(CitoStream stream, Packet_ServerReady instance)
	{
	}

	public static int GetSize(Packet_ServerReady instance)
	{
		int size = 0;
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ServerReady instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_ServerReadySerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_ServerReady instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_ServerReadySerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ServerReady instance)
	{
		byte[] data = Packet_ServerReadySerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
