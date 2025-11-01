using System;

public class Packet_StopMovementSerializer
{
	public static Packet_StopMovement DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_StopMovement instance = new Packet_StopMovement();
		Packet_StopMovementSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_StopMovement DeserializeBuffer(byte[] buffer, int length, Packet_StopMovement instance)
	{
		Packet_StopMovementSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_StopMovement Deserialize(CitoMemoryStream stream, Packet_StopMovement instance)
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

	public static Packet_StopMovement DeserializeLengthDelimited(CitoMemoryStream stream, Packet_StopMovement instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_StopMovement packet_StopMovement = Packet_StopMovementSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_StopMovement;
	}

	public static void Serialize(CitoStream stream, Packet_StopMovement instance)
	{
	}

	public static int GetSize(Packet_StopMovement instance)
	{
		int size = 0;
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_StopMovement instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_StopMovementSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_StopMovement instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_StopMovementSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_StopMovement instance)
	{
		byte[] data = Packet_StopMovementSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
