using System;

public class Packet_GrindingPropertiesSerializer
{
	public static Packet_GrindingProperties DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_GrindingProperties instance = new Packet_GrindingProperties();
		Packet_GrindingPropertiesSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_GrindingProperties DeserializeBuffer(byte[] buffer, int length, Packet_GrindingProperties instance)
	{
		Packet_GrindingPropertiesSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_GrindingProperties Deserialize(CitoMemoryStream stream, Packet_GrindingProperties instance)
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
				instance.GroundStack = ProtocolParser.ReadBytes(stream);
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

	public static Packet_GrindingProperties DeserializeLengthDelimited(CitoMemoryStream stream, Packet_GrindingProperties instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_GrindingProperties packet_GrindingProperties = Packet_GrindingPropertiesSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_GrindingProperties;
	}

	public static void Serialize(CitoStream stream, Packet_GrindingProperties instance)
	{
		if (instance.GroundStack != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteBytes(stream, instance.GroundStack);
		}
	}

	public static int GetSize(Packet_GrindingProperties instance)
	{
		int size = 0;
		if (instance.GroundStack != null)
		{
			size += ProtocolParser.GetSize(instance.GroundStack) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_GrindingProperties instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_GrindingPropertiesSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_GrindingProperties instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_GrindingPropertiesSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_GrindingProperties instance)
	{
		byte[] data = Packet_GrindingPropertiesSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
