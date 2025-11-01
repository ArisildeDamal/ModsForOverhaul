using System;

public class Packet_PlayerDataSerializer
{
	public static Packet_PlayerData DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_PlayerData instance = new Packet_PlayerData();
		Packet_PlayerDataSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_PlayerData DeserializeBuffer(byte[] buffer, int length, Packet_PlayerData instance)
	{
		Packet_PlayerDataSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_PlayerData Deserialize(CitoMemoryStream stream, Packet_PlayerData instance)
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
							instance.ClientId = ProtocolParser.ReadUInt32(stream);
							continue;
						}
					}
					else
					{
						if (keyInt == 16)
						{
							instance.EntityId = ProtocolParser.ReadUInt64(stream);
							continue;
						}
						if (keyInt == 24)
						{
							instance.GameMode = ProtocolParser.ReadUInt32(stream);
							continue;
						}
						if (keyInt == 32)
						{
							instance.MoveSpeed = ProtocolParser.ReadUInt32(stream);
							continue;
						}
					}
				}
				else if (keyInt <= 48)
				{
					if (keyInt == 40)
					{
						instance.FreeMove = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 48)
					{
						instance.NoClip = ProtocolParser.ReadUInt32(stream);
						continue;
					}
				}
				else
				{
					if (keyInt == 58)
					{
						instance.InventoryContentsAdd(Packet_InventoryContentsSerializer.DeserializeLengthDelimitedNew(stream));
						continue;
					}
					if (keyInt == 66)
					{
						instance.PlayerUID = ProtocolParser.ReadString(stream);
						continue;
					}
					if (keyInt == 72)
					{
						instance.PickingRange = ProtocolParser.ReadUInt32(stream);
						continue;
					}
				}
			}
			else if (keyInt <= 114)
			{
				if (keyInt <= 88)
				{
					if (keyInt == 80)
					{
						instance.FreeMovePlaneLock = ProtocolParser.ReadUInt32(stream);
						continue;
					}
					if (keyInt == 88)
					{
						instance.AreaSelectionMode = ProtocolParser.ReadUInt32(stream);
						continue;
					}
				}
				else
				{
					if (keyInt == 98)
					{
						instance.PrivilegesAdd(ProtocolParser.ReadString(stream));
						continue;
					}
					if (keyInt == 106)
					{
						instance.PlayerName = ProtocolParser.ReadString(stream);
						continue;
					}
					if (keyInt == 114)
					{
						instance.Entitlements = ProtocolParser.ReadString(stream);
						continue;
					}
				}
			}
			else if (keyInt <= 136)
			{
				if (keyInt == 120)
				{
					instance.HotbarSlotId = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 128)
				{
					instance.Deaths = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 136)
				{
					instance.Spawnx = ProtocolParser.ReadUInt32(stream);
					continue;
				}
			}
			else
			{
				if (keyInt == 144)
				{
					instance.Spawny = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 152)
				{
					instance.Spawnz = ProtocolParser.ReadUInt32(stream);
					continue;
				}
				if (keyInt == 162)
				{
					instance.RoleCode = ProtocolParser.ReadString(stream);
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

	public static Packet_PlayerData DeserializeLengthDelimited(CitoMemoryStream stream, Packet_PlayerData instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_PlayerData packet_PlayerData = Packet_PlayerDataSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_PlayerData;
	}

	public static void Serialize(CitoStream stream, Packet_PlayerData instance)
	{
		if (instance.ClientId != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.ClientId);
		}
		if (instance.EntityId != 0L)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt64(stream, instance.EntityId);
		}
		if (instance.GameMode != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.GameMode);
		}
		if (instance.MoveSpeed != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.MoveSpeed);
		}
		if (instance.FreeMove != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.FreeMove);
		}
		if (instance.NoClip != 0)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt32(stream, instance.NoClip);
		}
		if (instance.InventoryContents != null)
		{
			Packet_InventoryContents[] elems = instance.InventoryContents;
			int elemCount = instance.InventoryContentsCount;
			int i = 0;
			while (i < elems.Length && i < elemCount)
			{
				stream.WriteByte(58);
				Packet_InventoryContentsSerializer.SerializeWithSize(stream, elems[i]);
				i++;
			}
		}
		if (instance.PlayerUID != null)
		{
			stream.WriteByte(66);
			ProtocolParser.WriteString(stream, instance.PlayerUID);
		}
		if (instance.PickingRange != 0)
		{
			stream.WriteByte(72);
			ProtocolParser.WriteUInt32(stream, instance.PickingRange);
		}
		if (instance.FreeMovePlaneLock != 0)
		{
			stream.WriteByte(80);
			ProtocolParser.WriteUInt32(stream, instance.FreeMovePlaneLock);
		}
		if (instance.AreaSelectionMode != 0)
		{
			stream.WriteByte(88);
			ProtocolParser.WriteUInt32(stream, instance.AreaSelectionMode);
		}
		if (instance.Privileges != null)
		{
			string[] elems2 = instance.Privileges;
			int elemCount2 = instance.PrivilegesCount;
			int j = 0;
			while (j < elems2.Length && j < elemCount2)
			{
				stream.WriteByte(98);
				ProtocolParser.WriteString(stream, elems2[j]);
				j++;
			}
		}
		if (instance.PlayerName != null)
		{
			stream.WriteByte(106);
			ProtocolParser.WriteString(stream, instance.PlayerName);
		}
		if (instance.Entitlements != null)
		{
			stream.WriteByte(114);
			ProtocolParser.WriteString(stream, instance.Entitlements);
		}
		if (instance.HotbarSlotId != 0)
		{
			stream.WriteByte(120);
			ProtocolParser.WriteUInt32(stream, instance.HotbarSlotId);
		}
		if (instance.Deaths != 0)
		{
			stream.WriteKey(16, 0);
			ProtocolParser.WriteUInt32(stream, instance.Deaths);
		}
		if (instance.Spawnx != 0)
		{
			stream.WriteKey(17, 0);
			ProtocolParser.WriteUInt32(stream, instance.Spawnx);
		}
		if (instance.Spawny != 0)
		{
			stream.WriteKey(18, 0);
			ProtocolParser.WriteUInt32(stream, instance.Spawny);
		}
		if (instance.Spawnz != 0)
		{
			stream.WriteKey(19, 0);
			ProtocolParser.WriteUInt32(stream, instance.Spawnz);
		}
		if (instance.RoleCode != null)
		{
			stream.WriteKey(20, 2);
			ProtocolParser.WriteString(stream, instance.RoleCode);
		}
	}

	public static int GetSize(Packet_PlayerData instance)
	{
		int size = 0;
		if (instance.ClientId != 0)
		{
			size += ProtocolParser.GetSize(instance.ClientId) + 1;
		}
		if (instance.EntityId != 0L)
		{
			size += ProtocolParser.GetSize(instance.EntityId) + 1;
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
		if (instance.InventoryContents != null)
		{
			for (int i = 0; i < instance.InventoryContentsCount; i++)
			{
				int packetlength = Packet_InventoryContentsSerializer.GetSize(instance.InventoryContents[i]);
				size += packetlength + ProtocolParser.GetSize(packetlength) + 1;
			}
		}
		if (instance.PlayerUID != null)
		{
			size += ProtocolParser.GetSize(instance.PlayerUID) + 1;
		}
		if (instance.PickingRange != 0)
		{
			size += ProtocolParser.GetSize(instance.PickingRange) + 1;
		}
		if (instance.FreeMovePlaneLock != 0)
		{
			size += ProtocolParser.GetSize(instance.FreeMovePlaneLock) + 1;
		}
		if (instance.AreaSelectionMode != 0)
		{
			size += ProtocolParser.GetSize(instance.AreaSelectionMode) + 1;
		}
		if (instance.Privileges != null)
		{
			for (int j = 0; j < instance.PrivilegesCount; j++)
			{
				string i2 = instance.Privileges[j];
				size += ProtocolParser.GetSize(i2) + 1;
			}
		}
		if (instance.PlayerName != null)
		{
			size += ProtocolParser.GetSize(instance.PlayerName) + 1;
		}
		if (instance.Entitlements != null)
		{
			size += ProtocolParser.GetSize(instance.Entitlements) + 1;
		}
		if (instance.HotbarSlotId != 0)
		{
			size += ProtocolParser.GetSize(instance.HotbarSlotId) + 1;
		}
		if (instance.Deaths != 0)
		{
			size += ProtocolParser.GetSize(instance.Deaths) + 2;
		}
		if (instance.Spawnx != 0)
		{
			size += ProtocolParser.GetSize(instance.Spawnx) + 2;
		}
		if (instance.Spawny != 0)
		{
			size += ProtocolParser.GetSize(instance.Spawny) + 2;
		}
		if (instance.Spawnz != 0)
		{
			size += ProtocolParser.GetSize(instance.Spawnz) + 2;
		}
		if (instance.RoleCode != null)
		{
			size += ProtocolParser.GetSize(instance.RoleCode) + 2;
		}
		instance.size = size;
		return size;
	}

	public static void SerializeWithSize(CitoStream stream, Packet_PlayerData instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_PlayerDataSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_PlayerData instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_PlayerDataSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_PlayerData instance)
	{
		byte[] data = Packet_PlayerDataSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
