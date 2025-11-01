using System;

public class Packet_PlayerModeSerializer
{
	public static Packet_PlayerMode DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_PlayerMode instance = new Packet_PlayerMode();
		Packet_PlayerModeSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_PlayerMode DeserializeBuffer(byte[] buffer, int length, Packet_PlayerMode instance)
	{
		Packet_PlayerModeSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_PlayerMode Deserialize(CitoMemoryStream stream, Packet_PlayerMode instance)
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
			if (keyInt <= 32)
			{
				if (keyInt <= 10)
				{
					if (keyInt == 0)
					{
						goto IL_0097;
					}
					if (keyInt == 10)
					{
						instance.PlayerUID = ProtocolParser.ReadString(stream);
						continue;
					}
				}
				else
				{
					if (keyInt == 16)
					{
						instance.GameMode = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 24)
					{
						instance.MoveSpeed = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 32)
					{
						instance.FreeMove = ProtocolParser.ReadUInt32(stream);
						continue;
					}
				}
			}
			else if (keyInt <= 56)
			{
				if (keyInt == 40)
				{
					instance.NoClip = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 48)
				{
					instance.ViewDistance = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 56)
				{
					instance.PickingRange = ProtocolParser.ReadUInt32(stream);
					continue;
				}
			}
			else
			{
				if (keyInt == 64)
				{
					instance.FreeMovePlaneLock = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 72)
				{
					instance.ImmersiveFpMode = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 80)
				{
					instance.RenderMetaBlocks = ProtocolParser.ReadUInt32(stream);
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
		IL_0097:
		return null;
	}

	public static Packet_PlayerMode DeserializeLengthDelimited(CitoMemoryStream stream, Packet_PlayerMode instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_PlayerMode packet_PlayerMode = Packet_PlayerModeSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_PlayerMode;
	}

	public static void Serialize(CitoStream stream, Packet_PlayerMode instance)
	{
		if (instance.PlayerUID != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteString(stream, instance.PlayerUID);
		}
		if (instance.GameMode != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.GameMode);
		}
		if (instance.MoveSpeed != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.MoveSpeed);
		}
		if (instance.FreeMove != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.FreeMove);
		}
		if (instance.NoClip != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.NoClip);
		}
		if (instance.ViewDistance != 0)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt32(stream, instance.ViewDistance);
		}
		if (instance.PickingRange != 0)
		{
			stream.WriteByte(56);
			ProtocolParser.WriteUInt32(stream, instance.PickingRange);
		}
		if (instance.FreeMovePlaneLock != 0)
		{
			stream.WriteByte(64);
			ProtocolParser.WriteUInt32(stream, instance.FreeMovePlaneLock);
		}
		if (instance.ImmersiveFpMode != 0)
		{
			stream.WriteByte(72);
			ProtocolParser.WriteUInt32(stream, instance.ImmersiveFpMode);
		}
		if (instance.RenderMetaBlocks != 0)
		{
			stream.WriteByte(80);
			ProtocolParser.WriteUInt32(stream, instance.RenderMetaBlocks);
		}
	}

	public static int GetSize(Packet_PlayerMode instance)
	{
		int size = 0;
		if (instance.PlayerUID != null)
		{
			size += ProtocolParser.GetSize(instance.PlayerUID) + 1;
		}
		if (instance.GameMode != 0)
		{
			size += ProtocolParser.GetSize(instance.GameMode) + 1;
		}
		if (instance.MoveSpeed != 0)
		{
			size += ProtocolParser.GetSize(instance.MoveSpeed) + 1;
		}
		if (instance.FreeMove != 0)
		{
			size += ProtocolParser.GetSize(instance.FreeMove) + 1;
		}
		if (instance.NoClip != 0)
		{
			size += ProtocolParser.GetSize(instance.NoClip) + 1;
		}
		if (instance.ViewDistance != 0)
		{
			size += ProtocolParser.GetSize(instance.ViewDistance) + 1;
		}
		if (instance.PickingRange != 0)
		{
			size += ProtocolParser.GetSize(instance.PickingRange) + 1;
		}
		if (instance.FreeMovePlaneLock != 0)
		{
			size += ProtocolParser.GetSize(instance.FreeMovePlaneLock) + 1;
		}
		if (instance.ImmersiveFpMode != 0)
		{
			size += ProtocolParser.GetSize(instance.ImmersiveFpMode) + 1;
		}
		if (instance.RenderMetaBlocks != 0)
		{
			size += ProtocolParser.GetSize(instance.RenderMetaBlocks) + 1;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_PlayerMode instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_PlayerModeSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_PlayerMode instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_PlayerModeSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_PlayerMode instance)
	{
		byte[] data = Packet_PlayerModeSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
