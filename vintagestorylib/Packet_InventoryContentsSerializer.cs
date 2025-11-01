using System;

public class Packet_InventoryContentsSerializer
{
	public static Packet_InventoryContents DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_InventoryContents instance = new Packet_InventoryContents();
		Packet_InventoryContentsSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_InventoryContents DeserializeBuffer(byte[] buffer, int length, Packet_InventoryContents instance)
	{
		Packet_InventoryContentsSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_InventoryContents Deserialize(CitoMemoryStream stream, Packet_InventoryContents instance)
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
					goto IL_004B;
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
					instance.InventoryClass = ProtocolParser.ReadString(stream);
					continue;
				}
				if (keyInt == 26)
				{
					instance.InventoryId = ProtocolParser.ReadString(stream);
					continue;
				}
				if (keyInt == 34)
				{
					instance.ItemstacksAdd(Packet_ItemStackSerializer.DeserializeLengthDelimitedNew(stream));
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
		IL_004B:
		return null;
	}

	public static Packet_InventoryContents DeserializeLengthDelimited(CitoMemoryStream stream, Packet_InventoryContents instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_InventoryContents packet_InventoryContents = Packet_InventoryContentsSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_InventoryContents;
	}

	public static void Serialize(CitoStream stream, Packet_InventoryContents instance)
	{
		if (instance.ClientId != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.ClientId);
		}
		if (instance.InventoryClass != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteString(stream, instance.InventoryClass);
		}
		if (instance.InventoryId != null)
		{
			stream.WriteByte(26);
			ProtocolParser.WriteString(stream, instance.InventoryId);
		}
		if (instance.Itemstacks != null)
		{
			Packet_ItemStack[] elems = instance.Itemstacks;
			int elemCount = instance.ItemstacksCount;
			int i = 0;
			while (i < elems.Length && i < elemCount)
			{
				stream.WriteByte(34);
				Packet_ItemStackSerializer.SerializeWithSize(stream, elems[i]);
				i++;
			}
		}
	}

	public static int GetSize(Packet_InventoryContents instance)
	{
		int size = 0;
		if (instance.ClientId != 0)
		{
			size += ProtocolParser.GetSize(instance.ClientId) + 1;
		}
		if (instance.InventoryClass != null)
		{
			size += ProtocolParser.GetSize(instance.InventoryClass) + 1;
		}
		if (instance.InventoryId != null)
		{
			size += ProtocolParser.GetSize(instance.InventoryId) + 1;
		}
		if (instance.Itemstacks != null)
		{
			for (int i = 0; i < instance.ItemstacksCount; i++)
			{
				int packetlength = Packet_ItemStackSerializer.GetSize(instance.Itemstacks[i]);
				size += packetlength + ProtocolParser.GetSize(packetlength) + 1;
			}
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_InventoryContents instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_InventoryContentsSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_InventoryContents instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_InventoryContentsSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_InventoryContents instance)
	{
		byte[] data = Packet_InventoryContentsSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
