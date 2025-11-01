using System;

public class Packet_InventoryUpdateSerializer
{
	public static Packet_InventoryUpdate DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_InventoryUpdate instance = new Packet_InventoryUpdate();
		Packet_InventoryUpdateSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_InventoryUpdate DeserializeBuffer(byte[] buffer, int length, Packet_InventoryUpdate instance)
	{
		Packet_InventoryUpdateSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_InventoryUpdate Deserialize(CitoMemoryStream stream, Packet_InventoryUpdate instance)
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
					goto IL_004E;
				}
				if (keyInt == 8)
				{
					instance.ClientId = ProtocolParser.ReadUInt32(stream);
					continue;
				}
			}
			else
			{
				if (keyInt == 18)
				{
					instance.InventoryId = ProtocolParser.ReadString(stream);
					continue;
				}
				if (keyInt == 24)
				{
					instance.SlotId = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 34)
				{
					if (instance.ItemStack == null)
					{
						instance.ItemStack = Packet_ItemStackSerializer.DeserializeLengthDelimitedNew(stream);
						continue;
					}
					Packet_ItemStackSerializer.DeserializeLengthDelimited(stream, instance.ItemStack);
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
		IL_004E:
		return null;
	}

	public static Packet_InventoryUpdate DeserializeLengthDelimited(CitoMemoryStream stream, Packet_InventoryUpdate instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_InventoryUpdate packet_InventoryUpdate = Packet_InventoryUpdateSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_InventoryUpdate;
	}

	public static void Serialize(CitoStream stream, Packet_InventoryUpdate instance)
	{
		if (instance.ClientId != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.ClientId);
		}
		if (instance.InventoryId != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteString(stream, instance.InventoryId);
		}
		if (instance.SlotId != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.SlotId);
		}
		if (instance.ItemStack != null)
		{
			stream.WriteByte(34);
			Packet_ItemStackSerializer.SerializeWithSize(stream, instance.ItemStack);
		}
	}

	public static int GetSize(Packet_InventoryUpdate instance)
	{
		int size = 0;
		if (instance.ClientId != 0)
		{
			size += ProtocolParser.GetSize(instance.ClientId) + 1;
		}
		if (instance.InventoryId != null)
		{
			size += ProtocolParser.GetSize(instance.InventoryId) + 1;
		}
		if (instance.SlotId != 0)
		{
			size += ProtocolParser.GetSize(instance.SlotId) + 1;
		}
		if (instance.ItemStack != null)
		{
			int packetlength = Packet_ItemStackSerializer.GetSize(instance.ItemStack);
			size += packetlength + ProtocolParser.GetSize(packetlength) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_InventoryUpdate instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_InventoryUpdateSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_InventoryUpdate instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_InventoryUpdateSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_InventoryUpdate instance)
	{
		byte[] data = Packet_InventoryUpdateSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
