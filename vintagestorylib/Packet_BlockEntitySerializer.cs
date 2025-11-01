using System;

public class Packet_BlockEntitySerializer
{
	public static Packet_BlockEntity DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_BlockEntity instance = new Packet_BlockEntity();
		Packet_BlockEntitySerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_BlockEntity DeserializeBuffer(byte[] buffer, int length, Packet_BlockEntity instance)
	{
		Packet_BlockEntitySerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_BlockEntity Deserialize(CitoMemoryStream stream, Packet_BlockEntity instance)
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
			if (keyInt <= 16)
			{
				if (keyInt == 0)
				{
					goto IL_0055;
				}
				if (keyInt == 10)
				{
					instance.Classname = ProtocolParser.ReadString(stream);
					continue;
				}
				if (keyInt == 16)
				{
					instance.PosX = ProtocolParser.ReadUInt32(stream);
					continue;
				}
			}
			else
			{
				if (keyInt == 24)
				{
					instance.PosY = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 32)
				{
					instance.PosZ = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 42)
				{
					instance.Data = ProtocolParser.ReadBytes(stream);
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
		IL_0055:
		return null;
	}

	public static Packet_BlockEntity DeserializeLengthDelimited(CitoMemoryStream stream, Packet_BlockEntity instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_BlockEntity packet_BlockEntity = Packet_BlockEntitySerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_BlockEntity;
	}

	public static void Serialize(CitoStream stream, Packet_BlockEntity instance)
	{
		if (instance.Classname != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.Classname);
		}
		if (instance.PosX != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.PosX);
		}
		if (instance.PosY != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.PosY);
		}
		if (instance.PosZ != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.PosZ);
		}
		if (instance.Data != null)
		{
			stream.WriteByte(42);
			ProtocolParser.WriteBytes(stream, instance.Data);
		}
	}

	public static int GetSize(Packet_BlockEntity instance)
	{
		int size = 0;
		if (instance.Classname != null)
		{
			size += ProtocolParser.GetSize(instance.Classname) + 1;
		}
		if (instance.PosX != 0)
		{
			size += ProtocolParser.GetSize(instance.PosX) + 1;
		}
		if (instance.PosY != 0)
		{
			size += ProtocolParser.GetSize(instance.PosY) + 1;
		}
		if (instance.PosZ != 0)
		{
			size += ProtocolParser.GetSize(instance.PosZ) + 1;
		}
		if (instance.Data != null)
		{
			size += ProtocolParser.GetSize(instance.Data) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_BlockEntity instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_BlockEntitySerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_BlockEntity instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_BlockEntitySerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_BlockEntity instance)
	{
		byte[] data = Packet_BlockEntitySerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
