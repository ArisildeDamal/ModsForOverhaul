using System;

public class Packet_SpawnParticlesSerializer
{
	public static Packet_SpawnParticles DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_SpawnParticles instance = new Packet_SpawnParticles();
		Packet_SpawnParticlesSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_SpawnParticles DeserializeBuffer(byte[] buffer, int length, Packet_SpawnParticles instance)
	{
		Packet_SpawnParticlesSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_SpawnParticles Deserialize(CitoMemoryStream stream, Packet_SpawnParticles instance)
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
			if (keyInt == 0)
			{
				goto IL_003C;
			}
			if (keyInt != 10)
			{
				if (keyInt != 18)
				{
					ProtocolParser.SkipKey(stream, Key.Create(keyInt));
				}
				else
				{
					instance.Data = ProtocolParser.ReadBytes(stream);
				}
			}
			else
			{
				instance.ParticlePropertyProviderClassName = ProtocolParser.ReadString(stream);
			}
		}
		if (keyInt >= 0)
		{
			return null;
		}
		return instance;
		IL_003C:
		return null;
	}

	public static Packet_SpawnParticles DeserializeLengthDelimited(CitoMemoryStream stream, Packet_SpawnParticles instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_SpawnParticles packet_SpawnParticles = Packet_SpawnParticlesSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_SpawnParticles;
	}

	public static void Serialize(CitoStream stream, Packet_SpawnParticles instance)
	{
		if (instance.ParticlePropertyProviderClassName != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.ParticlePropertyProviderClassName);
		}
		if (instance.Data != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteBytes(stream, instance.Data);
		}
	}

	public static int GetSize(Packet_SpawnParticles instance)
	{
		int size = 0;
		if (instance.ParticlePropertyProviderClassName != null)
		{
			size += ProtocolParser.GetSize(instance.ParticlePropertyProviderClassName) + 1;
		}
		if (instance.Data != null)
		{
			size += ProtocolParser.GetSize(instance.Data) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_SpawnParticles instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_SpawnParticlesSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_SpawnParticles instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_SpawnParticlesSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_SpawnParticles instance)
	{
		byte[] data = Packet_SpawnParticlesSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
