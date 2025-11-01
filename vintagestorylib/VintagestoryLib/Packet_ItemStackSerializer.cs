using System;

public class Packet_ItemStackSerializer
{
	public static Packet_ItemStack DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ItemStack instance = new Packet_ItemStack();
		Packet_ItemStackSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ItemStack DeserializeBuffer(byte[] buffer, int length, Packet_ItemStack instance)
	{
		Packet_ItemStackSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ItemStack Deserialize(CitoMemoryStream stream, Packet_ItemStack instance)
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
					instance.ItemClass = ProtocolParser.ReadUInt32(stream);
					continue;
				}
			}
			else
			{
				if (keyInt == 16)
				{
					instance.ItemId = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 24)
				{
					instance.StackSize = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 34)
				{
					instance.Attributes = ProtocolParser.ReadBytes(stream);
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

	public static Packet_ItemStack DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ItemStack instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ItemStack packet_ItemStack = Packet_ItemStackSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_ItemStack;
	}

	public static void Serialize(CitoStream stream, Packet_ItemStack instance)
	{
		if (instance.ItemClass != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.ItemClass);
		}
		if (instance.ItemId != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.ItemId);
		}
		if (instance.StackSize != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.StackSize);
		}
		if (instance.Attributes != null)
		{
			stream.WriteByte(34);
			ProtocolParser.WriteBytes(stream, instance.Attributes);
		}
	}

	public static int GetSize(Packet_ItemStack instance)
	{
		int size = 0;
		if (instance.ItemClass != 0)
		{
			size += ProtocolParser.GetSize(instance.ItemClass) + 1;
		}
		if (instance.ItemId != 0)
		{
			size += ProtocolParser.GetSize(instance.ItemId) + 1;
		}
		if (instance.StackSize != 0)
		{
			size += ProtocolParser.GetSize(instance.StackSize) + 1;
		}
		if (instance.Attributes != null)
		{
			size += ProtocolParser.GetSize(instance.Attributes) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ItemStack instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_ItemStackSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_ItemStack instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_ItemStackSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ItemStack instance)
	{
		byte[] data = Packet_ItemStackSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
