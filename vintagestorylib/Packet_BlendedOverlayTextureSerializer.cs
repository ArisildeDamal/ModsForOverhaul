using System;

public class Packet_BlendedOverlayTextureSerializer
{
	public static Packet_BlendedOverlayTexture DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_BlendedOverlayTexture instance = new Packet_BlendedOverlayTexture();
		Packet_BlendedOverlayTextureSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_BlendedOverlayTexture DeserializeBuffer(byte[] buffer, int length, Packet_BlendedOverlayTexture instance)
	{
		Packet_BlendedOverlayTextureSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_BlendedOverlayTexture Deserialize(CitoMemoryStream stream, Packet_BlendedOverlayTexture instance)
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
					instance.Mode = ProtocolParser.ReadUInt32(stream);
				}
			}
			else
			{
				instance.Base = ProtocolParser.ReadString(stream);
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

	public static Packet_BlendedOverlayTexture DeserializeLengthDelimited(CitoMemoryStream stream, Packet_BlendedOverlayTexture instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_BlendedOverlayTexture packet_BlendedOverlayTexture = Packet_BlendedOverlayTextureSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_BlendedOverlayTexture;
	}

	public static void Serialize(CitoStream stream, Packet_BlendedOverlayTexture instance)
	{
		if (instance.Base != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.Base);
		}
		if (instance.Mode != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.Mode);
		}
	}

	public static int GetSize(Packet_BlendedOverlayTexture instance)
	{
		int size = 0;
		if (instance.Base != null)
		{
			size += ProtocolParser.GetSize(instance.Base) + 1;
		}
		if (instance.Mode != 0)
		{
			size += ProtocolParser.GetSize(instance.Mode) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_BlendedOverlayTexture instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_BlendedOverlayTextureSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_BlendedOverlayTexture instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_BlendedOverlayTextureSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_BlendedOverlayTexture instance)
	{
		byte[] data = Packet_BlendedOverlayTextureSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
