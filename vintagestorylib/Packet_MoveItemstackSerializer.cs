using System;

public class Packet_MoveItemstackSerializer
{
	public static Packet_MoveItemstack DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_MoveItemstack instance = new Packet_MoveItemstack();
		Packet_MoveItemstackSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_MoveItemstack DeserializeBuffer(byte[] buffer, int length, Packet_MoveItemstack instance)
	{
		Packet_MoveItemstackSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_MoveItemstack Deserialize(CitoMemoryStream stream, Packet_MoveItemstack instance)
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
			if (keyInt <= 40)
			{
				if (keyInt <= 18)
				{
					if (keyInt == 0)
					{
						goto IL_00A5;
					}
					if (keyInt == 10)
					{
						instance.SourceInventoryId = ProtocolParser.ReadString(stream);
						continue;
					}
					if (keyInt == 18)
					{
						instance.TargetInventoryId = ProtocolParser.ReadString(stream);
						continue;
					}
				}
				else
				{
					if (keyInt == 24)
					{
						instance.SourceSlot = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 32)
					{
						instance.TargetSlot = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 40)
					{
						instance.Quantity = ProtocolParser.ReadUInt32(stream);
						continue;
					}
				}
			}
			else if (keyInt <= 64)
			{
				if (keyInt == 48)
				{
					instance.SourceLastChanged = ProtocolParser.ReadUInt64(stream);
					continue;
				}
				if (keyInt == 56)
				{
					instance.TargetLastChanged = ProtocolParser.ReadUInt64(stream);
					continue;
				}
				if (keyInt == 64)
				{
					instance.MouseButton = ProtocolParser.ReadUInt32(stream);
					continue;
				}
			}
			else
			{
				if (keyInt == 72)
				{
					instance.Modifiers = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 80)
				{
					instance.Priority = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 88)
				{
					instance.TabIndex = ProtocolParser.ReadUInt32(stream);
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
		IL_00A5:
		return null;
	}

	public static Packet_MoveItemstack DeserializeLengthDelimited(CitoMemoryStream stream, Packet_MoveItemstack instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_MoveItemstack packet_MoveItemstack = Packet_MoveItemstackSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_MoveItemstack;
	}

	public static void Serialize(CitoStream stream, Packet_MoveItemstack instance)
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
		if (instance.Quantity != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.Quantity);
		}
		if (instance.SourceLastChanged != 0L)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt64(stream, instance.SourceLastChanged);
		}
		if (instance.TargetLastChanged != 0L)
		{
			stream.WriteByte(56);
			ProtocolParser.WriteUInt64(stream, instance.TargetLastChanged);
		}
		if (instance.MouseButton != 0)
		{
			stream.WriteByte(64);
			ProtocolParser.WriteUInt32(stream, instance.MouseButton);
		}
		if (instance.Modifiers != 0)
		{
			stream.WriteByte(72);
			ProtocolParser.WriteUInt32(stream, instance.Modifiers);
		}
		if (instance.Priority != 0)
		{
			stream.WriteByte(80);
			ProtocolParser.WriteUInt32(stream, instance.Priority);
		}
		if (instance.TabIndex != 0)
		{
			stream.WriteByte(88);
			ProtocolParser.WriteUInt32(stream, instance.TabIndex);
		}
	}

	public static int GetSize(Packet_MoveItemstack instance)
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
		if (instance.Quantity != 0)
		{
			size += ProtocolParser.GetSize(instance.Quantity) + 1;
		}
		if (instance.SourceLastChanged != 0L)
		{
			size += ProtocolParser.GetSize(instance.SourceLastChanged) + 1;
		}
		if (instance.TargetLastChanged != 0L)
		{
			size += ProtocolParser.GetSize(instance.TargetLastChanged) + 1;
		}
		if (instance.MouseButton != 0)
		{
			size += ProtocolParser.GetSize(instance.MouseButton) + 1;
		}
		if (instance.Modifiers != 0)
		{
			size += ProtocolParser.GetSize(instance.Modifiers) + 1;
		}
		if (instance.Priority != 0)
		{
			size += ProtocolParser.GetSize(instance.Priority) + 1;
		}
		if (instance.TabIndex != 0)
		{
			size += ProtocolParser.GetSize(instance.TabIndex) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_MoveItemstack instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_MoveItemstackSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_MoveItemstack instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_MoveItemstackSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_MoveItemstack instance)
	{
		byte[] data = Packet_MoveItemstackSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
