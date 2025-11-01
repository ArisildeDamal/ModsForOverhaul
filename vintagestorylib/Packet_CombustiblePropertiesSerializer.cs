using System;

public class Packet_CombustiblePropertiesSerializer
{
	public static Packet_CombustibleProperties DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_CombustibleProperties instance = new Packet_CombustibleProperties();
		Packet_CombustiblePropertiesSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_CombustibleProperties DeserializeBuffer(byte[] buffer, int length, Packet_CombustibleProperties instance)
	{
		Packet_CombustiblePropertiesSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_CombustibleProperties Deserialize(CitoMemoryStream stream, Packet_CombustibleProperties instance)
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
			if (keyInt <= 32)
			{
				if (keyInt <= 8)
				{
					if (keyInt == 0)
					{
						goto IL_0095;
					}
					if (keyInt == 8)
					{
						instance.BurnTemperature = ProtocolParser.ReadUInt32(stream);
						continue;
					}
				}
				else
				{
					if (keyInt == 16)
					{
						instance.BurnDuration = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 24)
					{
						instance.HeatResistance = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 32)
					{
						instance.MeltingPoint = ProtocolParser.ReadUInt32(stream);
						continue;
					}
				}
			}
			else if (keyInt <= 56)
			{
				if (keyInt == 40)
				{
					instance.MeltingDuration = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 50)
				{
					instance.SmeltedStack = ProtocolParser.ReadBytes(stream);
					continue;
				}
				if (keyInt == 56)
				{
					instance.SmeltedRatio = ProtocolParser.ReadUInt32(stream);
					continue;
				}
			}
			else
			{
				if (keyInt == 64)
				{
					instance.RequiresContainer = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 72)
				{
					instance.MeltingType = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 80)
				{
					instance.MaxTemperature = ProtocolParser.ReadUInt32(stream);
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
		IL_0095:
		return null;
	}

	public static Packet_CombustibleProperties DeserializeLengthDelimited(CitoMemoryStream stream, Packet_CombustibleProperties instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_CombustibleProperties packet_CombustibleProperties = Packet_CombustiblePropertiesSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_CombustibleProperties;
	}

	public static void Serialize(CitoStream stream, Packet_CombustibleProperties instance)
	{
		if (instance.BurnTemperature != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.BurnTemperature);
		}
		if (instance.BurnDuration != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.BurnDuration);
		}
		if (instance.HeatResistance != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.HeatResistance);
		}
		if (instance.MeltingPoint != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.MeltingPoint);
		}
		if (instance.MeltingDuration != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.MeltingDuration);
		}
		if (instance.SmeltedStack != null)
		{
			stream.WriteByte(50);
			ProtocolParser.WriteBytes(stream, instance.SmeltedStack);
		}
		if (instance.SmeltedRatio != 0)
		{
			stream.WriteByte(56);
			ProtocolParser.WriteUInt32(stream, instance.SmeltedRatio);
		}
		if (instance.RequiresContainer != 0)
		{
			stream.WriteByte(64);
			ProtocolParser.WriteUInt32(stream, instance.RequiresContainer);
		}
		if (instance.MeltingType != 0)
		{
			stream.WriteByte(72);
			ProtocolParser.WriteUInt32(stream, instance.MeltingType);
		}
		if (instance.MaxTemperature != 0)
		{
			stream.WriteByte(80);
			ProtocolParser.WriteUInt32(stream, instance.MaxTemperature);
		}
	}

	public static int GetSize(Packet_CombustibleProperties instance)
	{
		int size = 0;
		if (instance.BurnTemperature != 0)
		{
			size += ProtocolParser.GetSize(instance.BurnTemperature) + 1;
		}
		if (instance.BurnDuration != 0)
		{
			size += ProtocolParser.GetSize(instance.BurnDuration) + 1;
		}
		if (instance.HeatResistance != 0)
		{
			size += ProtocolParser.GetSize(instance.HeatResistance) + 1;
		}
		if (instance.MeltingPoint != 0)
		{
			size += ProtocolParser.GetSize(instance.MeltingPoint) + 1;
		}
		if (instance.MeltingDuration != 0)
		{
			size += ProtocolParser.GetSize(instance.MeltingDuration) + 1;
		}
		if (instance.SmeltedStack != null)
		{
			size += ProtocolParser.GetSize(instance.SmeltedStack) + 1;
		}
		if (instance.SmeltedRatio != 0)
		{
			size += ProtocolParser.GetSize(instance.SmeltedRatio) + 1;
		}
		if (instance.RequiresContainer != 0)
		{
			size += ProtocolParser.GetSize(instance.RequiresContainer) + 1;
		}
		if (instance.MeltingType != 0)
		{
			size += ProtocolParser.GetSize(instance.MeltingType) + 1;
		}
		if (instance.MaxTemperature != 0)
		{
			size += ProtocolParser.GetSize(instance.MaxTemperature) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_CombustibleProperties instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_CombustiblePropertiesSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_CombustibleProperties instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_CombustiblePropertiesSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_CombustibleProperties instance)
	{
		byte[] data = Packet_CombustiblePropertiesSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
