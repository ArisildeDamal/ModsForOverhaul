using System;

public class Packet_BehaviorSerializer
{
	public static Packet_Behavior DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_Behavior instance = new Packet_Behavior();
		Packet_BehaviorSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_Behavior DeserializeBuffer(byte[] buffer, int length, Packet_Behavior instance)
	{
		Packet_BehaviorSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_Behavior Deserialize(CitoMemoryStream stream, Packet_Behavior instance)
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
					instance.Code = ProtocolParser.ReadString(stream);
					continue;
				}
			}
			else
			{
				if (keyInt == 18)
				{
					instance.Attributes = ProtocolParser.ReadString(stream);
					continue;
				}
				if (keyInt == 24)
				{
					instance.ClientSideOptional = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_Behavior DeserializeLengthDelimited(CitoMemoryStream stream, Packet_Behavior instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_Behavior packet_Behavior = Packet_BehaviorSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_Behavior;
	}

	public static void Serialize(CitoStream stream, Packet_Behavior instance)
	{
		if (instance.Code != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.Code);
		}
		if (instance.Attributes != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteString(stream, instance.Attributes);
		}
		if (instance.ClientSideOptional != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.ClientSideOptional);
		}
	}

	public static int GetSize(Packet_Behavior instance)
	{
		int size = 0;
		if (instance.Code != null)
		{
			size += ProtocolParser.GetSize(instance.Code) + 1;
		}
		if (instance.Attributes != null)
		{
			size += ProtocolParser.GetSize(instance.Attributes) + 1;
		}
		if (instance.ClientSideOptional != 0)
		{
			size += ProtocolParser.GetSize(instance.ClientSideOptional) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_Behavior instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_BehaviorSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_Behavior instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_BehaviorSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_Behavior instance)
	{
		byte[] data = Packet_BehaviorSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
