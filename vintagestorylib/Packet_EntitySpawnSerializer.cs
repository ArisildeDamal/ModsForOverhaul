using System;

public class Packet_EntitySpawnSerializer
{
	public static Packet_EntitySpawn DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_EntitySpawn instance = new Packet_EntitySpawn();
		Packet_EntitySpawnSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_EntitySpawn DeserializeBuffer(byte[] buffer, int length, Packet_EntitySpawn instance)
	{
		Packet_EntitySpawnSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_EntitySpawn Deserialize(CitoMemoryStream stream, Packet_EntitySpawn instance)
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
				instance.EntityAdd(Packet_EntitySerializer.DeserializeLengthDelimitedNew(stream));
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

	public static Packet_EntitySpawn DeserializeLengthDelimited(CitoMemoryStream stream, Packet_EntitySpawn instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_EntitySpawn packet_EntitySpawn = Packet_EntitySpawnSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_EntitySpawn;
	}

	public static void Serialize(CitoStream stream, Packet_EntitySpawn instance)
	{
		if (instance.Entity != null)
		{
			Packet_Entity[] elems = instance.Entity;
			int elemCount = instance.EntityCount;
			int i = 0;
			while (i < elems.Length && i < elemCount)
			{
				stream.WriteByte(10);
				Packet_EntitySerializer.SerializeWithSize(stream, elems[i]);
				i++;
			}
		}
	}

	public static int GetSize(Packet_EntitySpawn instance)
	{
		int size = 0;
		if (instance.Entity != null)
		{
			for (int i = 0; i < instance.EntityCount; i++)
			{
				int packetlength = Packet_EntitySerializer.GetSize(instance.Entity[i]);
				size += packetlength + ProtocolParser.GetSize(packetlength) + 1;
			}
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_EntitySpawn instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_EntitySpawnSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_EntitySpawn instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_EntitySpawnSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_EntitySpawn instance)
	{
		byte[] data = Packet_EntitySpawnSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
