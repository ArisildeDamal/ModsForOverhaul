using System;

public class Packet_IngameDiscoverySerializer
{
	public static Packet_IngameDiscovery DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_IngameDiscovery instance = new Packet_IngameDiscovery();
		Packet_IngameDiscoverySerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_IngameDiscovery DeserializeBuffer(byte[] buffer, int length, Packet_IngameDiscovery instance)
	{
		Packet_IngameDiscoverySerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_IngameDiscovery Deserialize(CitoMemoryStream stream, Packet_IngameDiscovery instance)
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
					instance.Message = ProtocolParser.ReadString(stream);
					continue;
				}
				if (keyInt == 26)
				{
					instance.LangParamsAdd(ProtocolParser.ReadString(stream));
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

	public static Packet_IngameDiscovery DeserializeLengthDelimited(CitoMemoryStream stream, Packet_IngameDiscovery instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_IngameDiscovery packet_IngameDiscovery = Packet_IngameDiscoverySerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_IngameDiscovery;
	}

	public static void Serialize(CitoStream stream, Packet_IngameDiscovery instance)
	{
		if (instance.Code != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.Code);
		}
		if (instance.Message != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteString(stream, instance.Message);
		}
		if (instance.LangParams != null)
		{
			string[] elems = instance.LangParams;
			int elemCount = instance.LangParamsCount;
			int i = 0;
			while (i < elems.Length && i < elemCount)
			{
				stream.WriteByte(26);
				ProtocolParser.WriteString(stream, elems[i]);
				i++;
			}
		}
	}

	public static int GetSize(Packet_IngameDiscovery instance)
	{
		int size = 0;
		if (instance.Code != null)
		{
			size += ProtocolParser.GetSize(instance.Code) + 1;
		}
		if (instance.Message != null)
		{
			size += ProtocolParser.GetSize(instance.Message) + 1;
		}
		if (instance.LangParams != null)
		{
			for (int i = 0; i < instance.LangParamsCount; i++)
			{
				string i2 = instance.LangParams[i];
				size += ProtocolParser.GetSize(i2) + 1;
			}
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_IngameDiscovery instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_IngameDiscoverySerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_IngameDiscovery instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_IngameDiscoverySerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_IngameDiscovery instance)
	{
		byte[] data = Packet_IngameDiscoverySerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
