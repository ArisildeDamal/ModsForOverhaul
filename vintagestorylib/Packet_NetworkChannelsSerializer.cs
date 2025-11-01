using System;

public class Packet_NetworkChannelsSerializer
{
	public static Packet_NetworkChannels DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_NetworkChannels instance = new Packet_NetworkChannels();
		Packet_NetworkChannelsSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_NetworkChannels DeserializeBuffer(byte[] buffer, int length, Packet_NetworkChannels instance)
	{
		Packet_NetworkChannelsSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_NetworkChannels Deserialize(CitoMemoryStream stream, Packet_NetworkChannels instance)
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
					instance.ChannelIdsAdd(ProtocolParser.ReadUInt32(stream));
					continue;
				}
			}
			else
			{
				if (keyInt == 18)
				{
					instance.ChannelNamesAdd(ProtocolParser.ReadString(stream));
					continue;
				}
				if (keyInt == 24)
				{
					instance.ChannelUdpIdsAdd(ProtocolParser.ReadUInt32(stream));
					continue;
				}
				if (keyInt == 34)
				{
					instance.ChannelUdpNamesAdd(ProtocolParser.ReadString(stream));
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

	public static Packet_NetworkChannels DeserializeLengthDelimited(CitoMemoryStream stream, Packet_NetworkChannels instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_NetworkChannels packet_NetworkChannels = Packet_NetworkChannelsSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_NetworkChannels;
	}

	public static void Serialize(CitoStream stream, Packet_NetworkChannels instance)
	{
		if (instance.ChannelIds != null)
		{
			int[] elems = instance.ChannelIds;
			int elemCount = instance.ChannelIdsCount;
			int i = 0;
			while (i < elems.Length && i < elemCount)
			{
				stream.WriteByte(8);
				ProtocolParser.WriteUInt32(stream, elems[i]);
				i++;
			}
		}
		if (instance.ChannelNames != null)
		{
			string[] elems2 = instance.ChannelNames;
			int elemCount2 = instance.ChannelNamesCount;
			int j = 0;
			while (j < elems2.Length && j < elemCount2)
			{
				stream.WriteByte(18);
				ProtocolParser.WriteString(stream, elems2[j]);
				j++;
			}
		}
		if (instance.ChannelUdpIds != null)
		{
			int[] elems3 = instance.ChannelUdpIds;
			int elemCount3 = instance.ChannelUdpIdsCount;
			int k = 0;
			while (k < elems3.Length && k < elemCount3)
			{
				stream.WriteByte(24);
				ProtocolParser.WriteUInt32(stream, elems3[k]);
				k++;
			}
		}
		if (instance.ChannelUdpNames != null)
		{
			string[] elems4 = instance.ChannelUdpNames;
			int elemCount4 = instance.ChannelUdpNamesCount;
			int l = 0;
			while (l < elems4.Length && l < elemCount4)
			{
				stream.WriteByte(34);
				ProtocolParser.WriteString(stream, elems4[l]);
				l++;
			}
		}
	}

	public static int GetSize(Packet_NetworkChannels instance)
	{
		int size = 0;
		if (instance.ChannelIds != null)
		{
			for (int i = 0; i < instance.ChannelIdsCount; i++)
			{
				int i2 = instance.ChannelIds[i];
				size += ProtocolParser.GetSize(i2) + 1;
			}
		}
		if (instance.ChannelNames != null)
		{
			for (int j = 0; j < instance.ChannelNamesCount; j++)
			{
				string i3 = instance.ChannelNames[j];
				size += ProtocolParser.GetSize(i3) + 1;
			}
		}
		if (instance.ChannelUdpIds != null)
		{
			for (int k = 0; k < instance.ChannelUdpIdsCount; k++)
			{
				int i4 = instance.ChannelUdpIds[k];
				size += ProtocolParser.GetSize(i4) + 1;
			}
		}
		if (instance.ChannelUdpNames != null)
		{
			for (int l = 0; l < instance.ChannelUdpNamesCount; l++)
			{
				string i5 = instance.ChannelUdpNames[l];
				size += ProtocolParser.GetSize(i5) + 1;
			}
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_NetworkChannels instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_NetworkChannelsSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_NetworkChannels instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_NetworkChannelsSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_NetworkChannels instance)
	{
		byte[] data = Packet_NetworkChannelsSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
