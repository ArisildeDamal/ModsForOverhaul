using System;

public class Packet_ClientPlayingSerializer
{
	public static Packet_ClientPlaying DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ClientPlaying instance = new Packet_ClientPlaying();
		Packet_ClientPlayingSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ClientPlaying DeserializeBuffer(byte[] buffer, int length, Packet_ClientPlaying instance)
	{
		Packet_ClientPlayingSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ClientPlaying Deserialize(CitoMemoryStream stream, Packet_ClientPlaying instance)
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

	public static Packet_ClientPlaying DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ClientPlaying instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ClientPlaying packet_ClientPlaying = Packet_ClientPlayingSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_ClientPlaying;
	}

	public static void Serialize(CitoStream stream, Packet_ClientPlaying instance)
	{
	}

	public static int GetSize(Packet_ClientPlaying instance)
	{
		int size = 0;
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ClientPlaying instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_ClientPlayingSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_ClientPlaying instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_ClientPlayingSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ClientPlaying instance)
	{
		byte[] data = Packet_ClientPlayingSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
