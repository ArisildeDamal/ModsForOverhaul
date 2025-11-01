using System;

public class Packet_ServerPlayerPingSerializer
{
	public static Packet_ServerPlayerPing DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ServerPlayerPing instance = new Packet_ServerPlayerPing();
		Packet_ServerPlayerPingSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ServerPlayerPing DeserializeBuffer(byte[] buffer, int length, Packet_ServerPlayerPing instance)
	{
		Packet_ServerPlayerPingSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ServerPlayerPing Deserialize(CitoMemoryStream stream, Packet_ServerPlayerPing instance)
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
				goto IL_003B;
			}
			if (keyInt != 8)
			{
				if (keyInt != 16)
				{
					ProtocolParser.SkipKey(stream, Key.Create(keyInt));
				}
				else
				{
					instance.PingsAdd(ProtocolParser.ReadUInt32(stream));
				}
			}
			else
			{
				instance.ClientIdsAdd(ProtocolParser.ReadUInt32(stream));
			}
		}
		if (keyInt >= 0)
		{
			return null;
		}
		return instance;
		IL_003B:
		return null;
	}

	public static Packet_ServerPlayerPing DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ServerPlayerPing instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ServerPlayerPing packet_ServerPlayerPing = Packet_ServerPlayerPingSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_ServerPlayerPing;
	}

	public static void Serialize(CitoStream stream, Packet_ServerPlayerPing instance)
	{
		if (instance.ClientIds != null)
		{
			int[] elems = instance.ClientIds;
			int elemCount = instance.ClientIdsCount;
			int i = 0;
			while (i < elems.Length && i < elemCount)
			{
				stream.WriteByte(8);
				ProtocolParser.WriteUInt32(stream, elems[i]);
				i++;
			}
		}
		if (instance.Pings != null)
		{
			int[] elems2 = instance.Pings;
			int elemCount2 = instance.PingsCount;
			int j = 0;
			while (j < elems2.Length && j < elemCount2)
			{
				stream.WriteByte(16);
				ProtocolParser.WriteUInt32(stream, elems2[j]);
				j++;
			}
		}
	}

	public static int GetSize(Packet_ServerPlayerPing instance)
	{
		int size = 0;
		if (instance.ClientIds != null)
		{
			for (int i = 0; i < instance.ClientIdsCount; i++)
			{
				int i2 = instance.ClientIds[i];
				size += ProtocolParser.GetSize(i2) + 1;
			}
		}
		if (instance.Pings != null)
		{
			for (int j = 0; j < instance.PingsCount; j++)
			{
				int i3 = instance.Pings[j];
				size += ProtocolParser.GetSize(i3) + 1;
			}
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ServerPlayerPing instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_ServerPlayerPingSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_ServerPlayerPing instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_ServerPlayerPingSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ServerPlayerPing instance)
	{
		byte[] data = Packet_ServerPlayerPingSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
