using System;

public class Packet_ClientLeaveSerializer
{
	public static Packet_ClientLeave DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ClientLeave instance = new Packet_ClientLeave();
		Packet_ClientLeaveSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ClientLeave DeserializeBuffer(byte[] buffer, int length, Packet_ClientLeave instance)
	{
		Packet_ClientLeaveSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ClientLeave Deserialize(CitoMemoryStream stream, Packet_ClientLeave instance)
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
				instance.Reason = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_ClientLeave DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ClientLeave instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ClientLeave packet_ClientLeave = Packet_ClientLeaveSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_ClientLeave;
	}

	public static void Serialize(CitoStream stream, Packet_ClientLeave instance)
	{
		if (instance.Reason != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.Reason);
		}
	}

	public static int GetSize(Packet_ClientLeave instance)
	{
		int size = 0;
		if (instance.Reason != 0)
		{
			size += ProtocolParser.GetSize(instance.Reason) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ClientLeave instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_ClientLeaveSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_ClientLeave instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_ClientLeaveSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ClientLeave instance)
	{
		byte[] data = Packet_ClientLeaveSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
