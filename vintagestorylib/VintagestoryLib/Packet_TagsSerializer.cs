using System;

public class Packet_TagsSerializer
{
	public static Packet_Tags DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_Tags instance = new Packet_Tags();
		Packet_TagsSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_Tags DeserializeBuffer(byte[] buffer, int length, Packet_Tags instance)
	{
		Packet_TagsSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_Tags Deserialize(CitoMemoryStream stream, Packet_Tags instance)
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
			if (keyInt <= 10)
			{
				if (keyInt == 0)
				{
					goto IL_0048;
				}
				if (keyInt == 10)
				{
					instance.EntityTagsAdd(ProtocolParser.ReadString(stream));
					continue;
				}
			}
			else
			{
				if (keyInt == 18)
				{
					instance.BlockTagsAdd(ProtocolParser.ReadString(stream));
					continue;
				}
				if (keyInt == 26)
				{
					instance.ItemTagsAdd(ProtocolParser.ReadString(stream));
					continue;
				}
			}
			ProtocolParser.SkipKey(stream, Key.Create(keyInt));
		}
		if (keyInt >= 0)
		{
			return null;
		}
		return instance;
		IL_0048:
		return null;
	}

	public static Packet_Tags DeserializeLengthDelimited(CitoMemoryStream stream, Packet_Tags instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_Tags packet_Tags = Packet_TagsSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_Tags;
	}

	public static void Serialize(CitoStream stream, Packet_Tags instance)
	{
		if (instance.EntityTags != null)
		{
			string[] elems = instance.EntityTags;
			int elemCount = instance.EntityTagsCount;
			int i = 0;
			while (i < elems.Length && i < elemCount)
			{
				stream.WriteByte(10);
				ProtocolParser.WriteString(stream, elems[i]);
				i++;
			}
		}
		if (instance.BlockTags != null)
		{
			string[] elems2 = instance.BlockTags;
			int elemCount2 = instance.BlockTagsCount;
			int j = 0;
			while (j < elems2.Length && j < elemCount2)
			{
				stream.WriteByte(18);
				ProtocolParser.WriteString(stream, elems2[j]);
				j++;
			}
		}
		if (instance.ItemTags != null)
		{
			string[] elems3 = instance.ItemTags;
			int elemCount3 = instance.ItemTagsCount;
			int k = 0;
			while (k < elems3.Length && k < elemCount3)
			{
				stream.WriteByte(26);
				ProtocolParser.WriteString(stream, elems3[k]);
				k++;
			}
		}
	}

	public static int GetSize(Packet_Tags instance)
	{
		int size = 0;
		if (instance.EntityTags != null)
		{
			for (int i = 0; i < instance.EntityTagsCount; i++)
			{
				string i2 = instance.EntityTags[i];
				size += ProtocolParser.GetSize(i2) + 1;
			}
		}
		if (instance.BlockTags != null)
		{
			for (int j = 0; j < instance.BlockTagsCount; j++)
			{
				string i3 = instance.BlockTags[j];
				size += ProtocolParser.GetSize(i3) + 1;
			}
		}
		if (instance.ItemTags != null)
		{
			for (int k = 0; k < instance.ItemTagsCount; k++)
			{
				string i4 = instance.ItemTags[k];
				size += ProtocolParser.GetSize(i4) + 1;
			}
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_Tags instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_TagsSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_Tags instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_TagsSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_Tags instance)
	{
		byte[] data = Packet_TagsSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
