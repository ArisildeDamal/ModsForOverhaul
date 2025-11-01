using System;

public class Packet_BulkEntityPositionSerializer
{
	public static Packet_BulkEntityPosition DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_BulkEntityPosition instance = new Packet_BulkEntityPosition();
		Packet_BulkEntityPositionSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_BulkEntityPosition DeserializeBuffer(byte[] buffer, int length, Packet_BulkEntityPosition instance)
	{
		Packet_BulkEntityPositionSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_BulkEntityPosition Deserialize(CitoMemoryStream stream, Packet_BulkEntityPosition instance)
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
				instance.EntityPositionsAdd(Packet_EntityPositionSerializer.DeserializeLengthDelimitedNew(stream));
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

	public static Packet_BulkEntityPosition DeserializeLengthDelimited(CitoMemoryStream stream, Packet_BulkEntityPosition instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_BulkEntityPosition packet_BulkEntityPosition = Packet_BulkEntityPositionSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_BulkEntityPosition;
	}

	public static void Serialize(CitoStream stream, Packet_BulkEntityPosition instance)
	{
		if (instance.EntityPositions != null)
		{
			Packet_EntityPosition[] elems = instance.EntityPositions;
			int elemCount = instance.EntityPositionsCount;
			int i = 0;
			while (i < elems.Length && i < elemCount)
			{
				stream.WriteByte(10);
				Packet_EntityPositionSerializer.SerializeWithSize(stream, elems[i]);
				i++;
			}
		}
	}

	public static int GetSize(Packet_BulkEntityPosition instance)
	{
		int size = 0;
		if (instance.EntityPositions != null)
		{
			for (int i = 0; i < instance.EntityPositionsCount; i++)
			{
				int packetlength = Packet_EntityPositionSerializer.GetSize(instance.EntityPositions[i]);
				size += packetlength + ProtocolParser.GetSize(packetlength) + 1;
			}
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_BulkEntityPosition instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_BulkEntityPositionSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_BulkEntityPosition instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_BulkEntityPositionSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_BulkEntityPosition instance)
	{
		byte[] data = Packet_BulkEntityPositionSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
