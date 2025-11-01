using System;

public class Packet_BlockEntitiesSerializer
{
	public static Packet_BlockEntities DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_BlockEntities instance = new Packet_BlockEntities();
		Packet_BlockEntitiesSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_BlockEntities DeserializeBuffer(byte[] buffer, int length, Packet_BlockEntities instance)
	{
		Packet_BlockEntitiesSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_BlockEntities Deserialize(CitoMemoryStream stream, Packet_BlockEntities instance)
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
				instance.BlockEntititesAdd(Packet_BlockEntitySerializer.DeserializeLengthDelimitedNew(stream));
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

	public static Packet_BlockEntities DeserializeLengthDelimited(CitoMemoryStream stream, Packet_BlockEntities instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_BlockEntities packet_BlockEntities = Packet_BlockEntitiesSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_BlockEntities;
	}

	public static void Serialize(CitoStream stream, Packet_BlockEntities instance)
	{
		if (instance.BlockEntitites != null)
		{
			Packet_BlockEntity[] elems = instance.BlockEntitites;
			int elemCount = instance.BlockEntititesCount;
			int i = 0;
			while (i < elems.Length && i < elemCount)
			{
				stream.WriteByte(10);
				Packet_BlockEntitySerializer.SerializeWithSize(stream, elems[i]);
				i++;
			}
		}
	}

	public static int GetSize(Packet_BlockEntities instance)
	{
		int size = 0;
		if (instance.BlockEntitites != null)
		{
			for (int i = 0; i < instance.BlockEntititesCount; i++)
			{
				int packetlength = Packet_BlockEntitySerializer.GetSize(instance.BlockEntitites[i]);
				size += packetlength + ProtocolParser.GetSize(packetlength) + 1;
			}
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_BlockEntities instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_BlockEntitiesSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_BlockEntities instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_BlockEntitiesSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_BlockEntities instance)
	{
		byte[] data = Packet_BlockEntitiesSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
