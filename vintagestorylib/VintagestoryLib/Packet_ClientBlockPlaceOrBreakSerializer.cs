using System;

public class Packet_ClientBlockPlaceOrBreakSerializer
{
	public static Packet_ClientBlockPlaceOrBreak DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ClientBlockPlaceOrBreak instance = new Packet_ClientBlockPlaceOrBreak();
		Packet_ClientBlockPlaceOrBreakSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ClientBlockPlaceOrBreak DeserializeBuffer(byte[] buffer, int length, Packet_ClientBlockPlaceOrBreak instance)
	{
		Packet_ClientBlockPlaceOrBreakSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ClientBlockPlaceOrBreak Deserialize(CitoMemoryStream stream, Packet_ClientBlockPlaceOrBreak instance)
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
			if (keyInt <= 40)
			{
				if (keyInt <= 16)
				{
					if (keyInt == 0)
					{
						goto IL_00A4;
					}
					if (keyInt == 8)
					{
						instance.X = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 16)
					{
						instance.Y = ProtocolParser.ReadUInt32(stream);
						continue;
					}
				}
				else
				{
					if (keyInt == 24)
					{
						instance.Z = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 32)
					{
						instance.Mode = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 40)
					{
						instance.BlockType = ProtocolParser.ReadUInt32(stream);
						continue;
					}
				}
			}
			else if (keyInt <= 72)
			{
				if (keyInt == 56)
				{
					instance.OnBlockFace = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 64)
				{
					instance.HitX = ProtocolParser.ReadUInt64(stream);
					continue;
				}
				if (keyInt == 72)
				{
					instance.HitY = ProtocolParser.ReadUInt64(stream);
					continue;
				}
			}
			else
			{
				if (keyInt == 80)
				{
					instance.HitZ = ProtocolParser.ReadUInt64(stream);
					continue;
				}
				if (keyInt == 88)
				{
					instance.SelectionBoxIndex = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 96)
				{
					instance.DidOffset = ProtocolParser.ReadUInt32(stream);
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
		IL_00A4:
		return null;
	}

	public static Packet_ClientBlockPlaceOrBreak DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ClientBlockPlaceOrBreak instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ClientBlockPlaceOrBreak packet_ClientBlockPlaceOrBreak = Packet_ClientBlockPlaceOrBreakSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_ClientBlockPlaceOrBreak;
	}

	public static void Serialize(CitoStream stream, Packet_ClientBlockPlaceOrBreak instance)
	{
		if (instance.X != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.X);
		}
		if (instance.Y != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.Y);
		}
		if (instance.Z != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.Z);
		}
		if (instance.Mode != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.Mode);
		}
		if (instance.BlockType != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.BlockType);
		}
		if (instance.OnBlockFace != 0)
		{
			stream.WriteByte(56);
			ProtocolParser.WriteUInt32(stream, instance.OnBlockFace);
		}
		if (instance.HitX != 0L)
		{
			stream.WriteByte(64);
			ProtocolParser.WriteUInt64(stream, instance.HitX);
		}
		if (instance.HitY != 0L)
		{
			stream.WriteByte(72);
			ProtocolParser.WriteUInt64(stream, instance.HitY);
		}
		if (instance.HitZ != 0L)
		{
			stream.WriteByte(80);
			ProtocolParser.WriteUInt64(stream, instance.HitZ);
		}
		if (instance.SelectionBoxIndex != 0)
		{
			stream.WriteByte(88);
			ProtocolParser.WriteUInt32(stream, instance.SelectionBoxIndex);
		}
		if (instance.DidOffset != 0)
		{
			stream.WriteByte(96);
			ProtocolParser.WriteUInt32(stream, instance.DidOffset);
		}
	}

	public static int GetSize(Packet_ClientBlockPlaceOrBreak instance)
	{
		int size = 0;
		if (instance.X != 0)
		{
			size += ProtocolParser.GetSize(instance.X) + 1;
		}
		if (instance.Y != 0)
		{
			size += ProtocolParser.GetSize(instance.Y) + 1;
		}
		if (instance.Z != 0)
		{
			size += ProtocolParser.GetSize(instance.Z) + 1;
		}
		if (instance.Mode != 0)
		{
			size += ProtocolParser.GetSize(instance.Mode) + 1;
		}
		if (instance.BlockType != 0)
		{
			size += ProtocolParser.GetSize(instance.BlockType) + 1;
		}
		if (instance.OnBlockFace != 0)
		{
			size += ProtocolParser.GetSize(instance.OnBlockFace) + 1;
		}
		if (instance.HitX != 0L)
		{
			size += ProtocolParser.GetSize(instance.HitX) + 1;
		}
		if (instance.HitY != 0L)
		{
			size += ProtocolParser.GetSize(instance.HitY) + 1;
		}
		if (instance.HitZ != 0L)
		{
			size += ProtocolParser.GetSize(instance.HitZ) + 1;
		}
		if (instance.SelectionBoxIndex != 0)
		{
			size += ProtocolParser.GetSize(instance.SelectionBoxIndex) + 1;
		}
		if (instance.DidOffset != 0)
		{
			size += ProtocolParser.GetSize(instance.DidOffset) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_ClientBlockPlaceOrBreak instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_ClientBlockPlaceOrBreakSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_ClientBlockPlaceOrBreak instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_ClientBlockPlaceOrBreakSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ClientBlockPlaceOrBreak instance)
	{
		byte[] data = Packet_ClientBlockPlaceOrBreakSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
