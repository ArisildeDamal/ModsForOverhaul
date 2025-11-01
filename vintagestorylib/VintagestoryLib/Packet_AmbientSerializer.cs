using System;

public class Packet_AmbientSerializer
{
	public static Packet_Ambient DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_Ambient instance = new Packet_Ambient();
		Packet_AmbientSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_Ambient DeserializeBuffer(byte[] buffer, int length, Packet_Ambient instance)
	{
		Packet_AmbientSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_Ambient Deserialize(CitoMemoryStream stream, Packet_Ambient instance)
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
				goto IL_0037;
			}
			if (keyInt != 10)
			{
				ProtocolParser.SkipKey(stream, Key.Create(keyInt));
			}
			else
			{
				instance.Data = ProtocolParser.ReadBytes(stream);
			}
		}
		if (keyInt >= 0)
		{
			return null;
		}
		return instance;
		IL_0037:
		return null;
	}

	public static Packet_Ambient DeserializeLengthDelimited(CitoMemoryStream stream, Packet_Ambient instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_Ambient packet_Ambient = Packet_AmbientSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_Ambient;
	}

	public static void Serialize(CitoStream stream, Packet_Ambient instance)
	{
		if (instance.Data != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteBytes(stream, instance.Data);
		}
	}

	public static int GetSize(Packet_Ambient instance)
	{
		int size = 0;
		if (instance.Data != null)
		{
			size += ProtocolParser.GetSize(instance.Data) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_Ambient instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_AmbientSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_Ambient instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_AmbientSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_Ambient instance)
	{
		byte[] data = Packet_AmbientSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
