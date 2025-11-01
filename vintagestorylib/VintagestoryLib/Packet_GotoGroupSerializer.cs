using System;

public class Packet_GotoGroupSerializer
{
	public static Packet_GotoGroup DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_GotoGroup instance = new Packet_GotoGroup();
		Packet_GotoGroupSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_GotoGroup DeserializeBuffer(byte[] buffer, int length, Packet_GotoGroup instance)
	{
		Packet_GotoGroupSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_GotoGroup Deserialize(CitoMemoryStream stream, Packet_GotoGroup instance)
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
				instance.GroupId = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_GotoGroup DeserializeLengthDelimited(CitoMemoryStream stream, Packet_GotoGroup instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_GotoGroup packet_GotoGroup = Packet_GotoGroupSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_GotoGroup;
	}

	public static void Serialize(CitoStream stream, Packet_GotoGroup instance)
	{
		if (instance.GroupId != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.GroupId);
		}
	}

	public static int GetSize(Packet_GotoGroup instance)
	{
		int size = 0;
		if (instance.GroupId != 0)
		{
			size += ProtocolParser.GetSize(instance.GroupId) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_GotoGroup instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_GotoGroupSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_GotoGroup instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_GotoGroupSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_GotoGroup instance)
	{
		byte[] data = Packet_GotoGroupSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
