using System;

public class Packet_ConnectionPacketSerializer
{
	public static Packet_ConnectionPacket DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ConnectionPacket instance = new Packet_ConnectionPacket();
		Packet_ConnectionPacketSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ConnectionPacket DeserializeBuffer(byte[] buffer, int length, Packet_ConnectionPacket instance)
	{
		Packet_ConnectionPacketSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ConnectionPacket Deserialize(CitoMemoryStream stream, Packet_ConnectionPacket instance)
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
				goto IL_0037;
			}
			if (keyInt != 10)
			{
				ProtocolParser.SkipKey(stream, Key.Create(keyInt));
			}
			else
			{
				instance.LoginToken = ProtocolParser.ReadString(stream);
			}
		}
		if (keyInt >= 0)
		{
			return null;
		}
		return instance;
		IL_0037:
		return null;
	}

	public static Packet_ConnectionPacket DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ConnectionPacket instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ConnectionPacket packet_ConnectionPacket = Packet_ConnectionPacketSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_ConnectionPacket;
	}

	public static void Serialize(CitoStream stream, Packet_ConnectionPacket instance)
	{
		if (instance.LoginToken != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.LoginToken);
		}
	}

	public static int GetSize(Packet_ConnectionPacket instance)
	{
		int size = 0;
		if (instance.LoginToken != null)
		{
			size += ProtocolParser.GetSize(instance.LoginToken) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ConnectionPacket instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_ConnectionPacketSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_ConnectionPacket instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_ConnectionPacketSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ConnectionPacket instance)
	{
		byte[] data = Packet_ConnectionPacketSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
