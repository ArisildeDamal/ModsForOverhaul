using System;

public class Packet_BulkEntityAttributesSerializer
{
	public static Packet_BulkEntityAttributes DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_BulkEntityAttributes instance = new Packet_BulkEntityAttributes();
		Packet_BulkEntityAttributesSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_BulkEntityAttributes DeserializeBuffer(byte[] buffer, int length, Packet_BulkEntityAttributes instance)
	{
		Packet_BulkEntityAttributesSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_BulkEntityAttributes Deserialize(CitoMemoryStream stream, Packet_BulkEntityAttributes instance)
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
				goto IL_003C;
			}
			if (keyInt != 10)
			{
				if (keyInt != 18)
				{
					ProtocolParser.SkipKey(stream, Key.Create(keyInt));
				}
				else
				{
					instance.PartialUpdatesAdd(Packet_EntityAttributeUpdateSerializer.DeserializeLengthDelimitedNew(stream));
				}
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
		IL_003C:
		return null;
	}

	public static Packet_BulkEntityAttributes DeserializeLengthDelimited(CitoMemoryStream stream, Packet_BulkEntityAttributes instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_BulkEntityAttributes packet_BulkEntityAttributes = Packet_BulkEntityAttributesSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_BulkEntityAttributes;
	}

	public static void Serialize(CitoStream stream, Packet_BulkEntityAttributes instance)
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
		if (instance.PartialUpdates != null)
		{
			Packet_EntityAttributeUpdate[] elems2 = instance.PartialUpdates;
			int elemCount2 = instance.PartialUpdatesCount;
			int j = 0;
			while (j < elems2.Length && j < elemCount2)
			{
				stream.WriteByte(18);
				Packet_EntityAttributeUpdateSerializer.SerializeWithSize(stream, elems2[j]);
				j++;
			}
		}
	}

	public static int GetSize(Packet_BulkEntityAttributes instance)
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
		if (instance.PartialUpdates != null)
		{
			for (int j = 0; j < instance.PartialUpdatesCount; j++)
			{
				int packetlength2 = Packet_EntityAttributeUpdateSerializer.GetSize(instance.PartialUpdates[j]);
				size += packetlength2 + ProtocolParser.GetSize(packetlength2) + 1;
			}
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_BulkEntityAttributes instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_BulkEntityAttributesSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_BulkEntityAttributes instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_BulkEntityAttributesSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_BulkEntityAttributes instance)
	{
		byte[] data = Packet_BulkEntityAttributesSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
