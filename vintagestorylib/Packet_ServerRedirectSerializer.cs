using System;

public class Packet_ServerRedirectSerializer
{
	public static Packet_ServerRedirect DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ServerRedirect instance = new Packet_ServerRedirect();
		Packet_ServerRedirectSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ServerRedirect DeserializeBuffer(byte[] buffer, int length, Packet_ServerRedirect instance)
	{
		Packet_ServerRedirectSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ServerRedirect Deserialize(CitoMemoryStream stream, Packet_ServerRedirect instance)
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
					instance.Host = ProtocolParser.ReadString(stream);
				}
			}
			else
			{
				instance.Name = ProtocolParser.ReadString(stream);
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

	public static Packet_ServerRedirect DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ServerRedirect instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ServerRedirect packet_ServerRedirect = Packet_ServerRedirectSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_ServerRedirect;
	}

	public static void Serialize(CitoStream stream, Packet_ServerRedirect instance)
	{
		if (instance.Name != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.Name);
		}
		if (instance.Host != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteString(stream, instance.Host);
		}
	}

	public static int GetSize(Packet_ServerRedirect instance)
	{
		int size = 0;
		if (instance.Name != null)
		{
			size += ProtocolParser.GetSize(instance.Name) + 1;
		}
		if (instance.Host != null)
		{
			size += ProtocolParser.GetSize(instance.Host) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ServerRedirect instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_ServerRedirectSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_ServerRedirect instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_ServerRedirectSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ServerRedirect instance)
	{
		byte[] data = Packet_ServerRedirectSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
