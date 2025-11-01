using System;

public class Packet_ModelTransformSerializer
{
	public static Packet_ModelTransform DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ModelTransform instance = new Packet_ModelTransform();
		Packet_ModelTransformSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ModelTransform DeserializeBuffer(byte[] buffer, int length, Packet_ModelTransform instance)
	{
		Packet_ModelTransformSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ModelTransform Deserialize(CitoMemoryStream stream, Packet_ModelTransform instance)
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
			if (keyInt <= 48)
			{
				if (keyInt <= 16)
				{
					if (keyInt == 0)
					{
						goto IL_00D4;
					}
					if (keyInt == 8)
					{
						instance.TranslateX = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 16)
					{
						instance.TranslateY = ProtocolParser.ReadUInt32(stream);
						continue;
					}
				}
				else if (keyInt <= 32)
				{
					if (keyInt == 24)
					{
						instance.TranslateZ = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 32)
					{
						instance.RotateX = ProtocolParser.ReadUInt32(stream);
						continue;
					}
				}
				else
				{
					if (keyInt == 40)
					{
						instance.RotateY = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 48)
					{
						instance.RotateZ = ProtocolParser.ReadUInt32(stream);
						continue;
					}
				}
			}
			else if (keyInt <= 80)
			{
				if (keyInt == 64)
				{
					instance.Rotate = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 72)
				{
					instance.OriginX = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 80)
				{
					instance.OriginY = ProtocolParser.ReadUInt32(stream);
					continue;
				}
			}
			else if (keyInt <= 96)
			{
				if (keyInt == 88)
				{
					instance.OriginZ = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 96)
				{
					instance.ScaleX = ProtocolParser.ReadUInt32(stream);
					continue;
				}
			}
			else
			{
				if (keyInt == 104)
				{
					instance.ScaleY = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 112)
				{
					instance.ScaleZ = ProtocolParser.ReadUInt32(stream);
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
		IL_00D4:
		return null;
	}

	public static Packet_ModelTransform DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ModelTransform instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ModelTransform packet_ModelTransform = Packet_ModelTransformSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_ModelTransform;
	}

	public static void Serialize(CitoStream stream, Packet_ModelTransform instance)
	{
		if (instance.TranslateX != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.TranslateX);
		}
		if (instance.TranslateY != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.TranslateY);
		}
		if (instance.TranslateZ != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.TranslateZ);
		}
		if (instance.RotateX != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.RotateX);
		}
		if (instance.RotateY != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.RotateY);
		}
		if (instance.RotateZ != 0)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt32(stream, instance.RotateZ);
		}
		if (instance.Rotate != 0)
		{
			stream.WriteByte(64);
			ProtocolParser.WriteUInt32(stream, instance.Rotate);
		}
		if (instance.OriginX != 0)
		{
			stream.WriteByte(72);
			ProtocolParser.WriteUInt32(stream, instance.OriginX);
		}
		if (instance.OriginY != 0)
		{
			stream.WriteByte(80);
			ProtocolParser.WriteUInt32(stream, instance.OriginY);
		}
		if (instance.OriginZ != 0)
		{
			stream.WriteByte(88);
			ProtocolParser.WriteUInt32(stream, instance.OriginZ);
		}
		if (instance.ScaleX != 0)
		{
			stream.WriteByte(96);
			ProtocolParser.WriteUInt32(stream, instance.ScaleX);
		}
		if (instance.ScaleY != 0)
		{
			stream.WriteByte(104);
			ProtocolParser.WriteUInt32(stream, instance.ScaleY);
		}
		if (instance.ScaleZ != 0)
		{
			stream.WriteByte(112);
			ProtocolParser.WriteUInt32(stream, instance.ScaleZ);
		}
	}

	public static int GetSize(Packet_ModelTransform instance)
	{
		int size = 0;
		if (instance.TranslateX != 0)
		{
			size += ProtocolParser.GetSize(instance.TranslateX) + 1;
		}
		if (instance.TranslateY != 0)
		{
			size += ProtocolParser.GetSize(instance.TranslateY) + 1;
		}
		if (instance.TranslateZ != 0)
		{
			size += ProtocolParser.GetSize(instance.TranslateZ) + 1;
		}
		if (instance.RotateX != 0)
		{
			size += ProtocolParser.GetSize(instance.RotateX) + 1;
		}
		if (instance.RotateY != 0)
		{
			size += ProtocolParser.GetSize(instance.RotateY) + 1;
		}
		if (instance.RotateZ != 0)
		{
			size += ProtocolParser.GetSize(instance.RotateZ) + 1;
		}
		if (instance.Rotate != 0)
		{
			size += ProtocolParser.GetSize(instance.Rotate) + 1;
		}
		if (instance.OriginX != 0)
		{
			size += ProtocolParser.GetSize(instance.OriginX) + 1;
		}
		if (instance.OriginY != 0)
		{
			size += ProtocolParser.GetSize(instance.OriginY) + 1;
		}
		if (instance.OriginZ != 0)
		{
			size += ProtocolParser.GetSize(instance.OriginZ) + 1;
		}
		if (instance.ScaleX != 0)
		{
			size += ProtocolParser.GetSize(instance.ScaleX) + 1;
		}
		if (instance.ScaleY != 0)
		{
			size += ProtocolParser.GetSize(instance.ScaleY) + 1;
		}
		if (instance.ScaleZ != 0)
		{
			size += ProtocolParser.GetSize(instance.ScaleZ) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ModelTransform instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_ModelTransformSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_ModelTransform instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_ModelTransformSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ModelTransform instance)
	{
		byte[] data = Packet_ModelTransformSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
