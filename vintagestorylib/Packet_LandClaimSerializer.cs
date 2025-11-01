using System;

public class Packet_LandClaimSerializer
{
	public static Packet_LandClaim DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_LandClaim instance = new Packet_LandClaim();
		Packet_LandClaimSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_LandClaim DeserializeBuffer(byte[] buffer, int length, Packet_LandClaim instance)
	{
		Packet_LandClaimSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_LandClaim Deserialize(CitoMemoryStream stream, Packet_LandClaim instance)
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
				instance.Data = ProtocolParser.ReadBytes(stream);
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

	public static Packet_LandClaim DeserializeLengthDelimited(CitoMemoryStream stream, Packet_LandClaim instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_LandClaim packet_LandClaim = Packet_LandClaimSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_LandClaim;
	}

	public static void Serialize(CitoStream stream, Packet_LandClaim instance)
	{
		if (instance.Data != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteBytes(stream, instance.Data);
		}
	}

	public static int GetSize(Packet_LandClaim instance)
	{
		int size = 0;
		if (instance.Data != null)
		{
			size += ProtocolParser.GetSize(instance.Data) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_LandClaim instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_LandClaimSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_LandClaim instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_LandClaimSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_LandClaim instance)
	{
		byte[] data = Packet_LandClaimSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
