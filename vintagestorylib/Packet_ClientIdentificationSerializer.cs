using System;

public class Packet_ClientIdentificationSerializer
{
	public static Packet_ClientIdentification DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ClientIdentification instance = new Packet_ClientIdentification();
		Packet_ClientIdentificationSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ClientIdentification DeserializeBuffer(byte[] buffer, int length, Packet_ClientIdentification instance)
	{
		Packet_ClientIdentificationSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ClientIdentification Deserialize(CitoMemoryStream stream, Packet_ClientIdentification instance)
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
			if (keyInt <= 34)
			{
				if (keyInt <= 10)
				{
					if (keyInt == 0)
					{
						goto IL_0089;
					}
					if (keyInt == 10)
					{
						instance.MdProtocolVersion = ProtocolParser.ReadString(stream);
						continue;
					}
				}
				else
				{
					if (keyInt == 18)
					{
						instance.Playername = ProtocolParser.ReadString(stream);
						continue;
					}
					if (keyInt == 26)
					{
						instance.MpToken = ProtocolParser.ReadString(stream);
						continue;
					}
					if (keyInt == 34)
					{
						instance.ServerPassword = ProtocolParser.ReadString(stream);
						continue;
					}
				}
			}
			else if (keyInt <= 56)
			{
				if (keyInt == 50)
				{
					instance.PlayerUID = ProtocolParser.ReadString(stream);
					continue;
				}
				if (keyInt == 56)
				{
					instance.ViewDistance = ProtocolParser.ReadUInt32(stream);
					continue;
				}
			}
			else
			{
				if (keyInt == 64)
				{
					instance.RenderMetaBlocks = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 74)
				{
					instance.NetworkVersion = ProtocolParser.ReadString(stream);
					continue;
				}
				if (keyInt == 82)
				{
					instance.ShortGameVersion = ProtocolParser.ReadString(stream);
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
		IL_0089:
		return null;
	}

	public static Packet_ClientIdentification DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ClientIdentification instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ClientIdentification packet_ClientIdentification = Packet_ClientIdentificationSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_ClientIdentification;
	}

	public static void Serialize(CitoStream stream, Packet_ClientIdentification instance)
	{
		if (instance.MdProtocolVersion != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.MdProtocolVersion);
		}
		if (instance.Playername != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteString(stream, instance.Playername);
		}
		if (instance.MpToken != null)
		{
			stream.WriteByte(26);
			ProtocolParser.WriteString(stream, instance.MpToken);
		}
		if (instance.ServerPassword != null)
		{
			stream.WriteByte(34);
			ProtocolParser.WriteString(stream, instance.ServerPassword);
		}
		if (instance.PlayerUID != null)
		{
			stream.WriteByte(50);
			ProtocolParser.WriteString(stream, instance.PlayerUID);
		}
		if (instance.ViewDistance != 0)
		{
			stream.WriteByte(56);
			ProtocolParser.WriteUInt32(stream, instance.ViewDistance);
		}
		if (instance.RenderMetaBlocks != 0)
		{
			stream.WriteByte(64);
			ProtocolParser.WriteUInt32(stream, instance.RenderMetaBlocks);
		}
		if (instance.NetworkVersion != null)
		{
			stream.WriteByte(74);
			ProtocolParser.WriteString(stream, instance.NetworkVersion);
		}
		if (instance.ShortGameVersion != null)
		{
			stream.WriteByte(82);
			ProtocolParser.WriteString(stream, instance.ShortGameVersion);
		}
	}

	public static int GetSize(Packet_ClientIdentification instance)
	{
		int size = 0;
		if (instance.MdProtocolVersion != null)
		{
			size += ProtocolParser.GetSize(instance.MdProtocolVersion) + 1;
		}
		if (instance.Playername != null)
		{
			size += ProtocolParser.GetSize(instance.Playername) + 1;
		}
		if (instance.MpToken != null)
		{
			size += ProtocolParser.GetSize(instance.MpToken) + 1;
		}
		if (instance.ServerPassword != null)
		{
			size += ProtocolParser.GetSize(instance.ServerPassword) + 1;
		}
		if (instance.PlayerUID != null)
		{
			size += ProtocolParser.GetSize(instance.PlayerUID) + 1;
		}
		if (instance.ViewDistance != 0)
		{
			size += ProtocolParser.GetSize(instance.ViewDistance) + 1;
		}
		if (instance.RenderMetaBlocks != 0)
		{
			size += ProtocolParser.GetSize(instance.RenderMetaBlocks) + 1;
		}
		if (instance.NetworkVersion != null)
		{
			size += ProtocolParser.GetSize(instance.NetworkVersion) + 1;
		}
		if (instance.ShortGameVersion != null)
		{
			size += ProtocolParser.GetSize(instance.ShortGameVersion) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ClientIdentification instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_ClientIdentificationSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_ClientIdentification instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_ClientIdentificationSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ClientIdentification instance)
	{
		byte[] data = Packet_ClientIdentificationSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
