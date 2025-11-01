using System;

public class Packet_ChatLineSerializer
{
	public static Packet_ChatLine DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ChatLine instance = new Packet_ChatLine();
		Packet_ChatLineSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ChatLine DeserializeBuffer(byte[] buffer, int length, Packet_ChatLine instance)
	{
		Packet_ChatLineSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ChatLine Deserialize(CitoMemoryStream stream, Packet_ChatLine instance)
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
			if (keyInt <= 10)
			{
				if (keyInt == 0)
				{
					goto IL_004D;
				}
				if (keyInt == 10)
				{
					instance.Message = ProtocolParser.ReadString(stream);
					continue;
				}
			}
			else
			{
				if (keyInt == 16)
				{
					instance.Groupid = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 24)
				{
					instance.ChatType = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 34)
				{
					instance.Data = ProtocolParser.ReadString(stream);
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
		IL_004D:
		return null;
	}

	public static Packet_ChatLine DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ChatLine instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ChatLine packet_ChatLine = Packet_ChatLineSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_ChatLine;
	}

	public static void Serialize(CitoStream stream, Packet_ChatLine instance)
	{
		if (instance.Message != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.Message);
		}
		if (instance.Groupid != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.Groupid);
		}
		if (instance.ChatType != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.ChatType);
		}
		if (instance.Data != null)
		{
			stream.WriteByte(34);
			ProtocolParser.WriteString(stream, instance.Data);
		}
	}

	public static int GetSize(Packet_ChatLine instance)
	{
		int size = 0;
		if (instance.Message != null)
		{
			size += ProtocolParser.GetSize(instance.Message) + 1;
		}
		if (instance.Groupid != 0)
		{
			size += ProtocolParser.GetSize(instance.Groupid) + 1;
		}
		if (instance.ChatType != 0)
		{
			size += ProtocolParser.GetSize(instance.ChatType) + 1;
		}
		if (instance.Data != null)
		{
			size += ProtocolParser.GetSize(instance.Data) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ChatLine instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_ChatLineSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_ChatLine instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_ChatLineSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ChatLine instance)
	{
		byte[] data = Packet_ChatLineSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
