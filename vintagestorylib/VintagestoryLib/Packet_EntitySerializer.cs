using System;

public class Packet_EntitySerializer
{
	public static Packet_Entity DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_Entity instance = new Packet_Entity();
		Packet_EntitySerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_Entity DeserializeBuffer(byte[] buffer, int length, Packet_Entity instance)
	{
		Packet_EntitySerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_Entity Deserialize(CitoMemoryStream stream, Packet_Entity instance)
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
			if (keyInt <= 10)
			{
				if (keyInt == 0)
				{
					goto IL_004D;
				}
				if (keyInt == 10)
				{
					instance.EntityType = ProtocolParser.ReadString(stream);
					continue;
				}
			}
			else
			{
				if (keyInt == 16)
				{
					instance.EntityId = ProtocolParser.ReadUInt64(stream);
					continue;
				}
				if (keyInt == 24)
				{
					instance.SimulationRange = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 34)
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
		IL_004D:
		return null;
	}

	public static Packet_Entity DeserializeLengthDelimited(CitoMemoryStream stream, Packet_Entity instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_Entity packet_Entity = Packet_EntitySerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_Entity;
	}

	public static void Serialize(CitoStream stream, Packet_Entity instance)
	{
		if (instance.EntityType != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.EntityType);
		}
		if (instance.EntityId != 0L)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt64(stream, instance.EntityId);
		}
		if (instance.SimulationRange != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.SimulationRange);
		}
		if (instance.Data != null)
		{
			stream.WriteByte(34);
			ProtocolParser.WriteBytes(stream, instance.Data);
		}
	}

	public static int GetSize(Packet_Entity instance)
	{
		int size = 0;
		if (instance.EntityType != null)
		{
			size += ProtocolParser.GetSize(instance.EntityType) + 1;
		}
		if (instance.EntityId != 0L)
		{
			size += ProtocolParser.GetSize(instance.EntityId) + 1;
		}
		if (instance.SimulationRange != 0)
		{
			size += ProtocolParser.GetSize(instance.SimulationRange) + 1;
		}
		if (instance.Data != null)
		{
			size += ProtocolParser.GetSize(instance.Data) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_Entity instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_EntitySerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_Entity instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_EntitySerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_Entity instance)
	{
		byte[] data = Packet_EntitySerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
