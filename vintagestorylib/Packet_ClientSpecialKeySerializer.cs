using System;

public class Packet_ClientSpecialKeySerializer
{
	public static Packet_ClientSpecialKey DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ClientSpecialKey instance = new Packet_ClientSpecialKey();
		Packet_ClientSpecialKeySerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ClientSpecialKey DeserializeBuffer(byte[] buffer, int length, Packet_ClientSpecialKey instance)
	{
		Packet_ClientSpecialKeySerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ClientSpecialKey Deserialize(CitoMemoryStream stream, Packet_ClientSpecialKey instance)
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
				goto IL_0036;
			}
			if (keyInt != 8)
			{
				ProtocolParser.SkipKey(stream, Key.Create(keyInt));
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
		IL_0036:
		return null;
	}

	public static Packet_ClientSpecialKey DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ClientSpecialKey instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ClientSpecialKey packet_ClientSpecialKey = Packet_ClientSpecialKeySerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_ClientSpecialKey;
	}

	public static void Serialize(CitoStream stream, Packet_ClientSpecialKey instance)
	{
		if (instance.Key_ != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.Key_);
		}
	}

	public static int GetSize(Packet_ClientSpecialKey instance)
	{
		int size = 0;
		if (instance.Key_ != 0)
		{
			size += ProtocolParser.GetSize(instance.Key_) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ClientSpecialKey instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_ClientSpecialKeySerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_ClientSpecialKey instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_ClientSpecialKeySerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ClientSpecialKey instance)
	{
		byte[] data = Packet_ClientSpecialKeySerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
