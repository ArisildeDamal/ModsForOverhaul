using System;

public class Packet_EntityPositionSerializer
{
	public static Packet_EntityPosition DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_EntityPosition instance = new Packet_EntityPosition();
		Packet_EntityPositionSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_EntityPosition DeserializeBuffer(byte[] buffer, int length, Packet_EntityPosition instance)
	{
		Packet_EntityPositionSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_EntityPosition Deserialize(CitoMemoryStream stream, Packet_EntityPosition instance)
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
			if (keyInt <= 72)
			{
				if (keyInt <= 32)
				{
					if (keyInt <= 8)
					{
						if (keyInt == 0)
						{
							goto IL_0131;
						}
						if (keyInt == 8)
						{
							instance.EntityId = ProtocolParser.ReadUInt64(stream);
							continue;
						}
					}
					else
					{
						if (keyInt == 16)
						{
							instance.X = ProtocolParser.ReadUInt64(stream);
							continue;
						}
						if (keyInt == 24)
						{
							instance.Y = ProtocolParser.ReadUInt64(stream);
							continue;
						}
						if (keyInt == 32)
						{
							instance.Z = ProtocolParser.ReadUInt64(stream);
							continue;
						}
					}
				}
				else if (keyInt <= 48)
				{
					if (keyInt == 40)
					{
						instance.Yaw = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 48)
					{
						instance.Pitch = ProtocolParser.ReadUInt32(stream);
						continue;
					}
				}
				else
				{
					if (keyInt == 56)
					{
						instance.Roll = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 64)
					{
						instance.HeadYaw = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 72)
					{
						instance.HeadPitch = ProtocolParser.ReadUInt32(stream);
						continue;
					}
				}
			}
			else if (keyInt <= 112)
			{
				if (keyInt <= 88)
				{
					if (keyInt == 80)
					{
						instance.BodyYaw = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 88)
					{
						instance.Controls = ProtocolParser.ReadUInt32(stream);
						continue;
					}
				}
				else
				{
					if (keyInt == 96)
					{
						instance.Tick = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 104)
					{
						instance.PositionVersion = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 112)
					{
						instance.MotionX = ProtocolParser.ReadUInt64(stream);
						continue;
					}
				}
			}
			else if (keyInt <= 136)
			{
				if (keyInt == 120)
				{
					instance.MotionY = ProtocolParser.ReadUInt64(stream);
					continue;
				}
				if (keyInt == 128)
				{
					instance.MotionZ = ProtocolParser.ReadUInt64(stream);
					continue;
				}
				if (keyInt == 136)
				{
					instance.Teleport = ProtocolParser.ReadBool(stream);
					continue;
				}
			}
			else
			{
				if (keyInt == 144)
				{
					instance.TagsBitmask1 = ProtocolParser.ReadUInt64(stream);
					continue;
				}
				if (keyInt == 152)
				{
					instance.TagsBitmask2 = ProtocolParser.ReadUInt64(stream);
					continue;
				}
				if (keyInt == 160)
				{
					instance.MountControls = ProtocolParser.ReadUInt32(stream);
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
		IL_0131:
		return null;
	}

	public static Packet_EntityPosition DeserializeLengthDelimited(CitoMemoryStream stream, Packet_EntityPosition instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_EntityPosition packet_EntityPosition = Packet_EntityPositionSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_EntityPosition;
	}

	public static void Serialize(CitoStream stream, Packet_EntityPosition instance)
	{
		if (instance.EntityId != 0L)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt64(stream, instance.EntityId);
		}
		if (instance.X != 0L)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt64(stream, instance.X);
		}
		if (instance.Y != 0L)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt64(stream, instance.Y);
		}
		if (instance.Z != 0L)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt64(stream, instance.Z);
		}
		if (instance.Yaw != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.Yaw);
		}
		if (instance.Pitch != 0)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt32(stream, instance.Pitch);
		}
		if (instance.Roll != 0)
		{
			stream.WriteByte(56);
			ProtocolParser.WriteUInt32(stream, instance.Roll);
		}
		if (instance.HeadYaw != 0)
		{
			stream.WriteByte(64);
			ProtocolParser.WriteUInt32(stream, instance.HeadYaw);
		}
		if (instance.HeadPitch != 0)
		{
			stream.WriteByte(72);
			ProtocolParser.WriteUInt32(stream, instance.HeadPitch);
		}
		if (instance.BodyYaw != 0)
		{
			stream.WriteByte(80);
			ProtocolParser.WriteUInt32(stream, instance.BodyYaw);
		}
		if (instance.Controls != 0)
		{
			stream.WriteByte(88);
			ProtocolParser.WriteUInt32(stream, instance.Controls);
		}
		if (instance.Tick != 0)
		{
			stream.WriteByte(96);
			ProtocolParser.WriteUInt32(stream, instance.Tick);
		}
		if (instance.PositionVersion != 0)
		{
			stream.WriteByte(104);
			ProtocolParser.WriteUInt32(stream, instance.PositionVersion);
		}
		if (instance.MotionX != 0L)
		{
			stream.WriteByte(112);
			ProtocolParser.WriteUInt64(stream, instance.MotionX);
		}
		if (instance.MotionY != 0L)
		{
			stream.WriteByte(120);
			ProtocolParser.WriteUInt64(stream, instance.MotionY);
		}
		if (instance.MotionZ != 0L)
		{
			stream.WriteKey(16, 0);
			ProtocolParser.WriteUInt64(stream, instance.MotionZ);
		}
		if (instance.Teleport)
		{
			stream.WriteKey(17, 0);
			ProtocolParser.WriteBool(stream, instance.Teleport);
		}
		if (instance.TagsBitmask1 != 0L)
		{
			stream.WriteKey(18, 0);
			ProtocolParser.WriteUInt64(stream, instance.TagsBitmask1);
		}
		if (instance.TagsBitmask2 != 0L)
		{
			stream.WriteKey(19, 0);
			ProtocolParser.WriteUInt64(stream, instance.TagsBitmask2);
		}
		if (instance.MountControls != 0)
		{
			stream.WriteKey(20, 0);
			ProtocolParser.WriteUInt32(stream, instance.MountControls);
		}
	}

	public static int GetSize(Packet_EntityPosition instance)
	{
		int size = 0;
		if (instance.EntityId != 0L)
		{
			size += ProtocolParser.GetSize(instance.EntityId) + 1;
		}
		if (instance.X != 0L)
		{
			size += ProtocolParser.GetSize(instance.X) + 1;
		}
		if (instance.Y != 0L)
		{
			size += ProtocolParser.GetSize(instance.Y) + 1;
		}
		if (instance.Z != 0L)
		{
			size += ProtocolParser.GetSize(instance.Z) + 1;
		}
		if (instance.Yaw != 0)
		{
			size += ProtocolParser.GetSize(instance.Yaw) + 1;
		}
		if (instance.Pitch != 0)
		{
			size += ProtocolParser.GetSize(instance.Pitch) + 1;
		}
		if (instance.Roll != 0)
		{
			size += ProtocolParser.GetSize(instance.Roll) + 1;
		}
		if (instance.HeadYaw != 0)
		{
			size += ProtocolParser.GetSize(instance.HeadYaw) + 1;
		}
		if (instance.HeadPitch != 0)
		{
			size += ProtocolParser.GetSize(instance.HeadPitch) + 1;
		}
		if (instance.BodyYaw != 0)
		{
			size += ProtocolParser.GetSize(instance.BodyYaw) + 1;
		}
		if (instance.Controls != 0)
		{
			size += ProtocolParser.GetSize(instance.Controls) + 1;
		}
		if (instance.Tick != 0)
		{
			size += ProtocolParser.GetSize(instance.Tick) + 1;
		}
		if (instance.PositionVersion != 0)
		{
			size += ProtocolParser.GetSize(instance.PositionVersion) + 1;
		}
		if (instance.MotionX != 0L)
		{
			size += ProtocolParser.GetSize(instance.MotionX) + 1;
		}
		if (instance.MotionY != 0L)
		{
			size += ProtocolParser.GetSize(instance.MotionY) + 1;
		}
		if (instance.MotionZ != 0L)
		{
			size += ProtocolParser.GetSize(instance.MotionZ) + 2;
		}
		if (instance.Teleport)
		{
			size += 3;
		}
		if (instance.TagsBitmask1 != 0L)
		{
			size += ProtocolParser.GetSize(instance.TagsBitmask1) + 2;
		}
		if (instance.TagsBitmask2 != 0L)
		{
			size += ProtocolParser.GetSize(instance.TagsBitmask2) + 2;
		}
		if (instance.MountControls != 0)
		{
			size += ProtocolParser.GetSize(instance.MountControls) + 2;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_EntityPosition instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_EntityPositionSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_EntityPosition instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_EntityPositionSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_EntityPosition instance)
	{
		byte[] data = Packet_EntityPositionSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
