using System;

public class Packet_RuntimeSettingSerializer
{
	public static Packet_RuntimeSetting DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_RuntimeSetting instance = new Packet_RuntimeSetting();
		Packet_RuntimeSettingSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_RuntimeSetting DeserializeBuffer(byte[] buffer, int length, Packet_RuntimeSetting instance)
	{
		Packet_RuntimeSettingSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_RuntimeSetting Deserialize(CitoMemoryStream stream, Packet_RuntimeSetting instance)
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
				goto IL_003B;
			}
			if (keyInt != 8)
			{
				if (keyInt != 16)
				{
					ProtocolParser.SkipKey(stream, Key.Create(keyInt));
				}
				else
				{
					instance.ItemCollectMode = ProtocolParser.ReadUInt32(stream);
				}
			}
			else
			{
				instance.ImmersiveFpMode = ProtocolParser.ReadUInt32(stream);
			}
		}
		if (keyInt >= 0)
		{
			return null;
		}
		return instance;
		IL_003B:
		return null;
	}

	public static Packet_RuntimeSetting DeserializeLengthDelimited(CitoMemoryStream stream, Packet_RuntimeSetting instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_RuntimeSetting packet_RuntimeSetting = Packet_RuntimeSettingSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_RuntimeSetting;
	}

	public static void Serialize(CitoStream stream, Packet_RuntimeSetting instance)
	{
		if (instance.ImmersiveFpMode != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.ImmersiveFpMode);
		}
		if (instance.ItemCollectMode != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.ItemCollectMode);
		}
	}

	public static int GetSize(Packet_RuntimeSetting instance)
	{
		int size = 0;
		if (instance.ImmersiveFpMode != 0)
		{
			size += ProtocolParser.GetSize(instance.ImmersiveFpMode) + 1;
		}
		if (instance.ItemCollectMode != 0)
		{
			size += ProtocolParser.GetSize(instance.ItemCollectMode) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_RuntimeSetting instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_RuntimeSettingSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_RuntimeSetting instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_RuntimeSettingSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_RuntimeSetting instance)
	{
		byte[] data = Packet_RuntimeSettingSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
