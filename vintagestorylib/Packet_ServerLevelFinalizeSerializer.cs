using System;

public class Packet_ServerLevelFinalizeSerializer
{
	public static Packet_ServerLevelFinalize DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ServerLevelFinalize instance = new Packet_ServerLevelFinalize();
		Packet_ServerLevelFinalizeSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ServerLevelFinalize DeserializeBuffer(byte[] buffer, int length, Packet_ServerLevelFinalize instance)
	{
		Packet_ServerLevelFinalizeSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ServerLevelFinalize Deserialize(CitoMemoryStream stream, Packet_ServerLevelFinalize instance)
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

	public static Packet_ServerLevelFinalize DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ServerLevelFinalize instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ServerLevelFinalize packet_ServerLevelFinalize = Packet_ServerLevelFinalizeSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_ServerLevelFinalize;
	}

	public static void Serialize(CitoStream stream, Packet_ServerLevelFinalize instance)
	{
	}

	public static int GetSize(Packet_ServerLevelFinalize instance)
	{
		int size = 0;
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ServerLevelFinalize instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_ServerLevelFinalizeSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_ServerLevelFinalize instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_ServerLevelFinalizeSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ServerLevelFinalize instance)
	{
		byte[] data = Packet_ServerLevelFinalizeSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
