using System;

public class Packet_EntityPacketSerializer
{
	public static Packet_EntityPacket DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_EntityPacket instance = new Packet_EntityPacket();
		Packet_EntityPacketSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_EntityPacket DeserializeBuffer(byte[] buffer, int length, Packet_EntityPacket instance)
	{
		Packet_EntityPacketSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_EntityPacket Deserialize(CitoMemoryStream stream, Packet_EntityPacket instance)
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
					instance.EntityId = ProtocolParser.ReadUInt64(stream);
					continue;
				}
			}
			else
			{
				if (keyInt == 16)
				{
					instance.Packetid = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_EntityPacket DeserializeLengthDelimited(CitoMemoryStream stream, Packet_EntityPacket instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_EntityPacket packet_EntityPacket = Packet_EntityPacketSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_EntityPacket;
	}

	public static void Serialize(CitoStream stream, Packet_EntityPacket instance)
	{
		if (instance.EntityId != 0L)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt64(stream, instance.EntityId);
		}
		if (instance.Packetid != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.Packetid);
		}
		if (instance.Data != null)
		{
			stream.WriteByte(26);
			ProtocolParser.WriteBytes(stream, instance.Data);
		}
	}

	public static int GetSize(Packet_EntityPacket instance)
	{
		int size = 0;
		if (instance.EntityId != 0L)
		{
			size += ProtocolParser.GetSize(instance.EntityId) + 1;
		}
		if (instance.Packetid != 0)
		{
			size += ProtocolParser.GetSize(instance.Packetid) + 1;
		}
		if (instance.Data != null)
		{
			size += ProtocolParser.GetSize(instance.Data) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_EntityPacket instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_EntityPacketSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_EntityPacket instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_EntityPacketSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_EntityPacket instance)
	{
		byte[] data = Packet_EntityPacketSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
