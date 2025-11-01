using System;

public class Packet_ServerLevelInitializeSerializer
{
	public static Packet_ServerLevelInitialize DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ServerLevelInitialize instance = new Packet_ServerLevelInitialize();
		Packet_ServerLevelInitializeSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ServerLevelInitialize DeserializeBuffer(byte[] buffer, int length, Packet_ServerLevelInitialize instance)
	{
		Packet_ServerLevelInitializeSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ServerLevelInitialize Deserialize(CitoMemoryStream stream, Packet_ServerLevelInitialize instance)
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
					instance.ServerChunkSize = ProtocolParser.ReadUInt32(stream);
					continue;
				}
			}
			else
			{
				if (keyInt == 16)
				{
					instance.ServerMapChunkSize = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 24)
				{
					instance.ServerMapRegionSize = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 32)
				{
					instance.MaxViewDistance = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_ServerLevelInitialize DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ServerLevelInitialize instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ServerLevelInitialize packet_ServerLevelInitialize = Packet_ServerLevelInitializeSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_ServerLevelInitialize;
	}

	public static void Serialize(CitoStream stream, Packet_ServerLevelInitialize instance)
	{
		if (instance.ServerChunkSize != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.ServerChunkSize);
		}
		if (instance.ServerMapChunkSize != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.ServerMapChunkSize);
		}
		if (instance.ServerMapRegionSize != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.ServerMapRegionSize);
		}
		if (instance.MaxViewDistance != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.MaxViewDistance);
		}
	}

	public static int GetSize(Packet_ServerLevelInitialize instance)
	{
		int size = 0;
		if (instance.ServerChunkSize != 0)
		{
			size += ProtocolParser.GetSize(instance.ServerChunkSize) + 1;
		}
		if (instance.ServerMapChunkSize != 0)
		{
			size += ProtocolParser.GetSize(instance.ServerMapChunkSize) + 1;
		}
		if (instance.ServerMapRegionSize != 0)
		{
			size += ProtocolParser.GetSize(instance.ServerMapRegionSize) + 1;
		}
		if (instance.MaxViewDistance != 0)
		{
			size += ProtocolParser.GetSize(instance.MaxViewDistance) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ServerLevelInitialize instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_ServerLevelInitializeSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_ServerLevelInitialize instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_ServerLevelInitializeSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ServerLevelInitialize instance)
	{
		byte[] data = Packet_ServerLevelInitializeSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
