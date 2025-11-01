using System;

public class Packet_ClientPingReplySerializer
{
	public static Packet_ClientPingReply DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ClientPingReply instance = new Packet_ClientPingReply();
		Packet_ClientPingReplySerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ClientPingReply DeserializeBuffer(byte[] buffer, int length, Packet_ClientPingReply instance)
	{
		Packet_ClientPingReplySerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ClientPingReply Deserialize(CitoMemoryStream stream, Packet_ClientPingReply instance)
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
				goto Block_4;
			}
			ProtocolParser.SkipKey(stream, Key.Create(keyInt));
		}
		if (keyInt >= 0)
		{
			return null;
		}
		return instance;
		Block_4:
		return null;
	}

	public static Packet_ClientPingReply DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ClientPingReply instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ClientPingReply packet_ClientPingReply = Packet_ClientPingReplySerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_ClientPingReply;
	}

	public static void Serialize(CitoStream stream, Packet_ClientPingReply instance)
	{
	}

	public static int GetSize(Packet_ClientPingReply instance)
	{
		int size = 0;
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ClientPingReply instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_ClientPingReplySerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_ClientPingReply instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_ClientPingReplySerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ClientPingReply instance)
	{
		byte[] data = Packet_ClientPingReplySerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
