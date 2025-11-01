using System;

public class Packet_ClientLoadedSerializer
{
	public static Packet_ClientLoaded DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ClientLoaded instance = new Packet_ClientLoaded();
		Packet_ClientLoadedSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ClientLoaded DeserializeBuffer(byte[] buffer, int length, Packet_ClientLoaded instance)
	{
		Packet_ClientLoadedSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ClientLoaded Deserialize(CitoMemoryStream stream, Packet_ClientLoaded instance)
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

	public static Packet_ClientLoaded DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ClientLoaded instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ClientLoaded packet_ClientLoaded = Packet_ClientLoadedSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_ClientLoaded;
	}

	public static void Serialize(CitoStream stream, Packet_ClientLoaded instance)
	{
	}

	public static int GetSize(Packet_ClientLoaded instance)
	{
		int size = 0;
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ClientLoaded instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_ClientLoadedSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_ClientLoaded instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_ClientLoadedSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ClientLoaded instance)
	{
		byte[] data = Packet_ClientLoadedSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
