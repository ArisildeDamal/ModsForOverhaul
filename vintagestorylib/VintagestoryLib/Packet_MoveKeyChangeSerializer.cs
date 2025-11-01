using System;

public class Packet_MoveKeyChangeSerializer
{
	public static Packet_MoveKeyChange DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_MoveKeyChange instance = new Packet_MoveKeyChange();
		Packet_MoveKeyChangeSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_MoveKeyChange DeserializeBuffer(byte[] buffer, int length, Packet_MoveKeyChange instance)
	{
		Packet_MoveKeyChangeSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_MoveKeyChange Deserialize(CitoMemoryStream stream, Packet_MoveKeyChange instance)
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
				goto IL_003B;
			}
			if (keyInt != 8)
			{
				if (keyInt != 16)
				{
					ProtocolParser.SkipKey(stream, Key.Create(keyInt));
				}
				else
				{
					instance.Down = ProtocolParser.ReadUInt32(stream);
				}
			}
			else
			{
				instance.Key = ProtocolParser.ReadUInt32(stream);
			}
		}
		if (keyInt >= 0)
		{
			return null;
		}
		return instance;
		IL_003B:
		return null;
	}

	public static Packet_MoveKeyChange DeserializeLengthDelimited(CitoMemoryStream stream, Packet_MoveKeyChange instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_MoveKeyChange packet_MoveKeyChange = Packet_MoveKeyChangeSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_MoveKeyChange;
	}

	public static void Serialize(CitoStream stream, Packet_MoveKeyChange instance)
	{
		if (instance.Key != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.Key);
		}
		if (instance.Down != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.Down);
		}
	}

	public static int GetSize(Packet_MoveKeyChange instance)
	{
		int size = 0;
		if (instance.Key != 0)
		{
			size += ProtocolParser.GetSize(instance.Key) + 1;
		}
		if (instance.Down != 0)
		{
			size += ProtocolParser.GetSize(instance.Down) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_MoveKeyChange instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_MoveKeyChangeSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_MoveKeyChange instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_MoveKeyChangeSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_MoveKeyChange instance)
	{
		byte[] data = Packet_MoveKeyChangeSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
