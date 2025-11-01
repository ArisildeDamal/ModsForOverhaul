using System;

public class Packet_HighlightBlocksSerializer
{
	public static Packet_HighlightBlocks DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_HighlightBlocks instance = new Packet_HighlightBlocks();
		Packet_HighlightBlocksSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_HighlightBlocks DeserializeBuffer(byte[] buffer, int length, Packet_HighlightBlocks instance)
	{
		Packet_HighlightBlocksSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_HighlightBlocks Deserialize(CitoMemoryStream stream, Packet_HighlightBlocks instance)
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
			if (keyInt <= 16)
			{
				if (keyInt == 0)
				{
					goto IL_0060;
				}
				if (keyInt == 8)
				{
					instance.Mode = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 16)
				{
					instance.Shape = ProtocolParser.ReadUInt32(stream);
					continue;
				}
			}
			else if (keyInt <= 32)
			{
				if (keyInt == 26)
				{
					instance.Blocks = ProtocolParser.ReadBytes(stream);
					continue;
				}
				if (keyInt == 32)
				{
					instance.ColorsAdd(ProtocolParser.ReadUInt32(stream));
					continue;
				}
			}
			else
			{
				if (keyInt == 40)
				{
					instance.Slotid = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 48)
				{
					instance.Scale = ProtocolParser.ReadUInt32(stream);
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
		IL_0060:
		return null;
	}

	public static Packet_HighlightBlocks DeserializeLengthDelimited(CitoMemoryStream stream, Packet_HighlightBlocks instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_HighlightBlocks packet_HighlightBlocks = Packet_HighlightBlocksSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_HighlightBlocks;
	}

	public static void Serialize(CitoStream stream, Packet_HighlightBlocks instance)
	{
		if (instance.Mode != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.Mode);
		}
		if (instance.Shape != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.Shape);
		}
		if (instance.Blocks != null)
		{
			stream.WriteByte(26);
			ProtocolParser.WriteBytes(stream, instance.Blocks);
		}
		if (instance.Colors != null)
		{
			int[] elems = instance.Colors;
			int elemCount = instance.ColorsCount;
			int i = 0;
			while (i < elems.Length && i < elemCount)
			{
				stream.WriteByte(32);
				ProtocolParser.WriteUInt32(stream, elems[i]);
				i++;
			}
		}
		if (instance.Slotid != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.Slotid);
		}
		if (instance.Scale != 0)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt32(stream, instance.Scale);
		}
	}

	public static int GetSize(Packet_HighlightBlocks instance)
	{
		int size = 0;
		if (instance.Mode != 0)
		{
			size += ProtocolParser.GetSize(instance.Mode) + 1;
		}
		if (instance.Shape != 0)
		{
			size += ProtocolParser.GetSize(instance.Shape) + 1;
		}
		if (instance.Blocks != null)
		{
			size += ProtocolParser.GetSize(instance.Blocks) + 1;
		}
		if (instance.Colors != null)
		{
			for (int i = 0; i < instance.ColorsCount; i++)
			{
				int i2 = instance.Colors[i];
				size += ProtocolParser.GetSize(i2) + 1;
			}
		}
		if (instance.Slotid != 0)
		{
			size += ProtocolParser.GetSize(instance.Slotid) + 1;
		}
		if (instance.Scale != 0)
		{
			size += ProtocolParser.GetSize(instance.Scale) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_HighlightBlocks instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_HighlightBlocksSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_HighlightBlocks instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_HighlightBlocksSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_HighlightBlocks instance)
	{
		byte[] data = Packet_HighlightBlocksSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
