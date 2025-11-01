using System;

public class Packet_EntityAttributeUpdateSerializer
{
	public static Packet_EntityAttributeUpdate DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_EntityAttributeUpdate instance = new Packet_EntityAttributeUpdate();
		Packet_EntityAttributeUpdateSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_EntityAttributeUpdate DeserializeBuffer(byte[] buffer, int length, Packet_EntityAttributeUpdate instance)
	{
		Packet_EntityAttributeUpdateSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_EntityAttributeUpdate Deserialize(CitoMemoryStream stream, Packet_EntityAttributeUpdate instance)
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
				if (keyInt != 18)
				{
					ProtocolParser.SkipKey(stream, Key.Create(keyInt));
				}
				else
				{
					instance.AttributesAdd(Packet_PartialAttributeSerializer.DeserializeLengthDelimitedNew(stream));
				}
			}
			else
			{
				instance.EntityId = ProtocolParser.ReadUInt64(stream);
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

	public static Packet_EntityAttributeUpdate DeserializeLengthDelimited(CitoMemoryStream stream, Packet_EntityAttributeUpdate instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_EntityAttributeUpdate packet_EntityAttributeUpdate = Packet_EntityAttributeUpdateSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_EntityAttributeUpdate;
	}

	public static void Serialize(CitoStream stream, Packet_EntityAttributeUpdate instance)
	{
		if (instance.EntityId != 0L)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt64(stream, instance.EntityId);
		}
		if (instance.Attributes != null)
		{
			Packet_PartialAttribute[] elems = instance.Attributes;
			int elemCount = instance.AttributesCount;
			int i = 0;
			while (i < elems.Length && i < elemCount)
			{
				stream.WriteByte(18);
				Packet_PartialAttributeSerializer.SerializeWithSize(stream, elems[i]);
				i++;
			}
		}
	}

	public static int GetSize(Packet_EntityAttributeUpdate instance)
	{
		int size = 0;
		if (instance.EntityId != 0L)
		{
			size += ProtocolParser.GetSize(instance.EntityId) + 1;
		}
		if (instance.Attributes != null)
		{
			for (int i = 0; i < instance.AttributesCount; i++)
			{
				int packetlength = Packet_PartialAttributeSerializer.GetSize(instance.Attributes[i]);
				size += packetlength + ProtocolParser.GetSize(packetlength) + 1;
			}
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_EntityAttributeUpdate instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_EntityAttributeUpdateSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_EntityAttributeUpdate instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_EntityAttributeUpdateSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_EntityAttributeUpdate instance)
	{
		byte[] data = Packet_EntityAttributeUpdateSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
