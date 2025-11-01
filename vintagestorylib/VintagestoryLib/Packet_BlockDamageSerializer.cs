using System;

public class Packet_BlockDamageSerializer
{
	public static Packet_BlockDamage DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_BlockDamage instance = new Packet_BlockDamage();
		Packet_BlockDamageSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_BlockDamage DeserializeBuffer(byte[] buffer, int length, Packet_BlockDamage instance)
	{
		Packet_BlockDamageSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_BlockDamage Deserialize(CitoMemoryStream stream, Packet_BlockDamage instance)
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
					goto IL_0054;
				}
				if (keyInt == 8)
				{
					instance.PosX = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 16)
				{
					instance.PosY = ProtocolParser.ReadUInt32(stream);
					continue;
				}
			}
			else
			{
				if (keyInt == 24)
				{
					instance.PosZ = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 32)
				{
					instance.Facing = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 40)
				{
					instance.Damage = ProtocolParser.ReadUInt32(stream);
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
		IL_0054:
		return null;
	}

	public static Packet_BlockDamage DeserializeLengthDelimited(CitoMemoryStream stream, Packet_BlockDamage instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_BlockDamage packet_BlockDamage = Packet_BlockDamageSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_BlockDamage;
	}

	public static void Serialize(CitoStream stream, Packet_BlockDamage instance)
	{
		if (instance.PosX != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.PosX);
		}
		if (instance.PosY != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.PosY);
		}
		if (instance.PosZ != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.PosZ);
		}
		if (instance.Facing != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.Facing);
		}
		if (instance.Damage != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.Damage);
		}
	}

	public static int GetSize(Packet_BlockDamage instance)
	{
		int size = 0;
		if (instance.PosX != 0)
		{
			size += ProtocolParser.GetSize(instance.PosX) + 1;
		}
		if (instance.PosY != 0)
		{
			size += ProtocolParser.GetSize(instance.PosY) + 1;
		}
		if (instance.PosZ != 0)
		{
			size += ProtocolParser.GetSize(instance.PosZ) + 1;
		}
		if (instance.Facing != 0)
		{
			size += ProtocolParser.GetSize(instance.Facing) + 1;
		}
		if (instance.Damage != 0)
		{
			size += ProtocolParser.GetSize(instance.Damage) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_BlockDamage instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_BlockDamageSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_BlockDamage instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_BlockDamageSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_BlockDamage instance)
	{
		byte[] data = Packet_BlockDamageSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
