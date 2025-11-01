using System;

public class Packet_InvOpenCloseSerializer
{
	public static Packet_InvOpenClose DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_InvOpenClose instance = new Packet_InvOpenClose();
		Packet_InvOpenCloseSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_InvOpenClose DeserializeBuffer(byte[] buffer, int length, Packet_InvOpenClose instance)
	{
		Packet_InvOpenCloseSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_InvOpenClose Deserialize(CitoMemoryStream stream, Packet_InvOpenClose instance)
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
					instance.Opened = ProtocolParser.ReadUInt32(stream);
				}
			}
			else
			{
				instance.InventoryId = ProtocolParser.ReadString(stream);
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

	public static Packet_InvOpenClose DeserializeLengthDelimited(CitoMemoryStream stream, Packet_InvOpenClose instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_InvOpenClose packet_InvOpenClose = Packet_InvOpenCloseSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_InvOpenClose;
	}

	public static void Serialize(CitoStream stream, Packet_InvOpenClose instance)
	{
		if (instance.InventoryId != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.InventoryId);
		}
		if (instance.Opened != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.Opened);
		}
	}

	public static int GetSize(Packet_InvOpenClose instance)
	{
		int size = 0;
		if (instance.InventoryId != null)
		{
			size += ProtocolParser.GetSize(instance.InventoryId) + 1;
		}
		if (instance.Opened != 0)
		{
			size += ProtocolParser.GetSize(instance.Opened) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_InvOpenClose instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_InvOpenCloseSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_InvOpenClose instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_InvOpenCloseSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_InvOpenClose instance)
	{
		byte[] data = Packet_InvOpenCloseSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
