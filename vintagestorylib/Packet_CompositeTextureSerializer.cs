using System;

public class Packet_CompositeTextureSerializer
{
	public static Packet_CompositeTexture DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_CompositeTexture instance = new Packet_CompositeTexture();
		Packet_CompositeTextureSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_CompositeTexture DeserializeBuffer(byte[] buffer, int length, Packet_CompositeTexture instance)
	{
		Packet_CompositeTextureSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_CompositeTexture Deserialize(CitoMemoryStream stream, Packet_CompositeTexture instance)
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
			if (keyInt <= 26)
			{
				if (keyInt <= 10)
				{
					if (keyInt == 0)
					{
						goto IL_0076;
					}
					if (keyInt == 10)
					{
						instance.Base = ProtocolParser.ReadString(stream);
						continue;
					}
				}
				else
				{
					if (keyInt == 18)
					{
						instance.OverlaysAdd(Packet_BlendedOverlayTextureSerializer.DeserializeLengthDelimitedNew(stream));
						continue;
					}
					if (keyInt == 26)
					{
						instance.AlternatesAdd(Packet_CompositeTextureSerializer.DeserializeLengthDelimitedNew(stream));
						continue;
					}
				}
			}
			else if (keyInt <= 40)
			{
				if (keyInt == 32)
				{
					instance.Rotation = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 40)
				{
					instance.Alpha = ProtocolParser.ReadUInt32(stream);
					continue;
				}
			}
			else
			{
				if (keyInt == 50)
				{
					instance.TilesAdd(Packet_CompositeTextureSerializer.DeserializeLengthDelimitedNew(stream));
					continue;
				}
				if (keyInt == 56)
				{
					instance.TilesWidth = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_CompositeTexture DeserializeLengthDelimited(CitoMemoryStream stream, Packet_CompositeTexture instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_CompositeTexture packet_CompositeTexture = Packet_CompositeTextureSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_CompositeTexture;
	}

	public static void Serialize(CitoStream stream, Packet_CompositeTexture instance)
	{
		if (instance.Base != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.Base);
		}
		if (instance.Overlays != null)
		{
			Packet_BlendedOverlayTexture[] elems = instance.Overlays;
			int elemCount = instance.OverlaysCount;
			int i = 0;
			while (i < elems.Length && i < elemCount)
			{
				stream.WriteByte(18);
				Packet_BlendedOverlayTextureSerializer.SerializeWithSize(stream, elems[i]);
				i++;
			}
		}
		if (instance.Alternates != null)
		{
			Packet_CompositeTexture[] elems2 = instance.Alternates;
			int elemCount2 = instance.AlternatesCount;
			int j = 0;
			while (j < elems2.Length && j < elemCount2)
			{
				stream.WriteByte(26);
				Packet_CompositeTextureSerializer.SerializeWithSize(stream, elems2[j]);
				j++;
			}
		}
		if (instance.Rotation != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.Rotation);
		}
		if (instance.Alpha != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.Alpha);
		}
		if (instance.Tiles != null)
		{
			Packet_CompositeTexture[] elems3 = instance.Tiles;
			int elemCount3 = instance.TilesCount;
			int k = 0;
			while (k < elems3.Length && k < elemCount3)
			{
				stream.WriteByte(50);
				Packet_CompositeTextureSerializer.SerializeWithSize(stream, elems3[k]);
				k++;
			}
		}
		if (instance.TilesWidth != 0)
		{
			stream.WriteByte(56);
			ProtocolParser.WriteUInt32(stream, instance.TilesWidth);
		}
	}

	public static int GetSize(Packet_CompositeTexture instance)
	{
		int size = 0;
		if (instance.Base != null)
		{
			size += ProtocolParser.GetSize(instance.Base) + 1;
		}
		if (instance.Overlays != null)
		{
			for (int i = 0; i < instance.OverlaysCount; i++)
			{
				int packetlength = Packet_BlendedOverlayTextureSerializer.GetSize(instance.Overlays[i]);
				size += packetlength + ProtocolParser.GetSize(packetlength) + 1;
			}
		}
		if (instance.Alternates != null)
		{
			for (int j = 0; j < instance.AlternatesCount; j++)
			{
				int packetlength2 = Packet_CompositeTextureSerializer.GetSize(instance.Alternates[j]);
				size += packetlength2 + ProtocolParser.GetSize(packetlength2) + 1;
			}
		}
		if (instance.Rotation != 0)
		{
			size += ProtocolParser.GetSize(instance.Rotation) + 1;
		}
		if (instance.Alpha != 0)
		{
			size += ProtocolParser.GetSize(instance.Alpha) + 1;
		}
		if (instance.Tiles != null)
		{
			for (int k = 0; k < instance.TilesCount; k++)
			{
				int packetlength3 = Packet_CompositeTextureSerializer.GetSize(instance.Tiles[k]);
				size += packetlength3 + ProtocolParser.GetSize(packetlength3) + 1;
			}
		}
		if (instance.TilesWidth != 0)
		{
			size += ProtocolParser.GetSize(instance.TilesWidth) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_CompositeTexture instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_CompositeTextureSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_CompositeTexture instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_CompositeTextureSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_CompositeTexture instance)
	{
		byte[] data = Packet_CompositeTextureSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
