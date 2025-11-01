using System;

public class Packet_RoleSerializer
{
	public static Packet_Role DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_Role instance = new Packet_Role();
		Packet_RoleSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_Role DeserializeBuffer(byte[] buffer, int length, Packet_Role instance)
	{
		Packet_RoleSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_Role Deserialize(CitoMemoryStream stream, Packet_Role instance)
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
				if (keyInt != 16)
				{
					ProtocolParser.SkipKey(stream, Key.Create(keyInt));
				}
				else
				{
					instance.PrivilegeLevel = ProtocolParser.ReadUInt32(stream);
				}
			}
			else
			{
				instance.Code = ProtocolParser.ReadString(stream);
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

	public static Packet_Role DeserializeLengthDelimited(CitoMemoryStream stream, Packet_Role instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_Role packet_Role = Packet_RoleSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_Role;
	}

	public static void Serialize(CitoStream stream, Packet_Role instance)
	{
		if (instance.Code != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.Code);
		}
		if (instance.PrivilegeLevel != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.PrivilegeLevel);
		}
	}

	public static int GetSize(Packet_Role instance)
	{
		int size = 0;
		if (instance.Code != null)
		{
			size += ProtocolParser.GetSize(instance.Code) + 1;
		}
		if (instance.PrivilegeLevel != 0)
		{
			size += ProtocolParser.GetSize(instance.PrivilegeLevel) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_Role instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_RoleSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_Role instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_RoleSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_Role instance)
	{
		byte[] data = Packet_RoleSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
