using System;

public class Packet_ServerEntityDrawBlockSerializer
{
	public static Packet_ServerEntityDrawBlock DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ServerEntityDrawBlock instance = new Packet_ServerEntityDrawBlock();
		Packet_ServerEntityDrawBlockSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ServerEntityDrawBlock DeserializeBuffer(byte[] buffer, int length, Packet_ServerEntityDrawBlock instance)
	{
		Packet_ServerEntityDrawBlockSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ServerEntityDrawBlock Deserialize(CitoMemoryStream stream, Packet_ServerEntityDrawBlock instance)
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
				goto IL_0036;
			}
			if (keyInt != 8)
			{
				ProtocolParser.SkipKey(stream, Key.Create(keyInt));
			}
			else
			{
				instance.BlockType = ProtocolParser.ReadUInt32(stream);
			}
		}
		if (keyInt >= 0)
		{
			return null;
		}
		return instance;
		IL_0036:
		return null;
	}

	public static Packet_ServerEntityDrawBlock DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ServerEntityDrawBlock instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ServerEntityDrawBlock packet_ServerEntityDrawBlock = Packet_ServerEntityDrawBlockSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_ServerEntityDrawBlock;
	}

	public static void Serialize(CitoStream stream, Packet_ServerEntityDrawBlock instance)
	{
		if (instance.BlockType != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.BlockType);
		}
	}

	public static int GetSize(Packet_ServerEntityDrawBlock instance)
	{
		int size = 0;
		if (instance.BlockType != 0)
		{
			size += ProtocolParser.GetSize(instance.BlockType) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ServerEntityDrawBlock instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_ServerEntityDrawBlockSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_ServerEntityDrawBlock instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_ServerEntityDrawBlockSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ServerEntityDrawBlock instance)
	{
		byte[] data = Packet_ServerEntityDrawBlockSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
