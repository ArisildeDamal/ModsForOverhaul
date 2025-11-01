using System;

public class Packet_QueuePacketSerializer
{
	public static Packet_QueuePacket DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_QueuePacket instance = new Packet_QueuePacket();
		Packet_QueuePacketSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_QueuePacket DeserializeBuffer(byte[] buffer, int length, Packet_QueuePacket instance)
	{
		Packet_QueuePacketSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_QueuePacket Deserialize(CitoMemoryStream stream, Packet_QueuePacket instance)
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
				instance.Position = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_QueuePacket DeserializeLengthDelimited(CitoMemoryStream stream, Packet_QueuePacket instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_QueuePacket packet_QueuePacket = Packet_QueuePacketSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_QueuePacket;
	}

	public static void Serialize(CitoStream stream, Packet_QueuePacket instance)
	{
		if (instance.Position != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.Position);
		}
	}

	public static int GetSize(Packet_QueuePacket instance)
	{
		int size = 0;
		if (instance.Position != 0)
		{
			size += ProtocolParser.GetSize(instance.Position) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_QueuePacket instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_QueuePacketSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_QueuePacket instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_QueuePacketSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_QueuePacket instance)
	{
		byte[] data = Packet_QueuePacketSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
