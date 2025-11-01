using System;

public class Packet_LoginTokenAnswerSerializer
{
	public static Packet_LoginTokenAnswer DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_LoginTokenAnswer instance = new Packet_LoginTokenAnswer();
		Packet_LoginTokenAnswerSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_LoginTokenAnswer DeserializeBuffer(byte[] buffer, int length, Packet_LoginTokenAnswer instance)
	{
		Packet_LoginTokenAnswerSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_LoginTokenAnswer Deserialize(CitoMemoryStream stream, Packet_LoginTokenAnswer instance)
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
				instance.Token = ProtocolParser.ReadString(stream);
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

	public static Packet_LoginTokenAnswer DeserializeLengthDelimited(CitoMemoryStream stream, Packet_LoginTokenAnswer instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_LoginTokenAnswer packet_LoginTokenAnswer = Packet_LoginTokenAnswerSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_LoginTokenAnswer;
	}

	public static void Serialize(CitoStream stream, Packet_LoginTokenAnswer instance)
	{
		if (instance.Token != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.Token);
		}
	}

	public static int GetSize(Packet_LoginTokenAnswer instance)
	{
		int size = 0;
		if (instance.Token != null)
		{
			size += ProtocolParser.GetSize(instance.Token) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_LoginTokenAnswer instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_LoginTokenAnswerSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_LoginTokenAnswer instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_LoginTokenAnswerSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_LoginTokenAnswer instance)
	{
		byte[] data = Packet_LoginTokenAnswerSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
