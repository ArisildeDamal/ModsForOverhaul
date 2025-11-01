using System;

public class Packet_TransitionablePropertiesSerializer
{
	public static Packet_TransitionableProperties DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_TransitionableProperties instance = new Packet_TransitionableProperties();
		Packet_TransitionablePropertiesSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_TransitionableProperties DeserializeBuffer(byte[] buffer, int length, Packet_TransitionableProperties instance)
	{
		Packet_TransitionablePropertiesSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_TransitionableProperties Deserialize(CitoMemoryStream stream, Packet_TransitionableProperties instance)
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
			if (keyInt <= 18)
			{
				if (keyInt == 0)
				{
					goto IL_005B;
				}
				if (keyInt != 10)
				{
					if (keyInt == 18)
					{
						if (instance.TransitionHours == null)
						{
							instance.TransitionHours = Packet_NatFloatSerializer.DeserializeLengthDelimitedNew(stream);
							continue;
						}
						Packet_NatFloatSerializer.DeserializeLengthDelimited(stream, instance.TransitionHours);
						continue;
					}
				}
				else
				{
					if (instance.FreshHours == null)
					{
						instance.FreshHours = Packet_NatFloatSerializer.DeserializeLengthDelimitedNew(stream);
						continue;
					}
					Packet_NatFloatSerializer.DeserializeLengthDelimited(stream, instance.FreshHours);
					continue;
				}
			}
			else
			{
				if (keyInt == 26)
				{
					instance.TransitionedStack = ProtocolParser.ReadBytes(stream);
					continue;
				}
				if (keyInt == 32)
				{
					instance.TransitionRatio = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 40)
				{
					instance.Type = ProtocolParser.ReadUInt32(stream);
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
		IL_005B:
		return null;
	}

	public static Packet_TransitionableProperties DeserializeLengthDelimited(CitoMemoryStream stream, Packet_TransitionableProperties instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_TransitionableProperties packet_TransitionableProperties = Packet_TransitionablePropertiesSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_TransitionableProperties;
	}

	public static void Serialize(CitoStream stream, Packet_TransitionableProperties instance)
	{
		if (instance.FreshHours != null)
		{
			stream.WriteByte(10);
			Packet_NatFloatSerializer.SerializeWithSize(stream, instance.FreshHours);
		}
		if (instance.TransitionHours != null)
		{
			stream.WriteByte(18);
			Packet_NatFloatSerializer.SerializeWithSize(stream, instance.TransitionHours);
		}
		if (instance.TransitionedStack != null)
		{
			stream.WriteByte(26);
			ProtocolParser.WriteBytes(stream, instance.TransitionedStack);
		}
		if (instance.TransitionRatio != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.TransitionRatio);
		}
		if (instance.Type != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.Type);
		}
	}

	public static int GetSize(Packet_TransitionableProperties instance)
	{
		int size = 0;
		if (instance.FreshHours != null)
		{
			int packetlength = Packet_NatFloatSerializer.GetSize(instance.FreshHours);
			size += packetlength + ProtocolParser.GetSize(packetlength) + 1;
		}
		if (instance.TransitionHours != null)
		{
			int packetlength2 = Packet_NatFloatSerializer.GetSize(instance.TransitionHours);
			size += packetlength2 + ProtocolParser.GetSize(packetlength2) + 1;
		}
		if (instance.TransitionedStack != null)
		{
			size += ProtocolParser.GetSize(instance.TransitionedStack) + 1;
		}
		if (instance.TransitionRatio != 0)
		{
			size += ProtocolParser.GetSize(instance.TransitionRatio) + 1;
		}
		if (instance.Type != 0)
		{
			size += ProtocolParser.GetSize(instance.Type) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_TransitionableProperties instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_TransitionablePropertiesSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_TransitionableProperties instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_TransitionablePropertiesSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_TransitionableProperties instance)
	{
		byte[] data = Packet_TransitionablePropertiesSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
