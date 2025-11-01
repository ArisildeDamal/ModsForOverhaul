using System;

public class Packet_BlockSoundSetSerializer
{
	public static Packet_BlockSoundSet DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_BlockSoundSet instance = new Packet_BlockSoundSet();
		Packet_BlockSoundSetSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_BlockSoundSet DeserializeBuffer(byte[] buffer, int length, Packet_BlockSoundSet instance)
	{
		Packet_BlockSoundSetSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_BlockSoundSet Deserialize(CitoMemoryStream stream, Packet_BlockSoundSet instance)
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
			if (keyInt <= 42)
			{
				if (keyInt <= 18)
				{
					if (keyInt == 0)
					{
						goto IL_009F;
					}
					if (keyInt == 10)
					{
						instance.Walk = ProtocolParser.ReadString(stream);
						continue;
					}
					if (keyInt == 18)
					{
						instance.Break = ProtocolParser.ReadString(stream);
						continue;
					}
				}
				else
				{
					if (keyInt == 26)
					{
						instance.Place = ProtocolParser.ReadString(stream);
						continue;
					}
					if (keyInt == 34)
					{
						instance.Hit = ProtocolParser.ReadString(stream);
						continue;
					}
					if (keyInt == 42)
					{
						instance.Inside = ProtocolParser.ReadString(stream);
						continue;
					}
				}
			}
			else if (keyInt <= 66)
			{
				if (keyInt == 50)
				{
					instance.Ambient = ProtocolParser.ReadString(stream);
					continue;
				}
				if (keyInt == 56)
				{
					instance.ByToolToolAdd(ProtocolParser.ReadUInt32(stream));
					continue;
				}
				if (keyInt == 66)
				{
					instance.ByToolSoundAdd(Packet_BlockSoundSetSerializer.DeserializeLengthDelimitedNew(stream));
					continue;
				}
			}
			else
			{
				if (keyInt == 72)
				{
					instance.AmbientBlockCount = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 80)
				{
					instance.AmbientSoundType = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 88)
				{
					instance.AmbientMaxDistanceMerge = ProtocolParser.ReadUInt32(stream);
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
		IL_009F:
		return null;
	}

	public static Packet_BlockSoundSet DeserializeLengthDelimited(CitoMemoryStream stream, Packet_BlockSoundSet instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_BlockSoundSet packet_BlockSoundSet = Packet_BlockSoundSetSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_BlockSoundSet;
	}

	public static void Serialize(CitoStream stream, Packet_BlockSoundSet instance)
	{
		if (instance.Walk != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.Walk);
		}
		if (instance.Break != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteString(stream, instance.Break);
		}
		if (instance.Place != null)
		{
			stream.WriteByte(26);
			ProtocolParser.WriteString(stream, instance.Place);
		}
		if (instance.Hit != null)
		{
			stream.WriteByte(34);
			ProtocolParser.WriteString(stream, instance.Hit);
		}
		if (instance.Inside != null)
		{
			stream.WriteByte(42);
			ProtocolParser.WriteString(stream, instance.Inside);
		}
		if (instance.Ambient != null)
		{
			stream.WriteByte(50);
			ProtocolParser.WriteString(stream, instance.Ambient);
		}
		if (instance.AmbientBlockCount != 0)
		{
			stream.WriteByte(72);
			ProtocolParser.WriteUInt32(stream, instance.AmbientBlockCount);
		}
		if (instance.AmbientSoundType != 0)
		{
			stream.WriteByte(80);
			ProtocolParser.WriteUInt32(stream, instance.AmbientSoundType);
		}
		if (instance.AmbientMaxDistanceMerge != 0)
		{
			stream.WriteByte(88);
			ProtocolParser.WriteUInt32(stream, instance.AmbientMaxDistanceMerge);
		}
		if (instance.ByToolTool != null)
		{
			int[] elems = instance.ByToolTool;
			int elemCount = instance.ByToolToolCount;
			int i = 0;
			while (i < elems.Length && i < elemCount)
			{
				stream.WriteByte(56);
				ProtocolParser.WriteUInt32(stream, elems[i]);
				i++;
			}
		}
		if (instance.ByToolSound != null)
		{
			Packet_BlockSoundSet[] elems2 = instance.ByToolSound;
			int elemCount2 = instance.ByToolSoundCount;
			int j = 0;
			while (j < elems2.Length && j < elemCount2)
			{
				stream.WriteByte(66);
				Packet_BlockSoundSetSerializer.SerializeWithSize(stream, elems2[j]);
				j++;
			}
		}
	}

	public static int GetSize(Packet_BlockSoundSet instance)
	{
		int size = 0;
		if (instance.Walk != null)
		{
			size += ProtocolParser.GetSize(instance.Walk) + 1;
		}
		if (instance.Break != null)
		{
			size += ProtocolParser.GetSize(instance.Break) + 1;
		}
		if (instance.Place != null)
		{
			size += ProtocolParser.GetSize(instance.Place) + 1;
		}
		if (instance.Hit != null)
		{
			size += ProtocolParser.GetSize(instance.Hit) + 1;
		}
		if (instance.Inside != null)
		{
			size += ProtocolParser.GetSize(instance.Inside) + 1;
		}
		if (instance.Ambient != null)
		{
			size += ProtocolParser.GetSize(instance.Ambient) + 1;
		}
		if (instance.AmbientBlockCount != 0)
		{
			size += ProtocolParser.GetSize(instance.AmbientBlockCount) + 1;
		}
		if (instance.AmbientSoundType != 0)
		{
			size += ProtocolParser.GetSize(instance.AmbientSoundType) + 1;
		}
		if (instance.AmbientMaxDistanceMerge != 0)
		{
			size += ProtocolParser.GetSize(instance.AmbientMaxDistanceMerge) + 1;
		}
		if (instance.ByToolTool != null)
		{
			for (int i = 0; i < instance.ByToolToolCount; i++)
			{
				int i2 = instance.ByToolTool[i];
				size += ProtocolParser.GetSize(i2) + 1;
			}
		}
		if (instance.ByToolSound != null)
		{
			for (int j = 0; j < instance.ByToolSoundCount; j++)
			{
				int packetlength = Packet_BlockSoundSetSerializer.GetSize(instance.ByToolSound[j]);
				size += packetlength + ProtocolParser.GetSize(packetlength) + 1;
			}
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_BlockSoundSet instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_BlockSoundSetSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_BlockSoundSet instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_BlockSoundSetSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_BlockSoundSet instance)
	{
		byte[] data = Packet_BlockSoundSetSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
