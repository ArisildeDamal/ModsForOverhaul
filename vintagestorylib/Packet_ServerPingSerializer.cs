using System;

public class Packet_ServerPingSerializer
{
	public static Packet_ServerPing DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ServerPing instance = new Packet_ServerPing();
		Packet_ServerPingSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ServerPing DeserializeBuffer(byte[] buffer, int length, Packet_ServerPing instance)
	{
		Packet_ServerPingSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ServerPing Deserialize(CitoMemoryStream stream, Packet_ServerPing instance)
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

	public static Packet_ServerPing DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ServerPing instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ServerPing packet_ServerPing = Packet_ServerPingSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_ServerPing;
	}

	public static void Serialize(CitoStream stream, Packet_ServerPing instance)
	{
	}

	public static int GetSize(Packet_ServerPing instance)
	{
		int size = 0;
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ServerPing instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_ServerPingSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_ServerPing instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_ServerPingSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ServerPing instance)
	{
		byte[] data = Packet_ServerPingSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
