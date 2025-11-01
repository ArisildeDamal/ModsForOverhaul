using System;

public class Packet_FlipItemstacksSerializer
{
	public static Packet_FlipItemstacks DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_FlipItemstacks instance = new Packet_FlipItemstacks();
		Packet_FlipItemstacksSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_FlipItemstacks DeserializeBuffer(byte[] buffer, int length, Packet_FlipItemstacks instance)
	{
		Packet_FlipItemstacksSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_FlipItemstacks Deserialize(CitoMemoryStream stream, Packet_FlipItemstacks instance)
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
			if (keyInt <= 24)
			{
				if (keyInt <= 10)
				{
					if (keyInt == 0)
					{
						goto IL_007E;
					}
					if (keyInt == 10)
					{
						instance.SourceInventoryId = ProtocolParser.ReadString(stream);
						continue;
					}
				}
				else
				{
					if (keyInt == 18)
					{
						instance.TargetInventoryId = ProtocolParser.ReadString(stream);
						continue;
					}
					if (keyInt == 24)
					{
						instance.SourceSlot = ProtocolParser.ReadUInt32(stream);
						continue;
					}
				}
			}
			else if (keyInt <= 40)
			{
				if (keyInt == 32)
				{
					instance.TargetSlot = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 40)
				{
					instance.SourceLastChanged = ProtocolParser.ReadUInt64(stream);
					continue;
				}
			}
			else
			{
				if (keyInt == 48)
				{
					instance.TargetLastChanged = ProtocolParser.ReadUInt64(stream);
					continue;
				}
				if (keyInt == 56)
				{
					instance.SourceTabIndex = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 64)
				{
					instance.TargetTabIndex = ProtocolParser.ReadUInt32(stream);
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
		IL_007E:
		return null;
	}

	public static Packet_FlipItemstacks DeserializeLengthDelimited(CitoMemoryStream stream, Packet_FlipItemstacks instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_FlipItemstacks packet_FlipItemstacks = Packet_FlipItemstacksSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_FlipItemstacks;
	}

	public static void Serialize(CitoStream stream, Packet_FlipItemstacks instance)
	{
		if (instance.SourceInventoryId != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.SourceInventoryId);
		}
		if (instance.TargetInventoryId != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteString(stream, instance.TargetInventoryId);
		}
		if (instance.SourceSlot != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.SourceSlot);
		}
		if (instance.TargetSlot != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.TargetSlot);
		}
		if (instance.SourceLastChanged != 0L)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt64(stream, instance.SourceLastChanged);
		}
		if (instance.TargetLastChanged != 0L)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt64(stream, instance.TargetLastChanged);
		}
		if (instance.SourceTabIndex != 0)
		{
			stream.WriteByte(56);
			ProtocolParser.WriteUInt32(stream, instance.SourceTabIndex);
		}
		if (instance.TargetTabIndex != 0)
		{
			stream.WriteByte(64);
			ProtocolParser.WriteUInt32(stream, instance.TargetTabIndex);
		}
	}

	public static int GetSize(Packet_FlipItemstacks instance)
	{
		int size = 0;
		if (instance.SourceInventoryId != null)
		{
			size += ProtocolParser.GetSize(instance.SourceInventoryId) + 1;
		}
		if (instance.TargetInventoryId != null)
		{
			size += ProtocolParser.GetSize(instance.TargetInventoryId) + 1;
		}
		if (instance.SourceSlot != 0)
		{
			size += ProtocolParser.GetSize(instance.SourceSlot) + 1;
		}
		if (instance.TargetSlot != 0)
		{
			size += ProtocolParser.GetSize(instance.TargetSlot) + 1;
		}
		if (instance.SourceLastChanged != 0L)
		{
			size += ProtocolParser.GetSize(instance.SourceLastChanged) + 1;
		}
		if (instance.TargetLastChanged != 0L)
		{
			size += ProtocolParser.GetSize(instance.TargetLastChanged) + 1;
		}
		if (instance.SourceTabIndex != 0)
		{
			size += ProtocolParser.GetSize(instance.SourceTabIndex) + 1;
		}
		if (instance.TargetTabIndex != 0)
		{
			size += ProtocolParser.GetSize(instance.TargetTabIndex) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_FlipItemstacks instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_FlipItemstacksSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_FlipItemstacks instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_FlipItemstacksSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_FlipItemstacks instance)
	{
		byte[] data = Packet_FlipItemstacksSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
