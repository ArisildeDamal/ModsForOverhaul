using System;

public class Packet_HeldSoundSetSerializer
{
	public static Packet_HeldSoundSet DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_HeldSoundSet instance = new Packet_HeldSoundSet();
		Packet_HeldSoundSetSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_HeldSoundSet DeserializeBuffer(byte[] buffer, int length, Packet_HeldSoundSet instance)
	{
		Packet_HeldSoundSetSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_HeldSoundSet Deserialize(CitoMemoryStream stream, Packet_HeldSoundSet instance)
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
			if (keyInt <= 18)
			{
				if (keyInt == 0)
				{
					goto IL_0061;
				}
				if (keyInt == 10)
				{
					instance.Idle = ProtocolParser.ReadString(stream);
					continue;
				}
				if (keyInt == 18)
				{
					instance.Equip = ProtocolParser.ReadString(stream);
					continue;
				}
			}
			else if (keyInt <= 34)
			{
				if (keyInt == 26)
				{
					instance.Unequip = ProtocolParser.ReadString(stream);
					continue;
				}
				if (keyInt == 34)
				{
					instance.Attack = ProtocolParser.ReadString(stream);
					continue;
				}
			}
			else
			{
				if (keyInt == 42)
				{
					instance.InvPickup = ProtocolParser.ReadString(stream);
					continue;
				}
				if (keyInt == 50)
				{
					instance.InvPlace = ProtocolParser.ReadString(stream);
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
		IL_0061:
		return null;
	}

	public static Packet_HeldSoundSet DeserializeLengthDelimited(CitoMemoryStream stream, Packet_HeldSoundSet instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_HeldSoundSet packet_HeldSoundSet = Packet_HeldSoundSetSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_HeldSoundSet;
	}

	public static void Serialize(CitoStream stream, Packet_HeldSoundSet instance)
	{
		if (instance.Idle != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.Idle);
		}
		if (instance.Equip != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteString(stream, instance.Equip);
		}
		if (instance.Unequip != null)
		{
			stream.WriteByte(26);
			ProtocolParser.WriteString(stream, instance.Unequip);
		}
		if (instance.Attack != null)
		{
			stream.WriteByte(34);
			ProtocolParser.WriteString(stream, instance.Attack);
		}
		if (instance.InvPickup != null)
		{
			stream.WriteByte(42);
			ProtocolParser.WriteString(stream, instance.InvPickup);
		}
		if (instance.InvPlace != null)
		{
			stream.WriteByte(50);
			ProtocolParser.WriteString(stream, instance.InvPlace);
		}
	}

	public static int GetSize(Packet_HeldSoundSet instance)
	{
		int size = 0;
		if (instance.Idle != null)
		{
			size += ProtocolParser.GetSize(instance.Idle) + 1;
		}
		if (instance.Equip != null)
		{
			size += ProtocolParser.GetSize(instance.Equip) + 1;
		}
		if (instance.Unequip != null)
		{
			size += ProtocolParser.GetSize(instance.Unequip) + 1;
		}
		if (instance.Attack != null)
		{
			size += ProtocolParser.GetSize(instance.Attack) + 1;
		}
		if (instance.InvPickup != null)
		{
			size += ProtocolParser.GetSize(instance.InvPickup) + 1;
		}
		if (instance.InvPlace != null)
		{
			size += ProtocolParser.GetSize(instance.InvPlace) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_HeldSoundSet instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_HeldSoundSetSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_HeldSoundSet instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_HeldSoundSetSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_HeldSoundSet instance)
	{
		byte[] data = Packet_HeldSoundSetSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
