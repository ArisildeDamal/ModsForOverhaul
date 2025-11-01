using System;

public class Packet_PlayerGroupsSerializer
{
	public static Packet_PlayerGroups DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_PlayerGroups instance = new Packet_PlayerGroups();
		Packet_PlayerGroupsSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_PlayerGroups DeserializeBuffer(byte[] buffer, int length, Packet_PlayerGroups instance)
	{
		Packet_PlayerGroupsSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_PlayerGroups Deserialize(CitoMemoryStream stream, Packet_PlayerGroups instance)
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
				instance.GroupsAdd(Packet_PlayerGroupSerializer.DeserializeLengthDelimitedNew(stream));
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

	public static Packet_PlayerGroups DeserializeLengthDelimited(CitoMemoryStream stream, Packet_PlayerGroups instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_PlayerGroups packet_PlayerGroups = Packet_PlayerGroupsSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_PlayerGroups;
	}

	public static void Serialize(CitoStream stream, Packet_PlayerGroups instance)
	{
		if (instance.Groups != null)
		{
			Packet_PlayerGroup[] elems = instance.Groups;
			int elemCount = instance.GroupsCount;
			int i = 0;
			while (i < elems.Length && i < elemCount)
			{
				stream.WriteByte(10);
				Packet_PlayerGroupSerializer.SerializeWithSize(stream, elems[i]);
				i++;
			}
		}
	}

	public static int GetSize(Packet_PlayerGroups instance)
	{
		int size = 0;
		if (instance.Groups != null)
		{
			for (int i = 0; i < instance.GroupsCount; i++)
			{
				int packetlength = Packet_PlayerGroupSerializer.GetSize(instance.Groups[i]);
				size += packetlength + ProtocolParser.GetSize(packetlength) + 1;
			}
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_PlayerGroups instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_PlayerGroupsSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_PlayerGroups instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_PlayerGroupsSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_PlayerGroups instance)
	{
		byte[] data = Packet_PlayerGroupsSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
