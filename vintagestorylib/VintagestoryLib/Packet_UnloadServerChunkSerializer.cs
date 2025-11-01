using System;

public class Packet_UnloadServerChunkSerializer
{
	public static Packet_UnloadServerChunk DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_UnloadServerChunk instance = new Packet_UnloadServerChunk();
		Packet_UnloadServerChunkSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_UnloadServerChunk DeserializeBuffer(byte[] buffer, int length, Packet_UnloadServerChunk instance)
	{
		Packet_UnloadServerChunkSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_UnloadServerChunk Deserialize(CitoMemoryStream stream, Packet_UnloadServerChunk instance)
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
					goto IL_0046;
				}
				if (keyInt == 8)
				{
					instance.XAdd(ProtocolParser.ReadUInt32(stream));
					continue;
				}
			}
			else
			{
				if (keyInt == 16)
				{
					instance.YAdd(ProtocolParser.ReadUInt32(stream));
					continue;
				}
				if (keyInt == 24)
				{
					instance.ZAdd(ProtocolParser.ReadUInt32(stream));
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
		IL_0046:
		return null;
	}

	public static Packet_UnloadServerChunk DeserializeLengthDelimited(CitoMemoryStream stream, Packet_UnloadServerChunk instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_UnloadServerChunk packet_UnloadServerChunk = Packet_UnloadServerChunkSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_UnloadServerChunk;
	}

	public static void Serialize(CitoStream stream, Packet_UnloadServerChunk instance)
	{
		if (instance.X != null)
		{
			int[] elems = instance.X;
			int elemCount = instance.XCount;
			int i = 0;
			while (i < elems.Length && i < elemCount)
			{
				stream.WriteByte(8);
				ProtocolParser.WriteUInt32(stream, elems[i]);
				i++;
			}
		}
		if (instance.Y != null)
		{
			int[] elems2 = instance.Y;
			int elemCount2 = instance.YCount;
			int j = 0;
			while (j < elems2.Length && j < elemCount2)
			{
				stream.WriteByte(16);
				ProtocolParser.WriteUInt32(stream, elems2[j]);
				j++;
			}
		}
		if (instance.Z != null)
		{
			int[] elems3 = instance.Z;
			int elemCount3 = instance.ZCount;
			int k = 0;
			while (k < elems3.Length && k < elemCount3)
			{
				stream.WriteByte(24);
				ProtocolParser.WriteUInt32(stream, elems3[k]);
				k++;
			}
		}
	}

	public static int GetSize(Packet_UnloadServerChunk instance)
	{
		int size = 0;
		if (instance.X != null)
		{
			for (int i = 0; i < instance.XCount; i++)
			{
				int i2 = instance.X[i];
				size += ProtocolParser.GetSize(i2) + 1;
			}
		}
		if (instance.Y != null)
		{
			for (int j = 0; j < instance.YCount; j++)
			{
				int i3 = instance.Y[j];
				size += ProtocolParser.GetSize(i3) + 1;
			}
		}
		if (instance.Z != null)
		{
			for (int k = 0; k < instance.ZCount; k++)
			{
				int i4 = instance.Z[k];
				size += ProtocolParser.GetSize(i4) + 1;
			}
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_UnloadServerChunk instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_UnloadServerChunkSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_UnloadServerChunk instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_UnloadServerChunkSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_UnloadServerChunk instance)
	{
		byte[] data = Packet_UnloadServerChunkSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
