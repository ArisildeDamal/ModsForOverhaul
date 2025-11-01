using System;

public class Packet_CompositeShapeSerializer
{
	public static Packet_CompositeShape DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_CompositeShape instance = new Packet_CompositeShape();
		Packet_CompositeShapeSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_CompositeShape DeserializeBuffer(byte[] buffer, int length, Packet_CompositeShape instance)
	{
		Packet_CompositeShapeSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_CompositeShape Deserialize(CitoMemoryStream stream, Packet_CompositeShape instance)
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
			if (keyInt <= 64)
			{
				if (keyInt <= 24)
				{
					if (keyInt <= 10)
					{
						if (keyInt == 0)
						{
							goto IL_010F;
						}
						if (keyInt == 10)
						{
							instance.Base = ProtocolParser.ReadString(stream);
							continue;
						}
					}
					else
					{
						if (keyInt == 16)
						{
							instance.Rotatex = ProtocolParser.ReadUInt32(stream);
							continue;
						}
						if (keyInt == 24)
						{
							instance.Rotatey = ProtocolParser.ReadUInt32(stream);
							continue;
						}
					}
				}
				else if (keyInt <= 42)
				{
					if (keyInt == 32)
					{
						instance.Rotatez = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 42)
					{
						instance.AlternatesAdd(Packet_CompositeShapeSerializer.DeserializeLengthDelimitedNew(stream));
						continue;
					}
				}
				else
				{
					if (keyInt == 48)
					{
						instance.VoxelizeShape = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 58)
					{
						instance.SelectiveElementsAdd(ProtocolParser.ReadString(stream));
						continue;
					}
					if (keyInt == 64)
					{
						instance.QuantityElements = ProtocolParser.ReadUInt32(stream);
						continue;
					}
				}
			}
			else if (keyInt <= 96)
			{
				if (keyInt <= 80)
				{
					if (keyInt == 72)
					{
						instance.QuantityElementsSet = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 80)
					{
						instance.Format = ProtocolParser.ReadUInt32(stream);
						continue;
					}
				}
				else
				{
					if (keyInt == 90)
					{
						instance.OverlaysAdd(Packet_CompositeShapeSerializer.DeserializeLengthDelimitedNew(stream));
						continue;
					}
					if (keyInt == 96)
					{
						instance.Offsetx = ProtocolParser.ReadUInt32(stream);
						continue;
					}
				}
			}
			else if (keyInt <= 112)
			{
				if (keyInt == 104)
				{
					instance.Offsety = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 112)
				{
					instance.Offsetz = ProtocolParser.ReadUInt32(stream);
					continue;
				}
			}
			else
			{
				if (keyInt == 120)
				{
					instance.InsertBakedTextures = ProtocolParser.ReadBool(stream);
					continue;
				}
				if (keyInt == 128)
				{
					instance.ScaleAdjust = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 138)
				{
					instance.IgnoreElementsAdd(ProtocolParser.ReadString(stream));
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
		IL_010F:
		return null;
	}

	public static Packet_CompositeShape DeserializeLengthDelimited(CitoMemoryStream stream, Packet_CompositeShape instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_CompositeShape packet_CompositeShape = Packet_CompositeShapeSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_CompositeShape;
	}

	public static void Serialize(CitoStream stream, Packet_CompositeShape instance)
	{
		if (instance.Base != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.Base);
		}
		if (instance.Rotatex != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.Rotatex);
		}
		if (instance.Rotatey != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.Rotatey);
		}
		if (instance.Rotatez != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.Rotatez);
		}
		if (instance.Alternates != null)
		{
			Packet_CompositeShape[] elems = instance.Alternates;
			int elemCount = instance.AlternatesCount;
			int i = 0;
			while (i < elems.Length && i < elemCount)
			{
				stream.WriteByte(42);
				Packet_CompositeShapeSerializer.SerializeWithSize(stream, elems[i]);
				i++;
			}
		}
		if (instance.Overlays != null)
		{
			Packet_CompositeShape[] elems2 = instance.Overlays;
			int elemCount2 = instance.OverlaysCount;
			int j = 0;
			while (j < elems2.Length && j < elemCount2)
			{
				stream.WriteByte(90);
				Packet_CompositeShapeSerializer.SerializeWithSize(stream, elems2[j]);
				j++;
			}
		}
		if (instance.VoxelizeShape != 0)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt32(stream, instance.VoxelizeShape);
		}
		if (instance.SelectiveElements != null)
		{
			string[] elems3 = instance.SelectiveElements;
			int elemCount3 = instance.SelectiveElementsCount;
			int k = 0;
			while (k < elems3.Length && k < elemCount3)
			{
				stream.WriteByte(58);
				ProtocolParser.WriteString(stream, elems3[k]);
				k++;
			}
		}
		if (instance.IgnoreElements != null)
		{
			string[] elems4 = instance.IgnoreElements;
			int elemCount4 = instance.IgnoreElementsCount;
			int l = 0;
			while (l < elems4.Length && l < elemCount4)
			{
				stream.WriteKey(17, 2);
				ProtocolParser.WriteString(stream, elems4[l]);
				l++;
			}
		}
		if (instance.QuantityElements != 0)
		{
			stream.WriteByte(64);
			ProtocolParser.WriteUInt32(stream, instance.QuantityElements);
		}
		if (instance.QuantityElementsSet != 0)
		{
			stream.WriteByte(72);
			ProtocolParser.WriteUInt32(stream, instance.QuantityElementsSet);
		}
		if (instance.Format != 0)
		{
			stream.WriteByte(80);
			ProtocolParser.WriteUInt32(stream, instance.Format);
		}
		if (instance.Offsetx != 0)
		{
			stream.WriteByte(96);
			ProtocolParser.WriteUInt32(stream, instance.Offsetx);
		}
		if (instance.Offsety != 0)
		{
			stream.WriteByte(104);
			ProtocolParser.WriteUInt32(stream, instance.Offsety);
		}
		if (instance.Offsetz != 0)
		{
			stream.WriteByte(112);
			ProtocolParser.WriteUInt32(stream, instance.Offsetz);
		}
		if (instance.InsertBakedTextures)
		{
			stream.WriteByte(120);
			ProtocolParser.WriteBool(stream, instance.InsertBakedTextures);
		}
		if (instance.ScaleAdjust != 0)
		{
			stream.WriteKey(16, 0);
			ProtocolParser.WriteUInt32(stream, instance.ScaleAdjust);
		}
	}

	public static int GetSize(Packet_CompositeShape instance)
	{
		int size = 0;
		if (instance.Base != null)
		{
			size += ProtocolParser.GetSize(instance.Base) + 1;
		}
		if (instance.Rotatex != 0)
		{
			size += ProtocolParser.GetSize(instance.Rotatex) + 1;
		}
		if (instance.Rotatey != 0)
		{
			size += ProtocolParser.GetSize(instance.Rotatey) + 1;
		}
		if (instance.Rotatez != 0)
		{
			size += ProtocolParser.GetSize(instance.Rotatez) + 1;
		}
		if (instance.Alternates != null)
		{
			for (int i = 0; i < instance.AlternatesCount; i++)
			{
				int packetlength = Packet_CompositeShapeSerializer.GetSize(instance.Alternates[i]);
				size += packetlength + ProtocolParser.GetSize(packetlength) + 1;
			}
		}
		if (instance.Overlays != null)
		{
			for (int j = 0; j < instance.OverlaysCount; j++)
			{
				int packetlength2 = Packet_CompositeShapeSerializer.GetSize(instance.Overlays[j]);
				size += packetlength2 + ProtocolParser.GetSize(packetlength2) + 1;
			}
		}
		if (instance.VoxelizeShape != 0)
		{
			size += ProtocolParser.GetSize(instance.VoxelizeShape) + 1;
		}
		if (instance.SelectiveElements != null)
		{
			for (int k = 0; k < instance.SelectiveElementsCount; k++)
			{
				string i2 = instance.SelectiveElements[k];
				size += ProtocolParser.GetSize(i2) + 1;
			}
		}
		if (instance.IgnoreElements != null)
		{
			for (int l = 0; l < instance.IgnoreElementsCount; l++)
			{
				string i3 = instance.IgnoreElements[l];
				size += ProtocolParser.GetSize(i3) + 2;
			}
		}
		if (instance.QuantityElements != 0)
		{
			size += ProtocolParser.GetSize(instance.QuantityElements) + 1;
		}
		if (instance.QuantityElementsSet != 0)
		{
			size += ProtocolParser.GetSize(instance.QuantityElementsSet) + 1;
		}
		if (instance.Format != 0)
		{
			size += ProtocolParser.GetSize(instance.Format) + 1;
		}
		if (instance.Offsetx != 0)
		{
			size += ProtocolParser.GetSize(instance.Offsetx) + 1;
		}
		if (instance.Offsety != 0)
		{
			size += ProtocolParser.GetSize(instance.Offsety) + 1;
		}
		if (instance.Offsetz != 0)
		{
			size += ProtocolParser.GetSize(instance.Offsetz) + 1;
		}
		if (instance.InsertBakedTextures)
		{
			size += 2;
		}
		if (instance.ScaleAdjust != 0)
		{
			size += ProtocolParser.GetSize(instance.ScaleAdjust) + 2;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_CompositeShape instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_CompositeShapeSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_CompositeShape instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_CompositeShapeSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_CompositeShape instance)
	{
		byte[] data = Packet_CompositeShapeSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
