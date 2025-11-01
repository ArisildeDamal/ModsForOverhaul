using System;

public class Packet_CrushingPropertiesSerializer
{
	public static Packet_CrushingProperties DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_CrushingProperties instance = new Packet_CrushingProperties();
		Packet_CrushingPropertiesSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_CrushingProperties DeserializeBuffer(byte[] buffer, int length, Packet_CrushingProperties instance)
	{
		Packet_CrushingPropertiesSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_CrushingProperties Deserialize(CitoMemoryStream stream, Packet_CrushingProperties instance)
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
					instance.CrushedStack = ProtocolParser.ReadBytes(stream);
					continue;
				}
			}
			else
			{
				if (keyInt == 16)
				{
					instance.HardnessTier = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 26)
				{
					if (instance.Quantity == null)
					{
						instance.Quantity = Packet_NatFloatSerializer.DeserializeLengthDelimitedNew(stream);
						continue;
					}
					Packet_NatFloatSerializer.DeserializeLengthDelimited(stream, instance.Quantity);
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

	public static Packet_CrushingProperties DeserializeLengthDelimited(CitoMemoryStream stream, Packet_CrushingProperties instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_CrushingProperties packet_CrushingProperties = Packet_CrushingPropertiesSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_CrushingProperties;
	}

	public static void Serialize(CitoStream stream, Packet_CrushingProperties instance)
	{
		if (instance.CrushedStack != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteBytes(stream, instance.CrushedStack);
		}
		if (instance.HardnessTier != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.HardnessTier);
		}
		if (instance.Quantity != null)
		{
			stream.WriteByte(26);
			Packet_NatFloatSerializer.SerializeWithSize(stream, instance.Quantity);
		}
	}

	public static int GetSize(Packet_CrushingProperties instance)
	{
		int size = 0;
		if (instance.CrushedStack != null)
		{
			size += ProtocolParser.GetSize(instance.CrushedStack) + 1;
		}
		if (instance.HardnessTier != 0)
		{
			size += ProtocolParser.GetSize(instance.HardnessTier) + 1;
		}
		if (instance.Quantity != null)
		{
			int packetlength = Packet_NatFloatSerializer.GetSize(instance.Quantity);
			size += packetlength + ProtocolParser.GetSize(packetlength) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_CrushingProperties instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_CrushingPropertiesSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_CrushingProperties instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_CrushingPropertiesSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_CrushingProperties instance)
	{
		byte[] data = Packet_CrushingPropertiesSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
