using System;

public class Packet_BlockEntityMessageSerializer
{
	public static Packet_BlockEntityMessage DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_BlockEntityMessage instance = new Packet_BlockEntityMessage();
		Packet_BlockEntityMessageSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_BlockEntityMessage DeserializeBuffer(byte[] buffer, int length, Packet_BlockEntityMessage instance)
	{
		Packet_BlockEntityMessageSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_BlockEntityMessage Deserialize(CitoMemoryStream stream, Packet_BlockEntityMessage instance)
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
					goto IL_0054;
				}
				if (keyInt == 8)
				{
					instance.X = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 16)
				{
					instance.Y = ProtocolParser.ReadUInt32(stream);
					continue;
				}
			}
			else
			{
				if (keyInt == 24)
				{
					instance.Z = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 32)
				{
					instance.PacketId = ProtocolParser.ReadUInt32(stream);
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
		IL_0054:
		return null;
	}

	public static Packet_BlockEntityMessage DeserializeLengthDelimited(CitoMemoryStream stream, Packet_BlockEntityMessage instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_BlockEntityMessage packet_BlockEntityMessage = Packet_BlockEntityMessageSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_BlockEntityMessage;
	}

	public static void Serialize(CitoStream stream, Packet_BlockEntityMessage instance)
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
		if (instance.PacketId != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.PacketId);
		}
		if (instance.Data != null)
		{
			stream.WriteByte(42);
			ProtocolParser.WriteBytes(stream, instance.Data);
		}
	}

	public static int GetSize(Packet_BlockEntityMessage instance)
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
		if (instance.PacketId != 0)
		{
			size += ProtocolParser.GetSize(instance.PacketId) + 1;
		}
		if (instance.Data != null)
		{
			size += ProtocolParser.GetSize(instance.Data) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_BlockEntityMessage instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_BlockEntityMessageSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_BlockEntityMessage instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_BlockEntityMessageSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_BlockEntityMessage instance)
	{
		byte[] data = Packet_BlockEntityMessageSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
