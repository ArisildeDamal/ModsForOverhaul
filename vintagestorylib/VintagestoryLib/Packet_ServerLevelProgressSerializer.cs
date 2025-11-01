using System;

public class Packet_ServerLevelProgressSerializer
{
	public static Packet_ServerLevelProgress DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ServerLevelProgress instance = new Packet_ServerLevelProgress();
		Packet_ServerLevelProgressSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ServerLevelProgress DeserializeBuffer(byte[] buffer, int length, Packet_ServerLevelProgress instance)
	{
		Packet_ServerLevelProgressSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ServerLevelProgress Deserialize(CitoMemoryStream stream, Packet_ServerLevelProgress instance)
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
					goto IL_0048;
				}
				if (keyInt == 16)
				{
					instance.PercentComplete = ProtocolParser.ReadUInt32(stream);
					continue;
				}
			}
			else
			{
				if (keyInt == 26)
				{
					instance.Status = ProtocolParser.ReadString(stream);
					continue;
				}
				if (keyInt == 32)
				{
					instance.PercentCompleteSubitem = ProtocolParser.ReadUInt32(stream);
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
		IL_0048:
		return null;
	}

	public static Packet_ServerLevelProgress DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ServerLevelProgress instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ServerLevelProgress packet_ServerLevelProgress = Packet_ServerLevelProgressSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_ServerLevelProgress;
	}

	public static void Serialize(CitoStream stream, Packet_ServerLevelProgress instance)
	{
		if (instance.PercentComplete != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.PercentComplete);
		}
		if (instance.Status != null)
		{
			stream.WriteByte(26);
			ProtocolParser.WriteString(stream, instance.Status);
		}
		if (instance.PercentCompleteSubitem != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.PercentCompleteSubitem);
		}
	}

	public static int GetSize(Packet_ServerLevelProgress instance)
	{
		int size = 0;
		if (instance.PercentComplete != 0)
		{
			size += ProtocolParser.GetSize(instance.PercentComplete) + 1;
		}
		if (instance.Status != null)
		{
			size += ProtocolParser.GetSize(instance.Status) + 1;
		}
		if (instance.PercentCompleteSubitem != 0)
		{
			size += ProtocolParser.GetSize(instance.PercentCompleteSubitem) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ServerLevelProgress instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_ServerLevelProgressSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_ServerLevelProgress instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_ServerLevelProgressSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ServerLevelProgress instance)
	{
		byte[] data = Packet_ServerLevelProgressSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
