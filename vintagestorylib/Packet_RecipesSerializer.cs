using System;

public class Packet_RecipesSerializer
{
	public static Packet_Recipes DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_Recipes instance = new Packet_Recipes();
		Packet_RecipesSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_Recipes DeserializeBuffer(byte[] buffer, int length, Packet_Recipes instance)
	{
		Packet_RecipesSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_Recipes Deserialize(CitoMemoryStream stream, Packet_Recipes instance)
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
				if (keyInt == 16)
				{
					instance.Quantity = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 26)
				{
					instance.Data = ProtocolParser.ReadBytes(stream);
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

	public static Packet_Recipes DeserializeLengthDelimited(CitoMemoryStream stream, Packet_Recipes instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_Recipes packet_Recipes = Packet_RecipesSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_Recipes;
	}

	public static void Serialize(CitoStream stream, Packet_Recipes instance)
	{
		if (instance.Code != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.Code);
		}
		if (instance.Quantity != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.Quantity);
		}
		if (instance.Data != null)
		{
			stream.WriteByte(26);
			ProtocolParser.WriteBytes(stream, instance.Data);
		}
	}

	public static int GetSize(Packet_Recipes instance)
	{
		int size = 0;
		if (instance.Code != null)
		{
			size += ProtocolParser.GetSize(instance.Code) + 1;
		}
		if (instance.Quantity != 0)
		{
			size += ProtocolParser.GetSize(instance.Quantity) + 1;
		}
		if (instance.Data != null)
		{
			size += ProtocolParser.GetSize(instance.Data) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_Recipes instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_RecipesSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_Recipes instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_RecipesSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_Recipes instance)
	{
		byte[] data = Packet_RecipesSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
