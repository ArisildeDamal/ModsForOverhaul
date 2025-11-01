using System;

public class Packet_ServerSetBlockSerializer
{
	public static Packet_ServerSetBlock DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ServerSetBlock instance = new Packet_ServerSetBlock();
		Packet_ServerSetBlockSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ServerSetBlock DeserializeBuffer(byte[] buffer, int length, Packet_ServerSetBlock instance)
	{
		Packet_ServerSetBlockSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ServerSetBlock Deserialize(CitoMemoryStream stream, Packet_ServerSetBlock instance)
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
					goto IL_004B;
				}
				if (keyInt == 8)
				{
					instance.X = ProtocolParser.ReadUInt32(stream);
					continue;
				}
			}
			else
			{
				if (keyInt == 16)
				{
					instance.Y = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 24)
				{
					instance.Z = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 32)
				{
					instance.BlockType = ProtocolParser.ReadUInt32(stream);
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
		IL_004B:
		return null;
	}

	public static Packet_ServerSetBlock DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ServerSetBlock instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ServerSetBlock packet_ServerSetBlock = Packet_ServerSetBlockSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_ServerSetBlock;
	}

	public static void Serialize(CitoStream stream, Packet_ServerSetBlock instance)
	{
		if (instance.X != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.X);
		}
		if (instance.Y != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.Y);
		}
		if (instance.Z != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.Z);
		}
		if (instance.BlockType != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.BlockType);
		}
	}

	public static int GetSize(Packet_ServerSetBlock instance)
	{
		int size = 0;
		if (instance.X != 0)
		{
			size += ProtocolParser.GetSize(instance.X) + 1;
		}
		if (instance.Y != 0)
		{
			size += ProtocolParser.GetSize(instance.Y) + 1;
		}
		if (instance.Z != 0)
		{
			size += ProtocolParser.GetSize(instance.Z) + 1;
		}
		if (instance.BlockType != 0)
		{
			size += ProtocolParser.GetSize(instance.BlockType) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ServerSetBlock instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_ServerSetBlockSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_ServerSetBlock instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_ServerSetBlockSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ServerSetBlock instance)
	{
		byte[] data = Packet_ServerSetBlockSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
