using System;

public class Packet_IntMapSerializer
{
	public static Packet_IntMap DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_IntMap instance = new Packet_IntMap();
		Packet_IntMapSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_IntMap DeserializeBuffer(byte[] buffer, int length, Packet_IntMap instance)
	{
		Packet_IntMapSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_IntMap Deserialize(CitoMemoryStream stream, Packet_IntMap instance)
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
			if (keyInt <= 8)
			{
				if (keyInt == 0)
				{
					goto IL_004B;
				}
				if (keyInt == 8)
				{
					instance.DataAdd(ProtocolParser.ReadUInt32(stream));
					continue;
				}
			}
			else
			{
				if (keyInt == 16)
				{
					instance.Size = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 24)
				{
					instance.TopLeftPadding = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 32)
				{
					instance.BottomRightPadding = ProtocolParser.ReadUInt32(stream);
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
		IL_004B:
		return null;
	}

	public static Packet_IntMap DeserializeLengthDelimited(CitoMemoryStream stream, Packet_IntMap instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_IntMap packet_IntMap = Packet_IntMapSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_IntMap;
	}

	public static void Serialize(CitoStream stream, Packet_IntMap instance)
	{
		if (instance.Data != null)
		{
			int[] elems = instance.Data;
			int elemCount = instance.DataCount;
			int i = 0;
			while (i < elems.Length && i < elemCount)
			{
				stream.WriteByte(8);
				ProtocolParser.WriteUInt32(stream, elems[i]);
				i++;
			}
		}
		if (instance.Size != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.Size);
		}
		if (instance.TopLeftPadding != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.TopLeftPadding);
		}
		if (instance.BottomRightPadding != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.BottomRightPadding);
		}
	}

	public static int GetSize(Packet_IntMap instance)
	{
		int size = 0;
		if (instance.Data != null)
		{
			for (int i = 0; i < instance.DataCount; i++)
			{
				int i2 = instance.Data[i];
				size += ProtocolParser.GetSize(i2) + 1;
			}
		}
		if (instance.Size != 0)
		{
			size += ProtocolParser.GetSize(instance.Size) + 1;
		}
		if (instance.TopLeftPadding != 0)
		{
			size += ProtocolParser.GetSize(instance.TopLeftPadding) + 1;
		}
		if (instance.BottomRightPadding != 0)
		{
			size += ProtocolParser.GetSize(instance.BottomRightPadding) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_IntMap instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_IntMapSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_IntMap instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_IntMapSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_IntMap instance)
	{
		byte[] data = Packet_IntMapSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
