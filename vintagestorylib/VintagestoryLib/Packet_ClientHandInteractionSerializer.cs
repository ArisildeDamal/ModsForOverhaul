using System;

public class Packet_ClientHandInteractionSerializer
{
	public static Packet_ClientHandInteraction DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ClientHandInteraction instance = new Packet_ClientHandInteraction();
		Packet_ClientHandInteractionSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ClientHandInteraction DeserializeBuffer(byte[] buffer, int length, Packet_ClientHandInteraction instance)
	{
		Packet_ClientHandInteractionSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ClientHandInteraction Deserialize(CitoMemoryStream stream, Packet_ClientHandInteraction instance)
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
			if (keyInt <= 64)
			{
				if (keyInt <= 24)
				{
					if (keyInt <= 8)
					{
						if (keyInt == 0)
						{
							goto IL_010A;
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
							instance.InventoryId = ProtocolParser.ReadString(stream);
							continue;
						}
						if (keyInt == 24)
						{
							instance.SlotId = ProtocolParser.ReadUInt32(stream);
							continue;
						}
					}
				}
				else if (keyInt <= 40)
				{
					if (keyInt == 32)
					{
						instance.X = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 40)
					{
						instance.Y = ProtocolParser.ReadUInt32(stream);
						continue;
					}
				}
				else
				{
					if (keyInt == 48)
					{
						instance.Z = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 56)
					{
						instance.OnBlockFace = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 64)
					{
						instance.HitX = ProtocolParser.ReadUInt64(stream);
						continue;
					}
				}
			}
			else if (keyInt <= 96)
			{
				if (keyInt <= 80)
				{
					if (keyInt == 72)
					{
						instance.HitY = ProtocolParser.ReadUInt64(stream);
						continue;
					}
					if (keyInt == 80)
					{
						instance.HitZ = ProtocolParser.ReadUInt64(stream);
						continue;
					}
				}
				else
				{
					if (keyInt == 88)
					{
						instance.EnumHandInteract = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 96)
					{
						instance.UsingCount = ProtocolParser.ReadUInt32(stream);
						continue;
					}
				}
			}
			else if (keyInt <= 112)
			{
				if (keyInt == 104)
				{
					instance.SelectionBoxIndex = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 112)
				{
					instance.OnEntityId = ProtocolParser.ReadUInt64(stream);
					continue;
				}
			}
			else
			{
				if (keyInt == 120)
				{
					instance.UseType = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 128)
				{
					instance.CancelReason = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 136)
				{
					instance.FirstEvent = ProtocolParser.ReadUInt32(stream);
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
		IL_010A:
		return null;
	}

	public static Packet_ClientHandInteraction DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ClientHandInteraction instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ClientHandInteraction packet_ClientHandInteraction = Packet_ClientHandInteractionSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_ClientHandInteraction;
	}

	public static void Serialize(CitoStream stream, Packet_ClientHandInteraction instance)
	{
		if (instance.UseType != 0)
		{
			stream.WriteByte(120);
			ProtocolParser.WriteUInt32(stream, instance.UseType);
		}
		if (instance.MouseButton != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.MouseButton);
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
		if (instance.X != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.X);
		}
		if (instance.Y != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.Y);
		}
		if (instance.Z != 0)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt32(stream, instance.Z);
		}
		if (instance.OnBlockFace != 0)
		{
			stream.WriteByte(56);
			ProtocolParser.WriteUInt32(stream, instance.OnBlockFace);
		}
		if (instance.HitX != 0L)
		{
			stream.WriteByte(64);
			ProtocolParser.WriteUInt64(stream, instance.HitX);
		}
		if (instance.HitY != 0L)
		{
			stream.WriteByte(72);
			ProtocolParser.WriteUInt64(stream, instance.HitY);
		}
		if (instance.HitZ != 0L)
		{
			stream.WriteByte(80);
			ProtocolParser.WriteUInt64(stream, instance.HitZ);
		}
		if (instance.OnEntityId != 0L)
		{
			stream.WriteByte(112);
			ProtocolParser.WriteUInt64(stream, instance.OnEntityId);
		}
		if (instance.EnumHandInteract != 0)
		{
			stream.WriteByte(88);
			ProtocolParser.WriteUInt32(stream, instance.EnumHandInteract);
		}
		if (instance.UsingCount != 0)
		{
			stream.WriteByte(96);
			ProtocolParser.WriteUInt32(stream, instance.UsingCount);
		}
		if (instance.SelectionBoxIndex != 0)
		{
			stream.WriteByte(104);
			ProtocolParser.WriteUInt32(stream, instance.SelectionBoxIndex);
		}
		if (instance.CancelReason != 0)
		{
			stream.WriteKey(16, 0);
			ProtocolParser.WriteUInt32(stream, instance.CancelReason);
		}
		if (instance.FirstEvent != 0)
		{
			stream.WriteKey(17, 0);
			ProtocolParser.WriteUInt32(stream, instance.FirstEvent);
		}
	}

	public static int GetSize(Packet_ClientHandInteraction instance)
	{
		int size = 0;
		if (instance.UseType != 0)
		{
			size += ProtocolParser.GetSize(instance.UseType) + 1;
		}
		if (instance.MouseButton != 0)
		{
			size += ProtocolParser.GetSize(instance.MouseButton) + 1;
		}
		if (instance.InventoryId != null)
		{
			size += ProtocolParser.GetSize(instance.InventoryId) + 1;
		}
		if (instance.SlotId != 0)
		{
			size += ProtocolParser.GetSize(instance.SlotId) + 1;
		}
		if (instance.X != 0)
		{
			size += ProtocolParser.GetSize(instance.X) + 1;
		}
		if (instance.Y != 0)
		{
			size += ProtocolParser.GetSize(instance.Y) + 1;
		}
		if (instance.Z != 0)
		{
			size += ProtocolParser.GetSize(instance.Z) + 1;
		}
		if (instance.OnBlockFace != 0)
		{
			size += ProtocolParser.GetSize(instance.OnBlockFace) + 1;
		}
		if (instance.HitX != 0L)
		{
			size += ProtocolParser.GetSize(instance.HitX) + 1;
		}
		if (instance.HitY != 0L)
		{
			size += ProtocolParser.GetSize(instance.HitY) + 1;
		}
		if (instance.HitZ != 0L)
		{
			size += ProtocolParser.GetSize(instance.HitZ) + 1;
		}
		if (instance.OnEntityId != 0L)
		{
			size += ProtocolParser.GetSize(instance.OnEntityId) + 1;
		}
		if (instance.EnumHandInteract != 0)
		{
			size += ProtocolParser.GetSize(instance.EnumHandInteract) + 1;
		}
		if (instance.UsingCount != 0)
		{
			size += ProtocolParser.GetSize(instance.UsingCount) + 1;
		}
		if (instance.SelectionBoxIndex != 0)
		{
			size += ProtocolParser.GetSize(instance.SelectionBoxIndex) + 1;
		}
		if (instance.CancelReason != 0)
		{
			size += ProtocolParser.GetSize(instance.CancelReason) + 2;
		}
		if (instance.FirstEvent != 0)
		{
			size += ProtocolParser.GetSize(instance.FirstEvent) + 2;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ClientHandInteraction instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_ClientHandInteractionSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_ClientHandInteraction instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_ClientHandInteractionSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ClientHandInteraction instance)
	{
		byte[] data = Packet_ClientHandInteractionSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
