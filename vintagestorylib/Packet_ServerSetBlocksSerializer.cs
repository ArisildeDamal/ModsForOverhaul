using System;

public class Packet_ServerSetBlocksSerializer
{
	public static Packet_ServerSetBlocks DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ServerSetBlocks instance = new Packet_ServerSetBlocks();
		Packet_ServerSetBlocksSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ServerSetBlocks DeserializeBuffer(byte[] buffer, int length, Packet_ServerSetBlocks instance)
	{
		Packet_ServerSetBlocksSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ServerSetBlocks Deserialize(CitoMemoryStream stream, Packet_ServerSetBlocks instance)
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
				instance.SetBlocks = ProtocolParser.ReadBytes(stream);
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

	public static Packet_ServerSetBlocks DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ServerSetBlocks instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ServerSetBlocks packet_ServerSetBlocks = Packet_ServerSetBlocksSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_ServerSetBlocks;
	}

	public static void Serialize(CitoStream stream, Packet_ServerSetBlocks instance)
	{
		if (instance.SetBlocks != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteBytes(stream, instance.SetBlocks);
		}
	}

	public static int GetSize(Packet_ServerSetBlocks instance)
	{
		int size = 0;
		if (instance.SetBlocks != null)
		{
			size += ProtocolParser.GetSize(instance.SetBlocks) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ServerSetBlocks instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_ServerSetBlocksSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_ServerSetBlocks instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_ServerSetBlocksSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ServerSetBlocks instance)
	{
		byte[] data = Packet_ServerSetBlocksSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
