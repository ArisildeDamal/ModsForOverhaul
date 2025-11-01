using System;

public class Packet_CustomPacketSerializer
{
	public static Packet_CustomPacket DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_CustomPacket instance = new Packet_CustomPacket();
		Packet_CustomPacketSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_CustomPacket DeserializeBuffer(byte[] buffer, int length, Packet_CustomPacket instance)
	{
		Packet_CustomPacketSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_CustomPacket Deserialize(CitoMemoryStream stream, Packet_CustomPacket instance)
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
					goto IL_0046;
				}
				if (keyInt == 8)
				{
					instance.ChannelId = ProtocolParser.ReadUInt32(stream);
					continue;
				}
			}
			else
			{
				if (keyInt == 16)
				{
					instance.MessageId = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 26)
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
		IL_0046:
		return null;
	}

	public static Packet_CustomPacket DeserializeLengthDelimited(CitoMemoryStream stream, Packet_CustomPacket instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_CustomPacket packet_CustomPacket = Packet_CustomPacketSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_CustomPacket;
	}

	public static void Serialize(CitoStream stream, Packet_CustomPacket instance)
	{
		if (instance.ChannelId != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.ChannelId);
		}
		if (instance.MessageId != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.MessageId);
		}
		if (instance.Data != null)
		{
			stream.WriteByte(26);
			ProtocolParser.WriteBytes(stream, instance.Data);
		}
	}

	public static int GetSize(Packet_CustomPacket instance)
	{
		int size = 0;
		if (instance.ChannelId != 0)
		{
			size += ProtocolParser.GetSize(instance.ChannelId) + 1;
		}
		if (instance.MessageId != 0)
		{
			size += ProtocolParser.GetSize(instance.MessageId) + 1;
		}
		if (instance.Data != null)
		{
			size += ProtocolParser.GetSize(instance.Data) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_CustomPacket instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_CustomPacketSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_CustomPacket instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_CustomPacketSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_CustomPacket instance)
	{
		byte[] data = Packet_CustomPacketSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
