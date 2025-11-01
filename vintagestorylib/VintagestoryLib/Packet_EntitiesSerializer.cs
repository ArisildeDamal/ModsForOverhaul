using System;

public class Packet_EntitiesSerializer
{
	public static Packet_Entities DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_Entities instance = new Packet_Entities();
		Packet_EntitiesSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_Entities DeserializeBuffer(byte[] buffer, int length, Packet_Entities instance)
	{
		Packet_EntitiesSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_Entities Deserialize(CitoMemoryStream stream, Packet_Entities instance)
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
				instance.EntitiesAdd(Packet_EntitySerializer.DeserializeLengthDelimitedNew(stream));
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

	public static Packet_Entities DeserializeLengthDelimited(CitoMemoryStream stream, Packet_Entities instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_Entities packet_Entities = Packet_EntitiesSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_Entities;
	}

	public static void Serialize(CitoStream stream, Packet_Entities instance)
	{
		if (instance.Entities != null)
		{
			Packet_Entity[] elems = instance.Entities;
			int elemCount = instance.EntitiesCount;
			int i = 0;
			while (i < elems.Length && i < elemCount)
			{
				stream.WriteByte(10);
				Packet_EntitySerializer.SerializeWithSize(stream, elems[i]);
				i++;
			}
		}
	}

	public static int GetSize(Packet_Entities instance)
	{
		int size = 0;
		if (instance.Entities != null)
		{
			for (int i = 0; i < instance.EntitiesCount; i++)
			{
				int packetlength = Packet_EntitySerializer.GetSize(instance.Entities[i]);
				size += packetlength + ProtocolParser.GetSize(packetlength) + 1;
			}
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_Entities instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_EntitiesSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_Entities instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_EntitiesSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_Entities instance)
	{
		byte[] data = Packet_EntitiesSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
