using System;

public class Packet_ClientRequestJoinSerializer
{
	public static Packet_ClientRequestJoin DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ClientRequestJoin instance = new Packet_ClientRequestJoin();
		Packet_ClientRequestJoinSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ClientRequestJoin DeserializeBuffer(byte[] buffer, int length, Packet_ClientRequestJoin instance)
	{
		Packet_ClientRequestJoinSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ClientRequestJoin Deserialize(CitoMemoryStream stream, Packet_ClientRequestJoin instance)
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
				instance.Language = ProtocolParser.ReadString(stream);
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

	public static Packet_ClientRequestJoin DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ClientRequestJoin instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ClientRequestJoin packet_ClientRequestJoin = Packet_ClientRequestJoinSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_ClientRequestJoin;
	}

	public static void Serialize(CitoStream stream, Packet_ClientRequestJoin instance)
	{
		if (instance.Language != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.Language);
		}
	}

	public static int GetSize(Packet_ClientRequestJoin instance)
	{
		int size = 0;
		if (instance.Language != null)
		{
			size += ProtocolParser.GetSize(instance.Language) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ClientRequestJoin instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_ClientRequestJoinSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_ClientRequestJoin instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_ClientRequestJoinSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ClientRequestJoin instance)
	{
		byte[] data = Packet_ClientRequestJoinSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
