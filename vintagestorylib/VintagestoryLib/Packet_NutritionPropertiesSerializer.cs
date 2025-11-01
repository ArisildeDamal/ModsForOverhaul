using System;

public class Packet_NutritionPropertiesSerializer
{
	public static Packet_NutritionProperties DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_NutritionProperties instance = new Packet_NutritionProperties();
		Packet_NutritionPropertiesSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_NutritionProperties DeserializeBuffer(byte[] buffer, int length, Packet_NutritionProperties instance)
	{
		Packet_NutritionPropertiesSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_NutritionProperties Deserialize(CitoMemoryStream stream, Packet_NutritionProperties instance)
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
					instance.FoodCategory = ProtocolParser.ReadUInt32(stream);
					continue;
				}
			}
			else
			{
				if (keyInt == 16)
				{
					instance.Saturation = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 24)
				{
					instance.Health = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 34)
				{
					instance.EatenStack = ProtocolParser.ReadBytes(stream);
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

	public static Packet_NutritionProperties DeserializeLengthDelimited(CitoMemoryStream stream, Packet_NutritionProperties instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_NutritionProperties packet_NutritionProperties = Packet_NutritionPropertiesSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_NutritionProperties;
	}

	public static void Serialize(CitoStream stream, Packet_NutritionProperties instance)
	{
		if (instance.FoodCategory != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.FoodCategory);
		}
		if (instance.Saturation != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.Saturation);
		}
		if (instance.Health != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.Health);
		}
		if (instance.EatenStack != null)
		{
			stream.WriteByte(34);
			ProtocolParser.WriteBytes(stream, instance.EatenStack);
		}
	}

	public static int GetSize(Packet_NutritionProperties instance)
	{
		int size = 0;
		if (instance.FoodCategory != 0)
		{
			size += ProtocolParser.GetSize(instance.FoodCategory) + 1;
		}
		if (instance.Saturation != 0)
		{
			size += ProtocolParser.GetSize(instance.Saturation) + 1;
		}
		if (instance.Health != 0)
		{
			size += ProtocolParser.GetSize(instance.Health) + 1;
		}
		if (instance.EatenStack != null)
		{
			size += ProtocolParser.GetSize(instance.EatenStack) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_NutritionProperties instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_NutritionPropertiesSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_NutritionProperties instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_NutritionPropertiesSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_NutritionProperties instance)
	{
		byte[] data = Packet_NutritionPropertiesSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
