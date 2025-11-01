using System;

public class Packet_ServerDisconnectPlayerSerializer
{
	public static Packet_ServerDisconnectPlayer DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ServerDisconnectPlayer instance = new Packet_ServerDisconnectPlayer();
		Packet_ServerDisconnectPlayerSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ServerDisconnectPlayer DeserializeBuffer(byte[] buffer, int length, Packet_ServerDisconnectPlayer instance)
	{
		Packet_ServerDisconnectPlayerSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ServerDisconnectPlayer Deserialize(CitoMemoryStream stream, Packet_ServerDisconnectPlayer instance)
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
				instance.DisconnectReason = ProtocolParser.ReadString(stream);
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

	public static Packet_ServerDisconnectPlayer DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ServerDisconnectPlayer instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ServerDisconnectPlayer packet_ServerDisconnectPlayer = Packet_ServerDisconnectPlayerSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_ServerDisconnectPlayer;
	}

	public static void Serialize(CitoStream stream, Packet_ServerDisconnectPlayer instance)
	{
		if (instance.DisconnectReason != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.DisconnectReason);
		}
	}

	public static int GetSize(Packet_ServerDisconnectPlayer instance)
	{
		int size = 0;
		if (instance.DisconnectReason != null)
		{
			size += ProtocolParser.GetSize(instance.DisconnectReason) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ServerDisconnectPlayer instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_ServerDisconnectPlayerSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_ServerDisconnectPlayer instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_ServerDisconnectPlayerSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ServerDisconnectPlayer instance)
	{
		byte[] data = Packet_ServerDisconnectPlayerSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
