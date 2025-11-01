using System;

public class Packet_IntStringSerializer
{
	public static Packet_IntString DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_IntString instance = new Packet_IntString();
		Packet_IntStringSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_IntString DeserializeBuffer(byte[] buffer, int length, Packet_IntString instance)
	{
		Packet_IntStringSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_IntString Deserialize(CitoMemoryStream stream, Packet_IntString instance)
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
				goto IL_003B;
			}
			if (keyInt != 8)
			{
				if (keyInt != 18)
				{
					ProtocolParser.SkipKey(stream, Key.Create(keyInt));
				}
				else
				{
					instance.Value_ = ProtocolParser.ReadString(stream);
				}
			}
			else
			{
				instance.Key_ = ProtocolParser.ReadUInt32(stream);
			}
		}
		if (keyInt >= 0)
		{
			return null;
		}
		return instance;
		IL_003B:
		return null;
	}

	public static Packet_IntString DeserializeLengthDelimited(CitoMemoryStream stream, Packet_IntString instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_IntString packet_IntString = Packet_IntStringSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_IntString;
	}

	public static void Serialize(CitoStream stream, Packet_IntString instance)
	{
		if (instance.Key_ != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.Key_);
		}
		if (instance.Value_ != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteString(stream, instance.Value_);
		}
	}

	public static int GetSize(Packet_IntString instance)
	{
		int size = 0;
		if (instance.Key_ != 0)
		{
			size += ProtocolParser.GetSize(instance.Key_) + 1;
		}
		if (instance.Value_ != null)
		{
			size += ProtocolParser.GetSize(instance.Value_) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_IntString instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_IntStringSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_IntString instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_IntStringSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_IntString instance)
	{
		byte[] data = Packet_IntStringSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
