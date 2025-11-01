using System;

public class Packet_PlayerGroupSerializer
{
	public static Packet_PlayerGroup DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_PlayerGroup instance = new Packet_PlayerGroup();
		Packet_PlayerGroupSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_PlayerGroup DeserializeBuffer(byte[] buffer, int length, Packet_PlayerGroup instance)
	{
		Packet_PlayerGroupSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_PlayerGroup Deserialize(CitoMemoryStream stream, Packet_PlayerGroup instance)
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
					goto IL_0060;
				}
				if (keyInt == 8)
				{
					instance.Uid = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 18)
				{
					instance.Owneruid = ProtocolParser.ReadString(stream);
					continue;
				}
			}
			else if (keyInt <= 34)
			{
				if (keyInt == 26)
				{
					instance.Name = ProtocolParser.ReadString(stream);
					continue;
				}
				if (keyInt == 34)
				{
					instance.ChathistoryAdd(Packet_ChatLineSerializer.DeserializeLengthDelimitedNew(stream));
					continue;
				}
			}
			else
			{
				if (keyInt == 40)
				{
					instance.Createdbyprivatemessage = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 48)
				{
					instance.Membership = ProtocolParser.ReadUInt32(stream);
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
		IL_0060:
		return null;
	}

	public static Packet_PlayerGroup DeserializeLengthDelimited(CitoMemoryStream stream, Packet_PlayerGroup instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_PlayerGroup packet_PlayerGroup = Packet_PlayerGroupSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_PlayerGroup;
	}

	public static void Serialize(CitoStream stream, Packet_PlayerGroup instance)
	{
		if (instance.Uid != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.Uid);
		}
		if (instance.Owneruid != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteString(stream, instance.Owneruid);
		}
		if (instance.Name != null)
		{
			stream.WriteByte(26);
			ProtocolParser.WriteString(stream, instance.Name);
		}
		if (instance.Chathistory != null)
		{
			Packet_ChatLine[] elems = instance.Chathistory;
			int elemCount = instance.ChathistoryCount;
			int i = 0;
			while (i < elems.Length && i < elemCount)
			{
				stream.WriteByte(34);
				Packet_ChatLineSerializer.SerializeWithSize(stream, elems[i]);
				i++;
			}
		}
		if (instance.Createdbyprivatemessage != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.Createdbyprivatemessage);
		}
		if (instance.Membership != 0)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt32(stream, instance.Membership);
		}
	}

	public static int GetSize(Packet_PlayerGroup instance)
	{
		int size = 0;
		if (instance.Uid != 0)
		{
			size += ProtocolParser.GetSize(instance.Uid) + 1;
		}
		if (instance.Owneruid != null)
		{
			size += ProtocolParser.GetSize(instance.Owneruid) + 1;
		}
		if (instance.Name != null)
		{
			size += ProtocolParser.GetSize(instance.Name) + 1;
		}
		if (instance.Chathistory != null)
		{
			for (int i = 0; i < instance.ChathistoryCount; i++)
			{
				int packetlength = Packet_ChatLineSerializer.GetSize(instance.Chathistory[i]);
				size += packetlength + ProtocolParser.GetSize(packetlength) + 1;
			}
		}
		if (instance.Createdbyprivatemessage != 0)
		{
			size += ProtocolParser.GetSize(instance.Createdbyprivatemessage) + 1;
		}
		if (instance.Membership != 0)
		{
			size += ProtocolParser.GetSize(instance.Membership) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_PlayerGroup instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_PlayerGroupSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_PlayerGroup instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_PlayerGroupSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_PlayerGroup instance)
	{
		byte[] data = Packet_PlayerGroupSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
