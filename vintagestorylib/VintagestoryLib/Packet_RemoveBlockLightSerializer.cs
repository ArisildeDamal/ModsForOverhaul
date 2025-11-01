using System;

public class Packet_RemoveBlockLightSerializer
{
	public static Packet_RemoveBlockLight DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_RemoveBlockLight instance = new Packet_RemoveBlockLight();
		Packet_RemoveBlockLightSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_RemoveBlockLight DeserializeBuffer(byte[] buffer, int length, Packet_RemoveBlockLight instance)
	{
		Packet_RemoveBlockLightSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_RemoveBlockLight Deserialize(CitoMemoryStream stream, Packet_RemoveBlockLight instance)
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
			if (keyInt <= 16)
			{
				if (keyInt == 0)
				{
					goto IL_0060;
				}
				if (keyInt == 8)
				{
					instance.PosX = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 16)
				{
					instance.PosY = ProtocolParser.ReadUInt32(stream);
					continue;
				}
			}
			else if (keyInt <= 32)
			{
				if (keyInt == 24)
				{
					instance.PosZ = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 32)
				{
					instance.LightH = ProtocolParser.ReadUInt32(stream);
					continue;
				}
			}
			else
			{
				if (keyInt == 40)
				{
					instance.LightS = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 48)
				{
					instance.LightV = ProtocolParser.ReadUInt32(stream);
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
		IL_0060:
		return null;
	}

	public static Packet_RemoveBlockLight DeserializeLengthDelimited(CitoMemoryStream stream, Packet_RemoveBlockLight instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_RemoveBlockLight packet_RemoveBlockLight = Packet_RemoveBlockLightSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_RemoveBlockLight;
	}

	public static void Serialize(CitoStream stream, Packet_RemoveBlockLight instance)
	{
		if (instance.PosX != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.PosX);
		}
		if (instance.PosY != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.PosY);
		}
		if (instance.PosZ != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.PosZ);
		}
		if (instance.LightH != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.LightH);
		}
		if (instance.LightS != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.LightS);
		}
		if (instance.LightV != 0)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt32(stream, instance.LightV);
		}
	}

	public static int GetSize(Packet_RemoveBlockLight instance)
	{
		int size = 0;
		if (instance.PosX != 0)
		{
			size += ProtocolParser.GetSize(instance.PosX) + 1;
		}
		if (instance.PosY != 0)
		{
			size += ProtocolParser.GetSize(instance.PosY) + 1;
		}
		if (instance.PosZ != 0)
		{
			size += ProtocolParser.GetSize(instance.PosZ) + 1;
		}
		if (instance.LightH != 0)
		{
			size += ProtocolParser.GetSize(instance.LightH) + 1;
		}
		if (instance.LightS != 0)
		{
			size += ProtocolParser.GetSize(instance.LightS) + 1;
		}
		if (instance.LightV != 0)
		{
			size += ProtocolParser.GetSize(instance.LightV) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_RemoveBlockLight instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_RemoveBlockLightSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_RemoveBlockLight instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_RemoveBlockLightSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_RemoveBlockLight instance)
	{
		byte[] data = Packet_RemoveBlockLightSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
