using System;

public class Packet_ServerQueryAnswerSerializer
{
	public static Packet_ServerQueryAnswer DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ServerQueryAnswer instance = new Packet_ServerQueryAnswer();
		Packet_ServerQueryAnswerSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ServerQueryAnswer DeserializeBuffer(byte[] buffer, int length, Packet_ServerQueryAnswer instance)
	{
		Packet_ServerQueryAnswerSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ServerQueryAnswer Deserialize(CitoMemoryStream stream, Packet_ServerQueryAnswer instance)
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
			if (keyInt <= 24)
			{
				if (keyInt <= 10)
				{
					if (keyInt == 0)
					{
						goto IL_0076;
					}
					if (keyInt == 10)
					{
						instance.Name = ProtocolParser.ReadString(stream);
						continue;
					}
				}
				else
				{
					if (keyInt == 18)
					{
						instance.MOTD = ProtocolParser.ReadString(stream);
						continue;
					}
					if (keyInt == 24)
					{
						instance.PlayerCount = ProtocolParser.ReadUInt32(stream);
						continue;
					}
				}
			}
			else if (keyInt <= 42)
			{
				if (keyInt == 32)
				{
					instance.MaxPlayers = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 42)
				{
					instance.GameMode = ProtocolParser.ReadString(stream);
					continue;
				}
			}
			else
			{
				if (keyInt == 48)
				{
					instance.Password = ProtocolParser.ReadBool(stream);
					continue;
				}
				if (keyInt == 58)
				{
					instance.ServerVersion = ProtocolParser.ReadString(stream);
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
		IL_0076:
		return null;
	}

	public static Packet_ServerQueryAnswer DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ServerQueryAnswer instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ServerQueryAnswer packet_ServerQueryAnswer = Packet_ServerQueryAnswerSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_ServerQueryAnswer;
	}

	public static void Serialize(CitoStream stream, Packet_ServerQueryAnswer instance)
	{
		if (instance.Name != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.Name);
		}
		if (instance.MOTD != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteString(stream, instance.MOTD);
		}
		if (instance.PlayerCount != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.PlayerCount);
		}
		if (instance.MaxPlayers != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.MaxPlayers);
		}
		if (instance.GameMode != null)
		{
			stream.WriteByte(42);
			ProtocolParser.WriteString(stream, instance.GameMode);
		}
		if (instance.Password)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteBool(stream, instance.Password);
		}
		if (instance.ServerVersion != null)
		{
			stream.WriteByte(58);
			ProtocolParser.WriteString(stream, instance.ServerVersion);
		}
	}

	public static int GetSize(Packet_ServerQueryAnswer instance)
	{
		int size = 0;
		if (instance.Name != null)
		{
			size += ProtocolParser.GetSize(instance.Name) + 1;
		}
		if (instance.MOTD != null)
		{
			size += ProtocolParser.GetSize(instance.MOTD) + 1;
		}
		if (instance.PlayerCount != 0)
		{
			size += ProtocolParser.GetSize(instance.PlayerCount) + 1;
		}
		if (instance.MaxPlayers != 0)
		{
			size += ProtocolParser.GetSize(instance.MaxPlayers) + 1;
		}
		if (instance.GameMode != null)
		{
			size += ProtocolParser.GetSize(instance.GameMode) + 1;
		}
		if (instance.Password)
		{
			size += 2;
		}
		if (instance.ServerVersion != null)
		{
			size += ProtocolParser.GetSize(instance.ServerVersion) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ServerQueryAnswer instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_ServerQueryAnswerSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_ServerQueryAnswer instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_ServerQueryAnswerSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ServerQueryAnswer instance)
	{
		byte[] data = Packet_ServerQueryAnswerSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
