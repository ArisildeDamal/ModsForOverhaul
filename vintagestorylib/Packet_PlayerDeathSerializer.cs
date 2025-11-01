using System;

public class Packet_PlayerDeathSerializer
{
	public static Packet_PlayerDeath DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_PlayerDeath instance = new Packet_PlayerDeath();
		Packet_PlayerDeathSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_PlayerDeath DeserializeBuffer(byte[] buffer, int length, Packet_PlayerDeath instance)
	{
		Packet_PlayerDeathSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_PlayerDeath Deserialize(CitoMemoryStream stream, Packet_PlayerDeath instance)
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
					instance.LivesLeft = ProtocolParser.ReadUInt32(stream);
				}
			}
			else
			{
				instance.ClientId = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_PlayerDeath DeserializeLengthDelimited(CitoMemoryStream stream, Packet_PlayerDeath instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_PlayerDeath packet_PlayerDeath = Packet_PlayerDeathSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_PlayerDeath;
	}

	public static void Serialize(CitoStream stream, Packet_PlayerDeath instance)
	{
		if (instance.ClientId != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.ClientId);
		}
		if (instance.LivesLeft != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.LivesLeft);
		}
	}

	public static int GetSize(Packet_PlayerDeath instance)
	{
		int size = 0;
		if (instance.ClientId != 0)
		{
			size += ProtocolParser.GetSize(instance.ClientId) + 1;
		}
		if (instance.LivesLeft != 0)
		{
			size += ProtocolParser.GetSize(instance.LivesLeft) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_PlayerDeath instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_PlayerDeathSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_PlayerDeath instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_PlayerDeathSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_PlayerDeath instance)
	{
		byte[] data = Packet_PlayerDeathSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
