using System;

public class Packet_LandClaimsSerializer
{
	public static Packet_LandClaims DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_LandClaims instance = new Packet_LandClaims();
		Packet_LandClaimsSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_LandClaims DeserializeBuffer(byte[] buffer, int length, Packet_LandClaims instance)
	{
		Packet_LandClaimsSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_LandClaims Deserialize(CitoMemoryStream stream, Packet_LandClaims instance)
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
					instance.AddclaimsAdd(Packet_LandClaimSerializer.DeserializeLengthDelimitedNew(stream));
				}
			}
			else
			{
				instance.AllclaimsAdd(Packet_LandClaimSerializer.DeserializeLengthDelimitedNew(stream));
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

	public static Packet_LandClaims DeserializeLengthDelimited(CitoMemoryStream stream, Packet_LandClaims instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_LandClaims packet_LandClaims = Packet_LandClaimsSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_LandClaims;
	}

	public static void Serialize(CitoStream stream, Packet_LandClaims instance)
	{
		if (instance.Allclaims != null)
		{
			Packet_LandClaim[] elems = instance.Allclaims;
			int elemCount = instance.AllclaimsCount;
			int i = 0;
			while (i < elems.Length && i < elemCount)
			{
				stream.WriteByte(10);
				Packet_LandClaimSerializer.SerializeWithSize(stream, elems[i]);
				i++;
			}
		}
		if (instance.Addclaims != null)
		{
			Packet_LandClaim[] elems2 = instance.Addclaims;
			int elemCount2 = instance.AddclaimsCount;
			int j = 0;
			while (j < elems2.Length && j < elemCount2)
			{
				stream.WriteByte(18);
				Packet_LandClaimSerializer.SerializeWithSize(stream, elems2[j]);
				j++;
			}
		}
	}

	public static int GetSize(Packet_LandClaims instance)
	{
		int size = 0;
		if (instance.Allclaims != null)
		{
			for (int i = 0; i < instance.AllclaimsCount; i++)
			{
				int packetlength = Packet_LandClaimSerializer.GetSize(instance.Allclaims[i]);
				size += packetlength + ProtocolParser.GetSize(packetlength) + 1;
			}
		}
		if (instance.Addclaims != null)
		{
			for (int j = 0; j < instance.AddclaimsCount; j++)
			{
				int packetlength2 = Packet_LandClaimSerializer.GetSize(instance.Addclaims[j]);
				size += packetlength2 + ProtocolParser.GetSize(packetlength2) + 1;
			}
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_LandClaims instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_LandClaimsSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_LandClaims instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_LandClaimsSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_LandClaims instance)
	{
		byte[] data = Packet_LandClaimsSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
