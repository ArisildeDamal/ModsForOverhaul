using System;

public class Packet_ToolModeSerializer
{
	public static Packet_ToolMode DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ToolMode instance = new Packet_ToolMode();
		Packet_ToolModeSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ToolMode DeserializeBuffer(byte[] buffer, int length, Packet_ToolMode instance)
	{
		Packet_ToolModeSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ToolMode Deserialize(CitoMemoryStream stream, Packet_ToolMode instance)
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
			if (keyInt <= 32)
			{
				if (keyInt <= 8)
				{
					if (keyInt == 0)
					{
						goto IL_0087;
					}
					if (keyInt == 8)
					{
						instance.Mode = ProtocolParser.ReadUInt32(stream);
						continue;
					}
				}
				else
				{
					if (keyInt == 16)
					{
						instance.X = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 24)
					{
						instance.Y = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 32)
					{
						instance.Z = ProtocolParser.ReadUInt32(stream);
						continue;
					}
				}
			}
			else if (keyInt <= 48)
			{
				if (keyInt == 40)
				{
					instance.Face = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 48)
				{
					instance.SelectionBoxIndex = ProtocolParser.ReadUInt32(stream);
					continue;
				}
			}
			else
			{
				if (keyInt == 56)
				{
					instance.HitX = ProtocolParser.ReadUInt64(stream);
					continue;
				}
				if (keyInt == 64)
				{
					instance.HitY = ProtocolParser.ReadUInt64(stream);
					continue;
				}
				if (keyInt == 72)
				{
					instance.HitZ = ProtocolParser.ReadUInt64(stream);
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
		IL_0087:
		return null;
	}

	public static Packet_ToolMode DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ToolMode instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ToolMode packet_ToolMode = Packet_ToolModeSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_ToolMode;
	}

	public static void Serialize(CitoStream stream, Packet_ToolMode instance)
	{
		if (instance.Mode != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.Mode);
		}
		if (instance.X != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.X);
		}
		if (instance.Y != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.Y);
		}
		if (instance.Z != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.Z);
		}
		if (instance.Face != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.Face);
		}
		if (instance.SelectionBoxIndex != 0)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt32(stream, instance.SelectionBoxIndex);
		}
		if (instance.HitX != 0L)
		{
			stream.WriteByte(56);
			ProtocolParser.WriteUInt64(stream, instance.HitX);
		}
		if (instance.HitY != 0L)
		{
			stream.WriteByte(64);
			ProtocolParser.WriteUInt64(stream, instance.HitY);
		}
		if (instance.HitZ != 0L)
		{
			stream.WriteByte(72);
			ProtocolParser.WriteUInt64(stream, instance.HitZ);
		}
	}

	public static int GetSize(Packet_ToolMode instance)
	{
		int size = 0;
		if (instance.Mode != 0)
		{
			size += ProtocolParser.GetSize(instance.Mode) + 1;
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
		if (instance.Face != 0)
		{
			size += ProtocolParser.GetSize(instance.Face) + 1;
		}
		if (instance.SelectionBoxIndex != 0)
		{
			size += ProtocolParser.GetSize(instance.SelectionBoxIndex) + 1;
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
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ToolMode instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_ToolModeSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_ToolMode instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_ToolModeSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ToolMode instance)
	{
		byte[] data = Packet_ToolModeSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
