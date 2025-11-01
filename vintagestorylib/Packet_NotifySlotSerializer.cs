using System;

public class Packet_NotifySlotSerializer
{
	public static Packet_NotifySlot DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_NotifySlot instance = new Packet_NotifySlot();
		Packet_NotifySlotSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_NotifySlot DeserializeBuffer(byte[] buffer, int length, Packet_NotifySlot instance)
	{
		Packet_NotifySlotSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_NotifySlot Deserialize(CitoMemoryStream stream, Packet_NotifySlot instance)
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
				goto IL_003C;
			}
			if (keyInt != 10)
			{
				if (keyInt != 16)
				{
					ProtocolParser.SkipKey(stream, Key.Create(keyInt));
				}
				else
				{
					instance.SlotId = ProtocolParser.ReadUInt32(stream);
				}
			}
			else
			{
				instance.InventoryId = ProtocolParser.ReadString(stream);
			}
		}
		if (keyInt >= 0)
		{
			return null;
		}
		return instance;
		IL_003C:
		return null;
	}

	public static Packet_NotifySlot DeserializeLengthDelimited(CitoMemoryStream stream, Packet_NotifySlot instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_NotifySlot packet_NotifySlot = Packet_NotifySlotSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_NotifySlot;
	}

	public static void Serialize(CitoStream stream, Packet_NotifySlot instance)
	{
		if (instance.InventoryId != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.InventoryId);
		}
		if (instance.SlotId != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.SlotId);
		}
	}

	public static int GetSize(Packet_NotifySlot instance)
	{
		int size = 0;
		if (instance.InventoryId != null)
		{
			size += ProtocolParser.GetSize(instance.InventoryId) + 1;
		}
		if (instance.SlotId != 0)
		{
			size += ProtocolParser.GetSize(instance.SlotId) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_NotifySlot instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_NotifySlotSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_NotifySlot instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_NotifySlotSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_NotifySlot instance)
	{
		byte[] data = Packet_NotifySlotSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
