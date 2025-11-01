using System;

public class Packet_RolesSerializer
{
	public static Packet_Roles DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_Roles instance = new Packet_Roles();
		Packet_RolesSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_Roles DeserializeBuffer(byte[] buffer, int length, Packet_Roles instance)
	{
		Packet_RolesSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_Roles Deserialize(CitoMemoryStream stream, Packet_Roles instance)
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
				instance.RolesAdd(Packet_RoleSerializer.DeserializeLengthDelimitedNew(stream));
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

	public static Packet_Roles DeserializeLengthDelimited(CitoMemoryStream stream, Packet_Roles instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_Roles packet_Roles = Packet_RolesSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_Roles;
	}

	public static void Serialize(CitoStream stream, Packet_Roles instance)
	{
		if (instance.Roles != null)
		{
			Packet_Role[] elems = instance.Roles;
			int elemCount = instance.RolesCount;
			int i = 0;
			while (i < elems.Length && i < elemCount)
			{
				stream.WriteByte(10);
				Packet_RoleSerializer.SerializeWithSize(stream, elems[i]);
				i++;
			}
		}
	}

	public static int GetSize(Packet_Roles instance)
	{
		int size = 0;
		if (instance.Roles != null)
		{
			for (int i = 0; i < instance.RolesCount; i++)
			{
				int packetlength = Packet_RoleSerializer.GetSize(instance.Roles[i]);
				size += packetlength + ProtocolParser.GetSize(packetlength) + 1;
			}
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_Roles instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_RolesSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_Roles instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_RolesSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_Roles instance)
	{
		byte[] data = Packet_RolesSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
