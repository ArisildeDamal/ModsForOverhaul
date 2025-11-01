using System;

public class Packet_EntityAttributesSerializer
{
	public static Packet_EntityAttributes DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_EntityAttributes instance = new Packet_EntityAttributes();
		Packet_EntityAttributesSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_EntityAttributes DeserializeBuffer(byte[] buffer, int length, Packet_EntityAttributes instance)
	{
		Packet_EntityAttributesSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_EntityAttributes Deserialize(CitoMemoryStream stream, Packet_EntityAttributes instance)
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
					instance.Data = ProtocolParser.ReadBytes(stream);
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

	public static Packet_EntityAttributes DeserializeLengthDelimited(CitoMemoryStream stream, Packet_EntityAttributes instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_EntityAttributes packet_EntityAttributes = Packet_EntityAttributesSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_EntityAttributes;
	}

	public static void Serialize(CitoStream stream, Packet_EntityAttributes instance)
	{
		if (instance.EntityId != 0L)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt64(stream, instance.EntityId);
		}
		if (instance.Data != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteBytes(stream, instance.Data);
		}
	}

	public static int GetSize(Packet_EntityAttributes instance)
	{
		int size = 0;
		if (instance.EntityId != 0L)
		{
			size += ProtocolParser.GetSize(instance.EntityId) + 1;
		}
		if (instance.Data != null)
		{
			size += ProtocolParser.GetSize(instance.Data) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_EntityAttributes instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_EntityAttributesSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_EntityAttributes instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_EntityAttributesSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_EntityAttributes instance)
	{
		byte[] data = Packet_EntityAttributesSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
