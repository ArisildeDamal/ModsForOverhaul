using System;

public class Packet_ServerSoundSerializer
{
	public static Packet_ServerSound DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ServerSound instance = new Packet_ServerSound();
		Packet_ServerSoundSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ServerSound DeserializeBuffer(byte[] buffer, int length, Packet_ServerSound instance)
	{
		Packet_ServerSoundSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ServerSound Deserialize(CitoMemoryStream stream, Packet_ServerSound instance)
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
						instance.Name = ProtocolParser.ReadString(stream);
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
				}
			}
			else if (keyInt <= 40)
			{
				if (keyInt == 32)
				{
					instance.Z = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 40)
				{
					instance.Pitch = ProtocolParser.ReadUInt32(stream);
					continue;
				}
			}
			else
			{
				if (keyInt == 48)
				{
					instance.Range = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 56)
				{
					instance.Volume = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 64)
				{
					instance.SoundType = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_ServerSound DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ServerSound instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ServerSound packet_ServerSound = Packet_ServerSoundSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_ServerSound;
	}

	public static void Serialize(CitoStream stream, Packet_ServerSound instance)
	{
		if (instance.Name != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.Name);
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
		if (instance.Pitch != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.Pitch);
		}
		if (instance.Range != 0)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt32(stream, instance.Range);
		}
		if (instance.Volume != 0)
		{
			stream.WriteByte(56);
			ProtocolParser.WriteUInt32(stream, instance.Volume);
		}
		if (instance.SoundType != 0)
		{
			stream.WriteByte(64);
			ProtocolParser.WriteUInt32(stream, instance.SoundType);
		}
	}

	public static int GetSize(Packet_ServerSound instance)
	{
		int size = 0;
		if (instance.Name != null)
		{
			size += ProtocolParser.GetSize(instance.Name) + 1;
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
		if (instance.Pitch != 0)
		{
			size += ProtocolParser.GetSize(instance.Pitch) + 1;
		}
		if (instance.Range != 0)
		{
			size += ProtocolParser.GetSize(instance.Range) + 1;
		}
		if (instance.Volume != 0)
		{
			size += ProtocolParser.GetSize(instance.Volume) + 1;
		}
		if (instance.SoundType != 0)
		{
			size += ProtocolParser.GetSize(instance.SoundType) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ServerSound instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_ServerSoundSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_ServerSound instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_ServerSoundSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ServerSound instance)
	{
		byte[] data = Packet_ServerSoundSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
