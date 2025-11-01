using System;

public class Packet_SelectedHotbarSlotSerializer
{
	public static Packet_SelectedHotbarSlot DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_SelectedHotbarSlot instance = new Packet_SelectedHotbarSlot();
		Packet_SelectedHotbarSlotSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_SelectedHotbarSlot DeserializeBuffer(byte[] buffer, int length, Packet_SelectedHotbarSlot instance)
	{
		Packet_SelectedHotbarSlotSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_SelectedHotbarSlot Deserialize(CitoMemoryStream stream, Packet_SelectedHotbarSlot instance)
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
					goto IL_0051;
				}
				if (keyInt == 8)
				{
					instance.SlotNumber = ProtocolParser.ReadUInt32(stream);
					continue;
				}
			}
			else
			{
				if (keyInt == 16)
				{
					instance.ClientId = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt != 26)
				{
					if (keyInt == 34)
					{
						if (instance.OffhandStack == null)
						{
							instance.OffhandStack = Packet_ItemStackSerializer.DeserializeLengthDelimitedNew(stream);
							continue;
						}
						Packet_ItemStackSerializer.DeserializeLengthDelimited(stream, instance.OffhandStack);
						continue;
					}
				}
				else
				{
					if (instance.Itemstack == null)
					{
						instance.Itemstack = Packet_ItemStackSerializer.DeserializeLengthDelimitedNew(stream);
						continue;
					}
					Packet_ItemStackSerializer.DeserializeLengthDelimited(stream, instance.Itemstack);
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
		IL_0051:
		return null;
	}

	public static Packet_SelectedHotbarSlot DeserializeLengthDelimited(CitoMemoryStream stream, Packet_SelectedHotbarSlot instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_SelectedHotbarSlot packet_SelectedHotbarSlot = Packet_SelectedHotbarSlotSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_SelectedHotbarSlot;
	}

	public static void Serialize(CitoStream stream, Packet_SelectedHotbarSlot instance)
	{
		if (instance.SlotNumber != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.SlotNumber);
		}
		if (instance.ClientId != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.ClientId);
		}
		if (instance.Itemstack != null)
		{
			stream.WriteByte(26);
			Packet_ItemStackSerializer.SerializeWithSize(stream, instance.Itemstack);
		}
		if (instance.OffhandStack != null)
		{
			stream.WriteByte(34);
			Packet_ItemStackSerializer.SerializeWithSize(stream, instance.OffhandStack);
		}
	}

	public static int GetSize(Packet_SelectedHotbarSlot instance)
	{
		int size = 0;
		if (instance.SlotNumber != 0)
		{
			size += ProtocolParser.GetSize(instance.SlotNumber) + 1;
		}
		if (instance.ClientId != 0)
		{
			size += ProtocolParser.GetSize(instance.ClientId) + 1;
		}
		if (instance.Itemstack != null)
		{
			int packetlength = Packet_ItemStackSerializer.GetSize(instance.Itemstack);
			size += packetlength + ProtocolParser.GetSize(packetlength) + 1;
		}
		if (instance.OffhandStack != null)
		{
			int packetlength2 = Packet_ItemStackSerializer.GetSize(instance.OffhandStack);
			size += packetlength2 + ProtocolParser.GetSize(packetlength2) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_SelectedHotbarSlot instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_SelectedHotbarSlotSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_SelectedHotbarSlot instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_SelectedHotbarSlotSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_SelectedHotbarSlot instance)
	{
		byte[] data = Packet_SelectedHotbarSlotSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
