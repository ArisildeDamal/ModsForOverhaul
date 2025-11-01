using System;

public class Packet_ActivateInventorySlotSerializer
{
	public static Packet_ActivateInventorySlot DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ActivateInventorySlot instance = new Packet_ActivateInventorySlot();
		Packet_ActivateInventorySlotSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ActivateInventorySlot DeserializeBuffer(byte[] buffer, int length, Packet_ActivateInventorySlot instance)
	{
		Packet_ActivateInventorySlotSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ActivateInventorySlot Deserialize(CitoMemoryStream stream, Packet_ActivateInventorySlot instance)
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
				if (keyInt <= 8)
				{
					if (keyInt == 0)
					{
						goto IL_007C;
					}
					if (keyInt == 8)
					{
						instance.MouseButton = ProtocolParser.ReadUInt32(stream);
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
						instance.TargetSlot = ProtocolParser.ReadUInt32(stream);
						continue;
					}
				}
			}
			else if (keyInt <= 40)
			{
				if (keyInt == 32)
				{
					instance.Modifiers = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 40)
				{
					instance.TargetLastChanged = ProtocolParser.ReadUInt64(stream);
					continue;
				}
			}
			else
			{
				if (keyInt == 48)
				{
					instance.TabIndex = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 56)
				{
					instance.Priority = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 64)
				{
					instance.Dir = ProtocolParser.ReadUInt32(stream);
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
		IL_007C:
		return null;
	}

	public static Packet_ActivateInventorySlot DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ActivateInventorySlot instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ActivateInventorySlot packet_ActivateInventorySlot = Packet_ActivateInventorySlotSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_ActivateInventorySlot;
	}

	public static void Serialize(CitoStream stream, Packet_ActivateInventorySlot instance)
	{
		if (instance.MouseButton != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.MouseButton);
		}
		if (instance.Modifiers != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.Modifiers);
		}
		if (instance.TargetInventoryId != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteString(stream, instance.TargetInventoryId);
		}
		if (instance.TargetSlot != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.TargetSlot);
		}
		if (instance.TargetLastChanged != 0L)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt64(stream, instance.TargetLastChanged);
		}
		if (instance.TabIndex != 0)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt32(stream, instance.TabIndex);
		}
		if (instance.Priority != 0)
		{
			stream.WriteByte(56);
			ProtocolParser.WriteUInt32(stream, instance.Priority);
		}
		if (instance.Dir != 0)
		{
			stream.WriteByte(64);
			ProtocolParser.WriteUInt32(stream, instance.Dir);
		}
	}

	public static int GetSize(Packet_ActivateInventorySlot instance)
	{
		int size = 0;
		if (instance.MouseButton != 0)
		{
			size += ProtocolParser.GetSize(instance.MouseButton) + 1;
		}
		if (instance.Modifiers != 0)
		{
			size += ProtocolParser.GetSize(instance.Modifiers) + 1;
		}
		if (instance.TargetInventoryId != null)
		{
			size += ProtocolParser.GetSize(instance.TargetInventoryId) + 1;
		}
		if (instance.TargetSlot != 0)
		{
			size += ProtocolParser.GetSize(instance.TargetSlot) + 1;
		}
		if (instance.TargetLastChanged != 0L)
		{
			size += ProtocolParser.GetSize(instance.TargetLastChanged) + 1;
		}
		if (instance.TabIndex != 0)
		{
			size += ProtocolParser.GetSize(instance.TabIndex) + 1;
		}
		if (instance.Priority != 0)
		{
			size += ProtocolParser.GetSize(instance.Priority) + 1;
		}
		if (instance.Dir != 0)
		{
			size += ProtocolParser.GetSize(instance.Dir) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ActivateInventorySlot instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_ActivateInventorySlotSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_ActivateInventorySlot instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_ActivateInventorySlotSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ActivateInventorySlot instance)
	{
		byte[] data = Packet_ActivateInventorySlotSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
