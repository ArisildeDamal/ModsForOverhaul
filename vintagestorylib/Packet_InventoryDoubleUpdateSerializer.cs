using System;

public class Packet_InventoryDoubleUpdateSerializer
{
	public static Packet_InventoryDoubleUpdate DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_InventoryDoubleUpdate instance = new Packet_InventoryDoubleUpdate();
		Packet_InventoryDoubleUpdateSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_InventoryDoubleUpdate DeserializeBuffer(byte[] buffer, int length, Packet_InventoryDoubleUpdate instance)
	{
		Packet_InventoryDoubleUpdateSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_InventoryDoubleUpdate Deserialize(CitoMemoryStream stream, Packet_InventoryDoubleUpdate instance)
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
			if (keyInt <= 26)
			{
				if (keyInt <= 8)
				{
					if (keyInt == 0)
					{
						goto IL_007A;
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
						instance.InventoryId1 = ProtocolParser.ReadString(stream);
						continue;
					}
					if (keyInt == 26)
					{
						instance.InventoryId2 = ProtocolParser.ReadString(stream);
						continue;
					}
				}
			}
			else if (keyInt <= 40)
			{
				if (keyInt == 32)
				{
					instance.SlotId1 = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 40)
				{
					instance.SlotId2 = ProtocolParser.ReadUInt32(stream);
					continue;
				}
			}
			else if (keyInt != 50)
			{
				if (keyInt == 58)
				{
					if (instance.ItemStack2 == null)
					{
						instance.ItemStack2 = Packet_ItemStackSerializer.DeserializeLengthDelimitedNew(stream);
						continue;
					}
					Packet_ItemStackSerializer.DeserializeLengthDelimited(stream, instance.ItemStack2);
					continue;
				}
			}
			else
			{
				if (instance.ItemStack1 == null)
				{
					instance.ItemStack1 = Packet_ItemStackSerializer.DeserializeLengthDelimitedNew(stream);
					continue;
				}
				Packet_ItemStackSerializer.DeserializeLengthDelimited(stream, instance.ItemStack1);
				continue;
			}
			ProtocolParser.SkipKey(stream, Key.Create(keyInt));
		}
		if (keyInt >= 0)
		{
			return null;
		}
		return instance;
		IL_007A:
		return null;
	}

	public static Packet_InventoryDoubleUpdate DeserializeLengthDelimited(CitoMemoryStream stream, Packet_InventoryDoubleUpdate instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_InventoryDoubleUpdate packet_InventoryDoubleUpdate = Packet_InventoryDoubleUpdateSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_InventoryDoubleUpdate;
	}

	public static void Serialize(CitoStream stream, Packet_InventoryDoubleUpdate instance)
	{
		if (instance.ClientId != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.ClientId);
		}
		if (instance.InventoryId1 != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteString(stream, instance.InventoryId1);
		}
		if (instance.InventoryId2 != null)
		{
			stream.WriteByte(26);
			ProtocolParser.WriteString(stream, instance.InventoryId2);
		}
		if (instance.SlotId1 != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.SlotId1);
		}
		if (instance.SlotId2 != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.SlotId2);
		}
		if (instance.ItemStack1 != null)
		{
			stream.WriteByte(50);
			Packet_ItemStackSerializer.SerializeWithSize(stream, instance.ItemStack1);
		}
		if (instance.ItemStack2 != null)
		{
			stream.WriteByte(58);
			Packet_ItemStackSerializer.SerializeWithSize(stream, instance.ItemStack2);
		}
	}

	public static int GetSize(Packet_InventoryDoubleUpdate instance)
	{
		int size = 0;
		if (instance.ClientId != 0)
		{
			size += ProtocolParser.GetSize(instance.ClientId) + 1;
		}
		if (instance.InventoryId1 != null)
		{
			size += ProtocolParser.GetSize(instance.InventoryId1) + 1;
		}
		if (instance.InventoryId2 != null)
		{
			size += ProtocolParser.GetSize(instance.InventoryId2) + 1;
		}
		if (instance.SlotId1 != 0)
		{
			size += ProtocolParser.GetSize(instance.SlotId1) + 1;
		}
		if (instance.SlotId2 != 0)
		{
			size += ProtocolParser.GetSize(instance.SlotId2) + 1;
		}
		if (instance.ItemStack1 != null)
		{
			int packetlength = Packet_ItemStackSerializer.GetSize(instance.ItemStack1);
			size += packetlength + ProtocolParser.GetSize(packetlength) + 1;
		}
		if (instance.ItemStack2 != null)
		{
			int packetlength2 = Packet_ItemStackSerializer.GetSize(instance.ItemStack2);
			size += packetlength2 + ProtocolParser.GetSize(packetlength2) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_InventoryDoubleUpdate instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_InventoryDoubleUpdateSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_InventoryDoubleUpdate instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_InventoryDoubleUpdateSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_InventoryDoubleUpdate instance)
	{
		byte[] data = Packet_InventoryDoubleUpdateSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
