using System;

public class Packet_BlockDropSerializer
{
	public static Packet_BlockDrop DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_BlockDrop instance = new Packet_BlockDrop();
		Packet_BlockDropSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_BlockDrop DeserializeBuffer(byte[] buffer, int length, Packet_BlockDrop instance)
	{
		Packet_BlockDropSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_BlockDrop Deserialize(CitoMemoryStream stream, Packet_BlockDrop instance)
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
					goto IL_0054;
				}
				if (keyInt == 8)
				{
					instance.QuantityAvg = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 16)
				{
					instance.QuantityVar = ProtocolParser.ReadUInt32(stream);
					continue;
				}
			}
			else
			{
				if (keyInt == 24)
				{
					instance.QuantityDist = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 34)
				{
					instance.DroppedStack = ProtocolParser.ReadBytes(stream);
					continue;
				}
				if (keyInt == 40)
				{
					instance.Tool = ProtocolParser.ReadUInt32(stream);
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
		IL_0054:
		return null;
	}

	public static Packet_BlockDrop DeserializeLengthDelimited(CitoMemoryStream stream, Packet_BlockDrop instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_BlockDrop packet_BlockDrop = Packet_BlockDropSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_BlockDrop;
	}

	public static void Serialize(CitoStream stream, Packet_BlockDrop instance)
	{
		if (instance.QuantityAvg != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.QuantityAvg);
		}
		if (instance.QuantityVar != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.QuantityVar);
		}
		if (instance.QuantityDist != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.QuantityDist);
		}
		if (instance.DroppedStack != null)
		{
			stream.WriteByte(34);
			ProtocolParser.WriteBytes(stream, instance.DroppedStack);
		}
		if (instance.Tool != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.Tool);
		}
	}

	public static int GetSize(Packet_BlockDrop instance)
	{
		int size = 0;
		if (instance.QuantityAvg != 0)
		{
			size += ProtocolParser.GetSize(instance.QuantityAvg) + 1;
		}
		if (instance.QuantityVar != 0)
		{
			size += ProtocolParser.GetSize(instance.QuantityVar) + 1;
		}
		if (instance.QuantityDist != 0)
		{
			size += ProtocolParser.GetSize(instance.QuantityDist) + 1;
		}
		if (instance.DroppedStack != null)
		{
			size += ProtocolParser.GetSize(instance.DroppedStack) + 1;
		}
		if (instance.Tool != 0)
		{
			size += ProtocolParser.GetSize(instance.Tool) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_BlockDrop instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_BlockDropSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_BlockDrop instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_BlockDropSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_BlockDrop instance)
	{
		byte[] data = Packet_BlockDropSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
