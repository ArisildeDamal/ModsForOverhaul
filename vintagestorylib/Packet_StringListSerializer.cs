using System;

public class Packet_StringListSerializer
{
	public static Packet_StringList DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_StringList instance = new Packet_StringList();
		Packet_StringListSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_StringList DeserializeBuffer(byte[] buffer, int length, Packet_StringList instance)
	{
		Packet_StringListSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_StringList Deserialize(CitoMemoryStream stream, Packet_StringList instance)
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
				instance.ItemsAdd(ProtocolParser.ReadString(stream));
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

	public static Packet_StringList DeserializeLengthDelimited(CitoMemoryStream stream, Packet_StringList instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_StringList packet_StringList = Packet_StringListSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_StringList;
	}

	public static void Serialize(CitoStream stream, Packet_StringList instance)
	{
		if (instance.Items != null)
		{
			string[] elems = instance.Items;
			int elemCount = instance.ItemsCount;
			int i = 0;
			while (i < elems.Length && i < elemCount)
			{
				stream.WriteByte(10);
				ProtocolParser.WriteString(stream, elems[i]);
				i++;
			}
		}
	}

	public static int GetSize(Packet_StringList instance)
	{
		int size = 0;
		if (instance.Items != null)
		{
			for (int i = 0; i < instance.ItemsCount; i++)
			{
				string i2 = instance.Items[i];
				size += ProtocolParser.GetSize(i2) + 1;
			}
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_StringList instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_StringListSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_StringList instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_StringListSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_StringList instance)
	{
		byte[] data = Packet_StringListSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
