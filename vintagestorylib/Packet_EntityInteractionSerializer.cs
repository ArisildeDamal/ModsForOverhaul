using System;

public class Packet_EntityInteractionSerializer
{
	public static Packet_EntityInteraction DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_EntityInteraction instance = new Packet_EntityInteraction();
		Packet_EntityInteractionSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_EntityInteraction DeserializeBuffer(byte[] buffer, int length, Packet_EntityInteraction instance)
	{
		Packet_EntityInteractionSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_EntityInteraction Deserialize(CitoMemoryStream stream, Packet_EntityInteraction instance)
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
						goto IL_0074;
					}
					if (keyInt == 8)
					{
						instance.MouseButton = ProtocolParser.ReadUInt32(stream);
						continue;
					}
				}
				else
				{
					if (keyInt == 16)
					{
						instance.EntityId = ProtocolParser.ReadUInt64(stream);
						continue;
					}
					if (keyInt == 24)
					{
						instance.OnBlockFace = ProtocolParser.ReadUInt32(stream);
						continue;
					}
				}
			}
			else if (keyInt <= 40)
			{
				if (keyInt == 32)
				{
					instance.HitX = ProtocolParser.ReadUInt64(stream);
					continue;
				}
				if (keyInt == 40)
				{
					instance.HitY = ProtocolParser.ReadUInt64(stream);
					continue;
				}
			}
			else
			{
				if (keyInt == 48)
				{
					instance.HitZ = ProtocolParser.ReadUInt64(stream);
					continue;
				}
				if (keyInt == 56)
				{
					instance.SelectionBoxIndex = ProtocolParser.ReadUInt32(stream);
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
		IL_0074:
		return null;
	}

	public static Packet_EntityInteraction DeserializeLengthDelimited(CitoMemoryStream stream, Packet_EntityInteraction instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_EntityInteraction packet_EntityInteraction = Packet_EntityInteractionSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_EntityInteraction;
	}

	public static void Serialize(CitoStream stream, Packet_EntityInteraction instance)
	{
		if (instance.MouseButton != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.MouseButton);
		}
		if (instance.EntityId != 0L)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt64(stream, instance.EntityId);
		}
		if (instance.OnBlockFace != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.OnBlockFace);
		}
		if (instance.HitX != 0L)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt64(stream, instance.HitX);
		}
		if (instance.HitY != 0L)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt64(stream, instance.HitY);
		}
		if (instance.HitZ != 0L)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt64(stream, instance.HitZ);
		}
		if (instance.SelectionBoxIndex != 0)
		{
			stream.WriteByte(56);
			ProtocolParser.WriteUInt32(stream, instance.SelectionBoxIndex);
		}
	}

	public static int GetSize(Packet_EntityInteraction instance)
	{
		int size = 0;
		if (instance.MouseButton != 0)
		{
			size += ProtocolParser.GetSize(instance.MouseButton) + 1;
		}
		if (instance.EntityId != 0L)
		{
			size += ProtocolParser.GetSize(instance.EntityId) + 1;
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
		if (instance.SelectionBoxIndex != 0)
		{
			size += ProtocolParser.GetSize(instance.SelectionBoxIndex) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_EntityInteraction instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_EntityInteractionSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_EntityInteraction instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_EntityInteractionSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_EntityInteraction instance)
	{
		byte[] data = Packet_EntityInteractionSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
