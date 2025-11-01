using System;

public class Packet_ServerChunksSerializer
{
	public static Packet_ServerChunks DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ServerChunks instance = new Packet_ServerChunks();
		Packet_ServerChunksSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ServerChunks DeserializeBuffer(byte[] buffer, int length, Packet_ServerChunks instance)
	{
		Packet_ServerChunksSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ServerChunks Deserialize(CitoMemoryStream stream, Packet_ServerChunks instance)
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
				instance.ChunksAdd(Packet_ServerChunkSerializer.DeserializeLengthDelimitedNew(stream));
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

	public static Packet_ServerChunks DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ServerChunks instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ServerChunks packet_ServerChunks = Packet_ServerChunksSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_ServerChunks;
	}

	public static void Serialize(CitoStream stream, Packet_ServerChunks instance)
	{
		if (instance.Chunks != null)
		{
			Packet_ServerChunk[] elems = instance.Chunks;
			int elemCount = instance.ChunksCount;
			int i = 0;
			while (i < elems.Length && i < elemCount)
			{
				stream.WriteByte(10);
				Packet_ServerChunkSerializer.SerializeWithSize(stream, elems[i]);
				i++;
			}
		}
	}

	public static int GetSize(Packet_ServerChunks instance)
	{
		int size = 0;
		if (instance.Chunks != null)
		{
			for (int i = 0; i < instance.ChunksCount; i++)
			{
				int packetlength = Packet_ServerChunkSerializer.GetSize(instance.Chunks[i]);
				size += packetlength + ProtocolParser.GetSize(packetlength) + 1;
			}
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ServerChunks instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_ServerChunksSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_ServerChunks instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_ServerChunksSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ServerChunks instance)
	{
		byte[] data = Packet_ServerChunksSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
