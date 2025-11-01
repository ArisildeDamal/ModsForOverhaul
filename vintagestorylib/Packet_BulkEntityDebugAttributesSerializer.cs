using System;

public class Packet_BulkEntityDebugAttributesSerializer
{
	public static Packet_BulkEntityDebugAttributes DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_BulkEntityDebugAttributes instance = new Packet_BulkEntityDebugAttributes();
		Packet_BulkEntityDebugAttributesSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_BulkEntityDebugAttributes DeserializeBuffer(byte[] buffer, int length, Packet_BulkEntityDebugAttributes instance)
	{
		Packet_BulkEntityDebugAttributesSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_BulkEntityDebugAttributes Deserialize(CitoMemoryStream stream, Packet_BulkEntityDebugAttributes instance)
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
				instance.FullUpdatesAdd(Packet_EntityAttributesSerializer.DeserializeLengthDelimitedNew(stream));
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

	public static Packet_BulkEntityDebugAttributes DeserializeLengthDelimited(CitoMemoryStream stream, Packet_BulkEntityDebugAttributes instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_BulkEntityDebugAttributes packet_BulkEntityDebugAttributes = Packet_BulkEntityDebugAttributesSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_BulkEntityDebugAttributes;
	}

	public static void Serialize(CitoStream stream, Packet_BulkEntityDebugAttributes instance)
	{
		if (instance.FullUpdates != null)
		{
			Packet_EntityAttributes[] elems = instance.FullUpdates;
			int elemCount = instance.FullUpdatesCount;
			int i = 0;
			while (i < elems.Length && i < elemCount)
			{
				stream.WriteByte(10);
				Packet_EntityAttributesSerializer.SerializeWithSize(stream, elems[i]);
				i++;
			}
		}
	}

	public static int GetSize(Packet_BulkEntityDebugAttributes instance)
	{
		int size = 0;
		if (instance.FullUpdates != null)
		{
			for (int i = 0; i < instance.FullUpdatesCount; i++)
			{
				int packetlength = Packet_EntityAttributesSerializer.GetSize(instance.FullUpdates[i]);
				size += packetlength + ProtocolParser.GetSize(packetlength) + 1;
			}
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_BulkEntityDebugAttributes instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_BulkEntityDebugAttributesSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_BulkEntityDebugAttributes instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_BulkEntityDebugAttributesSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_BulkEntityDebugAttributes instance)
	{
		byte[] data = Packet_BulkEntityDebugAttributesSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
