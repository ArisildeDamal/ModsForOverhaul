using System;

public class Packet_ServerSetDecorsSerializer
{
	public static Packet_ServerSetDecors DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ServerSetDecors instance = new Packet_ServerSetDecors();
		Packet_ServerSetDecorsSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ServerSetDecors DeserializeBuffer(byte[] buffer, int length, Packet_ServerSetDecors instance)
	{
		Packet_ServerSetDecorsSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ServerSetDecors Deserialize(CitoMemoryStream stream, Packet_ServerSetDecors instance)
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
				instance.SetDecors = ProtocolParser.ReadBytes(stream);
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

	public static Packet_ServerSetDecors DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ServerSetDecors instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ServerSetDecors packet_ServerSetDecors = Packet_ServerSetDecorsSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_ServerSetDecors;
	}

	public static void Serialize(CitoStream stream, Packet_ServerSetDecors instance)
	{
		if (instance.SetDecors != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteBytes(stream, instance.SetDecors);
		}
	}

	public static int GetSize(Packet_ServerSetDecors instance)
	{
		int size = 0;
		if (instance.SetDecors != null)
		{
			size += ProtocolParser.GetSize(instance.SetDecors) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ServerSetDecors instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_ServerSetDecorsSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_ServerSetDecors instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_ServerSetDecorsSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ServerSetDecors instance)
	{
		byte[] data = Packet_ServerSetDecorsSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
